using UPlayGround.Component;
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

            var distance = detection != null && detection.HasTarget
                ? detection.DistanceToTarget
                : float.MaxValue;

            return combat.HasAvailableSkillAtDistance(distance) ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
