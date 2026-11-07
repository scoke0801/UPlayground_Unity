using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 전투 로그 한 줄. CombatResult에 기록 시점 메타데이터를 붙인다.
    /// </summary>
    public readonly struct CombatLogEntry
    {
        public readonly int Sequence;
        public readonly int Frame;
        public readonly float CombatTime;
        public readonly float UnscaledTime;
        public readonly CombatResult Result;

        public CombatLogEntry(int sequence, int frame, float combatTime, float unscaledTime, CombatResult result)
        {
            Sequence = sequence;
            Frame = frame;
            CombatTime = combatTime;
            UnscaledTime = unscaledTime;
            Result = result;
        }

        public static CombatLogEntry Create(int sequence, CombatResult result)
            => new CombatLogEntry(sequence, Time.frameCount, Time.time, Time.unscaledTime, result);
    }
}
