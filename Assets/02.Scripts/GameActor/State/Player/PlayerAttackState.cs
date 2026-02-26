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
    /// - Attack Snap: 공격 모션(루트모션) 위에 스냅 보정 속도를 합산하여
    ///   가까운 적에게 자연스럽게 접근하면서 공격
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
        private float _snapStartDistance; // 스냅 시작 시 타겟까지 거리 (EaseOut 비율 계산용)
        
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
                animState.OwnedEvents.OnEnd = ()=>
                {
                    ChangeToNextState();
                };
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
            
            if (_comboInputted)
            {
                var animState = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd = ChangeToNextState;
                }
                
                _playerActorAnimator.IsOpenedComboWindow = false;
                
                _combat.CloseComboWindow();
                _comboInputted = false;
                
                // 콤보 연결 시에도 스냅 재시도
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
            int skillCount = 4;
            for (int i = 0; i < skillCount; i++)
            {
                if (playerController.HasSkillInput(i))
                {
                    _currentAttack = _combat.ExecuteSkillAttack(i);
                    return _currentAttack?.animKey ?? AnimKey.None;
                }
            }

            if (_isHeavyAttack == false &&
                playerController.HasMoveInput() 
                && gameActor.MoveAnimType == BaseMoveAnimType.Sprint)
            {
                return AnimKey.DashAttack_1;
            }
            
            _currentAttack = (_isHeavyAttack) 
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

            // 락온 대상 우선 체크
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(
                    gameActor.transform.position, lockOnTarget.position);
                
                // 히트 범위 안이면 스냅 불필요
                if (dist <= _currentAttack.hitRange)
                    return;

                // 자석 탐색 범위 안이면 스냅 대상으로 설정
                if (dist <= _combat.SnapSearchRange)
                {
                    BeginSnap(lockOnTarget, dist);
                    return;
                }
            }

            // 락온 대상이 없으면 자석 탐색
            Transform snapCandidate = _combat.FindAttackSnapTarget(
                _currentAttack.hitRange, _currentAttack.hitAngle);

            if (snapCandidate != null)
            {
                float dist = HorizontalDistance(
                    gameActor.transform.position, snapCandidate.position);
                BeginSnap(snapCandidate, dist);
            }
        }

        private void BeginSnap(Transform target, float initialDistance)
        {
            _snapTarget = target;
            _isSnapping = true;
            _snapStartDistance = initialDistance;
        }

        /// <summary>
        /// 스냅 종료 조건 체크
        /// - 타겟이 사라짐
        /// - 히트 판정 시작됨 (충분히 접근했으므로 루트모션에 맡김)
        /// - 정지 거리 도달
        /// </summary>
        private void UpdateSnapState()
        {
            if (!_isSnapping)
                return;

            if (_snapTarget == null)
            {
                ClearSnapState();
                return;
            }

            // 히트 판정이 시작되면 스냅 종료 → 이후 순수 루트모션
            if (_combat.IsPossibleCollide)
            {
                ClearSnapState();
                return;
            }

            float dist = HorizontalDistance(
                gameActor.transform.position, _snapTarget.position);
            
            if (dist <= _combat.SnapStopDistance)
            {
                ClearSnapState();
            }
        }

        private void ClearSnapState()
        {
            _snapTarget = null;
            _isSnapping = false;
            _snapStartDistance = 0f;
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

            // 루트모션이 베이스
            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;

            if (_isSnapping && _snapTarget != null)
            {
                Vector3 toTarget = _snapTarget.position - gameActor.transform.position;
                toTarget.y = 0f;

                float distance = toTarget.magnitude;
                if (distance > 0.01f)
                {
                    Vector3 snapDir = toTarget / distance;

                    // EaseOut: 시작 거리 대비 남은 거리 비율로 감속
                    float progress = (_snapStartDistance > 0.01f)
                        ? 1f - Mathf.Clamp01(distance / _snapStartDistance)
                        : 1f;
                    float easeOut = 1f - (progress * progress); // 빠르게 출발, 도착에 감속

                    float snapSpeed = _combat.SnapMoveSpeed * easeOut;

                    // 루트모션 + 스냅 보정 합산
                    Vector3 snapVel = snapDir * snapSpeed;
                    
                    currentVelocity = rootMotionVel + new Vector3(snapVel.x, 0f, snapVel.z);
                    return;
                }
            }

            currentVelocity = rootMotionVel;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 스냅 중이면 타겟 방향으로 회전
            if (_isSnapping && _snapTarget != null)
            {
                Vector3 dirToTarget = (_snapTarget.position - gameActor.transform.position);
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 10f);
                    currentRotation = currentRotation.normalized;
                    return;
                }
            }

            // Lock-On 타겟이 있으면 타겟 방향으로 회전
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