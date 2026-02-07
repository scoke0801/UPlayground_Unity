using Interaction.Enum;
using Mono.Cecil;
using UnityEngine;

namespace UPlayGround
{
    public class GatheringActor : GameActor, IInteractable
    {
        [SerializeField] private InteractableActorSO _interactableData;
        
        private int _currentHits = 0;
        private bool _isGathering = false;
        
        public void Interact(GameActor user)
        {
            if (_isGathering) return;
            
            // ...
        }

        public bool CanInteract()
        {
            return !_isGathering;
        }

        public GameObject GetActor()
        {
            return this.gameObject;
        }

        public InteractableActorSO GetData()
        {
            return _interactableData;
        }

        private void OnGatheringComplete()
        {
            // 채집 완료 로직 (아이템 드랍, 오브젝트 파괴 등)
            Destroy(gameObject);
        }
    }
}