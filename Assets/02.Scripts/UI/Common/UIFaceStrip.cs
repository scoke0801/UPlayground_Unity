using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>전투 시계와 분리된 얼굴 패널 한 개를 소유 토큰으로 교체한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UIFaceStrip : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _background;
        [SerializeField, Range(0f, 1f)] private float _backgroundOpacity = 0.55f;
        [SerializeField] private Image _face;
        [SerializeField] private Image _flash;

        private static long _nextToken;
        private long _token;
        private Tween _timer;
        private bool _isDisposed;

        public bool IsVisible => _token != 0;

        /// <summary>이전 표시를 즉시 정리하고 새 표시의 소유 토큰을 반환한다.</summary>
        public long Show(FaceStripRequest request)
        {
            HideCurrent();
            if (_isDisposed || !isActiveAndEnabled || request.Face == null
                || _panel == null || _group == null || _background == null
                || _face == null || _flash == null)
                return 0;

            float duration = request.InDuration + request.HoldDuration + request.OutDuration;
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                return 0;

            long token = Interlocked.Increment(ref _nextToken);
            _token = token;
            _face.sprite = request.Face;
            Color backgroundColor = request.Accent;
            backgroundColor.a *= Mathf.Clamp01(_backgroundOpacity);
            _background.color = backgroundColor;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            ApplyFrame(request, 0f);
            float elapsed = 0f;
            _timer = DOTween.To(() => elapsed, value =>
                {
                    elapsed = value;
                    if (_token == token)
                        ApplyFrame(request, value);
                }, duration, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() => Hide(token));
            return token;
        }

        /// <summary>현재 표시를 소유한 토큰의 요청만 즉시 닫는다.</summary>
        public void Hide(long token)
        {
            if (token != 0 && token == _token)
                HideCurrent();
        }

        /// <summary>일시정지와 씬 전환에서 현재 표시를 즉시 정리한다.</summary>
        public void HideCurrent()
        {
            _token = 0;
            _timer?.Kill(false);
            _timer = null;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
            if (_face != null)
            {
                _face.sprite = null;
                _face.color = new Color(1f, 1f, 1f, 0f);
                _face.rectTransform.localScale = Vector3.one;
            }
            if (_flash != null)
                _flash.color = Color.clear;
            if (_panel != null)
                _panel.anchoredPosition = Vector2.zero;
        }

        /// <summary>종료 후 재표시를 거부하고 남은 트윈을 해제한다.</summary>
        public void Dispose()
        {
            _isDisposed = true;
            HideCurrent();
        }

        private void Awake() => HideCurrent();
        private void OnDisable() => HideCurrent();
        private void OnDestroy() => Dispose();

        private void ApplyFrame(FaceStripRequest request, float elapsed)
        {
            float entered = request.InDuration > 0f
                ? Mathf.Clamp01(elapsed / request.InDuration) : 1f;
            float holdEnd = request.InDuration + request.HoldDuration;
            float exited = elapsed >= holdEnd
                ? (request.OutDuration > 0f
                    ? Mathf.Clamp01((elapsed - holdEnd) / request.OutDuration) : 1f)
                : 0f;
            float entryEase = 1f - Mathf.Pow(1f - entered, 3f);
            _group.alpha = entered * (1f - exited);
            _panel.anchoredPosition = new Vector2(
                -_panel.rect.width * (1f - entryEase + exited), 0f);
            float faceDuration = request.InDuration - request.FaceDelay;
            float faceAlpha = elapsed < request.FaceDelay ? 0f
                : (faceDuration > 0f ? Mathf.Clamp01((elapsed - request.FaceDelay) / faceDuration) : 1f);
            _face.color = new Color(1f, 1f, 1f, faceAlpha);
            float holdProgress = request.HoldDuration > 0f
                ? Mathf.Clamp01((elapsed - request.InDuration) / request.HoldDuration) : 0f;
            _face.rectTransform.localScale = Vector3.one
                * (1f + request.BreathScale * Mathf.Sin(holdProgress * Mathf.PI));
            float flashStart = holdEnd - request.FlashDuration;
            float flashAlpha = elapsed >= flashStart && request.FlashDuration > 0f
                ? 1f - Mathf.Clamp01((elapsed - flashStart) / request.FlashDuration) : 0f;
            _flash.color = new Color(request.Accent.r, request.Accent.g, request.Accent.b,
                flashAlpha * request.Accent.a);
        }
    }
}
