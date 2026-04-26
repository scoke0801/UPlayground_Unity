# 플레이어 전투 무기 상태 연동 가이드

> 작성일: 2026-04-26  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

플레이어의 전투 상태(`PlayerCombat.IsInCombat`) 변화에 맞춰 무기를 자연스럽게 손에 들거나 등에 넣는 처리 추가를 위한 분석 문서.

요구 동작은 다음 흐름으로 해석한다.

- 전투 상태 진입 시: 장착된 무기를 손으로 꺼내는 애니메이션 재생
- 전투 상태 해제 시: 들고 있던 무기를 자연스럽게 해제하는 애니메이션 재생
- 공격/피격/가드 등 즉시 반응이 필요한 상태에서는 무기 토글 애니메이션이 전투 액션을 끊지 않도록 지연 처리

현재 프로젝트에는 전투 상태 이벤트와 무기 토글용 애니메이션 키가 이미 존재하지만, 둘을 연결하는 런타임 제어층이 없다.

---

## 현재 구조

```
PlayerCombat
├── IsInCombat
├── RefreshCombatState()
├── ForceExitCombat()
└── OnChangeCombatState(bool)
        │
        ├── UI_GamePlay: HUD 표시/숨김에 사용 중
        └── 신규 연동 필요: PlayerEquipment 또는 별도 컴포넌트

PlayerEquipment
├── GetMainWeaponType()
├── SetWeaponType(WeaponType)
├── IsMainWeaponEquipped
├── OnEquipRightWeapon()  ← 애니메이션 이벤트 콜백
└── OnEquipLeftWeapon()   ← 애니메이션 이벤트 콜백

PlayerActorAnimator
└── PlayMotion(AnimKey)   ← 현재 WeaponType 기준 MotionSet 탐색
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 전투 상태 타이머와 `OnChangeCombatState` 이벤트 발화 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerEquipment.cs` | 무기 프리팹 생성, `ParentConstraint` weight 토글, 장착 상태 보관 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | `PlayerCombat`, `PlayerEquipment`, 입력, 캐릭터 교체 참조 갱신 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerIdleState.cs` | 기존 수동 장착/해제 테스트 코드가 주석 처리되어 있음 |
| `Assets/02.Scripts/GameActor/Animation/PlayerActorAnimator.cs` | `WeaponType`별 `MotionSet` 선택 |
| `Assets/02.Scripts/Data/Actor/Animation/PlayerActorAnimationMotionSet.cs` | 무기별 MotionSet 탐색, 없으면 `NoWeapon` fallback |

---

## 확인된 코드 지점

### PlayerCombat

`PlayerCombat`는 이미 전투 상태 변화를 이벤트로 노출한다.

| 위치 | 내용 |
|------|------|
| `PlayerCombat.cs:126` | `public event Action<bool> OnChangeCombatState;` |
| `PlayerCombat.cs:152` | `IsInCombat` 계산: 마지막 전투 이벤트 후 `_combatStateDuration` 이내 |
| `PlayerCombat.cs:235` | `UpdateCombatState()`에서 위협 탐색 및 상태 변화 감지 |
| `PlayerCombat.cs:251` | 상태가 바뀔 때 `OnChangeCombatState?.Invoke(current)` 발화 |
| `PlayerCombat.cs:294` | 공격/피격/가드 등에서 `RefreshCombatState()` 호출 가능 |
| `PlayerCombat.cs:303` | `ForceExitCombat()`으로 즉시 전투 해제 예약 가능 |

전투 상태 연동의 진입점은 새 폴링보다 이 이벤트를 사용하는 것이 맞다.

### PlayerEquipment

`PlayerEquipment`는 실제 무기를 교체하거나 손/등 위치를 바꾸는 기능을 일부 보유한다.

| 위치 | 내용 |
|------|------|
| `PlayerEquipment.cs:57` | `IsMainWeaponEquipped`로 손에 들고 있는지 기록 |
| `PlayerEquipment.cs:73` | `GetMainWeaponType()`으로 현재 무기 타입 반환 |
| `PlayerEquipment.cs:76` | `SetWeaponType()` 테스트용 타입 변경 |
| `PlayerEquipment.cs:224` | `EquipWeapon()`이 무기 프리팹을 생성하고 제약 오브젝트 아래에 배치 |
| `PlayerEquipment.cs:259` | `SetRightWeaponType()`이 오른손 무기 제약을 선택 |
| `PlayerEquipment.cs:285` | `OnEquipRightWeapon()`이 constraint source weight를 토글 |
| `PlayerEquipment.cs:316` | `OnEquipLeftWeapon()`이 왼손 무기 constraint source weight를 토글 |

현재 `OnEquipRightWeapon()` / `OnEquipLeftWeapon()`은 토글 함수라서 외부에서 "장착으로 맞춰라" 또는 "해제로 맞춰라"를 명시할 수 없다. 전투 상태 자동 연동에는 명령형 API가 추가되어야 한다.

### PlayerActor

`PlayerActor`는 컴포넌트 접근과 캐릭터 교체 시 참조 갱신을 담당한다.

| 위치 | 내용 |
|------|------|
| `PlayerActor.cs:90` | `IsEquippedRightWeapon`이 `PlayerEquipment.IsMainWeaponEquipped`를 노출 |
| `PlayerActor.cs:92` | `IsInCombat`이 `PlayerCombat.IsInCombat`을 노출 |
| `PlayerActor.cs:118` | 카메라에 전투 상태 provider 등록 |
| `PlayerActor.cs:351` | `GetPlayerEquipment()` |
| `PlayerActor.cs:352` | `GetCombat()` |
| `PlayerActor.cs:358` | `RefreshForCharacter()`에서 모델/장비/전투 데이터 갱신 |
| `PlayerActor.cs:385` | Bokusei 교체 시 `WeaponType.Katana` 설정 |
| `PlayerActor.cs:388` | 그 외 캐릭터는 `WeaponType.NoWeapon` 설정 |

캐릭터 교체가 있는 구조이므로 전투 상태 이벤트 구독은 모델 교체 후에도 현재 `PlayerEquipment`를 다시 참조해야 한다.

---

## 수정 필요 사항

### 1. 무기 타입과 손에 든 상태를 분리

현재 `WeaponType`은 `PlayerActorAnimator.PlayMotion()`의 MotionSet 선택에도 사용된다. 따라서 전투 해제 시 `SetWeaponType(WeaponType.NoWeapon)`으로 처리하면 공격/장비 데이터의 무기 타입까지 사라져 전투 모션 선택이 꼬일 수 있다.

권장 원칙:

- `WeaponType`: 어떤 무기를 장비했는지
- `IsMainWeaponEquipped`: 그 무기를 현재 손에 들고 있는지

전투 해제는 `WeaponType.NoWeapon`으로 바꾸는 것이 아니라 constraint source weight만 손에서 등으로 이동해야 한다.

### 2. PlayerEquipment에 명령형 API 추가

`OnEquipRightWeapon()`은 현재 토글만 수행한다. 자동 연동에서는 목표 상태가 명확해야 하므로 다음 API를 추가하는 것이 좋다.

```csharp
public bool CanToggleMainWeapon();
public void SetMainWeaponDrawn(bool drawn);
public void SetSubWeaponDrawn(bool drawn);
public bool TryPlayMainWeaponDrawMotion(bool drawn, ActorAnimator animator, Action onComplete = null);
```

구현 방향:

- `drawn == true`이고 이미 `IsMainWeaponEquipped == true`면 아무것도 하지 않는다.
- `drawn == false`이고 이미 `IsMainWeaponEquipped == false`면 아무것도 하지 않는다.
- 실제 weight 변경은 기존 `OnEquipRightWeapon()`의 로직을 재사용하되, 토글이 아닌 목표 상태 기반으로 분리한다.
- `AnimKey.Equip_Weapon`, `AnimKey.UnEquip_Weapon` 재생 중 지정 프레임에서 애니메이션 이벤트가 기존 콜백을 호출하도록 유지한다.

### 3. 전투 상태 이벤트를 받을 연동 컴포넌트 추가

`PlayerCombat`이나 `PlayerEquipment`에 직접 섞기보다, 역할을 분리한 `PlayerCombatWeaponStateController` 신규 컴포넌트를 권장한다.

```
Assets/02.Scripts/GameActor/Component/Player/PlayerCombatWeaponStateController.cs
```

역할:

- `PlayerCombat.OnChangeCombatState` 구독/해제
- 전투 진입 시 장착 애니메이션 요청
- 전투 해제 시 해제 애니메이션 요청
- 현재 플레이어 상태가 공격/피격/사망/잡힘이면 예약 후 Idle/GroundMove 진입 시 처리
- 캐릭터 교체 후 `PlayerActor.GetPlayerEquipment()`와 `PlayerActor.Animator` 재조회

### 4. 상태 전환 중 안전 처리 추가

장착/해제 모션은 전투 입력을 막거나 공격 루트모션을 끊으면 안 된다. 다음 상태에서는 즉시 재생하지 않는 것이 안전하다.

| 상태 | 처리 |
|------|------|
| `Attack`, `Charge`, `DashAttack`, `JumpAttack`, `FinishAttack` | 전투 모션 우선, 장착/해제 예약 |
| `Hit`, `Death`, `Grabbed`, `GuardBreak` | 예약 또는 취소 |
| `Guard`, `Dodge`, `Dash` | 필요 시 예약. 즉시 재생은 조작감 저하 가능 |
| `Idle`, `GroundMove`, `Stop`, `TurnInPlace` | 즉시 재생 가능 |

이를 위해 `PlayerCombatWeaponStateController.Update()`에서 pending 요청을 검사하거나, `PlayerActorState.OnEnter()`에서 공통 훅을 호출하는 방식을 선택할 수 있다. 침범 범위가 작은 쪽은 컨트롤러의 `Update()` 폴링 방식이다.

### 5. WeaponType별 constraint 매핑 보완

`SetRightWeaponType()`은 현재 `Sword`, `GreatSword`, `Staff`, `Bow`만 처리한다. 그런데 `PlayerActor.RefreshForCharacter()`는 Bokusei에 `WeaponType.Katana`를 직접 넣는다.

수정 필요:

```csharp
case WeaponType.Sword:
case WeaponType.Katana:
    _mainWeaponConstraint = swordConstraint;
    break;
```

Honoka(`DoubleAxe`)와 LianLian(`Whip`)은 프리팹 구조에 맞는 별도 constraint가 필요하다. 현재 `PlayerEquipment`에는 쌍도끼/채찍 전용 필드가 없으므로 데이터 또는 필드 추가가 필요하다.

---

## 권장 구현 흐름

1. `PlayerEquipment`에 목표 상태 기반 메서드를 추가한다.
2. `SetRightWeaponType()`에 `Katana`, `DoubleAxe`, `Whip` 매핑을 보완한다.
3. `PlayerCombatWeaponStateController`를 추가하고 `PlayerCombat.OnChangeCombatState`를 구독한다.
4. 전투 진입 시 `Equip_Weapon`, 전투 해제 시 `UnEquip_Weapon`을 재생하도록 연결한다.
5. 공격/피격 등 블로킹 상태에서는 pending 플래그만 저장하고, 안전 상태에서 처리한다.
6. `PlayerActor.RefreshForCharacter()` 이후에도 새 모델의 `PlayerEquipment`를 다시 참조하도록 컨트롤러에 `RefreshReferences()`를 제공한다.

---

## 예시 코드 구조

```csharp
namespace UPlayGround.Component
{
    public class PlayerCombatWeaponStateController : PlayerActorComponent
    {
        private PlayerActor _player;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private bool? _pendingDrawn;

        private void Awake()
        {
            _player = GetComponent<PlayerActor>();
            RefreshReferences();
        }

        private void OnEnable()
        {
            if (_combat != null)
                _combat.OnChangeCombatState += OnCombatStateChanged;
        }

        private void OnDisable()
        {
            if (_combat != null)
                _combat.OnChangeCombatState -= OnCombatStateChanged;
        }

        public void RefreshReferences()
        {
            _combat = _player.GetCombat();
            _equipment = _player.GetPlayerEquipment();
        }

        private void OnCombatStateChanged(bool isInCombat)
        {
            RequestDrawn(isInCombat);
        }

        private void RequestDrawn(bool drawn)
        {
            if (!CanPlayNow())
            {
                _pendingDrawn = drawn;
                return;
            }

            PlayDrawMotion(drawn);
        }

        private void Update()
        {
            if (_pendingDrawn.HasValue && CanPlayNow())
            {
                bool drawn = _pendingDrawn.Value;
                _pendingDrawn = null;
                PlayDrawMotion(drawn);
            }
        }
    }
}
```

위 코드는 구조 예시다. 실제 구현 시 `CanPlayNow()`와 `PlayDrawMotion()`은 현재 상태명, `ActorAnimator.PlayMotion()`, `PlayerEquipment.SetMainWeaponDrawn()` API에 맞춰 작성한다.

---

## 애니메이션 / 데이터 셋업

| 항목 | 필요 작업 |
|------|----------|
| `AnimKey.Equip_Weapon` | 전투 진입 시 재생할 모션셋 등록 |
| `AnimKey.UnEquip_Weapon` | 전투 해제 시 재생할 모션셋 등록 |
| 애니메이션 이벤트 | 무기를 손/등으로 옮기는 프레임에서 `OnEquipRightWeapon` 또는 신규 이벤트 호출 |
| `PlayerActorAnimationMotionSet` | 각 `WeaponType`에 장착/해제 모션셋 등록. 없으면 `NoWeapon` fallback 동작 확인 |
| 캐릭터별 constraint | Bokusei/Honoka/LianLian 모델의 오른손/등 source 순서 확인 |

`PlayerActorAnimator.PlayMotion()`은 현재 무기 타입의 MotionSet을 먼저 찾고, 없으면 `NoWeapon` MotionSet으로 fallback한다. 장착/해제 모션을 공통 모션으로 둘 수 있지만, 무기별 실루엣이 다르면 각 `WeaponType`에 별도 등록하는 편이 안전하다.

---

## 주의 사항

- 전투 해제 시 `SetWeaponType(WeaponType.NoWeapon)`을 호출하지 않는다. 장착 아이템 타입과 전투 모션셋 선택이 함께 사라진다.
- `OnEquipRightWeapon()`은 현재 토글 방식이므로 같은 이벤트가 중복 호출되면 의도와 반대로 다시 손에 들 수 있다.
- `ParentConstraint.GetSource(0)`과 `GetSource(1)` 순서가 오른손/등이라는 전제를 갖고 있다. 프리팹별 source 순서를 검증해야 한다.
- `PlayerCombat.ForceExitCombat()`은 다음 프레임 `UpdateCombatState()`에서 이벤트를 발화한다. 즉시 모션이 필요하면 별도 강제 알림 API가 필요하다.
- 캐릭터 교체는 `PlayerActor.RefreshForCharacter()`에서 모델과 장비 참조를 바꾼다. 전투 무기 연동 컴포넌트가 이전 모델의 `PlayerEquipment`를 들고 있지 않도록 갱신 경로가 필요하다.
- `PlayerIdleState`의 `PlayEquipItem()`은 테스트 코드 성격이고 현재 입력 처리도 주석 처리되어 있다. 이 코드를 그대로 복구하기보다 `PlayerEquipment`/신규 컨트롤러로 역할을 이동하는 것이 좋다.

---

## 검증 체크리스트

| 시나리오 | 기대 결과 |
|----------|----------|
| 비전투 상태에서 첫 공격 | 공격 전 또는 공격 시작 시 무기가 손에 있음 |
| 공격 후 `_combatStateDuration` 경과 | 안전 상태에서 `UnEquip_Weapon` 재생 후 무기가 등으로 이동 |
| 전투 해제 타이밍에 이동 중 | 이동을 과도하게 끊지 않고 해제 모션 또는 예약 처리 |
| 공격 콤보 중 전투 상태 변화 | 공격 모션이 중단되지 않음 |
| 피격/사망 중 전투 상태 변화 | 무기 토글이 피격/사망 모션을 덮지 않음 |
| 캐릭터 교체 후 전투 진입 | 새 모델의 장비 constraint를 사용 |
| Bokusei Katana | `Katana`가 올바른 constraint에 매핑 |
| Honoka DoubleAxe / LianLian Whip | 전용 constraint와 MotionSet 등록 후 정상 장착/해제 |

---

## 결론

수정의 핵심은 `PlayerCombat.OnChangeCombatState`를 활용해 전투 상태 변화는 이벤트로 받고, `PlayerEquipment`에는 토글이 아닌 목표 상태 기반 API를 추가하는 것이다. `WeaponType`은 장착 데이터와 MotionSet 선택에 쓰이므로 전투 해제 표현을 위해 `NoWeapon`으로 바꾸면 안 된다. 실제 손/등 이동은 `IsMainWeaponEquipped`와 `ParentConstraint` weight만 조정하는 별도 상태로 관리해야 한다.
