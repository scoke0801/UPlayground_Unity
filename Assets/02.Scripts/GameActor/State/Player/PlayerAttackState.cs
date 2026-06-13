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
        // 현재 공격 모션에서 액티브 히트(콜리전)가 최소 1회 발생했는지. 이동 후딜 캔슬 게이트에 사용.
        // 단일 페이즈 공격은 윈드업에도 CurrentHitPhaseIndex == LastHitPhaseIndex == 0 이라
        // 페이즈 비교만으로는 윈드업을 못 거른다 → 히트 1회 발생 여부를 함께 본다.
        private bool _hasActiveHitFired;
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
            // 브레이크 특수공격은 Interact 입력 전용으로 유지한다.
            bool hasForcedAttack = forcedAttackAction != PlayerInterruptAction.None;
            bool isHeavyPending = hasForcedAttack
                ? (forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0
                : InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack);
            if (isHeavyPending && combat.FindFinishableTarget() != null)
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
            bool hasForcedAttack = forcedAttackAction != PlayerInterruptAction.None;

            if (!hasForcedAttack)
            {
                // 0순위: 패리 반격
                if (combat.IsParryCounterAvailable)
                    return combat.PeekParryCounterAttackAnimKey();

                // 1순위: 퍼펙트 가드 반격
                bool isCounter = combat.IsPerfectGuardCounterAvailable
                                 || (playerActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false);
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
            }

            // ★ 연계 라우트 판정(side effect 없음). GetAnimKey의 실행 우선순위와 맞춘다.
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
                for (int i = 0; i < PlayerSkillGauge.SkillSlotCount; i++)
                {
                    if (!controller.HasSkillInput(i)) continue;
                    if (forcedSkillGauge != null && !forcedSkillGauge.CanUseSkill(i)) continue;

                    return combat.PeekSkillAttackAnimKey(i);
                }

                return AnimKey.None;
            }

            // 1순위: 숫자 키 스킬 (게이지 보유 여부만 확인하고 실제로 소비하지 않음)
            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < PlayerSkillGauge.SkillSlotCount; i++)
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

            _isParryCounter = !hasForcedAttack && _combat.IsParryCounterAvailable;
            if (_isParryCounter)
            {
                _combat.CloseParryCounterWindow();
                Debug.Log("[ParryCounter] 패리 반격 진입");
            }

            bool hasPerfectGuardCounterTag = !hasForcedAttack
                                             && (gameActor.Tags?.HasTag(GameplayTagId.State_Combat_Counter) ?? false);
            if (hasPerfectGuardCounterTag)
                gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Counter);

            // 퍼펙트 가드 반격은 카운터 윈도우가 1차 소스. ConsumePerfectGuardCounterWindow가
            // 윈도우를 닫으며 소비하므로 별도 Close 호출은 불필요하다.
            // 윈도우가 프레임 경계에서 만료된 경우에도 태그가 남아 있으면 반격을 성립시켜
            // PeekNextAnimKey의 (윈도우 OR 태그) 판정과 실행 분기를 일치시킨다.
            bool consumedPerfectGuardCounter = !hasForcedAttack
                                               && !_isParryCounter
                                               && _combat.ConsumePerfectGuardCounterWindow();
            _isCounter = !hasForcedAttack
                         && !_isParryCounter
                         && (consumedPerfectGuardCounter || hasPerfectGuardCounterTag);

            _dodgeCounterTarget = _combat.DodgeCounterTarget != null ? _combat.DodgeCounterTarget.transform : null;
            bool consumedDodgeCounter = !hasForcedAttack && _combat.ConsumeDodgeCounterWindow();
            bool consumedSwapEvadeCounter = !hasForcedAttack
                                            && !consumedDodgeCounter
                                            && playerActor.ConsumeSwapEvadeCounterAttackPending();
            _isDodgeCounterAttack = consumedDodgeCounter;
            _isSwapEvadeCounterAttack = consumedDodgeCounter || consumedSwapEvadeCounter;
            _isSwapSpecialAttack = !hasForcedAttack && !_isSwapEvadeCounterAttack && playerActor.ConsumeSwapSpecialAttackPending();
            _isEntryAttack = !hasForcedAttack && playerActor.ConsumeEntryAttackPending();

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
            _hasActiveHitFired = false;

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
            // 상태를 빠져나갈 때 만료 정지를 반드시 해제(콜리전 ON 도중 전환되어도 버퍼가 멈춘 채 남지 않도록).
            InputManager.Instance.InputBuffer.SetExpiryPaused(false);

            gameActor.Tags?.RemoveTag(GameplayTagId.State_Combat_Attack);

            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _playerActorAnimator.IsOpenedComboWindow = false;
            playerActor.Animator.ApplyRootMotion(false);
            gameActor.Animator.Speed = gameActor.LocalTimeScale;
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

            // 이번 공격 모션에서 액티브 히트가 한 번이라도 열렸는지 기록(이동 후딜 캔슬 게이트용).
            if (_combat.IsPossibleCollide)
                _hasActiveHitFired = true;

            // 선입력 보존: 액티브 히트(캔슬 불가) 동안엔 입력 버퍼 만료를 정지해, 이 구간에 들어온
            // 캔슬/콤보 선입력이 0.24s 만료로 유실되지 않게 한다. 캔슬창이 열리면(콜리전 OFF) 정지가
            // 풀려 선입력이 살아있는 채로 아래 TryInterrupt/콤보 검사에 즉시 소비된다.
            InputManager.Instance.InputBuffer.SetExpiryPaused(_combat.IsPossibleCollide);

            // 인터럽트(캔슬): 허용 액션은 데이터(interruptActions) 마스크로, 허용 구간은
            // 캔슬 윈도우(히트박스 콜리전 비활성 구간)로 제어한다. 액티브 히트 중엔 캔슬 불가.
            // 콤보 검사보다 먼저 실행되어 둘 다 성립하면 캔슬이 우선한다.
            // Dash가 입력만 소비하고 전환에 실패하면 false가 반환되어 아래 콤보 로직으로 fall-through 한다.
            // allowGuardCancel: 가드(hold) 캔슬은 액티브 히트가 한 번이라도 발생한 뒤(리커버리/멀티히트 간격)에만
            // 허용한다. 초기 윈드업에서 가드를 쥔 채 시작하는 패리/카운터 반격이 곧바로 가드로 튕기는 걸 막는다.
            if (_combat.IsCancelWindowOpen
                && PlayerInterruptResolver.TryInterrupt(playerController, _currentAttack.interruptActions,
                    allowGuardCancel: _hasActiveHitFired))
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
            {
                ChangeToNextState();
                return;
            }

            // 이동 후딜 캔슬: 마지막 히트 페이즈 이후(리커버리) 구간에서 이동 입력이 들어오면
            // 모션 완료를 기다리지 않고 즉시 지상 이동으로 캔슬한다. 콤보(위)가 우선이며,
            // 윈드업/멀티히트 간격은 게이트(액티브 히트 1회 이상 + 마지막 페이즈 통과 + 콜리전 비활성)로 제외한다.
            // 콤보 윈도우가 열려 있는 동안엔 억제 — 스틱을 쥔 채 반응형으로 콤보를 이을 수 있게 하고,
            // 윈도우가 닫힌 리커버리 꼬리에서만 이동 캔슬을 허용한다.
            if ((_currentAttack.interruptActions & PlayerInterruptAction.Move) != 0
                && !_combat.CanCombo
                && _hasActiveHitFired
                && !_combat.IsPossibleCollide
                && _combat.CurrentHitPhaseIndex >= _combat.LastHitPhaseIndex
                && playerController.HasMoveInput())
            {
                _combat.ResetCombo();
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
        }

        private void ChangeToNextState()
        {
            _combat.ClearHitTargets();
            _attackTimer = 0f;
            _hasActiveHitFired = false;

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
            for (int i = 0; skillAllowed && i < PlayerSkillGauge.SkillSlotCount; i++)
            {
                if (!playerController.HasSkillInput(i)) continue;

                // 자원 소비 가능 여부만 먼저 확인한다(아직 소비하지 않음).
                if (skillGauge != null && !skillGauge.CanUseSkill(i))
                {
                    Debug.Log($"[PlayerAttackState] Skill {i + 1} 게이지 부족");
                    continue;
                }

                // resolve(ExecuteSkillAttack)가 실패하면 자원을 소비하지 않는다.
                // 정의 우선 정책상 정의가 있어도 Variant 조건이 모두 실패하면 null이 반환될 수 있어,
                // 소비를 발동 확정 이후로 미뤄 Ultimate 게이지/쿨다운이 헛소비되는 것을 막는다.
                _currentAttack = _combat.ExecuteSkillAttack(i);
                if (_currentAttack != null)
                    skillGauge?.ConsumeSkill(i);

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

            Transform preferredTarget = _combat.CurrentAttackPreferredTarget;
            if (_isSwapEvadeCounterAttack && preferredTarget != null)
                return preferredTarget;

            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(gameActor.transform.position, lockOnTarget.position);
                if (dist <= _combat.GetSnapSearchRange(true))
                    return lockOnTarget;
            }

            if ((_isEntryAttack || _isSwapSpecialAttack) && preferredTarget != null)
                return preferredTarget;

            bool isLockedOn = lockOnTarget != null;
            if (!isLockedOn)
                return _combat.FindFreeAttackFacingTarget(
                    _currentAttack.hitRange, _currentAttack.hitAngle);

            return _combat.FindAttackSnapTarget(
                _currentAttack.hitRange, _currentAttack.hitAngle, isLockedOn);
        }

        #region Movement & Rotation

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

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
                    0f,
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
