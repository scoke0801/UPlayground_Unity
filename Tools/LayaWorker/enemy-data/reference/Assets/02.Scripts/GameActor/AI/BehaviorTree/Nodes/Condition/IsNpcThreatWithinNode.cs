using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>어그로 중인 몬스터가 NPC 전투 반응 프로파일의 해당 단계 반경 안에 있으면 Success.</summary>
    public class IsNpcThreatWithinNode : BTConditionNode
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
            return reaction != null && reaction.IsThreatWithin(_level) ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
