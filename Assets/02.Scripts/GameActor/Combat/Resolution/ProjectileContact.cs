using UnityEngine;

namespace UPlayGround.Combat
{
    public enum ProjectileDefenseSource { None, Guard, Attack, Assist }
    public enum ProjectileResponse { None, Reflected, Absorbed }

    /// <summary>풀 재사용이나 중복 접촉이 방어 적용으로 이어지지 않게 하는 불변 접촉 식별자.</summary>
    public readonly struct ProjectileContact
    {
        public readonly ProjectileRuntime Runtime;
        public readonly ulong LaunchId;
        public readonly uint PoolGeneration;
        public readonly uint ContactId;
        public readonly float Time;
        public readonly Vector3 Center;

        public ProjectileContact(ProjectileRuntime runtime, ulong launchId, uint generation,
            uint contactId, float time, Vector3 center)
        {
            Runtime = runtime;
            LaunchId = launchId;
            PoolGeneration = generation;
            ContactId = contactId;
            Time = time;
            Center = center;
        }

        public bool IsValid => Runtime != null && LaunchId != 0 && ContactId != 0;
    }
}
