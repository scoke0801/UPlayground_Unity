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

         /// <summary>
         /// 상호작용 아이콘과 플레이어 회전의 기준 Transform.
         /// GameActor가 아닌 월드 오브젝트도 상호작용 대상이 될 수 있으므로 GetActor와 분리한다.
         /// </summary>
         Transform GetInteractionTransform();

         /// <summary>
         /// 액터 기반 상호작용 대상. 액터가 아닌 대상은 null을 반환할 수 있다.
         /// </summary>
         GameActor GetActor();

         InteractableActorSO GetData();
    }
}
