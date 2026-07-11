using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// UI 창 열기 위한 메뉴 패널
    /// </summary>
    public class UI_MenuPanel : UI_Base
    {
        #region UI_Base 생명주기

        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _bagButton;
        [SerializeField] private Button _craftButton;
        [SerializeField] private Button _questButton;
        [SerializeField] private Button _partyButton;
        [SerializeField] private Button _configButton;
        [SerializeField] private Button _exitButton;

        private int _openedFrame = -1;


        protected override void Awake()
        {
            base.Awake();

            _mapButton.onClick.AddListener(OnClickedMapButton);
            _bagButton.onClick.AddListener(OnClickedBagButton);
            _craftButton.onClick.AddListener(OnClickedCraftButton);
            _questButton.onClick.AddListener(OnClickedQuestButton);
            _partyButton.onClick.AddListener(OnClickedPartyButton);
            _configButton.onClick.AddListener(OnClickedConfigButton);
            if (_exitButton != null) _exitButton.onClick.AddListener(OnClickedExitButton);
        }

        // 메뉴가 열려 있는 동안 게임플레이 입력을 차단한다.
        // 커서 표시와 입력 레이어 상승/복원은 UI_Base가 _layer/BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            _openedFrame = Time.frameCount;
        }

        protected override void RegisterInputEvents()
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
                null, OnPerformedMenuPanel, null, null, null, InputLayer.Level_1);
        }

        protected override void UnRegisterInputEvents()
        {
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
                null, OnPerformedMenuPanel, null);
        }

        protected override void OnDispose()
        {
            if (_mapButton != null) _mapButton.onClick.RemoveListener(OnClickedMapButton);
            if (_bagButton != null) _bagButton.onClick.RemoveListener(OnClickedBagButton);
            if (_craftButton != null) _craftButton.onClick.RemoveListener(OnClickedCraftButton);
            if (_questButton != null) _questButton.onClick.RemoveListener(OnClickedQuestButton);
            if (_partyButton != null) _partyButton.onClick.RemoveListener(OnClickedPartyButton);
            if (_configButton != null) _configButton.onClick.RemoveListener(OnClickedConfigButton);
            if (_exitButton != null) _exitButton.onClick.RemoveListener(OnClickedExitButton);

            base.OnDispose();
        }

        public override bool PerformBackFunction()
        {
            Hide();
            return false;
        }
        #endregion

        private void OnClickedMapButton()
        {
            Toggle(UIKeyType.Map);
        }

        private void OnClickedBagButton()
        {
            Toggle(UIKeyType.Inventory);
        }

        private void OnClickedCraftButton()
        {
            Toggle(UIKeyType.Craft);
        }

        private void OnClickedQuestButton()
        {
            Toggle(UIKeyType.Quest);
        }

        private void OnClickedPartyButton()
        {
            Toggle(UIKeyType.Party);
        }

        private void OnClickedConfigButton()
        {
            Toggle(UIKeyType.Config);
        }

        private void OnClickedExitButton()
        {
            Hide();
        }

        private void OnPerformedMenuPanel(InputAction.CallbackContext obj)
        {
            if (Time.frameCount == _openedFrame)
                return;

            Hide();
        }

        private void Toggle(UIKeyType type)
        {
            GameObject go = UIManager.Instance.GetActiveUI(type);
            UI_Base ui = go != null ? go.GetComponent<UI_Base>() : null;
            bool shouldShowTarget = ui == null || ui.IsVisible == false;

            Hide();

            if (shouldShowTarget)
            {
                UIManager.Instance.ShowUI(type);
            }
            else if (go != null)
            {
                UIManager.Instance.HideUI(type);
            }
        }
    }
}
