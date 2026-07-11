#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Data.Item.Editor
{
    /// <summary>
    /// ItemSO / EquipmentSO 에셋을 ID 대역 규칙에 따라 자동 발급하는 에디터 윈도우.
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
        private ConsumableEffectType _consumableEffect = ConsumableEffectType.HealFlat;
        private float _consumableAmount;
        private bool _requireEffectiveUse = true;
        private bool _refreshDatabase = true;
        private bool _generateItemEnum = true;
        private bool _selectCreatedAsset = true;

        private Vector2 _scroll;
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

        private void OnEnable()
        {
            LoadItemDatabase();
            RefreshPreviewID();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawModeSection();
            EditorGUILayout.Space(6);
            DrawDataSection();
            EditorGUILayout.Space(6);
            DrawPreviewSection();
            EditorGUILayout.Space(10);
            DrawActionSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                LoadItemDatabase();
                RefreshPreviewID();
            }

            GUILayout.Space(8);
            GUI.color = _itemDb == null ? Color.red : Color.white;
            GUILayout.Label(_itemDb != null ? $"ItemDB: {_itemDb.name}" : "ItemDB 없음", EditorStyles.miniLabel);
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            _refreshDatabase = GUILayout.Toggle(_refreshDatabase, "DB 갱신", EditorStyles.toolbarButton, GUILayout.Width(70));
            _generateItemEnum = GUILayout.Toggle(_generateItemEnum, "ItemIdType 생성", EditorStyles.toolbarButton, GUILayout.Width(105));
            _selectCreatedAsset = GUILayout.Toggle(_selectCreatedAsset, "생성 에셋 선택", EditorStyles.toolbarButton, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("발급 카테고리", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _issueMode = (IssueMode)EditorGUILayout.EnumPopup("발급 모드", _issueMode);

            switch (_issueMode)
            {
                case IssueMode.Armor:
                    _itemType = ItemType.EQUIPMENT;
                    _equipPosition = (EquipPosition)EditorGUILayout.EnumPopup("장비 부위", _equipPosition);
                    _weaponType = WeaponType.NoWeapon;
                    break;
                case IssueMode.Weapon:
                    _itemType = ItemType.EQUIPMENT;
                    _equipPosition = GetDefaultWeaponEquipPosition(_weaponType);
                    _weaponType = (WeaponType)EditorGUILayout.EnumPopup("무기 타입", _weaponType);
                    _equipPosition = GetDefaultWeaponEquipPosition(_weaponType);
                    EditorGUILayout.LabelField("장착 위치", _equipPosition.ToString());
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

            _itemRarity = (ItemRarity)EditorGUILayout.EnumPopup("희귀도", _itemRarity);

            if (EditorGUI.EndChangeCheck())
                RefreshPreviewID();

            EditorGUILayout.EndVertical();
        }

        private void DrawDataSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("아이템 데이터", EditorStyles.boldLabel);

            _itemName = EditorGUILayout.TextField("표시 이름", _itemName);
            _assetName = EditorGUILayout.TextField("파일명", _assetName);
            _itemDescription = EditorGUILayout.TextField("설명", _itemDescription);
            _weight = Mathf.Max(0f, EditorGUILayout.FloatField("무게", _weight));
            _icon = (Sprite)EditorGUILayout.ObjectField("아이콘", _icon, typeof(Sprite), false);

            if (_itemType == ItemType.EQUIPMENT)
                _equipmentPrefab = (GameObject)EditorGUILayout.ObjectField("장비 프리팹", _equipmentPrefab, typeof(GameObject), false);

            if (_itemType == ItemType.CONSUMABLE)
            {
                _consumableEffect = (ConsumableEffectType)EditorGUILayout.EnumPopup("효과 타입", _consumableEffect);
                string amountLabel = _consumableEffect == ConsumableEffectType.HealPercent ? "회복 비율 (0~1)" : "회복량";
                _consumableAmount = Mathf.Max(0f, EditorGUILayout.FloatField(amountLabel, _consumableAmount));
                _requireEffectiveUse = EditorGUILayout.Toggle("효과 없으면 소모 안 함", _requireEffectiveUse);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSection()
        {
            ItemIdRange range = GetCurrentRange();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("발급 미리보기", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("ID 대역", range.ToString());
            EditorGUILayout.LabelField("발급 ID", _previewID > 0 ? _previewID.ToString() : "발급 가능 ID 없음");
            EditorGUILayout.LabelField("저장 경로", range.SavePath);
            EditorGUILayout.LabelField("SO 타입",
                _itemType == ItemType.EQUIPMENT ? nameof(EquipmentSO)
                : _itemType == ItemType.CONSUMABLE ? nameof(ConsumableSO)
                : nameof(ItemSO));
            EditorGUILayout.LabelField("최종 파일명", BuildAssetFileName(range));

            if (_duplicateIDs.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"현재 ItemDatabase 기준 중복 ID가 있습니다: {string.Join(", ", _duplicateIDs.OrderBy(i => i))}\n" +
                    "생성은 가능하지만 DB 조회에서 뒤 항목이 무시될 수 있으니 정리를 권장합니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionSection()
        {
            string validation = GetValidationMessage();
            if (!string.IsNullOrEmpty(validation))
                EditorGUILayout.HelpBox(validation, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(validation)))
            {
                if (GUILayout.Button("아이템 데이터 생성", GUILayout.Height(38)))
                    CreateItemAsset();
            }
        }

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
    }
    #endif
}
