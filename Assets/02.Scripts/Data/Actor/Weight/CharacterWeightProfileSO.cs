using UnityEngine;

namespace UPlayGround.Data.Actor
{
    public enum CharacterWeightClass { Light, Standard, Heavy }

    [CreateAssetMenu(fileName = "CharacterWeightProfile", menuName = "UPlayGround/Actor/캐릭터 무게 프로필")]
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

}
