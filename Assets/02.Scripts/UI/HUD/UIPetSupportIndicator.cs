using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>지원 가능 여부와 성립 순간을 기존 펫 아이콘으로 알린다.</summary>
    public sealed class UIPetSupportIndicator : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _status;
        [SerializeField, Range(0f, 1f)] private float _unavailableAlpha = .35f;
        [SerializeField, Range(0f, 1f)] private float _flashMinimumAlpha = .45f;
        [SerializeField, Min(0f)] private float _flashFrequency = 18f;
        private IPetService _pets;
        private IPetSupportFeedbackService _feedback;
        private int _activationVersion;
        private float _flashEnds;
        private float _nextRefresh;
        private bool _available;

        private void OnEnable()
        {
            Services.TryGet(out _pets); Services.TryGet(out _feedback);
            _activationVersion = _feedback?.SupportActivationVersion ?? 0;
            _flashEnds = 0f; _nextRefresh = 0f;
        }

        private void Update()
        {
            if (_icon == null || _status == null) return;
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + .25f;
                if (_pets == null) Services.TryGet(out _pets);
                if (_feedback == null) Services.TryGet(out _feedback);
                var skill = _pets?.Catalog?.skills?.Find(_feedback?.SelectedSupportId);
                _icon.enabled = skill?.icon != null;
                _icon.sprite = skill?.icon;
                _available = _feedback?.CanSupport == true;
                string text = _available ? "도움 대기" : "지금은 쉬는 중";
                if (_status.text != text) _status.text = text;
            }
            int version = _feedback?.SupportActivationVersion ?? 0;
            if (_activationVersion != version)
            {
                _activationVersion = version;
                _flashEnds = Time.unscaledTime + (_pets?.Catalog?.skills?.supportFlashDuration ?? 0f);
            }
            float alpha = Time.unscaledTime < _flashEnds
                ? _flashMinimumAlpha + (1f - _flashMinimumAlpha) * Mathf.Abs(Mathf.Sin(Time.unscaledTime * _flashFrequency))
                : _available ? 1f : _unavailableAlpha;
            _icon.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
