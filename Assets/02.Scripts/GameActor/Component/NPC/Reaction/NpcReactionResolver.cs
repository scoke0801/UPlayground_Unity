using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>NPC 전투 반응의 강도. 값이 클수록 위협에 가깝다.</summary>
    public enum NpcReactionLevel
    {
        None = 0,
        Watch = 1,
        Cower = 2,
        Flee = 3,
    }

    /// <summary>위협까지의 거리와 현재 단계로 반응 단계를 정하는 순수 판정.</summary>
    public static class NpcReactionResolver
    {
        /// <summary>단계의 기본 반경. None이면 0.</summary>
        public static float GetRadius(NpcCombatReactionProfileSO profile, NpcReactionLevel level)
        {
            if (profile == null) return 0f;
            return level switch
            {
                NpcReactionLevel.Watch => profile.watchRadius,
                NpcReactionLevel.Cower => profile.cowerRadius,
                NpcReactionLevel.Flee => profile.fleeRadius,
                _ => 0f,
            };
        }

        /// <summary>
        /// 거리가 해당 단계 반경 안인지 본다. 현재 단계가 같거나 더 높으면 반경을 넓혀 적용한다(경계 깜빡임 방지).
        /// </summary>
        public static bool IsWithinLevel(
            NpcCombatReactionProfileSO profile, NpcReactionLevel level, NpcReactionLevel current, float distance)
        {
            if (profile == null || level == NpcReactionLevel.None) return false;
            float radius = GetRadius(profile, level);
            if (current >= level) radius *= profile.releaseRadiusScale;
            return distance <= radius;
        }

        /// <summary>거리에 맞는 가장 높은 단계. 위협이 없으면 None.</summary>
        public static NpcReactionLevel Resolve(
            NpcCombatReactionProfileSO profile, NpcReactionLevel current, float distance)
        {
            if (IsWithinLevel(profile, NpcReactionLevel.Flee, current, distance)) return NpcReactionLevel.Flee;
            if (IsWithinLevel(profile, NpcReactionLevel.Cower, current, distance)) return NpcReactionLevel.Cower;
            if (IsWithinLevel(profile, NpcReactionLevel.Watch, current, distance)) return NpcReactionLevel.Watch;
            return NpcReactionLevel.None;
        }
    }
}
