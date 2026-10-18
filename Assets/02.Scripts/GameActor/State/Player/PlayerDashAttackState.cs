using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerDashAttackState : PlayerActorState
    {
        
        public override ActorStateId StateId => ActorStateId.DashAttack;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;

        private AttackData _attackData;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private MotionWarpController _motionWarp;
        private Transform _homingTarget;
        
        public PlayerDashAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 대시 공격 진입을 승인한 강공격은 이 상태가 즉시 소유한다.
            // 호출부가 OnEnter 이후에 소비하면 전투 시작 이벤트가 같은 입력을 다시 관찰할 수 있다.
            Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);

            gameActor.MoveAnimType = BaseMoveAnimType.Run;

            _motionWarp = controller.MotionWarp;
            _combat = playerActor.GetCombat();
            _attackData = _combat?.ExecuteDashAttack();

            // 이 공격 동안엔 첫 타겟만 유지 — 타임라인 워프 이벤트가 다른 적으로 재결정해
            // 한 타격 안에서 방향이 여러 번 튀는 것을 막는다.
            _motionWarp?.BeginTargetLock(CameraManager.Instance.GetLockOnTarget() != null
                ? Vector3.zero : playerController.MoveInputVector);
            _homingTarget = _combat.GetAttackWarpTarget(FindHomingTarget(), _combat.DashAttackAssistSearchRange);

            _motionWarp?.SetTarget(_homingTarget);
            (gameActor.Animator as PlayerActorAnimator)?.SetWeaponAimTarget(_homingTarget);

            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            var state = _attackData?.motionAsset != null
                ? gameActor.Animator.PlayMotion(
                    _attackData.motionAsset, _attackData.ResolvedMotionKey, 0.1f)
                : null;
            if (state != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
            }
            else
            {
                _motionWarp?.EndTargetLock();
                _motionWarp?.ClearTarget();
                controller.TransitionToState(ActorStateId.Idle);
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            _motionWarp?.EndTargetLock();
            _motionWarp?.EndMotionWarpForStateExit();
            _motionWarp?.ClearTarget();
            _combat?.ClearHitTargets();
            gameActor.Animator.Speed = gameActor.LocalTimeScale;
            _homingTarget = null;
            (gameActor.Animator as PlayerActorAnimator)?.SetWeaponAimTarget(null);
            
            base.OnExit(toState);
        }

        private Transform FindHomingTarget()
        {
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
                return lockOnTarget;

            if (_combat == null || _attackData == null)
                return null;

            return _combat.FindDashAttackFacingTarget();
        }

        private void OnAttackAnimationEnd()
        {
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(ActorStateId.GroundMove);
            }
            else
            {
                controller.TransitionToState(ActorStateId.Idle);
            }
        }
        
        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (fromState == ActorStateId.Hit) return false;
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Dash))
            {
                if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Dash);
                    return;
                }
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            Vector3 rootMotionVel = gameActor.Animator.GetRootMotionStepVelocity(deltaTime);
            if (_motionWarp != null)
            {
                gameActor.Animator.Speed = gameActor.LocalTimeScale;

                rootMotionVel = _motionWarp.EvaluateVelocity(
                    rootMotionVel,
                    motor.TransientPosition,
                    _combat != null && _combat.IsMotionWarping,
                    _combat != null ? _combat.WarpRemainingTime : 0f,
                    _combat != null ? _combat.WarpDuration : 0f,
                    _combat != null ? _combat.WarpMinDistance : 0.3f,
                    _combat != null ? _combat.WarpMaxDistance : 7f,
                    _combat != null ? _combat.WarpMaxSpeed : 22f,
                    deltaTime,
                    _combat != null ? _combat.EndMotionWarpAction : null);

                rootMotionVel = _motionWarp.ClampApproachVelocity(
                    rootMotionVel,
                    motor.TransientPosition,
                    deltaTime);
            }

            // 직전 프레임의 루트 속도를 다시 더하면 접근 모션의 이동량이 프레임마다 누적된다.
            currentVelocity = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                rootMotionVel, currentVelocity, motor.CharacterUp);
            if (_motionWarp != null
                && _motionWarp.ShouldOverrideVerticalVelocity)
            {
                currentVelocity = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                    currentVelocity,
                    rootMotionVel,
                    motor.CharacterUp);
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = (currentRotation * gameActor.Animator.RootMotionStepDeltaRotation).normalized;
            if (_motionWarp != null && _motionWarp.TryEvaluateRotation(currentRotation,
                    motor.TransientPosition, _motionWarp.IsMotionWarping, _motionWarp.WarpRemainingTime,
                    _motionWarp.WarpDuration, _combat.WarpMinDistance, _combat.WarpMaxDistance,
                    _combat.WarpMaxSpeed, out Quaternion warpedRotation))
                currentRotation = warpedRotation;
        }
    }
}
