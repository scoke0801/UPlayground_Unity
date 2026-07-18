#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Quest;
using UPlayGround.Tool.Editor;
using UPlayGround.Data.Item;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 퀘스트 비주얼 에디터 윈도우 (UIToolkit).
    /// 메뉴: UPlayGround / 게임플레이 / 퀘스트 / 퀘스트 에디터
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
        private List<QuestSO>   _filtered     = new List<QuestSO>();
        private QuestDatabase   _questDb;
        private HashSet<string> _duplicateIds = new HashSet<string>();
        private List<ItemSO>    _allItems     = new List<ItemSO>();

        // ──── 선택 / 필터 ────
        private QuestSO _selected;
        private string  _searchText = "";
        private int     _filterTab  = 0;  // 0=전체, 1=반복, 2=자동완료

        // ──── 생성 팝업 상태 ────
        private string _newQuestId   = "quest_new";
        private string _newQuestName = "새 퀘스트";
        private string _newSavePath  = DEFAULT_QUEST_PATH;

        // ──── UI 요소 ────
        private ListView      _listView;
        private Label         _countLabel;
        private VisualElement _detailPane;
        private VisualElement _createPopup;
        private VisualElement _itemPickerPopup;
        private ToolbarButton _duplicateButton;
        private ToolbarButton _deleteButton;
        private readonly List<ToolbarToggle> _filterToggles = new List<ToolbarToggle>();
        private QuestSO _pendingSelect;

        // ──── 상수 ────
        private const float  LIST_PANEL_WIDTH   = 300f;
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
        #region 데이터 로드

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

        #endregion

        // ──────────────────────────────────────────────────────────
        #region UI 구성

        private void CreateGUI()
        {
            LoadAllQuests();
            LoadQuestDatabase();
            LoadAllItems();

            var root = rootVisualElement;
            root.Clear();

            root.Add(BuildToolbar());

            var body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            body.Add(BuildListPanel());
            _detailPane = new VisualElement { style = { flexGrow = 1 } };
            body.Add(_detailPane);
            root.Add(body);

            _createPopup = BuildCreatePopup();
            root.Add(_createPopup);

            RefreshList();

            if (_pendingSelect != null)
            {
                SelectQuest(_pendingSelect);
                _pendingSelect = null;
            }
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new ToolbarButton(ToggleCreatePopup) { text = "+ 새 퀘스트" });

            _duplicateButton = new ToolbarButton(DuplicateSelected) { text = "복제" };
            toolbar.Add(_duplicateButton);

            _deleteButton = new ToolbarButton(DeleteSelected) { text = "삭제" };
            _deleteButton.style.color = new Color(1f, 0.5f, 0.5f);
            toolbar.Add(_deleteButton);

            toolbar.Add(new ToolbarSpacer());

            var tabs = new[] { "전체", "반복", "자동완료" };
            _filterToggles.Clear();
            for (int i = 0; i < tabs.Length; i++)
            {
                int captured = i;
                var toggle = new ToolbarToggle { text = tabs[i], value = _filterTab == i };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (!evt.newValue)
                    {
                        toggle.SetValueWithoutNotify(_filterTab == captured);
                        return;
                    }
                    _filterTab = captured;
                    foreach (var t in _filterToggles)
                        t.SetValueWithoutNotify(t == toggle);
                    RefreshList();
                });
                _filterToggles.Add(toggle);
                toolbar.Add(toggle);
            }

            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            var search = new ToolbarSearchField { style = { width = 180 } };
            search.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshList();
            });
            toolbar.Add(search);

            toolbar.Add(new ToolbarButton(RefreshDatabase) { text = "DB 갱신" });
            toolbar.Add(new ToolbarButton(GenerateQuestIdEnum) { text = "Enum 생성" });
            toolbar.Add(new ToolbarButton(() =>
            {
                LoadAllQuests();
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
                fixedItemHeight = 52,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                makeItem = MakeListRow,
                bindItem = BindListRow,
            };
            _listView.selectionChanged += _ =>
            {
                _selected = _listView.selectedItem as QuestSO;
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

            // 목표 수 뱃지
            row.Add(new Label
            {
                name = "badge",
                style =
                {
                    width = 28, height = 22, flexShrink = 0, marginRight = 6,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    color = Color.white, unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            });

            var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };

            var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            nameRow.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold } });
            nameRow.Add(new Label("⚠ ID중복")
            {
                name = "dup",
                style = { color = new Color(1f, 0.4f, 0.4f), fontSize = 10, marginLeft = 4 }
            });
            info.Add(nameRow);

            info.Add(new Label { name = "tags", style = { color = new Color(0.6f, 0.6f, 0.6f), fontSize = 10 } });
            row.Add(info);

            return row;
        }

        private void BindListRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _filtered.Count) return;
            var q = _filtered[index];
            if (q == null) return;

            row.Q<Label>("badge").text = q.objectives?.Count.ToString() ?? "0";
            row.Q<Label>("name").text = string.IsNullOrEmpty(q.questName) ? "(이름 없음)" : q.questName;
            row.Q<Label>("dup").style.display =
                _duplicateIds.Contains(q.questId) ? DisplayStyle.Flex : DisplayStyle.None;

            var tags = new List<string> { q.questId };
            if (q.isRepeatable) tags.Add("반복");
            if (q.autoComplete) tags.Add("자동완료");
            if (q.requiredQuestIds?.Count > 0) tags.Add($"선행{q.requiredQuestIds.Count}");
            if (q.autoAcceptNextQuestIds?.Count > 0) tags.Add($"연계{q.autoAcceptNextQuestIds.Count}");
            row.Q<Label>("tags").text = string.Join("  |  ", tags);
        }

        private void RefreshList(bool rebuildDetail = true)
        {
            _filtered = GetFilteredQuests();
            _listView.itemsSource = _filtered;
            _listView.RefreshItems();
            _countLabel.text = $"퀘스트  ({_filtered.Count}/{_quests.Count})";

            int idx = _selected != null ? _filtered.IndexOf(_selected) : -1;
            _listView.SetSelectionWithoutNotify(idx >= 0 ? new[] { idx } : System.Array.Empty<int>());
            bool selectionCleared = _selected != null && idx < 0;
            if (selectionCleared)
                _selected = null;
            if (rebuildDetail || selectionCleared)
                RebuildDetail();
            UpdateSelectionButtons();
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

        private void SelectQuest(QuestSO q)
        {
            if (_listView == null)
            {
                _pendingSelect = q;
                return;
            }

            LoadAllQuests();
            _selected = q;
            RefreshList();

            int idx = _filtered.IndexOf(q);
            if (idx >= 0) _listView.ScrollToItem(idx);
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

        private void RebuildDetail()
        {
            CloseItemPicker();
            _detailPane.Clear();
            _detailPane.Unbind();

            if (_selected == null)
            {
                _detailPane.Add(MakeCenteredHint("← 퀘스트를 선택하세요"));
                return;
            }

            var q  = _selected;
            var so = new SerializedObject(q);

            // ── 상단 헤더 ────────────────────────────────────────
            var header = new Toolbar();
            header.Add(new Label($"  {q.questName}")
            {
                name = "detail-title",
                style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new Label(AssetDatabase.GetAssetPath(q))
            {
                style = { color = new Color(0.55f, 0.55f, 0.55f), fontSize = 10, unityTextAlign = TextAnchor.MiddleRight }
            });
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(q)) { text = "↗ 에셋 열기" });
            _detailPane.Add(header);

            var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 6, paddingRight = 6, paddingTop = 4 } };
            _detailPane.Add(scroll);

            // ID 중복 경고
            var dupWarning = new HelpBox("", HelpBoxMessageType.Error);
            scroll.Add(dupWarning);

            // ── 기본 정보 ────────────────────────────────────────
            var basic = MakeSection("기본 정보");
            basic.Add(new PropertyField { bindingPath = "questId",          label = "퀘스트 ID" });
            basic.Add(new PropertyField { bindingPath = "questName",        label = "퀘스트 이름" });
            basic.Add(new PropertyField { bindingPath = "questType",        label = "분류(메인/서브)" });
            basic.Add(new PropertyField { bindingPath = "shortSummary",     label = "짧은 부제" });
            basic.Add(new PropertyField { bindingPath = "questDescription", label = "설명" });
            scroll.Add(basic);

            // ── 선행 조건 ────────────────────────────────────────
            var prereq = MakeSection("선행 조건");
            prereq.Add(new PropertyField { bindingPath = "requiredQuestIds",      label = "완료 필요 퀘스트 ID" });
            prereq.Add(new PropertyField { bindingPath = "requiredStoryProgress", label = "필요 스토리 진행도" });
            scroll.Add(prereq);

            // ── 자동 연계 ────────────────────────────────────────
            var autoLink = MakeSection("자동 연계");
            autoLink.Add(new PropertyField { bindingPath = "autoAcceptOnNewGame",    label = "새 게임 시작 시 자동 수락" });
            autoLink.Add(new PropertyField { bindingPath = "autoAcceptNextQuestIds", label = "완료 후 자동 수락 퀘스트 ID" });
            scroll.Add(autoLink);

            // ── 목표 ────────────────────────────────────────────
            var objSection = MakeSection("목표");
            var objHeader  = objSection.Q<Label>(className: "section-title");
            var objList    = new VisualElement();
            var objAddRow  = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            objAddRow.Add(new Button(() =>
            {
                so.Update();
                var objsProp = so.FindProperty("objectives");
                AddNewObjective(objsProp);
                so.ApplyModifiedProperties();
                RebuildObjectiveCards(so, objList, objHeader);
                _listView.RefreshItems();
            }) { text = "+ 추가" });
            objSection.Insert(1, objAddRow);
            objSection.Add(objList);
            scroll.Add(objSection);
            RebuildObjectiveCards(so, objList, objHeader);

            // ── 보상 ────────────────────────────────────────────
            var rewardSection = MakeSection("보상");
            rewardSection.Add(new PropertyField { bindingPath = "reward.gold", label = "골드" });
            rewardSection.Add(new PropertyField { bindingPath = "reward.exp",  label = "경험치" });

            var rewardHeaderRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 }
            };
            var rewardCountLabel = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            rewardHeaderRow.Add(rewardCountLabel);
            rewardHeaderRow.Add(new VisualElement { style = { flexGrow = 1 } });
            var rewardItemsList = new VisualElement();
            rewardHeaderRow.Add(new Button(() =>
            {
                so.Update();
                var itemsProp = so.FindProperty("reward.items");
                itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
                var el = itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1);
                el.FindPropertyRelative("itemId").intValue = 0;
                el.FindPropertyRelative("count").intValue  = 1;
                so.ApplyModifiedProperties();
                RebuildRewardItems(so, rewardItemsList, rewardCountLabel);
            }) { text = "+ 추가" });
            rewardSection.Add(rewardHeaderRow);
            rewardSection.Add(rewardItemsList);
            scroll.Add(rewardSection);
            RebuildRewardItems(so, rewardItemsList, rewardCountLabel);

            // ── 설정 ────────────────────────────────────────────
            var settings = MakeSection("설정");
            settings.Add(new PropertyField { bindingPath = "isRepeatable", label = "반복 퀘스트" });
            settings.Add(new PropertyField { bindingPath = "autoComplete", label = "자동 완료" });
            var autoCompleteInfo = new HelpBox("목표 모두 달성 즉시 자동 완료됩니다.", HelpBoxMessageType.Info);
            settings.Add(autoCompleteInfo);
            scroll.Add(settings);

            var acProp = so.FindProperty("autoComplete");
            void UpdateAcInfo(SerializedProperty p) =>
                autoCompleteInfo.style.display = p.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateAcInfo(acProp);
            settings.TrackPropertyValue(acProp, UpdateAcInfo);

            // 값 변경 추적: 중복 재계산 + 목록/헤더 갱신
            void RefreshDupWarning()
            {
                bool dup = _duplicateIds.Contains(q.questId);
                dupWarning.text = $"Quest ID '{q.questId}'가 다른 퀘스트와 중복됩니다.";
                dupWarning.style.display = dup ? DisplayStyle.Flex : DisplayStyle.None;
            }
            RefreshDupWarning();

            _detailPane.TrackSerializedObjectValue(so, _ =>
            {
                RebuildDuplicateSet();
                RefreshDupWarning();
                _detailPane.Q<Label>("detail-title").text = $"  {q.questName}";
                RefreshList(false);
            });

            _detailPane.Bind(so);
        }

        // ── 목표 카드 ───────────────────────────────────────────

        private void RebuildObjectiveCards(SerializedObject so, VisualElement objList, Label headerLabel)
        {
            so.Update();
            objList.Clear();

            var objsProp = so.FindProperty("objectives");
            if (headerLabel != null)
                headerLabel.text = $"목표  ({objsProp.arraySize}개)";

            if (objsProp.arraySize == 0)
            {
                objList.Add(new HelpBox("목표가 없습니다.", HelpBoxMessageType.Info));
                return;
            }

            for (int i = 0; i < objsProp.arraySize; i++)
                objList.Add(MakeObjectiveCard(so, objList, headerLabel, i));

            objList.Bind(so);
        }

        private VisualElement MakeObjectiveCard(SerializedObject so, VisualElement objList, Label headerLabel, int index)
        {
            var objsProp = so.FindProperty("objectives");
            var elem     = objsProp.GetArrayElementAtIndex(index);
            string path  = elem.propertyPath;
            var typeProp = elem.FindPropertyRelative("type");
            int typeIdx  = typeProp.enumValueIndex;
            Color bg     = typeIdx >= 0 && typeIdx < ObjColors.Length ? ObjColors[typeIdx] : Color.gray;

            var card = new VisualElement
            {
                style =
                {
                    marginTop = 3, marginBottom = 3,
                    paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    backgroundColor = new Color(bg.r, bg.g, bg.b, 0.12f),
                    borderLeftWidth = 3, borderLeftColor = bg,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                }
            };

            // ── 헤더 행 ─────────────────────────────────────────
            var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var badge = new Label(typeIdx >= 0 && typeIdx < ObjLabels.Length ? ObjLabels[typeIdx] : ((QuestObjectiveType)typeIdx).ToString())
            {
                style =
                {
                    width = 72, height = 18, backgroundColor = bg,
                    color = Color.white, unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter, fontSize = 10, marginRight = 4,
                }
            };
            headerRow.Add(badge);

            var descLabel = new Label { style = { color = new Color(0.75f, 0.75f, 0.75f), fontSize = 10 } };
            var descProp  = elem.FindPropertyRelative("description");
            descLabel.text = string.IsNullOrEmpty(descProp.stringValue) ? "—" : descProp.stringValue;
            card.TrackPropertyValue(descProp, p =>
                descLabel.text = string.IsNullOrEmpty(p.stringValue) ? "—" : p.stringValue);
            headerRow.Add(descLabel);

            headerRow.Add(new VisualElement { style = { flexGrow = 1 } });

            void ApplyAndRebuild()
            {
                so.ApplyModifiedProperties();
                RebuildObjectiveCards(so, objList, headerLabel);
                _listView.RefreshItems();
            }

            var upBtn = new Button(() =>
            {
                so.Update();
                so.FindProperty("objectives").MoveArrayElement(index, index - 1);
                ApplyAndRebuild();
            }) { text = "▲", style = { width = 22 } };
            upBtn.SetEnabled(index > 0);
            headerRow.Add(upBtn);

            var downBtn = new Button(() =>
            {
                so.Update();
                so.FindProperty("objectives").MoveArrayElement(index, index + 1);
                ApplyAndRebuild();
            }) { text = "▼", style = { width = 22 } };
            downBtn.SetEnabled(index < objsProp.arraySize - 1);
            headerRow.Add(downBtn);

            var removeBtn = new Button(() =>
            {
                so.Update();
                so.FindProperty("objectives").DeleteArrayElementAtIndex(index);
                ApplyAndRebuild();
            }) { text = "✕", style = { width = 22, color = new Color(1f, 0.5f, 0.5f) } };
            headerRow.Add(removeBtn);

            card.Add(headerRow);

            // ── 편집 필드 ────────────────────────────────────────
            card.Add(new PropertyField { bindingPath = $"{path}.objectiveId", label = "목표 ID" });
            card.Add(new PropertyField { bindingPath = $"{path}.description", label = "설명" });
            card.Add(new PropertyField { bindingPath = $"{path}.type",        label = "타입" });

            // 타입별 조건부 필드
            var conditional = new VisualElement();
            card.Add(conditional);
            BuildObjectiveConditionalFields(conditional, path, (QuestObjectiveType)typeIdx);
            card.TrackPropertyValue(typeProp, _ =>
            {
                // 타입 변경 시 카드 전체(뱃지 색상 포함) 재구축 — 콜백 내 제거를 피하려고 지연 실행
                objList.schedule.Execute(() => RebuildObjectiveCards(so, objList, headerLabel));
            });

            // 연결 포인트 힌트
            string hint = typeIdx >= 0 && typeIdx < ObjHints.Length ? ObjHints[typeIdx] : "";
            card.Add(new Label($"▶ QuestManager.Instance.{hint}")
            {
                style = { color = new Color(0.55f, 0.55f, 0.55f), fontSize = 10, marginTop = 2 }
            });

            return card;
        }

        private static void BuildObjectiveConditionalFields(VisualElement container, string path, QuestObjectiveType type)
        {
            container.Clear();
            switch (type)
            {
                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemUse:
                case QuestObjectiveType.ItemEnhance:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetId",      label = "아이템 ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.requiredCount", label = "필요 수량" });
                    break;
                case QuestObjectiveType.ItemDeliver:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetId",      label = "아이템 ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.npcId",         label = "NPC ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.requiredCount", label = "전달 수량" });
                    break;
                case QuestObjectiveType.MonsterKill:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetStringId", label = "Actor ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.targetId",       label = "레거시 숫자 ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.requiredCount",  label = "처치 수" });
                    break;
                case QuestObjectiveType.StoryProgress:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetId", label = "필요 진행도" });
                    break;
                case QuestObjectiveType.ItemCraft:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetId",      label = "레시피 ID" });
                    container.Add(new PropertyField { bindingPath = $"{path}.requiredCount", label = "제작 횟수" });
                    break;
                case QuestObjectiveType.ReachLocation:
                    container.Add(new PropertyField { bindingPath = $"{path}.targetStringId", label = "위치 ID" });
                    break;
            }
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

        // ── 보상 아이템 ─────────────────────────────────────────

        private void RebuildRewardItems(SerializedObject so, VisualElement listRoot, Label countLabel)
        {
            so.Update();
            listRoot.Clear();

            var itemsProp = so.FindProperty("reward.items");
            countLabel.text = $"보상 아이템  ({itemsProp.arraySize}개)";

            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                int captured = i;
                var el       = itemsProp.GetArrayElementAtIndex(i);
                var idProp   = el.FindPropertyRelative("itemId");
                var item     = _allItems.Find(x => x.itemId == idProp.intValue);

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, alignItems = Align.Center,
                        marginTop = 1, paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2,
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f),
                    }
                };

                var icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    sprite = item != null ? item.icon : null,
                    style = { width = 24, height = 24, flexShrink = 0, marginRight = 4 }
                };
                row.Add(icon);

                row.Add(new Label(item != null ? $"[{item.itemId}] {item.itemName}" : $"ID: {idProp.intValue}")
                {
                    style = { minWidth = 120 }
                });

                row.Add(new Button(() =>
                {
                    OpenItemPicker(pickedId =>
                    {
                        so.Update();
                        var items = so.FindProperty("reward.items");
                        if (captured < items.arraySize)
                            items.GetArrayElementAtIndex(captured).FindPropertyRelative("itemId").intValue = pickedId;
                        so.ApplyModifiedProperties();
                        RebuildRewardItems(so, listRoot, countLabel);
                    });
                }) { text = "선택" });

                row.Add(new Label("수량") { style = { marginLeft = 4, marginRight = 2 } });
                var countField = new IntegerField { value = el.FindPropertyRelative("count").intValue, style = { width = 50 } };
                countField.RegisterValueChangedCallback(evt =>
                {
                    int v = Mathf.Max(1, evt.newValue);
                    countField.SetValueWithoutNotify(v);
                    so.Update();
                    var items = so.FindProperty("reward.items");
                    if (captured < items.arraySize)
                        items.GetArrayElementAtIndex(captured).FindPropertyRelative("count").intValue = v;
                    so.ApplyModifiedProperties();
                });
                row.Add(countField);

                row.Add(new VisualElement { style = { flexGrow = 1 } });

                row.Add(new Button(() =>
                {
                    so.Update();
                    var items = so.FindProperty("reward.items");
                    if (captured < items.arraySize)
                        items.DeleteArrayElementAtIndex(captured);
                    so.ApplyModifiedProperties();
                    RebuildRewardItems(so, listRoot, countLabel);
                }) { text = "✕", style = { width = 22, color = new Color(1f, 0.5f, 0.5f) } });

                listRoot.Add(row);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공통 UI 헬퍼

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginTop = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f),
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = new Color(0f, 0f, 0f, 0.25f), borderRightColor = new Color(0f, 0f, 0f, 0.25f),
                    borderTopColor = new Color(0f, 0f, 0f, 0.25f), borderBottomColor = new Color(0f, 0f, 0f, 0.25f),
                }
            };
            var titleLabel = new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } };
            titleLabel.AddToClassList("section-title");
            section.Add(titleLabel);
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
        #region 생성 팝업

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
                    position = Position.Absolute, left = 4, top = 22, width = 360,
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
            header.Add(new Label("새 퀘스트 생성")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new ToolbarButton(() => popup.style.display = DisplayStyle.None) { text = "✕" });
            popup.Add(header);

            var idField = new TextField("Quest ID") { value = _newQuestId, style = { marginTop = 4 } };
            popup.Add(idField);

            var nameField = new TextField("퀘스트 이름") { value = _newQuestName };
            popup.Add(nameField);

            var pathRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var pathField = new TextField("저장 경로") { value = _newSavePath, style = { flexGrow = 1 } };
            pathRow.Add(pathField);
            pathRow.Add(new Button(() =>
            {
                string sel = EditorUtility.OpenFolderPanel("저장 폴더", _newSavePath, "");
                if (!string.IsNullOrEmpty(sel))
                {
                    string proj = Path.GetFullPath(Application.dataPath + "/..");
                    if (sel.StartsWith(proj))
                        pathField.value = "Assets" + sel.Substring(proj.Length).Replace('\\', '/');
                }
            }) { text = "..." });
            popup.Add(pathRow);

            var dupWarning = new HelpBox("이미 존재하는 Quest ID입니다.", HelpBoxMessageType.Warning);
            popup.Add(dupWarning);

            var createBtn = new Button { text = "생성", style = { height = 24, marginTop = 4, marginLeft = 8, marginRight = 8 } };
            popup.Add(createBtn);

            void Validate()
            {
                _newQuestId   = idField.value;
                _newQuestName = nameField.value;
                _newSavePath  = pathField.value;

                bool idDuplicate = _quests.Exists(x => x != null && x.questId == _newQuestId);
                dupWarning.style.display = idDuplicate ? DisplayStyle.Flex : DisplayStyle.None;
                createBtn.SetEnabled(!string.IsNullOrWhiteSpace(_newQuestId) &&
                                     !string.IsNullOrWhiteSpace(_newQuestName) &&
                                     !idDuplicate);
            }
            idField.RegisterValueChangedCallback(_ => Validate());
            nameField.RegisterValueChangedCallback(_ => Validate());
            pathField.RegisterValueChangedCallback(_ => Validate());
            Validate();

            createBtn.clicked += () =>
            {
                popup.style.display = DisplayStyle.None;
                CreateNewQuest();
            };

            return popup;
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

            LoadAllQuests();
            _selected = q;
            RefreshList();

            EditorGUIUtility.PingObject(q);
            Debug.Log($"[QuestEditor] 생성 완료: {assetPath}");
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 복제 / 삭제

        private void DuplicateSelected()
        {
            if (_selected == null) return;

            var src        = _selected;
            string srcPath = AssetDatabase.GetAssetPath(src);
            string dir     = Path.GetDirectoryName(srcPath);
            string copy    = AssetDatabase.GenerateUniqueAssetPath(
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
            _selected = newQ;
            RefreshList();
            EditorGUIUtility.PingObject(newQ);
        }

        private void DeleteSelected()
        {
            if (_selected == null) return;

            var q       = _selected;
            string path = AssetDatabase.GetAssetPath(q);

            if (!EditorUtility.DisplayDialog("퀘스트 삭제",
                $"'{q.questName}' (ID: {q.questId})을 삭제합니다.\n이 작업은 되돌릴 수 없습니다.",
                "삭제", "취소"))
                return;

            _selected = null;

            AssetDatabase.DeleteAsset(path);
            LoadAllQuests();
            RefreshList();
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
            RefreshList();
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

        private void OpenItemPicker(System.Action<int> onPicked)
        {
            CloseItemPicker();

            var popup = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, right = 8, top = 28, width = 320, height = 400,
                    backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.8f, 0.8f, 0.8f),
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = Color.black, borderRightColor = Color.black,
                    borderTopColor = Color.black, borderBottomColor = Color.black,
                }
            };

            var header = new Toolbar();
            header.Add(new Label("보상 아이템 선택")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new ToolbarButton(CloseItemPicker) { text = "✕" });
            popup.Add(header);

            var search = new ToolbarSearchField { style = { width = Length.Percent(98) } };
            popup.Add(search);

            var filteredItems = new List<ItemSO>(_allItems);
            var pickerList = new ListView
            {
                fixedItemHeight = 24,
                selectionType = SelectionType.None,
                style = { flexGrow = 1 },
                itemsSource = filteredItems,
                makeItem = () =>
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                    row.Add(new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit, style = { width = 18, height = 18, flexShrink = 0, marginLeft = 2 } });
                    var btn = new Button { name = "pick", style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
                    row.Add(btn);
                    return row;
                },
            };
            pickerList.bindItem = (row, i) =>
            {
                if (i < 0 || i >= filteredItems.Count) return;
                var item = filteredItems[i];
                row.Q<Image>("icon").sprite = item.icon;
                var btn = row.Q<Button>("pick");
                btn.text = $"[{item.itemId}] {item.itemName}";
                btn.clickable = new Clickable(() =>
                {
                    onPicked?.Invoke(item.itemId);
                    CloseItemPicker();
                });
            };
            popup.Add(pickerList);

            search.RegisterValueChangedCallback(evt =>
            {
                string s = evt.newValue ?? "";
                filteredItems.Clear();
                filteredItems.AddRange(_allItems.Where(i =>
                    string.IsNullOrEmpty(s)
                    || i.itemName.IndexOf(s, System.StringComparison.CurrentCultureIgnoreCase) >= 0
                    || i.itemId.ToString().Contains(s)));
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
