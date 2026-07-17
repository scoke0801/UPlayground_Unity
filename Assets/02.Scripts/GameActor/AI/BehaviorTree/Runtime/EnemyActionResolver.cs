using UPlayGround.Components;
using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public static class EnemyActionResolver
    {
        public static bool TryTransition(
            BehaviorTreeContext context,
            EnemyActionRequest request,
            bool skipIfAlreadyInState,
            out string failureReason)
        {
            failureReason = null;

            var controller = context?.GetComponentCached<ActorMovementController>();
            if (controller == null)
            {
                failureReason = "ActorMovementController가 없습니다.";
                return false;
            }

            if (!IsCooldownReady(context, request.CooldownId))
            {
                failureReason = $"쿨다운이 준비되지 않았습니다. cooldownId={request.CooldownId}";
                return false;
            }

            var nextState = CreateState(context, controller, request, out var creationFailure);
            if (nextState == null)
            {
                failureReason = creationFailure
                    ?? $"요청을 상태로 해석할 수 없습니다. intent={request.Intent}, style={request.Style}";
                return false;
            }

            if (IsTransitionBlockedByActionLock(controller.CurrentState, request, out failureReason))
                return false;

            if (skipIfAlreadyInState && controller.CurrentState?.StateName == nextState.StateName)
            {
                RecordCooldown(context, request.CooldownId, request.CooldownDuration);
                return true;
            }

            if (!controller.TryTransitionToState(nextState))
            {
                failureReason = $"상태 전환 조건을 통과하지 못했습니다. from={controller.CurrentState?.StateName ?? "null"}, to={nextState.StateName}";
                return false;
            }

            RecordCooldown(context, request.CooldownId, request.CooldownDuration);
            return true;
        }

        public static bool IsTransitionBlockedByActionLock(
            GameActorState currentState,
            EnemyActionRequest request,
            out string failureReason)
        {
            failureReason = null;

            if (currentState == null)
                return false;

            if (IsHardLockedState(currentState))
            {
                failureReason = $"현재 상태가 전환 불가 상태입니다. state={currentState.StateName}";
                return true;
            }

            if (!IsProtectedActionState(currentState))
                return false;

            if (IsEvadeActionState(currentState))
            {
                failureReason = $"회피 액션 모션 재생 중이라 BT 전환 요청을 차단했습니다. state={currentState.StateName}, intent={request.Intent}, style={request.Style}";
                return true;
            }

            if (IsLocomotionRequest(request))
                return false;

            failureReason = $"보호 액션 모션 재생 중이라 다른 액션 요청을 차단했습니다. state={currentState.StateName}, intent={request.Intent}, style={request.Style}";
            return true;
        }

        public static EnemyActionRequest FromGroundTransition(
            EnemyTransitionStateType state,
            string cooldownId = null,
            float cooldownDuration = 0f)
        {
            return state switch
            {
                EnemyTransitionStateType.Idle => new EnemyActionRequest(EnemyActionIntent.Recover, EnemyActionStyle.Idle, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Patrol => new EnemyActionRequest(EnemyActionIntent.Recover, EnemyActionStyle.Patrol, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Chase => new EnemyActionRequest(EnemyActionIntent.Chase, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Attack => new EnemyActionRequest(EnemyActionIntent.Attack, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Retreat => new EnemyActionRequest(EnemyActionIntent.Retreat, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Circle => new EnemyActionRequest(EnemyActionIntent.KeepDistance, EnemyActionStyle.Circle, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Guard => new EnemyActionRequest(EnemyActionIntent.Defend, EnemyActionStyle.Guard, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Charge => new EnemyActionRequest(EnemyActionIntent.Pressure, EnemyActionStyle.Charge, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Flank => new EnemyActionRequest(EnemyActionIntent.Pressure, EnemyActionStyle.Flank, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Counter => new EnemyActionRequest(EnemyActionIntent.Counter, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Dodge => new EnemyActionRequest(EnemyActionIntent.Evade, EnemyActionStyle.Dodge, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.JumpBack => new EnemyActionRequest(EnemyActionIntent.Evade, EnemyActionStyle.JumpBack, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                EnemyTransitionStateType.Step => new EnemyActionRequest(EnemyActionIntent.Evade, EnemyActionStyle.Step, cooldownId: cooldownId, cooldownDuration: cooldownDuration),
                _ => new EnemyActionRequest(EnemyActionIntent.None, cooldownId: cooldownId, cooldownDuration: cooldownDuration)
            };
        }

        public static bool IsCooldownReady(BehaviorTreeContext context, string cooldownId)
        {
            if (context?.Blackboard == null || string.IsNullOrWhiteSpace(cooldownId))
                return true;

            var key = EnemyBlackboardKeys.CooldownReadyTime(cooldownId);
            return !context.Blackboard.TryGetFloat(key, out var readyTime) || Time.time >= readyTime;
        }

        public static void RecordCooldown(BehaviorTreeContext context, string cooldownId, float cooldownDuration)
        {
            if (context?.Blackboard == null || string.IsNullOrWhiteSpace(cooldownId) || cooldownDuration <= 0f)
                return;

            context.Blackboard.SetFloat(EnemyBlackboardKeys.CooldownReadyTime(cooldownId), Time.time + cooldownDuration);
        }

        private static GameActorState CreateState(BehaviorTreeContext context, ActorMovementController controller, EnemyActionRequest request, out string failureReason)
        {
            failureReason = null;
            var flyingContext = context.GetComponentCached<EnemyFlyingAIContext>();
            if (flyingContext != null && TryCreateFlyingState(controller, flyingContext, request, out var flyingState))
                return flyingState;

            return CreateGroundState(context, controller, request, out failureReason);
        }

        private static GameActorState CreateGroundState(BehaviorTreeContext context, ActorMovementController controller, EnemyActionRequest request, out string failureReason)
        {
            failureReason = null;
            var aiContext = context.GetComponentCached<EnemyAIContext>();
            var detection = context.GetComponentCached<EnemyDetection>();
            var combat = context.GetComponentCached<EnemyCombat>();
            var memory = context.GetComponentCached<EnemyTacticalMemory>();
            var state = ResolveGroundState(request);

            if (state == EnemyTransitionStateType.Guard)
                return CreateGuardState(controller, aiContext, detection, memory, out failureReason);
            if (state == EnemyTransitionStateType.Dodge)
                return CreateDodgeState(controller, aiContext, detection, out failureReason);
            if (state == EnemyTransitionStateType.Step)
                return CreateStepState(controller, aiContext, detection, out failureReason);

            return state switch
            {
                EnemyTransitionStateType.Idle => new EnemyIdleState(controller),
                EnemyTransitionStateType.Patrol when aiContext != null => new EnemyPatrolState(controller, aiContext),
                EnemyTransitionStateType.Chase when aiContext != null && detection != null => new EnemyChaseState(controller, aiContext, detection),
                EnemyTransitionStateType.Attack when aiContext != null && detection != null && combat != null => new EnemyAttackState(controller, combat, aiContext, detection),
                EnemyTransitionStateType.Retreat when aiContext != null && detection != null => new EnemyRetreatState(controller, aiContext, detection, aiContext.RetreatDistance),
                EnemyTransitionStateType.Circle when aiContext != null && detection != null => new EnemyCircleState(controller, aiContext, detection, aiContext.CircleDuration),
                EnemyTransitionStateType.Charge when aiContext != null && detection != null && combat != null => new EnemyChargeState(controller, combat, aiContext, detection, memory),
                EnemyTransitionStateType.Flank when aiContext != null && detection != null && combat != null => new EnemyFlankState(controller, combat, aiContext, detection),
                EnemyTransitionStateType.Counter when aiContext != null && detection != null && combat != null => new EnemyCounterState(controller, combat, aiContext, detection, memory),
                EnemyTransitionStateType.JumpBack when aiContext != null && detection != null => new EnemyJumpBackState(controller, aiContext, detection, memory),
                _ => null
            };
        }

        private static GameActorState CreateDodgeState(
            ActorMovementController controller,
            EnemyAIContext aiContext,
            EnemyDetection detection,
            out string failureReason)
        {
            failureReason = null;
            if (aiContext == null || detection == null)
            {
                failureReason = "Dodge 전이에 필요한 AIContext/Detection이 없습니다.";
                return null;
            }

            if (!EnemyDodgeState.CanExecute(controller.Actor))
            {
                failureReason = "Dodge 모션(Dodge 또는 Dodge_F/B/L/R)이 정의되지 않았습니다.";
                return null;
            }

            if (!EnemyDodgeState.TryResolveDodgeMotion(
                    controller.Actor,
                    aiContext,
                    detection,
                    controller.Motor.TransientPosition,
                    out var dodgeDirection,
                    out var dodgeMotionKey))
            {
                failureReason = $"계산된 Dodge 방향 모션과 기본 Dodge 모션이 없습니다. motion={dodgeMotionKey}";
                return null;
            }

            return new EnemyDodgeState(controller, aiContext, detection, dodgeDirection, dodgeMotionKey);
        }

        private static GameActorState CreateStepState(
            ActorMovementController controller,
            EnemyAIContext aiContext,
            EnemyDetection detection,
            out string failureReason)
        {
            failureReason = null;
            if (aiContext == null || detection == null)
            {
                failureReason = "Dash 전이에 필요한 AIContext/Detection이 없습니다.";
                return null;
            }

            if (!EnemyStepState.CanExecute(controller.Actor))
            {
                failureReason = "Dash 방향성 모션(Dash_F/B/L/R)이 정의되지 않았습니다.";
                return null;
            }

            if (!EnemyStepState.TryResolveStepMotion(
                    controller.Actor,
                    aiContext,
                    detection,
                    controller.Motor.TransientPosition,
                    out var stepDirection,
                    out var stepMotionKey))
            {
                failureReason = $"계산된 Dash 방향 모션이 없습니다. motion={stepMotionKey}";
                return null;
            }

            return new EnemyStepState(controller, aiContext, detection, stepDirection, stepMotionKey);
        }

        private static GameActorState CreateGuardState(
            ActorMovementController controller,
            EnemyAIContext aiContext,
            EnemyDetection detection,
            EnemyTacticalMemory memory,
            out string failureReason)
        {
            failureReason = null;
            if (aiContext == null || detection == null)
            {
                failureReason = "Guard 전이에 필요한 AIContext/Detection이 없습니다.";
                return null;
            }

            if (!aiContext.HasGuardMotion)
            {
                failureReason = "Guard 모션이 정의되지 않았습니다.";
                return null;
            }

            if (memory != null && !memory.CanStartGuard())
            {
                failureReason = "Guard 쿨다운/연속 가드 제한으로 시작 불가합니다.";
                return null;
            }

            return new EnemyGuardState(controller, aiContext, detection, aiContext.GuardDuration);
        }

        private static EnemyTransitionStateType? ResolveGroundState(EnemyActionRequest request)
        {
            return request.Style switch
            {
                EnemyActionStyle.Idle => EnemyTransitionStateType.Idle,
                EnemyActionStyle.Patrol => EnemyTransitionStateType.Patrol,
                EnemyActionStyle.Dodge => EnemyTransitionStateType.Dodge,
                EnemyActionStyle.JumpBack => EnemyTransitionStateType.JumpBack,
                EnemyActionStyle.Step => EnemyTransitionStateType.Step,
                EnemyActionStyle.Guard => EnemyTransitionStateType.Guard,
                EnemyActionStyle.Circle => EnemyTransitionStateType.Circle,
                EnemyActionStyle.Flank => EnemyTransitionStateType.Flank,
                EnemyActionStyle.Charge => EnemyTransitionStateType.Charge,
                _ => ResolveGroundStateFromIntent(request.Intent)
            };
        }

        private static EnemyTransitionStateType? ResolveGroundStateFromIntent(EnemyActionIntent intent)
        {
            return intent switch
            {
                EnemyActionIntent.Attack => EnemyTransitionStateType.Attack,
                EnemyActionIntent.Punish => EnemyTransitionStateType.Attack,
                EnemyActionIntent.Counter => EnemyTransitionStateType.Counter,
                EnemyActionIntent.Pressure => EnemyTransitionStateType.Circle,
                EnemyActionIntent.Chase => EnemyTransitionStateType.Chase,
                EnemyActionIntent.Retreat => EnemyTransitionStateType.Retreat,
                EnemyActionIntent.KeepDistance => EnemyTransitionStateType.Circle,
                EnemyActionIntent.Defend => EnemyTransitionStateType.Guard,
                EnemyActionIntent.Evade => EnemyTransitionStateType.Dodge,
                EnemyActionIntent.Recover => EnemyTransitionStateType.Idle,
                _ => null
            };
        }

        private static bool TryCreateFlyingState(
            ActorMovementController controller,
            EnemyFlyingAIContext context,
            EnemyActionRequest request,
            out GameActorState state)
        {
            var resolved = ResolveFlyingState(request);
            state = resolved switch
            {
                FlyingEnemyTransitionStateType.Idle => new EnemyIdleState(controller),
                FlyingEnemyTransitionStateType.Patrol => new EnemyFlyingPatrolState(controller, context),
                FlyingEnemyTransitionStateType.Chase => new EnemyFlyingChaseState(controller, context),
                FlyingEnemyTransitionStateType.GroundAttack => new EnemyFlyingGroundAttackState(controller, context),
                FlyingEnemyTransitionStateType.Circle => new EnemyFlyingCircleState(controller, context, context.CircleDuration),
                FlyingEnemyTransitionStateType.Retreat => new EnemyFlyingRetreatState(controller, context),
                FlyingEnemyTransitionStateType.TakeOff => new EnemyFlyingTakeOffState(controller, context),
                FlyingEnemyTransitionStateType.AirCircle => new EnemyFlyingAirCircleState(controller, context),
                FlyingEnemyTransitionStateType.Land => new EnemyFlyingLandState(controller, context),
                FlyingEnemyTransitionStateType.Dive => new EnemyFlyingDiveState(controller, context),
                _ => null
            };

            return state != null;
        }

        private static FlyingEnemyTransitionStateType? ResolveFlyingState(EnemyActionRequest request)
        {
            return request.Style switch
            {
                EnemyActionStyle.Idle => FlyingEnemyTransitionStateType.Idle,
                EnemyActionStyle.Patrol => FlyingEnemyTransitionStateType.Patrol,
                EnemyActionStyle.Dive => FlyingEnemyTransitionStateType.Dive,
                EnemyActionStyle.Land => FlyingEnemyTransitionStateType.Land,
                EnemyActionStyle.TakeOff => FlyingEnemyTransitionStateType.TakeOff,
                EnemyActionStyle.Circle => FlyingEnemyTransitionStateType.Circle,
                _ => ResolveFlyingStateFromIntent(request.Intent)
            };
        }

        private static FlyingEnemyTransitionStateType? ResolveFlyingStateFromIntent(EnemyActionIntent intent)
        {
            return intent switch
            {
                EnemyActionIntent.Attack => FlyingEnemyTransitionStateType.GroundAttack,
                EnemyActionIntent.Punish => FlyingEnemyTransitionStateType.GroundAttack,
                EnemyActionIntent.Chase => FlyingEnemyTransitionStateType.Chase,
                EnemyActionIntent.Retreat => FlyingEnemyTransitionStateType.Retreat,
                EnemyActionIntent.KeepDistance => FlyingEnemyTransitionStateType.Circle,
                EnemyActionIntent.Pressure => FlyingEnemyTransitionStateType.Circle,
                EnemyActionIntent.Defend => FlyingEnemyTransitionStateType.Retreat,
                EnemyActionIntent.Evade => FlyingEnemyTransitionStateType.TakeOff,
                EnemyActionIntent.Recover => FlyingEnemyTransitionStateType.Idle,
                _ => null
            };
        }

        private static bool IsProtectedActionState(GameActorState state)
        {
            if (state == null)
                return false;

            if (!IsBlockedEnemyStateNode.IsBlockedState(state))
                return false;

            return !IsLocomotionState(state);
        }

        private static bool IsHardLockedState(GameActorState state)
        {
            return state?.StateName is
                "Death" or
                "Hit" or
                "Stun" or
                "Knockdown" or
                "Grabbed" or
                "Airborne" or
                "Land" or
                "SpecialBreakVictim";
        }

        private static bool IsLocomotionState(GameActorState state)
        {
            return state is
                EnemyIdleState or
                EnemyPatrolState or
                EnemyChaseState or
                EnemyCircleState or
                EnemyRetreatState;
        }

        private static bool IsEvadeActionState(GameActorState state)
        {
            return state is
                EnemyDodgeState or
                EnemyStepState or
                EnemyJumpBackState;
        }

        private static bool IsLocomotionRequest(EnemyActionRequest request)
        {
            if (request.Style is
                EnemyActionStyle.Idle or
                EnemyActionStyle.Patrol or
                EnemyActionStyle.Circle)
            {
                return true;
            }

            return request.Intent is
                EnemyActionIntent.Chase or
                EnemyActionIntent.Retreat or
                EnemyActionIntent.KeepDistance or
                EnemyActionIntent.Recover;
        }
    }
}
