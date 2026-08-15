using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Sound;

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
        public string soundKey;
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
                    profile.soundKey = GameSoundKey.CombatParry;
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
                    profile.soundKey = GameSoundKey.CombatPerfectGuard;
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
                    profile.soundKey = GameSoundKey.CombatPerfectDodge;
                    profile.vitalOrbTrigger = VitalOrbTrigger.Dodge;
                    break;
            }

            profile.minPostProcessVisibleDuration = Mathf.Max(
                profile.minPostProcessVisibleDuration,
                profile.postProcessHoldDuration);
            return profile;
        }

        /// <summary>
        /// 대시 회피 전용 프로필.
        /// 퍼펙트 도지 연출(카메라/오브)을 재사용하되, 타임스케일이 또렷하게 읽히도록 튜닝한다.
        /// - freeze는 짧게: 대시는 이동 중이라 멈춤이 길면 대시가 끊기는 느낌을 준다.
        /// - tail은 더 깊고 길게: 플레이어는 전속, 월드만 느려지는 구간이라 '스쳐 지나가는' 회피가 이 구간에서 읽힌다.
        /// 포스트프로세스(볼륨) 플래시는 호출부에서 끄므로 여기서 값은 의미 없다.
        /// </summary>
        public static DefenseSuccessFeedbackProfile CreateDashEvade()
        {
            var profile = CreateDefault(DefenseSuccessType.PerfectDodge);

            // 짧은 임팩트 정지(대시 끊김 최소화)
            profile.freezeDuration = 0.04f;
            profile.freezeTimeScale = 0.03f;
            profile.attackerFreezeDuration = 0.12f;

            // 깊고 길게 — 월드만 느려지는 tail 구간에서 회피가 또렷하게 읽힘
            profile.tailDuration = 0.45f;
            profile.tailTimeScale = 0.10f;
            profile.soundKey = GameSoundKey.PlayerDashEvade;

            // 대시 회피는 위협 스캔으로 판정하므로 퍼펙트 도지보다 훨씬 자주 성립한다.
            // 보상(바이탈 오브)까지 같이 주면 회복량이 증폭되므로 연출만 남긴다.
            // 반격창도 열지 않는다(소비자 없음) — 보상 계층은 퍼펙트 도지/패리의 몫으로 유지.
            profile.spawnVitalOrb = false;
            profile.counterWindowDuration = 0f;

            return profile;
        }
    }
}
