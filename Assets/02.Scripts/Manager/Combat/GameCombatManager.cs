using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
{
    /// <summary>
    /// 전투 관련 핸들러를 보유·중계하는 매니저.
    /// 각 핸들러(HitStop, VitalOrb)는 독립 모듈이며, MonoBehaviour 기능
    /// (코루틴, transform)이 필요할 경우 이 매니저의 인스턴스를 빌려 쓴다.
    /// </summary>
    public class GameCombatManager : BaseManager<GameCombatManager>, IManager
    {
        private GameHitStopHandler _gameHitStopHandler;
        private GameVitalOrbHandler _gameVitalOrbHandler;

        private readonly List<GameHandlerBase> _handlers = new List<GameHandlerBase>();

        public GameHitStopHandler GameHitStop => _gameHitStopHandler;
        public GameVitalOrbHandler GameVitalOrb => _gameVitalOrbHandler;

        public void Init()
        {
            _gameHitStopHandler = new GameHitStopHandler();
            _gameVitalOrbHandler = new GameVitalOrbHandler();

            _handlers.Add(_gameHitStopHandler);
            _handlers.Add(_gameVitalOrbHandler);

            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].Init();
        }

        public void AfterInit()
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].AfterInit();
        }

        public void Dispose()
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].Dispose();

            _handlers.Clear();
            _gameHitStopHandler = null;
            _gameVitalOrbHandler = null;
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].Update();
        }

        public void OnFixedUpdate()
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].FixedUpdate();
        }

        public void OnLateUpdate()
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].LateUpdate();
        }

        public void OnSceneChanged(string sceneType)
        {
            for (int i = 0; i < _handlers.Count; ++i)
                _handlers[i].OnSceneChanged(sceneType);
        }
    }
}
