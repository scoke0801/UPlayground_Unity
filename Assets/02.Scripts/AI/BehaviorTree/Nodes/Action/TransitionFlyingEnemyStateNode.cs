using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 적의 BT 전환 Action. EnemyFlyingAIContext를 통해 비행 State를 생성한다.
    /// 지상 전용 <see cref="TransitionEnemyStateNode"/>와 형제 관계.
    /// </summary>
    public class TransitionFlyingEnemyStateNode : BTActionNode
    {
        [SerializeField] private FlyingEnemyTransitionStateType _targetState = FlyingEnemyTransitionStateType.Chase;
        [SerializeField] private bool _skipIfAlreadyInState = true;

        public FlyingEnemyTransitionStateType TargetState
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
            if (controller == null || IsBlockedEnemyStateNode.IsBlockedState(controller.CurrentState?.StateName))
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
            var context = Context.GetComponentCached<EnemyFlyingAIContext>();

            return _targetState switch
            {
                FlyingEnemyTransitionStateType.Idle => new EnemyIdleState(controller),
                FlyingEnemyTransitionStateType.Patrol when context != null => new EnemyFlyingPatrolState(controller, context),
                FlyingEnemyTransitionStateType.Chase when context != null => new EnemyFlyingChaseState(controller, context),
                FlyingEnemyTransitionStateType.GroundAttack when context != null => new EnemyFlyingGroundAttackState(controller, context),
                FlyingEnemyTransitionStateType.Circle when context != null => new EnemyFlyingCircleState(controller, context, context.CircleDuration),
                FlyingEnemyTransitionStateType.Retreat when context != null => new EnemyFlyingRetreatState(controller, context),
                FlyingEnemyTransitionStateType.TakeOff when context != null => new EnemyFlyingTakeOffState(controller, context),
                FlyingEnemyTransitionStateType.AirCircle when context != null => new EnemyFlyingAirCircleState(controller, context),
                FlyingEnemyTransitionStateType.Land when context != null => new EnemyFlyingLandState(controller, context),
                FlyingEnemyTransitionStateType.Dive when context != null => new EnemyFlyingDiveState(controller, context),
                _ => null
            };
        }

        public static string GetStateName(FlyingEnemyTransitionStateType stateType)
        {
            return stateType switch
            {
                FlyingEnemyTransitionStateType.Idle => "Idle",
                FlyingEnemyTransitionStateType.Patrol => "Flying_Patrol",
                FlyingEnemyTransitionStateType.Chase => "Flying_Chase",
                FlyingEnemyTransitionStateType.GroundAttack => "Flying_GroundAttack",
                FlyingEnemyTransitionStateType.Circle => "Flying_Circle",
                FlyingEnemyTransitionStateType.Retreat => "Flying_Retreat",
                FlyingEnemyTransitionStateType.TakeOff => "Flying_TakeOff",
                FlyingEnemyTransitionStateType.AirCircle => "Flying_AirCircle",
                FlyingEnemyTransitionStateType.Land => "Flying_Land",
                FlyingEnemyTransitionStateType.Dive => "Flying_Dive",
                _ => ""
            };
        }
    }
}
