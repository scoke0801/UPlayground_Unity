using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Quest;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 퀘스트 UI — Popup 레이어
///
/// 레이아웃:
///   왼쪽 : 상태 탭(수락가능/진행중/완료/실패) + 퀘스트 리스트
///   오른쪽: 상세(제목/상태/설명 + 목표 + 보상 + 추적/완료/포기 버튼)
///
/// UIPrefabDatabase 키: "Quest" (UIKeyType.Quest)
/// 프리팹 초안은 에디터 툴 "UPlayGround/UI/퀘스트 UI 프리팹 빌드"로 생성한다.
/// </summary>
public class UI_QuestMenu : UI_Base
{
    // ──── 카테고리 탭 ────
    [Header("상태 탭")]
    [SerializeField] private Button _tabAvailable;
    [SerializeField] private Button _tabActive;
    [SerializeField] private Button _tabCompleted;
    [SerializeField] private Button _tabFailed;
    [SerializeField] private TextMeshProUGUI _txtCountAvailable;
    [SerializeField] private TextMeshProUGUI _txtCountActive;
    [SerializeField] private TextMeshProUGUI _txtCountCompleted;
    [SerializeField] private TextMeshProUGUI _txtCountFailed;

    // ──── 리스트 ────
    [Header("퀘스트 리스트")]
    [SerializeField] private Transform    _questListContent;
    [SerializeField] private UI_QuestSlot _questSlotPrefab;

    // ──── 상세 ────
    [Header("퀘스트 상세")]
    [SerializeField] private GameObject      _detailPanel;
    [SerializeField] private TextMeshProUGUI _txtQuestTitle;
    [SerializeField] private TextMeshProUGUI _txtStatusBadge;
    [SerializeField] private TextMeshProUGUI _txtQuestDesc;
    [SerializeField] private Transform             _objectiveContent;
    [SerializeField] private UI_QuestObjectiveSlot  _objectiveSlotPrefab;
    [SerializeField] private TextMeshProUGUI _txtRewardGold;
    [SerializeField] private TextMeshProUGUI _txtRewardExp;
    [SerializeField] private Transform          _rewardItemContent;
    [SerializeField] private UI_QuestRewardSlot _rewardItemSlotPrefab;

    // ──── 버튼 ────
    [Header("조작")]
    [SerializeField] private Button          _btnTrack;
    [SerializeField] private TextMeshProUGUI _txtTrackButton;
    [SerializeField] private Button          _btnComplete;
    [SerializeField] private Button          _btnAbandon;
    [SerializeField] private Button          _btnClose;

    // ──── 런타임 상태 ────
    private readonly List<UI_QuestSlot>          _spawnedSlots      = new List<UI_QuestSlot>();
    private readonly List<UI_QuestObjectiveSlot> _spawnedObjectives = new List<UI_QuestObjectiveSlot>();
    private readonly List<UI_QuestRewardSlot>    _spawnedRewards    = new List<UI_QuestRewardSlot>();

    private QuestStatus _currentTab       = QuestStatus.Active;
    private string      _selectedQuestId  = null;
    private QuestStatus _selectedStatus   = QuestStatus.Active;

    // ──────────────────────────────────────────────────────────
    #region UI_Base 생명주기

    protected override void Awake()
    {
        base.Awake();

        _tabAvailable?.onClick.AddListener(() => SetTab(QuestStatus.Available));
        _tabActive?.onClick.AddListener(()    => SetTab(QuestStatus.Active));
        _tabCompleted?.onClick.AddListener(()  => SetTab(QuestStatus.Completed));
        _tabFailed?.onClick.AddListener(()     => SetTab(QuestStatus.Failed));

        _btnTrack?.onClick.AddListener(OnClickTrack);
        _btnComplete?.onClick.AddListener(OnClickComplete);
        _btnAbandon?.onClick.AddListener(OnClickAbandon);
        _btnClose?.onClick.AddListener(Hide);
    }

    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        _selectedQuestId = null;
        _currentTab      = QuestStatus.Active;

        if (_detailPanel != null)
            _detailPanel.SetActive(false);

        RefreshTabCounts();
        RefreshList();
    }

    protected override void OnDispose() { }

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

    private void SetTab(QuestStatus tab)
    {
        _currentTab      = tab;
        _selectedQuestId = null;

        if (_detailPanel != null)
            _detailPanel.SetActive(false);

        RefreshList();
    }

    private void RefreshTabCounts()
    {
        if (QuestManager.Instance == null) return;

        SetCount(_txtCountAvailable, QuestManager.Instance.GetAvailableQuests().Count);
        SetCount(_txtCountActive,    CountEnumerable(QuestManager.Instance.GetActiveQuests()));
        SetCount(_txtCountCompleted, QuestManager.Instance.GetCompletedQuests().Count);
        SetCount(_txtCountFailed,    QuestManager.Instance.GetFailedQuests().Count);
    }

    private void RefreshList()
    {
        ClearSlots();

        var qm = QuestManager.Instance;
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
            if (_detailPanel != null) _detailPanel.SetActive(false);
        }
    }

    private void AddSlot(QuestSO so, QuestStatus status)
    {
        if (so == null) return;

        var slot = Instantiate(_questSlotPrefab, _questListContent);
        bool tracked = QuestManager.Instance.IsQuestTracked(so.questId);
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

    private void ShowDetail(string questId, QuestStatus status)
    {
        var qm = QuestManager.Instance;
        var so = qm?.GetQuestData(questId);
        if (so == null) return;

        if (_detailPanel != null)
            _detailPanel.SetActive(true);

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
        bool tracked = QuestManager.Instance.IsQuestTracked(so.questId);

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

        var qm = QuestManager.Instance;
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

        if (QuestManager.Instance != null && QuestManager.Instance.CompleteQuest(_selectedQuestId))
        {
            RefreshTabCounts();
            RefreshList();  // 완료 탭으로 이동 → 현재(진행중) 리스트에서 사라지며 상세 자동 닫힘
        }
    }

    private void OnClickAbandon()
    {
        if (string.IsNullOrEmpty(_selectedQuestId)) return;

        if (QuestManager.Instance != null && QuestManager.Instance.AbandonQuest(_selectedQuestId))
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

    private void ClearSlots()
    {
        foreach (var s in _spawnedSlots)
            if (s != null) Destroy(s.gameObject);
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

    public void TrackQuest(string questId)   => QuestManager.Instance?.TrackQuest(questId);
    public void UntrackQuest()               => QuestManager.Instance?.UntrackQuest();

    public void ToggleTrackQuest(string questId)
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        if (qm.IsQuestTracked(questId)) qm.UntrackQuest();
        else                            qm.TrackQuest(questId);
    }

    #endregion
}
