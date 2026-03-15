using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// DialogueManager 이벤트를 구독해 UI를 그리는 역할만 담당합니다.
/// 대화 흐름 제어는 DialogueManager에게 위임합니다.
/// </summary>
public class UI_Dialogue : UI_Base
{
    [Header("대화 패널")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button advanceButton;

    [Header("선택지 패널")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private UI_DialogueChoiceButton choiceButtonPrefab;
    [SerializeField] private Transform choiceContainer;

    private readonly List<UI_DialogueChoiceButton> _choiceButtons = new();
    private Coroutine _typingCoroutine;

    private void Awake()
    {
        advanceButton.onClick.AddListener(() => DialogueManager.Instance.Advance());
    }

    protected override void OnShow()
    {
        DialogueManager.Instance.OnMainNodeEnter   += HandleNodeEnter;
        DialogueManager.Instance.OnChoicePresented += HandleChoicePresented;
        DialogueManager.Instance.OnDialogueEnd     += HandleDialogueEnd;
        
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputDialogueNext, null, null, null, InputLayer.Level_1);
        
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
    }

    protected override void OnHide()
    {
        DialogueManager.Instance.OnMainNodeEnter   -= HandleNodeEnter;
        DialogueManager.Instance.OnChoicePresented -= HandleChoicePresented;
        DialogueManager.Instance.OnDialogueEnd     -= HandleDialogueEnd;
        
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
            null, OnInputDialogueNext, null);
        
        InputManager.Instance.SetInputLayer(InputLayer.None);
    }

    // ── 이벤트 핸들러 ───────────────────────────────────────────────

    private void HandleNodeEnter(DialogueNodeSO node)
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
        
        dialoguePanel?.SetActive(true);

        speakerNameText.text = node.speakerId;
        portraitImage.sprite = node.portrait;
        portraitImage.gameObject.SetActive(node.portrait != null);

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
