using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>목록이 비었을 때 이유와 다음 행동을 함께 안내하는 공용 빈 상태.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIEmptyStateView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _hint;
        [SerializeField, Range(0.08f, 0.3f)] private float _duration = 0.15f;

        private CanvasGroup _group;
        private RectTransform _rect;
        private Vector3 _baseScale;
        private Sequence _sequence;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Configure(TextMeshProUGUI title, TextMeshProUGUI hint)
        {
            _title = title;
            _hint = hint;
        }

        public void Show(string title, string hint)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            ResolveReferences();

            if (_title != null)
                _title.text = title;
            if (_hint != null)
                _hint.text = hint;

            _sequence?.Kill();
            _group.alpha = 0f;
            _rect.localScale = _baseScale * 0.96f;
            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Join(DOTween.To(
                () => _group.alpha,
                value => _group.alpha = value,
                1f,
                _duration));
            _sequence.Join(_rect.DOScale(_baseScale, _duration).SetEase(Ease.OutQuad));
        }

        public void Hide(bool immediate = false)
        {
            if (!gameObject.activeSelf)
                return;

            ResolveReferences();
            _sequence?.Kill();
            if (immediate || !isActiveAndEnabled)
            {
                _group.alpha = 0f;
                gameObject.SetActive(false);
                return;
            }

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(DOTween.To(
                () => _group.alpha,
                value => _group.alpha = value,
                0f,
                0.10f));
            _sequence.OnComplete(() => gameObject.SetActive(false));
        }

        private void ResolveReferences()
        {
            _group ??= GetComponent<CanvasGroup>();
            _rect ??= transform as RectTransform;
            if (_rect != null && _baseScale == default)
                _baseScale = _rect.localScale;
            if (_title == null)
                _title = transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (_hint == null)
                _hint = transform.Find("Hint")?.GetComponent<TextMeshProUGUI>();
        }
    }
}
