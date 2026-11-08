# Mac HTTPS Quick Tunnel 인계 결과

확인: 2026-11-08T12:51:08.911014+09:00. MAC_HTTPS_TUNNEL_WORK_ORDER.md 기준으로 실제 실행했다. HTTPS Quick Tunnel이 기존 SSH/LAN 안내보다 우선한다.

## 현재 연결

루트 URL: https://yamaha-uniform-concern-wearing.trycloudflare.com
health: https://yamaha-uniform-concern-wearing.trycloudflare.com/health
판단: https://yamaha-uniform-concern-wearing.trycloudflare.com/v1/decide
모니터: https://yamaha-uniform-concern-wearing.trycloudflare.com/monitor

무료·무계정 Cloudflare Quick Tunnel 사용. 계정 생성·로그인·결제·유료 전환·도메인·Named Tunnel·Cloudflare API 토큰 발급 없음. 주소는 임시이며 터널을 종료하면 접근이 중단되고 재실행 시 바뀐다. 가동 시간 보장 없음. 공식 근거: https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/ , https://developers.cloudflare.com/workers/wrangler/commands/tunnel/ .

서버 PID 13173, 터널 PID 13249, 두 프로세스 모두 독립 세션/PPID 1로 실행 유지 확인. 서버는 127.0.0.1:8765에만 바인딩한다. 기존 사용자가 실행한 별도 SSH 프로세스는 종료하지 않았다. 시스템 서비스·자동 시작·VPN·공유기·포트 포워딩·방화벽·DNS 서버 설정은 변경하지 않았다. 신규 주소의 DNS 반영 대기 중 로컬 DNS 조회 캐시만 한 번 비웠다. 초기 DNS 실패 원문은 public-readiness-errors.json에 보존했다. 최종 검증은 일반 hostname 조회와 기본 TLS 인증으로 성공했다.

## 인증과 토큰 전달

서버를 기존 난수 Bearer 인증으로 재시작했다. 새 32-byte 난수 공유 토큰은 프로세스 환경에만 있으며 대화·파일·Git·URL·로그·CLI 인자에 저장/출력하지 않았다. cloudflared 프로세스에는 Laya 토큰을 전달하지 않았다. 토큰을 아직 Windows에 공유하지 않았으므로 URL만으로는 요청할 수 없다. 비밀은 공개 URL과 별도로 전달해야 한다.

사용자가 Mac 터미널에서 다음을 실행하면 토큰을 화면에 출력하지 않고 Mac 클립보드로 복사한다. 이 복사 동작은 에이전트가 실행하지 않았다.

```bash
cd /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker
/Users/jonghyun/Documents/Codex/2026-11-07/new-chat/work/laya-runtime/venv/bin/python copy_laya_token.py --pid 13173
```

별도 비밀 전달 경로로 Windows의 아래 숨김 입력에 전달한다. 채팅/작업 보고/Git에 붙여넣지 않는다. 사용 후 클립보드를 비운다. copy_laya_token.py는 같은 Mac 사용자 권한으로 해당 서버 프로세스의 환경을 메모리에 읽고 pbcopy에만 전달한다. --check-only 검사는 통과했다. 이 도구는 임의 PID나 다른 공유 비밀의 전달을 위한 범용 덤프 도구가 아니다.

Mac의 /monitor도 이제 인증 토큰 입력 후 데이터가 갱신된다. HTML 틀 자체는 공개 가능하지만 /monitor/state는 기존 Bearer 인증을 적용한다. 토큰은 브라우저 메모리에서만 사용한다.

## 검증 결과

| 검사 | 결과 |
|---|---|
| 로컬 무토큰 health | 401, 실제 Laya JSON |
| 로컬 올바른 토큰 health | 200, ready=true, 실제 metadata 일치 |
| H01 공개 무토큰 health | 401, 실제 Laya JSON |
| H02 공개 잘못된 토큰 health | 401, 실제 Laya JSON |
| H03 공개 올바른 토큰 health | 200, 실제 모델 metadata 일치 |
| H04 공개 무토큰 decide | 401, 실제 Laya JSON |
| H05 공개 실제 모델 추론 | 골렘·엔트 합성 요청 총 12/12 통과. ID/state_seq/후보·확률/usage/metadata 확인 |
| H06 터널 종료 | 최초 검증 터널 종료 후 공개 URL 530/1033, 로컬 health는 200. 이후 새 최종 URL로 다시 H01~H05 확인 |
| 기존 자동 검사 | 36개 통과 |
| 패키지 무결성 | reference 18개, enemy 513개·객체 486개 통과 |
| Windows 공개 URL | 미수행 / 토큰 별도 전달 및 Windows 검사 대기 |
| Unity 실제 전투 | 미수행. 이번 입력은 synthetic_fixture이며 runtime_capture 아님 |

Cloudflare 오류 HTML/text·리다이렉트·429/502/timeout을 성공으로 처리하지 않았다. TLS 검증은 기본 활성 상태이며 기존 송신기의 리다이렉트 거절을 사용했다. HTTPS POST는 기존 send_request를 사용했다. 로그인 페이지·브라우저 쿠키·이메일 인증 사용 없음.

## 모델·측정

Python 3.12.16, laya-mlx 0.6.0, 기존 HF 캐시 오프라인 재사용. 모델 aac6fef/laya-multilingual-mlx, revision ba40c87fcb357f1643d04d71323af9cdc3b9e591, float16.
설정 enemy-data/worker-config.json, config_sha256 e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3.
실제 실행 cloudflared 2026.10.7, 공식 Darwin arm64 배포. Homebrew 명령이 로컬의 오래된 formula로 2024.1.5를 설치했기 때문에 그 바이너리 대신 공식 최신 arm64를 별도 작업 경로에 준비했다. Homebrew의 자동 캐시 정리도 실행되었으며 패키지/서비스 추가 업그레이드는 하지 않았다. 다운로드/설치 근거: https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/downloads/ .

실제 12건 추론 중앙값 196.250ms, Mac→공개 HTTPS→터널→Mac 왕복 중앙값 1690.949ms. Windows↔Mac 왕복이 아니다. 새 프로세스 첫 추론 표본은 predictions.jsonl 첫 행이며 warmed 값과 구분하여 원문 보존했다. 모델 로드 시간은 이번 터널 작업에서 별도 계측하지 않았다.

현재 골렘/엔트 Unity 프로필은 validitySeconds=0.75, timeoutSeconds=1이다. HTTPS 지연이 이를 넘으면 실제 Unity에서 timeout/TTL 폐기될 수 있다. 값을 늘리거나 폴백 정책을 바꾸지 않았다. Windows 실측과 session JSON에서 지연·폐기·실제 적용을 확인해야 한다. 터널 성공은 전투 연동 품질 보장이 아니다. 단순 로컬 추론과 네트워크 오버헤드의 엄밀한 차이는 같은 조건의 비교 실험 없이 단정하지 않는다.

## Windows 실행

SSH/VPN/cloudflared 설치 및 로컬 전달 포트가 필요 없다. 같은 공유 토큰을 아래에 숨김 입력한다.

```powershell
$layaSecret = Read-Host 'Mac과 같은 Laya 공유 토큰' -AsSecureString
$env:LAYA_WORKER_TOKEN = [System.Net.NetworkCredential]::new('', $layaSecret).Password
$env:LAYA_WORKER_URL = 'https://yamaha-uniform-concern-wearing.trycloudflare.com'
Invoke-RestMethod -Method Get -Uri ($env:LAYA_WORKER_URL + '/health') -MaximumRedirection 0 -TimeoutSec 10 -Headers @{ Authorization = ('Bearer ' + $env:LAYA_WORKER_TOKEN) }
```

같은 PowerShell에서 기존 송신기로 검증한다.

```powershell
$layaRunId = Get-Date -Format 'yyyyMMddTHHmmssfff'
python Tools/LayaWorker/laya_worker.py send --url $env:LAYA_WORKER_URL --input Tools/LayaWorker/enemy-data/examples/requests.jsonl --output ("Tools/LayaWorker/results/https-tunnel/" + $layaRunId + "/windows-predictions.jsonl")
```

Unity는 이 셸의 환경변수를 상속하도록 새로 실행한다. 이미 실행한 Unity/Hub에는 변수가 전달되지 않는다. 기존 프로세스는 저장 후 사용자가 종료하고 설치된 Unity 실행 파일을 이 셸에서 직접 시작한다. 시스템 전역 환경변수로 토큰을 영구 저장하지 않는다. 디버거 서버 루트에는 위 URL만 입력하고 /v1/decide를 붙이지 않는다. URL만 저장하며 토큰은 저장하지 않는다. Windows health의 backend/model/revision/hash가 동일해야 실제 모델 연결이다. 로컬 모의 서버 metadata를 성공으로 인정하지 않는다.

## 운영·종료·재시작

현재 결과/로그: Tools/LayaWorker/results/https-tunnel/2026-11-08-mac-01/
서버: server.pid / server.log
터널: tunnel.pid / tunnel.log
URL: url.txt
로컬 인증: local-auth.json / 공개 인증: public-auth.json
원시 요청/응답: requests.jsonl / predictions.jsonl
종료 증거: h06.json / 자동 검사: unit-tests.txt

종료 전 PID가 해당 프로세스인지 재확인한다. 외부 접속만 종료: `kill 13249`. Laya 서버까지 종료할 때만 별도로 `kill 13173`. 다른 프로세스를 일괄 종료하지 않는다. 터널이 종료되면 이 URL은 사용할 수 없다.

서버를 유지하고 터널만 재시작할 때:

```bash
/Users/jonghyun/Documents/Codex/2026-11-08/mac-uplayground-laya-x20-windows-unity/work/cloudflared/cloudflared tunnel --url http://127.0.0.1:8765 --no-autoupdate
```

출력된 새 trycloudflare URL을 Windows에 다시 전달한다. 터미널 유지 또는 별도 사용자 세션 로그/PID 관리가 필요하다. 다음 서버 재시작 때 무인증으로 되돌리지 않도록 기존 start-server.sh 대신 숨김 입력 도구를 사용한다.

```bash
cd /Users/jonghyun/ProjectRepo/UnityProj/UPlayground_Unity/Tools/LayaWorker
HF_HOME=/Users/jonghyun/Documents/Codex/2026-11-07/new-chat/work/laya-runtime/hf HF_HUB_OFFLINE=1 /Users/jonghyun/Documents/Codex/2026-11-07/new-chat/work/laya-runtime/venv/bin/python start_authenticated_worker.py
```

프롬프트에 Windows와 같은 공유 토큰을 직접 입력한다. 토큰은 표시되지 않고 자식 환경으로만 전달된다. 이미 서버가 실행 중이면 중복 시작하지 않는다. 무토큰 health 401과 올바른 토큰 health 200을 먼저 확인한 뒤 터널을 연다. 토큰을 잃으면 터널부터 종료하고 새 토큰으로 서버를 시작해 Windows에도 별도 전달한다.

서버 v1 코드·프롬프트·모델·오류·확률·캐시·추론 제한을 변경하지 않았다. 사용자용 토큰 복사/인증 시작 도구와 문서만 추가. 새 Python 중계/TLS/WebSocket 없음. 커밋·push 없음.
