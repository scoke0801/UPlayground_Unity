using UnityEngine;

namespace UPlayGround
{
    public interface IInteractable
    {
         void Interact(GameActor interactor); // 상호작용 실행 로직
         
         // (선택) 현재 상호작용이 가능한 상태인지 체크
         bool CanInteract();

         GameObject GetActor();
    }
}