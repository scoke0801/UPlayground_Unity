using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 단일 Ability 또는 공격 카테고리 후보의 활성화 가능 여부를 검사한다.
    /// 실행 중인 Ability는 쿨다운 때문에 false가 되더라도 성공으로 유지해
    /// 조건부 abort가 현재 공격을 진동시키지 않게 한다.
    /// </summary>
    public class CanActivateAbilityNode : BTConditionNode
    {
        [SerializeField] private GameplayAbilitySO _ability;
        [SerializeField] private AbilityAttackCategory _category =
            AbilityAttackCategory.None;
        [SerializeField] private AbilityAIRole _abilityRole =
            AbilityAIRole.None;

        public GameplayAbilitySO Ability
        {
            get => _ability;
            set => _ability = value;
        }

        public AbilityAttackCategory Category
        {
            get => _category;
            set => _category = value;
        }

        public AbilityAIRole AbilityRole
        {
            get => _abilityRole;
            set => _abilityRole = value;
        }

        protected override BTStatus OnUpdate()
        {
            EnemyCombat combat = Context?.GetComponentCached<EnemyCombat>();
            if (combat?.AbilitySystem == null)
                return BTStatus.Failure;

            if (_ability != null)
            {
                bool executing = combat.AbilitySystem.TryGetActiveExecutionHandle(
                    _ability,
                    out _);
                return executing || combat.CanActivateAbility(_ability)
                    ? BTStatus.Success
                    : BTStatus.Failure;
            }

            bool categoryExecuting = combat.CurrentAbility != null
                && EnemyAbilitySelectionPolicy.MatchesCategory(
                    combat.CurrentSkill,
                    _category)
                && EnemyAbilitySelectionPolicy.MatchesRole(
                    combat.CurrentSkill,
                    _abilityRole)
                && combat.AbilitySystem.TryGetActiveExecutionHandle(
                    combat.CurrentAbility,
                    out _);
            if (categoryExecuting)
                return BTStatus.Success;

            EnemyDetection detection =
                Context?.GetComponentCached<EnemyDetection>();
            float distance = detection != null && detection.HasTarget
                ? detection.DistanceToTarget
                : float.MaxValue;
            return combat.HasAvailableSkillAtDistance(
                    distance,
                    _category,
                    _abilityRole)
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
