# Behavior Tree Blackboard·몬스터 AI 고도화 스펙

> 작성일: 2026-07-26
>
> 상태: 설계 확정 전 TODO
>
> 대상: `UPlayGround.AI.BehaviorTree`, 몬스터 전투 의사결정, Ability 연동 데이터
>
> 선행 기준:
> - `Assets/docs/Complete/GAMEPLAY_TAG_SYSTEM_GUIDE.md`
> - `Assets/docs/Complete/ATTRIBUTE_ID_DATA_MIGRATION_SPEC.md`
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_PROGRESS.md`
> - `Assets/docs/Complete/MONSTER_AI_BT_APPLICATION_PLAN_GUIDE.md`

---

## 1. 결론

현재 Behavior Tree와 몬스터 Intent 시스템은 기본 동작, 시각 저작, 런타임
관측까지 갖췄지만, 핵심 식별자와 전투 데이터의 권위가 최신 Ability 시스템과
맞지 않는다.

다음 두 축을 하나의 마이그레이션으로 진행한다.

1. Blackboard Key를 자유 문자열에서
   `BlackboardKeyRegistrySO → BlackboardKeyReference → BlackboardKeyHandle`
   구조로 전환한다.
2. 몬스터 전투 의사결정을
   `전략 Intent → Ability 후보 평가/점수화 → 기존 State·Motion 실행`
   3계층으로 재정렬한다.

```text
저작 데이터
├─ BlackboardKeyRegistrySO      Key 정본·stableId·타입·설명·별칭
├─ EnemyCombatStrategySO        Intent 성향·역할·페이즈 전략
└─ AbilitySetSO                 몬스터가 실제 사용할 수 있는 Ability 정본
       └─ GameplayAbilitySO
            └─ UPlayGroundMotionAbilityPayloadSO
                 └─ AbilityAttackInfo / MotionReferenceSO

런타임
상황 Snapshot
    → Intent 평가
    → AbilitySet 후보 필터
    → Ability 전술 점수
    → TryPrepareAbility
    → Motion 해석
    → Commit
    → 기존 Enemy State / MotionSet / MotionEvent 실행
```

Blackboard Key에 `GameplayTag`를 직접 사용하지는 않는다. 두 시스템은 같은
안정 ID/Registry 패턴을 사용하지만 의미가 다르다.

- GameplayTag: 액터가 보유한 상태·분류·요구 조건을 계층 질의하는 값
- Blackboard Key: 특정 타입의 런타임 값을 읽고 쓰는 메모리 주소
- Attribute: 수치 정의·집계·클램프·저장을 포함하는 게임플레이 수치

세 도메인을 같은 문자열 공간으로 합치면 `State.Stun`이라는 상태 태그와
`Target.Distance`라는 값 슬롯이 같은 질의 체계에 섞이고, Key 타입·기본값·
쓰기 권한을 표현하기 어렵다. 공용화 대상은 **레지스트리 인프라와 저작 UX**이지
도메인 자체가 아니다.

---

## 2. 현재 구현 상태

### 2.1 이미 갖춘 기반

현재 BT는 다음 기능을 보유한다.

- `BehaviorTreeAsset`과 Runner별 런타임 Clone
- Composite, Decorator, Condition, Action, Service, Subtree
- Self/Lower Priority Conditional Abort
- Blackboard 타입 필터 선택기와 키 일괄 Rename
- 구조·Subtree 순환·Blackboard 참조 검증
- 검색, 그룹, 미니맵, 복사/붙여넣기, Undo
- 브레이크포인트, Pause/Step, 실행 경로, Trace
- Intent Score Timeline과 Encounter Replay
- `EnemyCombatDecisionEvaluator`의 9종 Intent 점수
- 역할·페이즈·그룹 보정과 플레이어 행동 기억

따라서 새 BT 프레임워크를 만들거나 상태 머신을 제거하지 않는다.

### 2.2 Blackboard의 현재 문제

현재 `Blackboard`는 `Dictionary<string, BlackboardEntry>`를 지연 생성하며,
`BlackboardEntry._key`와 `BlackboardKeySelector._key`가 문자열을 저장한다.

| 문제 | 현재 영향 |
|---|---|
| Key 정본 없음 | 새 Key가 코드 상수, BT Asset, JSON 중 어디에서 생겨도 실행됨 |
| 오타가 새 Entry 생성 | `SetXxx`의 `GetOrCreate`가 오타 Key도 정상 Entry로 만든다 |
| Rename이 프로젝트 검색 범위에 의존 | 외부 JSON·세이브·미로딩 에셋의 과거 이름을 해석하지 못한다 |
| 타입이 참조마다 중복 | Selector의 `ExpectedType`과 실제 Entry 타입이 불일치할 수 있다 |
| 문자열 비교가 런타임 경로에 남음 | Key 조회와 Trace가 문자열 중심이다 |
| 접근 계약 없음 | 어떤 Service/Node가 어떤 Key를 읽고 쓰는지 정적으로 알기 어렵다 |
| 로컬/공용/입출력 구분 없음 | Subtree가 부모 Blackboard와 어떤 계약을 갖는지 불명확하다 |

### 2.3 몬스터 데이터의 현재 문제

현재 Intent/BT 데이터는 최신 GAS 전체 마이그레이션보다 먼저 만들어졌다.
그 결과 `EnemyBehaviorSO`, Blackboard 기본값, `BehaviorPhase`,
`AbilityAttackInfo`가 전투 판단에 필요한 정보를 부분적으로 중복 소유한다.

특히 현재 `EnemyCombat.GetAvailableAbilities`는 `AbilitySetSO` 멤버십을
“이 몬스터가 쓸 수 있는 것”으로 간주하고 `AbilityAttackInfo.aiSelectable`을
별도 게이트로 사용하지 않는다. 이는 프로젝트 규칙인
“몬스터 BT는 AbilitySet 안에서 `aiSelectable` Ability만 선택”과 충돌한다.

이 불일치는 **P0 결함**으로 취급한다.

또한 현재 선택 과정은 대략 다음과 같다.

```text
Intent 선택
→ BT가 AttackCategory 선택
→ AbilitySet의 모든 공격 후보 수집
→ 레벨/거리/조건/Category 필터
→ selectionWeight 가중 랜덤
→ Ability 활성화
```

이 구조에서는 Punish, Counter, Pressure 같은 상위 Intent가 실제 Ability의
전술적 성격과 직접 연결되지 않는다. BT의 `AttackCategory`가 중간 프록시가
되며, 최신 Ability의 Tag·Cost·Cooldown·Variant·Effect 상태를 선택 이유로
충분히 설명하지 못한다.

---

## 3. Blackboard Key 목표 모델

### 3.1 채택안

`AttributeRegistrySO`의 완료된 패턴을 Blackboard 도메인에 이식한다.

| 계층 | 타입 | 책임 |
|---|---|---|
| 데이터 정본 | `BlackboardKeyRegistrySO` | 모든 Key 정의와 stableId·alias 소유 |
| 저작 참조 | `BlackboardKeyReference` | 직렬화 가능한 검증 참조 |
| Core 값 ID | `BlackboardKeyId` | BT 런타임 경계의 경량 값 타입 |
| 런타임 핸들 | `BlackboardKeyHandle` | Registry 로드 후 정수 인덱스 |
| 해석 포트 | `IBlackboardKeyResolver` | 프로젝트 레지스트리와 런타임 경계 |

`GameplayTagRegistrySO`와 `AttributeRegistrySO`처럼 Registry 에셋 하나를
단일 원본으로 사용한다.

```text
Assets/Resources/BlackboardKeyRegistry.asset
```

Key마다 ScriptableObject 파일을 하나씩 만드는 방식은 채택하지 않는다.
몬스터 Key는 수가 많고 서로 함께 검증·검색·리네임되어야 하므로, 단일 Registry의
항목 모델이 에셋 폭증과 GUID 참조 노이즈를 줄인다.

### 3.2 권장 정의

아래는 목표 형태를 설명하기 위한 설계 예시다. 실제 구현 시 기존 Data/Core
asmdef 경계에 맞춰 파일 위치를 확정한다.

```csharp
[Serializable]
public sealed class BlackboardKeyDefinition
{
    public string keyName;              // "AI.Decision.SelectedIntent"
    public string stableId;             // 생성 후 변경 금지
    public List<string> aliases;         // 과거 이름
    public string displayName;           // "선택된 전투 의도"
    public string description;
    public string category;              // "AI/Decision"
    public BlackboardValueType valueType;
    public BlackboardKeyScope scope;
    public BlackboardWritePolicy writePolicy;
    public bool required;
}
```

`stableId`는 이름과 분리한다. `keyName` 변경 시 과거 이름은 `aliases`에
보존하고, 저장된 참조를 정규 이름으로 승격한다.

### 3.3 Reference와 Handle

```text
직렬화
BlackboardKeyReference
├─ stableId
└─ cachedName      사람이 YAML/diff/오류를 읽기 위한 캐시

초기화
stableId 또는 alias/name
→ Registry Resolve
→ BlackboardKeyHandle(int)

런타임
BlackboardKeyHandle
→ Entry 배열/Dictionary 조회
```

직렬화 정본은 `stableId`다. `cachedName`은 에디터 표시와 diff 가독성을 위한
비권위 캐시로만 사용한다.

Attribute는 기존 직렬화 호환 때문에 문자열 ID를 유지했지만, Blackboard는
별도 세이브 포맷의 장기 호환 부담이 상대적으로 작다. 따라서 마이그레이션 후
새 Reference는 stableId를 직접 직렬화하는 방향을 우선한다.

단, 기존 BT Asset과 JSON을 한 번에 깨뜨리지 않도록 마이그레이션 기간에는
다음 해석 순서를 허용한다.

```text
1. stableId 정확 일치
2. keyName 정확 일치
3. alias 정확 일치
4. 실패 — 이름 유사도나 임의 생성으로 폴백하지 않음
```

### 3.4 Scope와 Subtree 계약

```csharp
public enum BlackboardKeyScope
{
    TreeLocal,
    SubtreeInput,
    SubtreeOutput,
    AgentRuntime,
    SharedGroup,
    DebugOnly,
}
```

- `TreeLocal`: 해당 런타임 트리 내부 임시 값
- `SubtreeInput`: 부모가 호출 시 제공해야 하는 값
- `SubtreeOutput`: Subtree 종료 후 부모에 반환하는 값
- `AgentRuntime`: Target, Distance처럼 Runner/Owner와 결합된 값
- `SharedGroup`: 그룹 전술 메모리처럼 여러 몬스터가 공유하는 읽기 모델
- `DebugOnly`: 점수·Reason처럼 Player Build에서 보존 정책을 달리할 수 있는 값

Subtree는 같은 문자열 이름이 우연히 일치해서 값을 공유하지 않는다.
명시적인 `BlackboardBinding`으로 입출력을 연결한다.

```text
Parent Key Handle
→ Subtree Parameter stableId
→ In / Out / InOut
```

### 3.5 타입과 쓰기 안전성

Key의 `valueType`은 Registry 정의가 단일 권위를 가진다.
`BlackboardKeySelector`가 각 필드에서 `ExpectedType`을 중복 저장하는 구조는
점진적으로 제거한다.

목표 API:

```csharp
bool TryGetFloat(BlackboardKeyReference key, out float value);
bool TryGetFloat(BlackboardKeyHandle key, out float value);
bool TrySetFloat(BlackboardKeyHandle key, float value, BlackboardWriteSource source);
```

다음 경우는 Entry 자동 생성 대신 실패한다.

- Registry 미등록 Key
- Key 정의 타입과 API 타입 불일치
- ReadOnly/외부 소유 Key 쓰기
- Subtree Output을 입력 전에 읽음
- 필수 Input 바인딩 누락

개발 빌드에서는 오류와 Trace를 남기고, Release에서는 실패 반환 정책을 사용한다.

### 3.6 GameplayTag·Attribute와의 관계

Blackboard에 상태를 복제하지 않는 것이 원칙이다.

| 데이터 | 단일 권위 | Blackboard 처리 |
|---|---|---|
| `State.Stun`, `State.Invincible` | ASC GameplayTag | 필요 시 Reader/Query 결과만 캐시 |
| `Vital.Health`, `Vital.Poise` | ASC Attribute | Snapshot 또는 Reader로 조회 |
| Ability Cooldown | ASC Cooldown Store | 준비 여부/남은 시간 Snapshot |
| Target Transform | Detection/Context | AgentRuntime Key |
| 선택된 Intent | AI Decision Session | TreeLocal Key |
| Intent Score | AI Decision Session | DebugOnly Key |
| 그룹 공격 슬롯 | MonsterGroupController | SharedGroup Reader 결과 |

GameplayTag나 Attribute 값을 Blackboard 기본값으로 복제하고 양쪽에서 갱신하는
이중 권위는 금지한다.

---

## 4. Registry와 에디터 도구

### 4.1 Registry 항목 예시

```text
AI.Target.Actor                 Object     AgentRuntime
AI.Target.Distance              Float      AgentRuntime
AI.Target.HasLineOfSight        Bool       AgentRuntime

AI.Decision.SelectedIntent      String     TreeLocal
AI.Decision.LastIntent          String     TreeLocal
AI.Decision.ConsecutiveCount    Int        TreeLocal
AI.Decision.Score.Attack        Float      DebugOnly
AI.Decision.Score.Punish        Float      DebugOnly

AI.Ability.Selected             Object     TreeLocal
AI.Ability.LastFailureReason    String     DebugOnly
AI.Ability.CommitmentUntil      Float      TreeLocal

AI.Group.HasAttackSlot          Bool       SharedGroup
AI.Group.FormationSlotIndex     Int        SharedGroup
```

초기에는 기존 `EnemyBlackboardKeys.generated.cs`가 가진 이름과 타입을
Registry로 정확히 옮긴다. 이름 정리와 계층 재편은 두 번째 마이그레이션으로
분리해 참조 변경과 의미 변경을 한 번에 섞지 않는다.

### 4.2 저작 UX

- Registry 기반 검색·계층형 선택
- 타입 필터는 Registry의 `valueType`으로 수행
- 표시명, raw keyName, stableId, 타입, Scope를 함께 표시
- 미등록/별칭/폐기 예정 Key 상태 표시
- Key 선택 시 Read/Write 사용처 수 표시
- Rename은 alias 보존을 기본값으로 사용
- 삭제 전 프로젝트 전체 사용처와 JSON 사용처 확인
- Key 타입 변경은 일반 편집이 아니라 전용 마이그레이션으로만 허용

### 4.3 검증기

`BlackboardKeyRegistryBuildValidator`는 다음을 검사한다.

1. Registry 에셋 정확히 1개
2. 빈 `stableId`, `keyName`, 중복 ID/이름/alias 충돌 0
3. 타입·Scope 조합 유효성
4. 모든 BT Asset의 Reference 해석 성공
5. 노드 API 기대 타입과 Registry 타입 일치
6. Subtree 필수 Input/Output 바인딩 완전성
7. JSON의 미등록 Key와 타입 불일치
8. 손 작성 Well-known Key 슬롯의 Registry 등록 일치
9. 런타임 자동 생성 Key 0

### 4.4 공용 Registry 인프라

GameplayTag, Attribute, Blackboard Key가 모두 다음 기능을 요구한다.

- stableId/이름/alias 인덱스
- AdvancedDropdown
- Find References
- 안전 Rename
- 빌드 전 무결성 검증
- 런타임 Intern Table

세 시스템의 데이터 타입은 분리하되, 반복되는 에디터·인덱싱 코드는
`UPlayGround.Data` 또는 Editor 전용 공용 유틸리티로 일반화한다.

---

## 5. 몬스터 행동 목표 아키텍처

### 5.1 책임 분리

```text
Perception / Memory / ASC
    │
    ▼
EnemyDecisionSnapshot
    │  사실 수집: 거리, LOS, 플레이어 상태, 내 Tag/Attribute,
    │  Ability 준비 상태, 그룹 슬롯, 최근 결과
    ▼
EnemyIntentEvaluator
    │  목표 선택: Attack, Punish, Counter, Pressure,
    │  Chase, Retreat, KeepDistance, Defend, Recover
    ▼
EnemyAbilitySelector
    │  AbilitySet 안의 실제 실행 후보를 필터·점수화
    ▼
EnemyActionRequest
    │  선택 결과와 실패 이유를 실행 계층에 전달
    ▼
Enemy State / EnemyCombat / ASC / MotionSet
```

- BT는 전략 흐름과 예외 우선순위를 시각적으로 구성한다.
- Intent Evaluator는 순수 계산으로 테스트 가능해야 한다.
- Ability Selector는 최신 Ability 데이터만 읽는다.
- State/MotionEvent는 실제 이동·애니메이션·히트 타이밍을 계속 소유한다.

### 5.2 AbilitySet 단일 권위

몬스터 공격 후보는 반드시 다음 순서로 결정한다.

```text
AbilitySetSO.GetRuntimeAbilities()
→ aiSelectable == true
→ ASC EvaluateAbility
   - required/blocked GameplayTag
   - Cost
   - Cooldown
   - Target/grounded
   - Variant 선택
→ Payload/AbilityAttackInfo 해석
   - HitPhase 존재
   - 레벨 해금
   - 기존 SkillCondition
   - 공격 거리/공중/급강하 조건
→ 현재 Intent와 전술 적합도 점수
→ 후보 선택
→ TryPrepareAbility
→ MotionReferenceSO.Resolve(WeaponType.NoWeapon)
→ Commit
→ 실행
```

`EnemyBehaviorSO`와 BT Blackboard에 다음 값을 다시 만들지 않는다.

- Ability Cost
- Ability Cooldown
- required/blocked/granted Tag
- MotionReference 또는 AnimKey
- HitPhase/공격 범위의 복제
- Variant 선택 조건
- Ability가 실제 활성화 가능한지 여부

### 5.3 AI 전술 메타데이터

Ability 선택에는 실행 데이터 외에 “어떤 상황에 어울리는가”가 필요하다.
이 정보는 기존 실행 값을 복제하지 않는 범위에서 Ability 쪽에 둔다.

권장 형태:

```csharp
[Serializable]
public sealed class AIAbilityPolicy
{
    public bool aiSelectable;
    public List<GameplayTag> tacticalTags;
    public float selectionWeight = 1f;
    public float repeatPenalty = 1f;
    public int maxConsecutiveUses;
}
```

예시 태그:

```text
AI.Ability.Intent.Attack
AI.Ability.Intent.Punish
AI.Ability.Intent.Counter
AI.Ability.Role.Opener
AI.Ability.Role.Finisher
AI.Ability.Role.Mobility
AI.Ability.Role.AntiGuard
AI.Ability.Role.AntiAir
AI.Ability.Commitment.High
```

이 태그는 Blackboard Key가 아니라 GameplayTag다. Ability의 분류·질의 조건이기
때문이다.

기존 `AbilityAttackInfo.aiSelectable`, `selectionWeight`,
`attackCategory`, `isAerialSkill`, `isDiveAttack`, `SkillCondition`과
겹치는 필드를 새로 추가하지 않는다. 먼저 기존 필드를 `AIAbilityPolicy`로
논리적으로 묶을지, 유지한 채 에디터 섹션만 통합할지 결정한다.

### 5.4 Ability 점수

Ability 선택 점수는 재현 가능한 순수 함수로 만든다.

```text
FinalScore =
    BaseSelectionWeight
  × IntentCompatibility
  × RangeFitness
  × TargetStateFitness
  × SelfStateFitness
  × GroupRoleFitness
  × RepetitionPenalty
  × PhaseBias
```

필터와 점수를 구분한다.

- 필터: 실행 불가능하면 후보에서 제외
- 점수: 실행 가능 후보 사이의 선호도

`ASC EvaluateAbility` 실패를 낮은 점수로 처리하지 않는다. Cost/Cooldown/Tag
조건을 통과하지 못한 Ability는 후보가 아니다.

선택 결과에는 다음 진단을 남긴다.

```text
Ability
├─ Eligible / Rejected
├─ ActivationResult
├─ Variant
├─ RangeFitness
├─ IntentFitness
├─ RepeatPenalty
├─ FinalScore
└─ RejectReason
```

### 5.5 선택 안정화

Intent와 Ability를 매 Tick 다시 뽑으면 행동이 흔들린다.

- 실행 중 Ability는 정상 종료/Abort까지 잠근다.
- Intent는 최소 유지 시간 또는 명확한 높은 우선순위 Abort에서만 바꾼다.
- Commit 전 실패는 다음 후보를 한정 횟수 재평가한다.
- Commit 후 실패는 비용/쿨다운 소유권과 Abort 정책을 ASC에 위임한다.
- 같은 Ability 반복은 최근 실행 이력으로 감점한다.
- Seed 기반 선택기를 주입해 테스트와 Replay에서 동일 선택을 재현한다.

---

## 6. `EnemyBehaviorSO`와 페이즈 데이터 재편

### 6.1 남길 데이터

`EnemyBehaviorSO`는 몬스터의 전략·이동 성향과 BT 조립점만 소유한다.

- BehaviorTree Asset
- AI 역할/Archetype
- Intent/Strategy Profile
- Patrol 반경·대기
- 개인 공간과 기본 포지셔닝 성향
- 그룹 역할
- 페이즈 Strategy Override

### 6.2 Ability로 이동하거나 제거할 데이터

실제 공격 선택·실행과 겹치는 값은 최신 Ability 경로를 권위로 한다.

| 현재 성격 | 목표 권위 |
|---|---|
| 공격 사용 가능 여부 | ASC `EvaluateAbility` |
| 공격 거리 | Payload `AbilityAttackInfo`/기존 SkillCondition |
| 기본 선택 가중치 | Ability AI Policy |
| 공격 Category | Ability AI Policy 또는 기존 `attackCategory` |
| 공중/급강하 여부 | `AbilityAttackInfo` |
| 비용·쿨다운 | `GameplayAbilitySO` |
| 모션 | Payload `baseInfo.motionRef` |
| Hit/Telegraph | `HitPhaseData` |

`EnemyBehaviorSO.optimalCombatDistance`는 공격 사거리 복제가 아니라
몬스터의 **포지셔닝 선호**로만 남긴다. 런타임에서는 현재 사용할 수 있는
Ability들의 유효 거리와 교차해 실제 선호 거리를 계산한다.

### 6.3 Strategy Profile

Intent Weight의 필드 폭증을 줄이기 위해 전략 프로필을 별도 에셋으로 둔다.

```text
EnemyCombatStrategySO
├─ role
├─ IntentWeightProfile
├─ preferredAbilityTags
├─ blockedAbilityTags
├─ repetitionPolicy
├─ commitmentPolicy
└─ groupCoordinationPolicy
```

`BehaviorPhase`는 모든 수치를 다시 갖지 않고 Strategy Override를 참조한다.

```text
Phase Trigger
├─ Health Attribute 비율
├─ GameplayTag/Effect 상태
├─ 시간/사건
└─ Strategy Override
```

보스 페이즈의 실제 Ability 추가/제거가 필요하면 `AbilitySetSO`의 Base/Override
구성을 사용한다. BT나 BehaviorPhase가 Ability 목록을 별도로 소유하지 않는다.

---

## 7. 몬스터 행동 고도화 백로그

### P0 — 권위와 안정성 정리

1. `aiSelectable` 필수 게이트 복구
2. AbilitySet·Payload·MotionReference 해석 실패 이유 수집
3. Blackboard Key Registry/Reference 도입
4. BT Asset 전체 Key 마이그레이션
5. 기존 문자열 자동 생성 차단
6. Ability 선택과 BT Intent Trace 연결

### P1 — Ability-aware 전술 선택

1. `EnemyDecisionSnapshot` 도입
2. `EnemyAbilitySelector` 순수 계산 분리
3. Intent ↔ Ability tactical tag 적합도
4. Cost/Cooldown/Tag/Variant 실패 이유 노출
5. 반복 억제와 실행 Commitment
6. 결정 Seed 주입과 Replay 재현

### P2 — 플레이어 읽기 고도화

- 회피 방향과 회피 종료 시점 학습
- Guard/Parry 빈도와 성공률
- 회복·긴 후딜·공중 상태의 Punish Window
- 반복 공격 패턴의 Counter 선호
- 플레이어와의 실제 거리/명중 결과로 Ability별 효율 갱신

학습 결과는 영구 머신러닝 모델이 아니라 현재 Encounter 범위의 제한된
`EnemyTacticalMemory` 통계로 유지한다. 디버그 가능한 규칙 기반 점수 모델을
우선한다.

### P2 — 그룹 전술

- 공격 슬롯뿐 아니라 역할 슬롯: Frontline, Flank, Ranged, Support
- 같은 Ability/Intent 동시 선택 억제
- 아군 텔레그래프와 충돌하는 공격 회피
- 플레이어에게 숨 돌릴 창을 주는 Pressure Budget
- 그룹 집중 공격과 분산 공격 Strategy

그룹 Blackboard를 각 Runner가 직접 쓰지 않는다.
`MonsterGroupController/Memory`가 권위를 갖고 BT에는 읽기 Snapshot을 제공한다.

### P2 — 보스/페이즈

- 페이즈별 AbilitySet Override
- 페이즈 전환 중 Ability Commit 금지
- 강제 연출과 전투 BT의 명시적 소유권 전환
- Phase Entry Ability/Effect
- 체력 외 Tag·Break·시간·사건 기반 전환
- 중앙 보스와 BossAssist 데이터 경로 분리 유지

### P3 — 공간·협동 판단

- NavMesh 도달 가능성/경로 비용을 후보 평가에 포함
- 공격 위치 예약과 충돌 회피
- 원거리 Line of Fire
- 광역 공격의 예상 적중 수
- 공중 몬스터의 고도·착륙 지점·급강하 경로 점수화

EQS 전체 시스템을 먼저 만들지 않는다. 실제 Ability가 요구하는 공간 Query를
작은 `IEnemySpatialQuery` 포트로 추가하고, 반복 요구가 확인될 때 공용화한다.

---

## 8. 에디터·디버거 연계

이 문서는 BT 에디터 고도화 제안의 다음 항목을 몬스터 Ability 선택에 연결한다.

### Author

- Blackboard Key Registry 브라우저
- Key 사용처 Read/Write 그래프
- AbilitySet의 AI 후보 미리보기
- 선택된 Intent에 대해 후보 Ability 점수 시뮬레이션
- BT Key/Ability 누락 Quick Fix

### Debug

- Watch: Key stableId, 값, 마지막 작성자, 이전 값
- 현재 Intent와 유지 시간
- Ability 후보별 Eligible/Rejected/Score
- ASC ActivationResult
- 선택 Variant와 MotionReference
- Commit/Abort/Complete 원인

### Analyze

- Intent 분포
- Ability 선택/실행/명중률
- RejectReason 분포
- 반복 Abort와 긴 Running 노드
- 그룹 공격 슬롯 대기 시간
- Phase별 전투 리듬

Trace는 문자열 설명만 기록하지 않고 다음 구조를 공유한다.

```text
SessionId, AgentId, Tick,
NodeGuid, BlackboardKeyHandle,
Intent, AbilityGuid,
EventKind, Status, Cause, ScoreDelta
```

---

## 9. 마이그레이션 계획

### Phase 0 — 인벤토리와 기준선

- 모든 `EnemyBlackboardKeys` 이름·타입·사용처 수집
- BT Asset/JSON의 문자열 Key 목록 추출
- `EnemyBehaviorSO`와 AbilityAttackInfo 중복 필드 표 작성
- 몬스터별 AbilitySet과 `aiSelectable` 현황 추출
- 현재 Intent/Ability 선택 분포 Replay 확보
- Dryad 3건과 Training Dummy 1건의 미해결 MotionReference는 임의 연결 금지

완료 조건:

- Key 이름/타입 충돌과 미등록 사용처를 보고서로 재현 가능
- 변경 전 대표 몬스터 행동 Replay 보존

### Phase 1 — Blackboard Registry 기반

- `BlackboardKeyRegistrySO`와 단일 Registry 에셋
- stableId·alias·타입·Scope
- Reference/Handle/Resolver
- PropertyDrawer와 Registry Editor
- 빌드 검증기
- 기존 String API와 병존하는 Adapter

완료 조건:

- 새 Key 추가가 코드 생성·재컴파일을 요구하지 않음
- 미등록 Reference를 에디터와 빌드에서 차단

### Phase 2 — 데이터 마이그레이션

- 기존 Key를 동일 이름으로 Registry에 등록
- `BlackboardEntry`와 `BlackboardKeySelector`에 Reference 추가
- BT Asset/JSON GUID/path 정확 일치 기반 마이그레이션
- old string → stableId 변환
- 모든 Node/Service를 Handle API로 전환
- 문자열 필드 제거 전 사용처 0 확인

마이그레이션은 이름 유사도 폴백을 사용하지 않는다. 실패 시 에셋 단위로
Undo/백업 복구하고 일부 적용 상태를 성공으로 취급하지 않는다.

### Phase 3 — Ability 권위 복구

- `aiSelectable` 필수 필터
- ActivationResult/Variant/Payload/Motion 해석 실패 보고
- Ability 후보 선택을 `EnemyAbilitySelector`로 분리
- 기존 `SelectWeighted` 결과와 비교 테스트
- Intent와 Ability 선택 Trace 연결

완료 조건:

- `aiSelectable == false` Ability 실행 0
- 후보가 없을 때 비용/쿨다운을 소비하지 않음
- Motion 해석 실패 시 Commit 이전 Abort

### Phase 4 — Strategy/Ability 데이터 재정렬

- Ability tactical tag와 기존 AI 필드 중복 검토
- `EnemyCombatStrategySO`
- Phase Strategy Override
- `EnemyBehaviorSO`의 공격 실행 중복 데이터 제거
- BT JSON의 공격별 하드코딩을 공용 Intent 실행 구조로 축소

### Phase 5 — 수직 슬라이스

아래 순서로 한 종류씩 적용한다.

1. Training Dummy — 선택/실패 진단
2. 지상 근접 Normal — Attack/Chase/Retreat
3. 지상 원거리 — 거리/LOS/KeepDistance
4. 비행 몬스터 — Aerial/Dive/Land
5. Elite — 반복 억제·Counter·그룹 전술
6. Boss — Phase/Break/연출 소유권

각 단계는 기존 데이터 일괄 변경 전에 프리팹 1종과 AbilitySet 1개로 검증한다.

### Phase 6 — 레거시 제거

- 자유 문자열 Key API 제거 또는 Editor/Import 경계로 제한
- `EnemyBlackboardKeys.generated.cs` 제거 여부 결정
- Legacy Intent fallback 점수 제거
- `EnemyBehaviorSO`의 무사용 확률/공격 필드 제거
- 구 JSON 스키마 importer를 명시적 버전 변환기로 제한

---

## 10. 자동 테스트

### Blackboard

- stableId/name/alias Resolve
- 중복 stableId/name/alias 차단
- 타입 불일치 Read/Write 실패
- 미등록 Key 자동 생성 금지
- Rename 후 과거 alias 해석
- Clone 후 Handle/값 격리
- Subtree In/Out/InOut
- Registry 누락 빌드 차단
- JSON round-trip

### BT 런타임

- Sequence/Selector/Parallel 상태
- Self/LowerPriority/Both Abort
- Abort 시 Action과 Service 정리
- Disabled 노드 의미
- Subtree 순환
- Breakpoint/Step
- 고정 Seed Trace 재현

### Ability 연동

- `aiSelectable == false` 후보 제외
- required/blocked Tag
- Cost/Cooldown
- Variant 선택
- Payload/HitPhase/MotionReference 누락 집계
- 공격 거리와 공중/급강하 조건
- Prepare 후 Motion 실패 시 Commit 없음
- 같은 Seed/Snapshot에서 같은 선택
- Intent별 tactical tag 보정
- 반복 사용 감점

### 콘텐츠 통합

- 34개 AbilitySet 전체
- 482개 GameplayAbility와 493개 Variant/Payload 기준
- 모든 몬스터 AbilitySet의 AI 후보 최소 1개 여부
- 알려진 미해결 MotionReference 4건은 별도 명시 실패로 유지
- BT Asset 전체 Registry Key 해석
- 프리팹 Missing Script 0
- Play Mode 서비스 경고·예외 0

---

## 11. 완료 조건

- Blackboard 직렬화 참조의 자유 문자열 Key 0
- 미등록 Key 런타임 자동 생성 0
- Registry stableId/name/alias/type 오류 0
- Subtree 필수 바인딩 누락 0
- 몬스터가 `aiSelectable == false` Ability를 선택하는 경로 0
- Ability Cost/Cooldown/Tag/Motion 데이터의 BT·BehaviorSO 중복 0
- Ability 선택 실패 이유가 Trace에서 확인 가능
- Intent·Ability 선택이 Seed 기반 Replay로 재현 가능
- BT/Ability EditMode와 PlayMode 테스트 통과
- Unity 컴파일 오류 0
- Player Build 오류 0
- `Assets/10.Datas/`와 `Assets/03.Prefabs/` 자동 변경 diff 검토 완료

---

## 12. 구현하지 않을 것

- Blackboard Key에 GameplayTag 타입을 그대로 사용
- Key마다 개별 ScriptableObject 파일 생성
- BT가 Ability Cost/Cooldown/Tag 조건을 다시 계산
- BT가 Payload 밖의 MotionReference/AnimKey 폴백을 소유
- AbilitySet 바깥 Ability를 이름 검색으로 선택
- `aiSelectable` 실패를 임의 허용
- 콘텐츠 근거 없는 MotionReference 자동 매핑
- 상태 머신/KCC 실행 계층 제거
- 초기 단계의 DOTS/Burst 전환
- 디버그 불가능한 온라인 학습 AI 도입

---

## 13. 참고 자료

- [Unity Behavior 1.0 — 기능과 모듈형 Subgraph, 실시간 Debug](https://docs.unity3d.com/Packages/com.unity.behavior%401.0/manual/behavior-features.html)
- [Unity Behavior — Editor UI](https://docs.unity3d.com/Packages/com.unity.behavior%401.0/manual/user-interface.html)
- [Unity Graph Toolkit — Graph/runtime 모델 분리 지침](https://docs.unity3d.com/Packages/com.unity.graphtoolkit%400.2/manual/design-guidelines.html)
- [Unreal Engine StateTree Debugger — Trace/Timeline/사후 분석](https://dev.epicgames.com/documentation/en-us/unreal-engine/statetree-debugger-quick-start-guide)
- [Opsive Behavior Designer — Conditional Abort](https://opsive.com/support/documentation/behavior-designer/conditional-aborts/)
- [BehaviorTree.CPP — 타입 안전 Blackboard와 기록·재생](https://github.com/BehaviorTree/BehaviorTree.CPP)
- [BehaviorTree.CPP — Reactive/Asynchronous Behavior](https://www.behaviortree.dev/docs/guides/asynchronous_nodes/)
