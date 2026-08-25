using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 삽화 연출에 쓰는 가속 곡선. 트윈 라이브러리 타입을 계약에 노출하지 않으려고 별도 enum으로 둔다.
    /// 실제 곡선 대응은 <see cref="DialogueIllustrationEaseExtensions"/>가 소유한다.
    /// </summary>
    public enum DialogueIllustrationEase
    {
        /// <summary>속도가 일정하다. 오래 흐르는 패닝·줌에 쓴다.</summary>
        Linear = 0,
        /// <summary>끝에서 부드럽게 잦아든다.</summary>
        SmoothOut = 1,
        /// <summary>초반이 빠르고 확실히 멈춘다. 등장 연출에 쓴다.</summary>
        EaseOut = 2,
        /// <summary>가장 강하게 감속한다. 충격 연출에 쓴다.</summary>
        EaseOutStrong = 3,
        /// <summary>시작과 끝이 모두 부드럽다.</summary>
        EaseInOut = 4
    }

    /// <summary>대사 삽화가 노출되는 동안 재생할 연출 프리셋.</summary>
    public enum DialogueIllustrationMotion
    {
        /// <summary>프리셋을 고르지 않은 대사에 적용할 기본 연출. 은은하게 확대된다.</summary>
        Auto = 0,

        /// <summary>천천히 밀려 들어오는 확대. 감정을 쌓는 정적인 장면에 쓴다.</summary>
        SoftZoomIn = 1,
        /// <summary>천천히 물러나는 축소. 장면을 닫거나 여운을 남길 때 쓴다.</summary>
        SoftZoomOut = 2,
        /// <summary>짧고 단단하게 파고드는 확대. 대사의 무게를 실을 때 쓴다.</summary>
        PushIn = 3,
        /// <summary>짧게 물러나며 자리를 잡는 축소. 상황을 넓게 보여줄 때 쓴다.</summary>
        PullBack = 4,

        /// <summary>화면을 왼쪽으로 훑는 느린 패닝.</summary>
        DriftLeft = 5,
        /// <summary>화면을 오른쪽으로 훑는 느린 패닝.</summary>
        DriftRight = 6,
        /// <summary>화면을 위로 훑는 느린 패닝. 높이를 강조할 때 쓴다.</summary>
        DriftUp = 7,
        /// <summary>화면을 아래로 훑는 느린 패닝. 내려다보는 시선에 쓴다.</summary>
        DriftDown = 8,

        /// <summary>오른쪽에서 미끄러져 들어오는 등장.</summary>
        SlideInLeft = 9,
        /// <summary>왼쪽에서 미끄러져 들어오는 등장.</summary>
        SlideInRight = 10,
        /// <summary>아래에서 떠오르는 등장. 회상·독백에 어울린다.</summary>
        RiseUp = 11,
        /// <summary>크게 잡혔다가 정착하는 충격 연출. 반전·타격에 쓴다.</summary>
        Impact = 12,

        /// <summary>액션 에셋에 직접 입력한 수치를 그대로 사용한다.</summary>
        Custom = 100
    }

    /// <summary>삽화 연출 한 종류가 갖는 이동·확대 수치.</summary>
    public readonly struct DialogueIllustrationMotionValues
    {
        public DialogueIllustrationMotionValues(
            Vector2 startOffset,
            Vector2 endOffset,
            float startScale,
            float endScale,
            float duration,
            DialogueIllustrationEase ease)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            StartScale = Mathf.Max(0.01f, startScale);
            EndScale = Mathf.Max(0.01f, endScale);
            Duration = Mathf.Max(0f, duration);
            Ease = ease;
        }

        public Vector2 StartOffset { get; }
        public Vector2 EndOffset { get; }
        public float StartScale { get; }
        public float EndScale { get; }
        public float Duration { get; }
        public DialogueIllustrationEase Ease { get; }

        public bool HasMotion => Duration > 0f;
    }

    /// <summary>
    /// 삽화 연출 프리셋의 수치를 해석해 <see cref="DialogueIllustrationPresentation"/>으로 만든다.
    /// 어떤 저작 경로로 들어와도 정지 삽화가 남지 않도록, 재생 길이가 비어 있으면 기본 프리셋으로 폴백한다.
    /// </summary>
    public static class DialogueIllustrationMotionLibrary
    {
        /// <summary>프리셋을 고르지 않았거나 Custom 수치가 비어 있을 때 적용할 연출.</summary>
        public const DialogueIllustrationMotion DefaultMotion = DialogueIllustrationMotion.SoftZoomIn;

        // 읽는 동안 계속 살아 있어야 하는 패닝·줌은 대사 한 줄을 넘기는 길이를 준다.
        private const float AmbientDuration = 7f;
        private const float AccentDuration = 0.75f;
        private const float EntranceDuration = 0.5f;

        /// <summary>프리셋에 대응하는 이동·확대 수치를 돌려준다.</summary>
        public static DialogueIllustrationMotionValues Resolve(DialogueIllustrationMotion motion)
        {
            switch (motion)
            {
                case DialogueIllustrationMotion.SoftZoomOut:
                    return new DialogueIllustrationMotionValues(
                        Vector2.zero, Vector2.zero, 1.10f, 1.00f, AmbientDuration, DialogueIllustrationEase.SmoothOut);

                case DialogueIllustrationMotion.PushIn:
                    return new DialogueIllustrationMotionValues(
                        Vector2.zero, Vector2.zero, 1.00f, 1.06f, AccentDuration, DialogueIllustrationEase.EaseOut);

                case DialogueIllustrationMotion.PullBack:
                    return new DialogueIllustrationMotionValues(
                        Vector2.zero, Vector2.zero, 1.10f, 1.00f, AccentDuration, DialogueIllustrationEase.EaseOut);

                // 패닝은 화면 밖 여백이 드러나지 않도록 살짝 확대한 상태로 움직인다.
                case DialogueIllustrationMotion.DriftLeft:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(40f, 0f), new Vector2(-40f, 0f), 1.08f, 1.08f, AmbientDuration, DialogueIllustrationEase.SmoothOut);

                case DialogueIllustrationMotion.DriftRight:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(-40f, 0f), new Vector2(40f, 0f), 1.08f, 1.08f, AmbientDuration, DialogueIllustrationEase.SmoothOut);

                case DialogueIllustrationMotion.DriftUp:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(0f, -36f), new Vector2(0f, 36f), 1.08f, 1.08f, AmbientDuration, DialogueIllustrationEase.SmoothOut);

                case DialogueIllustrationMotion.DriftDown:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(0f, 36f), new Vector2(0f, -36f), 1.08f, 1.08f, AmbientDuration, DialogueIllustrationEase.SmoothOut);

                case DialogueIllustrationMotion.SlideInLeft:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(120f, 0f), Vector2.zero, 1.04f, 1.00f, EntranceDuration, DialogueIllustrationEase.EaseOut);

                case DialogueIllustrationMotion.SlideInRight:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(-120f, 0f), Vector2.zero, 1.04f, 1.00f, EntranceDuration, DialogueIllustrationEase.EaseOut);

                case DialogueIllustrationMotion.RiseUp:
                    return new DialogueIllustrationMotionValues(
                        new Vector2(0f, -80f), Vector2.zero, 1.02f, 1.00f, EntranceDuration, DialogueIllustrationEase.EaseOut);

                case DialogueIllustrationMotion.Impact:
                    return new DialogueIllustrationMotionValues(
                        Vector2.zero, Vector2.zero, 1.18f, 1.00f, 0.35f, DialogueIllustrationEase.EaseOutStrong);

                // Auto와 Custom(수치 미입력)은 기본 연출인 SoftZoomIn과 같은 수치를 쓴다.
                case DialogueIllustrationMotion.Auto:
                case DialogueIllustrationMotion.Custom:
                case DialogueIllustrationMotion.SoftZoomIn:
                default:
                    return new DialogueIllustrationMotionValues(
                        Vector2.zero, Vector2.zero, 1.00f, 1.08f, AmbientDuration, DialogueIllustrationEase.SmoothOut);
            }
        }

        /// <summary>
        /// 액션 에셋에 직접 입력한 수치를 해석한다.
        /// 재생 길이가 비어 있으면 삽화가 정지하므로 기본 프리셋으로 폴백한다.
        /// </summary>
        public static DialogueIllustrationMotionValues ResolveCustom(
            Vector2 startOffset,
            Vector2 endOffset,
            float startScale,
            float endScale,
            float duration,
            DialogueIllustrationEase ease)
        {
            if (duration <= 0f)
                return Resolve(DefaultMotion);

            return new DialogueIllustrationMotionValues(
                startOffset,
                endOffset,
                startScale,
                endScale,
                duration,
                ease);
        }

        /// <summary>프리셋과 직접 입력한 수치 중 저작 의도에 맞는 쪽을 해석한다.</summary>
        public static DialogueIllustrationMotionValues Resolve(
            DialogueIllustrationMotion motion,
            Vector2 startOffset,
            Vector2 endOffset,
            float startScale,
            float endScale,
            float duration,
            DialogueIllustrationEase ease)
        {
            return motion == DialogueIllustrationMotion.Custom
                ? ResolveCustom(startOffset, endOffset, startScale, endScale, duration, ease)
                : Resolve(motion);
        }

        /// <summary>해석한 수치를 UI가 소비하는 연출 값으로 변환한다.</summary>
        public static DialogueIllustrationPresentation ToPresentation(
            this DialogueIllustrationMotionValues values,
            bool revealImmediately = false,
            DialogueIllustrationPlacement placement = DialogueIllustrationPlacement.AboveDialogue,
            DialogueIllustrationPresentationMode mode =
                DialogueIllustrationPresentationMode.StandardDialogue,
            bool persistAcrossFollowingLines = false)
        {
            return new DialogueIllustrationPresentation(
                values.StartOffset,
                values.EndOffset,
                values.StartScale,
                values.EndScale,
                values.Duration,
                revealImmediately,
                placement,
                mode,
                persistAcrossFollowingLines,
                motionEase: values.Ease);
        }

        /// <summary>배경 삽화 위에 전경 삽화를 합성하는 연출 값으로 변환한다.</summary>
        public static DialogueIllustrationPresentation ToPresentation(
            this DialogueIllustrationMotionValues values,
            in DialogueIllustrationMotionValues foregroundValues,
            bool revealImmediately,
            DialogueIllustrationPlacement placement,
            DialogueIllustrationPresentationMode mode,
            bool persistAcrossFollowingLines)
        {
            return new DialogueIllustrationPresentation(
                values.StartOffset,
                values.EndOffset,
                values.StartScale,
                values.EndScale,
                values.Duration,
                revealImmediately,
                placement,
                mode,
                persistAcrossFollowingLines,
                foregroundValues.StartOffset,
                foregroundValues.EndOffset,
                foregroundValues.StartScale,
                foregroundValues.EndScale,
                foregroundValues.Duration,
                values.Ease,
                foregroundValues.Ease);
        }
    }
}
