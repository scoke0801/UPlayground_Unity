using UPlayGround.Component;
using UPlayGround.MovementController;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class CanUseEnemySkillNode : BTConditionNode
    {
        [SerializeField] private bool _requireTarget = true;

        public bool RequireTarget
        {
            get => _requireTarget;
            set => _requireTarget = value;
        }

        protected override BTStatus OnUpdate()
        {
            var combat = Context?.GetComponentCached<EnemyCombat>();
            var detection = Context?.GetComponentCached<EnemyDetection>();
            if (combat?.AttackData == null)
                return BTStatus.Failure;

            if (_requireTarget && (detection == null || !detection.HasTarget))
                return BTStatus.Failure;

            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context != null && !context.CanUseSkill())
                return BTStatus.Failure;

            var distance = detection != null && detection.HasTarget
                ? detection.DistanceToTarget
                : float.MaxValue;

            return combat.HasAvailableSkillAtDistance(distance) ? BTStatus.Success : BTStatus.Failure;
        }
    }

    public class HasEnemyActionDelayElapsedNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Success;

            return Context.Blackboard.TryGetFloat(EnemyBlackboardKeys.NextActionAllowedTime, out var nextAllowedTime)
                && Time.time < nextAllowedTime
                    ? BTStatus.Failure
                    : BTStatus.Success;
        }
    }

    public class IsBlockedEnemyStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            return IsBlockedState(controller?.CurrentState?.StateName) ? BTStatus.Success : BTStatus.Failure;
        }

        public static bool IsBlockedState(string stateName)
        {
            return stateName is "Death" or "Hit" or "Grabbed" or "Airborne" or "Attack" or "Counter"
                or "Land" or "TakeOff" or "Aerial" or "AerialAttack"
                or "Flying_TakeOff" or "Flying_GroundAttack" or "Flying_Dive" or "Flying_Land";
        }
    }
}
