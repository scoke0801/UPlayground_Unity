# C — NPC 생활 행동 인계

> 2026-10-22 / C 독립 구현·전용 프리팹·관련 자동 검사를 인계한다. EditMode 14/14, 최종 Editor Play Mode 11/11 통과. 본편 배치, 실제 보행·모션 품질, 사용자 검증은 대기다.
> 기준: [A 6.2·6.4·8절](A_BASELINE.md), [작업 C](../LAKE_OF_LIFE_LIVING_WORLD_TASKS.md), [테스트 지침](../../guide/TEST_VALIDATION_GUIDE.md).

## 1. 완료 범위와 남은 범위

기존 주민 몸에 연결하는 `NpcRoutine`, 정원 1의 `NpcWorkPoint`, 기존 상태 머신/KCC를 사용하는 `NpcRoutineState`, D 환경 입력 연결 컴포넌트를 구현했다. 작업→다음 작업점 이동→휴식, 자리 경쟁, 정체/도달 제한시간, 정면 충돌, 취소, 대화 중단·재예약, 숨김·씬 종료·로드의 임시 소유권 정리를 처리한다. 새 군중 매니저나 NPC별 Update는 추가하지 않았다. 기존 `NpcMovementController`의 상태 틱을 사용한다.

`NpcBrain`의 기존 고정 작업/작업 방향과 `NpcLifeInteractable`의 일회성 인계는 유지한다. `NpcLifePresentation`은 서사 배치를 복원하는 동안 일과를 홀드한다. 반복 일과는 인벤토리, 완료 플래그, 퀘스트, 물자 표시 배열을 쓰지 않는다. 본편 씬·기존 주민 데이터·캐릭터 프리팹·Flow·공용 Data/Contracts/asmdef는 변경하지 않았다.

**남음:** I의 기존 몸·서사 단계 바인딩, B/I의 실제 보행 구간과 처마/야간 자리, 본편 D 연결, 실제 NPC 리그의 모션/소품 적합성, 게임패드 접근성, 저장 복사본과 두 맵 왕복, 성능 측정. 제공한 프리팹은 **배치용 설정 묶음**이며 주민 모델이나 완성된 조리·운반 연출을 포함하지 않는다.

공용 SO 타입이 I에서 게시되지 않았으므로 C의 인스턴스 설정은 전용 프리팹의 직렬화 필드에 둔다. 공용 Data 타입이나 동명 날씨 서비스를 독자적으로 추가하지 않았다. 공용 SO로 옮길 경우 I가 타입/직렬화 호환·변환을 먼저 제공해야 한다.

## 2. 변경 파일과 신규 식별자

기존 수정:

- `Assets/02.Scripts/GameActor/Object/NPC/NpcActor.cs`: 일과 중단 통지와 서사 배치 홀드.
- `Assets/02.Scripts/GameActor/MovementController/NpcMovementController.cs`: 일과 연결 및 비활성 정리.
- `Assets/02.Scripts/GameActor/State/NPC/NpcIdleState.cs`, `NpcWanderState.cs`: 일과 진입 및 보류 중 자율 배회 차단.
- `Assets/02.Scripts/GameActor/State/NPC/NpcTalkState.cs`, `NpcIdleState.cs`: 일과 중단 뒤에도 공중 낙하 속도 보존. 접지를 잃은 대화 보행은 수평 추진을 중지.
- `Assets/02.Scripts/Gameplay/Interaction/NpcLifePresentation.cs`: 복원 대기/배치 적용의 일과 홀드.
- `Assets/Tests/PlayMode/FlowGraph/NpcLifeActionTests.cs`: 기존 회귀에 실제 작업점 수명주기·임시 소품 정리·취소된 인계의 늦은 재개·공중 속도 보존 검사 추가.

후속 검수에서 `NpcLifePresentation` 인계 연출에 실행 세대를 추가했다. 취소한 IEnumerator를 새 인계 시작 뒤 재개해도 새 소품의 위치/표시나 실행을 변경하지 않는다. `NpcRoutineState.UpdateVelocity`는 접지를 잃으면 수평 이동만 정지하고 누적된 수직 속도를 보존해 매 KCC 스텝마다 낙하 가속도가 사라지는 문제를 수정했다.

추가 검수에서 일과를 취소한 뒤 Idle/Talk로 전환하면 기존 상태가 낙하 속도를 다시 지우는 문제를 확인했다. 두 상태에도 수직 속도 보존을 적용했다. 최종 Play Mode 검사에서 세 상태 각각의 두 연속 속도 갱신을 확인했다. 실제 지형에서의 낙하·재접지는 LW-T05-f로 구별한다.

신규 코드/검사(각 meta 포함):

- `Assets/02.Scripts/GameActor/Component/NPC/LivingWorld/NpcRoutine.cs`, `NpcWorkPoint.cs`
- `Assets/02.Scripts/GameActor/State/NPC/NpcRoutineState.cs`
- `Assets/02.Scripts/Gameplay/Interaction/LivingWorld/NpcRoutineEnvironmentBinding.cs`
- `Assets/Tests/EditMode/LivingWorld/NPC/NpcRoutineContractTests.cs`
- 본 문서와 meta. 신규 하위 폴더 meta도 함께 전달한다.

프리팹 루트: `Assets/03.Prefabs/LivingWorld/NPC/`. 각 파일은 주민 몸을 포함하지 않는다. 신규 Actor ID·SceneEntityId·Addressables 주소·저장 키는 없다. 아래 Actor ID는 A의 기존 몸을 찾는 통합 명세이며 이름별 런타임 분기로 사용하지 않는다.

| 프리팹 / 기존 Actor ID | GUID | 작업점(순서대로 작업 2곳 / 휴식 1곳) |
|---|---|---|
| `JoyRoutine.prefab` / `NPC_Joy` | `bf121b713ae3ba34ba20cc83d34b6ae7` | `lake.meal.prep`, `lake.meal.serve`, `lake.meal.rest` |
| `HazelRoutine.prefab` / `NPC_Hazel` | `4a425b7f88e87314a960a6698a191995` | `lake.care.pack`, `lake.care.visit`, `lake.care.rest` |
| `MorganRoutine.prefab` / `NPC_Morgan` | `a10dff7cb7afa11478051a692581a420` | `lake.transport.inspect`, `lake.transport.cart`, `lake.transport.rest` |
| `YeonmokRoutine.prefab` / `Npc_Yeonmok` | `d3409a2755d85aa4aa5f9c2b5f32c254` | `lake.repair.tools`, `lake.repair.work`, `lake.repair.rest` |
| `RemainingResidentRoutine.prefab` / `RedTree_RemainingResident` | `be8583bc5266a8b4c86eb0b52374fea5` | `drought.resident.tools`, `drought.well.wait`, `drought.shade.stay` |
| `SettlingResidentRoutine.prefab` / `RedTree_SettlingResident` | `2a39af9f9bf68be4aac260c1984af8b9` | `drought.depart.pack`, `drought.depart.wait`, `drought.shade.depart` |

호수 4개 mapId는 `LakeOfLifeScenario`, 피해 2개는 `DroughtVillage`다. 작업점 ID는 A의 예약값 그대로이며 슬롯은 모두 0/정원 1이다. 같은 mapId/pointId가 동시에 두 개 활성화되면 양쪽의 새 예약을 거부한다.

**전달 주의:** 기존 `.gitignore`가 `Assets/03.Prefabs/` 아래 신규 파일을 제외한다. 위 6개 prefab, meta와 `NPC.meta`를 I의 전달/커밋에 명시적으로 포함해야 한다. 규칙을 변경하거나 스테이징/커밋하지 않았다.

## 3. 공개 계약과 데이터 설정

### 일과와 서사의 경계

- `NpcRoutine.TryBindActor(NpcActor)`는 이미 존재하는 몸과 Brain/MovementController를 연결한다. 같은 몸에 다른 일과가 연결돼 있으면 false다. `_actor` 직렬화 참조로도 연결할 수 있다.
- `SetPlacementReady(bool)`는 I가 서사 배치와 통행 구간을 확정한 뒤 제어한다. 초기 프리팹 값은 **false**다. 일과가 서사 위치를 추측하거나 새로운 몸을 만들지 않는다.
- `_requiredFlag/_blockedFlag`는 읽기 전용 게이트다. 잔류 주민은 `lake.red_tree.resident_stays` 이후, 짐 주민은 `lake.red_tree.departure_prepared` 이전에만 일과가 가능하다. 출발 준비 뒤 짐을 원래 자리로 되돌리지 않는다. 다른 주민의 복합 서사 단계는 I가 `SetPlacementReady`로 연결한다.
- `NpcActor.PauseRoutine()`은 즉시 예약/소품을 해제하고 IDisposable 홀드를 반환한다. `NpcLifePresentation`이 복원 대기와 실제 적용에서 사용하며 정상 종료·비활성화 때 해제한다. 서사 좌표 복원은 기존 Presentation 정책이 소유한다.
- 플레이어 대화/기존 생활 보행의 `Interact`와 `BeginDialogueStage`는 일과를 즉시 중단한다. 일과 보행에는 대화 홀드를 사용하지 않으므로 NPC 대화 입력을 잠그지 않는다. 전역 대화가 진행 중일 때도 일과 이동을 보류한다. 종료 후 현재 시각·플래그·환경에서 다시 예약한다.
- `CancelRoutine()`은 임시 실행만 취소하며 이후 재시도 가능하다. 장기간 중지는 `SetPlacementReady(false)` 또는 컴포넌트 비활성화를 사용한다. `SetRiskActive(true)`는 소품/예약을 먼저 정리하고 보호 휴식점을 우선한다. 진행 중 서사 대화를 강제로 종료하지 않으며 위험에 의한 연출 취소 정책은 I가 기존 대화/상호작용 취소 경로로 연결한다.

### 점유와 이동

`TryReserve(owner, out generation)` → 경유점/KCC 이동 → 실제 도착·높이·접지·방향 확인 → 손 소품 생성/작업 모션 → 체류 종료/소품 비활성·파괴 → `Release(owner, generation)` 순서다. 소유자나 실행 세대가 다르면 해제/유효성 검사는 실패한다. 지연된 이전 실행이 새 예약을 해제할 수 없다.

`_steps`는 두 작업점, `_rest`는 휴식/피신 자리다. 작업 시간은 주민 순서에 따라 첫 작업 18~23초, 두 번째 10~15초, 휴식 15~20초다. 준비 06~09시는 첫 작업, 09~18시는 두 작업과 휴식을 순환, 18~22시와 22~06시는 가까운 휴식 자리다. 시작·재시도 오프셋은 0/2/4/6/8/10초다. 기본 재시도 5초, 이동 제한 20초, 정체 2초, 유효 진전 0.1m, 수평 도착 0.35m, 높이 허용 0.5m, 걷기 배율 0.6이다. 수치는 모두 직렬화 설정이며 현장 확정값이 아니다.

경로 탐색기를 새로 구현하지 않았다. `_steps[*].waypoints`와 `_rest.waypoints`에 짧은 검증된 경유점을 지정한다. 방향을 가로막는 비접지 충돌은 즉시 취소하고, 움직임 없는 경우 정체/제한시간으로 정리한다. 경쟁/막힘에는 휴식점을 우선하며 그것도 사용할 수 없으면 현재 자리에서 대기한다. 일과는 순간이동하거나 NPC를 숨기지 않는다. 경유점은 중간 대화 후 재접근·왕복도 안전해야 한다. 임의 장애물 우회·군중 회피까지 지원한다고 보지 않는다.

### 저장과 소품

예약·커서·손 소품·모션 프레임은 저장하지 않는다. `OnFlagsReloaded`에서 임시 소유권과 커서를 비우고 최소 2프레임 및 서사 배치 홀드 종료 후 다시 예약한다. 시간은 기존 `IGameTimeService`의 시각/분 변경을 읽는다. 환경은 생산자의 마지막 준비 상태를 유지해 저장 복원 순서가 달라도 임의로 맑음으로 바꾸지 않는다.

작업점 `_heldPropPrefab`은 장식 전용 손 소품, 일과 `_propSocket`은 기존 몸의 손 소켓이다. 소품이 있는데 소켓이 없으면 예약 전에 거부한다. 소품은 작업 도착 후에만 생성하며 이동 중 운반 자세를 만들지 않는다. 취소 시 즉시 비활성화하고 Destroy한다. 기존 인계 꾸러미, 보상/ItemActor, NpcLife 완료 소품을 여기에 연결하지 않는다. 기본 6개 설정의 손 소품은 모두 비어 있다.

### D 환경 입력

각 프리팹의 `NpcRoutineEnvironmentBinding._environment`에 같은 맵의 D `EnvironmentController`를 연결한다. `OnStateChanged`, `MapId/IsReady/Revision/Current`를 읽으며 날씨를 생성하지 않는다. 강수 진입 0.15/해제 0.05, 정규화 풍속 진입 0.65/해제 0.5의 히스테리시스를 사용한다. D v1의 풍속 범위 0~1에 맞췄다.

Actor 측 입력은 `BindEnvironment(object producer)`, `ApplyEnvironment(producer, mapId, revision, isReady, needsShelter)`, `ReleaseEnvironment(producer)`다. 다른 맵/옛 revision/교체 전 생산자의 입력은 거부한다. 미준비이면 현재 안전 상태를 유지하고 새 피신 결정을 하지 않는다. 연결 컴포넌트는 생산자 교체 때 새 토큰과 구독으로 시작한다. Actor→Assembly-CSharp 역참조나 공용 서비스 등록은 없다.

D 구현이 있는 현재는 실제 시간표와 공개 이벤트를 사용한 **독립 연결 검사**까지 수행했다. I의 공용 환경 계약 게시·서비스 등록과 실제 본편 연결은 여전히 대기다. 향후 I 어댑터로 바꿀 때 이 로컬 연결을 함께 실행하지 말고 생산자 하나로 유지한다. 실제 보호 볼륨과 작업점 `_isSheltered`의 일치도 I가 확인한다.

## 4. I 연결 순서와 부족 자산

1. 해당 맵에 각 설정 프리팹을 한 개씩 배치하고 기존 주민을 `_actor`에 연결한다. **프리팹 루트는 월드에 고정하고 움직이는 NPC의 자식으로 두지 않는다.** Actor ID/GUID·기존 몸·숨김 정책을 보존한다.
2. 자식 세 작업점의 초기 좌표 `(0,0,0)/(2,0,0)/(0,0,2)`는 독립 설정의 간격 예시다. A의 현재 배치와 B/I 바닥에 맞춰 실제 위치/회전/경유점을 지정한다. 본편 좌표로 검증된 값이 아니다. R03 입력 전면, R09 수레와 주민의 실제 동선, 필수 통로를 비운다.
3. 휴식점의 `_isSheltered=true`는 보호 자리로 배치해야 한다는 설정이다. 지붕이나 의자가 자동 생성되었다는 뜻이 아니다. 밤에도 대화·퀘스트 마커가 닿는 가까운 자리로 두고 두 피해 주민의 그늘 슬롯을 합치지 않는다. 접근 불가능한 문/비활성 구역으로 보내지 않는다.
4. 기존 `NpcLifeInteractable/Presentation`의 참조와 완료 플래그를 유지한다. R04/R09 실제 이동·짐 완료는 기존 일회성 입력이 생산한다. 일반 일과와 물자 완료를 연결하지 않는다. 장기 서사 전환에는 `SetPlacementReady(false)` 후 기존 Presentation 복원, 안전 배치 확정 뒤 true 순서를 사용한다.
5. D 생산자와 기존 시간 서비스가 준비되면 `_environment`를 연결한다. 실제 지붕·바닥·경유점·소품을 검증한 뒤 `_isPlacementReady=true`로 열어 준다. 프리팹의 기본 false를 통합 완료로 오인하지 않는다.
6. B의 출구에서 진행 중 플레이어 인계/서사 보행은 완료 또는 기존 `StopInteract`로 명시 취소한 뒤 전환한다. 강제 씬 이탈은 OnDisable 정리로 대응하지만, 출구 UX/입력 잠금 검증은 B/I의 책임과 함께 남는다.

| 주민 | 현재 연결 | 확보/검증이 필요한 클립·소품 |
|---|---|---|
| 조이 | 기본 Idle/Walk, 작업 모션 null | 조리 시작/반복/종료, 그릇 집기·내려놓기, 손잡이/그릇 접점. 빵·그릇·작업대는 A 후보이며 실제 연결 안 함 |
| 헤이즐 | `Humanoid_PackMedicine.asset`만 첫 작업에 연결 | 리그 재생·책상 높이·루프 검증, 처치 손동작, 약상자 운반 자세/소품. 연결된 MotionSet은 이벤트 없는 기존 약 포장 후보이며 시각 검증 완료 아님 |
| 모건 | 기본 Idle/Walk, 작업 모션 null | 짐 운반·끈 당김·수레 점검 전환, 손 소품과 수레 접점. 현재 R09 적재점까지 동선 확인 |
| 연목 | 기본 Idle/Walk, 작업 모션 null | A의 Blacksmith Start/Play/End 클립을 실제 Avatar·작업면에서 확인한 뒤 MotionSet 저작. 망치/공구 피벗과 스케일 |
| 잔류 주민 | 기본 Idle/Walk, 작업 모션 null | 씨앗/도구 돌봄, 통 운반. 반복 일과로 식수나 작물 회복 표현을 생성하지 않음 |
| 짐 주민 | 기본 Idle/Walk, 작업 모션 null | 짐 정리 시작/종료와 손 접점. 기존 before/ready 짐은 서사 소유 유지 |

휴식은 서 있는 기본 Idle이다. A의 `Npc_Chair_Sit_Idle`과 StandUp 후보는 의자 높이·전환이 미검증이어서 연결하지 않았다. 조리/운반/수선/착석을 확보된 것으로 보고하지 않는다. E에는 기존 후보와 위 접점 요구를 전달하되 실제 부족이 확정되기 전 새 모델 제작을 요구하지 않는다.

## 5. 수행 검사와 사용자 재현 절차

사용자 요청으로 추가 재실행한 `NpcLifeActionTests`는 **11/11 통과**, 실패·제외 0, 실행 1.393초다(job `50b264b50d2747bd840ecddb9cc1d369`, 상세 결과 `output/LivingWorldC/rerun_validation.json`). 이번 실행은 정규 러너 호출만 사용했으며 초기화 코루틴 수동 시작이나 코드·설정 변경 없이 완료됐다. 이전 러너 오류는 재현되지 않았으나 원인이 영구 해소되었다는 판정은 아니다. 종료 후 원래 LakeOfLifeScenario 편집 씬으로 자동 복귀했고 dirty=false, 컴파일 실패 없음, 씬 SHA-256은 착수값과 동일했다. 아래 본편·실물 입력·모션 품질 검증 대기는 유지한다.

| 검사 | 결과·한계 |
|---|---|
| Unity 컴파일 | Unity 6000.3.21f1, C 타입 로드 성공·컴파일 오류 0 확인. CLI 빌드로 대체하지 않음 |
| EditMode 계약 검사 | `UPlayGround.Core.Tests.NpcRoutineContractTests` **14/14 통과**, 실패/제외 0. 예약 경쟁·중복 ID·실행 세대·취소·소켓 누락·환경 생산자/revision·시간 경계·로드 정리. 후속 최종 job `0027c07d8a3946afb9a1367f3c4bbe4c` |
| Editor Play Mode 회귀 | 최종 `NpcLifeActionTests` **11/11 통과**, 실패/제외 0, 실행 1.393초, job `b5c9b7e1760c4da9b64b5103c45444d8`. 실제 Unity 수명주기의 점유/비활성화/소품 회수, 인계 취소·복원, 늦은 재개 차단 포함. Routine/Idle/Talk 낙하 검사에는 속도를 주입함. 아래 러너 초기화 보조 조치를 사용했으며 실제 지형 보행 검사로 확대하지 않음 |
| 후속 독립 API 회귀 | **2묶음 통과**. 취소한 인계의 늦은 재개가 새 실행을 변경하지 않음, 공중에서 수평속도 제거·두 스텝의 수직속도 보존. 비활성 PreviewScene에서 상태/속도를 주입한 검사이며 실제 KCC 낙하/Play Mode 검증은 아님 |
| 독립 상태 API | **5묶음 통과**. 위치·접지·대화 상태를 주입해 작업→이동→정체→휴식, 중단/재예약·Presentation 홀드, 도달 제한시간, 피신/충돌 정리, 플래그 쓰기 0 확인. 실제 KCC 보행/화면 검증 아님 |
| D 독립 연결 | **6묶음 통과**. D 실제 v1 시간표의 비/비 종료/건조 강풍, 미준비, 생산자 교체·낮은 revision, 늦은 통지 무시. PreviewScene에서 입력·소유권을 준비한 API 검사이며 본편 서비스 초기화/실제 피신 보행은 미검증 |
| 프리팹/참조 | AssetDatabase 로드: **6개, 작업점 18개, 중복 ID 0, Missing Script 0**, 작업 MotionSet 연결 1개, 몸 복제 0. 실제 리그·모션 품질과 구별 |
| 본편 Play Mode / 실물 게임패드 | **사용자 테스트 대기**. 테스트 러너의 독립 Play Mode 실행과 실제 본편 플레이를 구분 |
| Windows Player | **미수행**. C는 asmdef/타입 이동/주소/씬 등록을 변경하지 않음. I/V 최종 두 맵 통합에서는 Player 빌드와 실행 필요 |
| 성능 | **미측정**. 6명 CPU/GC·장시간 임대/예약/소품 누적·모션 비용은 I/V 비교 필요 |

초기 EditMode 2건은 일반 MonoBehaviour의 OnEnable/OnDisable이 EditMode에서 자동 호출된다고 가정한 fixture 실패였다. 명시 수명주기 호출로 고치고 실제 Unity 호출은 Play Mode 검사로 별도 확인했다. D 보조 검사 첫 컴파일의 internal 타입 직접 접근도 reflection 검사로 고쳤으며 런타임 공개 API를 넓히지 않았다. 실패를 최종 통과 결과와 구별한다.

이전 Play Mode 미시작 job `5a6cf0309bfa4d74a49326f837284cb7`, `85d378f6bbd34da381746389960f4d62`는 초기화 시간 초과였고, `04150b1e679f43e4ba70ac701f49125c`는 러너 오류로 0개 시작했다. 이번 보완 직전 job `429c6ad1cf9848c0afe14cfbbc439830`은 별도 조치 없이 11개 통과했다. Idle/Talk 수정 후 최종 job에서는 검사 씬이 재생 중이고 2,376프레임이 지났는데도 러너의 `ActiveController=null`, `m_Runner=null`, `RunInfrastructureHasRegistered=false`였다. 설치된 Test Framework의 `PlaymodeTestsController.Start`가 실행되지 않은 상태를 확인해 **해당 검사 씬과 미시작 상태를 확인한 뒤 Start 코루틴만 1회 시작**했다. 이후 정규 Test Runner가 11개를 실행·통과했다. 패키지 소스·테스트 필터·판정·프로젝트 설정은 변경하지 않았으며 테스트 오류를 무시하지 않았다. 이전 실패 전체의 원인이 같다고 단정하지 않는다. 상태 주입 fixture는 실제 손 모션·리깅·KCC 지형 보행과 별개다.

검사 러너 초기화 중 `The referenced script (Unknown) ... is missing` 경고가 다시 발생했다. 이번에는 실행 전 본편 씬의 모든 로드된 GameObject를 순회해 Missing Script **0**을 확인했으며 C 전용 프리팹도 0이었다. 검사 씬의 러너 컴포넌트 타입 자체도 로드되어 있었다. 따라서 경고를 본편 주민의 누락으로 단정하지 않는다. 경고의 직렬화 원인은 여전히 미확정이고, 기존 툴 메뉴 정책 경고도 C 소유권 밖이므로 수정하지 않았다.

검사 코드/로그: `output/LivingWorldC/`의 `create_prefabs.cs.txt`, `prefab_checks.cs.txt`, `api_checks.cs.txt`, `environment_checks.cs.txt`, `validation.json`, `followup_checks.cs.txt`, `followup_validation.json`, `final_validation.json`. 앞선 JSON의 미검증 상태는 당시 기록이며 최종 결과는 `final_validation.json`을 따른다. 보조 파일은 git ignored이며 재현 코드와 작은 텍스트만 보관한다. 프로젝트 사본/Player 빌드는 만들지 않았다.

### 준비와 결과 회신

I가 연결한 버전/커밋 또는 파일 해시, Unity 버전, 입력 장치, 시간·날씨 프로필을 기록한다. 본편 저장은 **빈 별도 슬롯에 복사**하고 원본을 보존한다. R03/R04 선행 조건과 R09 진행은 정상 플레이 저장을 사용한다. 시간/환경을 주입한 실험은 주입 검사라고 따로 표시한다. 아래 항목은 아직 모두 **사용자 테스트 대기**다.

| ID | 재현 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| LW-T03-a | R04 `resident_stays`를 정상 획득한 저장으로 식수 입력. 소품 내려놓기 도중 Cancel→재시도. 별도 사본에서 진행 중 저장/로드→재시도. 완료 후 다시 입력 | 미완료는 재시도 가능. 실제 인계 후에만 `water_handed_over`와 받은 물통 표시. 완료 중복/물자 복제/진행 소진 없음 |
| LW-T03-b | 출발 준비 주민의 기존 서사 보행 중 Cancel→저장/로드. 다음에는 끝까지 도착시킨 뒤 여러 일과 주기·밤·날씨 변경 관찰 | 완료 전 `departure_prepared` 없음. 완료 후 짐이 이전 자리로 돌아가지 않음. 일과가 출발/정착 사실을 쓰지 않음 |
| LW-T03-c | 일과 작업 중 대화 시작 또는 컴포넌트 비활성/씬 이탈. 손 소품을 시험 연결한 별도 배치에서도 반복 | 예약 즉시 해제, 손 소품 즉시 숨김 후 파괴, 중복 해제 오류 없음. 재입장에 기존 몸만 존재하고 서사 위치 복원 뒤 재예약 |
| LW-T05-a | 08:59→09:00, 17:59→18:00, 21:59→22:00, 05:59→06:00 경계에서 여섯 주민 관찰. 이동 중 대화→종료 | 인물마다 시간차, 대화 중 일과 이동/손 소품 없음. 종료 후 현재 조건으로 재예약, 화면 앞 순간이동 없음 |
| LW-T05-b | 별도 배치에서 두 일과의 첫 작업점을 같은 컴포넌트로 연결. 선점자가 일하는 중 다른 주민 접근, 이어 선점자 대화/비활성화 | 한 명만 점유. 다른 주민은 가까운 휴식/현재 위치 대기. 선점 해제 뒤 재예약 가능, 플레이어 밀기 없음 |
| LW-T05-c | 경유점 사이를 플레이어/상자로 막고, 별도 배치에서는 도착점을 높게 놓아 도달 불가로 만든다 | 정면 충돌 즉시 정리, 비충돌 정체는 기본 2초·전체 이동은 20초 내 정리. 소품을 들고 무한 보행하거나 벽을 뚫지 않음. 휴식점도 막히면 현재 자리에서 대화 가능 |
| LW-T05-d | 밤과 호수 비(10:00), 피해 건조 강풍(14:00)에 필수 NPC에게 접근. 피신 중 대화, 날씨 종료/생산자 교체, 같은 시각 저장/재입장 | 가까운 보호 자리/현재 자리에서 퀘스트 대화 가능. NPC 삭제·진행 잠금 없음. 비가 물길/식수 완료를 만들지 않음. 보호 자리와 D 볼륨이 일치 |
| LW-T05-e | 일과 중 Presentation 완료 사실이 바뀌는 정상 사건 수행→화면 밖 배치 복원→로드 | 서사 홀드 중 일과 이동/점유 없음. 복원 후 기존 HomePose/WorkMotion과 새 서사 상태가 우선. 완료 물자 before/after가 중복되지 않음 |
| LW-T05-f | 별도 검증 배치에서 낮은 단차의 일과 보행으로 접지를 잃게 한다. 정체 취소로 Idle 전환, 공중 대화로 Talk 전환을 각각 관찰 | 세 상태 모두 공중에서 수평 추진을 멈추고 정상 낙하·재접지. 대화·취소 시 낙하가 느려지지 않음. 공중 도착을 작업 성공으로 처리하지 않음. 접지 후 기존 서사 보행도 정상 재개 |
| LW-T08-a | 실물 게임패드로 일과 중 주민에게 접근→상호작용→Cancel→다시 대화. 이동/작업/휴식/피신 각각 반복 | 기존 상호작용/Cancel 입력으로 탈출, 입력 잠금 잔류 없음, NPC 이동 때문에 대화가 실패하지 않음 |
| LW-T08-b | 인계/서사 보행 중 출구 입력, 취소 후 호수↔피해 왕복 및 지도 귀환 | B/I의 완료 또는 명시 취소 정책 준수. 미완료 사실을 완료로 바꾸지 않고 소품·예약·입력 잠금 정리. 지도/포커스 검증은 B/I 통합 후 판정 |

실패 회신: `항목 ID / 통과·실패·미수행 / 실제 결과 / 시작 저장·시간·날씨·입력 장치 / 로그 또는 영상`. 실패 시 NPC/작업점 ID, 대화·서사 홀드 상태, 발생 시점을 함께 남긴다.

## 6. 인계 요청과 변경 보존

- I: 기존 몸과 서사 게이트, actual 작업점/경유점/지붕, 시간 서비스, D 생산자 연결. 공용 Data/환경 서비스 게시 전까지 현재 C/D 로컬 연결만 사용한다. 본편 주민 6명의 최종 배치는 수행하지 않았다.
- B/I: 새 마을 잔류·짐 주민과 원본 몸의 중복 방지, 기존 SceneEntityId 보존, 통로 교행·출구 입력 취소와 실제 저장 재진입 검사.
- D/I: `NpcRoutineEnvironmentBinding`이 공개 이벤트를 소비함을 확인하고 환경 서비스 도입 시 이 연결을 교체할 것. 문턱/보호 볼륨과 휴식점 일치, 강풍/비 임계값의 현장 조정.
- E/I: 4절의 실제 클립·손/작업면 접점·착석 전환을 확보/검증한 뒤 연결. 현재 null 작업 슬롯을 완성 연출로 표시하지 않는다.

다른 실행이 같은 C 파일을 수정해 중단했던 시점 이후 사용자에게 종료 확인을 받고 재개했다. 현재 호출 경로는 `NpcMovementController.Routine → TryStart` 하나이며 중복 TryEnter/Interrupt 경로가 없다. B/D 변경은 보존했다. 본편 씬 SHA-256은 착수값 `23F089199D37FB8FCD136574D0BAEE2A2A58CAD8660D8F0B222E4799B2D6FFD5`와 동일함을 확인했다. 자동 검사 후의 재직렬화 변경도 C 전용 프리팹 외 기존 데이터에 추가하지 않았다. 커밋하지 않았다.
