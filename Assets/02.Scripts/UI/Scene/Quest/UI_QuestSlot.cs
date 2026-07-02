using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.Quest;

/// <summary>
/// 퀘스트 메뉴 — 좌측 리스트 슬롯 1개.
/// UI_QuestMenu의 리스트에서 Instantiate해 사용한다.
/// </summary>
public class UI_QuestSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image           _imgIcon;          // 메인/서브 구분 아이콘
    [SerializeField] private TextMeshProUGUI _txtName;
    [SerializeField] private TextMeshProUGUI _txtSummary;       // 짧은 부제
    [SerializeField] private GameObject      _trackIndicator;   // 추적 중 표시
    [SerializeField] private GameObject      _selectOverlay;    // 선택 하이라이트

    [Header("분류 아이콘 색")]
    [SerializeField] private Color _colorMain = new Color(0.95f, 0.78f, 0.35f); // 메인
    [SerializeField] private Color _colorSub  = new Color(0.45f, 0.70f, 0.95f); // 서브

    [Header("상태별 이름 색")]
    [SerializeField] private Color _colorNormal    = new Color(0.90f, 0.92f, 0.95f);
    [SerializeField] private Color _colorCompleted = new Color(0.35f, 0.85f, 0.45f);
    [SerializeField] private Color _colorFailed    = new Color(0.90f, 0.35f, 0.35f);

    private string       _questId;
    private QuestStatus  _status;
    private UI_QuestMenu _parent;

    public string      QuestId => _questId;
    public QuestStatus Status  => _status;

    public void Init(QuestSO so, QuestStatus status, bool tracked, UI_QuestMenu parent)
    {
        _questId = so.questId;
        _status  = status;
        _parent  = parent;

        _txtName.text = so.questName;

        if (_txtSummary != null)
        {
            _txtSummary.text = !string.IsNullOrEmpty(so.shortSummary)
                ? so.shortSummary
                : FirstLine(so.questDescription);
        }

        // 메인/서브 아이콘 색
        if (_imgIcon != null)
            _imgIcon.color = so.questType == QuestType.Main ? _colorMain : _colorSub;

        // 상태별 이름 색
        _txtName.color = status switch
        {
            QuestStatus.Completed => _colorCompleted,
            QuestStatus.Failed    => _colorFailed,
            _                     => _colorNormal,
        };

        RefreshTracked(tracked);
        SetSelected(false);
    }

    public void RefreshTracked(bool tracked)
    {
        if (_trackIndicator != null)
            _trackIndicator.SetActive(tracked);
    }

    public void SetSelected(bool selected)
    {
        if (_selectOverlay != null)
            _selectOverlay.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _parent?.OnQuestSlotClicked(_questId, _status);
    }

    private static string FirstLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        int nl = text.IndexOf('\n');
        return nl < 0 ? text : text.Substring(0, nl);
    }
}
