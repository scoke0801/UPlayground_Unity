using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// Monologue 채널 다이얼로그 UI.
    /// 화면 중앙 이탤릭체 텍스트 등 주인공 독백 전용 레이아웃.
    /// 타이핑 효과 + 입력 대기로 진행 (Main 대화와 동일한 UX).
    /// 타이핑은 Main과 같은 DialogueTypewriter를 공유합니다.
    /// </summary>
    public class UI_MonologueDialogue : UI_Base
    {
        [SerializeField] private TextMeshProUGUI monologueText;
        [SerializeField] private DialogueTypewriter typewriter;

        private Coroutine _autoAdvanceCoroutine;
        private DialogueNodeSO _currentNode;
        private bool _typingCompletedBound;
        private bool _typingComplete;

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            base.Awake();
            EnsureTypewriter();
        }

        protected override void OnShow()
        {
            UISvc.Dialogue.OnMonologueNodeEnter += HandleNodeEnter;
            UISvc.Dialogue.OnDialogueEnd        += HandleDialogueEnd;
            UISvc.Dialogue.OnTypingCompleteRequested += HandleTypingCompleteRequested;
            UISvc.Dialogue.OnAutoChanged        += HandleAutoChanged;

            Svc.Input.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputNext, null, null, null, InputLayer.Level_1);
        }

        protected override void OnHide()
        {
            // 앱 종료 중 UIManager.Dispose 경유로도 호출되므로 서비스가 null일 수 있다.
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnMonologueNodeEnter -= HandleNodeEnter;
                dialogue.OnDialogueEnd        -= HandleDialogueEnd;
                dialogue.OnTypingCompleteRequested -= HandleTypingCompleteRequested;
                dialogue.OnAutoChanged        -= HandleAutoChanged;
            }

            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputNext, null);

            StopAutoAdvance();
        }

        // ── 이벤트 핸들러 ───────────────────────────────────────────────

        private void HandleNodeEnter(DialogueNodeSO node)
        {
            _currentNode = node;
            _typingComplete = false;
            StopAutoAdvance();

            var table = UISvc.Dialogue.ColorTable;
            monologueText.color = table != null ? table.GetColor(node.speakerId) : Color.white;

            EnsureTypewriter()?.Play(node.dialogueText, UISvc.Dialogue?.Palette, node.typingSpeed);
        }

        private void HandleDialogueEnd()
        {
            StopAutoAdvance();
            _currentNode = null;
            _typingComplete = false;
            EnsureTypewriter()?.Clear();
        }

        private void HandleTypingCompleteRequested()
        {
            EnsureTypewriter()?.CompleteTyping();
        }

        private void OnInputNext(InputAction.CallbackContext ctx)
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null || dialogue.IsPaused)
                return;

            // 타이핑 중이면 완성만 하고, 완성 상태면 다음으로 진행한다.
            if (EnsureTypewriter()?.CompleteTyping() == true)
                return;

            StopAutoAdvance();
            dialogue.Advance(DialogueChannel.Monologue);
        }

        // ── 타이핑 / 자동 진행 ───────────────────────────────────────────

        private DialogueTypewriter EnsureTypewriter()
        {
            if (typewriter == null && monologueText != null)
            {
                typewriter = monologueText.GetComponent<DialogueTypewriter>();
                if (typewriter == null)
                    typewriter = monologueText.gameObject.AddComponent<DialogueTypewriter>();
            }

            if (typewriter != null && !_typingCompletedBound)
            {
                typewriter.OnCompleted += HandleTypingCompleted;
                _typingCompletedBound = true;
            }

            return typewriter;
        }

        private void HandleTypingCompleted()
        {
            _typingComplete = true;
            StartAutoAdvanceIfNeeded();
        }

        // 컨트롤 바에서 자동 재생을 켰을 때, 이미 타이핑이 끝나 대기 중이면 즉시 카운트다운을 시작한다.
        private void HandleAutoChanged(bool auto)
        {
            if (auto)
                StartAutoAdvanceIfNeeded();
        }

        private void StartAutoAdvanceIfNeeded()
        {
            StopAutoAdvance();

            var dialogue = UISvc.Dialogue;
            if (dialogue == null || _currentNode == null || !_typingComplete || !isActiveAndEnabled)
                return;

            if (ResolveAutoDelay(dialogue) <= 0f)
                return;

            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
        }

        private void StopAutoAdvance()
        {
            if (_autoAdvanceCoroutine == null)
                return;

            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }

        // 노드 개별 딜레이는 하한이며, 자동 재생 토글이 켜져 있으면 전역 딜레이와 max로 결합한다.
        private float ResolveAutoDelay(IUIDialogueService dialogue)
        {
            float delay = _currentNode != null ? _currentNode.autoAdvanceDuration : 0f;
            if (dialogue.IsAuto)
                delay = Mathf.Max(delay, dialogue.AutoAdvanceDelay);

            return delay;
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            float elapsed = 0f;

            while (true)
            {
                var dialogue = UISvc.Dialogue;
                if (dialogue == null || _currentNode == null)
                {
                    _autoAdvanceCoroutine = null;
                    yield break;
                }

                // 대기 중 자동이 꺼지면(그리고 노드 자체 딜레이도 없으면) 진행하지 않고 중단한다.
                float delay = ResolveAutoDelay(dialogue);
                if (delay <= 0f)
                {
                    _autoAdvanceCoroutine = null;
                    yield break;
                }

                // 정지 중에는 카운트다운이 멈춘다.
                if (!dialogue.IsPaused)
                    elapsed += Time.unscaledDeltaTime;

                if (elapsed >= delay)
                    break;

                yield return null;
            }

            _autoAdvanceCoroutine = null;
            UISvc.Dialogue?.Advance(DialogueChannel.Monologue);
        }
    }
}
