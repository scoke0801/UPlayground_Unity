using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>대화 선택지의 데이터 바인딩과 포커스·등장 피드백을 담당한다.</summary>
    [DisallowMultipleComponent]
    public class UIDialogueChoiceButton : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("선택 표시")]
        [SerializeField] private Image focusMarker;
        [SerializeField] private Color normalTextColor = new(0.84f, 0.82f, 0.86f, 1f);
        [SerializeField] private Color focusedTextColor = new(1f, 0.86f, 0.48f, 1f);

        [Header("연출")]
        [SerializeField, Min(0.01f)] private float focusDuration = 0.10f;
        [SerializeField, Min(0.01f)] private float entranceDuration = 0.16f;
        [SerializeField, Min(0f)] private float entranceStagger = 0.04f;
        [SerializeField] private float markerStartOffset = -12f;

        private Tween _entranceTween;
        private Tween _labelTween;
        private Tween _markerColorTween;
        private Tween _markerPositionTween;
        private Vector2 _markerBasePosition;
        private Color _markerBaseColor = Color.white;
        private float _availableAlpha = 1f;

        public Button Selectable => button;
        public bool IsInteractable => button != null && button.interactable;

        private void Awake()
        {
            ResolveReferences();
            CacheMarkerVisual();
            ApplyFocusVisual(false, animate: false);
        }

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
            _availableAlpha = isAvailable ? 1f : 0.4f;
            canvasGroup.alpha = _availableAlpha;
            label.color = normalTextColor;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => UISvc.Dialogue?.SelectChoice(capturedIndex));
            ApplyFocusVisual(false, animate: false);
            return true;
        }

        /// <summary>선택지가 한꺼번에 튀어나오지 않도록 표시 순서에 맞춰 짧게 등장시킨다.</summary>
        public void PlayEntrance(int visualOrder)
        {
            if (canvasGroup == null)
                return;

            _entranceTween?.Kill();
            canvasGroup.alpha = 0f;
            _entranceTween = DOTween.To(
                    () => canvasGroup.alpha,
                    value => canvasGroup.alpha = value,
                    _availableAlpha,
                    entranceDuration)
                .SetDelay(Mathf.Max(0, visualOrder) * entranceStagger)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (IsInteractable)
                ApplyFocusVisual(true, animate: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ApplyFocusVisual(false, animate: true);
        }

        private void OnDisable()
        {
            KillTweens();
            ApplyFocusVisual(false, animate: false);
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void CacheMarkerVisual()
        {
            if (focusMarker == null)
                return;

            _markerBasePosition = focusMarker.rectTransform.anchoredPosition;
            _markerBaseColor = focusMarker.color;
        }

        private void ApplyFocusVisual(bool focused, bool animate)
        {
            if (label != null)
            {
                _labelTween?.Kill();
                Color targetColor = focused ? focusedTextColor : normalTextColor;
                if (animate)
                {
                    _labelTween = DOTween.To(
                            () => label.color,
                            value => label.color = value,
                            targetColor,
                            focusDuration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                }
                else
                {
                    label.color = targetColor;
                }
            }

            if (focusMarker == null)
                return;

            _markerColorTween?.Kill();
            _markerPositionTween?.Kill();

            Color targetMarkerColor = _markerBaseColor;
            targetMarkerColor.a = focused ? _markerBaseColor.a : 0f;
            Vector2 targetPosition = focused
                ? _markerBasePosition
                : _markerBasePosition + Vector2.right * markerStartOffset;

            if (!animate)
            {
                focusMarker.color = targetMarkerColor;
                focusMarker.rectTransform.anchoredPosition = targetPosition;
                return;
            }

            _markerColorTween = DOTween.To(
                    () => focusMarker.color,
                    value => focusMarker.color = value,
                    targetMarkerColor,
                    focusDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            _markerPositionTween = DOTween.To(
                    () => focusMarker.rectTransform.anchoredPosition,
                    value => focusMarker.rectTransform.anchoredPosition = value,
                    targetPosition,
                    focusDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

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

        private void KillTweens()
        {
            _entranceTween?.Kill();
            _labelTween?.Kill();
            _markerColorTween?.Kill();
            _markerPositionTween?.Kill();
            _entranceTween = null;
            _labelTween = null;
            _markerColorTween = null;
            _markerPositionTween = null;
        }
    }
}
