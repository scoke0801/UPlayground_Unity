using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>드롭 이름의 포커스와 클릭을 표시하며 지급은 서비스에 요청한다.</summary>
    public sealed class UIItemDropLabel : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private CanvasGroup _group;
        private Tween _fade;
        private Action _collect;
        private RectTransform _tail;
        private Image _tailImage;
        public RectTransform Rect => (RectTransform)transform;
        public void Initialize(string text, Sprite icon, Color color, float duration, Action collect)
        {
            _text.text = text; _text.color = color; _itemIcon.sprite = icon;
            _collect = collect;
            _button.onClick.AddListener(Collect);
            _group.alpha = 0f;
            _fade = DOTween.To(() => _group.alpha, value => _group.alpha = value, 1f, duration).SetUpdate(true);
        }

        /// <summary>이름표 아래로 늘어져 월드 아이템을 가리키는 연결선을 준비한다.</summary>
        public void ConfigureTail(float width, Color color)
        {
            if (_tail == null)
            {
                var go = new GameObject("Tail", typeof(RectTransform), typeof(Image));
                _tail = go.GetComponent<RectTransform>();
                _tail.SetParent(transform, false);
                _tail.anchorMin = _tail.anchorMax = new Vector2(0.5f, 0f);
                _tail.pivot = new Vector2(0.5f, 1f);
                _tail.anchoredPosition = Vector2.zero;
                _tailImage = go.GetComponent<Image>();
                _tailImage.raycastTarget = false;
            }
            color.a = 0.85f;
            _tailImage.color = color;
            _tail.sizeDelta = new Vector2(width, _tail.sizeDelta.y);
        }

        /// <summary>연결선 길이를 라벨 로컬 단위로 설정한다. 너무 짧으면 숨긴다.</summary>
        public void SetTailLength(float length)
        {
            if (_tail == null) return;
            bool show = length > 4f;
            if (_tail.gameObject.activeSelf != show) _tail.gameObject.SetActive(show);
            if (show) _tail.sizeDelta = new Vector2(_tail.sizeDelta.x, length);
        }

        public void SetFocused(bool focused) => _text.fontStyle = focused ? FontStyles.Bold : FontStyles.Normal;
        private void Collect()
        {
            if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked) _collect?.Invoke();
        }
        private void OnDestroy() { _fade?.Kill(); _button.onClick.RemoveListener(Collect); }
    }
}
