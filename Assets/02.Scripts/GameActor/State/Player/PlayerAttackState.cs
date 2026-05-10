using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;
using UPlayGround.Manager;
using UPlayGround.InputDefine;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;

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
        private bool _isCounter;
        private bool _isParryCounter;
        private bool _isEntryAttack;

        private PlayerActorAnimator _playerActorAnimator;

        // 호밍 타겟 (Motion Warp + 회전 보정 공통)
        private Transform _homingTarget;
        private MotionWarpController _motionWarp;

        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
            return true;
        }

        /// <summary>
        /// 진입 후 재생할 공격 모션이 실제로 존재하는지 side effect 없이 미리 판정한다.
        /// GetAnimKey()와 동일한 우선순위 체인을 따라 다음 AnimKey를 미리 조회 후
        /// ActorAnimator.HasMotion으로 보유 여부만 확인한다.
        ///
        /// 호출자 측 입력 소비/콤보 인덱스/스킬 게이지 등은 변경하지 않으므로
        /// false 반환 시 현재 상태를 그대로 유지해도 안전하다.
        /// </summary>
        public static bool CanEnter(PlayerMovementController controller)
        {
            if (controller == null) return false;

            var playerActor = controller.GetComponent<PlayerActor>();
            if (playerActor == null) return false;

            var combat   = playerActor.GetCombat();
            var animator = playerActor.Animator;
            if (combat == null || animator == null) return false;

            // 강 공격 입력이 들어와 있고 피니시 가능한 타겟이 있다면
            // PlayerFinishAttackState로 라우팅된다 → AttackState 진입은 항상 허용.
            bool isHeavyPending = InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (isHeavyPending && combat.FindFinishableTarget() != null)
                return true;

            AnimKey peekedKey = PeekNextAnimKey(playerActor, controller, combat, isHeavyPending);
            if (peekedKey == AnimKey.None) return false;

            return animator.HasMotion(peekedKey, true);
        }

        /// <summary>
        /// CanEnter 판정 후 통과하면 PlayerAttackState로 전환한다.
        /// 모션이 없으면 진입 자체를 막아 기존 애니메이션이 끊기는 스터터를 방지한다.
        /// </summary>
        public static bool TryEnter(PlayerMovementController controller)
        {
            if (!CanEnter(controller)) return false;
            controller.TransitionToState(new PlayerAttackState(controller));
            return true;
        }

        /// <summary>
        /// GetAnimKey()의 우선순위 그대로 다음 AnimKey를 미리 산출 (side effect 없음).
        /// 0순위: 패리 반격 → 카운터 → 등장 공격 → 스킬 → 강/약 콤보.
        /// </summary>
        private static AnimKey PeekNextAnimKey(
            PlayerActor playerActor,
            PlayerMovementController controller,
            PlayerCombat combat,
            bool isHeavyAttack)
        {
            // 0순위: 패리 반격
            if (combat.IsParryCounterAvailable)
                return combat.PeekParryCounterAttackAnimKey();

            // 1순위: 퍼펙트 가드 반격
            bool isCounter = playerActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false;
            if (isCounter)
                return combat.PeekCounterAttackAnimKey();

            // 1순위: 교체 등장 공격
            if (playerActor.IsEntryAttackPending)
                return combat.PeekEntryAttackAnimKey();

            // 1순위: 숫자 키 스킬 (게이지 보유 여부만 확인하고 실제로 소비하지 않음)
            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < 10; i++)
            {
                if (!controller.HasSkillInput(i)) continue;
                if (skillGauge != null && !skillGauge.CanUseSkill(i)) continue;

                return combat.PeekSkillAttackAnimKey(i);
            }

            // 2순위: 기본 약/강 콤보. 콤보 입력 없는 첫 진입이므로 isCombo=false.
            return isHeavyAttack
                ? combat.PeekHeavyAttackAnimKey(false)
                : combat.PeekNormalAttackAnimKey(false);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTagId.State_Combat_Attack);

            _isHeavyAttack = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            _motionWarp = controller.MotionWarp;

            _combat    = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = true;

            _isCounter = gameActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false;
            if (_isCounter)
                gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Counter);

            _isEntryAttack = playerActor.ConsumeEntryAttackPending();

            _isParryCounter = _combat.IsParryCounterAvailable;
            if (_isParryCounter)
            {
                _combat.CloseParryCounterWindow();
                GameCombatManager.Instance.GameHitStop.Stop();
                Debug.Log("[ParryCounter] 패리 반격 진입");
            }

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

            var animKey   = GetAnimKey();
            var animState = gameActor.Animator.PlayMotion(animKey, 0.25f);
            if (_isParryCounter)
                Debug.Log($"[ParryCounter] PlayMotion({animKey}) → {(animState != null ? "성공" : "실패(모션셋 없음)")}");

            if (animState != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            else
            {
                ChangeToNextState();
                return;
            }

            _homingTarget = FindHomingTarget();
            _motionWarp.SetTarget(_homingTarget);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Attack);

            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = false;
            _homingTarget = null;
            _motionWarp?.ClearTarget();
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

            if (!_combat.IsPossibleCollide && _comboInputted)
                ChangeToNextState();
        }

        private void ChangeToNextState()
        {
            _combat.ClearHitTargets();
            _attackTimer = 0f;

            if (!_comboInputted)
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
                _isCounter      = false;
                _isParryCounter = false;
                _isEntryAttack  = false;

                // 다음 콤보 키를 미리 조회해 보유 여부를 확인.
                // 모션이 없으면 콤보 인덱스를 진행시키지 않고 Idle/Move로 이탈.
                AnimKey peekedKey = _isHeavyAttack
                    ? _combat.PeekHeavyAttackAnimKey(true)
                    : _combat.PeekNormalAttackAnimKey(true);

                if (peekedKey == AnimKey.None || !gameActor.Animator.HasMotion(peekedKey, true))
                {
                    _comboInputted = false;
                    _combat.ResetCombo();
                    if (playerController.HasMoveInput())
                        controller.TransitionToState(new PlayerGroundMoveState(controller));
                    else
                        controller.TransitionToState(new PlayerIdleState(controller));
                    return;
                }

                gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
                var animState =  gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);
                if (animState != null)
                    gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
                _playerActorAnimator.IsOpenedComboWindow = false;
                _combat.CloseComboWindow();
                _comboInputted = false;
                _homingTarget = FindHomingTarget();
                _motionWarp.SetTarget(_homingTarget);
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
            // 0순위: 패리 반격
            if (_isParryCounter)
            {
                _currentAttack = _combat.ExecuteParryCounterAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            // 1순위: 퍼펙트 가드 반격
            if (_isCounter)
            {
                _currentAttack = _combat.ExecuteCounterAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            // 1순위: 교체 등장 공격
            if (_isEntryAttack)
            {
                _currentAttack = _combat.ExecuteEntryAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            var skillGauge = playerActor.SkillGauge;

            // 1순위: 숫자 키 스킬
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

            // 2순위: 기본 약/강 콤보
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
                _combat.EndMotionWarp);

            currentVelocity = _motionWarp.ClampApproachVelocity(
                currentVelocity,
                motor.TransientPosition,
                deltaTime);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 호밍: 워프 구간에서 타겟 방향으로 회전 보정
            // UpdateVelocity와 동일한 조건(스냅샷 거리 + 도달 가능 여부)으로만 적용해
            // 이동 방향과 회전 방향이 일치하도록 한다.
            if (_motionWarp.TryGetFacingDirection(
                    motor.TransientPosition,
                    _combat.IsMotionWarping,
                    _combat.WarpRemainingTime,
                    _combat.WarpMinDistance,
                    _combat.WarpMaxDistance,
                    _combat.WarpMaxSpeed,
                    out Vector3 warpDirection))
            {
                Quaternion targetRot = Quaternion.LookRotation(warpDirection);
                // Startup 0.15초: 빠르게 보정 → 이후: 무게감 있게 감속
                float rotSpeed = _attackTimer < 0.15f ? 25f : 8f;
                currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * rotSpeed);
                currentRotation = currentRotation.normalized;
                return;
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
