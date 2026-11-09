using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using Motions = UPlayGround.Data.Actor.Animation.MotionTags;

namespace UPlayGround.State
{
    /// <summary>
    /// 플레이어에게 세게 부딪힌 NPC가 밀려난 뒤 자세를 회복하는 상태.
    /// 밀림은 기존 KCC 넉백 채널에 맡기고, 고정 위치 NPC는 회복 뒤 원래 자리로 걸어 돌아간다.
    /// </summary>
    public sealed class NpcBumpState : NpcActorState
    {
        public override ActorStateId StateId => ActorStateId.NpcBump;

        private const float MotionFadeSeconds = 0.15f;
        private const float ReactionFadeSeconds = 0.05f;
        private const float ReturnArrivalDistance = 0.15f;
        private const float ReturnWalkScale = 0.7f;
        private const float ReturnTimeoutSeconds = 3f;

        private readonly NpcBumpReceiver _receiver;
        private IDisposable _routineLease;
        private NpcBrain _brain;
        private Vector3 _originPosition;
        private Vector3 _towardBumper;
        private float _elapsed;
        private bool _shouldReturn;
        private bool _hasPlayedRecoverIdle;

        public NpcBumpState(NpcMovementController controller, NpcBumpReceiver receiver) : base(controller)
        {
            _receiver = receiver;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        private bool IsRecovering => _elapsed < _receiver.Profile.recoverSeconds;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _routineLease = npcActor.PauseRoutine();
            _brain = npcActor.GetComponent<NpcBrain>();
            _originPosition = motor.TransientPosition;
            _towardBumper = -_receiver.LastPushDirection;
            _shouldReturn = _brain != null && !_brain.EnableWander
                && (npcController.Routine == null || !npcController.Routine.HasMovementControl);
            _elapsed = 0f;

            PlayReactionMotion();
            var profile = _receiver.Profile;
            float pushSpeed = NpcBumpResolver.GetPushSpeed(profile, _receiver.LastTier);
            npcController.AddPlanarKnockback(_receiver.LastPushDirection * pushSpeed, profile.pushDrag);
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
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            _elapsed += deltaTime;
            if (IsRecovering) return;

            if (!_hasPlayedRecoverIdle)
            {
                _hasPlayedRecoverIdle = true;
                gameActor.Animator?.PlayMotion(_shouldReturn ? Motions.Walk : Motions.Idle, MotionFadeSeconds);
            }

            bool isReturning = _shouldReturn && _elapsed < _receiver.Profile.recoverSeconds + ReturnTimeoutSeconds
                && HorizontalDistance(motor.TransientPosition, _originPosition) > ReturnArrivalDistance;
            if (!isReturning)
                npcController.TransitionToState(new NpcIdleState(npcController));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 부딪힌 순간에는 놀라 상대 쪽을 보고, 복귀 중에는 가는 방향을 본다.
            if (_elapsed < _receiver.Profile.turnDelaySeconds) return;
            Vector3 direction = IsRecovering ? _towardBumper : GetDirectionToOrigin();
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

            // 회복 중에는 넉백 채널이 속도를 합성하므로 상태는 정지 속도만 낸다.
            if (IsRecovering || !_shouldReturn)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 direction = motor.GetDirectionTangentToSurface(
                GetDirectionToOrigin(), motor.GroundingStatus.GroundNormal);
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                direction * (npcController.MaxWalkMoveSpeed * ReturnWalkScale),
                1f - Mathf.Exp(-npcController.StableMovementSharpness * deltaTime));
        }

        /// <summary>전용 피격 모션이 있으면 재생하고, 없으면 Idle을 유지해 절차 연출만 쓴다.</summary>
        private void PlayReactionMotion()
        {
            var animator = gameActor.Animator;
            if (animator == null) return;

            if (_receiver.HasMotion(_receiver.LastMotionTag))
                animator.PlayMotion(_receiver.LastMotionTag, ReactionFadeSeconds);
            else
                animator.PlayMotion(Motions.Idle, MotionFadeSeconds);
        }

        private Vector3 GetDirectionToOrigin()
        {
            Vector3 direction = _originPosition - motor.TransientPosition;
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
