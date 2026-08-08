using UnityEngine;
using UPlayGround.Diagnostics;

namespace UPlayGround.AI.BehaviorTree
{
    public class LogNode : BTActionNode
    {
        [SerializeField] private string _message = "Behavior Tree Log";
        [SerializeField] private bool _logEveryTick;

        private bool _logged;

        protected override void OnStart()
        {
            _logged = false;
        }

        protected override BTStatus OnUpdate()
        {
            if (_logEveryTick || !_logged)
            {
                RuntimeLog.Trace(
                    RuntimeLogCategory.Combat | RuntimeLogCategory.Monster,
                    $"[BT] {_message}",
                    Context?.Owner);
                _logged = true;
            }

            return BTStatus.Success;
        }
    }
}
