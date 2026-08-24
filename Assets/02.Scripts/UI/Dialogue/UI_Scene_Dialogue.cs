using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public class UI_Scene_Dialogue : UI_Base, IPointerClickHandler
    {
        [Header("대화 패널")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueBodyText;
        [SerializeField] private DialogueTypewriter typewriter;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Vector2 portraitMaxSize = new(160f, 160f);
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject advancePrompt;

        [Header("대사 삽화")]
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Image illustrationForegroundImage;
        [SerializeField] private CanvasGroup illustrationCanvasGroup;
        [SerializeField, Min(0.01f)] private float illustrationFadeDuration = 0.12f;
        [SerializeField, Min(0f)] private float illustrationRevealDelay = 0.2f;
        [SerializeField, Min(0f)] private float illustrationAutoHoldDuration = 1.2f;

        [Header("시네마틱 나레이션")]
        [SerializeField] private TextMeshProUGUI cinematicNarrationText;
        [SerializeField] private TextMeshProUGUI cinematicLocationTitleText;
        [SerializeField, Min(0.01f)] private float cinematicIllustrationEnterDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float cinematicTextEnterDuration = 0.24f;
        [SerializeField, Min(0f)] private float cinematicTextEnterOffset = 14f;
        [SerializeField, Min(0.01f)] private float cinematicExitDuration = 0.7f;

        [Header("선택지 패널")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Transform choiceContainer;

        [Header("대화 연출")]
        [SerializeField, Min(0.01f)] private float panelEnterDuration = 0.22f;
        [SerializeField, Min(0f)] private float panelEnterOffset = 32f;
        [SerializeField, Min(0.01f)] private float lineFadeDuration = 0.12f;

        private readonly List<UIDialogueChoiceButton> _choiceButtons = new();
        private Coroutine _autoAdvanceCoroutine;
        private DialogueNodeSO _currentNode;
        private RectTransform _dialoguePanelRect;
        private CanvasGroup _lineCanvasGroup;
        private Vector2 _dialoguePanelBasePosition;
        private Tween _panelPositionTween;
        private Tween _screenFadeTween;
        private Tween _lineFadeTween;
        private Tween _illustrationFadeTween;
        private Tween _illustrationRevealTween;
        private Tween _illustrationMotionTween;
        private Tween _foregroundEnterTween;
        private Tween _cinematicTextTween;
        private Tween _cinematicExitDelayTween;
        private Sprite _pendingIllustration;
        private Sprite _pendingForegroundIllustration;
        private Color _pendingIllustrationColor = Color.white;
        private DialogueIllustrationPresentation _pendingIllustrationPresentation =
            DialogueIllustrationPresentation.None;
        private bool _hasPendingIllustration;
        private RectTransform _illustrationRect;
        private Transform _illustrationOverlay;
        private int _illustrationOverlayBaseSiblingIndex;
        private Vector2 _illustrationBasePosition;
        private Vector3 _illustrationBaseScale = Vector3.one;
        private RectTransform _foregroundIllustrationRect;
        private Vector2 _foregroundIllustrationBasePosition;
        private Vector3 _foregroundIllustrationBaseScale = Vector3.one;
        private AspectRatioFitter _illustrationAspectRatioFitter;
        private Vector2 _cinematicNarrationBasePosition;
        private Vector2 _cinematicLocationTitleBasePosition;
        private bool _isIllustrationSortingOverrideActive;
        private bool _previousCanvasOverrideSorting;
        private int _previousCanvasSortingOrder;
        private bool _isCinematicNarration;
        private TextMeshProUGUI _activeCinematicText;

        /// <summary>현재 대사 삽화 오버레이가 입력을 점유하고 있는지 반환한다.</summary>
        public bool IsIllustrationVisible => illustrationImage != null
                                             && illustrationCanvasGroup != null
                                             && illustrationCanvasGroup.gameObject.activeSelf
                                             && illustrationImage.sprite != null;

        protected override void Awake()
        {
            base.Awake();
            advanceButton.onClick.AddListener(OnAdvanceRequested);
            EnsureTypewriter();
            CachePresentationReferences();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            // UIManager는 같은 인스턴스를 재사용한다. 새 게임을 같은 실행에서 다시 시작해도
            // 이전 시네마틱의 비활성 오버레이·알파·트윈 상태가 다음 재생으로 새지 않게 한다.
            ResetPresentation();

            UISvc.Dialogue.OnMainNodeEnter   += HandleNodeEnter;
            UISvc.Dialogue.OnChoicePresented += HandleChoicePresented;
            UISvc.Dialogue.OnDialogueChannelEnd += HandleDialogueEnd;
            UISvc.Dialogue.OnTypingCompleteRequested += HandleTypingCompleteRequested;
            UISvc.Dialogue.OnPauseChanged    += HandlePauseChanged;
            UISvc.Dialogue.OnAutoChanged     += HandleAutoChanged;

            Svc.Input.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputDialogueNext, null, null, null, InputLayer.Level_1);

            PlayPanelEntrance();
        }

        protected override void OnHide()
        {
            // 앱 종료 중 UIManager.Dispose 경유로도 호출되므로 서비스가 null일 수 있다.
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnMainNodeEnter   -= HandleNodeEnter;
                dialogue.OnChoicePresented -= HandleChoicePresented;
                dialogue.OnDialogueChannelEnd -= HandleDialogueEnd;
                dialogue.OnTypingCompleteRequested -= HandleTypingCompleteRequested;
                dialogue.OnPauseChanged    -= HandlePauseChanged;
                dialogue.OnAutoChanged     -= HandleAutoChanged;
            }

            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputDialogueNext, null);

            StopAutoAdvance();
            KillPresentationTweens();
            ResetPresentation();
            base.OnHide();
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

            ClearChoiceButtons();
            var dialogue = UISvc.Dialogue;
            DialogueIllustrationPresentation presentation = dialogue != null
                ? dialogue.CurrentLineIllustrationPresentation
                : DialogueIllustrationPresentation.None;
            bool usesCinematicNarration = UsesCinematicText(node);

            QueueIllustration(
                dialogue?.CurrentLineIllustration,
                dialogue?.CurrentLineForegroundIllustration,
                dialogue != null ? dialogue.CurrentLineIllustrationColor : Color.white,
                presentation);
            // 삽화가 형제 순서와 Canvas 정렬을 조정한 뒤 텍스트를 마지막에 올려
            // 전체 화면 이미지가 자막을 가리는 회귀를 막는다.
            SetCinematicNarrationActive(usesCinematicNarration, node);

            if (usesCinematicNarration)
            {
                EnsureTypewriter()?.Clear();
                _typingComplete = true;
                SetAdvanceVisible(false);
                StartAutoAdvanceIfNeeded();
                float cinematicExitDelay = presentation.PersistAcrossFollowingLines
                    ? node.textPresentation == DialogueTextPresentation.CinematicLocationTitle
                        ? node.autoAdvanceDuration
                        : 0f
                    : node.autoAdvanceDuration;
                ScheduleCinematicExit(cinematicExitDelay);
                return;
            }

            speakerNameText.text = ResolveSpeakerName(node);
            ApplyPortrait(ResolvePortrait(node));

            SetAdvanceVisible(false);
            EnsureTypewriter()?.Play(
                ResolveDialogueText(node.dialogueText),
                UISvc.Dialogue?.Palette,
                node.typingSpeed);
            PlayLineFade();
        }

        private static bool UsesCinematicText(DialogueNodeSO node)
        {
            return node != null
                   && node.textPresentation != DialogueTextPresentation.Standard;
        }

        private void HandleChoicePresented(List<ChoiceData> choices)
        {
            // 선택은 플레이어 몫이므로 자동 재생은 여기서 멈춘다.
            StopAutoAdvance();

            if (choicePanel == null || choiceButtonPrefab == null || choiceContainer == null)
            {
                Debug.LogError("[Dialogue] 선택지 UI 프리팹 참조가 누락되었습니다.", this);
                return;
            }

            choicePanel.SetActive(true);
            SetAdvanceVisible(false);

            ClearChoiceButtons();

            UIDialogueChoiceButton firstAvailable = null;
            for (int i = 0; i < choices.Count; i++)
            {
                var buttonObject = Instantiate(choiceButtonPrefab, choiceContainer);
                var btn = buttonObject.GetComponent<UIDialogueChoiceButton>()
                          ?? buttonObject.AddComponent<UIDialogueChoiceButton>();
                var choice = choices[i];
                bool isAvailable = choice.displayCondition == null || choice.displayCondition.Evaluate();

                if (!btn.Setup(ResolveDialogueText(choice.choiceText), isAvailable, capturedIndex: i))
                {
                    Destroy(buttonObject);
                    continue;
                }

                _choiceButtons.Add(btn);
                btn.PlayEntrance(i);
                if (!isAvailable)
                    continue;

                if (firstAvailable == null)
                    firstAvailable = btn;
            }

            ConfigureChoiceNavigation();
            if (firstAvailable != null)
                SetDefaultFocus(firstAvailable.Selectable, ensureSelection: true);
        }

        private void HandleDialogueEnd(DialogueChannel channel)
        {
            if (channel != DialogueChannel.Main)
                return;

            StopAutoAdvance();
            _currentNode = null;
            _typingComplete = false;

            EnsureTypewriter()?.Clear();
            dialoguePanel.SetActive(false);
            ClearIllustrationImmediately();

            if (choicePanel != null)
            {
                choicePanel.SetActive(false);
            }

            ClearChoiceButtons();
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in _choiceButtons)
            {
                if (btn == null)
                    continue;

                btn.gameObject.SetActive(false);
                Destroy(btn.gameObject);
            }

            _choiceButtons.Clear();
        }

        private void ConfigureChoiceNavigation()
        {
            var availableButtons = new List<Button>(_choiceButtons.Count);
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                UIDialogueChoiceButton choice = _choiceButtons[i];
                if (choice != null && choice.IsInteractable && choice.Selectable != null)
                    availableButtons.Add(choice.Selectable);
            }

            for (int i = 0; i < availableButtons.Count; i++)
            {
                Button current = availableButtons[i];
                Navigation navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = availableButtons[(i - 1 + availableButtons.Count) % availableButtons.Count];
                navigation.selectOnDown = availableButtons[(i + 1) % availableButtons.Count];
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                current.navigation = navigation;
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
                party != null ? party.StoryProtagonistType : CharacterActorType.None,
                UISvc.Dialogue?.PortraitTable);
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

        /// <summary>삽화는 대사를 다 읽은 뒤에 노출하므로 노드 진입 시점에는 예약만 한다.</summary>
        private void QueueIllustration(
            Sprite illustration,
            Sprite foregroundIllustration,
            Color color,
            DialogueIllustrationPresentation presentation)
        {
            if (presentation.PersistAcrossFollowingLines
                && IsIllustrationVisible
                && illustrationImage.sprite == illustration)
            {
                if (illustrationForegroundImage != null
                    && illustrationForegroundImage.sprite != foregroundIllustration)
                {
                    ApplyForegroundIllustration(
                        foregroundIllustration,
                        color,
                        presentation);
                }

                return;
            }

            CancelPendingIllustration();
            ApplyIllustration(null, Color.white);

            if (illustration == null)
                return;

            _pendingIllustration = illustration;
            _pendingForegroundIllustration = foregroundIllustration;
            _pendingIllustrationColor = color;
            _pendingIllustrationPresentation = presentation;
            _hasPendingIllustration = true;

            if (presentation.RevealImmediately)
                RevealPendingIllustration();
        }

        /// <summary>타이핑이 끝난 뒤 예약된 삽화를 노출한다.</summary>
        // 마지막 글자와 동시에 덮으면 대사를 읽을 틈이 없어 짧은 지연을 둔다.
        private void ScheduleIllustrationReveal()
        {
            if (!_hasPendingIllustration)
                return;

            _illustrationRevealTween?.Kill();
            _illustrationRevealTween = null;

            if (illustrationRevealDelay <= 0f)
            {
                RevealPendingIllustration();
                return;
            }

            _illustrationRevealTween = DOVirtual.DelayedCall(
                    illustrationRevealDelay,
                    RevealPendingIllustration,
                    ignoreTimeScale: true)
                .SetUpdate(true);
            ApplyDialoguePauseToCinematicTweens();
        }

        private void RevealPendingIllustration()
        {
            _illustrationRevealTween = null;
            if (!_hasPendingIllustration)
                return;

            Sprite illustration = _pendingIllustration;
            Sprite foregroundIllustration = _pendingForegroundIllustration;
            Color color = _pendingIllustrationColor;
            DialogueIllustrationPresentation presentation = _pendingIllustrationPresentation;
            CancelPendingIllustration();
            ApplyIllustration(
                illustration,
                foregroundIllustration,
                color,
                presentation);
        }

        private void CancelPendingIllustration()
        {
            _illustrationRevealTween?.Kill();
            _illustrationRevealTween = null;
            _pendingIllustration = null;
            _pendingForegroundIllustration = null;
            _pendingIllustrationColor = Color.white;
            _pendingIllustrationPresentation = DialogueIllustrationPresentation.None;
            _hasPendingIllustration = false;
        }

        private void ApplyIllustration(Sprite illustration, Color color)
        {
            ApplyIllustration(
                illustration,
                null,
                color,
                DialogueIllustrationPresentation.None);
        }

        private void ApplyIllustration(
            Sprite illustration,
            Color color,
            DialogueIllustrationPresentation presentation)
        {
            ApplyIllustration(
                illustration,
                null,
                color,
                presentation);
        }

        private void ApplyIllustration(
            Sprite illustration,
            Sprite foregroundIllustration,
            Color color,
            DialogueIllustrationPresentation presentation)
        {
            if (illustrationImage == null || illustrationCanvasGroup == null)
                return;

            _illustrationFadeTween?.Kill();
            _illustrationMotionTween?.Kill();
            _illustrationMotionTween = null;

            if (illustration == null)
            {
                if (!illustrationCanvasGroup.gameObject.activeSelf)
                    return;

                _illustrationFadeTween = DOTween.To(
                        () => illustrationCanvasGroup.alpha,
                        value => illustrationCanvasGroup.alpha = value,
                        0f,
                        illustrationFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(CompleteIllustrationHide);
                return;
            }

            illustrationImage.sprite = illustration;
            illustrationImage.color = color;
            ApplyIllustrationAspectMode(illustration, presentation);
            illustrationImage.gameObject.SetActive(true);
            ApplyForegroundIllustration(foregroundIllustration, color, presentation);
            illustrationCanvasGroup.gameObject.SetActive(true);
            illustrationCanvasGroup.alpha = 0f;
            illustrationCanvasGroup.blocksRaycasts = true;
            PlaceIllustration(presentation.Placement);
            RaiseIllustrationLayer();
            PlayIllustrationMotion(presentation);
            _illustrationFadeTween = DOTween.To(
                    () => illustrationCanvasGroup.alpha,
                    value => illustrationCanvasGroup.alpha = value,
                    1f,
                    presentation.IsCinematicNarration
                        ? cinematicIllustrationEnterDuration
                        : illustrationFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            ApplyDialoguePauseToCinematicTweens();
        }

        private void ApplyIllustrationAspectMode(
            Sprite illustration,
            DialogueIllustrationPresentation presentation)
        {
            bool isCinematicNarration = presentation.IsCinematicNarration;
            illustrationImage.preserveAspect = !isCinematicNarration;
            if (_illustrationAspectRatioFitter == null)
                return;

            _illustrationAspectRatioFitter.aspectMode = isCinematicNarration
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.None;
            if (isCinematicNarration && illustration.rect.height > 0f)
            {
                _illustrationAspectRatioFitter.aspectRatio =
                    illustration.rect.width / illustration.rect.height;
            }
        }

        private void ApplyForegroundIllustration(
            Sprite foregroundIllustration,
            Color color,
            DialogueIllustrationPresentation presentation)
        {
            if (illustrationForegroundImage == null)
                return;

            _foregroundEnterTween?.Kill();
            _foregroundEnterTween = null;
            ResetForegroundIllustrationTransform();
            if (foregroundIllustration == null)
            {
                ClearForegroundIllustration();
                return;
            }

            illustrationForegroundImage.sprite = foregroundIllustration;
            illustrationForegroundImage.preserveAspect = true;
            illustrationForegroundImage.gameObject.SetActive(true);

            Color transparentColor = color;
            transparentColor.a = 0f;
            illustrationForegroundImage.color = transparentColor;
            if (_foregroundIllustrationRect != null)
            {
                _foregroundIllustrationRect.anchoredPosition =
                    _foregroundIllustrationBasePosition + presentation.ForegroundStartOffset;
                _foregroundIllustrationRect.localScale =
                    _foregroundIllustrationBaseScale * presentation.ForegroundStartScale;
            }

            float duration = presentation.ForegroundEnterDuration;
            if (duration <= 0f)
            {
                illustrationForegroundImage.color = color;
                if (_foregroundIllustrationRect != null)
                {
                    _foregroundIllustrationRect.anchoredPosition =
                        _foregroundIllustrationBasePosition + presentation.ForegroundEndOffset;
                    _foregroundIllustrationRect.localScale =
                        _foregroundIllustrationBaseScale * presentation.ForegroundEndScale;
                }

                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                () => illustrationForegroundImage.color,
                value => illustrationForegroundImage.color = value,
                color,
                duration));
            if (_foregroundIllustrationRect != null)
            {
                sequence.Join(DOTween.To(
                    () => _foregroundIllustrationRect.anchoredPosition,
                    value => _foregroundIllustrationRect.anchoredPosition = value,
                    _foregroundIllustrationBasePosition + presentation.ForegroundEndOffset,
                    duration));
                sequence.Join(_foregroundIllustrationRect.DOScale(
                    _foregroundIllustrationBaseScale * presentation.ForegroundEndScale,
                    duration));
            }

            _foregroundEnterTween = sequence
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
            ApplyDialoguePauseToCinematicTweens();
        }

        private void CompleteIllustrationHide()
        {
            _illustrationFadeTween = null;
            ResetIllustrationTransform();
            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.color = Color.white;
                illustrationImage.gameObject.SetActive(false);
            }

            ClearForegroundIllustration();

            if (illustrationCanvasGroup != null)
            {
                illustrationCanvasGroup.blocksRaycasts = false;
                illustrationCanvasGroup.gameObject.SetActive(false);
            }

            RestoreIllustrationLayer();
            RestoreIllustrationPlacement();
        }

        private void ClearIllustrationImmediately()
        {
            CancelPendingIllustration();
            _illustrationFadeTween?.Kill();
            _illustrationMotionTween?.Kill();
            _illustrationFadeTween = null;
            _illustrationMotionTween = null;
            ResetIllustrationTransform();

            if (illustrationCanvasGroup != null)
            {
                illustrationCanvasGroup.alpha = 0f;
                illustrationCanvasGroup.blocksRaycasts = false;
                illustrationCanvasGroup.gameObject.SetActive(false);
            }

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.color = Color.white;
                illustrationImage.gameObject.SetActive(false);
            }

            ClearForegroundIllustration();

            RestoreIllustrationLayer();
            RestoreIllustrationPlacement();
        }

        private void ClearForegroundIllustration()
        {
            _foregroundEnterTween?.Kill();
            _foregroundEnterTween = null;
            if (illustrationForegroundImage == null)
                return;

            illustrationForegroundImage.sprite = null;
            illustrationForegroundImage.color = Color.white;
            illustrationForegroundImage.gameObject.SetActive(false);
            ResetForegroundIllustrationTransform();
        }

        private void SetCinematicNarrationActive(bool active, DialogueNodeSO node)
        {
            _cinematicTextTween?.Kill();
            _cinematicTextTween = null;
            bool continuesCinematicSequence = _isCinematicNarration && active;
            if (!continuesCinematicSequence)
                KillCinematicExitTween();
            _isCinematicNarration = active;
            dialoguePanel?.SetActive(!active);

            SetCinematicTextInactive(cinematicNarrationText);
            SetCinematicTextInactive(cinematicLocationTitleText);
            _activeCinematicText = null;
            if (!active)
                return;

            _activeCinematicText = node != null
                                   && node.textPresentation
                                       == DialogueTextPresentation.CinematicLocationTitle
                ? cinematicLocationTitleText
                : cinematicNarrationText;
            if (_activeCinematicText == null)
                return;

            _activeCinematicText.gameObject.SetActive(true);
            _activeCinematicText.transform.SetAsLastSibling();
            _activeCinematicText.text = DialogueMarkup.ToRichText(
                ResolveDialogueText(node.dialogueText),
                UISvc.Dialogue?.Palette);
            RectTransform activeTextRect = _activeCinematicText.rectTransform;
            Vector2 basePosition = GetCinematicTextBasePosition(_activeCinematicText);
            activeTextRect.anchoredPosition = basePosition + Vector2.down * cinematicTextEnterOffset;
            _activeCinematicText.alpha = 0f;
            _activeCinematicText.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);

            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                () => _activeCinematicText.alpha,
                value => _activeCinematicText.alpha = value,
                1f,
                cinematicTextEnterDuration));
            sequence.Join(DOTween.To(
                () => activeTextRect.anchoredPosition,
                value => activeTextRect.anchoredPosition = value,
                basePosition,
                cinematicTextEnterDuration));
            _cinematicTextTween = sequence
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
            ApplyDialoguePauseToCinematicTweens();
        }

        private Vector2 GetCinematicTextBasePosition(TextMeshProUGUI text)
        {
            return text == cinematicLocationTitleText
                ? _cinematicLocationTitleBasePosition
                : _cinematicNarrationBasePosition;
        }

        private static void SetCinematicTextInactive(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.text = string.Empty;
            text.alpha = 1f;
            text.gameObject.SetActive(false);
        }

        private void ScheduleCinematicExit(float autoAdvanceDuration)
        {
            if (!_isCinematicNarration
                || autoAdvanceDuration <= cinematicExitDuration
                || _cinematicExitDelayTween?.IsActive() == true)
                return;

            _cinematicExitDelayTween = DOVirtual.DelayedCall(
                    autoAdvanceDuration - cinematicExitDuration,
                    PlayCinematicExit,
                    ignoreTimeScale: true)
                .SetUpdate(true);
            ApplyDialoguePauseToCinematicTweens();
        }

        private void PlayCinematicExit()
        {
            _cinematicExitDelayTween = null;
            if (!_isCinematicNarration || illustrationCanvasGroup == null)
                return;

            _illustrationFadeTween?.Kill();
            _cinematicTextTween?.Kill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                () => illustrationCanvasGroup.alpha,
                value => illustrationCanvasGroup.alpha = value,
                0f,
                cinematicExitDuration));
            if (_activeCinematicText != null)
            {
                sequence.Join(DOTween.To(
                    () => _activeCinematicText.alpha,
                    value => _activeCinematicText.alpha = value,
                    0f,
                    cinematicExitDuration));
            }

            _cinematicTextTween = null;
            _illustrationFadeTween = sequence
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(CompleteIllustrationHide);
            ApplyDialoguePauseToCinematicTweens();
        }

        private void KillCinematicTweens()
        {
            _cinematicTextTween?.Kill();
            _cinematicTextTween = null;
            KillCinematicExitTween();
        }

        private void KillCinematicExitTween()
        {
            _cinematicExitDelayTween?.Kill();
            _cinematicExitDelayTween = null;
        }

        private void RaiseIllustrationLayer()
        {
            if (_canvas == null || _isIllustrationSortingOverrideActive)
                return;

            _previousCanvasOverrideSorting = _canvas.overrideSorting;
            _previousCanvasSortingOrder = _canvas.sortingOrder;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = (int)_layer + 1;
            _isIllustrationSortingOverrideActive = true;
        }

        private void RestoreIllustrationLayer()
        {
            if (_canvas == null || !_isIllustrationSortingOverrideActive)
                return;

            _canvas.overrideSorting = _previousCanvasOverrideSorting;
            _canvas.sortingOrder = _previousCanvasSortingOrder;
            _isIllustrationSortingOverrideActive = false;
        }

        private void CachePresentationReferences()
        {
            if (dialoguePanel != null)
            {
                _dialoguePanelRect = dialoguePanel.transform as RectTransform;
                if (_dialoguePanelRect != null)
                    _dialoguePanelBasePosition = _dialoguePanelRect.anchoredPosition;
            }

            if (dialogueBodyText != null)
            {
                _lineCanvasGroup = dialogueBodyText.GetComponent<CanvasGroup>();
                if (_lineCanvasGroup == null)
                    _lineCanvasGroup = dialogueBodyText.gameObject.AddComponent<CanvasGroup>();
            }

            if (illustrationImage != null)
            {
                _illustrationRect = illustrationImage.rectTransform;
                _illustrationBasePosition = _illustrationRect.anchoredPosition;
                _illustrationBaseScale = _illustrationRect.localScale;
                _illustrationAspectRatioFitter = illustrationImage.GetComponent<AspectRatioFitter>();
            }

            if (illustrationForegroundImage != null)
            {
                _foregroundIllustrationRect = illustrationForegroundImage.rectTransform;
                _foregroundIllustrationBasePosition =
                    _foregroundIllustrationRect.anchoredPosition;
                _foregroundIllustrationBaseScale = _foregroundIllustrationRect.localScale;
            }

            if (illustrationCanvasGroup != null)
            {
                _illustrationOverlay = illustrationCanvasGroup.transform;
                _illustrationOverlayBaseSiblingIndex = _illustrationOverlay.GetSiblingIndex();
            }

            if (cinematicNarrationText != null)
                _cinematicNarrationBasePosition = cinematicNarrationText.rectTransform.anchoredPosition;
            if (cinematicLocationTitleText != null)
            {
                _cinematicLocationTitleBasePosition =
                    cinematicLocationTitleText.rectTransform.anchoredPosition;
            }
        }

        private void PlaceIllustration(DialogueIllustrationPlacement placement)
        {
            if (_illustrationOverlay == null || dialoguePanel == null)
                return;

            if (placement == DialogueIllustrationPlacement.BehindDialogue)
            {
                // 오프닝 배경 삽화가 대사와 진행 버튼을 가리지 않도록 패널 바로 뒤에 둔다.
                _illustrationOverlay.SetSiblingIndex(dialoguePanel.transform.GetSiblingIndex());
                return;
            }

            RestoreIllustrationPlacement();
        }

        private void RestoreIllustrationPlacement()
        {
            if (_illustrationOverlay == null)
                return;

            _illustrationOverlay.SetSiblingIndex(_illustrationOverlayBaseSiblingIndex);
        }

        private void PlayIllustrationMotion(DialogueIllustrationPresentation presentation)
        {
            if (_illustrationRect == null)
                return;

            _illustrationRect.anchoredPosition = _illustrationBasePosition + presentation.StartOffset;
            _illustrationRect.localScale = _illustrationBaseScale * presentation.StartScale;
            if (presentation.Duration <= 0f)
                return;

            Vector2 endPosition = _illustrationBasePosition + presentation.EndOffset;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                () => _illustrationRect.anchoredPosition,
                value => _illustrationRect.anchoredPosition = value,
                endPosition,
                presentation.Duration));
            sequence.Join(_illustrationRect.DOScale(
                _illustrationBaseScale * presentation.EndScale,
                presentation.Duration));
            _illustrationMotionTween = sequence
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        private void ResetIllustrationTransform()
        {
            _illustrationMotionTween?.Kill();
            _illustrationMotionTween = null;
            if (_illustrationRect == null)
                return;

            _illustrationRect.anchoredPosition = _illustrationBasePosition;
            _illustrationRect.localScale = _illustrationBaseScale;
        }

        private void ResetForegroundIllustrationTransform()
        {
            if (_foregroundIllustrationRect == null)
                return;

            _foregroundIllustrationRect.anchoredPosition = _foregroundIllustrationBasePosition;
            _foregroundIllustrationRect.localScale = _foregroundIllustrationBaseScale;
        }

        private void PlayPanelEntrance()
        {
            KillPresentationTweens();
            CachePresentationReferences();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _screenFadeTween = DOTween.To(
                        () => _canvasGroup.alpha,
                        value => _canvasGroup.alpha = value,
                        1f,
                        panelEnterDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }

            if (_dialoguePanelRect == null)
                return;

            _dialoguePanelRect.anchoredPosition = _dialoguePanelBasePosition + Vector2.down * panelEnterOffset;
            _panelPositionTween = DOTween.To(
                    () => _dialoguePanelRect.anchoredPosition,
                    value => _dialoguePanelRect.anchoredPosition = value,
                    _dialoguePanelBasePosition,
                    panelEnterDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void PlayLineFade()
        {
            if (_lineCanvasGroup == null)
                return;

            _lineFadeTween?.Kill();
            _lineCanvasGroup.alpha = 0f;
            _lineFadeTween = DOTween.To(
                    () => _lineCanvasGroup.alpha,
                    value => _lineCanvasGroup.alpha = value,
                    1f,
                    lineFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void KillPresentationTweens()
        {
            _panelPositionTween?.Kill();
            _screenFadeTween?.Kill();
            _lineFadeTween?.Kill();
            _illustrationFadeTween?.Kill();
            _illustrationRevealTween?.Kill();
            _illustrationMotionTween?.Kill();
            _foregroundEnterTween?.Kill();
            KillCinematicTweens();
            _illustrationRevealTween = null;
            _panelPositionTween = null;
            _screenFadeTween = null;
            _lineFadeTween = null;
            _illustrationFadeTween = null;
            _illustrationMotionTween = null;
            _foregroundEnterTween = null;
        }

        private void ResetPresentation()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            if (_lineCanvasGroup != null)
                _lineCanvasGroup.alpha = 1f;
            if (_dialoguePanelRect != null)
                _dialoguePanelRect.anchoredPosition = _dialoguePanelBasePosition;
            SetCinematicNarrationActive(false, null);
            ClearIllustrationImmediately();
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
            SetAdvanceVisible(!isChoice);

            ScheduleIllustrationReveal();
            StartAutoAdvanceIfNeeded();
        }

        // 컨트롤 바에서 자동 재생을 켰을 때, 이미 타이핑이 끝나 대기 중이면 즉시 카운트다운을 시작한다.
        // (이 처리가 없으면 토글이 다음 노드부터 적용돼 '자동이 안 걸리는' 것처럼 보인다.)
        private void HandleAutoChanged(bool auto)
        {
            if (auto)
                StartAutoAdvanceIfNeeded();
        }

        private void HandlePauseChanged(bool paused)
        {
            SetCinematicTweensPaused(paused);
        }

        private void ApplyDialoguePauseToCinematicTweens()
        {
            SetCinematicTweensPaused(UISvc.Dialogue?.IsPaused == true);
        }

        private void SetCinematicTweensPaused(bool paused)
        {
            SetTweenPaused(_illustrationFadeTween, paused);
            SetTweenPaused(_illustrationRevealTween, paused);
            SetTweenPaused(_illustrationMotionTween, paused);
            SetTweenPaused(_foregroundEnterTween, paused);
            SetTweenPaused(_cinematicTextTween, paused);
            SetTweenPaused(_cinematicExitDelayTween, paused);
        }

        private static void SetTweenPaused(Tween tween, bool paused)
        {
            if (tween?.IsActive() != true)
                return;

            if (paused)
                tween.Pause();
            else
                tween.Play();
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

            // 자동 진행이라도 삽화가 떠 있는 시간은 확보한다. 자동 진행이 꺼진 상태를 켜지는 않는다.
            if (delay > 0f && (_hasPendingIllustration || IsIllustrationVisible))
                delay = Mathf.Max(delay, illustrationRevealDelay + illustrationAutoHoldDuration);

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

        /// <summary>뒤로 가기는 화면 일부가 아니라 Main 대화 세션 전체를 취소한다.</summary>
        public override bool PerformBackFunction()
        {
            UISvc.Dialogue?.CancelDialogue(DialogueChannel.Main);
            return true;
        }

        private void OnInputDialogueNext(InputAction.CallbackContext obj) => OnAdvanceRequested();

        /// <summary>삽화 클릭은 삽화를 닫고 다음 대사로 진행한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (IsIllustrationVisible)
                OnAdvanceRequested();
        }

        // 타이핑 중이면 완성만 하고, 완성 상태면 다음 노드로 진행한다.
        private void OnAdvanceRequested()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null || dialogue.IsPaused)
                return;

            // 노출 대기 중인 삽화는 건너뛰지 않고 먼저 보여준다.
            if (_hasPendingIllustration && _typingComplete)
            {
                RevealPendingIllustration();
                return;
            }

            if (EnsureTypewriter()?.CompleteTyping() == true)
                return;

            // 삽화는 해당 대사에 속하므로, 닫는 입력이 곧 다음 대사로의 진행이다.
            bool dismissedIllustration = CanDismissIllustrationOnAdvance(
                dialogue.CurrentLineIllustrationPresentation)
                && TryDismissIllustration();

            // 선택지 노드는 선택 UI가 진행을 담당하므로, 삽화만 닫고 진행은 넘기지 않는다.
            if (dismissedIllustration && _currentNode != null && _currentNode.nodeType == NodeType.Choice)
                return;

            StopAutoAdvance();
            dialogue.Advance();
        }

        private bool TryDismissIllustration()
        {
            if (!IsIllustrationVisible)
                return false;

            ApplyIllustration(null, Color.white);
            return true;
        }

        private static bool CanDismissIllustrationOnAdvance(
            DialogueIllustrationPresentation presentation)
        {
            return !presentation.PersistAcrossFollowingLines;
        }

        private void SetAdvanceVisible(bool visible)
        {
            if (advanceButton != null)
                advanceButton.gameObject.SetActive(visible);

            if (advancePrompt != null)
                advancePrompt.SetActive(visible);
        }
    }
}
