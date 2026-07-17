using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Manager.Combat
{
    /// <summary>
    /// 전투 관련 핸들러를 보유·중계하는 매니저.
    /// 각 핸들러(HitStop, VitalOrb)는 독립 모듈이며, MonoBehaviour 기능
    /// (코루틴, transform)이 필요할 경우 이 매니저의 인스턴스를 빌려 쓴다.
    /// </summary>
    public class GameCombatManager : BaseManager<GameCombatManager>, IManager,
        IUpdatableManager, IFixedUpdatableManager, ILateUpdatableManager,
        IHitStopService, IVitalOrbService, IActorCombatService
    {
        private GameHitStopHandler _gameHitStopHandler;
        private GameVitalOrbHandler _gameVitalOrbHandler;
        private DefenseSuccessFeedbackHandler _defenseSuccessFeedbackHandler;
        private LevelUpFeedbackHandler _levelUpFeedbackHandler;

        private readonly List<GameHandlerBase> _handlers = new List<GameHandlerBase>();

        public GameHitStopHandler GameHitStop => _gameHitStopHandler;
        public GameVitalOrbHandler GameVitalOrb => _gameVitalOrbHandler;
        public DefenseSuccessFeedbackHandler DefenseSuccessFeedback => _defenseSuccessFeedbackHandler;
        public LevelUpFeedbackHandler LevelUpFeedback => _levelUpFeedbackHandler;

        bool IHitStopService.IsHitStopping => _gameHitStopHandler?.IsHitStopping == true;

        void IHitStopService.Execute(float duration, float timeScale)
        {
            _gameHitStopHandler?.Execute(duration, timeScale);
        }

        void IHitStopService.Stop()
        {
            _gameHitStopHandler?.Stop();
        }

        void IVitalOrbService.TrySpawn(VitalOrbTrigger trigger, Vector3 spawnPosition)
        {
            _gameVitalOrbHandler?.TrySpawn(trigger, spawnPosition);
        }

        void IVitalOrbService.TrySpawnByPolicy(
            VitalOrbTrigger trigger,
            Vector3 spawnPosition,
            float probability,
            int count,
            float healScale)
        {
            _gameVitalOrbHandler?.TrySpawnByPolicy(
                trigger,
                spawnPosition,
                probability,
                count,
                healScale);
        }

        public bool IsActorHitStopping(GameActor actor) =>
            _gameHitStopHandler?.IsActorHitStopping(actor) == true;

        public void ExecuteActorHitStop(GameActor actor, float duration, float timeScale = 0.1f) =>
            _gameHitStopHandler?.ExecuteActorOnly(actor, duration, timeScale);

        public void ExecuteLocalImpact(
            GameActor attacker,
            GameActor victim,
            float duration,
            float localTimeScale = 0.1f,
            bool includeAttacker = true,
            float victimTimeScale = -1f) =>
            _gameHitStopHandler?.ExecuteLocalImpact(
                attacker,
                victim,
                duration,
                localTimeScale,
                includeAttacker,
                victimTimeScale);

        public void ExecuteHitStop(float duration, float timeScale = 0.1f) =>
            _gameHitStopHandler?.Execute(duration, timeScale);

        public void ExecutePlayerDeathHitStop() =>
            _gameHitStopHandler?.Execute(GameHitStopHandler.HitStopIntensity.PlayerDie);

        public void ResetActorHitStop() => _gameHitStopHandler?.ResetActorTimeScale();

        public void TrySpawnVitalOrb(VitalOrbTrigger trigger, Vector3 position) =>
            _gameVitalOrbHandler?.TrySpawn(trigger, position);

        public void TrySpawnVitalOrbByPolicy(
            VitalOrbTrigger trigger,
            Vector3 position,
            float probability,
            int count,
            float healScale) =>
            _gameVitalOrbHandler?.TrySpawnByPolicy(trigger, position, probability, count, healScale);

        public float GetCounterWindowDuration(DefenseSuccessType type) =>
            _defenseSuccessFeedbackHandler?.GetCounterWindowDuration(type) ?? -1f;

        public void PlayDefenseSuccess(
            DefenseSuccessType type,
            PlayerActor player,
            GameActor attacker,
            AttackData incomingAttack,
            Vector3 position,
            string fxKey = null) =>
            _defenseSuccessFeedbackHandler?.Play(
                type,
                new DefenseSuccessFeedbackContext(player, attacker, incomingAttack, position, fxKey));

        public void PlayDashEvade(
            PlayerActor player,
            GameActor attacker,
            AttackData incomingAttack,
            Vector3 position) =>
            _defenseSuccessFeedbackHandler?.PlayDashEvade(
                new DefenseSuccessFeedbackContext(player, attacker, incomingAttack, position));

        public void StopDefenseFeedbackForCounterAttack(GameActor counterActor) =>
            _defenseSuccessFeedbackHandler?.StopForCounterAttack(counterActor);

        public void Init()
        {
            _gameHitStopHandler = new GameHitStopHandler();
            _gameVitalOrbHandler = new GameVitalOrbHandler();
            _defenseSuccessFeedbackHandler = new DefenseSuccessFeedbackHandler();
            _levelUpFeedbackHandler = new LevelUpFeedbackHandler();

            _handlers.Add(_gameHitStopHandler);
            _handlers.Add(_gameVitalOrbHandler);
            _handlers.Add(_defenseSuccessFeedbackHandler);
            _handlers.Add(_levelUpFeedbackHandler);

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
            _defenseSuccessFeedbackHandler = null;
            _levelUpFeedbackHandler = null;
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
