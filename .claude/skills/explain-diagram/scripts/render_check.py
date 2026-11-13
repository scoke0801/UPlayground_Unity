"""HTML을 Edge/Chrome 헤드리스로 PNG 캡처한다. 겹침·잘림을 눈으로 확인하는 용도.

실행: python -I render_check.py <입력.html> <출력.png> [폭=1100] [높이=1400]
"""
import subprocess
import sys
from pathlib import Path

BROWSERS = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
]

src, dst = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
w = sys.argv[3] if len(sys.argv) > 3 else "1100"
h = sys.argv[4] if len(sys.argv) > 4 else "1400"
exe = next((b for b in BROWSERS if Path(b).exists()), None)
if not exe:
    sys.exit("Edge/Chrome을 찾지 못했다")
subprocess.run([exe, "--headless", "--disable-gpu", f"--window-size={w},{h}",
                f"--screenshot={dst}", src.as_uri()], check=True, capture_output=True)
print("saved", dst)
