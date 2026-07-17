# 제작 시스템 에디터 작업

## 현재 확인된 상태

- `RecipeManager`, `UI_Crafting`, 레시피/재료 슬롯 스크립트와 제작 데이터 에디터 도구는 구현되어 있다.
- `Assets/10.Datas/Craft/RecipeDatabase.asset`은 존재하며 Addressables 주소도 `RecipeDatabase`로 등록되어 있다.
- 현재 UI 키는 `UIKeyType.Craft`이며 주소 문자열은 `Craft`다.
- 현재 `UI_CraftMenu.prefab`은 기능이 없는 `UI_CraftMenu` 스크립트에 연결되어 있다.
- `UI_CraftingRecipeSlot`, `UI_CraftingIngredientSlot` 프리팹은 존재하지 않는다.
- DB의 레시피 2·3 결과 아이템 ID가 `0`이고 설명이 비어 있다. 재료 ID `100005`와 결과 ID `206`도 실제 ItemDatabase 존재 여부를 확인해야 한다.

즉, 제작 런타임 로직보다 프리팹 구성과 데이터 정리가 우선이다.

## 대상

- `Assets/03.Prefabs/UI/Scene/Craft/UI_CraftMenu.prefab`
- `Assets/10.Datas/Craft/RecipeDatabase.asset`
- 신규 레시피 슬롯/재료 슬롯 프리팹

## 1. 제작 UI 컴포넌트 교체

`UI_CraftMenu.prefab`을 Prefab Mode로 연다.

- [ ] 루트의 기존 `UI_CraftMenu` 컴포넌트를 제거
- [ ] 루트에 `UI_Crafting` 컴포넌트 추가
- [ ] 기존 `UI_Base` 공통 필드와 레이어 값이 유지되는지 확인
- [ ] 현재 Addressables/UI DB 주소 `Craft`는 유지

`UI_Crafting` 주석의 `Crafting` 키는 현재 `UIKeyType`과 다르므로 그대로 새 키를 만들지 않는다. 호출부를 변경하지 않는 한 `Craft`가 실제 기준이다.

## 2. 메인 프리팹 계층 재구성

권장 계층:

```text
UI_CraftMenu
├── CategoryTabs
│   ├── TabAll
│   ├── TabConsumable
│   ├── TabEquipment
│   ├── TabMaterial
│   └── TabSpecial
├── RecipeList
│   └── ScrollView/Viewport/Content
├── DetailPanel
│   ├── ResultIcon
│   ├── ResultNameText
│   ├── DescriptionText
│   ├── IngredientScroll/Viewport/Content
│   ├── CostText
│   └── CastTimeText
└── CraftControls
    ├── QtyMinusButton
    ├── QtyText
    ├── QtyPlusButton
    ├── CraftButton
    │   └── CraftButtonText
    ├── ProgressBar
    ├── CraftStatusText
    └── CloseButton
```

- [ ] 레시피와 재료 Content에 `VerticalLayoutGroup` 설정
- [ ] 필요하면 `ContentSizeFitter`의 Vertical Fit을 `Preferred Size`로 설정
- [ ] 진행 바 Image의 Type을 `Filled`, Fill Method를 `Horizontal`로 설정
- [ ] 상세 패널은 초기 비활성 상태로 설정
- [ ] 팝업 레이어와 배경 입력 차단이 기존 UI 규칙에 맞는지 확인

## 3. 레시피 슬롯 프리팹 제작

신규 권장 경로:

`Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingRecipeSlot.prefab`

필수 구성:

- [ ] 루트에 `UI_CraftingRecipeSlot` 추가
- [ ] 클릭을 받을 `Image`의 Raycast Target 활성화
- [ ] 결과 아이콘 Image 생성 및 `_imgResultIcon` 연결
- [ ] 레시피 이름 TMP 생성 및 `_txtRecipeName` 연결
- [ ] 제작 가능 표시 Image 생성 및 `_imgCraftable` 연결
- [ ] 선택 오버레이 생성 및 `_selectOverlay` 연결
- [ ] 선택 오버레이 기본 Active 끔
- [ ] 제작 가능/불가능 색상 확인

## 4. 재료 슬롯 프리팹 제작

신규 권장 경로:

`Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingIngredientSlot.prefab`

필수 구성:

- [ ] 루트에 `UI_CraftingIngredientSlot` 추가
- [ ] 재료 아이콘 Image 생성 및 `_imgIcon` 연결
- [ ] 재료 이름 TMP 생성 및 `_txtName` 연결
- [ ] 보유/필요 수량 TMP 생성 및 `_txtCount` 연결
- [ ] 수량 배경 Image 생성 및 `_imgCountBg` 연결
- [ ] 충분/부족 색상이 텍스트와 배경에서 읽히는지 확인

## 5. UI_Crafting 인스펙터 연결

모든 필드를 직접 연결한다.

- [ ] Recipe List Content
- [ ] Recipe Slot Prefab
- [ ] Tab All / Consumable / Equipment / Material / Special
- [ ] Detail Panel
- [ ] Result Icon / Result Name / Description
- [ ] Ingredient Content
- [ ] Ingredient Slot Prefab
- [ ] Cost / Cast Time
- [ ] Craft Button / Craft Button Text
- [ ] Qty Minus / Qty Plus / Qty Text
- [ ] Progress Bar
- [ ] Craft Status Text
- [ ] Close Button

버튼 이벤트는 `UI_Crafting.Awake()`에서 코드로 등록하므로 Inspector `OnClick`에 같은 함수를 중복 등록하지 않는다.

## 6. 레시피 데이터 정리

에디터 메뉴:

- `UPlayGround → 게임플레이 → 제작 → 레시피 에디터`
- `UPlayGround → 게임플레이 → 제작 → 레시피 데이터 가져오기`

현재 DB에서 우선 확인할 항목:

- [ ] 모든 `recipeID`가 고유함
- [ ] 레시피 2·3의 `resultItemID: 0`을 실제 아이템 ID로 교체
- [ ] 결과 아이템 ID `206`이 ItemDatabase에 존재함
- [ ] 재료 아이템 ID `100005`가 ItemDatabase에 존재함
- [ ] 레시피 이름과 설명 입력
- [ ] 결과 수량, 비용, 제작 시간, 카테고리 확인
- [ ] 각 레시피에 최소 1개 이상의 유효 재료 설정
- [ ] 초기 테스트 대상은 `isDebugUnlocked`를 켜거나 유효한 언락 조건을 설정

CSV를 다시 가져올 경우 현재 에셋을 덮어쓸 수 있으므로 먼저 버전 관리 diff 또는 백업을 확인한다.

## 7. 플레이 모드 검증

- [ ] 부팅 시 `[RecipeManager] RecipeDatabase 로드 완료` 확인
- [ ] `UIManager.Instance.ShowUI(UIKeyType.Craft)` 경로로 메뉴가 열림
- [ ] 언락된 레시피가 리스트에 표시됨
- [ ] 카테고리 탭별 필터가 동작함
- [ ] 레시피 선택 시 결과 아이콘·설명·재료·비용·시간이 표시됨
- [ ] 수량 +/-에 따라 필요 재료 수량이 갱신됨
- [ ] 재료 부족 시 제작 버튼이 비활성화됨
- [ ] 제작 시작 시 진행 바가 증가하고 버튼이 `취소`로 변경됨
- [ ] 제작 완료 시 재료가 차감되고 결과 아이템이 인벤토리에 추가됨
- [ ] 제작 취소 시 재료와 비용이 잘못 차감되지 않음
- [ ] 제작 중 닫기/수량 조작이 잠김
- [ ] 재오픈과 씬 전환 후 이벤트가 중복 호출되지 않음
- [ ] Console 에러 0개

## 완료 판정

기능이 없는 `UI_CraftMenu`가 아닌 `UI_Crafting`이 실제 `Craft` 프리팹에서 동작하고, 두 슬롯 프리팹과 유효한 테스트 레시피가 준비되어야 한다.
