# Mac Laya HTTPS 터널 전환 작업 지침

Mac 작업 에이전트에게 이 문서를 전달한다. 최신 체크아웃의 이 문서와 [기본 서버 준비 명세](MAC_SERVER_WORK_ORDER.md)를 읽고 실제 실행·검증·Windows 인계까지 진행한다. 이번 연결 방식은 기존 SSH·직접 LAN 안내보다 우선한다. 사용자 변경을 보존하고 별도 요청 없이 커밋·push하지 않는다.

## 1. 목표와 변경 범위

Windows에서 Mac 사설 주소로 가는 TCP 연결이 실패했다. VPN·공유기·포트 포워딩·SSH·기존 네트워크 설정을 변경하지 않고, Mac에서 외부로 연결하는 Cloudflare Quick Tunnel로 접근 경로를 만든다.

```text
Windows Unity / Python 송신기
  → HTTPS https://<임시주소>.trycloudflare.com
  → Cloudflare 중계
  → Mac cloudflared
  → HTTP 127.0.0.1:8765 Laya 서버
```

Mac 서버 포트는 `8765`, 바인딩은 `127.0.0.1`을 유지한다. Windows에는 로컬 리스너를 만들지 않으므로 로컬 `8765`·`18765` 점유와 무관하다. HTTPS는 터널이 제공하고 기존 Python 서버에 TLS·WebSocket·중계 서버 코드를 새로 만들지 않는다. 기존 HTTP v1·모델·revision·프롬프트를 보존한다.

## 2. 계정·로그인·비용 조건

**무료·무계정 Quick Tunnel만 사용한다.** Cloudflare 계정 생성·로그인, 결제수단 등록, 유료 플랜 전환, 도메인 구매, Named Tunnel 생성, `cloudflared tunnel login`, Cloudflare 계정 API 토큰 발급은 하지 않는다. 해당 절차가 나오면 다른 상품/명령인지 확인하고 진행하지 않는다. 무료 사용 조건이 달라졌다면 사용자에게 보고하고 유료 경로로 자동 전환하지 않는다.

Quick Tunnel은 개발용 임시 연결이다. 고정 주소·상시 운영·가동 시간 보장은 이번 목표가 아니다. 재실행 시 주소가 바뀌므로 Windows endpoint도 갱신해야 한다. 공식 무료·무계정 안내는 [Cloudflare 명령 문서](https://developers.cloudflare.com/workers/wrangler/commands/tunnel/#tunnel-quick-start), 주소·수명·제한은 [Quick Tunnels 문서](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/)를 기준으로 한다.

터널은 외부 Cloudflare 서비스를 경유한다. 허용한 전투 관측만 보내고 토큰·계정 정보·원본 프로젝트 전체를 관측에 넣지 않는다. URL 자체를 비밀 인증 수단으로 취급하지 않는다.

## 3. 시작 전 확인

1. 기본 서버 명세의 Python 환경·모델 캐시·몬스터 설정·기존 결과를 확인한다. 새 학습·전체 평가 반복은 필요 없다.
2. `laya_worker.py`가 `LAYA_WORKER_TOKEN`을 읽고, loopback에서도 토큰을 설정하면 `/health`와 `/v1/decide` 모두 Bearer 인증을 적용하는지 확인한다.
3. 현재 `8765` 사용 프로세스를 확인한다. 기존 무인증 Laya 서버가 있으면 해당 프로세스임을 확인한 후 종료하고 토큰을 넣어 재시작한다. 환경변수를 새 터미널에서 설정해도 이미 실행 중인 서버에는 적용되지 않는다.
4. 다른 프로세스·기존 터널·다른 프로젝트의 설정을 종료·변경하지 않는다. 과거 PID는 현재 PID라고 가정하지 않는다.

## 4. 토큰 인증을 먼저 적용

양쪽 프로세스에서 같은 `LAYA_WORKER_TOKEN`을 사용한다. Cloudflare 계정 토큰이 아니라 **기존 Laya 서버의 공유 비밀**이다. 충분히 긴 난수 값을 사용하고 환경변수로 전달한다. 토큰을 Git·작업 보고·대화·URL·세이브·로그·명령행 인자에 넣지 않는다. shell tracing도 끈다.

이미 안전하게 공유한 토큰이 있으면 재사용하고, 없다면 새로 준비하여 사용자에게 별도 비밀 전달이 필요한 상태임을 알린다. 토큰 값을 일반 결과물에 출력하지 않는다. 공개 URL에는 토큰을 포함하지 않는다.

Mac의 기존 가상환경을 활성화하고, **토큰이 설정된 같은 셸에서** 실행한다.

```bash
python laya_worker.py serve \
  --config enemy-data/worker-config.json \
  --host 127.0.0.1 \
  --port 8765
```

로컬에서 토큰 없는 `/health`가 401이고, 올바른 Bearer 토큰으로 200인지 확인한 뒤 다음 단계로 진행한다. 올바른 응답은 `ready=true`, `metadata.backend=laya-mlx`, 기대 모델·revision·설정 hash를 만족해야 한다. `transport-mock`·`MOCK-NOT-LAYA`는 연결 성공으로 인정하지 않는다.

## 5. cloudflared 설치와 임시 터널

이미 설치된 `cloudflared`를 먼저 확인하고 버전을 기록한다. 없고 Homebrew가 설치돼 있으면 다음 공식 설치 방법을 사용한다.

```bash
brew install cloudflared
cloudflared --version
```

Homebrew 자체를 임의 설치하지 않는다. 없는 경우 [공식 다운로드](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/downloads/)의 Mac 아키텍처에 맞는 바이너리를 사용자 영역에 준비한다. 기존 전역 서비스나 다른 터널 설정은 수정하지 않는다.

로컬 인증 검증 후 **별도 터미널**에서 실행한다.

```bash
cloudflared tunnel --url http://127.0.0.1:8765
```

출력된 `https://<임시주소>.trycloudflare.com`을 그대로 기록한다. 예시 주소를 실제 주소처럼 전달하지 않는다. `--allowed-mail` 등 브라우저 로그인 방식은 Unity/Python의 비대화형 요청에 적용하지 않는다. 앱의 Bearer 인증을 사용한다.

터널은 인증된 Laya 한 서비스만 노출한다. 무인증 상태에서 먼저 공개하지 않는다. 로컬 서버가 401을 반환하는 인증 검사를 통과하지 못했으면 터널을 열지 않는다.

Mac의 외부 인터넷 연결이 필요하다. Cloudflare는 터널 연결에 outbound 7844(QUIC UDP 또는 HTTP/2 TCP)를 사용한다. 기존 네트워크가 이를 차단하면 방화벽·VPN을 변경하지 말고 로그와 차단 항목을 보고한다. UDP만 실패한 경우 설치 버전의 도움말을 확인해 `--protocol http2`를 시도할 수 있으나, 이는 TCP 443만으로 연결된다는 뜻이 아니다. [공식 네트워크 요구사항](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/configure-tunnels/tunnel-with-firewall/)

기존 `~/.cloudflared` 설정 때문에 Quick Tunnel이 시작되지 않으면 다른 터널 설정을 삭제·덮어쓰지 않는다. 설치 버전의 옵션을 조사해 별도 설정 경로로 분리하거나 정확한 원인을 남긴다.

## 6. 공개 URL 검증

Mac에서 먼저 공개 HTTPS URL로 확인하고, Windows에서 직접 확인한 결과와 구분한다.

| ID | 검사 | 기대 결과 |
|---|---|---|
| H01 | 무토큰 `/health` | Laya JSON 401. 공개 URL이 있다고 인증을 우회하지 못함 |
| H02 | 잘못된 토큰 `/health` | Laya JSON 401 |
| H03 | 올바른 토큰 `/health` | 200, ready 및 실제 Laya metadata 일치 |
| H04 | 무토큰 `/v1/decide` | Laya JSON 401 |
| H05 | 올바른 토큰으로 골렘·엔트 예제 전송 | 기존 v1 ID·후보·확률·usage·metadata 검증 통과 |
| H06 | 터널 프로세스 종료 | 외부 접근 중단. 로컬 Laya 서버의 생존 여부는 별도 확인 |

인증 실패는 Cloudflare HTML 페이지가 아니라 실제 Laya의 JSON 401인지 검사한다. 로그인 HTML·리다이렉트·502·429·timeout을 추론 성공으로 기록하지 않는다. TLS 검증은 끄지 않고 인증 헤더가 다른 호스트로 전달되는 리다이렉트도 따르지 않는다. 로그인 화면이 나오면 토큰 우회나 브라우저 쿠키 복사를 하지 않는다.

기존 송신기는 환경변수 토큰을 읽고 HTTPS를 지원한다. 토큰을 추가 CLI 인자로 만들 필요 없다. 실제 임시 주소로 `LAYA_WORKER_URL`을 설정하고 실행한다.

```bash
LAYA_RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
python laya_worker.py send \
  --url "$LAYA_WORKER_URL" \
  --input enemy-data/examples/requests.jsonl \
  --output "results/https-tunnel/$LAYA_RUN_ID/mac-predictions.jsonl"
```

입력은 합성 fixture다. 실제 Unity 관측·전투 품질로 보고하지 않는다. 로컬 추론, HTTPS 왕복, Windows↔Mac 왕복을 각각 구분한다. 터널 추가 지연이 관측 유효기간을 넘으면 기록만 남기고 시간 예산을 임의로 늘리지 않는다. 503은 로컬 fallback 후 새 관측 정책을 따르며 오래된 요청을 자동 반복하지 않는다.

## 7. Windows 설정과 확인

Windows에서는 SSH·VPN·cloudflared 설치가 필요 없다. Mac에서 받은 실제 HTTPS 루트 주소와 같은 토큰을 사용한다. 다음 PowerShell에서 토큰을 화면에 표시하지 않고 입력한다.

```powershell
$layaSecret = Read-Host 'Mac과 같은 Laya 공유 토큰' -AsSecureString
$env:LAYA_WORKER_TOKEN = [System.Net.NetworkCredential]::new('', $layaSecret).Password
$env:LAYA_WORKER_URL = 'https://<실제주소>.trycloudflare.com'
Invoke-RestMethod -Method Get -Uri ($env:LAYA_WORKER_URL + '/health') -MaximumRedirection 0 -TimeoutSec 10 -Headers @{ Authorization = ('Bearer ' + $env:LAYA_WORKER_TOKEN) }
```

Unity Editor와 Development Player도 **이 환경변수를 상속받는 프로세스로 새로 실행**한다. 이미 실행 중인 Unity·Hub는 새 PowerShell의 변수를 자동으로 받지 않는다. 기존 Hub를 경유하면 상속 여부를 확인하고, 불확실하면 해당 셸에서 설치된 Unity 실행 파일을 직접 시작한다. 설치 경로는 현재 PC에서 확인한다. 시스템 전역 환경변수에 토큰을 영구 저장하지 않는다.

- 디버거 서버 주소 입력: `https://<실제주소>.trycloudflare.com`
- 실제 판단 요청: `https://<실제주소>.trycloudflare.com/v1/decide`
- 상태 검사: `https://<실제주소>.trycloudflare.com/health`

주소 입력란에는 `/v1/decide`를 붙이지 않는다. 기존 PlayerPrefs의 localhost 주소는 사용자가 새 주소로 바꾼다. 저장하는 것은 URL이며 토큰은 저장하지 않는다. 현재 Unity 클라이언트는 환경변수 Bearer 헤더와 HTTPS를 지원하므로 통신 계약 변경은 필요 없다.

```powershell
$layaRunId = Get-Date -Format 'yyyyMMddTHHmmssfff'
python Tools/LayaWorker/laya_worker.py send --url $env:LAYA_WORKER_URL --input Tools/LayaWorker/enemy-data/examples/requests.jsonl --output ("Tools/LayaWorker/results/https-tunnel/" + $layaRunId + "/windows-predictions.jsonl")
```

## 8. 운영·종료·인계

서버와 터널은 별도 프로세스다. 둘 다 살아 있어야 한다. 작업 세션에서 유지 가능한 실행 방식으로 준비하고 실제 PID·로그·종료 명령을 기록한다. 시스템 서비스·로그인 자동 실행은 추가하지 않는다. 터널 재실행 후에는 이전 URL을 재사용하지 않고 새 URL을 Windows에 전달한다.

종료 시 먼저 해당 터널만 Ctrl+C 또는 확인한 PID로 종료한다. Laya까지 종료할지는 별도로 판단한다. 다른 터널이나 모든 Python 프로세스를 한꺼번에 종료하지 않는다. 토큰이 노출됐다면 터널 종료 후 새 토큰으로 서버를 재시작한다.

결과는 기존 결과를 덮지 않는 `results/https-tunnel/<run-id>/`에 보관한다. 버전·코드 revision·설정 hash, 공개 루트 URL, 검사별 결과, 원시 응답, 서버/터널 PID·로그·재시작 방법을 기록한다. 토큰·Authorization 헤더·환경변수 덤프는 보관하지 않는다. 공용 보고서에는 인증 사용 여부만 적는다.

최종 보고는 다음을 구분한다.

- 무료·무계정 Quick Tunnel 사용 여부. 가입·결제·유료 전환을 하지 않았는지.
- 실제 HTTPS 루트 URL과 수명, 서버·터널 현재 실행 여부.
- 로컬 인증 / Mac에서 공개 URL / Windows에서 공개 URL / Unity 실제 전투의 통과·실패·미수행.
- Windows가 토큰을 별도 설정해야 하는지. 실제 토큰 값은 출력하지 않는다.
- 모델·revision·설정 hash, 추론·HTTPS 왕복 지연, 로그 위치와 종료·재시작 방법.
- 기존 네트워크 설정을 변경하지 않았는지와 남은 차단 조건.

이번 Windows 문서 작성 작업은 실제 터널 개설·계정 로그인·결제·Mac 서버 실행을 수행한 것이 아니다. Mac 에이전트가 위 절차를 실행한 뒤 결과를 받아 판정한다.
