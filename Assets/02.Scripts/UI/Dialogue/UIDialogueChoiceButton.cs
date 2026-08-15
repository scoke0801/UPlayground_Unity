using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    [DisallowMultipleComponent]
    public class UIDialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;

        public bool Setup(string text, bool isAvailable, int capturedIndex)
        {
            ResolveReferences();
            if (label == null || button == null || canvasGroup == null)
            {
                Debug.LogError($"[Dialogue] 선택지 버튼 필수 참조가 없습니다: {name}", this);
                return false;
            }

            label.text = text;
            button.interactable = isAvailable;
            canvasGroup.alpha = isAvailable ? 1f : 0.4f;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => UISvc.Dialogue?.SelectChoice(capturedIndex));
            return true;
        }

        public bool IsInteractable => button != null && button.interactable;

        private void ResolveReferences()
        {
            // 공용 버튼 프리팹은 자기 Button/TMP 참조를 이미 직렬화해 둔다.
            // 프리팹 복제본에 이 컴포넌트를 런타임으로 붙이는 경우 해당 참조를 우선 재사용한다.
            var commonButton = GetComponent<UICommonButton>();

            if (button == null)
                button = commonButton != null ? commonButton.Button : null;

            if (button == null)
                button = GetComponent<Button>();

            if (button == null)
                button = GetComponentInChildren<Button>(true);

            if (label == null)
                label = commonButton != null ? commonButton.Text : null;

            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
}
