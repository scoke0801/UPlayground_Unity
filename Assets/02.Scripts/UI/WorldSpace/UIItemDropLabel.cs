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
        private UIDropLabelConnector _tailGraphic;
        private Color _tailColor;
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
        public void ConfigureTail(float width, Color color, float opacity)
        {
            if (_tail == null)
            {
                var go = new GameObject("Tail", typeof(RectTransform), typeof(UIDropLabelConnector));
                _tail = go.GetComponent<RectTransform>();
                // 별도 형제로 내려 모든 이름표 배경 뒤에 선을 그린다.
                _tail.SetParent(transform.parent, false);
                _tail.SetAsFirstSibling();
                _tail.anchorMin = _tail.anchorMax = Rect.anchorMin;
                _tail.pivot = new Vector2(0.5f, 1f);
                _tailGraphic = go.GetComponent<UIDropLabelConnector>();
                _tailGraphic.raycastTarget = false;
            }
            color.a = opacity;
            _tailColor = color;
            _tail.sizeDelta = new Vector2(width, _tail.sizeDelta.y);
        }

        /// <summary>실제 모델을 향해 선을 기울이고 양 끝에 여백을 둔다.</summary>
        public void SetTailTarget(Vector2 target)
        {
            if (_tail == null) return;
            float gap = _tail.sizeDelta.x * 3f;
            Vector2 start = new Vector2(0f, -Rect.rect.height * Rect.pivot.y - gap);
            Vector2 delta = target - start;
            float length = delta.magnitude - gap;
            bool show = delta.y < 0f && length > gap;
            if (_tail.gameObject.activeSelf != show) _tail.gameObject.SetActive(show);
            if (!show) return;
            _tail.localScale = Rect.localScale;
            _tail.position = Rect.TransformPoint(start);
            _tail.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.down, delta));
            _tail.sizeDelta = new Vector2(_tail.sizeDelta.x, length);
            Color color = _tailColor;
            color.a *= _group.alpha;
            _tailGraphic.color = color;
        }

        public void SetFocused(bool focused) => _text.fontStyle = focused ? FontStyles.Bold : FontStyles.Normal;
        private void Collect()
        {
            if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked) _collect?.Invoke();
        }
        private void OnDisable() { if (_tail != null) _tail.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            _fade?.Kill();
            if (_button != null) _button.onClick.RemoveListener(Collect);
            if (_tail != null) Destroy(_tail.gameObject);
        }
    }
}
