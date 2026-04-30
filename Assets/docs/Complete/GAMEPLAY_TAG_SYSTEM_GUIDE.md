# GameplayTag 시스템 가이드

## 개요

Unreal Engine의 GameplayTag 개념을 차용한 **계층형 런타임 태그 시스템**입니다. `'.'`으로 구분된 계층 구조 태그(`State.Combat.Sprint` 등)를 액터별 컨테이너에 부착해, 상태 머신·전투 로직·AI·UI가 액터의 현재 상황을 빠르게 질의할 수 있게 합니다.

핵심 특징:

- **enum 우선 사용** — 코드에서는 항상 `GameplayTagId` enum을 통해 태그를 추가/조회한다. 문자열 직접 사용 금지.
- **데이터 주도** — 태그 정의는 `GameplayTagRegistrySO`(SO)에 저장되고, 전용 에디터 창이 `GameplayTagsGenerated.cs` 코드를 자동 생성한다.
- **계층 쿼리** — `IsChildOf`, `HasTagInHierarchy`로 부모 태그 한 개로 자식 태그 묶음을 한 번에 검사 가능.
- **상태 머신 통합** — 모든 `GameActor`에 `GameplayTagContainer`가 자동 부착되며, 각 상태의 `OnEnter` / `OnExit`에서 태그를 갱신한다.
- **이벤트 콜백** — `OnTagAdded` / `OnTagRemoved` 이벤트로 태그 변화를 외부 컴포넌트가 구독 가능.

---

## 아키텍처

```
GameplayTagRegistrySO (SO)              GameplayTagsGenerated.cs (auto)
└── List<GameplayTagDefinition>    ──►  enum GameplayTagId { ... }
       (tagName / enumName /             + s_TagNames[]
        description / color)             + ToTag() / TagName() 확장
            │
            │ 코드 생성 (에디터 창 "▶ 코드 생성" 버튼)
            ▼
GameplayTagRegistryEditorWindow
   메뉴: UPlayGround/GameplayTag/Tag Registry Editor

런타임:
GameActor (Awake)
└── GameplayTagContainer (자동 부착)
       ├── HashSet<GameplayTag>
       ├── AddTag / RemoveTag / RemoveTagsWithParent
       ├── HasTag / HasTagInHierarchy / HasAnyTag / HasAllTags
       └── OnTagAdded / OnTagRemoved 이벤트

상태 머신:
PlayerActorState / EnemyActorState
   └── OnEnter  → gameActor.Tags.AddTag(GameplayTagId.X)
   └── OnExit   → gameActor.Tags.RemoveTag(GameplayTagId.X)
```

### 파일 구조

```
Assets/02.Scripts/Gameplay/Tag/
├── GameplayTag.cs                      태그 값 객체 (struct, IEquatable)
├── GameplayTagContainer.cs             액터 부착용 런타임 컨테이너
├── GameplayTagRegistrySO.cs            태그 정의 SO + ResetToDefaults
├── GameplayTagsGenerated.cs            자동 생성된 enum + 확장 메서드
├── GameplayTagRegistry.asset           런타임/에디터 기본 SO 인스턴스
└── Editor/
    └── GameplayTagRegistryEditorWindow.cs   정의 편집 + 코드 생성기
```

---

## 핵심 클래스

### GameplayTag (struct)

`'.'`으로 계층을 표현하는 직렬화 가능한 값 객체. 문자열 한 개를 보관하며 비교는 ordinal로 한다.

| 멤버 | 설명 |
|------|------|
| `string TagName` | 원본 태그 문자열. 예) `"State.Combat.Sprint"` |
| `bool IsChildOf(GameplayTag parent)` | `parent`와 동일하거나 `parent.` 접두사로 시작하면 true |
| `bool IsValid()` | `TagName`이 비어 있지 않으면 true |
| `implicit operator GameplayTag(string)` | 문자열 → 태그 암시 변환 (테스트/외부 데이터 입력용) |

> **참고:** 코드 작성 시에는 `GameplayTagId`를 사용하고 `id.ToTag()`로 변환하는 것이 정석. 문자열 암시 변환은 테스트 또는 동적 입력에서만 사용한다.

### GameplayTagDefinition

`GameplayTagRegistrySO`의 항목 한 개. 에디터에서만 사용된다.

| 필드 | 용도 |
|------|------|
| `tagName` | 계층형 태그 이름. 예) `State.Combat.Sprint` |
| `enumName` | 생성될 enum 멤버 이름. 비우면 `tagName`의 `'.'` → `'_'` 자동 변환 |
| `description` | 에디터 표시용 설명 |
| `color` | 에디터 시각화 색상 |

핵심 헬퍼: `GetEffectiveEnumName()` — enumName이 비어 있을 때 자동 명명을 반환.

### GameplayTagRegistrySO

프로젝트 전역 태그 정의 목록 SO.

- 메뉴: `Create → UPlayGround/GameplayTag/Tag Registry`
- 기본 위치: `Assets/02.Scripts/Gameplay/Tag/GameplayTagRegistry.asset`
- `ResetToDefaults()` — 프로젝트 표준 태그(State / Combat / Combo 계층)로 초기화

### GameplayTagContainer

`GameActor`가 보유하는 런타임 태그 셋. `GameActor.Awake`에서 `GetOrAddComponent`로 자동 부착되며 `GameActor.Tags`로 접근한다.

```csharp
// 추가 / 제거
container.AddTag(GameplayTagId.State_Combat_Attack);
container.RemoveTag(GameplayTagId.State_Combat_Attack);
container.RemoveTagsWithParent("State.Combat");   // State.Combat.* 전부 제거

// 쿼리
container.HasTag(GameplayTagId.State_Sprint);                    // 정확 일치
container.HasTagInHierarchy(new GameplayTag("State.Combat"));   // 계층 검사
container.HasAnyTag(new[] { tagA, tagB });                      // 하나라도
container.HasAllTags(new[] { tagA, tagB });                     // 전부

// 변화 이벤트
container.OnTagAdded   += tag => Debug.Log($"[+] {tag}");
container.OnTagRemoved += tag => Debug.Log($"[-] {tag}");

// 일괄 제거
container.Clear();
```

> **주의:** `Clear()` / `RemoveTagsWithParent()`도 항목별로 `OnTagRemoved`를 발화한다. 이벤트 핸들러에서 컬렉션을 변경하지 말 것.

### GameplayTagId (자동 생성)

`GameplayTagsGenerated.cs`에 enum + 확장 메서드 형태로 생성된다. 코드는 직접 편집하지 말고 에디터의 **코드 생성** 버튼으로만 갱신한다.

현재 정의된 태그 (생성된 GameplayTagId 기준):

| Id | TagName | 설명 |
|----|---------|------|
| `State_Move` | `State.Move` | 이동 중 |
| `State_Sprint` | `State.Sprint` | 전력 질주 |
| `State_Dash` | `State.Dash` | 대시 |
| `State_Jump` | `State.Jump` | 점프 입력 진입 |
| `State_Airborne` | `State.Airborne` | 공중 상태 |
| `State_Crouching` | `State.Crouching` | 웅크림 |
| `State_Dodge` | `State.Dodge` | 회피 |
| `State_Combat` | `State.Combat` | 전투 상태 (부모) |
| `State_Combat_Attack` | `State.Combat.Attack` | 공격 중 |
| `State_Combat_Guard` | `State.Combat.Guard` | 가드 중 |
| `State_Combat_Charge` | `State.Combat.Charge` | 차지 중 |
| `State_Combat_DashAttack` | `State.Combat.DashAttack` | 대시 공격 |
| `State_Combat_JumpAttack` | `State.Combat.JumpAttack` | 점프 공격 |
| `State_Combat_Counter` | `State.Combat.Counter` | 반격 |
| `State_Combat_ParryCounter` | `State.Combat.ParryCounter` | 패리 반격 |
| `State_Hit` | `State.Hit` | 피격 |
| `State_Death` | `State.Death` | 사망 |
| `State_Grabbed` | `State.Grabbed` | 잡힌 상태 |
| `State_Interaction` | `State.Interaction` | 상호작용 |
| `Combo_Light` | `Combo.Light` | 약 공격 입력됨 |
| `Combo_Heavy` | `Combo.Heavy` | 강 공격 입력됨 |

확장 메서드:

```csharp
GameplayTagId.State_Combat_Attack.ToTag();    // → GameplayTag("State.Combat.Attack")
GameplayTagId.State_Combat_Attack.TagName();  // → "State.Combat.Attack"
```

---

## 셋업 방법

신규 프로젝트에서 처음 셋업하거나 태그 정의를 갱신할 때 절차.

1. **레지스트리 에셋 생성**
   - Project 창에서 우클릭 → `Create → UPlayGround/GameplayTag/Tag Registry`
   - 기본 경로: `Assets/02.Scripts/Gameplay/Tag/GameplayTagRegistry.asset`
2. **에디터 창 열기**
   - Unity 메뉴 `UPlayGround → GameplayTag → Tag Registry Editor`
   - 창이 열리면 `GameplayTagRegistry.asset`이 자동 로드된다.
3. **기본 태그로 초기화 (선택)**
   - 좌측 툴바의 **기본 태그로 초기화** 버튼 → `ResetToDefaults()` 실행.
   - 프로젝트 표준 태그(State / Combat / Combo) 19개가 한 번에 생성된다.
4. **태그 추가 / 편집**
   - 좌측 패널: 태그 행 추가/삭제/순서 변경
   - 우측 패널: 선택된 태그의 `tagName`, `enumName`, `description`, `color` 편집
   - `enumName`은 비워두면 `tagName`에서 자동 생성됨
5. **코드 생성**
   - 툴바의 **▶ 코드 생성** 버튼 클릭
   - `GameplayTagsGenerated.cs` 가 갱신되며 Unity 자동 재컴파일
   - 컴파일 완료 후 새 `GameplayTagId` 멤버 사용 가능
6. **(선택) 디폴트 컨테이너 설정**
   - 별도 셋업 불필요. `GameActor`가 `Awake`에서 `GetOrAddComponent<GameplayTagContainer>()`로 부착한다.

---

## 사용 예시

### 1. 상태 머신에서 태그 갱신 (가장 흔한 케이스)

```csharp
// PlayerGroundMoveState.cs
public override void OnEnter()
{
    base.OnEnter();
    gameActor.Tags?.AddTag(GameplayTagId.State_Move);
    // ...
}

public override void OnExit(GameActorState nextState)
{
    base.OnExit(nextState);
    gameActor.Tags?.RemoveTag(GameplayTagId.State_Move);
    gameActor.Tags?.RemoveTag(GameplayTagId.State_Sprint);
}

// 부분 상태(Sprint)는 UpdateState 안에서 동적으로 갱신
public override void UpdateState()
{
    if (IsSprinting) gameActor.Tags?.AddTag(GameplayTagId.State_Sprint);
    else             gameActor.Tags?.RemoveTag(GameplayTagId.State_Sprint);
}
```

### 2. 가드 → 반격 전환 (태그를 다음 상태에 신호로 사용)

```csharp
// PlayerGuardState.cs : 퍼펙트 가드 반격 창
playerActor.Tags?.AddTag(GameplayTagId.State_Combat_Counter);
playerController.TransitionToState(new PlayerAttackState(playerController));

// PlayerAttackState.cs : 다음 OnEnter에서 태그를 읽어 분기
_isCounter = gameActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false;
if (_isCounter)
    gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Counter);   // 1회용 신호
```

> 상태 간 1회용 신호로 태그를 사용할 때는 **읽고 즉시 제거**가 원칙. 그래야 다음 진입 시 잘못 트리거되지 않는다.

### 3. 컴포넌트 단의 콤보 태그

```csharp
// PlayerCombat.cs
public AttackData ExecuteAttack(bool isCombo)
{
    _playerActor.Tags?.AddTag(GameplayTagId.Combo_Light);
    // ...
}

// 콤보 종료 시 일괄 제거
public void ResetCombo()
{
    _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Light);
    _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Heavy);
}
```

### 4. 계층 쿼리 (전투 상태 일괄 검사)

```csharp
// "State.Combat.*" 중 하나라도 보유했다면 전투 중
if (actor.Tags.HasTagInHierarchy("State.Combat"))
{
    // 모든 전투 하위 상태 일괄 처리
}

// "State.Combat.*" 전부 제거 (사망 처리 등)
actor.Tags.RemoveTagsWithParent("State.Combat");
```

### 5. 태그 변화 이벤트 구독

```csharp
private void OnEnable()
{
    var tags = GetComponent<GameplayTagContainer>();
    tags.OnTagAdded   += HandleTagAdded;
    tags.OnTagRemoved += HandleTagRemoved;
}

private void HandleTagAdded(GameplayTag tag)
{
    if (tag.IsChildOf("State.Combat"))
        EnableCombatHUD();
}
```

---

## 에디터 도구

### GameplayTagRegistryEditorWindow

| 항목 | 값 |
|------|-----|
| 메뉴 경로 | `UPlayGround/GameplayTag/Tag Registry Editor` |
| 코드 생성 출력 | `Assets/02.Scripts/Gameplay/Tag/GameplayTagsGenerated.cs` |
| 기본 SO 경로 | `Assets/02.Scripts/Gameplay/Tag/GameplayTagRegistry.asset` |

기능:

| 기능 | 설명 |
|------|------|
| 좌측 태그 목록 | `ReorderableList` 기반. 태그 행 추가/삭제/순서 변경. 색상 스와치 표시 |
| 우측 상세 패널 | 선택된 태그의 `tagName` / `enumName` / `description` / `color` 편집 |
| **기본 태그로 초기화** | `GameplayTagRegistrySO.ResetToDefaults()` 호출. 확인 다이얼로그 후 초기화 |
| **▶ 코드 생성** | 검증(빈 tagName, 중복 enumName) 후 `GameplayTagsGenerated.cs` 작성 + AssetDatabase.Refresh |
| 미리보기 | 코드 생성 직후 생성된 코드 표시 |
| 상태 메시지 | 작업 결과를 색상(성공 녹색 / 실패 황색) + 자동 만료 표시로 안내 |

검증 규칙 (코드 생성 시):

- `tagName`이 비어 있는 항목 ➜ 생성 중단
- 두 항목의 `GetEffectiveEnumName()`이 동일 ➜ 생성 중단

---

## 주의 사항

- **문자열 직접 사용 금지.** 코드에서 태그를 다룰 때는 항상 `GameplayTagId` enum을 사용한다. 문자열은 외부 데이터(SO 인스펙터, 디버그 입력) 또는 계층 검사 인자에서만 허용.
- **상태 OnExit 누락 주의.** 상태 머신 OnEnter에서 태그를 추가했다면 OnExit에서 반드시 제거해야 한다. 다른 상태 종료 경로(예외, 사망, 강제 전환)도 모두 통과해야 한다.
- **`Tags` 는 nullable로 다룬다.** `GameActor.Awake`에서 부착되지만, 일부 초기화 순서/풀링 케이스에서 일시적으로 null일 수 있어 모든 호출은 `?.`로 가드한다(코드베이스 컨벤션).
- **자동 생성 파일 직접 편집 금지.** `GameplayTagsGenerated.cs`는 헤더에 명시되어 있듯 매 코드 생성 시 덮어쓴다. 새 태그가 필요하면 SO에 추가하고 에디터에서 코드 생성을 다시 돌릴 것.
- **enumName 중복 주의.** `tagName`이 다르더라도 `enumName`이 같으면 컴파일 충돌. 자동 명명(`'.'` → `'_'`) 규칙상 `Foo_Bar`와 `Foo.Bar`가 충돌할 수 있음을 인지하고 명시적으로 enumName을 다르게 부여한다.
- **이벤트 핸들러에서 컨테이너 변경 금지.** `OnTagAdded` / `OnTagRemoved` 콜백 내부에서 같은 컨테이너를 수정하면 컬렉션 변경 예외 가능. 변경이 필요하면 다음 프레임으로 지연 처리한다.
- **씬 전환 / 풀링 시 잔존 태그 주의.** 풀에서 재사용되는 액터는 이전 태그가 그대로 남을 수 있다. `OnDespawn` 또는 재사용 진입점에서 `Tags.Clear()` 호출.

---

## 확장 포인트

### 새 태그 추가

1. `Tag Registry Editor` 열기
2. 태그 추가 (`tagName` 필수, 그 외는 선택)
3. **▶ 코드 생성** 클릭
4. 새 `GameplayTagId.{enumName}` 멤버를 코드에서 사용

### 새 태그 카테고리 (예: `Buff.*`)

- 단일 부모 태그 컨벤션 유지: 부모 태그 자체를 SO에 같이 등록하면 (예: `Buff`, `Buff.Burning`) `HasTagInHierarchy("Buff")` 로 일괄 검사 가능.
- AI / UI 측에서 부모 단위 쿼리만 하면 자식 추가 시 코드 변경 없이 자동 반영된다.

### 외부 시스템 연동

`OnTagAdded` / `OnTagRemoved` 이벤트는 `GameplayTagContainer` 단에서 즉시 발화한다. 활용 예:

| 구독자 | 활용 방안 |
|--------|-----------|
| HUD | `State.Combat.*` 진입/탈출에 따라 전투 HUD 토글 |
| VFX | `State.Combat.Charge` 진입 시 차지 이펙트 부착, 이탈 시 해제 |
| 사운드 | `State.Hit` 추가 시 피격 SE 재생 |
| AI Brain | 적이 플레이어의 `State.Combat.Counter` 보유 여부를 보고 회피 분기 |

### 태그 기반 상태 전환 게이팅

상태의 `CanTransitionState(string nextStateName)`에서 `Tags.HasTag` / `HasTagInHierarchy`로 분기 조건을 추가하면 태그가 곧 상태 전환 룰의 1차 필터로 동작한다. (예: `State.Hit` 보유 중에는 일정 시간 다른 입력 무시)

### Behavior Tree / 외부 SO 연동

태그 자체가 직렬화 가능한 `struct GameplayTag`이므로, BT 노드 인스펙터나 임의의 SO에서 `[SerializeField] GameplayTag _requiredTag;` 식으로 보관 가능. 런타임에서 `actor.Tags.HasTag(_requiredTag)`로 평가한다.
