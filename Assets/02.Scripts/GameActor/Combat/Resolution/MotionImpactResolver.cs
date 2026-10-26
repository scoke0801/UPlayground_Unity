using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Combat;

namespace UPlayGround.Combat
{
    /// <summary>부착형 근접 적중 직전에 베이크된 운동 정보를 피해와 반응으로 해석한다.</summary>
    public static class MotionImpactResolver
    {
        /// <summary>활성 모션·타격 구간이 일치할 때만 적용하며, 명시적 범위와 투사체에는 적용하지 않는다.</summary>
        public static void Prepare(AttackData attack, ActorAnimator animator, bool isAttachedCollision)
        {
            if (attack == null) return;
            attack.motionImpact = default;
            var profile = attack.reactionProfile?.motionResponse;
            if (!isAttachedCollision || attack.isProjectile || profile == null || animator == null
                || animator.CurrentMotionAsset != attack.motionAsset || !animator.IsPlayingMotionSet) return;
            var catalog = animator.ResolveMotionImpactCatalog(attack.ResolvedMotionKey);
            if (catalog == null) return;
            attack.motionImpact = Evaluate(catalog, profile, attack.ResolvedMotionKey, attack.hitPhaseIndex,
                animator.CurrentEvaluatedMotionSetTime, animator.transform.rotation, animator.CurrentMotionEventTime);
        }

        /// <summary>같은 구간의 피해·히트스톱은 일정하게 유지하고 방향만 적중 시각에서 평가한다.</summary>
        public static MotionImpactResult Evaluate(MotionImpactCatalogSO catalog, MotionImpactResponseSO profile,
            UPlayGround.Data.Actor.Animation.MotionKey key, int phase, float time, Quaternion rotation,
            float? collisionTime = null)
        {
            if (catalog == null || profile == null || float.IsNaN(time) || float.IsInfinity(time)) return default;
            // 포즈가 다음 구간까지 앞서도 배율은 실제로 열린 충돌 창에 귀속된다.
            // 되감긴 포즈에 이전 창을 적용하지 않으며 방향만 해당 창의 종료 경계까지 평가한다.
            float windowTime = collisionTime ?? time;
            if (time < windowTime
                || !catalog.TrySample(key, phase, windowTime, out var window, out _)
                || !window.TrySampleVelocity(Mathf.Clamp(time, window.start, window.end), out var velocity))
                return default;
            profile.Evaluate(window.meanSpeed, out float multiplier, out float duration);
            bool hasDirection = profile.useSwingDirection && velocity.sqrMagnitude >
                Mathf.Max(0.000001f, profile.minimumDirectionSpeed * profile.minimumDirectionSpeed);
            return new MotionImpactResult
            {
                HasValue = true,
                DamageMultiplier = multiplier,
                HitStopDuration = duration,
                HitStopScale = Mathf.Clamp(profile.hitStopScale, 0.001f, 1f),
                HasDirection = hasDirection,
                WorldDirection = hasDirection ? rotation * velocity.normalized : default,
                MeanSpeed = window.meanSpeed,
            };
        }
    }

    /// <summary>공유 SO를 바꾸지 않고 한 번의 적중 계산과 피드백이 공유하는 운동 기반 결과.</summary>
    public struct MotionImpactResult
    {
        public bool HasValue, HasDirection;
        public float DamageMultiplier, HitStopDuration, HitStopScale, MeanSpeed;
        public Vector3 WorldDirection;
    }
}
