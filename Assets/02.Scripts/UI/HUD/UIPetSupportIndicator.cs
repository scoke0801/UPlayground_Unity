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
        [SerializeField] private int _slotIndex;
        [SerializeField, Range(0f, 1f)] private float _unavailableAlpha = .35f;
        [SerializeField, Range(0f, 1f)] private float _flashMinimumAlpha = .45f;
        [SerializeField, Min(0f)] private float _flashFrequency = 18f;
        private IPetService _pets;
        private IPetSupportFeedbackService _feedback;
        private IPetProgressionService _progression;
        private int _signatureVersion;
        private float _signatureEnds;
        private int _activationVersion;
        private float _flashEnds;
        private float _nextRefresh;
        private bool _available;

        private void OnEnable()
        {
            Services.TryGet(out _pets); Services.TryGet(out _feedback);
            Services.TryGet(out _progression);
            _signatureVersion = _progression?.SignatureActivationVersion ?? 0;
            _activationVersion = _feedback?.SupportActivationVersion ?? 0;
            _flashEnds = 0f; _nextRefresh = 0f;
            _signatureEnds = 0f;
            if (_icon != null) _icon.enabled = false;
            if (_status != null) _status.enabled = false;
        }

        private void Update()
        {
            if (_icon == null || _status == null) return;
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + .25f;
                if (_pets == null) Services.TryGet(out _pets);
                if (_feedback == null) Services.TryGet(out _feedback);
                if (_progression == null) Services.TryGet(out _progression);
                string id = _progression != null ? _progression.GetSupportId(_slotIndex) : _feedback?.SelectedSupportId;
                var skill = _pets?.Catalog?.skills?.Find(id);
                _icon.sprite = skill?.icon;
                _available = skill?.supportAbility != null && _feedback?.CanSupport == true;
            }
            int version = _feedback?.SupportActivationVersion ?? 0;
            if (_activationVersion != version)
            {
                _activationVersion = version;
                if ((_progression?.LastActivatedSupportSlot ?? 0) == _slotIndex)
                    _flashEnds = Time.unscaledTime + (_pets?.Catalog?.skills?.supportFlashDuration ?? 0f);
            }
            int signature = _progression?.SignatureActivationVersion ?? 0;
            if (_signatureVersion != signature)
            {
                _signatureVersion = signature;
                var ability = _pets?.Catalog?.skills?.signatureAbility;
                float duration = ability != null && ability.variants.Count > 0 && ability.variants[0].targetEffects.Count > 0
                    ? ability.variants[0].targetEffects[0].durationSeconds : 0f;
                _signatureEnds = Time.unscaledTime + duration;
                _flashEnds = _signatureEnds;
            }
            bool unlocked = _slotIndex < (_progression?.SupportSlotCount ?? 1);
            bool visible = unlocked && Time.unscaledTime < _flashEnds;
            _icon.enabled = visible && _icon.sprite != null;
            _status.enabled = visible;
            if (visible)
            {
                string text = Time.unscaledTime < _signatureEnds ? "손놀림이 가벼워요" : "도와줄게요";
                if (_status.text != text) _status.text = text;
            }
            float alpha = Time.unscaledTime < _flashEnds
                ? _flashMinimumAlpha + (1f - _flashMinimumAlpha) * Mathf.Abs(Mathf.Sin(Time.unscaledTime * _flashFrequency))
                : _available ? 1f : _unavailableAlpha;
            _icon.color = Time.unscaledTime < _signatureEnds ? new Color(1f, .8f, .35f, alpha) : new Color(1f, 1f, 1f, alpha);
        }
    }
}
