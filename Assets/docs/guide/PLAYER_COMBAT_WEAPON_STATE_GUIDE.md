# 플레이어 전투 무기 상태 연동 가이드

> 작성일: 2026-04-26  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

플레이어의 전투 상태(`PlayerCombat.IsInCombat`) 변화에 맞춰 무기를 자연스럽게 손에 들거나 등에 넣는 처리에 대한 구현 가이드.

요구 동작은 다음 흐름으로 해석한다.

- 전투 상태 진입 시: 장착된 무기를 손으로 꺼내는 애니메이션 재생
- 전투 상태 해제 시: 들고 있던 무기를 자연스럽게 해제하는 애니메이션 재생
- 공격/피격/가드 등 즉시 반응이 필요한 상태에서는 무기 토글 애니메이션이 전투 액션을 끊지 않도록 지연 처리

현재 프로젝트에는 `PlayerCombatWeaponStateController`가 추가되어 전투 상태 이벤트와 무기 장착/해제 모션을 런타임에서 연결한다.

---

## 현재 구조

```
PlayerCombat
├── IsInCombat
├── RefreshCombatState()
├── ForceExitCombat()
└── OnChangeCombatState(bool)
        │
        ├── UI_HUD_GamePlay: HUD 표시/숨김에 사용 중
        └── PlayerCombatWeaponStateController: 무기 장착/해제 모션 제어

PlayerCombatWeaponStateController
├── PlayerCombat.OnChangeCombatState 구독
├── 안전 상태에서 Equip_Weapon / UnEquip_Weapon 재생
├── 공격/피격/가드 등 블로킹 상태에서는 pending 처리
└── RefreshReferences()로 캐릭터 교체 후 참조 갱신

PlayerEquipment
├── GetMainWeaponType()
├── SetWeaponType(WeaponType)
├── IsMainWeaponEquipped
├── CanToggleMainWeapon()
├── SetMainWeaponDrawn(bool)
├── SetSubWeaponDrawn(bool)
├── TryPlayMainWeaponDrawMotion(bool, ActorAnimator, Action)
├── ForceSyncMainWeaponState(bool)  ← 시작/캐릭터 교체 시 weight↔플래그 강제 동기화
├── OnEquipRightWeapon()  ← 애니메이션 이벤트 콜백
└── OnEquipLeftWeapon()   ← 애니메이션 이벤트 콜백

PlayerActorAnimator
├── PlayMotion(AnimKey)              ← IsMainWeaponEquipped에 따라 WeaponType / NoWeapon MotionSet 분기
└── GetActiveWeaponTypeForMotion()   ← 발도/납도 키는 WeaponType 그대로 사용
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 전투 상태 타이머와 `OnChangeCombatState` 이벤트 발화 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerEquipment.cs` | 무기 프리팹 생성, `ParentConstraint` weight 토글, 장착 상태 보관 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombatWeaponStateController.cs` | 전투 상태 변화와 무기 장착/해제 모션 연동 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | `PlayerCombat`, `PlayerEquipment`, 입력, 캐릭터 교체 참조 갱신 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerIdleState.cs` | 기존 수동 장착/해제 테스트 코드가 주석 처리되어 있음 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerAttackState.cs` 외 공격 상태 | 첫 공격/차지/공중 공격 진입 시 무기를 즉시 손에 들도록 보정 |
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
| `PlayerEquipment.cs:80` | `SetWeaponType()`이 `SetRightWeaponType()`을 통해 무기 타입과 constraint를 함께 갱신 |
| `PlayerEquipment.cs:224` | `EquipWeapon()`이 무기 프리팹을 생성하고 제약 오브젝트 아래에 배치 |
| `PlayerEquipment.cs:264` | `SetRightWeaponType()`이 오른손 무기 제약을 선택 |
| `PlayerEquipment.cs:298` | `CanToggleMainWeapon()`으로 주 무기 토글 가능 여부 확인 |
| `PlayerEquipment.cs:305` | `SetMainWeaponDrawn(bool)`으로 주 무기 손/등 위치를 목표 상태로 지정 |
| `PlayerEquipment.cs:329` | `TryPlayMainWeaponDrawMotion()`으로 장착/해제 모션 재생 및 완료 콜백 처리 |
| `PlayerEquipment.cs:389` | `OnEquipRightWeapon()`이 애니메이션 이벤트 시 목표 상태 기반으로 weight 적용 |
| `PlayerEquipment.cs:400` | `OnEquipLeftWeapon()`이 왼손 무기 weight를 목표 상태 기반으로 적용 |

`OnEquipRightWeapon()` / `OnEquipLeftWeapon()`은 애니메이션 이벤트 콜백으로 유지하되, 자동 연동에서는 `_requestedMainWeaponDrawn` 같은 요청 상태를 기준으로 목표 상태를 맞춘다. 직접 토글 호출이 중복되어도 같은 목표 상태로 수렴하도록 `SetMainWeaponDrawn(bool)`을 사용한다.

### PlayerCombatWeaponStateController

`PlayerCombatWeaponStateController`는 전투 상태 이벤트를 무기 모션으로 변환하는 런타임 제어층이다.

| 위치 | 내용 |
|------|------|
| `PlayerCombatWeaponStateController.cs:12` | `PlayerActorComponent` 기반 신규 컴포넌트 |
| `PlayerCombatWeaponStateController.cs:55` | `RefreshReferences()`에서 `PlayerCombat`, `PlayerEquipment`, `ActorAnimator` 재조회 |
| `PlayerCombatWeaponStateController.cs:76` | `PlayerCombat.OnChangeCombatState` 구독 |
| `PlayerCombatWeaponStateController.cs:89` | 전투 상태 변화 시 장착/해제 요청 |
| `PlayerCombatWeaponStateController.cs:101` | 안전 상태가 아니면 `_pendingDrawn`으로 예약 |
| `PlayerCombatWeaponStateController.cs:118` | `TryPlayMainWeaponDrawMotion()` 호출 |
| `PlayerCombatWeaponStateController.cs:134` | `Idle`, `GroundMove`, `Stop`, `TurnInPlace`에서만 즉시 재생 |

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
| `PlayerActor.cs:398` | 캐릭터 교체 후 `PlayerCombatWeaponStateController.RefreshReferences()` 호출 |
| `PlayerActor.cs:442` | 컨트롤러가 없으면 `GetOrAddComponent<PlayerCombatWeaponStateController>()`로 보장 |

캐릭터 교체가 있는 구조이므로 전투 상태 이벤트 구독은 모델 교체 후에도 현재 `PlayerEquipment`를 다시 참조해야 한다.

---

## 구현 반영 사항

### 1. 무기 타입과 손에 든 상태 분리

현재 `WeaponType`은 `PlayerActorAnimator.PlayMotion()`의 MotionSet 선택에도 사용된다. 따라서 전투 해제 시 `SetWeaponType(WeaponType.NoWeapon)`으로 처리하면 공격/장비 데이터의 무기 타입까지 사라져 전투 모션 선택이 꼬일 수 있다.

권장 원칙:

- `WeaponType`: 어떤 무기를 장비했는지
- `IsMainWeaponEquipped`: 그 무기를 현재 손에 들고 있는지

전투 해제는 `WeaponType.NoWeapon`으로 바꾸는 것이 아니라 constraint source weight만 손에서 등으로 이동해야 한다.

구현에서는 `SetMainWeaponDrawn(bool)`이 `IsMainWeaponEquipped`와 `ParentConstraint` source weight만 변경한다. `WeaponType`은 장착 데이터와 MotionSet 선택용으로 유지한다.

### 2. PlayerEquipment에 명령형 API 추가

자동 연동에서는 목표 상태가 명확해야 하므로 다음 API를 추가했다.

```csharp
public bool CanToggleMainWeapon();
public void SetMainWeaponDrawn(bool drawn);
public void SetSubWeaponDrawn(bool drawn);
public bool TryPlayMainWeaponDrawMotion(bool drawn, ActorAnimator animator, Action onComplete = null);
```

구현 내용:

- `drawn == true`이고 이미 `IsMainWeaponEquipped == true`면 아무것도 하지 않는다.
- `drawn == false`이고 이미 `IsMainWeaponEquipped == false`면 아무것도 하지 않는다.
- 실제 weight 변경은 `SetWeaponDrawn()`으로 분리하고, 토글이 아닌 목표 상태 기반으로 적용한다.
- `AnimKey.Equip_Weapon`, `AnimKey.UnEquip_Weapon` 재생 중 지정 프레임에서 애니메이션 이벤트가 기존 콜백을 호출하도록 유지한다.
- 모션이 없거나 재생 실패 시에는 weight를 즉시 목표 상태로 보정한다.
- 장착/해제 모션 요청 중 공격 등 다른 상태가 끼어들면 `CancelMainWeaponDrawMotionRequest()`로 완료 콜백의 후속 처리를 무효화한다.

### 3. 전투 상태 이벤트를 받을 연동 컴포넌트 추가

`PlayerCombat`이나 `PlayerEquipment`에 직접 섞지 않고, 역할을 분리한 `PlayerCombatWeaponStateController` 신규 컴포넌트를 추가했다.

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

구현은 침범 범위가 작은 `PlayerCombatWeaponStateController.Update()` 폴링 방식을 사용한다. 전투 상태 변경 요청은 `_pendingDrawn`에 보관하고, `Idle`, `GroundMove`, `Stop`, `TurnInPlace`에서만 장착/해제 모션을 재생한다.

첫 공격 진입 시에는 전투 상태 이벤트보다 공격 상태 전환이 먼저 일어날 수 있으므로, 다음 공격 상태의 `OnEnter()`에서 `SetMainWeaponDrawn(true)`를 직접 호출한다.

- `PlayerAttackState`
- `PlayerChargeState`
- `PlayerDashAttackState`
- `PlayerFinishAttackState`
- `PlayerJumpAttackState`
- `PlayerJumpDashAttackState`

### 5. Model 하위 `Weapon` 루트 기반 constraint 자동 탐색

기존 구조는 `swordConstraint`, `greatSwordRightConstraint`, `staffRightConstraint`, `bowRightConstraint`, `shieldLeftConstraint`, `arrowLeftConstraint`처럼 무기별 필드를 직접 들고 있었다. 이 구조는 사용하지 않는다. 또한 에디터에서 사용자가 매핑 데이터를 직접 입력하지 않도록, 활성 Model 하위의 `Weapon` 오브젝트를 런타임에 스캔한다.

자동 탐색 규칙:

- `PlayerEquipment.RefreshWeaponConstraintsFromModel()`이 자신의 하위에서 이름이 정확히 `Weapon`인 Transform을 재귀 탐색한다.
- 찾은 `Weapon` 루트 하위의 모든 `ParentConstraint`를 수집한다.
- `SetRightWeaponType()` / `SetLeftWeaponType()`은 현재 `WeaponType`과 constraint 오브젝트 이름을 비교해 사용할 constraint를 선택한다.
- `Katana (1)`처럼 괄호/공백/언더스코어가 섞인 이름은 정규화해서 비교한다.
- `Shield`, `Arrow`는 기본적으로 `LeftHand`로 분류하고, 그 외 무기는 `RightHand`로 분류한다.
- 정확한 이름 매칭이 없으면 `Weapon` 루트 자체에 붙은 generic constraint 또는 해당 손에 해당하는 단일 constraint를 fallback으로 사용한다.

---

## 구현 완료 흐름

1. `PlayerEquipment`에 목표 상태 기반 메서드를 추가했다.
2. `SetRightWeaponType()` / `SetLeftWeaponType()`이 활성 Model 하위 `Weapon` 루트의 `ParentConstraint`를 자동 탐색해 constraint를 찾도록 변경했다.
3. `PlayerCombatWeaponStateController`를 추가하고 `PlayerCombat.OnChangeCombatState`를 구독했다.
4. 전투 진입 시 `Equip_Weapon`, 전투 해제 시 `UnEquip_Weapon`을 재생하도록 연결했다.
5. 공격/피격 등 블로킹 상태에서는 pending 플래그만 저장하고, 안전 상태에서 처리한다.
6. 첫 공격이 전투 이벤트보다 먼저 시작되는 케이스를 위해 공격 상태 진입 시 `SetMainWeaponDrawn(true)`를 호출한다.
7. `PlayerActor.RefreshForCharacter()` 이후에도 새 모델의 `PlayerEquipment`를 다시 참조하도록 컨트롤러에 `RefreshReferences()`를 제공했다.

---

## 현재 구현 구조

```csharp
namespace UPlayGround.Component
{
    public class PlayerCombatWeaponStateController : PlayerActorComponent
    {
        private PlayerActor _player;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private ActorAnimator _animator;
        private bool? _pendingDrawn;
        private bool _isPlayingDrawMotion;

        private void Awake()
        {
            _player = GetComponent<PlayerActor>();
            RefreshReferences();
        }

        private void OnEnable()
        {
            RefreshReferences();
            SubscribeCombat();
        }

        private void OnDisable()
        {
            if (_combat != null)
                _combat.OnChangeCombatState -= OnCombatStateChanged;
        }

        public void RefreshReferences()
        {
            UnsubscribeCombat();
            _combat = _player.GetCombat();
            _equipment = _player.GetPlayerEquipment();
            _animator = _player.Animator;
            SubscribeCombat();
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

        private bool CanPlayNow()
        {
            string stateName = _player.PlayerController.CurrentState?.StateName;
            return !_isPlayingDrawMotion &&
                   stateName is "Idle" or "GroundMove" or "Stop" or "TurnInPlace";
        }
    }
}
```

위 코드는 현재 구현의 핵심 흐름을 요약한 것이다. 실제 파일에는 구독 중복 방지, 모션 중 상태 변경 시 요청 취소, 모션 완료 후 현재 안전 상태 모션 복귀 처리가 포함되어 있다.

---

## 애니메이션 / 데이터 셋업

| 항목 | 필요 작업 |
|------|----------|
| `AnimKey.Equip_Weapon` | 전투 진입 시 재생할 모션셋 등록 |
| `AnimKey.UnEquip_Weapon` | 전투 해제 시 재생할 모션셋 등록 |
| 애니메이션 이벤트 | 무기를 손/등으로 옮기는 프레임에서 `OnEquipRightWeapon` 또는 신규 이벤트 호출 |
| `PlayerActorAnimationMotionSet` | 각 `WeaponType`에 장착/해제 모션셋 등록. 없으면 `NoWeapon` fallback 동작 확인 |
| 캐릭터별 constraint | Bokusei/Honoka/Lian 모델의 오른손/등 source 순서 확인 |

`PlayerActorAnimator.PlayMotion()`은 현재 무기 타입의 MotionSet을 먼저 찾고, 없으면 `NoWeapon` MotionSet으로 fallback한다. 장착/해제 모션을 공통 모션으로 둘 수 있지만, 무기별 실루엣이 다르면 각 `WeaponType`에 별도 등록하는 편이 안전하다.

---

## 주의 사항

- 전투 해제 시 `SetWeaponType(WeaponType.NoWeapon)`을 호출하지 않는다. 장착 아이템 타입과 전투 모션셋 선택이 함께 사라진다.
- `OnEquipRightWeapon()`은 애니메이션 이벤트 콜백으로 유지한다. 자동 연동 중에는 `_requestedMainWeaponDrawn`을 우선 사용하므로 목표 상태 기반으로 동작하지만, 수동 호출 시에는 기존 호환성을 위해 현재 상태 반전으로 처리된다.
- `ParentConstraint.GetSource(0)`과 `GetSource(1)` 순서가 오른손/등이라는 전제를 갖고 있다. 프리팹별 source 순서를 검증해야 한다.
- `PlayerCombat.ForceExitCombat()`은 다음 프레임 `UpdateCombatState()`에서 이벤트를 발화한다. 즉시 모션이 필요하면 별도 강제 알림 API가 필요하다.
- 캐릭터 교체는 `PlayerActor.RefreshForCharacter()`에서 모델과 장비 참조를 바꾼다. 현재는 `PlayerCombatWeaponStateController.RefreshReferences()`를 호출해 이전 모델의 `PlayerEquipment`를 계속 들고 있지 않도록 갱신한다.
- `PlayerIdleState`의 `PlayEquipItem()`은 테스트 코드 성격이고 현재 입력 처리도 주석 처리되어 있다. 이 코드를 그대로 복구하기보다 `PlayerEquipment`/신규 컨트롤러로 역할을 이동하는 것이 좋다.
- 기존 `swordConstraint`, `greatSwordRightConstraint`, `staffRightConstraint`, `bowRightConstraint`, `shieldLeftConstraint`, `arrowLeftConstraint` 필드는 제거했다. 캐릭터/무기별 constraint는 활성 Model 하위 `Weapon` 루트에서 자동 수집한다.
- `Weapon` 루트를 찾지 못하거나 현재 `WeaponType`에 맞는 constraint를 찾지 못하면 무기 프리팹은 생성 직후 제거되고 warning 로그가 출력된다.
- 자동 매핑은 constraint 오브젝트 이름을 사용한다. 신규 무기 constraint를 만들 때는 `Katana`, `DoubleAxe`, `Whip`, `Shield`, `Arrow`처럼 `WeaponType` 이름이 들어가도록 이름을 맞추는 것이 가장 안전하다.

---

## 검증 체크리스트

| 시나리오 | 기대 결과 |
|----------|----------|
| 비전투 상태에서 첫 공격 | 공격 전 또는 공격 시작 시 무기가 손에 있음 |
| 공격 후 `_combatStateDuration` 경과 | 안전 상태에서 `UnEquip_Weapon` 재생 후 무기가 등으로 이동 |
| 전투 해제 타이밍에 이동 중 | 이동을 과도하게 끊지 않고 해제 모션 또는 예약 처리 |
| 공격 콤보 중 전투 상태 변화 | 공격 모션이 중단되지 않음 |
| 피격/사망 중 전투 상태 변화 | 무기 토글이 피격/사망 모션을 덮지 않음 |
| 캐릭터 교체 후 전투 진입 | 새 모델 하위 `Weapon` 루트의 constraint를 사용 |
| Bokusei Katana | `Weapon` 하위 `Katana` 또는 `Sword` 계열 constraint를 자동 사용 |
| Honoka DoubleAxe / Lian Whip | `Weapon` 하위 `DoubleAxe`, `Whip` 이름의 constraint와 MotionSet 등록 후 정상 장착/해제 |

---

## 후속 수정 (2026-04-27)

초기 구현 후, 시작 시 `IsMainWeaponEquipped` 플래그가 prefab의 `ParentConstraint` weight 상태와 어긋나 발도/납도 가드가 잘못 동작하고, 등에 메인 상태에서도 무기 모션셋이 선택되는 두 가지 문제가 발견되어 다음과 같이 보정했다.

### 6. 시작/장착 시 weight↔플래그 강제 동기화 (B안)

`EquipWeapon()`은 무기 GameObject 생성과 `ParentConstraint` 부모 설정만 수행하고 `IsMainWeaponEquipped`를 갱신하지 않았다. 그 결과 prefab이 손 weight=1로 시작해도 `IsMainWeaponEquipped`는 `false`로 출발해, 비전투 진입 시 `PlayerCombatWeaponStateController.cs:115`의 `IsMainWeaponEquipped == drawn` 가드와 `PlayerEquipment.SetMainWeaponDrawn()`의 동일 가드(`PlayerEquipment.cs:420`)에 모두 막혀 sheath 모션이 재생되지 않았다.

해결: `EquipWeapon()` 끝에서 weight와 플래그를 모두 sheath 상태로 강제 동기화한다.

| 위치 | 내용 |
|------|------|
| `PlayerEquipment.cs:268` | `EquipWeapon()` 마지막에서 `ForceSyncWeaponState(equipPosition, false)` 호출 |
| `PlayerEquipment.cs:509` | `ForceSyncMainWeaponState(bool drawn)` public 진입점 |
| `PlayerEquipment.cs:514` | `ForceSyncWeaponState(EquipPosition, bool)` 가드 없이 weight + 플래그를 함께 set |

이로써 `CoEquipStartItem()` 흐름에서 시작 무기가 등록된 직후 항상 "등에 멘 상태 + `IsMainWeaponEquipped=false`"로 정렬되어, 첫 전투 진입에서 발도 모션이, 첫 전투 이탈에서 납도 모션이 정상 재생된다.

### 7. 캐릭터 교체 시 전투 상태 기반 동기화

캐릭터 교체 시점에 새 모델의 `ParentConstraint` 기본 weight는 prefab 세팅에 의존하므로, 현재 `PlayerCombat.IsInCombat` 값에 맞춰 새 `PlayerEquipment`의 weight와 플래그를 강제 정렬한다.

| 위치 | 내용 |
|------|------|
| `PlayerActor.cs:403` | `RefreshForCharacter()`에서 `_combat.IsInCombat`을 인자로 `ForceSyncMainWeaponState()` 호출 |

비전투 중 교체면 등에 멘 상태로, 전투 중 교체면 손에 든 상태로 자동 동기화된다.

### 8. MotionSet 선택을 IsMainWeaponEquipped 기준으로 분기

기존 `PlayerActorAnimator.PlayMotion()`은 `_playerEquipment.GetMainWeaponType()`만 보고 MotionSet을 선택했기 때문에, 무기를 등에 메고 있어도 `WeaponType=Katana`인 한 카타나 모션이 재생되었다.

해결: 모션 선택 분기 헬퍼 `GetActiveWeaponTypeForMotion(AnimKey)`를 도입하고 `PlayMotion`/`GetMotionSetDuration`이 이를 통하도록 변경했다.

선택 규칙:

- `IsMainWeaponEquipped == true` → `WeaponType` 모션셋
- `IsMainWeaponEquipped == false` → `WeaponType.NoWeapon` 모션셋
- 단, `AnimKey.Equip_Weapon` / `AnimKey.UnEquip_Weapon` / `AnimKey.Equip_LeftWeapon`은 발도/납도 모션 자체가 무기에 정의된 것이므로 `WeaponType`을 그대로 사용한다 (전투 진입/이탈 모션이 정상 재생되도록 보장).

| 위치 | 내용 |
|------|------|
| `PlayerActorAnimator.cs:62` | `PlayMotion()`이 `GetActiveWeaponTypeForMotion(key)` 결과로 MotionSet 조회 |
| `PlayerActorAnimator.cs:87` | `GetMotionSetDuration()`도 동일 헬퍼 사용 |
| `PlayerActorAnimator.cs:93` | `GetActiveWeaponTypeForMotion(AnimKey)` 헬퍼 정의 |

`HasMotion(key, checkWeapon: true)`은 "현재 장비 무기 데이터에 이 키가 있는가?"라는 데이터 존재 확인 용도라 `IsMainWeaponEquipped`와 무관하므로 기존 동작을 유지한다.

### 데이터 측 추가 요구

`PlayerActorAnimationMotionSet` ScriptableObject의 `WeaponType.NoWeapon` 슬롯에 비전투 기본 모션(Idle, Run, Walk, Sprint, Stop, TurnInPlace, Jump, Land 등)이 등록되어 있어야 한다. 등록되지 않은 키는 `PlayMotion`이 null을 반환해 모션이 재생되지 않으므로, 등에 멘 상태에서 캐릭터가 멈춘 자세로 보일 수 있다.

---

## 결론

구현의 핵심은 `PlayerCombat.OnChangeCombatState`를 활용해 전투 상태 변화는 이벤트로 받고, `PlayerEquipment`에는 토글이 아닌 목표 상태 기반 API를 둔 것이다. `WeaponType`은 장착 데이터와 MotionSet 후보 선택에 쓰이므로 전투 해제 표현을 위해 `NoWeapon`으로 바꾸지 않는다. 실제 손/등 이동은 `IsMainWeaponEquipped`와 `ParentConstraint` weight를 함께 조정하는 별도 상태로 관리하며, MotionSet 최종 선택은 `IsMainWeaponEquipped`에 따라 무기/맨손 슬롯으로 분기한다. 시작 시점과 캐릭터 교체 시점에는 `ForceSyncMainWeaponState()`로 weight와 플래그가 어긋나지 않도록 강제 정렬한다.
