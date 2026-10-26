using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>현재 환경의 실제 강수량과 구름량을 시계 HUD에 제공한다.</summary>
    public interface IUIWeatherService : IGameService
    {
        bool IsReady { get; }
        float RainIntensity { get; }
        float CloudCoverage { get; }
    }
}
