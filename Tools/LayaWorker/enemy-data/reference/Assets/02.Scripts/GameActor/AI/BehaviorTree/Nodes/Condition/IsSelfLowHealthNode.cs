using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsSelfLowHealthNode : BTConditionNode
    {
        [SerializeField] [Range(0f, 1f)] private float _threshold = 0.3f;

        public float Threshold
        {
            get => _threshold;
            set => _threshold = Mathf.Clamp01(value);
        }

        protected override BTStatus OnUpdate()
        {
            var monster = Context?.GetComponentCached<MonsterActor>();
            return monster != null && monster.GetHealthPercent() <= _threshold
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
