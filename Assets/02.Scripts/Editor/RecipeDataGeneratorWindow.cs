#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Tool.Editor;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Crafting.Editor
{
    /// <summary>
    /// ItemDatabase의 ItemSO를 기준으로 RecipeDatabase에 제작 데이터를 발급하는 에디터 윈도우 (UIToolkit).
    /// 메뉴: UPlayGround / Crafting / Recipe Data Generator
    /// </summary>
    public class RecipeDataGeneratorWindow : EditorWindow
    {
        private class IngredientDraft
        {
            public int itemID;
            public int quantity = 1;
        }

        private RecipeDatabase _recipeDb;
        private ItemDatabase _itemDb;
        private readonly Dictionary<int, ItemSO> _itemCache = new();
        private readonly List<ItemSO> _items = new();
        private readonly List<IngredientDraft> _ingredients = new();

        private ItemSO _selectedResultItem;
        private List<ItemSO> _filteredItems = new();
        private string _searchText = "";
        private ItemType? _filterType;

        private int _recipeID;
        private string _recipeName = "";
        private string _description = "";
        private int _resultQuantity = 1;
        private CostType _costType = CostType.Free;
        private int _costAmount;
        private float _castTimeSeconds = 2f;
        private CraftingCategory _category;
        private bool _isDebugUnlocked = true;
        private bool _overwriteExisting = true;
        private bool _generateRecipeEnum = true;

        private bool _useUnlockCondition;
        private UnlockConditionType _unlockConditionType = UnlockConditionType.None;
        private int _unlockConditionValue;
        private int _unlockConditionValue2 = 1;
        private string _unlockConditionStringValue = string.Empty;

        // ──── UI 요소 ────
        private VisualElement _body;
        private VisualElement _missingDbPane;
        private Label _recipeDbLabel;
        private Label _itemDbLabel;
        private ListView _listView;
        private VisualElement _generatorPane;
        private VisualElement _itemPickerPopup;
        private HelpBox _validationBox;
        private Button _saveButton;
        private readonly List<ToolbarToggle> _typeToggles = new();

        private const float LIST_WIDTH = 310f;
        private const string RECIPE_ENUM_OUTPUT_PATH = "Assets/02.Scripts/Data/Crafting/RecipeIdType.cs";

        public static void Open()
        {
            var win = GetWindow<RecipeDataGeneratorWindow>("Recipe Data Generator");
            win.minSize = new Vector2(860f, 560f);
            win.Show();
        }

        // ──────────────────────────────────────────────────────────
        #region UI 구성

        private void CreateGUI()
        {
            LoadDatabases();

            var root = rootVisualElement;
            root.Clear();

            root.Add(BuildToolbar());

            _missingDbPane = new VisualElement
            {
                style = { flexGrow = 1, justifyContent = Justify.Center, paddingLeft = 20, paddingRight = 20 }
            };
            _missingDbPane.Add(new HelpBox(
                "RecipeDatabase 또는 ItemDatabase를 찾을 수 없습니다.\n" +
                "RecipeDatabase는 Create > UPlayGround > PathDatabase > Recipe로 생성하고, ItemDatabase는 기존 아이템 DB를 준비하세요.",
                HelpBoxMessageType.Warning));
            root.Add(_missingDbPane);

            _body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            _body.Add(BuildItemListPanel());
            _generatorPane = new VisualElement { style = { flexGrow = 1 } };
            _body.Add(_generatorPane);
            root.Add(_body);

            RefreshAll();
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new ToolbarButton(() =>
            {
                LoadDatabases();
                RefreshAll();
            }) { text = "새로고침" });

            _recipeDbLabel = new Label { style = { fontSize = 10, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };
            toolbar.Add(_recipeDbLabel);
            _itemDbLabel = new Label { style = { fontSize = 10, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };
            toolbar.Add(_itemDbLabel);

            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            var overwriteToggle = new ToolbarToggle { text = "기존 결과물 레시피 갱신", value = _overwriteExisting };
            overwriteToggle.RegisterValueChangedCallback(evt =>
            {
                _overwriteExisting = evt.newValue;
                UpdateValidation();
            });
            toolbar.Add(overwriteToggle);

            var enumToggle = new ToolbarToggle { text = "RecipeIdType 생성", value = _generateRecipeEnum };
            enumToggle.RegisterValueChangedCallback(evt => _generateRecipeEnum = evt.newValue);
            toolbar.Add(enumToggle);

            return toolbar;
        }

        private VisualElement BuildItemListPanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = LIST_WIDTH,
                    flexShrink = 0,
                    borderRightWidth = 1,
                    borderRightColor = new Color(0f, 0f, 0f, 0.35f),
                }
            };

            // 타입 필터 탭
            var tabBar = new Toolbar();
            var filters = new (ItemType? type, string label)[]
            {
                (null, "전체"),
                (ItemType.EQUIPMENT, "장비"),
                (ItemType.CONSUMABLE, "소비"),
                (ItemType.OTHERS, "기타"),
            };
            _typeToggles.Clear();
            foreach (var (type, label) in filters)
            {
                var captured = type;
                var toggle = new ToolbarToggle { text = label, value = _filterType == type, style = { flexGrow = 1 } };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (!evt.newValue)
                    {
                        toggle.SetValueWithoutNotify(_filterType == captured);
                        return;
                    }
                    _filterType = captured;
                    foreach (var t in _typeToggles)
                        t.SetValueWithoutNotify(t == toggle);
                    RefreshItemList();
                });
                _typeToggles.Add(toggle);
                tabBar.Add(toggle);
            }
            panel.Add(tabBar);

            // 검색
            var searchBar = new Toolbar();
            var search = new ToolbarSearchField { style = { flexGrow = 1 } };
            search.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshItemList();
            });
            searchBar.Add(search);
            panel.Add(searchBar);

            _listView = new ListView
            {
                fixedItemHeight = 46,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                makeItem = MakeItemRow,
                bindItem = BindItemRow,
            };
            _listView.selectionChanged += _ =>
            {
                var item = _listView.selectedItem as ItemSO;
                if (item != null)
                    SelectResultItem(item);
            };
            panel.Add(_listView);

            return panel;
        }

        private static VisualElement MakeItemRow()
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingRight = 4 }
            };
            row.Add(new Image
            {
                name = "icon",
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 38, height = 38, flexShrink = 0, marginRight = 6,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f),
                }
            });
            var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
            info.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold } });
            info.Add(new Label { name = "sub", style = { color = new Color(0.65f, 0.65f, 0.65f), fontSize = 10 } });
            row.Add(info);
            row.Add(new Label("R")
            {
                name = "recipe-badge",
                style = { color = new Color(0.45f, 1f, 0.5f), unityFontStyleAndWeight = FontStyle.Bold, width = 14 }
            });
            return row;
        }

        private void BindItemRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _filteredItems.Count) return;
            var item = _filteredItems[index];

            row.Q<Image>("icon").sprite = item.icon;
            row.Q<Label>("name").text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            row.Q<Label>("sub").text = $"ID: {item.itemId} | {item.itemType} | {item.itemRarity}";
            row.Q<Label>("recipe-badge").style.display =
                FindRecipeByResultItem(item.itemId) != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshAll()
        {
            if (_body == null) return;

            bool hasDb = _recipeDb != null && _itemDb != null;
            _recipeDbLabel.text = _recipeDb != null ? $"RecipeDB: {_recipeDb.name}" : "RecipeDB 없음";
            _recipeDbLabel.style.color = _recipeDb == null ? (StyleColor)Color.red : StyleKeyword.Null;
            _itemDbLabel.text = _itemDb != null ? $"ItemDB: {_itemDb.name}" : "ItemDB 없음";
            _itemDbLabel.style.color = _itemDb == null ? (StyleColor)Color.red : StyleKeyword.Null;

            _missingDbPane.style.display = hasDb ? DisplayStyle.None : DisplayStyle.Flex;
            _body.style.display = hasDb ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasDb)
            {
                RefreshItemList();
                RebuildGeneratorPane();
            }
        }

        private void RefreshItemList()
        {
            _filteredItems = GetFilteredItems().ToList();
            _listView.itemsSource = _filteredItems;
            _listView.RefreshItems();

            int idx = _selectedResultItem != null ? _filteredItems.IndexOf(_selectedResultItem) : -1;
            _listView.SetSelectionWithoutNotify(idx >= 0 ? new[] { idx } : System.Array.Empty<int>());
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 데이터 로드

        private void LoadDatabases()
        {
            _recipeDb = FindDatabase<RecipeDatabase>();
            _itemDb = FindDatabase<ItemDatabase>();

            _itemCache.Clear();
            _items.Clear();

            if (_itemDb != null)
            {
                _itemDb.Initialize();
                foreach (var item in _itemDb.AllItems.Where(i => i != null).OrderBy(i => i.itemId))
                {
                    _items.Add(item);
                    if (!_itemCache.ContainsKey(item.itemId))
                        _itemCache.Add(item.itemId, item);
                }
            }

            if (_selectedResultItem != null && !_items.Contains(_selectedResultItem))
                _selectedResultItem = null;
        }

        private static T FindDatabase<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private IEnumerable<ItemSO> GetFilteredItems()
        {
            IEnumerable<ItemSO> query = _items;

            if (_filterType.HasValue)
                query = query.Where(i => i.itemType == _filterType.Value);

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string lower = _searchText.ToLower();
                query = query.Where(i =>
                    (!string.IsNullOrEmpty(i.itemName) && i.itemName.ToLower().Contains(lower)) ||
                    i.itemId.ToString().Contains(lower));
            }

            return query;
        }

        private void SelectResultItem(ItemSO item)
        {
            _selectedResultItem = item;
            ApplyDefaultsFromItem(item);
            RebuildGeneratorPane();
        }

        private void ApplyDefaultsFromItem(ItemSO item)
        {
            var existing = FindRecipeByResultItem(item.itemId);

            if (existing != null)
            {
                _recipeID = existing.recipeID;
                _recipeName = existing.recipeName;
                _description = existing.description;
                _resultQuantity = Mathf.Max(1, existing.resultQuantity);
                _costType = existing.costType;
                _costAmount = existing.costAmount;
                _castTimeSeconds = Mathf.Max(0f, existing.castTimeSeconds);
                _category = existing.category;
                _isDebugUnlocked = existing.isDebugUnlocked;

                _ingredients.Clear();
                foreach (var ingredient in _recipeDb.AllIngredients.Where(i => i.recipeID == existing.recipeID))
                {
                    _ingredients.Add(new IngredientDraft
                    {
                        itemID = ingredient.ingredientItemID,
                        quantity = Mathf.Max(1, ingredient.requiredQuantity)
                    });
                }

                var cond = _recipeDb.AllUnlockConditions.FirstOrDefault(u => u.recipeID == existing.recipeID);
                _useUnlockCondition = cond != null;
                if (cond != null)
                {
                    _unlockConditionType = cond.conditionType;
                    _unlockConditionValue = cond.conditionValue;
                    _unlockConditionValue2 = Mathf.Max(1, cond.conditionValue2);
                    _unlockConditionStringValue = cond.conditionStringValue;
                }
                return;
            }

            _recipeID = GetNextRecipeID();
            _recipeName = $"{item.itemName} 제작";
            _description = $"{item.itemName} 제작 레시피";
            _resultQuantity = 1;
            _category = GetCategoryFromItem(item);
            _costType = item.itemType == ItemType.EQUIPMENT ? CostType.Gold : CostType.Free;
            _costAmount = GetDefaultCost(item);
            _castTimeSeconds = GetDefaultCastTime(item);
            _isDebugUnlocked = true;
            _useUnlockCondition = false;
            _unlockConditionType = UnlockConditionType.None;
            _unlockConditionValue = 0;
            _unlockConditionValue2 = 1;
            _unlockConditionStringValue = string.Empty;

            FillSuggestedIngredients();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 생성 패널 (우)

        private void RebuildGeneratorPane()
        {
            CloseItemPicker();
            _generatorPane.Clear();

            if (_selectedResultItem == null)
            {
                var hint = new VisualElement
                {
                    style = { flexGrow = 1, justifyContent = Justify.Center, alignItems = Align.Center }
                };
                hint.Add(new Label("좌측에서 제작 결과 아이템을 선택하세요.")
                {
                    style = { color = new Color(0.55f, 0.55f, 0.55f) }
                });
                _generatorPane.Add(hint);
                return;
            }

            var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 8, paddingRight = 8, paddingTop = 6 } };
            _generatorPane.Add(scroll);

            scroll.Add(BuildResultHeader());

            scroll.Add(BuildRecipeFieldsSection());

            var ingrSection = MakeSection("필요 재료");
            BuildIngredientRows(ingrSection);
            scroll.Add(ingrSection);

            var unlockSection = MakeSection("언락 조건");
            BuildUnlockFields(unlockSection);
            scroll.Add(unlockSection);

            // 액션
            var actionArea = new VisualElement { style = { marginTop = 12, marginBottom = 12 } };
            _validationBox = new HelpBox("", HelpBoxMessageType.Warning);
            actionArea.Add(_validationBox);

            _saveButton = new Button(() =>
            {
                SaveRecipe();
                RefreshItemList();
                RebuildGeneratorPane();
            }) { text = "선택 아이템 제작 데이터 생성/갱신", style = { height = 36 } };
            actionArea.Add(_saveButton);

            actionArea.Add(new Button(() =>
            {
                GenerateMissingRecipesForCurrentFilter();
                RefreshItemList();
                RebuildGeneratorPane();
            }) { text = "현재 설정으로 같은 타입 누락 아이템 일괄 생성", style = { height = 26, marginTop = 4 } });

            _generatorPane.Add(actionArea);

            UpdateValidation();
        }

        private VisualElement BuildResultHeader()
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center,
                    paddingLeft = 6, paddingRight = 6, paddingTop = 6, paddingBottom = 6,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f),
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                }
            };

            header.Add(new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                sprite = _selectedResultItem.icon,
                style =
                {
                    width = 56, height = 56, flexShrink = 0, marginRight = 8,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.6f),
                }
            });

            var info = new VisualElement { style = { justifyContent = Justify.Center } };
            info.Add(new Label(_selectedResultItem.itemName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            info.Add(new Label($"ItemID: {_selectedResultItem.itemId} | {_selectedResultItem.itemType} | {_selectedResultItem.itemRarity}")
            {
                style = { fontSize = 10 }
            });

            var existing = FindRecipeByResultItem(_selectedResultItem.itemId);
            if (existing != null)
            {
                info.Add(new Label($"기존 레시피 있음: #{existing.recipeID} {existing.recipeName}")
                {
                    style = { color = new Color(0.4f, 1f, 0.5f), fontSize = 10 }
                });
            }
            header.Add(info);

            return header;
        }

        private VisualElement BuildRecipeFieldsSection()
        {
            var section = MakeSection("레시피 기본 데이터");

            var idField = new IntegerField("레시피 ID") { value = _recipeID };
            idField.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(1, evt.newValue);
                idField.SetValueWithoutNotify(v);
                _recipeID = v;
                UpdateValidation();
            });
            section.Add(idField);

            var nameField = new TextField("레시피 이름") { value = _recipeName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                _recipeName = evt.newValue;
                UpdateValidation();
            });
            section.Add(nameField);

            var descField = new TextField("설명") { value = _description };
            descField.RegisterValueChangedCallback(evt => _description = evt.newValue);
            section.Add(descField);

            var qtyField = new IntegerField("결과 수량") { value = _resultQuantity };
            qtyField.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(1, evt.newValue);
                qtyField.SetValueWithoutNotify(v);
                _resultQuantity = v;
            });
            section.Add(qtyField);

            var catField = new EnumField("카테고리", _category);
            catField.RegisterValueChangedCallback(evt => _category = (CraftingCategory)evt.newValue);
            section.Add(catField);

            var goldField = new IntegerField("골드 비용") { value = _costAmount };
            var costTypeField = new EnumField("비용 유형", _costType);
            costTypeField.RegisterValueChangedCallback(evt =>
            {
                _costType = (CostType)evt.newValue;
                if (_costType != CostType.Gold)
                {
                    _costAmount = 0;
                    goldField.SetValueWithoutNotify(0);
                }
                goldField.style.display = _costType == CostType.Gold ? DisplayStyle.Flex : DisplayStyle.None;
            });
            section.Add(costTypeField);

            goldField.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(0, evt.newValue);
                goldField.SetValueWithoutNotify(v);
                _costAmount = v;
            });
            goldField.style.display = _costType == CostType.Gold ? DisplayStyle.Flex : DisplayStyle.None;
            section.Add(goldField);

            var castField = new FloatField("제작 시간") { value = _castTimeSeconds };
            castField.RegisterValueChangedCallback(evt =>
            {
                float v = Mathf.Max(0f, evt.newValue);
                castField.SetValueWithoutNotify(v);
                _castTimeSeconds = v;
            });
            section.Add(castField);

            var debugToggle = new Toggle("디버그 언락") { value = _isDebugUnlocked };
            debugToggle.RegisterValueChangedCallback(evt => _isDebugUnlocked = evt.newValue);
            section.Add(debugToggle);

            return section;
        }

        private void BuildIngredientRows(VisualElement section)
        {
            // 타이틀(첫 요소)만 남기고 재구축
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            buttonRow.Add(new Button(() =>
            {
                FillSuggestedIngredients();
                BuildIngredientRows(section);
                UpdateValidation();
            }) { text = "추천 재료" });
            buttonRow.Add(new Button(() =>
            {
                _ingredients.Add(new IngredientDraft { quantity = 1 });
                BuildIngredientRows(section);
                UpdateValidation();
            }) { text = "+ 추가" });
            section.Add(buttonRow);

            if (_ingredients.Count == 0)
                section.Add(new HelpBox("재료가 없으면 비용만으로 제작 가능한 레시피가 됩니다.", HelpBoxMessageType.Info));

            for (int i = 0; i < _ingredients.Count; i++)
            {
                var ingredient = _ingredients[i];

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2,
                        paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2,
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f),
                    }
                };
                row.Add(new Label($"{i + 1}") { style = { width = 18 } });

                var hint = new Label { style = { fontSize = 10, marginLeft = 24 } };

                var idField = new IntegerField("아이템 ID") { value = ingredient.itemID, style = { flexGrow = 1 } };
                idField.RegisterValueChangedCallback(evt =>
                {
                    ingredient.itemID = evt.newValue;
                    UpdateItemHint(hint, evt.newValue);
                    UpdateValidation();
                });
                row.Add(idField);

                row.Add(new Button(() =>
                {
                    OpenItemPicker(id =>
                    {
                        ingredient.itemID = id;
                        idField.SetValueWithoutNotify(id);
                        UpdateItemHint(hint, id);
                        UpdateValidation();
                    });
                }) { text = "선택", style = { width = 48 } });

                var qtyField = new IntegerField("수량") { value = ingredient.quantity, style = { width = 120 } };
                qtyField.RegisterValueChangedCallback(evt =>
                {
                    int v = Mathf.Max(1, evt.newValue);
                    qtyField.SetValueWithoutNotify(v);
                    ingredient.quantity = v;
                });
                row.Add(qtyField);

                row.Add(new Button(() =>
                {
                    _ingredients.Remove(ingredient);
                    BuildIngredientRows(section);
                    UpdateValidation();
                }) { text = "✕", style = { width = 24, color = new Color(1f, 0.55f, 0.55f) } });

                section.Add(row);
                section.Add(hint);
                UpdateItemHint(hint, ingredient.itemID);
            }
        }

        private void BuildUnlockFields(VisualElement section)
        {
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);

            var useToggle = new Toggle("언락 조건 사용") { value = _useUnlockCondition };
            useToggle.RegisterValueChangedCallback(evt =>
            {
                _useUnlockCondition = evt.newValue;
                BuildUnlockFields(section);
            });
            section.Add(useToggle);

            if (!_useUnlockCondition)
                return;

            var typeField = new EnumField("조건 유형", _unlockConditionType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                _unlockConditionType = (UnlockConditionType)evt.newValue;
                if (_unlockConditionType != UnlockConditionType.ItemCollect &&
                    _unlockConditionType != UnlockConditionType.ItemHave &&
                    _unlockConditionType != UnlockConditionType.RecipeCraft &&
                    _unlockConditionType != UnlockConditionType.MonsterKill)
                {
                    _unlockConditionValue = 0;
                    _unlockConditionValue2 = 1;
                }
                BuildUnlockFields(section);
            });
            section.Add(typeField);

            switch (_unlockConditionType)
            {
                case UnlockConditionType.ItemCollect:
                case UnlockConditionType.ItemHave:
                {
                    var hint = new Label { style = { fontSize = 10, marginLeft = 24 } };
                    var idRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var idField = new IntegerField("조건 아이템 ID") { value = _unlockConditionValue, style = { flexGrow = 1 } };
                    idField.RegisterValueChangedCallback(evt =>
                    {
                        _unlockConditionValue = evt.newValue;
                        UpdateItemHint(hint, evt.newValue);
                    });
                    idRow.Add(idField);
                    idRow.Add(new Button(() =>
                    {
                        OpenItemPicker(id =>
                        {
                            _unlockConditionValue = id;
                            idField.SetValueWithoutNotify(id);
                            UpdateItemHint(hint, id);
                        });
                    }) { text = "선택", style = { width = 48 } });
                    section.Add(idRow);
                    section.Add(hint);
                    UpdateItemHint(hint, _unlockConditionValue);

                    section.Add(MakeMinClampedIntField("필요 수량", _unlockConditionValue2, 1, v => _unlockConditionValue2 = v));
                    break;
                }
                case UnlockConditionType.RecipeCraft:
                    section.Add(MakeMinClampedIntField("조건 레시피 ID", _unlockConditionValue, 1, v => _unlockConditionValue = v));
                    section.Add(MakeMinClampedIntField("제작 횟수", _unlockConditionValue2, 1, v => _unlockConditionValue2 = v));
                    break;
                case UnlockConditionType.MonsterKill:
                {
                    var actorField = new TextField("Actor ID") { value = _unlockConditionStringValue };
                    actorField.RegisterValueChangedCallback(evt => _unlockConditionStringValue = evt.newValue);
                    section.Add(actorField);

                    var legacyField = new IntegerField("레거시 숫자 ID") { value = _unlockConditionValue };
                    legacyField.RegisterValueChangedCallback(evt => _unlockConditionValue = evt.newValue);
                    section.Add(legacyField);

                    section.Add(MakeMinClampedIntField("처치 횟수", _unlockConditionValue2, 1, v => _unlockConditionValue2 = v));
                    break;
                }
            }
        }

        private static IntegerField MakeMinClampedIntField(string label, int value, int min, System.Action<int> setter)
        {
            var field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(min, evt.newValue);
                field.SetValueWithoutNotify(v);
                setter(v);
            });
            return field;
        }

        private void UpdateValidation()
        {
            if (_validationBox == null || _saveButton == null) return;

            string validation = GetValidationMessage();
            _validationBox.text = validation;
            _validationBox.style.display = string.IsNullOrEmpty(validation) ? DisplayStyle.None : DisplayStyle.Flex;
            _saveButton.SetEnabled(string.IsNullOrEmpty(validation));
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

        private void UpdateItemHint(Label hint, int itemID)
        {
            if (itemID <= 0)
            {
                hint.style.display = DisplayStyle.None;
                return;
            }

            hint.style.display = DisplayStyle.Flex;
            if (_itemCache.TryGetValue(itemID, out var item))
            {
                hint.text = $"→ {item.itemName} [{item.itemType}]";
                hint.style.color = new Color(0.45f, 1f, 0.5f);
            }
            else
            {
                hint.text = $"등록된 아이템 없음: {itemID}";
                hint.style.color = new Color(1f, 0.45f, 0.45f);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 저장 / 일괄 생성

        private string GetValidationMessage()
        {
            if (_selectedResultItem == null)
                return "결과 아이템이 선택되지 않았습니다.";
            if (string.IsNullOrWhiteSpace(_recipeName))
                return "레시피 이름이 비어 있습니다.";

            var sameIdRecipe = _recipeDb.AllRecipes.FirstOrDefault(r => r.recipeID == _recipeID);
            var existingByResult = FindRecipeByResultItem(_selectedResultItem.itemId);
            if (sameIdRecipe != null && (existingByResult == null || sameIdRecipe.recipeID != existingByResult.recipeID))
                return $"레시피 ID {_recipeID}가 이미 사용 중입니다.";

            if (existingByResult != null && !_overwriteExisting)
                return $"결과 아이템 {_selectedResultItem.itemName}의 기존 레시피가 있습니다. 갱신 옵션을 켜거나 다른 아이템을 선택하세요.";

            foreach (var ingredient in _ingredients)
            {
                if (ingredient.itemID <= 0)
                    return "재료 아이템 ID가 비어 있습니다.";
                if (!_itemCache.ContainsKey(ingredient.itemID))
                    return $"재료 아이템 ID {ingredient.itemID}를 ItemDatabase에서 찾을 수 없습니다.";
            }

            return "";
        }

        private void SaveRecipe()
        {
            if (_ingredients.Count == 0 && !EditorUtility.DisplayDialog(
                    "재료 없는 레시피",
                    "필요 재료가 없는 제작 데이터를 생성합니다. 계속할까요?",
                    "생성", "취소"))
                return;

            var recipes = _recipeDb.AllRecipes.Select(CloneRecipe).ToList();
            var ingredients = _recipeDb.AllIngredients.Select(CloneIngredient).ToList();
            var unlocks = _recipeDb.AllUnlockConditions.Select(CloneUnlock).ToList();

            var existing = recipes.FirstOrDefault(r => r.resultItemID == _selectedResultItem.itemId);
            int targetRecipeID = existing != null && _overwriteExisting ? existing.recipeID : _recipeID;

            if (existing != null && _overwriteExisting)
                recipes.Remove(existing);

            ingredients.RemoveAll(i => i.recipeID == targetRecipeID);
            unlocks.RemoveAll(u => u.recipeID == targetRecipeID);

            recipes.Add(new RecipeData
            {
                recipeID = targetRecipeID,
                recipeName = _recipeName,
                description = _description,
                resultItemID = _selectedResultItem.itemId,
                resultQuantity = _resultQuantity,
                costType = _costType,
                costAmount = _costAmount,
                castTimeSeconds = _castTimeSeconds,
                category = _category,
                isDebugUnlocked = _isDebugUnlocked,
            });

            foreach (var ingredient in _ingredients)
            {
                ingredients.Add(new IngredientData
                {
                    recipeID = targetRecipeID,
                    ingredientItemID = ingredient.itemID,
                    requiredQuantity = ingredient.quantity,
                });
            }

            if (_useUnlockCondition)
            {
                unlocks.Add(new RecipeUnlockCondition
                {
                    recipeID = targetRecipeID,
                    conditionType = _unlockConditionType,
                    conditionValue = _unlockConditionValue,
                    conditionValue2 = _unlockConditionValue2,
                    conditionStringValue = _unlockConditionType == UnlockConditionType.MonsterKill
                        ? _unlockConditionStringValue
                        : string.Empty,
                });
            }

            SaveDatabase(recipes, ingredients, unlocks);
            _recipeID = targetRecipeID;

            EditorUtility.DisplayDialog("완료", $"제작 데이터 저장 완료\n레시피 ID: {targetRecipeID}", "확인");
        }

        private void GenerateMissingRecipesForCurrentFilter()
        {
            var targets = GetFilteredItems()
                .Where(item => FindRecipeByResultItem(item.itemId) == null)
                .ToList();

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("생성 대상 없음", "현재 필터에서 레시피가 없는 아이템이 없습니다.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "일괄 생성",
                    $"현재 필터의 누락 아이템 {targets.Count}개에 제작 데이터를 생성합니다.\n" +
                    "재료는 각 아이템 타입/희귀도 기준 추천 재료로 채웁니다.",
                    "생성", "취소"))
                return;

            var recipes = _recipeDb.AllRecipes.Select(CloneRecipe).ToList();
            var ingredients = _recipeDb.AllIngredients.Select(CloneIngredient).ToList();
            var unlocks = _recipeDb.AllUnlockConditions.Select(CloneUnlock).ToList();
            int nextId = recipes.Count > 0 ? recipes.Max(r => r.recipeID) + 1 : 1;

            foreach (var item in targets)
            {
                var recipe = new RecipeData
                {
                    recipeID = nextId++,
                    recipeName = $"{item.itemName} 제작",
                    description = $"{item.itemName} 제작 레시피",
                    resultItemID = item.itemId,
                    resultQuantity = 1,
                    costType = item.itemType == ItemType.EQUIPMENT ? CostType.Gold : CostType.Free,
                    costAmount = GetDefaultCost(item),
                    castTimeSeconds = GetDefaultCastTime(item),
                    category = GetCategoryFromItem(item),
                    isDebugUnlocked = _isDebugUnlocked,
                };
                recipes.Add(recipe);

                foreach (var draft in BuildSuggestedIngredients(item))
                {
                    ingredients.Add(new IngredientData
                    {
                        recipeID = recipe.recipeID,
                        ingredientItemID = draft.itemID,
                        requiredQuantity = draft.quantity,
                    });
                }
            }

            SaveDatabase(recipes, ingredients, unlocks);
            EditorUtility.DisplayDialog("완료", $"제작 데이터 {targets.Count}개 생성 완료", "확인");
        }

        private void SaveDatabase(List<RecipeData> recipes, List<IngredientData> ingredients, List<RecipeUnlockCondition> unlocks)
        {
            _recipeDb.SetRecipes(recipes.OrderBy(r => r.recipeID).ToList());
            _recipeDb.SetIngredients(ingredients.OrderBy(i => i.recipeID).ThenBy(i => i.ingredientItemID).ToList());
            _recipeDb.SetUnlockConditions(unlocks.OrderBy(u => u.recipeID).ToList());
            EditorUtility.SetDirty(_recipeDb);
            AssetDatabase.SaveAssets();

            if (_generateRecipeEnum)
                GenerateRecipeIdType(_recipeDb.AllRecipes);

            AssetDatabase.Refresh();
        }

        private static RecipeData CloneRecipe(RecipeData src)
        {
            return new RecipeData
            {
                recipeID = src.recipeID,
                recipeName = src.recipeName,
                description = src.description,
                resultItemID = src.resultItemID,
                resultQuantity = src.resultQuantity,
                costType = src.costType,
                costAmount = src.costAmount,
                castTimeSeconds = src.castTimeSeconds,
                category = src.category,
                isDebugUnlocked = src.isDebugUnlocked,
            };
        }

        private static IngredientData CloneIngredient(IngredientData src)
        {
            return new IngredientData
            {
                recipeID = src.recipeID,
                ingredientItemID = src.ingredientItemID,
                requiredQuantity = src.requiredQuantity,
            };
        }

        private static RecipeUnlockCondition CloneUnlock(RecipeUnlockCondition src)
        {
            return new RecipeUnlockCondition
            {
                recipeID = src.recipeID,
                conditionType = src.conditionType,
                conditionValue = src.conditionValue,
                conditionValue2 = src.conditionValue2,
                conditionStringValue = src.conditionStringValue,
            };
        }

        private static void GenerateRecipeIdType(IReadOnlyList<RecipeData> recipes)
        {
            var raw = recipes
                .Where(r => r != null)
                .OrderBy(r => r.recipeID)
                .Select(r =>
                {
                    string name = string.IsNullOrEmpty(r.recipeName) ? $"Recipe_{r.recipeID}" : r.recipeName;
                    return (name, r.recipeID);
                });

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
            IdEnumGeneratorUtility.GenerateIntKeyEnum(
                "RecipeIdType",
                "ToRecipeId",
                "Recipe",
                RECIPE_ENUM_OUTPUT_PATH,
                "UPlayGround.Data.Crafting",
                entries,
                silent: true);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 추천 재료 / 조회 헬퍼

        private void FillSuggestedIngredients()
        {
            _ingredients.Clear();
            if (_selectedResultItem == null)
                return;

            _ingredients.AddRange(BuildSuggestedIngredients(_selectedResultItem));
        }

        private List<IngredientDraft> BuildSuggestedIngredients(ItemSO resultItem)
        {
            var materials = _items
                .Where(i => i != null && i.itemType == ItemType.OTHERS && i.itemId != resultItem.itemId)
                .OrderBy(i => i.itemId)
                .ToList();

            var result = new List<IngredientDraft>();
            if (materials.Count == 0)
                return result;

            int rarityMul = Mathf.Max(1, (int)resultItem.itemRarity);
            if (resultItem.itemType == ItemType.EQUIPMENT)
            {
                ItemSO primary = PickMaterial(materials, "가죽") ?? PickMaterial(materials, "장작") ?? materials[0];
                result.Add(new IngredientDraft { itemID = primary.itemId, quantity = 2 + rarityMul });

                ItemSO secondary = PickMaterial(materials, "수정") ?? PickMaterial(materials, "몬스터") ?? materials.FirstOrDefault(i => i.itemId != primary.itemId);
                if (secondary != null)
                    result.Add(new IngredientDraft { itemID = secondary.itemId, quantity = Mathf.Max(1, rarityMul) });
            }
            else if (resultItem.itemType == ItemType.CONSUMABLE)
            {
                result.Add(new IngredientDraft { itemID = materials[0].itemId, quantity = Mathf.Max(1, rarityMul) });
            }
            else
            {
                ItemSO source = materials.FirstOrDefault(i => i.itemId != resultItem.itemId);
                if (source != null)
                    result.Add(new IngredientDraft { itemID = source.itemId, quantity = 2 });
            }

            return result;
        }

        private static ItemSO PickMaterial(IEnumerable<ItemSO> materials, string keyword)
        {
            return materials.FirstOrDefault(i => !string.IsNullOrEmpty(i.itemName) && i.itemName.Contains(keyword));
        }

        private RecipeData FindRecipeByResultItem(int itemID)
        {
            return _recipeDb == null ? null : _recipeDb.AllRecipes.FirstOrDefault(r => r.resultItemID == itemID);
        }

        private int GetNextRecipeID()
        {
            return _recipeDb != null && _recipeDb.AllRecipes.Count > 0
                ? _recipeDb.AllRecipes.Max(r => r.recipeID) + 1
                : 1;
        }

        private static CraftingCategory GetCategoryFromItem(ItemSO item)
        {
            return item.itemType switch
            {
                ItemType.CONSUMABLE => CraftingCategory.Consumable,
                ItemType.EQUIPMENT => CraftingCategory.Equipment,
                ItemType.OTHERS => CraftingCategory.Material,
                _ => CraftingCategory.Special,
            };
        }

        private static int GetDefaultCost(ItemSO item)
        {
            if (item.itemType != ItemType.EQUIPMENT)
                return 0;

            return Mathf.Max(100, (int)item.itemRarity * 100);
        }

        private static float GetDefaultCastTime(ItemSO item)
        {
            return item.itemType switch
            {
                ItemType.EQUIPMENT => 3f + Mathf.Max(0, (int)item.itemRarity - 1),
                ItemType.CONSUMABLE => 1f,
                ItemType.OTHERS => 1.5f,
                _ => 2f,
            };
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 아이템 피커 팝업

        private void OpenItemPicker(System.Action<int> callback)
        {
            CloseItemPicker();

            var popup = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, right = 8, top = 28, width = 330, height = 420,
                    backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.8f, 0.8f, 0.8f),
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = Color.black, borderRightColor = Color.black,
                    borderTopColor = Color.black, borderBottomColor = Color.black,
                }
            };

            var header = new Toolbar();
            header.Add(new Label("아이템 선택")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new ToolbarButton(CloseItemPicker) { text = "✕" });
            popup.Add(header);

            var search = new ToolbarSearchField { style = { width = Length.Percent(98) } };
            popup.Add(search);

            var pickerItems = new List<ItemSO>(_items);
            var pickerList = new ListView
            {
                fixedItemHeight = 34,
                selectionType = SelectionType.None,
                style = { flexGrow = 1 },
                itemsSource = pickerItems,
                makeItem = () =>
                {
                    var row = new VisualElement
                    {
                        style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingRight = 4 }
                    };
                    var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
                    info.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold } });
                    info.Add(new Label { name = "sub", style = { fontSize = 10 } });
                    row.Add(info);
                    row.Add(new Button { name = "pick", text = "선택", style = { width = 50 } });
                    return row;
                },
            };
            pickerList.bindItem = (row, i) =>
            {
                if (i < 0 || i >= pickerItems.Count) return;
                var item = pickerItems[i];
                row.Q<Label>("name").text = item.itemName;
                row.Q<Label>("sub").text = $"ID: {item.itemId} | {item.itemType}";
                row.Q<Button>("pick").clickable = new Clickable(() =>
                {
                    callback?.Invoke(item.itemId);
                    CloseItemPicker();
                });
            };
            popup.Add(pickerList);

            search.RegisterValueChangedCallback(evt =>
            {
                string s = (evt.newValue ?? "").ToLower();
                pickerItems.Clear();
                pickerItems.AddRange(_items.Where(i =>
                    string.IsNullOrWhiteSpace(s)
                    || i.itemId.ToString().Contains(s)
                    || (!string.IsNullOrEmpty(i.itemName) && i.itemName.ToLower().Contains(s))));
                pickerList.RefreshItems();
            });

            _itemPickerPopup = popup;
            rootVisualElement.Add(popup);
            search.Focus();
        }

        private void CloseItemPicker()
        {
            if (_itemPickerPopup != null)
            {
                _itemPickerPopup.RemoveFromHierarchy();
                _itemPickerPopup = null;
            }
        }

        #endregion
    }
}
#endif
