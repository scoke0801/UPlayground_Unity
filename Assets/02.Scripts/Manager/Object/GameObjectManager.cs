using UnityEngine;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private GameObject _player;

        public GameObject Player => _player;

        public delegate void Interaction();

        public event Interaction OnInteractionOn;
        public event Interaction OnInteractionOut;
        public void OnStartInteraction() => OnInteractionOn?.Invoke();
        public void OnEndInteraction() => OnInteractionOut?.Invoke();

        public void Init()
        {
            _player = GameObject.FindWithTag("Player");

            LoadFXPrefabDatabase();
        }

        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
    }
}