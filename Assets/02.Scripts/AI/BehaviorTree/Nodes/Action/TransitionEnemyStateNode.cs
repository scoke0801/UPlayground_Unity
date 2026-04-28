using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class TransitionEnemyStateNode : BTActionNode
    {
        [SerializeField] private EnemyTransitionStateType _targetState = EnemyTransitionStateType.Idle;
        [SerializeField] private bool _skipIfAlreadyInState = true;

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

        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null || IsBlockedState(controller.CurrentState?.StateName))
                return BTStatus.Failure;

            var targetName = GetStateName(_targetState);
            if (_skipIfAlreadyInState && controller.CurrentState?.StateName == targetName)
                return BTStatus.Success;

            var nextState = CreateState(controller);
            if (nextState == null)
                return BTStatus.Failure;

            controller.TransitionToState(nextState);
            return BTStatus.Success;
        }

        private GameActorState CreateState(ActorMovementController controller)
        {
            var brain = Context.GetComponentCached<EnemyBrain>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            var combat = Context.GetComponentCached<EnemyCombat>();

            return _targetState switch
            {
                EnemyTransitionStateType.Idle => new EnemyIdleState(controller),
                EnemyTransitionStateType.Patrol when brain != null => new EnemyPatrolState(controller, brain),
                EnemyTransitionStateType.Chase when brain != null && detection != null => new EnemyChaseState(controller, brain, detection),
                EnemyTransitionStateType.Attack when brain != null && detection != null && combat != null => new EnemyAttackState(controller, combat, brain, detection),
                EnemyTransitionStateType.Retreat when brain != null && detection != null => new EnemyRetreatState(controller, brain, detection, brain.RetreatDistance),
                _ => null
            };
        }

        private static string GetStateName(EnemyTransitionStateType stateType)
        {
            return stateType switch
            {
                EnemyTransitionStateType.Idle => "Idle",
                EnemyTransitionStateType.Patrol => "Patrol",
                EnemyTransitionStateType.Chase => "Chase",
                EnemyTransitionStateType.Attack => "Attack",
                EnemyTransitionStateType.Retreat => "Retreat",
                _ => ""
            };
        }

        private static bool IsBlockedState(string stateName)
        {
            return stateName is "Death" or "Hit" or "Grabbed" or "Airborne" or "Land" or "TakeOff";
        }
    }
}
