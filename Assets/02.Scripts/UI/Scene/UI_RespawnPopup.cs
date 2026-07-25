using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 플레이어 사망 후 부활 방식을 선택하는 팝업.
    /// UIManager.ShowUI(UIKeyType.RespawnPopup) 로 표시한 뒤 Setup()을 호출한다.
    /// </summary>
    public class UI_RespawnPopup : UI_Base
    {
        [Header("제자리 부활")]
        [SerializeField] private Button           _spotReviveButton;
        [SerializeField] private TextMeshProUGUI  _spotReviveLabel;
        [SerializeField] private TextMeshProUGUI  _spotItemCountText;
        [SerializeField] private TextMeshProUGUI  _spotHealText;   // "HP 50% 회복"

        [Header("포탈 부활")]
        [SerializeField] private Button           _portalReviveButton;
        [SerializeField] private TextMeshProUGUI  _portalReviveLabel;
        [SerializeField] private TextMeshProUGUI  _portalHealText; // "HP 100% 회복"

        [Header("공용")]
        [SerializeField] private TextMeshProUGUI  _warningText;    // "전멸 상태입니다..."

        [Header("설정")]
        [Tooltip("제자리 부활에 사용할 아이템 ID (기본: 부활석=100006)")]
        [SerializeField] private int _revivalItemId = (int)ItemIdType.None;
        [Tooltip("제자리 부활 시 회복할 HP 비율 (0~1)")]
        [SerializeField] private float _spotHealPercent = 0.5f;
        [Tooltip("포탈 부활 시 회복할 HP 비율 (0~1)")]
        [SerializeField] private float _portalHealPercent = 1.0f;

        private Action _onSpotRevive;
        private Action _onPortalRevive;

        public float SpotHealPercent   => _spotHealPercent;
        public float PortalHealPercent => _portalHealPercent;

        protected override void Awake()
        {
            base.Awake();
            _canCloseWithEsc = false;

            if (_spotReviveButton  != null) _spotReviveButton.onClick.AddListener(OnSpotReviveClicked);
            if (_portalReviveButton != null) _portalReviveButton.onClick.AddListener(OnPortalReviveClicked);
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();
            Svc.GameTime?.SetPause(true);
            FadeIn(0.3f);
            RefreshItemCount();
            RefreshHealTexts();
            RebuildNavigation();
        }

        private void RefreshHealTexts()
        {
            if (_spotHealText != null)
                _spotHealText.text = $"HP {Mathf.RoundToInt(_spotHealPercent * 100f)}% 회복";
            if (_portalHealText != null)
                _portalHealText.text = $"HP {Mathf.RoundToInt(_portalHealPercent * 100f)}% 회복";
        }

        protected override void OnHide()
        {
            Svc.GameTime?.SetPause(false);
            _onSpotRevive  = null;
            _onPortalRevive = null;
            base.OnHide();
        }

        /// <summary>
        /// 팝업 표시 후 반드시 호출. 버튼 콜백과 소지 아이템 수를 세팅한다.
        /// </summary>
        public void Setup(Action onSpotRevive, Action onPortalRevive)
        {
            _onSpotRevive   = onSpotRevive;
            _onPortalRevive = onPortalRevive;
            RefreshItemCount();
        }

        private void RefreshItemCount()
        {
            int count = UISvc.Inventory?.GetItemCount(_revivalItemId) ?? 0;

            if (_spotItemCountText != null)
                _spotItemCountText.text = $"보유 부활석 x{count}";

            if (_spotReviveButton != null)
                _spotReviveButton.interactable = count > 0;

            if (_spotReviveLabel != null)
                _spotReviveLabel.color = count > 0
                    ? Color.white
                    : new Color(0.5f, 0.5f, 0.5f, 1f);

            RebuildNavigation();
        }

        private void RebuildNavigation()
        {
            UIFocusNavigation.ConfigureHorizontal(new Selectable[]
            {
                _spotReviveButton,
                _portalReviveButton
            });
            SetDefaultFocus(UIFocusNavigation.FirstNavigable(
                _spotReviveButton,
                _portalReviveButton));
        }

        private void OnSpotReviveClicked()
        {
            bool consumed = UISvc.Inventory?.RemoveItem(_revivalItemId, 1) ?? false;
            if (!consumed) return;

            UISvc.UI.HideUI(UIKeyType.RespawnPopup);
            _onSpotRevive?.Invoke();
        }

        private void OnPortalReviveClicked()
        {
            UISvc.UI.HideUI(UIKeyType.RespawnPopup);
            _onPortalRevive?.Invoke();
        }
    }
}
