# Mac Laya 연결 재확인 요청 명세

## 목적과 현재 상태

Windows Unity의 CombatTestMap에 골렘·엔트 원격 의도 연동을 구현했다. Windows 로컬 모의 HTTP 서버로 요청 → 응답 검증 → BT 의도 적용 → 실제 GAS 행동 실행을 확인했다. 이 결과는 실제 Laya 추론 결과가 아니다.

실제 연결에 사용하려던 경로는 `Windows Unity → Windows SSH 전달 포트 18765 → Mac 127.0.0.1:8765 → Laya`다. Windows에서 기존 인계 주소 `192.168.35.223:22`에 SSH 접속을 시도했으나 5초 후 Connection timed out으로 실패했다. 이 증상만으로 Mac 서버 장애, SSH 비활성화, IP 변경, 네트워크 차단 중 어느 원인인지 확정할 수 없다.

Mac 담당자는 아래 항목을 실제로 확인하고, 확인 시각과 증거를 회신한다. 서버 상태 확인과 Windows에서의 접근 성공은 별도로 판정한다. Unity/Player/Addressables 빌드는 이번 확인 범위에서 제외한다. 기존 사용자 변경을 보존하고 별도 요청 없이 커밋·push하지 않는다.

## 참고 자료

- `README.md`, `MAC_SERVER_WORK_ORDER.md`, `WINDOWS_CLIENT_HANDOFF.md`
- `enemy-data/README.md`, `enemy-data/worker-config.json`
- `output/server-readiness/2026-11-08-mac-01/REPORT.md`, `health.json`
- `TRAINING_GROUND_IMPLEMENTATION.md`

과거 문서의 IP·PID·서버 실행 상태는 현재 상태의 증거로 사용하지 않는다. 필요한 파일이 체크아웃에 없으면 누락을 명시한다.

## M01. 현재 접속 주소와 네트워크

Mac에서 실행하고 현재 사용 중인 인터페이스의 IPv4 주소를 확인한다. Wi-Fi가 반드시 en0라고 가정하지 않는다.

```bash
date '+%Y-%m-%d %H:%M:%S %z'
networksetup -listallhardwareports
scutil --get LocalHostName
```

확인한 인터페이스에 대해 `ipconfig getifaddr <인터페이스>`를 실행한다. Windows와 같은 LAN인지, VPN 또는 게스트 Wi-Fi 격리가 있는지도 확인한다. 이전 주소 192.168.35.223이 여전히 유효한지 회신한다. 불필요한 전체 네트워크 설정이나 개인 정보를 수집하지 않는다.

완료 조건: Windows에서 접근해야 할 현재 Mac 주소와 사용할 경로(LAN/VPN)가 특정되어야 한다.

## M02. SSH 수신과 사용자 허용

```bash
lsof -nP -iTCP:22 -sTCP:LISTEN
```

시스템 설정 → 일반 → 공유 → 원격 로그인에서 현재 활성화 여부와 `jonghyun` 계정의 접속 허용 여부를 확인한다. 명령 결과가 비어 있으면 권한·실제 서비스 상태를 추가 확인하고, 빈 출력만으로 SSH 비활성화를 확정하지 않는다.

원격 로그인이 꺼져 있거나 계정이 허용되지 않았다면 필요한 변경을 사용자에게 정확히 안내한다. 보안·공유·방화벽 설정을 임의로 변경하거나 전체 방화벽을 끄지 않는다. SSH 키·비밀번호·개인 키 내용을 회신하지 않는다.

완료 조건: 22번 포트 수신 상태와 허용 계정이 확인되고, 미충족 조건이 구체적으로 기록되어야 한다. Mac loopback SSH 성공만으로 Windows 접근 성공을 주장하지 않는다.

## M03. 실제 Laya 서버 상태와 모델

```bash
lsof -nP -iTCP:8765 -sTCP:LISTEN
curl --fail --show-error --max-time 10 http://127.0.0.1:8765/health
```

기존 프로세스의 정체를 확인한다. 실행 중이지 않다면 준비된 가상환경과 캐시를 재사용하고 Tools/LayaWorker에서 시작한다.

```bash
python laya_worker.py serve --config enemy-data/worker-config.json --host 127.0.0.1 --port 8765
```

현재 CLI를 먼저 확인하고, 기존 정상 서버가 있으면 중복 실행하지 않는다. 새 중계 서버나 운영용 모의 응답을 만들지 않는다. 기본 바인딩은 loopback을 유지한다.

health에서 다음을 확인한다.

| 필드 | 기대값 |
|---|---|
| ready | true |
| protocol_version | 1 |
| metadata.backend | laya-mlx |
| metadata.model | aac6fef/laya-multilingual-mlx |
| metadata.revision | ba40c87fcb357f1643d04d71323af9cdc3b9e591 |
| metadata.config_sha256 | e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3 |

`transport-mock` 또는 `MOCK-NOT-LAYA`이면 실제 모델 연결로 인정하지 않는다. 값이 다르면 기존 설정 파일과 모델을 조사하고 차이를 보고한다. 검사를 통과시키려고 metadata만 바꾸지 않는다.

완료 조건: 실제 health 원문, 서버 시작 명령·확인한 PID·로그 경로·유지/종료 방법을 회신한다. 과거 PID 9919를 확인 없이 종료하지 않는다.

## M04. Mac 내부 실제 추론

기존 송신기를 사용해 골렘·엔트 예제를 실제 서버에 보낸다. 현재 CLI 옵션을 확인한 뒤 실행한다.

```bash
python laya_worker.py send --url http://127.0.0.1:8765 --input enemy-data/examples/requests.jsonl --output results/connection-recheck/<새-run-id>/predictions.jsonl
```

후보가 2개 이상인 요청의 실제 추론을 확인한다. 요청/세션/관측 번호 일치, 후보 안 선택, probabilities·selected_probability·inference_ms·metadata·usage를 보존한다. 예제 입력은 synthetic_fixture이며 실제 Unity 관측으로 표시하지 않는다. Mac 내부 추론 성공과 Windows 통신 성공을 구분한다.

완료 조건: 실제 요청·응답과 통과/실패 건수를 보존한다. 실패를 가짜 정상 응답으로 대체하지 않는다.

## W01. Windows 담당자와 연결 재시도

M01~M03 결과를 전달한 뒤 Windows 담당자가 실행한다. 로컬 18765를 모의 서버나 다른 프로세스가 사용하는지 먼저 확인한다. 해당 프로세스의 소유와 용도를 확인한 뒤 종료하거나 사용하지 않는 전달 포트를 선택한다. 다른 포트로 바꾸면 디버거 주소도 함께 바꾼다.

```powershell
Test-NetConnection <현재-Mac주소> -Port 22
ssh -N -o ExitOnForwardFailure=yes -o ConnectTimeout=10 -L 18765:127.0.0.1:8765 <허용계정>@<현재-Mac주소>
```

SSH 창을 유지하고 별도 창에서 실행한다.

```powershell
curl.exe --fail --show-error --max-time 10 http://127.0.0.1:18765/health
```

SSH 최초 호스트 키는 Mac 담당자가 제공하는 지문과 비교해 사용자가 확인한다. 인증과 호스트 키 검증을 우회하지 않는다. timeout은 네트워크/수신 경로, Connection refused는 도달한 주소의 수신 상태, Permission denied는 인증/계정 조건부터 조사하되 한 메시지만으로 원인을 단정하지 않는다.

완료 조건: Windows에서 받은 health가 M03 실제 모델 정보와 일치해야 한다. Mac에서만 curl을 실행한 결과로 W01을 통과 처리하지 않는다.

## W02. Unity 실제 전투 최종 확인

Windows Editor에서 CombatTestMap Play → F8 또는 Laya 전투 디버거 → 서버 루트 `http://127.0.0.1:18765` → 골렘/엔트 프로필 선택 → 연결 검사 → Mac 의도 적용 → 실험 시작 순서로 진행한다. 몬스터별 새 세션으로 확인한다.

동일 요청 ID에서 실제 관측 송신, 실제 Laya 응답, 검증, 의도 적용, 실제 BT 분기·Ability/Variant/MotionKey 실행까지 추적한다. HTTP 성공·모델 선택·의도 적용·실제 실행·명중은 각각 기록한다. 명중 미확인은 명중 성공으로 처리하지 않는다. 결과는 Editor에서 내보낸 session JSON으로 회신한다. 전투 감각·실물 패드는 별도 사용자 테스트다.

## 결과 보관과 회신 형식

새 증거는 `results/connection-recheck/<새-run-id>/`에 보관하고 기존 결과를 덮어쓰지 않는다. 화면·명령 출력·로그에서 토큰·비밀번호·키·인증 헤더를 제외한다. 서버를 유지할 수 없으면 현재 종료 상태와 재실행 명령을 명시한다.

다음 형식으로 한국어 회신한다.

```text
확인 시각/시간대:
현재 Mac 주소 / 인터페이스 / LAN·VPN:
SSH 원격 로그인 상태 / 수신 포트 / 허용 계정:
서버 실행 여부 / 바인딩 / PID / 로그 경로:
health 원문 또는 비밀정보 제거 파일:
모델·revision·설정 hash 일치 여부:
M01 / 통과·실패·미수행 / 증거:
M02 / 통과·실패·미수행 / 증거:
M03 / 통과·실패·미수행 / 증거:
M04 / 통과·실패·미수행 / 실제 추론 건수·결과 경로:
W01 / 통과·실패·Windows 확인 대기 / 증거:
W02 / 통과·실패·Windows 확인 대기 / 세션·요청 ID:
Windows가 사용할 정확한 SSH 명령과 디버거 서버 주소:
남은 조건 / 사용자가 직접 변경해야 할 설정:
서버 시작·상태 확인·종료 방법:
```

Mac 담당자가 단독으로 완료할 수 있는 것은 M01~M04다. 최종 완료는 Windows에서 실제 모델 health 수신과 Unity 실제 전투 실행을 확인한 후 판정한다.
