using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class EnemyAirborneState : EnemyActorState
    {
        public override string StateName => "Airborne";
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;
        private bool _landStarted = false;
        private bool _hasLeftGround;
        private float _dragSpeed = 0.1f;
        private float _landElapsed;
        private float _landTimeout;

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
            gameActor.Tags?.AddTag(GameplayTags.State_Airborne);

            _hasLeftGround = false;
            _dragSpeed = controller.Drag;

            // 진입 시 수직 속도가 양수면 상승(점프), 음수면 낙하 애니메이션
            float verticalSpeed = Vector3.Dot(controller.PredictedVelocity, motor.CharacterUp);
            if (verticalSpeed > 0f)
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Jump, 0.05f);
            else
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fall, 0.2f);
        }

        public override void OnExit(GameActorState state)
        {
            gameActor.Tags?.RemoveTag(GameplayTags.State_Airborne);
            base.OnExit(state);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_landStarted)
            {
                _landElapsed += deltaTime;
                if (_landElapsed >= _landTimeout)
                {
                    Debug.LogWarning(
                        $"[{gameActor.name}] Land 모션 종료 신호가 없어 안전 복귀합니다.",
                        gameActor);
                    ChangeToNextState();
                }
                return;
            }

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
            if (controller.CurrentState != this)
                return;

            controller.TransitionToState(new EnemyIdleState(controller));
        }

        private void OnLanded()
        {
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
                _landElapsed = 0f;
                float duration = gameActor.Animator.CurrentMotionSet?.TotalDuration ?? 0f;
                _landTimeout = Mathf.Max(0.4f, duration * 1.5f + 0.1f);
        
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
