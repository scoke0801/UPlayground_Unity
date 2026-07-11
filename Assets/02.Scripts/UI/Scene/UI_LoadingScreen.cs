using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 오버레이 방식 로딩 UI (현재 미사용 — 별도 로딩씬 방식으로 대체됨).
    /// 추후 오버레이 방식 복귀 시 사용.
    /// </summary>
    public class UI_LoadingScreen : UI_Base
    {
        [SerializeField] private Slider _progressSlider;

        protected override void OnInit()
        {
            _layer = CanvasLayer.System;
        }

        protected override void OnShow()
        {
            _progressSlider.value = 0f;
            FadeIn(0.3f);
        }

        protected override void OnHide() { }

        protected override void OnClose()
        {
            FadeOut(0.3f);
        }

        public void SetProgress(float progress)
        {
            if (_fillCoroutine != null) StopCoroutine(_fillCoroutine);
            _fillCoroutine = StartCoroutine(SmoothFill(progress));
        }

        private Coroutine _fillCoroutine;

        private IEnumerator SmoothFill(float target)
        {
            while (_progressSlider.value < target - 0.001f)
            {
                _progressSlider.value = Mathf.Lerp(_progressSlider.value, target, Time.deltaTime * 6f);
                yield return null;
            }
            _progressSlider.value = target;
        }
    }
}
