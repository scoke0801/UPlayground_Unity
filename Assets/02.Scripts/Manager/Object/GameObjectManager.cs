using System.Collections.Generic;
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

        private List<GameHandlerBase> _handlerList;
        public void Init()
        {
            _player = GameObject.FindWithTag("Player")?.GetComponent<PlayerActor>();

            _interactionHandler = new GameInteractionHandler();
            
            _handlerList = new List<GameHandlerBase>();
            _handlerList.Add(_interactionHandler);
            
            for (int i = 0; i < _handlerList.Count; ++i)
            {
                _handlerList[i].Init();
            }
            LoadFXPrefabDatabase();
        }

        public void AfterInit()
        {
            for (int i = 0; i < _handlerList.Count; ++i)
            {
                _handlerList[i].AfterInit();
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _handlerList.Count; ++i)
            {
                _handlerList[i].Dispose();
            }

            _handlerList.Clear();
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _handlerList.Count; ++i)
            {
                _handlerList[i].Update();
            }
            
            ProcessPendingDestroyFX();
        }

        public void OnFixedUpdate()
        {
            for (int i = 0; i < _handlerList.Count; ++i)
            {
                _handlerList[i].FixedUpdate();
            }
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