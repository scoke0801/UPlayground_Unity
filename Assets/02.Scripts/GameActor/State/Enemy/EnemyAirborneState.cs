using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class EnemyAirborneState : GameActorState
    {
        public override string StateName => "Airborne";
        public override bool AdjustGravity => false;
        private bool _landStarted = false;
        private bool _hasLeftGround;
        private float _dragSpeed = 0.1f;

        public EnemyAirborneState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTagId.State_Airborne);

            _hasLeftGround = false;
            _dragSpeed = controller.Drag;

            // 진입 시 수직 속도가 양수면 상승(점프), 음수면 낙하 애니메이션
            float verticalSpeed = Vector3.Dot(motor.Velocity, motor.CharacterUp);
            if (verticalSpeed > 0f)
                gameActor.Animator.PlayMotion(AnimKey.Jump, 0.05f);
            else
                gameActor.Animator.PlayMotion(AnimKey.Fall, 0.2f);
        }

        public override void OnExit(GameActorState state)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Airborne);
            base.OnExit(state);
        }

        public override void UpdateState(float deltaTime)
        {
            // _hasLeftGround 가드: KCC 1프레임 grounding 지연으로 인한 조기 종료 방지
            if (_hasLeftGround && motor.GroundingStatus.IsStableOnGround && _landStarted == false)
            {
                ChangeToNextState();
            }
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                // 가변 중력: 상승/하강 구분
                float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                float gravityMultiplier = verticalSpeed < 0f
                    ? controller.FallGravityMultiplier
                    : controller.RiseGravityMultiplier;

                currentVelocity += gravityMultiplier * deltaTime * controller.Gravity;
            }

            // Drag
            currentVelocity *= (1f / (1f + (_dragSpeed * deltaTime)));
        }


        public override void PostGroundingUpdate(float deltaTime)
        {
            // 실제로 지면을 떠난 시점 기록
            if (!_hasLeftGround && !motor.GroundingStatus.IsStableOnGround)
            {
                _hasLeftGround = true;
            }

            // 착지 감지
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }
        private void ChangeToNextState()
        {
            controller.TransitionToState(new EnemyIdleState(controller));
        }

        private void OnLanded()
        {
            var state = gameActor.Animator.PlayMotion(AnimKey.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
        
                state.OwnedEvents.OnEnd += ChangeToNextState;
            }
            else
            {
                // 애니메이션이 없거나 찾을 수 없는 경우 즉시 다음 상태로
                ChangeToNextState();
            }
        }
    }
}