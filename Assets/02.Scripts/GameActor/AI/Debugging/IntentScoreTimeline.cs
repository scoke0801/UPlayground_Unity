using System;
using System.Collections.Generic;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.CombatDecision;
using UnityEngine;

namespace UPlayGround.AI.Debugging
{
    public sealed class IntentScoreTimeline : MonoBehaviour
    {
        [SerializeField] private int _capacity = 600;
        [SerializeField] private bool _recordInPlayerBuild;

        private IntentScoreSnapshot[] _snapshots;
        private int _start;
        private int _count;

        public int Count => _count;
        public int Version { get; private set; }

        public IReadOnlyList<IntentScoreSnapshot> Snapshots => BuildSnapshotList();

        private void Awake()
        {
            EnsureBuffer();
        }

        public void Record(in CombatIntentEvaluation evaluation, float time, Blackboard blackboard = null)
        {
#if !UNITY_EDITOR
            if (!_recordInPlayerBuild)
                return;
#endif
            EnsureBuffer();

            var lastIntent = CombatIntent.Recover;
            if (blackboard != null
                && blackboard.TryGetString(EnemyBlackboardKeys.DecisionLastIntent, out var lastIntentText)
                && Enum.TryParse(lastIntentText, out CombatIntent parsedLastIntent))
            {
                lastIntent = parsedLastIntent;
            }

            var consecutiveCount = 0;
            if (blackboard != null)
                blackboard.TryGetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, out consecutiveCount);

            var snapshot = new IntentScoreSnapshot(
                time,
                evaluation.SelectedIntent,
                lastIntent,
                consecutiveCount,
                evaluation.AttackScore,
                evaluation.PunishScore,
                evaluation.CounterScore,
                evaluation.PressureScore,
                evaluation.ChaseScore,
                evaluation.RetreatScore,
                evaluation.KeepDistanceScore,
                evaluation.DefendScore,
                evaluation.RecoverScore,
                evaluation.RhythmPhase,
                evaluation.Reason);

            var writeIndex = (_start + _count) % _snapshots.Length;
            if (_count == _snapshots.Length)
            {
                writeIndex = _start;
                _start = (_start + 1) % _snapshots.Length;
            }
            else
            {
                _count++;
            }

            _snapshots[writeIndex] = snapshot;
            Version++;
        }

        public bool TryCopySnapshots(List<IntentScoreSnapshot> destination)
        {
            if (destination == null)
                return false;

            destination.Clear();
            EnsureBuffer();
            for (var i = 0; i < _count; i++)
                destination.Add(_snapshots[(_start + i) % _snapshots.Length]);
            return _count > 0;
        }

        private void EnsureBuffer()
        {
            var capacity = Mathf.Max(16, _capacity);
            if (_snapshots != null && _snapshots.Length == capacity)
                return;

            _snapshots = new IntentScoreSnapshot[capacity];
            _start = 0;
            _count = 0;
            Version++;
        }

        private IReadOnlyList<IntentScoreSnapshot> BuildSnapshotList()
        {
            var list = new List<IntentScoreSnapshot>(_count);
            TryCopySnapshots(list);
            return list;
        }
    }
}
