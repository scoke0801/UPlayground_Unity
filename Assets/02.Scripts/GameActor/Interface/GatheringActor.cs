using Mono.Cecil;
using UnityEngine;

namespace UPlayGround
{
    public class GatheringActor : GameActor, IInteractable
    {
        public void Interact(GameActor user)
        {
        }

        public bool CanInteract()
        {
            return false;
        }

        private void OnGatheringComplete()
        {
            // 채집 완료 로직 (아이템 드랍, 오브젝트 파괴 등)
            Destroy(gameObject);
        }
    }
}