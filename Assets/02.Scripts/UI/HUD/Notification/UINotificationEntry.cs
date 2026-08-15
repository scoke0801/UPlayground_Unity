using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.HUD.Notification
{
    public class UINotificationEntry : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _accentImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private float _fadeInSeconds = 0.15f;
        [SerializeField] private float _holdSeconds = 2.4f;
        [SerializeField] private float _fadeOutSeconds = 0.3f;

        private Coroutine _routine;

        public void Init(string title, string message, Sprite icon, Color accentColor)
        {
            CacheComponents();

            if (_titleText != null)
                _titleText.text = title ?? string.Empty;

            if (_messageText != null)
                _messageText.text = message ?? string.Empty;

            if (_accentImage != null)
                _accentImage.color = accentColor;

            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayRoutine());
        }

        public void ForceClose()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            Destroy(gameObject);
        }

        private void CacheComponents()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private IEnumerator PlayRoutine()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            yield return FadeTo(1f, _fadeInSeconds);
            yield return new WaitForSecondsRealtime(_holdSeconds);
            yield return FadeTo(0f, _fadeOutSeconds);

            _routine = null;
            Destroy(gameObject);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (_canvasGroup == null || duration <= 0f)
            {
                if (_canvasGroup != null)
                    _canvasGroup.alpha = target;
                yield break;
            }

            float start = _canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = target;
        }
    }
}
