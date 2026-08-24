using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>획득 아이템의 희귀도·누적 수량을 짧은 팝인과 함께 보여주는 HUD 항목.</summary>
    public class UI_HUD_ItemAcquisitionEntry : UI_Base
    {
        // 루트 RectTransform은 부모 LayoutGroup이 매 리빌드마다 anchoredPosition을 덮어쓰므로
        // 슬라이드·스케일 연출은 반드시 이 내부 래퍼에서만 돌린다. 루트를 직접 트윈하면
        // 레이아웃과 트윈이 서로 위치를 덮어써서 항목이 겹치거나 튄다.
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private TextMeshProUGUI _itemInfoText;
        [SerializeField] private Image _rarityIcon;
        [SerializeField] private Image _itemIcon;

        [Header("연출")]
        [Min(0.01f)] [SerializeField] private float _introDuration = 0.12f;
        [Min(0f)] [SerializeField] private float _holdDuration = 1.25f;
        [Min(0.01f)] [SerializeField] private float _outroDuration = 0.16f;
        [Min(0f)] [SerializeField] private float _slideDistance = 36f;
        [Range(0.1f, 1f)] [SerializeField] private float _introScale = 0.90f;
        [Min(1f)] [SerializeField] private float _mergePulseScale = 1.06f;

        private Sequence _sequence;
        private Action<UI_HUD_ItemAcquisitionEntry> _onExpired;
        private ItemSO _itemData;
        private int _count;
        private Vector2 _restingPosition;
        private Vector3 _restingScale;
        private bool _isExpiring;

        protected override void Awake()
        {
            base.Awake();
            if (_visualRoot == null)
                _visualRoot = _rectTransform;

            _restingPosition = _visualRoot.anchoredPosition;
            _restingScale = _visualRoot.localScale;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _animator ??= GetComponent<Animator>();
            if (_animator != null)
                _animator.enabled = false;
        }

        /// <summary>표시할 아이템과 수량을 설정하고 진입 연출을 시작한다.</summary>
        public void Init(
            ItemSO itemData,
            int count,
            Action<UI_HUD_ItemAcquisitionEntry> onExpired)
        {
            _itemData = itemData;
            _count = Mathf.Max(1, count);
            _onExpired = onExpired;

            ApplyItemData();
            PlayEntrance();
        }

        /// <summary>병합된 획득 수량을 더하고 항목의 노출 시간을 갱신한다.</summary>
        public void AddCount(int count)
        {
            if (_isExpiring)
                return;

            _count += Mathf.Max(1, count);
            UpdateInfoText();
            PlayMergePulse();
        }

        /// <summary>표시 한도를 넘긴 항목을 대기 시간 없이 즉시 퇴장시킨다.</summary>
        public void ExpireImmediately()
        {
            if (_isExpiring)
                return;

            _isExpiring = true;
            KillSequence();
            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(CreateAlphaTween(0f, _outroDuration, Ease.InCubic));
            _sequence.OnComplete(Expire).SetUpdate(true);
        }

        private void ApplyItemData()
        {
            if (_itemData == null)
                return;

            _rarityIcon.color = _itemData.itemRarity.ToColor();
            _itemIcon.sprite = _itemData.icon;
            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            if (_itemData == null)
                return;

            _itemInfoText.text = _count > 1
                ? $"{_itemData.itemName}  ×{_count}"
                : _itemData.itemName;
        }

        private void PlayEntrance()
        {
            KillSequence();
            _canvasGroup.alpha = 0f;
            _visualRoot.anchoredPosition = _restingPosition + Vector2.right * _slideDistance;
            _visualRoot.localScale = _restingScale * _introScale;

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(CreatePositionTween(_restingPosition, _introDuration, Ease.OutCubic));
            _sequence.Join(CreateAlphaTween(1f, _introDuration, Ease.OutQuad));
            _sequence.Join(CreateScaleTween(_restingScale, _introDuration, Ease.OutBack));
            AppendLifetime();
        }

        private void PlayMergePulse()
        {
            KillSequence();
            _canvasGroup.alpha = 1f;
            _visualRoot.anchoredPosition = _restingPosition;
            _visualRoot.localScale = _restingScale * _mergePulseScale;

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(CreateScaleTween(_restingScale, _introDuration, Ease.OutBack));
            AppendLifetime();
        }

        private void AppendLifetime()
        {
            _sequence.AppendInterval(_holdDuration);
            _sequence.Append(CreateAlphaTween(0f, _outroDuration, Ease.InCubic));
            _sequence.AppendCallback(() => _isExpiring = true);
            _sequence.OnComplete(Expire).SetUpdate(true);
        }

        private Tween CreatePositionTween(Vector2 target, float duration, Ease ease)
        {
            return DOTween.To(
                    () => _visualRoot.anchoredPosition,
                    value => _visualRoot.anchoredPosition = value,
                    target,
                    duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private Tween CreateAlphaTween(float target, float duration, Ease ease)
        {
            return DOTween.To(
                    () => _canvasGroup.alpha,
                    value => _canvasGroup.alpha = value,
                    target,
                    duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private Tween CreateScaleTween(Vector3 target, float duration, Ease ease)
        {
            return DOTween.To(
                    () => _visualRoot.localScale,
                    value => _visualRoot.localScale = value,
                    target,
                    duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private void Expire()
        {
            _sequence = null;
            _isExpiring = true;
            _onExpired?.Invoke(this);
            Destroy(gameObject);
        }

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        protected override void OnDispose()
        {
            KillSequence();
            _onExpired = null;
            base.OnDispose();
        }
    }
}
