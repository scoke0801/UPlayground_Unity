using UnityEngine;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI
{
    public class UI_HUD_InteractionKey : UI_Base
    {
        // 상호작용 키 프롬프트. 활성 디바이스에 따라 F(키보드) / 버튼(게임패드)로 자동 전환된다.
        [SerializeField] private UIInputPromptIcon _promptIcon;
        [SerializeField] private Image _progressFill;

        private bool _isSubscribed = false;

        protected override void OnShow()
        {
            AnimationChange("On");
            if (_promptIcon != null)
                _promptIcon.SetAction(InputMapNames.PlayerAction, PlayerAction.Interact);
            SetProgressVisible(false);
            SubscribeEvents();
        }

        protected override void Update()
        {
            IActorInteractionService handler = UISvc.Actors?.InteractionHandler;
            if (handler == null || !handler.IsInteractionProgressActive)
            {
                SetProgressVisible(false);
                return;
            }

            SetProgressVisible(true);
            if (_progressFill != null)
            {
                _progressFill.fillAmount = handler.InteractionProgress;
            }
        }

        protected override void OnHide()
        {
            SetProgressVisible(false);
            UnsubscribeEvents();
        }

        protected override void OnClose()
        {
            SetProgressVisible(false);
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed) return;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed) return;

            _isSubscribed = false;
        }

        private void SetProgressVisible(bool visible)
        {
            if (_progressFill == null)
            {
                return;
            }

            if (_progressFill.gameObject.activeSelf != visible)
            {
                _progressFill.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                _progressFill.fillAmount = 0f;
            }
        }
    }
}
