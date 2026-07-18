using System;
using DG.Tweening;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// Dim 배경 + 중앙 Panel 구조의 팝업 UI 공통 베이스.
    /// 표시 시 Dim 페이드 인 + Panel 스케일 팝인, 숨길 때 그 반대 트윈을 재생한다.
    /// 히트스톱/일시정지(timeScale=0)에도 동작하도록 모든 트윈은 SetUpdate(true)로 갱신한다.
    /// 트윈 사용 여부는 인스펙터에서 켜고 끌 수 있으며 기본값은 사용이다.
    /// </summary>
    public abstract class UI_PopupBase : UI_Base
    {
        #region 팝업 구조

        [Header("팝업 구조")]
        [Tooltip("전체 화면을 덮는 Dim 배경. 표시/닫기 시 알파를 트윈한다.")]
        [SerializeField] protected CanvasGroup _dim;
        [Tooltip("중앙 UI 영역. 표시/닫기 시 스케일을 트윈한다.")]
        [SerializeField] protected RectTransform _panel;

        #endregion

        #region 트윈 설정

        [Header("팝업 트윈")]
        [Tooltip("표시(생성) 시 오픈 트윈 사용 여부. 끄면 즉시 표시.")]
        [SerializeField] protected bool _playOpenTween = true;
        [Tooltip("숨김(삭제) 시 클로즈 트윈 사용 여부. 끄면 즉시 숨김.")]
        [SerializeField] protected bool _playCloseTween = true;
        [SerializeField] protected float _openDuration = 0.22f;
        [SerializeField] protected float _closeDuration = 0.14f;
        [SerializeField] protected float _panelStartScale = 0.85f;
        [SerializeField] protected Ease _openEase = Ease.OutBack;
        [SerializeField] protected Ease _closeEase = Ease.InCubic;

        #endregion

        private Sequence _popupSequence;
        private bool _closeTweening;      // 클로즈 트윈 진행 중(재진입 방지)
        private bool _forceImmediateHide; // Close/OnDestroy 등 즉시 숨겨야 하는 경로

        protected override void OnShow()
        {
            base.OnShow();
            _closeTweening = false;
            PlayOpenTween();
        }

        protected override void OnHide()
        {
            KillPopupTween();
            _closeTweening = false;
            base.OnHide();
        }

        /// <summary>
        /// 숨김 요청. 클로즈 트윈이 켜져 있으면 트윈 재생 후 실제로 숨긴다.
        /// UIManager.HideUI 등 외부 경로도 이 오버라이드를 통해 트윈을 탄다.
        /// </summary>
        public override void Hide()
        {
            // 트윈 없이 즉시 숨겨야 하는 조건: 즉시숨김 강제 / 미표시 / 트윈 비활성 /
            // 지속시간 0 / 트윈 대상 없음 / 비활성 상태(파괴·씬전환 등).
            if (_forceImmediateHide
                || !IsVisible
                || !_playCloseTween
                || _closeDuration <= 0f
                || (_dim == null && _panel == null)
                || !isActiveAndEnabled)
            {
                KillPopupTween();
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

        /// <summary>
        /// 닫기(제거)는 직후 파괴가 뒤따르므로 트윈 없이 즉시 처리한다.
        /// </summary>
        public override void Close()
        {
            _forceImmediateHide = true;
            KillPopupTween();
            base.Close();
            _forceImmediateHide = false;
        }

        /// <summary>
        /// Dim 페이드 인 + Panel 스케일 팝인. 트윈이 꺼져 있으면 즉시 최종 상태로 스냅한다.
        /// </summary>
        protected virtual void PlayOpenTween()
        {
            KillPopupTween();

            if (!_playOpenTween || _openDuration <= 0f)
            {
                if (_dim != null) _dim.alpha = 1f;
                if (_panel != null) _panel.localScale = Vector3.one;
                return;
            }

            _popupSequence = DOTween.Sequence().SetUpdate(true);

            if (_dim != null)
            {
                _dim.alpha = 0f;
                _popupSequence.Join(FadeDim(1f, _openDuration));
            }

            if (_panel != null)
            {
                _panel.localScale = Vector3.one * _panelStartScale;
                _popupSequence.Join(_panel.DOScale(1f, _openDuration).SetEase(_openEase));
            }
        }

        /// <summary>
        /// Dim 페이드 아웃 + Panel 축소 후 onComplete를 호출한다.
        /// 트윈할 대상이 없거나 비활성/지속시간이 0이면 즉시 onComplete를 호출한다.
        /// </summary>
        protected virtual void PlayCloseTween(Action onComplete)
        {
            KillPopupTween();

            if ((_dim == null && _panel == null) || _closeDuration <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            _popupSequence = DOTween.Sequence().SetUpdate(true);

            if (_dim != null)
                _popupSequence.Join(FadeDim(0f, _closeDuration));

            if (_panel != null)
                _popupSequence.Join(_panel.DOScale(_panelStartScale, _closeDuration).SetEase(_closeEase));

            _popupSequence.OnComplete(() => onComplete?.Invoke());
        }

        // 이 프로젝트의 UI asmdef는 프리컴파일 DOTween.dll만 참조하므로 CanvasGroup.DOFade
        // 확장(DOTweenModuleUI)을 쓸 수 없다. 기존 UI 코드와 동일하게 DOTween.To로 알파를 트윈한다.
        private Tween FadeDim(float endAlpha, float duration)
        {
            return DOTween.To(() => _dim.alpha, value => _dim.alpha = value, endAlpha, duration);
        }

        private void KillPopupTween()
        {
            if (_popupSequence != null && _popupSequence.IsActive())
                _popupSequence.Kill();
            _popupSequence = null;
        }

        protected override void OnDestroy()
        {
            // 파괴 중에는 트윈 지연 없이 즉시 정리해야 커서/입력 레이어 복원이 누락되지 않는다.
            _forceImmediateHide = true;
            KillPopupTween();
            base.OnDestroy();
        }

        protected override void OnDispose()
        {
            KillPopupTween();
            base.OnDispose();
        }
    }
}
