using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// UI 창 열기 위한 메뉴 패널.
    /// 열릴 때 화면 우측 밖에서 원래 위치까지 슬라이드 인, 닫힐 때 다시 우측으로 슬라이드 아웃한다.
    /// 히트스톱/일시정지(timeScale=0)에도 동작하도록 트윈은 SetUpdate(true)로 갱신한다.
    /// </summary>
    public class UI_Scene_MenuPanel : UI_Base
    {
        #region UI_Base 생명주기

        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _bagButton;
        [SerializeField] private Button _craftButton;
        [SerializeField] private Button _questButton;
        [SerializeField] private Button _partyButton;
        [SerializeField] private Button _codexButton;
        [SerializeField] private Button _skillTreeButton;
        [SerializeField] private Button _configButton;
        [SerializeField] private Button _exitButton;

        [Header("슬라이드 트윈")]
        [Tooltip("슬라이드시킬 대상 RectTransform. 비우면 패널 루트를 사용한다.")]
        [SerializeField] private RectTransform _slidePanel;
        [Tooltip("열림 트윈 사용 여부. 끄면 즉시 표시.")]
        [SerializeField] private bool _playOpenTween = true;
        [Tooltip("닫힘 트윈 사용 여부. 끄면 즉시 숨김.")]
        [SerializeField] private bool _playCloseTween = true;
        [SerializeField] private float _openDuration = 0.28f;
        [SerializeField] private float _closeDuration = 0.2f;
        [Tooltip("우측 화면 밖으로 밀어낼 가로 거리. 0이면 패널 너비로 자동 계산.")]
        [SerializeField] private float _slideDistance = 0f;
        [SerializeField] private Ease _openEase = Ease.OutCubic;
        [SerializeField] private Ease _closeEase = Ease.InCubic;

        private int _openedFrame = -1;

        // 슬라이드 대상의 원래(홈) 앵커 위치. Awake에서 최초 1회만 캐싱한다.
        private Vector2 _homeAnchoredPos;
        private Tween _slideTween;
        private bool _closeTweening;      // 닫힘 트윈 진행 중(재진입 방지)
        private bool _forceImmediateHide; // OnDestroy 등 즉시 숨겨야 하는 경로

        private RectTransform SlideTarget => _slidePanel != null ? _slidePanel : _rectTransform;


        protected override void Awake()
        {
            base.Awake();

            // 홈 위치는 어떤 트윈보다 먼저, 디자인된 위치에서 캐싱한다.
            RectTransform target = SlideTarget;
            _homeAnchoredPos = target != null ? target.anchoredPosition : Vector2.zero;

            _mapButton.onClick.AddListener(OnClickedMapButton);
            _bagButton.onClick.AddListener(OnClickedBagButton);
            _craftButton.onClick.AddListener(OnClickedCraftButton);
            _questButton.onClick.AddListener(OnClickedQuestButton);
            _partyButton.onClick.AddListener(OnClickedPartyButton);
            if (_codexButton != null) _codexButton.onClick.AddListener(OnClickedCodexButton);
            if (_skillTreeButton != null) _skillTreeButton.onClick.AddListener(OnClickedSkillTreeButton);
            _configButton.onClick.AddListener(OnClickedConfigButton);
            if (_exitButton != null) _exitButton.onClick.AddListener(OnClickedExitButton);

            UIFocusNavigation.ConfigureVertical(new Selectable[]
            {
                _mapButton,
                _bagButton,
                _craftButton,
                _questButton,
                _partyButton,
                _codexButton,
                _skillTreeButton,
                _configButton,
                _exitButton
            });
        }

        // 메뉴가 열려 있는 동안 게임플레이 입력을 차단한다.
        // 커서 표시와 입력 레이어 상승/복원은 UI_Base가 _layer/BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();
            _openedFrame = Time.frameCount;
            PlayOpenTween();
            SetDefaultFocus(UIFocusNavigation.FirstNavigable(
                _mapButton,
                _bagButton,
                _craftButton,
                _questButton,
                _partyButton,
                _codexButton,
                _skillTreeButton,
                _configButton,
                _exitButton));
        }

        /// <summary>
        /// 숨김 요청. 닫힘 트윈이 켜져 있으면 슬라이드 아웃을 재생한 뒤 실제로 숨긴다.
        /// UIManager.HideUI 등 외부 경로도 이 오버라이드를 통해 트윈을 탄다.
        /// </summary>
        public override void Hide()
        {
            // 트윈 없이 즉시 숨겨야 하는 조건: 즉시숨김 강제 / 미표시 / 트윈 비활성 /
            // 지속시간 0 / 슬라이드 대상 없음 / 비활성 상태(파괴·씬전환 등).
            if (_forceImmediateHide
                || !IsVisible
                || !_playCloseTween
                || _closeDuration <= 0f
                || SlideTarget == null
                || !isActiveAndEnabled)
            {
                KillSlideTween();
                _closeTweening = false;
                base.Hide();
                return;
            }

            if (_closeTweening)
                return; // 이미 닫기 트윈 진행 중

            _closeTweening = true;
            PlayCloseTween(() =>
            {
                _closeTweening = false;
                base.Hide();
            });
        }

        protected override void OnHide()
        {
            // 실제 숨김 직전 트윈을 정리하고 홈 위치로 되돌려, 다음 표시가 항상 정상 위치에서 시작하도록 한다.
            KillSlideTween();
            _closeTweening = false;
            RectTransform target = SlideTarget;
            if (target != null)
                target.anchoredPosition = _homeAnchoredPos;

            base.OnHide();
        }

        protected override void OnDestroy()
        {
            // 파괴 중에는 트윈 지연 없이 즉시 정리해야 커서/입력 레이어 복원이 누락되지 않는다.
            _forceImmediateHide = true;
            KillSlideTween();
            base.OnDestroy();
        }

        protected override void RegisterInputEvents()
        {
            Svc.Input.RegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
                null, OnPerformedMenuPanel, null, null, null, InputLayer.Level_1);
        }

        protected override void UnRegisterInputEvents()
        {
            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
                null, OnPerformedMenuPanel, null);
        }

        protected override void OnDispose()
        {
            if (_mapButton != null) _mapButton.onClick.RemoveListener(OnClickedMapButton);
            if (_bagButton != null) _bagButton.onClick.RemoveListener(OnClickedBagButton);
            if (_craftButton != null) _craftButton.onClick.RemoveListener(OnClickedCraftButton);
            if (_questButton != null) _questButton.onClick.RemoveListener(OnClickedQuestButton);
            if (_partyButton != null) _partyButton.onClick.RemoveListener(OnClickedPartyButton);
            if (_codexButton != null) _codexButton.onClick.RemoveListener(OnClickedCodexButton);
            if (_skillTreeButton != null) _skillTreeButton.onClick.RemoveListener(OnClickedSkillTreeButton);
            if (_configButton != null) _configButton.onClick.RemoveListener(OnClickedConfigButton);
            if (_exitButton != null) _exitButton.onClick.RemoveListener(OnClickedExitButton);

            KillSlideTween();

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

        private void OnClickedCodexButton()
        {
            Toggle(UIKeyType.MonsterCodex);
        }

        private void OnClickedSkillTreeButton()
        {
            GameObject active = UISvc.UI.GetActiveUI(UI_Scene_SkillTree.UIKey);
            UI_Base activeUI = active != null ? active.GetComponent<UI_Base>() : null;
            bool shouldShow = activeUI == null || !activeUI.IsVisible;
            Hide();
            if (shouldShow)
            {
                GameObject instance = UISvc.UI.ShowUI(
                    UI_Scene_SkillTree.UIKey,
                    CanvasLayer.Popup);
                instance?.GetComponent<UI_Scene_SkillTree>()?.Configure(
                    UISvc.Party?.ActiveCharacterType ?? Data.EnumType.CharacterActorType.None,
                    allowChanges: false);
            }
            else
            {
                UISvc.UI.HideUI(UI_Scene_SkillTree.UIKey);
            }
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
            GameObject go = UISvc.UI.GetActiveUI(type);
            UI_Base ui = go != null ? go.GetComponent<UI_Base>() : null;
            bool shouldShowTarget = ui == null || ui.IsVisible == false;

            Hide();

            if (shouldShowTarget)
            {
                UISvc.UI.ShowUI(type);
            }
            else if (go != null)
            {
                UISvc.UI.HideUI(type);
            }
        }

        #region 슬라이드 트윈

        /// <summary>
        /// 우측 화면 밖에서 홈 위치까지 슬라이드 인. 트윈이 꺼져 있으면 즉시 홈 위치로 스냅한다.
        /// </summary>
        private void PlayOpenTween()
        {
            KillSlideTween();
            _closeTweening = false;

            RectTransform target = SlideTarget;
            if (target == null)
                return;

            if (!_playOpenTween || _openDuration <= 0f)
            {
                target.anchoredPosition = _homeAnchoredPos;
                return;
            }

            Vector2 start = _homeAnchoredPos + new Vector2(ResolveSlideDistance(target), 0f);
            target.anchoredPosition = start;
            _slideTween = TweenAnchoredPos(target, _homeAnchoredPos, _openDuration, _openEase);
        }

        /// <summary>
        /// 홈 위치에서 우측 화면 밖까지 슬라이드 아웃 후 onComplete를 호출한다.
        /// </summary>
        private void PlayCloseTween(Action onComplete)
        {
            KillSlideTween();

            RectTransform target = SlideTarget;
            if (target == null || _closeDuration <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            Vector2 exit = _homeAnchoredPos + new Vector2(ResolveSlideDistance(target), 0f);
            _slideTween = TweenAnchoredPos(target, exit, _closeDuration, _closeEase)
                .OnComplete(() => onComplete?.Invoke());
        }

        // UI asmdef는 프리컴파일 DOTween.dll만 참조하므로 RectTransform.DOAnchorPos(DOTweenModuleUI)를
        // 쓸 수 없다. 기존 UI 코드와 동일하게 DOTween.To로 anchoredPosition을 트윈한다.
        private Tween TweenAnchoredPos(RectTransform target, Vector2 end, float duration, Ease ease)
        {
            return DOTween.To(() => target.anchoredPosition,
                    value => target.anchoredPosition = value, end, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        // 밀어낼 거리: 지정값이 있으면 사용, 없으면 패널 너비(그마저 0이면 화면 너비)로 자동 계산한다.
        private float ResolveSlideDistance(RectTransform target)
        {
            if (_slideDistance > 0f)
                return _slideDistance;

            float width = target != null ? target.rect.width : 0f;
            return width > 0f ? width : Screen.width;
        }

        private void KillSlideTween()
        {
            if (_slideTween != null && _slideTween.IsActive())
                _slideTween.Kill();
            _slideTween = null;
        }

        #endregion
    }
}
