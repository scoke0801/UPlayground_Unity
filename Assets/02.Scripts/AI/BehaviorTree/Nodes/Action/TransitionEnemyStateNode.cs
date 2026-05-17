using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class TransitionEnemyStateNode : BTActionNode
    {
        [SerializeField] private EnemyTransitionStateType _targetState = EnemyTransitionStateType.Idle;
        [SerializeField] private bool _skipIfAlreadyInState = true;
        [SerializeField] private string _cooldownId;
        [SerializeField] private float _cooldownDuration;

        public EnemyTransitionStateType TargetState
        {
            get => _targetState;
            set => _targetState = value;
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
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null || IsBlockedEnemyStateNode.IsBlockedState(controller.CurrentState))
                return BTStatus.Failure;

            if (!IsCooldownReady())
                return BTStatus.Failure;

            if (_targetState == EnemyTransitionStateType.Guard && !CanStartGuard())
                return BTStatus.Failure;

            var targetName = GetStateName(_targetState);
            if (_skipIfAlreadyInState && controller.CurrentState?.StateName == targetName)
            {
                RecordCooldown();
                return BTStatus.Success;
            }

            var nextState = CreateState(controller);
            if (nextState == null)
                return BTStatus.Failure;

            controller.TransitionToState(nextState);
            RecordCooldown();
            return BTStatus.Success;
        }

        private void RecordCooldown()
        {
            if (Context?.Blackboard == null || string.IsNullOrWhiteSpace(_cooldownId) || _cooldownDuration <= 0f)
                return;

            Context.Blackboard.SetFloat($"Cooldown.{_cooldownId}.ReadyTime", Time.time + _cooldownDuration);
        }

        private bool IsCooldownReady()
        {
            if (Context?.Blackboard == null || string.IsNullOrWhiteSpace(_cooldownId))
                return true;

            return !Context.Blackboard.TryGetFloat($"Cooldown.{_cooldownId}.ReadyTime", out var readyTime)
                   || Time.time >= readyTime;
        }

        private bool CanStartGuard()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            return memory == null || memory.CanStartGuard();
        }

        private GameActorState CreateState(ActorMovementController controller)
        {
            var context = Context.GetComponentCached<EnemyAIContext>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            var combat = Context.GetComponentCached<EnemyCombat>();

            return _targetState switch
            {
                EnemyTransitionStateType.Idle => new EnemyIdleState(controller),
                EnemyTransitionStateType.Patrol when context != null => new EnemyPatrolState(controller, context),
                EnemyTransitionStateType.Chase when context != null && detection != null => new EnemyChaseState(controller, context, detection),
                EnemyTransitionStateType.Attack when context != null && detection != null && combat != null => new EnemyAttackState(controller, combat, context, detection),
                EnemyTransitionStateType.Retreat when context != null && detection != null => new EnemyRetreatState(controller, context, detection, context.RetreatDistance),
                EnemyTransitionStateType.Dodge when context != null && detection != null => new EnemyDodgeState(controller, context, detection),
                EnemyTransitionStateType.Circle when context != null && detection != null => new EnemyCircleState(controller, context, detection, context.CircleDuration),
                EnemyTransitionStateType.Guard when context != null && detection != null && context.HasGuardMotion => new EnemyGuardState(controller, context, detection, context.GuardDuration),
                EnemyTransitionStateType.Charge when context != null && detection != null && combat != null => new EnemyChargeState(controller, combat, context, detection, Context.GetComponentCached<EnemyTacticalMemory>()),
                EnemyTransitionStateType.Flank when context != null && detection != null && combat != null => new EnemyFlankState(controller, combat, context, detection),
                EnemyTransitionStateType.Counter when context != null && detection != null && combat != null => new EnemyCounterState(controller, combat, context, detection, Context.GetComponentCached<EnemyTacticalMemory>()),
                EnemyTransitionStateType.JumpBack when context != null && detection != null => new EnemyJumpBackState(controller, context, detection, Context.GetComponentCached<EnemyTacticalMemory>()),
                _ => null
            };
        }

        private static string GetStateName(EnemyTransitionStateType stateType)
        {
            return stateType switch
            {
                EnemyTransitionStateType.Idle => "Idle",
                EnemyTransitionStateType.Patrol => "Patrol",
                EnemyTransitionStateType.Chase => "Chase",
                EnemyTransitionStateType.Attack => "Attack",
                EnemyTransitionStateType.Retreat => "Retreat",
                EnemyTransitionStateType.Dodge => "Dodge",
                EnemyTransitionStateType.Circle => "Circle",
                EnemyTransitionStateType.Guard => "Guard",
                EnemyTransitionStateType.Charge => "Charge",
                EnemyTransitionStateType.Flank => "Flank",
                EnemyTransitionStateType.Counter => "Counter",
                EnemyTransitionStateType.JumpBack => "JumpBack",
                _ => ""
            };
        }
    }
}
