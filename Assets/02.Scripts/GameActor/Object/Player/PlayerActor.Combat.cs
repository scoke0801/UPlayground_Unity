using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround
{
    // IDamageable
    public partial class PlayerActor : GameActor, IDamageable
    {
        internal event Action<PassiveActivationType> PassiveActivationSucceeded;

        internal void NotifyPassiveActivation(PassiveActivationType activationType)
            => PassiveActivationSucceeded?.Invoke(activationType);

        public CombatResult ReceiveHit(in HitRequest request)
            => CombatResolutionPipeline.Execute(this, request);

        internal PlayerDefenseQuery BuildCombatDefenseQuery()
            => CreatePlayerDefenseQuery();

        public bool CanResolveHit(in HitRequest request) => true;

        public CombatResult ResolveHit(in HitRequest request)
            => CombatResolutionPipeline.ResolvePlayerHit(this, request, BuildCombatDefenseQuery());

        public CombatResult ApplyResolvedHit(in HitRequest request, in CombatResult combatResult)
        {
            AttackData attackData = request.ToReactionData();

            switch (combatResult.DefenseOutcome)
            {
                case DefenseOutcome.Guarded:
                    if (MovementController.CurrentState is not PlayerGuardState guardState)
                        return combatResult;

                    guardState.OnAttackBlocked(attackData);

                    if (!_combat.IsGuarding)
                        return OnGuardBrokenDamage(request);
                    return combatResult;

                case DefenseOutcome.Parried:
                    OnParrySuccess(attackData);
                    return combatResult;

                case DefenseOutcome.PerfectDodged:
                    TryPerfectDodge(attackData);
                    return combatResult;

                case DefenseOutcome.Invincible:
                    TryDashEvadeFeedback(attackData);
                    return combatResult;
            }

            DamageResult damageResult = combatResult.Damage;
            float finalDamage = combatResult.FinalDamage;

            AbilitySystem.ApplyResolvedDamage(finalDamage, request.Attacker?.AbilitySystem);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            _behaviorPredictor?.NotifyAction(PlayerActionToken.Hit);

            CombatFeedbackDispatcher.ShowDamageFloater(
                CombatFeedbackContext.FromCombatResult(combatResult, transform.position));
            CombatFeedbackDispatcher.PlayDamageImpact(combatResult);

            if (_currentHealth <= 0)
            {
                OnDeath(attackData);
                return combatResult;
            }

            ReactionDecision reactionDecision = OnDamaged(attackData, combatResult.Hit);
            GameplayTag triggerTag = ResolvePlayerHitTrigger(
                combatResult.Hit.ReactionType,
                reactionDecision);
            if (triggerTag.IsValid())
            {
                WarnIfReactionAbilityMissing(triggerTag, reactionDecision);
                Abilities?.IssueTriggerEvent(
                    triggerTag,
                    combatResult.Hit.Attacker,
                    this,
                    new HitReactionTriggerPayload(
                        combatResult.Hit,
                        reactionDecision.TargetState,
                        attackData));
            }
            return CombatResolutionPipeline.WithReaction(combatResult, reactionDecision);
        }

        public bool      IsAlive()          => _currentHealth > 0;
        public Transform GetTransform()     => transform;
        public void      LockOn()           { }
        public void      UnLockOn()         { }
        public float     GetHealthPercent() => _currentHealth / _maxHealth;
        public float     GetCurrentHealth() => _currentHealth;

        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        public bool CanTakeDamage()
            => IsAlive()
               && !_isInvincible
               && !IsSwapEvadeInvincible
               && !MovementController.CurrentState.GrantsInvincibility;

        public void ApplyHealingEffect(float amount)
        {
            if (!IsAlive()) return;
            float old = _currentHealth;
            AbilitySystem.ApplyHealing(amount);
            if (_currentHealth > old)
            {
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                ActorSvc.UI.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
            }
        }

        public void ApplyPercentHealingEffect(float ratio)
        {
            if (!IsAlive()) return;
            float old = _currentHealth;
            AbilitySystem.ApplyHealing(0f, ratio);
            if (_currentHealth > old)
            {
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                ActorSvc.UI.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
            }
        }

        private PlayerDefenseQuery CreatePlayerDefenseQuery()
        {
            bool alwaysParry = ActorSvc.CheatState?.IsAlwaysParryEnabled ?? false;
            bool isAttackState = MovementController.CurrentState.StateId == ActorStateId.Attack;
            bool isCurrentAttackParryCapable = _combat.CurrentAttackData?.attackKind == AttackKind.NormalAttack;

            return new PlayerDefenseQuery(
                _combat.IsGuarding,
                MovementController.CurrentState is PlayerGuardState,
                isAttackState,
                _combat.IsPossibleCollide,
                isCurrentAttackParryCapable,
                MovementController.CurrentState is PlayerDodgeState,
                _combat.IsPerfectDodgeWindow,
                CanTakeDamage(),
                alwaysParry,
                _combat.IsAssistParryWindow,
                Definition != null ? Definition.EffectiveCombatDefensePolicy : null);
        }

        private void OnParrySuccess(AttackData attackData)
        {
            // 어시스트 패리(§4.3)로 성립한 패리면 어시스트 창을 닫고 폴백(즉시공격)을 취소한다.
            // (일반 클래시 패리/퍼펙트 가드 반격창과 중복 발동 방지 = 보존 제약)
            if (_combat.IsAssistParryWindow)
            {
                _combat.CloseAssistParryWindow();
                _assistParryFallbackPending = false;
                Debug.Log("[PlayerActor] 어시스트 패리 성공!");
            }
            else
            {
                Debug.Log("[PlayerActor] 패리 성공!");
            }

            // 패리 반격 창을 먼저 열어둬야 상태 전환 후 반격 입력을 받을 수 있다
            _combat.OpenParryCounterWindow(
                GameCombatMgr?.GetCounterWindowDuration(DefenseSuccessType.Parry, this) ?? -1f);
            _combat.NotifyDefenseSucceeded(DefenseSuccessType.Parry);

            // 히트 감지를 즉시 비활성화해 이후 PerformHitDetection이 HitStop을 덮어쓰지 않도록 한다
            _combat.SetEnableCollision(false);

            // 공격 상태를 중단하고 Idle로 복귀 (패리 반격 창은 이미 열려 있으므로 다음 공격 입력 시 반격 발동)
            MovementController.TransitionToState(ActorStateId.Idle);

            Vector3 fxPos = TryGetSocket(ActorSocketType.Weapon, out var center)
                ? center.position
                : (attackData?.hitPoint ?? Vector3.zero) != Vector3.zero
                    ? attackData.hitPoint
                    : transform.position;

            // 공격자(몬스터) 경직
            if (attackData?.attacker is MonsterActor monster)
                monster.OnParried();

            GameCombatMgr?.PlayDefenseSuccess(
                DefenseSuccessType.Parry,
                this,
                attackData?.attacker,
                attackData,
                fxPos,
                _parryFxName);
        }

        /// <summary>
        /// 도지 중 피격 시도 시 호출. 퍼펙트 도지 판정 창 내면 보상 효과를 발동한다.
        /// </summary>
        private void TryPerfectDodge(AttackData attackData)
        {
            if (!_combat.IsPerfectDodgeWindow) return;

            // 퍼펙트 도지 성공 — 창 즉시 닫아 중복 발동 방지
            _combat.ClosePerfectDodgeWindow();
            _swapBehaviour?.RevealEvadeAfterimage();

            _combat.OpenDodgeCounterWindow(
                attackData,
                GameCombatMgr?.GetCounterWindowDuration(DefenseSuccessType.PerfectDodge, this) ?? -1f);
            _combat.NotifyDefenseSucceeded(DefenseSuccessType.PerfectDodge);

            Vector3 feedbackPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;

            GameCombatMgr?.PlayDefenseSuccess(
                DefenseSuccessType.PerfectDodge,
                this,
                attackData?.attacker,
                attackData,
                feedbackPos);

            NotifyPassiveActivation(PassiveActivationType.PerfectDodge);
            Debug.Log("[PlayerActor] 퍼펙트 도지 성공!");
        }

        internal void PrepareEvadeAfterimage()
            => _swapBehaviour?.PrepareEvadeAfterimage();

        internal void CancelEvadeAfterimage()
            => _swapBehaviour?.CancelEvadeAfterimage();

        /// <summary>
        /// Dash로 적 공격을 회피했을 때 타임스케일/카메라 연출을 발동한다.
        /// 퍼펙트 도지 피드백 핸들러를 재사용하되, 대시는 반격 창을 열지 않는다(연출만).
        /// 회피 판정 자체는 Dash가 GrantsInvincibility라 DefenseOutcome.Invincible로 들어온다.
        /// </summary>
        private void TryDashEvadeFeedback(AttackData attackData)
            => TryDashEvadeFeedback(attackData?.attacker, attackData);

        /// <summary>
        /// 대시 중 위협 스캔(<see cref="EnemyThreatScanner"/>)으로 회피가 성립했을 때 호출.
        /// 실제 피격이 없었으므로 AttackData는 없고 위협 소스만 전달한다.
        /// </summary>
        internal void TryDashEvadeFeedback(in EnemyAttackThreat threat)
            => TryDashEvadeFeedback(threat.Source, null);

        /// <summary>
        /// 대시 회피 피드백의 단일 진입점.
        /// 피격 기반(DefenseOutcome.Invincible)과 위협 스캔 기반이 모두 여기로 모이며,
        /// 대시 1회당 1번만 발동한다(PlayerDashState.TryConsumeEvadeFeedback).
        /// </summary>
        private void TryDashEvadeFeedback(GameActor attacker, AttackData attackData)
        {
            if (MovementController.CurrentState is not PlayerDashState dashState) return;
            if (!dashState.TryConsumeEvadeFeedback()) return;

            _swapBehaviour?.RevealEvadeAfterimage();

            if (GameCombatMgr == null) return;

            Vector3 feedbackPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;

            // 대시 회피는 포스트프로세스(볼륨) 플래시 없이 타임스케일 슬로우만 또렷하게 발동한다.
            GameCombatMgr.PlayDashEvade(
                this,
                attacker,
                attackData,
                feedbackPos);

            Debug.Log("[PlayerActor] 대시 회피 피드백 발동!");
        }

        /// <summary>
        /// 가드 브레이크 시 호출.
        /// GuardBreakState가 경직·애니를 담당하므로 State 전환 없이 데미지·피드백만 처리한다.
        /// </summary>
        private CombatResult OnGuardBrokenDamage(in HitRequest request)
        {
            if (!CanTakeDamage()) return default;

            AttackData attackData = request.ToReactionData();
            CombatResult combatResult = CombatResolutionPipeline.ResolvePlayerGuardBreakDamage(this, request);
            DamageResult damageResult = combatResult.Damage;
            float finalDamage = combatResult.FinalDamage;

            AbilitySystem.ApplyResolvedDamage(finalDamage, request.Attacker?.AbilitySystem);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            CombatFeedbackDispatcher.ShowDamageFloater(
                CombatFeedbackContext.FromCombatResult(combatResult, transform.position));

            CameraMgr.StartShake(_shakeKeyHeavyHit);

            CombatFeedbackDispatcher.ShowHitFx(
                attackData.hitParticleName,
                ResolveHitFxPosition(attackData.hitPoint),
                attackData.attackDirection);
            CombatFeedbackDispatcher.PlayDamageImpact(combatResult);

            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);

            if (_currentHealth <= 0)
                OnDeath(attackData);
            return combatResult;
        }

        /// <summary>
        /// 넉백/에어본 임펄스에 사용할 수평 방향. attackDirection은 히트박스 스윕 델타에서 유도되어
        /// 수직 성분을 포함할 수 있으므로 그대로 쓰면 ForceUnground로 인해 플레이어가 솟구친다.
        /// </summary>
        private Vector3 ResolveKnockbackDirection(AttackData attackData)
        {
            return KnockbackDirectionResolver.ResolveHorizontal(
                attackData.attackDirection,
                attackData.attacker != null ? attackData.attacker.transform : null,
                transform,
                MovementController != null && MovementController.Motor != null
                    ? MovementController.Motor.CharacterUp
                    : Vector3.up);
        }

        /// <summary>
        /// 피격 시 호출.
        /// 쉐이크 강도는 AttackReactionType으로 결정한다.
        /// </summary>
        protected virtual ReactionDecision OnDamaged(AttackData attackData, in HitContext hit)
        {
            // 슈퍼아머 체크: 한 단계 이상 차징 완료 시 물리 충격(밀려남) 및 상태 전환 무시
            bool hasSuperArmor = MovementController.CurrentState is PlayerChargeState chargeState &&
                                 chargeState.HasChargedAtLeastOneStage;
            bool suppressHitReaction = MovementController.CurrentState.SuppressesHitReaction;
            bool ignoreHitReaction = hasSuperArmor || suppressHitReaction;
            ActorStateId stateId = MovementController.CurrentState.StateId;
            ReactionDecision reactionDecision = ReactionResolver.ResolvePlayerReaction(
                new PlayerReactionQuery(
                    ignoreHitReaction,
                MovementController.CurrentState.CanTransitionState(ActorStateId.Hit),
                    stateId is ActorStateId.Hit or ActorStateId.Grabbed,
                    ShouldEnterAirborneState(attackData),
                    IsStaggerImmune),
                hit);

            if (reactionDecision.ShouldApplyForce && attackData != null)
            {
                switch (attackData.reactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddPlanarKnockback(
                            ResolveKnockbackDirection(attackData) * attackData.knockbackForce,
                            attackData.knockbackDrag);
                        break;

                    case AttackReactionType.Pull:
                        if (attackData.attacker != null)
                        {
                            Vector3 pullDir = (attackData.attacker.transform.position - transform.position).normalized;
                            pullDir.y = 0f;
                            MovementController.QueueVelocityChange(pullDir * attackData.pullForce);
                        }

                        break;

                    case AttackReactionType.Airborne:
                    {
                        Vector3 launchDir = ResolveKnockbackDirection(attackData);
                        Vector3 planarVelocity =
                            launchDir * attackData.knockbackForce;
                        if (ShouldEnterAirborneState(attackData))
                        {
                            MovementController.AddLaunch(
                                attackData.airborneForce,
                                planarVelocity,
                                attackData.knockbackDrag,
                                VerticalLaunchVelocityPolicy.Replace);
                        }
                        else
                        {
                            MovementController.AddPlanarKnockback(
                                planarVelocity,
                                attackData.knockbackDrag);
                        }
                        break;
                    }

                    case AttackReactionType.Grab:
                        break;
                }
            }

            if (reactionDecision.ShouldEnterState)
            {
                // 리액션 상태 전환은 태그 트리거 Ability 경로가 단독으로 수행한다(저작 축 단일화).

                if (reactionDecision.TargetState is CombatReactionState.Hit
                    or CombatReactionState.Stun
                    or CombatReactionState.Knockdown
                    && hit.Attacker is MonsterActor monsterAttacker)
                {
                    monsterAttacker.AIController?.Group?.NotifyPlayerEnteredHitReaction();
                }
            }

            if (reactionDecision.ShouldPlayCameraFeedback)
            {
                bool isHeavyReaction = attackData?.reactionType is
                    AttackReactionType.Heavy or
                    AttackReactionType.KnockBack or
                    AttackReactionType.Airborne or
                    AttackReactionType.Knockdown or
                    AttackReactionType.Stun;

                CombatFeedbackDispatcher.ApplyPlayerDamagedCamera(
                    isHeavyReaction,
                    _shakeKeyHit,
                    _shakeKeyHeavyHit);
            }

            // 경직 내성으로 흡수된 약한 피격(Light/Hit)은 히트스톱도 생략한다.
            // 그러지 않으면 리액션은 억제돼도 LocalTimeScale이 freeze/clear로 깜빡여
            // 흡수 구간 조작감이 끊긴다("데미지 O·경직 X" 의도를 체감으로 완성).
            // 컬러 플래시·HitFx는 아래에서 그대로 유지해 피격 자체는 시각 피드백한다.
            bool absorbedByStaggerImmunity = IsStaggerImmune
                && attackData != null
                && ReactionResolver.IsMinorPlayerReaction(attackData.reactionType);
            if (!absorbedByStaggerImmunity)
                CombatFeedbackDispatcher.ApplyPlayerDamagedHitStop(attackData, this);

            CombatFeedbackDispatcher.ShowHitFx(
                attackData?.hitParticleName,
                ResolveHitFxPosition(attackData?.hitPoint),
                attackData?.attackDirection ?? Vector3.zero);

            // 충돌음은 이 메서드의 호출자(ReceiveHit)가 이미 재생했다. 여기서 다시 부르면 이중 재생이다.
            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);
            return reactionDecision;
        }

        /// <summary>
        /// 히트 FX 위치. 타격 지점을 우선하고, 지정되지 않은 경우에만 몸통 중심으로 폴백한다.
        /// (Center 소켓을 항상 쓰면 FX가 실제 맞은 지점과 어긋난다.)
        /// </summary>
        private Vector3 ResolveHitFxPosition(Vector3? hitPoint)
        {
            if (hitPoint.HasValue && hitPoint.Value != Vector3.zero)
                return hitPoint.Value;

            return TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;
        }

        /// <summary>
        /// 리액션이 필요한데 해당 트리거를 받을 Ability가 AbilitySet에 없으면 알린다.
        /// 직접 전환 폴백이 사라졌으므로 이 경우 플레이어는 피해만 받고 아무 반응도 하지 않는다 —
        /// 증상이 "안 맞은 것처럼 보임"이라 조용히 넘어가면 추적이 매우 어렵다.
        /// </summary>
        private void WarnIfReactionAbilityMissing(
            GameplayTag triggerTag,
            in ReactionDecision reactionDecision)
        {
            if (!reactionDecision.ShouldEnterState)
                return;

            if (Abilities != null
                && Abilities.TryGetRequestTriggerAbility(triggerTag, out _))
            {
                return;
            }

            // 스왑으로 AbilitySet이 통째로 바뀌므로 캐릭터별로 따로 센다.
            // 인스턴스당 1회로 두면 두 번째 캐릭터의 누락이 첫 경고에 묻힌다.
            if (!_warnedReactionTriggers.Add($"{_characterActorType}:{triggerTag.TagName}"))
                return;

            Debug.LogError(
                $"[PlayerActor] '{gameObject.name}'의 AbilitySet에 피격 리액션 Ability가 없어 "
                + $"리액션이 재생되지 않습니다. trigger={triggerTag.TagName}. "
                + "GA_Player_Hit_* Ability를 AbilitySet에 추가하세요.",
                this);
        }

        /// <summary>
        /// 실행될 리액션 Ability와 실제 진입 상태의 범주를 맞춘다.
        /// 태그를 공격의 리액션 타입에서만 뽑으면 승격 조건 미달로 상태가 강등된 경우
        /// (예: Airborne 요청이지만 Hit 상태) 엉뚱한 Ability가 실행된다.
        /// </summary>
        private static GameplayTag ResolvePlayerHitTrigger(
            AttackReactionType reactionType,
            in ReactionDecision reactionDecision)
        {
            if (!reactionDecision.ShouldEnterState)
                return ResolvePlayerHitTrigger(reactionType);

            return reactionDecision.TargetState switch
            {
                CombatReactionState.Airborne => GameplayTags.Trigger_Player_Hit_Airborne,
                CombatReactionState.Grabbed => GameplayTags.Trigger_Player_Hit_Grab,
                CombatReactionState.Knockdown => GameplayTags.Trigger_Player_Hit_Knockdown,
                CombatReactionState.Stun => GameplayTags.Trigger_Player_Hit_Stun,
                _ => reactionType switch
                {
                    AttackReactionType.Light => GameplayTags.Trigger_Player_Hit_Light,
                    AttackReactionType.Heavy => GameplayTags.Trigger_Player_Hit_Heavy,
                    AttackReactionType.KnockBack => GameplayTags.Trigger_Player_Hit_KnockBack,
                    AttackReactionType.Pull => GameplayTags.Trigger_Player_Hit_Pull,
                    _ => GameplayTags.Trigger_Player_Hit_Hit,
                },
            };
        }

        private static GameplayTag ResolvePlayerHitTrigger(
            AttackReactionType reactionType) => reactionType switch
        {
            AttackReactionType.Light => GameplayTags.Trigger_Player_Hit_Light,
            AttackReactionType.Hit => GameplayTags.Trigger_Player_Hit_Hit,
            AttackReactionType.Heavy => GameplayTags.Trigger_Player_Hit_Heavy,
            AttackReactionType.KnockBack => GameplayTags.Trigger_Player_Hit_KnockBack,
            AttackReactionType.Stun => GameplayTags.Trigger_Player_Hit_Stun,
            AttackReactionType.Pull => GameplayTags.Trigger_Player_Hit_Pull,
            AttackReactionType.Airborne => GameplayTags.Trigger_Player_Hit_Airborne,
            AttackReactionType.Knockdown => GameplayTags.Trigger_Player_Hit_Knockdown,
            AttackReactionType.Grab => GameplayTags.Trigger_Player_Hit_Grab,
            _ => default,
        };

        private void SubscribeReactionAbilityTriggers()
        {
            if (Abilities == null)
                return;
            Abilities.AbilityTriggerRequested -= OnReactionAbilityTriggerRequested;
            Abilities.AbilityTriggerRequested += OnReactionAbilityTriggerRequested;
            if (MovementController != null)
            {
                MovementController.OnStateChanged -= OnReactionAbilityStateChanged;
                MovementController.OnStateChanged += OnReactionAbilityStateChanged;
            }
        }

        private void UnsubscribeReactionAbilityTriggers()
        {
            if (Abilities != null)
                Abilities.AbilityTriggerRequested -= OnReactionAbilityTriggerRequested;
            if (MovementController != null)
                MovementController.OnStateChanged -= OnReactionAbilityStateChanged;
        }

        private void OnReactionAbilityTriggerRequested(AbilityTriggerRequest request)
        {
            if (!request.TriggerTag.IsChildOf(GameplayTags.Trigger_Player_Hit))
                return;

            if (!request.TriggerEvent.HasValue
                || request.TriggerEvent.Value.Payload is not HitReactionTriggerPayload payload
                || payload.ReactionState == CombatReactionState.None
                || MovementController == null)
            {
                Abilities?.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.InvalidDefinition);
                return;
            }

            // 이전 트리거 리액션 실행을 먼저 명시적으로 회수한다.
            // concurrency(CancelExisting)에 기대면 정책 변경 시 핸들이 누수된다.
            ReleaseTriggeredReaction(false);

            if (!TryResolveTriggeredReactionStateId(
                    payload.ReactionState,
                    out ActorStateId targetStateId))
            {
                Abilities.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.InvalidDefinition);
                return;
            }

            bool grounded = MovementController.Motor == null
                || MovementController.Motor.GroundingStatus.IsStableOnGround;
            AbilityActivationResult prepared = Abilities.TryPrepareAbility(
                request.Ability,
                grounded,
                null,
                out AbilityExecutionHandle handle,
                out _,
                request.TriggerEvent);
            if (prepared != AbilityActivationResult.Success)
            {
                Abilities.ReportTriggerRejected(request.Ability, prepared);
                return;
            }

            // 상태 전환보다 Commit을 먼저 수행한다(spec §4).
            // 반대로 두면 Commit 실패 시 리액션 상태에는 들어갔는데
            // 핸들이 없어 종료 훅이 실행을 회수하지 못한다.
            AbilityActivationResult committed = Abilities.Commit(handle);
            if (committed != AbilityActivationResult.Success)
            {
                Abilities.Abort(handle);
                Abilities.ReportTriggerRejected(request.Ability, committed);
                return;
            }

            _triggeredReactionHandle = handle;
            _triggeredReactionState = targetStateId;

            if (!TryEnterTriggeredPlayerReaction(payload))
            {
                // 커밋 롤백: 상태 없이 활성 실행만 남지 않도록 즉시 종료한다.
                ReleaseTriggeredReaction(false);
                Abilities.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.StateTransitionRejected);
                return;
            }

            Abilities.BindActiveExecutionToTrigger(handle, request);
        }

        /// <summary>진행 중인 트리거 리액션 실행을 종료하고 추적 상태를 비운다.</summary>
        private void ReleaseTriggeredReaction(bool completed)
        {
            if (!_triggeredReactionHandle.IsValid)
            {
                _triggeredReactionState = null;
                return;
            }

            AbilityExecutionHandle handle = _triggeredReactionHandle;
            _triggeredReactionHandle = default;
            _triggeredReactionState = null;
            Abilities?.EndAbility(handle, completed);
        }

        /// <summary>
        /// 리액션 종류에 대응하는 상태 ID. 상태 전환보다 Commit이 앞서므로
        /// 전환 전에 추적할 StateId를 미리 확정해야 한다.
        /// </summary>
        private static bool TryResolveTriggeredReactionStateId(
            CombatReactionState reactionState,
            out ActorStateId stateId)
        {
            switch (reactionState)
            {
                case CombatReactionState.Airborne: stateId = ActorStateId.Airborne; return true;
                case CombatReactionState.Grabbed: stateId = ActorStateId.Grabbed; return true;
                case CombatReactionState.Stun: stateId = ActorStateId.Stun; return true;
                case CombatReactionState.Knockdown: stateId = ActorStateId.Knockdown; return true;
                case CombatReactionState.Hit: stateId = ActorStateId.Hit; return true;
                default: stateId = default; return false;
            }
        }

        private bool TryEnterTriggeredPlayerReaction(
            in HitReactionTriggerPayload payload) => payload.ReactionState switch
        {
            CombatReactionState.Airborne =>
                MovementController.TryTransitionToState(ActorStateId.Airborne),
            CombatReactionState.Grabbed =>
                MovementController.TryTransitionToState(
                    new PlayerGrabbedState(MovementController, payload.AttackData)),
            CombatReactionState.Stun =>
                MovementController.TryTransitionToState(
                    new PlayerStunState(MovementController, payload.AttackData)),
            CombatReactionState.Knockdown =>
                MovementController.TryTransitionToState(
                    new PlayerKnockdownState(MovementController, payload.AttackData)),
            CombatReactionState.Hit =>
                MovementController.TryTransitionToState(
                    new PlayerHitState(MovementController, payload.AttackData)),
            _ => false,
        };

        private void OnReactionAbilityStateChanged(
            GameActorState previous,
            GameActorState current)
        {
            if (!_triggeredReactionHandle.IsValid
                || !_triggeredReactionState.HasValue
                || previous?.StateId != _triggeredReactionState.Value
                || current?.StateId == _triggeredReactionState.Value)
                return;

            bool completed = current?.StateId != ActorStateId.Death;
            ReleaseTriggeredReaction(completed);
        }

        private void UpdateStaggerImmunityTag()
        {
            if (!_staggerImmunityTagGranted || IsStaggerImmune)
                return;
            ClearStaggerImmunityTag();
        }

        private void ClearStaggerImmunityTag()
        {
            if (!_staggerImmunityTagGranted)
                return;
            Tags?.RemoveTag(GameplayTags.State_SuperArmor);
            _staggerImmunityTagGranted = false;
        }

        private bool ShouldEnterAirborneState(AttackData attackData)
        {
            if (attackData == null || attackData.reactionType != AttackReactionType.Airborne)
                return false;

            if (attackData.airborneForce >= MinAirborneStateForce)
                return true;

            return false;
        }

        /// <summary>
        /// 사망 시 호출.
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[PlayerActor] {gameObject.name} 사망!");
            CombatTelemetrySession.NotifyPlayerDeath(this);
            ClearAllInputState();
            PlayerMovementPlayerController?.ClearInputAll();
            InputMgr?.InputBuffer?.Clear();
            CombatFeedbackDispatcher.ApplyPlayerDeathFeedback(_shakeKeyDeath);
            MovementController.TransitionToState(new PlayerDeathState(MovementController));
        }

        /// <summary>
        /// 지정 위치/회전으로 부활한다.
        /// healPercent: 회복할 HP 비율 (0~1). 기본값 1 = 최대 HP 전체 회복.
        /// </summary>
        public void Respawn(Vector3 position, Quaternion rotation, float healPercent = 1f)
        {
            _currentHealth = _maxHealth * Mathf.Clamp01(healPercent);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            var motor = ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(position, rotation);
            else
                transform.SetPositionAndRotation(position, rotation);

            MovementController.TransitionToState(ActorStateId.Idle);
            _behaviorPredictor?.ResetHistory();
            CameraMgr?.SnapToTarget(position);

            Debug.Log($"[PlayerActor] {gameObject.name} 부활 — 위치: {position}");
        }
    }
}
