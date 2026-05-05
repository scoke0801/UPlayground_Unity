# Weapon System 가이드

> 작성일: 2026-05-05  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

플레이어 무기 시스템은 장비 아이템(`EquipmentSO`)을 기준으로 무기 프리팹을 생성하고, 활성 캐릭터 모델 하위의 `ParentConstraint`에 부착한 뒤, 발도/납도 상태에 따라 손 위치와 수납 위치를 전환한다.

현재 구현은 다음 기능을 담당한다.

- `EquipmentSO.weaponType` / `equipSlot` 기반 무기 장착
- `GameObjectManager.CreateWeapon()`을 통한 무기 프리팹 생성
- 활성 모델 하위 `Weapon` 루트에서 `ParentConstraint` 자동 탐색
- `SetMainWeaponDrawn()` / `SetSubWeaponDrawn()`으로 손/등 source weight 전환
- `PlayerActorAnimator`에서 `WeaponType`별 MotionSet 선택
- 캐릭터 모델 교체 시 `CharacterModelData.defaultWeaponType`으로 무기 타입과 공격 데이터 갱신

---

## 현재 아키텍처

```
EquipmentSO
├── equipSlot
├── equipmentPrefab
└── weaponType
        │
        ▼
UI_ItemPopup / Inventory UI
└── PlayerEquipChangeEvent
        │
        ▼
EventManager
└── PlayerEvent.EquipItem / ChangeWeapon
        │
        ▼
PlayerEquipment
├── EquipWeapon()
├── SetWeaponType()
├── RefreshWeaponConstraintsFromModel()
├── SetMainWeaponDrawn()
├── SetSubWeaponDrawn()
└── ForceSyncMainWeaponState()
        │
        ├── GameObjectManager.CreateWeapon()
        │       └── EquipmentSO.equipmentPrefab Instantiate
        │
        └── Model/Weapon 하위 ParentConstraint
                ├── source 0: 손 위치
                └── source 1: 수납 위치

PlayerActorAnimator
└── IsMainWeaponEquipped ? WeaponType MotionSet : NoWeapon MotionSet
```

### 파일 구조

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Enum/WeaponType.cs` | `WeaponType`, `EquipPosition`, `EquipArmorType` 정의 |
| `Assets/02.Scripts/Data/Item/EquipmentSO.cs` | 장비 아이템 데이터. 무기 프리팹, 장착 슬롯, 무기 타입 보유 |
| `Assets/02.Scripts/Manager/Object/GameObjectManager.Weapon.cs` | `EquipmentSO.equipmentPrefab` 기반 무기 오브젝트 생성 |
| `Assets/02.Scripts/Data/Event/PlayerEventData.cs` | `PlayerEquipChangeEvent` 데이터 정의 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerEquipment.cs` | 장비 이벤트 처리, 무기 장착, constraint 탐색, 발도/납도 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombatWeaponStateController.cs` | 전투 상태 진입/해제와 무기 발도/납도 모션 연동 |
| `Assets/02.Scripts/GameActor/Animation/PlayerActorAnimator.cs` | `WeaponType`에 따른 플레이어 MotionSet 선택 |
| `Assets/02.Scripts/Data/Actor/Animation/PlayerActorAnimationMotionSet.cs` | 무기별 `ActorAnimationMotionSet` dictionary와 fallback |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerPreviewActor.cs` | 캐릭터 프리뷰용 장비 표시. 런타임과 별도 매핑 보유 |
| `Assets/02.Scripts/GameActor/Component/Player/CharacterModelData.cs` | 캐릭터별 기본 무기 타입, 공격 데이터, 교체 등장/스탯 데이터, 모델별 공용 소켓 보유 |

---

## 핵심 클래스

### EquipmentSO

`EquipmentSO`는 아이템 데이터와 무기 시스템의 연결 지점이다.

| 필드 | 역할 |
|------|------|
| `equipSlot` | 장착 위치. `RightHand`, `LeftHand`, 방어구 슬롯 등 |
| `equipmentPrefab` | 장착 시 생성할 프리팹 |
| `weaponType` | 무기 종류. MotionSet 선택과 constraint 매핑에 사용 |

현재 `EquipmentSO`에는 무기 스타일 정보가 없다. 예를 들어 한손 무기, 양손 무기, 쌍검, 활+화살 같은 장착 정책은 데이터가 아니라 `PlayerEquipment` 코드에서 처리한다.

### PlayerEquipment

`PlayerEquipment`는 현재 WeaponSystem의 중심 클래스다.

| 책임 | 관련 API |
|------|----------|
| 무기 타입 보관 | `GetMainWeaponType()`, `GetSubWeaponType()`, `SetWeaponType()` |
| 무기 프리팹 장착 | `EquipWeapon()` |
| constraint 자동 탐색 | `RefreshWeaponConstraintsFromModel()`, `GetWeaponConstraint()` |
| 발도/납도 상태 변경 | `SetMainWeaponDrawn()`, `SetSubWeaponDrawn()` |
| 전투 상태 동기화 | `TryPlayMainWeaponDrawMotion()`, `ForceSyncMainWeaponState()` |
| 시작 장비 처리 | `CoEquipStartItem()` |
| 방어구 장착 key 보관 | `GetActiveEquipmentKey()` |

방어구 장착은 인벤토리 슬롯 표시용 item key만 보관한다. 방어구에 따른 캐릭터 mesh/속옷 외형 변경은 사용하지 않는다.

현재 `SetWeaponType()`은 기본적으로 오른손 무기를 설정한다. `DualBlade`처럼 양손 페어 무기인 경우에는 오른손과 왼손 constraint를 모두 매핑하도록 보강되어 있다.

```csharp
public void SetWeaponType(WeaponType type)
{
    SetRightWeaponType(type);
    if (IsPairedWeaponType(type))
        SetLeftWeaponType(type);
    else
        SetLeftWeaponType(WeaponType.NoWeapon);
}
```

### PlayerActorAnimator

`PlayerActorAnimator`는 공격, 이동, 발도/납도 모션을 `PlayerActorAnimationMotionSet`에서 찾는다.

현재 모션 선택 기준은 다음과 같다.

- `Equip_Weapon`, `UnEquip_Weapon`, `Equip_LeftWeapon`: 현재 메인 `WeaponType` 사용
- 그 외 모션: `IsMainWeaponEquipped == true`일 때만 메인 `WeaponType` 사용
- `IsMainWeaponEquipped == false`: `NoWeapon` MotionSet 사용

이 구조 때문에 constraint 매핑 실패로 `IsMainWeaponEquipped`가 false로 남으면, 공격 MotionSet이 실제 무기 타입이 아니라 `NoWeapon`에서 검색된다.

### PlayerActorAnimationMotionSet

`PlayerActorAnimationMotionSet`은 `WeaponType`을 key로 `ActorAnimationMotionSet`을 찾는다.

```csharp
public SerializedDictionary<WeaponType, ActorAnimationMotionSet> motionSets;
```

`GetMotionSet(weaponType, key)`는 먼저 해당 무기 타입에서 모션을 찾고, 없으면 `NoWeapon` MotionSet으로 fallback한다.

### PlayerPreviewActor

`PlayerPreviewActor`는 프리뷰 전용 장비 표시를 담당하지만, 런타임 `PlayerEquipment`와 다른 방식으로 constraint를 매핑한다.

현재 프리뷰는 `SetRightWeaponType()` / `SetLeftWeaponType()` 내부 switch로 일부 무기만 직접 매핑한다. 런타임 쪽은 `Weapon` 루트 스캔과 alias 기반 자동 탐색을 사용하므로, 무기 타입이 늘어나면 프리뷰와 런타임 동작이 어긋날 수 있다.

---

## 현재 셋업 규칙

### 모델 하위 구조

활성 캐릭터 모델의 `PlayerEquipment` 하위에는 이름이 정확히 `Weapon`인 Transform이 있어야 한다.

```
CharacterModel
└── PlayerEquipment가 붙은 오브젝트
    └── Weapon
        ├── Sword
        │   └── ParentConstraint
        └── Sword
            └── ParentConstraint
```

`RefreshWeaponConstraintsFromModel()`은 `FindChildRecursive(transform, "Weapon")`으로 이 루트를 찾고, 그 하위의 `ParentConstraint`만 후보로 수집한다.

### ParentConstraint source 규칙

`SetWeaponDrawn()`은 source 순서를 다음처럼 전제한다.

| Source Index | 의미 |
|--------------|------|
| 0 | 손 위치 |
| 1 | 수납 위치 |

발도 시 source 0 weight를 1, source 1 weight를 0으로 설정한다. 납도 시 반대로 설정한다.

### 좌우 판정 규칙

constraint 좌우 판정은 다음 정보를 기반으로 추론한다.

- `WeaponType.Arrow`는 왼손으로 취급
- constraint 이름 또는 source Transform 이름에 `left`, `handl`, 끝 글자 `l`이 있으면 왼손
- 그 외는 오른손

DualBlade처럼 양손에 같은 이름의 무기 오브젝트가 있을 때는 source Transform 이름에 `Hand_R`, `Hand_L`처럼 좌우 구분이 있어야 안정적으로 매핑된다. 좌우 구분이 불명확하면 오브젝트 이름을 `Sword_R`, `Sword_L`처럼 분리하는 것이 안전하다.

---

## 확인된 레거시

### 1. PlayerEquipment 책임 과다

`PlayerEquipment`는 무기 시스템, 시작 장비 코루틴, 이벤트 구독을 함께 처리한다. 방어구는 장착 key만 보관하며 외형 변경은 하지 않는다.

현재 포함된 책임:

- 장비 이벤트 처리
- 무기 프리팹 생성 요청
- constraint 탐색과 매핑
- 발도/납도 source weight 변경
- MotionSet 선택에 영향을 주는 장착 상태 보관
- 방어구 장착 key 보관

이 구조는 작은 변경에는 빠르지만, 무기 타입과 캐릭터 모델이 늘어날수록 수정 영향 범위가 커진다.

### 2. 이름 기반 constraint 추론

constraint 매핑은 명시 데이터가 아니라 이름 규칙에 의존한다.

예:

- `Sword`
- `Katana` alias로 `sword`
- `DoubleAxe` alias로 `axe`
- `DualBlade` alias로 `dualblade`, `doubleblade`, `blade`, `sword`
- 좌우 판정용 `left`, `handl`, `l`

이 방식은 기존 모델을 빠르게 연결하기에는 유용하지만, 프리팹 이름이나 source 이름이 바뀌면 런타임에서 `_mainWeaponConstraint == null`이 발생할 수 있다.

### 3. 발도 상태와 장착 상태 이름 혼재

`IsMainWeaponEquipped`는 이름상 "메인 무기를 장착했는가"처럼 보이지만, 실제 의미는 "메인 무기가 현재 손에 들려 있는가"에 가깝다.

따라서 다음 상황에서 혼동이 생긴다.

- `GetMainWeaponType() != NoWeapon`이지만 `IsMainWeaponEquipped == false`
- constraint 문제로 발도 실패 후 공격 모션이 `NoWeapon`에서 검색됨
- 무기 보유/장착 여부와 손에 든 상태가 같은 플래그처럼 사용됨

장기적으로는 다음처럼 의미를 분리하는 편이 안전하다.

| 개념 | 권장 이름 |
|------|----------|
| 무기 타입이 설정되어 있음 | `HasMainWeapon` |
| 손에 들고 있음 | `IsMainWeaponDrawn` |
| constraint가 유효해 발도 가능 | `CanDrawMainWeapon` |
| 모션 검색에 사용할 타입 | `ActiveMotionWeaponType` |

### 4. source index 순서 의존

`SetWeaponDrawn()`은 source 0이 손, source 1이 수납 위치라고 가정한다. Unity Inspector에서 source 순서가 바뀌면 발도/납도 방향이 반대로 동작할 수 있다.

### 5. 런타임과 프리뷰 시스템 불일치

런타임 `PlayerEquipment`는 자동 탐색과 alias 매칭을 사용한다. 반면 `PlayerPreviewActor`는 직접 직렬화된 constraint 필드와 switch문을 사용한다.

이로 인해 다음 문제가 생길 수 있다.

- 런타임에서는 장착되지만 프리뷰에서는 표시되지 않음
- 프리뷰에서는 표시되지만 런타임 constraint 매핑은 실패
- 새 `WeaponType` 추가 시 두 파일을 모두 수정해야 함

### 6. WeaponType에 정책이 집중됨

현재 `WeaponType`은 다음 의미를 동시에 가진다.

- 아이템 분류
- 무기 프리팹 종류
- 장착 constraint 매핑 기준
- MotionSet dictionary key
- 전투 스타일 힌트
- 양손/쌍검 같은 장착 정책 분기

무기 타입이 단순할 때는 문제가 적지만, `Bow + Arrow`, `SwordShield`, `DualBlade`처럼 복합 슬롯을 사용하는 무기가 늘어나면 enum 하나로 정책을 표현하기 어렵다.

---

## 개선 방향

### 1. WeaponDefinitionSO 도입

무기 타입별 정책을 코드 switch에서 ScriptableObject로 이동한다.

예시 구조:

```csharp
public enum WeaponEquipStyle
{
    SingleRight,
    SingleLeft,
    RightWithSub,
    PairedBothHands
}

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "UPlayGround/SO/WeaponDefinition")]
public class WeaponDefinitionSO : ScriptableObject
{
    public WeaponType weaponType;
    public WeaponEquipStyle equipStyle;
    public WeaponType motionWeaponType;
    public string[] constraintAliases;
    public bool requiresDrawStateForAttackMotion = true;
}
```

`DualBlade`는 `PairedBothHands`로 정의하고, `SwordShield`는 `RightWithSub` 또는 별도 shield binding으로 정의한다.

### 2. WeaponSocketBinding 컴포넌트 도입

이름 추론 대신 모델 하위 부착점에 명시 컴포넌트를 둔다.

```csharp
public class WeaponSocketBinding : MonoBehaviour
{
    public EquipPosition equipPosition;
    public WeaponType weaponType;
    public ParentConstraint constraint;
}
```

이 방식의 장점:

- 오브젝트 이름 변경에 안전
- 좌우 손 판정이 명시적
- sourceCount 부족, constraint 누락을 에디터에서 검증 가능
- 프리뷰와 런타임이 같은 binding 데이터를 사용할 수 있음

기존 이름 기반 탐색은 구 모델 호환용 fallback으로 유지한다.

### 3. PlayerEquipment 분리

현재 클래스를 다음 역할로 나눌 수 있다.

| 분리 대상 | 책임 |
|----------|------|
| `PlayerEquipmentInventory` | 현재 장착 아이템 key, `EquipmentSO` 참조 보관 |
| `WeaponAttachmentController` | 무기 프리팹 생성, constraint 부착, 좌우 슬롯 관리 |
| `WeaponDrawController` | 발도/납도 요청, source weight 전환, 애니메이션 이벤트 처리 |
| `WeaponMotionResolver` | 현재 무기/발도 상태에 따른 MotionSet key 결정 |

한 번에 전부 분리하기보다, 먼저 `WeaponAttachmentController`와 `WeaponDrawController`를 추출하는 편이 위험이 낮다.

### 4. PlayerPreviewActor와 런타임 resolver 통합

프리뷰 시스템은 런타임과 같은 무기 binding resolver를 사용해야 한다.

권장 방향:

- `PlayerPreviewActor.SetRightWeaponType()` switch 제거
- `PlayerEquipment`의 constraint 탐색 로직을 공용 resolver로 이동
- 프리뷰는 `CharacterPreview` 레이어 설정과 프리뷰 전용 root만 담당

### 5. 장착 상태와 발도 상태 명명 정리

`IsMainWeaponEquipped`는 기존 public API라 즉시 삭제하기보다, 새 이름을 추가하고 구 이름은 래핑하는 식으로 단계적으로 교체한다.

```csharp
public bool IsMainWeaponDrawn { get; private set; }
public bool IsMainWeaponEquipped => IsMainWeaponDrawn;
```

이후 호출부를 `IsMainWeaponDrawn`으로 옮기고, 마지막에 구 API를 제거한다.

---

## 단기 점검 체크리스트

무기 장착 또는 공격 모션이 실패할 때는 다음 순서로 확인한다.

| 항목 | 확인 내용 |
|------|----------|
| `CharacterModelData.defaultWeaponType` | `NoWeapon`이 아닌지 |
| `PlayerEquipment` | 활성 모델 하위에서 다시 잡히는지 |
| `Weapon` 루트 | 이름이 정확히 `Weapon`인지 |
| `ParentConstraint` | `Weapon` 루트 하위에 있는지 |
| `sourceCount` | 최소 2개인지 |
| source 순서 | 0번 손, 1번 수납 위치인지 |
| 좌우 구분 | `Hand_R`, `Hand_L` 또는 오브젝트 이름으로 구분 가능한지 |
| MotionSet | `PlayerActorAnimationMotionSet`에 해당 `WeaponType` 키가 있는지 |
| 공격 데이터 | `CharacterModelData.attackData`가 null이 아니고 콤보 리스트가 비어 있지 않은지 |

---

## 단계별 개선 로드맵

> 2026-05-05 기준 1~5단계는 코드에 반영되어 있다. 기존 이름 기반 탐색은 구 모델 호환용 fallback으로 유지한다.

### 1단계: 진단 강화

`PlayerEquipment`에 constraint 매핑 실패 사유를 명확히 출력한다.

- `Weapon` 루트 없음
- constraint 후보 없음
- weapon alias 매칭 실패
- 좌/우 위치 판정 실패
- sourceCount 부족
- source 0/1 Transform 누락

### 2단계: 공용 resolver 추출

`PlayerEquipment` 내부의 다음 로직을 별도 클래스로 분리한다.

- `RefreshWeaponConstraintsFromModel()`
- `GetWeaponConstraint()`
- `GuessEquipPosition()`
- `MatchesWeaponAlias()`

프리뷰와 런타임이 같은 resolver를 사용하도록 만든다.

반영 파일:

| 파일 | 내용 |
|------|------|
| `WeaponAttachmentResolver.cs` | `WeaponSocketBinding` 우선 탐색, 이름 기반 fallback, 실패 진단 로그 |
| `PlayerEquipment.cs` | 런타임 무기 constraint 매핑을 resolver 경유로 전환 |
| `PlayerPreviewActor.cs` | 프리뷰 무기 constraint 매핑을 resolver 우선, 기존 필드 switch fallback으로 전환 |

### 3단계: 데이터 기반 WeaponDefinitionSO 추가

`IsPairedWeaponType()`, alias switch, 복합 슬롯 정책을 `WeaponDefinitionSO`로 옮긴다.

반영 파일:

| 파일 | 내용 |
|------|------|
| `WeaponDefinitionSO.cs` | `WeaponEquipStyle`, `motionWeaponType`, constraint alias, 발도 상태 요구 플래그 정의 |

### 4단계: WeaponSocketBinding 도입

새 모델부터는 명시 binding을 사용하고, 기존 모델은 이름 기반 fallback으로 유지한다.

반영 파일:

| 파일 | 내용 |
|------|------|
| `WeaponSocketBinding.cs` | 모델 하위 무기 부착점에 `EquipPosition`, `WeaponType`, `ParentConstraint`를 명시하는 컴포넌트 |

### 5단계: PlayerEquipment 책임 분리

방어구 외형 변경은 더 이상 사용하지 않으므로 제거했다. `PlayerEquipment`는 인벤토리 장착 슬롯 표시를 위해 방어구 item key만 보관하고, 런타임/프리뷰 캐릭터 mesh는 무기 장착에 의해서만 변경된다.

반영 파일:

| 파일 | 내용 |
|------|------|
| `PlayerEquipment.cs` | 방어구 mesh 처리 제거, 방어구 item key 보관만 유지 |
| `PlayerPreviewActor.cs` | 방어구 프리뷰 동기화 제거, 무기 프리뷰만 유지 |

남은 분리 후보:

- `WeaponAttachmentController`: 무기 프리팹 생성, 좌우 슬롯, constraint 부착
- `WeaponDrawController`: 발도/납도 요청, source weight 전환
- `WeaponMotionResolver`: 발도 상태와 무기 타입 기반 MotionSet key 결정

---

## 에디터에서 할 작업

### WeaponDefinitionSO 생성

새 무기 타입이나 복합 무기는 `WeaponDefinitionSO`로 장착 정책과 alias를 명시한다.

1. 상단 메뉴 `UPlayGround > Item > WeaponDefinition > Create Missing Definitions`를 실행한다.
2. `Assets/10.Datas/Item/WeaponDefinition`에 없는 무기 타입의 `WD_{WeaponType}` 에셋이 생성된다.
3. 기존 에셋까지 기본값으로 다시 맞춰야 할 때만 `Regenerate All Definitions`를 실행한다. 이 메뉴는 기존 alias와 설정을 덮어쓴다.
4. 필요하면 각 에셋의 `Constraint Aliases`에 모델 소켓 이름과 매칭할 별칭을 추가한다. 예: `dualblade`, `doubleblade`, `blade`, `sword`.
5. 생성한 에셋을 런타임 `PlayerEquipment`와 프리뷰 `PlayerPreviewActor`의 `Weapon Definitions` 리스트에 연결한다.

### WeaponSocketBinding 연결

새 캐릭터 모델은 이름 기반 fallback보다 `WeaponSocketBinding`을 우선 사용한다.

1. 캐릭터 모델 하위에 이름이 `Weapon`인 루트를 둔다.
2. `Weapon` 하위의 각 무기 소켓 오브젝트에 `ParentConstraint`가 있는지 확인한다.
3. 같은 오브젝트에 `WeaponSocketBinding`을 추가한다.
4. `Equip Position`을 `RightHand` 또는 `LeftHand`로 명시한다.
5. `Weapon Type`을 해당 소켓이 받을 무기 타입으로 설정한다.
6. `Constraint` 필드에 같은 오브젝트의 `ParentConstraint`를 연결한다.
7. 오브젝트 이름을 자유롭게 쓰고 싶으면 `Aliases`에 매칭 이름을 추가한다.

### DualBlade 셋업

DualBlade는 같은 `WeaponType`을 오른손과 왼손에 각각 매핑한다.

```
CharacterModel
└── Weapon
    ├── Sword_R
    │   ├── ParentConstraint
    │   └── WeaponSocketBinding
    │       ├── EquipPosition: RightHand
    │       └── WeaponType: DualBlade
    └── Sword_L
        ├── ParentConstraint
        └── WeaponSocketBinding
            ├── EquipPosition: LeftHand
            └── WeaponType: DualBlade
```

두 `ParentConstraint` 모두 source 순서는 동일하게 맞춘다.

| Source Index | 의미 |
|--------------|------|
| 0 | 손 위치 |
| 1 | 수납 위치 |

### 플레이 모드 검증

1. 캐릭터를 스왑한다.
2. Inspector 또는 로그에서 `PlayerEquipment.GetMainWeaponType()`이 기대 무기 타입인지 확인한다.
3. 공격 상태 진입 후 `IsMainWeaponEquipped`가 true로 바뀌는지 확인한다.
4. 오른손/왼손 무기 오브젝트가 각 소켓 아래에 생성되는지 확인한다.
5. `SetMainWeaponDrawn(true)` 시 source 0 weight가 1, source 1 weight가 0이 되는지 확인한다.
6. 콘솔에 `[WeaponAttachmentResolver]` 경고가 뜨면 `Weapon` 루트, `WeaponSocketBinding`, `ParentConstraint`, sourceCount, alias를 순서대로 확인한다.
7. `PlayerActorAnimator`의 해당 `WeaponType` MotionSet에 `Attack_1` 등 공격 모션이 연결되어 있는지 확인한다.
8. `CharacterModelData`의 `Socket Dict`에는 `Center`, `Weapon`, `GuardPosition`처럼 해당 모델의 런타임 연출에 필요한 공용 소켓을 연결한다. 캐릭터 스왑 시 이 값이 `PlayerActor`의 런타임 `Socket Dict`로 복사된다.
9. 무기 손/수납 위치는 `CharacterModelData.Socket Dict`가 아니라 `WeaponSocketBinding`과 `ParentConstraint`에서 관리한다.

---

## 주의 사항

- `IsMainWeaponEquipped == false`는 무기가 없다는 뜻이 아니다. 현재 손에 들려 있지 않거나, constraint 전환에 실패했을 수 있다.
- 공격 상태 진입 시 `SetMainWeaponDrawn(true)`가 실패하면 공격 MotionSet이 `NoWeapon`에서 검색될 수 있다.
- `DualBlade`는 같은 `WeaponType`을 오른손/왼손에 모두 매핑한다. 좌우 source 이름이 없으면 두 constraint를 구분하기 어렵다.
- `PlayerActor.RefreshForCharacter()` 후에는 활성 모델 기준으로 `PlayerEquipment`, constraint, animator 참조를 다시 갱신해야 한다.
- `PlayerPreviewActor`는 런타임과 매핑 방식이 다르므로 새 무기 타입 추가 시 반드시 별도 확인이 필요하다.
