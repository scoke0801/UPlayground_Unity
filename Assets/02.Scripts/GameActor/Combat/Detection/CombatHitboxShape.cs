using UnityEngine;

namespace UPlayGround.Combat
{
    public enum CombatHitboxShapeType
    {
        Box,
        Capsule,
    }

    /// <summary>
    /// Collider에서 추출한 월드 공간 공격 판정 형상.
    /// </summary>
    public readonly struct CombatHitboxShape
    {
        public readonly CombatHitboxShapeType Type;
        public readonly Vector3 Center;
        public readonly Quaternion Rotation;
        public readonly Vector3 HalfExtents;
        public readonly Vector3 Point0;
        public readonly Vector3 Point1;
        public readonly float Radius;

        private CombatHitboxShape(
            CombatHitboxShapeType type,
            Vector3 center,
            Quaternion rotation,
            Vector3 halfExtents,
            Vector3 point0,
            Vector3 point1,
            float radius)
        {
            Type = type;
            Center = center;
            Rotation = rotation;
            HalfExtents = halfExtents;
            Point0 = point0;
            Point1 = point1;
            Radius = radius;
        }

        public static CombatHitboxShape Box(
            Vector3 center,
            Quaternion rotation,
            Vector3 halfExtents)
            => new(
                CombatHitboxShapeType.Box,
                center,
                rotation,
                halfExtents,
                default,
                default,
                0f);

        public static CombatHitboxShape Capsule(
            Vector3 center,
            Vector3 point0,
            Vector3 point1,
            float radius)
            => new(
                CombatHitboxShapeType.Capsule,
                center,
                Quaternion.identity,
                default,
                point0,
                point1,
                radius);

        public static CombatHitboxShape Lerp(
            in CombatHitboxShape from,
            in CombatHitboxShape to,
            float t)
        {
            if (to.Type == CombatHitboxShapeType.Box)
            {
                return Box(
                    Vector3.Lerp(from.Center, to.Center, t),
                    Quaternion.Slerp(from.Rotation, to.Rotation, t),
                    Vector3.Lerp(from.HalfExtents, to.HalfExtents, t));
            }

            return Capsule(
                Vector3.Lerp(from.Center, to.Center, t),
                Vector3.Lerp(from.Point0, to.Point0, t),
                Vector3.Lerp(from.Point1, to.Point1, t),
                Mathf.Lerp(from.Radius, to.Radius, t));
        }
    }
}
