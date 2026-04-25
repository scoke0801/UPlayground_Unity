# 아이템 드랍 시스템 가이드

## 개요

몬스터 처치 및 인터랙션 오브젝트(채집, 낚시 등) 파괴 시 아이템이 플레이어에게 자동으로 날아와 인벤토리에 추가되는 시스템.

### 핵심 특징

- **ScriptableObject 기반 드랍 테이블** — `EnemyDropTableSO`로 드랍 데이터를 외부화, 코드 수정 없이 밸런싱 가능
- **독립 확률 방식** — 각 아이템이 rate(0~100%)로 독립적으로 판정되어 다중 드랍 지원
- **재사용 가능한 드랍 테이블** — 하나의 `EnemyDropTableSO`를 여러 몬스터 종류가 공유 가능
- **ActorDefinitionSO 연동** — 런타임 스폰 시 `ActorDefinitionSO.dropTable`로 드랍 테이블 주입
- **두 가지 드랍 소스** — 몬스터 사망(`MonsterActor`) / 인터랙션 오브젝트 파괴(`GatheringActor`) 모두 지원
- **전용 에디터 윈도우** — 아이템 피커, 확률 슬라이더, 기대 드랍량 계산을 제공하는 통합 에디터

---

## 아키텍처

```
┌──────────────────────────────────────────────────────────┐
│                     드랍 트리거 주체                         │
│   MonsterActor.OnDeath()       GatheringActor.OnHitEvent() │
└──────────┬──────────────────────────────┬─────────────────┘
           │                              │
           ▼                              ▼
  EnemyDropTableSO              InteractableActorSO
  (dropItems 목록)               (dropItems 목록)
           │                              │
           └──────────────┬───────────────┘
                          ▼
            ItemManager.GetDropItemList()
             (독립 확률 판정 → ItemInstance 목록)
                          │
                          ▼
            Instantiate(ItemActor 프리팹)
             × 드랍된 아이템 수만큼 반복
                          │
                          ▼
            ItemActor.SpreadAndMoveToPlayer()
             → InventoryManager.AddItem()
```

### 데이터 계층

```
Assets/02.Scripts/Data/
├── Item/
│   ├── ItemDropList.cs            # 드랍 항목 1개 (아이템 + 확률 + 최대 수량)
│   ├── ItemSO.cs                  # 아이템 기본 데이터
│   └── ItemIdType.cs              # 아이템 ID 열거형
└── Actor/
    ├── Enemy/
    │   └── EnemyDropTableSO.cs    # 몬스터 드랍 테이블 SO
    └── ActorDefinitionSO.cs       # 액터 정의 (dropTable 필드 포함)

Assets/02.Scripts/GameActor/Object/
├── Monster/
│   └── MonsterActor.cs            # 사망 시 드랍 처리
└── Prop/
    ├── ItemActor.cs               # 드랍 아이템 픽업 오브젝트
    └── GatheringActor.cs          # 채집 오브젝트 드랍 처리

Assets/02.Scripts/Data/Actor/
└── InteractableActorSO.cs         # 인터랙션 오브젝트 데이터 (dropItems 포함)

Assets/02.Scripts/Manager/Item/
└── ItemManager.cs                 # 드랍 확률 계산 로직

Assets/02.Scripts/Data/Actor/Enemy/Editor/
├── EnemyDropTableEditor.cs        # EnemyDropTableSO 인스펙터 커스텀 에디터
└── DropTableEditorWindow.cs       # 통합 에디터 윈도우
```

---

## 핵심 클래스

### `ItemDropList`

드랍 항목 한 개를 정의하는 직렬화 가능 클래스. `EnemyDropTableSO`와 `InteractableActorSO` 양쪽에서 공통으로 사용.

| 필드 | 타입 | 설명 |
|------|------|------|
| `itemData` | `ItemSO` | 드랍할 아이템 |
| `rate` | `float` (0~100) | 드랍 확률 (%) |
| `maximumDropCount` | `int` (0~100) | 최대 드랍 수량 |

> **주의:** `maximumDropCount`는 `Random.Range(1, maximumDropCount)` 에 사용됩니다. Unity의 `int` 버전 `Random.Range`는 상한이 **exclusive**이므로 실제 최대치는 `maximumDropCount - 1`개입니다. 수량 1개 고정을 원하면 값을 **2**로 설정하세요.

---

### `EnemyDropTableSO`

몬스터 종류별로 할당하는 재사용 가능한 드랍 테이블.  
`Assets/10.Datas/Actor/Enemy/DropTables/` 아래에 저장 권장.

```csharp
namespace UPlayGround.Data.Enemy

[CreateAssetMenu(menuName = "UPlayGround/Enemy/Drop Table")]
public class EnemyDropTableSO : ScriptableObject
{
    public List<ItemDropList> dropItems;
}
```

---

### `ItemManager`

드랍 판정 로직을 담당하는 매니저. `GameManager`에 의해 초기화.

```csharp
// 드랍 목록에서 확률 판정 후 실제 드랍 인스턴스 목록 반환
public List<ItemInstance> GetDropItemList(List<ItemDropList> itemDropList)

// 단일 아이템 인스턴스 수동 생성 유틸리티
public static ItemInstance GET_ITEM(ItemSO itemData, int count)
```

---

### `MonsterActor` (드랍 관련 필드)

| 필드 | 타입 | 설명 |
|------|------|------|
| `_dropTable` | `EnemyDropTableSO` | 이 몬스터의 드랍 테이블 |
| `_itemActorPrefab` | `ItemActor` | 스폰할 픽업 오브젝트 프리팹 |
| `_isDead` | `bool` (protected) | 사망 중복 처리 방지 가드 |

```csharp
// 사망 시 자동 호출 — _isDead 가드로 중복 호출 차단
protected virtual void OnDeath(AttackData attackData)
{
    if (_isDead) return;
    _isDead = true;
    ...
    SpawnDropItems();
}

// dropTable과 itemActorPrefab이 모두 할당된 경우에만 실행
private void SpawnDropItems()
```

---

### `ItemActor`

드랍된 아이템이 플레이어에게 날아가는 픽업 오브젝트. `Init()` 호출 후 자동으로 동작 시작.

```csharp
// Instantiate 직후 반드시 호출
public void Init(ItemInstance itemInstance)
```

동작 흐름: `Init()` → `SpreadAndMoveToPlayer()` (확산) → `MoveToPlayer()` (호밍) → `InventoryManager.AddItem()` → `Destroy(gameObject)`

---

### `ActorDefinitionSO` (드랍 관련 필드)

런타임 스폰 시 `SetDefinition()`을 통해 몬스터 프리팹에 드랍 테이블을 주입할 수 있음.

| 필드 | 타입 | 설명 |
|------|------|------|
| `dropTable` | `EnemyDropTableSO` | 사망 시 드랍 테이블. null이면 프리팹 값 사용 |

---

## 셋업 방법

### 몬스터 드랍 설정

1. **드랍 테이블 에셋 생성**
   ```
   Project 창 우클릭 → Create → UPlayGround → Enemy → Drop Table
   또는
   UPlayGround 메뉴 → Drop Table Editor → 좌측 패널 "＋ 생성"
   ```

2. **드랍 항목 추가**  
   생성된 에셋 선택 → Inspector에서 `＋ 아이템 추가` 클릭  
   각 항목에서 아이템 선택, 확률(rate), 최대 수량 설정

3. **몬스터 프리팹에 할당**  
   몬스터 프리팹 선택 → Inspector의 `Drop` 섹션
   - `Drop Table` : 생성한 `EnemyDropTableSO` 에셋 연결
   - `Item Actor Prefab` : `ItemActor` 컴포넌트가 붙은 픽업 프리팹 연결

4. **(선택) ActorDefinitionSO 경유**  
   해당 몬스터의 `ActorDefinitionSO`에서 `Drop Table` 필드 설정  
   → 런타임 스폰(`ActorSpawnManager`) 시 `SetDefinition()`이 자동으로 덮어씀  
   > ⚠️ `Item Actor Prefab`은 `ActorDefinitionSO`로 제어할 수 없습니다. 반드시 프리팹에서 직접 설정하세요.

### 인터랙션 오브젝트 드랍 설정

1. `InteractableActorSO` 에셋 선택 → `dropItems` 리스트에 항목 추가  
   또는 `Drop Table Editor` → `인터랙션 드랍` 탭에서 편집
2. 해당 `GatheringActor` 프리팹의 `Item Actor Prefab` 필드에 픽업 프리팹 연결

---

## 사용 예시

### 기본: 몬스터 사망 드랍

```csharp
// 몬스터 프리팹 Inspector 설정 후 추가 코드 불필요
// OnDeath() → SpawnDropItems() 가 자동 호출됨

// 하위 클래스에서 드랍 동작 커스터마이징이 필요한 경우
public class BossMonsterActor : MonsterActor
{
    [SerializeField] private EnemyDropTableSO _guaranteedDropTable; // 100% 드랍 테이블

    protected override void OnDeath(AttackData attackData)
    {
        base.OnDeath(attackData); // _isDead 가드 + 일반 드랍 처리

        // 보스 전용 보장 드랍 추가
        if (_guaranteedDropTable != null && _itemActorPrefab != null)
        {
            foreach (var item in _guaranteedDropTable.dropItems)
            {
                var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
                go.Init(ItemManager.GET_ITEM(item.itemData, 1));
            }
        }
    }
}
```

### 코드에서 드랍 목록 직접 계산

```csharp
// 특정 드랍 테이블의 결과를 수동으로 얻고 싶을 때
EnemyDropTableSO dropTable = ...; // 참조

List<ItemInstance> drops = ItemManager.Instance.GetDropItemList(dropTable.dropItems);
foreach (var item in drops)
{
    Debug.Log($"드랍: {item.data.itemName} x{item.count}");
}
```

### 아이템 픽업 오브젝트 수동 스폰

```csharp
// ItemActor 프리팹을 직접 Instantiate하는 경우
[SerializeField] private ItemActor _itemActorPrefab;

void SpawnPickup(ItemSO itemData, int count, Vector3 position)
{
    var go = Instantiate(_itemActorPrefab, position, Quaternion.identity);
    go.Init(ItemManager.GET_ITEM(itemData, count));
}
```

---

## 에디터 도구

### Drop Table Editor (통합 에디터 윈도우)

**메뉴 경로:** `UPlayGround / Drop Table Editor`

| 기능 | 설명 |
|------|------|
| **몬스터 드랍 탭** | 프로젝트 내 모든 `EnemyDropTableSO` 목록 표시 |
| **인터랙션 드랍 탭** | 프로젝트 내 `InteractableActorSO` 목록 표시 (`NpcActorSO` 등 하위 타입 제외) |
| **＋ 생성** | 새 `EnemyDropTableSO` 에셋을 지정 경로에 생성 |
| **아이템 선택 ▾** | 이름·ID로 검색하는 팝업 피커 |
| **확률 슬라이더** | 0~100% 직접 조절, 배경색으로 확률 등급 시각화 (🟢 75%↑ / 🟡 40%↑ / 🔴 미만) |
| **기대 드랍량** | `Σ (rate/100 × maxCount)` 실시간 계산 표시 |
| **↑ 순서 변경** | 항목 순서 위로 이동 |
| **프로젝트에서 보기** | 편집 중인 에셋을 Project 창에서 핑 |
| **↺ 새로고침** | 에셋 목록 재스캔 |

### EnemyDropTableSO 인스펙터 에디터

`EnemyDropTableSO`를 Inspector에서 선택하면 자동 적용:
- 아이콘 미리보기 + 아이템 이름 표시
- 확률 슬라이더 + 최대 수량 인라인 편집
- 요약 헤더 (항목 수, 기대 드랍량)
- `드랍 테이블 에디터 열기` 버튼

---

## 주의 사항

**1. `_itemActorPrefab`은 `ActorDefinitionSO`로 주입되지 않음**  
`dropTable`과 달리 `_itemActorPrefab`은 `SetDefinition()`에서 처리하지 않습니다. 런타임 스폰 환경에서도 **반드시 프리팹 자체에 직접 할당**해야 드랍이 동작합니다.

**2. `maximumDropCount`의 exclusive 상한**  
`Random.Range(1, maximumDropCount)`는 결과가 `[1, maximumDropCount)` 범위입니다.  
- 정확히 1개만 드랍하려면 → `maximumDropCount = 2`  
- 최대 3개 드랍하려면 → `maximumDropCount = 4`

**3. `_isDead` 가드 — 하위 클래스 오버라이드 시**  
`OnDeath()`를 오버라이드할 때는 반드시 `base.OnDeath(attackData)` 를 먼저 호출하세요. 가드 체크가 베이스에 있기 때문에 건너뛰면 중복 사망 처리가 발생할 수 있습니다.

```csharp
// ✅ 올바른 오버라이드
protected override void OnDeath(AttackData attackData)
{
    base.OnDeath(attackData); // 반드시 먼저 호출
    // 추가 처리...
}
```

**4. `ItemActor`는 `Start()`에서 플레이어를 탐색**  
`GameObjectManager.Instance.Player`가 씬에 존재해야 합니다. 플레이어가 없는 씬에서 드랍을 스폰하면 `NullReferenceException`이 발생합니다.

**5. `InteractableActorSO` 검색 시 `NpcActorSO` 제외**  
`NpcActorSO`는 `InteractableActorSO`를 상속하므로 `FindAssets("t:InteractableActorSO")` 시 함께 검색됩니다. 에디터 코드에서 직접 필터링이 필요한 경우 `so.GetType() == typeof(InteractableActorSO)` 조건을 사용하세요.

---

## 확장 포인트

### 몬스터 등급별 드랍 테이블 분기

```csharp
// MonsterActor를 상속한 클래스에서 등급에 따라 다른 테이블 선택
[SerializeField] private EnemyDropTableSO _normalDropTable;
[SerializeField] private EnemyDropTableSO _eliteDropTable;

private void SpawnDropItemsByGrade()
{
    var table = Grade == MonsterActorGrade.Elite ? _eliteDropTable : _normalDropTable;
    if (table == null || _itemActorPrefab == null) return;

    foreach (var item in ItemManager.Instance.GetDropItemList(table.dropItems))
    {
        var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
        go.Init(item);
    }
}
```

### 드랍 위치 분산

`SpawnDropItems()`를 오버라이드하거나, 스폰 위치에 랜덤 오프셋을 추가하면 아이템이 겹치지 않고 퍼져서 생성됩니다. `ItemActor.SpreadAndMoveToPlayer()`의 `_arcHeight`, `_moveSpeed`를 조절하면 날아오는 연출도 변경 가능합니다.

### 이벤트 연동

사망 시 드랍 외에 이벤트 기반 드랍이 필요하면 `EventManager.Instance.Send()`와 연동합니다.

```csharp
// 예: 특정 퀘스트 플래그 충족 시에만 특정 아이템 드랍
protected override void OnDeath(AttackData attackData)
{
    base.OnDeath(attackData);

    if (GlobalFlagManager.Instance.GetFlag("QuestActive"))
    {
        var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
        go.Init(ItemManager.GET_ITEM(_questItemData, 1));
    }
}
```

### 새 인터랙션 오브젝트 타입 추가

`InteractableActorSO`를 상속하는 새 SO 클래스를 만들고 드랍 테이블 에디터에서 노출하려면:

```csharp
// DropTableEditorWindow.cs RefreshAllAssets() 내
// 기존: so.GetType() == typeof(InteractableActorSO)
// 변경: 새 타입도 허용
if (so != null && (so.GetType() == typeof(InteractableActorSO) || so is MyNewActorSO))
    _interactables.Add(so);
```
