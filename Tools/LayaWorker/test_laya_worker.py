"""실제 전투·모델 없이 추론 경계와 HTTP 왕복을 검사한다."""

import copy
import io
import json
from pathlib import Path
import tempfile
import threading
import unittest
from types import SimpleNamespace
from unittest.mock import Mock, patch
from urllib.error import HTTPError
from urllib.request import Request, urlopen

import laya_worker as worker


def sample_request():
    return {
        "protocol_version": 1, "request_id": "request-1", "session_id": "test-session", "state_seq": 7,
        "observation": {"visible_telegraph": "incoming strike", "hp_ratio": 0.3},
        "candidates": [{"id": "attack", "description": "Attack in place."},
                       {"id": "dodge", "description": "Dodge the visible strike."}],
    }


class FakeBackend:
    metadata = {"backend": "test_double_not_laya"}

    def __init__(self):
        self.calls = 0
        self.result = {
            "answers": {"action": {"choice": "a1", "probabilities": {"a0": 0.1, "a1": 0.9}}},
            "usage": {"truncated": False, "state_tokens_dropped": 0, "input_tokens": 100, "output_tokens": 0},
        }

    def predict(self, observation, questions):
        self.calls += 1
        self.observation = observation
        self.questions = questions
        return copy.deepcopy(self.result)


class DecisionTests(unittest.TestCase):
    def setUp(self):
        self.backend = FakeBackend()
        self.config = worker.load_config(worker.ROOT / "worker-config.json")
        self.worker = worker.DecisionWorker(self.backend, self.config)

    def test_maps_neutral_labels_and_preserves_state(self):
        request = sample_request()
        response = self.worker.decide(request)
        self.assertEqual(response["selected_action_id"], "dodge")
        self.assertEqual(response["state_seq"], 7)
        self.assertEqual(response["probabilities"], {"attack": 0.1, "dodge": 0.9})
        self.assertEqual(self.backend.observation, request["observation"])
        self.assertEqual(list(self.backend.questions["action"]["criteria"]), ["a0", "a1"])

    def test_retry_and_conflicting_request_id(self):
        request = sample_request()
        first = self.worker.decide(request)
        retry = self.worker.decide(request)
        self.assertTrue(retry["cached"])
        self.assertEqual(first["completed_at_utc"], retry["completed_at_utc"])
        self.assertEqual(self.backend.calls, 1)
        request["state_seq"] += 1
        with self.assertRaises(worker.WorkerError) as caught:
            self.worker.decide(request)
        self.assertEqual(caught.exception.status, 409)

    def test_session_scopes_request_id(self):
        request = sample_request()
        self.worker.decide(request)
        request["session_id"] = "another-session"
        self.assertFalse(self.worker.decide(request)["cached"])
        self.assertEqual(self.backend.calls, 2)

    def test_single_candidate_skips_inference(self):
        request = sample_request()
        request["candidates"] = request["candidates"][:1]
        response = self.worker.decide(request)
        self.assertEqual(response["decision_source"], "single_candidate")
        self.assertEqual(self.backend.calls, 0)

    def test_busy_does_not_queue_another_inference(self):
        with self.worker.lock:
            with self.assertRaises(worker.WorkerError) as caught:
                self.worker.decide(sample_request())
        self.assertEqual(caught.exception.status, 503)
        self.assertEqual(self.backend.calls, 0)

    def test_invalid_input_rejected_before_inference(self):
        mutations = [
            lambda request: request.update(state_seq=True),
            lambda request: request.update(protocol_version=True),
            lambda request: request.update(extra="unknown"),
            lambda request: request.update(candidates=[]),
            lambda request: request.update(candidates=request["candidates"] * 2),
            lambda request: request.update(observation={"large": "x" * 4100}),
            lambda request: request.update(observation={"hp": float("nan")}),
            lambda request: request.update(request_id="invalid id"),
        ]
        for mutate in mutations:
            with self.subTest(mutate=mutate):
                request = sample_request()
                mutate(request)
                with self.assertRaises(worker.WorkerError):
                    self.worker.decide(request)
        self.assertEqual(self.backend.calls, 0)

    def test_truncation_and_option_collision_are_errors(self):
        for usage in ({"truncated": True, "state_tokens_dropped": 5},
                      {"truncated": False, "state_tokens_dropped": 0, "options": {"action": {"total": 2, "distinct": 1}}}):
            with self.subTest(usage=usage):
                self.backend.result["usage"] = usage
                with self.assertRaises(worker.WorkerError) as caught:
                    self.worker.decide(sample_request())
                self.assertEqual(caught.exception.status, 422)
                self.assertFalse(self.worker.cache)

    def test_invalid_model_output_never_becomes_action(self):
        variants = [None, {}, {"choice": "a9", "probabilities": {"a0": 0.1, "a1": 0.9}},
                    {"choice": "a0", "probabilities": {"a0": 0.1, "a1": 0.9}},
                    {"choice": "a1", "probabilities": {"a0": 0.5, "a1": float("nan")}}]
        for answer in variants:
            with self.subTest(answer=answer):
                self.backend.result["answers"]["action"] = answer
                with self.assertRaises(worker.WorkerError) as caught:
                    self.worker.decide(sample_request())
                self.assertEqual(caught.exception.code, "invalid_model_output")
        self.assertFalse(self.worker.cache)

    def test_duplicate_keys_and_nonfinite_json(self):
        for raw in ('{"key":1,"key":2}', '{"hp":NaN}', '{"hp":1e999}', b'\xff'):
            with self.subTest(raw=raw):
                with self.assertRaises(worker.WorkerError):
                    worker.decode_json(raw)

    def test_batch_preserves_failure_and_never_overwrites_output(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "output.jsonl"
            requests = [sample_request(), sample_request()]
            requests[1]["state_seq"] += 1
            self.assertEqual(worker.write_results(requests, path, self.worker.decide), 1)
            rows = [json.loads(row) for row in path.read_text(encoding="utf-8").splitlines()]
            self.assertEqual(rows[1]["error"]["code"], "request_conflict")
            with self.assertRaises(FileExistsError):
                worker.write_results(requests, path, self.worker.decide)

    def test_shipped_samples_and_model_input_inspection(self):
        requests = worker.read_requests(worker.ROOT / "data/examples/requests.jsonl")
        self.assertEqual(len(requests), 3)
        with patch("sys.stdout", new_callable=io.StringIO) as output:
            self.assertEqual(worker.main(["inspect"]), 0)
            self.assertEqual(len(output.getvalue().splitlines()), 3)

    def test_mlx_load_arguments_without_importing_real_model(self):
        from types import SimpleNamespace
        module = SimpleNamespace(load=lambda *args, **kwargs: SimpleNamespace(predict=None))
        with patch.object(worker.sys, "platform", "darwin"), patch.object(worker.platform, "machine", return_value="arm64"), \
                patch.object(worker.importlib.metadata, "version", return_value="0.6.0"), \
                patch.dict("sys.modules", {"laya_mlx": module}), patch.object(module, "load", wraps=module.load) as load:
            backend = worker.LayaBackend(self.config, offline=True)
        self.assertEqual(load.call_args.kwargs["revision"], self.config["revision"])
        self.assertTrue(load.call_args.kwargs["local_files_only"])
        self.assertEqual(backend.metadata["backend"], "laya-mlx")


class CandidateTokenTests(unittest.TestCase):
    def setUp(self):
        self.backend = worker.LayaBackend.__new__(worker.LayaBackend)
        self.backend.metadata = {"backend": "token_test_double_not_laya"}
        self.backend.agent = SimpleNamespace(
            tok=SimpleNamespace(mask_token="[MASK]"), cfg={"max_len": 1024, "head_max_len": 256},
            _to_internal=lambda definition: definition, predict=Mock(return_value=FakeBackend().result),
        )
        self.config = worker.load_config(worker.ROOT / "worker-config.json")

    def decide_with_tokens(self, full_options, actual_options, max_len=1024):
        self.backend.agent.cfg["max_len"] = max_len
        head = [0, 1, 2]
        markers = []
        for actual in actual_options:
            markers.append(len(head))
            head += [99] + actual
        head.append(3)
        rendered = [f"option-{index}" for index in range(len(full_options))]
        token_map = {" " + option: tokens for option, tokens in zip(rendered, full_options)}
        common = SimpleNamespace(
            build_head=Mock(return_value=(head, markers, {})),
            render_options=lambda question: rendered,
            encode_text=lambda tokenizer, text, **kwargs: {"input_ids": token_map[text]},
        )
        processor = worker.DecisionWorker(self.backend, self.config)
        with patch.dict("sys.modules", {"laya_mlx.common": common}):
            try:
                return processor.decide(sample_request())
            finally:
                common.build_head.assert_called_once_with(self.backend.agent.tok, unittest.mock.ANY, 256)
                if not self.backend.agent.predict.called:
                    self.assertFalse(processor.cache)

    def test_distinct_options_cut_at_sdk_per_option_cap_are_rejected(self):
        full = [list(range(10, 59)), list(range(110, 159))]
        with self.assertRaises(worker.WorkerError) as caught:
            self.decide_with_tokens(full, [tokens[:48] for tokens in full])
        self.assertEqual(caught.exception.code, "options_truncated")
        self.assertEqual(caught.exception.status, 422)
        self.backend.agent.predict.assert_not_called()

    def test_distinct_options_cut_by_shared_head_budget_are_rejected(self):
        full = [[10, 11, 12, 13], [20, 21, 22, 23]]
        with self.assertRaises(worker.WorkerError) as caught:
            self.decide_with_tokens(full, [tokens[:3] for tokens in full])
        self.assertEqual(caught.exception.code, "options_truncated")
        self.backend.agent.predict.assert_not_called()

    def test_sequence_limit_cannot_cut_the_last_candidate(self):
        full = [[10, 11], [20, 21]]
        with self.assertRaises(worker.WorkerError):
            self.decide_with_tokens(full, full, max_len=8)
        self.backend.agent.predict.assert_not_called()

    def test_complete_option_spans_allow_inference(self):
        full = [[10, 11], [20, 21]]
        self.assertEqual(self.decide_with_tokens(full, full)["selected_action_id"], "dodge")
        self.backend.agent.predict.assert_called_once()


class HttpTests(unittest.TestCase):
    def setUp(self):
        self.worker = worker.DecisionWorker(FakeBackend(), worker.load_config(worker.ROOT / "worker-config.json"))
        self.server = worker.create_server(self.worker, "127.0.0.1", 0, "test-secret")
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.url = f"http://127.0.0.1:{self.server.server_port}"

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=2)

    def test_http_round_trip_health_and_retry(self):
        health = Request(self.url + "/health", headers={"Authorization": "Bearer test-secret"})
        with urlopen(health, timeout=2) as response:
            self.assertTrue(json.load(response)["ready"])
        result = worker.send_request(self.url, sample_request(), "test-secret", timeout=2)
        self.assertEqual(result["selected_action_id"], "dodge")
        self.assertTrue(worker.send_request(self.url, sample_request(), "test-secret", timeout=2)["cached"])

    def test_http_auth_and_bad_body(self):
        with self.assertRaises(worker.WorkerError) as caught:
            worker.send_request(self.url, sample_request(), "wrong", timeout=2)
        self.assertEqual(caught.exception.status, 401)
        request = Request(self.url + "/v1/decide", data=b'{"x":NaN}',
                          headers={"Authorization": "Bearer test-secret", "Content-Type": "application/json"})
        with self.assertRaises(HTTPError) as caught:
            urlopen(request, timeout=2)
        self.assertEqual(caught.exception.code, 400)

    def test_nonlocal_binding_requires_token(self):
        with self.assertRaises(worker.WorkerError):
            worker.create_server(self.worker, "0.0.0.0", 0)

    def test_client_rejects_another_state(self):
        original = self.worker.decide

        def wrong_state(request):
            return {**original(request), "state_seq": 99}

        with patch.object(self.worker, "decide", side_effect=wrong_state):
            with self.assertRaises(worker.WorkerError) as caught:
                worker.send_request(self.url, sample_request(), "test-secret", timeout=2)
        self.assertEqual(caught.exception.code, "stale_response")

    def test_invalid_action_types_are_recorded_and_batch_continues(self):
        original = self.worker.decide
        for selected in ([], {}, None, 1):
            with self.subTest(selected=selected), tempfile.TemporaryDirectory() as directory:
                requests = [sample_request(), sample_request()]
                requests[1]["request_id"] = "second"

                def reply(request):
                    result = original(request)
                    return {**result, "selected_action_id": selected} if request["request_id"] == "request-1" else result

                with patch.object(self.worker, "decide", side_effect=reply):
                    output = Path(directory) / "results.jsonl"
                    status = worker.write_results(requests, output, lambda request: worker.send_request(
                        self.url, request, "test-secret", 2))
                rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]
                self.assertEqual(status, 1)
                self.assertEqual(rows[0]["error"]["code"], "invalid_response")
                self.assertEqual(rows[1]["selected_action_id"], "dodge")

    def test_reset_while_reading_http_error_keeps_status_and_continues_batch(self):
        class ResetBody(io.BytesIO):
            def read(self, *args):
                raise ConnectionResetError("response body reset")

        actual_open = worker.build_opener().open
        error = HTTPError(self.url, 307, "redirect", {}, ResetBody())
        requests = [sample_request(), sample_request()]
        requests[1]["request_id"] = "second"
        calls = 0

        def open_request(request, **kwargs):
            nonlocal calls
            calls += 1
            if calls == 1:
                raise error
            return actual_open(request, **kwargs)

        with tempfile.TemporaryDirectory() as directory, patch.object(
                worker, "build_opener", return_value=SimpleNamespace(open=open_request)):
            output = Path(directory) / "results.jsonl"
            status = worker.write_results(requests, output, lambda request: worker.send_request(
                self.url, request, "test-secret", 2))
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]
        self.assertEqual(status, 1)
        self.assertEqual(calls, 2)
        self.assertEqual(rows[0]["error"]["code"], "remote_error")
        self.assertEqual(rows[0]["error"]["http_status"], 307)
        self.assertIn("307", rows[0]["error"]["message"])
        self.assertEqual(rows[1]["selected_action_id"], "dodge")

    def test_client_does_not_follow_redirect(self):
        from http.server import BaseHTTPRequestHandler, HTTPServer

        class Redirect(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_POST(self):
                # 본문을 남겨 닫으면 응답 헤더 수신 전에도 RST가 발생해 리다이렉트 검사가 불안정해진다.
                self.rfile.read(int(self.headers["Content-Length"]))
                self.send_response(307)
                self.send_header("Location", self.server.destination)
                self.send_header("Content-Length", "0")
                self.end_headers()

        redirect = HTTPServer(("127.0.0.1", 0), Redirect)
        redirect.destination = self.url + "/v1/decide"
        thread = threading.Thread(target=redirect.serve_forever, daemon=True)
        thread.start()
        try:
            with self.assertRaises(worker.WorkerError) as caught:
                worker.send_request(f"http://127.0.0.1:{redirect.server_port}", sample_request(), "test-secret", 2)
            self.assertEqual(caught.exception.status, 307)
            self.assertFalse(self.worker.cache)
        finally:
            redirect.shutdown()
            redirect.server_close()
            thread.join(timeout=2)


if __name__ == "__main__":
    unittest.main()
