using UnityEngine;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private PlayerActor _player;

        public PlayerActor Player => _player;

        private GameInteractionHandler _interactionHandler;
        public delegate void Interaction();

        public event Interaction OnInteractionOn;
        public event Interaction OnInteractionOut;
        public void OnStartInteraction() => OnInteractionOn?.Invoke();
        public void OnEndInteraction() => OnInteractionOut?.Invoke();

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
}