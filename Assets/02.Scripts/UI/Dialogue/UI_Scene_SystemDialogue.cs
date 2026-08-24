using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 조작을 막지 않고 화면 하단에 표시되는 나레이션·이동 대화 UI.
    /// System 채널의 기존 프리팹을 재사용하며, 노드별 자동 진행 시간과 비차단 표시를 지원한다.
    /// </summary>
    public class UI_Scene_SystemDialogue : UI_Base
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [Tooltip("노드에 자동 진행 시간이 없을 때 사용할 기본 표시 시간(초).")]
        [SerializeField] private float autoHideDuration = 3f;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField, Min(0f)] private float enterDuration = 0.18f;
        [SerializeField, Min(0f)] private float exitDuration = 0.16f;
        [SerializeField] private float enterOffset = 16f;

        private Coroutine _autoHideCoroutine;
        private Sequence _presentationTween;
        private Vector2 _contentHomePosition;

        protected override void Awake()
        {
            base.Awake();
            EnsurePresentationReferences();
        }

        protected override void OnShow()
        {
            EnsurePresentationReferences();
            UISvc.Dialogue.OnSystemNodeEnter += HandleNodeEnter;
            UISvc.Dialogue.OnDialogueChannelEnd += HandleDialogueEnd;
        }

        protected override void OnHide()
        {
            // 앱 종료 중 UIManager.Dispose 경유로도 호출되므로 서비스가 null일 수 있다.
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnSystemNodeEnter -= HandleNodeEnter;
                dialogue.OnDialogueChannelEnd -= HandleDialogueEnd;
            }

            if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
            KillPresentationTween();
        }

        private void HandleNodeEnter(DialogueNodeSO node)
        {
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            var table = UISvc.Dialogue.ColorTable;
            messageText.color = table != null ? table.GetColor(node.speakerId) : Color.white;

            string body = DialogueMarkup.ToRichText(
                ResolveDialogueText(node.dialogueText),
                UISvc.Dialogue.Palette);
            string speakerName = ResolveSpeakerName(node);
            messageText.text = string.IsNullOrWhiteSpace(speakerName)
                ? body
                : $"<size=78%><b>{speakerName}</b></size>\n{body}";

            PlayEnterTween();

            float displayDuration = node.autoAdvanceDuration > 0f
                ? node.autoAdvanceDuration
                : autoHideDuration;
            if (displayDuration > 0f)
                _autoHideCoroutine = StartCoroutine(AutoHide(displayDuration));
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

        private static string ResolveSpeakerName(DialogueNodeSO node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.speakerId))
                return string.Empty;

            var party = UISvc.Party;
            return DialogueSpeakerResolver.ResolveSpeakerName(
                node,
                party != null ? party.PartyMemberDataSO : null,
                party != null ? party.ActiveCharacterType : CharacterActorType.None,
                party != null ? party.StoryProtagonistType : CharacterActorType.None);
        }

        private void HandleDialogueEnd(DialogueChannel channel)
        {
            if (channel != DialogueChannel.System)
                return;

            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            KillPresentationTween();
            messageText.text = string.Empty;
        }

        private IEnumerator AutoHide(float delay)
        {
            yield return WaitForPresentationTime(delay);

            PlayExitTween();
            if (exitDuration > 0f)
                yield return WaitForPresentationTime(exitDuration);

            _autoHideCoroutine = null;
            UISvc.Dialogue?.Advance(DialogueChannel.System);
        }

        private static IEnumerator WaitForPresentationTime(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                var dialogue = UISvc.Dialogue;
                if ((dialogue == null || !dialogue.IsPaused) && Time.timeScale > 0f)
                    elapsed += Time.unscaledDeltaTime;

                yield return null;
            }
        }

        private void EnsurePresentationReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (contentRoot == null && messageText != null)
                contentRoot = messageText.transform.parent as RectTransform;

            if (contentRoot != null)
                _contentHomePosition = contentRoot.anchoredPosition;
        }

        private void PlayEnterTween()
        {
            KillPresentationTween();
            EnsurePresentationReferences();

            if (canvasGroup == null || contentRoot == null)
                return;

            canvasGroup.alpha = 0f;
            contentRoot.anchoredPosition = _contentHomePosition + Vector2.down * enterOffset;

            _presentationTween = DOTween.Sequence()
                .Join(DOTween.To(
                    () => canvasGroup.alpha,
                    value => canvasGroup.alpha = value,
                    1f,
                    enterDuration))
                .Join(DOTween.To(
                    () => contentRoot.anchoredPosition,
                    value => contentRoot.anchoredPosition = value,
                    _contentHomePosition,
                    enterDuration).SetEase(Ease.OutCubic))
                .SetUpdate(true);
        }

        private void PlayExitTween()
        {
            KillPresentationTween();
            if (canvasGroup == null || contentRoot == null)
                return;

            _presentationTween = DOTween.Sequence()
                .Join(DOTween.To(
                    () => canvasGroup.alpha,
                    value => canvasGroup.alpha = value,
                    0f,
                    exitDuration))
                .Join(DOTween.To(
                    () => contentRoot.anchoredPosition,
                    value => contentRoot.anchoredPosition = value,
                    _contentHomePosition + Vector2.down * enterOffset,
                    exitDuration).SetEase(Ease.InCubic))
                .SetUpdate(true);
        }

        private void KillPresentationTween()
        {
            if (_presentationTween != null && _presentationTween.IsActive())
                _presentationTween.Kill();
            _presentationTween = null;
        }
    }
}
