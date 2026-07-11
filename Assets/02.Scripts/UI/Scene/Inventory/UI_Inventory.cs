using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Item;
using UPlayGround.Data.Sound;
using UPlayGround.Data.Stat;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using Image = UnityEngine.UI.Image;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인벤토리 UI
    /// </summary>
    public class UI_Inventory : UI_Base
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private InventoryManager _cachedInventoryManager;
        private InventoryManager InventoryMgr => _cachedInventoryManager != null ? _cachedInventoryManager : (_cachedInventoryManager = InventoryManager.Instance);
        private PartyManager _cachedPartyManager;
        private PartyManager PartyMgr => _cachedPartyManager != null ? _cachedPartyManager : (_cachedPartyManager = PartyManager.Instance);


        [SerializeField] private UI_InventorySlot _itemPanelPrefab;
        [SerializeField] private Transform _content;
        [SerializeField] private Image _imgWeightFill;
        [SerializeField] private TextMeshProUGUI _txtWeight;

        [Header("Slot Setting")]
        [SerializeField] private int _slotCountPerRow = 14;
        [SerializeField] private int _startRowCount = 9;

        [Header("Select Detail Panel")]
        [SerializeField] private GameObject _selectedItemPrefab;
        [SerializeField] private Image _selectedItemImage;
        [SerializeField] private TextMeshProUGUI _selectedItemCountText;
        [SerializeField] private TextMeshProUGUI _selectedItemNameText;
        [SerializeField] private TextMeshProUGUI _selectedItemTypeText;
        [SerializeField] private TextMeshProUGUI _selectedItemDescText;

        [Header("Selected Item Actions")]
        [SerializeField] private UICommonButton _useButton;
        [SerializeField] private UICommonButton _equipButton;
        [SerializeField] private UICommonButton _dropButton;

        [Header("Category Tabs")]
        // 탭 하이라이트/단일 선택은 UITabGroup이 관리한다. 인덱스 순서는 TabCategories와 일치.
        [SerializeField] private UITabGroup _tabGroup;

        [Header("Header / Footer")]
        [SerializeField] private TextMeshProUGUI _txtItemCount; // "전체 38 / 120"
        [SerializeField] private TextMeshProUGUI _txtGold;      // 골드
        [SerializeField] private TMP_Dropdown    _sortDropdown; // 정렬 드롭다운 (선택)
        [SerializeField] private UICommonButton  _sortButton;   // 하단 정렬 버튼 (선택, 클릭 시 순환)

        [Header("Detail - Extended")]
        [SerializeField] private TextMeshProUGUI _selectedRarityText;
        [SerializeField] private TextMeshProUGUI _selectedWeightText;
        [SerializeField] private TextMeshProUGUI _selectedEquipSlotText;
        [SerializeField] private GameObject      _statPanel;
        [SerializeField] private TextMeshProUGUI _statAttackText;
        [SerializeField] private TextMeshProUGUI _statCritText;
        [SerializeField] private TextMeshProUGUI _statCritDmgText;
        [SerializeField] private TextMeshProUGUI _statAtkSpeedText;

        [Header("Party Equipment")]
        [SerializeField] private Transform _partySelectorContainer;                 // 파티원 선택 버튼 컨테이너
        [SerializeField] private UIPartyEquipSelectorEntry _partyEntryPrefab;       // 파티원 선택 버튼 프리팹
        [SerializeField] private UIEquipmentSlot[] _equipmentSlots;                 // 선택 캐릭터 장비 슬롯(주/보조 무기 + 방어구 5)
        [SerializeField] private TextMeshProUGUI _selectedCharacterNameText;        // 선택 캐릭터 이름

        [Header("Etc")]
        [SerializeField] private Button              _btnClose;

        private enum InventorySortMode { Default = 0, Name = 1, Rarity = 2, Weight = 3 }

        private readonly List<UIPartyEquipSelectorEntry> _partyEntries = new List<UIPartyEquipSelectorEntry>();
        private CharacterActorType _selectedCharacter = CharacterActorType.None;

        private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();
        private readonly List<TextMeshProUGUI> _statRows = new List<TextMeshProUGUI>();
        private ItemSO _selectedItemData;
        private int _selectedItemCount;
        private int _selectedInventorySlotKey = -1;
        private ItemType? _categoryFilter = null;   // null = 전체
        private InventorySortMode _sortMode = InventorySortMode.Default;

        public GameObject _itemClickTap;

        protected override void Awake()
        {
            base.Awake();

            Init();
            BindActionButtons();
            BindCategoryTabs();
            BindSortControls();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnDispose()
        {
            base.OnDispose();

            if (_tabGroup != null)
                _tabGroup.SelectionChanged -= OnTabSelected;

            if (InventoryMgr != null)
                InventoryMgr.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;
        }

        protected override void OnShow()
        {
            _categoryFilter = null;
            _sortMode       = InventorySortMode.Default;
            if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify(0);

            // "전체" 탭(인덱스 0) 하이라이트만 갱신 (리스트 채우기는 아래에서 직접 수행하므로 notify:false)
            _tabGroup?.Select(0, notify: false);

            var inv = InventoryMgr;
            if (inv != null)
            {
                inv.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;
                inv.OnPartyEquipmentChanged += OnPartyEquipmentChanged;
            }

            var items = RefreshDictItem();
            SetInventory();
            InitPlayerEquipmentSlot();

            var firstItem = items.FirstOrDefault();
            if (firstItem != null)
                ShowSelectedItemDetail(firstItem.data, firstItem.count, firstItem.inventorySlotKey);
            else
                ClearSelectedItemDetail();

            // 키보드/게임패드 네비게이션 시작점: 아이템이 있으면 첫 아이템 슬롯을 선택 상태로 둔다.
            SetInitialItemSlotFocus(items);
        }

        /// <summary> 인벤토리를 열 때 첫 아이템 슬롯을 EventSystem 포커스로 지정한다(네비게이션 시작점). </summary>
        private void SetInitialItemSlotFocus(IReadOnlyList<ItemInstance> items)
        {
            if (EventSystem.current == null) return;
            if (items == null || items.Count == 0) return;
            if (_uiSlots.Count == 0) return;

            var first = _uiSlots[0];
            if (first == null || !first.HasItem) return;

            // 같은 슬롯이 이전 선택으로 남아 있으면 OnSelect가 다시 호출되지 않을 수 있어 한 번 해제 후 지정한다.
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.SetFocus(true);
        }

        public override bool PerformBackFunction()
        {
            // ESC 키 입력 시 닫는다.
            Hide();
            return false;
        }

        public void SetInventory()
        {
            foreach (var t in _uiSlots)
            {
                t.RefreshUI();
            }

            _imgWeightFill.fillAmount = InventoryMgr.GetTotalWeight() / InventoryMgr.MaxWeight;
            _txtWeight.text =
                $"({InventoryMgr.GetTotalWeight():0.0}/{InventoryMgr.MaxWeight:0.0})";

            if (_txtGold != null)
                _txtGold.text = InventoryMgr.Gold.ToString("N0");

            if (_txtItemCount != null)
                _txtItemCount.text = $"전체 {InventoryMgr.ItemDict.Count} / {InventoryMgr.MaxSlots}";
        }


        // 장착 부위별 표시 라벨 + 슬롯 매핑에 쓰는 순서(빌더의 _equipmentSlots 배열 순서와 동일).
        private static readonly EquipPosition[] EquipmentSlotOrder =
        {
            EquipPosition.RightHand, EquipPosition.LeftHand,
            EquipPosition.Head, EquipPosition.Chest, EquipPosition.Pants,
            EquipPosition.Shoes, EquipPosition.Gloves
        };

        private void InitPlayerEquipmentSlot()
        {
            BuildPartySelector();

            // 장비 슬롯 클릭 → 채워진 슬롯 해제 / 빈 슬롯에 선택 아이템 장착
            if (_equipmentSlots != null)
            {
                foreach (var slot in _equipmentSlots)
                {
                    if (slot == null) continue;
                    var captured = slot; // 클로저 캡처 안전
                    captured.SetClickHandler(OnClickEquipmentSlot);
                }
            }

            // 기본 선택 대상: 현재 활성 캐릭터
            CharacterActorType initial = PartyMgr?.ActiveCharacterType ?? CharacterActorType.None;
            if (initial == CharacterActorType.None)
            {
                var roster = PartyMgr?.Roster;
                if (roster != null && roster.Count > 0) initial = roster[0];
            }
            SelectCharacter(initial);
        }

        // 보유(Roster) 전체를 파티원 선택 버튼으로 구성한다.
        private void BuildPartySelector()
        {
            if (_partySelectorContainer == null || _partyEntryPrefab == null)
                return;

            foreach (var e in _partyEntries)
                if (e != null) Destroy(e.gameObject);
            _partyEntries.Clear();

            var roster = PartyMgr?.Roster;
            var memberData = PartyMgr?.PartyMemberDataSO;
            if (roster == null) return;

            foreach (var type in roster)
            {
                if (type == CharacterActorType.None) continue;

                var entry = Instantiate(_partyEntryPrefab, _partySelectorContainer);
                Sprite portrait = memberData != null ? memberData.GetHeadSprite(type) : null;
                string charName = memberData != null ? memberData.GetName(type) : type.ToString();
                entry.Bind(type, portrait, charName, SelectCharacter);
                _partyEntries.Add(entry);
            }
        }

        /// <summary> 장비 편집 대상 캐릭터를 선택한다. </summary>
        public void SelectCharacter(CharacterActorType type)
        {
            _selectedCharacter = type;

            foreach (var e in _partyEntries)
                if (e != null) e.SetSelected(e.Type == type);

            if (_selectedCharacterNameText != null)
            {
                var memberData = PartyMgr?.PartyMemberDataSO;
                _selectedCharacterNameText.text = memberData != null ? memberData.GetName(type) : type.ToString();
            }

            RefreshEquipmentPanel();
            RefreshActionButtons();
        }

        // 선택 캐릭터의 7개 장비 슬롯 아이콘을 레지스트리 값대로 갱신한다.
        private void RefreshEquipmentPanel()
        {
            if (_equipmentSlots == null) return;

            var inv = InventoryMgr;
            for (int i = 0; i < _equipmentSlots.Length; i++)
            {
                var slot = _equipmentSlots[i];
                if (slot == null) continue;

                EquipPosition pos = slot.Slot != EquipPosition.None
                    ? slot.Slot
                    : (i < EquipmentSlotOrder.Length ? EquipmentSlotOrder[i] : EquipPosition.None);

                slot.SetLabel(pos.ToDisplayString());

                int inventorySlotKey = inv != null && _selectedCharacter != CharacterActorType.None
                    ? inv.GetEquippedItem(_selectedCharacter, pos)
                    : -1;

                ItemSO item = inventorySlotKey >= 0
                    ? inv.GetInventoryItemBySlotKey(inventorySlotKey)?.data
                    : null;
                slot.SetItem(item);
            }
        }

        // 장비 슬롯 클릭: 채워진 슬롯이면 해제, 빈 슬롯이면 현재 선택한 아이템을 그 슬롯에 장착 시도.
        // (쌍검 캐릭터가 검을 주/보조 손에 각각 지정 장착하는 경로)
        private void OnClickEquipmentSlot(EquipPosition slot)
        {
            if (_selectedCharacter == CharacterActorType.None)
                return;

            var inv = InventoryMgr;
            if (inv == null)
                return;

            // 채워진 슬롯 → 해제
            if (inv.GetEquippedItem(_selectedCharacter, slot) >= 0)
            {
                inv.TryUnequipItem(_selectedCharacter, slot);
                return;
            }

            // 빈 슬롯 → 선택한 아이템을 이 슬롯에 지정 장착 (호환 불가 시 내부에서 거부됨)
            if (_selectedItemData == null || _selectedItemCount <= 0)
                return;

            inv.TryEquipInventorySlot(_selectedCharacter, _selectedInventorySlotKey, slot);
            // 레지스트리 변경은 OnPartyEquipmentChanged로 UI 일괄 갱신됨
        }

        private void OnPartyEquipmentChanged()
        {
            RefreshEquipmentPanel();
            SetInventory();            // 아이템 슬롯 장착중 뱃지 갱신
            RefreshActionButtons();
        }

        public void SetItemClickAnimation(UI_InventorySlot slot)
        {
            _itemClickTap.gameObject.SetActive(true);
            _itemClickTap.transform.SetParent(slot.transform);

            _itemClickTap.transform.localPosition = Vector3.zero;
        }

        public void OnSlotPointerExit()
        {
            _itemClickTap.gameObject.SetActive(false);
        }

        private void Init()
        {
            AddSlot(_startRowCount);
        }

        private List<ItemInstance> RefreshDictItem()
        {
            var items = GetFilteredSortedItems();

            int value = 0;
            foreach (var inst in items)
            {
                if (_uiSlots.Count <= value)
                {
                    AddSlot(1);
                }
                _uiSlots[value++].Init(inst.data, inst.count, inst.enhancementLevel, inst.inventorySlotKey);
            }

            for (int i = value; i < _uiSlots.Count; i++)
            {
                _uiSlots[i].Clear();
            }

            return items;
        }

        /// <summary> 현재 카테고리 필터 + 정렬을 적용한 아이템 목록을 반환한다. </summary>
        private List<ItemInstance> GetFilteredSortedItems()
        {
            IEnumerable<ItemInstance> src = InventoryMgr.ItemDict.Values
                .Where(i => i != null && i.data != null);

            if (_categoryFilter.HasValue)
                src = src.Where(i => i.data.itemType == _categoryFilter.Value);

            src = _sortMode switch
            {
                InventorySortMode.Name   => src.OrderBy(i => i.data.itemName),
                InventorySortMode.Rarity => src.OrderByDescending(i => (int)i.data.itemRarity)
                                               .ThenBy(i => i.data.itemId),
                InventorySortMode.Weight => src.OrderByDescending(i => i.data.weight)
                                               .ThenBy(i => i.data.itemId),
                _                        => src.OrderBy(i => i.data.itemId),
            };

            return src.ToList();
        }

        // ──── 카테고리 / 정렬 ────

        // 탭 인덱스 → 카테고리 필터 (프리팹의 탭 배치 순서와 반드시 일치, null = 전체)
        private static readonly ItemType?[] TabCategories =
        {
            null,
            ItemType.CONSUMABLE,
            ItemType.EQUIPMENT,
            ItemType.MATERIAL,
            ItemType.QUEST,
            ItemType.IMPORTANT,
        };

        private void BindCategoryTabs()
        {
            if (_tabGroup != null)
                _tabGroup.SelectionChanged += OnTabSelected;
        }

        // UITabGroup 선택 콜백 (탭 클릭 및 초기 Select 모두 여기로 들어온다)
        private void OnTabSelected(int index)
        {
            if (index < 0 || index >= TabCategories.Length) return;
            SetCategory(TabCategories[index]);
        }

        private void SetCategory(ItemType? type)
        {
            _categoryFilter = type;
            var items = RefreshDictItem();
            SetInventory();
            RefreshSelectionForItems(items);
        }

        private void BindSortControls()
        {
            if (_sortDropdown != null)
                _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);

            _sortButton?.BindClickResult(OnClickCycleSort);
        }

        private void OnSortDropdownChanged(int index)
        {
            _sortMode = (InventorySortMode)Mathf.Clamp(index, 0, 3);
            var items = RefreshDictItem();
            SetInventory();
            RefreshSelectionForItems(items);
        }

        private UICommonButtonClickResult OnClickCycleSort()
        {
            _sortMode = (InventorySortMode)(((int)_sortMode + 1) % 4);
            if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify((int)_sortMode);
            var items = RefreshDictItem();
            SetInventory();
            RefreshSelectionForItems(items);
            return UICommonButtonClickResult.Success;
        }

        private void RefreshSelectionForItems(IReadOnlyList<ItemInstance> items)
        {
            if (items == null || items.Count == 0)
            {
                ClearSelectedItemDetail();
                return;
            }

            if (_selectedItemData != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var inst = items[i];
                    if (inst?.data == null)
                        continue;

                    if (_selectedInventorySlotKey >= 0 && inst.inventorySlotKey != _selectedInventorySlotKey)
                        continue;

                    if (_selectedInventorySlotKey < 0 && inst.data.itemId != _selectedItemData.itemId)
                        continue;

                    ShowSelectedItemDetail(inst.data, inst.count, inst.inventorySlotKey);
                    return;
                }
            }

            var first = items[0];
            ShowSelectedItemDetail(first.data, first.count, first.inventorySlotKey);
        }

        private void AddSlot(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                for (int j = 0; j < _slotCountPerRow; ++j)
                {
                    var go = Instantiate(_itemPanelPrefab, _content);
                    _uiSlots.Add(go);
                    go.SetParent(this);
                }
            }
        }

        public void ShowSelectedItemDetail(ItemSO itemData, int count, int inventorySlotKey = -1)
        {
            if (itemData == null)
            {
                ClearSelectedItemDetail();
                return;
            }

            _selectedItemData = itemData;
            _selectedItemCount = count;
            _selectedInventorySlotKey = inventorySlotKey;

            // 강화 레벨 조회 (장비 인스턴스에서)
            int enhance = 0;
            var inst = InventoryMgr.GetInventoryItemBySlotKey(inventorySlotKey) ??
                       InventoryMgr.GetItem(itemData.itemId);
            if (inst != null)
                enhance = inst.enhancementLevel;

            var equip = itemData as EquipmentSO;
            bool isEquip = equip != null;

            _selectedItemPrefab.SetActive(true);
            _selectedItemImage.sprite = itemData.icon;
            _selectedItemImage.color = Color.white;
            _selectedItemImage.enabled = true;
            _selectedItemCountText.text = "보유: " + count.ToString();
            _selectedItemNameText.text = (isEquip && enhance > 0)
                ? $"{itemData.itemName} +{enhance}"
                : itemData.itemName;
            _selectedItemTypeText.text = itemData.itemType.ToDisplayString();
            _selectedItemDescText.text = itemData.itemDescription;

            // 등급 / 무게
            if (_selectedRarityText != null)
            {
                _selectedRarityText.text  = itemData.itemRarity.ToDisplayString();
                _selectedRarityText.color = itemData.itemRarity.ToColor();
            }
            if (_selectedWeightText != null)
                _selectedWeightText.text = $"{itemData.weight:0.0}";

            // 장착 부위
            if (_selectedEquipSlotText != null)
            {
                SetEquipSlotRowActive(isEquip);
                if (isEquip)
                    _selectedEquipSlotText.text = equip.equipSlot.ToDisplayString(equip.weaponType);
            }

            // 능력치 (장비) / 회복량 (소비)
            if (isEquip)
            {
                RefreshSelectedEquipmentStats(equip);
            }
            else if (itemData is ConsumableSO consumable)
            {
                RefreshSelectedConsumableStats(consumable);
            }
            else
            {
                if (_statPanel != null) _statPanel.SetActive(false);
                ClearSelectedEquipmentStats();
            }

            RefreshActionButtons();
        }

        public void ClearSelectedItemDetail()
        {
            _selectedItemData = null;
            _selectedItemCount = 0;
            _selectedInventorySlotKey = -1;

            _selectedItemImage.sprite = null;
            _selectedItemImage.enabled = false;
            _selectedItemCountText.text = string.Empty;
            _selectedItemNameText.text = string.Empty;
            _selectedItemTypeText.text = string.Empty;
            _selectedItemDescText.text = string.Empty;

            if (_selectedRarityText != null)    _selectedRarityText.text = string.Empty;
            if (_selectedWeightText != null)    _selectedWeightText.text = string.Empty;
            SetEquipSlotRowActive(false);
            if (_statPanel != null)             _statPanel.SetActive(false);
            ClearSelectedEquipmentStats();

            _selectedItemPrefab.SetActive(false);
            RefreshActionButtons();
        }

        private void RefreshSelectedEquipmentStats(EquipmentSO equip)
        {
            var modifiers = new List<StatModifier>();
            equip?.AddStatModifiersTo(modifiers, equip);

            if (_statPanel != null)
                _statPanel.SetActive(modifiers.Count > 0);

            EnsureStatRows(modifiers.Count);

            for (int i = 0; i < _statRows.Count; i++)
            {
                TextMeshProUGUI row = _statRows[i];
                if (row == null)
                    continue;

                bool active = i < modifiers.Count;
                SetStatRowActive(row, active);
                row.text = active
                    ? StatDisplayFormatter.FormatModifier(modifiers[i])
                    : string.Empty;
            }
        }

        private void RefreshSelectedConsumableStats(ConsumableSO consumable)
        {
            string healText = BuildConsumableHealText(consumable);
            bool hasInfo = !string.IsNullOrEmpty(healText);

            if (_statPanel != null)
                _statPanel.SetActive(hasInfo);

            EnsureStatRows(hasInfo ? 1 : 0);

            for (int i = 0; i < _statRows.Count; i++)
            {
                TextMeshProUGUI row = _statRows[i];
                if (row == null)
                    continue;

                bool active = hasInfo && i == 0;
                SetStatRowActive(row, active);
                row.text = active ? healText : string.Empty;
            }
        }

        private static string BuildConsumableHealText(ConsumableSO consumable)
        {
            if (consumable == null || consumable.amount <= 0f)
                return string.Empty;

            switch (consumable.effectType)
            {
                case ConsumableEffectType.HealFlat:
                    return $"체력 회복 +{consumable.amount:0.#}";
                case ConsumableEffectType.HealPercent:
                    return $"체력 회복 +{consumable.amount * 100f:0.#}%";
                default:
                    return string.Empty;
            }
        }

        private void ClearSelectedEquipmentStats()
        {
            EnsureStatRows(0);
            for (int i = 0; i < _statRows.Count; i++)
            {
                if (_statRows[i] == null)
                    continue;

                _statRows[i].text = string.Empty;
                SetStatRowActive(_statRows[i], false);
            }
        }

        private void EnsureStatRows(int requiredCount)
        {
            if (_statRows.Count == 0)
            {
                AddStatRowReference(_statAttackText);
                AddStatRowReference(_statCritText);
                AddStatRowReference(_statCritDmgText);
                AddStatRowReference(_statAtkSpeedText);
            }

            TextMeshProUGUI template = _statRows.Count > 0 ? _statRows[0] : null;
            while (_statRows.Count < requiredCount && template != null)
            {
                Transform templateRow = template.transform.parent != null
                    ? template.transform.parent
                    : template.transform;
                Transform parent = templateRow.parent;
                if (parent == null)
                    break;

                var clone = Instantiate(templateRow.gameObject, parent);
                clone.name = $"StatOptionRow_{_statRows.Count + 1}";

                TextMeshProUGUI cloneText = FindStatValueText(clone);
                if (cloneText == null)
                    break;

                _statRows.Add(cloneText);
                ConfigureStatRow(cloneText);
            }

            for (int i = 0; i < _statRows.Count; i++)
            {
                if (_statRows[i] != null)
                    ConfigureStatRow(_statRows[i]);
            }
        }

        private void AddStatRowReference(TextMeshProUGUI text)
        {
            if (text == null || _statRows.Contains(text))
                return;

            _statRows.Add(text);
        }

        private static TextMeshProUGUI FindStatValueText(GameObject row)
        {
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "Value")
                    return texts[i];
            }

            return texts.Length > 0 ? texts[texts.Length - 1] : null;
        }

        private static void ConfigureStatRow(TextMeshProUGUI valueText)
        {
            if (valueText == null)
                return;

            Transform row = valueText.transform.parent;
            if (row != null)
            {
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != valueText)
                        texts[i].gameObject.SetActive(false);
                }
            }

            valueText.alignment = TextAlignmentOptions.Left;
            LayoutElement layout = valueText.GetComponent<LayoutElement>();
            if (layout == null)
                layout = valueText.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = 0f;
            layout.preferredWidth = -1f;
            layout.flexibleWidth = 1f;
        }

        private static void SetStatRowActive(TextMeshProUGUI valueText, bool active)
        {
            Transform row = valueText != null && valueText.transform.parent != null
                ? valueText.transform.parent
                : valueText?.transform;
            if (row != null)
                row.gameObject.SetActive(active);
        }

        private void BindActionButtons()
        {
            _useButton?.BindClickResult(OnClickUseSelectedItem);
            _equipButton?.BindClickResult(OnClickEquipSelectedItem);
            _dropButton?.BindClickResult(OnClickDropSelectedItem);

            _btnClose?.onClick.AddListener(Hide);
        }

        private void RefreshActionButtons()
        {
            bool hasItem = _selectedItemData != null && _selectedItemCount > 0;
            // 장착은 선택된 파티원 대상으로 판정
            bool canEquip = hasItem && _selectedCharacter != CharacterActorType.None &&
                            InventoryMgr.CanEquipItem(_selectedCharacter, _selectedItemData.itemId);

            SetActionButtonActive(_useButton, hasItem && _selectedItemData.itemType == ItemType.CONSUMABLE);
            SetActionButtonActive(_equipButton, canEquip);
            SetActionButtonActive(_dropButton, hasItem);
        }

        private static void SetActionButtonActive(UICommonButton button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        private UICommonButtonClickResult OnClickUseSelectedItem()
        {
            if (_selectedItemData == null)
            {
                return UICommonButtonClickResult.Failed;
            }

            bool isConsumable = _selectedItemData is ConsumableSO;
            InventoryActionResult result = InventoryMgr.TryUseItem(_selectedItemData.itemId);

            // 소모품 사용이 실제로 성공(회복 발생)했을 때 회복 사운드 재생
            if (result == InventoryActionResult.Success && isConsumable)
                SoundManager.Instance?.PlayUi(GameSoundKey.Heal);

            return RefreshAfterAction(result);
        }

        private UICommonButtonClickResult OnClickEquipSelectedItem()
        {
            if (_selectedItemData == null ||
                _selectedCharacter == CharacterActorType.None ||
                !InventoryMgr.CanEquipItem(_selectedCharacter, _selectedItemData.itemId))
            {
                return UICommonButtonClickResult.Failed;
            }

            InventoryActionResult result = _selectedInventorySlotKey >= 0
                ? InventoryMgr.TryEquipInventorySlot(_selectedCharacter, _selectedInventorySlotKey)
                : InventoryMgr.TryEquipItem(_selectedCharacter, _selectedItemData.itemId);
            return RefreshAfterAction(result);
        }

        private void SetEquipSlotRowActive(bool active)
        {
            if (_selectedEquipSlotText == null)
            {
                return;
            }

            Transform row = _selectedEquipSlotText.transform.parent;
            if (row != null)
            {
                row.gameObject.SetActive(active);
            }
            else
            {
                _selectedEquipSlotText.gameObject.SetActive(active);
            }
        }

        private UICommonButtonClickResult OnClickDropSelectedItem()
        {
            if (_selectedItemData == null)
            {
                return UICommonButtonClickResult.Failed;
            }

            InventoryActionResult result = InventoryMgr.TryDropItem(_selectedItemData.itemId);
            return RefreshAfterAction(result);
        }

        private UICommonButtonClickResult RefreshAfterAction(InventoryActionResult result)
        {
            if (result != InventoryActionResult.Success)
            {
                Debug.LogWarning($"[UI_Inventory] 아이템 액션 실패: {result}");
                return UICommonButtonClickResult.Failed;
            }

            var items = RefreshDictItem();
            SetInventory();

            if (_selectedItemData != null && InventoryMgr.HasItem(_selectedItemData.itemId))
            {
                RefreshSelectionForItems(items);
            }
            else
            {
                RefreshSelectionForItems(items);
            }

            return UICommonButtonClickResult.Success;
        }
    }
}
