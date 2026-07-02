using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Quest;

/// <summary>
/// 퀘스트 상세 — 목표 1개 슬롯.
/// 체크 아이콘 + 설명 + 진행도(현재/필요)를 표시한다.
/// </summary>
public class UI_QuestObjectiveSlot : MonoBehaviour
{
    [SerializeField] private Image           _imgCheck;
    [SerializeField] private TextMeshProUGUI _txtDescription;
    [SerializeField] private TextMeshProUGUI _txtProgress;

    [Header("색상")]
    [SerializeField] private Color _colorComplete   = new Color(0.35f, 0.85f, 0.45f);
    [SerializeField] private Color _colorIncomplete = new Color(0.70f, 0.74f, 0.80f);

    /// <param name="current">현재 진행 카운트</param>
    public void Init(QuestObjectiveData objective, int current)
    {
        int required = Mathf.Max(1, objective.requiredCount);
        current = Mathf.Clamp(current, 0, required);
        bool complete = current >= required;

        _txtDescription.text = objective.description;
        _txtProgress.text    = $"{current} / {required}";

        var color = complete ? _colorComplete : _colorIncomplete;
        _txtProgress.color = color;
        if (_imgCheck != null) _imgCheck.color = color;
        _txtDescription.color = complete ? _colorComplete : new Color(0.90f, 0.92f, 0.95f);
    }
}
