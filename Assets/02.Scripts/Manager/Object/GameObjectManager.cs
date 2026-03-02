using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private PlayerActor _player;
        private List<GameActor> _allActors = new List<GameActor>();

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

        public void RegisterActor(GameActor actor)
        {
            if (!_allActors.Contains(actor))
                _allActors.Add(actor);
        }

        public void UnregisterActor(GameActor actor)
        {
            _allActors.Remove(actor);
        }

        /// <summary>
        /// 플레이어를 제외한 모든 액터의 타임스케일을 설정합니다.
        /// </summary>
        /// <param name="timeScale">설정할 타임스케일</param>
        /// <param name="duration">지속 시간 (0이면 영구적)</param>
        public void SetGlobalTimeScaleExceptPlayer(float timeScale, float duration = 0f)
        {
            foreach (var actor in _allActors)
            {
                if (actor is not PlayerActor)
                {
                    actor.LocalTimeScale = timeScale;
                }
            }

            if (duration > 0f)
            {
                StartCoroutine(ResetTimeScaleCoroutine(duration));
            }
        }

        public void ResetTimeScale()
        {
            SetGlobalTimeScaleExceptPlayer(1.0f);
        }
        
        private System.Collections.IEnumerator ResetTimeScaleCoroutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SetGlobalTimeScaleExceptPlayer(1.0f);
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

        public void OnSceneChanged(string sceneType)
        {
            // 씬 전환 시 Player 레퍼런스 재수집
            _player = GameObject.FindWithTag("Player")?.GetComponent<PlayerActor>();
            
            // Handler들도 씬 의존 상태 리셋
            foreach (var handler in _handlerList)
                handler.Init();
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