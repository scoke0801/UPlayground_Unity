using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>
    /// maxVisibleCharacters 기반 타이핑 공용 컴포넌트. Main/Monologue 채널이 공유합니다.
    ///
    /// 문자열을 한 글자씩 누적하는 방식(text += c)은 리치 텍스트 태그를 본문 문자처럼 노출하므로 쓰지 않습니다.
    /// 전체 리치 문자열을 한 번에 설정하고 '보이는 글자 수'만 늘려서, 태그가 화면에 절대 드러나지 않고
    /// 스킵도 maxVisibleCharacters = total 한 줄로 끝납니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueTypewriter : MonoBehaviour
    {
        [Tooltip("타이핑을 출력할 대상 텍스트. 비우면 같은 GameObject의 TMP를 사용합니다.")]
        [SerializeField] private TextMeshProUGUI targetText;

        [Tooltip("히트스톱 등 timeScale 변동에 대화 타이핑이 끌려가지 않도록 기본은 언스케일 시간입니다.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Tooltip("본문이 상자를 넘칠 때 사용할 스크롤. 지정하면 타이핑이 드러나는 줄을 따라 자동으로 내려갑니다.")]
        [SerializeField] private ScrollRect bodyScrollRect;

        [Tooltip("드러난 줄을 따라갈 때 아래쪽에 남길 여백(px).")]
        [SerializeField] private float followBottomPadding = 8f;

        private Coroutine _routine;
        private int _totalVisibleCharacters;

        /// <summary>타이핑이 진행 중인지. 완료·미시작이면 false.</summary>
        public bool IsTyping { get; private set; }

        /// <summary>현재 표시 중인 리치 텍스트(태그 포함). 이력 기록·검증에 사용합니다.</summary>
        public string CurrentRichText { get; private set; } = string.Empty;

        /// <summary>타이핑이 끝까지 도달했을 때(스킵으로 즉시 완성된 경우 포함) 1회 발생.</summary>
        public event Action OnCompleted;

        public TextMeshProUGUI TargetText => EnsureTarget();

        private void Awake() => EnsureTarget();

        private void OnDisable()
        {
            // 비활성화 중 코루틴이 죽으므로 상태를 정리해 재활성 시 IsTyping이 남지 않게 한다.
            _routine = null;
            IsTyping = false;
        }

        /// <summary>
        /// 원본 대사를 리치 텍스트로 변환해 타이핑을 시작합니다.
        /// </summary>
        /// <param name="rawText">저작 원본(커스텀 [c:key] 마크업 허용)</param>
        /// <param name="palette">색상 키 팔레트. null이면 키가 흰색으로 폴백</param>
        /// <param name="typingSpeed">글자당 간격(초). 0 이하이면 즉시 완성</param>
        public void Play(string rawText, DialoguePaletteSO palette, float typingSpeed)
        {
            var text = EnsureTarget();
            if (text == null)
                return;

            Stop();

            CurrentRichText = DialogueMarkup.ToRichText(rawText, palette);
            text.text = CurrentRichText;
            text.ForceMeshUpdate();
            _totalVisibleCharacters = text.textInfo.characterCount;

            // 새 대사는 항상 처음부터 읽어야 하므로 스크롤을 맨 위로 되돌린다.
            ResetScrollToTop();

            if (typingSpeed <= 0f || _totalVisibleCharacters <= 0 || !isActiveAndEnabled)
            {
                CompleteImmediately();
                return;
            }

            text.maxVisibleCharacters = 0;
            IsTyping = true;
            _routine = StartCoroutine(TypeRoutine(typingSpeed));
        }

        /// <summary>
        /// 타이핑 중이면 즉시 전체를 표시합니다(타이핑 스킵/약). 진행 중이 아니면 아무 것도 하지 않습니다.
        /// </summary>
        /// <returns>이 호출로 타이핑을 완성했으면 true.</returns>
        public bool CompleteTyping()
        {
            if (!IsTyping)
                return false;

            CompleteImmediately();
            return true;
        }

        /// <summary>표시 내용을 비우고 진행 중인 타이핑을 중단합니다.</summary>
        public void Clear()
        {
            Stop();

            var text = EnsureTarget();
            if (text == null)
                return;

            text.text = string.Empty;
            text.maxVisibleCharacters = int.MaxValue;
            CurrentRichText = string.Empty;
            _totalVisibleCharacters = 0;
            ResetScrollToTop();
        }

        // ── 내부 ────────────────────────────────────────────────────────

        private void CompleteImmediately()
        {
            Stop();

            var text = EnsureTarget();
            if (text != null)
                text.maxVisibleCharacters = Mathf.Max(_totalVisibleCharacters, 0);

            OnCompleted?.Invoke();
        }

        private void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            IsTyping = false;
        }

        private IEnumerator TypeRoutine(float typingSpeed)
        {
            var text = EnsureTarget();
            float interval = Mathf.Max(0.001f, typingSpeed * ResolveSpeedScale());
            int visible = 0;

            while (visible < _totalVisibleCharacters)
            {
                // 정지 중에는 글자 진행도 대기 시간도 흐르지 않는다.
                if (IsPlaybackPaused())
                {
                    yield return null;
                    continue;
                }

                visible++;
                text.maxVisibleCharacters = visible;
                FollowRevealedCharacter(visible);

                float elapsed = 0f;
                while (elapsed < interval)
                {
                    if (!IsPlaybackPaused())
                        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                    yield return null;
                }
            }

            _routine = null;
            IsTyping = false;
            OnCompleted?.Invoke();
        }

        // ── 넘치는 본문 스크롤 추종 ───────────────────────────────────────

        private void ResetScrollToTop()
        {
            RectTransform content = bodyScrollRect != null ? bodyScrollRect.content : null;
            if (content == null)
                return;

            // 높이는 ContentSizeFitter가 계산하므로 재빌드 후에야 값이 맞는다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        }

        /// <summary>
        /// 지금 드러난 마지막 글자가 보이도록 스크롤을 내린다.
        /// maxVisibleCharacters 방식에서는 본문 높이가 처음부터 전체 길이로 잡히므로,
        /// 하단으로 한 번에 보내지 않고 드러나는 줄을 따라가야 읽는 위치가 유지된다.
        /// </summary>
        private void FollowRevealedCharacter(int visibleCount)
        {
            if (bodyScrollRect == null)
                return;

            RectTransform content = bodyScrollRect.content;
            RectTransform viewport = bodyScrollRect.viewport;
            var text = EnsureTarget();
            if (content == null || viewport == null || text == null)
                return;

            TMP_TextInfo info = text.textInfo;
            if (info == null || info.characterCount == 0)
                return;

            int index = Mathf.Clamp(visibleCount - 1, 0, info.characterCount - 1);
            float descender = info.characterInfo[index].descender;

            // 글자 하단을 content 로컬 좌표로 옮겨 '상단에서 얼마나 내려왔는지'를 구한다.
            Vector3 world = text.rectTransform.TransformPoint(new Vector3(0f, descender, 0f));
            float distanceFromTop = -content.InverseTransformPoint(world).y;

            float maxScroll = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            if (maxScroll <= 0f)
                return;

            float target = Mathf.Clamp(distanceFromTop + followBottomPadding - viewport.rect.height, 0f, maxScroll);

            // 타이핑 중에는 앞으로만 따라간다(플레이어가 위로 올려 둔 경우를 되돌리지 않기 위해).
            Vector2 position = content.anchoredPosition;
            if (target > position.y)
                content.anchoredPosition = new Vector2(position.x, target);
        }

        private static bool IsPlaybackPaused()
        {
            var dialogue = UISvc.Dialogue;
            return dialogue != null && dialogue.IsPaused;
        }

        // 설정에서 조정하는 전역 타이핑 속도 배율(작을수록 빠름).
        private static float ResolveSpeedScale()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null)
                return 1f;

            return Mathf.Max(0.01f, dialogue.TypingSpeedScale);
        }

        private TextMeshProUGUI EnsureTarget()
        {
            if (targetText == null)
                targetText = GetComponent<TextMeshProUGUI>();

            return targetText;
        }
    }
}
