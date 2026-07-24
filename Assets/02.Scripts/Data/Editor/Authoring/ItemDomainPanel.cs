#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class ItemDomainRegistration
    {
        static ItemDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                ItemDomainPanel.DomainKey,
                "아이템",
                () => new ItemDomainPanel(),
                100);
        }
    }

    /// <summary>
    /// ItemSO 계열 자산의 생성·검색·편집과 ItemDatabase 갱신을 담당합니다.
    /// </summary>
    public sealed class ItemDomainPanel : DataDomainPanel<ItemSO>
    {
        public const string DomainKey = "items";
        private const string DefaultItemPath = "Assets/10.Datas/Item";
        private const string DefaultEquipmentPath = "Assets/10.Datas/Item/Equipment";
        private const string DefaultConsumablePath = "Assets/10.Datas/Item/Consumable";

        private enum NewItemKind
        {
            Item,
            Equipment,
            Consumable
        }

        private ItemDatabase _itemDatabase;
        private VisualElement _createPopup;
        private string _newItemName = "NewItem";
        private string _newSavePath = DefaultItemPath;
        private NewItemKind _newItemKind;

        public override string DomainId => DomainKey;
        public override string DisplayName => "아이템";
        public override Texture2D Icon => EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
        protected override float ListPanelWidth => 320f;
        protected override string CreateButtonLabel => "+ 새 아이템";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(ItemSO asset) => asset != null;
        protected override bool CanDelete(ItemSO asset) => asset != null;

        protected override IEnumerable<ItemSO> LoadAssets()
        {
            LoadItemDatabase();
            return AssetDatabase.FindAssets("t:ItemSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemSO>)
                .Where(item => item != null)
                .OrderBy(item => item.itemId)
                .ThenBy(item => item.itemName, StringComparer.CurrentCulture);
        }

        protected override string KeyOf(ItemSO asset)
        {
            return asset?.itemId.ToString();
        }

        protected override string LabelOf(ItemSO asset)
        {
            if (asset == null)
                return "(Missing Item)";

            string type = asset is EquipmentSO
                ? "장비"
                : asset is ConsumableSO
                    ? "소비"
                    : asset.itemType.ToDisplayString();
            return $"{asset.itemName}  ·  ID {asset.itemId}  ·  {type}";
        }

        protected override Sprite IconOf(ItemSO asset)
        {
            return asset != null ? asset.icon : null;
        }

        protected override IEnumerable<DataDomainFilter<ItemSO>> CreateFilters()
        {
            yield return new DataDomainFilter<ItemSO>("장비", item =>
                item is EquipmentSO || item.itemType == ItemType.EQUIPMENT);
            yield return new DataDomainFilter<ItemSO>("소비", item =>
                item is ConsumableSO || item.itemType == ItemType.CONSUMABLE);
            yield return new DataDomainFilter<ItemSO>("재료", item => item.itemType == ItemType.MATERIAL);
            yield return new DataDomainFilter<ItemSO>("퀘스트", item => item.itemType == ItemType.QUEST);
            yield return new DataDomainFilter<ItemSO>("중요", item => item.itemType == ItemType.IMPORTANT);
            yield return new DataDomainFilter<ItemSO>("기타", item =>
                item.itemType == ItemType.NONE || item.itemType == ItemType.OTHERS);
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var generator = new ToolbarButton(() => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.ItemGenerator,
                "아이템 데이터 생성기"))
            {
                text = "ID 발급 생성기"
            };
            generator.tooltip = "ID 대역 규칙에 따라 아이템 에셋을 발급하는 보조 도구를 엽니다.";
            toolbar.Add(generator);
            toolbar.Add(new ToolbarButton(RefreshDatabase) { text = "DB 갱신" });
        }

        protected override void CreateNew()
        {
            if (_createPopup == null)
            {
                _createPopup = BuildCreatePopup();
                Root.Add(_createPopup);
            }

            _createPopup.style.display = _createPopup.style.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        protected override ItemSO Duplicate(ItemSO asset)
        {
            int nextId = NextItemId();
            ItemSO copy = AssetCrudService.DuplicateAsset(
                asset,
                duplicated => duplicated.itemId = nextId,
                "아이템 복제");
            if (copy != null)
                EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(ItemSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "아이템 삭제",
                    $"'{asset.itemName}' (ID: {asset.itemId})을 삭제합니다.\nUndo로 복구할 수 있습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "아이템 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(ItemSO asset)
        {
            if (HasDuplicateKey(asset))
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Error,
                    $"아이템 ID {asset.itemId}가 중복됩니다.",
                    asset);
            }
        }

        protected override VisualElement BuildDetail(ItemSO item)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(item);

            var header = new Toolbar();
            var title = new Label(item.itemName) { name = "detail-title" };
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            header.Add(flexibleSpace);

            var path = new Label(AssetDatabase.GetAssetPath(item));
            path.style.fontSize = 10f;
            header.Add(path);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(item)) { text = "Project에서 열기" });
            detail.Add(header);

            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.marginTop = 8f;

            var iconPreview = new Image { sprite = item.icon, scaleMode = ScaleMode.ScaleToFit };
            iconPreview.style.width = 80f;
            iconPreview.style.height = 80f;
            iconPreview.style.flexShrink = 0f;
            iconPreview.style.marginRight = 8f;
            iconPreview.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            topRow.Add(iconPreview);

            var basicFields = new VisualElement();
            basicFields.style.flexGrow = 1f;
            AddProperty(basicFields, "itemId", "아이템 ID");
            AddProperty(basicFields, "itemName", "이름");
            AddProperty(basicFields, "itemType", "타입");
            AddProperty(basicFields, "itemRarity", "희귀도");
            AddProperty(basicFields, "icon", "아이콘");
            topRow.Add(basicFields);
            detail.Add(topRow);

            var baseSection = MakeSection("기본 데이터");
            AddProperty(baseSection, "weight", "무게");
            AddProperty(baseSection, "itemDescription", "설명");
            detail.Add(baseSection);

            if (item is EquipmentSO)
                BuildEquipmentSections(detail);
            if (item is ConsumableSO)
                BuildConsumableSection(detail, serializedObject);

            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                NotifyAssetChanged(item);
                iconPreview.sprite = item.icon;
                title.text = item.itemName;
            });
            detail.Bind(serializedObject);
            return detail;
        }

        private static void BuildEquipmentSections(VisualElement detail)
        {
            var equipmentSection = MakeSection("장비 데이터");
            AddProperty(equipmentSection, "equipSlot", "장비 슬롯");
            AddProperty(equipmentSection, "weaponType", "무기 타입");
            AddProperty(equipmentSection, "equipmentPrefab", "장비 프리팹");
            detail.Add(equipmentSection);

            var statSection = MakeSection("장비 능력치");
            AddProperty(statSection, "_statModifiers", "능력치 수정자");
            AddProperty(statSection, "attackPower", "공격력");
            AddProperty(statSection, "critChance", "치명타 확률 (%)");
            AddProperty(statSection, "critDamage", "치명타 피해 (%)");
            AddProperty(statSection, "attackSpeed", "공격 속도");
            detail.Add(statSection);

            var growthSection = MakeSection("랜덤 성장 능력치");
            AddProperty(growthSection, "grantRandomGrowthAttributes", "획득 시 랜덤 부여");
            AddProperty(growthSection, "randomAttributeCountMin", "최소 능력치 개수");
            AddProperty(growthSection, "randomAttributeCountMax", "최대 능력치 개수");
            AddProperty(growthSection, "randomRankMin", "최소 랭크");
            AddProperty(growthSection, "randomRankMax", "최대 랭크");
            AddProperty(growthSection, "randomAttributePool", "능력치 후보");
            detail.Add(growthSection);
        }

        private static void BuildConsumableSection(VisualElement detail, SerializedObject serializedObject)
        {
            var section = MakeSection("소비 데이터");
            AddProperty(section, "effectType", "효과 타입");
            var amountField = new PropertyField { bindingPath = "amount", label = "회복 수치" };
            section.Add(amountField);
            AddProperty(section, "requireEffectiveUse", "효과 없으면 소모 안 함");
            AddProperty(section, "cooldownDuration", "재사용 대기시간 (초)");
            detail.Add(section);

            SerializedProperty effectProperty = serializedObject.FindProperty("effectType");
            void UpdateAmountLabel(SerializedProperty property)
            {
                amountField.label = property.enumValueIndex == (int)ConsumableEffectType.HealPercent
                    ? "회복 비율 (0~1)"
                    : "회복 수치";
            }

            UpdateAmountLabel(effectProperty);
            section.TrackPropertyValue(effectProperty, UpdateAmountLabel);
        }

        private VisualElement BuildCreatePopup()
        {
            var popup = new VisualElement();
            popup.style.position = Position.Absolute;
            popup.style.left = 4f;
            popup.style.top = 22f;
            popup.style.width = 380f;
            popup.style.display = DisplayStyle.None;
            popup.style.paddingLeft = 8f;
            popup.style.paddingRight = 8f;
            popup.style.paddingBottom = 8f;
            popup.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.82f, 0.82f, 0.82f);
            SetBorder(popup);

            var header = new Toolbar();
            var title = new Label("새 아이템 생성");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var headerSpace = new VisualElement();
            headerSpace.style.flexGrow = 1f;
            header.Add(headerSpace);
            header.Add(new ToolbarButton(() => popup.style.display = DisplayStyle.None) { text = "×" });
            popup.Add(header);

            TextField pathField = null;
            var kindField = new DropdownField(
                "자산 타입",
                new List<string> { "일반 ItemSO", "장비 EquipmentSO", "소비 ConsumableSO" },
                0);
            kindField.RegisterValueChangedCallback(evt =>
            {
                _newItemKind = evt.newValue.StartsWith("장비", StringComparison.Ordinal)
                    ? NewItemKind.Equipment
                    : evt.newValue.StartsWith("소비", StringComparison.Ordinal)
                        ? NewItemKind.Consumable
                        : NewItemKind.Item;
                _newSavePath = DefaultPathFor(_newItemKind);
                pathField.SetValueWithoutNotify(_newSavePath);
            });
            popup.Add(kindField);

            var nameField = new TextField("파일명") { value = _newItemName };
            nameField.RegisterValueChangedCallback(evt => _newItemName = evt.newValue);
            popup.Add(nameField);

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathField = new TextField("저장 경로") { value = _newSavePath };
            pathField.style.flexGrow = 1f;
            pathField.RegisterValueChangedCallback(evt => _newSavePath = evt.newValue);
            pathRow.Add(pathField);
            pathRow.Add(new Button(() => SelectSaveFolder(pathField)) { text = "..." });
            popup.Add(pathRow);

            var createButton = new Button(() => CreateItemFromPopup(popup));
            createButton.text = "생성";
            createButton.style.height = 25f;
            createButton.style.marginTop = 6f;
            popup.Add(createButton);
            return popup;
        }

        private void CreateItemFromPopup(VisualElement popup)
        {
            if (string.IsNullOrWhiteSpace(_newItemName) || string.IsNullOrWhiteSpace(_newSavePath))
                return;

            if (!AssetCrudService.IsAssetPathWithin(_newSavePath, DefaultItemPath))
            {
                EditorUtility.DisplayDialog(
                    "잘못된 저장 경로",
                    $"아이템은 ItemDatabase 검색 루트 아래에 저장해야 합니다.\n{DefaultItemPath}",
                    "확인");
                return;
            }

            Type type = _newItemKind switch
            {
                NewItemKind.Equipment => typeof(EquipmentSO),
                NewItemKind.Consumable => typeof(ConsumableSO),
                _ => typeof(ItemSO)
            };
            int nextId = NextItemId();
            ItemSO newItem = (ItemSO)AssetCrudService.CreateAsset(
                type,
                _newSavePath,
                _newItemName,
                asset =>
                {
                    var item = (ItemSO)asset;
                    item.itemId = nextId;
                    item.itemName = _newItemName;
                    item.itemType = _newItemKind switch
                    {
                        NewItemKind.Equipment => ItemType.EQUIPMENT,
                        NewItemKind.Consumable => ItemType.CONSUMABLE,
                        _ => ItemType.NONE
                    };
                },
                "아이템 생성");

            popup.style.display = DisplayStyle.None;
            RefreshAssets(newItem);
            EditorGUIUtility.PingObject(newItem);
            Debug.Log($"[DataAuthoringHub] 아이템 생성 완료: {AssetDatabase.GetAssetPath(newItem)}");
        }

        private void SelectSaveFolder(TextField pathField)
        {
            string selected = EditorUtility.OpenFolderPanel("아이템 저장 폴더 선택", _newSavePath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            if (!AssetCrudService.TryConvertAbsoluteFolderToAssetPath(selected, out string assetPath)
                || !AssetCrudService.IsAssetPathWithin(assetPath, DefaultItemPath))
            {
                EditorUtility.DisplayDialog(
                    "잘못된 저장 경로",
                    $"아이템은 ItemDatabase 검색 루트 아래에 저장해야 합니다.\n{DefaultItemPath}",
                    "확인");
                return;
            }

            _newSavePath = assetPath;
            pathField.SetValueWithoutNotify(_newSavePath);
        }

        private void RefreshDatabase()
        {
            LoadItemDatabase();
            if (_itemDatabase == null)
            {
                EditorUtility.DisplayDialog(
                    "ItemDatabase 없음",
                    "프로젝트에서 ItemDatabase를 찾을 수 없습니다.",
                    "확인");
                return;
            }

            Undo.RecordObject(_itemDatabase, "ItemDatabase 갱신");
            _itemDatabase.RefreshDatabase(DefaultItemPath);
            RefreshAssets(Selected);
            Debug.Log($"[DataAuthoringHub] ItemDatabase 갱신 완료: {_itemDatabase.AllItems.Count}개");
        }

        private void LoadItemDatabase()
        {
            if (_itemDatabase != null)
                return;

            string guid = AssetDatabase.FindAssets("t:ItemDatabase").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private int NextItemId()
        {
            return Assets.Count > 0 ? Assets.Max(item => item.itemId) + 1 : 1;
        }

        private static string DefaultPathFor(NewItemKind kind)
        {
            return kind switch
            {
                NewItemKind.Equipment => DefaultEquipmentPath,
                NewItemKind.Consumable => DefaultConsumablePath,
                _ => DefaultItemPath
            };
        }

        private static void AddProperty(VisualElement parent, string bindingPath, string label)
        {
            parent.Add(new PropertyField { bindingPath = bindingPath, label = label });
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 7f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 6f;
            section.style.paddingBottom = 6f;
            section.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f);
            SetBorder(section);

            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 3f;
            section.Add(heading);
            return section;
        }

        private static void SetBorder(VisualElement element)
        {
            Color color = new Color(0f, 0f, 0f, 0.3f);
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderTopWidth = 1f;
            element.style.borderBottomWidth = 1f;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
        }
    }
}
#endif
