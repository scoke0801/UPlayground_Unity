using DG.Tweening;

namespace UPlayGround.UI
{
    /// <summary>
    /// 삽화 연출 곡선을 실제 트윈 곡선으로 옮긴다.
    /// 계약(<see cref="DialogueIllustrationPresentation"/>)이 트윈 라이브러리 타입을 노출하지 않도록
    /// 변환 책임을 이 한 곳에 모은다.
    /// </summary>
    public static class DialogueIllustrationEaseExtensions
    {
        public static Ease ToTweenEase(this DialogueIllustrationEase ease)
        {
            switch (ease)
            {
                case DialogueIllustrationEase.SmoothOut:
                    return Ease.OutSine;
                case DialogueIllustrationEase.EaseOut:
                    return Ease.OutCubic;
                case DialogueIllustrationEase.EaseOutStrong:
                    return Ease.OutQuint;
                case DialogueIllustrationEase.EaseInOut:
                    return Ease.InOutSine;
                case DialogueIllustrationEase.Linear:
                default:
                    return Ease.Linear;
            }
        }
    }
}
