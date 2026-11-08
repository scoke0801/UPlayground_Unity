"""선별 관측과 행동 후보를 Laya에 전달하는 독립 Mac 추론 처리자."""

from __future__ import annotations

import argparse
from collections import OrderedDict
from datetime import datetime, timezone
import hashlib
import hmac
from http.client import HTTPException
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import importlib.metadata
import json
import math
import os
from pathlib import Path
import platform
import re
import sys
import threading
import time

from worker_monitor import TrafficMonitor
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener


ROOT = Path(__file__).resolve().parent
PROTOCOL_VERSION = 1
WORKER_VERSION = "1.0.1"
MAX_BODY_BYTES = 65536
MAX_OBSERVATION_BYTES = 4096
MAX_CANDIDATES = 8
CACHE_SIZE = 256
IDENTIFIER = re.compile(r"[A-Za-z0-9_.:-]{1,128}\Z")


class WorkerError(Exception):
    """호출자가 실패 원인과 재시도 여부를 구분할 수 있는 오류."""

    def __init__(self, code, message, status=400):
        super().__init__(message)
        self.code = code
        self.status = status


def encode_json(value):
    """파일·통신·요청 해시가 공유하는 JSON 표현을 만든다."""
    return json.dumps(value, ensure_ascii=False, allow_nan=False, separators=(",", ":"))


def decode_json(raw):
    """중복 키와 비표준 숫자를 조용히 받아들이지 않는다."""
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError("중복 JSON 키")
            result[key] = value
        return result

    def invalid_constant(value):
        raise ValueError("유효하지 않은 숫자")

    try:
        value = json.loads(raw, object_pairs_hook=pairs, parse_constant=invalid_constant)
        encode_json(value)
        return value
    except (ValueError, UnicodeError, RecursionError) as error:
        raise WorkerError("invalid_json", "유효한 UTF-8 JSON이 필요합니다.") from error


def require_identifier(value, name):
    if not isinstance(value, str) or not IDENTIFIER.fullmatch(value):
        raise WorkerError("invalid_request", f"{name}: 영문·숫자·_.:-, 1~128자만 허용합니다.")


def validate_request(value):
    """상태 번호와 호출자가 선별한 합법 후보의 통신 계약을 검사한다."""
    fields = {"protocol_version", "request_id", "session_id", "state_seq", "observation", "candidates"}
    if not isinstance(value, dict) or set(value) != fields:
        raise WorkerError("invalid_request", "요청 필드가 계약과 다릅니다.")
    if type(value["protocol_version"]) is not int or value["protocol_version"] != PROTOCOL_VERSION:
        raise WorkerError("unsupported_version", "protocol_version은 1이어야 합니다.")
    for name in ("request_id", "session_id"):
        require_identifier(value[name], name)
    if type(value["state_seq"]) is not int or not 0 <= value["state_seq"] <= 2**53 - 1:
        raise WorkerError("invalid_request", "state_seq는 0 이상의 안전한 정수여야 합니다.")
    observation = value["observation"]
    if not isinstance(observation, dict) or not observation:
        raise WorkerError("invalid_request", "observation은 비어 있지 않은 객체여야 합니다.")
    try:
        observation_bytes = encode_json(observation).encode("utf-8")
    except (ValueError, TypeError, RecursionError) as error:
        raise WorkerError("invalid_request", "관측은 유한한 숫자와 JSON 값만 포함해야 합니다.") from error
    if len(observation_bytes) > MAX_OBSERVATION_BYTES:
        raise WorkerError("observation_too_large", "관측을 4096 bytes 이하로 요약하세요.", 413)
    candidates = value["candidates"]
    if not isinstance(candidates, list) or not 1 <= len(candidates) <= MAX_CANDIDATES:
        raise WorkerError("invalid_request", "행동 후보는 1~8개여야 합니다.")
    identifiers = set()
    for candidate in candidates:
        if not isinstance(candidate, dict) or set(candidate) != {"id", "description"}:
            raise WorkerError("invalid_request", "후보에는 id와 description만 넣으세요.")
        require_identifier(candidate["id"], "candidate.id")
        if candidate["id"] in identifiers:
            raise WorkerError("invalid_request", "후보 id가 중복되었습니다.")
        identifiers.add(candidate["id"])
        description = candidate["description"]
        if not isinstance(description, str) or not description.strip() or len(description) > 160:
            raise WorkerError("invalid_request", "후보 설명은 1~160자여야 합니다.")
    return value


def build_question(request, instructions):
    """외부 행동 ID의 단어가 선택을 유도하지 않도록 중립 라벨로 바꾼다."""
    criteria = {f"a{index}": item["description"] for index, item in enumerate(request["candidates"])}
    return {"action": {"type": "choice", "instructions": instructions, "criteria": criteria}}


def load_config(path):
    config = decode_json(Path(path).read_text(encoding="utf-8-sig"))
    if not isinstance(config, dict) or set(config) != {"model", "revision", "dtype", "instructions"}:
        raise WorkerError("invalid_config", "설정에는 model, revision, dtype, instructions가 필요합니다.")
    if any(not isinstance(value, str) or not value.strip() for value in config.values()):
        raise WorkerError("invalid_config", "설정값은 비어 있지 않은 문자열이어야 합니다.")
    if not re.fullmatch(r"[0-9a-f]{40}", config["revision"]):
        raise WorkerError("invalid_config", "모델 revision은 불변 40자리 commit SHA로 고정하세요.")
    if config["dtype"] not in ("float16", "float32"):
        raise WorkerError("invalid_config", "dtype은 float16 또는 float32여야 합니다.")
    if len(config["instructions"]) > 600:
        raise WorkerError("invalid_config", "instructions는 600자 이하로 요약하세요.")
    return config


class LayaBackend:
    """고정된 MLX 체크포인트를 한 번 로드하고 실제 추론을 수행한다."""

    def __init__(self, config, offline=False):
        if sys.platform != "darwin" or platform.machine() != "arm64":
            raise WorkerError("unsupported_platform", "실제 추론은 Apple Silicon Mac에서 실행하세요.")
        try:
            version = importlib.metadata.version("laya-mlx")
            if version != "0.6.0":
                raise WorkerError("unsupported_runtime", "requirements-mac.txt의 laya-mlx 0.6.0을 설치하세요.")
            import laya_mlx
        except ImportError as error:
            raise WorkerError("missing_dependency", "requirements-mac.txt를 먼저 설치하세요.") from error
        self.agent = laya_mlx.load(
            config["model"], revision=config["revision"], dtype=config["dtype"],
            local_files_only=offline,
        )
        self.metadata = {
            "backend": "laya-mlx", "runtime_version": version,
            "model": config["model"], "revision": config["revision"], "dtype": config["dtype"],
        }

    def predict(self, observation, questions):
        """모델의 choice 출력과 토큰 사용 내역을 반환한다."""
        self.validate_candidate_tokens(questions)
        return self.agent.predict(observation, questions)

    def validate_candidate_tokens(self, questions):
        """SDK의 실제 후보 토큰 구간을 원문 토큰과 비교해 부분 손실도 거절한다."""
        from laya_mlx.common import build_head, encode_text, render_options

        # 0.6.0으로 고정한 SDK의 렌더러·정규화·예산을 함께 사용한다.
        tokenizer = self.agent.tok
        max_length = self.agent.cfg.get("max_len", 512)
        head_budget = self.agent.cfg.get("head_max_len", 192)
        for definition in questions.values():
            question = self.agent._to_internal(definition)
            head, markers, _ = build_head(tokenizer, question, head_budget)
            options = render_options(question)
            if len(markers) != len(options):
                raise WorkerError("options_truncated", "후보 토큰이 누락되었습니다. 설명을 줄이세요.", 422)
            ends = markers[1:] + [len(head) - 1]
            for option, start, end in zip(options, markers, ends):
                full_tokens = encode_text(
                    tokenizer, " " + option.replace(tokenizer.mask_token, " "),
                    add_special_tokens=False, truncation=False,
                )["input_ids"]
                actual_tokens = head[start + 1:min(end, max_length)]
                if actual_tokens != full_tokens:
                    raise WorkerError("options_truncated", "후보 설명이 토큰 예산에서 잘립니다. 설명이나 후보 수를 줄이세요.", 422)


def parse_prediction(result, candidates):
    """잘린 관측이나 잘못된 모델 출력을 실행 가능한 선택으로 내보내지 않는다."""
    try:
        usage = result["usage"]
        if not isinstance(usage, dict) or "truncated" not in usage or "state_tokens_dropped" not in usage:
            raise ValueError("토큰 계측 누락")
        if usage["truncated"] or usage["state_tokens_dropped"] or usage.get("truncated_questions"):
            raise WorkerError("context_truncated", "관측이 잘렸습니다. 상태와 후보 설명을 줄이세요.", 422)
        options = usage.get("options", {}).get("action", {})
        if options and options.get("distinct", 0) < options.get("total", 0):
            raise WorkerError("options_collapsed", "후보의 토큰 구분이 유실되었습니다.", 422)
        answer = result["answers"]["action"]
        labels = [f"a{index}" for index in range(len(candidates))]
        probabilities = answer["probabilities"]
        choice = answer["choice"]
        if choice not in labels or not isinstance(probabilities, dict) or set(probabilities) != set(labels):
            raise ValueError("후보 불일치")
        for probability in probabilities.values():
            if type(probability) not in (int, float) or not math.isfinite(probability) or not 0 <= probability <= 1:
                raise ValueError("유효하지 않은 확률")
        if abs(sum(probabilities.values()) - 1) > 0.01:
            raise ValueError("확률 합계 불일치")
        if probabilities[choice] < max(probabilities.values()):
            raise ValueError("최대 확률과 선택 불일치")
        mapped = {item["id"]: probabilities[label] for item, label in zip(candidates, labels)}
        selected = candidates[labels.index(choice)]["id"]
        return selected, mapped, usage
    except WorkerError:
        raise
    except (KeyError, TypeError, ValueError, AttributeError) as error:
        raise WorkerError("invalid_model_output", "Laya 응답이 처리자 계약과 맞지 않습니다.", 502) from error


class DecisionWorker:
    """추론 직렬화와 제한된 재시도 캐시를 제공한다."""

    def __init__(self, backend, config):
        self.backend = backend
        self.config = config
        self.lock = threading.Lock()
        self.cache = OrderedDict()
        self.metadata = {
            "worker_version": WORKER_VERSION, **backend.metadata,
            "config_sha256": hashlib.sha256(encode_json(config).encode("utf-8")).hexdigest(),
        }

    def decide(self, request):
        """동일 요청의 재전송은 같은 결과를 반환하고 ID의 다른 내용 재사용은 거절한다."""
        request = validate_request(request)
        digest = hashlib.sha256(json.dumps(request, sort_keys=True, ensure_ascii=False).encode("utf-8")).hexdigest()
        key = (request["session_id"], request["request_id"])
        if not self.lock.acquire(blocking=False):
            raise WorkerError("worker_busy", "처리 중입니다. 같은 요청 ID로 나중에 재시도하세요.", 503)
        try:
            if key in self.cache:
                previous_digest, response = self.cache[key]
                if digest != previous_digest:
                    raise WorkerError("request_conflict", "같은 요청 ID에 다른 내용을 보낼 수 없습니다.", 409)
                self.cache.move_to_end(key)
                return {**response, "cached": True}
            started = time.perf_counter()
            candidates = request["candidates"]
            if len(candidates) == 1:
                selected, probabilities, usage = candidates[0]["id"], {candidates[0]["id"]: 1.0}, None
                decision_source = "single_candidate"
            else:
                questions = build_question(request, self.config["instructions"])
                try:
                    prediction = self.backend.predict(request["observation"], questions)
                except WorkerError:
                    raise
                except Exception as error:
                    raise WorkerError("inference_failed", "모델 추론이 실패했습니다. Mac 처리자 상태를 확인하세요.", 502) from error
                selected, probabilities, usage = parse_prediction(prediction, candidates)
                decision_source = "laya"
            response = {
                "protocol_version": PROTOCOL_VERSION, "request_id": request["request_id"],
                "session_id": request["session_id"], "state_seq": request["state_seq"],
                "selected_action_id": selected, "probabilities": probabilities,
                "selected_probability": probabilities[selected], "decision_source": decision_source,
                "inference_ms": round((time.perf_counter() - started) * 1000, 3),
                "completed_at_utc": datetime.now(timezone.utc).isoformat(),
                "metadata": self.metadata, "usage": usage, "cached": False,
            }
            encode_json(response)
            self.cache[key] = (digest, response)
            if len(self.cache) > CACHE_SIZE:
                self.cache.popitem(last=False)
            return response
        finally:
            self.lock.release()


def error_response(error):
    return {"protocol_version": PROTOCOL_VERSION,
            "error": {"code": error.code, "message": str(error), "http_status": error.status}}


def create_server(worker, host, port, token=None):
    """관측을 노출하지 않는 로컬 기본값으로 HTTP 처리를 구성한다."""
    if host not in ("127.0.0.1", "localhost", "::1") and not token:
        raise WorkerError("token_required", "외부 연결 대기에는 LAYA_WORKER_TOKEN이 필요합니다.")

    monitor = TrafficMonitor(worker.metadata)

    class Handler(BaseHTTPRequestHandler):
        def setup(self):
            super().setup()
            self.connection.settimeout(10)

        def log_message(self, format, *args):
            # URL·관측·인증 정보를 일반 접근 로그에 남기지 않는다.
            pass

        def reply(self, status, body):
            if getattr(self, "monitor_event", None) is not None:
                monitor.complete(self.monitor_event, status, body)
            raw = encode_json(body).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(raw)))
            self.send_header("Connection", "close")
            self.end_headers()
            try:
                self.wfile.write(raw)
            except (BrokenPipeError, ConnectionResetError, TimeoutError):
                pass

        def authorized(self):
            if token and not hmac.compare_digest(self.headers.get("Authorization", "").encode("utf-8"), ("Bearer " + token).encode("utf-8")):
                self.reply(401, error_response(WorkerError("unauthorized", "인증 토큰을 확인하세요.", 401)))
                return False
            return True

        def do_GET(self):
            if self.path in ("/monitor", "/monitor/"):
                raw = (ROOT / "monitor.html").read_bytes()
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.send_header("Content-Length", str(len(raw)))
                self.send_header("Cache-Control", "no-store")
                self.end_headers()
                self.wfile.write(raw)
                return
            if not self.authorized():
                return
            if self.path == "/monitor/state":
                self.reply(200, monitor.snapshot())
                return
            if self.path != "/health":
                self.reply(404, error_response(WorkerError("not_found", "경로가 없습니다.", 404)))
                return
            self.reply(200, {"ready": True, "protocol_version": PROTOCOL_VERSION, "metadata": worker.metadata})

        def do_POST(self):
            if self.path != "/v1/decide":
                self._do_POST()
                return
            self.monitor_event = monitor.begin()
            try:
                self._do_POST()
            finally:
                monitor.finish(self.monitor_event)

        def _do_POST(self):
            if not self.authorized():
                return
            if self.path != "/v1/decide":
                self.reply(404, error_response(WorkerError("not_found", "경로가 없습니다.", 404)))
                return
            try:
                if self.headers.get("Transfer-Encoding"):
                    raise WorkerError("invalid_request", "Content-Length 방식으로 보내세요.")
                if self.headers.get_content_type() != "application/json":
                    raise WorkerError("invalid_request", "Content-Type은 application/json이어야 합니다.", 415)
                lengths = self.headers.get_all("Content-Length", [])
                if len(lengths) != 1 or not lengths[0].isdigit():
                    raise WorkerError("invalid_request", "유효한 Content-Length 하나가 필요합니다.")
                length = int(lengths[0])
                if not 0 < length <= MAX_BODY_BYTES:
                    raise WorkerError("request_too_large", "요청은 1~65536 bytes여야 합니다.", 413)
                raw = self.rfile.read(length)
                if len(raw) != length:
                    raise WorkerError("invalid_request", "요청 본문이 끝나지 않았습니다.")
                request = decode_json(raw)
                monitor.identify(self.monitor_event, request)
                response = worker.decide(request)
                self.reply(200, response)
            except WorkerError as error:
                self.reply(error.status, error_response(error))
            except (TimeoutError, OSError):
                self.reply(408, error_response(WorkerError("read_timeout", "본문 수신 시간이 초과되었습니다.", 408)))
            except Exception as error:
                print(f"처리 실패: {type(error).__name__}", file=sys.stderr)
                self.reply(500, error_response(WorkerError("internal_error", "Mac 처리자 내부 오류입니다.", 500)))

    class Server(ThreadingHTTPServer):
        daemon_threads = True

    return Server((host, port), Handler)


def read_requests(path):
    """모델 로드 전에 JSONL 전체의 요청 계약을 확인한다."""
    requests = []
    with Path(path).open(encoding="utf-8-sig") as stream:
        for line_number, line in enumerate(stream, 1):
            if not line.strip():
                continue
            try:
                if len(line.encode("utf-8")) > MAX_BODY_BYTES:
                    raise WorkerError("request_too_large", "JSONL 한 행이 너무 큽니다.", 413)
                requests.append(validate_request(decode_json(line)))
            except WorkerError as error:
                raise WorkerError(error.code, f"{line_number}행: {error}", error.status) from error
    if not requests:
        raise WorkerError("invalid_request", "JSONL에 요청이 없습니다.")
    return requests


def write_results(requests, output, decide):
    """입력별 실패를 보존하며 결과 파일을 새로 만든다."""
    destination = Path(output)
    destination.parent.mkdir(parents=True, exist_ok=True)
    failed = False
    with destination.open("x", encoding="utf-8", newline="\n") as stream:
        for request in requests:
            try:
                response = decide(request)
            except WorkerError as error:
                failed = True
                response = {**error_response(error), "request_id": request["request_id"],
                            "session_id": request["session_id"], "state_seq": request["state_seq"]}
            stream.write(encode_json(response) + "\n")
            stream.flush()
    return 1 if failed else 0


def send_request(url, request, token=None, timeout=30):
    """Mac 선택을 받아 요청·상태·후보가 일치하는지 확인한다. 재시도는 호출자가 결정한다."""
    validate_request(request)
    parsed = urlsplit(url)
    if parsed.scheme not in ("http", "https") or not parsed.netloc or parsed.query or parsed.fragment or parsed.username:
        raise WorkerError("invalid_url", "http(s) 서버 기본 주소를 사용하세요.")
    if parsed.path not in ("", "/"):
        raise WorkerError("invalid_url", "경로 없이 서버 기본 주소를 사용하세요.")
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    outgoing = Request(url.rstrip("/") + "/v1/decide", data=encode_json(request).encode("utf-8"), headers=headers)

    class NoRedirect(HTTPRedirectHandler):
        def redirect_request(self, request, file_pointer, code, message, headers, new_url):
            # 인증 헤더가 다른 호스트로 전달되지 않도록 재지정을 따라가지 않는다.
            return None

    try:
        with build_opener(NoRedirect).open(outgoing, timeout=timeout) as incoming:
            raw = incoming.read(MAX_BODY_BYTES + 1)
    except HTTPError as error:
        status = error.code
        try:
            detail = error.read(1024).decode("utf-8", errors="replace")
        except (OSError, HTTPException):
            detail = "오류 본문을 수신하지 못했습니다."
        raise WorkerError("remote_error", f"Mac이 HTTP {status}를 반환했습니다: {detail}", status) from error
    except (URLError, TimeoutError, OSError, HTTPException) as error:
        raise WorkerError("connection_failed", "Mac 연결 실패 또는 시간 초과입니다. 같은 요청 ID로 재시도할 수 있습니다.", 503) from error
    if len(raw) > MAX_BODY_BYTES:
        raise WorkerError("invalid_response", "Mac 응답이 너무 큽니다.", 502)
    response = decode_json(raw)
    if not isinstance(response, dict):
        raise WorkerError("invalid_response", "Mac 응답은 객체여야 합니다.", 502)
    for key in ("protocol_version", "request_id", "session_id", "state_seq"):
        if type(response.get(key)) is not type(request[key]) or response.get(key) != request[key]:
            raise WorkerError("stale_response", "다른 요청·상태의 응답을 받았습니다.", 409)
    candidates = {item["id"] for item in request["candidates"]}
    selected = response.get("selected_action_id")
    if not isinstance(selected, str) or selected not in candidates:
        raise WorkerError("invalid_response", "후보에 없는 행동이 반환되었습니다.", 502)
    return response


def verify_data(folder):
    """복사한 참고 자료의 바이트 해시를 확인한다."""
    folder = Path(folder).resolve()
    manifest = decode_json((folder / "manifest.json").read_text(encoding="utf-8-sig"))
    count = 0
    for entry in manifest["files"]:
        path = (folder / entry["copy_path"]).resolve()
        if not path.is_relative_to(folder):
            raise WorkerError("invalid_manifest", "데이터 경로가 폴더 밖을 가리킵니다.")
        raw = path.read_bytes()
        if len(raw) != entry["bytes"] or hashlib.sha256(raw).hexdigest() != entry["sha256"]:
            raise WorkerError("data_mismatch", f"참고 데이터 해시 불일치: {entry['copy_path']}")
        count += 1
    return {"verified_files": count, "scope": manifest["scope"]}


def main(argv=None):
    """오프라인 파일 처리·HTTP 서버·Windows 송신을 같은 계약으로 실행한다."""
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("infer", "serve", "inspect"):
        command = commands.add_parser(name)
        command.add_argument("--config", type=Path, default=ROOT / "worker-config.json")
        if name != "serve":
            command.add_argument("--input", type=Path, default=ROOT / "data/examples/requests.jsonl")
        if name != "inspect":
            command.add_argument("--offline", action="store_true", help="다운로드 없이 로컬 모델 캐시만 사용")
        if name == "infer":
            command.add_argument("--output", type=Path, required=True)
        if name == "serve":
            command.add_argument("--host", default="127.0.0.1")
            command.add_argument("--port", type=int, default=8765)
    command = commands.add_parser("send")
    command.add_argument("--url", required=True)
    command.add_argument("--input", type=Path, default=ROOT / "data/examples/requests.jsonl")
    command.add_argument("--output", type=Path, required=True)
    command.add_argument("--timeout", type=float, default=30)
    command = commands.add_parser("verify-data")
    command.add_argument("--data", type=Path, default=ROOT / "data")
    args = parser.parse_args(argv)
    try:
        if args.command == "verify-data":
            print(encode_json(verify_data(args.data)))
            return 0
        if args.command == "send":
            if not math.isfinite(args.timeout) or args.timeout <= 0:
                raise WorkerError("invalid_timeout", "timeout은 양의 유한수여야 합니다.")
            requests = read_requests(args.input)
            return write_results(requests, args.output, lambda request: send_request(
                args.url, request, os.environ.get("LAYA_WORKER_TOKEN"), args.timeout))
        config = load_config(args.config)
        if args.command == "inspect":
            for request in read_requests(args.input):
                print(encode_json({"request_id": request["request_id"], "state": request["observation"],
                                   "questions": build_question(request, config["instructions"])}))
            return 0
        requests = read_requests(args.input) if args.command == "infer" else None
        backend = LayaBackend(config, args.offline)
        worker = DecisionWorker(backend, config)
        if args.command == "infer":
            return write_results(requests, args.output, worker.decide)
        server = create_server(worker, args.host, args.port, os.environ.get("LAYA_WORKER_TOKEN"))
        print(f"Laya 준비 완료: {args.host}:{server.server_port}", file=sys.stderr, flush=True)
        try:
            server.serve_forever()
        finally:
            server.server_close()
        return 0
    except (WorkerError, OSError, ValueError) as error:
        if isinstance(error, WorkerError):
            print(encode_json(error_response(error)), file=sys.stderr)
        else:
            print(f"실행 실패: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
