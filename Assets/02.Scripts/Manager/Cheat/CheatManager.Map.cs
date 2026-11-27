#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 지도 치트. 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>현재 지도의 탐험 안개를 모두 걷는다. 탐험 기록에 남아 저장하면 유지된다.</summary>
        public bool RevealCurrentMap()
        {
            MapExplorationManager exploration = MapExplorationManager.Instance;
            if (exploration == null)
            {
                Log(CheatCategory.Map, "지도 밝히기 실패: 탐험 관리자 없음");
                return false;
            }

            bool revealed = exploration.TryRevealCurrentMap(out string failureReason);
            Log(CheatCategory.Map, revealed
                ? "현재 지도를 전부 밝힘"
                : $"지도 밝히기 실패: {failureReason}");
            return revealed;
        }
    }
}
#endif
