#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 개발 빌드 전용 명시적 판정 세션 레지스트리.
    /// <c>HitboxRuntimeDebugRenderer</c>가 부착형 <see cref="CombatHitbox"/>와 같은 방식으로 순회한다.
    /// 릴리스 빌드에서는 파일 전체가 컴파일되지 않으며 호출부도 조건부 컴파일로 스트립된다.
    /// </summary>
    public static class ExplicitCollisionDebugRegistry
    {
        private static readonly HashSet<CombatCollisionSession> s_active = new();

        public static IReadOnlyCollection<CombatCollisionSession> Active => s_active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_active.Clear();

        public static void Register(CombatCollisionSession session)
        {
            if (session != null)
                s_active.Add(session);
        }

        public static void Unregister(CombatCollisionSession session)
        {
            if (session != null)
                s_active.Remove(session);
        }
    }
}
#endif
