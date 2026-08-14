using System.Collections;
using TMPro;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
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
            UISvc.Dialogue.OnSystemNodeEnter += HandleNodeEnter;
            UISvc.Dialogue.OnDialogueEnd     += HandleDialogueEnd;
        }

        protected override void OnHide()
        {
            // 앱 종료 중 UIManager.Dispose 경유로도 호출되므로 서비스가 null일 수 있다.
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnSystemNodeEnter -= HandleNodeEnter;
                dialogue.OnDialogueEnd     -= HandleDialogueEnd;
            }

            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
        }

        private void HandleNodeEnter(DialogueNodeSO node)
        {
            var table = UISvc.Dialogue.ColorTable;
            messageText.color = table != null ? table.GetColor(node.speakerId) : Color.white;

            // System 채널은 타이핑이 없지만 인라인 색상 마크업은 동일하게 해석한다.
            messageText.text = DialogueMarkup.ToRichText(
                ResolveDialogueText(node.dialogueText),
                UISvc.Dialogue.Palette);

            if (autoHideDuration > 0f)
            {
                if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = StartCoroutine(AutoHide(autoHideDuration));
            }
        }

        private static string ResolveDialogueText(string source)
        {
            var party = UISvc.Party;
            var memberData = party != null ? party.PartyMemberDataSO : null;
            return DialogueTextResolver.Resolve(
                source,
                memberData != null && party != null ? memberData.GetName(party.ActiveCharacterType) : string.Empty,
                memberData != null && party != null ? memberData.GetName(party.StoryProtagonistType) : string.Empty);
        }

        private void HandleDialogueEnd()
        {
            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
            messageText.text = string.Empty;
        }

        private IEnumerator AutoHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            UISvc.Dialogue.Advance(DialogueChannel.System);
        }
    }
}
