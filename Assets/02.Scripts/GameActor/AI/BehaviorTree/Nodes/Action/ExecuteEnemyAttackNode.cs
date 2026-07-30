using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class ExecuteEnemyAttackNode : BTActionNode
    {
        [SerializeField] private AbilityAttackCategory _attackCategory = AbilityAttackCategory.None;

        private bool _attackStarted;

        public AbilityAttackCategory AttackCategory
        {
            get => _attackCategory;
            set => _attackCategory = value;
        }

        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null)
                return BTStatus.Failure;

            if (controller.CurrentState?.StateName == "Attack")
            {
                _attackStarted = true;
                return BTStatus.Running;
            }

            if (controller.CurrentState?.BlocksBehaviorTree == true)
                return BTStatus.Failure;

            if (_attackStarted)
                return BTStatus.Success;

            var combat = Context.GetComponentCached<EnemyCombat>();
            var context = Context.GetComponentCached<EnemyAIContext>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            if (combat == null || context == null || detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            if (!combat.HasAvailableSkillAtDistance(detection.DistanceToTarget, _attackCategory))
                return BTStatus.Failure;

            if (!context.TryRequestAttackSlot())
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                return BTStatus.Failure;
            }

            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
            combat.ReserveAttackCategory(_attackCategory);
            var preparedSkill =
                combat.SelectAndExecuteSkill(detection.DistanceToTarget);
            if (preparedSkill == null)
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                context.ReleaseGroupSlot();
                return BTStatus.Failure;
            }

            context.NotifyBTAttackStarted();
            if (!controller.TryTransitionToState(
                    new EnemyAttackState(
                        controller,
                        combat,
                        context,
                        detection,
                        preparedSkill)))
            {
                combat.CancelCurrentAction();
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                context.ReleaseGroupSlot();
                return BTStatus.Failure;
            }

            CombatIntentHistoryUtility.RecordSelectedIntentExecution(Context?.Blackboard);
            _attackStarted = true;
            return BTStatus.Running;
        }

        protected override void OnStart()
        {
            _attackStarted = false;
        }

        protected override void OnStop()
        {
            _attackStarted = false;
        }
    }
}
