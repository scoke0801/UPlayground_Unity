# 몬스터 BT 행동 개선 설계

> 작성일: 2026-11-14
> 상태: **구현 진행 중.** 실제 반영·검증·사용자 테스트 상태는 [작업 상태](../../../../../docs/work-status/monster-bt-improvement/STATUS.md)에서 관리한다. 설계의 모든 단계가 구현된 것으로 보지 않는다.
> 분야 위키: [전투와 실행 데이터](../../../../../docs/wiki/combat.md)
> 관련 설계: [Intent 가중치 외부화](MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md) · [단련된 검사](MONSTER_AI_MELEE_SWORDSMAN_DESIGN_KR.md) · [적 스태미나](ENEMY_STAMINA_MODEL_DESIGN.md) · [보스 다단계 계획](BOSS_HIERARCHICAL_PLAN_DESIGN.md) · [BT 고도화 목표 가이드](../../../guide/MONSTER_BT_ADVANCEMENT_GOAL_GUIDE.md)

## 0. 요약

몬스터 BT의 문제는 "규칙이 모자라다"보다 **"만든 BT가 맞는 몬스터에 연결되어 있지 않다"**가 먼저다. 2026-11-14 에셋 대조 결과는 다음과 같다.

- 테스트 폴더의 BT `EnemyBehavior_Test_AggressiveGroundMelee_Tuned`를 BehaviorData 13개가 참조한다. 이 중 몬스터 프로필로 사용이 확인된 것은 리치·거미 여왕·거미 졸개·드라이어드 4종(프로필 9개)이다. 이들이 공격적 근접형 하나로 같은 방식으로 싸운다.
- 설계 문서까지 있는 `SkeletonSwordsman_Disciplined`는 어떤 BehaviorData에도 연결되어 있지 않다. 해골 검사는 프로필로 확인한 결과 범용 `GroundMelee_Balanced`로 동작한다.
- 파티 캐릭터 몬스터판 BehaviorData 10개는 무기와 상관없이 모두 `GroundMelee_Balanced`를 참조한다(참조 기준, 스폰 경로 미확인).

실행 순서는 [작업 요청서](../../../requests/MONSTER_BT_IMPROVEMENT_TASK_PROMPTS.md)에 따라 **검증기 → 연결 정상화 → 플레이어 읽기 조건 → 전투 구역·회피 제한(P2a) → 원형별 템플릿 → 선택 안정화 → 그룹 역할**로 잡는다. 기존 P0~P5 식별자는 유지한다. P2a는 별도로 구현·검수해 원형 분화와 체감 변화를 구분한다.

## 1. 플레이어 경험 목표

| 목표 | 현재 문제 | 판단 기준 |
|---|---|---|
| 몬스터마다 싸우는 법이 다르다 | 시전형(리치)·무리형(거미)·근접형이 같은 BT로 압박 위주 행동을 한다 | 처음 보는 몬스터 3종을 상대할 때 각자 다른 대응을 요구한다 |
| 몬스터가 플레이어를 "보고" 반응한다 | 플레이어 읽기가 공격·가드·회피·후딜 빈도에 한정된다. 차지·후방·가드 브레이크 직후는 판단에 쓰이지 않는다 | 플레이어가 강공격을 차지하면 일부 몬스터가 끊으러 들어오거나 물러난다 |
| 행동이 읽히되 단조롭지 않다 | 의도 선택에 유지·전환 비용이 없어 같은 상황에서 행동이 흔들리거나 같은 행동만 반복할 수 있다 | 2~3회 교전 안에 패턴을 배울 수 있고, 10회 교전에도 완전히 같은 흐름이 반복되지 않는다 |
| 다수 전투가 공정하다 | 그룹에 속하지 않은 몬스터는 공격 슬롯 제한을 받지 않는다 | 3마리 이상 교전에서 동시에 2마리 넘게 공격하지 않는다(그룹 설정 기준) |

## 2. 현재 구조와 확인된 사실

### 2.1 책임 분담 (변경하지 않는다)

| 층 | 책임 | 위치 |
|---|---|---|
| Rules JSON | 규칙 우선순위, 행동 사이 공백 | `Assets/10.Datas/AI/BehaviorTree/SourceJson/` → `Generated/` |
| 스코어러 | 매 틱 9개 `CombatIntent` 점수와 `SelectedIntent` | `EvaluateEnemyCombatIntentService`, `IntentScoreComputer`, `IW_*` SO |
| 공격 데이터·MotionSet | 선딜·후딜·히트박스 타이밍. **공격이 읽히는지는 이 층이 정한다** | AbilitySet → MotionKey → MotionSet |
| 코드 템포 | 공격 후 다음 행동까지의 지연 | `EnemyAIController.DecidePostAttack` |
| 그룹 | 공격 슬롯, 브리더, 대형 | `MonsterGroupController` |

판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다는 전투 원칙을 그대로 따른다.

### 2.2 BT 연결 현황 (2026-11-14 GUID 대조)

지상 몬스터는 BehaviorData가 BT의 단일 소스다. `EnemyAIController.EnsureBehaviorTreeRunner`가 `restartIfRunning: true`로 프리팹에 직렬화된 BT를 덮어쓴다. 비행 몬스터는 `EnemyFlyingAIController`가 컴포넌트에 직렬화된 `_behaviorTree`를 쓰므로 BehaviorData의 BT를 읽지 않는다.

| BT | 참조하는 BehaviorData | 문제 |
|---|---|---|
| `Test/…AggressiveGroundMelee_Tuned` | spider_queen, spider_minion, lich, dryad, griffin, Skeleton, PlantChild, Monster_Raon·Lian·Hwarin, DefaultEnemy, DefaultStats, traning_dummy (13) | 테스트 BT가 기본값 역할을 한다. 몬스터 프로필 연결 확인: 리치 2, 거미 여왕 3, 거미 졸개 3, 드라이어드 1 |
| `GroundMelee_Balanced` | skeleton_sword, skeleton_common(프로필 사용 확인), Humanoid 캐릭터판 10 (Raon, Hwarin, Reien, Sera, Yura, YeonHoa, MyoRyeong, Myomyo, Lili, Lian — 참조 기준) | 무기 구분 없이 같은 BT |
| `Humanoid_*` 5종 | 랜덤 휴머노이드(검방·대검·쌍검·쌍도끼·활), HumanoidSpear | 정상. 다만 의도 분기 구조가 5종 모두 거의 같다 |
| `SkeletonBow_AdaptiveRanged` | skeleton_bow, SeolA | 정상 |
| `Boss_*` 5종, Golem, Ent, Plant 3종, Wolf, Lucia | 각 전용 BehaviorData | 정상 |
| `SkeletonSwordsman_Disciplined` | 없음 | 설계 문서가 있지만 연결되지 않았다 |
| `RangedKiter`(SourceJson·Json 2종), `Example`, `Organic`, 나머지 Test 3종 | 없음 | 연결되지 않은 BT |
| `Flying_DiveHarasser` | BehaviorData 없음. 그리핀 프리팹 2종의 `EnemyFlyingAIController._behaviorTree`에 직접 연결 | 정상. 그리핀 프리팹에는 `EnemyAIController`가 없고 비행 컨트롤러는 `EnemyAIController`를 상속하지 않으므로, griffin BehaviorData의 BT 칸(Aggressive Test)은 읽히지 않는다 |
| 확인 불가 GUID `547eec6e…` | BehaviorData_humanoid | `Assets/10.Datas`의 BT `.meta`에서 찾지 못했다. 깨진 참조인지 다른 위치의 에셋인지 **미확인** |

griffin, Skeleton, PlantChild, Monster_*, Default*, traning_dummy BehaviorData는 몬스터 프로필에서 참조를 찾지 못했다(griffin은 프로필이 참조하지만 위 이유로 BT 칸이 쓰이지 않는다). 실제 스폰 경로(ActorDefinition·배치 데이터)에서 쓰이는지는 **미확인**이다.

### 2.3 검증기의 빈틈 (코드 확인)

| 빈틈 | 근거 | 결과 |
|---|---|---|
| `SelectedIntent` 값이 비어 있는지만 본다 | `MonsterBehaviorTreeJsonImporter.Validation.cs`의 `SelectedIntent` 검사 | `Evade`, 오타 같은 값이 통과하고 런타임에 그 분기는 영원히 실행되지 않는다 |
| `ExecuteAttack`의 `cooldownId`를 버린다 | `NodeFactory`의 `ExecuteAttack` 정의는 `attackCategory`·`abilityRole`만 넘긴다 | JSON 작성자는 쿨다운을 걸었다고 믿지만 반복 제한이 없다 |
| `CooldownReady`가 등록되지 않는 ID를 기다릴 수 있다 | 쿨다운은 `RequestAction`·`Transition`에서만 등록된다 | 같은 트리에 등록 지점이 없으면 조건이 항상 같은 값을 반환한다 |
| BehaviorData가 어떤 BT를 가리키는지 검사하지 않는다 | — | 테스트 BT 연결, 비행 몬스터의 지상 BT 연결을 놓친다 |

### 2.4 이미 있는 것 (다시 제안하지 않는다)

- 그룹 공격 슬롯(근접 2, 원거리 2), 그룹·플레이어 브리더, 대형 슬롯 — `MonsterGroupController`
- Intent 가중치 SO 4종, 반복 패널티 — [Intent 가중치 외부화](MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md) Phase A~D
- 플레이어 읽기: 공격·가드·회피·후딜 빈도와 현재 상태 — `EnemyTacticalMemory`
- Intent 타임라인, 인카운터 리플레이 — `AI/Debugging/`
- 도지 카운터, 저스트 가드, 입장 공격(플레이어 측)

## 3. 단계별 설계

각 단계는 독립적으로 끝내고 체감을 평가한 뒤 다음으로 간다. 한 번에 여러 단계를 넣으면 어떤 변경이 체감을 바꿨는지 분리할 수 없다.

### P0. BT 연결 정상화 (데이터만)

**목표:** 몬스터 종류에 맞는 BT를 연결하고 테스트 BT를 정식 BT로 승격한다.

1. `Test/EnemyBehavior_Test_AggressiveGroundMelee_Tuned.json`과 생성 에셋을 정식 이름(예: `EnemyBehavior_GroundMelee_Aggressive`)으로 옮긴다.
   - 생성 에셋은 `AssetDatabase.MoveAsset`으로 옮겨 **GUID를 보존**한다. 13개 BehaviorData가 이 GUID를 참조한다.
   - JSON의 `id`를 바꾸면 importer가 새 이름으로 에셋을 만들 수 있다. 재생성 시 기존 에셋을 덮어쓰는지 먼저 확인한다.
   - **삭제하지 않는다.** 이 BT를 지우면 13개 BehaviorData의 BT가 비고 해당 몬스터가 움직이지 않는다.
2. 몬스터별 연결을 원형(§P3)에 맞춘다. 초안:

| 몬스터 | 현재 | 제안 | 근거 |
|---|---|---|---|
| skeleton_sword | Balanced | `SkeletonSwordsman_Disciplined` | 이 몬스터를 위해 만든 BT와 설계 문서가 있다 |
| lich (Normal·Elite) | Aggressive(Test) | 원거리 시전형(`RangedKiter` 계열 검토) | 시전형이 근접 압박을 하면 정체성이 무너진다. **AbilitySet의 공격 사거리를 먼저 확인한다** |
| spider_queen | Aggressive(Test) | 보스·정예형 전용 또는 Golem 계열 검토 | 졸개와 같은 BT를 쓰면 여왕의 위계가 드러나지 않는다 |
| spider_minion | Aggressive(Test) | 무리형(`ChildPlant_Swarm` 구조 참고) | 다수가 함께 나오는 몬스터는 개별 압박보다 포위·교대가 맞다 |
| dryad | Aggressive(Test) | 미정. 공격 구성 확인 후 결정 | AbilitySet 확인 전에는 판단하지 않는다 |
| Humanoid 캐릭터판 10 | Balanced | 무기별 `Humanoid_*` 대응 BT | 캐릭터 무기와 BT 원형을 맞춘다 |

3. `BehaviorData_humanoid`의 BT 참조(`547eec6e…`)를 Unity에서 열어 확인한다. 깨진 참조면 의도한 BT를 다시 연결한다.
4. 연결되지 않은 BT(`RangedKiter` 2종, `Example`, `Organic`, 나머지 Test 3종)는 P0 연결 결과를 본 뒤 보관·삭제를 사용자에게 제안한다. 이 단계에서는 지우지 않는다.

**인수 조건**

- 자동 검사: 모든 BehaviorData의 BT가 null이 아니고 `Test/` 아래 에셋을 가리키지 않는다(P1 검증기로 확인). 지상 BehaviorData는 `actorKind=Ground` BT를 가리킨다.
- 사용자 테스트 대기: 리치·거미 여왕·거미 졸개·해골 검사를 각각 상대해 행동 차이가 느껴지는지 확인한다.

### P1. BT 정적 검증기

**목표:** 규칙이 늘어나도 런타임에서 조용히 죽는 분기를 import 시점에 잡는다.

| 검사 | 등급 | 구현 위치 |
|---|---|---|
| `SelectedIntent` 값이 `CombatIntent`(Attack, Punish, Counter, Pressure, Chase, Retreat, KeepDistance, Defend, Recover)인가 | 오류 | `Validation.cs`의 `SelectedIntent` 검사에 `Enum.TryParse<CombatIntent>` 추가 |
| `ExecuteAttack`에 `cooldownId`·`cooldownDuration`이 있는가 | 오류. 메시지에 "RequestAction(intent=Attack)으로 바꾸거나 ActionDelayElapsed로 제한" 안내 | `ValidateAction` |
| `CooldownReady`의 ID가 같은 트리의 `RequestAction`·`Transition` `cooldownId`로 등록되는가 | 경고 | `Validate` 끝에서 트리 전체 ID 집합 대조 |
| 같은 그룹에서 앞선 규칙의 조건이 비어 있어 뒤 규칙에 도달할 수 없는가 | 경고 | 그룹별 규칙 순회 |
| BehaviorData가 `Test/` 아래 BT를 가리키는가, `actorKind`가 액터 이동 방식과 맞는가 | 경고 | 별도 감사 도구(아래) |

- JSON 검사는 기존 `Validate`에 넣는다. 오류는 지금처럼 `InvalidDataException`으로 import를 막는다.
- BehaviorData 감사는 BT JSON과 별개의 에셋 대조라서 Editor 전용 감사 메서드로 둔다. 사람이 데이터를 고친 뒤 반복해서 돌리는 상시 검증이므로 툴 런처 등록 대상이다.
- 기존 Rules JSON 27종(`SourceJson/` 하위 전체)에 새 검사를 돌렸을 때 오류가 나면 JSON을 먼저 고친다. 검사를 완화해서 통과시키지 않는다.

**인수 조건**

- 자동 검사: EditMode 테스트. 잘못된 `SelectedIntent`, `ExecuteAttack`+`cooldownId`, 등록되지 않은 `CooldownReady` ID를 담은 JSON 3종은 import 실패 또는 경고가 나야 하고, 현행 Rules JSON 27종은 오류 0이어야 한다.
- 사용자 확인: 필요 없음(에디터 도구).

### P2. 플레이어 읽기 조건 보강

**목표:** 몬스터가 지금 플레이어가 무엇을 하는지 보고 판단할 수 있게 한다. 값 계산은 `EnemyTacticalMemory`가 맡고, 조건 노드는 부작용 없이 읽기만 한다(목표 가이드 §3.1 원칙).

| 조건 키 | 의미 | 계산 근거 | 쓰임 |
|---|---|---|---|
| `IsTargetCastingOrCharging` (목표 가이드 §3의 키를 그대로 쓴다) | 플레이어가 차지·시전·긴 선딜 중 | 플레이어 상태 태그. 차지 상태 태그가 없으면 상태 ID로 판별하되 **문자열 비교를 새로 늘리지 않는다** | 끊으러 들어가기(공격형) / 사거리 밖으로 빠지기(방어형) |
| `IsTargetBehind` | 플레이어가 몬스터 후방 각도 안 | 몬스터 forward와 타겟 방향의 내적. 각도는 블랙보드 `rearAngle` | 회전 공격, 뒤돌기 |
| `RecentlyGuardBroken` | 몬스터가 최근 가드 브레이크를 당함 | `EnemyTacticalMemory`에 시각 기록 추가 | 가드 대신 거리 벌리기 |
| `IsPlayerLowHealth` | 플레이어 체력 비율이 기준 이하 | 타겟 체력 조회 | 마무리 압박 강화. 기본은 꺼 두고 보스·정예에서만 쓴다 |
| `HasAttackSlot` | 그룹 공격 슬롯을 점유할 수 있음 | `MonsterGroupController`에 부작용 없는 조회 추가 | 슬롯이 없을 때 원이동·위협 자세로 전환 |

- 이미 있는 조건(`HasEnemyLineOfSight`, `CooldownReady`, `IsSelfLowHealth`)은 다시 만들지 않는다.
- 핫패스 규칙: 조건 평가에서 `GetComponent`·할당·문자열 결합을 쓰지 않는다. 필요한 참조는 `SetPlayerTarget`에서 한 번 캐시한다.
- 새 조건은 `IntentEvaluationContext`에도 넣을지 각각 판단한다. 스코어러 가중치에 반영할 값(차지 중)만 넣고, 규칙 전용 값(후방)은 넣지 않는다.
- `HasAttackSlot`은 조회만 한다. 슬롯 점유는 기존 `RequestAttackSlot` 액션이 맡는다.

**그룹 비소속 몬스터의 슬롯 문제:** `EnemyAIController.TryRequestAttackSlot`은 그룹이 없으면 항상 `true`를 반환한다. 이 단계에서는 동작을 바꾸지 않고 P1 감사에 "전투 몬스터가 그룹 없이 배치됨" 경고를 추가해 배치 데이터에서 먼저 해결한다. 그래도 문제가 남으면 P5에서 전역 기본 슬롯을 검토한다.

**인수 조건**

- 자동 검사: 컴파일 오류 0. 조건별 EditMode 테스트(후방 각도 경계값, 가드 브레이크 기록 만료). JSON 노드 키가 importer와 표시명 레지스트리에 모두 등록되었는지 확인한다.
- 사용자 테스트 대기: 차지 공격 시 공격형 몬스터의 끊기 반응과 후방으로 돌아갔을 때 회전 공격이 자연스러운지 확인한다.

### P2a. 전투 구역 유지와 반복 회피 제한

**사용자 보고:** 궁수가 반복해서 굴러 원래 전투 위치에서 크게 벗어나고, 플레이어가 따라가 공격하기도 어렵다. 전 몬스터의 전투 구역 유지와 궁수의 회피 후 공략 기회를 함께 다룬다. 구르기 횟수만 줄여도 일반 후퇴·거리 유지가 같은 이탈을 만들 수 있으므로 이동 경로까지 대조한다.

**현재 코드에서 확인한 범위:** `EnemyAIController`는 초기화 시 `_spawnPosition`을 보관한다. `EnemyDodgeState.CalculateDodgeDirection`은 근거리에서 후방 성분을 크게 사용하며 전투 구역 경계를 참조하지 않는다. `OnMovementHit`은 벽 충돌 후 방향을 다시 계산할 수 있다. 구르기는 이동 시간 이후 모션 완료/잠금 시간까지 유지될 수 있고, 상태 전체의 `GrantsInvincibility`가 true다. 반복 진입의 원인과 실제 무적 소비 경로는 구현 단계에서 별도로 조사한다. 이 사실만으로 BT 쿨다운 누락이나 무적 시간이 원인이라고 단정하지 않는다.

#### 구역과 이동 계약

- 플레이어와의 선호 거리와 자기 구역의 허용 반경을 분리한다. 기준점은 배치·스폰 소유자가 제공하는 고정 귀속 지점을 우선하고 기존 스폰 위치를 재사용할 수 있는지 조사한다. 플레이어 위치·회피 종료 위치로 기준점을 매번 옮기지 않는다. 순찰·풀링 재스폰·귀속 변경 시 갱신 시점을 명시한다.
- 경계 여유 구간에서는 구역 안쪽 또는 접선 방향의 유효한 회피·후퇴 후보를 고른다. 예상 종료점뿐 아니라 이동 중 경로·벽 충돌 후 재선택도 확인한다. 경로가 없으면 회피 요청을 실패시키고 가능한 다른 행동을 선택한다. 순간이동·벽 관통·외향 이동 강행으로 해결하지 않는다.
- Chase·Retreat·KeepDistance·Circle과 구르기 모두 같은 경계 계약을 소비한다. BT의 선택 제한과 실행 중 이동 제한의 책임을 구분하며 기존 이동·내비게이션 기능을 우선 재사용한다. 비행은 별도 이동 경로를 조사하고, 공통 적용 여부와 미지원 범위를 기록한다.
- 외부 넉백 등으로 밖에 나간 경우 복귀가 외향 전투 행동보다 우선한다. 복귀 진입·해제에 서로 다른 임계값을 써 경계에서 왕복하지 않게 한다. 사망·잡힘·피격 등 강제 상태를 무시하지 않는다. 타겟이 구역 밖에 있을 때 추적 중단·복귀·재교전 조건을 명시한다. 경계 왕복으로 무한 회복하는 악용을 막으며 체력 초기화·무적 복귀 정책을 임의로 추가하지 않는다.

#### 회피와 공략 기회 계약

- 단순 접근만으로 무한 구르기를 선택하지 않는다. 실제 위협 판단, 회피 성공 시 시작하는 재사용 제한, 연속 회피 상한과 회복 조건을 함께 정한다. 실패한 요청은 성공 횟수·쿨다운을 소비하지 않으며, BT 외 직접 상태 요청도 같은 실행 허용 검사를 거친다.
- 구르기 후에는 플레이어가 추적 공격을 시도할 수 있는 회복 구간을 둔다. 이 구간에 재구르기·즉시 후퇴로 공격 기회를 계속 지우지 않는다. 측면 이동·자리 잡기·견제는 구역과 회복 계약 안에서 선택한다. 궁수를 항상 멈춰 맞게 하거나 원거리 정체성을 없애는 것을 목표로 삼지 않는다.
- 무적 유효 구간과 모션 전체 길이를 구분해 실제 소비 경로를 확인한다. 이동 거리·무적·회복·캔슬은 기존 실행 데이터/MotionSet 계약에 맞춰 조정하고, 판단은 BT가 맡는다. 플레이어 추적 속도·공격 흡착·락온을 함께 바꿔 원인을 섞지 않는다.
- 반경·경계 여유·복귀 임계값·회피 간격·연속 횟수·회복 시간은 `Assets/10.Datas/`의 적절한 기존 SO를 우선 사용한다. SO 필드와 블랙보드에 같은 값을 중복 저작하지 않는다. 수치는 실제 이동 거리·공격 사거리·모션을 측정해 정하며 이 문서에서 임의로 고정하지 않는다. 몬스터 이름 분기는 금지한다.

**인수 조건**

- 자동 검사: 경계 안/위/밖 및 예상 회피 경로, 벽 충돌 후 재선택, 외부 밀침 후 복귀·임계값, 풀링 재스폰 기준점, 요청 성공/실패와 연속 회피 회복을 확인한다. BT·직접 요청 모두 재사용·회복 제한을 우회하지 않는다. P1 검증기를 통과한다.
- 사용자 테스트 대기: 실제 사용 씬과 BT가 확인된 궁수로 평지에서 계속 접근하며 반복 구르기·최대 귀속 거리·회피 후 공격 시도를 기록한다. 벽/절벽/구역 경계, 타겟의 경계 왕복, 넉백 후 복귀도 확인한다. 근접·비행 대표의 추적과 복귀를 회귀 확인한다. 씬·몬스터·SO 수치·허용 오차·관찰 시간은 실행 전에 명세에 적는다.
- 실패 기준: 자발적 이동으로 허용 구역을 계속 이탈함, 연속 회피 상한/회복 제한 우회, 복귀와 교전의 진동, 추적 공격 기회를 주지 않는 회피 반복, 귀환 중 정지·벽 관통·체력 초기화 악용. 수치 통과와 사용자 체감 합격을 구분한다.
- 인계: 구현 시 `docs/work-status/monster-bt-improvement/P2A_TERRITORY_EVASION.md`에 경로별 소비자, 기준점 수명, 변경 전후 측정, SO 값과 근거, 검증 및 사용자 테스트 명세를 기록한다. 지금은 이 문서가 생성되었다고 가정하지 않는다.

### P3. 원형별 템플릿 분화

**목표:** "무기만 다른 같은 몬스터"를 없앤다. 원형 분류는 [Intent 가중치 외부화](MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md) §5.1(Phase F)의 Archetype을 그대로 쓰고 새 분류를 만들지 않는다.

| 원형 | IW 프로필 | BT 템플릿 | 행동 정체성 | 대상 |
|---|---|---|---|---|
| Bruiser | `IW_AggressiveMelee`(Bruiser 전용은 미구현) | GreatSword, DoubleAxe, Golem | 긴 선딜 후 큰 한 방, 맞아도 잘 안 물러남, 플레이어 후딜 응징 | 대검·쌍도끼 휴머노이드, 골렘 |
| Skirmisher | 신규 필요(`IW_Skirmisher`) | DualBlade | 측면 이동 → 짧은 연타 → 이탈 반복, 후방 노림 | 쌍검 휴머노이드, 거미 졸개 |
| Guardian | `IW_DefensiveShield` | SwordShield, Disciplined | 가드 후 반격, 가드 브레이크당하면 거리 벌림 | 검방 휴머노이드, 해골 검사 |
| RangedCaster | `IW_RangedCaster` | Bow, SkeletonBow, RangedKiter | 선호 거리 유지, 접근하면 물러나며 견제, 차지 중인 플레이어에 견제 사격 | 활 휴머노이드, 해골 궁수, 리치 |
| FlyingHarasser | `IW_RangedCaster` 또는 전용 | DiveHarasser | 공중 견제, 급강하 쿨다운 압박 | 그리핀 |

- 한 원형 안에서는 블랙보드 수치(`aggression`, `counterChance`, `preferredRange` 등)로 개체 차이를 낸다. 같은 원형의 JSON을 복사해 규칙을 조금씩 바꾸는 방식은 피한다.
- Humanoid 5종은 지금 의도 분기 구조가 거의 같다. 원형별로 **최소 한 개 그룹을 정체성 그룹으로 교체**한다(예: Skirmisher의 "측면 진입 → 연타 → 이탈" 그룹).
- 단순한 BT(늑대 1.7KB·의도 분기 0, Lucia 1.8KB)는 원형을 정한 뒤 최소한의 후퇴·측면 분기를 추가한다. 늑대는 무리 협공(Skirmisher)이 후보다.
- 공격이 읽히는지는 공격 데이터 층이 정한다. BT가 정체성을 요구해도 해당 몬스터 AbilitySet에 맞는 공격(예: Bruiser의 긴 선딜 강공격)이 없으면 공격 데이터 작업을 따로 요청하고 BT는 보류로 표시한다.
- BT JSON 작성은 `generate-bt-json` 스킬과 Rules 포맷(SourceJson)을 따른다.

**인수 조건**

- 자동 검사: P1 검증기 통과. 원형별 JSON 차이 비교(정체성 그룹 존재 여부).
- 사용자 테스트 대기: 원형마다 한 종씩 상대해 대응법이 다르게 느껴지는지 확인한다. 밸런스(TTK) 변화가 있으면 [밸런스 도구](../../../guide/BALANCE_DESIGNER_TOOL_GUIDE.md)로 측정한다.

### P4. 의도 선택 안정화

**새로 설계하지 않는다.** [Intent 가중치 외부화](MONSTER_INTENT_WEIGHTS_EXTERNALIZATION_DESIGN.md) §4.2 Phase E의 미정 항목(유지 보너스, 전환 비용, 최소 유지 시간)을 그 문서에서 확정하고 구현한다. 이 문서는 우선순위만 정한다.

- P3 이후에 한다. 원형별 BT가 자리 잡기 전에 선택을 안정화하면 정체성 문제를 안정화 부족으로 오인하기 쉽다.
- 피격·포이즈 브레이크·타겟 상실은 최소 유지 시간의 예외로 즉시 재선택한다(Phase E 원안 유지).
- `LegacyIntentScoring`과의 회귀 동등성은 안정화 보정을 끈 상태에서 계속 유지한다.

### P5. 그룹 역할

**목표:** 다수 전투를 "한 마리씩 번갈아 공격"에서 "역할을 나눈 압박"으로 바꾼다. 기존 슬롯·브리더·대형 위에 역할만 얹는다.

| 역할 | 행동 | 할당 |
|---|---|---|
| 공격 | 공격 슬롯 점유, 실제 공격 | 슬롯을 얻은 몬스터 |
| 견제 | 원이동, 위협 자세, 플레이어 시야 안에 머무름 | 슬롯을 못 얻은 근접 몬스터 |
| 지원 | 거리 유지 사격, 플레이어 후딜에만 개입 | 원거리 몬스터 |

- `MonsterGroupController`가 역할을 정해 각 몬스터 블랙보드 `groupRole`에 쓰고, BT는 `BlackboardCompare`로 읽는다. 새 조건 노드를 만들지 않는다.
- 다수 전투 조작감([다인 전투 조작감 설계](../../../Complete/MULTI_ENEMY_CONTROL_FEEL_DESIGN.md))과 함께 체감을 봐야 한다. 이 단계는 단독으로 넣고 평가한다.

**인수 조건**

- 자동 검사: 3·5마리 그룹 시뮬레이션에서 동시 공격 수가 슬롯 상한을 넘지 않는다.
- 사용자 테스트 대기: 3마리 이상 교전에서 억울함 없이 압박감이 느껴지는지 확인한다.

### 보류: 페이크 공격

`FeintAttack`(선딜 중 취소)은 타이밍을 공격 데이터·MotionSet 층이 소유한다. 몬스터별 취소 가능한 모션이 정해지기 전에는 BT 액션을 만들지 않는다. 대신 지금 가능한 "지연 공격"(`Wait` 뒤 공격)으로 낚시 효과를 낸다. [단련된 검사 §4.1](MONSTER_AI_MELEE_SWORDSMAN_DESIGN_KR.md) 참고.

## 4. 범위 밖

| 항목 | 이유 | 정본 |
|---|---|---|
| 적 스태미나 | 별도 설계가 있고 미구현이다. 도입되면 P2 조건에 "스태미나 부족"을 추가한다 | [적 스태미나](ENEMY_STAMINA_MODEL_DESIGN.md) |
| 보스 다단계 계획 | 일반 몬스터와 구조가 다르다 | [보스 다단계 계획](BOSS_HIERARCHICAL_PLAN_DESIGN.md) |
| 원격 의도 실험(Laya) | 에디터·개발 빌드 전용 훈련 실험이다 | `AI/CombatDecision/EnemyRemoteIntentController.cs` |
| 리플레이 재생 UI | 튜닝 편의 기능이다. 필요해지면 따로 진행한다 | [디버깅 인프라](../../../Complete/MONSTER_AI_DEBUG_TUNING_INFRASTRUCTURE_DESIGN.md) §6 |

## 5. 진행 순서와 위험

| 단계 | 작업 성격 | 위험 | 대응 |
|---|---|---|---|
| P0 | 데이터 | 테스트 BT 이동 중 GUID가 바뀌면 13개 몬스터의 BT가 빈다 | `MoveAsset` 사용, 이동 후 GUID 재대조 |
| P1 | 에디터 코드 | 기존 JSON이 새 검사에 걸린다 | JSON을 고친다. 검사는 완화하지 않는다 |
| P2 | 런타임 코드 | BT 평가 핫패스 비용 | 메모리에서 미리 계산, 조건 노드는 읽기만 |
| P3 | 데이터 | 원형이 요구하는 공격이 AbilitySet에 없다 | 공격 데이터 작업으로 분리, BT는 보류 표시 |
| P4 | 런타임 코드 | 안정화가 반응성을 떨어뜨린다 | 피격·브레이크 예외, 유지 시간은 SO 수치 |
| P5 | 런타임 코드 | 다수 전투 체감 변화의 원인 분리가 어렵다 | 단독 적용 후 평가 |

실제 작업은 P1 검증기를 먼저 완료하고 P0 연결 정상화를 수행한다. P2a는 P3 전에 별도 검수한다. 자세한 선행 조건과 복사 가능한 프롬프트는 [작업 요청서](../../../requests/MONSTER_BT_IMPROVEMENT_TASK_PROMPTS.md)를 따른다.

## 6. 추가 행동 패턴 상세 설계 (2026-11-14 조사)

이 절은 P3의 행동 계약을 구체화한다. 기존 P0~P2a 구현을 다시 개발하는 계획이 아니며, 실제 배포는 기존 사용자 검수 선행 조건을 따른다. P4 점수 보정은 Intent 가중치 외부화 문서가 정본이다. 이번 조사·검증 범위는 [추가 조사 상태](../../../../../docs/work-status/monster-bt-improvement/ADDITIONAL_PATTERN_DESIGN.md)에 기록한다.

### 6.1 조사 결론과 근거

현행 `MonsterBehaviorJsonNodeKeys.cs`에는 차지·후방·가드 파괴·저체력·슬롯 조건이 이미 있다. `EnemyTacticalMemory.PlayerRead.cs`의 `IsTargetBehind`는 **적 자신의 뒤에 플레이어가 있는지**이며, 적이 플레이어 후방을 잡았다는 뜻이 아니다. Skirmisher 후방 공격 조건으로 재사용하지 않는다.

`Humanoid/EnemyBehavior_Humanoid_Bow.json`에는 차지 견제와 Wait 뒤 Signature 공격, 슬롯 부재 시 Circle/Flank가 이미 있다. 따라서 차지 견제·지연 공격·슬롯 양보 자체를 신규 기능으로 제안하지 않는다. 반면 상위 Signature 그룹이 차지 견제보다 우선한다. 원형의 반응을 평가할 때 선택 빈도뿐 아니라 상위 규칙에 가려진 횟수를 확인해야 한다.

외부 참고: [Epic의 BT 개요](https://dev.epicgames.com/documentation/en-us/unreal-engine/behavior-tree-in-unreal-engine---overview)는 Blackboard, Services, Observer Aborts를 통한 조건 변화 대응을 설명한다. 이를 참고한 프로젝트 설계 판단은 **패턴의 시작 조건과 중단 조건을 함께 저작해야 한다**는 것이다. Unreal의 실행 의미나 이벤트 기반 구현을 이 프로젝트에 그대로 가정하지 않는다. 전체 BT 엔진 교체는 범위 밖이다.

### 6.2 원형별 추가 패턴

아래는 행동 제안이며 대응 Ability·Motion 검증 완료를 뜻하지 않는다. 대표 후보는 기존 무기별 Humanoid로 제한하고 리치·거미·드라이어드 재연결은 기존 미확정 상태를 유지한다.

| 우선 | 원형·대표 후보 | 시작 → 행동 → 종료 | 플레이어 대응과 제한 | 필요한 작업 |
|---|---|---|---|---|
| 1 | Guardian·SwordShield | 가드 성공 확인 → 짧은 반격 기회 → 실패하면 자리 유지 | 공격을 멈추고 반격을 피하거나 측면으로 이동. 가드 자세만으로 반격 확정 금지 | 실제 가드 성공 관찰·유효 시간 계약 조사 필요. Counter Ability 확인 |
| 1 | Bruiser·GreatSword | 유효 거리에서 준비 → 강공격 → 확정 후딜 | 준비 동작을 보고 피한 뒤 후딜 공략. 추적 회전·즉시 회피가 보상을 없애지 않아야 함 | 기존 Signature/Finisher와 Motion 취소 구간 확인. BT에 선딜 수치 복제 금지 |
| 1 | Skirmisher·DualBlade | 유효 측면 경로 → 짧은 공격 묶음 → 경계 안 이탈 → 재진입 | 접근을 끊거나 이탈 끝을 추격. 계속 옆으로만 도는 행동 금지 | Flank 성공/진행/실패 관찰 계약 확인. 행동 완료를 시간 경과만으로 추정하지 않음 |
| 2 | RangedCaster·Bow | 사선 확보 → 제한된 사격 → 자리 바꿈 | 접근하면 방어 행동을 유도하고 회복 구간 공략 | 기존 차지 견제 유지. 사선 상실 시 재배치 후보·최대 시도 설정 추가 검토 |
| 3 | FlyingHarasser·Griffin | 공중 견제 → 예고된 급강하 → 착지 취약 구간 → 이륙 | 급강하 회피 뒤 착지 공략 | Flying 전용 노드만 사용. 착지/이륙 Ability·모션과 경로 확인 전 보류 |

Guardian의 신규 관찰은 플레이어 가드 상태와 구분한다. 기존 `RecentlyGuardBroken`은 적 자신의 파괴 결과 기억이므로 가드 성공으로 해석하지 않는다. Skirmisher의 후방 보너스는 초기 범위에서 제외하고 측면 접근만으로 정체성을 만든다. 새 공격·소환·힐·잡기·페이크는 필요한 콘텐츠가 확정되기 전 추가하지 않는다.

### 6.3 공통 실행·실패 계약

1. 실행 중 강제 상태 보호와 타깃 부재 처리는 최상위에 유지한다. 패턴 시작 전에 타깃·행동 지연·해당 Ability 사용 가능·사거리·사선·슬롯을 확인한다. 요청 시 실행 계층도 다시 검사한다. 슬롯 조회는 예약 성공이 아니다.
2. Wait 또는 이동 뒤 공격은 오래된 조건을 믿지 않는다. 공격 직전 타깃·사거리·사선·Ability·슬롯을 다시 검사한다. 현재 Rules JSON의 `do`가 중간 조건 재검사를 표현하는지는 importer와 Sequence 런타임을 구현 단계에서 확인한다. 지원되지 않으면 존재하지 않는 JSON 키를 만들지 말고 최소 조건 재검사 계약을 먼저 구현한다.
3. 이동 요청 수락과 목적지 도착, 공격 선택과 실제 발동을 구분한다. 성공한 실행에만 패턴 진행·반복 횟수를 반영한다. 실패는 원인별로 대체 행동을 선택하고 매 틱 같은 실패를 무한 재시도하지 않는다.
4. 사선 상실은 제한된 재배치 후 관찰/추적, 슬롯 부재는 공격하지 않는 위치 조정, 경로 실패는 도달 가능한 구역 내 대기·재탐색으로 처리한다. 재탐색 횟수와 간격은 SO 설정으로 둔다. 공격 없는 대기만 영구 반복하면 실패다. `PathPartial` 문제를 BT 성공 처리로 숨기지 않는다.
5. 피격·사망·타깃 변경·풀 반환·복귀 전환 시 패턴 진행과 예약을 정리한다. 강제 상태 이후는 유효 조건부터 다시 판단한다. 최소 유지 시간이 강제 상태나 실행 불가 행동을 붙잡지 않게 한다.
6. 관측된 차지에 대한 반응은 관측 지속 시간과 반응 제한을 둔다. 원시 입력을 읽지 않는다. 모든 개체가 같은 프레임에 대응하지 않도록 개체별 반응 설정을 사용하되, 무작위 선택을 매 틱 다시 뽑아 반응을 무한 연장하지 않는다. 구체 값은 실제 차지·공격 모션 측정 후 확정한다.

### 6.4 데이터 소유와 작업 경계

- Rules JSON: 조건·우선순위·대체 행동. Generated는 importer로만 생성한다.
- EnemyBehaviorSO 또는 기존 행동 설정: 반응 지속 시간, 재배치 시도 상한 등 판단 설정. 같은 값을 Blackboard와 SO 양쪽에서 저작하지 않는다.
- 기존 IntentWeights SO: P4 유지·전환 보정. 신규 Intent는 만들지 않는다.
- AbilitySet/GAS: 사용 가능한 공격·비용·공격 쿨다운. MotionKey/MotionSet: 예고·타격·후딜·캔슬. 패턴 재시도 제한과 Ability 쿨다운을 구별한다.
- 실행 결과·관찰 시각·패턴 진행: 액터별 런타임 메모리. 공유 BT 에셋에 개체별 상태를 쓰지 않는다. 새 Blackboard 키는 registry와 생성 절차를 따른다.
- P5 역할은 기존 그룹 슬롯·브리더 위에서 처리한다. 독립 그룹 여럿의 동시 압박과 그룹 없는 적은 그룹 내부 상한만으로 보장되지 않는다. 전역 제한을 구현했다고 간주하지 말고 별도 인카운터 검증 대상으로 남긴다.

### 6.5 구현 순서와 인수

P2a 회피 후 공략 기회·급경사 복귀 확인 → P3 대표 3종의 Ability/모션 목록 대조 → 원형별 한 패턴씩 적용 → P4 안정화 → P5 역할 순서를 유지한다. 사선 재배치·비행 확장은 후순위다. 대표 선정은 설계 제안이며 에셋 연결 변경은 이번 범위에 없다.

| ID | 준비·조작 | 기대 결과·실패 기준 |
|---|---|---|
| A01 | 동일 평지·동일 플레이어 장비로 대표 3종을 개별 교전, 접근/대기/공격 반복 | 각 원형에 다른 대응이 유효해야 함. 수치만 다르고 행동 순서가 같으면 실패 |
| A02 | 지연 공격 준비 중 사거리 이탈·엄폐·타깃 제거를 각각 수행 | 무효 공격 요청 취소와 대체 행동. 과거 타깃 공격·영구 대기는 실패 |
| A03 | 측면 이동 중 벽·구역 경계로 유도 | 제한된 실패 처리와 유효 위치 복귀. 제자리 반복·벽 관통은 실패 |
| A04 | 차지 시작/취소 반복 및 적 회피 직후 추적 공격 | 즉시 입력 읽기처럼 보이는 전원 반응 금지. 회피 후 공략 가능 여부는 사용자 판단 |
| A05 | 근접·원거리 혼합 3/5마리 및 별도 그룹 중첩 교전 | 그룹별 슬롯 준수와 전체 압박을 별도 기록. 화면 밖 연속 피격·무한 경직은 실패 |
| A06 | 실행 중 피격·사망·풀 재사용·타깃 교체 | 잔류 슬롯·이전 패턴 이어 실행 없음 |

구현 자동 검사는 관련 기존 EditMode에 실패·중단·초기화 사례를 추가하고 JSON 검사·Unity import·참조 검증을 수행한다. 플레이 테스트는 실제 씬/액터/설정 값과 관찰 시간을 구현 시 명세에 고정한다. 기존 타임라인·리플레이로 규칙 선택, 실행 성공/거절 원인, 반복 수, 동시 공격 수를 비교한다. 새 텔레메트리 플랫폼은 만들지 않는다. 결과 회신은 `ID / 통과·실패·미수행 / 실제 결과 / 근거`로 받는다.

### §6.3 구현 연결

Wait 이후 그룹/규칙 조건 재검사와 지상 공격 직전 사선 확인은 [구현·검증 상태](../../../../../docs/work-status/monster-bt-improvement/DELAY_RECHECK.md)에서 관리한다. 이 반영은 §6.2 원형별 신규 패턴의 구현 완료를 뜻하지 않는다.
