"""기존 HTTP 요청의 제한된 메모리 계측. 관측 원문·인증 헤더는 수집하지 않는다."""
from collections import deque
from datetime import datetime, timezone
import threading
import time


class TrafficMonitor:
    """추론 lock과 분리된 짧은 lock으로 최근 요청 200개를 관측한다."""

    def __init__(self, metadata):
        self.lock = threading.Lock()
        self.events = deque(maxlen=200)
        self.metadata = metadata
        self.started = datetime.now(timezone.utc).isoformat()
        self.total = self.success = self.errors = self.active = 0

    def begin(self):
        with self.lock:
            self.total += 1
            self.active += 1
            event = {"number": self.total, "received_at": datetime.now(timezone.utc).isoformat(),
                     "phase": "receiving", "started": time.perf_counter()}
            self.events.append(event)
            return event

    def identify(self, event, request):
        if not isinstance(request, dict):
            return
        with self.lock:
            for key in ("session_id", "request_id", "state_seq"):
                value = request.get(key)
                if type(value) is int or isinstance(value, str) and len(value) <= 128:
                    event[key] = value
            candidates = request.get("candidates")
            event["candidate_count"] = len(candidates) if isinstance(candidates, list) else None
            event["phase"] = "processing"

    def complete(self, event, status, body):
        with self.lock:
            if "http_status" in event:
                return
            event.update(http_status=status, phase="response_prepared",
                         server_ms=round((time.perf_counter() - event["started"]) * 1000, 3))
            if status == 200:
                self.success += 1
                for key in ("selected_action_id", "selected_probability", "inference_ms", "decision_source", "cached", "probabilities", "usage"):
                    event[key] = body.get(key)
            else:
                self.errors += 1
                event["error_code"] = body.get("error", {}).get("code")

    def finish(self, event):
        with self.lock:
            self.active -= 1
            if "http_status" not in event:
                self.errors += 1
                event["phase"] = "connection_interrupted"

    def snapshot(self):
        with self.lock:
            return {"started_at": self.started, "metadata": self.metadata,
                    "total": self.total, "success": self.success, "errors": self.errors,
                    "active": self.active,
                    "events": [{k: v for k, v in e.items() if k != "started"} for e in reversed(self.events)]}
