using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>
    /// HUD 소비 아이템 퀵슬롯 한 칸.
    /// 아이템 사용 규칙은 직접 판단하지 않고 <see cref="IUIInventoryService"/>에 위임한다.
    /// </summary>
    public sealed class UIHudQuickSlotEntry : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private Image _slotBackground;
        [SerializeField] private Outline _rarityOutline;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameObject _countRoot;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _emptyMark;
        [SerializeField] private CanvasGroup _stateGroup;
        [SerializeField] private Button _useButton;
        [SerializeField] private GameObject _cooldownRoot;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TextMeshProUGUI _cooldownText;
        [Header("트윈 (DOTween)")]
        [SerializeField] private RectTransform _tweenTarget;
        [SerializeField] private float _usePunch = 0.16f;
        [SerializeField] private float _useDuration = 0.25f;

        public int ItemId => UIQuickSlotAssignments.GetItemId(_slotIndex);

        private Vector3 _baseScale = Vector3.one;
        private Tween _useTween;
        private bool _hasUsableCount;

        private void Awake()
        {
            if (_tweenTarget != null)
                _baseScale = _tweenTarget.localScale;

            if (_useButton != null)
                _useButton.onClick.AddListener(TryUse);
        }

        private void OnDestroy()
        {
            KillTween();

            if (_useButton != null)
                _useButton.onClick.RemoveListener(TryUse);
        }

        private void OnDisable()
        {
            KillTween();
        }

        public void Refresh(IUIInventoryService inventory)
        {
            int itemId = ItemId;
            ItemInstance item = inventory != null && itemId > 0
                ? inventory.GetItem(itemId)
                : null;
            int count = item != null
                ? inventory.GetItemCount(itemId)
                : 0;

            if (_iconImage != null)
            {
                _iconImage.sprite = item?.data?.icon;
                _iconImage.enabled = _iconImage.sprite != null;
            }

            bool useRarityAccent = item?.data != null
                && item.data.itemRarity > ItemRarity.COMMON;
            Color rarityColor = useRarityAccent
                ? item.data.itemRarity.ToColor()
                : Color.clear;
            if (_rarityOutline != null)
                _rarityOutline.effectColor = rarityColor.a > 0f
                    ? rarityColor
                    : new Color(0.12f, 0.68f, 1f, 0.95f);
            if (_slotBackground != null)
            {
                Color baseColor = new Color(0.025f, 0.075f, 0.13f, 0.88f);
                _slotBackground.color = rarityColor.a > 0f
                    ? Color.Lerp(baseColor, rarityColor, 0.24f)
                    : baseColor;
            }

            if (_countText != null)
            {
                _countText.text = count > 0 ? count.ToString() : string.Empty;
                _countText.gameObject.SetActive(itemId > 0);
            }
            if (_countRoot != null)
                _countRoot.SetActive(itemId > 0);

            if (_emptyMark != null)
                _emptyMark.SetActive(itemId <= 0);

            if (_stateGroup != null)
                _stateGroup.alpha = itemId <= 0 ? 1f : count > 0 ? 1f : 0.38f;

            _hasUsableCount = count > 0;
            RefreshCooldown(inventory);
        }

        public void RefreshCooldown(IUIInventoryService inventory)
        {
            int itemId = ItemId;
            float remaining = 0f;
            float duration = 0f;
            bool onCooldown = inventory != null
                && itemId > 0
                && inventory.TryGetConsumableCooldown(
                    itemId, out remaining, out duration);

            if (_cooldownRoot != null)
                _cooldownRoot.SetActive(onCooldown);
            if (_cooldownFill != null)
                _cooldownFill.fillAmount = onCooldown && duration > 0f
                    ? Mathf.Clamp01(remaining / duration)
                    : 0f;
            if (_cooldownText != null)
                _cooldownText.text = onCooldown ? remaining.ToString("0.0") : string.Empty;
            if (_useButton != null)
                _useButton.interactable = _hasUsableCount && !onCooldown;
        }

        public void TryUse()
        {
            var inventory = UISvc.Inventory;
            int itemId = ItemId;
            if (inventory == null || itemId <= 0)
                return;

            InventoryActionResult result = inventory.TryUseItem(itemId);
            if (result == InventoryActionResult.Success)
                PlayUseFeedback();

            Refresh(inventory);
        }

        private void PlayUseFeedback()
        {
            if (_tweenTarget == null || !_tweenTarget.gameObject.activeInHierarchy)
                return;

            _useTween?.Kill(complete: true);
            _tweenTarget.localScale = _baseScale;
            _useTween = _tweenTarget
                .DOPunchScale(Vector3.one * _usePunch, _useDuration, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(true);
        }

        private void KillTween()
        {
            _useTween?.Kill(complete: true);
            _useTween = null;
            if (_tweenTarget != null)
                _tweenTarget.localScale = _baseScale;
        }
    }
}
