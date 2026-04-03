using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;
using UPlayGround.Data.Enemy;

namespace UPlayGround.State
{
    public class EnemyAttackState : GameActorState
    {
        public override string StateName => "Attack";

        private EnemyCombat _combat;
        private EnemyBrain _brain;
        private EnemyDetection _detection;

        private EnemyAttackInfo _currentSkill;
        private float _attackTimer;
        private bool _isAttackActive;

        // --- Motion Warp ---
        private Transform _warpTarget;
        private bool _isWarping;
        private float _warpStartDistance;
        private const float SweetSpotMultiplier = 0.8f;
        
        public EnemyAttackState(ActorMovementController controller, EnemyCombat combat, EnemyBrain brain, EnemyDetection detection) : base(controller)
        {
            _combat = combat;
            _brain = brain;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit")
                return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            _attackTimer = 0f;
            _isAttackActive = true;
            
            // 공격 모션 진입 → Hyper Armor 활성화
            gameActor.GetComponent<UPlayGround.Component.PoiseStat>()?.SetHyperArmor(true);
            
            // 거리 기반 스킬 선택
            float distanceToTarget = _detection.DistanceToTarget;
            _currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);

            if (_currentSkill != null)
            {
                // 공격 애니메이션 재생
                var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
                if (animState != null)
                {
                    gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
                }
                else
                {
                    Debug.LogWarning($"[EnemyAttackState] 애니메이션을 찾을 수 없습니다: {_currentSkill.baseInfo.animKey}");
                    OnAttackAnimationEnd();
                }

                // 모션 워핑 시도
                TryInitWarp();
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
            _combat.ClearHitTargets();

            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;

            // 공격 모션 종료 → Hyper Armor 해제
            gameActor.GetComponent<UPlayGround.Component.PoiseStat>()?.SetHyperArmor(false);

            // 그룹 슬롯 반환
            _brain.ReleaseGroupSlot();

            ClearWarpState();
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isAttackActive || _currentSkill == null)
                return;

            _attackTimer += deltaTime;

            // 워핑 종료 조건 체크
            UpdateWarpState();

            // 근접 공격 히트 체크
            if (_currentSkill.baseInfo.attackType == AttackType.Melee && _combat.IsPossibleCollide)
            {
                _combat.CheckMeleeAttackHit();
            }
        }

        private void OnAttackAnimationEnd()
        {
            Debug.Log("OnAttackAnimationEnd");
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            if (!_isAttackActive)
                return;
            
            _combat.ClearHitTargets();
            TransitionToNextState();
        }

        private void TransitionToNextState()
        {
            bool didHit = _combat.LastHitCount > 0;
            _brain.DecidePostAttack(didHit);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 호밍: 워핑 타겟이 있으면 공격 전반부에서 타겟을 향해 회전
            if (_warpTarget != null)
            {
                Vector3 dirToTarget = _warpTarget.position - motor.TransientPosition;
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);

                    // Startup(0.15초): 빠르게 보정 → 이후: 무게감 있게 감속
                    float rotSpeed = _attackTimer < 0.15f ? 25f : 8f;

                    // 히트 판정 시작 이후에는 호밍 종료
                    if (!_combat.IsPossibleCollide)
                    {
                        currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * rotSpeed);
                        currentRotation = currentRotation.normalized;
                        return;
                    }
                }
            }
            else if (_detection.HasTarget && _attackTimer < 0.3f)
            {
                Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                directionToTarget.y = 0;

                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }

            currentRotation = currentRotation.normalized;
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
                Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;

                // 모션 워핑: 타겟을 향해 EaseOut 이동
                if (_isWarping && _warpTarget != null)
                {
                    Vector3 toTarget = _warpTarget.position - motor.TransientPosition;
                    toTarget.y = 0f;
                    float currentDist = toTarget.magnitude;
                    float sweetSpotDist = _currentSkill.maxRange * SweetSpotMultiplier;
                    float distToTravel = currentDist - sweetSpotDist;

                    if (distToTravel > 0.01f)
                    {
                        Vector3 warpDir = toTarget.normalized;

                        float progress = (_warpStartDistance > 0.01f)
                            ? Mathf.Clamp01(1f - distToTravel / _warpStartDistance)
                            : 1f;
                        float easedSpeed = Mathf.Lerp(_combat.WarpMoveSpeed, 0f, EaseOut(progress));

                        float rootMotionBlend = Mathf.Clamp01(progress);
                        Vector3 warpVel = warpDir * easedSpeed;
                        Vector3 finalVel = Vector3.Lerp(warpVel, rootMotionVel, rootMotionBlend);

                        float dot = Vector3.Dot(rootMotionVel.normalized, warpDir);
                        if (dot > 0.5f && rootMotionVel.magnitude > 0.1f)
                        {
                            finalVel = Vector3.Max(finalVel, rootMotionVel);
                        }

                        currentVelocity = new Vector3(finalVel.x, 0f, finalVel.z);
                    }
                    else
                    {
                        currentVelocity = rootMotionVel;
                    }
                }
                else
                {
                    currentVelocity = rootMotionVel;
                }
            }

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

        #region Motion Warp

        private void TryInitWarp()
        {
            ClearWarpState();

            if (_currentSkill == null || _currentSkill.baseInfo.attackType == AttackType.Ranged)
                return;

            if (!_detection.HasTarget)
                return;

            _warpTarget = _detection.CurrentTarget;
            float dist = HorizontalDistance(motor.TransientPosition, _warpTarget.position);
            float sweetSpotDist = _currentSkill.maxRange * SweetSpotMultiplier;

            // 스윗 스팟 안쪽이면 이동 워핑 불필요, 호밍(회전)만 적용
            if (dist > sweetSpotDist)
            {
                _isWarping = true;
                _warpStartDistance = dist - sweetSpotDist;
            }
        }

        private void UpdateWarpState()
        {
            if (!_isWarping)
                return;

            if (_warpTarget == null || _combat.IsPossibleCollide)
            {
                _isWarping = false;
                return;
            }

            float dist = HorizontalDistance(motor.TransientPosition, _warpTarget.position);
            float sweetSpotDist = _currentSkill.maxRange * SweetSpotMultiplier;

            if (dist <= sweetSpotDist)
                _isWarping = false;
        }

        private void ClearWarpState()
        {
            _warpTarget = null;
            _isWarping = false;
            _warpStartDistance = 0f;
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        #endregion
    }
}