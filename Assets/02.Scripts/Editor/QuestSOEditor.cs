#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Item;
using UPlayGround.Editor.Authoring;

namespace UPlayGround.Editor
{
    /// <summary>
    /// QuestSO 인스펙터 커스텀 에디터.
    ///
    /// 기능:
    ///   - 목표(Objective) 타입별 컬러 코딩 및 관련 필드만 노출
    ///   - 타입별 연결 포인트 안내 툴팁
    ///   - 보상 항목 편집
    ///   - Quest Editor 창 바로 열기
    /// </summary>
    [CustomEditor(typeof(QuestSO))]
    public class QuestSOEditor : UnityEditor.Editor
    {
        // ──── 프로퍼티 캐시 ────
        private SerializedProperty _questId;
        private SerializedProperty _questName;
        private SerializedProperty _questType;
        private SerializedProperty _shortSummary;
        private SerializedProperty _questDescription;
        private SerializedProperty _requiredQuestIds;
        private SerializedProperty _requiredStoryProgress;
        private SerializedProperty _autoAcceptOnNewGame;
        private SerializedProperty _autoAcceptNextQuestIds;
        private SerializedProperty _objectives;
        private SerializedProperty _reward;
        private SerializedProperty _isRepeatable;
        private SerializedProperty _autoComplete;
        private SerializedProperty _suppressCompletionPresentation;

        // ──── 아이템 피커 ────
        private bool              _showItemPicker      = false;
        private int               _pickerRewardIndex   = -1;
        private string            _pickerSearch        = "";
        private Vector2           _pickerScroll;
        private List<ItemSO>      _allItems            = new List<ItemSO>();

        // ──── 스타일 ────
        private bool      _stylesReady;
        private GUIStyle  _sectionStyle;
        private GUIStyle  _objectiveBoxStyle;
        private GUIStyle  _badgeStyle;

        // ──── 목표 타입별 색상 ────
        private static readonly Color[] ObjectiveColors =
        {
            new Color(0.35f, 0.75f, 0.35f),  // ItemCollect
            new Color(0.35f, 0.65f, 0.95f),  // ItemDeliver
            new Color(0.90f, 0.75f, 0.20f),  // ItemUse
            new Color(0.90f, 0.35f, 0.35f),  // MonsterKill
            new Color(0.75f, 0.45f, 0.90f),  // StoryProgress
            new Color(0.95f, 0.60f, 0.20f),  // ItemCraft
            new Color(0.30f, 0.80f, 0.80f),  // ItemEnhance
            new Color(0.80f, 0.80f, 0.80f),  // ReachLocation
        };

        private static readonly string[] ObjectiveLabels =
        {
            "아이템 수집", "아이템 전달", "아이템 사용", "몬스터 처치",
            "스토리 진행", "아이템 제작", "아이템 강화", "위치 도달",
        };

        private static readonly string[] ObjectiveHints =
        {
            "QuestManager.NotifyItemCollected(itemId, count)",
            "QuestManager.NotifyItemDelivered(npcId, itemId, count)",
            "QuestManager.NotifyItemUsed(itemId, count)",
            "QuestManager.NotifyMonsterKill(actorId)",
            "QuestManager.NotifyStoryProgress(progress)",
            "QuestManager.NotifyItemCrafted(recipeId, quantity)",
            "QuestManager.NotifyItemEnhanced(itemId)",
            "QuestManager.NotifyLocationReached(locationId)",
        };

        private void OnEnable()
        {
            _questId               = serializedObject.FindProperty("questId");
            _questName             = serializedObject.FindProperty("questName");
            _questType             = serializedObject.FindProperty("questType");
            _shortSummary          = serializedObject.FindProperty("shortSummary");
            _questDescription      = serializedObject.FindProperty("questDescription");
            _requiredQuestIds      = serializedObject.FindProperty("requiredQuestIds");
            _requiredStoryProgress = serializedObject.FindProperty("requiredStoryProgress");
            _autoAcceptOnNewGame   = serializedObject.FindProperty("autoAcceptOnNewGame");
            _autoAcceptNextQuestIds = serializedObject.FindProperty("autoAcceptNextQuestIds");
            _objectives            = serializedObject.FindProperty("objectives");
            _reward                = serializedObject.FindProperty("reward");
            _isRepeatable          = serializedObject.FindProperty("isRepeatable");
            _autoComplete          = serializedObject.FindProperty("autoComplete");
            _suppressCompletionPresentation = serializedObject.FindProperty("suppressCompletionPresentation");

            LoadAllItems();
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

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _sectionStyle = new GUIStyle("helpBox") { padding = new RectOffset(8, 8, 6, 6) };

            _objectiveBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin  = new RectOffset(0, 0, 3, 3),
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
            };
        }

        // ──────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawQuestHeader();
            EditorGUILayout.Space(4);
            DrawBasicInfo();
            EditorGUILayout.Space(4);
            DrawPrerequisites();
            EditorGUILayout.Space(4);
            DrawAutoAcceptNextQuests();
            EditorGUILayout.Space(4);
            DrawObjectives();
            EditorGUILayout.Space(4);
            DrawReward();
            EditorGUILayout.Space(4);
            DrawSettings();
            EditorGUILayout.Space(8);
            DrawFooter();

            serializedObject.ApplyModifiedProperties();

            if (_showItemPicker)
                DrawItemPickerPopup();
        }

        // ── 헤더 ─────────────────────────────────────────────────

        private void DrawQuestHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"  Quest  [{_questId.stringValue}]", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Quest Editor", EditorStyles.toolbarButton, GUILayout.Width(90)))
                DataAuthoringHubWindow.Open(QuestDomainPanel.DomainKey);
            EditorGUILayout.EndHorizontal();
        }

        // ── 기본 정보 ────────────────────────────────────────────

        private void DrawBasicInfo()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_questId,          new GUIContent("퀘스트 ID"));
            EditorGUILayout.PropertyField(_questName,        new GUIContent("퀘스트 이름"));
            EditorGUILayout.PropertyField(_questType,        new GUIContent("분류(메인/서브)"));
            EditorGUILayout.PropertyField(_shortSummary,     new GUIContent("짧은 부제"));
            EditorGUILayout.PropertyField(_questDescription, new GUIContent("설명"));
            EditorGUILayout.EndVertical();
        }

        // ── 선행 조건 ────────────────────────────────────────────

        private void DrawPrerequisites()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("선행 조건", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_requiredQuestIds,      new GUIContent("완료 필요 퀘스트"));
            EditorGUILayout.PropertyField(_requiredStoryProgress, new GUIContent("필요 스토리 진행도"));
            EditorGUILayout.EndVertical();
        }

        private void DrawAutoAcceptNextQuests()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("자동 연계", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoAcceptOnNewGame, new GUIContent("새 게임 시작 시 자동 수락"));
            EditorGUILayout.PropertyField(_autoAcceptNextQuestIds, new GUIContent("완료 후 자동 수락 퀘스트 ID"));
            EditorGUILayout.EndVertical();
        }

        // ── 목표 목록 ────────────────────────────────────────────

        private void DrawObjectives()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"목표  ({_objectives.arraySize}개)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 추가", EditorStyles.miniButton, GUILayout.Width(50)))
                AddObjective();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            int removeIndex = -1;
            for (int i = 0; i < _objectives.arraySize; i++)
            {
                if (DrawObjectiveElement(i))
                    removeIndex = i;
            }

            if (removeIndex >= 0)
                _objectives.DeleteArrayElementAtIndex(removeIndex);

            if (_objectives.arraySize == 0)
            {
                EditorGUILayout.HelpBox("목표가 없습니다. + 추가 버튼으로 목표를 등록하세요.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        /// <returns>삭제 요청이면 true</returns>
        private bool DrawObjectiveElement(int index)
        {
            var elem    = _objectives.GetArrayElementAtIndex(index);
            var typeProp = elem.FindPropertyRelative("type");
            var typeVal  = (QuestObjectiveType)typeProp.enumValueIndex;
            int typeIdx  = (int)typeVal;

            Color bgColor = typeIdx < ObjectiveColors.Length
                ? ObjectiveColors[typeIdx] : Color.gray;

            // ── 카드 배경 ────────────────────────────────────────
            var savedBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor * 0.5f + Color.white * 0.5f;
            EditorGUILayout.BeginVertical(_objectiveBoxStyle);
            GUI.backgroundColor = savedBg;

            // 헤더 행
            EditorGUILayout.BeginHorizontal();

            // 타입 뱃지
            var badgeRect = GUILayoutUtility.GetRect(72, 18, GUILayout.Width(72), GUILayout.Height(18));
            EditorGUI.DrawRect(badgeRect, bgColor);
            string label = typeIdx < ObjectiveLabels.Length ? ObjectiveLabels[typeIdx] : typeVal.ToString();
            GUI.Label(badgeRect, label, _badgeStyle);

            GUILayout.Space(4);

            // 목표 설명 미리보기
            var descProp = elem.FindPropertyRelative("description");
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label(string.IsNullOrEmpty(descProp.stringValue) ? "—" : descProp.stringValue,
                EditorStyles.miniLabel);
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            // 위/아래 이동
            GUI.enabled = index > 0;
            if (GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Width(20)))
                _objectives.MoveArrayElement(index, index - 1);
            GUI.enabled = index < _objectives.arraySize - 1;
            if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(20)))
                _objectives.MoveArrayElement(index, index + 1);
            GUI.enabled = true;

            // 삭제
            GUI.color = new Color(1f, 0.5f, 0.5f);
            bool remove = GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);

            // ── 공통 필드 ────────────────────────────────────────
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(elem.FindPropertyRelative("objectiveId"),  new GUIContent("목표 ID"));
            EditorGUILayout.PropertyField(descProp,                                   new GUIContent("설명"));
            EditorGUILayout.PropertyField(typeProp,                                   new GUIContent("타입"));

            // ── 타입별 조건부 필드 ────────────────────────────────
            typeVal = (QuestObjectiveType)typeProp.enumValueIndex; // 타입 변경 반영
            typeIdx = (int)typeVal;

            switch (typeVal)
            {
                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemUse:
                case QuestObjectiveType.ItemEnhance:
                    DrawIntField(elem, "targetId",      "아이템 ID");
                    DrawIntField(elem, "requiredCount", "필요 수량");
                    break;

                case QuestObjectiveType.ItemDeliver:
                    DrawIntField(elem, "targetId",      "아이템 ID");
                    DrawIntField(elem, "npcId",         "NPC ID");
                    DrawIntField(elem, "requiredCount", "전달 수량");
                    break;

                case QuestObjectiveType.MonsterKill:
                    EditorGUILayout.PropertyField(
                        elem.FindPropertyRelative("targetStringId"), new GUIContent("Actor ID"));
                    DrawIntField(elem, "targetId", "레거시 숫자 ID");
                    DrawIntField(elem, "requiredCount", "처치 수");
                    break;

                case QuestObjectiveType.StoryProgress:
                    DrawIntField(elem, "targetId", "필요 진행도");
                    // StoryProgress는 1회 달성 (requiredCount 불필요)
                    break;

                case QuestObjectiveType.ItemCraft:
                    DrawIntField(elem, "targetId",      "레시피 ID");
                    DrawIntField(elem, "requiredCount", "제작 횟수");
                    break;

                case QuestObjectiveType.ReachLocation:
                    EditorGUILayout.PropertyField(
                        elem.FindPropertyRelative("targetStringId"), new GUIContent("위치 ID"));
                    // 도달은 1회 달성
                    break;

            }

            // ── 마커 표시 ────────────────────────────────────────
            EditorGUILayout.PropertyField(
                elem.FindPropertyRelative("markerLocationId"), new GUIContent("마커 위치 ID"));
            EditorGUILayout.PropertyField(
                elem.FindPropertyRelative("markerIntent"), new GUIContent("마커 성격"));

            EditorGUI.indentLevel--;

            // ── 연결 포인트 힌트 ─────────────────────────────────
            string hint = typeIdx < ObjectiveHints.Length ? ObjectiveHints[typeIdx] : "";
            EditorGUILayout.LabelField($"▶ {hint}", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndVertical();
            return remove;
        }

        private void DrawIntField(SerializedProperty parent, string propName, string label)
        {
            EditorGUILayout.PropertyField(parent.FindPropertyRelative(propName), new GUIContent(label));
        }

        private void AddObjective()
        {
            _objectives.InsertArrayElementAtIndex(_objectives.arraySize);
            var newElem = _objectives.GetArrayElementAtIndex(_objectives.arraySize - 1);
            newElem.FindPropertyRelative("objectiveId").stringValue  = $"obj_{_objectives.arraySize}";
            newElem.FindPropertyRelative("description").stringValue  = "";
            newElem.FindPropertyRelative("type").enumValueIndex      = 0;
            newElem.FindPropertyRelative("targetId").intValue        = 0;
            newElem.FindPropertyRelative("npcId").intValue           = 0;
            newElem.FindPropertyRelative("targetStringId").stringValue = "";
            newElem.FindPropertyRelative("requiredCount").intValue   = 1;
        }

        // ── 보상 ─────────────────────────────────────────────────

        private void DrawReward()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("보상", EditorStyles.boldLabel);

            var goldProp  = _reward.FindPropertyRelative("gold");
            var expProp   = _reward.FindPropertyRelative("exp");
            var itemsProp = _reward.FindPropertyRelative("items");

            EditorGUILayout.PropertyField(goldProp, new GUIContent("골드"));
            EditorGUILayout.PropertyField(expProp, new GUIContent("경험치"));

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"보상 아이템  ({itemsProp.arraySize}개)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ 추가", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
                var newEl = itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1);
                newEl.FindPropertyRelative("itemId").intValue = 0;
                newEl.FindPropertyRelative("count").intValue  = 1;
            }
            EditorGUILayout.EndHorizontal();

            int removeIdx = -1;
            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                var el       = itemsProp.GetArrayElementAtIndex(i);
                var idProp   = el.FindPropertyRelative("itemId");
                var cntProp  = el.FindPropertyRelative("count");

                var item = _allItems.Find(x => x.itemId == idProp.intValue);

                EditorGUILayout.BeginHorizontal("box");

                // 아이콘
                if (item?.icon != null)
                {
                    var r = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                    GUI.DrawTexture(r, item.icon.texture, ScaleMode.ScaleToFit);
                }

                // 아이템 이름 표시 + 피커
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

            if (removeIdx >= 0)
                itemsProp.DeleteArrayElementAtIndex(removeIdx);

            EditorGUILayout.EndVertical();
        }

        // ── 설정 ─────────────────────────────────────────────────

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_isRepeatable, new GUIContent("반복 퀘스트"));
            EditorGUILayout.PropertyField(_autoComplete, new GUIContent("자동 완료"));
            EditorGUILayout.PropertyField(
                _suppressCompletionPresentation,
                new GUIContent("완료 연출 생략"));
            if (_autoComplete.boolValue)
                EditorGUILayout.HelpBox("모든 목표 달성 즉시 완료 처리됩니다. (UI 확인 불필요)", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        // ── 하단 버튼 ────────────────────────────────────────────

        private void DrawFooter()
        {
            if (GUILayout.Button("Quest Editor에서 열기", GUILayout.Height(26)))
                DataAuthoringHubWindow.Open(QuestDomainPanel.DomainKey, target);
        }

        // ── 아이템 피커 팝업 ──────────────────────────────────────

        private void DrawItemPickerPopup()
        {
            var itemsProp = _reward.FindPropertyRelative("items");
            if (_pickerRewardIndex < 0 || _pickerRewardIndex >= itemsProp.arraySize)
            {
                _showItemPicker = false;
                return;
            }

            Rect pickerRect = new Rect(0,
                GUILayoutUtility.GetLastRect().yMax - 200,
                EditorGUIUtility.currentViewWidth, 220);

            GUI.Box(pickerRect, GUIContent.none, "window");
            GUILayout.BeginArea(new Rect(pickerRect.x + 4, pickerRect.y + 4,
                pickerRect.width - 8, pickerRect.height - 8));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("아이템 검색", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(20)))
                _showItemPicker = false;
            EditorGUILayout.EndHorizontal();

            _pickerSearch = EditorGUILayout.TextField(_pickerSearch, EditorStyles.toolbarSearchField);

            _pickerScroll = EditorGUILayout.BeginScrollView(_pickerScroll, GUILayout.Height(150));
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
                    serializedObject.Update();
                    itemsProp.GetArrayElementAtIndex(_pickerRewardIndex)
                        .FindPropertyRelative("itemId").intValue = item.itemId;
                    serializedObject.ApplyModifiedProperties();
                    _showItemPicker = false;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();

            if (Event.current.type == EventType.MouseDown && !pickerRect.Contains(Event.current.mousePosition))
            {
                _showItemPicker = false;
                Repaint();
            }
        }
    }
}
#endif
