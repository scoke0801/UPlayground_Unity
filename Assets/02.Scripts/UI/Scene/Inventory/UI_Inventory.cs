using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Sound;
using UPlayGround.Data.Stat;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;
using Image = UnityEngine.UI.Image;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인벤토리 UI
    /// </summary>
    public class UI_Inventory : UI_SceneBase
    {
        // 빌더/HUD와 동일한 시계 방향 슬롯 순서: 위 → 오른쪽 → 아래 → 왼쪽.
        private static readonly string[] QuickSlotActionNames =
        {
            PlayerAction.QuickSlot_Up,
            PlayerAction.QuickSlot_Right,
            PlayerAction.QuickSlot_Down,
            PlayerAction.QuickSlot_Left,
        };

        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private IUIInventoryService _cachedInventoryManager;
        private IUIInventoryService InventoryMgr => _cachedInventoryManager != null ? _cachedInventoryManager : (_cachedInventoryManager = UISvc.Inventory);
        private IUIPartyService _cachedPartyManager;
        private IUIPartyService PartyMgr => _cachedPartyManager != null ? _cachedPartyManager : (_cachedPartyManager = UISvc.Party);


        [SerializeField] private UI_InventorySlot _itemPanelPrefab;
        [SerializeField] private Transform _content;
        [SerializeField] private GridLayoutGroup _itemGrid;
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
        [Tooltip("소비 아이템을 위/오른쪽/아래/왼쪽 퀵슬롯에 등록하는 버튼.")]
        [SerializeField] private UICommonButton[] _quickSlotButtons;
        [Tooltip("퀵슬롯 등록 라벨과 방향별 버튼을 함께 숨기기 위한 루트.")]
        [SerializeField] private GameObject _quickSlotRegistrationRoot;

        [Header("Category Tabs")]
        // 탭 하이라이트/단일 선택은 UITabGroup이 관리한다. 인덱스 순서는 TabCategories와 일치.
        [SerializeField] private UITabGroup _tabGroup;

        [Header("Header / Footer")]
        [SerializeField] private TextMeshProUGUI _txtItemCount; // "전체 38 / 120"
        [SerializeField] private TextMeshProUGUI _txtGold;      // 골드
        [SerializeField] private TMP_Dropdown    _sortDropdown; // 정렬 드롭다운 (선택)
        [SerializeField] private UICommonButton  _sortButton;   // 하단 정렬 버튼 (선택, 클릭 시 순환)
        [SerializeField] private TextMeshProUGUI _sortModeText; // "정렬 : 최근 획득순"
        [SerializeField] private TextMeshProUGUI _txtPlayTime;

        [Header("Detail - Extended")]
        [SerializeField] private TextMeshProUGUI _selectedRarityText;
        [SerializeField] private TextMeshProUGUI _selectedWeightText;
        [SerializeField] private TextMeshProUGUI _selectedEquipSlotText;
        [SerializeField] private GameObject      _statPanel;
        [SerializeField] private TextMeshProUGUI _statAttackText;
        [SerializeField] private TextMeshProUGUI _statCritText;
        [SerializeField] private TextMeshProUGUI _statCritDmgText;
        [SerializeField] private TextMeshProUGUI _statAtkSpeedText;
        [SerializeField] private GameObject _comparisonPanel;
        [SerializeField] private TextMeshProUGUI _comparisonItemNameText;
        [SerializeField] private TextMeshProUGUI _comparisonStatsText;

        [Header("Party Equipment")]
        [SerializeField] private Transform _partySelectorContainer;                 // 파티원 선택 버튼 컨테이너
        [SerializeField] private UIPartyEquipSelectorEntry _partyEntryPrefab;       // 파티원 선택 버튼 프리팹
        [SerializeField] private UIEquipmentSlot[] _equipmentSlots;                 // 선택 캐릭터 장비 슬롯(주/보조 무기 + 방어구 5)
        [SerializeField] private TextMeshProUGUI _selectedCharacterNameText;        // 선택 캐릭터 이름

        [Header("Selected Character Summary")]
        [SerializeField] private Image _selectedCharacterPortrait;
        [SerializeField] private TextMeshProUGUI _selectedCharacterLevelText;
        [SerializeField] private Image _selectedCharacterExpFill;
        [SerializeField] private TextMeshProUGUI _selectedCharacterExpText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterCombatPowerText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterHpText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterAttackText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterDefenseText;
        [SerializeField] private TextMeshProUGUI _selectedCharacterCritText;

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
        private int _visibleItemCount;
        private float _lastGridWidth = -1f;
        private float _nextPlayTimeRefresh;
        private IInputService _inputService;
        private Coroutine _gridLayoutRefreshCoroutine;

        public GameObject _itemClickTap;

        protected override void Awake()
        {
            base.Awake();

            Init();
            BindActionButtons();
            BindCategoryTabs();
            BindSortControls();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsVisible || Time.unscaledTime < _nextPlayTimeRefresh)
                return;

            _nextPlayTimeRefresh = Time.unscaledTime + 1f;
            RefreshPlayTime();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnDispose()
        {
            base.OnDispose();
            StopResponsiveGridRefresh();

            if (_tabGroup != null)
                _tabGroup.SelectionChanged -= OnTabSelected;

            if (InventoryMgr != null)
                InventoryMgr.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;

            UnbindInputDeviceChanged();
        }

        protected override void OnShow()
        {
            base.OnShow();

            _categoryFilter = null;
            _sortMode       = InventorySortMode.Default;
            if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify(0);
            RefreshSortLabel();
            BindInputDeviceChanged();
            RefreshPlayTime();
            _nextPlayTimeRefresh = Time.unscaledTime + 1f;

            // "전체" 탭(인덱스 0) 하이라이트만 갱신 (리스트 채우기는 아래에서 직접 수행하므로 notify:false)
            _tabGroup?.Select(0, notify: false);

            var inv = InventoryMgr;
            if (inv != null)
            {
                inv.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;
                inv.OnPartyEquipmentChanged += OnPartyEquipmentChanged;
            }

            var items = RefreshDictItem();
            RequestResponsiveGridRefresh();
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

        protected override void OnHide()
        {
            StopResponsiveGridRefresh();
            UnbindInputDeviceChanged();
            base.OnHide();
        }

        private void BindInputDeviceChanged()
        {
            UnbindInputDeviceChanged();
            _inputService = Svc.Input;
            if (_inputService != null)
            {
                _inputService.OnActiveDeviceChanged += OnActiveInputDeviceChanged;
                RefreshQuickSlotBindingLabels(_inputService.ActiveDevice);
            }
        }

        private void UnbindInputDeviceChanged()
        {
            if (_inputService == null)
                return;

            _inputService.OnActiveDeviceChanged -= OnActiveInputDeviceChanged;
            _inputService = null;
        }

        private void OnActiveInputDeviceChanged(ActiveInputDevice device)
            => RefreshQuickSlotBindingLabels(device);

        /// <summary>
        /// 구 빌더 산출물(숫자 5~8 라벨)도 프리팹 재생성 전부터 실제 액션 바인딩을 표시한다.
        /// 신 빌더 산출물에서는 UI_InputPromptIcon이 스프라이트 글리프를 우선 표시한다.
        /// </summary>
        private void RefreshQuickSlotBindingLabels(ActiveInputDevice device)
        {
            if (_quickSlotButtons == null)
                return;

            int count = Mathf.Min(_quickSlotButtons.Length, QuickSlotActionNames.Length);
            for (int i = 0; i < count; i++)
            {
                TMP_Text label = _quickSlotButtons[i]?.Text;
                if (label == null)
                    continue;

                InputGlyphResult result = InputGlyphResolver.Resolve(
                    InputMapNames.PlayerAction,
                    QuickSlotActionNames[i],
                    device,
                    _inputService?.GamepadBrand ?? GamepadBrand.Generic,
                    null);
                label.text = result.Count > 0
                    ? result.Primary.Text
                    : QuickSlotActionNames[i];
            }
        }

        private void RefreshPlayTime()
        {
            if (_txtPlayTime == null)
                return;

            string formatted = Svc.GameTime?.FormatPlayTime();
            _txtPlayTime.text = string.IsNullOrWhiteSpace(formatted)
                ? "플레이 시간  --:--:--"
                : $"플레이 시간  {formatted}";
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
                $"{InventoryMgr.GetTotalWeight():0.0} / {InventoryMgr.MaxWeight:0.0} kg";

            if (_txtGold != null)
                _txtGold.text = InventoryMgr.Gold.ToString("N0");

            if (_txtItemCount != null)
                _txtItemCount.text = $"{GetCategoryLabel()}  {_visibleItemCount} / {InventoryMgr.MaxSlots}";
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

            IReadOnlyList<CharacterActorType> roster = PartyMgr?.Roster;
            var memberData = PartyMgr?.PartyMemberDataSO;
            if (roster == null) return;

            IReadOnlyList<CharacterActorType> displayed = PartyMgr?.BattleOrder;
            if (displayed == null || displayed.Count == 0)
                displayed = roster;

            int maxBattleSize = Mathf.Max(1, PartyMgr?.MaxBattleSize ?? 4);
            int createdCount = 0;
            for (int i = 0; i < displayed.Count && createdCount < maxBattleSize; i++)
            {
                var type = displayed[i];
                if (type == CharacterActorType.None) continue;

                var entry = Instantiate(_partyEntryPrefab, _partySelectorContainer);
                Sprite portrait = memberData != null ? memberData.GetHeadSprite(type) : null;
                string charName = memberData != null ? memberData.GetName(type) : type.ToString();
                if (string.IsNullOrWhiteSpace(charName))
                    charName = type.ToString();
                entry.Bind(type, portrait, charName, SelectCharacter);
                entry.SetMeta(createdCount + 1, PartyMgr?.GetLevel(type) ?? 1, type == PartyMgr?.ActiveCharacterType);
                _partyEntries.Add(entry);
                createdCount++;
            }

            for (int i = createdCount; i < maxBattleSize; i++)
            {
                var lockedEntry = Instantiate(_partyEntryPrefab, _partySelectorContainer);
                lockedEntry.Bind(CharacterActorType.None, null, "빈 슬롯", null);
                lockedEntry.SetMeta(i + 1, 1, false);
                lockedEntry.SetLocked(true);
                _partyEntries.Add(lockedEntry);
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
            RefreshSelectedCharacterSummary();
            RefreshActionButtons();
        }

        private void RefreshSelectedCharacterSummary()
        {
            var party = PartyMgr;
            var memberData = party?.PartyMemberDataSO;
            if (party == null || _selectedCharacter == CharacterActorType.None)
                return;

            if (_selectedCharacterPortrait != null)
            {
                _selectedCharacterPortrait.sprite = memberData?.GetFullBodySprite(_selectedCharacter)
                                                    ?? memberData?.GetHeadSprite(_selectedCharacter);
                _selectedCharacterPortrait.enabled = _selectedCharacterPortrait.sprite != null;
            }

            int level = party.GetLevel(_selectedCharacter);
            long exp = party.GetExp(_selectedCharacter);
            long requiredExp = party.GetRequiredExp(_selectedCharacter);
            if (_selectedCharacterLevelText != null)
                _selectedCharacterLevelText.text = $"Lv.{level}";
            if (_selectedCharacterExpFill != null)
                _selectedCharacterExpFill.fillAmount = requiredExp > 0
                    ? Mathf.Clamp01((float)exp / requiredExp)
                    : 1f;
            if (_selectedCharacterExpText != null)
                _selectedCharacterExpText.text = $"{exp:N0} / {requiredExp:N0}";

            PartyCombatPowerResult power = party.GetEffectiveCombatPower(_selectedCharacter);
            if (_selectedCharacterCombatPowerText != null)
                _selectedCharacterCombatPowerText.text = power.CombatPower.ToString("N0");

            var stats = power.GrowthStats;
            float maxHp = GetAttribute(stats, AttributeIds.Vital.MaxHealth);
            float currentHp = maxHp;
            var player = UISvc.Actors?.Player;
            if (player != null)
                currentHp = player.GetHealthForCharacter(_selectedCharacter);

            if (_selectedCharacterHpText != null)
                _selectedCharacterHpText.text = $"{Mathf.RoundToInt(currentHp):N0} / {Mathf.RoundToInt(maxHp):N0}";
            if (_selectedCharacterAttackText != null)
                _selectedCharacterAttackText.text = StatDisplayFormatter.FormatValue(
                    AttributeIds.Combat.AttackPower,
                    GetAttribute(stats, AttributeIds.Combat.AttackPower));
            if (_selectedCharacterDefenseText != null)
                _selectedCharacterDefenseText.text = StatDisplayFormatter.FormatValue(
                    AttributeIds.Combat.Defense,
                    GetAttribute(stats, AttributeIds.Combat.Defense));
            if (_selectedCharacterCritText != null)
                _selectedCharacterCritText.text = StatDisplayFormatter.FormatValue(
                    AttributeIds.Combat.CritRate,
                    GetAttribute(stats, AttributeIds.Combat.CritRate));
        }

        private static float GetAttribute(
            IReadOnlyDictionary<AttributeId, float> attributes,
            AttributeId attributeId)
            => attributes != null
               && attributes.TryGetValue(attributeId, out float value)
                ? value
                : UPlayGroundAttributeDefaults.Get(attributeId);

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
            if (_itemClickTap.transform is RectTransform highlight)
            {
                highlight.anchorMin = Vector2.zero;
                highlight.anchorMax = Vector2.one;
                highlight.offsetMin = new Vector2(2f, 2f);
                highlight.offsetMax = new Vector2(-2f, -2f);
                highlight.localScale = Vector3.one;
            }
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
            _visibleItemCount = items.Count;

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
            RefreshSortLabel();
            var items = RefreshDictItem();
            SetInventory();
            RefreshSelectionForItems(items);
        }

        private UICommonButtonClickResult OnClickCycleSort()
        {
            _sortMode = (InventorySortMode)(((int)_sortMode + 1) % 4);
            if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify((int)_sortMode);
            RefreshSortLabel();
            var items = RefreshDictItem();
            SetInventory();
            RefreshSelectionForItems(items);
            return UICommonButtonClickResult.Success;
        }

        private void RefreshSortLabel()
        {
            if (_sortModeText == null)
                return;

            _sortModeText.text = _sortMode switch
            {
                InventorySortMode.Name => "정렬 : 이름순",
                InventorySortMode.Rarity => "정렬 : 등급순",
                InventorySortMode.Weight => "정렬 : 무게순",
                _ => "정렬 : 최근 획득순",
            };
        }

        private string GetCategoryLabel()
        {
            return _categoryFilter.HasValue
                ? _categoryFilter.Value.ToDisplayString()
                : "전체";
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

        private void RequestResponsiveGridRefresh()
        {
            StopResponsiveGridRefresh();

            // 같은 프레임에도 가능한 한 먼저 맞추고, LayoutGroup/ScrollRect가 최종 폭을
            // 확정하는 다음 프레임들에서 다시 계산해 첫 진입과 재진입 결과를 동일하게 만든다.
            ForceRefreshResponsiveGrid();
            _gridLayoutRefreshCoroutine = StartCoroutine(RefreshResponsiveGridAfterLayout());
        }

        private IEnumerator RefreshResponsiveGridAfterLayout()
        {
            const int stabilizationFrames = 2;
            for (int i = 0; i < stabilizationFrames; i++)
            {
                yield return null;
                ForceRefreshResponsiveGrid();
            }

            _gridLayoutRefreshCoroutine = null;
        }

        private void ForceRefreshResponsiveGrid()
        {
            Canvas.ForceUpdateCanvases();
            if (_rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);

            _lastGridWidth = -1f;
            RefreshResponsiveGrid();
        }

        private void StopResponsiveGridRefresh()
        {
            if (_gridLayoutRefreshCoroutine == null)
                return;

            StopCoroutine(_gridLayoutRefreshCoroutine);
            _gridLayoutRefreshCoroutine = null;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshResponsiveGrid();
        }

        /// <summary>
        /// 중앙 패널 폭에 따라 8~12열을 선택해 셀을 약 118px 수준으로 유지한다.
        /// 남는 폭은 선택된 열 수로 다시 균등 분배해 우측에 큰 공백도 남기지 않는다.
        /// </summary>
        private void RefreshResponsiveGrid()
        {
            if (_itemGrid == null || _content == null)
                return;

            var contentRect = _content as RectTransform;
            if (contentRect == null)
                return;

            // Content는 첫 활성화 프레임에 이전/기본 폭을 잠시 가질 수 있다.
            // 실제 표시 폭의 소유자인 Viewport를 기준으로 계산해야 첫 진입도 안정적이다.
            var viewportRect = contentRect.parent as RectTransform;
            float width = viewportRect != null ? viewportRect.rect.width : contentRect.rect.width;
            if (width <= 1f || Mathf.Approximately(width, _lastGridWidth))
                return;

            _lastGridWidth = width;
            const float preferredCellSize = 118f;
            const int minColumns = 8;
            const int maxColumns = 12;

            float horizontalPadding = _itemGrid.padding.left + _itemGrid.padding.right;
            float usableWidth = Mathf.Max(0f, width - horizontalPadding);
            int columns = Mathf.Clamp(
                Mathf.FloorToInt((usableWidth + _itemGrid.spacing.x) /
                                 (preferredCellSize + _itemGrid.spacing.x)),
                minColumns,
                maxColumns);
            float horizontalSpacing = _itemGrid.spacing.x * (columns - 1);
            float cellSize = Mathf.Max(72f, Mathf.Floor((usableWidth - horizontalSpacing) / columns));
            _itemGrid.constraintCount = columns;
            _itemGrid.cellSize = new Vector2(cellSize, cellSize);
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
                RefreshSelectedEquipmentStats(equip, inst);
                RefreshEquipmentComparison(equip);
            }
            else if (itemData is ConsumableSO consumable)
            {
                RefreshSelectedConsumableStats(consumable);
                SetComparisonPanelActive(false);
            }
            else
            {
                if (_statPanel != null) _statPanel.SetActive(false);
                ClearSelectedEquipmentStats();
                SetComparisonPanelActive(false);
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
            SetComparisonPanelActive(false);
            ClearSelectedEquipmentStats();

            _selectedItemPrefab.SetActive(false);
            RefreshActionButtons();
        }

        private void RefreshSelectedEquipmentStats(EquipmentSO equip, ItemInstance instance)
        {
            var modifiers = new List<AttributeModifierValue>();
            equip?.AddAttributeModifiersTo(modifiers);

            var displayRows = new List<string>(modifiers.Count + (instance?.growthAttributeRolls?.Count ?? 0));
            for (int i = 0; i < modifiers.Count; i++)
            {
                AttributeModifierValue modifier = modifiers[i];
                if (modifier.AttributeId.IsValid)
                    displayRows.Add(StatDisplayFormatter.FormatModifier(
                        modifier.AttributeId,
                        modifier.Operation,
                        modifier.Value));
            }

            if (instance?.growthAttributeRolls != null)
            {
                for (int i = 0; i < instance.growthAttributeRolls.Count; i++)
                    displayRows.Add(FormatGrowthAttributeRoll(instance.growthAttributeRolls[i]));
            }

            if (_statPanel != null)
                _statPanel.SetActive(displayRows.Count > 0);

            EnsureStatRows(displayRows.Count);

            for (int i = 0; i < _statRows.Count; i++)
            {
                TextMeshProUGUI row = _statRows[i];
                if (row == null)
                    continue;

                bool active = i < displayRows.Count;
                SetStatRowActive(row, active);
                row.text = active
                    ? displayRows[i]
                    : string.Empty;
            }
        }

        private void RefreshEquipmentComparison(EquipmentSO selected)
        {
            if (selected == null ||
                _selectedCharacter == CharacterActorType.None ||
                selected.equipSlot == EquipPosition.None)
            {
                SetComparisonPanelActive(false);
                return;
            }

            int equippedSlotKey = InventoryMgr.GetEquippedItem(_selectedCharacter, selected.equipSlot);
            if (equippedSlotKey < 0 || equippedSlotKey == _selectedInventorySlotKey)
            {
                SetComparisonPanelActive(false);
                return;
            }

            ItemInstance equippedInstance = InventoryMgr.GetInventoryItemBySlotKey(equippedSlotKey);
            var equipped = equippedInstance?.data as EquipmentSO;
            if (equipped == null)
            {
                SetComparisonPanelActive(false);
                return;
            }

            SetComparisonPanelActive(true);
            if (_comparisonItemNameText != null)
                _comparisonItemNameText.text = $"현재: {equipped.itemName}  →  선택: {selected.itemName}";
            if (_comparisonStatsText != null)
            {
                ItemInstance selectedInstance =
                    InventoryMgr.GetInventoryItemBySlotKey(_selectedInventorySlotKey)
                    ?? InventoryMgr.GetItem(selected.itemId);
                _comparisonStatsText.text = BuildEquipmentComparisonText(
                    equipped,
                    equippedInstance,
                    selected,
                    selectedInstance);
            }
        }

        private void SetComparisonPanelActive(bool active)
        {
            if (_comparisonPanel != null)
                _comparisonPanel.SetActive(active);
            if (!active && _comparisonStatsText != null)
                _comparisonStatsText.text = string.Empty;
        }

        private static string BuildEquipmentComparisonText(
            EquipmentSO equipped,
            ItemInstance equippedInstance,
            EquipmentSO selected,
            ItemInstance selectedInstance)
        {
            var equippedValues = CollectModifierValues(equipped);
            var selectedValues = CollectModifierValues(selected);
            var keys = new HashSet<(AttributeId attribute, AttributeModifierOperation modifier)>(
                equippedValues.Keys);
            keys.UnionWith(selectedValues.Keys);

            var orderedKeys = keys
                .OrderBy(key => key.attribute.Value, StringComparer.Ordinal)
                .ThenBy(key => (int)key.modifier);
            var rows = new List<string>();

            foreach (var key in orderedKeys)
            {
                equippedValues.TryGetValue(key, out float current);
                selectedValues.TryGetValue(key, out float next);
                float delta = next - current;
                rows.Add(
                    $"{StatDisplayFormatter.GetDisplayName(key.attribute)}  " +
                    $"{FormatComparisonValue(key.attribute, key.modifier, current)} → " +
                    $"{FormatComparisonValue(key.attribute, key.modifier, next)}  " +
                    FormatComparisonDelta(key.attribute, key.modifier, delta));
            }

            AppendGrowthComparisonRows(
                rows,
                equippedInstance?.growthAttributeRolls,
                selectedInstance?.growthAttributeRolls);

            return rows.Count > 0
                ? string.Join("\n", rows)
                : "비교할 능력치가 없습니다.";
        }

        private static Dictionary<(AttributeId attribute, AttributeModifierOperation modifier), float>
            CollectModifierValues(
            EquipmentSO equipment)
        {
            var modifiers = new List<AttributeModifierValue>();
            equipment?.AddAttributeModifiersTo(modifiers);
            var values =
                new Dictionary<(AttributeId, AttributeModifierOperation), float>();

            for (int i = 0; i < modifiers.Count; i++)
            {
                AttributeModifierValue modifier = modifiers[i];
                if (!modifier.AttributeId.IsValid)
                    continue;
                var key = (modifier.AttributeId, modifier.Operation);
                if (modifier.Operation == AttributeModifierOperation.Multiply)
                {
                    float previous = values.TryGetValue(key, out float value) ? value : 1f;
                    values[key] = previous * modifier.Value;
                }
                else
                {
                    values[key] = values.TryGetValue(key, out float value)
                        ? value + modifier.Value
                        : modifier.Value;
                }
            }

            return values;
        }

        private static void AppendGrowthComparisonRows(
            ICollection<string> rows,
            IReadOnlyList<EquipmentGrowthAttributeRoll> equippedRolls,
            IReadOnlyList<EquipmentGrowthAttributeRoll> selectedRolls)
        {
            var currentRanks = CollectGrowthRanks(equippedRolls);
            var selectedRanks = CollectGrowthRanks(selectedRolls);
            var types = new HashSet<GrowthAttributeType>(currentRanks.Keys);
            types.UnionWith(selectedRanks.Keys);

            foreach (GrowthAttributeType type in types.OrderBy(value => (int)value))
            {
                currentRanks.TryGetValue(type, out int current);
                selectedRanks.TryGetValue(type, out int next);
                int delta = next - current;
                string deltaText = delta == 0
                    ? "<color=#A6B3C2>—</color>"
                    : delta > 0
                        ? $"<color=#78D86B>▲ +{delta}</color>"
                        : $"<color=#F06B67>▼ {delta}</color>";
                rows.Add($"랜덤 성장 {GetGrowthAttributeName(type)}  R{current} → R{next}  {deltaText}");
            }
        }

        private static Dictionary<GrowthAttributeType, int> CollectGrowthRanks(
            IReadOnlyList<EquipmentGrowthAttributeRoll> rolls)
        {
            var ranks = new Dictionary<GrowthAttributeType, int>();
            if (rolls == null)
                return ranks;

            for (int i = 0; i < rolls.Count; i++)
            {
                EquipmentGrowthAttributeRoll roll = rolls[i];
                ranks[roll.attributeType] = ranks.TryGetValue(roll.attributeType, out int rank)
                    ? rank + Mathf.Max(0, roll.rank)
                    : Mathf.Max(0, roll.rank);
            }

            return ranks;
        }

        private static string FormatComparisonValue(
            AttributeId attributeId,
            AttributeModifierOperation modifier,
            float value)
        {
            return modifier switch
            {
                AttributeModifierOperation.Percent => $"{value * 100f:0.#}%",
                AttributeModifierOperation.Multiply => $"x{value:0.##}",
                _ when IsRatioAttribute(attributeId) =>
                    $"{value * 100f:0.#}%",
                _ => $"{value:0.##}",
            };
        }

        private static string FormatComparisonDelta(
            AttributeId attributeId,
            AttributeModifierOperation modifier,
            float delta)
        {
            if (Mathf.Approximately(delta, 0f))
                return "<color=#A6B3C2>—</color>";

            string marker = delta > 0f ? "▲" : "▼";
            string color = delta > 0f ? "#78D86B" : "#F06B67";
            string sign = delta > 0f ? "+" : string.Empty;
            string value = modifier == AttributeModifierOperation.Percent ||
                           IsRatioAttribute(attributeId)
                ? $"{sign}{delta * 100f:0.#}%"
                : $"{sign}{delta:0.##}";
            return $"<color={color}>{marker} {value}</color>";
        }

        private static bool IsRatioAttribute(AttributeId attributeId)
            => attributeId == AttributeIds.Combat.Defense
               || attributeId == AttributeIds.Combat.CritRate
               || attributeId == AttributeIds.Combat.CritMultiplier;

        private static string GetGrowthAttributeName(GrowthAttributeType type)
        {
            return type switch
            {
                GrowthAttributeType.Health => "체력",
                GrowthAttributeType.Defense => "방어력",
                GrowthAttributeType.Critical => "크리티컬",
                GrowthAttributeType.AttackSpeed => "공격 속도",
                _ => "공격력",
            };
        }

        private static string FormatGrowthAttributeRoll(EquipmentGrowthAttributeRoll roll)
        {
            return $"랜덤 성장 - {GetGrowthAttributeName(roll.attributeType)} +{Mathf.Max(0, roll.rank)}랭크";
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
            if (_quickSlotButtons != null)
            {
                for (int i = 0; i < _quickSlotButtons.Length; i++)
                {
                    int slotIndex = i;
                    _quickSlotButtons[i]?.BindClickResult(
                        () => OnClickAssignQuickSlot(slotIndex));
                }
            }

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
            bool canAssignQuickSlot = hasItem && _selectedItemData.itemType == ItemType.CONSUMABLE;

            if (_quickSlotRegistrationRoot != null)
            {
                _quickSlotRegistrationRoot.SetActive(canAssignQuickSlot);
            }
            else if (_quickSlotButtons != null)
            {
                // 구 프리팹 호환: 전용 루트가 없으면 버튼만 개별 제어한다.
                for (int i = 0; i < _quickSlotButtons.Length; i++)
                    SetActionButtonActive(_quickSlotButtons[i], canAssignQuickSlot);
            }
        }

        private UICommonButtonClickResult OnClickAssignQuickSlot(int slotIndex)
        {
            if (_selectedItemData == null
                || _selectedItemData.itemType != ItemType.CONSUMABLE)
                return UICommonButtonClickResult.Failed;

            return UIQuickSlotAssignments.Assign(slotIndex, _selectedItemData)
                ? UICommonButtonClickResult.Success
                : UICommonButtonClickResult.Failed;
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
                Svc.Sound?.PlayUi(GameSoundKey.Heal);

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
