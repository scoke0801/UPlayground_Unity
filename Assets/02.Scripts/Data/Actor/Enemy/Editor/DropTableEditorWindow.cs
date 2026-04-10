#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 드랍 테이블 통합 에디터 윈도우.
    /// 메뉴: UPlayGround / Drop Table Editor
    ///
    /// 기능:
    ///   - 몬스터 드랍(EnemyDropTableSO) / 인터랙션 드랍(InteractableActorSO) 탭 분리
    ///   - 좌우 2패널 레이아웃 (목록 / 상세 편집)
    ///   - 아이템 피커 팝업 (이름·ID 검색)
    ///   - 확률 슬라이더 + 기대 드랍량 요약
    ///   - 새 EnemyDropTableSO 에셋 생성
    /// </summary>
    public class DropTableEditorWindow : EditorWindow
    {
        // ── 탭 ────────────────────────────────────────────────────
        private enum Tab { 몬스터_드랍, 인터랙션_드랍 }
        private Tab _currentTab = Tab.몬스터_드랍;

        // ── 데이터 ────────────────────────────────────────────────
        private List<EnemyDropTableSO>   _monsterTables  = new();
        private List<InteractableActorSO> _interactables = new();
        private List<ItemSO>             _allItems       = new();

        // ── 선택 상태 ─────────────────────────────────────────────
        private int _selectedMonsterIndex      = -1;
        private int _selectedInteractableIndex = -1;

        // ── SerializedObject (편집 대상) ──────────────────────────
        private SerializedObject   _serializedTarget;
        private SerializedProperty _dropItemsProp;

        // ── 검색 ─────────────────────────────────────────────────
        private string _listSearch   = "";

        // ── 스크롤 ───────────────────────────────────────────────
        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        // ── 아이템 피커 ──────────────────────────────────────────
        private bool        _showItemPicker       = false;
        private int         _pickerTargetIndex    = -1;
        private string      _pickerSearch         = "";
        private Vector2     _pickerScroll;
        private Rect        _pickerRect;

        // ── 생성 팝업 ────────────────────────────────────────────
        private bool   _showCreatePopup   = false;
        private string _newTableName      = "DropTable_";
        private string _newTableSavePath  = "Assets/10.Datas/Actor/Enemy/DropTables";

        // ── 레이아웃 상수 ─────────────────────────────────────────
        private const float LIST_PANEL_WIDTH = 260f;
        private const float SPLITTER_WIDTH   = 2f;
        private const float ROW_HEIGHT       = 48f;

        // ── 스타일 캐시 ──────────────────────────────────────────
        private GUIStyle _titleStyle;
        private GUIStyle _selectedRowStyle;
        private GUIStyle _normalRowStyle;
        private GUIStyle _sectionHeaderStyle;
        private bool     _stylesInitialized = false;

        // ─────────────────────────────────────────────────────────

        [MenuItem("UPlayGround/Drop Table Editor")]
        public static void ShowWindow()
        {
            var win = GetWindow<DropTableEditorWindow>("Drop Table Editor");
            win.minSize = new Vector2(700, 480);
        }

        // ─────────────────────────────────────────────────────────
        #region 초기화

        private void OnEnable()
        {
            RefreshAllAssets();
        }

        public void RefreshAllAssets()
        {
            // 몬스터 드랍 테이블
            _monsterTables.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyDropTableSO"))
            {
                var so = AssetDatabase.LoadAssetAtPath<EnemyDropTableSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null) _monsterTables.Add(so);
            }
            _monsterTables.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // 인터랙션 SO — NpcActorSO 등 하위 타입 제외, 정확히 InteractableActorSO 타입만 포함
            _interactables.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:InteractableActorSO"))
            {
                var so = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && so.GetType() == typeof(InteractableActorSO))
                    _interactables.Add(so);
            }
            _interactables.Sort((a, b) => string.Compare(a.actorName, b.actorName, StringComparison.Ordinal));

            // 아이템 목록
            _allItems.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null) _allItems.Add(item);
            }
            _allItems.Sort((a, b) => a.itemId.CompareTo(b.itemId));

            // 선택 초기화
            _selectedMonsterIndex      = -1;
            _selectedInteractableIndex = -1;
            _serializedTarget          = null;
            _dropItemsProp             = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 스타일 초기화

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft
            };

            _selectedRowStyle = new GUIStyle("SelectionRect")
            {
                padding  = new RectOffset(6, 6, 4, 4),
                margin   = new RectOffset(0, 0, 1, 1),
                fixedHeight = ROW_HEIGHT
            };

            _normalRowStyle = new GUIStyle("box")
            {
                padding  = new RectOffset(6, 6, 4, 4),
                margin   = new RectOffset(0, 0, 1, 1),
                fixedHeight = ROW_HEIGHT
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.7f, 0.9f, 1f) }
            };
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region OnGUI 루트

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            GUILayout.Box(GUIContent.none, GUILayout.Width(SPLITTER_WIDTH), GUILayout.ExpandHeight(true));
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            // 팝업은 가장 마지막에 그린다
            if (_showCreatePopup) DrawCreatePopup();
            if (_showItemPicker)  DrawItemPickerPopup();
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 툴바

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 탭 버튼
            foreach (Tab tab in System.Enum.GetValues(typeof(Tab)))
            {
                string label  = tab.ToString().Replace("_", " ");
                bool selected = _currentTab == tab;
                GUI.color = selected ? new Color(0.6f, 0.9f, 1f) : Color.white;
                if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton, GUILayout.Width(140)) && !selected)
                {
                    _currentTab   = tab;
                    _listSearch   = "";
                    _showItemPicker  = false;
                    _showCreatePopup = false;
                }
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            // 검색창
            GUILayout.Label("검색:", EditorStyles.miniLabel, GUILayout.Width(32));
            string newSearch = GUILayout.TextField(_listSearch, EditorStyles.toolbarSearchField, GUILayout.Width(160));
            if (newSearch != _listSearch)
                _listSearch = newSearch;
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)) && _listSearch.Length > 0)
            {
                _listSearch = "";
                GUI.FocusControl(null);
            }

            GUILayout.Space(4);

            // 새로고침
            if (GUILayout.Button("↺ 새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshAllAssets();

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 좌측 패널 (목록)

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LIST_PANEL_WIDTH));

            if (_currentTab == Tab.몬스터_드랍)
                DrawMonsterList();
            else
                DrawInteractableList();

            EditorGUILayout.EndVertical();
        }

        private void DrawMonsterList()
        {
            // 헤더
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"몬스터 드랍 테이블 ({_monsterTables.Count})", _sectionHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 생성", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _showCreatePopup = true;
                _newTableName    = "DropTable_";
            }
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            string lower = _listSearch.ToLower();
            for (int i = 0; i < _monsterTables.Count; i++)
            {
                var table = _monsterTables[i];
                if (!string.IsNullOrEmpty(_listSearch) && !table.name.ToLower().Contains(lower))
                    continue;

                bool isSelected = _selectedMonsterIndex == i;
                GUIStyle rowStyle = isSelected ? _selectedRowStyle : _normalRowStyle;

                EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(ROW_HEIGHT));
                EditorGUILayout.BeginVertical();
                GUILayout.Label(table.name, EditorStyles.boldLabel);
                GUILayout.Label($"{table.dropItems.Count}개 항목  |  기대: {GetExpected(table.dropItems):F2}개",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                // 클릭 감지
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    SelectMonsterTable(i);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawInteractableList()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"인터랙션 오브젝트 ({_interactables.Count})", _sectionHeaderStyle);
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            string lower = _listSearch.ToLower();
            for (int i = 0; i < _interactables.Count; i++)
            {
                var ia = _interactables[i];
                string displayName = string.IsNullOrEmpty(ia.actorName) ? ia.name : ia.actorName;
                if (!string.IsNullOrEmpty(_listSearch) && !displayName.ToLower().Contains(lower))
                    continue;

                bool isSelected = _selectedInteractableIndex == i;
                GUIStyle rowStyle = isSelected ? _selectedRowStyle : _normalRowStyle;

                EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(ROW_HEIGHT));
                EditorGUILayout.BeginVertical();
                GUILayout.Label(displayName, EditorStyles.boldLabel);
                GUILayout.Label($"{ia.dropItems.Count}개 항목  |  기대: {GetExpected(ia.dropItems):F2}개",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    SelectInteractable(i);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 우측 패널 (상세 편집)

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (_serializedTarget == null || _dropItemsProp == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← 좌측에서 편집할 항목을 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            _serializedTarget.Update();

            DrawDetailHeader();
            EditorGUILayout.Space(4);
            DrawDropSummaryBar();
            EditorGUILayout.Space(4);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            DrawDropEntries();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            DrawDetailFooter();

            _serializedTarget.ApplyModifiedProperties();

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string assetName = _serializedTarget.targetObject.name;
            GUILayout.Label(assetName, _titleStyle);
            GUILayout.FlexibleSpace();

            // 에셋 핑
            if (GUILayout.Button("프로젝트에서 보기", EditorStyles.toolbarButton, GUILayout.Width(110)))
                EditorGUIUtility.PingObject(_serializedTarget.targetObject);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropSummaryBar()
        {
            int count = _dropItemsProp.arraySize;
            float expected = 0f;
            float maxProb  = 0f;

            for (int i = 0; i < count; i++)
            {
                var el   = _dropItemsProp.GetArrayElementAtIndex(i);
                float r  = el.FindPropertyRelative("rate").floatValue;
                int   mx = el.FindPropertyRelative("maximumDropCount").intValue;
                expected += (r / 100f) * Mathf.Max(1, mx);
                maxProb  = Mathf.Max(maxProb, r);
            }

            EditorGUILayout.BeginHorizontal("helpbox");
            GUILayout.Label($"항목 수: {count}개", GUILayout.Width(90));
            GUILayout.Label($"기대 드랍량: {expected:F2}개/처치", GUILayout.Width(160));
            GUILayout.Label($"최고 확률: {maxProb:F1}%", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropEntries()
        {
            int removeAt = -1;
            int moveUpAt = -1;

            // 컬럼 헤더
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#",       GUILayout.Width(22));
            GUILayout.Label("아이템",  GUILayout.Width(200));
            GUILayout.Label("확률 (%)", GUILayout.ExpandWidth(true));
            GUILayout.Label("최대 수량", GUILayout.Width(90));
            GUILayout.Label("",        GUILayout.Width(46));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _dropItemsProp.arraySize; i++)
            {
                SerializedProperty entry    = _dropItemsProp.GetArrayElementAtIndex(i);
                SerializedProperty itemProp = entry.FindPropertyRelative("itemData");
                SerializedProperty rateProp = entry.FindPropertyRelative("rate");
                SerializedProperty maxProp  = entry.FindPropertyRelative("maximumDropCount");

                ItemSO item = itemProp.objectReferenceValue as ItemSO;

                // 확률에 따른 행 배경색
                float rateVal   = rateProp.floatValue;
                Color rowBgColor = rateVal >= 75f ? new Color(0.2f, 0.5f, 0.2f, 0.3f)
                                 : rateVal >= 40f ? new Color(0.5f, 0.5f, 0.2f, 0.3f)
                                 : new Color(0.5f, 0.2f, 0.2f, 0.3f);

                var rowStyle = new GUIStyle("box") { margin = new RectOffset(0, 0, 1, 1) };
                EditorGUILayout.BeginVertical(rowStyle);

                // 메인 행
                EditorGUILayout.BeginHorizontal(GUILayout.Height(28));

                // 인덱스
                GUILayout.Label($"{i + 1}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(22));

                // 아이콘
                if (item?.icon != null)
                {
                    Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                    GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Box("?", GUILayout.Width(24), GUILayout.Height(24));
                }

                GUILayout.Space(4);

                // 아이템 이름 + 피커
                EditorGUILayout.BeginVertical(GUILayout.Width(170));
                string label = item != null ? $"[{item.itemId}] {item.itemName}" : "— 미설정 —";
                GUILayout.Label(label, item != null ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                if (GUILayout.Button("아이템 선택 ▾", EditorStyles.miniButton, GUILayout.Width(90)))
                    OpenItemPicker(i);
                EditorGUILayout.EndVertical();

                // 확률 슬라이더
                float newRate = EditorGUILayout.Slider(rateVal, 0f, 100f, GUILayout.ExpandWidth(true));
                if (!Mathf.Approximately(newRate, rateVal))
                    rateProp.floatValue = newRate;

                // 최대 수량
                int newMax = EditorGUILayout.IntField(maxProp.intValue, GUILayout.Width(36));
                newMax = Mathf.Clamp(newMax, 1, 999);
                if (newMax != maxProp.intValue) maxProp.intValue = newMax;

                // 위로 이동
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(20))) moveUpAt = i;
                GUI.enabled = true;

                // 삭제
                GUI.color = new Color(1f, 0.45f, 0.45f);
                if (GUILayout.Button("✕", GUILayout.Width(22))) removeAt = i;
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (removeAt >= 0)
            {
                _dropItemsProp.DeleteArrayElementAtIndex(removeAt);
                if (_pickerTargetIndex == removeAt) CloseItemPicker();
            }
            if (moveUpAt > 0)
                _dropItemsProp.MoveArrayElement(moveUpAt, moveUpAt - 1);
        }

        private void DrawDetailFooter()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.color = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("+ 아이템 추가", GUILayout.Height(26)))
            {
                int newIdx = _dropItemsProp.arraySize;
                _dropItemsProp.InsertArrayElementAtIndex(newIdx);
                var el = _dropItemsProp.GetArrayElementAtIndex(newIdx);
                el.FindPropertyRelative("itemData").objectReferenceValue = null;
                el.FindPropertyRelative("rate").floatValue               = 50f;
                el.FindPropertyRelative("maximumDropCount").intValue     = 1;
            }
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // 전체 삭제
            GUI.color = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("전체 삭제", GUILayout.Height(26), GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("드랍 테이블 초기화",
                    "모든 드랍 항목을 삭제하시겠습니까?", "삭제", "취소"))
                    _dropItemsProp.ClearArray();
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 아이템 피커 팝업

        private void OpenItemPicker(int entryIndex)
        {
            _pickerTargetIndex = entryIndex;
            _showItemPicker    = true;
            _pickerSearch      = "";

            // 피커 위치: 현재 마우스 위치 기준
            Vector2 mouse = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            _pickerRect = new Rect(mouse.x, mouse.y, 320, 320);
        }

        private void CloseItemPicker()
        {
            _showItemPicker    = false;
            _pickerTargetIndex = -1;
        }

        private void DrawItemPickerPopup()
        {
            // 스크린 좌표 → 윈도우 로컬 좌표
            Rect screenRect = _pickerRect;
            // 윈도우 경계 클램프
            screenRect.x = Mathf.Clamp(screenRect.x, position.x + LIST_PANEL_WIDTH + 10,
                position.x + position.width - screenRect.width - 10);
            screenRect.y = Mathf.Clamp(screenRect.y, position.y + 30,
                position.y + position.height - screenRect.height - 10);

            Rect localRect = new Rect(
                screenRect.x - position.x,
                screenRect.y - position.y,
                screenRect.width,
                screenRect.height);

            GUI.Box(localRect, GUIContent.none, "window");

            GUILayout.BeginArea(new Rect(localRect.x + 6, localRect.y + 4, localRect.width - 12, localRect.height - 8));

            // 헤더
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("아이템 선택", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(20)))
                CloseItemPicker();
            EditorGUILayout.EndHorizontal();

            // 검색
            _pickerSearch = EditorGUILayout.TextField(_pickerSearch, EditorStyles.toolbarSearchField);

            // 목록
            _pickerScroll = EditorGUILayout.BeginScrollView(_pickerScroll, GUILayout.ExpandHeight(true));
            string lower = _pickerSearch.ToLower();

            foreach (var item in _allItems)
            {
                if (!string.IsNullOrEmpty(_pickerSearch))
                {
                    bool nameMatch = item.itemName.ToLower().Contains(lower);
                    bool idMatch   = item.itemId.ToString().Contains(_pickerSearch);
                    if (!nameMatch && !idMatch) continue;
                }

                EditorGUILayout.BeginHorizontal();

                // 아이콘
                if (item.icon != null)
                {
                    Rect r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                    GUI.DrawTexture(r, item.icon.texture, ScaleMode.ScaleToFit);
                }

                // 선택 버튼
                string btnLabel = $"[{item.itemId}]  {item.itemName}";
                if (GUILayout.Button(btnLabel, EditorStyles.miniButton))
                {
                    if (_dropItemsProp != null && _pickerTargetIndex >= 0 &&
                        _pickerTargetIndex < _dropItemsProp.arraySize)
                    {
                        _serializedTarget.Update();
                        var entry = _dropItemsProp.GetArrayElementAtIndex(_pickerTargetIndex);
                        entry.FindPropertyRelative("itemData").objectReferenceValue = item;
                        _serializedTarget.ApplyModifiedProperties();
                    }
                    CloseItemPicker();
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // 팝업 외부 클릭 → 닫기
            if (Event.current.type == EventType.MouseDown && !localRect.Contains(Event.current.mousePosition))
            {
                CloseItemPicker();
                Repaint();
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 생성 팝업

        private void DrawCreatePopup()
        {
            Rect popupRect = new Rect(
                LIST_PANEL_WIDTH / 2 - 130,
                80,
                280, 130);

            GUI.Box(popupRect, GUIContent.none, "window");
            GUILayout.BeginArea(new Rect(popupRect.x + 8, popupRect.y + 4, popupRect.width - 16, popupRect.height - 8));

            GUILayout.Label("새 드랍 테이블 생성", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _newTableName     = EditorGUILayout.TextField("이름",       _newTableName);
            _newTableSavePath = EditorGUILayout.TextField("저장 경로",  _newTableSavePath);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("생성", GUILayout.Height(24)))
            {
                CreateNewDropTable();
                _showCreatePopup = false;
            }
            if (GUILayout.Button("취소", GUILayout.Height(24)))
                _showCreatePopup = false;

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CreateNewDropTable()
        {
            if (string.IsNullOrWhiteSpace(_newTableName))
            {
                EditorUtility.DisplayDialog("오류", "이름을 입력해주세요.", "확인");
                return;
            }

            if (!Directory.Exists(_newTableSavePath))
                Directory.CreateDirectory(_newTableSavePath);

            string path    = $"{_newTableSavePath}/{_newTableName}.asset";
            string unique  = AssetDatabase.GenerateUniqueAssetPath(path);

            var newTable = CreateInstance<EnemyDropTableSO>();
            AssetDatabase.CreateAsset(newTable, unique);
            AssetDatabase.SaveAssets();

            RefreshAllAssets();

            // 생성된 테이블 자동 선택
            int idx = _monsterTables.FindIndex(t => t == newTable);
            if (idx >= 0) SelectMonsterTable(idx);

            EditorGUIUtility.PingObject(newTable);
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 선택 처리

        private void SelectMonsterTable(int index)
        {
            _selectedMonsterIndex      = index;
            _selectedInteractableIndex = -1;
            _showItemPicker            = false;

            _serializedTarget = new SerializedObject(_monsterTables[index]);
            _dropItemsProp    = _serializedTarget.FindProperty("dropItems");
        }

        private void SelectInteractable(int index)
        {
            _selectedInteractableIndex = index;
            _selectedMonsterIndex      = -1;
            _showItemPicker            = false;

            _serializedTarget = new SerializedObject(_interactables[index]);
            _dropItemsProp    = _serializedTarget.FindProperty("dropItems");
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region 유틸리티

        private static float GetExpected(List<ItemDropList> list)
        {
            float total = 0f;
            foreach (var d in list)
                total += (d.rate / 100f) * Mathf.Max(1, d.maximumDropCount);
            return total;
        }

        #endregion
    }
}
#endif
