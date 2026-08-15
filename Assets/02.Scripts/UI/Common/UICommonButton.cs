

using TMPro;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    public enum UICommonButtonClickResult
    {
        None = 0,
        Success,
        Failed,
    }

    public class UICommonButton : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISubmitHandler,
        IUIFocusPresentation
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _buttonText;

        [Header("공용 비주얼")]
        [SerializeField] private UIVisualThemeSO _theme;
        [Tooltip("비우면 이 컴포넌트의 RectTransform을 사용한다.")]
        [SerializeField] private RectTransform _visualTarget;
        [Tooltip("비우면 Button.targetGraphic의 Image를 사용한다.")]
        [SerializeField] private Image _background;
        [SerializeField] private bool _animateScale = true;
        [SerializeField] private bool _tintBackground = true;
        [SerializeField, Range(0.01f, 0.5f)] private float _fallbackDuration = 0.10f;
        [SerializeField, Range(1f, 1.15f)] private float _fallbackFocusScale = 1.035f;
        [SerializeField, Range(0.8f, 1f)] private float _fallbackPressedScale = 0.96f;

        public TextMeshProUGUI Text => this._buttonText;
        public Button Button => _button;
        public UICommonButtonClickResult LastClickResult { get; private set; } = UICommonButtonClickResult.None;

        public event Action<UICommonButtonClickResult> OnClickResultChanged;

        private Func<UICommonButtonClickResult> _clickResultHandler;
        private Vector3 _baseScale;
        private Color _baseBackgroundColor = Color.white;
        private bool _selected;
        private bool _hovered;
        private bool _pressed;
        private bool _lastInteractable;
        private Tween _scaleTween;
        private Tween _colorTween;
        private Tween _submitTween;
        private Sequence _resultSequence;

        public bool SuppressGlobalFocusIndicator =>
            (_animateScale && _visualTarget != null)
            || (_tintBackground && _background != null);

        public RectTransform GlobalFocusIndicatorTarget => null;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_visualTarget == null)
                _visualTarget = transform as RectTransform;
            if (_background == null && _button != null)
                _background = _button.targetGraphic as Image;

            if (_visualTarget != null)
                _baseScale = _visualTarget.localScale;
            if (_background != null)
                _baseBackgroundColor = _background.color;

            _lastInteractable = _button == null || _button.interactable;
            _button?.onClick.AddListener(InvokeClickResultHandler);
            ApplyVisual(animate: false);
        }

        private void LateUpdate()
        {
            bool interactable = _button == null || _button.interactable;
            if (interactable == _lastInteractable)
                return;

            _lastInteractable = interactable;
            if (!interactable)
            {
                _pressed = false;
                _hovered = false;
            }
            ApplyVisual(animate: true);
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(InvokeClickResultHandler);
            KillTweens();
        }

        private void OnDisable()
        {
            KillTweens();
            _selected = false;
            _hovered = false;
            _pressed = false;
            ApplyVisual(animate: false);
        }

        public void BindClickResult(Func<UICommonButtonClickResult> handler)
        {
            _clickResultHandler = handler;
            LastClickResult = UICommonButtonClickResult.None;
        }

        public void ClearClickResult()
        {
            _clickResultHandler = null;
            LastClickResult = UICommonButtonClickResult.None;
        }

        private void InvokeClickResultHandler()
        {
            if (_clickResultHandler == null)
            {
                return;
            }

            LastClickResult = _clickResultHandler.Invoke();
            OnClickResultChanged?.Invoke(LastClickResult);
            PlayResultFeedback(LastClickResult);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            ApplyVisual(animate: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
            ApplyVisual(animate: true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisual(animate: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyVisual(animate: true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            _pressed = true;
            ApplyVisual(animate: true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyVisual(animate: true);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            _pressed = true;
            ApplyVisual(animate: true);

            _submitTween?.Kill();
            _submitTween = DOVirtual.DelayedCall(
                    Mathf.Max(0.06f, Duration),
                    () =>
                    {
                        _pressed = false;
                        ApplyVisual(animate: true);
                    },
                    ignoreTimeScale: true)
                .SetUpdate(true);
        }

        private void ApplyVisual(bool animate)
        {
            bool interactable = _button == null || _button.interactable;
            bool focused = interactable && (_selected || _hovered);
            float scale = _pressed
                ? PressedScale
                : focused
                    ? FocusScale
                    : 1f;

            Color color = !interactable
                ? DisabledColor
                : _pressed
                    ? Color.Lerp(_baseBackgroundColor, Color.black, 0.18f)
                    : focused
                        ? Color.Lerp(_baseBackgroundColor, FocusColor, 0.22f)
                        : _baseBackgroundColor;

            _scaleTween?.Kill();
            _colorTween?.Kill();

            float duration = animate ? Duration : 0f;
            if (_animateScale && _visualTarget != null)
            {
                Vector3 targetScale = _baseScale * scale;
                if (duration <= 0f)
                {
                    _visualTarget.localScale = targetScale;
                }
                else
                {
                    _scaleTween = DOTween.To(
                            () => _visualTarget.localScale,
                            value => _visualTarget.localScale = value,
                            targetScale,
                            duration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                }
            }

            if (_tintBackground && _background != null)
            {
                if (duration <= 0f)
                {
                    _background.color = color;
                }
                else
                {
                    _colorTween = DOTween.To(
                            () => _background.color,
                            value => _background.color = value,
                            color,
                            duration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                }
            }
        }

        private void PlayResultFeedback(UICommonButtonClickResult result)
        {
            if (_background == null || result == UICommonButtonClickResult.None)
                return;

            _colorTween?.Kill();
            _colorTween = null;
            _resultSequence?.Kill();
            Color feedback = result == UICommonButtonClickResult.Success
                ? PositiveColor
                : NegativeColor;

            _resultSequence = DOTween.Sequence().SetUpdate(true);
            _resultSequence.Append(DOTween.To(
                () => _background.color,
                value => _background.color = value,
                feedback,
                0.08f));
            _resultSequence.AppendInterval(0.04f);
            _resultSequence.AppendCallback(() => ApplyVisual(animate: true));
        }

        private UIVisualThemeSO Theme => _theme != null
            ? _theme
            : UIVisualThemeProvider.Current;

        private float Duration => Theme != null
            ? Theme.FocusDuration
            : _fallbackDuration;

        private float FocusScale => Theme != null
            ? Theme.FocusScale
            : _fallbackFocusScale;

        private float PressedScale => Theme != null
            ? Theme.PressedScale
            : _fallbackPressedScale;

        private Color FocusColor => Theme != null
            ? Theme.Focus
            : new Color(0.82f, 0.65f, 0.32f, 1f);

        private Color PositiveColor => Theme != null
            ? Theme.Positive
            : new Color(0.34f, 0.82f, 0.53f, 1f);

        private Color NegativeColor => Theme != null
            ? Theme.Negative
            : new Color(0.90f, 0.30f, 0.32f, 1f);

        private Color DisabledColor => Theme != null
            ? Theme.Disabled
            : new Color(0.36f, 0.39f, 0.43f, 1f);

        private void KillTweens()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _submitTween?.Kill();
            _resultSequence?.Kill();
            _scaleTween = null;
            _colorTween = null;
            _submitTween = null;
            _resultSequence = null;
        }
    }
}
