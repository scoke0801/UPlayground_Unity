using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// DialogueManager 이벤트를 구독해 UI를 그리는 역할만 담당합니다.
    /// 대화 흐름 제어는 DialogueManager에게 위임합니다.
    /// 타이핑은 DialogueTypewriter(maxVisibleCharacters)에 위임해 리치 텍스트 태그가 노출되지 않게 합니다.
    /// </summary>
    public class UI_Dialogue : UI_Base
    {
        [Header("대화 패널")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueBodyText;
        [SerializeField] private DialogueTypewriter typewriter;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Vector2 portraitMaxSize = new(160f, 160f);
        [SerializeField] private Button advanceButton;

        [Header("선택지 패널")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private UI_DialogueChoiceButton choiceButtonPrefab;
        [SerializeField] private Transform choiceContainer;

        private readonly List<UI_DialogueChoiceButton> _choiceButtons = new();
        private Coroutine _autoAdvanceCoroutine;
        private DialogueNodeSO _currentNode;

        protected override void Awake()
        {
            base.Awake();
            advanceButton.onClick.AddListener(OnAdvanceRequested);
            EnsureTypewriter();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            UISvc.Dialogue.OnMainNodeEnter   += HandleNodeEnter;
            UISvc.Dialogue.OnChoicePresented += HandleChoicePresented;
            UISvc.Dialogue.OnDialogueEnd     += HandleDialogueEnd;
            UISvc.Dialogue.OnTypingCompleteRequested += HandleTypingCompleteRequested;
            UISvc.Dialogue.OnAutoChanged     += HandleAutoChanged;

            Svc.Input.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputDialogueNext, null, null, null, InputLayer.Level_1);
        }

        protected override void OnHide()
        {
            // 앱 종료 중 UIManager.Dispose 경유로도 호출되므로 서비스가 null일 수 있다.
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnMainNodeEnter   -= HandleNodeEnter;
                dialogue.OnChoicePresented -= HandleChoicePresented;
                dialogue.OnDialogueEnd     -= HandleDialogueEnd;
                dialogue.OnTypingCompleteRequested -= HandleTypingCompleteRequested;
                dialogue.OnAutoChanged     -= HandleAutoChanged;
            }

            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputDialogueNext, null);

            StopAutoAdvance();
        }

        // ── 이벤트 핸들러 ───────────────────────────────────────────────

        private void HandleNodeEnter(DialogueNodeSO node)
        {
            _currentNode = node;
            _typingComplete = false;
            StopAutoAdvance();

            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            dialoguePanel?.SetActive(true);

            speakerNameText.text = ResolveSpeakerName(node);
            ApplyPortrait(ResolvePortrait(node));

            advanceButton.gameObject.SetActive(false);
            EnsureTypewriter()?.Play(
                ResolveDialogueText(node.dialogueText),
                UISvc.Dialogue?.Palette,
                node.typingSpeed);
        }

        private void HandleChoicePresented(List<ChoiceData> choices)
        {
            // 선택은 플레이어 몫이므로 자동 재생은 여기서 멈춘다.
            StopAutoAdvance();

            choicePanel.SetActive(true);
            advanceButton.gameObject.SetActive(false);

            // 기존 버튼 반환 (풀링이 아닌 단순 재사용)
            foreach (var btn in _choiceButtons) Destroy(btn.gameObject);
            _choiceButtons.Clear();

            for (int i = 0; i < choices.Count; i++)
            {
                var btn = Instantiate(choiceButtonPrefab, choiceContainer);
                var choice = choices[i];
                bool isAvailable = choice.displayCondition == null || choice.displayCondition.Evaluate();

                btn.Setup(ResolveDialogueText(choice.choiceText), isAvailable, capturedIndex: i);
                _choiceButtons.Add(btn);
            }
        }

        private void HandleDialogueEnd()
        {
            StopAutoAdvance();
            _currentNode = null;
            _typingComplete = false;

            EnsureTypewriter()?.Clear();
            dialoguePanel.SetActive(false);

            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }
        }

        // 컨트롤 바/전용 입력이 요청한 타이핑 스킵(약).
        private void HandleTypingCompleteRequested()
        {
            EnsureTypewriter()?.CompleteTyping();
        }

        private static string ResolveSpeakerName(DialogueNodeSO node)
        {
            var party = UISvc.Party;
            return DialogueSpeakerResolver.ResolveSpeakerName(
                node,
                party != null ? party.PartyMemberDataSO : null,
                party != null ? party.ActiveCharacterType : CharacterActorType.None,
                party != null ? party.StoryProtagonistType : CharacterActorType.None);
        }

        private static Sprite ResolvePortrait(DialogueNodeSO node)
        {
            var party = UISvc.Party;
            return DialogueSpeakerResolver.ResolvePortrait(
                node,
                party != null ? party.PartyMemberDataSO : null,
                party != null ? party.ActiveCharacterType : CharacterActorType.None,
                party != null ? party.StoryProtagonistType : CharacterActorType.None);
        }

        private static string ResolveDialogueText(string source)
        {
            var party = UISvc.Party;
            var memberData = party != null ? party.PartyMemberDataSO : null;
            string activeName = memberData != null && party != null
                ? memberData.GetName(party.ActiveCharacterType)
                : string.Empty;
            string protagonistName = memberData != null && party != null
                ? memberData.GetName(party.StoryProtagonistType)
                : string.Empty;

            return DialogueTextResolver.Resolve(source, activeName, protagonistName);
        }

        private void ApplyPortrait(Sprite portrait)
        {
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);

            if (portrait == null)
            {
                return;
            }

            portraitImage.preserveAspect = true;

            float width = portrait.rect.width;
            float height = portrait.rect.height;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            float maxWidth = Mathf.Max(1f, portraitMaxSize.x);
            float maxHeight = Mathf.Max(1f, portraitMaxSize.y);
            float scale = Mathf.Min(maxWidth / width, maxHeight / height);

            portraitImage.rectTransform.sizeDelta = new Vector2(width * scale, height * scale);
        }

        // ── 타이핑 / 자동 진행 ───────────────────────────────────────────

        private DialogueTypewriter EnsureTypewriter()
        {
            if (typewriter == null && dialogueBodyText != null)
            {
                typewriter = dialogueBodyText.GetComponent<DialogueTypewriter>();
                if (typewriter == null)
                    typewriter = dialogueBodyText.gameObject.AddComponent<DialogueTypewriter>();
            }

            if (typewriter != null && !_typingCompletedBound)
            {
                typewriter.OnCompleted += HandleTypingCompleted;
                _typingCompletedBound = true;
            }

            return typewriter;
        }

        private bool _typingCompletedBound;
        private bool _typingComplete;

        private void HandleTypingCompleted()
        {
            _typingComplete = true;

            // 선택지 노드는 선택 UI가 진행을 담당하므로 진행 버튼을 띄우지 않는다.
            bool isChoice = _currentNode != null && _currentNode.nodeType == NodeType.Choice;
            advanceButton.gameObject.SetActive(!isChoice);

            StartAutoAdvanceIfNeeded();
        }

        // 컨트롤 바에서 자동 재생을 켰을 때, 이미 타이핑이 끝나 대기 중이면 즉시 카운트다운을 시작한다.
        // (이 처리가 없으면 토글이 다음 노드부터 적용돼 '자동이 안 걸리는' 것처럼 보인다.)
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

            // 선택은 플레이어 몫이므로 선택지 노드에서는 자동 진행하지 않는다.
            if (_currentNode.nodeType == NodeType.Choice)
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
            UISvc.Dialogue?.Advance();
        }

        // ── 입력 ────────────────────────────────────────────────────────

        private void OnInputDialogueNext(InputAction.CallbackContext obj) => OnAdvanceRequested();

        // 타이핑 중이면 완성만 하고, 완성 상태면 다음 노드로 진행한다.
        private void OnAdvanceRequested()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null || dialogue.IsPaused)
                return;

            if (EnsureTypewriter()?.CompleteTyping() == true)
                return;

            StopAutoAdvance();
            dialogue.Advance();
        }
    }
}
