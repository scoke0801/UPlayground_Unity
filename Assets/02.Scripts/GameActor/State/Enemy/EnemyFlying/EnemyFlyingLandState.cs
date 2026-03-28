using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 일반 착지 (Dive가 아닌 부드러운 하강).
    /// 하강 → 지면 도달 → 착지 모션 → Brain.OnDiveLanded()로 루프 복귀.
    /// </summary>
    public class EnemyFlyingLandState : GameActorState
    {
        public override string StateName => "Flying_Land";
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingBrain _brain;

        private bool _groundReached;
        private float _timer;
        private float _descentSpeed;

        private const float LandingMotionDuration = 0.8f; // 착지 모션 대기
        private const float MaxDescentTime = 5f;           // 안전장치

        public EnemyFlyingLandState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _groundReached = false;
            _timer = 0f;
            _descentSpeed = 6f; // Dive(20)보다 훨씬 느린 부드러운 하강

            // 착지 준비 모션
            gameActor.Animator.PlayMotion(AnimKey.Fly_Landing, 0.2f);

            // GroundSolving을 켜야 지면 접촉을 감지할 수 있다
            motor.SetGroundSolvingActivation(true);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            motor.SetGroundSolvingActivation(true);
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            if (_groundReached)
            {
                // 착지 모션 대기 후 루프 복귀
                if (_timer >= LandingMotionDuration)
                {
                    _brain.OnDiveLanded(); // Dive/Land 공용 콜백
                }
                return;
            }

            // 지면 도달 체크
            if (motor.GroundingStatus.IsStableOnGround)
            {
                OnLanded();
                return;
            }

            // 안전장치
            if (_timer >= MaxDescentTime)
            {
                Debug.LogWarning("[FlyingLand] 하강 타임아웃");
                OnLanded();
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_groundReached)
            {
                // 착지 후 정지
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                if (motor.GroundingStatus.IsStableOnGround && currentVelocity.y < 0)
                    currentVelocity.y = -0.1f;
                return;
            }

            // 부드러운 하강 — 타겟 방향으로 약간 이동하면서 내려온다
            Vector3 descendVel = Vector3.down * _descentSpeed;

            if (_brain.Detection.HasTarget)
            {
                Vector3 toTarget = _brain.Detection.CurrentTarget.position - motor.TransientPosition;
                toTarget.y = 0;
                if (toTarget.magnitude > 2f)
                {
                    // 수평으로 살짝 접근 (착지 후 바로 Chase 거리 줄이기)
                    descendVel += toTarget.normalized * 2f;
                }
            }

            currentVelocity = Vector3.Lerp(currentVelocity, descendVel, deltaTime * 5f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_brain.Detection.HasTarget)
            {
                Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, target,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            currentRotation = currentRotation.normalized;
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            if (!_groundReached
                && motor.GroundingStatus.IsStableOnGround
                && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }

        private void OnLanded()
        {
            _groundReached = true;
            _timer = 0f; // 착지 모션 타이머 리셋

            gameActor.Animator.PlayMotion(AnimKey.Land, 0.15f);
            Debug.Log("[FlyingLand] 일반 착지 완료");
        }
    }
}
