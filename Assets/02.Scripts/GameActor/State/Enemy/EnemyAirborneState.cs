using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Enum;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class EnemyAirborneState : PlayerActorState
    {
        public override string StateName => "Airborne";
        
        private bool _hasLanded = false;
        private bool _landStarted = false;
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

            _dragSpeed = controller.Drag;
            
            gameActor.Animator.PlayMotion(AnimKey.Fall);
        }

        public override void OnExit(GameActorState state)
        {
            base.OnExit(state);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_hasLanded)
            {
                ChangeToNextState();
                return;
            }

            if ((motor.GroundingStatus.IsStableOnGround && _landStarted == false))
            {
                ChangeToNextState();
                return;
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
                // Gravity
                currentVelocity += controller.Gravity * deltaTime;
            }

            // Drag
            currentVelocity *= (1f / (1f + (_dragSpeed * deltaTime)));
        }


        public override void PostGroundingUpdate(float deltaTime)
        {
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
            Debug.Log("Landed on ground");
            
            var state = gameActor.Animator.PlayMotion(AnimKey.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
                
                state.OwnedEvents.OnEnd += () =>
                {
                    _hasLanded = true;
                };
            }
        }
    }
}