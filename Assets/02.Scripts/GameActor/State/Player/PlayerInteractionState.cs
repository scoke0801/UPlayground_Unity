using System.Collections;
using Animancer;
using UPlayGround.Data.EnumType;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Data.Actor;

namespace UPlayGround.State
{
    public class PlayerInteractionState : PlayerActorState
    {
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private enum AnimPlayState
        {
            None = 0,
            Start,
            Idle,   
            End,
        }
        
        /// <summary>NPC 대화 진입 시 직전 이동 모션에서 대기 모션으로 넘어가는 페이드 시간.</summary>
        private const float TalkIdleFadeDuration = 0.25f;

        private InteractableActorSO _cachedData = null;
        private Transform _cachedTarget = null;
        private AnimPlayState _animPlayState = AnimPlayState.None;
        private bool _requiresHeldInput;
        private bool _hasPickupMotion;
        
        public override ActorStateId StateId => ActorStateId.Interaction;

        public PlayerInteractionState(ActorMovementController controller) : base(controller)
        {
            
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _cachedData = null;
            _cachedTarget = null;
            _animPlayState = AnimPlayState.None;
            _requiresHeldInput = false;
            _hasPickupMotion = false;

            IActorInteractionService handler = ActorSvc.Objects?.InteractionHandler;
            if (handler == null)
            {
                ForceChangeToNextState();
                return;
            }

            if (!playerActor.CanStartInteraction())
            {
                ForceChangeToNextState();
                return;
            }

            IInteractable interactable = handler.CurrentClosestInteractable;
            if (interactable == null)
            {
                ForceChangeToNextState();
                return;
            }

            _cachedData = interactable.GetData();
            // 상호작용을 시작한 실제 대상을 캐싱한다. UpdateRotation은 매 프레임 CurrentClosestInteractable를
            // 재조회하지 않고 이 대상만 바라본다. 대상이 소진되어 사라지면 다음 프레임 핸들러가
            // CurrentClosestInteractable를 주변의 다른 대상으로 교체하는데, 그쪽으로 회전하면 어색하기 때문이다.
            _cachedTarget = interactable.GetInteractionTransform();
            _requiresHeldInput = _cachedData != null && _cachedData.interactionCompleteDuration > 0f;

            if (Svc.Events != null)
            {
                Svc.Events.Subscribe<PlayerEvent, EmptyEventData>(
                    PlayerEvent.InteractionTargetDestroy,
                    OnInteractionTargetDestroy);
            }

            // 플레이어에 대한 처리. 데이터가 없는 즉시 상호작용은 모션 없이 Interact만 실행한다.
            if (_cachedData != null)
            {
                playerActor.GetPlayerEquipment()?.BeginInteractionEquipment(_cachedData.interactionObjectType);
                PlayAnimation();
            }

            // 채광/채집/벌목은 대상이 소진될 때까지 같은 모션을 반복 재생해야 한다.
            // (AnimancerState.OwnedEvents.OnEnd는 타임라인 완료와 무관하게 클립 종료 시점에 발화하고,
            //  재진입 시 _animPlayState 가드에 막혀 재생이 끊기므로 MotionSet 완료 이벤트를 사용한다)
            gameActor.Animator.OnMotionSetCompleted += OnInteractionMotionCompleted;

            handler.StartInteraction();

            if (_cachedData == null)
            {
                ForceChangeToNextState();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnInteractionMotionCompleted;

            ActorSvc.Objects?.InteractionHandler?.StopInteraction();
            playerActor.GetPlayerEquipment()?.EndInteractionEquipment();

            _cachedTarget = null;

            if (Svc.Events != null)
            {
                Svc.Events.Unsubscribe<PlayerEvent, EmptyEventData>(
                    PlayerEvent.InteractionTargetDestroy,
                    OnInteractionTargetDestroy);
            }

            base.OnExit(toState);
        }
        public override void UpdateState(float deltaTime)
        {
            // 드랍 아이템은 완료 후 입력이 풀려도 남은 획득 모션 재생을 기다려야 하므로 입력 체크보다 먼저 처리한다.
            if (_cachedData?.interactionObjectType == InteractionObjectType.DROP_ITEM
                && IsCurrentInteractionCompleted())
            {
                // 획득 모션이 재생 중이면 루프 구간을 풀고 남은 모션이 끝날 때까지 대기한다.
                if (_hasPickupMotion && gameActor.Animator.IsPlayingMotionSet)
                {
                    gameActor.Animator.BreakAllInfiniteLoops();
                    return;
                }

                ForceChangeToNextState();
                return;
            }

            if (_requiresHeldInput && !playerController.IsInteractHeld())
            {
                ForceChangeToNextState();
                return;
            }

            if (!_requiresHeldInput && playerController.HasInteractInput())
            {
                ForceChangeToNextState();
                return;
            }

            if (_cachedData?.interactionObjectType == InteractionObjectType.REST_POINT)
            {
                if (IsCurrentInteractionCompleted())
                {
                    ForceChangeToNextState();
                }

                return;
            }

            if (_cachedData?.interactionObjectType == InteractionObjectType.DROP_ITEM)
            {
                // 완료 처리는 위쪽(입력 체크 이전)에서 수행. 진행 중에는 대기만 한다.
                return;
            }

            // NPC 대화가 끝나면 자동으로 상태 종료
            var handler = ActorSvc.Objects.InteractionHandler;
            if (_cachedData?.interactionObjectType == InteractionObjectType.NPC
                && handler?.CurrentClosestInteractable?.IsInteracting() == false)
            {
                ForceChangeToNextState();
            }
        }

        private bool IsCurrentInteractionCompleted()
        {
            var handler = ActorSvc.Objects?.InteractionHandler;
            IInteractable interactable = handler?.CurrentClosestInteractable;
            return interactable == null || interactable.IsInteracting() == false;
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
                    PlayTalkIdleAnimation();
                    return;
                case InteractionObjectType.DROP_ITEM:
                    PlayDropItemPickupAnimation();
                    return;
            }
        }

        /// <summary>
        /// 드랍 아이템 획득 모션 재생.
        /// 듀레이션이 있는 대상은 모션 내 Loop 이벤트 구간에서 정지해 있다가 완료 시 UpdateState에서 루프를 해제하고,
        /// 즉시 획득 대상은 Loop 이벤트를 건너뛰고 모션을 끝까지 재생한다.
        /// </summary>
        private void PlayDropItemPickupAnimation()
        {
            if (_animPlayState != AnimPlayState.None)
                return;

            UPlayGround.Gameplay.Tag.GameplayTag animKey = _cachedData.interactionMotionSlot;
            if (animKey == default)
                return;

            AnimancerState state = gameActor.Animator.PlayMotion(animKey);
            _hasPickupMotion = state != null;
            _animPlayState = AnimPlayState.Start;

            if (_hasPickupMotion && !_requiresHeldInput)
            {
                gameActor.Animator.SuppressLoopEvents();
            }
        }

        /// <summary>
        /// NPC 대화 자세로 전환한다. 대화 종료는 UpdateState의 상호작용 종료 체크가 담당한다.
        /// 이 경로는 대화 홀드보다 먼저 상태를 잡고 있으므로 자세도 여기서 세운다
        /// (<see cref="PlayerActor.BeginDialogueStage"/>는 상호작용 중이면 시선 갱신만 맡는다).
        /// </summary>
        private void PlayTalkIdleAnimation()
        {
            if (_animPlayState != AnimPlayState.None)
                return;

            _animPlayState = AnimPlayState.Idle;
            PlayDialogueMotion(TalkIdleFadeDuration);
        }

        private void PlayFishingAnimation()
        {
            AnimancerState state = null; 
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fishing_Throw);
                    _animPlayState = AnimPlayState.Start;
                    break;
                case AnimPlayState.Start:
                    state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fishing_Idle);
                    _animPlayState = AnimPlayState.Idle;
                    
                    ActorSvc.Objects.InteractionHandler?.SetWaitEvent(OnFishCatch);
                    return;
                default: break;    
            }

            if (state == null)
                return;
            state.OwnedEvents.OnEnd = PlayAnimation;
        }

        private void OnFishCatch()
        {
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fishing_Catch);
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
                    gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Mining_Ground);
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
                    gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.HandGathering);
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
                case InteractionObjectType.DROP_ITEM:
                    // 획득 모션은 반복하지 않는다. 상호작용이 끝났다면 즉시 상태를 빠져나간다.
                    _hasPickupMotion = false;
                    if (IsCurrentInteractionCompleted())
                    {
                        ForceChangeToNextState();
                    }
                    break;
            }
        }

        private void PlayWoodCuttingAnimation()
        {
            switch (_animPlayState)
            {
                case AnimPlayState.None:
                    gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Woodcutting);
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
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

            // 이동 입력이 있으면 GroundMove 상태로 전환
            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(ActorStateId.GroundMove);
            }
            else
            {
                playerController.TransitionToState(ActorStateId.Idle);
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
           // 상호작용을 시작한 대상만 바라본다. CurrentClosestInteractable를 매 프레임 재조회하면
           // 대상 소진 직후 주변의 다른 대상으로 교체되어 상태 이탈 전 몇 프레임 동안 그쪽으로 회전한다.
           // 즉시 소모형 대상(드랍 아이템 등)은 상태 종료 전에 타겟이 사라질 수 있으므로 회전을 생략한다.
           SmoothLookAt(_cachedTarget, ref currentRotation, deltaTime);
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
