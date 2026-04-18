using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.State
{
    /// <summary>
    /// 월드 속도를 액터 로컬 기준 각도로 변환해 적절한 방향성 AnimKey를 반환한다.
    /// 몸이 항상 타겟을 향하는 적 몬스터의 8방향 로코모션에 사용.
    /// </summary>
    public static class EnemyLocomotionHelper
    {
        public enum LocoStyle { WalkSlow, Walk, Run }

        // 속도가 이 이하면 방향 갱신 생략 (감속 중 애니메이션 전환 방지)
        public const float MIN_SPEED_SQ = 0.25f; // 0.5 m/s

        /// <summary>
        /// 로컬 각도(deg, −180~180, 오른쪽 양수)를 AnimKey로 변환한다.
        /// </summary>
        public static AnimKey GetKey(float localAngleDeg, LocoStyle style)
        {
            float abs   = Mathf.Abs(localAngleDeg);
            bool  right = localAngleDeg >= 0f;

            return style switch
            {
                LocoStyle.Run =>
                    abs <= 22.5f  ? AnimKey.Run :
                    abs <= 67.5f  ? (right ? AnimKey.Run_F_R45  : AnimKey.Run_F_L45) :
                    abs <= 112.5f ? (right ? AnimKey.Run_F_R90  : AnimKey.Run_F_L90) :
                    abs <= 157.5f ? (right ? AnimKey.Run_B_R45  : AnimKey.Run_B_L45) :
                                    AnimKey.Run_B,

                LocoStyle.WalkSlow =>
                    abs <= 22.5f  ? AnimKey.Walk_Slow :
                    abs <= 67.5f  ? (right ? AnimKey.Walk_Slow_F_R45  : AnimKey.Walk_Slow_F_L45) :
                    abs <= 112.5f ? (right ? AnimKey.Walk_Slow_F_R90  : AnimKey.Walk_Slow_F_L90) :
                    abs <= 157.5f ? (right ? AnimKey.Walk_Slow_B_R45  : AnimKey.Walk_Slow_B_L45) :
                                    AnimKey.Walk_Slow_B,

                _ =>  // Walk
                    abs <= 22.5f  ? AnimKey.Walk :
                    abs <= 67.5f  ? (right ? AnimKey.Walk_F_R45  : AnimKey.Walk_F_L45) :
                    abs <= 112.5f ? (right ? AnimKey.Walk_F_R90  : AnimKey.Walk_F_L90) :
                    abs <= 157.5f ? (right ? AnimKey.Walk_B_R45  : AnimKey.Walk_B_L45) :
                                    AnimKey.Walk_B,
            };
        }

        /// <summary>
        /// 월드 속도 벡터와 액터 Transform을 받아 방향성 AnimKey를 반환한다.
        /// </summary>
        public static AnimKey GetDirectionalKey(Vector3 worldVelocity, Transform actorTransform, LocoStyle style)
        {
            if (worldVelocity.sqrMagnitude < MIN_SPEED_SQ)
                return ForwardKey(style);

            Vector3 local = actorTransform.InverseTransformDirection(worldVelocity);
            local.y = 0f;
            if (local.sqrMagnitude < 0.001f) return ForwardKey(style);

            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            return GetKey(angle, style);
        }

        /// <summary>
        /// 각 스타일의 정방향(기본) 키를 반환한다.
        /// </summary>
        public static AnimKey ForwardKey(LocoStyle style) => style switch
        {
            LocoStyle.Run      => AnimKey.Run,
            LocoStyle.WalkSlow => AnimKey.Walk_Slow,
            _                  => AnimKey.Walk,
        };

        /// <summary>
        /// 방향성 애니메이션 없는 액터용 — WalkSlow 포함 전부 Walk/Run으로 수렴.
        /// </summary>
        public static AnimKey BasicKey(LocoStyle style) => style switch
        {
            LocoStyle.Run => AnimKey.Run,
            _             => AnimKey.Walk,
        };

        /// <summary>
        /// UpdateState에서 매 프레임 호출 — 키가 바뀔 때만 PlayMotion을 실행한다.
        /// lastKey를 ref로 추적하므로 호출 측에서 별도 필드를 유지할 필요 없음.
        /// fallbackMotionSet이 없는 액터는 자동으로 Walk/Run만 사용.
        /// </summary>
        public static void UpdateAnim(
            GameActor actor,
            KinematicCharacterController.KinematicCharacterMotor motor,
            ref AnimKey lastKey,
            LocoStyle style,
            float crossfade = 0.15f)
        {
            Vector3 vel = motor.Velocity;
            vel.y = 0f;

            if (vel.sqrMagnitude < MIN_SPEED_SQ) return; // 감속 중 유지

            AnimKey key = actor.Animator.HasFallbackMotionSet
                ? GetDirectionalKey(vel, actor.transform, style)
                : BasicKey(style);

            if (key == lastKey) return;

            actor.Animator.PlayMotion(key, crossfade);
            lastKey = key;
        }
    }
}
