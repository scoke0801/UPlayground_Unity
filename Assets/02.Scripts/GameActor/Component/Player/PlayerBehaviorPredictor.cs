using System.Collections.Generic;
using UPlayGround.AI.CombatDecision;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.Component
{
    [DisallowMultipleComponent]
    public class PlayerBehaviorPredictor : MonoBehaviour
    {
        [Header("기록")]
        [Tooltip("최근 보관할 행동 개수")]
        [SerializeField] private int _historyCapacity = 16;

        [Header("예측 신뢰도")]
        [Tooltip("이 회수 이상 관찰된 전이만 예측에 사용")]
        [SerializeField] private int _minTransitionsForConfidence = 4;
        [SerializeField] private float _confidenceDecayPerSecond = 0.01f;
        [SerializeField] private float _recoverIdleThreshold = 1.2f;

        private readonly Dictionary<(PlayerActionToken From, PlayerActionToken To), int> _bigramCounts = new();
        private PlayerActionRecord[] _history;
        private ActorMovementController _controller;
        private int _head;
        private int _count;
        private float _overallConfidence;
        private bool _hasPendingPrediction;
        private PlayerActionToken _pendingPrediction;
        private bool _isIdleCandidate;
        private bool _hasNotifiedRecover;
        private float _idleStartedTime;
        private int _lastNotifiedFrame = -1;
        private PlayerActionToken _lastNotifiedToken = PlayerActionToken.None;

        public float OverallConfidence => _overallConfidence;
        public PlayerActionToken LastToken => _count > 0 ? GetRecord(_count - 1).Token : PlayerActionToken.None;
        public float TimeSinceLastAction => _count > 0 ? Time.time - GetRecord(_count - 1).StartTime : float.PositiveInfinity;

        private void Awake()
        {
            _historyCapacity = Mathf.Max(2, _historyCapacity);
            _minTransitionsForConfidence = Mathf.Max(1, _minTransitionsForConfidence);
            _history = new PlayerActionRecord[_historyCapacity];
            _controller = GetComponent<ActorMovementController>();
        }

        private void OnEnable()
        {
            if (_controller == null)
                _controller = GetComponent<ActorMovementController>();
            if (_controller != null)
                _controller.OnStateChanged += OnControllerStateChanged;
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnStateChanged -= OnControllerStateChanged;
        }

        private void Update()
        {
            if (_confidenceDecayPerSecond <= 0f || _overallConfidence <= 0f)
            {
                UpdateRecoverRead();
                return;
            }

            _overallConfidence = Mathf.Clamp01(_overallConfidence - _confidenceDecayPerSecond * Time.deltaTime);
            UpdateRecoverRead();
        }

        public void NotifyAction(PlayerActionToken token)
        {
            if (token == PlayerActionToken.None)
                return;
            if (_lastNotifiedFrame == Time.frameCount && _lastNotifiedToken == token)
                return;

            ApplyPendingPrediction(token);

            var now = Time.time;
            var previous = LastToken;
            var timeSincePreviousAction = _count > 0 ? Mathf.Max(0f, now - GetRecord(_count - 1).StartTime) : 0f;

            if (previous != PlayerActionToken.None)
            {
                var key = (previous, token);
                _bigramCounts.TryGetValue(key, out var count);
                _bigramCounts[key] = count + 1;
            }

            Push(new PlayerActionRecord(token, now, timeSincePreviousAction));
            _lastNotifiedFrame = Time.frameCount;
            _lastNotifiedToken = token;
        }

        public void ResetHistory()
        {
            _bigramCounts.Clear();
            _head = 0;
            _count = 0;
            _overallConfidence = 0f;
            _hasPendingPrediction = false;
            _pendingPrediction = PlayerActionToken.None;
            _isIdleCandidate = false;
            _hasNotifiedRecover = false;
            _idleStartedTime = 0f;
            _lastNotifiedFrame = -1;
            _lastNotifiedToken = PlayerActionToken.None;
        }

        public PlayerActionToken PredictNext(out float confidence)
        {
            return PredictNextAfter(LastToken, out confidence);
        }

        public PlayerActionToken PredictNextAfter(PlayerActionToken token, out float confidence)
        {
            var predicted = CalculatePrediction(token, out confidence);
            if (confidence > 0f)
            {
                _pendingPrediction = predicted;
                _hasPendingPrediction = true;
            }

            return predicted;
        }

        public float ProbabilityOf(PlayerActionToken from, PlayerActionToken to)
        {
            if (from == PlayerActionToken.None || to == PlayerActionToken.None)
                return 0f;

            var total = CountTransitionsFrom(from);
            if (total <= 0)
                return 0f;

            return _bigramCounts.TryGetValue((from, to), out var count) ? (float)count / total : 0f;
        }

        private void OnControllerStateChanged(GameActorState previous, GameActorState current)
        {
            if (current?.StateName == "Idle")
            {
                _isIdleCandidate = true;
                _hasNotifiedRecover = false;
                _idleStartedTime = Time.time;
            }
            else
            {
                _isIdleCandidate = false;
                _hasNotifiedRecover = false;
            }

            NotifyAction(PlayerActionTokenMapper.FromStateName(current?.StateName));
        }

        private void UpdateRecoverRead()
        {
            if (!_isIdleCandidate || _hasNotifiedRecover)
                return;

            if (Time.time - _idleStartedTime < _recoverIdleThreshold)
                return;

            NotifyAction(PlayerActionToken.Recover);
            _hasNotifiedRecover = true;
        }

        private PlayerActionToken CalculatePrediction(PlayerActionToken from, out float confidence)
        {
            confidence = 0f;
            if (from == PlayerActionToken.None)
                return PlayerActionToken.None;

            var total = CountTransitionsFrom(from);
            if (total < _minTransitionsForConfidence)
                return PlayerActionToken.None;

            var best = PlayerActionToken.None;
            var bestCount = 0;
            foreach (var kv in _bigramCounts)
            {
                if (kv.Key.From != from || kv.Value <= bestCount)
                    continue;

                best = kv.Key.To;
                bestCount = kv.Value;
            }

            confidence = bestCount > 0 ? (float)bestCount / total : 0f;
            return best;
        }

        private int CountTransitionsFrom(PlayerActionToken from)
        {
            var total = 0;
            foreach (var kv in _bigramCounts)
            {
                if (kv.Key.From == from)
                    total += kv.Value;
            }

            return total;
        }

        private void ApplyPendingPrediction(PlayerActionToken actual)
        {
            if (!_hasPendingPrediction)
                return;

            _overallConfidence = Mathf.Clamp01(_overallConfidence + (_pendingPrediction == actual ? 0.05f : -0.03f));
            _hasPendingPrediction = false;
            _pendingPrediction = PlayerActionToken.None;
        }

        private void Push(PlayerActionRecord record)
        {
            if (_history.Length != _historyCapacity)
            {
                _historyCapacity = Mathf.Max(2, _historyCapacity);
                _history = new PlayerActionRecord[_historyCapacity];
                _head = 0;
                _count = 0;
            }

            var index = (_head + _count) % _history.Length;
            if (_count == _history.Length)
            {
                _history[_head] = record;
                _head = (_head + 1) % _history.Length;
                return;
            }

            _history[index] = record;
            _count++;
        }

        private PlayerActionRecord GetRecord(int offset)
        {
            return _history[(_head + offset) % _history.Length];
        }
    }

    public readonly struct PlayerActionRecord
    {
        public PlayerActionRecord(PlayerActionToken token, float startTime, float timeSincePreviousAction)
        {
            Token = token;
            StartTime = startTime;
            TimeSincePreviousAction = timeSincePreviousAction;
        }

        public readonly PlayerActionToken Token;
        public readonly float StartTime;
        public readonly float TimeSincePreviousAction;
    }
}
