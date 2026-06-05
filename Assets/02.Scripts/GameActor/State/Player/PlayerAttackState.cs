using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Path;
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
        public override bool SuppressesHitReaction => _isSwapEvadeCounterAttack || _isEntryAttack || _isSwapSpecialAttack;

        private PlayerCombat    _combat;
        private PlayerEquipment _equipment;

        private AttackData _currentAttack;
        private float      _attackTimer;

        private bool _comboInputted;
        private bool _isHeavyAttack;
        private bool _isCounter;
        private bool _isParryCounter;
        private bool _isSwapEvadeCounterAttack;
        private bool _isDodgeCounterAttack;
        private bool _isEntryAttack;
        private bool _isSwapSpecialAttack;
        private readonly PlayerInterruptAction _forcedAttackAction;

        private PlayerActorAnimator _playerActorAnimator;

        // 호밍 타겟 (Motion Warp + 회전 보정 공통)
        private Transform _homingTarget;
        private Transform _dodgeCounterTarget;
        private MotionWarpController _motionWarp;

        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
        }

        private PlayerAttackState(ActorMovementController controller, PlayerInterruptAction forcedAttackAction) : base(controller)
        {
            _forcedAttackAction = forcedAttackAction;
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
            => CanEnter(controller, PlayerInterruptAction.None);

        public static bool CanEnter(PlayerMovementController controller, PlayerInterruptAction forcedAttackAction)
        {
            if (controller == null) return false;

            var playerActor = controller.GetComponent<PlayerActor>();
            if (playerActor == null) return false;

            var combat   = playerActor.GetCombat();
            var animator = playerActor.Animator;
            if (combat == null || animator == null) return false;

            // 강 공격 입력이 들어와 있고 피니시 가능한 타겟이 있다면
            // PlayerFinishAttackState로 라우팅된다 → AttackState 진입은 항상 허용.
            bool hasForcedAttack = forcedAttackAction != PlayerInterruptAction.None;
            bool isHeavyPending = hasForcedAttack
                ? (forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0
                : InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (isHeavyPending && combat.FindFinishableTarget() != null)
                return true;
            if (isHeavyPending && combat.FindSpecialBreakAttackTarget() != null)
                return true;

            AnimKey peekedKey = PeekNextAnimKey(playerActor, controller, combat, isHeavyPending, forcedAttackAction);
            if (peekedKey == AnimKey.None) return false;

            return animator.HasMotion(peekedKey, true);
        }

        /// <summary>
        /// CanEnter 판정 후 통과하면 PlayerAttackState로 전환한다.
        /// 모션이 없으면 진입 자체를 막아 기존 애니메이션이 끊기는 스터터를 방지한다.
        /// </summary>
        public static bool TryEnter(PlayerMovementController controller)
            => TryEnter(controller, PlayerInterruptAction.None);

        public static bool TryEnter(PlayerMovementController controller, PlayerInterruptAction forcedAttackAction)
        {
            if (!CanEnter(controller, forcedAttackAction)) return false;

            var playerActor = controller.GetComponent<PlayerActor>();
            var combat = playerActor != null ? playerActor.GetCombat() : null;
            bool hasForcedAttack = forcedAttackAction != PlayerInterruptAction.None;
            bool isHeavyPending = hasForcedAttack
                ? (forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0
                : InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (isHeavyPending && combat != null)
            {
                Transform finishTarget = combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    controller.TransitionToState(new PlayerFinishAttackState(controller, finishTarget));
                    return true;
                }

                Transform breakTarget = combat.FindSpecialBreakAttackTarget();
                if (breakTarget != null)
                {
                    InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    controller.TransitionToState(new PlayerSpecialBreakAttackState(controller, breakTarget));
                    return true;
                }
            }

            controller.TransitionToState(hasForcedAttack
                ? new PlayerAttackState(controller, forcedAttackAction)
                : new PlayerAttackState(controller));
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
            bool isHeavyAttack,
            PlayerInterruptAction forcedAttackAction)
        {
            // ★ 연계 라우트 우선 판정(side effect 없음). 라우트가 매칭되면 그 animKey를 미리 반환해
            //   CanEnter가 라우트 모션 보유 여부로 진입을 결정하게 한다(설계 §5.3, advisor #2).
            //   recordToken:false → 트래커에 push하지 않고 가상 append로만 매칭.
            {
                var route = ComboRouteRunner.ResolveRoute(playerActor, controller, combat,
                    isHeavyAttack, forcedAttackAction, recordToken: false);
                if (route != null)
                    return route.attackInfo?.baseInfo?.animKey ?? AnimKey.None;
            }

            if ((forcedAttackAction & PlayerInterruptAction.LightAttack) != 0)
                return combat.PeekNormalAttackAnimKey(false);

            if ((forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
                return combat.PeekHeavyAttackAnimKey(false);

            if ((forcedAttackAction & PlayerInterruptAction.Skill) != 0)
            {
                var forcedSkillGauge = playerActor.SkillGauge;
                for (int i = 0; i < 10; i++)
                {
                    if (!controller.HasSkillInput(i)) continue;
                    if (forcedSkillGauge != null && !forcedSkillGauge.CanUseSkill(i)) continue;

                    return combat.PeekSkillAttackAnimKey(i);
                }

                return AnimKey.None;
            }

            // 0순위: 패리 반격
            if (combat.IsParryCounterAvailable)
                return combat.PeekParryCounterAttackAnimKey();

            // 1순위: 퍼펙트 가드 반격
            bool isCounter = playerActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false;
            if (isCounter)
                return combat.PeekCounterAttackAnimKey();

            // 1순위: 회피 카운터 / 스왑 회피 카운터
            if (combat.IsDodgeCounterAvailable || playerActor.IsSwapEvadeCounterAttackPending)
                return combat.PeekSwapEvadeCounterAttackAnimKey();

            // 2순위: 풀 게이지 교체 특수 공격
            if (playerActor.IsSwapSpecialAttackPending)
                return combat.PeekSwapSpecialAttackAnimKey();

            // 3순위: 교체 등장 공격
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

            bool hasForcedAttack = _forcedAttackAction != PlayerInterruptAction.None;
            _isHeavyAttack = !hasForcedAttack
                             && InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;

            playerActor.Animator.ApplyRootMotion(true);
            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            _motionWarp = controller.MotionWarp;

            _combat    = playerActor.GetCombat();
            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = true;
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            _isCounter = !hasForcedAttack
                         && (gameActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false);
            if (_isCounter)
                gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Counter);

            _dodgeCounterTarget = _combat.DodgeCounterTarget != null ? _combat.DodgeCounterTarget.transform : null;
            bool consumedDodgeCounter = !hasForcedAttack && _combat.ConsumeDodgeCounterWindow();
            bool consumedSwapEvadeCounter = !hasForcedAttack
                                            && !consumedDodgeCounter
                                            && playerActor.ConsumeSwapEvadeCounterAttackPending();
            _isDodgeCounterAttack = consumedDodgeCounter;
            _isSwapEvadeCounterAttack = consumedDodgeCounter || consumedSwapEvadeCounter;
            _isSwapSpecialAttack = !hasForcedAttack && !_isSwapEvadeCounterAttack && playerActor.ConsumeSwapSpecialAttackPending();
            _isEntryAttack = !hasForcedAttack && playerActor.ConsumeEntryAttackPending();

            _isParryCounter = !hasForcedAttack && _combat.IsParryCounterAvailable;
            if (_isParryCounter)
            {
                _combat.CloseParryCounterWindow();
                Debug.Log("[ParryCounter] 패리 반격 진입");
            }

            if ((_forcedAttackAction & PlayerInterruptAction.LightAttack) != 0)
            {
                InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack);
                _isHeavyAttack = false;
            }
            else if ((_forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
            {
                _isHeavyAttack = InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null;
            }

            bool shouldResetCombo = !_isCounter
                                    && !_isParryCounter
                                    && !_isSwapEvadeCounterAttack
                                    && !_isEntryAttack
                                    && !_isSwapSpecialAttack
                                    && !_combat.CanUseStoredCombo(_isHeavyAttack);
            if (shouldResetCombo)
                // 공격 상태 재진입(크로스타입 캔슬 포함)은 진짜 콤보 종료가 아니므로 약/강 체인 분기 메모리는 보존한다.
                // (진입 체인은 ExecuteAttack/ExecuteHeavyAttack이 isCombo=false → index 0으로 알아서 시작)
                _combat.ResetComboPreserveChains();
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

            if (_isDodgeCounterAttack)
                CameraManager.Instance?.CombatCamera?.PlayDodgeCounter(_homingTarget, CameraShakeIdType.PlayerHit);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Attack);

            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            gameActor.Animator.Speed = 1f;
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = false;
            _homingTarget = null;
            _dodgeCounterTarget = null;
            _isDodgeCounterAttack = false;
            _motionWarp?.ClearTarget();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _attackTimer += deltaTime;

            // 인터럽트(캔슬): 허용 액션은 데이터(interruptActions) 마스크로, 허용 구간은
            // 캔슬 윈도우(히트박스 콜리전 비활성 구간)로 제어한다. 액티브 히트 중엔 캔슬 불가.
            // 콤보 검사보다 먼저 실행되어 둘 다 성립하면 캔슬이 우선한다.
            // Dash가 입력만 소비하고 전환에 실패하면 false가 반환되어 아래 콤보 로직으로 fall-through 한다.
            if (_combat.IsCancelWindowOpen
                && PlayerInterruptResolver.TryInterrupt(playerController, _currentAttack.interruptActions))
                return;

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
                _isSwapEvadeCounterAttack = false;
                _isEntryAttack  = false;
                _isSwapSpecialAttack = false;

                // 다음 콤보 키를 미리 조회해 보유 여부를 확인.
                // 모션이 없으면 콤보 인덱스를 진행시키지 않고 Idle/Move로 이탈.
                // 연계 라우트가 매칭되면 라우트 모션으로 판정(기본 콤보 리스트가 비어 있어도
                // 라우트 진입이 막히지 않도록 — 콤보 연속 입력은 약/강만 가능).
                var peekRoute = ComboRouteRunner.ResolveRoute(
                    playerActor, playerController, _combat,
                    _isHeavyAttack, PlayerInterruptAction.None, recordToken: false);

                AnimKey peekedKey = peekRoute != null
                    ? (peekRoute.attackInfo?.baseInfo?.animKey ?? AnimKey.None)
                    : (_isHeavyAttack ? _combat.PeekHeavyAttackAnimKey(true)
                                      : _combat.PeekNormalAttackAnimKey(true));

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

            // 1순위: 스왑 회피 카운터
            if (_isSwapEvadeCounterAttack)
            {
                _currentAttack = _combat.ExecuteSwapEvadeCounterAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            // 2순위: 풀 게이지 교체 특수 공격
            if (_isSwapSpecialAttack)
            {
                _currentAttack = _combat.ExecuteSwapSpecialAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            // 3순위: 교체 등장 공격
            if (_isEntryAttack)
            {
                _currentAttack = _combat.ExecuteEntryAttack();
                return _currentAttack?.animKey ?? AnimKey.Attack_1;
            }

            // ★ 연계 라우트 — forced/normal 공통 단일 판정점 (설계 §5.3, advisor #1).
            //   "약약약→강"의 강공은 HeavyAttack 인터럽트(forced)로 들어와 아래 forced 분기로
            //   빠지므로, forced 분기보다 '앞'에서 라우트를 가로채야 한다.
            //   여기서 pending 토큰을 트래커에 1회 push(기록)하고, 매칭 시 라우트를 실행한다.
            {
                var routeAttack = ComboRouteRunner.TryExecuteRoute(playerActor, playerController, _combat,
                    _isHeavyAttack, _forcedAttackAction, out var routeAnimKey);
                if (routeAttack != null)
                {
                    _currentAttack = routeAttack;
                    return routeAnimKey;
                }
            }

            if ((_forcedAttackAction & PlayerInterruptAction.LightAttack) != 0)
            {
                _currentAttack = _combat.ExecuteAttack(false);
                return _currentAttack?.animKey ?? AnimKey.None;
            }

            if ((_forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
            {
                _currentAttack = _combat.ExecuteHeavyAttack(false);
                return _currentAttack?.animKey ?? AnimKey.None;
            }

            var skillGauge = playerActor.SkillGauge;

            // 1순위: 숫자 키 스킬
            bool skillAllowed = _forcedAttackAction == PlayerInterruptAction.None
                                || (_forcedAttackAction & PlayerInterruptAction.Skill) != 0;
            for (int i = 0; skillAllowed && i < 10; i++)
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

            if (_isDodgeCounterAttack && _dodgeCounterTarget != null)
                return _dodgeCounterTarget;

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

            // 워프 구간에서 클립 재생 속도를 타겟 거리 비율로 보정해 풋슬라이딩 감소.
            gameActor.Animator.Speed = _combat.IsMotionWarping
                ? _motionWarp.WarpPlayRateScale
                : 1f;

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
            // 호밍: 워프 구간에서 타겟 방향으로 회전 보정.
            // rotationCurve 기반 곡선 보간 — 시간 상수가 아닌 정규화 진행도로 회전 진행.
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
