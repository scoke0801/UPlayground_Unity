using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 보스 이륙.
    /// Fly_Start 모션 재생 → 수직 상승 → 모션 완료 시 Air_Circle로 전환.
    /// </summary>
    public class EnemyFlyingTakeOffState : EnemyActorState
    {
        public override string StateName => "Flying_TakeOff";
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.None; // 이륙 중 중력 무시

        private readonly EnemyFlyingAIContext _brain;
        private float _timer;
        private bool _motionDone;
        private float _targetHeight;

        private const float TakeOffDuration = 0.7f;

        private float Cfg_TakeOffDuration => _brain.FlyingSettings ? _brain.FlyingSettings.takeOffDuration : TakeOffDuration;
        private float Cfg_AscentSpringK => _brain.FlyingSettings ? _brain.FlyingSettings.ascentSpringK : 5f;
        private float Cfg_AscentMin => _brain.FlyingSettings ? _brain.FlyingSettings.ascentSpeedMin : 2f;
        private float Cfg_AscentMax => _brain.FlyingSettings ? _brain.FlyingSettings.ascentSpeedMax : 16f;

        public EnemyFlyingTakeOffState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName)
            => stateName is "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _timer = 0f;
            _motionDone = false;

            // 지면 판정 해제 + 강제 이탈 — 공중으로 떠야 하므로
            motor.SetGroundSolvingActivation(false);
            motor.ForceUnground();

            _targetHeight = motor.TransientPosition.y + _brain.AirHoverHeight;

            var animState = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fly_Start, 0.15f);
            if (animState != null)
                gameActor.Animator.OnMotionSetCompleted += OnMotionEnd;
            else
                _motionDone = true;

            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(true);
        }

        /// <summary> 매 물리 틱 직전에 ForceUnground — KCC가 재접지하는 것 방지 </summary>
        public override void BeforeCharacterUpdate(float deltaTime)
        {
            motor.ForceUnground();
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            gameActor.Animator.OnMotionSetCompleted -= OnMotionEnd;
            gameActor.GetComponent<PoiseStat>()?.SetHyperArmor(false);
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            // 모션 완료 or 시간 초과 시 Air_Circle 진입
            if (_motionDone || _timer >= Cfg_TakeOffDuration + 0.5f)
            {
                _brain.ResetAirCounters();
                controller.TransitionToState(
                    new EnemyFlyingAirCircleState(controller, _brain));
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 수직 상승 — 목표 고도까지 부드럽게
            float currentY = motor.TransientPosition.y;
            float diff = _targetHeight - currentY;
            float ascentSpeed = Mathf.Clamp(diff * Cfg_AscentSpringK, Cfg_AscentMin, Cfg_AscentMax);

            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 5f);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 5f);

            // 하강 속도 허용 안 함 — 이전 State의 음수 잔여 + 지면 스냅 방지
            currentVelocity.y = Mathf.Max(currentVelocity.y, 0f);
            currentVelocity.y = Mathf.Lerp(currentVelocity.y, ascentSpeed, deltaTime * 10f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 이륙 중 타겟 방향 유지
            if (_brain.Detection.HasTarget)
            {
                Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, target,
                        1 - Mathf.Exp(-controller.OrientationSharpness * 0.5f * deltaTime));
                }
            }
            currentRotation = currentRotation.normalized;
        }

        private void OnMotionEnd()
        {
            _motionDone = true;
        }
    }
}
