#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Item;

namespace UPlayGround.Editor
{
    /// <summary>
    /// EnemyDropTableSO 인스펙터 커스텀 에디터.
    /// 드랍 항목을 테이블 형태로 표시하고 확률 슬라이더로 직관적으로 편집.
    /// </summary>
    [CustomEditor(typeof(EnemyDropTableSO))]
    public class EnemyDropTableEditor : UnityEditor.Editor
    {
        private SerializedProperty _dropItemsProp;

        // 아이템 피커 상태
        private bool _showItemPicker = false;
        private int _pickerTargetIndex = -1;
        private string _pickerSearch = "";
        private Vector2 _pickerScroll;
        private List<ItemSO> _allItems = new List<ItemSO>();

        // 스타일 캐시
        private GUIStyle _headerStyle;
        private GUIStyle _rowBoxStyle;
        private bool _stylesInitialized = false;

        private void OnEnable()
        {
            _dropItemsProp = serializedObject.FindProperty("dropItems");
            LoadAllItems();
        }

        private void LoadAllItems()
        {
            _allItems.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ItemSO");
            foreach (var guid in guids)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null) _allItems.Add(item);
            }
            _allItems.Sort((a, b) => a.itemId.CompareTo(b.itemId));
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            _rowBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin  = new RectOffset(0, 0, 2, 2)
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawSummaryHeader();
            EditorGUILayout.Space(4);
            DrawDropTable();
            EditorGUILayout.Space(6);
            DrawAddButton();
            EditorGUILayout.Space(4);
            DrawOpenEditorButton();

            serializedObject.ApplyModifiedProperties();

            // 아이템 피커는 최상단에 그려야 다른 요소를 가림
            if (_showItemPicker)
                DrawItemPickerPopup();
        }

        // ── 요약 헤더 ────────────────────────────────────────────

        private void DrawSummaryHeader()
        {
            int count = _dropItemsProp.arraySize;
            float avgExpected = 0f;
            for (int i = 0; i < count; i++)
            {
                var el    = _dropItemsProp.GetArrayElementAtIndex(i);
                float rate = el.FindPropertyRelative("rate").floatValue;
                int maxCnt = el.FindPropertyRelative("maximumDropCount").intValue;
                avgExpected += (rate / 100f) * Mathf.Max(1, maxCnt);
            }

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("드랍 테이블", _headerStyle);
            EditorGUILayout.LabelField($"항목 수: {count}개  |  기대 드랍량: {avgExpected:F2}개/처치",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── 드랍 항목 테이블 ──────────────────────────────────────

        private void DrawDropTable()
        {
            int removeIndex = -1;

            for (int i = 0; i < _dropItemsProp.arraySize; i++)
            {
                SerializedProperty entry    = _dropItemsProp.GetArrayElementAtIndex(i);
                SerializedProperty itemProp = entry.FindPropertyRelative("itemData");
                SerializedProperty rateProp = entry.FindPropertyRelative("rate");
                SerializedProperty maxProp  = entry.FindPropertyRelative("maximumDropCount");

                ItemSO item = itemProp.objectReferenceValue as ItemSO;

                EditorGUILayout.BeginVertical(_rowBoxStyle);

                // 첫 줄: 아이콘 + 아이템 이름 피커 + 삭제 버튼
                EditorGUILayout.BeginHorizontal();

                // 아이템 아이콘
                Rect iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
                if (item?.icon != null)
                    GUI.DrawTexture(iconRect, item.icon.texture, ScaleMode.ScaleToFit);
                else
                    GUI.Box(iconRect, "?");

                GUILayout.Space(4);

                // 아이템 이름 / 피커 버튼
                EditorGUILayout.BeginVertical();
                string itemLabel = item != null ? $"[{item.itemId}] {item.itemName}" : "— 아이템 없음 —";
                GUILayout.Label(itemLabel, EditorStyles.boldLabel);
                if (GUILayout.Button("아이템 선택", EditorStyles.miniButton, GUILayout.Width(80)))
                    OpenItemPicker(i);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 삭제 버튼
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(32)))
                    removeIndex = i;
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();

                // 두 번째 줄: 확률 슬라이더
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("확률", GUILayout.Width(36));
                float newRate = EditorGUILayout.Slider(rateProp.floatValue, 0f, 100f);
                if (!Mathf.Approximately(newRate, rateProp.floatValue))
                    rateProp.floatValue = newRate;
                GUILayout.Label($"{rateProp.floatValue:F1}%", GUILayout.Width(44));
                EditorGUILayout.EndHorizontal();

                // 세 번째 줄: 최대 수량
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("최대 수량", GUILayout.Width(60));
                int newMax = EditorGUILayout.IntSlider(maxProp.intValue, 1, 99);
                if (newMax != maxProp.intValue)
                    maxProp.intValue = newMax;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                _dropItemsProp.DeleteArrayElementAtIndex(removeIndex);
                if (_pickerTargetIndex == removeIndex)
                    CloseItemPicker();
            }
        }

        // ── 추가 / 에디터 열기 버튼 ──────────────────────────────

        private void DrawAddButton()
        {
            GUI.color = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("+ 아이템 추가", GUILayout.Height(24)))
            {
                _dropItemsProp.InsertArrayElementAtIndex(_dropItemsProp.arraySize);
                var newEl = _dropItemsProp.GetArrayElementAtIndex(_dropItemsProp.arraySize - 1);
                newEl.FindPropertyRelative("itemData").objectReferenceValue = null;
                newEl.FindPropertyRelative("rate").floatValue = 50f;
                newEl.FindPropertyRelative("maximumDropCount").intValue = 1;
            }
            GUI.color = Color.white;
        }

        private void DrawOpenEditorButton()
        {
            if (GUILayout.Button("드랍 테이블 에디터 열기", GUILayout.Height(22)))
                DropTableEditorWindow.ShowWindow();
        }

        // ── 아이템 피커 팝업 ──────────────────────────────────────

        private void OpenItemPicker(int index)
        {
            _pickerTargetIndex = index;
            _showItemPicker    = true;
            _pickerSearch      = "";
        }

        private void CloseItemPicker()
        {
            _showItemPicker    = false;
            _pickerTargetIndex = -1;
        }

        private void DrawItemPickerPopup()
        {
            // 현재 인스펙터 전체 폭으로 팝업 그리기
            Rect popupRect = new Rect(0, GUILayoutUtility.GetLastRect().yMax - 180, EditorGUIUtility.currentViewWidth, 200);
            GUI.Box(popupRect, GUIContent.none, "window");

            GUILayout.BeginArea(new Rect(popupRect.x + 4, popupRect.y + 4, popupRect.width - 8, popupRect.height - 8));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("아이템 검색", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(20)))
                CloseItemPicker();
            EditorGUILayout.EndHorizontal();

            _pickerSearch = EditorGUILayout.TextField(_pickerSearch, EditorStyles.toolbarSearchField);

            _pickerScroll = EditorGUILayout.BeginScrollView(_pickerScroll, GUILayout.Height(130));

            string lowerSearch = _pickerSearch.ToLower();
            foreach (var item in _allItems)
            {
                if (!string.IsNullOrEmpty(_pickerSearch) &&
                    !item.itemName.ToLower().Contains(lowerSearch) &&
                    !item.itemId.ToString().Contains(_pickerSearch))
                    continue;

                EditorGUILayout.BeginHorizontal();
                if (item.icon != null)
                {
                    Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                    GUI.DrawTexture(r, item.icon.texture, ScaleMode.ScaleToFit);
                }
                if (GUILayout.Button($"[{item.itemId}] {item.itemName}", EditorStyles.miniButton))
                {
                    serializedObject.Update();
                    var entry    = _dropItemsProp.GetArrayElementAtIndex(_pickerTargetIndex);
                    entry.FindPropertyRelative("itemData").objectReferenceValue = item;
                    serializedObject.ApplyModifiedProperties();
                    CloseItemPicker();
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // 팝업 영역 밖 클릭 시 닫기
            if (Event.current.type == EventType.MouseDown && !popupRect.Contains(Event.current.mousePosition))
            {
                CloseItemPicker();
                Repaint();
            }
        }
    }
}
#endif
