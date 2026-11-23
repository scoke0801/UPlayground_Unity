using UPlayGround.Data.UI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>프로필의 연출 종류에 맞는 <see cref="IGuideEffect"/> 구현을 만든다. 새 연출은 여기에 한 줄을 더한다.</summary>
    public static class GuideEffectFactory
    {
        public static IGuideEffect Create(GuideEffectKind kind)
        {
            return kind switch
            {
                GuideEffectKind.Wind => new WindGuideEffect(),
                _ => new ButterflyGuideEffect(),
            };
        }
    }
}
