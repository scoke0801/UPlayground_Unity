using UnityEngine;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private PlayerActor _player;

        public PlayerActor Player => _player;

        private GameInteractionHandler _interactionHandler;
        public GameInteractionHandler InteractionHandler => _interactionHandler;
        
        public void Init()
        {
            _player = GameObject.FindWithTag("Player")?.GetComponent<PlayerActor>();

            _interactionHandler = new GameInteractionHandler();
            _interactionHandler.Init();
            
            LoadFXPrefabDatabase();
        }

        public void AfterInit()
        {
            
        }

        public void Dispose()
        {
            _interactionHandler.Dispose();
        }

        public void OnUpdate()
        {
            _interactionHandler.Update();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
    }

    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        public bool CanInteract()
        {
            return _interactionHandler.CurrentClosestInteractable != null;
        }
    }
}