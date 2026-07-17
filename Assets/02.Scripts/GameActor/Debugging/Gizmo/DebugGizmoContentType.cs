using System;

namespace UPlayGround.Debugging
{
    [Flags]
    public enum DebugGizmoContentType
    {
        None = 0,
        PlayerCombatHit = 1 << 0,
        EnemyDetection = 1 << 1,
        MotionWarp = 1 << 2,
        HitboxSwingTrail = 1 << 3,
        All = ~0,
    }
}
