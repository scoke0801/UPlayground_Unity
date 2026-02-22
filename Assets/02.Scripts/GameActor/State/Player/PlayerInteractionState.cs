using System.Collections;
using Animancer;
using Interaction.Enum;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerInteractionState : PlayerActorState
    {
        private enum AnimPlayState
        {
            None = 0,
            Start,
            Idle,   
            End,
        }
        
        private InteractableActorSO _cachedData = null;
        private AnimPlayState _animPlayState = AnimPlayState.None;
        
        public override string StateName { get; }

        public PlayerInteractionState(ActorMovementController controller) : base(controller)
        {
            
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _cachedData = null;
            _animPlayState = AnimPlayState.None;

            GameInteractionHandler handler = GameObjectManager.Instance.InteractionHandler;
            if (handler == null)
            {
                ForceChangeToNextState();
                return;
            }

            _cachedData = handler.CurrentClosestInteractable.GetData();
            if (_cachedData == null)
            {
                ForceChangeToNextState();
                return;
            }

            // 플레이어에 대한 처리
            PlayAnimation();

            handler.StartInteraction();

            if (EventManager.Instance != null)
            {
                EventManager.Instance.Subscribe<PlayerEvent, EmptyEventData>(
                    PlayerEvent.InteractionTargetDestroy,
                    OnInteractionTargetDestroy);
            }
        }

        public override void OnExit(GameActorState toState)
        {
            GameObjectManager.Instance?.InteractionHandler?.StopInteraction();

            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<PlayerEvent, EmptyEventData>(
                    PlayerEvent.InteractionTargetDestroy,
                    OnInteractionTargetDestroy);
            }

            base.OnExit(toState);
        }
        public override void UpdateState(float deltaTime)
        {
            if (playerController.HasInteractInput())
            {
                ForceChangeToNextState();
                return;
            }
        }

        private void PlayAnimation()
        {
            switch (_cachedData.interactionObjectType)
            {
                case InteractionObjectType.FISHING_ZONE:
                    PlayFishingAnimation();
                    return;
                case InteractionObjectType.STONE:
                    PlayMiningAnimation();
                    return;
                case InteractionObjectType.GATERING_ZONE:
                    PlayDefaultGatheringAnimation();
                    return;
                case InteractionObjectType.TREE:
                    PlayWoodCuttingAnimation();
                    return;
            }
        }

        private void PlayFishingAnimation()
        {
            AnimancerState state = null; 
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    state = gameActor.Animator.PlayMotion(AnimKey.Fishing_Throw);
                    _animPlayState = AnimPlayState.Start;
                    break;
                case AnimPlayState.Start:
                    state = gameActor.Animator.PlayMotion(AnimKey.Fishing_Idle);
                    _animPlayState = AnimPlayState.Idle;
                    
                    GameObjectManager.Instance.InteractionHandler?.SetWaitEvent(OnFishCatch);
                    return;
                default: break;    
            }

            if (state == null)
                return;
            state.OwnedEvents.OnEnd = PlayAnimation;
        }

        private void OnFishCatch()
        {
            var state = gameActor.Animator.PlayMotion(AnimKey.Fishing_Catch);
            if (state != null)
            {
                state.OwnedEvents.OnEnd = () =>
                {
                    _animPlayState = AnimPlayState.None;
                    PlayFishingAnimation();
                };
            }
        }

        private void PlayMiningAnimation()
        {
            AnimancerState state = null; 
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    state = gameActor.Animator.PlayMotion(AnimKey.Mining_Ground);
                    _animPlayState = AnimPlayState.Start;
                    break;
                default: break;    
            }

            if (state == null)
                return;
            
            state.OwnedEvents.OnEnd = PlayAnimation;
        }

        private void PlayDefaultGatheringAnimation()
        {
            AnimancerState state = null; 
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    state = gameActor.Animator.PlayMotion(AnimKey.HandGathering);
                    _animPlayState = AnimPlayState.Start;
                    break;
                default: break;    
            }

            if (state == null)
                return;
            
            state.OwnedEvents.OnEnd = PlayAnimation;
        }

        private void PlayWoodCuttingAnimation()
        {
            AnimancerState state = null; 
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    state = gameActor.Animator.PlayMotion(AnimKey.Woodcutting);
                    _animPlayState = AnimPlayState.Idle;
                    break;
                default: break;    
            }
        }
        
        private void OnInteractionTargetDestroy(EmptyEventData obj)
        {
            ForceChangeToNextState();
        }
        
        private void ForceChangeToNextState()
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }

            // 이동 입력이 있으면 GroundMove 상태로 전환
            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(new PlayerGroundMoveState(playerController));
            }
            else
            {
                playerController.TransitionToState(new PlayerIdleState(playerController));
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
           // [TODO] 인터렉션 타겟 대상을 바라보도록 수정필요
           GameActor target = GameObjectManager.Instance.InteractionHandler.CurrentClosestInteractable.GetActor();

           Vector3 lookDirection = target.transform.position - playerActor.transform.position;
           lookDirection.y = 0f;
           
           if (lookDirection.sqrMagnitude > 0.001f && controller.OrientationSharpness > 0f)
           {
               // 부드럽게 타겟 방향으로 회전 처리
               Vector3 smoothedLookInputDirection = Vector3.Slerp(
                   motor.CharacterForward, 
                   lookDirection.normalized, // 정규화된 방향 사용
                   1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime)).normalized;
            
               currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
           }

           currentRotation = currentRotation.normalized;
           
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.z = 0;
            currentVelocity.x = 0;
            
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                // Gravity
                currentVelocity += controller.Gravity * deltaTime;
            }
        }
    }
}