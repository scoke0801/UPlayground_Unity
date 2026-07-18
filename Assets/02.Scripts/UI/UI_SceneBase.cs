using System;
using DG.Tweening;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 전체 화면 메뉴 성격의 Scene UI 공통 베이스.
    /// 표시(생성) 시 루트 CanvasGroup 페이드 인 + 콘텐츠 슬라이드 인, 숨김(삭제) 시 그 반대 트윈을 재생한다.
    /// <see cref="UI_PopupBase"/>가 Dim + 중앙 Panel 팝인 구조라면, 이쪽은 전체 화면 패널의 등장/퇴장 연출을 담당한다.
    /// 히트스톱/일시정지(timeScale=0)에도 동작하도록 모든 트윈은 SetUpdate(true)로 갱신한다.
    /// 트윈 사용 여부는 인스펙터에서 켜고 끌 수 있으며 기본값은 사용이다.
    /// </summary>
    public abstract class UI_SceneBase : UI_Base
    {
        #region Scene 구조

        [Header("Scene 구조")]
        [Tooltip("표시/닫기 시 슬라이드 이동할 콘텐츠 영역. 비우면 루트 페이드만 재생한다.")]
        [SerializeField] protected RectTransform _sceneContent;

        #endregion

        #region 트윈 설정

        [Header("Scene 트윈")]
        [Tooltip("표시(생성) 시 오픈 트윈 사용 여부. 끄면 즉시 표시.")]
        [SerializeField] protected bool _playOpenTween = true;
        [Tooltip("숨김(삭제) 시 클로즈 트윈 사용 여부. 끄면 즉시 숨김.")]
        [SerializeField] protected bool _playCloseTween = true;
        [SerializeField] protected float _openDuration = 0.25f;
        [SerializeField] protected float _closeDuration = 0.18f;
        [Tooltip("콘텐츠가 홈 위치로부터 얼마나 떨어진 곳에서 슬라이드해 들어오는지(anchoredPosition 오프셋). 예: (0,-80)이면 아래에서 위로.")]
        [SerializeField] protected Vector2 _slideOffset = new Vector2(0f, -80f);
        [SerializeField] protected Ease _openEase = Ease.OutCubic;
        [SerializeField] protected Ease _closeEase = Ease.InCubic;

        #endregion

        private Sequence _sceneSequence;
        private bool _closeTweening;         // 클로즈 트윈 진행 중(재진입 방지)
        private bool _forceImmediateHide;    // Close/OnDestroy 등 즉시 숨겨야 하는 경로
        private Vector2 _sceneContentHomePosition; // 프리팹에 저작된 콘텐츠 기준 위치
        private bool _sceneContentHomeCached;

        protected override void Awake()
        {
            base.Awake();
            CacheContentHome();
        }

        // 콘텐츠의 "홈" anchoredPosition을 트윈이 개입하기 전 1회 캐싱한다.
        private void CacheContentHome()
        {
            if (_sceneContentHomeCached || _sceneContent == null)
                return;

            _sceneContentHomePosition = _sceneContent.anchoredPosition;
            _sceneContentHomeCached = true;
        }

        protected override void OnShow()
        {
            base.OnShow();
            _closeTweening = false;
            PlayOpenTween();
        }

        protected override void OnHide()
        {
            KillSceneTween();
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
                || (_sceneContent == null && _canvasGroup == null)
                || !isActiveAndEnabled)
            {
                KillSceneTween();
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
            KillSceneTween();
            base.Close();
            _forceImmediateHide = false;
        }

        /// <summary>
        /// 루트 페이드 인 + 콘텐츠 슬라이드 인. 트윈이 꺼져 있으면 즉시 최종 상태로 스냅한다.
        /// </summary>
        protected virtual void PlayOpenTween()
        {
            CacheContentHome();
            KillSceneTween();

            if (!_playOpenTween || _openDuration <= 0f)
            {
                SnapToOpenState();
                return;
            }

            _sceneSequence = DOTween.Sequence().SetUpdate(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _sceneSequence.Join(FadeCanvas(1f, _openDuration));
            }

            if (_sceneContent != null)
            {
                _sceneContent.anchoredPosition = _sceneContentHomePosition + _slideOffset;
                _sceneSequence.Join(TweenAnchoredPos(_sceneContentHomePosition, _openDuration, _openEase));
            }
        }

        /// <summary>
        /// 루트 페이드 아웃 + 콘텐츠 슬라이드 아웃 후 onComplete를 호출한다.
        /// 트윈할 대상이 없거나 지속시간이 0이면 즉시 onComplete를 호출한다.
        /// </summary>
        protected virtual void PlayCloseTween(Action onComplete)
        {
            CacheContentHome();
            KillSceneTween();

            if ((_sceneContent == null && _canvasGroup == null) || _closeDuration <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            _sceneSequence = DOTween.Sequence().SetUpdate(true);

            if (_canvasGroup != null)
                _sceneSequence.Join(FadeCanvas(0f, _closeDuration));

            if (_sceneContent != null)
                _sceneSequence.Join(TweenAnchoredPos(_sceneContentHomePosition + _slideOffset, _closeDuration, _closeEase));

            _sceneSequence.OnComplete(() => onComplete?.Invoke());
        }

        // 오픈 트윈을 생략할 때 최종(열린) 상태로 즉시 스냅한다.
        private void SnapToOpenState()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_sceneContent != null) _sceneContent.anchoredPosition = _sceneContentHomePosition;
        }

        // 이 프로젝트의 UI asmdef는 프리컴파일 DOTween.dll만 참조하므로 CanvasGroup.DOFade /
        // RectTransform.DOAnchorPos(DOTweenModuleUI)를 쓸 수 없다. 기존 UI 코드와 동일하게
        // DOTween.To로 알파와 anchoredPosition을 직접 트윈한다.
        private Tween FadeCanvas(float endAlpha, float duration)
        {
            return DOTween.To(() => _canvasGroup.alpha, value => _canvasGroup.alpha = value, endAlpha, duration);
        }

        private Tween TweenAnchoredPos(Vector2 end, float duration, Ease ease)
        {
            return DOTween.To(() => _sceneContent.anchoredPosition,
                    value => _sceneContent.anchoredPosition = value, end, duration)
                .SetEase(ease);
        }

        private void KillSceneTween()
        {
            if (_sceneSequence != null && _sceneSequence.IsActive())
                _sceneSequence.Kill();
            _sceneSequence = null;
        }

        protected override void OnDestroy()
        {
            // 파괴 중에는 트윈 지연 없이 즉시 정리해야 커서/입력 레이어 복원이 누락되지 않는다.
            _forceImmediateHide = true;
            KillSceneTween();
            base.OnDestroy();
        }

        protected override void OnDispose()
        {
            KillSceneTween();
            base.OnDispose();
        }
    }
}
