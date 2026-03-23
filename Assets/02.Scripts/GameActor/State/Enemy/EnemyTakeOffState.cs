using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 지상 → 공중 전환 State.
    /// - Fly_Start 애니메이션 재생
    /// - 전 구간 하이퍼아머 ON
    /// - 애니메이션 종료 시 EnemyAerialState로 자동 전환
    /// </summary>
    public class EnemyTakeOffState : GameActorState
    {
        public override string StateName => "TakeOff";

        private readonly AerialBehaviorLayer _aerialLayer;
        private bool _animDone;
        private bool _liftedOff; // MotionEvent_TakeOffLift 발생 여부

        public EnemyTakeOffState(ActorMovementController controller, AerialBehaviorLayer aerialLayer)
            : base(controller)
        {
            _aerialLayer = aerialLayer;
        }

        public override bool CanTransitionState(string stateName)
        {
            // 하이퍼아머 — Hit 막음. Death 만 허용
            return stateName == "Death";
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _animDone  = false;
            _liftedOff = false;

            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);

            var anim = gameActor.Animator.PlayMotion(AnimKey.Fly_Start, 0.15f);
            if (anim != null)
                gameActor.Animator.OnMotionSetCompleted += OnAnimEnd;
            else
                OnAnimEnd(); // 클립 없으면 즉시 진행
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            gameActor.Animator.OnMotionSetCompleted -= OnAnimEnd;
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);
        }

        public override void UpdateState(float deltaTime)
        {
            // LiftOff 없이 애니가 끝나는 경우(클립 없음)에도 GroundSolving 해제 보장
            if (_animDone)
            {
                if (!_liftedOff) motor.SetGroundSolvingActivation(false);
                controller.TransitionToState(new EnemyAerialState(controller, _aerialLayer));
            }
        }

        /// <summary>
        /// MotionEvent_TakeOffLift에서 호출 — 발이 지면을 떠나는 프레임
        /// KCC GroundSolving 해제 + 공중 물리 모드 전환
        /// </summary>
        public void OnLiftOff()
        {
            if (_liftedOff) return;
            _liftedOff = true;
            motor.SetGroundSolvingActivation(false);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_liftedOff)
            {
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
                return;
            }

            // 목표 고도 = 스폰Y + minHoverHeight (절대값 기준)
            float targetY   = _aerialLayer.SpawnY + _aerialLayer.Data.minHoverHeight;
            float diff      = targetY - motor.TransientPosition.y;
            float liftSpeed = Mathf.Clamp(diff * 4f, 0.5f, 8f); // 최소 0.5 보장 — 이미 도달해도 미세 상승

            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 5f);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 5f);
            currentVelocity.y = liftSpeed;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        private void OnAnimEnd() => _animDone = true;
    }
}
