using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Config;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Simulation
{
    [DisallowMultipleComponent]
    public sealed class ActorSimulationParticipant : MonoBehaviour
    {
        private sealed class ActiveLease : IDisposable
        {
            private ActorSimulationParticipant _participant;
            private readonly int _id;

            public ActiveLease(ActorSimulationParticipant participant, int id)
            {
                _participant = participant;
                _id = id;
            }

            public void Dispose()
            {
                ActorSimulationParticipant participant = _participant;
                _participant = null;
                participant?.ReleaseActiveLease(_id);
            }
        }

        private readonly Dictionary<int, string> _activeLeases = new();
        private readonly List<MonoBehaviour> _resumeBehaviours = new();
        private GameActor _actor;
        private ActorMovementController _movement;
        private ActorAnimator _animator;
        private EnemyDetection _detection;
        private int _nextLeaseId;

        public static event Action<GameActor, ActorSimulationState> AnyStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AnyStateChanged = null;

        public GameActor Actor => _actor;
        public ActorSimulationState State { get; private set; } = ActorSimulationState.Active;
        public bool IsSuspended => State == ActorSimulationState.Suspended;
        public bool HasActiveLease => _activeLeases.Count > 0;
        public float LastTransitionTime { get; private set; }
        public float LastActivatedTime { get; private set; }
        public float LastDistanceSquared { get; set; }
        public ActorSimulationTransitionReason LastReason { get; set; }
        public float NextUnsafeRetryTime { get; set; }

        private void Awake()
        {
            ResolveReferences();
            _movement?.SetSimulationParticipant(this);
            LastTransitionTime = Time.unscaledTime;
            LastActivatedTime = LastTransitionTime;
        }

        public void Initialize(GameActor actor)
        {
            _actor = actor != null ? actor : GetComponent<GameActor>();
            ResolveReferences();
            _movement?.SetSimulationParticipant(this);
        }

        public bool CanSuspendSimulation(ActorSimulationSettingsSO settings)
        {
            if (_actor == null || !_actor.isActiveAndEnabled || HasActiveLease)
                return false;
            if (_actor.Abilities?.HasActiveAbility == true)
                return false;
            if (_movement == null || _movement.Motor == null || !_movement.Motor.isActiveAndEnabled)
                return false;

            KinematicCharacterMotor motor = _movement.Motor;
            if (!motor.GroundingStatus.IsStableOnGround || motor.AttachedRigidbody != null)
                return false;
            if (_movement.HasImpulse || _movement.MotionWarp?.IsMotionWarping == true)
                return false;
            if (_movement.PredictedVelocity.sqrMagnitude > settings.MaximumSuspendSpeedSquared)
                return false;

            ActorStateId stateId = _movement.CurrentState?.StateId ?? ActorStateId.None;
            if (_actor is NpcActor npc)
                return !npc.IsInteracting() && stateId is ActorStateId.Idle or ActorStateId.Wander;

            if (_actor is MonsterActor)
            {
                if (_detection != null && _detection.HasTarget)
                    return false;
                return stateId is ActorStateId.Idle or ActorStateId.Patrol;
            }

            return false;
        }

        public void SetSimulationState(ActorSimulationState state)
        {
            if (State == state)
                return;
            if (state == ActorSimulationState.Suspended && HasActiveLease)
                return;

            ResolveReferences();
            State = state;
            if (state == ActorSimulationState.Suspended)
            {
                if (_movement?.Motor != null)
                    _movement.Motor.BaseVelocity = Vector3.zero;
                _movement?.ClearImpulse();
                _animator?.SetSimulationPaused(true);
            }
            else
            {
                if (_movement?.Motor != null)
                {
                    _movement.Motor.SetPositionAndRotation(transform.position, transform.rotation);
                    _movement.Motor.BaseVelocity = Vector3.zero;
                }
                _animator?.SetSimulationPaused(false);
                NotifyResumedHandlers();
                LastActivatedTime = Time.unscaledTime;
            }

            LastTransitionTime = Time.unscaledTime;
            AnyStateChanged?.Invoke(_actor, state);
        }

        public IDisposable AcquireActiveLease(object owner, string reason)
        {
            int id = ++_nextLeaseId;
            string ownerName = owner != null ? owner.GetType().Name : "Unknown";
            _activeLeases.Add(id, $"{ownerName}: {reason}");
            SetSimulationState(ActorSimulationState.Active);
            return new ActiveLease(this, id);
        }

        private void ReleaseActiveLease(int id) => _activeLeases.Remove(id);

        private void ResolveReferences()
        {
            _actor ??= GetComponent<GameActor>();
            _movement ??= GetComponent<ActorMovementController>();
            _animator ??= _actor != null ? _actor.Animator : GetComponentInChildren<ActorAnimator>();
            _detection ??= GetComponent<EnemyDetection>();
        }

        private void NotifyResumedHandlers()
        {
            // 런타임에 추가되는 BT 러너도 놓치지 않되, 재개마다 새 배열을 만들지 않는다.
            GetComponents(_resumeBehaviours);
            for (int i = 0; i < _resumeBehaviours.Count; i++)
            {
                if (_resumeBehaviours[i] is IActorSimulationResumeHandler handler)
                    handler.OnActorSimulationResumed();
            }
        }

        private void OnDestroy()
        {
            _activeLeases.Clear();
            _resumeBehaviours.Clear();
            if (State == ActorSimulationState.Suspended)
                _animator?.SetSimulationPaused(false);
        }
    }
}
