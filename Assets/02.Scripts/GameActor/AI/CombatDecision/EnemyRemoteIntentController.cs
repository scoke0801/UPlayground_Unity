#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Data.AI;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>훈련 세션에 주입되는 단일 상대의 비동기 판단과 일회 소비 수명.</summary>
    public sealed class EnemyRemoteIntentController : MonoBehaviour
    {
        private MonsterActor _actor;
        private EnemyCombat _combat;
        private EnemyDetection _detection;
        private EnemyAIContext _ai;
        private ActorMovementController _movement;
        private BehaviorTreeRunner _runner;
        private LayaTrainingProfileSO _profile;
        private LayaTrainingMode _mode;
        private string _endpoint;
        private CancellationTokenSource _cancellation;
        private bool _inFlight, _stopped, _suspended, _permanentFailure;
        private int _generation, _epoch, _sequence, _failures;
        private double _nextRequest;
        private LayaDecisionRecord _pending, _applied, _execution;
        private JObject _response;
        private readonly Dictionary<BTNode, (string path, string intent)> _branches = new();
        private BTNode _currentAction;
        private BTNode _requestedAction;
        private LayaDecisionRecord _requestedDecision;
        private LayaTargetLifetime _targetLifetime;
        private GameActor _observedTarget;
        private int _requestedFrame;
        private readonly List<LayaDecisionRecord> _attacks = new();
        public LayaDiagnostics Diagnostics { get; private set; }
        public bool IsStopped => _stopped;
        public void RecheckConnection(string endpoint, LayaTrainingProfileSO profile)
        {
            if (_stopped || endpoint != _endpoint || profile != _profile) return;
            _permanentFailure = false; _failures = 0; _nextRequest = Now;
        }
        private double Now => Time.realtimeSinceStartupAsDouble;

        /// <summary>원본 에셋을 변경하지 않고 생성된 상대 한 마리에 실험을 바인딩한다.</summary>
        public void Configure(LayaTrainingProfileSO profile, LayaTrainingMode mode, string endpoint, LayaDiagnostics diagnostics)
        {
            _profile = profile; _mode = mode; _endpoint = endpoint; Diagnostics = diagnostics;
            _actor = GetComponent<MonsterActor>(); _combat = GetComponent<EnemyCombat>();
            _detection = GetComponent<EnemyDetection>(); _ai = GetComponent<EnemyAIContext>();
            _movement = GetComponent<ActorMovementController>(); _runner = GetComponent<BehaviorTreeRunner>();
            _generation++; _stopped = false;
            _cancellation = new CancellationTokenSource();
            _runner.Context.RemoteIntent = this;
            MapBranches(_runner.RuntimeTree.RootNode, "", null);
            _movement.OnStateChanged += OnStateChanged;
            _combat.LayaAbilityStarted += OnAbilityStarted;
            _combat.LayaAbilityEnded += OnAbilityEnded;
            CombatLogRecorder.ResultObserved += OnCombatResult;
            MonitorLifetime().Forget();
        }

        private void MapBranches(BTNode node, string path, string intent)
        {
            if (node == null) return;
            path += "/" + node.DisplayName;
            // 시퀀스의 실제 SelectedIntent 조건을 읽는다. 노드/액터 이름으로 행동을 추측하지 않는다.
            foreach (var child in node.Children)
                if (child is BlackboardCompareNode compare && compare.Key == EnemyBlackboardKeys.DecisionSelectedIntent
                    && compare.Comparison == BlackboardComparisonType.Equal) intent = compare.Value;
            _branches[node] = (path, intent);
            foreach (var child in node.Children) MapBranches(child, path, intent);
        }

        public void BeginAction(BTNode node) { _currentAction = node; }
        public void MarkAttackRequest()
        { _requestedAction = _currentAction; _requestedDecision = _applied; _requestedFrame = Time.frameCount; }
        public void EndAction(BTNode node, BTStatus status)
        {
            if (_currentAction != node) return;
            if (status == BTStatus.Failure && _applied != null)
                Diagnostics.Event(_applied, "BT 행동 거절: " + node.DisplayName, Now);
            _currentAction = null;
        }

        /// <summary>Blackboard 기록 직전에만 원격 선택을 확정한다. 대기 중에는 로컬 선택을 유지한다.</summary>
        public CombatIntentEvaluation Resolve(EnemyCombatDecisionEvaluator evaluator, Blackboard blackboard, CombatIntentEvaluation local)
        {
            if (_stopped || _mode == LayaTrainingMode.Native || _suspended) return local;
            double now = Now;
            var context = evaluator.LastContext;
            if (_pending != null && !IsLive(_pending)) Invalidate("대상·전투 상황 변경 또는 유효기간 만료");
            if (_response != null && _pending != null && CanChoose())
            {
                var record = _pending;
                var chosen = (string)_response["selected_action_id"];
                _response = null; _pending = null;
                if (!Enum.TryParse(chosen, out CombatIntent intent) || !IsCandidate(intent, context))
                { record.status = "폐기"; record.reason = "현재 후보 자격 상실"; Diagnostics.Changed(); return local; }
                if (_mode == LayaTrainingMode.Shadow)
                { record.status = "관찰 비교"; record.reason = "기존 AI로 실행 · 원격 적용 안 함"; Diagnostics.Changed(); return local; }
                record.intentApplied = true; record.applied = now; record.status = "의도 적용 · 실행 대기";
                record.reason = "현재 BT 판단 틱에서 한 번 소비"; _applied = record;
                Diagnostics.Event(record, "BT 의도 확정: " + chosen, now);
                return new CombatIntentEvaluation(intent, local.AttackScore, local.PunishScore, local.CounterScore,
                    local.PressureScore, local.ChaseScore, local.RetreatScore, local.KeepDistanceScore,
                    local.DefendScore, local.RecoverScore, local.RhythmPhase, "Mac 의도 " + record.requestId);
            }
            if (_applied != null)
            {
                if (!_applied.executed) { _applied.status = "미실행"; _applied.reason = "다음 판단까지 해당 의도 행동이 시작되지 않음"; Diagnostics.Changed(); }
                _applied = null;
            }
            if (!_inFlight && _pending == null && !_permanentFailure && now >= _nextRequest && CanChoose())
            {
                _inFlight = true;
                // 직렬화·HTTP 준비를 BT 평가 스택 밖으로 미룬다. Context는 값 복사다.
                CaptureAndSend(context, blackboard, local.SelectedIntent, now).Forget();
            }
            return local;
        }

        private bool CanChoose() => !_stopped && !_suspended && _actor != null && _actor.IsAlive()
            && UPlayGround.Manager.Svc.HitStop?.IsHitStopping != true
            && _detection.HasTarget && _runner.IsRunning && !_runner.IsTrainingPaused
            && _movement.CurrentState?.BlocksBehaviorTree != true
            && _movement.CurrentState?.StateId != ActorStateId.Attack && !_combat.AbilitySystem.HasActiveAbility;

        private bool IsCandidate(CombatIntent intent, in IntentEvaluationContext c)
        {
            if (Array.IndexOf(_profile.supportedIntents, intent) < 0) return false;
            return intent switch {
                CombatIntent.Attack => c.ActionDelayElapsed && (c.HasAvailableAttack || c.Distance > c.OptimalDistance),
                CombatIntent.Punish => c.ActionDelayElapsed && c.HasAvailableAttack && (c.IsPlayerRecovering || c.IsPlayerStaggered),
                CombatIntent.Counter => c.ActionDelayElapsed && c.HasAvailableAttack && c.IsPlayerAttacking,
                CombatIntent.Defend => c.HasGuardMotion,
                CombatIntent.Chase => c.Distance > c.MinDistance,
                _ => true };
        }

        private async UniTaskVoid CaptureAndSend(IntentEvaluationContext context, Blackboard blackboard, CombatIntent local, double observed)
        {
            int generation = _generation, epoch = _epoch;
            try
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                if (!CanChoose() || generation != _generation || epoch != _epoch) return;
                if (_failures >= _profile.failureThreshold)
                {
                    var health = await LayaDecisionClient.Send(_endpoint, null, _profile.timeoutSeconds, _cancellation.Token);
                    if (health.Error != null || !LayaProtocol.ValidateHealth(JObject.Parse(health.Body), _profile))
                    { Diagnostics.Connection = "재연결 대기 · 설정 확인 필요 · 로컬 전환"; return; }
                    _failures = 0;
                }
                var candidates = new JArray();
                foreach (var intent in _profile.supportedIntents)
                    if (IsCandidate(intent, context)) candidates.Add(new JObject { ["id"] = intent.ToString(), ["description"] = Describe(intent) });
                if (candidates.Count < 2) return;
                var target = _detection.CurrentTarget.GetComponentInParent<GameActor>();
                _observedTarget = target;
                _targetLifetime = _detection.CurrentTarget.GetComponent<LayaTargetLifetime>();
                if (_targetLifetime == null) _targetLifetime = _detection.CurrentTarget.gameObject.AddComponent<LayaTargetLifetime>();
                var ctx = JObject.FromObject(context); ctx["PredictedNextPlayerAction"] = context.PredictedNextPlayerAction.ToString();
                var bias = _ai.CurrentGroupIntentBias;
                blackboard.TryGetString(EnemyBlackboardKeys.DecisionLastIntent, out var last);
                blackboard.TryGetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, out var repeat);
                blackboard.TryGetBool(EnemyBlackboardKeys.HasAttackSlot, out var slot);
                blackboard.TryGetFloat(EnemyBlackboardKeys.NextActionAllowedTime, out var delay);
                var attacks = _combat.CaptureAttackEligibility(context.Distance);
                // 상세 거절 사유는 로컬 기록에 남기고 모델에는 동일한 실측 자격을 압축한다.
                var compact = new JArray();
                foreach (var attack in attacks) compact.Add(new JObject { ["id"] = attack["id"], ["variant"] = attack["variant"],
                    ["category"] = attack["category"], ["eligible"] = attack["eligible"], ["cooldown_s"] = attack["cooldown_s"] });
                var observation = new JObject { ["actor"] = _profile.definition.actorId, ["source"] = "runtime_capture",
                    ["role"] = _ai.BehaviorData.aiRole.ToString(), ["runtime_level"] = _combat.CurrentLevel,
                    ["context"] = ctx, ["phase_index"] = Array.IndexOf(_ai.BehaviorData.phases, _ai.CurrentPhase),
                    ["history"] = new JArray(last ?? "None", repeat),
                    ["group_bias"] = new JObject { ["AttackMultiplier"] = bias.AttackMultiplier, ["PunishMultiplier"] = bias.PunishMultiplier,
                        ["CounterMultiplier"] = bias.CounterMultiplier, ["PressureBonus"] = bias.PressureBonus,
                        ["RetreatBonus"] = bias.RetreatBonus, ["KeepDistanceBonus"] = bias.KeepDistanceBonus },
                    ["blackboard"] = new JObject { ["Target.Has"] = true, ["Target.Distance"] = context.Distance,
                        ["Self.StateId"] = _movement.CurrentState.StateId.ToString(), ["HasAttackSlot"] = slot, ["NextActionAllowedTime"] = delay },
                    ["self_tags"] = CaptureTags(_actor), ["target_tags"] = CaptureTags(target), ["attacks"] = compact };
                var record = new LayaDecisionRecord { session = Diagnostics.Session, sequence = ++_sequence,
                    generation = generation, epoch = epoch, targetId = _detection.CurrentTarget.GetInstanceID(), targetGeneration = _targetLifetime.Generation,
                    actor = _profile.definition.displayName + " #" + _actor.GetInstanceID(), target = target != null ? target.name : _detection.CurrentTarget.name,
                    state = _movement.CurrentState.StateId.ToString(), localIntent = local.ToString(), observed = observed };
                record.requestId = Diagnostics.Session + "-" + generation + "-" + record.sequence;
                var request = new JObject { ["protocol_version"] = 1, ["request_id"] = record.requestId,
                    ["session_id"] = record.session, ["state_seq"] = record.sequence, ["observation"] = observation, ["candidates"] = candidates };
                record.requestJson = request.ToString(Formatting.None);
                record.observationJson = observation.ToString(Formatting.Indented);
                record.candidatesJson = "공격 Variant 자격\n" + attacks.ToString() + "\n의도 후보\n" + candidates.ToString();
                Diagnostics.Add(record);
                if (candidates.Count > 8 || !LayaProtocol.IsWithinBudget(request))
                { record.status = "로컬 전환"; record.reason = "후보 또는 관측 byte 예산 초과 · 생략 전송 안 함"; _permanentFailure = true; return; }
                _pending = record; Diagnostics.Connection = "추론 중";
                var reply = await LayaDecisionClient.Send(_endpoint, record.requestJson, _profile.timeoutSeconds, _cancellation.Token);
                record.sent = reply.Sent; record.received = reply.Received; record.responseJson = reply.Body;
                record.httpSuccess = reply.Status == 200 && reply.Error == null;
                if (_pending != record || !IsLive(record)) { record.status = "폐기"; record.reason = "취소·수명 종료 후 응답"; return; }
                if (!record.httpSuccess)
                {
                    record.status = "로컬 전환"; record.reason = reply.Error;
                    _failures++; _pending = null;
                    _permanentFailure = reply.Status >= 400 && reply.Status < 500;
                    Diagnostics.Connection = _permanentFailure ? "계약/인증 오류 · 연결 재검사 필요" : "연결 장애 · 로컬 전환";
                    return;
                }
                var response = JObject.Parse(reply.Body, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (!LayaProtocol.ValidateResponse(request, response, _profile, out var reason))
                { record.status = "거절"; record.reason = reason; _pending = null; _permanentFailure = true; return; }
                record.validated = true; record.selectedIntent = (string)response["selected_action_id"];
                record.inferenceMs = (double)response["inference_ms"]; record.status = "응답 검증 · 적용 대기";
                _response = response; _failures = 0; Diagnostics.Connection = "연결됨";
            }
            catch (Exception error)
            {
                if (_pending != null) { _pending.status = "거절"; _pending.reason = LayaProtocol.Redact(error.Message); }
                _pending = null; _response = null; _permanentFailure = true;
                Diagnostics.Connection = "관측/응답 오류 · 로컬 전환";
            }
            finally
            {
                _inFlight = false;
                _nextRequest = Now + (_failures >= _profile.failureThreshold ? _profile.reconnectSeconds : _profile.requestInterval);
                Diagnostics.Changed();
            }
        }

        private static JArray CaptureTags(GameActor actor)
        {
            var result = new JArray();
            if (actor?.Tags != null) foreach (var tag in actor.Tags.AllTags) result.Add(tag.TagName);
            return result;
        }

        private bool IsLive(LayaDecisionRecord record) => !_stopped && !_suspended && _actor != null && _actor.IsAlive()
            && _observedTarget != null && _observedTarget is IDamageable targetHealth && targetHealth.IsAlive()
            && _targetLifetime != null && _targetLifetime.isActiveAndEnabled && _targetLifetime.Generation == record.targetGeneration
            && _detection.HasTarget && _runner.IsRunning && !_runner.IsTrainingPaused
            && LayaProtocol.IsLive(Now, record.observed, _profile.validitySeconds, _generation, record.generation,
                _epoch, record.epoch, _detection.CurrentTarget.GetInstanceID(), record.targetId, record.intentApplied);

        private async UniTaskVoid MonitorLifetime()
        {
            var phase = _ai.CurrentPhase;
            var target = _detection.CurrentTarget;
            while (!_stopped && this != null)
            {
                bool paused = Time.timeScale <= 0 || _runner.IsTrainingPaused || !_runner.IsRunning;
                if (paused && !_suspended) Invalidate("전투 일시정지/훈련 정지");
                _suspended = paused;
                if (!_actor.IsAlive()) { StopExperiment("몬스터 사망"); break; }
                if (target != _detection.CurrentTarget || phase != _ai.CurrentPhase)
                { Invalidate("대상 또는 페이즈 변경"); target = _detection.CurrentTarget; phase = _ai.CurrentPhase; }
                await UniTask.Yield();
            }
        }

        private void Invalidate(string reason)
        {
            _epoch++;
            if (_pending != null) { _pending.status = "폐기"; _pending.reason = reason; Diagnostics.Event(_pending, reason, Now); }
            _pending = null; _response = null;
            if (_cancellation != null) { _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = new CancellationTokenSource(); }
        }

        private void OnStateChanged(GameActorState previous, GameActorState current)
        {
            if (current == null) return;
            if (current.BlocksBehaviorTree || current.StateId == ActorStateId.Attack) Invalidate("공격·피격 등 행동 경계 진입");
            if (_execution != null && previous != null && current != previous && _execution.ended < 0 && _execution.ability == "미확인")
            { _execution.ended = Now; Diagnostics.Event(_execution, "이동/상태 행동 종료", Now); }
            if (_currentAction != null && current.StateId != ActorStateId.Attack) RecordExecution();
        }

        private LayaDecisionRecord RecordExecution()
        {
            if (_currentAction == null || !_branches.TryGetValue(_currentAction, out var branch)) return null;
            var record = _applied;
            if (record == null || record.executed)
            {
                record = new LayaDecisionRecord { session = Diagnostics.Session, requestId = Diagnostics.Session + "-local-" + (++_sequence),
                    actor = _profile.definition.displayName, observed = Now, localIntent = "로컬/우선 분기", status = "로컬 실행" };
                Diagnostics.Add(record);
            }
            record.branch = branch.path;
            record.preempted = record.intentApplied && branch.intent != record.selectedIntent;
            record.executed = true; record.started = Now;
            record.status = record.preempted ? "우선 분기 실행" : record.intentApplied ? "Mac 의도 행동 실행" : "기존 AI 행동 실행";
            record.reason = record.preempted ? "실행 분기의 의도 조건이 원격 선택과 다름" : "실제 상태 전환/Ability 시작 관측";
            _execution = record;
            Diagnostics.Event(record, record.status + ": " + branch.path, Now);
            return record;
        }

        private void OnAbilityStarted(string ability, string variant, string motion)
        {
            if (_currentAction == null && _requestedAction != null && Time.frameCount <= _requestedFrame + 1)
            { _currentAction = _requestedAction; _applied = _requestedDecision; }
            var record = RecordExecution();
            _currentAction = null; _requestedAction = null; _requestedDecision = null;
            Invalidate("새 Ability 시작");
            if (record == null) return;
            record.ability = ability; record.variant = variant; record.motionKey = motion;
            _attacks.Add(record); if (_attacks.Count > 16) _attacks.RemoveAt(0);
            Diagnostics.Event(record, "Ability 시작: " + ability + " / " + variant + " / " + motion, Now);
        }

        private void OnAbilityEnded(bool completed)
        {
            if (_execution == null || _execution.ended >= 0) return;
            _execution.ended = Now;
            Diagnostics.Event(_execution, completed ? "Ability 종료" : "Ability 중단", Now);
        }

        private void OnCombatResult(CombatResult result)
        {
            if (result.Victim == _actor) Invalidate("피격 결과 발생");
            if (result.Attacker != _actor) return;
            LayaDecisionRecord match = null;
            foreach (var attack in _attacks)
                if (attack.ability == result.Hit.AbilityId && attack.variant == result.Hit.AbilityVariantId
                    && attack.motionKey == result.Hit.MotionKey) { if (match != null) return; match = attack; }
            // 투사체에 실행 handle이 없으면 반복된 동일 공격을 임의의 요청에 귀속하지 않는다.
            if (match == null) return;
            if (result.DamageApplied) match.hits++;
            Diagnostics.Event(match, $"타격 판정: {result.DefenseOutcome}, 피해 {result.FinalDamage:0.##}, HitPhase {result.Hit.HitPhaseIndex}", Now);
        }

        public void StopExperiment(string reason)
        {
            if (_stopped) return;
            if (_execution != null && _execution.ended < 0)
            { _execution.ended = Now; Diagnostics.Event(_execution, "실험 종료로 추적 중단 · 행동 완료 여부 미확인", Now); }
            _stopped = true; _generation++; Invalidate(reason);
            _cancellation?.Cancel(); _cancellation?.Dispose(); _cancellation = null;
            if (_runner?.Context != null) _runner.Context.RemoteIntent = null;
            if (_movement != null) _movement.OnStateChanged -= OnStateChanged;
            if (_combat != null) { _combat.LayaAbilityStarted -= OnAbilityStarted; _combat.LayaAbilityEnded -= OnAbilityEnded; }
            CombatLogRecorder.ResultObserved -= OnCombatResult;
            Diagnostics.Connection = "실험 종료 · " + reason; Diagnostics.Changed();
        }
        private void OnDisable() { if (Diagnostics != null) StopExperiment("풀 반환/씬 종료"); }

        private static string Describe(CombatIntent intent) => intent switch {
            CombatIntent.Attack => "Commit to an eligible attack, or approach first when out of attack range.",
            CombatIntent.Punish => "Punish the observed recovery or stagger with an eligible attack.",
            CombatIntent.Counter => "Counter the observed aggression with an eligible attack.",
            CombatIntent.Pressure => "Maintain pressure through positioning without immediate attack commitment.",
            CombatIntent.Chase => "Close distance toward the current target.",
            CombatIntent.Retreat => "Disengage and create space.",
            CombatIntent.KeepDistance => "Maintain preferred combat spacing.",
            CombatIntent.Defend => "Use supported defensive behavior.",
            _ => "Recover through safe positioning." };
    }
}
#endif
