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
        
        public override string StateName => "Interaction";

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

            _cachedData = handler.CurrentClosestInteractable?.GetData();
            if (_cachedData == null)
            {
                ForceChangeToNextState();
                return;
            }

            // 플레이어에 대한 처리
            PlayAnimation();

            // 채광/채집/벌목은 대상이 소진될 때까지 같은 모션을 반복 재생해야 한다.
            // (AnimancerState.OwnedEvents.OnEnd는 타임라인 완료와 무관하게 클립 종료 시점에 발화하고,
            //  재진입 시 _animPlayState 가드에 막혀 재생이 끊기므로 MotionSet 완료 이벤트를 사용한다)
            gameActor.Animator.OnMotionSetCompleted += OnInteractionMotionCompleted;

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
            gameActor.Animator.OnMotionSetCompleted -= OnInteractionMotionCompleted;

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

            // REST_POINT: 회복은 OnEnter의 StartInteraction에서 즉시 처리됨.
            // 별도 애니메이션/대기 없이 바로 상태를 빠져나와 플레이어 고착을 방지한다.
            // [TODO] 추후 휴식 모션을 넣을 경우 PlayAnimation에 REST_POINT 케이스 추가 후 이 즉시 종료 제거.
            if (_cachedData?.interactionObjectType == InteractionObjectType.REST_POINT)
            {
                ForceChangeToNextState();
                return;
            }

            // NPC 대화가 끝나면 자동으로 상태 종료
            var handler = GameObjectManager.Instance.InteractionHandler;
            if (_cachedData?.interactionObjectType == InteractionObjectType.NPC
                && handler?.CurrentClosestInteractable?.IsInteracting() == false)
            {
                ForceChangeToNextState();
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
                case InteractionObjectType.NPC:
                    // NPC 대화 중에는 별도 애니메이션 없이 대기.
                    // 대화 종료 시 NpcActor가 _isInteracting을 false로 바꾸므로
                    // UpdateState의 CanInteract 체크로 자연스럽게 상태가 빠져나옵니다.
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
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    gameActor.Animator.PlayMotion(AnimKey.Mining_Ground);
                    _animPlayState = AnimPlayState.Start;
                    break;
                default: break;
            }
        }

        private void PlayDefaultGatheringAnimation()
        {
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    gameActor.Animator.PlayMotion(AnimKey.HandGathering);
                    _animPlayState = AnimPlayState.Start;
                    break;
                default: break;
            }
        }

        /// <summary>
        /// 채광/채집/벌목 MotionSet 타임라인 완료 시 같은 모션을 처음부터 반복 재생한다.
        /// 대상 소진(InteractionTargetDestroy)이나 상태 이탈 시 OnExit에서 구독 해제되어 반복이 멈춘다.
        /// 낚시는 Throw→Idle→Catch 체이닝(OwnedEvents.OnEnd)으로 별도 진행되므로 제외.
        /// </summary>
        private void OnInteractionMotionCompleted()
        {
            switch (_cachedData?.interactionObjectType)
            {
                case InteractionObjectType.STONE:
                case InteractionObjectType.GATERING_ZONE:
                case InteractionObjectType.TREE:
                    _animPlayState = AnimPlayState.None;
                    PlayAnimation();
                    break;
            }
        }

        private void PlayWoodCuttingAnimation()
        {
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    gameActor.Animator.PlayMotion(AnimKey.Woodcutting);
                    _animPlayState = AnimPlayState.Start;
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