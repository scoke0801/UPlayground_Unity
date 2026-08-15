using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 퀘스트 UI — Popup 레이어
    ///
    /// 레이아웃:
    ///   왼쪽 : 상태 탭(수락가능/진행중/완료/실패) + 퀘스트 리스트
    ///   오른쪽: 상세(제목/상태/설명 + 목표 + 보상 + 추적/완료/포기 버튼)
    ///
    /// UIPrefabDatabase 키: "Quest" (UIKeyType.Quest)
    /// 프리팹 구조는 "UPlayGround/UI 에디터"의 퀘스트 빌더로 생성하거나 갱신한다.
    /// </summary>
    public class UI_Scene_QuestMenu : UI_SceneBase
    {
        // ──── 카테고리 탭 ────
        [Header("상태 탭")]
        [SerializeField] private UITabGroup _tabGroup;
        [SerializeField] private TextMeshProUGUI _txtCountAvailable;
        [SerializeField] private TextMeshProUGUI _txtCountActive;
        [SerializeField] private TextMeshProUGUI _txtCountCompleted;
        [SerializeField] private TextMeshProUGUI _txtCountFailed;

        // ──── 리스트 ────
        [Header("퀘스트 리스트")]
        [SerializeField] private Transform    _questListContent;
        [SerializeField] private UIQuestSlot _questSlotPrefab;

        // ──── 상세 ────
        [Header("퀘스트 상세")]
        [SerializeField] private GameObject      _detailPanel;
        [SerializeField] private CanvasGroup     _detailPanelGroup;
        [SerializeField] private TextMeshProUGUI _txtQuestTitle;
        [SerializeField] private TextMeshProUGUI _txtStatusBadge;
        [SerializeField] private TextMeshProUGUI _txtQuestDesc;
        [SerializeField] private Transform             _objectiveContent;
        [SerializeField] private UIQuestObjectiveSlot  _objectiveSlotPrefab;
        [SerializeField] private TextMeshProUGUI _txtRewardGold;
        [SerializeField] private TextMeshProUGUI _txtRewardExp;
        [SerializeField] private Transform          _rewardItemContent;
        [SerializeField] private UIQuestRewardSlot _rewardItemSlotPrefab;

        // ──── 버튼 ────
        [Header("조작")]
        [SerializeField] private Button          _btnTrack;
        [SerializeField] private TextMeshProUGUI _txtTrackButton;
        [SerializeField] private Button          _btnComplete;
        [SerializeField] private Button          _btnAbandon;
        [SerializeField] private Button          _btnClose;

        // ──── 런타임 상태 ────
        private readonly List<UIQuestSlot>          _spawnedSlots      = new List<UIQuestSlot>();
        private readonly List<UIQuestObjectiveSlot> _spawnedObjectives = new List<UIQuestObjectiveSlot>();
        private readonly List<UIQuestRewardSlot>    _spawnedRewards    = new List<UIQuestRewardSlot>();

        private QuestStatus _currentTab       = QuestStatus.Available;
        private string      _selectedQuestId  = null;
        private QuestStatus _selectedStatus   = QuestStatus.Active;

        // 탭 인덱스 → 상태 매핑 (프리팹의 탭 배치 순서와 반드시 일치)
        private static readonly QuestStatus[] TabOrder =
        {
            QuestStatus.Available,
            QuestStatus.Active,
            QuestStatus.Completed,
            QuestStatus.Failed,
        };

        // ──────────────────────────────────────────────────────────
        #region UI_Base 생명주기

        protected override void Awake()
        {
            base.Awake();

            if (_tabGroup != null)
                _tabGroup.SelectionChanged += OnTabSelected;

            _btnTrack?.onClick.AddListener(OnClickTrack);
            _btnComplete?.onClick.AddListener(OnClickComplete);
            _btnAbandon?.onClick.AddListener(OnClickAbandon);
            _btnClose?.onClick.AddListener(Hide);
            ConfigureTabShortcuts(subTabs: _tabGroup);
            ConfigureMainPageShortcut(UIKeyType.Quest);
        }

        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            _selectedQuestId = null;
            _currentTab      = QuestStatus.Available;

            SetDetailVisible(false);
            RefreshTabCounts();

            // "수락 가능" 탭(인덱스 0)을 선택 상태로 시작 → SelectionChanged → SetTab이 리스트를 채운다.
            if (_tabGroup != null)
            {
                _tabGroup.Select(IndexOfTab(QuestStatus.Available));
            }
            else
            {
                RefreshList(); // 그룹 미연결 폴백
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            if (_tabGroup != null)
                _tabGroup.SelectionChanged -= OnTabSelected;
        }

        public override bool PerformBackFunction()
        {
            Hide();
            return false;
        }

        protected override void Update()
        {
            base.Update();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 탭 / 리스트

        // UITabGroup 선택 콜백 (탭 클릭 및 초기 Select 모두 여기로 들어온다)
        private void OnTabSelected(int index)
        {
            if (index < 0 || index >= TabOrder.Length) return;
            SetTab(TabOrder[index]);
        }

        private static int IndexOfTab(QuestStatus status)
        {
            for (int i = 0; i < TabOrder.Length; i++)
                if (TabOrder[i] == status) return i;
            return 0;
        }

        private void SetTab(QuestStatus tab)
        {
            _currentTab      = tab;
            _selectedQuestId = null;

            SetDetailVisible(false);

            RefreshList();
        }

        private void RefreshTabCounts()
        {
            if (UISvc.Quest == null) return;

            SetCount(_txtCountAvailable, UISvc.Quest.GetAvailableQuests().Count);
            SetCount(_txtCountActive,    CountEnumerable(UISvc.Quest.GetActiveQuests()));
            SetCount(_txtCountCompleted, UISvc.Quest.GetCompletedQuests().Count);
            SetCount(_txtCountFailed,    UISvc.Quest.GetFailedQuests().Count);
        }

        private void RefreshList()
        {
            ClearSlots();

            var qm = UISvc.Quest;
            if (qm == null) return;

            switch (_currentTab)
            {
                case QuestStatus.Available:
                    foreach (var so in qm.GetAvailableQuests())  AddSlot(so, QuestStatus.Available);
                    break;
                case QuestStatus.Active:
                    foreach (var rt in qm.GetActiveQuests())     AddSlot(rt.QuestSO, QuestStatus.Active);
                    break;
                case QuestStatus.Completed:
                    foreach (var so in qm.GetCompletedQuests())  AddSlot(so, QuestStatus.Completed);
                    break;
                case QuestStatus.Failed:
                    foreach (var so in qm.GetFailedQuests())     AddSlot(so, QuestStatus.Failed);
                    break;
            }

            // 선택 상태 복원 (탭 내에 여전히 존재하면)
            bool stillPresent = false;
            foreach (var slot in _spawnedSlots)
            {
                bool selected = slot.QuestId == _selectedQuestId;
                slot.SetSelected(selected);
                stillPresent |= selected;
            }

            if (!stillPresent)
            {
                _selectedQuestId = null;

                // 선택된 퀘스트가 없으면 리스트의 첫 대상 퀘스트를 자동 선택
                if (_spawnedSlots.Count > 0)
                    OnQuestSlotClicked(_spawnedSlots[0].QuestId, _currentTab);
                else
                    SetDetailVisible(false);
            }

            RebuildNavigation();
        }

        private void AddSlot(QuestSO so, QuestStatus status)
        {
            if (so == null) return;

            var slot = Instantiate(_questSlotPrefab, _questListContent);
            bool tracked = UISvc.Quest.IsQuestTracked(so.questId);
            slot.Init(so, status, tracked, this);
            _spawnedSlots.Add(slot);
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 선택 / 상세

        public void OnQuestSlotClicked(string questId, QuestStatus status)
        {
            _selectedQuestId = questId;
            _selectedStatus  = status;

            foreach (var slot in _spawnedSlots)
                slot.SetSelected(slot.QuestId == questId);

            ShowDetail(questId, status);
        }

        private void RebuildNavigation()
        {
            var tabs = new List<Selectable>();
            if (_tabGroup != null)
            {
                for (int i = 0; i < _tabGroup.TabCount; i++)
                {
                    Button button = _tabGroup.GetTab(i)?.Button;
                    if (button != null)
                        tabs.Add(button);
                }
            }
            UIFocusNavigation.ConfigureHorizontal(tabs, wrap: true);

            var slotSelectables = new List<Selectable>();
            foreach (UIQuestSlot slot in _spawnedSlots)
            {
                if (slot != null && slot.Selectable != null)
                    slotSelectables.Add(slot.Selectable);
            }
            UIFocusNavigation.ConfigureVertical(slotSelectables);

            var actions = new Selectable[]
            {
                _btnTrack,
                _btnComplete,
                _btnAbandon,
                _btnClose
            };
            UIFocusNavigation.ConfigureVertical(actions);
            Selectable firstAction = UIFocusNavigation.FirstNavigable(actions);
            Selectable firstSlot = slotSelectables.Count > 0 ? slotSelectables[0] : null;
            Selectable selectedTab = _tabGroup?.GetTab(_tabGroup.SelectedIndex)?.Button;
            foreach (Selectable tab in tabs)
            {
                Navigation navigation = tab.navigation;
                navigation.selectOnDown = firstSlot ?? firstAction;
                tab.navigation = navigation;
            }

            for (int i = 0; i < slotSelectables.Count; i++)
            {
                Selectable slot = slotSelectables[i];
                Navigation navigation = slot.navigation;
                if (i == 0)
                    navigation.selectOnUp = selectedTab ?? (tabs.Count > 0 ? tabs[0] : null);
                navigation.selectOnRight = firstAction;
                slot.navigation = navigation;
            }

            Selectable selectedSlot = null;
            foreach (UIQuestSlot slot in _spawnedSlots)
            {
                if (slot != null && slot.QuestId == _selectedQuestId)
                {
                    selectedSlot = slot.Selectable;
                    break;
                }
            }

            foreach (Selectable action in actions)
            {
                if (action == null)
                    continue;
                Navigation navigation = action.navigation;
                navigation.selectOnLeft = selectedSlot;
                action.navigation = navigation;
            }

            Selectable initial = firstSlot
                ?? (tabs.Count > 0 ? tabs[0] : firstAction);
            SetDefaultFocus(initial, IsVisible);
        }

        private void ShowDetail(string questId, QuestStatus status)
        {
            var qm = UISvc.Quest;
            var so = qm?.GetQuestData(questId);
            if (so == null) return;

            SetDetailVisible(true);

            _txtQuestTitle.text  = so.questName;
            if (_txtStatusBadge != null) _txtStatusBadge.text = StatusLabel(status);
            if (_txtQuestDesc != null)   _txtQuestDesc.text   = so.questDescription;

            var runtime = qm.GetActiveQuestRuntime(questId);

            // 목표
            ClearObjectives();
            foreach (var obj in so.objectives)
            {
                int current = ResolveObjectiveProgress(runtime, obj, status);
                var slot = Instantiate(_objectiveSlotPrefab, _objectiveContent);
                slot.Init(obj, current);
                _spawnedObjectives.Add(slot);
            }

            // 보상
            if (_txtRewardGold != null) _txtRewardGold.text = so.reward.gold.ToString("N0");
            if (_txtRewardExp != null)  _txtRewardExp.text  = so.reward.exp.ToString("N0");

            ClearRewards();
            foreach (var item in so.reward.items)
            {
                var slot = Instantiate(_rewardItemSlotPrefab, _rewardItemContent);
                slot.Init(item.itemId, item.count);
                _spawnedRewards.Add(slot);
            }

            RefreshDetailButtons(so, status, runtime);
        }

        private static int ResolveObjectiveProgress(QuestRuntimeData runtime, QuestObjectiveData obj, QuestStatus status)
        {
            if (runtime != null && runtime.ObjectiveProgress.TryGetValue(obj.objectiveId, out var c))
                return c;

            // 진행 런타임이 없는 경우(수락 전/완료/실패): 완료 상태만 목표 달성으로 간주
            return status == QuestStatus.Completed ? obj.requiredCount : 0;
        }

        private void RefreshDetailButtons(QuestSO so, QuestStatus status, QuestRuntimeData runtime)
        {
            bool active  = status == QuestStatus.Active;
            bool tracked = UISvc.Quest.IsQuestTracked(so.questId);

            if (_btnTrack != null)      _btnTrack.interactable = active;
            if (_txtTrackButton != null) _txtTrackButton.text  = tracked ? "추적 해제" : "추적";

            if (_btnAbandon != null)    _btnAbandon.interactable = active;

            bool canComplete = active && runtime != null
                               && runtime.AreAllObjectivesComplete() && !so.autoComplete;
            if (_btnComplete != null)   _btnComplete.interactable = canComplete;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 버튼 핸들러

        private void OnClickTrack()
        {
            if (string.IsNullOrEmpty(_selectedQuestId)) return;

            var qm = UISvc.Quest;
            if (qm == null) return;

            if (qm.IsQuestTracked(_selectedQuestId))
                qm.UntrackQuest();
            else
                qm.TrackQuest(_selectedQuestId);

            RefreshList();
            ShowDetail(_selectedQuestId, _selectedStatus);
        }

        private void OnClickComplete()
        {
            if (string.IsNullOrEmpty(_selectedQuestId)) return;

            if (UISvc.Quest != null && UISvc.Quest.CompleteQuest(_selectedQuestId))
            {
                RefreshTabCounts();
                RefreshList();  // 완료 탭으로 이동 → 현재(진행중) 리스트에서 사라지며 상세 자동 닫힘
            }
        }

        private void OnClickAbandon()
        {
            if (string.IsNullOrEmpty(_selectedQuestId)) return;

            if (UISvc.Quest != null && UISvc.Quest.AbandonQuest(_selectedQuestId))
            {
                RefreshTabCounts();
                RefreshList();
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 헬퍼

        private static string StatusLabel(QuestStatus status) => status switch
        {
            QuestStatus.Available => "수락 가능",
            QuestStatus.Active    => "진행 중",
            QuestStatus.Completed => "완료",
            QuestStatus.Failed    => "실패",
            _                     => string.Empty,
        };

        private static void SetCount(TextMeshProUGUI label, int count)
        {
            if (label != null) label.text = count.ToString();
        }

        private static int CountEnumerable<T>(IEnumerable<T> src)
        {
            if (src is ICollection<T> c) return c.Count;
            int n = 0;
            foreach (var _ in src) n++;
            return n;
        }

        private void SetDetailVisible(bool visible)
        {
            if (_detailPanel == null) return;

            if (_detailPanelGroup == null)
                _detailPanelGroup = _detailPanel.GetComponent<CanvasGroup>();

            if (_detailPanelGroup == null)
            {
                _detailPanel.SetActive(visible);
                return;
            }

            if (!_detailPanel.activeSelf)
                _detailPanel.SetActive(true);

            _detailPanelGroup.alpha = visible ? 1f : 0f;
            _detailPanelGroup.interactable = visible;
            _detailPanelGroup.blocksRaycasts = visible;
        }

        private void ClearSlots()
        {
            foreach (var s in _spawnedSlots)
            {
                if (s == null) continue;
                s.gameObject.SetActive(false);
                Destroy(s.gameObject);
            }
            _spawnedSlots.Clear();
        }

        private void ClearObjectives()
        {
            foreach (var s in _spawnedObjectives)
                if (s != null) Destroy(s.gameObject);
            _spawnedObjectives.Clear();
        }

        private void ClearRewards()
        {
            foreach (var s in _spawnedRewards)
                if (s != null) Destroy(s.gameObject);
            _spawnedRewards.Clear();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 외부 API (기존 유지)

        public void TrackQuest(string questId)   => UISvc.Quest?.TrackQuest(questId);
        public void UntrackQuest()               => UISvc.Quest?.UntrackQuest();

        public void ToggleTrackQuest(string questId)
        {
            var qm = UISvc.Quest;
            if (qm == null) return;

            if (qm.IsQuestTracked(questId)) qm.UntrackQuest();
            else                            qm.TrackQuest(questId);
        }

        #endregion
    }
}
