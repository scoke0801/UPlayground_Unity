using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>NPC를 지정 단계의 전투 반응에 머물게 한다. 상위 조건이 깨져 중단될 때까지 Running을 유지한다.</summary>
    public class HoldNpcReactionNode : BTActionNode
    {
        [SerializeField] private NpcReactionLevel _level = NpcReactionLevel.Watch;

        public NpcReactionLevel Level
        {
            get => _level;
            set => _level = value;
        }

        protected override BTStatus OnUpdate()
        {
            var reaction = Context?.GetComponentCached<NpcCombatReaction>();
            return reaction != null && reaction.Hold(_level) ? BTStatus.Running : BTStatus.Failure;
        }
    }
}
