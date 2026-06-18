using System;

namespace UPlayGround.Debugging
{
    [Flags]
    public enum DebugGizmoCategory
    {
        None = 0,
        Combat = 1 << 0,
        AI = 1 << 1,
        Movement = 1 << 2,
        Camera = 1 << 3,
        Projectile = 1 << 4,
        SpawnGroup = 1 << 5,
        Animation = 1 << 6,
        All = ~0,
    }
}
