#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Quest;
using UPlayGround.Tool.Editor;
using UPlayGround.Data.Item;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 퀘스트 비주얼 에디터 윈도우.
    /// 메뉴: UPlayGround / Quest / Quest Editor
    ///
    /// 기능:
    ///   - 좌우 2패널 레이아웃 (목록 / 상세 편집)
    ///   - QuestSO 생성 / 복제 / 삭제
    ///   - 퀘스트 ID 중복 감지
    ///   - 목표 타입별 컬러 코딩 + 조건부 필드
    ///   - 보상 아이템 피커
    ///   - QuestDatabase DB 갱신
    ///   - 검색 + 상태 필터 탭
    /// </summary>
    public class QuestEditorWindow : EditorWindow
    {
        // ──── 데이터 ────
        private List<QuestSO>   _quests       = new List<QuestSO>();
        private QuestDatabase   _questDb;
        private HashSet<string> _duplicateIds = new HashSet<string>();

        // ──── 선택 / 필터 ────
        private int     _selectedIndex = -1;
        private string  _searchText    = "";
        private int     _filterTab     = 0;  // 0=전체, 1=반복, 2=자동완료

        // ──── 스크롤 ────
        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        // ──── 편집 대상 ────
        private SerializedObject _serializedQuest;

        // ──── 생성 팝업 ────
        private bool   _showCreatePopup = false;
        private string _newQuestId      = "quest_new";
        private string _newQuestName    = "새 퀘스트";
        private string _newSavePath     = "Assets/10.Datas/Quest";

        // ──── 아이템 피커 ────
        private bool         _showItemPicker    = false;
        private int          _pickerRewardIndex = -1;
        private string       _pickerSearch      = "";
        private Vector2      _pickerScroll;
        private List<ItemSO> _allItems          = new List<ItemSO>();

        // ──── 스타일 캐시 ────
        private bool     _stylesReady;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _objectiveStyle;
        private GUIStyle _badgeStyle;

        // ──── 상수 ────
        private const float LIST_PANEL_WIDTH  = 300f;
        private const string DEFAULT_QUEST_PATH = "Assets/10.Datas/Quest";

        // ──── 목표 타입 메타 ────
        private static readonly Color[] ObjColors =
        {
            new Color(0.35f, 0.75f, 0.35f),
            new Color(0.35f, 0.65f, 0.95f),
            new Color(0.90f, 0.75f, 0.20f),
            new Color(0.90f, 0.35f, 0.35f),
            new Color(0.75f, 0.45f, 0.90f),
            new Color(0.95f, 0.60f, 0.20f),
            new Color(0.30f, 0.80f, 0.80f),
            new Color(0.80f, 0.80f, 0.80f),
        };
        private static readonly string[] ObjLabels =
        {
            "아이템 수집","아이템 전달","아이템 사용","몬스터 처치",
            "스토리 진행","아이템 제작","아이템 강화","위치 도달",
        };
        private static readonly string[] ObjHints =
        {
            "NotifyItemCollected(itemId, count)",
            "NotifyItemDelivered(npcId, itemId, count)",
            "NotifyItemUsed(itemId, count)",
            "NotifyMonsterKill(actorId)",
            "NotifyStoryProgress(progress)",
            "NotifyItemCrafted(recipeId, quantity)",
            "NotifyItemEnhanced(itemId)",
            "NotifyLocationReached(locationId)",
        };

        // ──────────────────────────────────────────────────────────

        [MenuItem("UPlayGround/게임플레이/퀘스트/퀘스트 에디터")]
        public static void ShowWindow()
        {
            var win = GetWindow<QuestEditorWindow>("Quest Editor");
            win.minSize = new Vector2(820, 540);
        }

        public static void ShowAndSelect(QuestSO quest)
        {
            var win = GetWindow<QuestEditorWindow>("Quest Editor");
            win.minSize = new Vector2(820, 540);
            win.SelectQuest(quest);
        }

        // ──────────────────────────────────────────────────────────
        #region 초기화

        private void OnEnable()
        {
            LoadAllQuests();
            LoadQuestDatabase();
            LoadAllItems();
        }

        private void LoadAllQuests()
        {
            _quests.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:QuestSO"))
            {
                var q = AssetDatabase.LoadAssetAtPath<QuestSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (q != null) _quests.Add(q);
            }
            _quests = _quests.OrderBy(q => q.questId).ToList();
            RebuildDuplicateSet();
            _selectedIndex   = -1;
            _serializedQuest = null;
        }

        private void LoadQuestDatabase()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:QuestDatabase"))
            {
                _questDb = AssetDatabase.LoadAssetAtPath<QuestDatabase>(AssetDatabase.GUIDToAssetPath(guid));
                if (_questDb != null) break;
            }
        }

        private void LoadAllItems()
        {
            _allItems.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null) _allItems.Add(item);
            }
            _allItems.Sort((a, b) => a.itemId.CompareTo(b.itemId));
        }

        private void RebuildDuplicateSet()
        {
            _duplicateIds.Clear();
            var seen = new HashSet<string>();
            foreach (var q in _quests)
            {
                if (q == null || string.IsNullOrEmpty(q.questId)) continue;
                if (!seen.Add(q.questId)) _duplicateIds.Add(q.questId);
            }
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 13, alignment = TextAnchor.MiddleLeft };

            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.55f, 0.55f, 0.55f) } };

            _sectionStyle    = new GUIStyle("helpBox") { padding = new RectOffset(8, 8, 6, 6) };
            _objectiveStyle  = new GUIStyle("box")
                { padding = new RectOffset(8, 8, 6, 6), margin = new RectOffset(0, 0, 3, 3) };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
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

            if (_showCreatePopup) DrawCreatePopup();
            if (_showItemPicker)  DrawItemPickerPopup();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 툴바

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+ 새 퀘스트", EditorStyles.toolbarButton, GUILayout.Width(80)))
                _showCreatePopup = !_showCreatePopup;

            GUILayout.Space(4);

            var filtered = GetFilteredQuests();

            GUI.enabled = _selectedIndex >= 0 && _selectedIndex < filtered.Count;
            if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(48)))
                DuplicateSelected();

            GUI.color   = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(48)))
                DeleteSelected();
            GUI.color   = Color.white;
            GUI.enabled = true;

            GUILayout.Space(8);
            DrawFilterTabs();
            GUILayout.FlexibleSpace();

            // 검색
            GUILayout.Label("검색:", EditorStyles.miniLabel, GUILayout.Width(35));
            string ns = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(160));
            if (ns != _searchText) { _searchText = ns; _selectedIndex = -1; }
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)) && _searchText.Length > 0)
            {
                _searchText = ""; _selectedIndex = -1; GUI.FocusControl(null);
            }

            GUILayout.Space(4);
            if (GUILayout.Button("DB 갱신", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshDatabase();
            if (GUILayout.Button("Enum 생성", EditorStyles.toolbarButton, GUILayout.Width(70)))
                GenerateQuestIdEnum();
            if (GUILayout.Button("↺", EditorStyles.toolbarButton, GUILayout.Width(24)))
                LoadAllQuests();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterTabs()
        {
            var tabs = new[] { "전체", "반복", "자동완료" };
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.color = _filterTab == i ? new Color(0.6f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button(tabs[i], EditorStyles.toolbarButton, GUILayout.Width(52)))
                { _filterTab = i; _selectedIndex = -1; }
            }
            GUI.color = Color.white;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 목록 패널 (좌)

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LIST_PANEL_WIDTH));

            var filtered = GetFilteredQuests();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"퀘스트  ({filtered.Count}/{_quests.Count})", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll,
                GUILayout.Width(LIST_PANEL_WIDTH), GUILayout.ExpandHeight(true));

            for (int i = 0; i < filtered.Count; i++)
            {
                var q = filtered[i];
                if (q == null) continue;

                bool isSelected  = _selectedIndex == i;
                bool isDuplicate = _duplicateIds.Contains(q.questId);

                var rowRect = EditorGUILayout.BeginHorizontal(
                    isSelected ? "selectionRect" : "helpBox", GUILayout.Height(52));

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                { SelectIndex(i, q); Event.current.Use(); }

                // 목표 수 뱃지
                EditorGUILayout.BeginVertical(GUILayout.Width(30));
                GUILayout.Space(8);
                var badgeRect = GUILayoutUtility.GetRect(28, 22, GUILayout.Width(28), GUILayout.Height(22));
                EditorGUI.DrawRect(badgeRect, new Color(0.3f, 0.3f, 0.3f));
                GUI.Label(badgeRect, q.objectives?.Count.ToString() ?? "0", _badgeStyle);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                GUILayout.Space(4);

                // 이름 + 중복 경고
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(string.IsNullOrEmpty(q.questName) ? "(이름 없음)" : q.questName,
                    EditorStyles.boldLabel);
                if (isDuplicate)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label("⚠ ID중복", EditorStyles.miniLabel, GUILayout.Width(52));
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                // ID + 태그
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                var tags = new List<string> { q.questId };
                if (q.isRepeatable) tags.Add("반복");
                if (q.autoComplete) tags.Add("자동완료");
                if (q.requiredQuestIds?.Count > 0) tags.Add($"선행{q.requiredQuestIds.Count}");
                if (q.autoAcceptNextQuestIds?.Count > 0) tags.Add($"연계{q.autoAcceptNextQuestIds.Count}");
                GUILayout.Label(string.Join("  |  ", tags), EditorStyles.miniLabel);
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

        private List<QuestSO> GetFilteredQuests()
        {
            IEnumerable<QuestSO> result = _quests.Where(q => q != null);

            result = _filterTab switch
            {
                1 => result.Where(q => q.isRepeatable),
                2 => result.Where(q => q.autoComplete),
                _ => result
            };

            if (!string.IsNullOrEmpty(_searchText))
                result = result.Where(q =>
                    q.questId.IndexOf(_searchText, System.StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    q.questName.IndexOf(_searchText, System.StringComparison.CurrentCultureIgnoreCase) >= 0);

            return result.ToList();
        }

        private void SelectIndex(int index, QuestSO q)
        {
            _selectedIndex   = index;
            _serializedQuest = new SerializedObject(q);
            GUI.FocusControl(null);
        }

        private void SelectQuest(QuestSO q)
        {
            LoadAllQuests();
            var idx = GetFilteredQuests().FindIndex(x => x == q);
            if (idx >= 0) SelectIndex(idx, q);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 상세 패널 (우)

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical();

            var filtered = GetFilteredQuests();
            if (_selectedIndex < 0 || _selectedIndex >= filtered.Count || _serializedQuest == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← 퀘스트를 선택하세요", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            var q = filtered[_selectedIndex];
            if (q == null) { EditorGUILayout.EndVertical(); return; }
            if (_serializedQuest.targetObject != q) _serializedQuest = new SerializedObject(q);

            _serializedQuest.Update();

            // ── 상단 헤더 ────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"  {q.questName}", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(AssetDatabase.GetAssetPath(q), _subtitleStyle);
            if (GUILayout.Button("↗ 에셋 열기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                EditorGUIUtility.PingObject(q);
            EditorGUILayout.EndHorizontal();

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            // ID 중복 경고
            if (_duplicateIds.Contains(q.questId))
                EditorGUILayout.HelpBox($"Quest ID '{q.questId}'가 다른 퀘스트와 중복됩니다.", MessageType.Error);

            EditorGUILayout.Space(4);
            DrawDetailBasicInfo();
            EditorGUILayout.Space(4);
            DrawDetailPrerequisites();
            EditorGUILayout.Space(4);
            DrawDetailAutoAcceptNextQuests();
            EditorGUILayout.Space(4);
            DrawDetailObjectives();
            EditorGUILayout.Space(4);
            DrawDetailReward();
            EditorGUILayout.Space(4);
            DrawDetailSettings();
            EditorGUILayout.Space(8);

            if (_serializedQuest.ApplyModifiedProperties())
            {
                RebuildDuplicateSet();
                Repaint();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── 기본 정보 ────────────────────────────────────────────

        private void DrawDetailBasicInfo()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("기본 정보", EditorStyles.boldLabel);
            DrawProp("questId",          "퀘스트 ID");
            DrawProp("questName",        "퀘스트 이름");
            DrawProp("questType",        "분류(메인/서브)");
            DrawProp("shortSummary",     "짧은 부제");
            DrawProp("questDescription", "설명");
            EditorGUILayout.EndVertical();
        }

        // ── 선행 조건 ────────────────────────────────────────────

        private void DrawDetailPrerequisites()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("선행 조건", EditorStyles.boldLabel);
            DrawProp("requiredQuestIds",      "완료 필요 퀘스트 ID");
            DrawProp("requiredStoryProgress", "필요 스토리 진행도");
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailAutoAcceptNextQuests()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("자동 연계", EditorStyles.boldLabel);
            DrawProp("autoAcceptNextQuestIds", "완료 후 자동 수락 퀘스트 ID");
            EditorGUILayout.EndVertical();
        }

        // ── 목표 ────────────────────────────────────────────────

        private void DrawDetailObjectives()
        {
            var objsProp = _serializedQuest.FindProperty("objectives");

            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"목표  ({objsProp.arraySize}개)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 추가", EditorStyles.miniButton, GUILayout.Width(50)))
                AddNewObjective(objsProp);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            int removeIdx = -1;
            for (int i = 0; i < objsProp.arraySize; i++)
                if (DrawObjectiveCard(objsProp, i)) removeIdx = i;

            if (removeIdx >= 0) objsProp.DeleteArrayElementAtIndex(removeIdx);

            if (objsProp.arraySize == 0)
                EditorGUILayout.HelpBox("목표가 없습니다.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private bool DrawObjectiveCard(SerializedProperty objsProp, int index)
        {
            var elem    = objsProp.GetArrayElementAtIndex(index);
            var typeProp = elem.FindPropertyRelative("type");
            int typeIdx  = typeProp.enumValueIndex;
            var typeVal  = (QuestObjectiveType)typeIdx;
            Color bg     = typeIdx < ObjColors.Length ? ObjColors[typeIdx] : Color.gray;

            // 카드 배경
            var savedBg = GUI.backgroundColor;
            GUI.backgroundColor = bg * 0.4f + Color.white * 0.6f;
            EditorGUILayout.BeginVertical(_objectiveStyle);
            GUI.backgroundColor = savedBg;

            // ── 헤더 행 ─────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            var badgeRect = GUILayoutUtility.GetRect(72, 18, GUILayout.Width(72), GUILayout.Height(18));
            EditorGUI.DrawRect(badgeRect, bg);
            string lbl = typeIdx < ObjLabels.Length ? ObjLabels[typeIdx] : typeVal.ToString();
            GUI.Label(badgeRect, lbl, _badgeStyle);
            GUILayout.Space(4);

            var descProp = elem.FindPropertyRelative("description");
            GUI.color    = new Color(0.75f, 0.75f, 0.75f);
            GUILayout.Label(string.IsNullOrEmpty(descProp.stringValue) ? "—" : descProp.stringValue,
                EditorStyles.miniLabel);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            // 위아래 이동
            GUI.enabled = index > 0;
            if (GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Width(20)))
                objsProp.MoveArrayElement(index, index - 1);
            GUI.enabled = index < objsProp.arraySize - 1;
            if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(20)))
                objsProp.MoveArrayElement(index, index + 1);
            GUI.enabled = true;

            GUI.color = new Color(1f, 0.5f, 0.5f);
            bool remove = GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20));
            GUI.color   = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // ── 편집 필드 ────────────────────────────────────────
            EditorGUI.indentLevel++;
            DrawElemProp(elem, "objectiveId", "목표 ID");
            DrawElemProp(elem, "description", "설명");
            DrawElemProp(elem, "type",        "타입");

            // 타입 변경 반영
            typeIdx = typeProp.enumValueIndex;
            typeVal = (QuestObjectiveType)typeIdx;

            switch (typeVal)
            {
                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemUse:
                case QuestObjectiveType.ItemEnhance:
                    DrawElemProp(elem, "targetId",      "아이템 ID");
                    DrawElemProp(elem, "requiredCount", "필요 수량");
                    break;
                case QuestObjectiveType.ItemDeliver:
                    DrawElemProp(elem, "targetId",      "아이템 ID");
                    DrawElemProp(elem, "npcId",         "NPC ID");
                    DrawElemProp(elem, "requiredCount", "전달 수량");
                    break;
                case QuestObjectiveType.MonsterKill:
                    DrawElemProp(elem, "targetStringId", "Actor ID");
                    DrawElemProp(elem, "targetId",       "레거시 숫자 ID");
                    DrawElemProp(elem, "requiredCount", "처치 수");
                    break;
                case QuestObjectiveType.StoryProgress:
                    DrawElemProp(elem, "targetId", "필요 진행도");
                    break;
                case QuestObjectiveType.ItemCraft:
                    DrawElemProp(elem, "targetId",      "레시피 ID");
                    DrawElemProp(elem, "requiredCount", "제작 횟수");
                    break;
                case QuestObjectiveType.ReachLocation:
                    DrawElemProp(elem, "targetStringId", "위치 ID");
                    break;
            }
            EditorGUI.indentLevel--;

            // 연결 포인트 힌트
            string hint = typeIdx < ObjHints.Length ? ObjHints[typeIdx] : "";
            GUI.color = new Color(0.55f, 0.55f, 0.55f);
            GUILayout.Label($"▶ QuestManager.Instance.{hint}", EditorStyles.miniLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            return remove;
        }

        private void AddNewObjective(SerializedProperty objsProp)
        {
            objsProp.InsertArrayElementAtIndex(objsProp.arraySize);
            var el = objsProp.GetArrayElementAtIndex(objsProp.arraySize - 1);
            el.FindPropertyRelative("objectiveId").stringValue    = $"obj_{objsProp.arraySize}";
            el.FindPropertyRelative("description").stringValue    = "";
            el.FindPropertyRelative("type").enumValueIndex        = 0;
            el.FindPropertyRelative("targetId").intValue          = 0;
            el.FindPropertyRelative("npcId").intValue             = 0;
            el.FindPropertyRelative("targetStringId").stringValue = "";
            el.FindPropertyRelative("requiredCount").intValue     = 1;
        }

        // ── 보상 ────────────────────────────────────────────────

        private void DrawDetailReward()
        {
            var rewardProp = _serializedQuest.FindProperty("reward");
            var goldProp   = rewardProp.FindPropertyRelative("gold");
            var expProp    = rewardProp.FindPropertyRelative("exp");
            var itemsProp  = rewardProp.FindPropertyRelative("items");

            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("보상", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(goldProp, new GUIContent("골드"));
            EditorGUILayout.PropertyField(expProp, new GUIContent("경험치"));

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"보상 아이템  ({itemsProp.arraySize}개)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 추가", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
                var el = itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1);
                el.FindPropertyRelative("itemId").intValue = 0;
                el.FindPropertyRelative("count").intValue  = 1;
            }
            EditorGUILayout.EndHorizontal();

            int removeIdx = -1;
            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                var el      = itemsProp.GetArrayElementAtIndex(i);
                var idProp  = el.FindPropertyRelative("itemId");
                var cntProp = el.FindPropertyRelative("count");
                var item    = _allItems.Find(x => x.itemId == idProp.intValue);

                EditorGUILayout.BeginHorizontal("box");

                // 아이콘
                if (item?.icon != null)
                {
                    var r = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                    GUI.DrawTexture(r, item.icon.texture, ScaleMode.ScaleToFit);
                }

                string itemLabel = item != null ? $"[{item.itemId}] {item.itemName}" : $"ID: {idProp.intValue}";
                GUILayout.Label(itemLabel, GUILayout.MinWidth(120));

                if (GUILayout.Button("선택", EditorStyles.miniButton, GUILayout.Width(36)))
                {
                    _pickerRewardIndex = i;
                    _showItemPicker    = true;
                    _pickerSearch      = "";
                }

                GUILayout.Space(4);
                GUILayout.Label("수량", GUILayout.Width(30));
                cntProp.intValue = Mathf.Max(1, EditorGUILayout.IntField(cntProp.intValue, GUILayout.Width(50)));
                GUILayout.FlexibleSpace();

                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                    removeIdx = i;
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIdx >= 0) itemsProp.DeleteArrayElementAtIndex(removeIdx);

            EditorGUILayout.EndVertical();
        }

        // ── 설정 ────────────────────────────────────────────────

        private void DrawDetailSettings()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            GUILayout.Label("설정", EditorStyles.boldLabel);
            DrawProp("isRepeatable", "반복 퀘스트");
            DrawProp("autoComplete", "자동 완료");

            var ac = _serializedQuest.FindProperty("autoComplete");
            if (ac.boolValue)
                EditorGUILayout.HelpBox("목표 모두 달성 즉시 자동 완료됩니다.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        // ── 공통 헬퍼 ───────────────────────────────────────────

        private void DrawProp(string propName, string label)
        {
            var p = _serializedQuest.FindProperty(propName);
            if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label));
        }

        private void DrawElemProp(SerializedProperty parent, string propName, string label)
        {
            var p = parent.FindPropertyRelative(propName);
            if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label));
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 생성 팝업

        private void DrawCreatePopup()
        {
            Rect popupRect = new Rect(4, 22, 340f, 162f);
            GUI.Box(popupRect, GUIContent.none, "window");
            GUILayout.BeginArea(popupRect);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("새 퀘스트 생성", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
            { _showCreatePopup = false; EditorGUILayout.EndHorizontal(); GUILayout.EndArea(); return; }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quest ID", GUILayout.Width(72));
            _newQuestId = EditorGUILayout.TextField(_newQuestId);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("퀘스트 이름", GUILayout.Width(72));
            _newQuestName = EditorGUILayout.TextField(_newQuestName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("저장 경로", GUILayout.Width(72));
            _newSavePath = EditorGUILayout.TextField(_newSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string sel = EditorUtility.OpenFolderPanel("저장 폴더", _newSavePath, "");
                if (!string.IsNullOrEmpty(sel))
                {
                    string proj = Path.GetFullPath(Application.dataPath + "/..");
                    if (sel.StartsWith(proj))
                        _newSavePath = "Assets" + sel.Substring(proj.Length).Replace('\\', '/');
                }
            }
            EditorGUILayout.EndHorizontal();

            bool idDuplicate = _quests.Exists(q => q != null && q.questId == _newQuestId);
            if (idDuplicate)
                EditorGUILayout.HelpBox("이미 존재하는 Quest ID입니다.", MessageType.Warning);

            EditorGUILayout.Space(4);
            bool valid = !string.IsNullOrWhiteSpace(_newQuestId) &&
                         !string.IsNullOrWhiteSpace(_newQuestName) &&
                         !idDuplicate;
            GUI.enabled = valid;
            if (GUILayout.Button("생성", GUILayout.Height(24))) CreateNewQuest();
            GUI.enabled = true;

            GUILayout.EndArea();
        }

        private void CreateNewQuest()
        {
            if (!AssetDatabase.IsValidFolder(_newSavePath))
                Directory.CreateDirectory(_newSavePath);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{_newSavePath}/{_newQuestId}.asset");

            var q = ScriptableObject.CreateInstance<QuestSO>();
            q.questId   = _newQuestId;
            q.questName = _newQuestName;

            AssetDatabase.CreateAsset(q, assetPath);
            AssetDatabase.SaveAssets();

            _showCreatePopup = false;
            LoadAllQuests();

            int idx = GetFilteredQuests().FindIndex(x => x == q);
            if (idx >= 0) SelectIndex(idx, q);

            EditorGUIUtility.PingObject(q);
            Debug.Log($"[QuestEditor] 생성 완료: {assetPath}");
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 복제 / 삭제

        private void DuplicateSelected()
        {
            var filtered = GetFilteredQuests();
            if (_selectedIndex < 0 || _selectedIndex >= filtered.Count) return;

            var src     = filtered[_selectedIndex];
            string srcPath = AssetDatabase.GetAssetPath(src);
            string dir  = Path.GetDirectoryName(srcPath);
            string copy = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(dir, Path.GetFileName(srcPath)).Replace('\\', '/'));

            AssetDatabase.CopyAsset(srcPath, copy);
            AssetDatabase.SaveAssets();

            var newQ = AssetDatabase.LoadAssetAtPath<QuestSO>(copy);
            if (newQ != null)
            {
                newQ.questId = newQ.questId + "_copy";
                EditorUtility.SetDirty(newQ);
                AssetDatabase.SaveAssets();
            }

            LoadAllQuests();
            int idx = GetFilteredQuests().FindIndex(x => x == newQ);
            if (idx >= 0) SelectIndex(idx, newQ);
            EditorGUIUtility.PingObject(newQ);
        }

        private void DeleteSelected()
        {
            var filtered = GetFilteredQuests();
            if (_selectedIndex < 0 || _selectedIndex >= filtered.Count) return;

            var q    = filtered[_selectedIndex];
            string path = AssetDatabase.GetAssetPath(q);

            if (!EditorUtility.DisplayDialog("퀘스트 삭제",
                $"'{q.questName}' (ID: {q.questId})을 삭제합니다.\n이 작업은 되돌릴 수 없습니다.",
                "삭제", "취소"))
                return;

            _selectedIndex   = -1;
            _serializedQuest = null;

            AssetDatabase.DeleteAsset(path);
            LoadAllQuests();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region DB 갱신 / Enum 생성

        private void RefreshDatabase()
        {
            if (_questDb == null)
            {
                EditorUtility.DisplayDialog("QuestDatabase 없음",
                    "프로젝트에서 QuestDatabase asset을 찾을 수 없습니다.\n먼저 QuestDatabase를 생성하세요.",
                    "확인");
                return;
            }
            _questDb.RefreshDatabase(DEFAULT_QUEST_PATH);
            LoadAllQuests();
        }

        /// <summary>
        /// QuestDatabase에 등록된 퀘스트 ID로 QuestIdType enum 파일을 생성한다.
        /// 출력: Assets/02.Scripts/Data/Quest/QuestIdType.cs
        /// </summary>
        private void GenerateQuestIdEnum()
        {
            if (_questDb == null)
            {
                EditorUtility.DisplayDialog("QuestDatabase 없음",
                    "QuestDatabase asset을 찾을 수 없습니다.\nDB 갱신을 먼저 실행하세요.",
                    "확인");
                return;
            }

            const string outputPath = "Assets/02.Scripts/Data/Quest/QuestIdType.cs";
            var raw = new List<(string, string)>();
            foreach (var q in _questDb.QuestList)
            {
                if (q == null || string.IsNullOrEmpty(q.questId)) continue;
                raw.Add((q.questId, q.questId));
            }

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
            bool ok = IdEnumGeneratorUtility.GenerateStringKeyEnum(
                "QuestIdType", "ToQuestId", "Quest ID",
                outputPath, "UPlayGround.Data.Quest", entries);

            if (ok)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Enum 생성 완료",
                    $"QuestIdType 생성 완료 ({entries.Count}개)\n→ {outputPath}", "확인");
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 아이템 피커 팝업

        private void DrawItemPickerPopup()
        {
            // 보상 목록이 유효한지 확인
            if (_serializedQuest == null) { _showItemPicker = false; return; }
            var itemsProp = _serializedQuest.FindProperty("reward.items");
            if (itemsProp == null || _pickerRewardIndex < 0 || _pickerRewardIndex >= itemsProp.arraySize)
            { _showItemPicker = false; return; }

            Rect pickerRect = new Rect(
                LIST_PANEL_WIDTH + 4,
                position.height - 250,
                position.width - LIST_PANEL_WIDTH - 8,
                240);

            GUI.Box(pickerRect, GUIContent.none, "window");
            GUILayout.BeginArea(new Rect(pickerRect.x + 4, pickerRect.y + 4,
                pickerRect.width - 8, pickerRect.height - 8));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("보상 아이템 선택", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(20))) _showItemPicker = false;
            EditorGUILayout.EndHorizontal();

            _pickerSearch = EditorGUILayout.TextField(_pickerSearch, EditorStyles.toolbarSearchField);

            _pickerScroll = EditorGUILayout.BeginScrollView(_pickerScroll, GUILayout.Height(170));
            string lower = _pickerSearch.ToLower();

            foreach (var item in _allItems)
            {
                if (!string.IsNullOrEmpty(_pickerSearch) &&
                    !item.itemName.ToLower().Contains(lower) &&
                    !item.itemId.ToString().Contains(_pickerSearch))
                    continue;

                EditorGUILayout.BeginHorizontal();
                if (item.icon != null)
                {
                    var r = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                    GUI.DrawTexture(r, item.icon.texture, ScaleMode.ScaleToFit);
                }
                if (GUILayout.Button($"[{item.itemId}] {item.itemName}", EditorStyles.miniButton))
                {
                    _serializedQuest.Update();
                    itemsProp.GetArrayElementAtIndex(_pickerRewardIndex)
                        .FindPropertyRelative("itemId").intValue = item.itemId;
                    _serializedQuest.ApplyModifiedProperties();
                    _showItemPicker = false;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (Event.current.type == EventType.MouseDown && !pickerRect.Contains(Event.current.mousePosition))
            { _showItemPicker = false; Repaint(); }
        }

        #endregion
    }
}
#endif
