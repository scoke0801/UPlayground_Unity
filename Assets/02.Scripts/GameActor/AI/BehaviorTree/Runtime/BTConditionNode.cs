namespace UPlayGround.AI.BehaviorTree
{
    public abstract class BTConditionNode : BTNode
    {
        public bool HasAbortEvaluation { get; private set; }
        public BTStatus LastAbortEvaluation { get; private set; } = BTStatus.Failure;

        public BTStatus EvaluateForAbort()
        {
            var status = OnUpdate();
            SetAbortEvaluation(status);
            return status;
        }

        public bool HasConditionChanged(BTStatus currentStatus)
        {
            return HasAbortEvaluation && LastAbortEvaluation != currentStatus;
        }

        public bool EvaluateAbortChanged(out BTStatus currentStatus)
        {
            var previousStatus = LastAbortEvaluation;
            var hadEvaluation = HasAbortEvaluation;
            currentStatus = OnUpdate();
            SetAbortEvaluation(currentStatus);
            return hadEvaluation && previousStatus != currentStatus;
        }

        internal void SetAbortEvaluation(BTStatus status)
        {
            LastAbortEvaluation = status;
            HasAbortEvaluation = true;
        }
    }
}
