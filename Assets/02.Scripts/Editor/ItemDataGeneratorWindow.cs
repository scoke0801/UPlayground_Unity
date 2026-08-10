#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Data.Item.Editor
{
    /// <summary>
    /// ItemSO / EquipmentSO 에셋을 ID 대역 규칙에 따라 자동 발급하는 에디터 윈도우 (UIToolkit).
    /// 메뉴: UPlayGround / Item / Item Data Generator
    /// </summary>
    public class ItemDataGeneratorWindow : EditorWindow
    {
        private readonly struct ItemIdRange
        {
            public readonly int Min;
            public readonly int Max;
            public readonly string Label;
            public readonly string SavePath;
            public readonly string FilePrefix;

            public ItemIdRange(int min, int max, string label, string savePath, string filePrefix)
            {
                Min = min;
                Max = max;
                Label = label;
                SavePath = savePath;
                FilePrefix = filePrefix;
            }

            public bool Contains(int id) => id >= Min && id <= Max;
            public override string ToString() => $"{Label} ({Min}~{Max})";
        }

        private enum IssueMode
        {
            Armor,
            Weapon,
            Consumable,
            Material,
            Special,
        }

        private ItemDatabase _itemDb;
        private readonly List<ItemSO> _items = new();
        private readonly HashSet<int> _duplicateIDs = new();

        private IssueMode _issueMode = IssueMode.Material;
        private EquipPosition _equipPosition = EquipPosition.Head;
        private WeaponType _weaponType = WeaponType.Sword;
        private ItemType _itemType = ItemType.OTHERS;
        private ItemRarity _itemRarity = ItemRarity.COMMON;

        private int _previewID;
        private string _itemName = "새 아이템";
        private string _assetName = "";
        private string _itemDescription = "";
        private float _weight;
        private Sprite _icon;
        private GameObject _equipmentPrefab;
        private EquipmentVisualMode _equipmentVisualMode = EquipmentVisualMode.Prefab;
        private ConsumableEffectType _consumableEffect = ConsumableEffectType.HealFlat;
        private float _consumableAmount;
        private bool _requireEffectiveUse = true;
        private bool _refreshDatabase = true;
        private bool _generateItemEnum = true;
        private bool _selectCreatedAsset = true;

        // ──── UI 요소 ────
        private Label       _dbLabel;
        private EnumField   _equipPositionField;
        private EnumField   _weaponTypeField;
        private Label       _weaponSlotLabel;
        private ObjectField _equipPrefabField;
        private VisualElement _consumableGroup;
        private FloatField  _consumableAmountField;
        private Label       _previewRangeLabel;
        private Label       _previewIdLabel;
        private Label       _previewPathLabel;
        private Label       _previewTypeLabel;
        private Label       _previewFileLabel;
        private HelpBox     _duplicateWarning;
        private HelpBox     _validationBox;
        private Button      _createButton;

        private const string ITEM_ENUM_OUTPUT_PATH = "Assets/02.Scripts/Data/Item/ItemIdType.cs";

        private static readonly Dictionary<EquipPosition, ItemIdRange> ArmorRanges = new()
        {
            { EquipPosition.Head,   new ItemIdRange(100, 199, "머리 장비", "Assets/10.Datas/Item/Equipment", "Head") },
            { EquipPosition.Chest,  new ItemIdRange(200, 299, "상의 장비", "Assets/10.Datas/Item/Equipment", "Chest") },
            { EquipPosition.Pants,  new ItemIdRange(300, 399, "하의 장비", "Assets/10.Datas/Item/Equipment", "Pants") },
            { EquipPosition.Gloves, new ItemIdRange(400, 499, "장갑 장비", "Assets/10.Datas/Item/Equipment", "Gloves") },
            { EquipPosition.Shoes,  new ItemIdRange(500, 599, "신발 장비", "Assets/10.Datas/Item/Equipment", "Shoes") },
        };

        private static readonly Dictionary<WeaponType, ItemIdRange> WeaponRanges = new()
        {
            { WeaponType.Sword,      new ItemIdRange(1000, 1999, "소검", "Assets/10.Datas/Item/Equipment/Weapon", "Sword") },
            { WeaponType.SwordShield,     new ItemIdRange(2000, 2999, "방패", "Assets/10.Datas/Item/Equipment/Weapon", "Shield") },
            { WeaponType.Staff,      new ItemIdRange(3000, 3999, "지팡이", "Assets/10.Datas/Item/Equipment/Weapon", "Staff") },
            { WeaponType.GreatSword, new ItemIdRange(4000, 4999, "대검", "Assets/10.Datas/Item/Equipment/Weapon", "GreatSword") },
            { WeaponType.Bow,        new ItemIdRange(5000, 5999, "활", "Assets/10.Datas/Item/Equipment/Weapon", "Bow") },
            { WeaponType.Arrow,      new ItemIdRange(6000, 6999, "화살", "Assets/10.Datas/Item/Equipment/Weapon", "Arrow") },
            { WeaponType.Katana,     new ItemIdRange(7000, 7999, "카타나", "Assets/10.Datas/Item/Equipment/Weapon", "Katana") },
            { WeaponType.DoubleAxe,  new ItemIdRange(8000, 8999, "쌍도끼", "Assets/10.Datas/Item/Equipment/Weapon", "DoubleAxe") },
            { WeaponType.Whip,       new ItemIdRange(9000, 9999, "채찍", "Assets/10.Datas/Item/Equipment/Weapon", "Whip") },
            { WeaponType.Spear,      new ItemIdRange(10000, 10999, "창", "Assets/10.Datas/Item/Equipment/Weapon", "Spear") },
            { WeaponType.DualBlade,  new ItemIdRange(11000, 11999, "쌍검", "Assets/10.Datas/Item/Equipment/Weapon", "DualBlade") },
        };

        private static readonly ItemIdRange ConsumableRange = new(50000, 99999, "소비 아이템", "Assets/10.Datas/Item", "Consumable");
        private static readonly ItemIdRange MaterialRange = new(100000, 199999, "재료/기타", "Assets/10.Datas/Item", "Material");
        private static readonly ItemIdRange SpecialRange = new(200000, 299999, "특수 아이템", "Assets/10.Datas/Item", "Special");

        public static void Open()
        {
            var win = GetWindow<ItemDataGeneratorWindow>("Item Data Generator");
            win.minSize = new Vector2(620f, 520f);
            win.Show();
        }

        // ──────────────────────────────────────────────────────────
        #region UI 구성

        private void CreateGUI()
        {
            LoadItemDatabase();
            RefreshPreviewID();

            var root = rootVisualElement;
            root.Clear();

            root.Add(BuildToolbar());

            var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 6, paddingRight = 6, paddingTop = 6 } };
            root.Add(scroll);

            scroll.Add(BuildModeSection());
            scroll.Add(BuildDataSection());
            scroll.Add(BuildPreviewSection());
            scroll.Add(BuildActionSection());

            UpdateUI();
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new ToolbarButton(() =>
            {
                LoadItemDatabase();
                RefreshPreviewID();
                UpdateUI();
            }) { text = "새로고침" });

            _dbLabel = new Label { style = { fontSize = 10, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };
            toolbar.Add(_dbLabel);

            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            var refreshToggle = new ToolbarToggle { text = "DB 갱신", value = _refreshDatabase };
            refreshToggle.RegisterValueChangedCallback(evt => _refreshDatabase = evt.newValue);
            toolbar.Add(refreshToggle);

            var enumToggle = new ToolbarToggle { text = "ItemIdType 생성", value = _generateItemEnum };
            enumToggle.RegisterValueChangedCallback(evt => _generateItemEnum = evt.newValue);
            toolbar.Add(enumToggle);

            var selectToggle = new ToolbarToggle { text = "생성 에셋 선택", value = _selectCreatedAsset };
            selectToggle.RegisterValueChangedCallback(evt => _selectCreatedAsset = evt.newValue);
            toolbar.Add(selectToggle);

            return toolbar;
        }

        private VisualElement BuildModeSection()
        {
            var section = MakeSection("발급 카테고리");

            var modeField = new EnumField("발급 모드", _issueMode);
            modeField.RegisterValueChangedCallback(evt =>
            {
                _issueMode = (IssueMode)evt.newValue;
                ApplyModeSideEffects();
                RefreshPreviewID();
                UpdateUI();
            });
            section.Add(modeField);

            _equipPositionField = new EnumField("장비 부위", _equipPosition);
            _equipPositionField.RegisterValueChangedCallback(evt =>
            {
                _equipPosition = (EquipPosition)evt.newValue;
                RefreshPreviewID();
                UpdateUI();
            });
            section.Add(_equipPositionField);

            _weaponTypeField = new EnumField("무기 타입", _weaponType);
            _weaponTypeField.RegisterValueChangedCallback(evt =>
            {
                _weaponType = (WeaponType)evt.newValue;
                _equipPosition = GetDefaultWeaponEquipPosition(_weaponType);
                RefreshPreviewID();
                UpdateUI();
            });
            section.Add(_weaponTypeField);

            _weaponSlotLabel = new Label { style = { marginLeft = 4 } };
            section.Add(_weaponSlotLabel);

            var rarityField = new EnumField("희귀도", _itemRarity);
            rarityField.RegisterValueChangedCallback(evt =>
            {
                _itemRarity = (ItemRarity)evt.newValue;
                RefreshPreviewID();
                UpdateUI();
            });
            section.Add(rarityField);

            return section;
        }

        private VisualElement BuildDataSection()
        {
            var section = MakeSection("아이템 데이터");

            var nameField = new TextField("표시 이름") { value = _itemName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _itemName = evt.newValue;
                UpdateUI();
            });
            section.Add(nameField);

            var assetNameField = new TextField("파일명") { value = _assetName };
            assetNameField.RegisterValueChangedCallback(evt =>
            {
                _assetName = evt.newValue;
                UpdateUI();
            });
            section.Add(assetNameField);

            var descField = new TextField("설명") { value = _itemDescription };
            descField.RegisterValueChangedCallback(evt => _itemDescription = evt.newValue);
            section.Add(descField);

            var weightField = new FloatField("무게") { value = _weight };
            weightField.RegisterValueChangedCallback(evt =>
            {
                float v = Mathf.Max(0f, evt.newValue);
                weightField.SetValueWithoutNotify(v);
                _weight = v;
            });
            section.Add(weightField);

            var iconField = new ObjectField("아이콘") { objectType = typeof(Sprite), allowSceneObjects = false, value = _icon };
            iconField.RegisterValueChangedCallback(evt => _icon = evt.newValue as Sprite);
            section.Add(iconField);

            _equipPrefabField = new ObjectField("장비 프리팹") { objectType = typeof(GameObject), allowSceneObjects = false, value = _equipmentPrefab };
            _equipPrefabField.RegisterValueChangedCallback(evt => _equipmentPrefab = evt.newValue as GameObject);
            section.Add(_equipPrefabField);

            var visualModeField = new EnumField("무기 외형 방식", _equipmentVisualMode);
            visualModeField.RegisterValueChangedCallback(evt =>
                _equipmentVisualMode = (EquipmentVisualMode)evt.newValue);
            section.Add(visualModeField);

            _consumableGroup = new VisualElement();
            var effectField = new EnumField("효과 타입", _consumableEffect);
            effectField.RegisterValueChangedCallback(evt =>
            {
                _consumableEffect = (ConsumableEffectType)evt.newValue;
                UpdateUI();
            });
            _consumableGroup.Add(effectField);

            _consumableAmountField = new FloatField("회복량") { value = _consumableAmount };
            _consumableAmountField.RegisterValueChangedCallback(evt =>
            {
                float v = Mathf.Max(0f, evt.newValue);
                _consumableAmountField.SetValueWithoutNotify(v);
                _consumableAmount = v;
            });
            _consumableGroup.Add(_consumableAmountField);

            var effectiveToggle = new Toggle("효과 없으면 소모 안 함") { value = _requireEffectiveUse };
            effectiveToggle.RegisterValueChangedCallback(evt => _requireEffectiveUse = evt.newValue);
            _consumableGroup.Add(effectiveToggle);
            section.Add(_consumableGroup);

            return section;
        }

        private VisualElement BuildPreviewSection()
        {
            var section = MakeSection("발급 미리보기");

            _previewRangeLabel = MakePreviewLabel("ID 대역");
            _previewIdLabel    = MakePreviewLabel("발급 ID");
            _previewPathLabel  = MakePreviewLabel("저장 경로");
            _previewTypeLabel  = MakePreviewLabel("SO 타입");
            _previewFileLabel  = MakePreviewLabel("최종 파일명");
            section.Add(_previewRangeLabel);
            section.Add(_previewIdLabel);
            section.Add(_previewPathLabel);
            section.Add(_previewTypeLabel);
            section.Add(_previewFileLabel);

            _duplicateWarning = new HelpBox("", HelpBoxMessageType.Warning);
            section.Add(_duplicateWarning);

            return section;
        }

        private static Label MakePreviewLabel(string prefix)
        {
            var label = new Label { userData = prefix };
            return label;
        }

        private static void SetPreview(Label label, string value)
        {
            label.text = $"{label.userData,-12}  {value}";
        }

        private VisualElement BuildActionSection()
        {
            var section = new VisualElement { style = { marginTop = 10, marginBottom = 10 } };

            _validationBox = new HelpBox("", HelpBoxMessageType.Warning);
            section.Add(_validationBox);

            _createButton = new Button(() =>
            {
                CreateItemAsset();
                UpdateUI();
            }) { text = "아이템 데이터 생성", style = { height = 38 } };
            section.Add(_createButton);

            return section;
        }

        private void ApplyModeSideEffects()
        {
            switch (_issueMode)
            {
                case IssueMode.Armor:
                    _itemType = ItemType.EQUIPMENT;
                    _weaponType = WeaponType.NoWeapon;
                    break;
                case IssueMode.Weapon:
                    _itemType = ItemType.EQUIPMENT;
                    if (_weaponType == WeaponType.NoWeapon)
                    {
                        _weaponType = WeaponType.Sword;
                        _weaponTypeField?.SetValueWithoutNotify(_weaponType);
                    }
                    _equipPosition = GetDefaultWeaponEquipPosition(_weaponType);
                    break;
                case IssueMode.Consumable:
                    _itemType = ItemType.CONSUMABLE;
                    break;
                case IssueMode.Material:
                    _itemType = ItemType.OTHERS;
                    break;
                case IssueMode.Special:
                    _itemType = ItemType.NONE;
                    break;
            }
        }

        private void UpdateUI()
        {
            // DB 라벨
            _dbLabel.text = _itemDb != null ? $"ItemDB: {_itemDb.name}" : "ItemDB 없음";
            _dbLabel.style.color = _itemDb == null ? (StyleColor)Color.red : StyleKeyword.Null;

            // 모드별 조건부 필드
            _equipPositionField.style.display = _issueMode == IssueMode.Armor ? DisplayStyle.Flex : DisplayStyle.None;
            _weaponTypeField.style.display    = _issueMode == IssueMode.Weapon ? DisplayStyle.Flex : DisplayStyle.None;
            _weaponSlotLabel.style.display    = _issueMode == IssueMode.Weapon ? DisplayStyle.Flex : DisplayStyle.None;
            _weaponSlotLabel.text             = $"장착 위치      {_equipPosition}";

            _equipPrefabField.style.display = _itemType == ItemType.EQUIPMENT ? DisplayStyle.Flex : DisplayStyle.None;
            _consumableGroup.style.display  = _itemType == ItemType.CONSUMABLE ? DisplayStyle.Flex : DisplayStyle.None;
            _consumableAmountField.label =
                _consumableEffect == ConsumableEffectType.HealPercent ? "회복 비율 (0~1)" : "회복량";

            // 미리보기
            ItemIdRange range = GetCurrentRange();
            SetPreview(_previewRangeLabel, range.ToString());
            SetPreview(_previewIdLabel, _previewID > 0 ? _previewID.ToString() : "발급 가능 ID 없음");
            SetPreview(_previewPathLabel, range.SavePath);
            SetPreview(_previewTypeLabel,
                _itemType == ItemType.EQUIPMENT ? nameof(EquipmentSO)
                : _itemType == ItemType.CONSUMABLE ? nameof(ConsumableSO)
                : nameof(ItemSO));
            SetPreview(_previewFileLabel, BuildAssetFileName(range));

            // 중복 ID 경고
            if (_duplicateIDs.Count > 0)
            {
                _duplicateWarning.text =
                    $"현재 ItemDatabase 기준 중복 ID가 있습니다: {string.Join(", ", _duplicateIDs.OrderBy(i => i))}\n" +
                    "생성은 가능하지만 DB 조회에서 뒤 항목이 무시될 수 있으니 정리를 권장합니다.";
                _duplicateWarning.style.display = DisplayStyle.Flex;
            }
            else
            {
                _duplicateWarning.style.display = DisplayStyle.None;
            }

            // 유효성 검사
            string validation = GetValidationMessage();
            _validationBox.text = validation;
            _validationBox.style.display = string.IsNullOrEmpty(validation) ? DisplayStyle.None : DisplayStyle.Flex;
            _createButton.SetEnabled(string.IsNullOrEmpty(validation));
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginBottom = 6, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f),
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = new Color(0f, 0f, 0f, 0.25f), borderRightColor = new Color(0f, 0f, 0f, 0.25f),
                    borderTopColor = new Color(0f, 0f, 0f, 0.25f), borderBottomColor = new Color(0f, 0f, 0f, 0.25f),
                }
            };
            section.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            return section;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 데이터 로드 / 발급 로직

        private void LoadItemDatabase()
        {
            _items.Clear();
            _duplicateIDs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
            if (guids.Length > 0)
                _itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (_itemDb == null)
                return;

            _itemDb.Initialize();
            var seen = new HashSet<int>();
            foreach (var item in _itemDb.AllItems.Where(i => i != null).OrderBy(i => i.itemId))
            {
                _items.Add(item);
                if (!seen.Add(item.itemId))
                    _duplicateIDs.Add(item.itemId);
            }
        }

        private void RefreshPreviewID()
        {
            _previewID = IssueNextID(GetCurrentRange());
        }

        private int IssueNextID(ItemIdRange range)
        {
            var used = new HashSet<int>(_items.Where(i => i != null).Select(i => i.itemId));
            for (int id = range.Min; id <= range.Max; id++)
            {
                if (!used.Contains(id))
                    return id;
            }

            return 0;
        }

        private ItemIdRange GetCurrentRange()
        {
            return _issueMode switch
            {
                IssueMode.Armor when ArmorRanges.TryGetValue(_equipPosition, out var range) => range,
                IssueMode.Weapon when WeaponRanges.TryGetValue(_weaponType, out var range) => range,
                IssueMode.Consumable => ConsumableRange,
                IssueMode.Material => MaterialRange,
                IssueMode.Special => SpecialRange,
                _ => SpecialRange,
            };
        }

        private string GetValidationMessage()
        {
            if (_itemDb == null)
                return "ItemDatabase를 찾을 수 없습니다.";
            if (_previewID <= 0)
                return "현재 ID 대역에 빈 ID가 없습니다.";
            if (string.IsNullOrWhiteSpace(_itemName))
                return "표시 이름이 비어 있습니다.";
            if (_issueMode == IssueMode.Armor && !ArmorRanges.ContainsKey(_equipPosition))
                return $"{_equipPosition}은 방어구 ID 대역이 정의되지 않았습니다.";
            if (_issueMode == IssueMode.Weapon && (_weaponType == WeaponType.NoWeapon || !WeaponRanges.ContainsKey(_weaponType)))
                return $"{_weaponType}은 무기 ID 대역이 정의되지 않았습니다.";

            return "";
        }

        private void CreateItemAsset()
        {
            ItemIdRange range = GetCurrentRange();
            EnsureFolder(range.SavePath);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{range.SavePath}/{BuildAssetFileName(range)}.asset");
            ItemSO item = _itemType == ItemType.EQUIPMENT
                ? ScriptableObject.CreateInstance<EquipmentSO>()
                : _itemType == ItemType.CONSUMABLE
                    ? ScriptableObject.CreateInstance<ConsumableSO>()
                    : ScriptableObject.CreateInstance<ItemSO>();

            item.itemId = _previewID;
            item.itemName = _itemName;
            item.itemDescription = _itemDescription;
            item.weight = _weight;
            item.itemType = _itemType;
            item.itemRarity = _itemRarity;
            item.icon = _icon;

            if (item is EquipmentSO equipment)
            {
                equipment.equipSlot = _equipPosition;
                equipment.weaponType = _weaponType;
                equipment.visualMode = _equipmentVisualMode;
                equipment.equipmentPrefab = _equipmentPrefab;
            }

            if (item is ConsumableSO consumable)
            {
                consumable.effectType = _consumableEffect;
                consumable.amount = _consumableAmount;
                consumable.requireEffectiveUse = _requireEffectiveUse;
            }

            AssetDatabase.CreateAsset(item, assetPath);
            AssetDatabase.SaveAssets();

            if (_refreshDatabase)
            {
                _itemDb.RefreshDatabase("Assets/10.Datas/Item");
                LoadItemDatabase();
            }
            else
            {
                _items.Add(item);
            }

            if (_generateItemEnum)
                GenerateItemIdType();

            AssetDatabase.Refresh();
            RefreshPreviewID();

            if (_selectCreatedAsset)
            {
                Selection.activeObject = item;
                EditorGUIUtility.PingObject(item);
            }

            EditorUtility.DisplayDialog("생성 완료", $"아이템 생성 완료\nID: {item.itemId}\n경로: {assetPath}", "확인");
        }

        private string BuildAssetFileName(ItemIdRange range)
        {
            if (!string.IsNullOrWhiteSpace(_assetName))
                return SanitizeFileName(_assetName);

            return $"{range.FilePrefix}_{_previewID:000}";
        }

        private static EquipPosition GetDefaultWeaponEquipPosition(WeaponType weaponType)
        {
            return weaponType == WeaponType.SwordShield || weaponType == WeaponType.Arrow
                ? EquipPosition.LeftHand
                : EquipPosition.RightHand;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private void GenerateItemIdType()
        {
            var sourceItems = _itemDb != null
                ? _itemDb.AllItems.Where(i => i != null).OrderBy(i => i.itemId).ToList()
                : _items.Where(i => i != null).OrderBy(i => i.itemId).ToList();

            var raw = sourceItems.Select(item =>
            {
                string name = string.IsNullOrEmpty(item.itemName) ? $"Item_{item.itemId}" : item.itemName;
                return (name, item.itemId);
            });

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
            IdEnumGeneratorUtility.GenerateIntKeyEnum(
                "ItemIdType",
                "ToItemId",
                "Item",
                ITEM_ENUM_OUTPUT_PATH,
                "UPlayGround.Data.Item",
                entries,
                silent: true);
        }

        private static string SanitizeFileName(string value)
        {
            string name = value.Trim();
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(name) ? "NewItem" : name;
        }

        #endregion
    }
}
#endif
