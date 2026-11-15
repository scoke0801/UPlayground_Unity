using System;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>개별 브레이크의 실제 전환을 구독자에게 중계한다. 과거 신호는 보관하지 않는다.</summary>
    public static class MonsterBreakSignals
    {
        public static event Action<MonsterActor, GameActor, ulong> Exposed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Exposed = null;
        internal static void Publish(MonsterActor target, GameActor source, ulong sequence)
        { if (target != null && source != null) Exposed?.Invoke(target, source, sequence); }
    }
}
