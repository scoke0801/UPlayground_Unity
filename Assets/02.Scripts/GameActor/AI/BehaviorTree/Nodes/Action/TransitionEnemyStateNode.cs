using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class TransitionEnemyStateNode : BTActionNode
    {
        [SerializeField] private EnemyTransitionStateType _targetState = EnemyTransitionStateType.Idle;
        [SerializeField] private bool _skipIfAlreadyInState = true;
        [SerializeField] private string _cooldownId;
        [SerializeField] private float _cooldownDuration;

        public EnemyTransitionStateType TargetState
        {
            get => _targetState;
            set => _targetState = value;
        }

        public bool SkipIfAlreadyInState
        {
            get => _skipIfAlreadyInState;
            set => _skipIfAlreadyInState = value;
        }

        public string CooldownId
        {
            get => _cooldownId;
            set => _cooldownId = value;
        }

        public float CooldownDuration
        {
            get => _cooldownDuration;
            set => _cooldownDuration = Mathf.Max(0f, value);
        }

        protected override BTStatus OnUpdate()
        {
            var request = EnemyActionResolver.FromGroundTransition(_targetState, _cooldownId, _cooldownDuration);
            if (!EnemyActionResolver.TryTransition(Context, request, _skipIfAlreadyInState, out var failureReason))
            {
                Context?.DebugTrace?.Record(this, "TransitionFailure", BTStatus.Failure, failureReason);
                return BTStatus.Failure;
            }

            return BTStatus.Success;
        }
    }
}
