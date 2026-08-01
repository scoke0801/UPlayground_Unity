using System;
using System.IO;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.Debugging
{
    public sealed class EncounterReplayRecorder : MonoBehaviour
    {
        [Tooltip("Encounter Replay 기록 활성화 여부. 빌드에서는 항상 false 권장")]
        [SerializeField] private bool _enableReplayRecording;
        [SerializeField] private float _lostTargetSaveDelay = 0.5f;

        private EnemyDetection _detection;
        private EncounterReplay _replay;
        private float _startTime;
        private float _lostTargetTime = -1f;

        public bool IsRecording => _replay != null;

        private void Awake()
        {
            _detection = GetComponent<EnemyDetection>();
            if (_detection != null)
            {
                _detection.OnTargetAcquiredExternally += BeginRecording;
                _detection.OnTargetLost += HandleTargetLost;
            }

            // 실제 기록이 시작되기 전에는 개별 MonoBehaviour.Update 디스패치를 만들지 않는다.
            enabled = false;
        }

        private void Update()
        {
            if (_lostTargetTime > 0f && Time.time - _lostTargetTime >= _lostTargetSaveDelay)
                EndAndSave("target_lost", "타겟 상실");
        }

        private void OnDestroy()
        {
            if (_detection != null)
            {
                _detection.OnTargetAcquiredExternally -= BeginRecording;
                _detection.OnTargetLost -= HandleTargetLost;
            }
        }

        public void RecordFrame(in CombatIntentEvaluation evaluation, Blackboard blackboard)
        {
            if (!_enableReplayRecording || _replay == null)
                return;

            var lastIntent = CombatIntent.Recover;
            if (blackboard != null
                && blackboard.TryGetString(EnemyBlackboardKeys.DecisionLastIntent, out var lastIntentText)
                && Enum.TryParse(lastIntentText, out CombatIntent parsedLastIntent))
            {
                lastIntent = parsedLastIntent;
            }

            var frame = new ReplayFrame
            {
                t = Time.time - _startTime,
                selectedIntent = evaluation.SelectedIntent,
                lastIntent = lastIntent,
                consecutiveIntentCount = ReadInt(blackboard, EnemyBlackboardKeys.DecisionConsecutiveIntentCount),
                scores = new[]
                {
                    evaluation.AttackScore,
                    evaluation.PunishScore,
                    evaluation.CounterScore,
                    evaluation.PressureScore,
                    evaluation.ChaseScore,
                    evaluation.RetreatScore,
                    evaluation.KeepDistanceScore,
                    evaluation.DefendScore,
                    evaluation.RecoverScore
                },
                distance = ReadFloat(blackboard, EnemyBlackboardKeys.TargetDistance, float.MaxValue),
                preferredRange = ReadFloat(blackboard, EnemyBlackboardKeys.AIPreferredRange),
                optimalRange = ReadFloat(blackboard, EnemyBlackboardKeys.OptimalCombatDistance),
                healthPercent = ReadFloat(blackboard, EnemyBlackboardKeys.SelfHpPercent, 1f),
                stamina = 1f,
                playerState = ReadString(blackboard, EnemyBlackboardKeys.PlayerActionLastToken),
                predictedNextPlayerAction = ReadString(blackboard, EnemyBlackboardKeys.PredictedNextPlayerAction),
                predictionConfidence = ReadFloat(blackboard, EnemyBlackboardKeys.PredictionConfidence),
                rhythmPhase = evaluation.RhythmPhase,
                reason = evaluation.Reason,
                hasAttackSlot = ReadBool(blackboard, EnemyBlackboardKeys.HasAttackSlot),
                resolverFailureReason = ReadString(blackboard, EnemyBlackboardKeys.ResolverFailureReason)
            };
            _replay.frames.Add(frame);
        }

        public void EndAndSave(string eventType, string detail)
        {
            if (!_enableReplayRecording || _replay == null)
                return;

            AddEvent(eventType, detail);
            _replay.endTime = Time.time;

            var directory = Path.Combine(Application.persistentDataPath, "EncounterReplays");
            Directory.CreateDirectory(directory);
            var actorId = string.IsNullOrWhiteSpace(_replay.actorId) ? gameObject.GetInstanceID().ToString() : _replay.actorId;
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{actorId}.json";
            var json = JsonUtility.ToJson(_replay, true);
            File.WriteAllText(Path.Combine(directory, fileName), json);
            _replay = null;
            _lostTargetTime = -1f;
            enabled = false;
        }

        private void BeginRecording()
        {
            if (!_enableReplayRecording)
                return;

            if (_replay != null)
            {
                _lostTargetTime = -1f;
                return;
            }

            _startTime = Time.time;
            enabled = true;
            _replay = new EncounterReplay
            {
                actorId = gameObject.GetInstanceID().ToString(),
                actorName = gameObject.name,
                startTime = _startTime,
                endTime = _startTime
            };
            AddEvent("target_acquired", "타겟 획득");
        }

        private void HandleTargetLost()
        {
            if (_replay != null)
                _lostTargetTime = Time.time;
        }

        private void AddEvent(string eventType, string detail)
        {
            _replay?.events.Add(new ReplayEvent
            {
                t = Time.time - _startTime,
                eventType = eventType,
                detail = detail
            });
        }

        private static float ReadFloat(Blackboard blackboard, string key, float fallback = 0f)
            => blackboard != null && blackboard.TryGetFloat(key, out var value) ? value : fallback;

        private static int ReadInt(Blackboard blackboard, string key)
            => blackboard != null && blackboard.TryGetInt(key, out var value) ? value : 0;

        private static bool ReadBool(Blackboard blackboard, string key)
            => blackboard != null && blackboard.TryGetBool(key, out var value) && value;

        private static string ReadString(Blackboard blackboard, string key)
            => blackboard != null && blackboard.TryGetString(key, out var value) ? value : string.Empty;
    }
}
