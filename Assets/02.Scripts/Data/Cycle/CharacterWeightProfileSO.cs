using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    public enum CharacterWeightClass { Light, Standard, Heavy }

    [CreateAssetMenu(fileName = "CharacterWeightProfile", menuName = "UPlayGround/사이클/캐릭터 무게 프로필")]
    public sealed class CharacterWeightProfileSO : ScriptableObject
    {
        public CharacterWeightClass weightClass = CharacterWeightClass.Standard;
        [Min(0.01f)] public float moveSpeedMultiplier = 1f;
        [Min(0.01f)] public float attackTempoMultiplier = 1f;
        [Min(0.01f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float breakDamageMultiplier = 1f;
        [Range(0.1f, 0.6f)] public float dodgeIFrameSeconds = 0.35f;
        public VitalRecoveryPolicySO recoveryPolicy;

        public bool Validate(out string error)
        {
            if (moveSpeedMultiplier <= 0f || attackTempoMultiplier <= 0f || damageMultiplier <= 0f || breakDamageMultiplier <= 0f)
            { error = "모든 무게 배율은 0보다 커야 합니다."; return false; }
            if (dodgeIFrameSeconds is < 0.1f or > 0.6f)
            { error = "회피 무적 시간은 0.1~0.6초여야 합니다."; return false; }
            error = null;
            return true;
        }
    }

    [CreateAssetMenu(fileName = "VitalRecoveryPolicy", menuName = "UPlayGround/사이클/바이탈 회복 정책")]
    public sealed class VitalRecoveryPolicySO : ScriptableObject
    {
        [Header("일반 유효 히트")]
        [Range(0f, 1f)] public float normalHitSpawnChance = 0.1f;
        [Min(0)] public int normalHitOrbCount = 1;
        [Min(0f)] public float normalHitHealScale = 0.25f;
        [Header("브레이크 특수공격")]
        [Range(0f, 1f)] public float specialBreakSpawnChance = 0.25f;
        [Min(0)] public int specialBreakOrbCount = 1;
        [Min(0f)] public float specialBreakHealScale = 1f;
    }

}
