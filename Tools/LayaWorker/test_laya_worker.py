"""실제 전투·모델 없이 추론 경계와 HTTP 왕복을 검사한다."""

import copy
import io
import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch
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
                      {"truncated": False, "state_tokens_dropped": 0, "options": {"total": 2, "distinct": 1}}):
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

    def test_client_does_not_follow_redirect(self):
        from http.server import BaseHTTPRequestHandler, HTTPServer

        class Redirect(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_POST(self):
                self.send_response(307)
                self.send_header("Location", self.server.destination)
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
