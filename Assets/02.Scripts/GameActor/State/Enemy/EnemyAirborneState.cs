using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class EnemyAirborneState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Airborne;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private const float INITIAL_GROUNDING_GRACE = 0.2f;
        private const float MAXIMUM_AIRBORNE_DURATION = 15f;
        private const float FALLBACK_LAND_TIMEOUT = 2f;
        private const float MINIMUM_PLAY_RATE = 0.5f;
        private const float MOTION_COMPLETION_GRACE = 0.25f;

        private bool _isActive;
        private bool _landStarted;
        private bool _hasLeftGround;
        private float _dragSpeed = 0.1f;
        private float _airborneTimer;
        private float _landTimer;
        private float _landTimeout;
        private MotionSet _landMotionSet;

        public EnemyAirborneState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTags.State_Airborne);

            _isActive = true;
            _landStarted = false;
            _hasLeftGround = false;
            _dragSpeed = controller.Drag;
            _airborneTimer = 0f;
            _landTimer = 0f;
            _landTimeout = FALLBACK_LAND_TIMEOUT;
            _landMotionSet = null;

            // 진입 시 수직 속도가 양수면 상승(점프), 음수면 낙하 애니메이션
            float verticalSpeed = Vector3.Dot(controller.PredictedVelocity, motor.CharacterUp);
            if (verticalSpeed > 0f)
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Jump, 0.05f);
            else
                gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fall, 0.2f);
        }

        public override void OnExit(GameActorState state)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnLandMotionEnded;
            _isActive = false;
            _landMotionSet = null;
            gameActor.Tags?.RemoveTag(GameplayTags.State_Airborne);
            base.OnExit(state);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            _airborneTimer += deltaTime;

            if (_landStarted)
            {
                _landTimer += deltaTime;
                if (_landTimer >= _landTimeout)
                {
                    Debug.LogWarning(
                        $"[EnemyAirborneState] 착지 Motion 종료 신호가 없어 강제 복귀합니다. " +
                        $"actor={gameActor.name}, timeout={_landTimeout:0.00}s",
                        gameActor);
                    ChangeToNextState();
                }

                return;
            }

            // PostGroundingUpdate의 접지 전환을 놓친 경우에도 착지 경로를 복구한다.
            if (_hasLeftGround && motor.GroundingStatus.IsStableOnGround)
            {
                OnLanded();
                return;
            }

            // KCC grounding 지연을 넘겼는데 실제로 이륙하지 않았다면 잘못된 Airborne 진입이다.
            if (!_hasLeftGround
                && motor.GroundingStatus.IsStableOnGround
                && _airborneTimer >= INITIAL_GROUNDING_GRACE)
            {
                ChangeToNextState();
                return;
            }

            if (_airborneTimer >= MAXIMUM_AIRBORNE_DURATION)
            {
                Debug.LogWarning(
                    $"[EnemyAirborneState] 최대 공중 체류 시간을 초과해 BT 차단을 해제합니다. " +
                    $"actor={gameActor.name}, timeout={MAXIMUM_AIRBORNE_DURATION:0.00}s",
                    gameActor);
                ChangeToNextState();
            }
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                // 가변 중력: 상승/하강 구분
                float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                float gravityMultiplier = verticalSpeed < 0f
                    ? controller.FallGravityMultiplier
                    : controller.RiseGravityMultiplier;

                currentVelocity += gravityMultiplier * deltaTime * controller.Gravity;
            }

            // Drag
            currentVelocity *= (1f / (1f + (_dragSpeed * deltaTime)));
        }


        public override void PostGroundingUpdate(float deltaTime)
        {
            // 실제로 지면을 떠난 시점 기록
            if (!_hasLeftGround && !motor.GroundingStatus.IsStableOnGround)
            {
                _hasLeftGround = true;
            }

            // 착지 감지
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }
        private void ChangeToNextState()
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            _isActive = false;
            gameActor.Animator.OnMotionSetEndedWithReason -= OnLandMotionEnded;
            controller.TransitionToState(ActorStateId.Idle);
        }

        private void OnLanded()
        {
            if (!_isActive || _landStarted || controller.CurrentState != this)
                return;

            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
                _landMotionSet = gameActor.Animator.CurrentMotionSet;
                _landTimer = 0f;

                float motionDuration = _landMotionSet?.TotalDuration ?? 0f;
                if (motionDuration > 0f)
                {
                    _landTimeout = Mathf.Max(
                        FALLBACK_LAND_TIMEOUT,
                        motionDuration / MINIMUM_PLAY_RATE + MOTION_COMPLETION_GRACE);
                }

                gameActor.Animator.OnMotionSetEndedWithReason += OnLandMotionEnded;
            }
            else
            {
                // 애니메이션이 없거나 찾을 수 없는 경우 즉시 다음 상태로
                ChangeToNextState();
            }
        }

        private void OnLandMotionEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (_landMotionSet != null && ReferenceEquals(motionSet, _landMotionSet))
                ChangeToNextState();
        }
    }
}
