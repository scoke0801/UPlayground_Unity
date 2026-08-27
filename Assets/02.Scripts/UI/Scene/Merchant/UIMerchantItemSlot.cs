using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>상점 목록의 품목 정보와 포커스 피드백을 표시한다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIMerchantItemSlot : MonoBehaviour, ISelectHandler, IUIFocusPresentation
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _itemName;
        [SerializeField] private TextMeshProUGUI _price;
        [SerializeField] private TextMeshProUGUI _secondary;
        [SerializeField] private GameObject _selectedOverlay;
        [SerializeField] private RectTransform _visualTarget;

        private Button _button;
        private UI_Scene_Merchant _owner;
        private int _listingIndex;
        private Vector3 _baseScale;
        private Tween _focusTween;

        public Selectable Selectable => _button;
        public bool SuppressGlobalFocusIndicator => _selectedOverlay != null;
        public RectTransform GlobalFocusIndicatorTarget => _visualTarget;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _visualTarget ??= transform as RectTransform;
            _baseScale = _visualTarget != null ? _visualTarget.localScale : Vector3.one;
            _button.onClick.AddListener(SelectListing);
        }

        /// <summary>목록 인덱스와 표시할 거래 품목을 슬롯에 연결한다.</summary>
        public void Initialize(
            int listingIndex,
            ItemSO item,
            int unitPrice,
            string secondary,
            UI_Scene_Merchant owner)
        {
            _listingIndex = listingIndex;
            _owner = owner;

            _itemName.text = item != null ? item.itemName : string.Empty;
            _price.text = $"{unitPrice:N0} G";
            _secondary.text = secondary ?? string.Empty;
            if (_itemIcon != null)
            {
                _itemIcon.sprite = item != null ? item.icon : null;
                _itemIcon.enabled = _itemIcon.sprite != null;
            }

            SetSelected(false, false);
        }

        /// <summary>현재 상세 선택과 일치하는지 시각적으로 표시한다.</summary>
        public void SetSelected(bool selected, bool animate = true)
        {
            if (_selectedOverlay != null)
                _selectedOverlay.SetActive(selected);

            if (_visualTarget == null)
                return;

            _focusTween?.Kill();
            UIVisualThemeSO theme = UIVisualThemeProvider.Current;
            Vector3 targetScale = _baseScale * (selected
                ? theme != null ? theme.FocusScale : 1.035f
                : 1f);
            float duration = animate
                ? theme != null ? theme.FocusDuration : 0.1f
                : 0f;

            if (duration <= 0f)
            {
                _visualTarget.localScale = targetScale;
                return;
            }

            _focusTween = DOTween.To(
                    () => _visualTarget.localScale,
                    value => _visualTarget.localScale = value,
                    targetScale,
                    duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void OnSelect(BaseEventData eventData) => SelectListing();

        private void SelectListing()
        {
            _owner?.SelectListing(_listingIndex);
        }

        private void OnDisable()
        {
            _focusTween?.Kill();
            _focusTween = null;
            if (_visualTarget != null)
                _visualTarget.localScale = _baseScale;
        }

        private void OnDestroy()
        {
            _button?.onClick.RemoveListener(SelectListing);
            _focusTween?.Kill();
        }
    }
}
