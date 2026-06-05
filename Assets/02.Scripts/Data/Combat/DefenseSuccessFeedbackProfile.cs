using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.Data.Combat
{
    public enum DefenseSuccessType
    {
        Parry,
        PerfectGuard,
        PerfectDodge,
    }

    /// <summary>
    /// 방어 성공 순간 피드백 튜닝값.
    /// 에셋화 전 기본값을 코드에서 안전하게 사용하기 위한 직렬화 데이터다.
    /// </summary>
    [Serializable]
    public class DefenseSuccessFeedbackProfile
    {
        public DefenseSuccessType successType;

        [Header("Freeze")]
        [Min(0f)] public float freezeDuration = 0.08f;
        [Range(0.001f, 1f)] public float freezeTimeScale = 0.02f;
        [Min(0f)] public float attackerFreezeDuration = 0.18f;

        [Header("Tail Slow")]
        [Min(0f)] public float tailDuration = 0.24f;
        [Range(0.001f, 1f)] public float tailTimeScale = 0.18f;

        [Header("Post Process")]
        [Range(0f, 1f)] public float postProcessPeakWeight = 1f;
        [Min(0f)] public float postProcessHoldDuration = 0.08f;
        [Min(0.01f)] public float postProcessFadeOutDuration = 0.24f;
        [Min(0f)] public float minPostProcessVisibleDuration = 0.12f;

        [Header("Counter")]
        [Min(0f)] public float counterWindowDuration = 1.5f;

        [Header("Feedback")]
        public CameraShakeIdType shakeKey = CameraShakeIdType.CriticalHit;
        public string fxKey;
        public VitalOrbTrigger vitalOrbTrigger = VitalOrbTrigger.PerfectGuard;
        public bool spawnVitalOrb = true;

        public static DefenseSuccessFeedbackProfile CreateDefault(DefenseSuccessType type)
        {
            var profile = new DefenseSuccessFeedbackProfile { successType = type };

            switch (type)
            {
                case DefenseSuccessType.Parry:
                    profile.freezeDuration = 0.10f;
                    profile.freezeTimeScale = 0.01f;
                    profile.attackerFreezeDuration = 0.25f;
                    profile.tailDuration = 0.22f;
                    profile.tailTimeScale = 0.18f;
                    profile.postProcessHoldDuration = 0.08f;
                    profile.postProcessFadeOutDuration = 0.22f;
                    profile.counterWindowDuration = 1.5f;
                    profile.shakeKey = CameraShakeIdType.CriticalHit;
                    profile.vitalOrbTrigger = VitalOrbTrigger.PerfectGuard;
                    break;

                case DefenseSuccessType.PerfectGuard:
                    profile.freezeDuration = 0.08f;
                    profile.freezeTimeScale = 0.02f;
                    profile.attackerFreezeDuration = 0.18f;
                    profile.tailDuration = 0.20f;
                    profile.tailTimeScale = 0.20f;
                    profile.postProcessHoldDuration = 0.06f;
                    profile.postProcessFadeOutDuration = 0.20f;
                    profile.counterWindowDuration = 1.5f;
                    profile.shakeKey = CameraShakeIdType.CriticalHit;
                    profile.vitalOrbTrigger = VitalOrbTrigger.PerfectGuard;
                    break;

                case DefenseSuccessType.PerfectDodge:
                    profile.freezeDuration = 0.06f;
                    profile.freezeTimeScale = 0.03f;
                    profile.attackerFreezeDuration = 0.12f;
                    profile.tailDuration = 0.30f;
                    profile.tailTimeScale = 0.15f;
                    profile.postProcessHoldDuration = 0.08f;
                    profile.postProcessFadeOutDuration = 0.28f;
                    profile.counterWindowDuration = 1.2f;
                    profile.shakeKey = CameraShakeIdType.PlayerHit;
                    profile.vitalOrbTrigger = VitalOrbTrigger.Dodge;
                    break;
            }

            profile.minPostProcessVisibleDuration = Mathf.Max(
                profile.minPostProcessVisibleDuration,
                profile.postProcessHoldDuration);
            return profile;
        }
    }
}
