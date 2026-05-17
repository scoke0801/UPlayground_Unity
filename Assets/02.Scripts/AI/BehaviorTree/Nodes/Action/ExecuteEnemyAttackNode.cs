using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class ExecuteEnemyAttackNode : BTActionNode
    {
        [SerializeField] private EnemyAttackCategory _attackCategory = EnemyAttackCategory.None;

        public EnemyAttackCategory AttackCategory
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
                return BTStatus.Running;

            if (controller.CurrentState?.StateName is "Death" or "Hit" or "Grabbed" or "Airborne")
                return BTStatus.Failure;

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
            context.NotifyBTAttackStarted();
            controller.TransitionToState(new EnemyAttackState(controller, combat, context, detection));
            return BTStatus.Running;
        }
    }
}
