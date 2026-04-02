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

        // --- Motion Warp ---
        private Transform _warpTarget;
        private bool _isWarping;

        // 워핑 시작 시 '스윗 스팟'까지 남은 거리 (감속 비율 계산용)
        private float _warpStartDistance;

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
            
            _isHeavyAttack = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            
            _combat = playerActor.GetCombat();
            _combat.ResetCombo();
            
            _equipment = playerActor.GetPlayerEquipment();
            
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
            
            // 모션 워핑 시도
            TryInitWarp();
        }

        public override void OnExit(GameActorState toState)
        {
            _combat.ClearHitTargets();
            
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            
            ClearWarpState();

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;

            // 워핑 종료 조건 체크
            UpdateWarpState();

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
                    if(_isHeavyAttack == true)
                        _combat.ResetCombo();
                    
                    _comboInputted = true;
                    _isHeavyAttack = false;
                    _combat.CloseComboWindow();
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {                    
                    if(_isHeavyAttack == false)
                        _combat.ResetCombo();
                    
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
            
            _isHeavyAttack = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;

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
                
                // 콤보 연결 시 워핑 재시도
                TryInitWarp();
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

        #region Motion Warp + Homing

        /// <summary>
        /// 공격 시작/콤보 연결 시 워핑 대상 탐색 및 초기화
        /// </summary>
        private void TryInitWarp()
        {
            ClearWarpState();

            if (_currentAttack == null)
                return;

            Transform target = FindWarpTarget();
            if (target == null)
                return;

            _warpTarget = target;

            float dist = HorizontalDistance(gameActor.transform.position, target.position);
            float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;

            // 스윗 스팟 안쪽이면 이동 워핑 불필요, 호밍(회전)만 적용
            if (dist > sweetSpotDist)
            {
                _isWarping = true;
                _warpStartDistance = dist - sweetSpotDist;
            }
        }

        private Transform FindWarpTarget()
        {
            // 1. 락온 대상 우선
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(gameActor.transform.position, lockOnTarget.position);
                if (dist <= _combat.GetSnapSearchRange(true))
                    return lockOnTarget;
            }

            // 2. 자유 전투 자석 탐색
            bool isLockedOn = lockOnTarget != null;
            return _combat.FindAttackSnapTarget(
                _currentAttack.hitRange, _currentAttack.hitAngle, isLockedOn);
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

            float dist = HorizontalDistance(gameActor.transform.position, _warpTarget.position);
            float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;

            if (dist <= sweetSpotDist)
                _isWarping = false;
        }

        private void ClearWarpState()
        {
            _warpTarget = null;
            _isWarping = false;
            _warpStartDistance = 0f;
        }

        /// <summary>
        /// EaseOut 커브: 출발 시 빠르게, 도착 시 부드럽게 감속
        /// t: 0~1 진행률 → 반환: 0~1 이징된 값
        /// </summary>
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

        #region Movement & Rotation

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;

            if (_isWarping && _warpTarget != null)
            {
                Vector3 toTarget = _warpTarget.position - gameActor.transform.position;
                toTarget.y = 0f;
                float currentDist = toTarget.magnitude;

                float sweetSpotDist = _currentAttack.hitRange * SweetSpotMultiplier;
                float distToTravel = currentDist - sweetSpotDist;

                if (distToTravel > 0.01f)
                {
                    Vector3 warpDir = toTarget.normalized;

                    // 진행률 0→1, EaseOut 적용: 출발 시 빠르게, 도착 시 부드럽게
                    float progress = (_warpStartDistance > 0.01f)
                        ? Mathf.Clamp01(1f - distToTravel / _warpStartDistance)
                        : 1f;
                    float easedSpeed = Mathf.Lerp(_combat.SnapMoveSpeed, 0f, EaseOut(progress));

                    // 루트모션과 워핑을 블렌딩: 진행률이 높을수록 루트모션 비중 증가
                    float rootMotionBlend = Mathf.Clamp01(progress);
                    Vector3 warpVel = warpDir * easedSpeed;
                    Vector3 finalVel = Vector3.Lerp(warpVel, rootMotionVel, rootMotionBlend);

                    // 루트모션이 타겟 방향과 같으면 속도 보강
                    float dot = Vector3.Dot(rootMotionVel.normalized, warpDir);
                    if (dot > 0.5f && rootMotionVel.magnitude > 0.1f)
                    {
                        finalVel = Vector3.Max(finalVel, rootMotionVel);
                    }

                    currentVelocity = new Vector3(finalVel.x, rootMotionVel.y, finalVel.z);
                    return;
                }
            }

            currentVelocity = rootMotionVel;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 호밍: 워핑 타겟이 있으면 공격 전반부에서 타겟을 향해 회전
            if (_warpTarget != null)
            {
                Vector3 dirToTarget = _warpTarget.position - gameActor.transform.position;
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);

                    // Startup(0.15초): 빠르게 보정 → 이후: 무게감 있게 감속
                    float rotSpeed = _attackTimer < 0.15f ? 25f : 8f;

                    // 히트 판정 시작 이후에는 호밍 종료 (무게감 유지)
                    if (!_combat.IsPossibleCollide)
                    {
                        currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * rotSpeed);
                        currentRotation = currentRotation.normalized;
                        return;
                    }
                }
            }

            // Lock-On 타겟이 있으면 항상 타겟 쪽을 바라봄
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
                currentRotation *= gameActor.Animator.DeltaRotation;
            }

            currentRotation = currentRotation.normalized;
        }

        #endregion
    }
}