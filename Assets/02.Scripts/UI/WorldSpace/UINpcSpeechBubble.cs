using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>입력을 받지 않는 주민 말풍선의 텍스트와 등장·퇴장 연출을 담당한다.</summary>
    public sealed class UINpcSpeechBubble : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _line;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private float _enterSeconds = 0.18f;
        [SerializeField] private float _exitSeconds = 0.12f;
        private Tween _fade;
        private Tween _scale;
        private float _alpha;
        private bool _isVisible;
        public RectTransform Rect => _panel;
        public float ExitSeconds => _exitSeconds;

        /// <summary>풀에서 꺼낸 말풍선에 이름과 완성된 한 문장을 표시한다.</summary>
        public void Present(string speaker, string line)
        {
            StopTweens();
            gameObject.SetActive(true);
            _name.text = speaker;
            _line.text = line;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _isVisible = true;
            ApplyAlpha(0f);
            _panel.localScale = Vector3.one * 0.94f;
            _fade = DOTween.To(() => _alpha, ApplyAlpha, 1f, _enterSeconds).SetUpdate(true);
            _scale = _panel.DOScale(1f, _enterSeconds).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>표시 시간을 마친 말풍선을 짧게 페이드한다.</summary>
        public void FadeOut()
        {
            StopTweens();
            _fade = DOTween.To(() => _alpha, ApplyAlpha, 0f, _exitSeconds).SetUpdate(true);
        }

        /// <summary>짧은 가림에는 표시만 숨기고 문장·읽기 시간·트윈 상태를 버리지 않는다.</summary>
        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            _group.alpha = isVisible ? _alpha : 0f;
        }

        private void ApplyAlpha(float value)
        {
            _alpha = value;
            _group.alpha = _isVisible ? value : 0f;
        }

        /// <summary>상호작용·씬 이탈 때 트윈까지 즉시 정리한다.</summary>
        public void Clear()
        {
            StopTweens();
            gameObject.SetActive(false);
        }

        private void StopTweens() { _fade?.Kill(); _scale?.Kill(); _fade = null; _scale = null; }
        private void OnDisable() => StopTweens();
        private void OnDestroy() => StopTweens();
    }
}
