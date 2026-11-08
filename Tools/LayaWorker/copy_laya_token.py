"""Mac 사용자가 직접 실행해 현재 Laya 프로세스의 공유 토큰을 클립보드로 옮긴다."""
import argparse
import re
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--pid", type=int, required=True)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    if sys.platform != "darwin" or args.pid <= 0:
        parser.error("Mac의 실행 중인 Laya 서버 PID가 필요합니다.")
    # stdout은 변수로만 받아 토큰을 터미널·파일·로그로 보내지 않는다.
    process = subprocess.run(["/bin/ps", "eww", "-p", str(args.pid), "-o", "command="],
                             capture_output=True, text=True)
    if process.returncode or "laya_worker.py serve" not in process.stdout:
        print("해당 PID의 Laya 서버를 확인할 수 없습니다.", file=sys.stderr)
        return 1
    match = re.search(r"(?:^| )LAYA_WORKER_TOKEN=([A-Za-z0-9_-]{32,})(?: |$)", process.stdout.strip())
    if not match:
        print("해당 프로세스에서 지원하는 형식의 공유 토큰을 확인할 수 없습니다.", file=sys.stderr)
        return 1
    if args.check_only:
        print("공유 토큰 확인 완료. 출력·복사하지 않았습니다.")
        return 0
    copied = subprocess.run(["/usr/bin/pbcopy"], input=match.group(1), text=True,
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    if copied.returncode:
        print("클립보드 복사 실패.", file=sys.stderr)
        return 1
    print("공유 토큰을 Mac 클립보드에 복사했습니다. 별도 비밀 전달 경로로만 전달하고 사용 후 클립보드를 비우세요.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
