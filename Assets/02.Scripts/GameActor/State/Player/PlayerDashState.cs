using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 전방 직선 대시 상태
    /// - 대시 중 Enemy 레이어를 CollidableLayers에서 제거해 충돌 무시
    /// - 대시 종료 시 ComputePenetration으로 겹침 해소 후 레이어 복구
    /// </summary>
    public class PlayerDashState : PlayerActorState
    {
        public override string StateName => "Dash";

        private Vector3 _dashDirection;
        private float _elapsedTime;
        private int _originalCollidableLayers;

        private static readonly int PlayerLayer   = LayerMask.NameToLayer("Player");
        private static readonly int EnemyLayer    = LayerMask.NameToLayer("Enemy");
        private static readonly int EnemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        public PlayerDashState(ActorMovementController controller) : base(controller) { }

        // ─── 상태 전환 제한 ────────────────────────────────────────────
        public override bool CanTransitionToState(string stateName)
        {
            return true;
        }

        // ─── Enter ─────────────────────────────────────────────────────
        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _elapsedTime = 0f;

            _dashDirection = playerController.HasMoveInput()
                ? playerController.MoveInputVector.normalized
                : motor.CharacterForward;

            motor.SetRotation(Quaternion.LookRotation(_dashDirection, motor.CharacterUp));

            // Enemy 레이어를 CollidableLayers에서 제거 + Physics 레벨 충돌 무시
            _originalCollidableLayers = motor.CollidableLayers;
            motor.CollidableLayers &= ~EnemyLayerMask;
            Physics.IgnoreLayerCollision(PlayerLayer, EnemyLayer, true);

            var animState = gameActor.Animator.PlayMotion(AnimKey.Dash, 0.1f);
            if (animState != null)
                animState.OwnedEvents.OnEnd = OnAnimationEnd;
            else
                FinishDash();
        }

        // ─── Exit ──────────────────────────────────────────────────────
        public override void OnExit(GameActorState toState)
        {
            ResolvePenetrationAndRestoreLayer();
            base.OnExit(toState);
        }

        // ─── Velocity ──────────────────────────────────────────────────
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = _dashDirection * controller.DashSpeed;
            currentVelocity.y = 0f;
        }

        // ─── State 업데이트 ─────────────────────────────────────────────
        public override void UpdateState(float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_elapsedTime >= controller.DashDuration)
                FinishDash();
        }

        // ─── 내부 메서드 ───────────────────────────────────────────────

        /// <summary>
        /// 대시 종료 시:
        /// 1. Enemy 레이어 콜라이더와의 겹침을 ComputePenetration으로 해소
        /// 2. CollidableLayers 원복
        /// </summary>
        private void ResolvePenetrationAndRestoreLayer()
        {
            // 1. 겹침 해소 - Enemy 레이어 콜라이더를 직접 쿼리
            Vector3 resolvedPosition = motor.TransientPosition;

            Vector3 capsuleBottom = resolvedPosition + motor.TransientRotation * motor.CharacterTransformToCapsuleBottomHemi;
            Vector3 capsuleTop    = resolvedPosition + motor.TransientRotation * motor.CharacterTransformToCapsuleTopHemi;

            int hitCount = Physics.OverlapCapsuleNonAlloc(
                capsuleBottom,
                capsuleTop,
                motor.Capsule.radius,
                OverlapBuffer,
                EnemyLayerMask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null || col == motor.Capsule) continue;

                bool overlapping = Physics.ComputePenetration(
                    motor.Capsule,      resolvedPosition,        motor.TransientRotation,
                    col,                col.transform.position,  col.transform.rotation,
                    out Vector3 dir,    out float dist
                );

                if (overlapping)
                    resolvedPosition += dir * (dist + KinematicCharacterMotor.CollisionOffset);
            }

            if (resolvedPosition != motor.TransientPosition)
                motor.SetPosition(resolvedPosition);

            // 2. CollidableLayers 원복 + Physics 레벨 충돌 복구
            motor.CollidableLayers = _originalCollidableLayers;
            Physics.IgnoreLayerCollision(PlayerLayer, EnemyLayer, false);
        }

        private void FinishDash()
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }

        private void OnAnimationEnd()
        {
            if (controller.CurrentState == this)
                FinishDash();
        }
    }
}
