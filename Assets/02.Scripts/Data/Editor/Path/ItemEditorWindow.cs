#if UNITY_EDITOR
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

namespace UPlayGround.Data.Path.Editor
{
    /// <summary>
    /// 아이템 비주얼 에디터 윈도우 (UIToolkit).
    /// 메뉴: UPlayGround / 게임플레이 / 아이템 / 아이템 에디터
    ///
    /// 기능:
    ///   - 좌우 2패널 레이아웃 (목록 / 상세 편집)
    ///   - ItemSO / EquipmentSO 생성 및 편집
    ///   - 타입 필터 탭 + 한글 검색
    ///   - ID 중복 실시간 감지
    ///   - 아이콘 미리보기
    ///   - ItemDatabase 수동 갱신
    ///   - 아이템 복제 / 삭제
    /// </summary>
    public class ItemEditorWindow : EditorWindow
    {
        // ──── 데이터 ────
        private List<ItemSO>  _items        = new List<ItemSO>();
        private List<ItemSO>  _filtered     = new List<ItemSO>();
        private ItemDatabase  _itemDb;
        private HashSet<int>  _duplicateIDs = new HashSet<int>();

        // ──── 선택 & 필터 상태 ────
        private ItemSO    _selected;
        private string    _searchText = "";
        private ItemType? _filterType = null;

        // ──── 생성 팝업 상태 ────
        private string _newItemName     = "NewItem";
        private string _newSavePath     = DEFAULT_ITEM_PATH;
        private bool   _createEquipment = false;

        // ──── UI 요소 ────
        private ListView      _listView;
        private Label         _countLabel;
        private VisualElement _detailPane;
        private VisualElement _createPopup;
        private ToolbarButton _duplicateButton;
        private ToolbarButton _deleteButton;
        private readonly List<ToolbarToggle> _filterToggles = new List<ToolbarToggle>();

        // ──── 상수 ────
        private const float  LIST_PANEL_WIDTH  = 280f;
        private const float  ICON_PREVIEW_SIZE = 80f;
        private const string DEFAULT_ITEM_PATH  = "Assets/10.Datas/Item";
        private const string DEFAULT_EQUIP_PATH = "Assets/10.Datas/Item/Equipment";

        // ──────────────────────────────────────────────────────────

        [MenuItem("UPlayGround/게임플레이/아이템/아이템 에디터")]
        public static void ShowWindow()
        {
            var win = GetWindow<ItemEditorWindow>("Item Editor");
            win.minSize = new Vector2(760, 500);
        }

        // ──────────────────────────────────────────────────────────
        #region 데이터 로드

        private void LoadAllItems()
        {
            _items.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ItemSO");
            foreach (var guid in guids)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null)
                    _items.Add(item);
            }
            _items = _items.OrderBy(i => i.itemId).ToList();
            RebuildDuplicateSet();
        }

        private void LoadItemDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
            if (guids.Length > 0)
                _itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void RebuildDuplicateSet()
        {
            _duplicateIDs.Clear();
            var seen = new HashSet<int>();
            foreach (var item in _items)
            {
                if (item == null) continue;
                if (!seen.Add(item.itemId))
                    _duplicateIDs.Add(item.itemId);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region UI 구성

        private void CreateGUI()
        {
            LoadAllItems();
            LoadItemDatabase();

            var root = rootVisualElement;
            root.Clear();

            root.Add(BuildToolbar());

            var body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            body.Add(BuildListPanel());
            body.Add(BuildDetailPane());
            root.Add(body);

            _createPopup = BuildCreatePopup();
            root.Add(_createPopup);

            RefreshList();
            UpdateSelectionButtons();
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            var newBtn = new ToolbarButton(ToggleCreatePopup) { text = "+ 새 아이템" };
            toolbar.Add(newBtn);

            _duplicateButton = new ToolbarButton(DuplicateSelected) { text = "복제" };
            toolbar.Add(_duplicateButton);

            _deleteButton = new ToolbarButton(DeleteSelected) { text = "삭제" };
            _deleteButton.style.color = new Color(1f, 0.5f, 0.5f);
            toolbar.Add(_deleteButton);

            toolbar.Add(new ToolbarSpacer());

            // 타입 필터 탭
            var filters = new (string label, ItemType? value)[]
            {
                ("전체", null),
                ("장비", ItemType.EQUIPMENT),
                ("소비", ItemType.CONSUMABLE),
                ("기타", ItemType.OTHERS),
            };
            _filterToggles.Clear();
            foreach (var (label, value) in filters)
            {
                var captured = value;
                var toggle = new ToolbarToggle { text = label, value = _filterType == value };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (!evt.newValue)
                    {
                        // 항상 하나는 켜져 있어야 한다
                        toggle.SetValueWithoutNotify(_filterType == captured);
                        return;
                    }
                    _filterType = captured;
                    foreach (var t in _filterToggles)
                        t.SetValueWithoutNotify(t == toggle);
                    RefreshList();
                });
                _filterToggles.Add(toggle);
                toolbar.Add(toggle);
            }

            var flexSpacer = new VisualElement { style = { flexGrow = 1 } };
            toolbar.Add(flexSpacer);

            var search = new ToolbarSearchField { style = { width = 180 } };
            search.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshList();
            });
            toolbar.Add(search);

            toolbar.Add(new ToolbarButton(RefreshDatabase) { text = "DB 갱신" });
            toolbar.Add(new ToolbarButton(() =>
            {
                LoadAllItems();
                RefreshList();
            }) { text = "↺" });

            return toolbar;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 목록 패널 (좌)

        private VisualElement BuildListPanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = LIST_PANEL_WIDTH,
                    flexShrink = 0,
                    borderRightWidth = 1,
                    borderRightColor = new Color(0f, 0f, 0f, 0.35f),
                }
            };

            var header = new Toolbar();
            _countLabel = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft } };
            header.Add(_countLabel);
            panel.Add(header);

            _listView = new ListView
            {
                fixedItemHeight = 48,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                makeItem = MakeListRow,
                bindItem = BindListRow,
            };
            _listView.selectionChanged += _ =>
            {
                _selected = _listView.selectedItem as ItemSO;
                RebuildDetail();
                UpdateSelectionButtons();
            };
            panel.Add(_listView);

            return panel;
        }

        private static VisualElement MakeListRow()
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingRight = 4 }
            };

            var icon = new Image
            {
                name = "icon",
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 40, height = 40, flexShrink = 0, marginRight = 6,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f),
                }
            };
            row.Add(icon);

            var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };

            var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            nameRow.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold } });
            nameRow.Add(new Label("⚠ ID 중복")
            {
                name = "dup",
                style = { color = new Color(1f, 0.4f, 0.4f), fontSize = 10, marginLeft = 4 }
            });
            info.Add(nameRow);

            info.Add(new Label { name = "sub", style = { color = new Color(0.6f, 0.6f, 0.6f), fontSize = 10 } });
            row.Add(info);

            return row;
        }

        private void BindListRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _filtered.Count) return;
            var item = _filtered[index];
            if (item == null) return;

            var icon = row.Q<Image>("icon");
            icon.sprite = item.icon;

            row.Q<Label>("name").text = item.itemName;
            row.Q<Label>("dup").style.display =
                _duplicateIDs.Contains(item.itemId) ? DisplayStyle.Flex : DisplayStyle.None;

            bool isEquip = item is EquipmentSO;
            row.Q<Label>("sub").text =
                $"ID: {item.itemId}  |  {(isEquip ? "장비" : item.itemType.ToString())}  |  {item.itemRarity}";
        }

        private void RefreshList(bool rebuildDetail = true)
        {
            _filtered = GetFilteredItems();

            _listView.itemsSource = _filtered;
            _listView.RefreshItems();
            _countLabel.text = $"아이템 ({_filtered.Count}/{_items.Count})";

            // 기존 선택 유지 (필터 결과에 남아 있으면)
            int idx = _selected != null ? _filtered.IndexOf(_selected) : -1;
            _listView.SetSelectionWithoutNotify(idx >= 0 ? new[] { idx } : System.Array.Empty<int>());
            bool selectionCleared = _selected != null && idx < 0;
            if (selectionCleared)
                _selected = null;
            if (rebuildDetail || selectionCleared)
                RebuildDetail();
            UpdateSelectionButtons();
        }

        private List<ItemSO> GetFilteredItems()
        {
            var result = _items.Where(i => i != null);

            if (_filterType.HasValue)
                result = result.Where(i => i.itemType == _filterType.Value);

            if (!string.IsNullOrEmpty(_searchText))
                result = result.Where(i =>
                    i.itemName.IndexOf(_searchText, System.StringComparison.CurrentCultureIgnoreCase) >= 0
                    || i.itemId.ToString().Contains(_searchText));

            return result.ToList();
        }

        private void SelectItem(ItemSO item)
        {
            _selected = item;
            int idx = _filtered.IndexOf(item);
            _listView.SetSelectionWithoutNotify(idx >= 0 ? new[] { idx } : System.Array.Empty<int>());
            if (idx >= 0) _listView.ScrollToItem(idx);
            RebuildDetail();
            UpdateSelectionButtons();
        }

        private void UpdateSelectionButtons()
        {
            bool has = _selected != null;
            _duplicateButton?.SetEnabled(has);
            _deleteButton?.SetEnabled(has);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 상세 패널 (우)

        private VisualElement BuildDetailPane()
        {
            _detailPane = new VisualElement { style = { flexGrow = 1 } };
            return _detailPane;
        }

        private void RebuildDetail()
        {
            _detailPane.Clear();
            _detailPane.Unbind();

            if (_selected == null)
            {
                _detailPane.Add(MakeCenteredHint("← 아이템을 선택하세요"));
                return;
            }

            var item = _selected;
            var so   = new SerializedObject(item);

            // 헤더
            var header = new Toolbar();
            header.Add(new Label($"  {item.itemName}")
            {
                name = "detail-title",
                style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new Label(AssetDatabase.GetAssetPath(item))
            {
                style = { color = new Color(0.55f, 0.55f, 0.55f), fontSize = 10, unityTextAlign = TextAnchor.MiddleRight }
            });
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(item)) { text = "↗ Project에서 열기" });
            _detailPane.Add(header);

            var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 6, paddingRight = 6, paddingTop = 6 } };
            _detailPane.Add(scroll);

            // ── 아이콘 미리보기 + 기본 정보 ──────────────────────
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var iconPreview = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                sprite = item.icon,
                style =
                {
                    width = ICON_PREVIEW_SIZE, height = ICON_PREVIEW_SIZE, flexShrink = 0, marginRight = 8,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                }
            };
            topRow.Add(iconPreview);

            var basicFields = new VisualElement { style = { flexGrow = 1 } };
            basicFields.Add(new PropertyField { bindingPath = "itemId",     label = "아이템 ID" });
            basicFields.Add(new PropertyField { bindingPath = "itemName",   label = "이름" });
            basicFields.Add(new PropertyField { bindingPath = "itemType",   label = "타입" });
            basicFields.Add(new PropertyField { bindingPath = "itemRarity", label = "희귀도" });
            basicFields.Add(new PropertyField { bindingPath = "icon",       label = "아이콘" });
            topRow.Add(basicFields);
            scroll.Add(topRow);

            // ── ID 중복 경고 ──────────────────────────────────────
            var dupWarning = new HelpBox("", HelpBoxMessageType.Error) { style = { marginTop = 4 } };
            scroll.Add(dupWarning);

            // ── 공통 데이터 ───────────────────────────────────────
            var baseSection = MakeSection("기본 데이터");
            baseSection.Add(new PropertyField { bindingPath = "weight",          label = "무게" });
            baseSection.Add(new PropertyField { bindingPath = "itemDescription", label = "설명" });
            scroll.Add(baseSection);

            // ── 장비 데이터 ───────────────────────────────────────
            if (item is EquipmentSO)
            {
                var equipSection = MakeSection("장비 데이터");
                equipSection.Add(new PropertyField { bindingPath = "equipSlot",       label = "장비 슬롯" });
                equipSection.Add(new PropertyField { bindingPath = "weaponType",      label = "무기 타입" });
                equipSection.Add(new PropertyField { bindingPath = "equipmentPrefab", label = "장비 프리팹" });
                scroll.Add(equipSection);

                var statSection = MakeSection("장비 능력치");
                statSection.Add(new PropertyField { bindingPath = "_statModifiers", label = "능력치 수정자" });
                statSection.Add(new Label("레거시 능력치 (수정자 목록이 비어있을 때만 적용)")
                {
                    style = { fontSize = 10, color = new Color(0.6f, 0.6f, 0.6f), marginTop = 2 }
                });
                statSection.Add(new PropertyField { bindingPath = "attackPower", label = "공격력" });
                statSection.Add(new PropertyField { bindingPath = "critChance",  label = "치명타 확률 (%)" });
                statSection.Add(new PropertyField { bindingPath = "critDamage",  label = "치명타 피해 (%)" });
                statSection.Add(new PropertyField { bindingPath = "attackSpeed", label = "공격 속도" });
                scroll.Add(statSection);
            }

            // ── 소비 데이터 ───────────────────────────────────────
            if (item is ConsumableSO)
            {
                var consumeSection = MakeSection("소비 데이터");
                consumeSection.Add(new PropertyField { bindingPath = "effectType", label = "효과 타입" });
                var amountField = new PropertyField { bindingPath = "amount", label = "회복 수치" };
                consumeSection.Add(amountField);
                consumeSection.Add(new PropertyField { bindingPath = "requireEffectiveUse", label = "효과 없으면 소모 안 함" });
                scroll.Add(consumeSection);

                // 효과 타입에 따라 amount 라벨 변경
                var effectProp = so.FindProperty("effectType");
                void UpdateAmountLabel(SerializedProperty p) =>
                    amountField.label = p.enumValueIndex == (int)ConsumableEffectType.HealPercent
                        ? "회복 비율 (0~1)" : "회복 수치";
                UpdateAmountLabel(effectProp);
                consumeSection.TrackPropertyValue(effectProp, UpdateAmountLabel);
            }

            // 값 변경 추적: 중복 재계산 + 목록/헤더/아이콘 갱신
            void RefreshDupWarning()
            {
                bool dup = _duplicateIDs.Contains(item.itemId);
                dupWarning.text = $"ID {item.itemId}가 다른 아이템과 중복됩니다.";
                dupWarning.style.display = dup ? DisplayStyle.Flex : DisplayStyle.None;
            }
            RefreshDupWarning();

            _detailPane.TrackSerializedObjectValue(so, _ =>
            {
                RebuildDuplicateSet();
                RefreshDupWarning();
                iconPreview.sprite = item.icon;
                _detailPane.Q<Label>("detail-title").text = $"  {item.itemName}";
                RefreshList(false);
            });

            _detailPane.Bind(so);
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginTop = 6, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
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

        private static VisualElement MakeCenteredHint(string text)
        {
            var hint = new VisualElement
            {
                style = { flexGrow = 1, justifyContent = Justify.Center, alignItems = Align.Center }
            };
            hint.Add(new Label(text) { style = { color = new Color(0.55f, 0.55f, 0.55f) } });
            return hint;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 아이템 생성 팝업

        private void ToggleCreatePopup()
        {
            _createPopup.style.display = _createPopup.style.display == DisplayStyle.None
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement BuildCreatePopup()
        {
            var popup = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 4, top = 22, width = 340,
                    display = DisplayStyle.None,
                    backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.8f, 0.8f, 0.8f),
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = Color.black, borderRightColor = Color.black,
                    borderTopColor = Color.black, borderBottomColor = Color.black,
                    paddingBottom = 8,
                }
            };

            var header = new Toolbar();
            header.Add(new Label("새 아이템 생성")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new ToolbarButton(() => popup.style.display = DisplayStyle.None) { text = "✕" });
            popup.Add(header);

            // 타입 선택
            var typeRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 4, marginLeft = 8, marginRight = 8, alignItems = Align.Center }
            };
            typeRow.Add(new Label("타입") { style = { width = 60 } });

            var pathField = new TextField { value = _newSavePath, style = { flexGrow = 1 } };

            Button itemBtn = null, equipBtn = null;
            void UpdateTypeButtons()
            {
                var on  = new Color(0.24f, 0.48f, 0.9f, 0.55f);
                var off = new Color(0f, 0f, 0f, 0f);
                itemBtn.style.backgroundColor  = _createEquipment ? off : on;
                equipBtn.style.backgroundColor = _createEquipment ? on : off;
            }
            itemBtn = new Button(() =>
            {
                _createEquipment = false;
                _newSavePath = DEFAULT_ITEM_PATH;
                pathField.SetValueWithoutNotify(_newSavePath);
                UpdateTypeButtons();
            }) { text = "ItemSO", style = { flexGrow = 1 } };
            equipBtn = new Button(() =>
            {
                _createEquipment = true;
                _newSavePath = DEFAULT_EQUIP_PATH;
                pathField.SetValueWithoutNotify(_newSavePath);
                UpdateTypeButtons();
            }) { text = "EquipmentSO", style = { flexGrow = 1 } };
            typeRow.Add(itemBtn);
            typeRow.Add(equipBtn);
            popup.Add(typeRow);
            UpdateTypeButtons();

            // 파일명
            var nameRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 2, marginLeft = 8, marginRight = 8, alignItems = Align.Center }
            };
            nameRow.Add(new Label("파일명") { style = { width = 60 } });
            var nameField = new TextField { value = _newItemName, style = { flexGrow = 1 } };
            nameField.RegisterValueChangedCallback(evt => _newItemName = evt.newValue);
            nameRow.Add(nameField);
            popup.Add(nameRow);

            // 저장 경로
            var pathRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 2, marginLeft = 8, marginRight = 8, alignItems = Align.Center }
            };
            pathRow.Add(new Label("저장 경로") { style = { width = 60 } });
            pathField.RegisterValueChangedCallback(evt => _newSavePath = evt.newValue);
            pathRow.Add(pathField);
            pathRow.Add(new Button(() =>
            {
                string selected = EditorUtility.OpenFolderPanel("저장 폴더 선택", _newSavePath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 절대경로 → 프로젝트 상대경로로 변환
                    string projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
                    if (selected.StartsWith(projectPath))
                    {
                        _newSavePath = "Assets" + selected.Substring(projectPath.Length).Replace('\\', '/');
                        pathField.SetValueWithoutNotify(_newSavePath);
                    }
                }
            }) { text = "..." });
            popup.Add(pathRow);

            // 생성 버튼
            var createBtn = new Button(() =>
            {
                if (string.IsNullOrWhiteSpace(_newItemName) || string.IsNullOrWhiteSpace(_newSavePath))
                    return;
                popup.style.display = DisplayStyle.None;
                CreateNewItem();
            }) { text = "생성", style = { height = 24, marginTop = 6, marginLeft = 8, marginRight = 8 } };
            popup.Add(createBtn);

            return popup;
        }

        private void CreateNewItem()
        {
            // 경로 확보
            if (!AssetDatabase.IsValidFolder(_newSavePath))
                Directory.CreateDirectory(_newSavePath);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{_newSavePath}/{_newItemName}.asset");

            ItemSO newItem = _createEquipment
                ? ScriptableObject.CreateInstance<EquipmentSO>()
                : ScriptableObject.CreateInstance<ItemSO>();

            newItem.itemName = _newItemName;
            // 사용 중인 최대 ID + 1을 기본값으로
            newItem.itemId = _items.Count > 0 ? _items.Max(i => i.itemId) + 1 : 1;

            AssetDatabase.CreateAsset(newItem, assetPath);
            AssetDatabase.SaveAssets();

            LoadAllItems();
            RefreshList();
            SelectItem(newItem);

            EditorGUIUtility.PingObject(newItem);
            Debug.Log($"[ItemEditor] 생성 완료: {assetPath}");
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 복제 / 삭제

        private void DuplicateSelected()
        {
            if (_selected == null) return;

            string srcPath = AssetDatabase.GetAssetPath(_selected);
            string dir     = System.IO.Path.GetDirectoryName(srcPath);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(dir, System.IO.Path.GetFileName(srcPath)).Replace('\\', '/'));

            AssetDatabase.CopyAsset(srcPath, newPath);
            AssetDatabase.SaveAssets();

            // ID 자동 증가
            var copy = AssetDatabase.LoadAssetAtPath<ItemSO>(newPath);
            if (copy != null)
            {
                copy.itemId = _items.Count > 0 ? _items.Max(i => i.itemId) + 1 : 1;
                EditorUtility.SetDirty(copy);
                AssetDatabase.SaveAssets();
            }

            LoadAllItems();
            RefreshList();
            SelectItem(copy);

            EditorGUIUtility.PingObject(copy);
        }

        private void DeleteSelected()
        {
            if (_selected == null) return;

            var target = _selected;
            string path = AssetDatabase.GetAssetPath(target);

            if (!EditorUtility.DisplayDialog("아이템 삭제",
                $"'{target.itemName}' (ID: {target.itemId})을 삭제합니다.\n이 작업은 되돌릴 수 없습니다.",
                "삭제", "취소"))
                return;

            _selected = null;

            AssetDatabase.DeleteAsset(path);
            LoadAllItems();
            RefreshList();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region ItemDatabase 갱신

        private void RefreshDatabase()
        {
            if (_itemDb == null)
            {
                EditorUtility.DisplayDialog("ItemDatabase 없음",
                    "프로젝트에서 ItemDatabase를 찾을 수 없습니다.\nItemDatabase asset을 먼저 생성하세요.",
                    "확인");
                return;
            }

            _itemDb.RefreshDatabase(DEFAULT_ITEM_PATH);
            LoadAllItems();
            RefreshList();
            Debug.Log($"[ItemEditor] ItemDatabase 갱신 완료 — {_itemDb.AllItems.Count}개");
        }

        #endregion
    }
}
#endif
