# Windows 클라이언트 연결 안내

2026-11-08 Mac 검증 기록이다. 기존 LayaWorker v1 구현을 재사용했으며 Unity 코드나 통신 계약은 변경하지 않았다. 서버는 인텐트 후보 하나만 선택한다. 이동·피해·명중·애니메이션·공격 실행과 실행 직전 자격 검사는 Unity 책임이다.

## 현재 연결: 무료·무계정 HTTPS 터널

Windows에서 Mac 사설 주소로의 연결이 실패했으므로 [Mac HTTPS 터널 전환 지침](MAC_HTTPS_TUNNEL_WORK_ORDER.md)을 따른다. Mac 서버는 Bearer 인증을 설정한 `127.0.0.1:8765`를 유지하고, Windows는 Mac이 전달한 실제 `https://<임시주소>.trycloudflare.com` 루트 주소로 직접 요청한다. 로그인·계정 생성·결제수단 등록·유료 전환을 하지 않는다.

디버거 서버 주소는 HTTPS 루트, 판단 경로는 `/v1/decide`다. Windows의 `LAYA_WORKER_TOKEN`을 설정한 환경에서 Unity/Player를 새로 실행한다. SSH 터널·로컬 `18765`·Mac 사설 IP는 이 경로에서 사용하지 않는다. 실제 발급 주소는 아직 없으며 아래 과거 검증 기록을 현재 연결 성공으로 해석하지 않는다.

## 결과 보기

- [검증 결과 HTML](output/server-readiness/2026-11-08-mac-01/results.html): 파일을 내려받아 브라우저에서 연다. Unity 없이 합성 골렘·엔트의 실제 모델 응답을 확인할 수 있다.
- [상세 보고서](output/server-readiness/2026-11-08-mac-01/REPORT.md)
- [원시 요청](output/server-readiness/2026-11-08-mac-01/requests.jsonl) / [원시 응답](output/server-readiness/2026-11-08-mac-01/predictions.jsonl)
- [HTTP 검증](output/server-readiness/2026-11-08-mac-01/http-checks.json) / [자동 테스트](output/server-readiness/2026-11-08-mac-01/unit-tests.txt)

HTML은 저장된 결과이며 실시간 상태나 전투 시뮬레이터가 아니다. 입력은 synthetic_fixture이며 runtime_capture가 아니다.

## SSH 대체 연결 순서 — 직접 접근 가능한 경우에만

1. Mac의 시스템 설정 → 일반 → 공유 → 원격 로그인을 활성화하고 `jonghyun` 사용자의 SSH 접속을 허용한다. 조사 시 TCP 22가 연결을 거절했다. 이 설정은 자동 변경하지 않았다.
2. Windows에서 Mac에 TCP 22로 도달 가능한 같은 네트워크 또는 VPN을 사용한다. 조사 시 Mac LAN 주소는 `192.168.35.223`이며 바뀔 수 있다.
3. Windows PowerShell에서 터널을 열고 해당 창을 유지한다.

```powershell
ssh -N -o ExitOnForwardFailure=yes -L 18765:127.0.0.1:8765 jonghyun@192.168.35.223
```

4. 다른 PowerShell에서 health를 확인한다.

```powershell
curl.exe --fail http://127.0.0.1:18765/health
```

5. 기존 송신기로 합성 예제를 보낼 수 있다. 결과 경로는 매 실행 새 이름을 사용한다.

```powershell
python Tools/LayaWorker/laya_worker.py send --url http://127.0.0.1:18765 --input Tools/LayaWorker/enemy-data/examples/requests.jsonl --output Tools/LayaWorker/results/windows-enemy-smoke-01.jsonl
```

Windows의 전달 포트는 `18765`, Mac 서버 포트는 기존 `8765`다. Unity의 요청 endpoint는 `http://127.0.0.1:18765/v1/decide`이며, 현재 디버거의 서버 주소 입력란에는 루트 `http://127.0.0.1:18765`를 넣는다. Mac 서버는 `127.0.0.1:8765`에만 바인딩하며 토큰 인증을 사용하지 않는다. SSH 인증은 필요하다. LAN 직접 공개·HTTPS·WebSocket·중계 서버는 도입하지 않았다.

health의 `ready=true`만으로 실제 Laya 연결을 판정하지 않는다. `metadata.backend=laya-mlx`와 모델·revision·설정 hash를 함께 확인한다. `transport-mock` 또는 `MOCK-NOT-LAYA`가 표시되면 테스트 서버이므로 실제 Mac 연결 검증으로 인정하지 않는다. 로컬 `18765`가 이미 사용 중이면 SSH는 실패해야 하며, 기존 프로세스의 용도를 확인한 뒤 포트 충돌을 해소한다.

## 클라이언트 계약

`protocol_version=1`을 사용한다. 응답의 session_id / request_id / state_seq를 해당 pending 요청과 비교한다. state_seq는 전송한 스냅샷 순번이므로 매 틱 현재 순번과 비교해 무조건 폐기하지 않는다. 현재 대상·몬스터 생성 세대·전투 상황·유효기간은 별도로 검사한다. selected_action_id는 요청 후보에 있어야 한다. 후보별 probabilities, selected_probability, inference_ms, decision_source, metadata, usage를 보존한다. 단일 후보 응답은 single_candidate로 모델 추론을 생략한다.

동일 ID·동일 본문 재전송은 메모리 캐시가 남아 있으면 같은 결과를 반환한다. 동일 ID·다른 본문은 409다. 서버의 503은 추론 중임을 뜻한다. 현재 Unity 정책은 실패 후 로컬 판단을 유지하고 backoff 후 새 관측을 보내며 오래된 요청을 자동 재전송하지 않는다. 후속 재전송을 도입한다면 아직 유효한 요청의 동일 ID·동일 본문만 허용한다. 캐시는 최근 256개이며 재시작·퇴출 후 영속 exactly-once를 보장하지 않는다.

400/413/415는 요청 수정, 401은 인증 확인, 422는 토큰 잘림·후보 구분 유실 등의 입력 거절, 502는 추론 실패다. 실패를 정상 행동 선택으로 취급하지 않는다. 요청 전체 64KiB, 관측 4096 bytes, 후보 1~8개이며 토큰 제한도 별도 적용한다. 실제 실행 전에 Unity BT/Ability 자격을 다시 검사한다.

## 과거 서버 운영 및 검증 범위 — 무인증 loopback/SSH 기준 기록

커밋 직전 Mac health ready=true를 재확인했다. 당시 PID는 9919다. 커밋에 보존한 PID·주소·실행 상태는 시점 기록이므로 사용할 때 재확인한다. 자동 시작 서비스는 설치하지 않았다.

Mac 상태 확인: `curl --fail http://127.0.0.1:8765/health`

Mac 재시작: [start-server.sh](output/server-readiness/2026-11-08-mac-01/start-server.sh)를 `sh`로 실행한다. 이 스크립트의 Python·HF 캐시 절대 경로는 검증한 Mac 전용이다. 이미 실행 중이면 중복 실행하지 않는다. 터미널 실행의 종료는 Ctrl+C이며 현재 백그라운드 서버는 PID가 이 서버인지 확인 후 `kill 9919`로 종료한다.

실제 로그·결과 원본은 Mac의 `Tools/LayaWorker/results/server-readiness/2026-11-08-mac-01/`에 있다. 이 문서에 연결된 output 디렉터리는 Git 전달용 사본이다.

자동 테스트 34개, 패키지 무결성, 실제 모델 HTTP 12건, 캐시·409·400·413·422·503 검증 통과. 추론 실패·인증은 기존 모의 테스트로 확인했다. 모델 로드 1884ms, 첫 추론 219ms, 이후 11회 중앙값 230ms, HTTP 중앙값 226ms는 Mac 단독 측정이다. Windows↔Mac 통신과 실제 Unity 전투·판단 품질은 미검증이다. Windows 담당자가 SSH 터널, 실제 관측 송신, 상태 갱신 경쟁, 오류/503 처리와 BT/Ability 실행 자격을 확인해야 한다.

모델·패키지·revision·설정 hash와 상세 측정 범위는 보고서와 health.json에 기록했다. 요청한 TRAINING_GROUND_INTEGRATION_DESIGN.md는 현재 체크아웃에서 발견되지 않았다.
