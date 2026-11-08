"""사용자가 입력한 공유 비밀을 환경변수로만 전달해 기존 서버 CLI를 시작한다."""
import getpass
import os
from pathlib import Path
import subprocess
import sys


def main():
    root = Path(__file__).resolve().parent
    token = os.environ.get("LAYA_WORKER_TOKEN") or getpass.getpass("Laya 공유 토큰 (표시 안 됨): ")
    if len(token) < 32 or not token.isascii() or any(c.isspace() for c in token):
        print("32자 이상의 공백 없는 ASCII 공유 토큰이 필요합니다.", file=sys.stderr)
        return 1
    environment = os.environ.copy()
    environment["LAYA_WORKER_TOKEN"] = token
    # HF_HOME은 기존 캐시를 가리키도록 호출 셸에서 지정한다.
    return subprocess.call([sys.executable, "-u", str(root / "laya_worker.py"), "serve", "--offline",
                            "--config", str(root / "enemy-data/worker-config.json"),
                            "--host", "127.0.0.1", "--port", "8765"], cwd=root, env=environment)


if __name__ == "__main__":
    raise SystemExit(main())
