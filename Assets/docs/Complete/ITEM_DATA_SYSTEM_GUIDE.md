# 아이템 데이터 시스템 가이드

## 개요

아이템 데이터는 `ItemSO`를 기본 단위로 두고, 장비는 `EquipmentSO`가 이를 상속하는 ScriptableObject 구조다. 런타임에서는 Addressables 키 `ItemDatabase`로 로드한 `ItemDatabase`를 `ItemManager`가 보관하고, 인벤토리는 `itemId`를 키로 `ItemInstance`를 관리한다.

### 핵심 특징

- **ScriptableObject 기반 아이템 정의** — `Assets/10.Datas/Item/` 아래의 `ItemSO`/`EquipmentSO` 에셋이 원본 데이터
- **int ID 중심 조회** — `ItemDatabase.GetItemById(int)`와 `ItemIdType` enum이 모두 `itemId`를 기준으로 동작
- **장비 상속 구조** — 장비는 `EquipmentSO : ItemSO`로 장비 슬롯, 무기 타입, 장비 프리팹을 추가 보관
- **Addressables DB 로드** — `ItemManager`가 `ItemDatabase` Addressables 에셋을 비동기 로드한 뒤 `InventoryManager` 복원을 진행
- **에디터 도구 분리** — `ItemEditorWindow`는 아이템 생성/편집/DB 갱신, `IdEnumGeneratorWindow`는 `ItemIdType` 생성 담당

---

## 아키텍처

```
ItemSO / EquipmentSO asset
        │
        ▼
ItemDatabase
  - allItems
  - itemDictionary<int, ItemSO>
        │ Addressables key: "ItemDatabase"
        ▼
ItemManager
  - GetItemData(int)
  - GetDropItemList(...)
        │
        ├── InventoryManager
        │     - Dictionary<int, ItemInstance>
        │     - AddItem / RemoveItem / Save
        │
        ├── ItemActor
        │     - 픽업 연출 후 InventoryManager.AddItem(...)
        │
        └── UI_Scene_Inventory / UI_Popup_Item
              - 아이콘, 이름, 무게, 장착/사용 버튼 표시
```

### 파일 구조

```
Assets/02.Scripts/Data/
├── Enum/
│   └── ItemEnum.cs                  # ItemType, ItemRarity
├── Item/
│   ├── ItemSO.cs                    # 공통 아이템 데이터 + ItemInstance
│   ├── EquipmentSO.cs               # 장비 전용 데이터
│   ├── ItemDropList.cs              # 드랍 항목
│   └── ItemIdType.cs                # 자동 생성 enum
└── Path/
    ├── ItemDatabase.cs              # 전체 아이템 DB
    └── Editor/
        ├── ItemEditorWindow.cs      # 아이템 생성/편집 도구
        └── ItemDatabaseEditor.cs    # DB 수동 갱신 인스펙터

Assets/02.Scripts/Manager/Item/
├── ItemManager.cs
└── InventoryManager.cs

Assets/02.Scripts/Tool/Editor/
├── IdEnumGeneratorWindow.cs
└── IdEnumGeneratorUtility.cs

Assets/10.Datas/Item/
├── ItemDatabase.asset
├── Equipment/
│   ├── Weapon/
│   ├── Chest_001.asset ...
│   └── ...
└── Crystal.asset / Feather.asset / ...
```

---

## 핵심 클래스

### `ItemSO`

| 필드 | 타입 | 설명 |
|------|------|------|
| `itemId` | `int` | 아이템의 고유 ID. 런타임 조회와 저장 데이터의 기준 |
| `itemName` | `string` | 표시 이름. `ItemIdType` 생성 시 enum 식별자 원본으로도 사용 |
| `itemDescription` | `string` | 팝업 설명 |
| `weight` | `float` | 인벤토리 무게 계산용 |
| `itemType` | `ItemType` | `NONE`, `EQUIPMENT`, `CONSUMABLE`, `OTHERS` |
| `itemRarity` | `ItemRarity` | `COMMON`~`LEGENDARY` 희귀도 |
| `icon` | `Sprite` | 인벤토리/획득 UI 표시 아이콘 |

### `EquipmentSO`

`EquipmentSO`는 `ItemSO`를 상속하며 장비 전용 필드를 추가한다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `equipSlot` | `EquipPosition` | 장착 부위 |
| `equipmentPrefab` | `GameObject` | 장착 시 사용할 프리팹 |
| `weaponType` | `WeaponType` | 무기 타입. 방어구는 기본 `NoWeapon` 사용 |

### `ItemInstance`

런타임 보유 수량을 표현하는 직렬화 클래스다. `ItemSO` 원본 데이터와 수량을 함께 들고 있으며, 세이브 시에는 `itemId`, `count`, `inventorySlotKey`로 변환된다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `count` | `int` | 보유 수량 |
| `data` | `ItemSO` | 원본 아이템 데이터 |
| `inventorySlotKey` | `int` | 인벤토리 슬롯 키 |

### `ItemDatabase`

`allItems` 리스트를 보관하고 `Initialize()`에서 `Dictionary<int, ItemSO>`를 만든다. 중복 ID가 있을 경우 먼저 등록된 아이템만 딕셔너리에 들어가므로, 에디터 단계에서 중복을 제거해야 한다.

```csharp
public ItemSO GetItemById(int itemId)
public List<ItemSO> GetItemsByType(ItemType type)
public List<EquipmentSO> GetEquipmentsBySlot(EquipPosition slot)
```

### `ItemManager`

게임 시작 시 Addressables 키 `ItemDatabase`로 DB를 로드한다. 로드 완료 후 `InventoryManager.Instance.OnItemDatabaseReady()`를 호출해 세이브 복원을 진행한다.

```csharp
public ItemDatabase GetItemDB()
public ItemSO GetItemData(int itemKey)
public List<ItemInstance> GetDropItemList(List<ItemDropList> itemDropList)
public static ItemInstance GET_ITEM(ItemSO itemData, int count)
```

### `InventoryManager`

`Dictionary<int, ItemInstance>`를 실제 인벤토리 저장소로 사용한다. `AddItem(int itemId, int count)`는 `ItemManager`에서 `ItemSO`를 조회하므로 `ItemDatabase` 로드 이후에 호출되어야 한다.

---

## 현재 에디터 도구

### Item Editor

**메뉴 경로:** `UPlayGround / Item / Item Editor`

| 기능 | 설명 |
|------|------|
| `+ 새 아이템` | `ItemSO` 또는 `EquipmentSO` 에셋 생성 |
| 타입 필터 | 전체/장비/소비/기타 필터 |
| 검색 | 이름 또는 ID 검색 |
| ID 중복 감지 | `_duplicateIDs`로 중복 ID를 목록/상세에 표시 |
| 복제 | 선택 아이템 복제 후 현재 최대 ID + 1로 변경 |
| DB 갱신 | `ItemDatabase.RefreshDatabase("Assets/10.Datas/Item")` 호출 |

현재 생성 로직은 전체 아이템 중 최대 `itemId`에 1을 더한다. 장비 부위나 소비 아이템 대역을 고려하지 않으므로, 아이템 수가 늘어나면 전용 발급 규칙이 필요하다.

### ID Enum Generator

**메뉴 경로:** `UPlayGround / Util / ID Enum Generator`

`ItemDatabase.AllItems`를 읽어 `Assets/02.Scripts/Data/Item/ItemIdType.cs`를 자동 생성한다. enum 값은 `itemId` 자체이며, `ToItemId()`는 `(int)type`을 반환한다.

---

## 현재 ID 대역 분석

현재 에셋과 `ItemIdType` 기준으로 다음 대역이 사용 중이다.

| 대역 | 현재 의미 | 예시 |
|------|-----------|------|
| `100~199` | 머리 장비 | `모자_2 = 102` |
| `200~299` | 상의 장비 | `상의_1 = 201` |
| `300~399` | 하의 장비 | `하의_1 = 301` |
| `400~499` | 장갑 장비 | `장갑_1 = 401` |
| `500~599` | 신발 장비 | `신발_1 = 501` |
| `1000~1999` | 소검 | `소검_1 = 1001` |
| `2000~2999` | 방패 | `방패_1 = 2001` |
| `3000~3999` | 지팡이 | `지팡이_1 = 3001` |
| `4000~4999` | 대검 | `대검_1 = 4001` |
| `5000~5999` | 활 | `활_1 = 5001` |
| `6000~6999` | 화살 | `화살_1 = 6001` |
| `7000~7999` | 카타나 | `카타나 = 7001` |
| `100000~199999` | 재료/기타 | `수정 = 100001`, `몬스터정수 = 100005` |

> 확인된 주의점: `Head_012.asset`은 `itemId: 110`, `itemName: 모자_10`으로 `Head_010.asset`과 중복되는 상태다. `ItemDatabase.Initialize()`는 중복 ID를 무시하고 첫 항목만 등록하므로, 자동 발급기 도입 전 중복 정리가 필요하다.

---

## 데이터 자동 발급기 정의

### 목적

아이템 데이터 자동 발급기는 새 아이템 에셋을 만들 때 `itemId`, 저장 경로, 파일명, 기본 `itemType`, 장비 전용 필드를 규칙 기반으로 자동 채우는 에디터 도구다. 현재 `ItemEditorWindow`의 생성 기능을 대체하거나 확장하는 형태가 적합하다.

### 제안 메뉴

```
UPlayGround / Item / Item Data Issuer
```

### 발급 단위

| 입력 | 설명 |
|------|------|
| `itemType` | `EQUIPMENT`, `CONSUMABLE`, `OTHERS` 중 선택 |
| `equipSlot` | 장비일 때 필수. 머리/상의/하의/신발/장갑/무기 슬롯 |
| `weaponType` | 무기일 때 필수. 소검/방패/지팡이/대검/활/화살/카타나 등 |
| `displayName` | `itemName`에 들어갈 표시 이름 |
| `assetName` | 저장 파일명. 비어 있으면 표시 이름 또는 대역 prefix로 생성 |
| `rarity` | 기본 희귀도 |
| `icon` | 선택 사항 |
| `equipmentPrefab` | 장비일 때 선택 사항 |

### ID 발급 규칙

자동 발급기는 선택된 카테고리에 맞는 ID 대역을 결정하고, 해당 대역에서 사용 중인 최대 ID + 1을 발급한다.

| 카테고리 | 조건 | 발급 대역 | 저장 경로 |
|----------|------|-----------|-----------|
| 머리 장비 | `EQUIPMENT + equipSlot Head` | `100~199` | `Assets/10.Datas/Item/Equipment` |
| 상의 장비 | `EQUIPMENT + equipSlot Chest` | `200~299` | `Assets/10.Datas/Item/Equipment` |
| 하의 장비 | `EQUIPMENT + equipSlot Pants` | `300~399` | `Assets/10.Datas/Item/Equipment` |
| 장갑 장비 | `EQUIPMENT + equipSlot Gloves` | `400~499` | `Assets/10.Datas/Item/Equipment` |
| 신발 장비 | `EQUIPMENT + equipSlot Shoes` | `500~599` | `Assets/10.Datas/Item/Equipment` |
| 소검 | `EQUIPMENT + weaponType Sword` | `1000~1999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 방패 | `EQUIPMENT + weaponType Shield` | `2000~2999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 지팡이 | `EQUIPMENT + weaponType Staff` | `3000~3999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 대검 | `EQUIPMENT + weaponType GreatSword` | `4000~4999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 활 | `EQUIPMENT + weaponType Bow` | `5000~5999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 화살 | `EQUIPMENT + weaponType Arrow` | `6000~6999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 카타나 | `EQUIPMENT + weaponType Katana` | `7000~7999` | `Assets/10.Datas/Item/Equipment/Weapon` |
| 소비 아이템 | `CONSUMABLE` | `50000~99999` | `Assets/10.Datas/Item` |
| 재료/기타 | `OTHERS` | `100000~199999` | `Assets/10.Datas/Item` |

`EquipPosition`과 `WeaponType`의 실제 enum 멤버명은 코드 기준으로 매핑해야 한다. 자동 발급기 구현 시에는 문자열 비교 대신 enum 값을 직접 매핑하는 `Dictionary<EquipPosition, IdRange>`와 `Dictionary<WeaponType, IdRange>`를 사용한다.

### 발급 절차

1. 프로젝트 전체에서 `ItemSO`를 검색한다.
2. 모든 `itemId`를 수집해 중복 ID를 검사한다.
3. 선택한 카테고리의 ID 대역을 결정한다.
4. 대역 안에서 비어 있는 가장 작은 ID를 찾는다.
5. `ItemSO` 또는 `EquipmentSO` 인스턴스를 생성한다.
6. 기본 필드를 채운다.
7. `AssetDatabase.GenerateUniqueAssetPath()`로 충돌 없는 경로에 저장한다.
8. `ItemDatabase.RefreshDatabase("Assets/10.Datas/Item")`를 호출한다.
9. `ItemIdType` 생성을 실행하거나, 사용자에게 `ID Enum Generator` 실행을 안내한다.

### 검증 규칙

| 검증 | 실패 처리 |
|------|-----------|
| `displayName`이 비어 있음 | 생성 버튼 비활성화 |
| 선택 대역이 가득 참 | 오류 표시, 생성 중단 |
| 전체 아이템 ID 중복 존재 | 경고 표시. 새 발급은 가능하지만 DB 갱신 전 정리 권장 |
| 선택 카테고리와 SO 타입 불일치 | 예: 장비인데 `ItemSO` 생성 시도하면 `EquipmentSO`로 강제 |
| 무기 장비인데 `weaponType == NoWeapon` | 생성 버튼 비활성화 |
| 방어구 장비인데 `equipSlot` 미지정 | 생성 버튼 비활성화 |

### 최소 구현 스케치

```csharp
private readonly struct ItemIdRange
{
    public readonly int Min;
    public readonly int Max;

    public bool Contains(int id) => id >= Min && id <= Max;
}

private static int IssueNextId(IEnumerable<ItemSO> items, ItemIdRange range)
{
    var used = new HashSet<int>(items.Where(i => i != null).Select(i => i.itemId));
    for (int id = range.Min; id <= range.Max; id++)
    {
        if (!used.Contains(id))
            return id;
    }

    throw new InvalidOperationException($"아이템 ID 대역이 가득 찼습니다: {range.Min}~{range.Max}");
}
```

---

## 셋업 방법

### 현재 방식

1. `UPlayGround / Item / Item Editor`를 연다.
2. `+ 새 아이템`으로 `ItemSO` 또는 `EquipmentSO`를 생성한다.
3. `itemId`, 이름, 타입, 희귀도, 아이콘을 수동 보정한다.
4. 장비면 `equipSlot`, `weaponType`, `equipmentPrefab`을 설정한다.
5. `DB 갱신`을 누른다.
6. `UPlayGround / Util / ID Enum Generator`에서 `ItemIdType`을 생성한다.

### 자동 발급기 도입 후 권장 방식

1. `UPlayGround / Item / Item Data Issuer`를 연다.
2. 아이템 타입과 세부 카테고리를 선택한다.
3. 이름, 희귀도, 아이콘, 장비 프리팹을 입력한다.
4. 미리보기 ID와 저장 경로를 확인한다.
5. `발급`을 누르면 에셋 생성, DB 갱신, enum 생성까지 한 번에 수행한다.

---

## 주의 사항

**1. `ItemIdType.cs`는 직접 수정하지 않는다.**  
파일 헤더에도 명시되어 있듯 `ID Enum Generator`가 자동 생성하는 파일이다.

**2. `ItemDatabase`에 누락된 아이템은 런타임 조회되지 않는다.**  
에셋을 만든 뒤 반드시 DB를 갱신해야 `ItemManager.GetItemData()`와 인벤토리 복원이 정상 동작한다.

**3. 중복 ID는 런타임에서 조용히 한쪽이 무시된다.**  
`ItemDatabase.Initialize()`는 `ContainsKey` 체크 후 첫 항목만 추가한다. 중복이 있으면 뒤 항목은 조회 불가능하다.

**4. `InventoryManager.AddItem(int, ItemInstance)`는 `itemId`와 `itemInstance.data.itemId` 일치 여부를 검증하지 않는다.**  
직접 호출할 때는 두 값이 같은지 호출자가 보장해야 한다.

**5. `ItemActor.Init()`은 `Start()`보다 먼저 호출되어야 한다.**  
`Instantiate()` 직후 `Init()`을 호출하는 현재 패턴을 유지해야 획득 UI와 인벤토리 추가가 올바르게 동작한다.

---

## 확장 포인트

### `ItemEditorWindow`에 발급 기능 통합

현재 `CreateNewItem()`의 `최대 ID + 1` 로직을 `IssueNextId(...)` 기반으로 교체하면 기존 창을 유지하면서 발급 품질을 개선할 수 있다.

### 발급 정책 ScriptableObject화

ID 대역과 저장 경로를 코드에 고정하지 않고 `ItemIdIssuePolicySO`로 빼면, 새 무기군이나 재료 대역을 추가할 때 에디터 코드 수정 없이 확장할 수 있다.

### DB/Enum 자동 후처리

발급 완료 후 다음 처리를 자동 실행하면 수동 실수를 줄일 수 있다.

```csharp
itemDatabase.RefreshDatabase("Assets/10.Datas/Item");
// ItemIdType 생성은 IdEnumGeneratorUtility.GenerateIntKeyEnum(...) 재사용
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

