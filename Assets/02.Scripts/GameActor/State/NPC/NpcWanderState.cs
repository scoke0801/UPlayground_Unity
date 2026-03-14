using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 배회 상태.
    /// EnemyPatrolState 구조를 그대로 따르되 전투 로직은 제거합니다.
    /// </summary>
    public class NpcWanderState : NpcActorState
    {
        public override string StateName => "Wander";

        private NpcBrain _brain;

        private Vector3 _targetPosition;
        private float   _waitTimer;
        private bool    _isWaiting;

        private float   _stuckTimer;
        private Vector3 _lastPosition;
        private int     _retryCount;

        private const float ARRIVAL_THRESHOLD       = 0.6f;
        private const float STUCK_CHECK_INTERVAL    = 0.5f;
        private const float STUCK_DISTANCE_THRESHOLD = 0.15f;
        private const int   MAX_RETRY               = 3;

        public NpcWanderState(NpcMovementController controller, NpcBrain brain) : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _isWaiting  = false;
            _waitTimer  = 0f;
            _stuckTimer = 0f;
            _retryCount = 0;
            _lastPosition = motor.TransientPosition;

            SetNewWanderPoint();
            gameActor.Animator.PlayMotion(AnimKey.Walk, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 대화 시작 시 즉시 TalkState로 전환
            if (npcActor.IsInteracting())
            {
                npcController.TransitionToState(new NpcTalkState(npcController));
                return;
            }

            if (_isWaiting)
            {
                _waitTimer += deltaTime;
                if (_waitTimer >= _brain.PatrolWaitTime)
                    ExitWait();
            }
            else
            {
                float dist = HorizontalDistance(motor.TransientPosition, _targetPosition);
                if (dist <= ARRIVAL_THRESHOLD)
                {
                    EnterWait();
                    return;
                }

                _stuckTimer += deltaTime;
                if (_stuckTimer >= STUCK_CHECK_INTERVAL)
                {
                    CheckStuck();
                    _stuckTimer = 0f;
                }
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_isWaiting) return;

            Vector3 dir = (_targetPosition - motor.TransientPosition);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(dir.normalized);
            currentRotation = Quaternion.Slerp(
                currentRotation, target,
                1 - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround) return;

            if (_isWaiting)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 dir = (_targetPosition - motor.TransientPosition);
            dir.y = 0f;

            Vector3 targetVel = dir.normalized * (npcController.MaxWalkMoveSpeed * 0.6f);
            targetVel = motor.GetDirectionTangentToSurface(
                targetVel, motor.GroundingStatus.GroundNormal) * targetVel.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity, targetVel,
                1 - Mathf.Exp(-npcController.StableMovementSharpness * deltaTime));
        }

        // 이동 중 정면 충돌 시 새 지점으로
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_isWaiting) return;

            Vector3 moveDir = (_targetPosition - motor.TransientPosition).normalized;
            moveDir.y = 0f;
            if (Vector3.Dot(moveDir, hitNormal) < -0.35f)
            {
                _retryCount++;
                if (_retryCount >= MAX_RETRY) EnterWait();
                else SetNewWanderPoint();
            }
        }

        // ── 내부 헬퍼 ───────────────────────────────────────────────

        private void EnterWait()
        {
            _isWaiting  = true;
            _waitTimer  = 0f;
            _stuckTimer = 0f;
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
        }

        private void ExitWait()
        {
            _isWaiting  = false;
            _waitTimer  = 0f;
            _retryCount = 0;
            SetNewWanderPoint();
            gameActor.Animator.PlayMotion(AnimKey.Walk, 0.25f);
        }

        private void CheckStuck()
        {
            float moved = HorizontalDistance(motor.TransientPosition, _lastPosition);
            if (moved < STUCK_DISTANCE_THRESHOLD)
            {
                _retryCount++;
                if (_retryCount >= MAX_RETRY) EnterWait();
                else SetNewWanderPoint();
            }
            else
            {
                _retryCount = 0;
            }
            _lastPosition = motor.TransientPosition;
        }

        private void SetNewWanderPoint()
        {
            _targetPosition   = _brain.GetRandomWanderPoint();
            _targetPosition.y = motor.TransientPosition.y;
            _lastPosition     = motor.TransientPosition;
            _stuckTimer       = 0f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
