# 훈련장 Laya 구현 및 검증 기록

2026-11-08 / Unity 6000.3.21f1 / Windows / 후속 사용자 요청에 따라 이번 세션 변경만 커밋.

최신 사용자 지시: Player 및 Addressables 빌드를 수행하지 않는다. 이전 지시로 시작한 Development 빌드에 취소를 요청했으나 Unity MCP 응답 시간 초과와 컴퓨터 조작 연결 불가로 취소 여부를 확인하지 못했다. 추가 빌드·Player 실행은 하지 않는다. 이후 검증은 Editor 컴파일·EditMode·Editor Play Mode 범위로 진행한다.

## 실행

1. Mac에서 `Tools/LayaWorker`로 이동하고 준비된 가상환경을 활성화한다.
   `python laya_worker.py serve --config enemy-data/worker-config.json --host 127.0.0.1 --port 8765`
   이미 실행 중이면 중복 실행하지 않는다. 상세 환경은 `WINDOWS_CLIENT_HANDOFF.md`와 Mac readiness 보고서를 따른다.
2. Windows에서 `ssh -N -o ExitOnForwardFailure=yes -L 18765:127.0.0.1:8765 jonghyun@192.168.35.223`을 유지한다. 주소는 Mac 현재 주소로 확인한다. 인증 토큰이 필요한 환경은 Unity/Player를 시작하기 전에 `LAYA_WORKER_TOKEN` 환경변수만 설정한다. 화면이나 파일에 토큰을 넣지 않는다.
3. Editor에서 `Assets/01.Scenes/Test/CombatTestMap.unity`를 열고 Play. UPlayground 툴 런처의 전투 카테고리에서 `Laya 전투 디버거`를 연다. Game 화면에서는 F8 또는 개발 치트 패널의 Laya 버튼으로 연다.
4. Development 런타임 진입 코드는 F8을 지원하지만, 최신 사용자 지시에 따라 Player 빌드·실행은 검증 범위에서 제외한다. 실행 파일 생성 완료를 확인하지 않았다.
5. 서버 루트 `http://127.0.0.1:18765`, 골렘 또는 엔트, 판단 모드를 고른다. 원격/비교 모드는 선택한 프로필로 연결 검사를 통과한 뒤 `실험 시작`. 기존 AI는 서버 없이 시작한다. 한 실험에는 한 마리만 생성한다.
6. 시작 시 기존 훈련 UI를 닫고 전투가 진행된다. F8 상세에서 요청을 선택하고 요약·시간/관측/후보/패킷/실행 탭을 확인한다. 상세는 플레이어 입력을 억제하며 Cancel/F8로 요약에 복귀한다. 요약은 입력을 점유하지 않는다.
7. 화면 갱신 멈춤은 기록·전투를 멈추지 않는다. 전투 일시정지는 별도 GameTime 요청으로 처리한다. 실험 종료는 상대를 정지시키고 결과를 저장한다. 다른 프로필/모드는 훈련장 재시작 후 새 실험으로 시작한다.
8. Editor 결과는 `Tools/LayaWorker/results/training/<session>/`, Player 결과는 `Application.persistentDataPath/LayaTraining/<session>/`. 화면 하단에 실제 경로와 성공/실패를 표시한다. 에디터 창의 `저장 로그 열기`에서 `session-*.json`을 연다. 별도 Player 실시간 연결 기능은 없다.

## 구현 경계

- `EnemyRemoteIntentController`는 훈련 상대에만 주입한다. 운영 BT 에셋과 GAS 수치/MotionSet은 변경하지 않는다.
- 기존 평가를 한 번 수행하고, `EvaluateEnemyCombatIntentService`의 Blackboard 최종 쓰기 직전에서 유효한 원격 의도로 치환한다. 비동기 응답 콜백은 Blackboard를 직접 쓰지 않는다.
- 요청/세션/관측 번호, 생성 세대, 대상과 대상 활성 세대, 전투 epoch, TTL, 후보·확률·모델·설정 hash·토큰 잘림을 검사한다. 매 이동 상태 변화만으로 폐기하지 않는다. 공격·피격·정지·대상/페이즈 변경·사망·풀 반환은 무효화한다.
- GET health와 POST v1/decide는 UnityWebRequest 비동기 호출이다. 재전송으로 오래된 판단을 되살리지 않는다. 일시적 실패는 로컬 AI를 유지하고 임계 횟수 후 health 재검사, 계약/인증 오류는 명시적 연결 재검사로 복구한다.
- GAS 조회는 비용·쿨다운·활성화 실패 계측을 소비하지 않는 InspectVariant를 사용한다. 관측이 추가 난수 추첨이나 공격 슬롯 획득을 하지 않는다. 실제 BT 실행은 기존 그룹 슬롯·GAS·모션 검사를 그대로 통과해야 한다.
- 이동 상태 진입, Ability 시작/종료, CombatResult를 별도로 기록한다. 요청 ID로 BT 확정과 실제 branch/Ability/Variant/MotionKey를 연결한다. 우선 분기 개입은 원격 행동 성공에 집계하지 않는다.
- 같은 Ability/Variant/MotionKey의 반복 투사체가 여러 실행에 대응하면 임의 귀속하지 않는다. 타격 0은 명중 확인 없음이며 명중 성공이 아니다.
- 진단은 기본 512건/본문 8MiB 상한. 본문 만료와 기록 퇴출을 표시한다. 두 화면은 하나의 세션 저장소를 읽는다. ListView는 고정 높이 가상화, 화면은 5Hz 갱신이다.
- 같은 snapshot JSON과 observations/requests/responses/decisions/executions JSONL을 내보낸다. 파일은 매번 새 이름으로 저장한다. 과거 버퍼에서 이미 퇴출한 기록을 복구할 수는 없다.
- UI Toolkit UXML/USS/PanelSettings/NotoSansKR 폰트/두 프로필은 Resources 참조로 Player에 포함한다. 런타임 뷰에는 UnityEditor 의존이 없다. 상세 기록·진입점·통신 주입 코드는 UNITY_EDITOR 또는 DEVELOPMENT_BUILD에서만 컴파일한다.

## 자동 검사와 실제 전투 관측

| 항목 | 결과 | 근거/범위 |
|---|---|---|
| 관련 EditMode | 75/75 통과 | 프로토콜, HTTP 모의, 공격 자격/거리, 그룹 슬롯 정책. `Logs/LayaTraining/EditMode-75.txt` |
| 최종 프로토콜·HTTP 회귀 | 15/15 통과 | job 366504f0497442adb2b0a64a6f3f9b57, 2.707초. `Logs/LayaTraining/EditMode-15-final.txt` |
| 모의 오류 | 통과 | 503, timeout, malformed JSON, 크기 초과, 취소. 실제 모델 추론 아님 |
| 골렘 Editor 전투 | 원격 적용 5, 원격 행동 3, 확인된 명중 0 | session `9c235517daeb42d9999c16542958659b`. 실제 GAS 공격 1.01/3.03 관측. 최초 실험은 대상 감지 API로 타깃 지정; 이후 시작 시 자동 지정 보정 |
| 엔트 Editor 전투 | 원격 적용 2, 원격 행동 2, 확인된 명중 0 | session `5333b351791249a3af70af4fcaaa7359`. `Actor.Ent.Attack.2.02` 실행 확인. 실제 생성/탐지/BT/GAS, 공격 강제 실행 없음 |
| 실제 Mac | 연결 실패 | 인계된 192.168.35.223:22 SSH가 5초 후 timeout. Windows↔Mac 전투 검증 미수행 |
| 화면 | 캡처 확인·보정 | 입력 필드 대비, 긴 미측정 거리, 상세/하단 잘림 보정. `Logs/LayaTraining/*Debugger.png`. 실물 패드와 1280×720 품질은 사용자 테스트 대기 |

모의 서버는 `training_mock_server.mjs`이며 production 중계 서버가 아니다. backend=`transport-mock`, worker_version=`MOCK-NOT-LAYA`를 내보낸다. 실제 기본 프로필은 이 서버의 연결 검사를 거절한다. 자동 전투 검증은 메모리에서만 복제한 프로필의 backend를 변경했으며 저장된 실험 프로필은 실제 laya-mlx 설정이다. 원격 응답만 모의이고 실제 전투 관측·Ability 실행은 Unity 런타임에서 수집했다.

Development 빌드는 Addressables 전체 빌드를 하지 않고 `Builds/ScenarioOptimization_20261024/UPlayground_Data/StreamingAssets/aa`의 기존 공통 콘텐츠를 재사용한다. 현재 두 몬스터 Definition은 프로필 직접 참조로 포함하고 SpawnActor(Definition)으로 실행한다. 이전 공통 번들의 다른 콘텐츠까지 최신이라고 보장하지 않는다. 빌드 결과는 `Logs/LayaTraining/BuildReport.txt`에 기록한다.

## 사용자 테스트 명세

준비: 위 CombatTestMap 또는 Development 실행 파일, 실제 Mac 서버/SSH 터널, 키보드·마우스와 게임패드. 기존 저장 데이터 변경 없이 훈련장을 사용한다. 저장 파일 삭제나 진행도 초기화는 하지 않는다. 각 항목은 골렘과 엔트로 각각 실행한다.

| ID | 절차 | 기대 결과와 실패 기준 |
|---|---|---|
| L01 | 기존 AI로 시작해 30초 전투 | Mac 송신 0, 기존 공격/텔레그래프/피격 반응 유지. 서버가 없어도 진행 |
| L02 | 재시작 → 관찰 비교 → 연결 검사 → 시작 | 요청·모델 확률 표시, 원격 의도 적용 0. 기존 BT만 실제 전투 |
| L03 | 재시작 → Mac 의도 적용 → 1분 전투 | 동일 요청의 송신/응답/검증/적용/실제 실행 확인. 일부 요청의 `실행` 탭에 Ability·Variant·MotionKey. HTTP 성공만 있고 실행이 없으면 통과 아님 |
| L04 | 상대 공격 범위 안에서 공격을 맞거나 방어 | 타격 판정과 실행 연결 확인. 피격 무적·방어를 실제 피해와 구분. 반복 투사체 귀속 불확실은 미확인으로 유지 |
| L05 | 추론 중 정지·일시정지·상대 처치·재시작 | 이전 요청으로 행동이 뒤늦게 시작하지 않음. 폐기 사유 표시. 재시작 후 세션 ID 변경 |
| L06 | 전투 중 Mac 종료/터널 종료 → 재연결 | 게임 멈춤 없이 로컬 전투. 시간 초과/로컬 전환 기록. 복구 후 새 요청만 실행. 잘못된 worker-config는 거절 |
| L07 | 상세 열기 → 목록 이동 → 숄더/트리거 탭 전환 → Cancel | 초기 포커스 보임, 상세 중 플레이어 공격/이동 없음, Cancel로 요약 복귀하고 조작 복원. 목록 선택·스크롤·긴 JSON 확인 가능 |
| L08 | 1280×720 및 좁은 Editor 창 | 겹침/잘림 없이 목록/상세 전환, 한글·오류·빈 목록 읽힘. 좁은 화면은 요청 목록/선택 상세 버튼 사용 |
| L09 | 화면 갱신 멈춤 → 해제, 별도로 전투 일시정지 | 화면만 멈춘 동안 전투·수집 계속. 전투 일시정지에서 실제 전투 정지. 해제 후 시간 배율 복원 |
| L10 | Editor+인게임 디버거 동시에 열기 | 요청 수가 두 배가 되지 않음. 선택은 독립, 진단 저장소는 동일 |
| L11 | 결과 내보내기 → Player 종료 → Editor에서 JSON 열기 | 원문 JSON/실행/시간 유지, 저장 위치/실패 표시. 토큰이 화면·복사·파일에 없어야 함 |
| L12 | 두 몬스터의 전투를 직접 플레이 | 텔레그래프, 히트스톱, 캔슬 창, 그룹 공격 슬롯이 기존 체감과 일치. 원격 지연에 멈춤/부당한 즉발 공격 없음 |

회신 형식: `항목 ID / 골렘·엔트 / Editor·Development / 통과·실패·미수행 / 실제 결과 / 로그 또는 화면`.
실패 시 요청 ID, 입력 장치, 모드, 연결 방식, 해당 session JSON과 Player.log를 전달한다. 토큰·SSH 키는 전달하지 않는다.

알려진 검증 한계: 실물 게임패드·전투 감각은 사용자 테스트 대기. 명중 귀속과 실제 Mac 응답 기반 전투는 미확인. 화면 캡처 시 Editor가 백그라운드 상태였으며 FPS 숫자는 성능 측정 결과로 사용하지 않는다. 기존 Raon 무게 프로필 폴백 및 기존 훈련 안내 폰트의 중점 누락 경고는 이번 변경과 별개로 관측했다.
