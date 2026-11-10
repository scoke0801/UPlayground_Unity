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
        public RectTransform Rect => (RectTransform)transform;
        public void Initialize(string text, Sprite icon, Color color, float duration, Action collect)
        {
            _text.text = text; _text.color = color; _itemIcon.sprite = icon;
            _collect = collect;
            _button.onClick.AddListener(Collect);
            _group.alpha = 0f;
            _fade = DOTween.To(() => _group.alpha, value => _group.alpha = value, 1f, duration).SetUpdate(true);
        }
        public void SetFocused(bool focused) => _text.fontStyle = focused ? FontStyles.Bold : FontStyles.Normal;
        private void Collect()
        {
            if (Cursor.visible && Cursor.lockState != CursorLockMode.Locked) _collect?.Invoke();
        }
        private void OnDestroy() { _fade?.Kill(); _button.onClick.RemoveListener(Collect); }
    }
}
