# Gameplay Ability / 스킬 시스템 구조 스펙

> 문서 버전: 2.1<br>
> 기준일: 2026-07-18<br>
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP<br>
> 상태: V1 구현·플레이어 데이터 전환·레거시 제거 완료 / 독립 패키지화 후속 진행<br>
> 관련 문서: `../design/PLAYER_SKILL_SYSTEM_REDESIGN_PLAN.md`, `../Complete/GAMEPLAY_TAG_SYSTEM_GUIDE.md`, `../guide/BALANCE_DESIGNER_TOOL_GUIDE.md`

## 1. 목적

이 문서는 UPlayground의 플레이어 스킬, 몬스터 특수 행동, 버프·디버프, 자원, 쿨다운을 데이터 중심으로 확장하기 위한 구현 계약을 정의한다.

언리얼 Gameplay Ability System을 그대로 복제하지 않는다. 현재 프로젝트가 이미 보유한 KCC 상태 머신, MotionSet, 전투 판정, 스탯, GameplayTag를 유지하면서 다음 책임만 공통화한다.

- 행동 활성화 조건
- 자원 비용과 쿨다운
- 실행 중 행동의 수명주기와 취소 관계
- 지속 효과, 주기 효과, 스탯 수정, 태그 부여
- HUD와 텔레메트리가 읽을 표준 상태
- 데이터 검증, 저장, 마이그레이션

Unity의 `ScriptableObject`는 여러 런타임 인스턴스가 공유하는 불변 데이터에 적합하다. 단, 배포 빌드에서 런타임 저장소로 사용할 수 없으므로 정의 데이터와 런타임 상태는 반드시 분리한다.

참고:

- Unity ScriptableObject: https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html
- Unity SerializeReference: https://docs.unity3d.com/kr/current/ScriptReference/SerializeReference.html
- Unreal Gameplay Ability System: https://dev.epicgames.com/documentation/unreal-engine/gameplay-ability-system-for-unreal-engine
- Unreal GAS 구조 설명: https://dev.epicgames.com/documentation/unreal-engine/understanding-the-unreal-engine-gameplay-ability-system

---

## 2. 규범 용어

이 문서에서 다음 단어는 구현 강도를 나타낸다.

| 용어 | 의미 |
|------|------|
| 필수 | 구현 및 데이터가 반드시 따라야 한다 |
| 권장 | 특별한 이유가 없다면 따라야 한다 |
| 선택 | 후속 단계에서 도입할 수 있다 |
| 금지 | 구조 안정성을 위해 사용하지 않는다 |

신규 타입과 API는 `제안`으로 표시한다. 현재 코드에 존재하는 타입은 `기존`으로 표시한다.

---

## 3. 범위와 비범위

### 3.1 범위

- 플레이어 Ability / Ultimate 2슬롯
- 같은 슬롯의 지상·공중·강화 Variant
- 콤보 라우트로 발동하는 스킬
- 몬스터와 보스의 데이터 기반 특수 행동
- 즉시, 지속, 무한, 주기 Effect
- 자원 비용, 쿨다운 그룹, 태그 기반 활성화
- 버프·디버프 중첩과 제거
- 캐릭터 교체, 잔류 공격, 저장·복원
- HUD 읽기 모델과 전투 텔레메트리
- 에디터 검증과 기존 데이터 마이그레이션

### 3.2 비범위

- 네트워크 복제, 서버 권한, 클라이언트 예측
- KCC 이동과 회전의 Ability 이전
- MotionSet 타임라인을 대체하는 범용 비주얼 스크립팅
- 기존 피해·방어·리액션 공식의 재구현
- 모든 일반 공격과 이동 상태의 즉시 Ability화
- 런타임에서 ScriptableObject 에셋 값을 수정해 저장하는 기능

---

## 4. 확정 설계 결정

| ID | 결정 |
|----|------|
| D-01 | `ScriptableObject`는 불변 정의, 일반 C# 런타임 인스턴스는 가변 상태를 소유한다 |
| D-02 | KCC 상태 머신은 이동·회전·상태 물리의 최종 소유자다 |
| D-03 | MotionSet은 애니메이션, Collision, VFX/SFX 이벤트 타이밍의 최종 소유자다 |
| D-04 | 기존 전투 파이프라인은 피해·방어·리액션 계산의 최종 소유자다 |
| D-05 | 공통 런타임은 `GameActor`에 붙는 Actor 모듈 컴포넌트이며 새 전역 매니저를 만들지 않는다 |
| D-06 | 플레이어 입력 슬롯은 `Ability`, `Ultimate` 2개로 고정한다 |
| D-07 | 몬스터는 플레이어 2슬롯을 사용하지 않지만 공통 활성화·Effect 런타임은 재사용할 수 있다 |
| D-08 | 비용과 쿨다운의 단일 소스는 Ability 정의 데이터다 |
| D-09 | V1에서는 기존 MotionSet/HitPhase 피해를 Effect로 중복 구현하지 않는다 |
| D-10 | 신규 다형 데이터는 V1에서 `[SerializeReference]`보다 `ScriptableObject` 서브에셋 참조를 우선한다 |
| D-11 | 런타임 저장은 안정 ID와 값만 저장하며 SO 인스턴스, `object source`, 절대 종료 시각을 저장하지 않는다 |
| D-12 | UI는 런타임 구현을 변경하지 않고 읽기 전용 상태만 소비한다 |
| D-13 | 최종 Ability Core는 UPlayground 전용 타입 없이 외부 Unity 프로젝트로 분리 가능한 독립 모듈로 유지하고, 프로젝트 결합은 별도 Adapter 모듈이 담당한다 |

---

## 5. 현재 기반과 구조적 간극

### 5.1 현재 기반

| 영역 | 기존 타입 | 현재 역할 |
|------|-----------|-----------|
| 플레이어 스킬 정의 | `GameplayAbilitySO`, `AbilityVariantDefinition` | 2슬롯과 조건별 공격 Variant |
| 스킬 해석 | `ActorAbilitySystem` | 지상·공중·자원·태그·우선순위 판정 |
| 캐릭터 전투 데이터 호스트 | `AbilitySetSO` | 일반 공격, 스킬, 콤보, 차지, 교체 공격 통합 보관 |
| 자원·쿨다운 | `PlayerSkillGauge` | 단일 게이지와 2슬롯 쿨다운 |
| 실행 | `PlayerCombat`, `PlayerAttackState` | 공격 데이터 변환과 MotionSet 실행 |
| 태그 | `GameplayTagContainer` | `HashSet` 기반 상태·콤보 태그 |
| 스탯 | `ActorStatContainer`, `StatModifier` | 기본값과 시간제 수정자 |
| UI | `UI_HudSkill`, `UISkillSlot` | 스킬 슬롯, 게이지, 쿨다운, 콤보 힌트 |
| 분석 | Balance Designer, Cycle Telemetry | 정적 전투 분석과 런 이벤트 기록 기반 |

### 5.2 해결할 간극

1. 플레이어 공격·스킬·콤보·교체 공격의 단일 소스를 `AbilitySetSO`와 실행 Payload로 통합했다.
2. 비용과 쿨다운 정의는 `GameplayAbilitySO`가 소유하고 `PlayerSkillGauge`는 런타임 상태만 관리한다.
3. 회복, 버프, 디버프, 설치기, 지속 장판을 공통 데이터로 표현할 Effect 계층을 확장한다.
4. 동일 태그를 여러 소스가 부여할 때 각 소유권을 보존한다.
5. 런타임 실패 사유와 UI 표시 사유를 동일한 표준 결과로 제공한다.
6. 캐릭터 교체와 저장 시 Ability/Effect 상태를 공통 정책으로 처리한다.

---

## 6. 상위 아키텍처

```text
입력 / ComboRoute / Behavior Tree / GameplayEvent
                       │
                       ▼
              ActorAbilitySystem
              ├─ Grant 목록 확인
              ├─ Variant 해석
              ├─ 태그·상태·타깃 검사
              ├─ 자원·쿨다운 검사
              ├─ 충돌 Ability 검사
              └─ 실행 트랜잭션 생성
                       │
                       ▼
              AbilityExecution
              ├─ 상태 전환 요청
              ├─ 실행 태그 부여
              ├─ 비용·쿨다운 커밋
              └─ MotionSet / 즉시 Effect 실행
                       │
        ┌──────────────┴──────────────┐
        ▼                             ▼
상태 머신 + MotionSet          GameplayEffectController
├─ KCC 이동·회전               ├─ Instant / Duration / Infinite
├─ 애니메이션                  ├─ Periodic Tick
├─ MotionEvent                 ├─ Stack / Refresh / Replace
└─ Collision                   ├─ StatModifier
        │                      └─ GrantedTag
        ▼                             │
기존 전투 파이프라인                  │
├─ 방어·패리·회피                     │
├─ 피해·Poise·Break                   │
└─ 리액션                             │
        └──────────────┬──────────────┘
                       ▼
             Cue / UI View / Telemetry
```

### 6.1 책임표

| 계층 | 소유 책임 | 금지 책임 |
|------|-----------|-----------|
| `GameplayAbilitySO` | 불변 정의, 활성화 규칙, 비용·쿨다운·실행 참조 | 남은 쿨다운, 현재 스택 저장 |
| `ActorAbilitySystem` | Grant, 활성화, 실행·취소, 쿨다운, Effect 오케스트레이션 | KCC 속도 계산, 직접 UI 갱신 |
| `AbilityExecution` | 한 번의 실행 상태와 수명주기 | 정의 에셋 변경 |
| 상태 머신 | 이동·회전·상태별 물리, 상태 완료 | 비용과 Effect 수명주기 |
| MotionSet | 시간축 이벤트와 히트 타이밍 | 스킬 사용 가능 조건 |
| 전투 파이프라인 | 피해·방어·리액션 계산 | 버프 지속시간 |
| `GameplayEffectController` | Effect 적용·중첩·틱·만료·제거 | 타격 대상 물리 탐색 |
| UI | 읽기 전용 표시 | 쿨다운 차감, 비용 소비 |

---

## 7. asmdef와 의존성 경계

### 7.1 모듈별 배치

```text
UPlayGround.Data
└── Data/Ability/
    ├── GameplayAbilitySO.cs
    ├── AbilitySetSO.cs
    ├── AbilityVariantDefinition.cs
    ├── AbilityActivationRules.cs
    ├── AbilityCostDefinition.cs
    ├── AbilityCooldownDefinition.cs
    ├── GameplayEffectSO.cs
    ├── GameplayEffectModifierDefinition.cs
    ├── AbilityCueDefinition.cs
    └── AbilitySaveData.cs

UPlayGround.Contracts
└── Contracts/Ability/
    └── IAbilityRuntimeReader.cs

UPlayGround.Actor
└── GameActor/Gameplay/
    ├── Ability/
    │   ├── ActorAbilitySystem.cs
    │   ├── AbilityExecution.cs
    │   ├── AbilityExecutionHandle.cs
    │   ├── AbilityActivationResult.cs
    │   └── PlayerCombatAbilityDataView.cs
    ├── Effect/
    │   ├── GameplayEffectController.cs
    │   ├── GameplayEffectInstance.cs
    │   └── GameplayEffectHandle.cs
    └── Cue/
        └── GameplayCueDispatcher.cs

UPlayGround.UI
└── UI/InputPrompt/
    ├── UI_HudSkill.cs
    └── UISkillSlot.cs

Editor asmdef
└── Data/Editor/Ability/
    ├── GameplayAbilitySOEditor.cs
    ├── AbilityDataValidator.cs
    ├── AbilityMigrationWindow.cs
    └── AbilityDataExtractor.cs
```

### 7.2 의존성 규칙

- Data는 Manager, Actor, Camera, UI 구현을 참조하면 안 된다.
- Actor 런타임은 Data와 Contracts를 참조한다.
- UI는 `IAbilityRuntimeReader` 또는 UI 소비자 계약을 통해 상태를 읽는다.
- Camera Cue가 필요하면 Actor가 Camera 구현을 새로 직접 참조하지 않고 기존 카메라 계약을 사용한다.
- Editor API는 Data/Editor 또는 GameActor/Editor asmdef 안에만 둔다.
- 신규 Ability 기능을 위해 전역 `AbilityManager.Instance`를 만들지 않는다.

### 7.3 모듈 분리의 정의

이 문서에서 `모듈 분리`는 다음 두 수준을 구분한다.

| 수준 | 정의 | 판정 |
|------|------|------|
| 프로젝트 내부 모듈화 | 기존 `Data`, `Contracts`, `Actor`, `UI` asmdef 경계를 지키는 상태 | UPlayground 저장소 내부에서만 모듈로 인정 |
| 독립 재사용 모듈 | Ability 관련 폴더와 asmdef를 다른 Unity 프로젝트로 옮겨도 UPlayground 구현 없이 컴파일되고, 프로젝트 Adapter만 새로 작성하여 사용할 수 있는 상태 | 외부 재사용 가능한 별도 모듈로 인정 |

단순히 `GameplayAbilitySO` 파일을 `UPlayGround.Data`에 배치한 것만으로는 독립 재사용 모듈로 간주하지 않는다.

독립 모듈은 다음 타입을 직접 참조하면 안 된다.

- `GameActor`, `PlayerActor`, `MonsterActor`
- `PlayerSkillGauge`, `ActorStatContainer`
- `AbilityAttackInfo`, `AbilityAttackInfo`, `AnimKey`
- `PlayerSkillSlot`, `GrowthSkillType`
- UPlayground 전용 `GameplayTagId`, `StatType`
- Manager 구현, `Svc`, `ActorSvc`, `UISvc`
- Camera, UI, MotionSet, 전투 파이프라인의 구체 구현

### 7.4 목표 물리 구조

독립 모듈화를 완료할 때의 목표 asmdef 구조는 다음과 같다.

```text
UPlayGround.Ability.Core
├── Definition/
│   ├── GameplayAbilitySO
│   ├── GameplayEffectSO
│   ├── AbilitySetSO
│   └── 공용 ID·정책·저장 DTO
├── Runtime/
│   ├── AbilityExecution
│   ├── CooldownRuntime
│   ├── GameplayEffectRuntime
│   └── Handle·Stack·수명주기
└── Ports/
    ├── IAbilityOwnerPort
    ├── IAbilityExecutionPort
    ├── IAbilityResourcePort
    ├── IAbilityTagPort
    ├── IAbilityStatPort
    └── IAbilityClock

UPlayGround.Ability.Editor
├── 공용 Ability/Effect 저작 창
├── ID·중첩·참조 순환 검증
└── 프로젝트 비종속 데이터 마이그레이션 기반

UPlayGround.Ability.UPlayGround
├── GameActorAbilityAdapter
├── MotionSetAbilityExecutionAdapter
├── PlayerSkillGaugeResourceAdapter
├── GameplayTagContainerAdapter
├── ActorStatModifierAdapter
└── UPlayground 전용 검증·마이그레이션 확장
```

의존 방향은 다음 단방향만 허용한다.

```text
UPlayGround.Ability.Editor ────────→ UPlayGround.Ability.Core
UPlayGround.Ability.UPlayGround ──→ UPlayGround.Ability.Core
UPlayGround.Actor / UI ────────────→ UPlayGround.Ability.UPlayGround

UPlayGround.Ability.Core ──X──→ UPlayGround.Data / Actor / Contracts / UI / Camera / Manager
```

`UPlayGround.Ability.Core`가 Unity `ScriptableObject`, `Sprite`, `Color` 등 UnityEngine 데이터 타입을 사용하는 것은 허용한다. 단, 특정 게임의 액터·전투·입력·태그·스탯 타입을 참조하면 안 된다.

### 7.5 Core와 Adapter 책임

| 영역 | Ability Core | UPlayground Adapter |
|------|--------------|---------------------|
| Ability 정의 | 안정 ID, 활성화 규칙, 비용·쿨다운 정책 | UPlayground 실행 Payload 연결 |
| 실행 | Prepare/Commit/End/Cancel 상태와 불변식 | KCC 상태 전환, MotionSet 재생 |
| 자원 | 자원 ID와 비용 계산 | `PlayerSkillGauge` 읽기·소비 |
| 태그 | 태그 ID와 핸들 소유권 | `GameplayTagContainer` 변환 |
| 스탯 | Modifier 연산과 source token | `StatType`, `ActorStatContainer` 적용 |
| Effect | Duration, Periodic, Stack, Remove | Heal, 전투 피해, 프로젝트 Cue 라우팅 |
| 시간 | `IAbilityClock`을 통한 시간 소비 | Unity `Time` 기반 구현 |
| UI | 읽기 전용 View State | HUD 슬롯·아이콘 바인딩 |
| 저장 | 안정 ID와 남은 값 DTO | 캐릭터 저장소와 정의 에셋 탐색 |

Core는 상태 전환이나 MotionSet 실행 성공 여부를 `IAbilityExecutionPort`의 결과값으로만 받아야 한다. Core가 `PlayerAttackState`, `EnemyAttackState`, `PlayerCombat`을 직접 호출하는 것은 금지한다.

### 7.6 실행 Payload 분리

공용 `GameplayAbilitySO`는 `AbilityAttackInfo` 또는 `AnimKey`를 직접 보관하지 않는다. 프로젝트별 실행 데이터는 Core가 정의한 추상 Payload 참조 또는 안정 실행 키로 연결한다.

```csharp
public abstract class AbilityExecutionPayloadSO : ScriptableObject
{
    public string executionId;
}
```

```text
GameplayAbilitySO
└── Variant
    ├── variantId
    ├── priority
    ├── 공용 조건
    └── AbilityExecutionPayloadSO
            │
            ▼
UPlayGroundMotionAbilityPayloadSO
├── AnimKey
└── AbilityAttackInfo
```

`UPlayGroundMotionAbilityPayloadSO`는 `UPlayGround.Ability.UPlayGround`에 둔다. 이를 통해 Core 데이터와 런타임은 Uplayground의 MotionSet 및 전투 데이터 구조를 알지 않는다.

### 7.7 구현 과정의 전환기 예외와 해소 상태

V1 수직 슬라이스 초기에는 회귀 위험을 낮추기 위해 다음 임시 결합을 허용했다.

- `AbilityVariantDefinition`의 `AnimKey`, `AbilityAttackInfo` 직접 참조
- `ActorAbilitySystem`의 `GameActor`, `PlayerActor`, `PlayerSkillGauge` 직접 연결
- Effect 런타임의 `ActorStatContainer`, `GameplayTagContainer` 직접 연결
- `PlayerSkillSlot` 기반 플레이어 2슬롯 바인딩

현재 Variant의 직접 실행 필드와 호환 Resolver/폴백은 제거되었고 실행 데이터는
`AbilityExecutionPayloadSO`만 소유한다. 자원·태그·스탯 접근도 Port를 통한다.
`PlayerSkillSlot`은 입력 슬롯 바인딩으로 유지하지만 공격 데이터 원본은 아니다.
`ActorAbilitySystem`과 Effect 수명주기의 UPlayground 연결은 프로젝트 어댑터 계층에 남아 있으므로
전체 시스템을 독립 재사용 모듈 완료로 간주하지 않는다.

분리는 다음 순서로 진행했으며 4~5번은 후속 작업이다.

1. 공용 ID, 정책, 저장 DTO, 실행 상태를 `UPlayGround.Ability.Core`로 이동했다.
2. `AnimKey`와 공격 데이터를 `UPlayGroundMotionAbilityPayloadSO`로 이동했다.
3. 자원·태그·스탯·시간 접근을 Port 인터페이스로 교체했다.
4. 기존 `ActorAbilitySystem`을 UPlayground Adapter 또는 Adapter 조립 컴포넌트로 축소한다.
5. 공용 에디터와 UPlayground 전용 MotionSet 검증 확장을 분리한다.

### 7.8 독립 모듈 완료 조건

다음 조건을 모두 만족해야 Ability 시스템을 외부 재사용 가능한 별도 모듈이라고 표기할 수 있다.

- `UPlayGround.Ability.Core.asmdef`가 UPlayground의 Data, Contracts, Actor, UI, Camera, Manager asmdef를 참조하지 않는다.
- Core 소스에서 UPlayground 전용 타입과 네임스페이스 참조가 0건이다.
- Core 테스트가 `GameActor`, `PlayerCombat`, `PlayerSkillGauge`, MotionSet 없이 실행된다.
- 샘플 Adapter만으로 일반 `MonoBehaviour` 소유자에서 Ability Prepare/Commit/Effect 만료가 동작한다.
- UPlayground Adapter를 제거해도 Core와 공용 Editor가 컴파일된다.
- 다른 Unity 프로젝트에서 자원·태그·스탯 Port의 대체 구현을 주입할 수 있다.
- 실행 Payload를 교체해도 Core의 Ability/Effect 수명주기 코드를 수정하지 않는다.
- 패키지 또는 독립 폴더 단위로 내보낼 때 UPlayground 에셋 GUID에 의존하지 않는다.

### 7.9 구현 진행 상태 (2026-07-18)

UPlayground V1 수직 슬라이스, 실제 데이터 전환, 레거시 제거와 자동 검증까지 완료했다.
외부 재사용 가능한 독립 패키지 승격은 진행 중이다.

- `UPlayGround.Ability.Core` asmdef를 생성했고 UPlayground 프로젝트 asmdef 참조는 0건이다.
- 실행 상태, 활성화 결과, UI View State, `IAbilityClock`, 자원·태그·스탯·실행 Port,
  쿨다운 런타임, 추상 실행 Payload가 Core에 배치되었다.
- `UPlayGround.Ability.UPlayGround` asmdef에 `UPlayGroundMotionAbilityPayloadSO`를 배치했다.
- 초기 전환용 Variant 호환 Resolver와 직접 실행 필드는 데이터 변환 완료 후 제거했다.
- `ActorAbilitySystem`의 쿨다운은 Core 런타임을 사용하며, 자원·태그 접근은
  `UPlayGroundAbilityOwnerPorts`를 거친다.
- Effect의 중첩/갱신/교체 판정은 Core의 `AbilityEffectStackRuntime`을 사용하고,
  자원·태그·스탯 적용과 핸들 제거는 Port 인터페이스를 거친다.
- Variant 실행 데이터는 `AbilityExecutionPayloadSO`만 소유한다.
- 플레이어 캐릭터 교체와 사망 시 Ability/Effect 정리 정책을 연결했고,
  캐릭터별 쿨다운·저장 허용 Effect·자원 DTO를 실제 파티 세이브에 연결했다.
- `Self`/`Ally`/`Enemy` 대상 관계와 Self 자동 대상 해석을 런타임에 적용했다.
- `GameplayCueDispatcher`가 시작·실패·종료·쿨다운 준비 신호를 액터 로컬 이벤트로 제공한다.
  - UI Toolkit Ability Editor는 생성·저장·참조 검사 기반 안전 삭제·전체 검증을 제공한다.
  - 2026-07-18 실제 프로젝트 데이터 일괄 변환을 완료했다.
    - 8개 캐릭터 전투 Set에 일반 공격·반격·교체 공격·차지·연계 라우트를 포함한
      GameplayAbility 에셋 210개와 Variant/Payload 221개를 구성했다.
    - `CharacterModelData`의 런타임 데이터 소스를 `AbilitySetSO` 하나로 통합하고
      `PlayerCombat`은 `PlayerCombatAbilityDataView`를 통해 Payload를 소비한다.
    - 밸런스 분석·몬테카를로·스냅샷·전투 검증 도구도 `AbilitySetSO`를 소비한다.
    - 기존 Player 프리팹 참조와 밸런스 시나리오 참조를 신규 Set으로 전환한 뒤
      `Assets/10.Datas/Actor/Player/AttackData`의 원본 에셋 9개를 제거했다.
    - 변환 완료 후 일회성 마이그레이션 UI, 구형 플레이어 공격 SO 타입·전용 에디터,
      Variant V1 중복 실행 필드와 런타임 폴백을 제거했다.

아직 `GameplayAbilitySO`, `GameplayEffectSO`, `AbilitySetSO` 정의 자체와 Effect 수명주기는
`UPlayGround.Data`/Actor 호환 계층에 남아 있다. 따라서 현재 상태를 독립 재사용 모듈
완료로 표기하면 안 되며, 7.8의 샘플 Adapter와 공용 Editor 분리까지 완료한 뒤 승격한다.

---

## 8. 용어와 식별자

| 용어 | 정의 |
|------|------|
| Ability | 조건 검사와 수명주기를 가진 실행 가능한 행동 |
| Definition | ScriptableObject에 저장된 불변 Ability 데이터 |
| Grant | Actor가 특정 Ability를 소유하게 된 상태 |
| Variant | 하나의 Ability 입력이 상황에 따라 선택하는 실행 변형 |
| Execution | 한 번 활성화된 Ability의 런타임 인스턴스 |
| Effect | 스탯, 현재 자원, 태그를 즉시 또는 일정 시간 변경하는 데이터 |
| Cue | 계산과 분리된 VFX, SFX, 카메라, UI 표현 신호 |
| Spec | 적용 순간 캡처한 Source, Target, Level, Magnitude 데이터 |
| Handle | 실행 또는 Effect 인스턴스를 정확히 종료·제거하기 위한 런타임 키 |

### 8.1 안정 ID 규칙

모든 Ability와 Effect는 사람이 읽을 수 있는 영구 ID를 가진다.

```text
Ability.Player.Bokusei.Skill.Basic
Ability.Player.Bokusei.Ultimate.Basic
Ability.Enemy.Golem.Slam
Effect.Buff.AttackUp.Small
Effect.Debuff.Poison.Common
Cooldown.Player.Bokusei.Skill
```

규칙:

- `abilityId`, `effectId`는 생성 후 이름 변경과 파일 이동으로 바뀌면 안 된다.
- 저장 데이터와 텔레메트리는 에셋 이름이나 GUID 대신 안정 ID를 사용한다.
- ID 변경이 필요하면 명시적 별칭 마이그레이션 테이블을 둔다.
- `schemaVersion`은 직렬화 구조 마이그레이션에 사용한다.
- ID 중복은 빌드 차단 오류다.

---

## 9. Ability 정의 데이터 스키마

### 9.1 `GameplayAbilitySO` — 신규 제안

```csharp
public sealed class GameplayAbilitySO : ScriptableObject
{
    public string abilityId;
    public int schemaVersion;

    public AbilityPresentationDefinition presentation;
    public List<GameplayTagId> abilityTagIds;
    public AbilityActivationRules activation;
    public AbilityCostDefinition cost;
    public AbilityCooldownDefinition cooldown;
    public AbilityConcurrencyPolicy concurrency;
    public List<AbilityVariantDefinition> variants;
    public List<GameplayEffectSO> commitEffects;
    public List<GameplayEffectSO> endEffects;
    public AbilityCueDefinition cues;
    public AbilityPersistencePolicy persistence;
    public AbilityBalanceMetadata balance;
}
```

위 코드는 목표 스키마를 설명하는 제안이며 현재 코드에 존재하지 않는다.

### 9.2 필드 계약

| 그룹 | 필드 | 계약 |
|------|------|------|
| 식별 | `abilityId` | 전역 유일, 런타임과 저장의 기본 키 |
| 식별 | `schemaVersion` | 1 이상, 마이그레이션 버전 |
| 표현 | `presentation` | 이름 키, 설명 키, 아이콘, HUD 색상 |
| 분류 | `abilityTagIds` | 검색·취소·분석용 분류 태그 |
| 활성화 | `activation` | Required/Blocked 태그, 지상 조건, 타깃 조건 |
| 비용 | `cost` | 자원 종류, 값, 소비 정책 |
| 쿨다운 | `cooldown` | 지속시간과 공유 그룹 ID |
| 동시성 | `concurrency` | 공존, 기존 실행 취소, 신규 실행 거절 정책 |
| 실행 | `variants` | 조건과 우선순위에 따라 실제 실행 데이터 선택 |
| Effect | `commitEffects` | 실행 커밋 직후 적용 |
| Effect | `endEffects` | 정상 종료 시 적용 |
| Cue | `cues` | 시작·실패·종료 표현 |
| 지속성 | `persistence` | 교체, 사망, 저장 시 처리 |
| 분석 | `balance` | 예상 피해, 지속시간, 역할 태그 |

### 9.3 플레이어 2슬롯

`PlayerSkillSlot`은 입력과 HUD 바인딩일 뿐 Ability 본체의 식별자가 아니다.

```text
SkillLoadoutSO
├── Ability  → GameplayAbilitySO
└── Ultimate → GameplayAbilitySO
```

필수 규칙:

- 플레이어 슬롯은 항상 2개다.
- 같은 슬롯의 지상·공중·강화·재사용 행동은 Variant로 표현한다.
- ComboRoute는 별도 입력 해석 경로로 유지하되 최종 실행은 Ability 활성화 경로를 사용할 수 있다.
- 몬스터와 보스는 `PlayerSkillSlot`을 사용하지 않는다.

### 9.4 Variant

```csharp
[Serializable]
public sealed class AbilityVariantDefinition
{
    public string variantId;
    public int priority;
    public AbilityVariantCondition condition;
    public AnimKey animKey;
    public AbilityAttackInfo attackInfo;
    public List<GameplayEffectSO> targetEffects;
    public List<GameplayEffectSO> ownerEffects;
}
```

V1 실행 정보는 프로젝트 전용 `AbilityExecutionPayloadSO` 구현이 소유한다.

선택 규칙:

1. 실행 불가능한 Variant를 제외한다.
2. 조건을 모두 만족한 Variant만 남긴다.
3. `priority`가 높은 순서로 선택한다.
4. 우선순위가 같으면 목록의 앞 항목을 선택하되 검증 경고를 발생시킨다.
5. 조건을 만족하는 Variant가 없으면 실패한다.
6. 실행 Payload가 없는 Variant는 검증 오류이며 런타임에서도 실행하지 않는다.

---

## 10. 활성화 규칙

### 10.1 조건 스키마

| 조건 | 의미 |
|------|------|
| Required Tags | 모두 보유해야 한다 |
| Blocked Tags | 하나라도 보유하면 실패한다 |
| Ground Condition | Any, Grounded, Airborne |
| Target Policy | None, Optional, Required |
| Target Relation | Self, Ally, Enemy |
| Distance | 최소·최대 거리 |
| Resource | 비용 지불 가능 여부 |
| Cooldown | Ability 또는 공유 그룹의 남은 시간 |
| Unlock | 성장 데이터의 해금 상태 |
| Concurrency | 실행 중 Ability와 공존·취소 가능 여부 |

### 10.2 표준 실패 결과 — 신규 제안

```csharp
public enum AbilityActivationResult
{
    Success,
    InvalidDefinition,
    NotGranted,
    Locked,
    MissingRequiredTag,
    BlockedByTag,
    InvalidGroundState,
    InvalidTarget,
    OutOfRange,
    InsufficientResource,
    CooldownActive,
    ConflictingAbility,
    StateTransitionRejected,
    MissingExecutionData,
}
```

계약:

- 실패 결과는 예외 대신 값으로 반환한다.
- UI, 디버그 로그, 텔레메트리는 동일 enum을 사용한다.
- 성공 전에는 자원과 쿨다운이 변경되면 안 된다.
- 실패 결과별 사용자 피드백 필요 여부는 UI 정책 데이터가 결정한다.

---

## 11. 활성화 트랜잭션

### 11.1 처리 순서

```text
TryPrepareAbility()
├─ Definition / Grant 검사
├─ Variant Resolve
├─ Tag / Ground / Target 검사
├─ Unlock 검사
├─ Resource / Cooldown 검사
├─ Concurrency 검사
└─ PreparedExecution 생성
        │
        ▼
외부 실행 시작 요청
├─ PlayerState 전환
├─ EnemyState 전환
└─ Instant Ability 실행
        │
        ├─ 실패 → AbortPreparedAbility()
        │
        └─ 성공 → CommitAbility()
                  ├─ Cost 소비
                  ├─ Cooldown 시작
                  ├─ GrantedTag 적용
                  ├─ CommitEffect 적용
                  └─ Active 상태 진입
```

### 11.2 필수 불변식

- 상태 전환 실패 전에 비용을 영구 소비하면 안 된다.
- `CommitAbility`는 한 실행에 한 번만 성공해야 한다.
- Prepared 상태는 프레임을 무기한 넘기면 안 된다.
- 취소와 정상 종료는 동일한 정리 경로를 사용해야 한다.
- 종료 시 실행 태그, 임시 Effect, 핸들이 남으면 안 된다.
- 쿨다운 시작 정책은 기본적으로 실행 커밋 시점이다.

### 11.3 실행 상태

```text
Created
  └─ Prepared
       ├─ Aborted
       └─ Active
            ├─ Ending
            │    └─ Ended
            └─ Cancelling
                 └─ Cancelled
```

`AbilityExecution`은 다음 정보를 가진다.

| 필드 | 내용 |
|------|------|
| Handle | 런타임 실행 식별자 |
| Definition ID | 실행한 Ability ID |
| Variant ID | 선택된 Variant |
| Source / Owner / Target | 실행 주체와 대상 |
| Start Time | 런타임 시작 시간 |
| State | Prepared, Active, Ended, Cancelled |
| Granted Tag Handles | 종료 시 정확히 제거할 태그 |
| Temporary Effect Handles | 실행 종료와 함께 제거할 Effect |
| Captured Values | 레벨, 피해 배율 등 시작 순간 값 |

---

## 12. 자원과 쿨다운

### 12.1 단일 소스

비용과 쿨다운 정의는 `GameplayAbilitySO`가 소유한다.

`PlayerSkillGauge`는 현재 자원과 슬롯별 쿨다운 상태를 관리하며 정의 값은 `GameplayAbilitySO`에서 읽는다.

### 12.2 자원 종류

V1 권장 자원:

| 자원 | 역할 |
|------|------|
| UltimateEnergy | Ultimate 발동 |
| Forte | 캐릭터 고유 강화 조건·비용 |
| Concerto | 교체 Intro/Outro 조건 |
| SkillCharge | 충전형 Ability 사용 횟수 |

현재 `PlayerSkillGauge.CurrentGauge`는 `UltimateEnergy` 런타임 상태로 해석한다.

### 12.3 비용 정책

| 정책 | 의미 |
|------|------|
| None | 비용 없음 |
| Fixed | 고정 값 소비 |
| All | 현재 자원 전체 소비 |
| PercentOfMax | 최대치 비율 소비 |
| ReserveUntilEnd | 실행 중 예약 후 정상 종료 시 확정, V2 선택 |

V1 기본값:

- Ability: 비용 없음 또는 Forte/Charge
- Ultimate: UltimateEnergy 전체 소비
- ComboRoute: 명시된 자원만 소비

### 12.4 쿨다운

쿨다운은 `cooldownGroupId`로 공유할 수 있다.

```text
Ability.Player.Bokusei.Skill.Ground
Ability.Player.Bokusei.Skill.Air
    └─ Cooldown.Player.Bokusei.Skill 공유
```

런타임은 다음 맵을 유지한다.

```text
Dictionary<string cooldownGroupId, CooldownRuntimeState>
```

저장 시 절대 종료 시각이 아니라 남은 초를 기록한다.

---

## 13. GameplayTag 소유권

### 13.1 현재 문제

현재 `GameplayTagContainer`는 `HashSet`이다. 두 Effect가 같은 태그를 부여한 뒤 하나가 만료되면 다른 Effect가 살아 있어도 태그가 제거될 수 있다.

### 13.2 목표 계약

신규 Ability·Effect 경로는 핸들 기반 참조 카운트를 사용한다.

```csharp
public GameplayTagHandle AddTag(GameplayTagId id, GameplayTagSource source);
public bool RemoveTag(GameplayTagHandle handle);
public int RemoveTagsBySource(GameplayTagSource source);
```

필수 규칙:

- 동일 태그를 여러 소스가 소유할 수 있다.
- 특정 핸들 제거는 해당 소유권만 제거한다.
- `HasTag`는 참조 카운트가 1 이상이면 true다.
- 기존 상태 머신의 `AddTag`/`RemoveTag`는 호환 API로 유지할 수 있다.
- Ability·Effect 내부에서 단순 `RemoveTag(GameplayTagId)` 사용은 금지한다.
- 저장 데이터에는 런타임 핸들을 저장하지 않고 Effect를 복원하며 새 핸들을 발급한다.

---

## 14. GameplayEffect 스펙

### 14.1 `GameplayEffectSO` — 신규 제안

```csharp
public sealed class GameplayEffectSO : ScriptableObject
{
    public string effectId;
    public int schemaVersion;
    public GameplayEffectDurationType durationType;
    public float durationSeconds;
    public float periodSeconds;
    public string stackingKey;
    public GameplayEffectStackPolicy stackPolicy;
    public int maxStackCount;
    public List<GameplayEffectModifierDefinition> modifiers;
    public List<GameplayResourceOperation> resourceOperations;
    public List<GameplayTagId> grantedTagIds;
    public GameplayEffectRemovalPolicy removalPolicy;
    public GameplayEffectSavePolicy savePolicy;
}
```

### 14.2 지속 타입

| 타입 | 의미 | 예 |
|------|------|----|
| Instant | 적용 즉시 종료 | 회복, 게이지 충전 |
| Duration | 일정 시간 유지 | 10초 공격력 증가 |
| Infinite | 명시적 제거까지 유지 | 장비, 패시브 |

### 14.3 중첩 정책

V1 필수 정책:

| 정책 | 재적용 결과 |
|------|-------------|
| RejectNew | 기존 Effect 유지, 신규 거절 |
| RefreshDuration | 스택 유지, 지속시간 갱신 |
| AddStackAndRefresh | 스택 증가 후 지속시간 갱신 |
| ReplaceExisting | 기존 제거 후 신규 적용 |

V2 선택:

- IndependentInstances
- ReplaceIfStronger
- ExtendDuration

### 14.4 Modifier 계산

기존 `ActorStatContainer` 공식을 유지한다.

```text
Final = (Base + ΣFlat) × (1 + ΣPercent) × ΠMultiply
```

Effect 인스턴스는 `StatModifier.source`에 임의 문자열이나 SO를 직접 넣는 대신 런타임 Effect source token을 사용한다. 제거 시 해당 인스턴스가 추가한 Modifier만 제거해야 한다.

### 14.5 현재값과 최대값 분리

| 값 | 저장 위치 |
|----|-----------|
| MaxHealth, AttackPower, Defense | `ActorStatContainer` |
| CurrentHealth | 액터 체력 런타임 |
| CurrentPoise / Break | 각 전용 런타임 컴포넌트 |
| UltimateEnergy / Forte / Concerto | 플레이어 자원 런타임 |

현재 자원 변경은 `StatModifier`가 아니라 `GameplayResourceOperation`으로 처리한다.

### 14.6 V1 Effect 범위

V1에 포함:

- 즉시 회복
- 자원 증가·감소
- 지속 StatModifier
- 태그 부여
- 주기 회복·주기 피해

V1에서 제외:

- 복잡한 조건 그래프
- Effect가 직접 타격 범위를 탐색하는 기능
- 반사·전이·오라의 범용 조합기

---

## 15. 기존 전투와 MotionSet 연동

### 15.1 피해 권위

V1에서 기존 경로가 계속 권위자다.

```text
MotionEvent_Collision
 → PlayerCombat / EnemyCombat
 → CombatResolutionPipeline
 → DamageResolver / DefenseResolver / ReactionResolver
 → HP / Poise / Break 적용
```

Ability Effect가 같은 HitPhase 피해를 다시 적용하면 안 된다.

### 15.2 Ability가 수행할 일

- 실행할 Variant와 `AnimKey` 선택
- 상태 전환 요청
- 비용·쿨다운 커밋
- 실행 중 태그 부여
- MotionSet 실행 전후 owner/target Effect 연결
- 종료·취소 정리

### 15.3 MotionEvent 연동

기존 Collision, Projectile, HealSkill, Invincibility 이벤트는 즉시 제거하지 않는다.

마이그레이션 원칙:

1. 기존 이벤트는 동작을 유지한다.
2. 신규 이벤트부터 Effect ID 또는 Ability 실행 컨텍스트를 사용할 수 있다.
3. MotionEvent가 `ActorAbilitySystem` 구체 구현에 직접 의존해야 한다면 Actor 내부 인터페이스를 둔다.
4. `[SerializeReference]` MotionEvent 타입 이동 시 `[MovedFrom]` 규칙을 지킨다.
5. managed reference 누락이 있는 상태에서는 MotionSet 에셋을 저장하거나 재직렬화하지 않는다.

---

## 16. 플레이어 스킬 통합

### 16.1 실행 흐름

```text
Ability / Ultimate 입력
        │
        ▼
AbilitySetSO 슬롯 Ability 조회
        │
        ▼
ActorAbilitySystem 활성화 판정
        │
        ▼
AbilityExecutionPayloadSO 해석 → PlayerCombat 실행
```

### 16.2 단일 소스 규칙

`AbilitySetSO → GameplayAbilitySO → AbilityExecutionPayloadSO`만 플레이어 전투 실행 데이터로 사용한다.
누락된 슬롯이나 Payload는 폴백하지 않고 명시적으로 실패한다.

### 16.3 `PlayerSkillGauge` 목표 책임

전환 후 유지:

- 현재 자원 값
- 자원 변경 이벤트
- 캐릭터별 자원 스냅샷

전환 후 제거:

- Ability 정의별 비용
- Ability 정의별 쿨다운 기본값
- Ability/Ultimate 하드코딩 사용 정책

클래스 이름 변경은 직렬화 위험이 있으므로 V1에서 강제하지 않는다. 필요하면 기존 컴포넌트를 유지하고 내부 책임만 축소한다.

---

## 17. 몬스터와 보스

몬스터는 플레이어 슬롯 구조를 사용하지 않는다.

2026-07-18 공용 AbilitySet 적용 현황:

- 플레이어/몬스터 전용 Ability 타입을 나누지 않고 모두 `GameplayAbilitySO`와
  `AbilitySetSO`를 사용한다.
- 플레이어는 입력 슬롯이 Ability를 선택하고, 몬스터는 BT와
  `EnemyCombatDecisionEvaluator`가 같은 Set의 Ability를 선택한다.
- 공격 데이터가 있는 `MonsterActorProfileSO` 50개를 공용 AbilitySet 22개에 연결했다.
- `AbilityAttackInfo` 192개를 공용 GameplayAbility에 연결했다.
- AI 선택 가중치, 공격 카테고리, 복합 전술 조건은 `AbilityAttackInfo`가 계속 소유한다.
- 거리, 쿨다운, 태그, 비용, Effect, 실행 수명주기는 `GameplayAbilitySO`와
  `ActorAbilitySystem`이 소유한다.
- MotionSet과 HitPhase 공격 판정은 기존 전투 파이프라인이 계속 최종 소유한다.

```text
Behavior Tree / EnemyCombatDecisionEvaluator
       │
       ▼
Ability 후보 ID 또는 AbilityAttackInfo 선택
       │
       ▼
ActorAbilitySystem.TryPrepareAbility()
       │
       ├─ 거리·페이즈·태그·쿨다운 검사
       └─ 실패 이유를 Blackboard/DebugTrace에 기록
       │
       ▼
EnemyAttackState 전환
```

단계적 적용:

1. 플레이어 수직 슬라이스와 전체 플레이어 전환을 완료했다.
2. 몬스터 BT 공격 선택을 공용 Ability Prepare/Commit/End 수명주기에 연결했다.
3. 적 Ability 전환 후에도 선택 확률과 공격 카테고리는 기존 AI가 계속 소유한다.
4. 연결된 공격의 거리·쿨다운 판정은 공용 Ability 런타임이 소유하며,
   미연결 레거시 공격만 기존 값을 폴백으로 사용한다.
5. 보스 페이즈별 Ability Set 추가·제거와 Play Mode 회귀 검증은 후속 단계다.

---

## 18. 캐릭터 교체와 잔류 공격

### 18.1 교체 정책

`CharacterModelData` 또는 캐릭터 성장 데이터가 캐릭터별 `AbilitySetSO`를 참조한다.

교체 순서:

```text
교체 요청
├─ 현재 Ability 취소 가능 여부 확인
├─ 현재 캐릭터 자원·쿨다운·지속 Effect 스냅샷
├─ 잔류 공격 스냅샷 생성
├─ 새 캐릭터 Ability Set Grant
└─ 새 캐릭터 런타임 스냅샷 복원
```

### 18.2 Effect 교체 정책

| 정책 | 동작 |
|------|------|
| RemoveOnSwap | 교체 시 제거, V1 기본값 |
| PersistPerCharacter | 캐릭터별 스냅샷에 저장 후 복귀 시 복원 |
| PersistOnPlayerActor | 모델 교체와 무관하게 단일 PlayerActor에 유지 |
| PartyShared | 파티 공용, V2 선택 |

정책이 명시되지 않은 Effect는 `RemoveOnSwap`으로 처리한다.

### 18.3 잔류 공격

`SwapResidualAttackRunner`는 `ActorAbilitySystem`을 복제하지 않는다.

필수 규칙:

- 스왑 시점의 `AttackData`, HitPhase, 실행 배율을 불변 스냅샷으로 받는다.
- 비용과 쿨다운을 다시 소비하지 않는다.
- owner buff를 다시 적용하지 않는다.
- 잔류 공격은 스킬 게이지를 충전하지 않는 기존 정책을 유지한다.
- target on-hit Effect가 필요하면 스냅샷에 명시된 Effect ID와 중복 방지 키를 사용한다.
- 잔류 공격 종료가 이미 교체된 원본 Ability의 태그를 제거하면 안 된다.

---

## 19. UI 읽기 계약

UI는 다음 읽기 모델만 필요로 한다.

```csharp
public readonly struct AbilitySlotViewState
{
    public readonly string AbilityId;
    public readonly bool IsGranted;
    public readonly bool IsUnlocked;
    public readonly bool IsReady;
    public readonly AbilityActivationResult BlockReason;
    public readonly float ResourceCurrent;
    public readonly float ResourceRequired;
    public readonly float CooldownRemaining;
    public readonly float CooldownDuration;
    public readonly string ResolvedVariantId;
}
```

`IAbilityRuntimeReader` — 신규 제안:

```csharp
public interface IAbilityRuntimeReader
{
    bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state);
}
```

UI 규칙:

- `Update()`에서 비용과 쿨다운을 계산하지 않는다.
- 런타임 이벤트 발생 시 슬롯 상태를 갱신한다.
- 쿨다운 숫자와 fill 진행만 표시 중 폴링을 허용한다.
- 캐릭터 교체 시 Ability ID, 아이콘, 이름, 자원 소스를 모두 다시 바인딩한다.
- `UI_HudSkill`은 Ability/Ultimate 두 슬롯만 표시하고 Variant마다 슬롯을 추가하지 않는다.

---

## 20. Cue와 표현

Cue는 전투 계산과 분리된 표현 신호다.

| 이벤트 | Cue 예 |
|--------|--------|
| Ability 시작 | 캐스팅 VFX, SFX, 카메라 프리셋 |
| 활성화 실패 | 자원 부족 SFX, 쿨다운 HUD 펀치 |
| Effect 적용 | 버프 아이콘, 오라 VFX |
| Effect 제거 | 오라 종료, 아이콘 제거 |
| Cooldown Ready | 슬롯 Ready 글로우 |

V1 원칙:

- 기존 `CombatFeedbackDispatcher`, MotionEvent VFX/SFX, 카메라 Intent를 유지한다.
- Cue는 계산 코드를 대체하지 않고 표현 라우팅을 점진적으로 통합한다.
- Camera 모듈에 Ability 구체 타입 의존을 추가하지 않는다.

---

## 21. 저장·복원 스펙

### 21.1 저장 대상

| 상태 | 저장 |
|------|------|
| Ability Grant | 캐릭터 성장/로드아웃 데이터가 이미 권위자면 중복 저장하지 않음 |
| 현재 자원 | 저장 |
| 쿨다운 남은 시간 | 정책에 따라 저장 |
| 활성 Duration/Infinite Effect | `savePolicy`가 허용할 때 저장 |
| 실행 중 Ability | 저장하지 않음, 로드 시 종료 상태 |
| 런타임 Handle | 저장하지 않음 |
| 절대 `Time.time` | 저장하지 않음 |

### 21.2 제안 DTO

```csharp
[Serializable]
public sealed class AbilityRuntimeSaveData
{
    public int version;
    public List<AbilityResourceSaveEntry> resources;
    public List<AbilityCooldownSaveEntry> cooldowns;
    public List<GameplayEffectSaveEntry> activeEffects;
}
```

```text
AbilityCooldownSaveEntry
├── cooldownGroupId
└── remainingSeconds

GameplayEffectSaveEntry
├── effectId
├── sourceActorId
├── remainingSeconds
├── stackCount
└── capturedMagnitude
```

복원 규칙:

- 정의 ID를 찾지 못하면 해당 항목만 건너뛰고 1회 경고한다.
- 음수 남은 시간은 0으로 보정한다.
- Effect 정의의 현재 `maxStackCount`를 초과하면 clamp한다.
- 런타임 핸들은 새로 발급한다.
- 저장 버전별 마이그레이션 함수를 둔다.

---

## 22. Addressables 정책

V1은 직접 ScriptableObject 참조를 권장한다.

Addressables는 다음 조건을 만족한 후 선택 도입한다.

- 안정 ID와 스키마 버전이 확정됨
- 로딩 실패 폴백이 있음
- 콘텐츠 빌드 상태 파일 보존 정책이 있음
- 코드 변경 없이 교체 가능한 데이터 범위가 정의됨

`AssetReference`를 사용할 경우 문자열 주소보다 타입 제한 필드를 사용한다.

참고:

- AssetReference: https://docs.unity3d.com/kr/Packages/com.unity.addressables%401.21/manual/asset-reference-intro.html
- Content Update: https://docs.unity3d.com/ja/Packages/com.unity.addressables%401.20/manual/ContentUpdateWorkflow.html

원격 변경 허용:

- 수치, 아이콘, 기존 태그 조합
- 기존 MotionSet과 Effect 참조 조합

Player 재빌드 필요:

- 신규 C# Effect/Action 타입
- enum·직렬화 필드 구조 변경
- 새로운 MotionEvent managed reference 타입

---

## 23. 에디터 저작 UX

### 23.1 Ability 에디터

한 화면에서 다음을 확인할 수 있어야 한다.

1. ID와 스키마 버전
2. 슬롯 또는 AI 분류
3. 활성화 조건
4. 비용과 쿨다운
5. Variant와 우선순위
6. MotionSet/AnimKey 연결
7. owner/target Effect
8. Cue
9. 저장·교체 정책
10. 정적 예상 피해와 지속시간

### 23.2 데이터 생성

MotionSet 기반 생성기는 다음을 자동 채울 수 있다.

- AnimKey
- Collision 이벤트 기반 HitPhase 수
- 스킬 카테고리
- 예상 startup/active/recovery
- 기본 쿨다운 추천값
- 기본 Danger Ring / Telegraph 경고

자동 생성은 기존 손튜닝 데이터를 기본적으로 덮어쓰지 않는다.

---

## 24. 자동 검증

### 24.1 오류

다음은 빌드 차단 오류다.

- `abilityId`, `effectId` 비어 있음 또는 중복
- Ability의 실행 가능한 Variant가 없음
- Variant의 `AnimKey.None`
- 참조 MotionSet에 AnimKey 없음
- 필수 Effect 정의 누락
- `durationSeconds`, `periodSeconds`, 비용, 쿨다운 음수
- `periodSeconds <= 0`인 주기 Effect
- 존재하지 않는 GameplayTag ID
- 중첩 키가 필요한 정책인데 `stackingKey`가 비어 있음
- Effect/Ability 참조 순환
- Data asmdef에서 Actor, Manager, UI 구현 참조

### 24.2 경고

- 같은 우선순위와 동일 조건의 Variant
- 사용되지 않는 Ability/Effect 에셋
- 제거된 플레이어 레거시 공격 타입·직접 실행 필드·호환 폴백이 재도입됨
- Ability 아이콘 또는 로컬라이즈 키 누락
- 쿨다운 그룹이 의도치 않게 여러 캐릭터에서 공유됨
- Duration Effect에 제거 정책 없음
- 저장 Effect가 복원 불가능한 런타임 참조를 요구함
- 잔류 공격 가능한 스킬에 중복 보상 Effect가 설정됨

### 24.3 검증 안전

- 검증기는 기본적으로 읽기 전용이다.
- 자동 수정은 Undo와 변경 전/후 리포트를 제공한다.
- MotionSet/Ultimate/프리팹 오류가 있는 상태에서 일괄 재직렬화하지 않는다.
- `Assets/10.Datas/`, `Assets/03.Prefabs/` 자동 변경은 반드시 diff를 확인한다.

---

## 25. 텔레메트리

### 25.1 최소 이벤트

| 이벤트 | 필수 필드 |
|--------|-----------|
| `AbilityActivationAttempt` | actor, abilityId, variantId, result, context |
| `AbilityCommitted` | actor, abilityId, resourceCost, cooldown |
| `AbilityHit` | source, target, abilityId, damage, poise, break |
| `AbilityEffectApplied` | source, target, effectId, stack |
| `AbilityCancelled` | actor, abilityId, reason, elapsed |
| `AbilityEnded` | actor, abilityId, elapsed |

### 25.2 분석 지표

- 캐릭터별 Ability/Ultimate 사용률
- 활성화 실패 사유 비율
- 적중률과 전체 피해 기여율
- 평균 취소 시점과 취소율
- 자원 부족으로 인한 미사용 시간
- 쿨다운 준비 후 실제 사용까지의 지연
- 버프 평균 유지율과 중첩 수
- 보스·사이클 단계별 스킬 성과

Balance Designer는 정적 예상값과 실제 로그를 비교해야 한다.

---

## 26. 테스트 스펙

### 26.1 EditMode

- Required/Blocked Tag 조합
- Variant 우선순위와 조건 충돌
- 비용 부족과 비용 커밋
- 공유 쿨다운 그룹
- Effect Duration 만료
- 동일 태그 다중 소유권
- Stack/Refresh/Replace 정책
- 저장 DTO 직렬화·복원
- ID 중복과 누락 검증
- Payload 필수 참조와 Variant 실행 데이터 단일 소스

### 26.2 PlayMode

- Ability 입력 → 상태 전환 → MotionSet → 종료
- 상태 전환 거절 시 자원·쿨다운 미소비
- 피격·사망·잡힘 중 활성화 차단
- 공격 캔슬과 Ability 취소 정리
- 캐릭터 교체 후 자원·쿨다운 복원
- 잔류 공격의 비용·게이지 중복 방지
- 씬 전환 후 Active Effect와 Tag 누수 없음
- HUD 슬롯 재바인딩과 쿨다운 표시
- Ultimate Sequence 중 입력·카메라·종료 정리

### 26.3 현재 자동 검증 기준

2026-07-18 Unity Test Runner 실제 실행 결과:

- EditMode: 14/14 통과
- PlayMode 수직 슬라이스: 2/2 통과
- EditMode와 PlayMode 결과 파일은 자동화 콜백을 각각 해제하여 서로 덮어쓰지 않는다.

### 26.4 회귀 검증

- 기존 일반 공격, 강공, 차지, 점프, 대시 공격
- 패리, 퍼펙트 가드, 회피 카운터
- MotionSet managed reference와 VFX 참조
- Missing Script
- Actor/Data/UI/Camera asmdef 경계
- Player Build

---

## 27. 마이그레이션 단계와 완료 상태

### Phase A — 데이터 단일 소스

목표:

- `GameplayAbilitySO`, `AbilitySetSO` 스키마 확정
- 안정 ID와 검증기 추가
- Ability/Ultimate 한 캐릭터 데이터 생성

작업:

1. 기존 플레이어 스킬 정의를 Ability 에셋으로 변환하는 에디터 도구를 만든다.
2. 비용·쿨다운을 Ability 데이터로 복사한다.
3. 기존 에셋은 변경하지 않고 비교 리포트를 만든다.
4. 신규 데이터와 원본 데이터의 충돌을 검사한다.

완료 조건:

- 동일 스킬의 조건, 비용, 쿨다운, Motion이 한 Ability 정의에서 보인다.
- ID 중복과 MotionSet 누락 검증이 동작한다.

### Phase B — 플레이어 수직 슬라이스

목표:

- Ability 슬롯 하나를 `ActorAbilitySystem`으로 실행

작업:

1. `ActorAbilitySystem`, `AbilityExecution`을 PlayerActor에 연결한다.
2. Prepare → 상태 전환 → Commit 순서를 적용한다.
3. 기존 `PlayerCombat.ExecuteSkillAttack`과 MotionSet 경로를 유지한다.
4. UI는 신규 읽기 상태를 표시한다.

완료 조건:

- 실패 시 자원·쿨다운이 소비되지 않는다.
- 실행 태그가 시작과 종료에 정확히 정리된다.
- 기존 히트 판정과 피해가 동일하다.

### Phase C — Effect V1

목표:

- 데이터만으로 회복·버프·디버프 하나씩 제작

작업:

1. Instant/Duration/Infinite Effect를 추가한다.
2. StatModifier와 GameplayTag 소유권을 연결한다.
3. Stack/Refresh/Replace를 구현한다.
4. 사망·교체·씬 전환 제거 정책을 연결한다.

완료 조건:

- 같은 태그를 두 Effect가 부여해도 하나의 만료로 사라지지 않는다.
- Duration Effect 만료 시 Modifier와 Tag가 모두 제거된다.

### Phase D — 전체 플레이어 이전

목표:

- 모든 플레이어 Ability/Ultimate를 신규 데이터로 이전

작업:

1. 캐릭터별 `AbilitySetSO` 생성
2. UI 아이콘과 이름을 Ability 데이터에 연결
3. 캐릭터별 자원·쿨다운 스냅샷
4. AbilitySet 연결 및 실행 Payload 누락 검증

완료 조건:

- 플레이어 실행 경로에서 구형 데이터 폴백 0회
- 구형 플레이어 공격 타입·에셋·마이그레이션 도구 제거

### Phase E — 몬스터와 보스

목표:

- 적 특수 행동의 쿨다운·Effect 공통화

작업:

1. 적 공격 1개를 Ability로 감싼다.
2. BT 실패 이유를 Debug Trace에 기록한다.
3. 기존 선택 가중치와 거리 조건 결과를 비교한다.
4. 보스 페이즈 Ability Set을 적용한다.

완료 조건:

- 기존 공격 빈도와 선택 분포가 허용 오차 안에서 유지된다.

### Phase F — Cue, Addressables, 밸런스 루프

목표:

- 표현 라우팅과 데이터 운영 완성

작업:

1. Cue를 기존 피드백 시스템과 연결한다.
2. Ability Data Extractor와 Replay Comparator를 구현한다.
3. 필요할 때만 Addressables 콘텐츠 업데이트를 도입한다.

---

## 28. 최종 완료 조건

구조 도입 완료는 다음을 모두 만족해야 한다.

- Ability 정의가 조건, 비용, 쿨다운, 실행, Effect의 단일 소스다.
- ScriptableObject에 런타임 쿨다운·스택·대상 상태가 기록되지 않는다.
- 상태 전환 실패 시 자원과 쿨다운이 소비되지 않는다.
- 실행·취소·사망·교체·씬 전환 후 태그와 Effect 누수가 없다.
- UI가 비용과 사용 가능 여부를 독자 계산하지 않는다.
- 기존 MotionSet, HitPhase, 전투 공식이 중복 구현되지 않는다.
- 플레이어 Ability/Ultimate 슬롯이 2개로 유지된다.
- 몬스터가 플레이어 슬롯 구조에 종속되지 않는다.
- 잔류 공격이 비용, 쿨다운, 게이지 보상을 중복 적용하지 않는다.
- 저장 데이터가 안정 ID와 남은 시간으로 복원된다.
- ID, MotionSet, Effect, Tag 자동 검증 오류가 0이다.
- 독립 모듈 완료를 선언하는 경우 `UPlayGround.Ability.Core`의 UPlayground 전용 타입·asmdef 의존이 0이다.
- Unity 컴파일 오류 0, Missing Script 0, managed reference/VFX 누락 0이다.
- Play Mode 서비스 경고·예외 0, Player Build 오류 0이다.

---

## 29. V1 구현 컷

첫 구현은 다음 범위로 제한한다.

1. `GameplayAbilitySO`와 캐릭터별 `AbilitySetSO`
2. 플레이어 Ability 슬롯 하나의 Prepare/Commit/End 수명주기
3. Ability 데이터 기반 비용과 쿨다운
4. Instant Heal Effect
5. Duration AttackPower Buff Effect
6. 참조 카운트 기반 GrantedTag
7. HUD 읽기 상태
8. ID·Variant·MotionSet·Effect 검증
9. EditMode 테스트와 Play Mode 수직 슬라이스

다음은 V1 이후다.

- 모든 일반 공격과 이동 상태 Ability화
- 범용 Action 그래프
- 복잡한 오라·전이·반사 Effect
- 몬스터 전체 이전
- 원격 Addressables 밸런스 패치

이 범위는 거대한 프레임워크를 먼저 만드는 것을 피하면서도, 이후 스킬을 데이터만으로 확장할 수 있는 최소한의 구조적 기반을 제공한다.
