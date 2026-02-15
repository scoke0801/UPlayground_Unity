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
        private GameHitStopHandler _hitStopHandler;
        
        public GameInteractionHandler InteractionHandler => _interactionHandler;
        public GameHitStopHandler HitStopHandler => _hitStopHandler;

        private List<GameHandlerBase> _handlerList;
        public void Init()
        {
            _player = GameObject.FindWithTag("Player")?.GetComponent<PlayerActor>();

            _interactionHandler = new GameInteractionHandler();
            _hitStopHandler = new GameHitStopHandler();
            
            _handlerList = new List<GameHandlerBase>();
            _handlerList.Add(_interactionHandler);
            _handlerList.Add(_hitStopHandler);
            
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