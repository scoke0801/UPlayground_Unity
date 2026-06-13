using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 공격 상태 — 루트모션 기반 Motion Warp
    ///
    /// [이동 로직]
    ///   타겟 있음 + IsMotionWarping: WarpRemainingTime 기반으로 속력을 역산해 타겟 방향으로 이동.
    ///   그 외: 루트모션 원본 그대로 적용.
    ///
    /// [워프 구간 지정]
    ///   공격 MotionSet 타임라인에 MotionEvent_MotionWarp 이벤트 추가.
    ///   endTime을 Collision 이벤트 startTime 직전으로 맞추면 된다.
    /// </summary>
    public class EnemyAttackState : EnemyActorState
    {
        public const string StateNameValue = "Attack";

        public override string StateName => StateNameValue;
        public override bool BlocksBehaviorTree => true;

        private EnemyCombat    _combat;
        private EnemyAIContext _context;
        private EnemyDetection _detection;

        private EnemyAttackInfo _currentSkill;
        private float           _attackTimer;
        private bool            _isAttackActive;

        // 호밍 타겟 (Motion Warp + 회전 보정 공통)
        private Transform _homingTarget;
        private MotionWarpController _motionWarp;

        public EnemyAttackState(ActorMovementController controller, EnemyCombat combat, EnemyAIContext context, EnemyDetection detection)
            : base(controller)
        {
            _combat    = combat;
            _context   = context;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
            return true;
        }

        public override bool CanPlayHitReaction(AttackData attackData)
        {
            return base.CanPlayHitReaction(attackData)
                   && _combat != null
                   && !_combat.IsPossibleCollide;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _attackTimer    = 0f;
            _isAttackActive = true;
            _motionWarp     = controller.MotionWarp;

            gameActor.GetComponent<UPlayGround.Component.PoiseStat>()?.SetHyperArmor(true);
            ActorWeaponTrailController.StartAttackTrails(gameActor);

            float distanceToTarget = _detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);

            if (_currentSkill != null)
            {
                var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
                if (!_currentSkill.useMotionEventTelegraph)
                    _combat.BeginCurrentSkillTelegraph();

                if (animState != null)
                    gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
                else
                {
                    Debug.LogWarning($"[EnemyAttackState] 애니메이션을 찾을 수 없습니다: {_currentSkill.baseInfo.animKey}");
                    OnAttackAnimationEnd();
                }

                // 근접 공격에만 타겟 잠금
                if (_currentSkill.baseInfo.attackType == AttackType.Melee)
                {
                    _homingTarget = _detection.HasTarget ? _detection.CurrentTarget : null;
                    _motionWarp.SetTarget(_homingTarget);
                }
            }
            else
            {
                Debug.LogWarning("[EnemyAttackState] 사용 가능한 스킬이 없습니다!");
                TransitionToNextState();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);

            _isAttackActive = false;
            _homingTarget   = null;
            _motionWarp?.ClearTarget();
            _combat.CancelCurrentAction();
            ActorWeaponTrailController.StopAttackTrails(gameActor);
            gameActor.Animator.Speed = gameActor.LocalTimeScale;

            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;
            gameActor.GetComponent<UPlayGround.Component.PoiseStat>()?.SetHyperArmor(false);
            _context.ReleaseGroupSlot();
        }

        public override void UpdateState(float deltaTime)
        {
            if (TryTransitionToAirborne(deltaTime))
                return;

            if (!_isAttackActive || _currentSkill == null) return;

            _attackTimer += deltaTime;
            _combat.UpdateTelegraphs();

            if (_currentSkill.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
                _combat.CheckMeleeAttackHit();
        }

        private void OnAttackAnimationEnd()
        {
            if (TryTransitionToAirborne(gameActor != null ? gameActor.DeltaTime : Time.deltaTime))
                return;

            if (!_isAttackActive) return;

            _combat.ClearHitTargets();
            TransitionToNextState();
        }

        private bool TryTransitionToAirborne(float deltaTime)
        {
            if (!ShouldTransitionToAirborne(deltaTime))
                return false;

            controller.TransitionToState(new EnemyAirborneState(controller));
            return true;
        }

        private void TransitionToNextState()
        {
            bool didHit = _combat.LastHitCount > 0;
            _context.DecidePostAttack(didHit);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            float lastVerticalVelocity = currentVelocity.y;

            if (_currentSkill != null && _currentSkill.baseInfo.attackType == AttackType.Ranged)
            {
                currentVelocity = Vector3.zero;
            }
            else
            {
                // 워프 구간에서 클립 재생 속도를 타겟 거리 비율로 보정해 풋슬라이딩 감소.
                float playbackScale = _combat.IsMotionWarping
                    ? _motionWarp.WarpPlayRateScale
                    : 1f;
                gameActor.Animator.Speed = playbackScale * gameActor.LocalTimeScale;

                Vector3 rootVelocity = gameActor.Animator.DeltaPosition / deltaTime;
                currentVelocity = _motionWarp.EvaluateVelocity(
                    rootVelocity,
                    motor.TransientPosition,
                    _combat.IsMotionWarping,
                    _combat.WarpRemainingTime,
                    _combat.WarpDuration,
                    _combat.WarpMinDistance,
                    _combat.WarpMaxDistance,
                    _combat.WarpMaxSpeed,
                    deltaTime,
                    _combat.EndMotionWarpAction);

                currentVelocity = _motionWarp.ClampApproachVelocity(
                    currentVelocity,
                    motor.TransientPosition,
                    deltaTime);
            }

            // Y축 복원 (중력/점프 보존)
            currentVelocity.y = lastVerticalVelocity;

            if (motor.GroundingStatus.IsStableOnGround)
            {
                if (currentVelocity.y < 0) currentVelocity.y = -0.1f;
            }
            else
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 워프 구간: rotationCurve 기반 곡선 보간으로 타겟 방향 정렬.
            if (_motionWarp.TryEvaluateRotation(
                    currentRotation,
                    motor.TransientPosition,
                    _combat.IsMotionWarping,
                    _combat.WarpRemainingTime,
                    _combat.WarpDuration,
                    _combat.WarpMinDistance,
                    _combat.WarpMaxDistance,
                    _combat.WarpMaxSpeed,
                    out Quaternion warpRotation))
            {
                currentRotation = warpRotation;
                return;
            }
            else if (_detection.HasTarget && _attackTimer < 0.3f)
            {
                // 워프 없을 때 공격 초반 0.3초는 타겟 방향 유지
                Vector3 dir = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    currentRotation = Quaternion.Slerp(
                        currentRotation, targetRot,
                        1f - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }

            currentRotation = currentRotation.normalized;
        }
    }
}
