using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using Motions = UPlayGround.Data.Actor.Animation.MotionTags;

namespace UPlayGround.State
{
    /// <summary>
    /// 주변 전투에 대한 NPC 반응 상태. 단계(Watch/Cower/Flee)는 <see cref="NpcCombatReaction"/>이 BT 결정으로 갱신하며,
    /// 이 상태는 그 단계를 몸으로 표현하고 단계가 None이 되면 Idle(→일과)로 돌아간다.
    /// </summary>
    public sealed class NpcReactState : NpcActorState
    {
        public override ActorStateId StateId => ActorStateId.NpcReact;

        private const float MotionFadeSeconds = 0.2f;
        private const float StuckCheckInterval = 0.25f;
        private const float StuckMinDistance = 0.1f;

        private NpcCombatReaction _reaction;
        private IDisposable _routineLease;
        private NpcReactionLevel _appliedMotionLevel;
        private bool _hasFinishedFlee;
        private float _stuckTimer;
        private float _stuckCheckTimer;
        private Vector3 _lastCheckPosition;

        public NpcReactState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _reaction = npcActor.GetComponent<NpcCombatReaction>();
            _routineLease = npcActor.PauseRoutine();
            _lastCheckPosition = motor.TransientPosition;
            ApplyMotion(EffectiveLevel);
        }

        public override void OnExit(GameActorState toState)
        {
            _routineLease?.Dispose();
            _routineLease = null;
            base.OnExit(toState);
        }

        /// <summary>도주를 마쳤거나 막힌 경우 Flee는 웅크림으로 대체된다.</summary>
        private NpcReactionLevel EffectiveLevel =>
            _reaction.Level == NpcReactionLevel.Flee && _hasFinishedFlee ? NpcReactionLevel.Cower : _reaction.Level;

        public override void UpdateState(float deltaTime)
        {
            if (npcActor.IsInteracting() || npcActor.IsDialogueStaged)
            {
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            if (_reaction == null || !_reaction.IsReacting)
            {
                // 도망친 NPC는 일과·고정 작업이 시작되기 전에 원래 자리로 돌아간다.
                GameActorState next = _reaction != null && _reaction.HasOrigin
                    ? new NpcReturnState(npcController, _reaction)
                    : new NpcIdleState(npcController);
                npcController.TransitionToState(next);
                return;
            }

            if (_reaction.Level != NpcReactionLevel.Flee)
                _hasFinishedFlee = false;
            else if (!_hasFinishedFlee)
                CheckFleeFinished(deltaTime);

            ApplyMotion(EffectiveLevel);
        }

        private void CheckFleeFinished(float deltaTime)
        {
            if (HorizontalDistance(motor.TransientPosition, _reaction.ThreatPosition) >= _reaction.Profile.fleeSafeDistance)
            {
                _hasFinishedFlee = true;
                return;
            }

            _stuckCheckTimer += deltaTime;
            if (_stuckCheckTimer < StuckCheckInterval) return;

            bool isMoving = HorizontalDistance(motor.TransientPosition, _lastCheckPosition) >= StuckMinDistance;
            _stuckTimer = isMoving ? 0f : _stuckTimer + _stuckCheckTimer;
            _stuckCheckTimer = 0f;
            _lastCheckPosition = motor.TransientPosition;
            if (_stuckTimer >= _reaction.Profile.fleeStuckSeconds)
                _hasFinishedFlee = true;
        }

        private void ApplyMotion(NpcReactionLevel level)
        {
            if (level == _appliedMotionLevel) return;
            _appliedMotionLevel = level;

            var animator = gameActor.Animator;
            if (animator == null) return;

            GameplayTag tag = level switch
            {
                NpcReactionLevel.Flee => animator.HasMotion(Motions.Run) ? Motions.Run : Motions.Walk,
                NpcReactionLevel.Cower => animator.HasMotion(Motions.Crouch_Idle) ? Motions.Crouch_Idle : Motions.Idle,
                _ => Motions.Idle,
            };
            animator.PlayMotion(tag, MotionFadeSeconds);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_reaction == null) return;

            Vector3 direction = _appliedMotionLevel == NpcReactionLevel.Flee
                ? GetAwayDirection()
                : GetThreatDirection();
            if (direction.sqrMagnitude < 0.001f) return;

            currentRotation = Quaternion.Slerp(
                currentRotation,
                Quaternion.LookRotation(direction, motor.CharacterUp),
                1f - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 공중이면 수평 추진 없이 낙하 속도만 유지한다(일과 상태와 같은 규약).
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Project(currentVelocity, motor.CharacterUp);
                return;
            }

            if (_reaction == null || _appliedMotionLevel != NpcReactionLevel.Flee)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            float speed = npcController.MaxRunMoveSpeed * _reaction.Profile.fleeSpeedMultiplier;
            Vector3 direction = motor.GetDirectionTangentToSurface(GetAwayDirection(), motor.GroundingStatus.GroundNormal);
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                direction * speed,
                1f - Mathf.Exp(-npcController.StableMovementSharpness * deltaTime));
        }

        private Vector3 GetThreatDirection()
        {
            Vector3 direction = _reaction.ThreatPosition - motor.TransientPosition;
            direction.y = 0f;
            return direction.normalized;
        }

        /// <summary>위협과 겹쳐 방향이 없으면 지금 바라보는 방향으로 달아난다.</summary>
        private Vector3 GetAwayDirection()
        {
            Vector3 direction = -GetThreatDirection();
            return direction.sqrMagnitude > 0.001f ? direction : motor.CharacterForward;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
