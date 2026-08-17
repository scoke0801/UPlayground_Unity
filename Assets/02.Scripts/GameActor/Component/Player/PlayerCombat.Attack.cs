using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Party;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Gameplay.Ability;
using UPlayGround.MovementController;
using UPlayGround.State;
using UPlayGround.Debugging;

namespace UPlayGround.Components
{
    public partial class PlayerCombat : PlayerActorComponent, UPlayGround.Combat.ICombatCollisionExecutor, IDebugGizmoProvider
    {
        #region Execute Attack

        public AttackData ExecuteAttack(bool isCombo)
        {
            ClearResidualAttackContext();
            if (GetComboLength(AttackState.NormalAttack) <= 0) return null;
            _attackState      = AttackState.NormalAttack;          // 전환(ResetCombo 호출 제거 — 강 체인 보존)
            CurrentComboIndex = _normalComboIndex;                 // 약 체인 보존 인덱스 복원(-1 = 미시작)
            // stale 콤보 윈도우 닫기: 전환 시 ResetCombo가 하던 CanCombo=false 대체.
            // advance 평가 전에 닫아, 캔슬 경로(isCombo=false)가 이전 공격의 열린 윈도우에 기대지 않게 한다.
            _comboController?.CloseWindow();
            CurrentComboIndex = (CurrentComboIndex >= 0 && isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _normalComboIndex = CurrentComboIndex;                 // 약 체인 저장
            // 태그 상호배타: ResetCombo(반대태그 제거)가 사라졌으므로 직접 반대태그 제거 후 추가.
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Heavy);
            _playerActor.Tags?.AddTag(GameplayTags.Combo_Light);
            _currentAttackData = ConvertToAttackData(_attackData.liteComboAttackList[CurrentComboIndex], AttackKind.NormalAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteHeavyAttack(bool isCombo)
        {
            if (GetComboLength(AttackState.HeavyAttack) <= 0) return null;
            int nextIndex = PeekNextComboIndex(
                AttackState.HeavyAttack,
                isCombo);
            if (!TryConsumeAttackAbilityCost(
                    _attackData.heavyComboAbilities,
                    nextIndex))
                return null;

            ClearResidualAttackContext();
            _attackState      = AttackState.HeavyAttack;           // 전환(ResetCombo 호출 제거 — 약 체인 보존)
            _comboController?.CloseWindow();                       // stale 콤보 윈도우 닫기(ExecuteAttack과 동일)
            CurrentComboIndex = nextIndex;
            _heavyComboIndex  = CurrentComboIndex;                 // 강 체인 저장
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Light);
            _playerActor.Tags?.AddTag(GameplayTags.Combo_Heavy);
            _currentAttackData = ConvertToAttackData(_attackData.heavyComboAttackList[CurrentComboIndex], AttackKind.HeavyAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }
        
        public float[] GetChargeStageThresholds()
        {
            int stageCount = _attackData.chargeStages?.Count ?? 0;
            if (stageCount <= 1) return System.Array.Empty<float>();

            var configured = _attackData.chargeStageThresholds;
            int needed     = stageCount - 1;

            if (configured != null && configured.Count == needed)
                return configured.ToArray();

            var result = new float[needed];
            for (int i = 0; i < needed; i++)
                result[i] = (float)(i + 1) / stageCount;
            return result;
        }

        public MotionSetAsset GetFirstChargeAttackMotion()
        {
            if (_attackData == null
                || !_attackData.chargeMotionKey.IsValid
                || !CanPayAttackAbilityCost(
                    _attackData.chargeStageAbilities,
                    0)
                || _playerActor?.Animator == null)
                return null;
            return ResolveFirstChargeAttackMotion();
        }

        private MotionSetAsset ResolveFirstChargeAttackMotion()
        {
            return _playerActor.Animator.TryResolveAbilityMotion(
                _attackData.chargeMotionKey,
                out MotionSetAsset motionAsset)
                ? motionAsset
                : null;
        }

        /// <summary> 차지(홀드) 도중 캔슬 가능한 입력 액션 마스크. </summary>
        public PlayerInterruptAction GetChargeInterruptActions() => _attackData.chargeInterruptActions;

        public (string key, ActorSocketType socket, Vector3 offset) GetFullChargeVfxData()
            => (_attackData.fullChargeVfxKey, _attackData.fullChargeVfxSocket, _attackData.fullChargeVfxOffset);

        public AttackData ExecuteChargeAttack(int stageIndex, float chargeRatio)
        {
            if (_attackData.chargeStages == null || _attackData.chargeStages.Count == 0) return null;
            int clampedStage = Mathf.Clamp(stageIndex, 0, _attackData.chargeStages.Count - 1);
            if (!TryConsumeAttackAbilityCost(
                    _attackData.chargeStageAbilities,
                    clampedStage))
                return null;

            ClearResidualAttackContext();
            _attackState = AttackState.ChargeAttack;
            ResetCombo();

            // 연계 라우트 prefix용 Charge 토큰 기록(예: 차지 → 스킬1). 차지 릴리즈는 별도 상태이므로
            // 여기서 push해야 트래커에 Charge가 남는다.
            _playerActor?.ComboInputTracker.Push(ComboInputToken.Charge);

            // stageIndex = InfiniteLoopStageIndex (0 = 1단계 차지, 1 = 2단계 차지 ...)
            // chargeStages 배열에서 해당 단계의 데이터를 사용한다.
            // hitPhaseIndex는 항상 0으로 시작 (각 스테이지의 첫 번째 히트 페이즈)
            _currentAttackData = ConvertToChargeAttackData(_attackData.chargeStages[clampedStage], chargeRatio, 0);
            _currentResidualHitPhases = _attackData.chargeStages[clampedStage].hitPhases;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        private bool CanPayAttackAbilityCost(
            IReadOnlyList<GameplayAbilitySO> abilities,
            int index)
        {
            if (abilities == null || index < 0 || index >= abilities.Count)
                return false;
            GameplayAbilitySO ability = abilities[index];
            if (!IsAttackAbilityUnlocked(ability))
                return false;
            return _playerActor?.AbilitySystem?.ProjectAbilities
                ?.CanPayAbilityCost(ability) == true;
        }

        private bool TryConsumeAttackAbilityCost(
            IReadOnlyList<GameplayAbilitySO> abilities,
            int index)
        {
            if (abilities == null || index < 0 || index >= abilities.Count)
                return false;
            GameplayAbilitySO ability = abilities[index];
            if (!IsAttackAbilityUnlocked(ability))
                return false;
            return _playerActor?.AbilitySystem?.ProjectAbilities
                ?.TryConsumeAbilityCost(ability) == true;
        }

        private bool IsAttackAbilityUnlocked(GameplayAbilitySO ability) =>
            ability != null
            && Svc.Party?.IsAbilityUnlocked(
                _playerActor.CharacterType,
                ability.abilityId) != false;

        private AttackData ConvertToChargeAttackData(ChargeStageData stage, float chargeRatio, int phaseIndex)
        {
            _currentAttackInfoBase = null;
            var phase = stage.GetHitPhase(phaseIndex);

            var data = new AttackData
            {
                motionAsset      = ResolveFirstChargeAttackMotion(),
                damage           = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f),
                poiseDamage      = phase.poiseDamage,
                breakDamage      = phase.breakDamage,
                reactionDuration = phase.reactionDuration,
                forceReaction    = phase.forceReaction,
                forceBreakExpose = phase.forceBreakExpose,
                interruptActions = stage.interruptActions,
                reactionType     = phase.reactionType,
                hitParticleName  = phase.hitParticleName,
                pullForce        = phase.pullForce,
                knockbackForce   = phase.knockBackForce,
                knockbackDrag    = phase.knockBackDrag,
                airborneForce    = phase.airborneForce,
                hitPhaseIndex    = 0,
                attackKind       = AttackKind.ChargeAttack,
                reactionData     = phase.reactionProfile?.Resolve(),
            };
            data.damage *= Mathf.Lerp(1.0f, 1.5f, chargeRatio);
            return data;
        }

        public AttackData ExecuteCounterAttack()
        {
            ClearResidualAttackContext();
            var source = _attackData.counterAttack?.baseInfo != null
                ? _attackData.counterAttack
                : (_attackData.heavyComboAttackList.Count > 0 ? _attackData.heavyComboAttackList[0] : null);

            if (source == null) return null;

            _attackState = AttackState.HeavyAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.HeavyAttack);
            // 퍼펙트 가드 반격임을 명시 — 적중 시 몬스터 '가벼운 밀쳐냄' 판정에 사용(SO 작성 의존 제거).
            _currentAttackData.isCounterAttack = true;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// §5.2 등장 변형 — PartyManager가 잡은 등장 타깃을 보관한다.
        /// PlayerActor.ConsumeEntryAttackQueue가 TryStartEntryAttack 직전 호출.
        /// </summary>
        public void SetPendingEntryTarget(MonsterActor target) => _pendingEntryTarget = target;

        /// <summary>
        /// 스왑 회피 카운터처럼 큐 단계에서 이미 결정된 공격 타깃을 보관한다.
        /// PlayerAttackState 진입 후 모션워핑/락온 트래킹의 우선 타깃으로 1회 사용된다.
        /// </summary>
        public void SetPendingSwapAttackTarget(MonsterActor target) => _pendingSwapAttackTarget = target;

        public AttackData ExecuteEntryAttack()
        {
            ClearResidualAttackContext();
            var source = SelectEntryAttackInfo();
            MonsterActor pendingEntryTarget = _pendingEntryTarget;
            _pendingEntryTarget = null; // 1회 소비 후 폐기(스테일 타깃 방지)

            if (source == null) return null;

            _currentAttackPreferredTarget = pendingEntryTarget != null && pendingEntryTarget.CanTakeDamage()
                ? pendingEntryTarget
                : null;

            var comboState = CaptureComboState();
            _currentAttackData = ConvertToAttackData(source, AttackKind.NormalAttack);
            // 등장 공격은 카운터급 '피드백'만 원한다. isCounterAttack을 쓰면 MonsterActor가
            // 리액션 정책/등장 변형용 guaranteedReaction을 전부 우회하고 shove로 단락되므로 금지.
            _currentAttackData.useCounterHitFeedback = true;
            RestoreComboState(comboState);
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// §5.2 타깃 적 상태로 등장 변형을 선택한다. 매칭 없으면 기본 entryAttack → 약공 첫 번째 폴백.
        /// (공중 변형 우선, 다음 그로기 변형)
        /// </summary>
        private AbilityAttackInfo SelectEntryAttackInfo()
        {
            if (_attackData == null) return null;

            // 변형은 명시 토글로만 활성(baseInfo는 Unity가 항상 인스턴스화하므로 null 검사로 미설정 구분 불가).
            if (_attackData.useEntryAttackVsAirborne
                && IsEntryTargetAirborne(_pendingEntryTarget)
                && _attackData.entryAttackVsAirborne != null)
                return _attackData.entryAttackVsAirborne;
            if (_attackData.useEntryAttackVsGroggy
                && IsEntryTargetGroggy(_pendingEntryTarget)
                && _attackData.entryAttackVsGroggy != null)
                return _attackData.entryAttackVsGroggy;

            if (_attackData.entryAttack?.baseInfo != null)
                return _attackData.entryAttack;
            return _attackData.liteComboAttackList.Count > 0 ? _attackData.liteComboAttackList[0] : null;
        }

        private static bool IsEntryTargetAirborne(MonsterActor target)
            => target != null && target.ActorController?.CurrentState?.StateId == ActorStateId.Airborne;

        private static bool IsEntryTargetGroggy(MonsterActor target)
        {
            if (target == null) return false;
            ActorStateId? stateId = target.ActorController?.CurrentState?.StateId;
            if (stateId is ActorStateId.Stun or ActorStateId.Knockdown) return true;
            return target.BreakGauge != null && target.BreakGauge.IsExposed;
        }

        public AttackData ExecuteSwapEvadeCounterAttack()
        {
            ClearResidualAttackContext();
            MonsterActor pendingSwapTarget = _pendingSwapAttackTarget;
            _pendingSwapAttackTarget = null;

            var source = _attackData.swapEvadeCounterAttack?.baseInfo != null
                ? _attackData.swapEvadeCounterAttack
                : (_attackData.entryAttack?.baseInfo != null
                    ? _attackData.entryAttack
                    : (_attackData.liteComboAttackList.Count > 0 ? _attackData.liteComboAttackList[0] : null));

            if (source == null) return null;

            _currentAttackPreferredTarget = pendingSwapTarget != null && pendingSwapTarget.CanTakeDamage()
                ? pendingSwapTarget
                : null;

            var comboState = CaptureComboState();
            _currentAttackData = ConvertToAttackData(source, AttackKind.NormalAttack);
            _currentAttackData.useCounterHitFeedback = true;
            RestoreComboState(comboState);
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteSwapSpecialAttack()
        {
            ClearResidualAttackContext();
            var source = _attackData.swapSpecialAttack?.baseInfo != null
                ? _attackData.swapSpecialAttack
                : null;

            if (source == null) return null;

            _attackState = AttackState.SkillAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.SkillAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteParryCounterAttack()
        {
            ClearResidualAttackContext();
            var source = _attackData.parryCounterAttack?.baseInfo != null
                ? _attackData.parryCounterAttack
                : (_attackData.counterAttack?.baseInfo != null
                    ? _attackData.counterAttack
                    : (_attackData.heavyComboAttackList.Count > 0 ? _attackData.heavyComboAttackList[0] : null));

            if (source == null) return null;

            _attackState = AttackState.HeavyAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.HeavyAttack);
            // 패리 반격임을 명시 — 적중 시 몬스터 '가벼운 밀쳐냄' 판정에 사용(SO 작성 의존 제거).
            _currentAttackData.isCounterAttack = true;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteSkillAttack(int skillIndex)
        {
            ClearResidualAttackContext();
            if (!TryResolveSkill(skillIndex, out AbilityAttackInfo attackInfo, out MotionSetAsset motionAsset)) return null;

            _attackState = AttackState.SkillAttack;
            ResetComboPreserveChains();
            _currentAttackData = ConvertToAttackData(attackInfo, AttackKind.SkillAttack);
            _currentAttackData.motionAsset = motionAsset;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// GameplayAbilitySO가 해석한 Variant를 기존 MotionSet/HitPhase 공격 경로로 실행한다.
        /// 비용과 쿨다운은 이 메서드가 아니라 ActorAbilitySystem.Commit이 소유한다.
        /// </summary>
        public AttackData ExecuteAbilityAttack(AbilityVariantDefinition variant)
        {
            ClearResidualAttackContext();
            if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                    variant,
                    out AbilityAttackInfo attackInfo)
                || !ActorAbilityMotionResolver.TryResolve(
                    _playerActor,
                    attackInfo,
                    out MotionSetAsset motionAsset))
                return null;

            _attackState = AttackState.SkillAttack;
            ResetComboPreserveChains();
            _currentAttackData = ConvertToAttackData(attackInfo, AttackKind.SkillAttack);
            _currentAttackData.motionAsset = motionAsset;
            _currentAttackData.abilityVariantId = variant?.variantId;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        // ── 연계 라우트 (Combo Route) ────────────────────────────────
        /// <summary> 현재 캐릭터 공격 데이터의 연계 라우트 목록(없으면 null). </summary>
        public IReadOnlyList<ComboRouteEntry> ComboRoutes
            => _attackData != null ? _attackData.comboRoutes : null;

        /// <summary>
        /// 라우트 자원(스킬 게이지) 충족 여부. Resolve의 resourceFilter로 전달한다.
        /// 소비하지 않고 가용 여부만 확인한다.
        /// </summary>
        public bool CanAffordRoute(ComboRouteEntry route)
        {
            if (route == null) return false;
            if (RouteUsesStamina(route)
                && _playerActor?.Abilities?.CanPayAbilityCost(route.ability)
                    != true)
                return false;
            if (route.skillGaugeIndex < 0) return true;
            if (!PlayerAbilityResourceView.IsValidSkillSlot(route.skillGaugeIndex))
                return false;
            return _playerActor?.Abilities?.EvaluatePlayerSlot(
                (PlayerSkillSlot)route.skillGaugeIndex,
                out _) == AbilityActivationResult.Success;
        }

        /// <summary>
        /// 연계 라우트로 공격을 실행한다. PlayerAttackState가 Resolve 매칭 후 호출.
        /// 패턴 마지막 토큰으로 AttackKind를 결정하고, 게이지를 소비한다.
        /// 연계는 단발이므로 약/강 분기 메모리는 보존하되 진행 인덱스는 종료한다(설계 §8).
        /// </summary>
        public AttackData ExecuteComboRoute(ComboRouteEntry route, bool isPerfect = false)
        {
            if (route == null || route.attackInfo?.baseInfo == null) return null;
            bool useEnhancedAttack = isPerfect && route.HasEnhancedAttack;
            GameplayAbilitySO ability = useEnhancedAttack
                ? route.enhancedAbility
                : route.ability;
            if (RouteUsesStamina(route)
                && _playerActor?.Abilities?.TryConsumeAbilityCost(ability)
                    != true)
                return null;
            ClearResidualAttackContext();

            AttackKind kind = RouteAttackKind(route.LastToken);
            _attackState = kind == AttackKind.HeavyAttack ? AttackState.HeavyAttack
                         : kind == AttackKind.SkillAttack ? AttackState.SkillAttack
                         :                                  AttackState.NormalAttack;

            ResetComboPreserveChains();

            // 퍼펙트 강화: 전용 공격이 있으면 그것으로 교체, 없으면 기본 공격에 런타임 배율을 싣는다(둘 다 지원).
            AbilityAttackInfo source = useEnhancedAttack ? route.enhancedAttackInfo : route.attackInfo;

            _currentAttackData = ConvertToAttackData(source, kind);

            if (isPerfect && !useEnhancedAttack && _currentAttackData != null)
            {
                float enhancedDamageMultiplier = Mathf.Max(0f, route.enhancedDamageMultiplier);
                float enhancedPoiseMultiplier = Mathf.Max(0f, route.enhancedPoiseMultiplier);
                _currentAttackData.damageMultiplier *= enhancedDamageMultiplier;
                _currentAttackData.poiseMultiplier  *= enhancedPoiseMultiplier;

                // Create가 세팅한 phase0 초기값에도 즉시 반영 — 첫 타가 SetHitPhaseIndex(0) 이전에 들어오는
                // 단일타 라우트 등의 엣지에서 강화분만 반영한다. 기존 장비/체급 배율은 이미 현재 값에 적용돼 있다.
                _currentAttackData.damage      *= enhancedDamageMultiplier;
                _currentAttackData.poiseDamage *= enhancedPoiseMultiplier;
                _currentAttackData.breakDamage *= enhancedPoiseMultiplier;
            }

            if (isPerfect && route.enhancedGrantTagId.IsValid())
                _playerActor?.Tags?.AddTag(route.enhancedGrantTagId);

            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        private static AttackKind RouteAttackKind(ComboInputToken lastToken) => lastToken switch
        {
            ComboInputToken.HeavyAttack => AttackKind.HeavyAttack,
            ComboInputToken.Charge      => AttackKind.HeavyAttack,
            ComboInputToken.Skill1      => AttackKind.SkillAttack,
            ComboInputToken.Skill2      => AttackKind.SkillAttack,
            ComboInputToken.Dash        => AttackKind.DashAttack,
            _                           => AttackKind.NormalAttack,
        };

        private static bool RouteUsesStamina(ComboRouteEntry route) =>
            route?.LastToken is ComboInputToken.HeavyAttack
                or ComboInputToken.Charge;

        public AttackData ExecuteJumpAttack(bool isCombo = false)
        {
            ClearResidualAttackContext();
            if (_attackData.jumpAttackList == null || _attackData.jumpAttackList.Count == 0) return null;
            if (_attackState != AttackState.JumpAttack) ResetCombo();
            _attackState      = AttackState.JumpAttack;
            CurrentComboIndex = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            CurrentComboIndex = Mathf.Clamp(CurrentComboIndex, 0, _attackData.jumpAttackList.Count - 1);
            _currentAttackData = ConvertToAttackData(_attackData.jumpAttackList[CurrentComboIndex], AttackKind.JumpAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        // jumpAttackList의 마지막 항목을 피니시 공격으로 실행
        public AttackData ExecuteJumpFinishAttack()
        {
            if (_attackData.jumpAttackList == null || _attackData.jumpAttackList.Count == 0) return null;
            int finishIndex = _attackData.jumpAttackList.Count - 1;
            if (!TryConsumeAttackAbilityCost(
                    _attackData.jumpAttackAbilities,
                    finishIndex))
                return null;

            ClearResidualAttackContext();
            _attackState      = AttackState.JumpAttack;
            CurrentComboIndex = finishIndex;
            _currentAttackData = ConvertToAttackData(_attackData.jumpAttackList[CurrentComboIndex], AttackKind.JumpAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteDashAttack()
        {
            ClearResidualAttackContext();
            if (_attackData.dashAttackList == null || _attackData.dashAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.dashAttackList[0], AttackKind.DashAttack);
            ResetCombo();
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteJumpDashAttack()
        {
            ClearResidualAttackContext();
            if (_attackData.dashAttackList == null || _attackData.dashAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.dashAttackList[0], AttackKind.DashAttack);
            ResetCombo();
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public void SetupFinishAttackData(Transform finishTarget = null)
        {
            ClearResidualAttackContext();
            _currentAttackInfoBase = null;
            _currentFinishTarget = finishTarget != null
                ? finishTarget.GetComponent<MonsterActor>() ?? finishTarget.GetComponentInParent<MonsterActor>()
                : null;
            _currentAttackData     = new AttackData
            {
                damage           = 9999f,
                poiseDamage      = 9999f,
                breakDamage      = 0f,
                interruptActions = PlayerInterruptAction.None,
                reactionType     = AttackReactionType.Knockdown,
                hitParticleName  = "HeavyHit",
                knockbackForce   = 0f,
                attackKind       = AttackKind.FinishAttack,
            };
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
        }

        public void SetupSpecialBreakAttackData(SpecialBreakAttackAsset specialBreakAttack, MonsterActor target)
        {
            // 어셋 누락 시 매직 디폴트(20% MaxHP)로 일반 모션을 발화하지 않도록 fail-fast.
            // 상태 진입은 호출부에서 막지 못해도, 여기서 데미지 흐름을 끊는다.
            if (specialBreakAttack == null)
            {
                Debug.LogError($"[PlayerCombat] SetupSpecialBreakAttackData: SpecialBreakAttackAsset이 null입니다. target={target?.name}");
                return;
            }

            ClearResidualAttackContext();
            _currentAttackInfoBase = null;
            _currentSpecialBreakTarget = target;
            _currentSpecialBreakDamageByMaxHpRate = Mathf.Max(0f, specialBreakAttack.damageByMaxHpRate);
            _currentSpecialBreakFixedDamage = Mathf.Max(0f, specialBreakAttack.fixedDamage);
            _currentSpecialBreakMinReferenceHealth = Mathf.Max(0f, specialBreakAttack.minReferenceHealth);
            _currentAttackData = new AttackData
            {
                damage = _currentSpecialBreakFixedDamage,
                poiseDamage = 0f,
                breakDamage = 0f,
                interruptActions = PlayerInterruptAction.None,
                reactionType = AttackReactionType.Heavy,
                hitParticleName = "HeavyHit",
                attackKind = AttackKind.SkillAttack,
            };
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
        }

        private AttackData ConvertToAttackData(AbilityAttackInfo attackInfo, AttackKind attackKind)
        {
            if (attackInfo?.baseInfo == null)
                return null;
            _currentAttackInfoBase = attackInfo.baseInfo;
            _currentResidualHitPhases = attackInfo.baseInfo.hitPhases;
            AttackData data = _attackController.Create(attackInfo, attackKind);
            if (data != null)
            {
                ActorAbilityMotionResolver.TryResolve(
                    _playerActor,
                    attackInfo,
                    out data.motionAsset);
                data.abilityId = _playerActor?.Abilities?.CurrentAbilityId;
                data.abilityVariantId = _playerActor?.Abilities?.CurrentVariantId;
            }
            if (data != null && _playerActor != null)
            {
                float attackMultiplier = attackKind switch
                {
                    AttackKind.NormalAttack => Svc.Passives?.GetActiveMultiplier(
                        PassiveModifierType.LightAttackDamage) ?? 1f,
                    AttackKind.HeavyAttack => Svc.Passives?.GetActiveMultiplier(
                        PassiveModifierType.HeavyAttackDamage) ?? 1f,
                    AttackKind.SkillAttack => Svc.Passives?.GetActiveMultiplier(
                        PassiveModifierType.SkillDamage) ?? 1f,
                    _ => 1f,
                };
                float passiveBreakMultiplier = Svc.Passives?.GetActiveMultiplier(
                    PassiveModifierType.BreakDamage) ?? 1f;
                string abilityId = _playerActor.Abilities?.CurrentAbilityId;
                float skillTreeDamageMultiplier = Svc.Party?.GetAbilityScalar(
                    _playerActor.CharacterType,
                    abilityId,
                    AbilityScalarKind.Damage) ?? 1f;
                float skillTreeBreakMultiplier = Svc.Party?.GetAbilityScalar(
                    _playerActor.CharacterType,
                    abilityId,
                    AbilityScalarKind.BreakDamage) ?? 1f;
                attackMultiplier *= skillTreeDamageMultiplier;
                passiveBreakMultiplier *= skillTreeBreakMultiplier;

                // 공격 생성 시 스냅샷해 스왑 후 잔류 공격도 outgoing 캐릭터 배율을 유지한다.
                data.damageMultiplier *= _playerActor.WeightDamageMultiplier * attackMultiplier;
                data.poiseMultiplier *= _playerActor.WeightBreakDamageMultiplier;
                data.breakDamageMultiplier *= passiveBreakMultiplier;
                data.damage *= _playerActor.WeightDamageMultiplier * attackMultiplier;
                data.poiseDamage *= _playerActor.WeightBreakDamageMultiplier;
                data.breakDamage *= _playerActor.WeightBreakDamageMultiplier * passiveBreakMultiplier;
            }
            return data;
        }

        private static AttackData CopyAttackData(AttackData source)
        {
            return PlayerAttackController.Copy(source);
        }

        #endregion
    }
}
