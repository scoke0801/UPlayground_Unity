using System.Collections;
using TMPro;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

/// <summary>
/// System 채널 다이얼로그 UI.
/// 입력 대기 없이 자동으로 일정 시간 후 사라지는 알림형 표시.
/// 보통 화면 상단/하단에 배치.
/// </summary>
public class UI_SystemDialogue : UI_Base
{
    [SerializeField] private TextMeshProUGUI messageText;
    [Tooltip("자동 닫힘 시간(초). 0이면 수동 닫기.")]
    [SerializeField] private float autoHideDuration = 3f;

    private Coroutine _autoHideCoroutine;

    protected override void OnShow()
    {
        DialogueManager.Instance.OnSystemNodeEnter += HandleNodeEnter;
        DialogueManager.Instance.OnDialogueEnd     += HandleDialogueEnd;
    }

    protected override void OnHide()
    {
        DialogueManager.Instance.OnSystemNodeEnter -= HandleNodeEnter;
        DialogueManager.Instance.OnDialogueEnd     -= HandleDialogueEnd;

        if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
    }

    private void HandleNodeEnter(DialogueNodeSO node)
    {
        var table = DialogueManager.Instance.ColorTable;
        messageText.color = table != null ? table.GetColor(node.speakerId) : Color.white;

        messageText.text = node.dialogueText;

        if (autoHideDuration > 0f)
        {
            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = StartCoroutine(AutoHide(autoHideDuration));
        }
    }

    private void HandleDialogueEnd()
    {
        if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
        messageText.text = string.Empty;
    }

    private IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        DialogueManager.Instance.Advance(DialogueChannel.System);
    }
}
