using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using Motions = UPlayGround.Data.Actor.Animation.MotionTags;

namespace UPlayGround.State
{
    /// <summary>
    /// 전투 반응이 끝난 NPC가 반응 직전 위치로 걸어 돌아가는 상태.
    /// 일과·고정 작업 NPC가 도망친 자리에서 그대로 일을 재개하지 않게 하며, 도착하거나 막히면 Idle로 넘긴다.
    /// </summary>
    public sealed class NpcReturnState : NpcActorState
    {
        public override ActorStateId StateId => ActorStateId.NpcReturn;

        private const float ArrivalDistance = 0.6f;
        private const float WalkSpeedScale = 0.8f;
        private const float StuckCheckInterval = 0.5f;
        private const float StuckMinDistance = 0.15f;
        private const int MaxStuckChecks = 3;

        private readonly NpcCombatReaction _reaction;
        private IDisposable _routineLease;
        private Vector3 _lastCheckPosition;
        private float _stuckCheckTimer;
        private int _stuckChecks;

        public NpcReturnState(NpcMovementController controller, NpcCombatReaction reaction) : base(controller)
        {
            _reaction = reaction;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _routineLease = npcActor.PauseRoutine();
            _lastCheckPosition = motor.TransientPosition;
            gameActor.Animator?.PlayMotion(Motions.Walk, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            _routineLease?.Dispose();
            _routineLease = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (npcActor.IsInteracting() || npcActor.IsDialogueStaged)
            {
                _reaction.ClearOrigin();
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            if (HasArrived() || IsStuck(deltaTime))
            {
                _reaction.ClearOrigin();
                npcController.TransitionToState(new NpcIdleState(npcController));
            }
        }

        private bool HasArrived() => HorizontalDistance(motor.TransientPosition, _reaction.OriginPosition) <= ArrivalDistance;

        private bool IsStuck(float deltaTime)
        {
            _stuckCheckTimer += deltaTime;
            if (_stuckCheckTimer < StuckCheckInterval) return false;
            _stuckCheckTimer = 0f;

            bool isMoving = HorizontalDistance(motor.TransientPosition, _lastCheckPosition) >= StuckMinDistance;
            _lastCheckPosition = motor.TransientPosition;
            _stuckChecks = isMoving ? 0 : _stuckChecks + 1;
            return _stuckChecks >= MaxStuckChecks;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 direction = GetDirectionToOrigin();
            if (direction.sqrMagnitude < 0.001f) return;
            currentRotation = Quaternion.Slerp(
                currentRotation,
                Quaternion.LookRotation(direction, motor.CharacterUp),
                1f - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Project(currentVelocity, motor.CharacterUp);
                return;
            }

            Vector3 direction = motor.GetDirectionTangentToSurface(
                GetDirectionToOrigin(), motor.GroundingStatus.GroundNormal);
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                direction * (npcController.MaxWalkMoveSpeed * WalkSpeedScale),
                1f - Mathf.Exp(-npcController.StableMovementSharpness * deltaTime));
        }

        private Vector3 GetDirectionToOrigin()
        {
            Vector3 direction = _reaction.OriginPosition - motor.TransientPosition;
            direction.y = 0f;
            return direction.normalized;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
