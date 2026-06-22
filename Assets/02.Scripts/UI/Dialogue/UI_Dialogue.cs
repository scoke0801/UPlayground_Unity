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

/// <summary>
/// DialogueManager 이벤트를 구독해 UI를 그리는 역할만 담당합니다.
/// 대화 흐름 제어는 DialogueManager에게 위임합니다.
/// </summary>
public class UI_Dialogue : UI_Base
{
    private const string PlayerSpeakerId = "당신";
    private const string PlayerActorId = "Player";

    [Header("대화 패널")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Vector2 portraitMaxSize = new(160f, 160f);
    [SerializeField] private Button advanceButton;

    [Header("선택지 패널")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private UI_DialogueChoiceButton choiceButtonPrefab;
    [SerializeField] private Transform choiceContainer;

    private readonly List<UI_DialogueChoiceButton> _choiceButtons = new();
    private Coroutine _typingCoroutine;

    protected override void Awake()
    {
        base.Awake();
        advanceButton.onClick.AddListener(() => DialogueManager.Instance.Advance());
    }

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        DialogueManager.Instance.OnMainNodeEnter   += HandleNodeEnter;
        DialogueManager.Instance.OnChoicePresented += HandleChoicePresented;
        DialogueManager.Instance.OnDialogueEnd     += HandleDialogueEnd;

        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputDialogueNext, null, null, null, InputLayer.Level_1);
    }

    protected override void OnHide()
    {
        DialogueManager.Instance.OnMainNodeEnter   -= HandleNodeEnter;
        DialogueManager.Instance.OnChoicePresented -= HandleChoicePresented;
        DialogueManager.Instance.OnDialogueEnd     -= HandleDialogueEnd;

        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputDialogueNext, null);
    }

    // ── 이벤트 핸들러 ───────────────────────────────────────────────

    private void HandleNodeEnter(DialogueNodeSO node)
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
        
        dialoguePanel?.SetActive(true);

        Sprite portrait = ResolvePortrait(node);
        speakerNameText.text = ResolveSpeakerName(node);
        ApplyPortrait(portrait);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeText(node.dialogueText, node.typingSpeed));
    }

    private void HandleChoicePresented(List<ChoiceData> choices)
    {
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

            btn.Setup(choice.choiceText, isAvailable, capturedIndex: i);
            _choiceButtons.Add(btn);
        }
    }

    private void HandleDialogueEnd()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        dialoguePanel.SetActive(false);

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    private static string ResolveSpeakerName(DialogueNodeSO node)
    {
        if (!IsPlayerSpeaker(node))
        {
            return node.speakerId;
        }

        var party = PartyManager.Instance;
        var memberData = party != null ? party.PartyMemberDataSO : null;
        CharacterActorType activeType = party != null ? party.ActiveCharacterType : CharacterActorType.None;
        string activeName = memberData != null ? memberData.GetName(activeType) : string.Empty;

        return string.IsNullOrEmpty(activeName) ? node.speakerId : activeName;
    }

    private static Sprite ResolvePortrait(DialogueNodeSO node)
    {
        if (!IsPlayerSpeaker(node))
        {
            return node.portrait;
        }

        var party = PartyManager.Instance;
        var memberData = party != null ? party.PartyMemberDataSO : null;
        CharacterActorType activeType = party != null ? party.ActiveCharacterType : CharacterActorType.None;
        Sprite activePortrait = memberData != null ? memberData.GetFullBodySprite(activeType) : null;

        return activePortrait != null ? activePortrait : node.portrait;
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

    private static bool IsPlayerSpeaker(DialogueNodeSO node)
    {
        return node != null && (node.speakerId == PlayerSpeakerId || node.speakerId == PlayerActorId);
    }

    // ── 타이핑 이펙트 ────────────────────────────────────────────────

    private IEnumerator TypeText(string text, float speed)
    {
        advanceButton.gameObject.SetActive(false);
        dialogueBodyText.text = "";

        foreach (char c in text)
        {
            dialogueBodyText.text += c;
            yield return new WaitForSeconds(speed);
        }

        advanceButton.gameObject.SetActive(true);
    }
    
    private void OnInputDialogueNext(InputAction.CallbackContext obj)
    {
        DialogueManager.Instance.Advance();
    }

}
