using UPlayGround.Data.Path;

namespace UPlayGround.Combat
{
    public readonly struct PlayerAttackHitFeedbackProfile
    {
        public readonly float PunchStrengthLight;
        public readonly float PunchStrengthHeavy;
        public readonly float PunchStrengthSkill;
        public readonly float PunchDurationLight;
        public readonly float PunchDurationHeavy;
        public readonly float PunchDurationSkill;
        public readonly CameraShakeIdType ShakeKeyLight;
        public readonly CameraShakeIdType ShakeKeyHeavy;

        public PlayerAttackHitFeedbackProfile(
            float punchStrengthLight,
            float punchStrengthHeavy,
            float punchStrengthSkill,
            float punchDurationLight,
            float punchDurationHeavy,
            float punchDurationSkill,
            CameraShakeIdType shakeKeyLight,
            CameraShakeIdType shakeKeyHeavy)
        {
            PunchStrengthLight = punchStrengthLight;
            PunchStrengthHeavy = punchStrengthHeavy;
            PunchStrengthSkill = punchStrengthSkill;
            PunchDurationLight = punchDurationLight;
            PunchDurationHeavy = punchDurationHeavy;
            PunchDurationSkill = punchDurationSkill;
            ShakeKeyLight = shakeKeyLight;
            ShakeKeyHeavy = shakeKeyHeavy;
        }
    }
}
