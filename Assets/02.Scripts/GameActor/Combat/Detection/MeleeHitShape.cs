using UnityEngine;

namespace UPlayGround.Combat
{
    public readonly struct MeleeHitShape
    {
        public readonly Transform Owner;
        public readonly Vector3 Origin;
        public readonly Vector3 Forward;
        public readonly float Radius;
        public readonly float HalfAngle;
        public readonly float HeightRange;
        public readonly LayerMask TargetLayer;

        public MeleeHitShape(
            Transform owner,
            Vector3 origin,
            Vector3 forward,
            float radius,
            float halfAngle,
            float heightRange,
            LayerMask targetLayer)
        {
            Owner = owner;
            Origin = origin;
            Forward = forward;
            Radius = radius;
            HalfAngle = halfAngle;
            HeightRange = heightRange;
            TargetLayer = targetLayer;
        }
    }
}
