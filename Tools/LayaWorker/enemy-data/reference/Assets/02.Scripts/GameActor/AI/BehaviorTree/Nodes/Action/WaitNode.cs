using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class WaitNode : BTActionNode
    {
        [SerializeField] private float _duration = 1f;

        private float _elapsed;

        protected override void OnStart()
        {
            _elapsed = 0f;
        }

        protected override BTStatus OnUpdate()
        {
            _elapsed += Time.deltaTime;
            return _elapsed >= _duration ? BTStatus.Success : BTStatus.Running;
        }
    }
}
