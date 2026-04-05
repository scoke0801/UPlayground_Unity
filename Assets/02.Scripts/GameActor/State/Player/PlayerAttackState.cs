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
    /// 공격 상태 — 루트모션 기반 Motion Warp
    ///
    /// [이동 로직]
    ///   타겟 있음 + IsMotionWarping: WarpRemainingTime(워프 이벤트 구간의 남은 시간) 기반으로
    ///              속력을 역산해 타겟 방향으로 이동. 루트모션 Y축만 유지.
    ///   그 외: 루트모션 원본 그대로 적용.
    ///
    /// [워프 구간 지정]
    ///   공격 MotionSet 타임라인에 MotionEvent_MotionWarp 이벤트를 추가.
    ///   endTime을 Collision 이벤트 startTime 직전으로 맞추면 된다.
    /// </summary>
    public class PlayerAttackState : PlayerActorState
    {
        public override string StateName => "Attack";

        private PlayerCombat    _combat;
        private PlayerEquipment _equipment;

        private AttackData _currentAttack;
        private float      _attackTimer;

        private bool _comboInputted;
        private bool _isHeavyAttack;

        private PlayerActorAnimator _playerActorAnimator;

        // 호밍 타겟 (Motion Warp + 회전 보정 공통)
        private Transform _homingTarget;

        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _isHeavyAttack = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;

            _combat    = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = true;
            _combat.ResetCombo();
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
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            else
            {
                ChangeToNextState();
                return;
            }

            _homingTarget = FindHomingTarget();
        }

        public override void OnExit(GameActorState toState)
        {
            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = false;
            _homingTarget = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;

            if (_currentAttack.canBeInterrupted)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dodge) != null)
                {
                    controller.TransitionToState(new PlayerDodgeState(controller));
                    return;
                }

                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Jump) != null)
                {
                    controller.TransitionToState(new PlayerAirborneState(controller));
                    return;
                }

                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dash) != null)
                {
                    if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                        return;
                }
            }

            if (_combat.CanCombo)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    if (_isHeavyAttack) _combat.ResetCombo();
                    _comboInputted = true;
                    _isHeavyAttack = false;
                    _combat.CloseComboWindow();
                }
                else if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    if (!_isHeavyAttack) _combat.ResetCombo();
                    _comboInputted = true;
                    _isHeavyAttack = true;
                    _combat.CloseComboWindow();
                }
            }

            if (!_combat.IsPossibleCollide && _comboInputted)
                ChangeToNextState();
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
                gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
                _playerActorAnimator.IsOpenedComboWindow = false;
                _combat.CloseComboWindow();
                _comboInputted = false;
                _homingTarget  = FindHomingTarget();
            }
            else
            {
                _combat.ResetCombo();
                if (playerController.HasMoveInput())
                    controller.TransitionToState(new PlayerGroundMoveState(controller));
                else
                    controller.TransitionToState(new PlayerIdleState(controller));
            }
        }

        private AnimKey GetAnimKey()
        {
            var skillGauge = playerActor.SkillGauge;

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

        private Transform FindHomingTarget()
        {
            if (_currentAttack == null) return null;

            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(gameActor.transform.position, lockOnTarget.position);
                if (dist <= _combat.GetSnapSearchRange(true))
                    return lockOnTarget;
            }

            bool isLockedOn = lockOnTarget != null;
            return _combat.FindAttackSnapTarget(
                _currentAttack.hitRange, _currentAttack.hitAngle, isLockedOn);
        }

        #region Movement & Rotation

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            Vector3 rootMotionDelta = gameActor.Animator.DeltaPosition;

            // 워프 비활성 구간 → 루트모션 원본
            if (_homingTarget == null || !_combat.IsMotionWarping)
            {
                currentVelocity = rootMotionDelta / deltaTime;
                return;
            }

            Vector3 toTarget = _homingTarget.position - gameActor.transform.position;
            toTarget.y = 0f;
            float remainingDist = toTarget.magnitude;

            // 최소/최대 거리 범위 밖 → 루트모션 원본
            if (remainingDist < _combat.WarpMinDistance || remainingDist > _combat.WarpMaxDistance)
            {
                currentVelocity = rootMotionDelta / deltaTime;
                return;
            }

            // 워프 이벤트 구간의 남은 시간으로 속력 역산
            // → GetRemainingTime()이 아닌 WarpRemainingTime을 써서
            //    이벤트 endTime에 정확히 타겟에 도달
            float remainingTime = _combat.WarpRemainingTime;
            float warpSpeed = remainingTime > 0.01f
                ? remainingDist / remainingTime
                : remainingDist / deltaTime; // 시간이 거의 없으면 이번 프레임에 즉시 도달

            Vector3 warpVelocity = toTarget.normalized * warpSpeed;

            // Y는 루트모션 원본 유지 (중력/점프 보존)
            currentVelocity = new Vector3(warpVelocity.x, rootMotionDelta.y / deltaTime, warpVelocity.z);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 호밍: 워프 구간에서 타겟 방향으로 회전 보정
            if (_homingTarget != null && _combat.IsMotionWarping)
            {
                Vector3 dirToTarget = _homingTarget.position - gameActor.transform.position;
                dirToTarget.y = 0f;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);
                    // Startup 0.15초: 빠르게 보정 → 이후: 무게감 있게 감속
                    float rotSpeed = _attackTimer < 0.15f ? 25f : 8f;
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * rotSpeed);
                    currentRotation = currentRotation.normalized;
                    return;
                }
            }

            // Lock-On 타겟은 항상 바라봄
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                Vector3 dir = (lockOnTarget.position - gameActor.transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    currentRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(dir), deltaTime * 10f);
            }
            else
            {
                currentRotation *= gameActor.Animator.DeltaRotation;
            }

            currentRotation = currentRotation.normalized;
        }

        #endregion

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
