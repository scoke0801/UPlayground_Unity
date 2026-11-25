# UPlayground 코드 관계 그래프

2026-11-22 · 기준 커밋 `cc425cb0a59b`와 당시 작업 트리에 저장된 C# 변경을 포함한 스냅샷.

- [그래프 열기](graph.html) · 클래스 검색, 시스템별 진입점, 방향·관계·근거 필터, 소스 경로와 행 번호.
- [클래스 그래프 JSON](graph.json) · Graphify 조회 형식. 관계별 발생 건수와 대표 근거 최대 3개, SCC 구성원과 asmdef 목록.
- [Graphify 원본 심벌 인덱스](graph-symbols-ast.json.gz) · 압축 JSON. 함수·파일·타입 수준 AST와 추론 후보를 포함한다.
- [개발 위키](../../wiki/development.md)

## 전체 구조

`Assets/Scripts`에 해당하는 실제 경로 `Assets/02.Scripts`에서 **C# 2,051개**를 먼저 로컬 AST로 인덱싱했다. 상위 폴더는 GameActor 546개, Data 338개, Editor 224개, UI 189개, Tool 183개다. Library, Temp, obj, bin, 빌드 산출물, 패키지 소스, 씬·프리팹·에셋·Markdown은 인덱싱 범위 밖이다. `Editor/P09Builder/Build`처럼 이름에 Build가 들어가도 실제 도구 C#인 파일은 포함했다. 파일에 저장된 생성 C#도 포함했다.

전투의 입력과 AI 판단은 액터와 상태를 통해 실행 경로에 들어온다. 플레이어 쪽은 입력 등록과 상태 전이에서, 적 쪽은 Behavior Tree와 액션 선택에서 시작한다. 전투는 AbilitySet/GAS에서 실행 데이터를 고르고 MotionKey로 모션을 조회한다. 모션 이벤트에는 충돌 창을 `CombatActionRunner`로 보내는 경로가 있다. 타격 결과는 전투 피드백에서 카메라와 입력 진동으로 전달된다. 카메라가 입력·리소스에 접근하는 방향은 자기 모듈의 `ICameraRuntimeAdapter`를 거쳐 호스트로 향한다.

| 시스템 | 주요 클래스와 직접 확인한 관계 |
|---|---|
| 전투 | `PlayerCombat`, `EnemyCombat`, `ActorAbilitySystem`, `AbilitySystemComponent`, `CombatActionRunner`, `CombatResolutionPipeline`, `CombatFeedbackDispatcher`. `PlayerCombat → ActorAnimator.TryResolveAbilityMotion`, `EnemyCombat → CombatActionRunner.SetCollisionExecutor`, `MonsterActor → CombatResolutionPipeline.Execute`를 확인했다. `PlayerCombat`은 `PlayerActorComponent`를 상속하고 양쪽 전투 클래스는 `ICombatCollisionExecutor`를 구현한다. [PlayerCombat.Attack.cs](../../../Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.Attack.cs#L114) · [EnemyCombat.cs](../../../Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs#L430) · [MonsterActor.cs](../../../Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs#L189) |
| 카메라 | `CameraManager`, `CombatCameraEventRouter`, `CameraLockOn`, `CameraRuntimeServices`, `ICameraRuntimeAdapter`, `UPlayGroundCameraRuntimeAdapter`. `CameraManager`는 라우터를 만들고 포트의 `LoadAssetAsync`를 호출한다. 호스트 어댑터는 `CameraRuntimeAdapterBase`를 상속하고 `IInputService.GetAction`을 호출한다. [CameraManager.cs](../../../Assets/02.Scripts/Camera/CameraManager.cs#L138) · [UPlayGroundCameraRuntimeAdapter.cs](../../../Assets/02.Scripts/Manager/Camera/UPlayGroundCameraRuntimeAdapter.cs#L68) |
| 애니메이션 | `ActorAnimator`, `PlayerActorAnimator`, `MotionSet`, `MotionSetAsset`, `MotionEventBase`, `BeginCollisionEvent`, `MotionSetPlayer`. `GameActor.Awake → ActorAnimator.Init`, `PlayerActorAnimator : ActorAnimator`, `BeginCollisionEvent : MotionEventBase`를 확인했다. 충돌 이벤트는 `CombatActionRunner.HandleCollisionEvent`를 호출한다. `MotionSetPlayer`는 `IMotionEventTargetProvider`와 `IMotionTimeSource`를 구현한다. [GameActor.cs](../../../Assets/02.Scripts/GameActor/Base/GameActor.cs#L243) · [PlayerActorAnimator.cs](../../../Assets/02.Scripts/GameActor/Animation/PlayerActorAnimator.cs#L11) · [MotionEvent_Collision.cs](../../../Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_Collision.cs#L142) · [MotionSetPlayer.cs](../../../Assets/02.Scripts/MotionSet/Animancer/MotionSetPlayer.cs#L14) |
| 입력 | `InputManager`, `IInputService`, `InputBuffer`, `PlayerActor`. `InputManager`는 `IInputService`를 구현하고 조합 입력을 처리하면서 버퍼를 소비한다. `PlayerActor`는 `IInputService.RegisterInputEvent`에 공격 콜백을 등록한다. 관리자가 콜백을 호출하는 코드와 연결은 확인했으며, 어떤 입력에서 그 콜백이 실제 발화하는지는 설정과 실행 상태에 달려 있다. [InputManager.cs](../../../Assets/02.Scripts/Manager/Input/InputManager.cs#L17) · [InputManager.Chord.cs](../../../Assets/02.Scripts/Manager/Input/InputManager.Chord.cs#L145) · [PlayerActor.Input.cs](../../../Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Input.cs#L56) · [InputManager.Event.cs](../../../Assets/02.Scripts/Manager/Input/InputManager.Event.cs#L492) |
| AI | `MonsterActor`, `BehaviorTreeRunner`, `BehaviorTreeAsset`, `BTNode`, `BTActionNode`, `EnemyActionResolver`, `EnemyCombatDecisionEvaluator`, `NpcBrain`, `PetBrain`. 몬스터는 Runner의 실행을 제어한다. Runner는 에셋을 복제하고 루트의 `Tick()`을 호출한다. `BTActionNode`는 `BTNode`를 상속한다. 전투 판단 평가기는 `EnemyCombat.HasAvailableSkillAtDistance`를 직접 호출한다. [MonsterActor.cs](../../../Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs#L1330) · [BehaviorTreeRunner.cs](../../../Assets/02.Scripts/GameActor/AI/BehaviorTree/Runtime/BehaviorTreeRunner.cs#L169) · [BTActionNode.cs](../../../Assets/02.Scripts/GameActor/AI/BehaviorTree/Runtime/BTActionNode.cs#L3) · [EnemyCombatDecisionEvaluator.cs](../../../Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombatDecisionEvaluator.cs#L83) |

**시스템을 가로지르는 직접 호출.** `CombatFeedbackDispatcher → CombatCameraEventRouter`가 피격 카메라를, `CombatFeedbackDispatcher → IInputService`가 타격 진동을 호출한다. `PlayerActorAnimator.OnAnimationEvent_HitCheck → PlayerCombat.PerformHitDetection`은 C# 메서드 본문에서 확인했다. 해당 콜백을 호출하는 Animation Clip 바인딩은 미검증이다. [CombatFeedbackDispatcher.cs](../../../Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs#L139) · [피격 카메라 호출](../../../Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs#L196) · [PlayerActorAnimator.cs](../../../Assets/02.Scripts/GameActor/Animation/PlayerActorAnimator.cs#L130)

## 순환 의존성

강연결 요소(SCC)는 방향을 따라 서로 도달할 수 있는 타입 집합이다. 아래 SCC 및 양방향 쌍 집계는 소스 확인 간선만 사용한다. 호출·생성 전용 집계도 클래스 단위이므로 메서드 재귀나 프레임 안의 호출 순서를 입증하지 않는다. 런타임 후보 집계는 Editor·EditorTools·Tool 경로를 경로 규칙으로 거르고 `Tool/Validation`을 포함했다.

| 범위 | 타입 또는 모듈 | 방향 쌍 | 2개 이상 SCC | 최대 SCC | 양방향 쌍 |
|---|---:|---:|---:|---:|---:|
| 전체 C# 타입 관계 | 3,784 | 18,691 | 83 | 241 | 289 |
| 런타임 후보의 타입 관계 | 2,598 | 12,119 | 61 | 241 | 251 |
| 런타임 후보의 호출·생성 관계에 등장한 타입 | 1,841 | 4,408 | 35 | 121 | 109 |
| 범위 내 asmdef 선언 참조 | 25 | 73 | 0 | — | 0 |

- **`MonsterActor ↔ EnemyCombat`**. 액터는 `EnemyCombat.Init`을 호출한다. `EnemyCombat.ShouldShowDangerRing`은 `MonsterActor.IsHostileToActivePlayer`를 호출한다. [MonsterActor.cs](../../../Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs#L133) · [EnemyCombat.cs](../../../Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs#L1495)
- **`CameraManager ↔ CombatCameraEventRouter`**. 매니저가 자기 자신을 넘겨 라우터를 생성한다. 라우터는 킬캠 대상 조회에서 매니저의 `GetTarget()`을 호출한다. [CameraManager.cs](../../../Assets/02.Scripts/Camera/CameraManager.cs#L151) · [CombatCameraEventRouter.cs](../../../Assets/02.Scripts/Camera/Combat/CombatCameraEventRouter.cs#L90)
- **`PlayerActor ↔ PlayerCombat`**. 액터는 `PreviewGuardOutcome`을 호출한다. 전투 컴포넌트는 `PlayerActor`를 조회하고 액터의 속성을 사용한다. 이 쌍은 전체 타입 관계에 포함된다. [PlayerActor.Combat.cs](../../../Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Combat.cs#L185) · [PlayerCombat.cs](../../../Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs#L556) · [PlayerCombat.Attack.cs](../../../Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.Attack.cs#L444)
- **asmdef의 경계**. `Actor → Camera/Contracts/Ability/MotionSet.Core`, `Camera → Data/Contracts/Core`, `MotionSet.Animancer → MotionSet.Core`가 선언돼 있다. 0건은 범위 내 asmdef의 명시 참조에 한정된다. 패키지 및 Unity 기본 어셈블리의 암묵 참조는 집계에서 제외했다. [Actor](../../../Assets/02.Scripts/GameActor/UPlayGround.Actor.asmdef) · [Camera](../../../Assets/02.Scripts/Camera/UPlayGround.Camera.asmdef) · [MotionSet.Animancer](../../../Assets/02.Scripts/MotionSet/Animancer/UPlayGround.MotionSet.Animancer.asmdef)

## 연결이 집중된 타입

런타임 후보 그래프에서 들어오는 고유 타입 수와 나가는 고유 타입 수를 더했다. 한 쌍에 여러 메서드나 참조가 있어도 방향별 1건이다.

| 타입 | 들어옴 | 나감 | 합계 | 해석 |
|---|---:|---:|---:|---|
| `PlayerActor` | 133 | 157 | 290 | 소비자와 의존 대상 모두 많다. 전투·입력·장비·애니메이션 변경의 영향 범위가 넓다. |
| `GameActor` | 233 | 35 | 268 | 기반 타입과 공통 계약 역할의 영향이 크다. |
| `MonsterActor` | 87 | 117 | 204 | AI·전투·모션 초기화와 피해 처리가 모인다. |
| `Svc` | 163 | 32 | 195 | 서비스 접근 지점이다. 변경 영향은 각 서비스 계약과 함께 봐야 한다. |
| `ActorMovementController` | 150 | 24 | 174 | 상태와 액터 컴포넌트가 공유하는 이동 진입점이다. |
| `PlayerCombat` | 43 | 119 | 162 | 공격·콤보·피니시·타격·피드백 경로의 소비 대상이 많다. |
| `ActorAnimator` | 103 | 29 | 132 | 상태·모션·이벤트 진입점으로 널리 쓰인다. |
| `EnemyCombat` | 40 | 81 | 121 | AI 선택과 모션·판정 사이에 있다. |
| `CameraManager` | 43 | 60 | 103 | 카메라 공개 API와 내부 조립이 한 타입에 있다. |
| `InputManager` | 4 | 44 | 48 | 소비자가 `IInputService`를 거치므로 구현체 연결도와 함께 계약의 연결도를 봐야 한다. |

## 개선 후보

1. **전투와 액터의 역참조부터 검토.** `MonsterActor`와 `EnemyCombat`의 양방향 참조에는 초기화와 적대 여부 조회처럼 수명이 다른 책임이 섞여 있다. 텔레그래프 계산에 필요한 적대 여부를 기존 전투 관계 계약이나 전달 값으로 공급할 수 있는지 우선 검토한다. 플레이어 쪽은 `PlayerCombat`의 구체 액터 조회를 용도별로 세어 본 뒤, 현재 초기화 순서와 가드·차지·콤보 동작을 보존할 수 있는 경계부터 선택한다.
2. **카메라 라우터에 필요한 기능을 좁히기.** `CombatCameraEventRouter`가 킬캠·락온·흔들림을 위해 매니저에 요청하는 기능을 목록으로 만든다. 기존 카메라용 접근 계약으로 표현 가능한 부분부터 옮길 수 있는지 검토한다. Camera 모듈과 호스트를 잇는 기존 `ICameraRuntimeAdapter` 경계를 활용한다.
3. **핵심 경로의 동적 연결을 먼저 검증.** 공격 입력 → 상태 → GAS → MotionKey → 충돌 → 카메라·진동 흐름과 몬스터 BT → 액션 선택 → Ability 실행 흐름을 우선한다. 실제 에셋과 콜백 연결을 확인해 두면 결합도를 줄이는 변경을 검증할 기준이 생긴다.

## 근거 등급과 분석 범위

- **소스 확인 / EXTRACTED.** 파일·행에서 Roslyn이 바인딩한 타입, 멤버, 메서드, 상속과 인터페이스다. 대상이 인터페이스나 가상 메서드면 그 정적 계약을 연결한다. 같은 타입 내부 참조는 클래스 그래프에 포함하지 않았다. partial 선언은 타입 이름으로 통합했고 원본 선언 위치를 JSON에 기록했다. 총 34,851개 관계 행에 소스 확인과 추론이 들어 있다. 양 끝과 관계 종류를 함께 키로 써서 동일 쌍의 호출·상속·멤버 참조를 따로 유지했다.
- **추론 / INFERRED.** Graphify AST가 추정한 호출·디스패치와 소스 검토에서 도출한 구현 후보다. 예를 들어 `CameraManager → ICameraRuntimeAdapter` 호출과 `GameManager → Configure(new UPlayGroundCameraRuntimeAdapter())`는 직접 확인했다. 관찰된 런타임 호출의 수신자가 `UPlayGroundCameraRuntimeAdapter`라는 경로는 추론으로 표시했다. 570개 방향 쌍에 추론이 있고, 그중 530개는 소스 확인 간선이 없다. [GameManager.cs](../../../Assets/02.Scripts/Manager/GameManager.cs#L97)
- **심벌 해석 한계.** Roslyn은 선택한 Windows Editor 전처리 분기와 설치된 엔진 API를 사용해 지정 C#을 한 합성 컴파일로 해석했다. Unity 어셈블리별 컴파일, 활성 프로젝트 define 전체, 패키지 생성 코드와 외부 플러그인 내부 실행은 재현하지 않았다. 호출·생성 구문 119,431개 중 5,289개에서 대상 심벌을 확정하지 못했다. 선언 타입에 귀속할 수 없는 튜플 등 402개 소스 후보는 클래스 그래프에서 제외했다.
- **Graphify 원본 인덱스의 주의점.** 심벌 노드 34,660개와 간선 115,807개를 얻었다. 노드 1,807개에는 소스 파일 위치가 없다. 원본 간선 6,158개는 한쪽 끝의 ID를 찾지 못했다. 진단상 단일 방향쌍으로 접으면 유효 후보 간선 6,566개가 합쳐질 수 있어 원본 간선을 그대로 보관했다. 원본의 `EXTRACTED`는 AST 문법 근거이며 Roslyn 바인딩을 뜻하지 않는다. Graphify가 심벌이 없다고 경고한 파일은 `MotionTags.cs`, `AbilityProductionAssemblyInfo.cs`, `BehaviorTreeAssemblyInfo.cs`다. 클래스 그래프는 Roslyn 소스 확인과 구분된 추론을 사용한다.
- **군집.** Graphify는 소스 확인 클래스 그래프에서 137개 군집을 만들었다. 상위 군집의 크기와 응집도는 `WorldBuilder · WorldDefinition` 185개 / 0.05952, `GameActor · PlayerActor` 121개 / 0.05427, `Editor · ScenarioResidentAuthoring` 115개 / 0.05873이다. 군집명은 대표 폴더와 연결도 상위 타입을 조합한 탐색용 라벨이다. HTML의 다섯 시스템 필터는 경로 기반으로 분류하며 중첩을 허용한다.

## 정적 그래프에서 빠질 수 있는 Unity 연결

| 연결 | 이 분석에서 확인한 범위 | 누락 가능성 및 확인 경로 |
|---|---|---|
| 인스펙터 필드, 프리팹, 씬, `UnityEvent` | C# 필드 타입, 이벤트 접근, 일부 콜백 등록 코드 | 실제 오브젝트, 프리팹 변형·씬 오버라이드, Persistent Listener. GUID/fileID 및 Unity 로드 결과로 확인한다. |
| Unity Animation Event 및 MotionSet 이벤트 | 콜백 본문에서 호출하는 메서드, C# 이벤트 타입의 상속 및 위임 | 클립의 함수명·시간, 대상 Animator, MotionSet의 `SerializeReference` 이벤트 인스턴스와 창 구간. 실제 클립과 MotionSet 타임라인에서 확인한다. [Motion.cs](../../../Assets/02.Scripts/MotionSet/Core/Data/Motion.cs#L288) |
| ScriptableObject·BT | AbilitySetSO·MotionSetAsset·BehaviorTreeAsset의 타입과 코드 참조 | 프리팹에 배정된 AbilitySet, variant의 MotionKey·HitPhase와 실재 모션, 트리의 루트와 노드·Blackboard·조건 데이터. 관련 에셋과 기존 GAS/BT 검증 경로에서 확인한다. [AbilitySetSO.cs](../../../Assets/02.Scripts/Data/Ability/AbilitySetSO.cs#L14) · [CombatData.cs](../../../Assets/02.Scripts/Data/Combat/CombatData.cs#L329) |
| Addressables 및 Resources | 로더 호출과 일부 키 사용 코드 | 빌드 프로필, 그룹·라벨·카탈로그·오버라이드가 돌려주는 에셋과 하위 의존성. 설정과 실제 로드 결과로 확인한다. [AssetManager.cs](../../../Assets/02.Scripts/Manager/AssetManager.cs#L206) |
| 입력 바인딩·서비스·Unity 수명주기 | 입력 이벤트 등록, 인터페이스 계약, 확인 가능한 코드상 조립 지점 | `.inputactions` 매핑, 리바인딩·장치·레이어, 런타임 등록/구독 순서, `Awake`·`Update` 자동 진입, 반사·문자열 호출. 실제 실행과 레지스트리 상태로 확인한다. |

## 검증 및 재조회

- Graphify `0.9.115`의 C# Tree-sitter AST를 외부 모델 없이 실행했다. Graphify 모델 토큰 사용량은 입력 0 / 출력 0이다. Roslyn 2,051개 트리에서 선택한 분기의 파싱 오류는 0개였다.
- 자동 검사에서 클래스 그래프 34,851개 관계의 끝점, 선언 및 대표 근거 위치, SCC 수, 문서 링크, HTML 내장 데이터 건수를 확인했다. Edge 헤드리스에서 초기 그래프, 시스템과 클래스 검색, 생성 관계와 추론 필터, 소스 링크 및 근거 펼치기를 확인했다.
- C# 스냅샷 SHA-256은 `ce8723d353e6c63deda50c5de90b3a9eb2df62b9d84822e26bb1f96e8ae0b239`이다. 프로젝트 기준 상대 경로를 순서대로 기록하고 각 파일 바이트의 SHA-256을 결합했다.
- HTML은 데이터가 포함된 단일 파일로, 로컬 브라우저에서 열 수 있다. 기본 화면은 `PlayerActor`와 소스 확인 간선이다. 가운데 그림은 최대 24개 이웃을 표시하고 우측 근거 패널에는 필터에 맞는 이웃이 모두 나온다. 위치 링크는 경로와 행을 제공하며 행 이동 지원은 열어 보는 도구에 따라 다르다.
- 저장소 루트에서 `graphify explain "PlayerCombat" --graph docs/work-status/code-graph/graph.json` 또는 `graphify path "EnemyCombat" "CameraManager" --graph docs/work-status/code-graph/graph.json`으로 다시 조회할 수 있다. 경로 탐색 시 간선의 근거 등급을 확인한다. 이 파일은 시점 스냅샷이며 코드 변경이 자동 반영되지 않는다.
- Unity 컴파일, Editor/Player 실행, 에셋 로드 및 Player/Addressables 빌드는 미수행이다. 이번 검증 범위는 정적 산출물이다.
