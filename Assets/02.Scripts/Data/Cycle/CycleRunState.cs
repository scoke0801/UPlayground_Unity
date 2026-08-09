using System;

namespace UPlayGround.Data.Cycle
{
    /// <summary>사이클 실행의 명시적인 진행 단계.</summary>
    public enum CycleRunPhase
    {
        Inactive,
        Preparing,
        Active,
        BossDefeated,
        Settling,
        Completed,
    }

    /// <summary>서로 추첨 순서를 공유하지 않는 결정적 난수 스트림.</summary>
    public enum CycleRandomStream
    {
        Layout,
        BossPool,
        Reward,
        Encounter,
        Loot,
        Interaction,
        Quest,
    }

    /// <summary>
    /// 저장과 이벤트 전달에 사용하는 사이클 실행 순수 데이터.
    /// UnityEngine.Object 참조는 보관하지 않는다.
    /// </summary>
    [Serializable]
    public sealed class CycleRunState
    {
        public int cycleIndex;
        public int seed;
        public string mapId;
        public CycleRunPhase phase;
        public float elapsedSeconds;
        public bool centralBossDefeated;
        public bool exitPortalActivated;

        public CycleRunState Clone()
        {
            return new CycleRunState
            {
                cycleIndex = cycleIndex,
                seed = seed,
                mapId = mapId,
                phase = phase,
                elapsedSeconds = elapsedSeconds,
                centralBossDefeated = centralBossDefeated,
                exitPortalActivated = exitPortalActivated,
            };
        }

        public static CycleRunState CreateInactive()
        {
            return new CycleRunState
            {
                phase = CycleRunPhase.Inactive,
                mapId = string.Empty,
            };
        }
    }
}
