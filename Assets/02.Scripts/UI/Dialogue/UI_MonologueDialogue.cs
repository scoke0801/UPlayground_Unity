using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// Monologue 채널 다이얼로그 UI.
/// 화면 중앙 이탤릭체 텍스트 등 주인공 독백 전용 레이아웃.
/// 타이핑 효과 + 입력 대기로 진행 (Main 대화와 동일한 UX).
/// </summary>
public class UI_MonologueDialogue : UI_Base
{
    [SerializeField] private TextMeshProUGUI monologueText;

    private Coroutine _typingCoroutine;

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        DialogueManager.Instance.OnMonologueNodeEnter += HandleNodeEnter;
        DialogueManager.Instance.OnDialogueEnd        += HandleDialogueEnd;

        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputNext, null, null, null, InputLayer.Level_1);
    }

    protected override void OnHide()
    {
        DialogueManager.Instance.OnMonologueNodeEnter -= HandleNodeEnter;
        DialogueManager.Instance.OnDialogueEnd        -= HandleDialogueEnd;

        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputNext, null);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
    }

    // ── 이벤트 핸들러 ───────────────────────────────────────────────

    private void HandleNodeEnter(DialogueNodeSO node)
    {
        var table = DialogueManager.Instance.ColorTable;
        monologueText.color = table != null ? table.GetColor(node.speakerId) : Color.white;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeText(node.dialogueText, node.typingSpeed, node.autoAdvanceDuration));
    }

    private void HandleDialogueEnd()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        monologueText.text = string.Empty;
    }

    private void OnInputNext(InputAction.CallbackContext ctx)
    {
        DialogueManager.Instance.Advance(DialogueChannel.Monologue);
    }

    // ── 타이핑 이펙트 ────────────────────────────────────────────────

    private IEnumerator TypeText(string text, float typingSpeed, float autoAdvanceDuration)
    {
        monologueText.text = string.Empty;

        foreach (char c in text)
        {
            monologueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // autoAdvanceDuration > 0 이면 자동 진행, 0이면 입력 대기
        if (autoAdvanceDuration > 0f)
        {
            yield return new WaitForSeconds(autoAdvanceDuration);
            DialogueManager.Instance.Advance(DialogueChannel.Monologue);
        }
    }
}
