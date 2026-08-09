using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Config;
using UPlayGround.Simulation;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 현재 조작 플레이어를 기준으로 설정에 포함된 몬스터와 NPC의 시뮬레이션 상태를 일괄 결정한다.
    /// </summary>
    public sealed class ActorSimulationManager : BaseManager<ActorSimulationManager>,
        IManager, IUpdatableManager, IActorSimulationService
    {
        private const string SettingsResourcePath = "Config/ActorSimulationSettings";

        private readonly List<ActorSimulationParticipant> _participants = new();
        private readonly Dictionary<GameActor, ActorSimulationParticipant> _lookup = new();
        private ActorSimulationSettingsSO _settings;
        private GameObjectManager _objects;
        private PlayerActor _lastPlayer;
        private Vector3 _lastPlayerPosition;
        private float _bucketTimer;
        private int _nextBucket;
        private bool _forceRefresh;
        private bool _isReady;

        public int RegisteredCount => _participants.Count;
        public int ActiveCount { get; private set; }
        public int SuspendedCount { get; private set; }

        public void Init()
        {
            _settings = Resources.Load<ActorSimulationSettingsSO>(SettingsResourcePath);
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<ActorSimulationSettingsSO>();
                _settings.hideFlags = HideFlags.HideAndDontSave;
                Debug.LogWarning(
                    $"[ActorSimulationManager] Resources/{SettingsResourcePath} 설정이 없어 기본값을 사용합니다.");
            }

            _objects = GameObjectManager.Instance;
            _forceRefresh = true;
        }

        public void AfterInit()
        {
            if (_objects == null)
                return;

            _objects.OnActorRegistered += HandleActorRegistered;
            _objects.OnActorUnregistered += HandleActorUnregistered;
            IReadOnlyList<GameActor> actors = _objects.AllActors;
            for (int i = 0; i < actors.Count; i++)
                HandleActorRegistered(actors[i]);
            EvaluateAll();
            _lastPlayer = _objects.Player;
            if (_lastPlayer != null)
                _lastPlayerPosition = _lastPlayer.transform.position;
            _forceRefresh = false;
            _isReady = true;
        }

        public void Dispose()
        {
            if (_objects != null)
            {
                _objects.OnActorRegistered -= HandleActorRegistered;
                _objects.OnActorUnregistered -= HandleActorUnregistered;
            }

            SetAllActive(ActorSimulationTransitionReason.PlayerUnavailable);
            _participants.Clear();
            _lookup.Clear();
            _objects = null;
            _lastPlayer = null;
            _isReady = false;
            if (_settings != null && (_settings.hideFlags & HideFlags.HideAndDontSave) != 0)
                Destroy(_settings);
            _settings = null;
        }

        public void OnUpdate()
        {
            if (_objects == null || _settings == null)
                return;

            PlayerActor player = _objects.Player;
            if (player == null)
            {
                if (_lastPlayer != null || SuspendedCount > 0)
                    SetAllActive(ActorSimulationTransitionReason.PlayerUnavailable);
                _lastPlayer = null;
                return;
            }

            Vector3 playerPosition = player.transform.position;
            bool playerChanged = player != _lastPlayer;
            bool teleported = !playerChanged &&
                              (playerPosition - _lastPlayerPosition).sqrMagnitude >=
                              _settings.TeleportRefreshDistanceSquared;
            _lastPlayerPosition = playerPosition;
            if (_forceRefresh || playerChanged || teleported)
            {
                _lastPlayer = player;
                EvaluateAll();
                _forceRefresh = false;
                _bucketTimer = 0f;
                return;
            }

            float bucketInterval = _settings.evaluationInterval /
                                   Mathf.Max(1, _settings.evaluationBuckets);
            _bucketTimer += Time.unscaledDeltaTime;
            if (_bucketTimer < bucketInterval)
                return;

            _bucketTimer = 0f;
            int bucketCount = Mathf.Max(1, _settings.evaluationBuckets);
            _nextBucket %= bucketCount;
            EvaluateBucket(playerPosition, _nextBucket);
            _nextBucket = (_nextBucket + 1) % bucketCount;
            if (_nextBucket == 0)
                RefreshCounts();
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            RemoveDestroyedParticipants();
            _lastPlayer = null;
            _forceRefresh = true;
        }

        public bool IsSuspended(GameActor actor) =>
            actor != null && _lookup.TryGetValue(actor, out var participant) && participant.IsSuspended;

        public IDisposable AcquireActiveLease(GameActor actor, object owner, string reason)
        {
            if (actor == null || !_lookup.TryGetValue(actor, out var participant))
                return EmptyDisposable.Instance;
            return participant.AcquireActiveLease(owner, reason);
        }

        public void ForceRefresh() => _forceRefresh = true;

        private void HandleActorRegistered(GameActor actor)
        {
            if (!IsEligible(actor) || _lookup.ContainsKey(actor))
                return;

            ActorSimulationParticipant participant =
                actor.GetComponent<ActorSimulationParticipant>() ??
                actor.gameObject.AddComponent<ActorSimulationParticipant>();
            participant.Initialize(actor);
            participant.ApplySettings(_settings);
            _lookup.Add(actor, participant);
            _participants.Add(participant);
            if (_isReady && _objects?.Player != null)
            {
                EvaluateParticipant(participant, _objects.Player.transform.position, true);
                if (participant.IsSuspended) SuspendedCount++;
                else ActiveCount++;
            }
            else
            {
                _forceRefresh = true;
            }
        }

        private void HandleActorUnregistered(GameActor actor)
        {
            if (actor == null || !_lookup.Remove(actor, out var participant))
                return;

            bool removedSuspended = participant != null && participant.IsSuspended;
            if (participant != null)
                participant.SetSimulationState(ActorSimulationState.Active);
            for (int i = _participants.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_participants[i], participant))
                {
                    _participants.RemoveAt(i);
                    break;
                }
            }
            if (participant != null)
            {
                if (removedSuspended) SuspendedCount = Mathf.Max(0, SuspendedCount - 1);
                else ActiveCount = Mathf.Max(0, ActiveCount - 1);
            }
            else
            {
                RefreshCounts();
            }
        }

        private bool IsEligible(GameActor actor)
        {
            if (actor is NpcActor)
                return true;
            if (actor is not MonsterActor monster)
                return false;
            return _settings.IncludesMonsterGrade(monster.Grade);
        }

        private void EvaluateAll()
        {
            if (_objects?.Player == null)
            {
                SetAllActive(ActorSimulationTransitionReason.PlayerUnavailable);
                return;
            }

            Vector3 playerPosition = _objects.Player.transform.position;
            for (int i = _participants.Count - 1; i >= 0; i--)
            {
                if (_participants[i] == null || _participants[i].Actor == null)
                {
                    _participants.RemoveAt(i);
                    continue;
                }
                EvaluateParticipant(_participants[i], playerPosition, true);
            }
            RebuildLookup();
            RefreshCounts();
        }

        private void EvaluateBucket(Vector3 playerPosition, int bucket)
        {
            int bucketCount = Mathf.Max(1, _settings.evaluationBuckets);
            for (int i = bucket; i < _participants.Count; i += bucketCount)
            {
                ActorSimulationParticipant participant = _participants[i];
                if (participant != null && participant.Actor != null)
                    EvaluateParticipant(participant, playerPosition, false);
            }
        }

        private void EvaluateParticipant(
            ActorSimulationParticipant participant,
            Vector3 playerPosition,
            bool forceUnsafeCheck)
        {
            if (!IsEligible(participant.Actor))
            {
                participant.LastReason = ActorSimulationTransitionReason.Unsafe;
                participant.SetSimulationState(ActorSimulationState.Active);
                return;
            }

            float now = Time.unscaledTime;
            Vector3 offset = participant.transform.position - playerPosition;
            participant.LastDistanceSquared = offset.sqrMagnitude;

            bool canSuspend;
            if (!forceUnsafeCheck && participant.State == ActorSimulationState.Active &&
                now < participant.NextUnsafeRetryTime)
            {
                canSuspend = false;
            }
            else
            {
                canSuspend = participant.CanSuspendSimulation(_settings);
                participant.NextUnsafeRetryTime = canSuspend
                    ? 0f
                    : now + _settings.unsafeRetryInterval;
            }

            ActorSimulationState desired = ActorSimulationPolicy.Evaluate(
                participant.State,
                hasPlayer: true,
                participant.HasActiveLease,
                canSuspend,
                participant.LastDistanceSquared,
                _settings.WakeDistanceSquared,
                _settings.SleepDistanceSquared,
                now,
                participant.LastActivatedTime,
                _settings.minimumActiveDuration,
                out ActorSimulationTransitionReason reason);

            participant.LastReason = reason;
            participant.SetSimulationState(desired);
        }

        private void SetAllActive(ActorSimulationTransitionReason reason)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                ActorSimulationParticipant participant = _participants[i];
                if (participant == null)
                    continue;
                participant.LastReason = reason;
                participant.SetSimulationState(ActorSimulationState.Active);
            }
            RefreshCounts();
        }

        private void RemoveDestroyedParticipants()
        {
            _participants.RemoveAll(participant => participant == null || participant.Actor == null);
            RebuildLookup();
            RefreshCounts();
        }

        private void RebuildLookup()
        {
            _lookup.Clear();
            for (int i = 0; i < _participants.Count; i++)
            {
                ActorSimulationParticipant participant = _participants[i];
                if (participant != null && participant.Actor != null)
                    _lookup[participant.Actor] = participant;
            }
        }

        private void RefreshCounts()
        {
            int active = 0;
            int suspended = 0;
            for (int i = 0; i < _participants.Count; i++)
            {
                ActorSimulationParticipant participant = _participants[i];
                if (participant == null)
                    continue;
                if (participant.IsSuspended) suspended++;
                else active++;
            }
            ActiveCount = active;
            SuspendedCount = suspended;
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
