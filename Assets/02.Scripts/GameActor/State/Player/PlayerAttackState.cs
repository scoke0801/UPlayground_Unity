using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Path;
using UPlayGround.MovementController;
using UPlayGround.Manager;
using UPlayGround.InputDefine;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Stat;
using UPlayGround.Contracts.Ability;
using UPlayGround.Gameplay.Ability;

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
        public override ActorStateId StateId => ActorStateId.Attack;
        public override bool AllowsSameTypeReentry => true;
        public override bool CanReenterFrom(GameActorState currentState)
        {
            if (currentState is not PlayerAttackState currentAttack)
                return false;

            PlayerInterruptAction requestedType = _forcedAttackAction &
                (PlayerInterruptAction.LightAttack |
                 PlayerInterruptAction.HeavyAttack |
                 PlayerInterruptAction.Skill);

            if (requestedType == PlayerInterruptAction.None)
                return false;

            // 모션 재생 중 기본 약/강공격 재진입은 금지한다. 기본 연계는
            // ComboWindow만 소유하며, 재진입은 isCombo=false로 1타를 재시작한다.
            // 저작된 스킬 캔슬과 MotionSet 완료 경계의 새 체인 진입만 허용한다.
            if (currentAttack.gameActor.Animator.IsPlayingMotionSet)
                return (requestedType & PlayerInterruptAction.Skill) != 0;

            return true;
        }

        // 후딜(리커버리) 꼬리 구간에선 Combat에 Recovery를 합성해 적 AI가 Punish 기회로 인식하게 한다.
        protected override ActorStateTag StateTagsCore
            => IsInRecoveryTail() ? (ActorStateTag.Combat | ActorStateTag.Recovery) : ActorStateTag.Combat;
        public override bool SuppressesHitReaction => _isSwapEvadeCounterAttack || _isEntryAttack || _isSwapSpecialAttack;

        private PlayerCombat    _combat;
        private PlayerEquipment _equipment;

        private AttackData _currentAttack;
        private float      _attackTimer;

        private bool _comboInputted;
        private bool _comboContinuesSameType;
        private bool _hasPendingAttack;
        private bool _pendingAttackIsHeavy;
        // 현재 공격 모션에서 액티브 히트(콜리전)가 최소 1회 발생했는지. 이동 후딜 캔슬 게이트에 사용.
        // 단일 페이즈 공격은 윈드업에도 CurrentHitPhaseIndex == LastHitPhaseIndex == 0 이라
        // 페이즈 비교만으로는 윈드업을 못 거른다 → 히트 1회 발생 여부를 함께 본다.
        private bool _hasActiveHitFired;
        private float _lastActiveHitEndTime = -1f;
        private bool _wasActiveHit;
        private bool _isHeavyAttack;
        private bool _isCounter;
        private bool _isParryCounter;
        private bool _isSwapEvadeCounterAttack;
        private bool _isDodgeCounterAttack;
        private bool _isEntryAttack;
        private bool _isSwapSpecialAttack;
        private readonly PlayerInterruptAction _forcedAttackAction;
        private bool _hasConsumedForcedAttackAction;
        private AbilityExecutionHandle _abilityExecutionHandle;

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

        public override bool CanTransitionState(ActorStateId fromState)
        {
            if (fromState == ActorStateId.Hit) return false;
            return true;
        }

        /// <summary>
        /// 진입 후 재생할 공격 모션이 실제로 존재하는지 side effect 없이 미리 판정한다.
        /// GetMotion()와 동일한 우선순위 체인을 따라 다음 Motion를 미리 조회 후
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
                : PlayerAttackInputArbiter.IsHeavyPreferred();
            if (isHeavyPending && combat.FindFinishableTarget() != null)
                return true;

            MotionSetAsset peekedKey = PeekNextMotion(playerActor, controller, combat, isHeavyPending, forcedAttackAction);
            if (peekedKey == default) return false;

            return animator.HasMotion(peekedKey);
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
                : PlayerAttackInputArbiter.IsHeavyPreferred();
            if (isHeavyPending && combat != null)
            {
                Transform finishTarget = combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    controller.TransitionToState(new PlayerFinishAttackState(controller, finishTarget));
                    return true;
                }
            }

            var nextAttackState = hasForcedAttack
                ? new PlayerAttackState(controller, forcedAttackAction)
                : new PlayerAttackState(controller);

            // 공용 상태 머신이 재진입을 거절하는 경우를 성공으로 보고하지 않는다.
            // false를 반환해야 인터럽트 해석기가 입력을 삼키지 않고 정식 콤보 처리로 넘길 수 있다.
            if (controller.CurrentState is PlayerAttackState currentAttack
                && !nextAttackState.CanReenterFrom(currentAttack))
            {
                return false;
            }

            controller.TransitionToState(nextAttackState);
            return true;
        }

        /// <summary>
        /// GetMotion()의 우선순위 그대로 다음 Motion를 미리 산출 (side effect 없음).
        /// 0순위: 패리 반격 → 카운터 → 등장 공격 → 스킬 → 강/약 콤보.
        /// </summary>
        private static MotionSetAsset PeekNextMotion(
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
                    return combat.PeekParryCounterAttackMotion();

                // 1순위: 퍼펙트 가드 반격
                bool isCounter = combat.IsPerfectGuardCounterAvailable
                                 || (playerActor.Tags?.HasTag(GameplayTags.State_Combat_Counter) ?? false);
                if (isCounter)
                    return combat.PeekCounterAttackMotion();

                // 1순위: 회피 카운터 / 스왑 회피 카운터
                if (combat.IsDodgeCounterAvailable || playerActor.IsSwapEvadeCounterAttackPending)
                    return combat.PeekSwapEvadeCounterAttackMotion();

                // 2순위: 풀 게이지 교체 특수 공격
                if (playerActor.IsSwapSpecialAttackPending)
                    return combat.PeekSwapSpecialAttackMotion();

                // 3순위: 교체 등장 공격
                if (playerActor.IsEntryAttackPending)
                    return combat.PeekEntryAttackMotion();
            }

            // ★ 연계 라우트 판정(side effect 없음). GetMotion의 실행 우선순위와 맞춘다.
            {
                var route = ComboRouteRunner.ResolveRoute(playerActor, controller, combat,
                    isHeavyAttack, forcedAttackAction, recordToken: false);
                if (route != null)
                    return ResolveAttackMotion(playerActor, route.attackInfo);
            }

            if ((forcedAttackAction & PlayerInterruptAction.LightAttack) != 0)
                return combat.PeekNormalAttackMotion(false);

            if ((forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
                return combat.PeekHeavyAttackMotion(false);

            if ((forcedAttackAction & PlayerInterruptAction.Skill) != 0)
            {
                for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
                {
                    if (!controller.HasSkillInput(i)) continue;
                    if (playerActor.Abilities != null
                        && playerActor.Abilities.HasPlayerAbility((PlayerSkillSlot)i))
                    {
                        return TryPeekAbility(playerActor, controller, i, out MotionSetAsset abilityKey)
                            ? abilityKey
                            : default;
                    }

                    continue;
                }

                return default;
            }

            // 1순위: 숫자 키 스킬. 판정 권위는 GameplayAbility에 있다.
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (!controller.HasSkillInput(i)) continue;
                if (playerActor.Abilities != null
                    && playerActor.Abilities.HasPlayerAbility((PlayerSkillSlot)i))
                {
                    return TryPeekAbility(playerActor, controller, i, out MotionSetAsset abilityKey)
                        ? abilityKey
                        : default;
                }
                continue;
            }

            // 2순위: 기본 약/강 콤보. 콤보 입력 없는 첫 진입이므로 isCombo=false.
            return isHeavyAttack
                ? combat.PeekHeavyAttackMotion(false)
                : combat.PeekNormalAttackMotion(false);
        }

        private static bool TryPeekAbility(
            PlayerActor actor,
            PlayerMovementController controller,
            int skillSlot,
            out MotionSetAsset motionAsset)
        {
            motionAsset = default;
            if (actor?.Abilities == null
                || !System.Enum.IsDefined(typeof(PlayerSkillSlot), skillSlot)
                || !actor.Abilities.HasPlayerAbility((PlayerSkillSlot)skillSlot))
                return false;

            bool grounded = controller?.Motor == null
                            || controller.Motor.GroundingStatus.IsStableOnGround;
            AbilityActivationResult result = actor.Abilities.EvaluatePlayerSlot(
                (PlayerSkillSlot)skillSlot, grounded, null, out AbilityVariantDefinition variant);
            if (result != AbilityActivationResult.Success || variant == null)
                return false;
            return UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                       variant,
                       out AbilityAttackInfo attackInfo)
                   && ActorAbilityMotionResolver.TryResolve(
                       actor,
                       attackInfo,
                       out motionAsset);
        }

        private static MotionSetAsset ResolveAttackMotion(PlayerActor actor, AbilityAttackInfo attackInfo)
        {
            return ActorAbilityMotionResolver.TryResolve(
                actor,
                attackInfo,
                out MotionSetAsset motionAsset)
                ? motionAsset
                : null;
        }

        private PlayerInterruptAction GetCurrentAttackInputType()
        {
            if (_isCounter
                || _isParryCounter
                || _isSwapEvadeCounterAttack
                || _isDodgeCounterAttack
                || _isEntryAttack
                || _isSwapSpecialAttack)
            {
                return PlayerInterruptAction.None;
            }

            if (_abilityExecutionHandle.IsValid)
                return PlayerInterruptAction.Skill;

            return _isHeavyAttack
                ? PlayerInterruptAction.HeavyAttack
                : PlayerInterruptAction.LightAttack;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTags.State_Combat_Attack);

            ApplyAttackSpeed(1f);

            bool hasForcedAttack = _forcedAttackAction != PlayerInterruptAction.None;
            // 약/강 선입력이 둘 다 남아 있으면 "더 최근에 누른 쪽"이 이긴다.
            // (예전엔 강을 무조건 먼저 소비해, 버퍼에 남은 오래된 강 입력이 방금 누른 약 입력을 이겼다)
            _isHeavyAttack = !hasForcedAttack
                             && PlayerAttackInputArbiter.TryConsumeAttackInput(out bool consumedHeavy)
                             && consumedHeavy;

            _playerActorAnimator = playerActor.Animator as PlayerActorAnimator;
            _motionWarp = controller.MotionWarp;

            _combat    = playerActor.GetCombat();
            // 이전 공격이 CancelWindowEvent 종료 전에 잘렸을 때의 잔존 캔슬 윈도우 정리(stale 방지).
            _combat?.ResetCancelWindows();
            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            // 공격 중 FootIK를 끄지 않는다. IK on/off 전환 자체가 발을 ~38mm 튀게 하는 스냅 원인이었음.
            // 들린 발은 per-foot weight(_footLiftThreshold)가 자동으로 weight를 낮추므로 ForceDisable 불필요.
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            _isParryCounter = !hasForcedAttack && _combat.IsParryCounterAvailable;
            if (_isParryCounter)
            {
                _combat.CloseParryCounterWindow();
                Debug.Log("[ParryCounter] 패리 반격 진입");
            }

            bool hasPerfectGuardCounterTag = !hasForcedAttack
                                             && (gameActor.Tags?.HasTag(GameplayTags.State_Combat_Counter) ?? false);
            if (hasPerfectGuardCounterTag)
                gameActor.Tags?.RemoveTag(GameplayTags.State_Combat_Counter);

            // 퍼펙트 가드 반격은 카운터 윈도우가 1차 소스. ConsumePerfectGuardCounterWindow가
            // 윈도우를 닫으며 소비하므로 별도 Close 호출은 불필요하다.
            // 윈도우가 프레임 경계에서 만료된 경우에도 태그가 남아 있으면 반격을 성립시켜
            // PeekNextMotion의 (윈도우 OR 태그) 판정과 실행 분기를 일치시킨다.
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
                Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Attack);
                _isHeavyAttack = false;
            }
            else if ((_forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
            {
                // forced action은 직전 AttackState가 완료 경계에서 이미 후보를 확정한 값이다.
                // 버퍼 소비 결과로 타입을 다시 뒤집으면, 로컬에서 인계된 강공격이 약공격으로 변질된다.
                Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                _isHeavyAttack = true;
            }

            bool shouldResetCombo = !_isCounter
                                    && !_isParryCounter
                                    && !_isSwapEvadeCounterAttack
                                    && !_isEntryAttack
                                    && !_isSwapSpecialAttack
                                    && !_combat.CanUseStoredCombo(_isHeavyAttack);
            if (shouldResetCombo)
                // 진입 입력 하나는 위 중재기에서 이미 소비했다. 같은 프레임에 들어온 다음 연타까지
                // 지우지 않으면서, 공격 상태 재진입의 약/강 체인 분기 메모리는 보존한다.
                _combat.ResetComboPreserveChains();
            _attackTimer = 0f;
            _hasActiveHitFired = false;
            _lastActiveHitEndTime = -1f;
            _wasActiveHit = false;
            _comboContinuesSameType = true;

            if (_isHeavyAttack)
            {
                Transform finishTarget = _combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    controller.TransitionToState(new PlayerFinishAttackState(controller, finishTarget));
                    return;
                }
            }

            var animKey   = GetMotion();
            var animState = PlayCurrentAttackMotion(animKey, GetAttackBlendDuration(animKey));
            if (_isParryCounter)
                Debug.Log($"[ParryCounter] PlayMotion({animKey}) → {(animState != null ? "성공" : "실패(모션셋 없음)")}");

            if (animState != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            else
            {
                ChangeToNextState();
                return;
            }

            // 이 공격 모션 동안엔 첫 타겟만 유지 — 타임라인 워프 이벤트가 다른 적으로 재결정해
            // 한 타격 안에서 회전이 여러 번 튀는 것을 막는다. 다음 타겟팅은 다음 타격에서 다시 열린다.
            _motionWarp.BeginTargetLock();
            _homingTarget = FindHomingTarget();
            _motionWarp.SetTarget(_homingTarget);

            if (_isDodgeCounterAttack)
                CameraManager.Instance?.CombatCamera?.PlayDodgeCounter(_homingTarget, CameraShakeIdType.PlayerHit);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTags.State_Combat_Attack);

            // 공격 종료 시 열린 채 남은 캔슬 윈도우 정리(다음 상태로 누수 방지).
            _combat.ResetCancelWindows();
            _combat.ClearHitTargets();
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _playerActorAnimator.IsOpenedComboWindow = false;
            gameActor.Animator.MotionTimelineSpeed = 1f;
            gameActor.Animator.Speed = gameActor.LocalTimeScale;
            // 공격 진입 시 끄지 않으므로 여기서 다시 켤 필요도 없음 (IK는 계속 활성 유지).
            _homingTarget = null;
            _dodgeCounterTarget = null;
            _isDodgeCounterAttack = false;
            _motionWarp?.EndTargetLock();
            _motionWarp?.ClearTarget();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            if (_abilityExecutionHandle.IsValid)
            {
                // 이 AttackState가 시작한 실행만 종료한다. Ultimate처럼 새 Ability가
                // 이미 primary가 된 뒤 공격 상태가 빠져나갈 수 있으므로, "현재 활성"
                // Ability를 취소하면 새 Ultimate를 잘못 종료하게 된다.
                playerActor.Abilities?.EndAbility(_abilityExecutionHandle, false);
                _abilityExecutionHandle = default;
            }
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            // MotionSet은 완료 처리에서 먼저 재생 상태를 내린 뒤 완료 이벤트를 보낸다.
            // 다른 완료 구독자의 예외나 외부 중단으로 ChangeToNextState 콜백이 누락되더라도
            // 다음 프레임 Attack 상태에 영구 잔류하지 않도록 상태 측에서 종료를 보증한다.
            if (!gameActor.Animator.IsPlayingMotionSet)
            {
                ChangeToNextState();
                return;
            }

            _attackTimer += deltaTime * GetAttackSpeed();

            // 이번 공격 모션에서 액티브 히트가 한 번이라도 열렸는지 기록(이동 후딜 캔슬 게이트용).
            if (_combat.IsPossibleCollide)
                _hasActiveHitFired = true;
            if (_wasActiveHit && !_combat.IsPossibleCollide)
                _lastActiveHitEndTime = _attackTimer;
            _wasActiveHit = _combat.IsPossibleCollide;

            // 선입력은 전역 큐의 수명을 연장하지 않고 현재 공격의 단일 대기 슬롯으로 즉시 이관한다.
            CapturePendingAttackIntent();

            // 인터럽트(캔슬): 허용 액션·허용 구간을 ResolveCancelMask가 함께 산출한다.
            // 이동/방어계 캔슬은 콤보보다 우선하고, 약/강공격과 스킬은 아래의
            // 콤보 처리 후에 평가해 하나의 입력이 두 전환 경로에서 중복 소비되지 않게 한다.
            // allowGuardCancel: 가드(hold) 캔슬은 액티브 히트가 한 번이라도 발생한 뒤(리커버리/멀티히트 간격)에만
            // 허용한다. 초기 윈드업에서 가드를 쥔 채 시작하는 패리/카운터 반격이 곧바로 가드로 튕기는 걸 막는다.
            var cancelMask = _combat.ResolveCancelMask();
            const PlayerInterruptAction controlCancelMask =
                PlayerInterruptAction.Dodge
                | PlayerInterruptAction.Jump
                | PlayerInterruptAction.Dash
                | PlayerInterruptAction.Guard;
            PlayerInterruptAction allowedControlCancels = cancelMask & controlCancelMask;
            if (allowedControlCancels != PlayerInterruptAction.None
                && PlayerInterruptResolver.TryInterrupt(playerController, allowedControlCancels,
                    allowGuardCancel: _hasActiveHitFired))
                return;

            if (_combat.CanCombo)
            {
                if (!_comboInputted && _hasPendingAttack)
                {
                    bool continuesSameType = _pendingAttackIsHeavy == _isHeavyAttack;
                    bool hasNextComboHit = !continuesSameType
                                           || _combat.CanContinueStoredCombo(_isHeavyAttack);

                    if (hasNextComboHit)
                    {
                        _comboInputted = true;
                        _comboContinuesSameType = continuesSameType;
                        _isHeavyAttack = _pendingAttackIsHeavy;
                        _hasPendingAttack = false;
                        _combat.CloseComboWindow();
                    }
                    // 현재 타입의 막타라면 0번으로 즉시 래핑하지 않는다.
                    // 입력은 로컬 슬롯에 그대로 두고 MotionSet 완료 경계에서 체인을
                    // ResetCombo한 뒤 새 AttackState의 1타로 인계한다.
                }
            }

            // 기본 약/강공격은 AttackState의 ComboWindow가 단일 소유한다.
            // 공용 인터럽트가 먼저 소비하면 isCombo=false로 재진입해 매번 1타로 되감긴다.
            // 콤보가 받지 못한 스킬 입력만 저작된 캔슬로 넘긴다.
            // 기본 약/강공격은 ComboWindow 밖에서 AttackState를 재진입하지 않는다.
            if (!_comboInputted)
            {
                const PlayerInterruptAction offensiveCancelMask =
                    PlayerInterruptAction.Skill;
                PlayerInterruptAction allowedOffensiveCancels = cancelMask & offensiveCancelMask;
                if (allowedOffensiveCancels != PlayerInterruptAction.None
                    && PlayerInterruptResolver.TryInterrupt(
                        playerController,
                        allowedOffensiveCancels,
                        allowGuardCancel: false))
                {
                    return;
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
                && IsMoveCancelDelayElapsed()
                && !_combat.IsPossibleCollide
                && _combat.CurrentHitPhaseIndex >= _combat.LastHitPhaseIndex
                && playerController.HasMoveInput())
            {
                _combat.ResetCombo();
                controller.TransitionToState(ActorStateId.GroundMove);
            }
        }

        /// <summary>
        /// 전역 InputBuffer의 약/강 입력을 즉시 회수해 현재 공격이 소유하는
        /// 단일 "다음 공격" 슬롯에 보관한다. ComboWindow를 기다리며 전역 버퍼의
        /// 만료를 멈추지 않으므로 다른 액션의 수명을 연장하지 않는다.
        /// </summary>
        private void CapturePendingAttackIntent()
        {
            if (!PlayerAttackInputArbiter.TryConsumeAttackInput(out bool isHeavy))
                return;

            // 이미 다음 콤보가 확정된 후의 추가 연타는 소비만 하고 적재하지 않는다.
            // 현재 타격 하나가 미래 타격 여러 개를 예약할 수 없게 하는 상한이다.
            if (_comboInputted)
                return;

            _hasPendingAttack = true;
            _pendingAttackIsHeavy = isHeavy;
        }

        // 후딜 꼬리 진입 후 이 시간만큼 지속돼야 Recovery로 노출한다. 버퍼된 콤보는 이 시간 안에
        // 다음 공격으로 전환되므로, 캔슬창을 후딜로 오인해 적 Punish 빈도가 콤보마다 누적되는 것을 막는다.
        private const float RecoveryRevealDelay = 0.1f;

        /// <summary>
        /// 공격의 후딜(리커버리) 꼬리 구간인지. 마지막 히트 페이즈를 지나 히트박스 콜리전이 닫히고,
        /// 캔슬/콤보 없이 RecoveryRevealDelay 이상 지속(=플레이어가 후딜에 커밋)된 상태.
        /// 이동 후딜 캔슬 게이트(UpdateState)와 동일한 기준에 dwell 조건을 더한 것이며,
        /// ActorStateTag.Recovery로 노출해 적 AI의 IsPlayerRecovering(=Punish 가중치)이 실제 후딜을 잡도록 한다.
        /// </summary>
        private bool IsInRecoveryTail()
            => _combat != null
               && _hasActiveHitFired
               && !_combat.IsPossibleCollide
               && _combat.CurrentHitPhaseIndex >= _combat.LastHitPhaseIndex
               && _lastActiveHitEndTime >= 0f
               && _attackTimer - _lastActiveHitEndTime >= RecoveryRevealDelay;

        private void ChangeToNextState()
        {
            // 모션 완료 이벤트가 UpdateState보다 먼저 실행된 프레임의 입력도
            // 전역 버퍼에 남기지 않고 로컬 슬롯으로 회수한다.
            CapturePendingAttackIntent();

            if (_abilityExecutionHandle.IsValid)
            {
                // 완료 시점에도 이 상태가 소유한 핸들만 닫는다. 상태 종료와 새 Ability
                // 시작이 같은 프레임에 겹쳐도 새 primary 실행을 건드리지 않는다.
                playerActor.Abilities?.EndAbility(_abilityExecutionHandle, true);
                _abilityExecutionHandle = default;
            }
            _combat.ClearHitTargets();
            _attackTimer = 0f;
            _hasActiveHitFired = false;
            _lastActiveHitEndTime = -1f;
            _wasActiveHit = false;

            // 대기 중인 강 입력으로 피니시가 가능한 경우에만 여기서 소비한다.
            // 피니시 대상이 없는데 미리 소비하면 새 강공 체인을 시작할 입력이 사라진다.
            if (!_comboInputted && _hasPendingAttack && _pendingAttackIsHeavy)
            {
                Transform finishTarget = _combat.FindFinishableTarget();
                if (finishTarget != null)
                {
                    _hasPendingAttack = false;
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
                bool continueCombo = _comboContinuesSameType;

                // 다음 콤보 키를 미리 조회해 보유 여부를 확인.
                // 모션이 없으면 콤보 인덱스를 진행시키지 않고 Idle/Move로 이탈.
                // 연계 라우트가 매칭되면 라우트 모션으로 판정(기본 콤보 리스트가 비어 있어도
                // 라우트 진입이 막히지 않도록 — 콤보 연속 입력은 약/강만 가능).
                var peekRoute = ComboRouteRunner.ResolveRoute(
                    playerActor, playerController, _combat,
                    _isHeavyAttack, PlayerInterruptAction.None, recordToken: false);

                MotionSetAsset peekedKey = peekRoute != null
                    ? ResolveAttackMotion(playerActor, peekRoute.attackInfo)
                    : (_isHeavyAttack ? _combat.PeekHeavyAttackMotion(continueCombo)
                                      : _combat.PeekNormalAttackMotion(continueCombo));

                if (peekedKey == null || !gameActor.Animator.HasMotion(peekedKey))
                {
                    _comboInputted = false;
                    _combat.ResetCombo();
                    if (playerController.HasMoveInput())
                        controller.TransitionToState(ActorStateId.GroundMove);
                    else
                        controller.TransitionToState(ActorStateId.Idle);
                    return;
                }

                gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
                MotionSetAsset animKey = GetMotion();
                // 콤보 간 0.25초 고정 블렌드는 짧은 공격에서 정지 프레임처럼 보인다.
                // MotionSet에 저작된 내부 블렌드 시간을 연속 공격 전환에도 사용한다.
                var animState = PlayCurrentAttackMotion(animKey, GetAttackBlendDuration(animKey));
                if (animState == null)
                {
                    // Peek 이후 런타임 해석/재생이 실패해도 완료 콜백 없는 Attack 상태에
                    // 영구 잔류하지 않도록 즉시 안전 상태로 복귀한다.
                    _comboInputted = false;
                    _combat.ResetCombo();
                    controller.TransitionToState(
                        playerController.HasMoveInput() ? ActorStateId.GroundMove : ActorStateId.Idle);
                    return;
                }

                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
                _playerActorAnimator.IsOpenedComboWindow = false;
                _combat.CloseComboWindow();
                _comboInputted = false;
                _comboContinuesSameType = true;
                // 콤보 다음 타격 = 새 공격 스코프. 여기서만 타겟을 다시 잡는다.
                _motionWarp.BeginTargetLock();
                _homingTarget = FindHomingTarget();
                _motionWarp.SetTarget(_homingTarget);
            }
            else
            {
                _combat.ResetCombo();

                // 콤보 윈도우가 닫힌 뒤 들어온 입력과 막타에서 받은 다음 입력은 MotionSet 완료
                // 경계에서 체인을 완전히 초기화한 다음 새 AttackState의 1타로 연결한다.
                // 완주 체인을 같은 상태에서 0번으로 래핑하면 다음 콤보 창의 수명주기가 이전
                // 시퀀스에 묶여, 이후 공격이 1타씩만 반복되는 문제가 생긴다.
                if (TryRestartPendingAttackChain())
                    return;

                if (playerController.HasMoveInput())
                    controller.TransitionToState(ActorStateId.GroundMove);
                else
                    controller.TransitionToState(ActorStateId.Idle);
            }
        }

        private bool TryRestartPendingAttackChain()
        {
            if (!_hasPendingAttack)
                return false;

            PlayerInterruptAction action = _pendingAttackIsHeavy
                ? PlayerInterruptAction.HeavyAttack
                : PlayerInterruptAction.LightAttack;
            _hasPendingAttack = false;
            return TryEnter(playerController, action);
        }

        private static float GetAttackBlendDuration(MotionSetAsset motionAsset)
        {
            return motionAsset?.motionSet?.InternalBlendDuration ?? 0f;
        }

        private MotionSetAsset GetMotion()
        {
            // 강제 입력은 AttackState 진입을 확정한 최초 공격에만 적용한다.
            // 막타 뒤 pending 입력으로 새 체인을 시작하면 같은 상태 인스턴스에서 후속 콤보도
            // 재생되므로, 필드를 계속 참조하면 승인된 2타 이후에도 매번 1타(false)로 실행된다.
            PlayerInterruptAction forcedAttackAction = _hasConsumedForcedAttackAction
                ? PlayerInterruptAction.None
                : _forcedAttackAction;
            _hasConsumedForcedAttackAction = true;

            // 0순위: 패리 반격
            if (_isParryCounter)
            {
                _currentAttack = _combat.ExecuteParryCounterAttack();
                return _currentAttack?.motionAsset ?? null;
            }

            // 1순위: 퍼펙트 가드 반격
            if (_isCounter)
            {
                _currentAttack = _combat.ExecuteCounterAttack();
                return _currentAttack?.motionAsset ?? null;
            }

            // 1순위: 스왑 회피 카운터
            if (_isSwapEvadeCounterAttack)
            {
                _currentAttack = _combat.ExecuteSwapEvadeCounterAttack();
                return _currentAttack?.motionAsset ?? null;
            }

            // 2순위: 풀 게이지 교체 특수 공격
            if (_isSwapSpecialAttack)
            {
                _currentAttack = _combat.ExecuteSwapSpecialAttack();
                return _currentAttack?.motionAsset ?? null;
            }

            // 3순위: 교체 등장 공격
            if (_isEntryAttack)
            {
                _currentAttack = _combat.ExecuteEntryAttack();
                return _currentAttack?.motionAsset ?? null;
            }

            // ★ 연계 라우트 — forced/normal 공통 단일 판정점 (설계 §5.3, advisor #1).
            //   "약약약→강"의 강공은 HeavyAttack 인터럽트(forced)로 들어와 아래 forced 분기로
            //   빠지므로, forced 분기보다 '앞'에서 라우트를 가로채야 한다.
            //   여기서 pending 토큰을 트래커에 1회 push(기록)하고, 매칭 시 라우트를 실행한다.
            {
                var routeAttack = ComboRouteRunner.TryExecuteRoute(playerActor, playerController, _combat,
                    _isHeavyAttack, forcedAttackAction, out var routeMotion);
                if (routeAttack != null)
                {
                    _currentAttack = routeAttack;
                    return routeMotion;
                }
            }

            if ((forcedAttackAction & PlayerInterruptAction.LightAttack) != 0)
            {
                _currentAttack = _combat.ExecuteAttack(false);
                return _currentAttack?.motionAsset ?? default;
            }

            if ((forcedAttackAction & PlayerInterruptAction.HeavyAttack) != 0)
            {
                _currentAttack = _combat.ExecuteHeavyAttack(false);
                return _currentAttack?.motionAsset ?? default;
            }

            // 1순위: 숫자 키 스킬
            bool skillAllowed = forcedAttackAction == PlayerInterruptAction.None
                                || (forcedAttackAction & PlayerInterruptAction.Skill) != 0;
            for (int i = 0; skillAllowed && i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (!playerController.HasSkillInput(i)) continue;

                if (playerActor.Abilities != null
                    && playerActor.Abilities.HasPlayerAbility((PlayerSkillSlot)i))
                {
                    bool grounded = playerController.Motor == null
                                    || playerController.Motor.GroundingStatus.IsStableOnGround;
                    AbilityActivationResult prepareResult =
                        playerActor.Abilities.TryPreparePlayerSlot(
                            (PlayerSkillSlot)i,
                            grounded,
                            null,
                            out AbilityExecutionHandle prepared,
                            out AbilityVariantDefinition variant);
                    if (prepareResult != AbilityActivationResult.Success)
                    {
                        Debug.Log($"[PlayerAttackState] Ability {i + 1} 활성화 실패: {prepareResult}");
                        continue;
                    }

                    _currentAttack = _combat.ExecuteAbilityAttack(variant);
                    if (_currentAttack == null)
                    {
                        playerActor.Abilities.Abort(prepared);
                        continue;
                    }

                    AbilityActivationResult commitResult = playerActor.Abilities.Commit(prepared);
                    if (commitResult != AbilityActivationResult.Success)
                    {
                        playerActor.Abilities.Abort(prepared);
                        _currentAttack = null;
                        continue;
                    }

                    _abilityExecutionHandle = prepared;
                    return _currentAttack.motionAsset;
                }

                Debug.LogWarning(
                    $"[PlayerAttackState] 슬롯 {i}에 GameplayAbility가 없습니다.");
                continue;
            }

            // 2순위: 기본 약/강 콤보
            bool continueCombo = !_comboInputted || _comboContinuesSameType;
            _currentAttack = _isHeavyAttack
                ? _combat.ExecuteHeavyAttack(continueCombo && _comboInputted)
                : _combat.ExecuteAttack(continueCombo && _comboInputted);

            return _currentAttack?.motionAsset ?? default;
        }

        private Animancer.AnimancerState PlayCurrentAttackMotion(MotionSetAsset motionAsset, float fadeDuration)
        {
            return motionAsset != null
                ? gameActor.Animator.PlayMotion(motionAsset, fadeDuration)
                : null;
        }

        private bool IsMoveCancelDelayElapsed()
        {
            float delay = _currentAttack != null ? Mathf.Max(0f, _currentAttack.moveCancelDelayAfterLastHit) : 0f;
            if (delay <= 0f) return true;
            if (_lastActiveHitEndTime < 0f) return false;
            return _attackTimer - _lastActiveHitEndTime >= delay;
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
            float warpSearchRange = Mathf.Max(_combat.GetSnapSearchRange(lockOnTarget != null), _combat.WarpMaxDistance);
            if (lockOnTarget != null)
            {
                float dist = HorizontalDistance(gameActor.transform.position, lockOnTarget.position);
                if (dist <= warpSearchRange)
                    return lockOnTarget;
            }

            if ((_isEntryAttack || _isSwapSpecialAttack) && preferredTarget != null)
                return preferredTarget;

            bool isLockedOn = lockOnTarget != null;
            return _combat.FindMotionWarpTarget(
                isLockedOn,
                warpSearchRange);
        }

        #region Movement & Rotation

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            Vector3 authoritativeVelocity = currentVelocity;

            // 워프 구간에서 클립 재생 속도를 타겟 거리 비율로 보정해 풋슬라이딩 감소.
            float playbackScale = _combat.IsMotionWarping
                ? _motionWarp.WarpPlayRateScale
                : 1f;
            ApplyAttackSpeed(playbackScale);

            Vector3 rootVelocity = gameActor.Animator.GetRootMotionStepVelocity(deltaTime);
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

            // 지상 공격의 애니메이션/워프 Y는 KCC 탄도를 소유하지 않는다.
            // 착지 직후 클립의 Root Y가 상승 속도로 변환되는 현상을 차단한다.
            currentVelocity = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                currentVelocity,
                authoritativeVelocity,
                motor.CharacterUp);
        }

        private float GetAttackSpeed()
        {
            return playerActor.AbilitySystem != null
                && playerActor.AbilitySystem.TryGetAttribute(
                    global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed,
                       current: true,
                       out float attackSpeed)
                ? Mathf.Clamp(attackSpeed, 0.1f, 5f)
                : 1f;
        }

        private void ApplyAttackSpeed(float playbackScale)
        {
            float attackSpeed = GetAttackSpeed();
            gameActor.Animator.MotionTimelineSpeed = attackSpeed;
            gameActor.Animator.Speed = Mathf.Max(0.1f, playbackScale) * attackSpeed * gameActor.LocalTimeScale;
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
                currentRotation *= gameActor.Animator.RootMotionStepDeltaRotation;
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
