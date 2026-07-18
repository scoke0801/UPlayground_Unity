# 버프 / 디버프 시스템 및 HUD 표시 스펙

> 문서 버전: 1.0  
> 기준일: 2026-07-18  
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP  
> 상태: 구현 전 설계 확정  
> 관련 문서: `../Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `PASSIVE_ABILITY_SYSTEM_SPEC.md`, `../onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html`

## 1. 목적

기존 `GameplayEffectSO`와 `GameplayEffectController`를 버프·디버프의 단일 런타임 기반으로 확장하고, 현재 플레이어에게 활성화된 표시 대상 Effect를 `UI_HudPlayerInfo`에 아이콘으로 노출한다.

핵심 목표는 다음과 같다.

- 버프·디버프 정의와 표시 데이터를 하나의 `GameplayEffectSO`에서 관리한다.
- Effect의 적용, 중첩, 지속시간, 교체, 저장 정책은 기존 런타임을 재사용한다.
- 각 Effect 데이터에서 HUD 아이콘 노출 여부를 선택할 수 있다.
- 일반 Effect와 패시브 조건으로 새로 발생한 시간제 Effect는 HUD 노출이 기본이다.
- 상시 패시브 자체를 버프로 표현한 Effect만 HUD 미노출이 기본이다.
- UI는 Effect를 적용하거나 제거하지 않고 읽기 전용 런타임 상태만 표시한다.
- `UI_HudPlayerInfo.prefab`의 현재 외형과 배치를 재현하는 코드형 프리팹 빌더를 만들고, 같은 빌더에서 아이콘 영역까지 생성·배선한다.

---

## 2. 범위와 비범위

### 2.1 1차 구현 범위

- `Beneficial`, `Harmful`, `Neutral` Effect 분류
- Duration / Infinite Effect의 HUD 표시
- Effect별 아이콘, 이름, 설명, HUD 노출, 정렬 우선순위 데이터
- 적용 문맥에 따른 HUD 노출 재정의
- 버프·디버프 적용, 중첩, 갱신, 교체, 만료, 캐릭터 교체 처리
- 스택 수, 남은 시간, 지속시간의 읽기 전용 View State
- `UI_HudPlayerInfo` 아이콘 생성·재사용·정렬·오버플로 표시
- 버프/디버프 색상뿐 아니라 형태 표식을 함께 사용하는 시각 구분
- 현재 프리팹을 재현하는 `UIHudPlayerInfoPrefabBuilder`
- Ability 데이터 검증과 런타임/UI 자동 테스트

### 2.2 1차 비범위

- 범용 해제(Dispel), 정화(Cleanse), 면역, 전이, 반사, 오라 조합기
- 버프·디버프 전용 신규 전역 매니저
- Effect가 직접 타격 범위를 탐색하거나 기존 전투 피해를 우회하는 기능
- 몬스터 머리 위 버프·디버프 HUD
- 아이콘 클릭, 툴팁, 전투 일시정지 상세 패널
- 독립 상태이상 축적 게이지
- 현재 프리팹에 연결되지 않은 EXP 패널의 신규 제작

정화나 해제가 필요한 콘텐츠를 추가할 때는 `GameplayEffectHandle`, `polarity`, 태그를 이용한 제거 API를 후속 스펙으로 확장한다.

---

## 3. 현재 기반

### 3.1 재사용할 런타임

| 영역 | 현재 타입 | 현재 상태 |
|------|-----------|-----------|
| Effect 정의 | `GameplayEffectSO` | ID, 극성, 지속/주기, 중첩, Modifier, 자원, 태그, 교체·저장 정책 보유 |
| Effect 인스턴스 | `GameplayEffectInstance` | Handle, 정의, 소스, 스택, 유효 지속시간, 남은 시간 보유 |
| Effect 수명주기 | `GameplayEffectController` | 적용, 중첩, 갱신, 교체, 틱, 만료, 제거, 저장·복원 구현 |
| 스택 판정 | `AbilityEffectStackRuntime` | Core 모듈에서 스택 정책 판정 |
| 플레이어 접근 | `GameActor.Effects` | 모든 GameActor에 `GameplayEffectController` 자동 구성 |
| 패시브 연계 | `PassiveAbilitySO.triggeredEffects` | 퍼펙트 회피·가드 등에서 Effect 적용 |
| UI 대상 | `UI_HudPlayerInfo` | HP, Ultimate 게이지, 레벨, EXP 런타임 표시 코드 보유 |

### 3.2 현재 구조의 간극

- `GameplayEffectSO`에는 아이콘, 표시 이름, 설명, HUD 노출 정책이 없다.
- `GameplayEffectController.StateChanged`는 존재하지만 UI가 읽을 안전한 Effect View State가 없다.
- `GameplayEffectInstance`의 런타임 필드는 내부 구현이므로 UI가 직접 참조하면 안 된다.
- `UI_HudPlayerInfo`에는 Effect 아이콘 영역과 아이콘 풀링 코드가 없다.
- `UI_HudPlayerInfo.prefab`을 재현하는 전용 빌더가 없다.
- 패시브 발동 Effect와 일반 Ability Effect가 같은 SO를 사용할 때 표시 정책을 적용 문맥별로 바꿀 수 없다.

---

## 4. 확정 설계 결정

| ID | 결정 |
|----|------|
| BD-01 | 버프·디버프의 런타임 권위자는 기존 `GameplayEffectController`다. 별도 `BuffManager`나 `DebuffManager`를 만들지 않는다. |
| BD-02 | `GameplayEffectPolarity.Beneficial`은 버프, `Harmful`은 디버프, `Neutral`은 중립 Effect로 해석한다. |
| BD-03 | 표시 데이터는 `GameplayEffectSO.presentation`에 둔다. UI 전용 데이터베이스에 복제하지 않는다. |
| BD-04 | 일반 Effect의 `showInHud` 기본값은 `true`다. |
| BD-05 | 상시 패시브 자체를 Effect로 표현하면 HUD 재정의 기본값은 `ForceHide`다. 퍼펙트 회피·가드처럼 패시브 조건으로 발생한 Trigger Effect의 기본값은 `UseDefinition`이며, 일반 버프처럼 노출한다. |
| BD-06 | Instant Effect는 활성 수명이 없으므로 아이콘을 표시하지 않는다. `showInHud` 값은 무시한다. |
| BD-07 | UI는 `IGameplayEffectRuntimeReader`의 View State만 읽고 Effect 인스턴스나 SO를 수정하지 않는다. |
| BD-08 | 구조 변경은 이벤트로 갱신하고, 남은 시간 표시만 현재 View State를 읽어 폴링한다. UI가 별도 타이머를 권위 값으로 사용하지 않는다. |
| BD-09 | 색각 구분을 위해 테두리 색상만 사용하지 않는다. 버프는 `+` 표식, 디버프는 `−` 표식을 함께 표시한다. |
| BD-10 | 한 줄 최대 10개를 표시하고 초과분은 `+N` 배지로 표시한다. 숨겨진 Effect는 초과 개수에 포함하지 않는다. |
| BD-11 | 현재 `UI_HudPlayerInfo.prefab`의 HP/Skill/Level 계층과 수치는 빌더 기준값으로 고정한다. 요청과 무관한 EXP 패널이나 애니메이터 배선 변경은 함께 수행하지 않는다. |
| BD-12 | UI Editor 코드는 `UI/Editor/`와 `UPlayGround.UI.Editor` asmdef에 둔다. |

---

## 5. 상위 아키텍처

```text
GameplayAbilitySO Variant / PassiveAbilityController / 기타 게임 규칙
                         │
                         │ ApplyEffect(definition, source, options)
                         ▼
                GameplayEffectController
                ├─ 중첩·갱신·교체
                ├─ Modifier / Tag / Resource
                ├─ 유효 지속시간·남은 시간
                ├─ 교체·저장·복원
                └─ HUD 가시성 인스턴스 값
                         │
                         │ IGameplayEffectRuntimeReader
                         ▼
                  UI_HudPlayerInfo
                  ├─ StateChanged 구독
                  ├─ 표시 대상 정렬·최대 개수 제한
                  ├─ UIGameplayEffectIcon 풀
                  └─ 남은 시간·스택·극성 표현
```

### 5.1 모듈 경계

```text
UPlayGround.Data
└─ Effect 표시 정의, 극성·가시성 enum

UPlayGround.Contracts
└─ IGameplayEffectRuntimeReader, GameplayEffectViewState

UPlayGround.Actor
└─ GameplayEffectController, GameplayEffectInstance

UPlayGround.UI
└─ UI_HudPlayerInfo, UIGameplayEffectIcon

UPlayGround.UI.Editor
└─ UIHudPlayerInfoPrefabBuilder
```

규칙:

- Data는 Actor와 UI 구현을 참조하지 않는다.
- Actor는 UI 타입을 참조하지 않는다.
- UI에 신규 `SomeManager.Instance` 접근을 추가하지 않는다.
- UI는 기존 `UISvc.Actors.Player`로 플레이어를 찾고, 플레이어가 제공하는 Effect Reader에 바인딩한다.
- 신규 Editor API는 런타임 asmdef에 포함하지 않는다.

---

## 6. 데이터 모델

### 6.1 `GameplayEffectPresentationDefinition`

제안 위치: `Assets/02.Scripts/Data/Ability/AbilityDefinitions.cs`

```csharp
[Serializable]
public sealed class GameplayEffectPresentationDefinition
{
    public string displayName = "새 Effect";
    [TextArea] public string description;
    public string nameLocalizationKey;
    public string descriptionLocalizationKey;
    public Sprite icon;

    [Tooltip("Duration/Infinite Effect를 HUD에 표시합니다.")]
    public bool showInHud = true;

    [Tooltip("값이 클수록 제한된 HUD 슬롯에서 먼저 선택됩니다.")]
    public int hudPriority;

    public bool showRemainingTime = true;
    public bool showStackCount = true;
}
```

`GameplayEffectSO`에는 다음 필드를 추가한다.

```csharp
public GameplayEffectPresentationDefinition presentation = new();
```

정책:

- 아이콘이 null이어도 `showInHud == true`인 Effect를 조용히 숨기지 않는다.
- 런타임에서는 공용 fallback 아이콘을 표시하고, 검증기에서는 경고를 발생시킨다.
- `displayName`은 디버그와 fallback용이다. Localization 시스템 연결 시 key를 우선한다.
- 테두리 색상은 극성에서 결정하므로 데이터에 버프/디버프 색을 중복 저장하지 않는다.

### 6.2 적용 문맥 가시성

```csharp
public enum GameplayEffectHudVisibility
{
    UseDefinition,
    ForceShow,
    ForceHide,
}

public readonly struct GameplayEffectApplicationOptions
{
    public readonly GameplayEffectHudVisibility HudVisibility;
}
```

적용 API 제안:

```csharp
public GameplayEffectHandle ApplyEffect(
    GameplayEffectSO definition,
    GameActor source = null,
    GameplayEffectApplicationOptions options = default);
```

최종 표시 여부:

```text
ForceShow      → true
ForceHide      → false
UseDefinition  → definition.presentation.showInHud
Instant        → 항상 false
```

최종 표시 여부는 적용 시 `GameplayEffectInstance`에 캡처한다. SO를 런타임에 변경하지 않는다.

### 6.3 패시브 자체와 패시브 발동 버프의 기본 정책

`PassiveAbilitySO`에 다음 데이터 값을 추가한다.

```csharp
[Header("Passive HUD")]
public GameplayEffectHudVisibility passiveEffectHudVisibility =
    GameplayEffectHudVisibility.ForceHide;

public GameplayEffectHudVisibility triggeredEffectHudVisibility =
    GameplayEffectHudVisibility.UseDefinition;
```

규칙:

- `Always` 패시브의 문맥형 Modifier는 원래 Active Effect가 아니므로 HUD에 표시하지 않는다.
- 상시 패시브 자체를 Infinite Effect 등으로 표현해야 한다면 `passiveEffectHudVisibility`를 적용하며 기본은 `ForceHide`다.
- 퍼펙트 회피·가드가 발동하는 `triggeredEffects`에는 `triggeredEffectHudVisibility`를 적용하며 기본은 `UseDefinition`이다.
- Trigger Effect의 `presentation.showInHud` 기본값이 `true`이므로 패시브 조건으로 발생한 시간제 버프도 기본적으로 HUD에 표시된다.
- 특정 패시브 발동 버프만 숨기려면 `triggeredEffectHudVisibility`를 `ForceHide`로 설정한다.
- 정의가 숨김인 Trigger Effect를 강제로 보여주려면 `ForceShow`를 선택한다.
- 같은 `GameplayEffectSO`를 일반 스킬과 패시브가 공유해도 적용 문맥별 표시 여부가 달라질 수 있다.

### 6.4 중첩 시 표시 정책

| 중첩 결과 | HUD 표시 처리 |
|-----------|---------------|
| `RejectNew` | 기존 인스턴스의 표시 여부 유지 |
| `RefreshDuration` | 수락된 최신 적용 옵션으로 표시 여부와 시간을 갱신 |
| `AddStackAndRefresh` | 수락된 최신 적용 옵션으로 표시 여부와 스택을 갱신 |
| `ReplaceExisting` | 기존 제거 후 신규 옵션으로 새 인스턴스 생성 |

같은 `stackingKey`를 서로 다른 표시 문맥에서 재사용하면 표시 상태가 바뀔 수 있으므로, 의도하지 않은 공유는 데이터 검증 경고 대상으로 삼는다.

---

## 7. 런타임 읽기 계약

### 7.1 `GameplayEffectViewState`

제안 위치: `Assets/02.Scripts/Contracts/Ability/IGameplayEffectRuntimeReader.cs`

```csharp
public readonly struct GameplayEffectViewState
{
    public readonly ulong RuntimeId;
    public readonly string EffectId;
    public readonly string DisplayName;
    public readonly Sprite Icon;
    public readonly GameplayEffectPolarity Polarity;
    public readonly int HudPriority;
    public readonly int StackCount;
    public readonly float DurationSeconds;
    public readonly float RemainingSeconds;
    public readonly bool IsInfinite;
    public readonly bool ShowRemainingTime;
    public readonly bool ShowStackCount;
}
```

View State에는 SO 참조, `GameplayEffectInstance`, Modifier handle을 노출하지 않는다.

### 7.2 `IGameplayEffectRuntimeReader`

```csharp
public interface IGameplayEffectRuntimeReader
{
    event Action StateChanged;

    void CopyVisibleEffects(List<GameplayEffectViewState> destination);

    bool TryGetVisibleEffect(
        ulong runtimeId,
        out GameplayEffectViewState state);
}
```

동작:

- `CopyVisibleEffects`는 destination을 먼저 비우고 현재 표시 가능한 Effect만 복사한다.
- UI는 리스트를 재사용해 구조 갱신 시 할당을 만들지 않는다.
- `TryGetVisibleEffect`는 아이콘별 남은 시간 갱신에 사용한다.
- 적용, 제거, 만료, 스택 변경, 시간 갱신, 표시 여부 변경 시 `StateChanged`를 한 번 발행한다.
- 매 프레임 남은 시간 감소만으로 `StateChanged`를 발행하지 않는다.

`GameplayEffectController`가 이 계약을 직접 구현한다. 새 서비스 등록이나 전역 싱글톤은 필요하지 않다.

### 7.3 시간 권위

Effect 남은 시간은 `_owner.DeltaTime`으로 감소하므로 UI가 `Time.deltaTime`으로 별도 카운트다운을 만들면 히트스톱과 액터 로컬 시간에서 어긋날 수 있다.

따라서:

- 아이콘 목록은 이벤트 기반으로 재구성한다.
- 표시 중인 최대 10개 아이콘만 매 프레임 `TryGetVisibleEffect`로 현재 시간을 읽는다.
- 방사형 fill은 `RemainingSeconds / DurationSeconds`를 사용한다.
- Infinite Effect는 시간 fill과 초 단위 텍스트를 숨긴다.

---

## 8. 버프·디버프 런타임 규칙

### 8.1 분류

| 극성 | 의미 | 기본 테두리 | 보조 표식 |
|------|------|-------------|-----------|
| `Beneficial` | 플레이어에게 이로운 버프 | 청록 계열 `#42E39A` | 우상단 `+` |
| `Harmful` | 플레이어에게 해로운 디버프 | 적색 계열 `#FF5263` | 우상단 `−` |
| `Neutral` | 분류되지 않은 지속 Effect | 금색 계열 `#E6C15A` | 작은 점 |

순수 색상에만 의존하지 않아 색각 이상과 밝은 배경에서도 구분 가능하게 한다.

### 8.2 지속 타입

| 타입 | 런타임 | HUD |
|------|--------|-----|
| `Instant` | 즉시 적용 후 종료 | 표시 안 함 |
| `Duration` | 남은 시간이 0이 될 때 제거 | 아이콘, 시간 fill, 선택적 초 표시 |
| `Infinite` | 명시적 제거까지 유지 | 아이콘 표시, 시간 요소 숨김 |

### 8.3 적용과 제거

- Ability의 owner/target Effect, 패시브 Trigger, 게임 규칙은 모두 `ApplyEffect`를 사용한다.
- 스탯 Modifier는 기존 `ActorStatContainer` 계산 순서를 유지한다.
- Granted Tag와 Modifier는 해당 Effect handle이 소유한 항목만 제거한다.
- 캐릭터 교체는 기존 `GameplayEffectRemovalPolicy`를 따른다.
- `PersistPerCharacter` 저장·복원 시 표시 여부도 런타임 저장 정책에 맞게 복원해야 한다.
- 세이브 파일에는 UI 오브젝트를 저장하지 않는다. 필요하면 가시성 override enum 값만 Effect 저장 DTO에 추가한다.

### 8.4 패시브 지속시간 보정

현재 구현된 규칙을 유지한다.

```text
Beneficial Duration = baseDuration × BeneficialEffectDuration 배율
Harmful Duration    = baseDuration ÷ HarmfulEffectDuration 회복속도 배율
Neutral Duration    = baseDuration
```

HUD는 SO의 원본 `durationSeconds`가 아니라 인스턴스에 캡처된 `DurationSeconds`와 `RemainingSeconds`를 표시한다.

---

## 9. `UI_HudPlayerInfo` 표시

### 9.1 현재 프리팹 기준

실제 대상 에셋은 `Assets/03.Prefabs/UI/HUD/UI_HudPlayerInfo.prefab`이다. 요청에서 언급한 `UI_PlayerInfo`는 이 에셋을 뜻하는 것으로 해석한다.

현재 확인된 핵심 RectTransform은 다음과 같다.

| 오브젝트 | 부모 | 위치 | 크기 |
|----------|------|------|------|
| `UI_HudPlayerInfo` | Root | `(0, 147)` | `(480.32153, 227.99341)` |
| `HpPanel` | Root | `(0, 0)` | `(600, 40)` |
| `SkillPanel` | Root | `(0, -37)` | `(600, 40)` |
| `LevelPanel` | Root | `(-349, -21)` | `(80, 45.8643)` |

현재 프리팹에는 HP, Skill, Level 계층만 있다. `UI_HudPlayerInfo.cs`의 `_expFill`, `_expText` 필드는 현재 프리팹에 연결돼 있지 않으므로 버프/디버프 빌더 작업에서 임의의 EXP 패널을 추가하지 않는다.

또한 Root에는 기존 Animator와 Controller가 있으나 `UI_Base._animator` 직렬화 필드는 현재 null이다. 버프/디버프 빌더 구현에서 이 기존 동작을 묵시적으로 바꾸지 않고, 애니메이터 배선 수정이 필요하면 별도 회귀 검증을 거친다.

### 9.2 아이콘 영역 배치

첨부 시안의 빨간 표시 영역을 기준으로 HP 바 위쪽 빈 공간에 다음 영역을 추가한다.

```text
UI_HudPlayerInfo
├─ EffectArea                    Rect (0, 65), Size (560, 64)
│  ├─ EffectIconRow              중앙 정렬, 한 줄
│  │  └─ UIGameplayEffectIcon    런타임 풀 템플릿
│  └─ OverflowBadge              "+N"
├─ HpPanel
├─ SkillPanel
└─ LevelPanel
```

권장 레이아웃:

| 속성 | 값 |
|------|----|
| Anchor / Pivot | 중앙 `(0.5, 0.5)` |
| Anchored Position | `(0, 65)` |
| Size | `(560, 64)` |
| 아이콘 셀 | `44 × 44` |
| 아이콘 간격 | `6` |
| 최대 표시 | 10개 |
| 정렬 | 중앙 |
| Raycast | 전부 비활성 |

이 수치는 현재 600 너비 바와 루트의 상단 여백 안에서 한 줄을 유지한다. 구현 후 Game View 캡처에서 첨부 시안과 비교하고, 아이콘이 HP 숫자나 레벨 텍스트를 침범하면 `y` 값만 미세 조정한다.

### 9.3 아이콘 구성

`UIGameplayEffectIcon`은 `UI_Base` 파생이 아닌 보조 컴포넌트이므로 `UI_` 접두사를 사용하지 않는다.

```text
UIGameplayEffectIcon
├─ Border             극성 색상
├─ Icon               Effect 아이콘
├─ TimeShade          어두운 Radial360 fill
├─ PolarityBadge      + / − / ·
├─ StackBadge
│  └─ StackText       2 이상일 때만
└─ RemainingText      기본 60초 이하에서만 표시
```

표시 규칙:

- 시간 shade는 남은 시간이 줄수록 아이콘 위를 덮는 방식으로 통일한다.
- 스택 1은 숫자를 숨긴다.
- `showStackCount == false`면 2 이상이어도 숫자를 숨긴다.
- 남은 시간은 `60초 초과`, `Infinite`, `showRemainingTime == false`에서 숨긴다.
- 10초 이상은 정수, 10초 미만은 소수점 한 자리 표시를 권장한다.
- 아이콘이 null이면 fallback 아이콘과 Effect 이름 첫 글자를 사용한다.

### 9.4 정렬과 오버플로

표시 후보 선택 순서:

1. `hudPriority` 내림차순
2. `Harmful` 우선
3. Duration은 남은 시간이 짧은 순
4. `effectId` 오름차순으로 결정성 보장

선택된 최대 10개를 화면에 배치할 때는 다음 시각 순서를 사용한다.

1. Beneficial
2. Neutral
3. Harmful

제한을 초과한 표시 대상은 `OverflowBadge`에 `+N`으로 표시한다. 데이터에서 `ForceHide` 또는 `showInHud == false`인 Effect는 후보와 `N`에 포함하지 않는다.

### 9.5 생명주기

`UI_HudPlayerInfo`의 처리 순서:

```text
OnShow
  → 현재 PlayerActor 확보
  → Effect Reader 바인딩
  → StateChanged 구독
  → 전체 아이콘 Refresh

StateChanged
  → 표시 목록 복사
  → 정렬·최대 개수 선택
  → 풀에서 아이콘 할당/반환

Update
  → 현재 표시 아이콘의 runtimeId로 최신 View State 조회
  → 시간 fill과 텍스트만 갱신

OnPlayerSwapCompleted
  → 기존 Reader 구독 안전 확인/재바인딩
  → 전체 아이콘 Refresh

OnHide / OnDestroy
  → Reader 구독 해제
  → 모든 아이콘 풀로 반환
```

단일 `PlayerActor` 인스턴스가 유지되는 현재 교체 구조에서도 교체 완료 시 전체 Refresh는 반드시 수행한다.

---

## 10. 프리팹 빌더

### 10.1 파일과 메뉴

제안 파일:

`Assets/02.Scripts/UI/Editor/UIHudPlayerInfoPrefabBuilder.cs`

제안 메뉴:

`UPlayGround/UI/프리팹 빌드/HUD 플레이어 정보`

대상:

`Assets/03.Prefabs/UI/HUD/UI_HudPlayerInfo.prefab`

### 10.2 빌더 책임

- 현재 프리팹의 Root, Canvas, Animator, `UI_HudPlayerInfo` 구성 재현
- 현재 HP/Skill/Level 계층, RectTransform, 색상, Sprite, TMP 설정 재현
- 현재 Animator Controller 참조 유지
- `EffectArea`, 아이콘 템플릿, overflow 배지 생성
- 현재 프리팹에서 이미 연결된 필드와 신규 Effect 관련 `[SerializeField]` 자동 배선
- 동일 경로에 저장해 기존 프리팹 `.meta`와 GUID 유지
- 빌드 직후 프리팹을 다시 로드해 필수 참조와 Missing Script 검증

### 10.3 현재 상태 보존 원칙

- 현재 프리팹에 없는 EXP 패널을 빌더가 추측해서 만들지 않는다.
- 현재 애니메이션 클립과 Controller를 다시 생성하지 않고 기존 에셋을 참조한다.
- 기존 HP/Skill/Level 오브젝트 이름을 바꾸지 않는다.
- 프리팹 생성 실패나 필수 Sprite/Controller 누락 시 기존 프리팹을 덮어쓰지 않는다.
- Builder는 `AssetDatabase` 경로 상수를 사용하고 GUID 문자열을 코드에 직접 박지 않는다.
- 프리팹 빌드 전 컴파일 오류나 Missing Script가 있으면 실행을 중단한다.

### 10.4 직렬화 배선

빌더가 배선할 신규 필드 제안:

```csharp
[Header("Buff / Debuff")]
[SerializeField] private RectTransform _effectArea;
[SerializeField] private RectTransform _effectIconRoot;
[SerializeField] private UIGameplayEffectIcon _effectIconTemplate;
[SerializeField] private TextMeshProUGUI _effectOverflowText;
[SerializeField, Min(1)] private int _maxVisibleEffects = 10;
```

아이콘 템플릿은 프리팹 내부 비활성 자식으로 두고 런타임에 복제해 풀링한다. 별도 Addressables 로드나 `Instantiate` 반복 호출을 요구하지 않는다.

---

## 11. 데이터 저작 규칙

### 버프 예시

```text
effectId: GE_PerfectDodgeAttackUp
polarity: Beneficial
durationType: Duration
durationSeconds: 5
presentation.showInHud: true
presentation.icon: 지정
presentation.hudPriority: 50
```

패시브 조건으로 발동하는 버프는 `triggeredEffectHudVisibility == UseDefinition`과 Effect의 기본 `showInHud == true`에 따라 별도 설정 없이 아이콘을 표시한다. 특정 Trigger만 숨길 때 `ForceHide`를 사용한다.

### 디버프 예시

```text
effectId: GE_Poison
polarity: Harmful
durationType: Duration
durationSeconds: 12
periodSeconds: 1
presentation.showInHud: true
presentation.icon: 지정
presentation.hudPriority: 100
```

### 숨김 Effect 예시

```text
effectId: GE_InternalCombatMarker
polarity: Neutral
durationType: Infinite
presentation.showInHud: false
```

전투 내부 태그나 계산용 Effect는 명시적으로 숨긴다.

---

## 12. 에디터 검증

`AbilityDataValidator.ValidateEffect()`에 다음 규칙을 추가한다.

1. `presentation`이 null이면 오류.
2. `showInHud == true`인 Duration/Infinite Effect의 icon이 null이면 경고.
3. `showInHud == true`인데 displayName과 localization key가 모두 비어 있으면 경고.
4. Instant Effect에 `showInHud == true`면 “HUD 표시되지 않음” 정보 메시지.
5. `Beneficial`/`Harmful` Duration Effect의 duration이 0 이하이면 기존 규칙대로 오류.
6. `showRemainingTime == true`인 Infinite Effect는 시간 표시가 무시된다는 정보 메시지.
7. `showStackCount == true`인데 `maxStackCount == 1`이면 정보 메시지.
8. 상시 패시브 자체의 기본값은 `ForceHide`, 패시브 Trigger Effect의 기본값은 `UseDefinition`인지 검사한다. 명시적 재정의는 유효한 데이터다.
9. HUD 표시 Effect가 fallback 아이콘에 의존하는 경우 전체 검증 결과에서 한눈에 찾을 수 있어야 한다.

기존 Effect 에셋에 새 필드를 추가할 때는 `schemaVersion`을 올리고 Ability Editor에서 일괄 검증한다. 에셋을 무조건 일괄 재직렬화하지 않는다.

---

## 13. 구현 단계

### Phase 1: 데이터와 런타임 View

1. Effect 표시 정의와 HUD 가시성 enum 추가.
2. `GameplayEffectSO.presentation` 추가.
3. 적용 옵션과 인스턴스 표시 여부 캡처.
4. `IGameplayEffectRuntimeReader`와 View State 추가.
5. `GameplayEffectController` 읽기 계약 구현.
6. 상시 패시브 자체에는 기본 `ForceHide`, 패시브 Trigger에는 기본 `UseDefinition` 적용.
7. Ability 데이터 검증 확장.

완료 기준:

- 일반 Duration/Infinite Effect는 기본 표시 대상으로 조회된다.
- `showInHud == false`와 `ForceHide`는 조회되지 않는다.
- 상시 패시브 자체를 Effect로 표현한 항목은 별도 설정이 없으면 조회되지 않는다.
- 퍼펙트 회피·가드 등으로 발생한 Trigger Effect는 별도 설정이 없으면 조회된다.
- SO 원본 값은 런타임에 변경되지 않는다.

### Phase 2: HUD와 아이콘

1. `UIGameplayEffectIcon` 구현.
2. `UI_HudPlayerInfo`에 Reader 구독과 아이콘 풀 추가.
3. 정렬, 최대 10개, `+N` 처리.
4. 남은 시간, 스택, 극성 시각화.
5. OnShow/OnHide/교체 생명주기 처리.

완료 기준:

- 버프는 청록 테두리와 `+`, 디버프는 빨간 테두리와 `−`로 구분된다.
- 숨김 Effect와 Instant Effect는 나타나지 않는다.
- 갱신/중첩/만료가 같은 아이콘에 즉시 반영된다.
- UI를 반복 표시해도 이벤트와 아이콘 인스턴스가 누적되지 않는다.

### Phase 3: 프리팹 빌더

1. 현재 프리팹 계층과 시각값을 빌더 코드로 재현.
2. Effect 영역과 템플릿 생성.
3. 직렬화 필드 자동 배선.
4. 기존 경로 저장과 사후 검증.
5. 16:9, 21:9 Game View 시각 확인.

완료 기준:

- 빌더 실행 전후 HP/Skill/Level 외형과 위치가 동일하다.
- Effect 영역만 요청 위치에 추가된다.
- 프리팹 GUID와 UI DB 연결이 유지된다.
- Missing Script와 null 필수 참조가 없다.

---

## 14. 테스트 시나리오

### 14.1 EditMode

- `UseDefinition`, `ForceShow`, `ForceHide` 우선순위가 정확하다.
- Instant는 어떤 옵션에서도 표시 대상이 아니다.
- Refresh와 AddStack이 표시 여부, 스택, 유효 지속시간을 올바르게 갱신한다.
- RejectNew는 기존 표시 여부를 유지한다.
- Copy API가 숨김 Effect를 제외하고 destination을 재사용한다.
- 정렬 결과가 같은 입력에서 항상 동일하다.
- 표시 후보 13개에서 10개와 `+3`이 계산된다.

### 14.2 PlayMode

- 버프 적용 직후 아이콘이 나타나고 만료 프레임에 사라진다.
- 디버프 테두리와 보조 표식이 버프와 다르다.
- 2스택부터 스택 숫자가 표시되고 갱신된다.
- 히트스톱과 로컬 시간 배율에서 HUD 시간이 실제 Effect 만료와 일치한다.
- 상시 패시브 자체를 표현한 Infinite Effect는 기본적으로 보이지 않는다.
- 퍼펙트 회피·가드 Trigger Effect는 기본적으로 표시된다.
- Trigger Effect를 `ForceHide`로 바꾸면 같은 버프가 표시 목록에서 제외된다.
- 캐릭터 교체 시 `RemoveOnSwap` Effect는 사라지고 유지 Effect는 정책대로 복원된다.
- HUD Show/Hide를 반복해도 `StateChanged` 구독이 한 번만 유지된다.

### 14.3 프리팹·시각 검증

- Builder 실행 전후 HP, Skill, Level RectTransform과 Sprite 참조 비교.
- `EffectArea`가 HP 바 위 중앙의 지정 영역에 위치.
- 0개, 1개, 5개, 10개, 11개 아이콘에서 중앙 정렬과 overflow 확인.
- 16:9, 21:9, 2560×1440에서 잘림과 겹침 없음.
- 밝은 하늘과 어두운 실내 배경에서 테두리·표식 식별 가능.
- UI/Player 프리팹 Missing Script 0.

---

## 15. 코드 변경 후보

| 파일 | 변경 내용 |
|------|-----------|
| `Data/Ability/AbilityDefinitions.cs` | Effect 표시 정의와 HUD 가시성 enum |
| `Data/Ability/GameplayEffectSO.cs` | `presentation` 필드 |
| `Data/Ability/Passive/PassiveAbilitySO.cs` | 상시 패시브 자체 `ForceHide`, Trigger Effect `UseDefinition` 기본 정책 |
| `Contracts/Ability/IGameplayEffectRuntimeReader.cs` | 읽기 계약과 View State |
| `GameActor/Gameplay/Effect/GameplayEffectInstance.cs` | 인스턴스 HUD 표시 여부 |
| `GameActor/Gameplay/Effect/GameplayEffectController.cs` | 적용 옵션, View 투영, 이벤트 |
| `GameActor/Gameplay/Passive/PassiveAbilityController.cs` | 패시브 가시성 옵션 전달 |
| `Data/Editor/Ability/AbilityDataValidator.cs` | 표시 데이터 검증 |
| `UI/HUD/UI_HudPlayerInfo.cs` | Effect Reader와 아이콘 풀 |
| `UI/HUD/UIGameplayEffectIcon.cs` | 아이콘 표시 보조 컴포넌트 |
| `UI/Editor/UIHudPlayerInfoPrefabBuilder.cs` | 현재 프리팹 재현과 Effect 영역 생성 |
| `03.Prefabs/UI/HUD/UI_HudPlayerInfo.prefab` | Effect 영역과 직렬화 참조 |
| `Tests/EditMode/Ability/` | 표시 정책·정렬·View 테스트 |
| `Tests/PlayMode/Ability/` | Effect-HUD 수직 슬라이스 |

---

## 16. 완료 조건

- Unity 컴파일 오류 0.
- Data/Contracts/Actor/UI asmdef 경계 위반 0.
- UI의 신규 Manager 싱글톤 참조 0.
- 일반 Effect HUD 노출 기본값 true.
- 상시 패시브 자체 Effect의 HUD 노출 기본값 false.
- 패시브 조건으로 발생한 Trigger Effect의 HUD 노출 기본값 true.
- 데이터 설정으로 각 Effect의 표시/숨김 전환 가능.
- 버프·디버프의 색상과 비색상 표식 구분.
- 중첩, 갱신, 만료, 교체, 저장·복원 시 HUD 상태 일치.
- `UI_HudPlayerInfo` Builder 재생성 후 기존 HP/Skill/Level 외형 회귀 없음.
- Missing Script와 필수 직렬화 null 0.
- Ability Editor 전체 데이터 검증 오류 0.
- 관련 EditMode/PlayMode 테스트 통과.
- Unity Play Mode에서 실제 배치와 첨부 시안 위치 확인.

---

## 17. 결론

버프·디버프는 이미 존재하는 Gameplay Effect 수명주기를 확장하는 것이 현재 구조와 가장 잘 맞는다. 별도 매니저나 UI 전용 상태를 만들지 않고, Effect 정의에 표시 메타데이터를 추가하고 런타임 인스턴스를 읽기 전용 View State로 투영한다.

UI는 HP 바 위의 제한된 한 줄 영역에서 중요한 Effect를 우선 표시한다. 일반 Effect와 패시브 조건으로 발생한 시간제 버프는 기본 노출하고, 상시 패시브 자체를 Effect로 표현한 항목만 기본 미노출한다. 두 정책은 데이터에서 명시적으로 바꿀 수 있어야 한다. 프리팹은 전용 Builder가 현재 상태를 재현하고 같은 코드에서 Effect 영역까지 생성하도록 하여 수동 배선과 프리팹 드리프트를 방지한다.
