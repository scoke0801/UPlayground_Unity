using UnityEngine;
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
        
        public override string StateName => "DashAttack";
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

            gameActor.MoveAnimType = BaseMoveAnimType.Run;

            _motionWarp = controller.MotionWarp;
            _combat = playerActor.GetCombat();
            _attackData = _combat?.ExecuteDashAttack();
            _homingTarget = FindHomingTarget();
            SnapToTarget(_homingTarget);
            _motionWarp?.SetTarget(_homingTarget, useSnapshot: false);

            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            var state = _attackData?.motionAsset != null
                ? gameActor.Animator.PlayMotion(_attackData.motionAsset, 0.1f)
                : null;
            if (state != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            _motionWarp?.ClearTarget();
            _combat?.ClearHitTargets();
            gameActor.Animator.Speed = gameActor.LocalTimeScale;
            _homingTarget = null;
            
            base.OnExit(toState);
        }

        private Transform FindHomingTarget()
        {
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
                return lockOnTarget;

            if (_combat == null || _attackData == null)
                return null;

            return _combat.FindFreeAttackFacingTarget();
        }

        private void SnapToTarget(Transform target)
        {
            if (target == null) return;

            Vector3 dir = target.position - gameActor.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                motor.SetRotation(Quaternion.LookRotation(dir.normalized));
        }

        private void OnAttackAnimationEnd()
        {
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
            }
        }
        
        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
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
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                currentVelocity.y = 0f;
                
                // 부드럽게 목표 속도로 이동
                currentVelocity = Vector3.Lerp(
                    currentVelocity, 
                    Vector3.zero, 
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
            

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;
            if (_motionWarp != null && _homingTarget != null)
            {
                float playbackScale = _combat != null && _combat.IsMotionWarping
                    ? _motionWarp.WarpPlayRateScale
                    : 1f;
                gameActor.Animator.Speed = playbackScale * gameActor.LocalTimeScale;

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

            currentVelocity += rootMotionVel;
        }
    }
}
