using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 → 지상 착지 State.
    ///
    /// [일반 착지]
    ///   Fly_Landing 애니메이션 재생 → MotionEvent_LandImpact 프레임에서 충격 판정
    ///   → 착지 완료 후 EnemyChaseState 전환
    ///
    /// [Dive Attack 착지]
    ///   isDiveAttack = true 스킬의 경우, landDescentSpeed로 고속 강하
    ///   → 착지 순간 충격 반경 × 1.5 범위 적용
    /// </summary>
    public class EnemyLandState : GameActorState
    {
        public override string StateName => "Land";

        private readonly AerialBehaviorLayer _aerialLayer;
        private readonly bool                _isDiveAttack;

        private bool _groundSnapRestored;
        private bool _landAnimDone;

        // MotionEvent_LandImpact에서 토글
        private bool _impactTriggered;

        private const float GroundProximity = 0.9f;

        public EnemyLandState(ActorMovementController controller, AerialBehaviorLayer aerialLayer)
            : base(controller)
        {
            _aerialLayer  = aerialLayer;
            _isDiveAttack = aerialLayer.HasPendingDiveAttack;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is "Hit" or "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _groundSnapRestored = false;
            _landAnimDone       = false;
            _impactTriggered    = false;

            motor.SetGroundSolvingActivation(false);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            motor.SetGroundSolvingActivation(true);
            _aerialLayer.OnLanded();
        }

        public override void UpdateState(float deltaTime)
        {
            if (_landAnimDone)
            {
                controller.TransitionToState(new EnemyChaseState(
                    controller,
                    gameActor.GetComponent<EnemyBrain>(),
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

            // Dive Attack은 스킬에 지정된 diveDescentSpeed 사용, 일반 착지는 landDescentSpeed
            float descentSpeed = _isDiveAttack && _aerialLayer.DiveAttackSkill != null
                ? _aerialLayer.DiveAttackSkill.diveDescentSpeed
                : _aerialLayer.Data.landDescentSpeed;

            currentVelocity.y = -descentSpeed;
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 5f);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 5f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        // ── MotionEvent 연동 ─────────────────────────────────────────

        /// <summary>
        /// MotionEvent_LandImpact에서 호출.
        /// 착지 충격 범위 내 플레이어에게 데미지 + 넉백 적용.
        /// </summary>
        public void OnLandImpact()
        {
            if (_impactTriggered) return;
            _impactTriggered = true;

            float radius = _aerialLayer.Data.landingImpactRadius
                           * (_isDiveAttack ? 1.5f : 1f);

            _aerialLayer.ApplyLandingImpact(motor.TransientPosition, radius);
        }

        /// <summary> Fly_Landing 애니메이션 종료 시 호출 </summary>
        public void OnLandAnimEnd() => _landAnimDone = true;

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

            var anim = gameActor.Animator.PlayMotion(AnimKey.Fly_Landing, 0.1f);
            if (anim != null)
                anim.OwnedEvents.OnEnd += () => _landAnimDone = true;
            else
                _landAnimDone = true;
        }
    }
}
