using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.CameraSystem
{
    public readonly struct CombatCameraIntent
    {
        public readonly CombatCameraIntentType Type;
        public readonly Transform Attacker;
        public readonly Transform Victim;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;
        public readonly CameraShakeIdType ShakeKey;
        public readonly float PunchStrength;
        public readonly float PunchDuration;

        public CombatCameraIntent(
            CombatCameraIntentType type,
            Transform attacker,
            Transform victim,
            Vector3 hitPoint,
            Vector3 hitDirection,
            AttackKind attackKind,
            AttackReactionType reactionType,
            CameraShakeIdType shakeKey,
            float punchStrength = 0f,
            float punchDuration = 0f)
        {
            Type = type;
            Attacker = attacker;
            Victim = victim;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            AttackKind = attackKind;
            ReactionType = reactionType;
            ShakeKey = shakeKey;
            PunchStrength = punchStrength;
            PunchDuration = punchDuration;
        }
    }
}
