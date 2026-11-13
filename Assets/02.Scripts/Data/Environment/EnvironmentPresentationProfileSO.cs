using UnityEngine;

namespace UPlayGround.Data.ReactiveEnvironment
{
    /// <summary>권위 상태를 바꾸지 않는 환경 효과와 시각 예산을 저작한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/환경 연출")]
    public sealed class EnvironmentPresentationProfileSO : ScriptableObject
    {
        public GameObject flame, smoke, impact, landing, leaves;
        public AudioClip impactSound, landingSound;
        [Min(1)] public int instancesPerEffect = 24;
        [Min(0)] public float maximumDistance = 60;
        [Range(0, 0.5f)] public float viewportMargin = 0.15f;
        [Range(0, 1)] public float heatingSmokeThreshold = 0.5f;
        [Range(0, 1)] public float minimumFlameStrength = 0.15f, wetFlameReduction = 0.75f;
        [Range(0, 1)] public float minimumBurnSmoke = 0.2f, heatingSmokeStrength = 0.25f;
        [Range(0.01f, 1)] public float particleVariation = 0.5f;
        [Min(0.01f)] public float residualSeconds = 6;
        [Min(0.01f)] public float steamSeconds = 1.2f;
        [Range(0, 1)] public float steamStrength = 0.4f;
        [Range(0, 1)] public float residualHeatThreshold = 0.05f;
        [Min(0)] public float flameRate = 18, smokeRate = 8;
        [Min(0.01f)] public float flameSize = 0.8f, flameSpeed = 1.8f;
        [Min(0)] public float updraft = 0.3f;
        [Min(0)] public float smokeHeightOffset = 0.6f;
        [Min(0.01f)] public float effectScale = 1;
        [Min(0)] public float hitCooldown = 0.12f;
        [Min(0.01f)] public float hitSeconds = 0.35f;
        [Min(0)] public float hitDegrees = 1.2f;
        [Min(0)] public float woundRadius = 0.6f;
        [Min(0.01f)] public float contactProbeDistance = 1;
        public LayerMask landingGroundLayers;
        [Min(0.01f)] public float landingProbeDistance = 3;
        [Min(1)] public int impactParticles = 8, landingParticles = 18;
        [Min(1)] public int terrainPatchCells = 2;
        [Range(0, 1)] public float damagedStage = 0.35f, severeStage = 0.7f;
        public bool IsValid => instancesPerEffect > 0 && maximumDistance > 0 && float.IsFinite(maximumDistance)
            && residualSeconds > 0 && float.IsFinite(residualSeconds) && hitSeconds > 0 && float.IsFinite(hitSeconds)
            && terrainPatchCells > 0 && damagedStage > 0 && severeStage > damagedStage && severeStage <= 1
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(flameRate)
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(smokeRate)
            && flameSize > 0 && float.IsFinite(flameSize) && flameSpeed > 0 && float.IsFinite(flameSpeed)
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(updraft)
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(smokeHeightOffset)
            && viewportMargin >= 0 && viewportMargin <= 0.5f
            && minimumFlameStrength >= 0 && minimumFlameStrength <= 1 && wetFlameReduction >= 0 && wetFlameReduction <= 1
            && minimumBurnSmoke >= 0 && minimumBurnSmoke <= 1 && heatingSmokeStrength >= 0 && heatingSmokeStrength <= 1
            && particleVariation > 0 && particleVariation <= 1
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(hitCooldown)
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(hitDegrees)
            && Core.ReactiveEnvironment.EnvironmentReaction.IsNonNegativeFinite(woundRadius)
            && effectScale > 0 && float.IsFinite(effectScale)
            && steamSeconds > 0 && float.IsFinite(steamSeconds) && steamStrength >= 0 && steamStrength <= 1
            && heatingSmokeThreshold >= 0 && heatingSmokeThreshold < 1 && residualHeatThreshold >= 0 && residualHeatThreshold <= 1
            && contactProbeDistance > 0 && float.IsFinite(contactProbeDistance)
            && landingProbeDistance > 0 && float.IsFinite(landingProbeDistance)
            && impactParticles > 0 && landingParticles > 0;

        /// <summary>권위 내구도에서 복원 가능한 세 단계 상처 크기.</summary>
        public float GetWoundRadius(float damage) => woundRadius * (damage >= severeStage ? 1 : damage >= damagedStage ? 0.65f : damage > 0 ? 0.3f : 0);
    }
}
