using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 → 지상 착지 State.
    ///
    /// </summary>
    public class EnemyLandState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Land;
        public override bool BlocksBehaviorTree => true;

        private bool _groundSnapRestored;
        private bool _landAnimDone;

        private const float GroundProximity = 0.9f;

        public EnemyLandState(ActorMovementController controller)
            : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
            => fromState is ActorStateId.Hit or ActorStateId.Death;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _groundSnapRestored = false;
            _landAnimDone       = false;

            motor.SetGroundSolvingActivation(false);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            motor.SetGroundSolvingActivation(true);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_landAnimDone)
            {
                controller.TransitionToState(new EnemyChaseState(
                    controller,
                    gameActor.GetComponent<EnemyAIContext>(),
                    gameActor.GetComponent<EnemyDetection>()));
                return;
            }

            if (!_groundSnapRestored)
                CheckGroundProximity();
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_groundSnapRestored && motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, deltaTime * 6f);
                return;
            }

            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 5f);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 5f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void CheckGroundProximity()
        {
            Vector3 origin = motor.TransientPosition + Vector3.up * 0.1f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    GroundProximity + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                return;
            if (hit.distance > GroundProximity) return;

            OnNearGround();
        }

        private void OnNearGround()
        {
            _groundSnapRestored = true;
            motor.SetGroundSolvingActivation(true);

            _landAnimDone = true;
        }
    }
}
