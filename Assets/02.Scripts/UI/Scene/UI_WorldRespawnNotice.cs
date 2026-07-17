using System.Collections;
using TMPro;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 몬스터 재스폰 안내 연출 UI.
    /// MonsterRespawnManager가 UIManager.ShowUI("WorldRespawnNotice") 후 ShowNotice()를 호출한다.
    ///
    /// - 게임을 멈추지 않고(입력 비차단, pause 없음) 화면을 얕게 암전하며 안내 문구를 표시한다.
    /// - 페이드는 히트스톱/슬로우에 흔들리지 않도록 unscaledDeltaTime으로 구동한다.
    /// - 연출이 끝나면 스스로 Hide된다. 연출 중 재호출되면 시퀀스를 처음부터 다시 시작한다.
    /// </summary>
    public class UI_WorldRespawnNotice : UI_Base
    {
        /// <summary> UIPrefabDatabase 등록 키. MonsterRespawnManager와 공유한다. </summary>
        public const string UIKey = "WorldRespawnNotice";

        [Header("연출")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private float _fadeInDuration = 0.35f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField] private float _fadeOutDuration = 0.45f;

        private Coroutine _sequence;

        // HUD 레이어 안내 연출: 커서/입력에 관여하지 않는다.
        protected override bool RequiresCursorVisible => false;
        protected override bool BlocksLowerInput => false;

        #region UI_Base

        protected override void OnInit()
        {
            _canCloseWithEsc = false;
        }

        protected override void OnShow()
        {
            // 연출 시작 전 투명 상태로 대기. 실제 페이드는 ShowNotice가 시작한다.
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        protected override void OnHide()
        {
            StopSequence();
        }

        protected override void OnClose()
        {
            StopSequence();
        }

        #endregion

        /// <summary> 안내 문구를 설정하고 페이드인 → 홀드 → 페이드아웃 연출을 시작한다. </summary>
        public void ShowNotice(string message)
        {
            if (_messageText != null)
                _messageText.text = message;

            StopSequence();
            _sequence = StartCoroutine(PlaySequence());
        }

        private void StopSequence()
        {
            if (_sequence == null) return;
            StopCoroutine(_sequence);
            _sequence = null;
        }

        private IEnumerator PlaySequence()
        {
            yield return FadeUnscaled(0f, 1f, _fadeInDuration);

            float elapsed = 0f;
            while (elapsed < _holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeUnscaled(1f, 0f, _fadeOutDuration);

            _sequence = null;
            UISvc.UI?.HideUI(UIKey);
        }

        /// <summary> UI_Base.FadeIn/Out은 scaled time 기반이라 사용하지 않고 unscaled로 직접 보간한다. </summary>
        private IEnumerator FadeUnscaled(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
