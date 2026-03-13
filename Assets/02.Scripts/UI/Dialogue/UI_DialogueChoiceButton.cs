using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dialogue;

public class UI_DialogueChoiceButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Setup(string text, bool isAvailable, int capturedIndex)
    {
        label.text = text;
        button.interactable = isAvailable;
        canvasGroup.alpha = isAvailable ? 1f : 0.4f;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => DialogueManager.Instance.SelectChoice(capturedIndex));
    }
}
