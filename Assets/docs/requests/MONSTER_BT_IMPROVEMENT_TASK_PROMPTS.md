# 몬스터 BT 행동 개선 — 작업 요청 프롬프트

다른 세션에서 아래 1단계부터 순서대로 요청한다. 각 코드 블록은 그대로 복사해 쓸 수 있다. 요청을 받은 세션은 이 파일의 공통 규칙을 먼저 읽으므로 대화 이력을 붙일 필요가 없다. 이 문서는 현재 지시가 아니다. 쓰기 전에 설계와 최신 코드·에셋을 대조한다.

기준 설계: [몬스터 BT 행동 개선 설계](../design/combat/monster/MONSTER_BT_BEHAVIOR_IMPROVEMENT_DESIGN.md) (설계만, 미구현 상태에서 작성).

## 사용 순서

| 단계 | 설계 | 내용 | 사용자 결정 |
|---|---|---|---|
| 1 | P1 | BT 정적 검증기와 BehaviorData 감사 | 없음 |
| 2 | P0 | BT 연결 정상화(테스트 BT 승격, 몬스터별 BT 재연결) | **필요.** 아래 "2단계 전 결정" 참고 |
| 3 | P2 | 플레이어 읽기 조건 노드 | 없음 |
| 3-A | P2a | 전투 구역 유지·반복 회피 제한 | 없음. 실제 측정으로 수치 초안 작성 후 사용자 검수 |
| 4 | P3 | 원형별 BT 템플릿 분화 | 원형별 대표 몬스터 범위 |
| 5 | P4 | 의도 선택 안정화 | 없음 |
| 6 | P5 | 그룹 역할 | 없음 |

1단계(검증기)를 2단계(연결 정상화)보다 먼저 한다. 검증기가 연결 결과를 확인하는 도구가 된다. 단계마다 인계 문서를 갱신하고 사용자 테스트 결과를 받은 뒤 다음으로 간다. 3-A는 궁수의 반복 구르기·전투 구역 이탈에 대한 추가 단계이며 3단계 후, 4단계 전에 단독 적용·검수한다. 기존 1~6단계 번호는 유지한다. 5·6단계는 3·3-A·4단계의 체감을 본 뒤 진행한다. 같은 작업 폴더와 Unity Editor에서 두 구현 세션을 동시에 실행하지 않는다.

### 2단계 전 결정 (사용자가 정해서 프롬프트에 채운다)

설계 §P0의 초안 표가 근거다. 아래를 확정하지 못했으면 2단계 프롬프트의 해당 줄을 "조사 후 후보만 제시하고 변경하지 않는다"로 바꾼다.

- 리치(Normal·Elite)를 원거리 시전형 BT로 바꿀 것인가. 바꾸면 어떤 BT를 쓸 것인가.
- 거미 여왕·거미 졸개·드라이어드에 어떤 BT를 줄 것인가.
- 해골 검사(skeleton_sword)를 `SkeletonSwordsman_Disciplined`로 바꿀 것인가.
- 파티 캐릭터 몬스터판 BehaviorData 10개를 무기별 `Humanoid_*` BT로 바꿀 것인가. 이들이 실제로 스폰되는지는 설계에서 미확인이다.

## 모든 단계의 공통 규칙

### 작업 기준

- 작업 루트는 `C:/UsingProject/UnityProject/UPlayground`다. 저장소 AGENTS.md, 기준 설계, [몬스터 BT 고도화 목표 가이드](../guide/MONSTER_BT_ADVANCEMENT_GOAL_GUIDE.md), [전투 저작 가이드](../guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [코드 작성 지침](../guide/CODE_AUTHORING_GUIDE.md), [테스트 작업 지침](../guide/TEST_VALIDATION_GUIDE.md)을 먼저 읽는다. 위키 [전투](../../../docs/wiki/combat.md)를 읽고 실제 코드·에셋과 대조한다.
- 제안이나 계획만 반환하지 말고 요청 단계의 조사·구현·데이터 연결·관련 검사·인계 기록까지 수행한다. 요청에 없는 다음 단계는 실행하지 않는다.
- 판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다. 제거된 `EnemyAttackDataSO`·Resolver를 되살리지 않는다. 몬스터·스테이지 이름으로 분기하지 않는다.
- 이미 있는 것은 다시 만들지 않는다: 그룹 공격 슬롯·브리더·대형, Intent 가중치 SO와 반복 패널티, 플레이어 읽기 빈도 관찰, Intent 타임라인·리플레이, `HasEnemyLineOfSight`·`CooldownReady`·`IsSelfLowHealth` 조건.
- 설계와 다른 결정은 이유·영향·검증 방법을 인계에 남긴다. 설계의 이름·수치를 이미 구현된 API로 가정하지 않는다.
- 전투 구역 유지와 회피 후 공략 기회는 모든 원형의 공통 제약이다(설계 §P2a). 선호 거리 유지·추적·후퇴·그룹 역할이 이 제약을 우회하면 안 된다. 3-A 이전 단계는 관련 경로를 조사·기록하되 해당 런타임 변경을 섞지 않는다.
- BT JSON 작성·수정은 `generate-bt-json` 스킬을 쓰고 Rules 포맷(`SourceJson/`)을 기본으로 한다. Ability·AbilitySet을 건드리면 `generate-gas-ability` 스킬을 쓴다.
- 핫패스(BT 평가, 조건 노드, `ManagedTick`)에서 `GetComponent`·`Find*`·LINQ·매 틱 할당·문자열 결합을 쓰지 않는다. 진단 로그는 `RuntimeLog.Trace`/`TraceThrottled`를 쓴다.
- 새 에디터 도구는 `[UPlaygroundTool("ID")]`를 붙이고 Unity 상단 메뉴에 올리지 않는다. 사람이 반복해서 여는 도구만 툴 런처에 등록한다.

### 사용자 데이터와 에셋

- 시작 시 관련 미커밋 변경을 기록하고 보존한다. 이전 세션 산출물도 사용자 작업으로 취급한다. 요청 없이 커밋·스테이징하지 않는다.
- BT 에셋(`Generated/*.asset`)·BehaviorData·몬스터 프로필의 GUID를 보존한다. 이동은 `AssetDatabase.MoveAsset`으로 하고 이동 후 GUID를 재대조한다. `.meta`를 동반한다. 직접 파일 이동·삭제를 하지 않는다.
- BT 에셋을 삭제하지 않는다. 연결되지 않은 BT의 보관·삭제는 근거를 정리해 사용자에게 제안만 한다.
- 검증 중 `Assets/10.Datas/`·`Assets/03.Prefabs/`가 자동 재직렬화되면 diff를 검사해 자동 변경분만 원복한다.
- Unity Editor는 하나다. MCP·Test Runner로 다루고 두 번째 Editor를 열지 않는다. C# 수정은 [Hot Reload 운영](../guide/HOT_RELOAD_USAGE_GUIDE.md)을 따른다.

### 검증과 단계 완료

- 코드를 바꾼 단계는 Unity 컴파일 오류 0과 관련 EditMode 테스트를 실행한다. 데이터만 바꾼 단계는 1단계 검증기와 BehaviorData 감사를 실행한다. 문서만 바꾼 단계는 링크·diff를 확인한다.
- 별도 요청 없이 Player·Addressables 빌드를 하지 않는다.
- 자동 검사와 사용자 플레이 확인을 구분한다. 검사마다 대상·실행 개수·통과/실패/미수행·근거 경로를 기록한다. 사용자 플레이 확인이 남으면 `사용자 테스트 대기`로 둔다. 미검증을 완료로 보고하지 않는다.
- 몬스터 체감은 AI가 대신 평가하지 않는다. 사용자가 실행할 테스트 명세(씬·몬스터·확인 항목)를 제공한다.

### 인계 기록

산출물 위치는 `docs/work-status/monster-bt-improvement/`이다. 폴더가 없으면 1단계에서 만든다. 아래는 앞으로 만들 문서이며 이 프롬프트가 존재한다고 완료로 보지 않는다.

| 문서 | 기록할 내용 |
|---|---|
| `STATUS.md` | 단계별 구현·자동 검증·사용자 검수 상태, 현재 단계, 알려진 실패, 다음 작업. 단계마다 갱신 |
| `P1_VALIDATOR.md` | 추가한 검사 목록, 기존 JSON 27종 결과, 수정한 JSON |
| `P0_ASSIGNMENT.md` | 변경 전후 BehaviorData↔BT 표, 이동한 에셋과 GUID 대조, 사용자 결정 |
| `P2_PLAYER_READ.md` | 조건별 계산 방식·캐시·성능 근거, 등록 위치, 테스트 |
| `P2A_TERRITORY_EVASION.md` | 구역 기준점 수명·이동 경로별 제약, 회피 재사용·연속 제한·회복, 변경 전후 측정과 사용자 테스트 |
| `P3_ARCHETYPES.md` | 원형별 BT·IW 프로필, 정체성 그룹, 공격 데이터 요청 항목 |

단계가 끝나면 `STATUS.md`를 먼저 갱신하고 [전투 위키](../../../docs/wiki/combat.md)의 현재 판단을 갱신한 뒤 링크를 확인한다. 설계 문서의 헤더 상태 문구도 실제 진행에 맞게 바꾸고 상태 문서를 연결한다. 인계와 최종 보고에 갱신한 위키 링크를 넣는다.

## 1단계 — BT 정적 검증기와 BehaviorData 감사 (설계 P1)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
Assets/docs/requests/MONSTER_BT_IMPROVEMENT_TASK_PROMPTS.md의 "모든 단계의 공통 규칙"을 먼저 읽고 따른다.
기준 설계: Assets/docs/design/combat/monster/MONSTER_BT_BEHAVIOR_IMPROVEMENT_DESIGN.md §P1.

목표: 규칙 JSON import 시점과 BehaviorData 감사에서, 런타임에 조용히 죽는 분기와 잘못된 BT 연결을 잡는다.

해야 할 일:
1. docs/work-status/monster-bt-improvement/ 폴더와 STATUS.md를 만든다. 시작 시 미커밋 변경을 기록한다.
2. 먼저 현재 코드를 확인한다: MonsterBehaviorTreeJsonImporter.Validation.cs의 SelectedIntent 검사, NodeFactory의 ExecuteAttack 정의, RequestEnemyActionNode·TransitionEnemyStateNode가 쿨다운을 등록하는 위치와 CooldownReadyNode가 읽는 위치. 설계 §2.3의 사실이 현재 코드와 맞는지 대조하고 다르면 인계에 적는다.
3. Validate에 다음을 추가한다.
   - SelectedIntent의 value가 CombatIntent 이름(Attack, Punish, Counter, Pressure, Chase, Retreat, KeepDistance, Defend, Recover)인지 Enum.TryParse로 검사. 실패는 오류.
   - ExecuteAttack에 cooldownId/cooldownDuration이 지정되면 오류. 메시지에 RequestAction(intent=Attack) 사용 또는 ActionDelayElapsed로 제한하라는 안내를 넣는다.
   - CooldownReady의 ID가 같은 JSON의 RequestAction/Transition cooldownId로 등록되는지 검사. 등록 지점이 없으면 경고.
   - 같은 그룹에서 앞 규칙이 조건 없이 항상 성립해 뒤 규칙에 도달할 수 없으면 경고.
4. BehaviorData 감사 에디터 메서드를 만든다. 다음을 경고로 보고한다.
   - BT가 null이거나 가리키는 에셋이 없음(깨진 참조)
   - Assets/10.Datas/AI/BehaviorTree/ 의 Test 경로 또는 Test 이름 BT를 가리킴
   - 지상 BehaviorData가 actorKind=Flying BT를 가리키거나 그 반대
   - 비행 컨트롤러 프리팹의 BehaviorData BT 칸처럼 읽히지 않는 연결은 오류가 아니라 "미사용 칸"으로 구분해 보고
   - 어떤 BehaviorData에도 연결되지 않은 BT 목록(정보)
   감사 결과는 사람이 읽는 목록으로 출력한다. 사람이 반복해 여는 검증이므로 툴 런처 등록 대상이며 UPlaygroundTool ID를 붙인다. 아니면 이유를 인계에 적는다.
5. 현행 Rules JSON 27종(SourceJson 전체)에 새 검사를 돌린다. 오류가 나오면 검사를 완화하지 말고 JSON을 고친다. 고친 JSON과 이유를 P1_VALIDATOR.md에 적는다. 이 단계에서는 BT 연결 변경은 하지 않는다(2단계).
6. EditMode 테스트를 추가한다: 잘못된 SelectedIntent, ExecuteAttack+cooldownId, 등록되지 않은 CooldownReady ID 각 1종은 실패 또는 경고, 정상 JSON은 통과.

검증: Unity 컴파일 오류 0, 신규 EditMode 테스트 통과, 현행 JSON 27종 오류 0. 감사 결과 전체(연결 현황 표)를 P1_VALIDATOR.md에 남긴다. 특히 BehaviorData_humanoid의 BT 참조(GUID 547eec6e…)가 실제로 깨졌는지 확인해 결과를 적는다.
최종 보고: 변경 파일, 검사 목록, 테스트 결과(실행 개수·통과/실패/미수행), 감사 결과 요약, 갱신한 위키·상태 링크.
```

## 2단계 — BT 연결 정상화 (설계 P0)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 먼저 읽고 따른다. 기준 설계: 몬스터 BT 행동 개선 설계 §P0. 1단계 인계(docs/work-status/monster-bt-improvement/)를 먼저 읽는다. 없거나 감사 도구가 없으면 1단계를 먼저 하라고 보고하고 멈춘다.

사용자 결정(이 블록을 보내기 전에 채운다. 비어 있으면 해당 항목은 후보만 제시하고 변경하지 않는다):
- 리치: <BT 이름 또는 "후보만">
- 거미 여왕: <...>
- 거미 졸개: <...>
- 드라이어드: <...>
- 해골 검사(skeleton_sword): <SkeletonSwordsman_Disciplined 연결 여부>
- 파티 캐릭터 몬스터판 BehaviorData 10개: <무기별 Humanoid_* 연결 여부 또는 "변경 안 함">

해야 할 일:
1. 시작 시 1단계 감사를 실행해 현재 연결 표를 P0_ASSIGNMENT.md에 기록한다.
2. Test/EnemyBehavior_Test_AggressiveGroundMelee_Tuned를 정식 이름(예: EnemyBehavior_GroundMelee_Aggressive)으로 승격한다.
   - 생성 에셋은 AssetDatabase.MoveAsset으로 옮겨 GUID를 보존한다. 이동 후 이 GUID를 참조하는 BehaviorData 13개가 여전히 같은 에셋을 가리키는지 재대조한다.
   - SourceJson의 JSON도 함께 이동·개명하고 JSON의 id와 Generated 에셋 이름이 일치하는지, 재import가 기존 에셋을 덮어쓰는지 새 에셋을 만드는지 먼저 확인한다. 새 에셋을 만들면 GUID가 바뀌므로 중단하고 방법을 다시 정한다.
   - BT를 삭제하지 않는다.
3. 사용자 결정에 따라 BehaviorData의 BT 참조를 바꾼다. 바꾸기 전에 대상 BT가 그 몬스터의 AbilitySet 공격 사거리·역할(Skill/Signature 등)과 맞는지 확인한다. 안 맞으면 변경하지 말고 불일치를 인계에 적는다(공격 데이터가 필요한 항목은 3단계 이후 작업으로 남긴다).
4. 비행 몬스터(그리핀)는 EnemyFlyingAIController 컴포넌트에 직렬화된 BT를 쓰므로 BehaviorData의 BT 칸을 바꿔도 효과가 없다. 건드리지 않고, 읽히지 않는 칸은 인계에만 적는다.
5. 연결되지 않은 BT(RangedKiter 2종, Example, Organic, 나머지 Test 3종 등)는 변경하지 않는다. 연결 결과를 반영한 보관·삭제 제안만 P0_ASSIGNMENT.md에 적는다.
6. 변경 후 1단계 감사를 다시 실행해 Test BT를 가리키는 BehaviorData 0, 깨진 참조 0(또는 남은 항목과 이유)을 확인한다.

검증: Unity 컴파일 오류 0, 감사 결과 전후 비교, Missing 참조 0. 이동한 에셋의 GUID 보존 증거(이동 전·후 값)를 적는다.
사용자 테스트 명세를 제공한다: 변경한 몬스터마다 씬·몬스터·확인 항목(공격 거리, 접근 방식, 플레이어 반응). 체감 평가는 사용자 테스트 대기로 남긴다.
최종 보고: 변경 전후 연결 표, 이동 에셋 GUID 증거, 변경하지 않은 항목과 이유, 사용자 테스트 명세, 갱신한 위키·상태 링크.
```

## 3단계 — 플레이어 읽기 조건 노드 (설계 P2)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 먼저 읽고 따른다. 기준 설계: 몬스터 BT 행동 개선 설계 §P2, 목표 가이드 §3(조건 노드 설계 원칙). 1단계 인계를 읽는다.

해야 할 일:
1. 현재 코드를 먼저 조사해 인계에 적는다: EnemyTacticalMemory가 읽는 플레이어 상태, 플레이어 차지·시전을 나타내는 상태/태그(ActorStateTag·상태 ID), 가드 브레이크 이벤트 경로, MonsterGroupController의 슬롯 조회 가능 여부, 조건 노드 등록 위치(MonsterBehaviorJsonNodeKeys, ConditionNodeDefinitions, 에디터 표시명 레지스트리 BehaviorTreeDisplayNameRegistry, BehaviorTreeEditorRegistry.json).
2. 아래 조건을 구현한다. 값 계산은 EnemyTacticalMemory(또는 소유 컴포넌트)가 하고 조건 노드는 읽기만 한다. 조건 키는 목표 가이드 이름을 그대로 쓴다.
   - IsTargetCastingOrCharging: 플레이어 차지·시전·긴 선딜 중. 상태 문자열 비교를 새로 늘리지 않고 상태 태그를 쓴다. 적절한 태그가 없으면 태그 추가가 적절한지 판단해 근거와 함께 처리한다.
   - IsTargetBehind: 타겟이 몬스터 후방 각도 안. 각도는 블랙보드 rearAngle(기본값은 SO 또는 블랙보드 기본값, 코드 상수 금지).
   - RecentlyGuardBroken: 최근 가드 브레이크를 당함. 유지 시간은 데이터로 둔다.
   - HasAttackSlot: 부작용 없는 슬롯 조회. 점유는 기존 RequestAttackSlot 액션이 맡는다.
   - IsPlayerLowHealth: 플레이어 체력 비율 이하. 기본은 사용하지 않고 보스·정예에서만 쓰도록 기본 JSON에는 넣지 않는다.
3. importer(NodeFactory의 ConditionNodeDefinitions, 키 상수, 검증)와 에디터 한글 표시명을 등록한다.
4. IntentEvaluationContext에는 스코어러 점수에 반영할 값(차지 중)만 추가한다. 추가하면 IntentConditionId·IntentConditionEvaluator·IW SO의 선택 가능한 조건 목록과 커스텀 인스펙터에도 같이 반영한다. 규칙 전용 값은 컨텍스트에 넣지 않는다.
5. 핫패스 규칙을 지킨다. 필요한 참조는 SetPlayerTarget에서 캐시한다.
6. 사용 예시로 대표 JSON 1~2종에 새 조건을 쓰는 규칙을 추가하고 1단계 검증기를 통과시킨다. 모든 JSON을 한꺼번에 바꾸지 않는다(원형 분화는 4단계).
7. EditMode 테스트: 후방 각도 경계값, 가드 브레이크 기록 만료, 슬롯 조회가 점유하지 않음.

검증: Unity 컴파일 오류 0, 신규·기존 관련 EditMode 통과, 1단계 검증기 통과, BT 에디터에서 새 노드 표시명 확인(스크린샷 근거). Play Mode 체감은 사용자 테스트 대기로 둔다.
사용자 테스트 명세: 강공격 차지 시 반응, 몬스터 후방으로 이동했을 때 반응, 그룹 슬롯이 가득 찼을 때의 행동.
최종 보고: 구현 항목과 위치, 테스트 결과, 성능 근거, 사용자 테스트 명세, 갱신한 위키·상태 링크.
```

## 3-A단계 — 전투 구역 유지·반복 회피 제한 (설계 P2a)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙과 기준 설계 §P2a를 먼저 읽는다. 1~3단계 인계와 실제 코드를 대조한다. 1단계 검증기 또는 필수 인계가 없으면 누락을 보고하고 선행 단계부터 진행하도록 멈춘다.

목표: 몬스터가 자기 구역을 벗어나지 않게 하고, 궁수가 구르기만 반복해 플레이어의 추적 공격 기회를 없애는 문제를 해결한다. 원형별 분화·플레이어 이동/락온·그룹 역할 변경과 섞지 않는다.

해야 할 일:
1. 실제 궁수의 씬·스폰/풀링 경로·BehaviorData↔BT↔AbilitySet↔MotionSet을 찾는다. EnemyAIController의 SpawnPosition 수명, Chase/Retreat/KeepDistance/Circle 이동, EnemyDodgeState의 방향/벽 충돌/상태 전체 무적 소비, BT와 직접 상태 요청의 회피 진입·쿨다운을 조사한다. 반복 원인을 추측하지 말고 근거와 변경 전 관찰을 P2A_TERRITORY_EVASION.md에 남긴다.
2. 기존 귀속·복귀 기능을 먼저 찾고 재사용한다. 고정 구역 기준점·허용 반경·경계 여유·복귀 진입/해제 조건과 타겟이 구역 밖에 있을 때 정책을 정한다. 풀링 재스폰·순찰·귀속 변경 시 갱신을 명시하고 플레이어/회피 종료 위치로 구역을 따라 옮기지 않는다.
3. 자발적 전투 이동이 같은 구역 제약을 소비하게 한다. 회피는 경로와 예상 종료점이 유효한 안쪽/접선 후보를 우선하고 벽 충돌 후에도 재검사한다. 유효 후보가 없으면 요청 실패 후 가능한 대안을 선택한다. 외부 밀침으로 이탈하면 복귀를 우선하되 사망/잡힘/피격을 무시하지 않는다. 순간이동·벽 관통·경계 진동·무한 회복을 만들지 않는다. 비행 경로 적용 여부와 미지원은 명시한다.
4. 실제 위협에 따른 회피 선택, 성공한 실행에만 적용하는 재사용 제한, 연속 회피 상한/회복을 구현한다. BT 외 직접 요청도 공통 실행 허용 검사를 거친다. 반복 패널티만으로 해결했다고 보지 않는다.
5. 회피 후 재구르기/즉시 후퇴로 지워지지 않는 공략 구간을 확보한다. 무적 구간·이동·모션 후딜·캔슬 소비를 대조하고 기존 MotionSet/실행 데이터 계약에 맞춰 조정한다. 궁수를 항상 정지시키는 방식으로 해결하지 않는다.
6. 수치는 Assets/10.Datas/의 기존 적절한 SO를 우선 사용한다. 이동 거리·공격 사거리·모션을 측정해 초안을 정하고 근거를 기록한다. 코드 상수·몬스터 이름 분기·동일 값 중복 저작은 금지한다. 새 API/필드 이름은 현행 구조를 조사한 뒤 정한다.
7. 경계 안/위/밖, 회피 경로·벽 충돌 후 후보, 넉백 후 복귀 임계값, 재스폰 기준점, 성공/실패 요청의 쿨다운·연속 횟수·회복 제한과 직접 요청 우회 방지에 관련 EditMode 테스트를 추가한다. 대표 궁수 BT만 필요 범위에서 수정하고 P1 검증기를 실행한다. GUID를 보존한다.

검증: Unity 컴파일 오류 0, 관련 EditMode 실행 개수/통과/실패, P1 검사 결과. 수행하지 못한 런타임 관찰은 미수행으로 적는다. 변경 전후 동일 조건에서 귀속점 최대 거리·연속 구르기 횟수·회피 간격·회피 후 공격 가능 구간을 비교한다. 수치가 좋아졌다는 이유로 체감 합격으로 처리하지 않는다.
사용자 테스트 명세: 실제 씬/궁수/입력 장치/적용 SO 값/관찰 시간/경계 허용 오차를 적는다. 평지 지속 접근과 추적 공격, 벽·절벽·구역 경계 압박, 타겟 경계 왕복, 넉백 이탈 후 복귀, 근접·비행 회귀를 항목별로 제공한다. 알려진 증상은 사용자 보고이며 이번 수정본의 실패/통과와 구분한다. 회신 형식은 항목 ID / 통과·실패·미수행 / 실제 결과 / 영상 또는 로그다.
최종 보고: 원인과 소비 경로, 변경 코드·SO·BT, 측정/자동 검사, 사용자 테스트 대기 항목, 갱신한 위키·상태 링크. 구역 이탈·무한 회피·추적 공격 기회 문제가 남으면 4단계로 넘어가지 않는다.
```

## 4단계 — 원형별 BT 분화 (설계 P3)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 먼저 읽고 따른다. 기준 설계: 몬스터 BT 행동 개선 설계 §P3, Intent 가중치 외부화 설계 §5.1(Archetype 표). 1~3단계와 3-A 인계를 읽는다. 3-A 사용자 검수가 없거나 구역 이탈·반복 회피·추적 공격 기회 문제가 남으면 보고하고 멈춘다.

사용자가 정한 범위(이 블록을 보내기 전에 채운다): 이번에 분화할 원형 <Bruiser / Skirmisher / Guardian / RangedCaster 중 선택>, 대표 몬스터 <...>.

해야 할 일:
1. 선택한 원형마다 대표 몬스터의 AbilitySet(공격 사거리·역할·선딜·후딜)을 조사해 원형이 요구하는 공격이 있는지 확인한다. 없는 공격은 BT에 규칙을 만들지 말고 "공격 데이터 필요" 항목으로 P3_ARCHETYPES.md에 적고 해당 규칙은 보류로 표시한다.
2. 원형별로 정체성 그룹 하나를 추가·교체한다(예: Skirmisher의 "측면 진입 → 짧은 연타 → 이탈", Guardian의 "가드 후 반격, 가드 브레이크 후 거리 벌림", RangedCaster의 "선호 거리 유지, 차지 중 견제"). 3단계의 새 조건을 쓸 수 있으면 쓴다.
3. 같은 원형 안의 개체 차이는 블랙보드 수치로 낸다. JSON 복사본을 늘리지 않는다. 필요한 IW 프로필(예: IW_Skirmisher)이 없으면 기존 IW 자산을 복제해 만들고 LegacyIntentScoring과의 회귀 동등성을 깨지 않는다.
4. 구조가 너무 단순한 BT(늑대, Lucia)는 이번 범위에 선택됐을 때만 후퇴·측면 분기를 최소로 추가한다.
5. BT JSON은 generate-bt-json 스킬로 작성하고 import 후 1단계 검증기를 통과시킨다. Generated 에셋 GUID를 보존한다.
6. BehaviorData의 BT 연결은 2단계 결정과 충돌하지 않게 한다. 충돌하면 중단하고 보고한다.

검증: Unity 컴파일 오류 0, 검증기 통과, 원형별 JSON에 정체성 그룹이 존재하는지 확인, Intent 타임라인으로 대표 몬스터의 Intent 분포가 원형에 맞는지 확인(기록 경로를 인계에 남긴다). 체감은 사용자 테스트 대기.
사용자 테스트 명세: 원형별 1종씩 상대할 씬·몬스터·확인 항목. TTK 변화가 있으면 밸런스 도구로 측정한 값을 적는다.
최종 보고: 원형별 변경 요약, 보류된 항목과 필요한 공격 데이터, 테스트 결과, 갱신한 위키·상태 링크.
```

## 5단계 — 의도 선택 안정화 (설계 P4)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 먼저 읽고 따른다. 기준 설계: 몬스터 Intent 가중치 외부화 설계 §4.2(Phase E)와 몬스터 BT 행동 개선 설계 §P4. 4단계 사용자 테스트 결과를 먼저 확인한다. 정체성 문제가 남아 있으면 안정화보다 먼저 해결해야 하므로 보고하고 멈춘다.

해야 할 일:
1. 현재 선택 로직(EnemyCombatDecisionEvaluator, ApplyLastIntentPenalty, SelectWeightedTopIntent 위치)을 조사해 인계에 적는다.
2. Intent 외부화 설계 §4.2의 미정 항목(유지 보너스, 전환 비용, 최소 유지 시간)을 해당 설계 문서에서 먼저 확정한다. 수치는 EnemyIntentWeightsSO 쪽 SO 필드로 두고 코드 상수로 하지 않는다. 보정은 점수 계산 이후 SelectWeightedTopIntent 직전에 적용한다.
3. 피격, 포이즈 브레이크, 타겟 상실은 최소 유지 시간의 예외로 즉시 재선택한다.
4. 블랙보드 키(DecisionIntentLockedUntil 등)를 추가하면 EnemyBlackboardKeys와 BT 에디터 블랙보드 표시, 커스텀 인스펙터에도 같이 반영한다.
5. 안정화를 끈 상태에서 LegacyIntentScoring과의 회귀 동등성 테스트가 계속 통과해야 한다.
6. 기존 IW 자산 4종의 인스펙터 값을 안정화 기본값으로 갱신하되 의도하지 않은 자동 재직렬화는 원복한다.

검증: Unity 컴파일 오류 0, 회귀 동등성 EditMode 통과, 신규 테스트(최소 유지 시간 내 재선택 억제, 예외 즉시 재선택, 전환 비용). Intent 타임라인으로 안정화 전후 Intent 전환 횟수를 비교해 기록 경로를 남긴다. 체감은 사용자 테스트 대기.
최종 보고: 확정한 수치와 근거, 변경 파일, 테스트 결과, 전환 횟수 비교, 갱신한 위키·상태 링크.
```

## 6단계 — 그룹 역할 (설계 P5)

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 먼저 읽고 따른다. 기준 설계: 몬스터 BT 행동 개선 설계 §P5, 다인 전투 조작감 설계(Assets/docs/Complete/MULTI_ENEMY_CONTROL_FEEL_DESIGN.md). 3·4단계 인계를 읽는다.

이 단계는 단독으로 적용하고 평가한다. 다른 전투 조작감 변경과 한 번에 넣지 않는다.

해야 할 일:
1. MonsterGroupController의 슬롯·브리더·대형 구조를 조사해 인계에 적는다. 이미 있는 것은 다시 만들지 않는다.
2. 역할(공격/견제/지원)을 MonsterGroupController가 정해 각 몬스터 블랙보드 groupRole에 쓰고, BT는 BlackboardCompare로 읽는다. 새 조건 노드를 만들지 않는다.
   - 공격: 슬롯 점유 몬스터
   - 견제: 슬롯을 못 얻은 근접 몬스터(원이동, 위협 자세)
   - 지원: 원거리 몬스터(거리 유지, 플레이어 후딜 개입)
3. 그룹에 속하지 않은 전투 몬스터는 슬롯 제한이 없다(TryRequestAttackSlot이 항상 true). 이 단계에서는 동작을 바꾸지 않는다. 배치 데이터에서 그룹 없이 놓인 전투 몬스터를 조사해 목록만 만들고 제안한다.
4. 역할 규칙을 대표 BT에 추가한다(전 BT 일괄 변경 금지). 1단계 검증기를 통과시킨다.

검증: Unity 컴파일 오류 0, 3·5마리 그룹 시뮬레이션에서 동시 공격 수가 슬롯 상한을 넘지 않는 EditMode/PlayMode 테스트. 체감은 사용자 테스트 대기.
사용자 테스트 명세: 3마리 이상 교전에서 억울한 연속 피격이 없는지, 견제 역할의 몬스터가 어색하게 서성이지 않는지.
최종 보고: 변경 파일, 역할 배정 규칙, 테스트 결과, 그룹 없는 몬스터 목록과 제안, 갱신한 위키·상태 링크.
```

## 재개·재검증 프롬프트

세션이 중간에 끝났을 때:

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
Assets/docs/requests/MONSTER_BT_IMPROVEMENT_TASK_PROMPTS.md의 공통 규칙을 읽는다.
docs/work-status/monster-bt-improvement/STATUS.md를 읽고 마지막 완료 항목과 미완료 항목을 확인한다. 미커밋 변경과 생성·이동된 에셋의 GUID를 현재 상태와 대조한 뒤, 진행 중이던 <N>단계를 이어서 끝낸다. 이미 끝난 항목을 다시 하지 않는다. 인계 문서에 이어받은 지점과 이어서 한 일을 적는다.
```

사용자 테스트에서 실패했을 때:

```text
작업 루트 C:/UsingProject/UnityProject/UPlayground.
공통 규칙을 읽고 docs/work-status/monster-bt-improvement/STATUS.md를 읽는다.
사용자 테스트 결과: <실패한 몬스터·씬·증상>.
Intent 타임라인과 BehaviorData↔BT 연결을 확인해 원인을 BT 규칙·스코어러·공격 데이터·연결 오류·이동/회피 실행·귀속 구역 중 어느 층인지 분류한다. 구역 이탈/반복 회피는 귀속점과 최대 거리·회피 성공 횟수·재사용 간격을 기록한다. 추측하지 말고 근거(타임라인 기록, 블랙보드 값)를 남긴다. 해당 층만 고치고 같은 시나리오를 다시 검증한다. 다른 단계의 변경을 되돌리거나 확대하지 않는다. 결과를 STATUS.md의 알려진 실패 항목에 기록한다.
```
