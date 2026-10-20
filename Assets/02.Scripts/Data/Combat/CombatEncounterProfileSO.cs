using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>교전 판정과 플레이어 보정, 그룹 대응의 공통 조정값.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/전투/교전 프로필")]
    public sealed class CombatEncounterProfileSO : ScriptableObject
    {
        public const string ResourceKey = "CombatEncounterProfile";
        [Min(0f)] public float bossRadius = 20f;
        [Min(0f)] public float releaseHoldSeconds = 2f;
        [Min(0.01f)] public float detectionInterval = 0.1f;
        [Min(0f)] public float sweepAttackAssistMultiplier = 1.4f;
        [Min(0f)] public float sweepDashAssistMultiplier = 1.3333333f;
        [Min(0f)] public float duelAttackAssistMultiplier = 0.8f;
        [Min(0f)] public float duelDashAssistMultiplier = 0.44444445f;
        [Min(0f)] public float actionAlignmentSpeed = 1200f;
        [Min(0f)] public float actionAlignmentSeconds = 0.15f;
        [Min(0f)] public float guardTrackingSpeed = 360f;
        [Min(0f)] public float longComboSeconds = 3f;
        [Min(0f)] public float continuousHitGap = 1.5f;

        public float GetAssistMultiplier(CombatEncounterKind kind, bool isDash) => kind switch
        {
            CombatEncounterKind.Sweep => isDash ? sweepDashAssistMultiplier : sweepAttackAssistMultiplier,
            CombatEncounterKind.Duel => isDash ? duelDashAssistMultiplier : duelAttackAssistMultiplier,
            _ => 1f,
        };
    }
}
