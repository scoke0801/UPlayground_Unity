using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class ExecuteEnemyAttackNode : BTActionNode
    {
        [SerializeField] private AbilityAttackCategory _attackCategory = AbilityAttackCategory.None;

        // 접근을 유지한 채 Running으로 머무를 수 있는 상한.
        // 지형/그룹 슬롯 때문에 사거리에 끝내 들어가지 못하면 이 노드가 BT를 영구 점유하므로,
        // 상한을 넘기면 Failure로 빠져 상위 BT가 후퇴·재배치 등 다른 분기를 고를 수 있게 한다.
        private const float APPROACH_TIMEOUT = 4f;

        private bool _attackStarted;
        private float _approachDeadline;

        public AbilityAttackCategory AttackCategory
        {
            get => _attackCategory;
            set => _attackCategory = value;
        }

        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null)
                return BTStatus.Failure;

            if (controller.CurrentState?.StateId == ActorStateId.Attack)
            {
                _attackStarted = true;
                return BTStatus.Running;
            }

            if (controller.CurrentState?.BlocksBehaviorTree == true)
                return BTStatus.Failure;

            if (_attackStarted)
                return BTStatus.Success;

            var combat = Context.GetComponentCached<EnemyCombat>();
            var context = Context.GetComponentCached<EnemyAIContext>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            if (combat == null || context == null || detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            // 최대 사거리 밖에 있을 때만 선택한 공격 의도를 유지하며 접근한다.
            // 최소 거리 안쪽이거나 카테고리 후보 자체가 없으면 Failure로 상위 BT의
            // 후퇴·재배치·다른 공격 분기가 선택될 수 있게 한다.
            EnemyAttackDistanceRelation distanceRelation =
                EnemyAttackRangePolicy.EvaluateAttackDistance(
                    combat.AbilitySet,
                    detection.DistanceToTarget,
                    combat.CurrentLevel,
                    _attackCategory,
                    useMeleeApproachRange: true,
                    personalSpaceDistance: context.PersonalSpaceDistance);
            if (distanceRelation == EnemyAttackDistanceRelation.TooFar)
            {
                if (_approachDeadline <= 0f)
                    _approachDeadline = Time.time + APPROACH_TIMEOUT;
                else if (Time.time >= _approachDeadline)
                    return BTStatus.Failure;

                if (controller.CurrentState is EnemyChaseState chaseState)
                {
                    chaseState.SetApproachAttackCategory(_attackCategory);
                }
                else if (!controller.TryTransitionToState(
                             new EnemyChaseState(
                                 controller,
                                 context,
                                 detection,
                                 _attackCategory)))
                {
                    return BTStatus.Failure;
                }

                return BTStatus.Running;
            }

            _approachDeadline = 0f;
            if (distanceRelation != EnemyAttackDistanceRelation.InRange)
                return BTStatus.Failure;

            if (!combat.HasAvailableSkillAtDistance(detection.DistanceToTarget, _attackCategory))
                return BTStatus.Failure;

            if (!context.TryRequestAttackSlot())
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                return BTStatus.Failure;
            }

            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
            combat.ReserveAttackCategory(_attackCategory);
            context.NotifyBTAttackStarted();
            controller.TransitionToState(new EnemyAttackState(controller, combat, context, detection));
            CombatIntentHistoryUtility.RecordSelectedIntentExecution(Context?.Blackboard);
            _attackStarted = true;
            return BTStatus.Running;
        }

        protected override void OnStart()
        {
            _attackStarted = false;
            _approachDeadline = 0f;
        }

        protected override void OnStop()
        {
            _attackStarted = false;
            _approachDeadline = 0f;
        }
    }
}
