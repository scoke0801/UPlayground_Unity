using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Data.Sound;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 환경 넉백 T0 — 넉백으로 밀려나던 액터가 벽에 부딪혔을 때의 판정과 연출.
    ///
    /// 물리 반사(월 바운스)나 별도 상태 승격(월 스플랫)은 여기서 하지 않는다.
    /// "잔여 넉백을 즉시 소멸시키고 충돌 임팩트를 낸다"까지가 T0의 범위다.
    /// </summary>
    public static class WallImpactResolver
    {
        /// <summary>이 속도 미만으로 벽에 닿는 건 무시한다(다 죽은 넉백이 벽을 스치는 경우).</summary>
        private const float MinImpactSpeed = 4f;

        /// <summary>
        /// 충돌면 법선과 캐릭터 Up의 내적 허용치. 이보다 크면 경사면·계단·작은 턱으로 보고 벽으로 취급하지 않는다.
        /// </summary>
        private const float MaxWallNormalUpDot = 0.35f;

        /// <summary>
        /// 벽 충돌을 판정하고, 성립하면 잔여 넉백을 소멸시킨 뒤 임팩트 연출을 낸다.
        /// </summary>
        /// <returns>벽 충돌로 처리했으면 true. 호출자는 중복 발동을 막을 소비 플래그를 직접 관리해야 한다.</returns>
        public static bool TryApplyWallImpact(
            ActorMovementController controller,
            AttackReactionType reactionType,
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            in HitStabilityReport hitStabilityReport)
        {
            if (controller == null)
                return false;

            // HasImpulse는 게이트로 쓰지 않는다. 넉백을 damper가 아니라 상태가 직접
            // UpdateVelocity에서 구동하는 경우(EnemyKnockdownState)에는 항상 false라
            // 정작 벽 충돌이 중요한 리액션에서 판정이 통째로 죽는다.
            // 실제 게이트는 아래의 리액션 타입 + 평면 속도 조합이다.

            // 넉백성 리액션만 대상. 평타(Light/Hit)까지 벽 연출을 내면 값이 없어진다.
            if (reactionType is not (AttackReactionType.KnockBack
                or AttackReactionType.Airborne
                or AttackReactionType.Knockdown))
            {
                return false;
            }

            Vector3 up = controller.Motor != null ? controller.Motor.CharacterUp : Vector3.up;

            // 경사면·계단을 벽으로 오판하지 않도록 "수직에 가까운 면 + 불안정 표면"을 함께 요구한다.
            if (hitStabilityReport.IsStable)
                return false;
            if (Mathf.Abs(Vector3.Dot(hitNormal, up)) > MaxWallNormalUpDot)
                return false;

            // 다른 액터와의 충돌은 제외. 적끼리 밀치다 벽 연출이 터지면 안 된다.
            if (hitCollider != null && hitCollider.GetComponentInParent<GameActor>() != null)
                return false;

            float planarSpeed = Vector3.ProjectOnPlane(controller.PredictedVelocity, up).magnitude;
            if (planarSpeed < MinImpactSpeed)
                return false;

            // 잔여 외부 속도 요청을 비우는 것만으로는 부족하다.
            // 넉백은 등록 다음 스텝에 이미 Motor.Velocity로 흡수돼 있고, damper는 그 값을
            // 매 프레임 "차감"하는 역할이라 damper만 지우면 오히려 감쇠가 사라진다.
            // 합성 결과 자체를 눌러야 실제로 멈춘다.
            controller.RequestPlanarVelocityStop();

            PlayImpactFeedback(hitPoint, hitNormal);
            return true;
        }

        private static void PlayImpactFeedback(Vector3 hitPoint, Vector3 hitNormal)
        {
            // 벽면에서 튀어나오는 방향으로 FX를 정렬한다.
            CombatFeedbackDispatcher.ShowHitFx(
                FXKeyType.DefaultCombatHit.ToKey(),
                hitPoint,
                hitNormal);

            string soundKey = Svc.Sound?.HasSound(GameSoundKey.CombatWallImpact) == true
                ? GameSoundKey.CombatWallImpact
                : GameSoundKey.CombatHitHeavy;
            Svc.Sound?.PlaySfx(soundKey, hitPoint);
        }
    }
}
