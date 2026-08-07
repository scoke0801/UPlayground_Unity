using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class RequestEnemyActionNode : BTActionNode
    {
        [SerializeField] private EnemyActionIntent _intent = EnemyActionIntent.None;
        [SerializeField] private EnemyActionStyle _style = EnemyActionStyle.None;
        [SerializeField] private AbilityAttackCategory _attackCategory = AbilityAttackCategory.None;
        [SerializeField] private AbilityAIRole _abilityRole = AbilityAIRole.None;
        [SerializeField] private bool _skipIfAlreadyInState = true;
        [SerializeField] private string _cooldownId;
        [SerializeField] private float _cooldownDuration;

        private readonly EnemyAttackTriggerRequest _triggerRequest = new();
        private bool _attackStarted;

        public EnemyActionIntent Intent
        {
            get => _intent;
            set => _intent = value;
        }

        public EnemyActionStyle Style
        {
            get => _style;
            set => _style = value;
        }

        public AbilityAttackCategory AttackCategory
        {
            get => _attackCategory;
            set => _attackCategory = value;
        }

        public AbilityAIRole AbilityRole
        {
            get => _abilityRole;
            set => _abilityRole = value;
        }

        public bool SkipIfAlreadyInState
        {
            get => _skipIfAlreadyInState;
            set => _skipIfAlreadyInState = value;
        }

        public string CooldownId
        {
            get => _cooldownId;
            set => _cooldownId = value;
        }

        public float CooldownDuration
        {
            get => _cooldownDuration;
            set => _cooldownDuration = Mathf.Max(0f, value);
        }

        protected override BTStatus OnUpdate()
        {
            if (_intent is EnemyActionIntent.Attack or EnemyActionIntent.Punish)
            {
                // 비행 적의 공격은 Resolver의 Flying 분기를 거쳐야 정확한 상태(Flying_GroundAttack 등)로 전이된다.
                // 현재 슬롯 예약/리저브 흐름은 지상 전용이므로 비행 컨텍스트가 있으면 일반 Resolver 경로로 위임한다.
                if (Context?.GetComponentCached<EnemyFlyingAIContext>() == null)
                    return UpdateAttackRequest();
            }

            var request = CreateRequest();
            if (!EnemyActionResolver.TryTransition(Context, request, _skipIfAlreadyInState, out var failureReason))
            {
                Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, failureReason ?? string.Empty);
                Context?.DebugTrace?.Record(this, "RequestActionFailure", BTStatus.Failure, failureReason);
                return BTStatus.Failure;
            }

            Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, string.Empty);
            CombatIntentHistoryUtility.RecordSelectedIntentExecution(Context?.Blackboard);
            return BTStatus.Success;
        }

        protected override void OnStart()
        {
            _attackStarted = false;
            _triggerRequest.Reset();
        }

        protected override void OnStop()
        {
            _triggerRequest.Stop();
            _attackStarted = false;
        }

        private BTStatus UpdateAttackRequest()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null)
                return BTStatus.Failure;

            if (_triggerRequest.TriggerIssued)
            {
                BTStatus triggerStatus = _triggerRequest.Update();
                if (_triggerRequest.ConsumeAcceptedSignal())
                {
                    _attackStarted = true;
                    CombatIntentHistoryUtility.RecordSelectedIntentExecution(
                        Context?.Blackboard);
                    EnemyActionResolver.RecordCooldown(
                        Context,
                        _cooldownId,
                        _cooldownDuration);
                }
                if (triggerStatus == BTStatus.Failure)
                {
                    Context?.Blackboard?.SetBool(
                        EnemyBlackboardKeys.HasAttackSlot,
                        false);
                    Context?.Blackboard?.SetString(
                        EnemyBlackboardKeys.ResolverFailureReason,
                        "공격 Ability 트리거가 거부되었습니다.");
                }
                return triggerStatus;
            }

            if (controller.CurrentState?.StateId == ActorStateId.Attack)
                return BTStatus.Failure;

            if (_attackStarted)
                return BTStatus.Success;

            var request = CreateRequest();
            if (EnemyActionResolver.IsTransitionBlockedByActionLock(controller.CurrentState, request, out var lockReason))
            {
                Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, lockReason ?? string.Empty);
                Context?.DebugTrace?.Record(this, "RequestAttackBlocked", BTStatus.Failure, lockReason);
                return BTStatus.Failure;
            }

            if (!EnemyActionResolver.IsCooldownReady(Context, _cooldownId))
            {
                Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, $"쿨다운이 준비되지 않았습니다. cooldownId={_cooldownId}");
                return BTStatus.Failure;
            }

            var combat = Context.GetComponentCached<EnemyCombat>();
            var aiContext = Context.GetComponentCached<EnemyAIContext>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            if (combat == null || aiContext == null || detection == null || !detection.HasTarget)
            {
                Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, "공격 요청에 필요한 Combat/AIContext/Detection/Target이 없습니다.");
                return BTStatus.Failure;
            }

            if (!combat.HasAvailableSkillAtDistance(
                    detection.DistanceToTarget,
                    _attackCategory,
                    _abilityRole))
            {
                string detail =
                    $"현재 거리에서 사용 가능한 공격이 없습니다. "
                    + $"distance={detection.DistanceToTarget:0.00}, "
                    + $"category={_attackCategory}, role={_abilityRole}; "
                    + combat.BuildAbilitySelectionDiagnosticSummary();
                Context?.Blackboard?.SetString(
                    EnemyBlackboardKeys.ResolverFailureReason,
                    detail);
                Context?.DebugTrace?.Record(
                    this,
                    "AbilitySelectionRejected",
                    BTStatus.Failure,
                    detail);
                return BTStatus.Failure;
            }

            if (!aiContext.TryRequestAttackSlot())
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, "공격 슬롯을 확보하지 못했습니다.");
                return BTStatus.Failure;
            }

            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
            Context?.Blackboard?.SetString(EnemyBlackboardKeys.ResolverFailureReason, string.Empty);
            combat.ReserveAttackSelection(_attackCategory, _abilityRole);
            aiContext.NotifyBTAttackStarted();
            _triggerRequest.Issue(combat, aiContext, _attackCategory);
            BTStatus status = _triggerRequest.Update();
            if (_triggerRequest.ConsumeAcceptedSignal())
            {
                CombatIntentHistoryUtility.RecordSelectedIntentExecution(
                    Context?.Blackboard);
                EnemyActionResolver.RecordCooldown(
                    Context,
                    _cooldownId,
                    _cooldownDuration);
                _attackStarted = true;
            }
            if (status == BTStatus.Failure)
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                Context?.Blackboard?.SetString(
                    EnemyBlackboardKeys.ResolverFailureReason,
                    "공격 Ability 트리거가 거부되었습니다.");
            }
            return status;
        }

        private EnemyActionRequest CreateRequest()
        {
            return new EnemyActionRequest(
                intent: _intent,
                style: _style,
                attackCategory: _attackCategory,
                cooldownId: _cooldownId,
                cooldownDuration: _cooldownDuration,
                abilityRole: _abilityRole);
        }
    }
}
