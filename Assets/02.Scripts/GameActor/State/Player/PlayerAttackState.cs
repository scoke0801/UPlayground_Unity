using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;
using UPlayGround.Manager;
using UPlayGround.InputDefine;

namespace UPlayGround.State
{
    /// <summary>
    /// 공격 상태
    /// - 콤보 입력, 히트 판정, 루트모션 이동 처리
    /// - Attack Snap: 적중하기 가장 좋은 '스윗 스팟(Sweet Spot)'으로 자연스럽게 접근 및 회전 보정
    /// </summary>
    public class PlayerAttackState : PlayerActorState
    {
        public override string StateName => "Attack";
        
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        
        private AttackData _currentAttack;
        private float _attackTimer;

        private bool _comboInputted;
        private bool _isHeavyAttack;
        
        private PlayerActorAnimator _playerActorAnimator;

        // --- Attack Snap ---
        private Transform _snapTarget;
        private bool _isSnapping;
        
        // 스냅 시작 시 '스윗 스팟'까지 남은 거리 (감속 비율 계산용)
        private float _snapStartTravelDistance; 
        
        // 타격하기 가장 좋은 이상적인 거리 비율 (사거리의 80% 지점)
        private const float SweetSpotMultiplier = 0.8f;

        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
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

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            
            _combat = playerActor.GetCombat();
            _combat.ResetCombo();
            
            _equipment = playerActor.GetPlayerEquipment();

            _isHeavyAttack = playerController.HasHeavyAttackInput();
            _attackTimer = 0f;

            if (_isHeavyAttack)
            {
                Transform finishTarget = _combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    controller.TransitionToState(new PlayerFinishAttackState(controller, finishTarget));
                    return;
                }
            }

            var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            }
            else
            {
                ChangeToNextState();
                return;
            }
            
            // Attack Snap 시도
            TryInitAttackSnap();
        }

        public override void OnExit(GameActorState toState)
        {
            _combat.ClearHitTargets();
            
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            
            ClearSnapState();
            
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;

            // 스냅 종료 조건 체크
            UpdateSnapState();

            if (_currentAttack.canBeInterrupted)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dodge) != null)
                {
                    controller.TransitionToState(new PlayerDodgeState(controller));
                    return;
                }
                
                if(InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Jump) != null)
                {    
                    controller.TransitionToState(new PlayerAirborneState(controller));
                    return;
                }

                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dash) != null)
                {
                    if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                    {
                        return;
                    }
                }
            }

            // 콤보 입력 체크
            if (_combat.CanCombo)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    _comboInputted = true;
                    _isHeavyAttack = false;
                    _combat.CloseComboWindow();
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    _comboInputted = true;
                    _isHeavyAttack = true;
                    _combat.CloseComboWindow();
                }
            }

            if (_combat.IsPossibleCollide == false && _comboInputted)
            {
                ChangeToNextState();
            }
        }
        
        private void ChangeToNextState()
        {
            _combat.ClearHitTargets();
            _attackTimer = 0f;
            
            _isHeavyAttack = playerController.HasHeavyAttackInput();
            
            if (_isHeavyAttack)
            {
                Transform finishTarget = _combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    controller.TransitionToState(new PlayerFinishAttackState(controller, finishTarget));
                    return;
                }
            }
            
            if (_comboInputted)
            {
                var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
                if (animState != null)
                {
                    // animState.OwnedEvents.OnEnd = ChangeToNextState;
                }
                
                _playerActorAnimator.IsOpenedComboWindow = false;
                
                _combat.CloseComboWindow();
                _comboInputted = false;
                
                // 콤보 연결 시 스냅 재시도
                TryInitAttackSnap();
            }
            else
            {
                _combat.ResetCombo();
                if (playerController.HasMoveInput())
                {
                    controller.TransitionToState(new PlayerGroundMoveState(controller));
                }
                else
                {
                    controller.TransitionToState(new PlayerIdleState(controller));
                }
            }
        }

        private AnimKey GetAnimKey()
        {
            var skillGauge = playerActor.SkillGauge;

            // 스킬
            for (int i = 0; i < 10; i++)
            {
                if (!playerController.HasSkillInput(i)) continue;

                if (skillGauge != null && !skillGauge.ConsumeSkill(i))
                {
                    Debug.Log($"[PlayerAttackState] Skill {i + 1} 게이지 부족");
                    continue;
                }

                _currentAttack = _combat.ExecuteSkillAttack(i);
                return _currentAttack?.animKey ?? AnimKey.None;
            }

            _currentAttack = _isHeavyAttack
                ? _combat.ExecuteHeavyAttack(_comboInputted)
                : _combat.ExecuteAttack(_comboInputted);

            return _currentAttack?.animKey ?? AnimKey.None;
        }

        #region Attack Snap (Target Magnetism)

        /// <summary>
        /// 공격 시작/콤보 연결 시 스냅 대상 탐색 및 초기화
        /// </summary>
        private void TryInitAttackSnap()
        {
            ClearSnapState();

            if (_currentAttack == null)
                return;

            Transform targetToSnap = null;
            float targetDistance = 0f;

            // 락온 대상 우선 체크
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(gameActor.transform.position, lockOnTarget.position);
                
                // 자석 탐색 범위 안이면 스냅 대상으로 설정
                if (dist <= _combat.SnapSearchRange)
                {
                    targetToSnap = lockOnTarget;
                    targetDistance = dist;
                }
            }

            // 락온 대상이 없으면 자석 탐색
            if (targetToSnap == null)
            {
                Transform snapCandidate = _combat.FindAttackSnapTarget(
                    _combat.SnapSearchRange, _currentAttack.hitAngle);

                if (snapCandidate != null)
                {
                    targetToSnap = snapCandidate;
                    targetDistance = HorizontalDistance(gameActor.transform.position, snapCandidate.position);
                }
            }

            if (targetToSnap != null)
            {
                float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;
                
                // 이미 스윗 스팟(이상적인 타격 거리) 안쪽에 있다면 스냅 불필요
                if (targetDistance <= sweetSpotDist)
                    return;

                BeginSnap(targetToSnap, targetDistance, sweetSpotDist);
            }
        }

        private void BeginSnap(Transform target, float currentDistance, float sweetSpotDist)
        {
            _snapTarget = target;
            _isSnapping = true;
            
            // 실제 이동해야 할 거리 (현재 거리 - 스윗 스팟 거리)
            _snapStartTravelDistance = currentDistance - sweetSpotDist;
        }

        /// <summary>
        /// 스냅 종료 조건 체크
        /// - 타겟이 사라짐
        /// - 히트 판정 시작됨 (충분히 접근했으므로 루트모션에 맡김)
        /// - 스윗 스팟(정지 거리) 도달
        /// </summary>
        private void UpdateSnapState()
        {
            if (!_isSnapping)
                return;

            if (_snapTarget == null || _combat.IsPossibleCollide)
            {
                ClearSnapState();
                return;
            }

            float dist = HorizontalDistance(gameActor.transform.position, _snapTarget.position);
            float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;

            // 스윗 스팟에 도달했거나 넘어섰다면 스냅 종료
            if (dist <= sweetSpotDist)
            {
                ClearSnapState();
            }
        }

        private void ClearSnapState()
        {
            _snapTarget = null;
            _isSnapping = false;
            _snapStartTravelDistance = 0f;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        #endregion

        #region Movement & Rotation

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;

            if (_isSnapping && _snapTarget != null)
            {
                Vector3 toTarget = _snapTarget.position - gameActor.transform.position;
                toTarget.y = 0f;
                float currentDist = toTarget.magnitude;

                float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;
                float distToTravel = currentDist - sweetSpotDist;

                if (distToTravel > 0.01f)
                {
                    Vector3 snapDir = toTarget.normalized;

                    // EaseOut: 출발할 땐 빠르게, 도착할 땐 부드럽게 감속
                    float progress = (_snapStartTravelDistance > 0.01f) 
                        ? 1f - Mathf.Clamp01(distToTravel / _snapStartTravelDistance) 
                        : 1f;
                    
                    float easeOutMultiplier = 1f - (progress * progress);
                    
                    // 남은 거리에 비례하여 필요한 속도를 구함 (거리가 멀수록 빠르게)
                    float neededSpeed = (distToTravel * 5.0f) * easeOutMultiplier;
                    float finalSpeed = Mathf.Min(neededSpeed, _combat.SnapMoveSpeed);

                    // 루트모션 방향이 타겟 방향과 얼마나 일치하는지 내적(Dot)으로 확인
                    float dot = Vector3.Dot(rootMotionVel.normalized, snapDir);
                    Vector3 finalVel;

                    if (dot > 0.5f && rootMotionVel.magnitude > 0.1f)
                    {
                        // 전진하는 공격 모션인 경우: 루트모션의 방향을 유지하면서 속도만 스케일 업(모션 워핑)
                        // 타겟 방향 벡터와 부족한 스피드만큼 더해줌
                        finalVel = rootMotionVel + (snapDir * Mathf.Max(0, finalSpeed - rootMotionVel.magnitude));
                    }
                    else
                    {
                        // 제자리 공격이거나 엉뚱한 방향인 경우: 강제로 타겟을 향해 끌어당김
                        finalVel = snapDir * finalSpeed;
                    }

                    currentVelocity = new Vector3(finalVel.x, rootMotionVel.y, finalVel.z);
                    return;
                }
            }

            // 스냅을 하지 않거나 종료된 상태라면 순수 루트모션만 적용
            currentVelocity = rootMotionVel;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_isSnapping && _snapTarget != null)
            {
                Vector3 dirToTarget = (_snapTarget.position - gameActor.transform.position);
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);

                    // 공격 극초반(0.15초 이내)에는 즉시 타겟을 바라보게 하여 빗나가는 것을 방지
                    if (_attackTimer < 0.15f)
                    {
                        currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 25f);
                    }
                    else
                    {
                        // 공격 중반 이후로는 회전 속도를 늦춰서 무게감을 줌
                        currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 8f);
                    }
                    
                    currentRotation = currentRotation.normalized;
                    return;
                }
            }

            // Lock-On 타겟이 있으면 스냅과 무관하게 항상 타겟 쪽을 바라보도록 보정
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                Vector3 directionToTarget = (lockOnTarget.position - gameActor.transform.position).normalized;
                directionToTarget.y = 0f;
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * 10f);
                }
            }
            else
            {
                // 아무것도 없으면 루트모션 회전값 적용
                currentRotation *= gameActor.Animator.DeltaRotation;
            }
            
            currentRotation = currentRotation.normalized;
        }

        #endregion
    }
}