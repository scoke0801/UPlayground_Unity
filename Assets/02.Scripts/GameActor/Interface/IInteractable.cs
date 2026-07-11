using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Actor;

namespace UPlayGround
{
    public interface IInteractable
    {
         void Interact(GameActor interactor); // 상호작용 실행 로직
         void StopInteract();

         void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
             where TData : IEventData;  
         
         // (선택) 현재 상호작용이 가능한 상태인지 체크
         bool CanInteract();
         bool IsInteracting();

         GameActor GetActor();

         InteractableActorSO GetData();
    }
}