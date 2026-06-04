#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

/// <summary>
/// 아이템 비주얼 에디터 윈도우.
/// 메뉴: UPlayGround / Item / Item Editor
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
    private List<ItemSO>        _items        = new List<ItemSO>();
    private ItemDatabase        _itemDb;
    private HashSet<int>        _duplicateIDs = new HashSet<int>();

    // ──── 선택 & 필터 상태 ────
    private int      _selectedIndex = -1;
    private string   _searchText    = "";
    private ItemType? _filterType   = null;

    // ──── 스크롤 ────
    private Vector2 _listScroll;
    private Vector2 _detailScroll;

    // ──── 편집 대상 ────
    private SerializedObject _serializedItem;

    // ──── 생성 팝업 ────
    private bool   _showCreatePopup  = false;
    private string _newItemName      = "NewItem";
    private string _newSavePath      = "Assets/10.Datas/Item";
    private bool   _createEquipment  = false;

    // ──── 상수 ────
    private const float LIST_PANEL_WIDTH = 280f;
    private const float ICON_PREVIEW_SIZE = 80f;
    private const string DEFAULT_ITEM_PATH      = "Assets/10.Datas/Item";
    private const string DEFAULT_EQUIP_PATH     = "Assets/10.Datas/Item/Equipment";

    // ──── 스타일 캐시 ────
    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;

    // ──────────────────────────────────────────────────────────

    [MenuItem("UPlayGround/게임플레이/아이템/아이템 에디터")]
    public static void ShowWindow()
    {
        var win = GetWindow<ItemEditorWindow>("Item Editor");
        win.minSize = new Vector2(760, 500);
    }

    // ──────────────────────────────────────────────────────────
    #region 초기화

    private void OnEnable()
    {
        LoadAllItems();
        LoadItemDatabase();
    }

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
        _selectedIndex = -1;
        _serializedItem = null;
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

    private void InitStyles()
    {
        if (_titleStyle != null) return;

        _titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };
        _subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region OnGUI

    private void OnGUI()
    {
        InitStyles();

        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawListPanel();
        DrawDetailPanel();
        EditorGUILayout.EndHorizontal();

        if (_showCreatePopup)
            DrawCreatePopup();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 툴바

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 새 아이템
        if (GUILayout.Button("+ 새 아이템", EditorStyles.toolbarButton, GUILayout.Width(80)))
            _showCreatePopup = !_showCreatePopup;

        GUILayout.Space(4);

        // 복제
        GUI.enabled = _selectedIndex >= 0 && _selectedIndex < GetFilteredItems().Count;
        if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(50)))
            DuplicateSelected();
        GUI.enabled = true;

        // 삭제
        GUI.enabled = _selectedIndex >= 0 && _selectedIndex < GetFilteredItems().Count;
        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(50)))
            DeleteSelected();
        GUI.color = Color.white;
        GUI.enabled = true;

        GUILayout.Space(8);

        // 타입 필터 탭
        DrawTypeFilterTabs();

        GUILayout.FlexibleSpace();

        // 검색창
        GUILayout.Label("검색:", EditorStyles.miniLabel, GUILayout.Width(35));
        string newSearch = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(160));
        if (newSearch != _searchText)
        {
            _searchText    = newSearch;
            _selectedIndex = -1;
        }
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)) && _searchText.Length > 0)
        {
            _searchText    = "";
            _selectedIndex = -1;
            GUI.FocusControl(null);
        }

        GUILayout.Space(4);

        // DB 갱신
        if (GUILayout.Button("DB 갱신", EditorStyles.toolbarButton, GUILayout.Width(60)))
            RefreshDatabase();

        // 목록 새로고침
        if (GUILayout.Button("↺", EditorStyles.toolbarButton, GUILayout.Width(24)))
            LoadAllItems();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTypeFilterTabs()
    {
        var types = new (string label, ItemType? value)[]
        {
            ("전체",   null),
            ("장비",   ItemType.EQUIPMENT),
            ("소비",   ItemType.CONSUMABLE),
            ("기타",   ItemType.OTHERS),
        };

        foreach (var (label, value) in types)
        {
            bool selected = _filterType == value;
            var style = selected ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
            GUI.color = selected ? new Color(0.6f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(label, style, GUILayout.Width(42)))
            {
                _filterType    = value;
                _selectedIndex = -1;
            }
            GUI.color = Color.white;
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 목록 패널 (좌)

    private void DrawListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LIST_PANEL_WIDTH));

        var filtered = GetFilteredItems();

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"아이템 ({filtered.Count}/{_items.Count})", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll,
            GUILayout.Width(LIST_PANEL_WIDTH), GUILayout.ExpandHeight(true));

        for (int i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            if (item == null) continue;

            bool isSelected = _selectedIndex == i;
            bool isDuplicate = _duplicateIDs.Contains(item.itemId);

            // 행 배경
            var rowRect = EditorGUILayout.BeginHorizontal(
                isSelected ? "selectionRect" : "helpBox",
                GUILayout.Height(46));

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                SelectItem(i, item);
                Event.current.Use();
            }

            // 아이콘
            Texture2D preview = item.icon != null ? AssetPreview.GetAssetPreview(item.icon) : null;
            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("□", EditorStyles.boldLabel, GUILayout.Width(40), GUILayout.Height(40));
                GUI.color = Color.white;
            }

            // 정보
            EditorGUILayout.BeginVertical();
            GUILayout.Space(4);

            // 이름 + 중복 경고
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(item.itemName, EditorStyles.boldLabel);
            if (isDuplicate)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label("⚠ ID 중복", EditorStyles.miniLabel, GUILayout.Width(55));
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            // ID + 타입
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            bool isEquip = item is EquipmentSO;
            GUILayout.Label($"ID: {item.itemId}  |  {(isEquip ? "장비" : item.itemType.ToString())}  |  {item.itemRarity}",
                EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(1);
        }

        if (filtered.Count == 0)
        {
            GUILayout.Space(20);
            GUILayout.Label("검색 결과 없음", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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

    private void SelectItem(int index, ItemSO item)
    {
        _selectedIndex  = index;
        _serializedItem = new SerializedObject(item);
        GUI.FocusControl(null);
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 상세 패널 (우)

    private void DrawDetailPanel()
    {
        EditorGUILayout.BeginVertical();

        var filtered = GetFilteredItems();

        // 선택 없음
        if (_selectedIndex < 0 || _selectedIndex >= filtered.Count || _serializedItem == null)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("← 아이템을 선택하세요", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        var item = filtered[_selectedIndex];
        if (item == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        // SerializedObject가 대상과 다르면 갱신
        if (_serializedItem.targetObject != item)
            _serializedItem = new SerializedObject(item);

        _serializedItem.Update();

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"  {item.itemName}", _titleStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(AssetDatabase.GetAssetPath(item), _subtitleStyle);
        if (GUILayout.Button("↗ Project에서 열기", EditorStyles.toolbarButton, GUILayout.Width(110)))
            EditorGUIUtility.PingObject(item);
        EditorGUILayout.EndHorizontal();

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        // ── 아이콘 미리보기 ──────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        // 아이콘 필드
        EditorGUILayout.BeginVertical(GUILayout.Width(ICON_PREVIEW_SIZE + 8));
        Texture2D preview = item.icon != null ? AssetPreview.GetAssetPreview(item.icon) : null;
        if (preview != null)
            GUILayout.Label(preview, GUILayout.Width(ICON_PREVIEW_SIZE), GUILayout.Height(ICON_PREVIEW_SIZE));
        else
        {
            var placeholderRect = GUILayoutUtility.GetRect(ICON_PREVIEW_SIZE, ICON_PREVIEW_SIZE,
                GUILayout.Width(ICON_PREVIEW_SIZE), GUILayout.Height(ICON_PREVIEW_SIZE));
            EditorGUI.DrawRect(placeholderRect, new Color(0.25f, 0.25f, 0.25f));
            GUI.Label(placeholderRect, "아이콘\n없음", new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { alignment = TextAnchor.MiddleCenter });
        }
        EditorGUILayout.EndVertical();

        // 기본 정보 (아이콘 옆)
        EditorGUILayout.BeginVertical();
        DrawProperty("itemId",          "아이템 ID");
        DrawProperty("itemName",        "이름");
        DrawProperty("itemType",        "타입");
        DrawProperty("itemRarity",      "희귀도");
        DrawProperty("icon",            "아이콘");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // ── ID 중복 경고 ──────────────────────────────────────
        if (_duplicateIDs.Contains(item.itemId))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox($"ID {item.itemId}가 다른 아이템과 중복됩니다.", MessageType.Error);
        }

        // ── 공통 데이터 ───────────────────────────────────────
        EditorGUILayout.Space(6);
        DrawSection("기본 데이터", () =>
        {
            DrawProperty("weight",          "무게");
            DrawProperty("itemDescription", "설명");
        });

        // ── 장비 데이터 ───────────────────────────────────────
        if (item is EquipmentSO)
        {
            EditorGUILayout.Space(4);
            DrawSection("장비 데이터", () =>
            {
                DrawProperty("equipSlot",        "장비 슬롯");
                DrawProperty("weaponType",       "무기 타입");
                DrawProperty("equipmentPrefab",  "장비 프리팹");
            });
        }

        EditorGUILayout.Space(10);

        // 변경사항 적용
        if (_serializedItem.ApplyModifiedProperties())
        {
            RebuildDuplicateSet();
            Repaint();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawProperty(string propertyName, string label)
    {
        var prop = _serializedItem.FindProperty(propertyName);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
    }

    private void DrawSection(string title, System.Action drawContent)
    {
        EditorGUILayout.BeginVertical("helpBox");
        GUILayout.Label(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        drawContent();
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 아이템 생성 팝업

    private void DrawCreatePopup()
    {
        float pw = 320f;
        float ph = 145f;
        Rect popupRect = new Rect(4, 22, pw, ph);

        // 배경
        GUI.Box(popupRect, GUIContent.none, "window");
        GUILayout.BeginArea(popupRect);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("새 아이템 생성", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            _showCreatePopup = false;
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 타입 선택
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("타입", GUILayout.Width(60));
        GUI.color = !_createEquipment ? new Color(0.6f, 0.85f, 1f) : Color.white;
        if (GUILayout.Button("ItemSO", EditorStyles.miniButton))
        {
            _createEquipment = false;
            _newSavePath     = DEFAULT_ITEM_PATH;
        }
        GUI.color = _createEquipment ? new Color(0.6f, 0.85f, 1f) : Color.white;
        if (GUILayout.Button("EquipmentSO", EditorStyles.miniButton))
        {
            _createEquipment = true;
            _newSavePath     = DEFAULT_EQUIP_PATH;
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        // 파일명
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("파일명", GUILayout.Width(60));
        _newItemName = EditorGUILayout.TextField(_newItemName);
        EditorGUILayout.EndHorizontal();

        // 저장 경로
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("저장 경로", GUILayout.Width(60));
        _newSavePath = EditorGUILayout.TextField(_newSavePath);
        if (GUILayout.Button("...", GUILayout.Width(28)))
        {
            string selected = EditorUtility.OpenFolderPanel("저장 폴더 선택", _newSavePath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                // 절대경로 → 프로젝트 상대경로로 변환
                string projectPath = Path.GetFullPath(Application.dataPath + "/..");
                if (selected.StartsWith(projectPath))
                    _newSavePath = "Assets" + selected.Substring(projectPath.Length).Replace('\\', '/');
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 생성 버튼
        bool valid = !string.IsNullOrWhiteSpace(_newItemName) && !string.IsNullOrWhiteSpace(_newSavePath);
        GUI.enabled = valid;
        if (GUILayout.Button("생성", GUILayout.Height(24)))
            CreateNewItem();
        GUI.enabled = true;

        GUILayout.EndArea();
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

        _showCreatePopup = false;
        LoadAllItems();

        // 생성한 아이템을 바로 선택
        int idx = GetFilteredItems().FindIndex(i => i == newItem);
        if (idx >= 0)
            SelectItem(idx, newItem);

        EditorGUIUtility.PingObject(newItem);
        Debug.Log($"[ItemEditor] 생성 완료: {assetPath}");
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 복제 / 삭제

    private void DuplicateSelected()
    {
        var filtered = GetFilteredItems();
        if (_selectedIndex < 0 || _selectedIndex >= filtered.Count) return;

        var src = filtered[_selectedIndex];
        string srcPath = AssetDatabase.GetAssetPath(src);
        string dir     = Path.GetDirectoryName(srcPath);
        string newPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(dir, Path.GetFileName(srcPath)).Replace('\\', '/'));

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
        int idx = GetFilteredItems().FindIndex(i => i == copy);
        if (idx >= 0) SelectItem(idx, copy);

        EditorGUIUtility.PingObject(copy);
    }

    private void DeleteSelected()
    {
        var filtered = GetFilteredItems();
        if (_selectedIndex < 0 || _selectedIndex >= filtered.Count) return;

        var target = filtered[_selectedIndex];
        string path = AssetDatabase.GetAssetPath(target);

        if (!EditorUtility.DisplayDialog("아이템 삭제",
            $"'{target.itemName}' (ID: {target.itemId})을 삭제합니다.\n이 작업은 되돌릴 수 없습니다.",
            "삭제", "취소"))
            return;

        _selectedIndex  = -1;
        _serializedItem = null;

        AssetDatabase.DeleteAsset(path);
        LoadAllItems();
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
        Debug.Log($"[ItemEditor] ItemDatabase 갱신 완료 — {_itemDb.AllItems.Count}개");
    }

    #endregion
}
#endif
