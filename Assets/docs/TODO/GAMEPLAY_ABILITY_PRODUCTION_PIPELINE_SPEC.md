# Gameplay Ability 스킬 양산화 파이프라인 스펙

> 작성일: 2026-07-25  
> 대상 버전: Unity 6 (6000.0.60f1), Animancer Pro V8, URP  
> 분류: TODO 구현 스펙  
> 적용 범위: Ability 레시피, 생성 마법사, 안전 복제, Motion 연동, 검증, 샌드박스 실행, 밸런스 피드백  
> 선행 문서:
>
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_SPEC.md`
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_PROGRESS.md`
> - `Assets/docs/Complete/PASSIVE_ABILITY_SYSTEM_SPEC.md`
> - `Assets/docs/guide/BALANCE_DESIGNER_TOOL_GUIDE.md`
>
> 관련 코드:
>
> - `Assets/02.Scripts/Data/Ability/`
> - `Assets/02.Scripts/Ability/Core/`
> - `Assets/02.Scripts/Ability/UPlayGround/`
> - `Assets/02.Scripts/Data/Editor/Ability/`
> - `Assets/02.Scripts/GameActor/Editor/Ability/GasRuntimeDebuggerWindow.cs`
> - `Assets/02.Scripts/Tool/Editor/Balance/`
> - `Assets/Tests/EditMode/Ability/`
> - `Assets/Tests/PlayMode/Ability/`

## 구현 진행 상태 (2026-07-25)

### Phase 1~5 구현

다음 제작 파이프라인을 구현했다.

- Phase 1: 결정적 Plan, 경로·ID 충돌 검사, Ability/Payload 생성, Undo/롤백
- Phase 2: 초기 필수 레시피 6종, Player Slot/Combat Sequence/Additional 바인딩,
  Effect 신규 생성 또는 기존 공유
- Phase 3: 기본 Motion 분석, Collision HitPhase/Projectile/Telegraph 분류,
  부족한 HitPhase만 선택 적용, Ability/Payload 안전 Fork, 공유 역참조 Preview
- Phase 4: 안정 Issue 코드, TaskGraph Root/null/cycle/eventTag 검증,
  Issue 에셋 이동, 실제 `ActorAbilitySystem` Prepare→Commit→End Play Mode 샌드박스
- Phase 5: HitPhase 피해·Motion 길이·쿨다운 정적 요약, 수동 실측 비교,
  Encounter Replay 비교와 CSV, 전체 Ability Balance Snapshot 전후 비교

### 2026-07-25 사용성·공용화 재검토

- `GameplayAbilityEditorWindow`
  - 선택 메인 에셋 안전 복제
  - 같은 에셋 타입·같은 탭에 한정한 탭 값 복사/붙여넣기
  - 에셋 타입과 현재 탭 조합별 문맥 도움말
- Production Wizard, Dashboard, Runtime Sandbox의 화면 구현을 C# 기반
  UI Toolkit으로 전환
- 동일 타입 몬스터의 데이터 공용화는 새 Ability를 반복 생성하는 방식보다
  `MonsterActorProfileSO.abilitySet`을 타입 공용 Base Set으로 사용하고,
  특수 몬스터만 파생 `AbilitySetSO`에서 일부 Ability를 교체하는 방식으로
  재설계하기로 결정

2026-07-25 후속 구현에서 `AbilitySetSO.baseSet`과 Replace/Remove, 로컬 Add,
슬롯·차지·콤보 재정의 정책을 런타임에 반영했다. Wizard의 기본 작업 흐름도
`레시피 → 신규 Ability 생성`에서 다음 구성 흐름으로 변경했다.

```text
선택 ActorDefinition / MonsterProfile / AbilitySet 분석
→ 동일 타입 후보와 중복 Ability 비교
→ 공용 Base AbilitySet 승격 Preview
→ 특수 몬스터별 파생 Set 생성
→ 교체·추가·제거 Override만 기록
→ 런타임 유효 Set과 BT 선택 풀 검증
```

구현된 핵심 API:

- `AbilitySetSO.GetPlayerAbility`
- `AbilitySetSO.GetCombatSequence`
- `AbilitySetSO.GetEffectiveCharge`
- `AbilitySetSO.GetEffectiveComboRoutes`
- `AbilitySetSO.GetEffectiveComboLinkWindow`
- `AbilitySetSO.EnumerateAll`
- `AbilitySetSO.HasInheritanceCycle`

모든 API는 Base 순환을 방어하며 기존 독립 Set은 이전과 같은 결과를 반환한다.
`ActorDefinitionSO.EffectiveAbilitySet`은 기존 Profile 우선 계약을 유지하되,
Definition의 Set이 Profile Set에서 명시적으로 파생된 경우에만 특수 Set을 사용한다.

진입점:

- Unity 상단 메뉴 `UPlayGround > 툴 런처`
- 런처 분류 `게임플레이 / 전투`
  - `Ability 양산화 Wizard`
  - `Ability 제작 검증 대시보드`
  - `Ability Runtime Sandbox`

각 도구의 내부 등록 ID는 다음과 같으며 Unity 상단 메뉴에 직접 노출되지는 않는다.

- `UPlayGround/게임플레이/Ability Production Wizard`
- `UPlayGround/게임플레이/Ability Production Dashboard`
- `UPlayGround/게임플레이/Ability Runtime Sandbox`

자동 검증:

- `UPlayGround.Data.Editor`, `UPlayGround.GameActor.Editor`,
  `UPlayGround.Ability.Tests` 보조 컴파일 오류 0
- `AbilityProductionPlannerTests` 13개가 Unity 테스트 어셈블리에 포함됨
- 최초 실행에서 기존 7개와 신규 3개가 통과했고 신규 테스트 3개가
  테스트 초기화/분석 경계 문제를 찾아 수정됨
- 최종 소스는 보조 컴파일 오류 0이며 Unity Editor의 다음 Asset Refresh 후
  13개 재실행 확인이 필요하다
- 전체 회귀의 기존 데이터 실패는 계속 별도다.
  - `AttributeProfile_100005.asset`: 필수 Attribute 누락
  - Dryad 공격 3개와 Training Dummy 공격 1개: MotionReference 누락

운영 한계:

- 샌드박스는 실제 ASC 수명주기를 실행하지만 Motion 재생, 상태 머신, 실제 히트 판정은
  선택 프리팹과 게임 부트스트랩에 의존하므로 전체 게임 Play Mode 스모크를 대체하지 않는다.
- 근거가 없는 Dryad 3개와 Training Dummy 1개의 Motion은 자동 보정하지 않는다.
- 레시피는 일반 구조를 생산한다. Projectile/AOE/Telegraph 이벤트 자체를 자동 삽입하지
  않으며 Dashboard에서 선택 Motion의 근거를 검증한다.

---

## 1. 목적

현재 Ability 런타임은 Ability, Task, Effect, Tag, Attribute, Cooldown을
`AbilitySystemComponent` 아래에서 운용할 수 있는 기반을 갖췄다. 그러나 신규 스킬 제작은
여전히 여러 ScriptableObject를 개별 생성하고 참조를 사람이 연결하는 작업에 의존한다.

이 문서의 목적은 기존 런타임을 다시 설계하는 것이 아니라 다음 제작 사이클을 하나의
안전한 에디터 워크플로로 완성하는 것이다.

```text
레시피 선택
→ Motion/대상 Set 선택
→ 생성 계획 Preview
→ Ability/Payload/Effect/Set 일괄 생성·연결
→ 정적 검증
→ 샌드박스 실행
→ 정적 예상값과 런타임 결과 비교
→ 변형 복제 또는 수치 보정
```

최종 목표는 “데이터만 채우면 실행된다”를 넘어, **잘못된 참조나 공유 에셋 오염 없이
반복적으로 스킬을 생산하고 검증할 수 있는 제작 시스템**을 제공하는 것이다.

---

## 2. 규범 용어

| 용어 | 의미 |
|------|------|
| 기존 | 현재 프로젝트 코드나 에셋에 존재한다 |
| 신규 제안 | 이 스펙에서 새로 도입할 타입·API·에디터 기능이다 |
| 필수 | 해당 Phase 완료를 선언하려면 구현해야 한다 |
| 선택 | 실제 콘텐츠 요구가 발생한 뒤 도입할 수 있다 |
| 금지 | 데이터 손상이나 이중 권위를 막기 위해 사용하지 않는다 |

이 문서의 신규 타입명은 구현 의도를 고정하기 위한 제안명이다. 구현 시 네임스페이스와
세부 이름은 조정할 수 있지만 책임 경계와 안전 불변식은 유지해야 한다.

---

## 3. 범위와 비범위

### 3.1 범위

- 플레이어, 일반 몬스터, 보스가 공용으로 사용할 Ability 제작 레시피
- Ability/Payload/Effect/AbilitySet 참조의 일괄 생성과 연결
- 공용 Task Graph 프리셋 선택
- 기존 Ability의 안전 복제와 변형 생성
- MotionReference/MotionSet/MotionEvent 기반 기본값 추출
- 생성 전 Preview, 충돌 검사, Undo, 실패 시 전체 롤백
- 실시간 단일 에셋 검증과 프로젝트 전체 검증
- 생성 결과의 샌드박스 실행과 런타임 Snapshot
- 정적 예상 피해·지속시간과 실제 측정값 비교
- 레시피 및 생성기의 EditMode 테스트
- 대표 레시피의 PlayMode 수직 슬라이스

### 3.2 비범위

- KCC 상태 머신과 이동 계산을 Ability Task로 이전
- MotionSet 타임라인을 범용 노드 그래프로 대체
- Unreal Blueprint 수준의 범용 비주얼 스크립팅
- 네트워크 예측·복제·서버 권한
- AI Behavior Tree를 Ability Editor 안에서 재구현
- 모든 전투 밸런스를 자동으로 결정하는 자동 튜너
- 근거 없는 MotionReference 또는 VFX 자동 매핑
- 기존 손튜닝 에셋의 무조건 덮어쓰기

---

## 4. 현재 구현 기준선

### 4.1 이미 존재하며 유지할 기반

| 영역 | 현재 구현 | 양산화에서의 역할 |
|------|-----------|-------------------|
| Ability 정의 | `GameplayAbilitySO` | 조건, 비용, 쿨다운, Variant, Effect, Cue의 단일 정의 |
| 실행 Payload | `UPlayGroundMotionAbilityPayloadSO` | `AbilityAttackInfo`와 `MotionReferenceSO` 연결 |
| 캐릭터 구성 | `AbilitySetSO` | 플레이어 슬롯, 전투 바인딩, 차지, 콤보, AI Ability 풀 |
| 실행 그래프 | `AbilityTaskGraphSO` | Ability 실행 수명주기의 필수 Root |
| 공용 Task | Wait/Event/Sequence/Parallel 계열 | 레시피가 선택할 공용 실행 블록 |
| Motion 종료 Task | `WaitMotionSetEndAbilityTask` | Motion 실행 완료·중단을 Ability 수명주기에 연결 |
| 데이터 검증 | `AbilityDataValidator` | ID, Task Graph, Variant, Payload, Effect, Set 기본 검증 |
| 편집기 | `GameplayAbilityEditorWindow` | Ability/Passive/Effect/Set 탐색·편집·저장·삭제·전체 검증 |
| 런타임 디버그 | `GasRuntimeDebuggerWindow` | Ability/Effect/Tag/Attribute 런타임 상태 관찰 |
| 밸런스 분석 | `BalanceAttackAnalyzer`, `BalanceDataExtractor` 등 | AbilitySet과 Payload의 정적 분석 |

### 4.2 현재 제작 흐름의 병목

현재 `GameplayAbilityEditorWindow`의 생성 버튼은 빈 `GameplayAbilitySO`,
`PassiveAbilitySO`, `GameplayEffectSO`, `AbilitySetSO`를 각각 만든다. 신규 공격
Ability를 실행 가능하게 만들려면 제작자가 다음 참조를 직접 구성해야 한다.

```text
AbilitySetSO
└── GameplayAbilitySO
    ├── AbilityTaskGraphSO
    ├── AbilityVariantDefinition
    │   └── UPlayGroundMotionAbilityPayloadSO
    │       └── AbilityAttackInfo
    │           └── MotionReferenceSO
    │               └── MotionSetAsset
    └── GameplayEffectSO 목록
```

현재 `BalanceDataAutoGenerator`는 AbilitySet이 누락된 경우 자동 생성하지 않고
Ability Editor에서 생성·연결하라는 경고만 출력한다. 따라서 기존 문서에 남아 있는
MotionSet 기반 Ability 자동 생성 설명은 현재 구현 계약으로 간주하지 않는다.

### 4.3 현재 안정화 상태

`GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_PROGRESS.md` 기준:

- 487개 `GameplayAbilitySO`가 공용 Motion Task Graph를 사용한다.
- Ability의 null `taskGraph`는 0건이다.
- Ability EditMode 테스트는 마지막 기록상 68개 중 67개 통과, 1개 실패가 남아 있다.
- Dryad 공격 3개와 Training Dummy 공격 1개는 근거가 되는 Motion이 미확정이다.
- Player/Monster Damage, Healing, Poise, UltimateEnergy, Cooldown 등에 대한
  Unity Play Mode 수동 스모크가 남아 있다.

양산화 도구 구현 전에 §18 Phase 0의 기준선 게이트를 먼저 통과해야 한다.

---

## 5. 핵심 설계 원칙

### 5.1 런타임과 제작 파이프라인 분리

양산화 도구는 Editor 전용이다. `GameplayAbilitySO`, `AbilityTaskGraphSO`,
`AbilitySystemComponent`의 런타임 계약을 제작 편의를 위해 우회하거나 이중화하지 않는다.

- Editor 코드는 모듈별 Editor asmdef에 둔다.
- Data 모듈에 `UnityEditor` 참조를 추가하지 않는다.
- 생성기는 런타임에서 사용되는 것과 동일한 SO 타입을 생성한다.
- 런타임 폴백이나 “임시 실행 데이터”를 추가하지 않는다.

### 5.2 레시피는 데이터 원본이 아니라 생성 정책

레시피는 새 Ability의 초기 구조와 추천값을 정의한다. 생성 후 실제 실행 권위는 생성된
`GameplayAbilitySO`, Payload, Effect, AbilitySet에 있다.

레시피를 수정했다고 이미 생성된 Ability가 암묵적으로 변경되면 안 된다. 기존 Ability를
새 레시피 정책에 맞춰 갱신하려면 별도 동기화 Preview와 사용자 승인이 필요하다.

### 5.3 공용 참조와 소유 참조 구분

| 기본 정책 | 대상 |
|-----------|------|
| 공유 | 검증된 공용 Task Graph, 명시적으로 공용인 Effect, 공용 MotionReference |
| 신규 생성 | GameplayAbility, Motion Payload |
| 선택적 복제 | 캐릭터 전용 Effect, 캐릭터/무기 전용 MotionReference |
| 직접 수정 금지 | 기존 MotionSet, 기존 프리팹, 다른 캐릭터가 공유하는 손튜닝 에셋 |

생성 Preview에는 각 참조가 `신규`, `기존 공유`, `복제`, `변경` 중 무엇인지 표시해야 한다.

### 5.4 결정적 결과

같은 입력은 같은 ID, 저장 경로, AbilitySet 바인딩 후보를 만들어야 한다. 이미 같은 ID나
경로가 존재하면 이름에 숫자를 붙여 우회하지 않고 충돌로 보고한다.

### 5.5 자동 추정과 확정값 구분

MotionEvent, 모션 길이, AnimKey 이름에서 추출한 값은 추천값이다. Preview에서 출처와
신뢰도를 보여주고 사용자가 확정한 뒤 생성한다.

근거를 찾지 못한 값은 임의의 첫 번째 후보로 채우지 않는다.

### 5.6 몬스터 타입 공용 Set과 특수 개체 Override

동일 타입 몬스터는 `MonsterActorProfileSO.abilitySet` 하나를 공유한다.
특수·엘리트·보스 변형을 만들기 위해 공용 Ability/Payload를 통째로 복제하지 않는다.

파생 Set은 선택적인 `baseSet`과 명시적인 Override 및 로컬 추가 Ability만 소유한다.

| Override 종류 | 의미 |
|---|---|
| Replace | Base Set의 특정 Ability를 다른 Ability로 교체 |
| Add | Base에 없는 특수 Ability 추가 |
| Remove | Base의 특정 Ability를 해당 파생 Set에서 제외 |
| Slot Override | Player/Combat/Charge처럼 순서 의미가 있는 슬롯 전체를 명시적으로 대체 |

런타임 소비자는 Base와 Override를 직접 순회하지 않고 `AbilitySetSO`의 유효 해석 API만
사용해야 한다. 해석 우선순위는 `특수 파생 Set → 타입 공용 Base Set`이다.

필수 안전 규칙:

- `baseSet` 순환 참조는 저장 전 오류로 차단한다.
- Replace 원본이 Base 유효 Set에 없으면 오류다.
- 같은 원본에 Replace와 Remove를 동시에 선언할 수 없다.
- Override가 없는 파생 Set은 공용 Set과 동일한 유효 결과를 내야 한다.
- 공용 Ability의 수치 일부를 바꾸려면 Ability/Payload 안전 Fork 후 Replace한다.
  공유 SO 인스턴스의 필드를 개체별로 런타임 변경하지 않는다.
- 유효 Set 해석 결과의 중복 Ability는 안정 ID 기준으로 오류 또는 명시적 중복 정책을
  요구한다.

`MonsterActorProfileSO.abilitySet`은 타입 공용 Set, 여기서 파생된
`ActorDefinitionSO.abilitySet`은 특수 개체 Set이다. 서로 무관한 Definition Set과
Profile Set이 동시에 존재하는 기존 데이터는 이전처럼 Profile Set을 우선해 의도치 않은
행동 변경을 막는다. `ActorAbilitySystem`, `EnemyCombat`, BT는
`EnumerateAll`/슬롯 해석 API를 통해 합성된 유효 풀을 소비한다.

---

## 6. 목표 아키텍처

```text
GameplayAbilityProductionWizardWindow
        │
        ├── AbilityRecipeCatalog
        │       └── AbilityRecipeDefinition
        │
        ├── AbilityMotionAnalyzer
        │       ├── MotionReferenceSO
        │       ├── MotionSetAsset
        │       └── MotionEvent
        │
        ├── AbilityCreationPlanner
        │       └── AbilityCreationPlan (메모리 전용)
        │
        ├── AbilityProductionValidator
        │       ├── 기존 AbilityDataValidator
        │       └── 생성 전 교차 참조·경로·공유 영향 검사
        │
        └── AbilityAssetFactory
                ├── Preview
                ├── Undo Group
                ├── 에셋 생성
                ├── AbilitySet 연결
                ├── 저장 후 재검증
                └── 실패 시 전체 롤백

AbilitySandboxWindow
        ├── 테스트 Actor/Target
        ├── Ability 실행
        ├── GasRuntimeDebugger Snapshot
        └── AbilityBalanceComparison
```

### 6.1 권장 물리 배치

```text
Assets/02.Scripts/Data/Editor/Ability/Production/
├── AbilityRecipeDefinition.cs
├── AbilityRecipeCatalog.cs
├── AbilityCreationRequest.cs
├── AbilityCreationPlan.cs
├── AbilityCreationPlanner.cs
├── AbilityAssetFactory.cs
├── AbilityAssetOwnershipAnalyzer.cs
├── AbilityProductionValidator.cs
├── AbilityMotionAnalyzer.cs
├── GameplayAbilityProductionWizardWindow.cs
├── AbilityCloneWindow.cs
└── AbilitySandboxWindow.cs
```

UPlayground 전용 MotionSet, `AbilityAttackInfo`, Actor 프리팹 접근이 필요한 구현은
공용 Ability Core Editor로 이동시키지 않는다. 향후 Core Editor를 분리할 때는 레시피와
검증의 공용 부분만 별도 asmdef로 추출한다.

---

## 7. 레시피 모델

### 7.1 초기 필수 레시피

| Recipe ID | 용도 | 기본 대상 정책 | Motion 분석 |
|-----------|------|----------------|-------------|
| `Player.Basic.Melee` | 플레이어 일반 근접 공격 | Optional/Enemy | Collision 기반 HitPhase |
| `Player.Skill.Projectile` | 플레이어 투사체 스킬 | Required/Enemy | Projectile 이벤트 기반 |
| `Monster.Basic.Melee` | 몬스터 기본 근접 공격 | Required/Enemy | Collision 기반 HitPhase |
| `Monster.Heavy.Telegraph` | 강공·위험 공격 | Required/Enemy | Telegraph/Danger Ring 추천 |
| `Combat.AreaAttack` | 자기 중심 또는 지정점 범위 공격 | Optional/Enemy | AOE 이벤트 기반 |
| `Support.HealOrBuff` | 자가·아군 회복/버프 | Self 또는 Required/Ally | Effect 중심 |

다음 레시피는 실제 콘텐츠 수직 슬라이스가 필요할 때 추가한다.

- 돌진/이동 공격
- 카운터/가드 성공 반응
- 채널링
- 소환
- 지속 장판
- 보스 페이즈 전용 Ability 묶음

### 7.2 신규 제안 `AbilityRecipeDefinition`

레시피는 처음부터 범용 ScriptableObject 그래프로 만들지 않는다. 1차 구현은 코드로
등록된 불변 Recipe Definition과 Inspector에서 조정 가능한 소수의 프리셋 중 하나를
선택한다. 레시피 자체의 런타임 로딩은 금지한다.

개념 필드:

| 그룹 | 필드 | 의미 |
|------|------|------|
| 식별 | `recipeId`, `displayName`, `version` | 안정 ID, UI 이름, 정책 버전 |
| 대상 | `supportedOwnerKinds` | Player/Monster/Boss 지원 범위 |
| 기본 Ability | category, target, ground, concurrency | Ability 기본 정책 |
| 비용/쿨다운 | 기본값 또는 추천 규칙 | 생성 초기값 |
| 실행 | `taskGraphPreset` | 검증된 공용 Task Graph |
| Motion | `motionAnalysisMode` | Collision/Projectile/AOE/None |
| Effect | owner/target Effect 생성 정책 | 공유 또는 신규 생성 |
| 바인딩 | Set 연결 후보 | 슬롯, combat binding, additionalAbilities |
| 검증 | 필수 입력·경고 규칙 | 생성 전 차단 조건 |

### 7.3 레시피 버전

- 생성된 Ability의 런타임 데이터에 recipe ID를 권위 필드로 추가하지 않는다.
- Editor 전용 생성 기록이 필요하면 별도 Import/Production metadata 서브에셋 또는
  Editor 전용 데이터베이스에 `recipeId`, `recipeVersion`, 생성 시각, 원본 GUID를 기록한다.
- 레시피 버전 상승은 기존 에셋을 자동 수정하지 않는다.
- “레시피와 비교” 기능은 변경 후보만 계산하고 적용 전 diff를 보여준다.

---

## 8. 생성 요청과 생성 계획

### 8.1 신규 제안 `AbilityCreationRequest`

Wizard가 수집하는 사용자 입력이다.

필수 입력:

- Recipe
- 소유 대상: Player/Monster/Boss와 캐릭터 또는 Actor
- 대상 `AbilitySetSO`
- Ability 표시명과 안정 `abilityId`
- 저장 루트
- MotionReference 또는 MotionSet
- Set 연결 방식

조건부 입력:

- Player 슬롯 또는 `PlayerCombatAbilitySlot`
- AI `selectionWeight`, `requiredLevel`, `minRange`, `maxRange`,
  `aiSelectable`
- 비용, 자원 종류, 쿨다운, 공유 쿨다운 그룹
- Effect 생성 또는 기존 Effect 선택
- Variant 구성
- 차지 단계 또는 Combo Route 연결

### 8.2 신규 제안 `AbilityCreationPlan`

`AbilityCreationPlan`은 `UnityEngine.Object`를 생성하지 않는 메모리 전용 계획이다.
Preview와 실제 적용은 같은 Plan을 사용해야 한다.

각 Plan 항목은 다음 정보를 가진다.

| 필드 | 의미 |
|------|------|
| `operation` | Create / Clone / Reuse / Modify |
| `assetType` | Ability, Payload, Effect, MotionReference, Set 등 |
| `targetPath` | 결정적 저장 경로 |
| `stableId` | Ability/Effect ID |
| `sourceAsset` | 복제 또는 공유 원본 |
| `targetAsset` | 수정 대상 기존 에셋 |
| `propertyChanges` | 변경할 필드의 이전값/이후값 |
| `dependencies` | 먼저 성공해야 하는 Plan 항목 |
| `warnings` | 공유 영향, 추정값, 수동 확인 항목 |

### 8.3 Plan 불변식

- Plan 생성은 프로젝트 에셋을 변경하지 않는다.
- 같은 Plan 안에서 경로와 안정 ID가 중복되면 실행할 수 없다.
- 기존 경로가 있는데 `Reuse`나 명시적 `Modify`가 아니면 충돌이다.
- 참조 대상이 Plan 외부 기존 에셋이면 GUID와 타입을 검증한다.
- AbilitySet 변경은 항상 별도 Plan 항목으로 표시한다.
- 오류가 하나라도 있으면 Apply 버튼을 비활성화한다.

---

## 9. ID와 저장 경로 정책

### 9.1 안정 ID

기존 Ability/Effect의 사람이 읽을 수 있는 안정 문자열 ID 정책을 유지한다.

권장 형식:

```text
Ability.Player.<Character>.<Category>.<Name>
Ability.Monster.<ActorId>.<Name>
Ability.Boss.<ActorId>.<Phase>.<Name>
Effect.<OwnerOrShared>.<Name>
Variant.Default
Variant.Grounded
Variant.Airborne
Variant.Enhanced
```

ID는 생성 후 파일명 변경이나 폴더 이동으로 자동 변경하지 않는다.

### 9.2 권장 저장 경로

현재 데이터 배치를 존중하고 대상 Set의 폴더를 기본 저장 루트로 제안한다.

```text
Assets/10.Datas/Ability/Actor/<ActorId>/
├── AbilitySet_<ActorId>.asset
├── Abilities/
│   └── GA_<Name>.asset
├── Payloads/
│   └── AbilityPayload_<Name>.asset
├── Effects/
│   └── GE_<Name>.asset
└── MotionReferences/
    └── MotionRef_<Name>.asset
```

플레이어 Migrated 데이터는 기존 경로와 참조 안정성을 우선한다. 양산화 도구 도입만을
이유로 기존 에셋을 일괄 이동하지 않는다.

### 9.3 충돌 정책

- GUID/path가 지정된 기존 에셋은 정확 일치만 허용한다.
- 지정 경로가 유효하지 않으면 이름으로 폴백하지 않는다.
- ID 중복은 자동 suffix로 회피하지 않는다.
- 같은 이름 후보가 여러 개면 사용자가 정확한 에셋을 선택해야 한다.

---

## 10. Motion 분석과 기본값 추천

### 10.1 입력 우선순위

```text
명시적 MotionReferenceSO
→ 명시적 MotionSetAsset
→ 선택 Actor의 확정된 Motion 연결
→ 미해결
```

적의 실제 `WeaponType`을 제공하는 계약이 없으면 임의의 첫 override를 선택하지 않는다.

### 10.2 분석 결과

신규 제안 `AbilityMotionAnalysis`는 다음 정보를 제공한다.

- 해석된 MotionReference와 MotionSet
- Motion 총 길이
- 발견한 Collision/Projectile/AOE/Heal/Invincibility/Telegraph 이벤트
- Collision별 `hitPhaseIndex`
- 추천 `HitPhaseData` 수
- 추천 startup/active/recovery
- 추천 Ability category
- Danger Ring/Telegraph 추천
- 경고와 미지원 이벤트
- 각 추천값의 출처와 신뢰도

### 10.3 동기화 정책

Motion 분석은 세 가지 모드를 제공한다.

| 모드 | 동작 |
|------|------|
| Create | 신규 Payload의 초기값 생성 |
| Compare | 기존 Payload와 Motion의 차이만 표시 |
| Apply Selected | 사용자가 선택한 차이만 Undo와 함께 반영 |

자동 동기화는 기존 손튜닝 피해량, Poise, AI 가중치, 텔레그래프 설정을 기본적으로
덮어쓰지 않는다.

### 10.4 미해결 처리

- MotionReference가 없으면 공격 레시피 생성은 차단한다.
- 프로젝트에 존재하는 다른 Motion을 임의 대체하지 않는다.
- 콘텐츠 결정이 필요한 경우 `UnresolvedContentDecision`으로 Preview에 표시한다.
- Dryad 3개와 Training Dummy 1개는 실제 Motion 확정 전 자동 연결 대상에서 제외한다.

---

## 11. 공용 Task Graph 정책

### 11.1 초기 방침

현재 대부분의 공격 Ability가 공용 Motion Task Graph를 사용하므로 1차 양산화에서는
범용 GraphView 편집기를 만들지 않는다.

레시피는 검증된 Task Graph 프리셋을 참조한다.

```text
Motion 실행 후 종료 대기
Delay 후 Effect 적용
GameplayEvent 대기
Sequence
Parallel
```

실제 프로젝트에 없는 조합은 레시피 이름만 먼저 만들지 않는다. 필요한 수직 슬라이스와
Task Definition을 구현하고 테스트한 뒤 Catalog에 등록한다.

### 11.2 프리셋 검증

- Graph와 Root가 존재해야 한다.
- Task 참조 순환이 없어야 한다.
- 종료 경로 없는 무한 Wait를 금지한다.
- 부모 Ability 실패/취소 시 활성 Task가 남지 않아야 한다.
- Commit 또는 동일 Effect 적용이 중복될 수 있는 구조를 경고한다.
- UPlayground 전용 Task는 Core asmdef에 넣지 않는다.

### 11.3 범용 그래프 에디터 도입 조건

다음 중 둘 이상이 반복될 때 별도 설계를 작성한다.

- Sequence/Parallel 조합을 사람이 빈번하게 새로 구성한다.
- 채널링, 소환, 장판 등에서 공용 프리셋 수가 과도하게 늘어난다.
- Task 분기 조건을 데이터로 저작할 필요가 생긴다.
- 프리셋 복제로 인한 중복 Graph가 실제 유지보수 문제를 만든다.

---

## 12. 제작 마법사 UX

### 12.1 진입점

권장 메뉴:

```text
UPlayGround/Gameplay/Ability/Ability Production Wizard
```

기존 `GameplayAbilityEditorWindow`에는 다음 진입 버튼을 추가한다.

- 레시피로 생성
- 선택 Ability 변형 생성
- Motion과 비교
- 샌드박스에서 실행

### 12.2 단계

#### Step 1 — 레시피

- Player/Monster/Boss 필터
- 레시피 설명과 생성 대상 표시
- 필수 MotionEvent와 지원 한계 표시

#### Step 2 — 소유자와 Set

- Actor/캐릭터 선택
- `AbilitySetSO` 선택
- 플레이어 슬롯, combat binding, combo route 또는 additionalAbilities 선택
- 이미 같은 슬롯·라우트가 있을 때 교체/추가/취소를 명시적으로 선택

#### Step 3 — Motion과 실행

- MotionReference/MotionSet 선택
- 해석된 모션과 WeaponType 표시
- MotionEvent 분석 결과
- Task Graph 프리셋
- Variant 구성

#### Step 4 — 전투·AI·Effect

- HitPhase 초안
- 비용·쿨다운
- 대상·거리·태그 조건
- AI 선택 조건
- owner/target/commit/end Effect

#### Step 5 — Preview

- 생성·복제·공유·변경 에셋 목록
- 경로와 안정 ID
- AbilitySet 변경 전/후
- 자동 추천값과 출처
- 오류, 경고, 수동 확인 항목

#### Step 6 — 적용 및 결과

- 전체 트랜잭션 적용
- 저장 후 전체 관련 에셋 재검증
- 생성 에셋 선택
- 샌드박스 실행 또는 Ability Editor로 이동
- 변경 리포트 복사/저장

### 12.3 편집 UX

- 필드 변경 시 선택 에셋 검증을 debounce하여 갱신한다.
- 오류 행을 클릭하면 해당 에셋을 선택하고 가능한 경우 PropertyField에 포커스한다.
- Console은 전체 로그의 보조 출력으로만 사용한다.
- 여러 에셋을 선택했을 때 공통 필드 일괄 편집은 별도 Phase로 둔다.

---

## 13. 안전 복제와 변형 생성

### 13.1 복제 모드

| 모드 | 용도 |
|------|------|
| Variant 추가 | 같은 Ability 안에 지상/공중/강화 실행 추가 |
| Ability Fork | 원본을 유지하고 독립 Ability 생성 |
| Character Fork | 다른 캐릭터/몬스터용으로 Ability와 소유 데이터를 복제 |
| Balance Variant | Motion은 공유하고 수치·AI 조건만 독립화 |

### 13.2 참조별 선택

복제 Preview에서 각 참조를 다음 중 하나로 선택한다.

- 원본 공유
- 독립 복제
- 다른 기존 에셋 선택
- 제외

기본값:

| 대상 | 기본 |
|------|------|
| GameplayAbilitySO | 복제 |
| Motion Payload | 복제 |
| 공용 Task Graph | 공유 |
| MotionReference | 공유 |
| 캐릭터 전용 Effect | 복제 |
| 명시적 Shared Effect | 공유 |
| AbilitySet | 기존 대상 Set 수정 |

### 13.3 공유 영향 분석

`AbilityAssetOwnershipAnalyzer`는 수정 대상 에셋의 역참조를 조사한다.

예:

```text
GE_AttackBuff는 Ability 17개와 Passive 2개가 참조합니다.
이 에셋을 직접 수정하면 19개 소비자에 영향을 줍니다.
```

다중 공유 에셋을 수정하려면 영향 목록을 확인하고 명시적으로 승인해야 한다. 안전한 기본
동작은 독립 복제다.

---

## 14. 트랜잭션과 롤백

### 14.1 적용 순서

```text
Plan 최종 검증
→ Undo Group 시작
→ 신규 에셋 생성
→ 생성 에셋 필드 채움
→ 기존 에셋 변경
→ AbilitySet 연결
→ Import/Save
→ 관련 에셋 검증
→ 성공 시 Undo Collapse
```

### 14.2 실패 처리

어느 단계에서든 예외 또는 검증 오류가 발생하면:

1. 현재 AssetEditing 범위를 종료한다.
2. 해당 Undo group 전체를 `Undo.RevertAllDownToGroup`으로 되돌린다.
3. 신규 생성 에셋과 기존 에셋 변경이 남지 않았는지 확인한다.
4. AssetDatabase를 저장·새로고침한다.
5. 실패 단계, 예외, 롤백 결과를 리포트한다.

부분 성공을 성공으로 보고하면 안 된다.

### 14.3 P09 계열 경로와의 구분

기존 에셋을 삭제·교체하는 빌더 경로는 완전한 transaction으로 간주하지 않는다. 양산화
Factory는 초기 구현에서 기존 에셋 삭제를 수행하지 않는다. 교체가 필요하면 새 에셋 생성,
검증, 참조 교체, 기존 에셋 정리의 별도 Plan으로 분리한다.

### 14.4 저장 안전

- MotionSet/Ultimate/프리팹 오류가 있으면 대량 저장을 수행하지 않는다.
- 생성 도구는 대상과 직접 관련된 에셋만 Dirty 처리한다.
- `Assets/10.Datas/`와 `Assets/03.Prefabs/` 변경 목록을 결과 리포트에 포함한다.
- 생성 후 자동 재직렬화된 사용자 데이터는 임의로 원복하지 않는다.

---

## 15. 검증 스펙

### 15.1 기존 필수 검증 유지

- 빈 값 또는 중복 `abilityId`, `effectId`
- Task Graph 또는 Root 누락
- 실행 가능한 Variant 없음
- Variant ID 누락 및 조건/우선순위 충돌
- Payload, `AbilityAttackInfo`, MotionReference, Motion 누락
- 비용·쿨다운·기간·주기 음수
- 미등록 GameplayTag
- Effect 참조와 AbilitySet 참조 누락
- Player 슬롯, combat binding 중복
- 차지 단계와 임계값 수 불일치

### 15.2 신규 생성 전 오류

- 생성 경로 또는 안정 ID 충돌
- Recipe 필수 입력 누락
- 공격 Recipe에 실행 가능한 Motion 없음
- Player 전용 바인딩을 Monster Set에 적용
- Set 연결 대상 슬롯을 정하지 않은 교체
- 복제 원본과 생성 대상이 같은 경로
- Plan 의존 순환
- Task Graph 순환 또는 Root 도달 불가
- Motion Collision `hitPhaseIndex`와 Payload HitPhase 불일치
- 생성 후 대상 Set에서 Ability에 도달할 수 없음

### 15.3 신규 경고

- 공유 Effect/쿨다운 그룹의 다중 캐릭터 영향
- MotionEvent에서 추정한 HitPhase와 기존 손튜닝 값 차이
- 투사체/소환형 공격에 Collision 기반 분석 적용
- AI Ability인데 `aiSelectable`이 아니거나 selectionWeight가 0 이하
- `minRange > maxRange`
- 공격 범위와 Ability activation 거리의 불일치
- Heavy/Skill인데 Telegraph 또는 Danger Ring 근거가 없음
- 아이콘·로컬라이즈 키·Cue 누락
- 사용되지 않는 신규 Effect 또는 MotionReference
- 같은 Ability가 Set의 여러 경로에 의도치 않게 중복 등록됨

### 15.4 검증 결과 모델

각 Issue는 최소한 다음 정보를 제공한다.

- Severity: Error / Warning / Info
- 코드: 자동 테스트에서 비교 가능한 안정 문자열
- 메시지
- Context 에셋
- Property Path
- 관련 에셋 목록
- 가능한 경우 안전한 Fix 제안

자동 Fix는 기본적으로 실행하지 않는다. Fix를 제공할 때도 변경 Preview와 Undo를 거친다.

---

## 16. 샌드박스 실행과 런타임 관찰

### 16.1 목표

제작자가 전체 게임 루프를 진입하지 않고 선택한 Ability의 첫 실행을 빠르게 확인할 수 있게
한다. 샌드박스는 실제 런타임 경로를 우회한 “가짜 피해 계산기”가 아니다.

### 16.2 입력

- 테스트 Actor 프리팹 또는 씬 Actor
- 대상 Dummy/Monster
- Ability 또는 AbilitySet 슬롯
- 초기 Attribute Profile
- 거리, 지상/공중, 초기 자원
- 반복 횟수

### 16.3 관찰 항목

- 활성화 성공/실패와 표준 실패 사유
- 선택된 Variant
- 비용과 쿨다운 Commit
- Task 시작·완료·실패·취소
- Motion 시작·종료
- HitPhase별 적중과 피해/Poise
- 적용·제거된 Effect와 Tag
- 실행 종료 후 남은 Task/Effect/Tag
- 총 실행 시간과 취소 시점

### 16.4 안전

- EditMode에서 실제 Scene 오브젝트를 변경하지 않는다.
- PlayMode 테스트용으로 생성한 오브젝트는 종료 시 정리한다.
- 샌드박스 디버거는 읽기 전용 Snapshot을 사용하며 런타임 결과를 보정하지 않는다.
- 샌드박스 통과가 실제 캐릭터 상태 머신·카메라·UI 스모크를 대체하지 않는다.

---

## 17. 밸런스 피드백 루프

### 17.1 정적 요약

Ability Editor와 Wizard 결과 화면에서 다음 값을 표시한다.

- 총 HitPhase 수
- 1회 실행 예상 총 피해와 Poise 피해
- Motion/Ability 예상 지속시간
- 쿨다운 포함 이론상 사용 주기
- 자원 대비 피해 또는 회복 효율
- AI 사용 가능 거리와 선택 가중치
- 같은 AbilitySet 안에서의 피해·쿨다운 분포

정적 요약은 기존 `BalanceAttackAnalyzer`, `BalanceDataExtractor`가 읽는 데이터와 같은
`AbilitySetSO`/Payload를 사용해야 한다.

### 17.2 실제 결과 비교

신규 제안 `AbilityBalanceComparison`:

| 항목 | 비교 |
|------|------|
| 피해 | `balance.expectedDamage` 대 실제 평균 피해 |
| 지속시간 | `balance.expectedDuration` 대 실제 실행 시간 |
| 적중 | HitPhase 수 대 실제 Hit 수 |
| 비용 | 정의 비용 대 실제 Commit 전후 자원 |
| 쿨다운 | 정의 시간 대 실제 Ready 시점 |
| 취소 | 취소 시점과 잔류 Task/Effect |

### 17.3 Replay Comparator

`BALANCE_DESIGNER_TOOL_GUIDE.md`에 미구현으로 남아 있는
`BalanceReplayComparator`는 양산화 Phase 5에서 연결한다.

- 정적 AI 선택 가중치와 실제 선택 빈도
- 정적 사용 가능 거리와 실제 교전 거리
- 기대 피해와 실제 피해 기여율
- 활성화 실패 사유 분포

자동 밸런스 수정은 초기 범위에 포함하지 않는다. 먼저 비교 결과와 수정 후보를 제공한다.

---

## 18. 구현 단계

### Phase 0 — 기준선 안정화

목표: 양산화 도구의 오류와 기존 시스템 오류를 구분할 수 있는 기준선을 만든다.

작업:

1. Ability EditMode 68개 전체 통과
2. Dryad 3개와 Training Dummy 1개의 Motion 미해결 상태를 명시적으로 유지
3. Player/Monster Damage, Healing, Poise, UltimateEnergy, Cooldown 수동 스모크
4. Ability 전체 검증 결과 Snapshot 저장
5. Ability/Effect/Set/Payload/TaskGraph 에셋 수와 참조 기준선 기록

완료 게이트:

- Unity 컴파일 오류 0
- Ability EditMode 오류 0
- 기존 PlayMode 수직 슬라이스 오류 0
- 기준선 미해결 콘텐츠 4건 외 Motion 해석 실패 0

### Phase 1 — Recipe, Plan, Factory 수직 슬라이스

목표: `Monster.Basic.Melee` 한 종류를 Preview부터 Set 연결까지 안전하게 생성한다.

작업:

1. Recipe Catalog
2. Creation Request/Plan
3. 결정적 ID·경로 정책
4. Plan 검증
5. Asset Factory와 전체 롤백
6. 생성 후 `AbilityDataValidator` 재검증

완료 게이트:

- Preview와 실제 생성 결과 일치
- 중간 실패 주입 시 잔류 에셋·Set 변경 0
- 같은 입력 재실행 시 충돌을 명시적으로 보고

### Phase 2 — Production Wizard와 필수 레시피

목표: 일반적인 근접·투사체·AOE·버프/회복을 Project 창 수동 생성 없이 제작한다.

작업:

1. 6개 초기 레시피
2. 단계형 Wizard UI
3. AbilitySet 바인딩 선택
4. Effect 생성/공유 선택
5. 결과 리포트와 Ability Editor 이동

완료 게이트:

- 각 레시피로 생성한 데이터의 검증 오류 0
- 기존 에셋 무단 변경 0
- Player와 Monster 각각 하나 이상의 PlayMode 실행 성공

### Phase 3 — Motion 분석과 안전 복제

목표: Motion 기반 초안 생성과 기존 스킬 변형 제작 시간을 줄인다.

작업:

1. MotionReference/MotionSet 분석
2. Collision/Projectile/AOE/Telegraph 추천
3. Create/Compare/Apply Selected
4. Ability Fork와 Character Fork
5. 공유 영향 분석

완료 게이트:

- HitPhase 인덱스 불일치를 생성 전에 차단
- 손튜닝 필드 자동 덮어쓰기 0
- 공유 Effect 수정 영향이 Preview에 표시됨

### Phase 4 — 검증 대시보드와 샌드박스

목표: 생성 직후 문제 위치를 찾고 실제 런타임 경로로 첫 실행을 확인한다.

작업:

1. 안정 Issue 코드와 Property Path
2. Issue 클릭 이동
3. Task Graph 교차 검증
4. 샌드박스 Actor/Target 실행
5. Gas Runtime Snapshot 연결

완료 게이트:

- 대표 오류를 에셋과 필드까지 추적 가능
- 종료·취소 후 Task/Effect/Tag 누수 0
- 샌드박스가 런타임 계산을 자체 보정하지 않음

### Phase 5 — 밸런스와 Replay 비교

목표: 생성, 실행, 실제 결과 비교를 하나의 반복 루프로 연결한다.

작업:

1. 정적 예상값 요약
2. 샌드박스 측정값 비교
3. `BalanceReplayComparator`
4. CSV/Snapshot 회귀 비교
5. 수정 후보 제안

완료 게이트:

- Ability ID로 정적 데이터와 런타임 로그를 연결
- 변경 전/후 결과 비교 가능
- 자동 수정 없이도 차이 원인과 수정 대상 필드를 확인 가능

---

## 19. 테스트 스펙

### 19.1 EditMode

Recipe/Plan:

- Recipe 필수 입력 누락
- 결정적 ID와 경로
- 기존 ID/path 충돌
- Player/Monster 호환성
- AbilitySet 바인딩 후보
- Plan 의존 순서와 순환 차단

Factory:

- Ability/Payload/Effect/Set 일괄 생성
- 공용 Task Graph 공유
- 선택적 Effect 복제
- 생성 후 참조 무결성
- 중간 단계별 실패 주입과 전체 롤백
- Undo/Redo
- 기존 손튜닝 에셋 무변경

Motion:

- Collision 이벤트에서 HitPhase 인덱스 추출
- Projectile/AOE/Heal 이벤트 분류
- MotionReference 기본/WeaponType override 해석
- 근거 없는 override 선택 금지
- 기존 Payload 비교와 선택 적용

Clone:

- Ability/Payload 복제와 Task Graph 공유
- 공유 Effect 영향 분석
- Character Fork ID/path 재작성
- 원본 참조 무변경

Validation:

- Task Graph 순환
- HitPhase/MotionEvent 불일치
- 중복 Set 연결
- AI 선택 조건 오류
- 공유 쿨다운 그룹 경고

### 19.2 PlayMode

- Player 근접 Ability 생성 결과 실행
- Monster BT가 생성된 `aiSelectable` Ability 선택·활성화
- 투사체 또는 AOE 대표 레시피 실행
- 상태 전환 거절 시 비용·쿨다운 미소비
- Motion 중단 시 Task와 Ability 취소
- 사망·교체·씬 전환 후 Task/Effect/Tag 누수 없음
- 샌드박스 반복 실행 후 생성 오브젝트 누수 없음

### 19.3 회귀

- 기존 Ability EditMode 전체
- 기존 Ability PlayMode 수직 슬라이스
- `MonsterAbilitySetIntegrationTests`
- MotionSet managed reference/VFX 누락 검사
- Missing Script 검사
- Data/Ability/Actor/UI asmdef 컴파일
- StandaloneWindows64 Development Player Build

---

## 20. 성능과 운영

- Wizard 목록은 에셋 전체를 매 프레임 재검색하지 않는다.
- AssetDatabase 전체 역참조 검사는 명시적 Preview/검증 시 실행하고 진행률을 표시한다.
- 필드 실시간 검증은 debounce하고 선택 에셋 범위로 제한한다.
- 전체 검증 결과는 에셋 GUID와 dependency hash 기준 캐시를 검토할 수 있으나,
  캐시가 검증 정확도를 낮추면 안 된다.
- 생성기는 BatchMode에서도 호출 가능한 UI 비종속 Planner/Factory API를 제공한다.
- BatchMode 적용도 Preview/Validate/Apply 단계를 분리한다.

---

## 21. 문서와 사용자 가이드

구현 완료 후:

1. 이 문서의 Phase 상태를 갱신한다.
2. 실제 메뉴·타입·경로를 반영한 사용 가이드를 `Assets/docs/guide/`에 작성한다.
3. `BALANCE_DESIGNER_TOOL_GUIDE.md`의 제거된 자동 생성 설명을 현재 Factory 계약으로 갱신한다.
4. `GAMEPLAY_ABILITY_SYSTEM_SPEC.md`의 Editor UX와 자동 검증 절을 실제 구현에 맞춘다.
5. 레시피별 지원 MotionEvent와 한계를 표로 유지한다.

---

## 22. 최종 완료 조건

### 22.1 제작성

- 일반 근접 Ability 하나를 5분 이내 첫 실행 가능한 상태로 생성할 수 있다.
- 복합 스킬도 15분 이내 첫 PlayMode 실행이 가능하다.
- 초기 필수 레시피는 Project 창에서 SO를 개별 생성하지 않고 완성할 수 있다.
- AbilitySet 연결, ID, 경로가 자동 제안되고 Preview에서 검토 가능하다.
- 기존 Ability에서 안전하게 캐릭터별 변형을 만들 수 있다.

### 22.2 안전성

- Apply 전 모든 생성·복제·공유·변경 대상을 확인할 수 있다.
- 오류가 있는 Plan은 적용할 수 없다.
- 중간 실패 후 신규 에셋과 기존 Set 변경이 남지 않는다.
- 기존 손튜닝 데이터의 무단 덮어쓰기 0
- 임의 Motion/WeaponType override 매핑 0
- 공유 에셋 영향 미표시 변경 0

### 22.3 데이터 무결성

- 생성 직후 Ability 검증 오류 0
- Ability ID, Effect ID, 경로 충돌 0
- Task Graph/Root 누락 0
- Payload/MotionReference/HitPhase 누락 0
- AbilitySet 도달 불가능 Ability 0
- Missing Script, managed reference, VFX 누락 0

### 22.4 런타임

- Player/Monster 대표 레시피가 실제 ASC 경로로 실행된다.
- 상태 전환 실패 시 비용·쿨다운이 소비되지 않는다.
- 실행·취소·사망·교체·씬 전환 후 Task/Effect/Tag 누수 0
- 샌드박스 결과와 실제 게임 스모크 사이에 런타임 권위 차이가 없다.
- Player Build 오류 0

### 22.5 밸런스 루프

- Ability ID 기준으로 정적 예상값과 실제 결과를 비교할 수 있다.
- 변경 전/후 Snapshot을 비교할 수 있다.
- AI 선택 빈도와 정적 가중치 차이를 Replay에서 확인할 수 있다.
- 자동 수정 없이도 원인 필드와 수정 후보를 찾을 수 있다.

---

## 23. 미결정 사항

다음은 구현 전에 작은 수직 슬라이스로 확정한다.

1. Recipe Definition을 코드 불변 등록으로 유지할지 Editor 전용 SO Catalog로 확장할지
2. 생성 metadata를 서브에셋으로 둘지 별도 Editor 데이터베이스로 둘지
3. 샌드박스 전용 씬을 둘지 현재 테스트 부트스트랩을 재사용할지
4. Projectile/소환형 공격의 MotionEvent 분석 계약
5. 공용 Effect를 판별하는 명시적 metadata가 필요한지
6. AbilitySet의 동일 Ability 다중 경로 등록을 항상 경고할지 허용 목록을 둘지
7. BatchMode Factory를 CI 검증 전용으로 제한할지 생성까지 허용할지

미결정 사항은 임의 기본값으로 숨기지 않는다. Phase별 구현 PR 또는 작업 기록에서 결정
근거와 테스트 결과를 남긴다.
