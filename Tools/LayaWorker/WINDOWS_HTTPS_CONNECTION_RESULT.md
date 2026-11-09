# Windows → Mac HTTPS 연결 검증 결과

2026-11-08 Windows에서 사용자가 명시적으로 허용한 HTTPS 목적지에 실제 요청을 보냈다. 토큰은 사용자가 지정한 바탕화면 `token.txt`를 메모리로 읽어 Authorization 헤더에만 사용했다. 토큰 값·헤더는 결과 파일·문서·Git에 보존하지 않았다.

## 확인한 연결

- 루트: `https://yamaha-uniform-concern-wearing.trycloudflare.com`
- 상태: `/health`
- 판단: `/v1/decide`
- 연결 방식: Windows에서 공개 HTTPS에 직접 접속. VPN·SSH·로컬 전달 포트 변경 없음.
- 실제 backend: `laya-mlx`, worker `1.0.1`, runtime `0.6.0`.
- 모델: `aac6fef/laya-multilingual-mlx`, revision `ba40c87fcb357f1643d04d71323af9cdc3b9e591`.
- 설정 hash: `e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3`.

이는 당시 실제 연결 성공 기록이다. Quick Tunnel 주소와 서버 생존은 후속 사용 시 재확인한다.

## 인증과 실제 모델 추론

| 검사 | 결과 |
|---|---|
| 무토큰 health | 401, 실제 Laya JSON 오류 |
| 잘못된 토큰 health | 401, 실제 Laya JSON 오류 |
| 정상 토큰 health | 200, ready=true, 기대 backend/model/revision/hash 일치 |
| 골렘·엔트 합성 요청 | 12/12 HTTP 200, 새로운 세션/요청 ID로 전송 |
| 응답 계약 | protocol/session/request/state_seq 일치, 후보 안 선택, 확률 키·범위·합·선택 확률·최고 확률 일치 |
| 실제 추론 여부 | 12/12 decision_source=laya, cached=false |
| 입력 손실 | 12/12 usage.truncated=false, state_tokens_dropped=0 |
| 모델·설정 | 12/12 기대 metadata 일치 |

입력은 기존 `enemy-data/examples/requests.jsonl`의 **synthetic_fixture**다. 실제 Unity 관측·BT 실행·명중·전투 판단 품질을 검증한 것이 아니다.

Windows 셸에서 Python 명령을 찾지 못해, 설치된 Node의 기본 HTTPS 모듈로 같은 HTTP 계약을 검증했다. TLS 기본 검증 유지, 리다이렉트 추적 없음, 응답 크기 제한, timeout을 적용했다. 최초 sandbox 네트워크 차단 결과는 성공으로 계산하지 않았다. 외부 전송에 대한 사용자 명시적 승인 후 실제 검증을 수행했다.

## Windows 실측 지연

| 항목 | 값 |
|---|---:|
| 정상 인증 health 왕복 | 497.554ms |
| 12건 추론 왕복 중앙값 | 558.256ms |
| 12건 서버 추론 중앙값 | 160.882ms |
| 최대 왕복 | 841.310ms |
| 왕복만으로 현재 TTL 750ms 초과 | 1/12 |
| 왕복만으로 현재 timeout 1000ms 초과 | 0/12 |

Mac에서 공개 주소로 측정한 1690.949ms와 이번 Windows 표본은 경로·시점이 다르므로 동일 측정값으로 취급하지 않는다. 이번 Windows 표본의 나머지 11건도 Unity에서 반드시 유효하다는 뜻은 아니다. 실제 적용에는 관측 준비·BT 대기·상태 무효화가 추가된다. 첫 요청의 추론은 415.282ms, 왕복은 841.310ms였으며 전체 중앙값과 분리한다. 네트워크 안정성을 보장하는 장기 표본은 아니다.

골렘·엔트 프로필은 requestInterval=0.5, validitySeconds=0.75, timeoutSeconds=1을 유지했다. 성공률을 높이려고 TTL·timeout을 임의로 늘리지 않았다. 추가 판단은 실제 Unity 세션의 폐기·timeout·행동 적용 근거로 한다.

## Unity 설정 시도와 검증 한계

설정 시도 전 Unity `6000.3.21f1`, Edit Mode, 컴파일 중 아님을 확인했다. MCP execute_code로 다음을 요청했으나 응답이 시간 초과됐다.

- 현재 Unity 프로세스의 환경변수에 토큰 설정. 시스템 전역 환경변수·PlayerPrefs에 토큰 저장 없음.
- PlayerPrefs `Laya.Endpoint`에 위 HTTPS 루트 저장.
- 비동기 health 요청과 두 몬스터 프로필의 metadata 검증.

후속 상태 조회도 시간 초과되어 **코드 실행·설정 적용·Unity health 완료 여부는 미확인**이다. 부분 실행 가능성이 있으므로 성공이나 미실행으로 단정하지 않는다. Play Mode 진입·빌드·씬/에셋 저장·강제 재시작은 하지 않았다.

## 사용자 테스트

1. Unity가 정상 반응하는지 확인하고, 기존 작업을 저장한다. 토큰을 전달받지 못한 프로세스라면 아래 PowerShell 환경에서 설치된 Unity 실행 파일을 직접 시작한다. 기존 Unity/Hub는 새 셸의 환경변수를 자동 상속하지 않는다.

```powershell
$layaTokenFile = Join-Path ([Environment]::GetFolderPath('Desktop')) 'token.txt'
$env:LAYA_WORKER_TOKEN = [System.IO.File]::ReadAllText($layaTokenFile).Trim()
```

2. `CombatTestMap`의 기존 전투 테스트 진입 절차로 Play하고 Laya 디버거를 연다. 서버 주소에는 루트 `https://yamaha-uniform-concern-wearing.trycloudflare.com`만 입력한다.
3. 골렘 프로필로 연결 검사 후 먼저 `관찰 비교`를 실행한다. 요청/응답·모델 식별·왕복·폐기 사유를 확인한다.
4. 훈련장을 재시작하고 `Mac 의도 적용`으로 전투한다. 원격 의도 적용과 실제 행동 실행을 별도로 확인한다. 엔트도 같은 순서로 확인한다.
5. 세션 결과를 내보내고 `session ID / 모드 / 연결 결과 / 요청·적용·실행·폐기 수 / timeout·TTL 사유 / 로그 위치`를 회신한다. 토큰 파일·토큰 값은 첨부하지 않는다.

통신 성공과 별도로 실제 입력·전조·히트스톱·캔슬·카메라·게임패드 품질은 사용자 테스트 대기다. 기존 지침대로 Player/Addressables 빌드는 수행하지 않는다.

## 원시 결과 위치

이 Windows 작업 공간의 로컬 결과이며 `results/`는 Git 제외 대상이다.

`Tools/LayaWorker/results/https-tunnel/windows-2026-11-08T04-01-48-427Z/`

- `health-checks.json`: 실제 인증 3건과 metadata.
- `inference-checks.json`: 합성 요청 12건과 원시 응답·왕복·검증 결과.

문서만 받아간 다른 PC에는 위 로컬 파일이 없을 수 있다. 이번 작업에서 커밋·push는 수행하지 않았다.
