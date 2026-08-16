using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Gameplay.Effect;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// AbilitySystemComponent가 소유하는 프로젝트 Ability 런타임.
    /// 정의(SO)와 실행 상태를 분리하고
    /// Prepare 이후 외부 상태 전환이 성공한 경우에만 비용/쿨다운을 Commit한다.
    /// </summary>
    public sealed partial class ActorAbilitySystem : IAbilityRuntimeReader
    {
        private enum AbilityTagEvaluation
        {
            Pass,
            MissingRequired,
            Blocked,
        }

        private const string GlobalCooldownGroupId = "Ability.Global";
        private readonly Dictionary<ulong, AbilityExecution> _executions = new();
        private readonly HashSet<ulong> _backgroundExecutions = new();
        private readonly Dictionary<GameplayAbilitySO, int> _temporaryAbilities =
            new();
        private readonly Dictionary<AbilityActivationResult, int>
            _activationFailureCounts = new();
        private readonly Dictionary<string, int> _activeAbilityBlockTags =
            new(StringComparer.Ordinal);
        private readonly List<(AbilityExecutionHandle Handle, bool Succeeded, string Reason)>
            _backgroundCompletionBuffer = new();
        private readonly List<AbilityExecutionHandle> _stalePreparedExecutionBuffer = new();
        private GameActor _owner;
        private AbilitySetSO _abilitySet;
        private AbilityResourceRuleSO _resourceRules;
        private ulong _nextHandle = 1;
        private ulong _primaryExecution;
        private ulong _latestPreparedExecution;
        private AbilitySystemComponent _abilitySystem;
        private GameplayEffectController _effects;
        private AbilityCooldownRuntime _cooldowns;
        private IAbilityResourcePort _resources;
        private IAbilityTagPort _tags;
        [ThreadStatic] private static AbilityTagPortQuerySource _tagQuerySource;
        [ThreadStatic] private static bool _isEvaluatingTagExpression;

        public event Action StateChanged;
        public AbilitySetSO AbilitySet => _abilitySet;
        public bool HasActiveAbility =>
            _primaryExecution != 0 || _backgroundExecutions.Count > 0;
        public bool HasActivePlayerAbility => _primaryExecution != 0;
        public string CurrentAbilityId =>
            _primaryExecution != 0
            && _executions.TryGetValue(
                _primaryExecution,
                out AbilityExecution execution)
                ? execution.Definition?.abilityId
                : _latestPreparedExecution != 0
                  && _executions.TryGetValue(
                      _latestPreparedExecution,
                      out AbilityExecution prepared)
                  && prepared.State == AbilityExecutionState.Prepared
                    ? prepared.Definition?.abilityId
                    : null;
        public string CurrentVariantId =>
            _primaryExecution != 0
            && _executions.TryGetValue(
                _primaryExecution,
                out AbilityExecution execution)
                ? execution.Variant?.variantId
                : _latestPreparedExecution != 0
                  && _executions.TryGetValue(
                      _latestPreparedExecution,
                      out AbilityExecution prepared)
                  && prepared.State == AbilityExecutionState.Prepared
                    ? prepared.Variant?.variantId
                    : null;

        internal ActorAbilitySystem(
            GameActor owner,
            AbilitySystemComponent abilitySystem)
        {
            Initialize(owner, abilitySystem);
        }

        private void Initialize(
            GameActor owner,
            AbilitySystemComponent abilitySystem)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ReferenceEquals(_owner, owner) && _cooldowns != null)
                return;

            _owner = owner;
            _abilitySystem = abilitySystem
                ?? throw new ArgumentNullException(nameof(abilitySystem));
            _abilitySystem.EnsureInitialized();
            _effects = _abilitySystem.ProjectEffects;
            _cooldowns = _abilitySystem.Runtime.Cooldowns;
            var ports = new UPlayGroundAbilityOwnerPorts(_abilitySystem);
            _resources = ports;
            _tags = ports;
            SubscribeTriggerEvents();
            _abilitySystem.Runtime.SetInputPort(
                owner is PlayerActor player
                    ? new PlayerAbilityInputPort(player)
                    : null);
        }

        public void SetAbilitySet(AbilitySetSO abilitySet)
        {
            using AbilityListLock abilityListLock = LockAbilityList();
            CancelAllAbilities();
            _abilitySet = abilitySet;
            _abilitySet?.RebuildRuntimeIndex();
            // Set 교체는 이전 Set 기준으로 쌓인 대기 트리거와 재트리거 이력을
            // 무효화하므로 인덱스 재구축과 함께 명시적으로 비운다.
            ClearPendingTriggerState();
            RebuildTriggerIndex();
            StateChanged?.Invoke();
        }

        public void SetResourceRules(AbilityResourceRuleSO resourceRules) =>
            _resourceRules = resourceRules;

        public void CopyActivationFailureCounts(
            IDictionary<AbilityActivationResult, int> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (KeyValuePair<AbilityActivationResult, int> pair
                     in _activationFailureCounts)
                destination[pair.Key] = pair.Value;
        }

        public bool HasPlayerAbility(PlayerSkillSlot slot) =>
            ResolvePlayerAbility(slot) != null;

        public AbilityActivationResult EvaluatePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            variant = null;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null)
            {
                RecordActivationResult(AbilityActivationResult.NotGranted);
                return AbilityActivationResult.NotGranted;
            }
            AbilityActivationResult result = Evaluate(
                definition,
                isGrounded,
                ResolveTarget(definition, target),
                out variant,
                triggerEvent);
            RecordActivationResult(result);
            return result;
        }

        public AbilityActivationResult EvaluatePlayerSlot(
            PlayerSkillSlot slot,
            out AbilityVariantDefinition variant)
        {
            bool grounded = _owner is not PlayerActor player
                            || player.PlayerController?.Motor == null
                            || player.PlayerController.Motor.GroundingStatus
                                .IsStableOnGround;
            return EvaluatePlayerSlot(slot, grounded, null, out variant);
        }

        public AbilityActivationResult EvaluateAbility(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            variant = null;
            if (!IsGrantedAbility(definition))
                return AbilityActivationResult.NotGranted;
            AbilityActivationResult result = Evaluate(
                definition,
                isGrounded,
                ResolveTarget(definition, target),
                out variant,
                triggerEvent);
            RecordActivationResult(result);
            return result;
        }

        public AbilityActivationResult TryPreparePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            return TryPreparePlayerSlot(
                slot,
                isGrounded,
                target,
                null,
                out handle,
                out variant,
                triggerEvent);
        }

        /// <summary>
        /// 슬롯 Variant의 프로젝트 실행 데이터를 검증한 뒤 Prepared 실행을 만든다.
        /// Ultimate처럼 일반 Motion 실행과 다른 Payload도 같은 GAS 진입점을 사용한다.
        /// </summary>
        public AbilityActivationResult TryPreparePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            Func<AbilityVariantDefinition, bool> validateExecutionData,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            handle = default;
            variant = null;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null) return AbilityActivationResult.NotGranted;

            return TryPrepareAbility(
                definition,
                isGrounded,
                target,
                validateExecutionData,
                out handle,
                out variant,
                triggerEvent);
        }

        public AbilityActivationResult TryPrepareAbility(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            return TryPrepareAbility(
                definition,
                isGrounded,
                target,
                null,
                out handle,
                out variant,
                triggerEvent);
        }

        /// <summary>
        /// Variant 실행 데이터를 검증한 뒤에만 기존 주 실행을 취소하고 Prepared 상태를 만든다.
        /// 모션/페이로드 해석 실패가 현재 실행 중인 Ability를 끊지 않게 하는 원자적 준비 경로다.
        /// </summary>
        public AbilityActivationResult TryPrepareAbility(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            Func<AbilityVariantDefinition, bool> validateExecutionData,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            handle = default;
            variant = null;
            if (!IsGrantedAbility(definition))
            {
                RecordPlayerTelemetryFailure(
                    definition,
                    target,
                    AbilityActivationResult.NotGranted);
                return AbilityActivationResult.NotGranted;
            }

            GameActor resolvedTarget = ResolveTarget(definition, target);
            AbilityActivationResult result =
                Evaluate(
                    definition,
                    isGrounded,
                    resolvedTarget,
                    out variant,
                    triggerEvent);
            if (result != AbilityActivationResult.Success)
            {
                RecordActivationResult(result);
                RecordPlayerTelemetryFailure(definition, resolvedTarget, result);
                return result;
            }

            if (validateExecutionData != null && !validateExecutionData(variant))
            {
                result = AbilityActivationResult.MissingExecutionData;
                RecordActivationResult(result);
                RecordPlayerTelemetryFailure(definition, resolvedTarget, result);
                return result;
            }

            if (_primaryExecution != 0
                && definition.concurrency != AbilityConcurrencyPolicy.Background)
            {
                if (definition.concurrency == AbilityConcurrencyPolicy.RejectNew)
                {
                    RecordPlayerTelemetryFailure(
                        definition,
                        resolvedTarget,
                        AbilityActivationResult.ConflictingAbility);
                    return AbilityActivationResult.ConflictingAbility;
                }
                if (definition.concurrency == AbilityConcurrencyPolicy.CancelExisting)
                    CancelActiveAbility();
            }

            handle = new AbilityExecutionHandle(_nextHandle++);
            _executions.Add(handle.Value, new AbilityExecution(
                handle,
                definition,
                variant,
                _owner,
                resolvedTarget,
                Time.frameCount,
                triggerEvent));
            if (definition.concurrency != AbilityConcurrencyPolicy.Background)
                _latestPreparedExecution = handle.Value;
            return AbilityActivationResult.Success;
        }

        public AbilityActivationResult Commit(AbilityExecutionHandle handle)
        {
            using AbilityListLock abilityListLock = LockAbilityList();
            if (!handle.IsValid || !_executions.TryGetValue(handle.Value, out AbilityExecution execution))
                return AbilityActivationResult.InvalidDefinition;
            if (execution.State == AbilityExecutionState.Active)
                return AbilityActivationResult.AlreadyCommitted;
            if (execution.State != AbilityExecutionState.Prepared)
                return AbilityActivationResult.InvalidDefinition;
            if (Time.frameCount > execution.PreparedFrame + 1)
            {
                RecordPlayerTelemetryFailure(
                    execution.Definition,
                    execution.Target,
                    AbilityActivationResult.PreparedExecutionExpired);
                Abort(handle);
                return AbilityActivationResult.PreparedExecutionExpired;
            }

            if (IsBlockedByActiveAbility(execution.Definition))
            {
                Abort(handle);
                RecordActivationResult(
                    AbilityActivationResult.BlockedByActiveAbility);
                RecordPlayerTelemetryFailure(
                    execution.Definition,
                    execution.Target,
                    AbilityActivationResult.BlockedByActiveAbility);
                return AbilityActivationResult.BlockedByActiveAbility;
            }

            if (!TryConsumeCost(
                    execution.Definition.cost,
                    execution.Handle,
                    execution.Definition.abilityId))
            {
                RecordPlayerTelemetryFailure(
                    execution.Definition,
                    execution.Target,
                    AbilityActivationResult.InsufficientResource);
                Abort(handle);
                return AbilityActivationResult.InsufficientResource;
            }

            CancelExecutionsMatchedBy(execution);
            StartCooldown(execution.Definition);
            ApplyEffects(execution.Definition.commitEffects, _owner);
            ApplyEffects(execution.Variant.ownerEffects, _owner);
            ApplyEffects(execution.Variant.targetEffects, execution.Target);
            ApplyResourceRules(
                AbilityResourceTrigger.AbilityCommitted,
                execution.Definition.abilityTagIds);

            execution.StartTime = Time.time;
            execution.State = AbilityExecutionState.Active;
            if (_latestPreparedExecution == handle.Value)
                _latestPreparedExecution = 0;
            if (execution.Definition.concurrency
                == AbilityConcurrencyPolicy.Background)
            {
                _backgroundExecutions.Add(handle.Value);
            }
            else
            {
                _primaryExecution = handle.Value;
            }
            AddAbilityBlocks(execution);
            AddExecutionTags(execution);
            if (execution.Definition.taskGraph?.Root != null)
                _abilitySystem.Runtime.Tasks.Start(handle, execution.Definition.taskGraph.Root);
            NotifyPlayerAbilityStarted(execution);
            StateChanged?.Invoke();
            return AbilityActivationResult.Success;
        }

        public void Abort(AbilityExecutionHandle handle)
        {
            using AbilityListLock abilityListLock = LockAbilityList();
            if (!handle.IsValid
                || !_executions.TryGetValue(handle.Value, out AbilityExecution execution)
                || execution.State == AbilityExecutionState.Active)
                return;
            _executions.Remove(handle.Value);
            _abilitySystem.Runtime.Tasks.CancelParent(handle, "AbilityAborted");
            execution.State = AbilityExecutionState.Aborted;
            _backgroundExecutions.Remove(handle.Value);
            if (_primaryExecution == handle.Value)
                _primaryExecution = 0;
            if (_latestPreparedExecution == handle.Value)
                _latestPreparedExecution = 0;
        }

        public void EndActiveAbility(bool completed)
        {
            if (_primaryExecution == 0
                || !_executions.TryGetValue(_primaryExecution, out AbilityExecution execution))
            {
                _primaryExecution = 0;
                return;
            }
            EndExecution(
                execution.Handle,
                completed,
                completed ? "AbilityEnded" : "AbilityCancelled");
            StateChanged?.Invoke();
        }

        public void EndActivePlayerAbility(bool completed) => EndActiveAbility(completed);

        public void CancelActiveAbility() => EndActiveAbility(false);

        public void CancelActivePlayerAbility() => CancelActiveAbility();

        public void EndAbility(AbilityExecutionHandle handle, bool completed)
        {
            EndExecution(
                handle,
                completed,
                completed ? "AbilityEnded" : "AbilityCancelled");
            StateChanged?.Invoke();
        }

        public void CancelAllAbilities()
        {
            using AbilityListLock abilityListLock = LockAbilityList();
            if (_executions.Count == 0)
            {
                _primaryExecution = 0;
                _latestPreparedExecution = 0;
                _backgroundExecutions.Clear();
                _activeAbilityBlockTags.Clear();
                return;
            }

            var handles = new List<AbilityExecutionHandle>(_executions.Count);
            foreach (AbilityExecution execution in _executions.Values)
                handles.Add(execution.Handle);
            for (int i = 0; i < handles.Count; i++)
                EndExecution(handles[i], false, "AbilityCancelled");
            _primaryExecution = 0;
            _latestPreparedExecution = 0;
            _backgroundExecutions.Clear();
            _activeAbilityBlockTags.Clear();
            StateChanged?.Invoke();
        }

        public bool TryGetTargetReservation(
            AbilityExecutionHandle handle,
            out AbilityTargetReservation reservation)
        {
            if (handle.IsValid
                && _executions.TryGetValue(handle.Value, out AbilityExecution execution))
            {
                reservation = execution.TargetReservation;
                return true;
            }
            reservation = default;
            return false;
        }

        public bool TryGetTriggerEvent(
            AbilityExecutionHandle handle,
            out GameplayEventData data)
        {
            if (handle.IsValid
                && _executions.TryGetValue(handle.Value, out AbilityExecution execution)
                && execution.TriggerEvent.HasValue)
            {
                data = execution.TriggerEvent.Value;
                return true;
            }
            data = default;
            return false;
        }

        public bool TryGetActiveExecutionHandle(
            GameplayAbilitySO definition,
            out AbilityExecutionHandle handle)
        {
            foreach (AbilityExecution execution in _executions.Values)
            {
                if (execution.State != AbilityExecutionState.Active
                    || execution.Definition != definition)
                    continue;
                handle = execution.Handle;
                return true;
            }
            handle = default;
            return false;
        }

        public bool IsExecutionActive(AbilityExecutionHandle handle)
        {
            return handle.IsValid
                   && _executions.TryGetValue(
                       handle.Value,
                       out AbilityExecution execution)
                   && execution.State == AbilityExecutionState.Active;
        }

        public bool TryGetPrimaryTargetReservation(
            out AbilityTargetReservation reservation) =>
            TryGetTargetReservation(
                new AbilityExecutionHandle(_primaryExecution),
                out reservation);

        public bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state)
        {
            state = default;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null) return false;

            bool grounded = true;
            if (_owner is PlayerActor player
                && player.PlayerController?.Motor != null)
            {
                grounded = player.PlayerController.Motor.GroundingStatus.IsStableOnGround;
            }

            AbilityActivationResult result =
                Evaluate(
                    definition,
                    grounded,
                    ResolveTarget(definition, null),
                    out AbilityVariantDefinition variant);
            float current = GetResourceCurrent(definition.cost.resourceType);
            float required = GetRequiredCost(
                definition.cost,
                definition.abilityId);
            string group = definition.cooldown.ResolveGroupId(definition.abilityId);
            state = new AbilitySlotViewState(
                definition.abilityId,
                true,
                result != AbilityActivationResult.Locked,
                result == AbilityActivationResult.Success,
                result,
                current,
                required,
                GetCooldownRemaining(group),
                GetEffectiveCooldownDuration(definition, slot),
                variant?.variantId);
            return true;
        }

        public bool TryGetPlayerSlotPresentation(
            PlayerSkillSlot slot,
            out AbilitySlotPresentationState presentation)
        {
            presentation = default;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null)
                return false;

            presentation = new AbilitySlotPresentationState(
                definition.presentation?.displayName,
                definition.presentation?.icon);
            return true;
        }

        public AbilitySystemSaveData CaptureAbilitySystemStateForCharacter(
            bool forCharacterSwap = true)
        {
            AbilitySystemSaveData data = _abilitySystem.Runtime.CaptureSaveData();
            if (!forCharacterSwap)
            {
                data.cooldowns.RemoveAll(
                    entry => entry == null || !ShouldSaveCooldown(entry.groupId));
            }
            data.activeEffects.Clear();
            _effects?.CaptureRuntimeState(data.activeEffects, forCharacterSwap);
            return data;
        }

        public void RestoreAbilitySystemStateForCharacter(AbilitySystemSaveData data)
        {
            BeginTriggerSuppression();
            try
            {
                _abilitySystem.Runtime.RestoreSaveData(data);
                _effects?.RestoreRuntimeState(data?.activeEffects, ResolveEffectDefinition);
            }
            finally
            {
                EndTriggerSuppression();
            }
            StateChanged?.Invoke();
        }

        private bool ShouldSaveCooldown(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return false;
            if (_abilitySet != null)
            {
                foreach (GameplayAbilitySO ability in _abilitySet.EnumerateAll())
                {
                    if (ability == null
                        || ability.persistence == null
                        || !ability.persistence.saveCooldown)
                        continue;
                    if (string.Equals(
                            ability.cooldown.ResolveGroupId(ability.abilityId),
                            groupId,
                            StringComparison.Ordinal))
                        return true;
                }
            }

            GameplayAbilitySO elementalImbue =
                ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue);
            if (elementalImbue?.persistence?.saveCooldown == true
                && string.Equals(
                    elementalImbue.cooldown.ResolveGroupId(elementalImbue.abilityId),
                    groupId,
                    StringComparison.Ordinal))
                return true;

            return false;
        }

        public void HandleCharacterSwap()
        {
            var cancelled = new List<AbilityExecutionHandle>();
            foreach (AbilityExecution execution in _executions.Values)
                if (execution.Definition?.persistence?.swapPolicy
                    == AbilitySwapPolicy.CancelOnSwap)
                    cancelled.Add(execution.Handle);
            for (int i = 0; i < cancelled.Count; i++)
                EndExecution(cancelled[i], false, "CharacterSwap");
            _effects?.RemoveForSwap();
            StateChanged?.Invoke();
        }

        public void HandleOwnerDeath()
        {
            CancelAllAbilities();
            _effects?.RemoveAll();
        }

        private GameplayEffectSO ResolveEffectDefinition(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return null;
            if (_abilitySet != null)
            {
                foreach (GameplayAbilitySO ability in _abilitySet.EnumerateAll())
                {
                    GameplayEffectSO found = FindEffect(ability?.commitEffects, effectId)
                                             ?? FindEffect(ability?.endEffects, effectId);
                    if (found != null) return found;
                    if (ability?.variants == null) continue;
                    for (int i = 0; i < ability.variants.Count; i++)
                    {
                        AbilityVariantDefinition variant = ability.variants[i];
                        found = FindEffect(variant?.ownerEffects, effectId)
                                ?? FindEffect(variant?.targetEffects, effectId);
                        if (found != null) return found;
                    }
                }
            }

            CharacterPassiveSetSO passiveSet =
                Svc.Passives?.GetPassiveSet(_owner.CharacterType);
            if (passiveSet?.passives != null)
            for (int i = 0; i < passiveSet.passives.Count; i++)
            {
                GameplayEffectSO found = FindEffect(
                    passiveSet.passives[i]?.triggeredEffects, effectId);
                if (found != null) return found;
            }
            IReadOnlyList<PassiveAbilitySO> granted =
                Svc.Passives?.GetGrantedPassives(_owner.CharacterType);
            if (granted != null)
            for (int i = 0; i < granted.Count; i++)
            {
                GameplayEffectSO found = FindEffect(
                    granted[i]?.triggeredEffects, effectId);
                if (found != null) return found;
            }
            return null;
        }

        private static GameplayEffectSO FindEffect(
            List<GameplayEffectSO> definitions,
            string effectId)
        {
            if (definitions == null) return null;
            for (int i = 0; i < definitions.Count; i++)
                if (definitions[i] != null
                    && string.Equals(
                        definitions[i].effectId, effectId, StringComparison.Ordinal))
                    return definitions[i];
            return null;
        }

        private AbilityActivationResult Evaluate(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant,
            GameplayEventData? triggerEvent = null)
        {
            variant = null;
            if (definition == null || string.IsNullOrWhiteSpace(definition.abilityId))
                return AbilityActivationResult.InvalidDefinition;
            if (definition.concurrency == AbilityConcurrencyPolicy.Background
                && (definition.persistence?.backgroundMaxDurationSeconds ?? 0f) <= 0f)
                return AbilityActivationResult.InvalidDefinition;
            // Request 전용 라우터 Ability는 실행 데이터(taskGraph / 실행 가능한 Variant
            // payload) 없이도 Prepare될 수 있다. 단 이 완화는 트리거 경로에서 들어온
            // 활성화에만 적용한다. 플레이어 슬롯·BT·치트 등 다른 경로에서 통과시키면
            // 모션 없이 비용과 쿨다운만 소모하는 실행이 만들어진다(spec §3-F).
            bool requestDriven = IsRequestDrivenAbility(definition);
            if (requestDriven && _triggerPathDepth == 0)
                return AbilityActivationResult.MissingExecutionData;
            if (definition.taskGraph?.Root == null && !requestDriven)
                return AbilityActivationResult.MissingExecutionData;
            if (!IsUnlocked(definition))
                return AbilityActivationResult.Locked;
            if (IsBlockedByActiveAbility(definition))
                return AbilityActivationResult.BlockedByActiveAbility;

            AbilityActivationRules activation = definition.activation ?? new AbilityActivationRules();
            AbilityTagEvaluation tagResult = EvaluateOwnerTags(activation);
            if (tagResult != AbilityTagEvaluation.Pass)
                return tagResult == AbilityTagEvaluation.MissingRequired
                    ? AbilityActivationResult.MissingRequiredTag
                    : AbilityActivationResult.BlockedByTag;
            AbilityTagEvaluation sourceTagResult =
                EvaluateSourceTags(activation, triggerEvent);
            if (sourceTagResult != AbilityTagEvaluation.Pass)
                return ToActivationResult(sourceTagResult);
            if (!MatchesGround(activation.groundCondition, isGrounded))
                return AbilityActivationResult.InvalidGroundState;
            if (activation.targetPolicy == AbilityTargetPolicy.Required && target == null)
                return AbilityActivationResult.InvalidTarget;
            if (target != null
                && !MatchesTargetRelation(activation.targetRelation, target))
                return AbilityActivationResult.InvalidTarget;
            AbilityTagEvaluation targetTagResult =
                EvaluateTargetTags(activation, target, triggerEvent);
            if (targetTagResult != AbilityTagEvaluation.Pass)
                return ToActivationResult(targetTagResult);
            if (target != null && !MatchesDistance(activation, target))
                return AbilityActivationResult.OutOfRange;
            if (!CanPayCost(definition.cost, definition.abilityId))
                return AbilityActivationResult.InsufficientResource;
            if (GetCooldownRemaining(definition.cooldown.ResolveGroupId(definition.abilityId)) > 0f)
                return AbilityActivationResult.CooldownActive;
            if ((definition.cooldown?.globalLockSeconds ?? 0f) > 0f
                && GetCooldownRemaining(GlobalCooldownGroupId) > 0f)
                return AbilityActivationResult.CooldownActive;
            if (_primaryExecution != 0
                && definition.concurrency == AbilityConcurrencyPolicy.RejectNew)
                return AbilityActivationResult.ConflictingAbility;

            variant = ResolveVariant(
                definition,
                isGrounded,
                requireExecutablePayload: !requestDriven);
            return variant != null
                ? AbilityActivationResult.Success
                : AbilityActivationResult.MissingExecutionData;
        }

        private static bool IsRequestDrivenAbility(GameplayAbilitySO definition)
        {
            if (definition?.triggers == null || definition.triggers.Count == 0)
                return false;
            for (int i = 0; i < definition.triggers.Count; i++)
                if (definition.triggers[i] == null
                    || definition.triggers[i].mode
                    != AbilityTriggerActivationMode.Request)
                    return false;
            return true;
        }

        private AbilityVariantDefinition ResolveVariant(
            GameplayAbilitySO definition,
            bool grounded,
            bool requireExecutablePayload)
        {
            AbilityVariantDefinition best = null;
            int bestPriority = int.MinValue;
            if (definition.variants == null) return null;

            for (int i = 0; i < definition.variants.Count; i++)
            {
                AbilityVariantDefinition candidate = definition.variants[i];
                if (candidate == null
                    || requireExecutablePayload
                    && !UPlayGroundAbilityPayloadResolver.IsExecutable(candidate))
                    continue;
                AbilityVariantCondition condition = candidate.condition;
                if (condition != null)
                {
                    if (!MatchesGround(condition.groundCondition, grounded)) continue;
                    if (EvaluateOwnerTags(condition) != AbilityTagEvaluation.Pass) continue;
                    float current = GetResourceCurrent(definition.cost.resourceType);
                    float max = GetResourceMax(definition.cost.resourceType);
                    if (condition.minResource > 0f && current < condition.minResource) continue;
                    if (condition.requiresFullResource && (max <= 0f || current < max)) continue;
                }
                if (best != null && candidate.priority <= bestPriority) continue;
                best = candidate;
                bestPriority = candidate.priority;
            }
            return best;
        }

        private bool IsUnlocked(GameplayAbilitySO definition)
        {
            if (_owner is not PlayerActor || Svc.Party == null) return true;
            return Svc.Party.IsAbilityUnlocked(
                Svc.Party.ActiveCharacterType,
                definition.abilityId);
        }

        private bool CanPayCost(AbilityCostDefinition cost, string abilityId)
        {
            if (cost == null || cost.policy == AbilityCostPolicy.None) return true;
            float current = GetResourceCurrent(cost.resourceType);
            float required = GetRequiredCost(cost, abilityId);
            if (float.IsInfinity(current)) return false;
            if (cost.policy == AbilityCostPolicy.All && current <= 0f) return false;
            return current >= required;
        }

        private bool TryConsumeCost(
            AbilityCostDefinition cost,
            AbilityExecutionHandle abilityHandle,
            string abilityId)
        {
            if (!CanPayCost(cost, abilityId)) return false;
            if (cost == null || cost.policy == AbilityCostPolicy.None) return true;
            string resourceId = cost.resourceType.ToString();
            if (!_resources.TryGet(resourceId, out float current, out _))
                return false;

            float required = GetRequiredCost(cost, abilityId);
            return _abilitySystem.TryApplyResourceCost(
                cost.resourceType, required, abilityHandle);
        }

        private float GetRequiredCost(AbilityCostDefinition cost, string abilityId)
        {
            if (cost == null || cost.policy == AbilityCostPolicy.None) return 0f;
            float max = GetResourceMax(cost.resourceType);
            float required = cost.policy switch
            {
                AbilityCostPolicy.Fixed => Mathf.Max(0f, cost.value),
                AbilityCostPolicy.All => Mathf.Max(0f, GetResourceCurrent(cost.resourceType)),
                AbilityCostPolicy.PercentOfMax => Mathf.Max(0f, max * cost.value),
                _ => 0f,
            };
            if (_owner is PlayerActor && Svc.Party != null)
                required *= Svc.Party.GetAbilityScalar(
                    Svc.Party.ActiveCharacterType,
                    abilityId,
                    AbilityScalarKind.Cost);
            return Mathf.Max(0f, required);
        }

        private float GetResourceCurrent(AbilityResourceType type)
        {
            if (type == AbilityResourceType.None) return 0f;
            if (_resources.TryGet(type.ToString(), out float current, out _))
                return current;
            return float.NegativeInfinity;
        }

        private float GetResourceMax(AbilityResourceType type)
        {
            if (_resources.TryGet(type.ToString(), out _, out float maximum))
                return maximum;
            return 0f;
        }

        private void StartCooldown(GameplayAbilitySO definition)
        {
            float duration = GetEffectiveCooldownDuration(
                definition,
                FindPlayerSlot(definition));
            string group = definition.cooldown.ResolveGroupId(definition.abilityId);
            _cooldowns.TryConsumeCharge(
                group,
                duration,
                Mathf.Max(1, definition.cooldown.maxCharges));
            float globalLock = Mathf.Max(
                0f,
                definition.cooldown.globalLockSeconds);
            if (globalLock > 0f)
                _cooldowns.Start(GlobalCooldownGroupId, globalLock);
        }

        private PlayerSkillSlot? FindPlayerSlot(GameplayAbilitySO definition)
        {
            if (definition == null)
                return null;
            if (_abilitySet != null
                && _abilitySet.TryGetPlayerSlot(definition, out PlayerSkillSlot slot))
                return slot;

            if (ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue) == definition)
                return PlayerSkillSlot.ElementalImbue;
            return null;
        }

        private GameplayAbilitySO ResolvePlayerAbility(PlayerSkillSlot slot)
        {
            if (slot == PlayerSkillSlot.ElementalImbue)
            {
                CharacterActorType type = _owner is PlayerActor
                    ? Svc.Party?.ActiveCharacterType ?? CharacterActorType.None
                    : _owner != null ? _owner.CharacterType : CharacterActorType.None;
                return Svc.Party?.GetElementalImbueAbility(type);
            }

            return _abilitySet?.GetPlayerAbility(slot);
        }

        private bool IsGrantedAbility(GameplayAbilitySO definition)
        {
            if (definition == null)
                return false;
            if (_temporaryAbilities.TryGetValue(definition, out int count)
                && count > 0)
                return true;
            if (_abilitySet != null && _abilitySet.Contains(definition))
                return true;
            return ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue) == definition;
        }

        public void GrantTemporaryAbilities(
            IReadOnlyList<GameplayAbilitySO> abilities)
        {
            if (abilities == null)
                return;
            for (int i = 0; i < abilities.Count; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                if (ability == null)
                    continue;
                _temporaryAbilities.TryGetValue(ability, out int count);
                _temporaryAbilities[ability] = count + 1;
            }
            RebuildTriggerIndex();
            StateChanged?.Invoke();
        }

        public void RevokeTemporaryAbilities(
            IReadOnlyList<GameplayAbilitySO> abilities)
        {
            if (abilities == null)
                return;
            for (int i = 0; i < abilities.Count; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                if (ability == null
                    || !_temporaryAbilities.TryGetValue(ability, out int count))
                    continue;
                if (count <= 1)
                    _temporaryAbilities.Remove(ability);
                else
                    _temporaryAbilities[ability] = count - 1;
            }
            RebuildTriggerIndex();
            StateChanged?.Invoke();
        }

        private float GetEffectiveCooldownDuration(
            GameplayAbilitySO definition,
            PlayerSkillSlot? slot)
        {
            float duration = Mathf.Max(0f, definition?.cooldown?.durationSeconds ?? 0f);
            if (_owner is not PlayerActor)
                return duration;

            float multiplier = slot.HasValue
                ? Svc.Passives?.GetActiveSkillCooldownMultiplier(slot.Value) ?? 1f
                : 1f;
            multiplier *= Svc.Party?.GetAbilityScalar(
                Svc.Party.ActiveCharacterType,
                definition.abilityId,
                AbilityScalarKind.Cooldown) ?? 1f;
            return duration * Mathf.Max(0.0001f, multiplier);
        }

        private float GetCooldownRemaining(string group)
        {
            return _cooldowns.GetRemaining(group);
        }

        private void AddExecutionTags(AbilityExecution execution)
        {
            List<GameplayTag> tags =
                execution.Definition.activation?.executionGrantedTagIds;
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], "executionGrantedTagIds", i);
                if (string.IsNullOrEmpty(tags[i].TagName)) continue;
                AbilityTagHandle handle = _tags.Add(
                    tags[i].TagName, "Ability", execution.Handle.Value);
                if (handle.IsValid) execution.GrantedTagHandles.Add(handle);
            }
        }

        private bool IsBlockedByActiveAbility(GameplayAbilitySO definition)
        {
            List<GameplayTag> abilityTags = definition?.abilityTagIds;
            if (abilityTags == null || _activeAbilityBlockTags.Count == 0)
                return false;
            for (int i = 0; i < abilityTags.Count; i++)
            {
                GameplayTag abilityTag = abilityTags[i];
                if (!abilityTag.IsValid()) continue;
                foreach (KeyValuePair<string, int> pair in _activeAbilityBlockTags)
                {
                    if (pair.Value > 0
                        && new AbilityTagId(abilityTag.TagName).IsChildOf(
                            new AbilityTagId(pair.Key)))
                        return true;
                }
            }
            return false;
        }

        private void CancelExecutionsMatchedBy(AbilityExecution incoming)
        {
            List<GameplayTag> cancelTags =
                incoming.Definition?.cancelAbilitiesWithTag;
            if (cancelTags == null || cancelTags.Count == 0)
                return;

            var cancelled = new List<AbilityExecutionHandle>();
            foreach (AbilityExecution active in _executions.Values)
            {
                if (active.State != AbilityExecutionState.Active
                    || active.Handle.Equals(incoming.Handle)
                    || !MatchesAnyAbilityTag(
                        active.Definition?.abilityTagIds,
                        cancelTags))
                    continue;
                cancelled.Add(active.Handle);
            }
            for (int i = 0; i < cancelled.Count; i++)
                EndExecution(cancelled[i], false, "CancelledByAbilityTag");
        }

        private void AddAbilityBlocks(AbilityExecution execution)
        {
            List<GameplayTag> blockTags =
                execution.Definition?.blockAbilitiesWithTag;
            for (int i = 0; i < (blockTags?.Count ?? 0); i++)
            {
                string tagId = blockTags[i].TagName;
                if (string.IsNullOrWhiteSpace(tagId)) continue;
                _activeAbilityBlockTags.TryGetValue(tagId, out int count);
                _activeAbilityBlockTags[tagId] = count + 1;
            }
        }

        private void RemoveAbilityBlocks(AbilityExecution execution)
        {
            List<GameplayTag> blockTags =
                execution.Definition?.blockAbilitiesWithTag;
            for (int i = 0; i < (blockTags?.Count ?? 0); i++)
            {
                string tagId = blockTags[i].TagName;
                if (string.IsNullOrWhiteSpace(tagId)
                    || !_activeAbilityBlockTags.TryGetValue(tagId, out int count))
                    continue;
                if (count <= 1)
                    _activeAbilityBlockTags.Remove(tagId);
                else
                    _activeAbilityBlockTags[tagId] = count - 1;
            }
        }

        private static bool MatchesAnyAbilityTag(
            List<GameplayTag> abilityTags,
            List<GameplayTag> filters)
        {
            for (int i = 0; i < (abilityTags?.Count ?? 0); i++)
            for (int j = 0; j < (filters?.Count ?? 0); j++)
                if (abilityTags[i].IsChildOf(filters[j]))
                    return true;
            return false;
        }

        private void CleanupExecution(AbilityExecution execution)
        {
            for (int i = 0; i < execution.GrantedTagHandles.Count; i++)
                _tags.Remove(execution.GrantedTagHandles[i]);
            execution.GrantedTagHandles.Clear();
        }

        private void ApplyEffects(List<GameplayEffectSO> effects, GameActor target)
        {
            if (effects == null || target?.Effects == null) return;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null)
                    target.Effects.ApplyEffect(effects[i], _owner);
        }

        public void ApplyResourceRules(
            AbilityResourceTrigger trigger,
            IReadOnlyList<GameplayTag> eventTags = null)
        {
            if (_resourceRules?.rules == null)
                return;
            for (int i = 0; i < _resourceRules.rules.Count; i++)
            {
                AbilityResourceRule rule = _resourceRules.rules[i];
                if (rule == null
                    || rule.trigger != trigger
                    || rule.resourceType is AbilityResourceType.None
                        or AbilityResourceType.SkillCharge
                    || Mathf.Approximately(rule.delta, 0f)
                    || !MatchesRuleTag(rule.requiredTag, eventTags))
                    continue;
                _abilitySystem.ApplyResourceDelta(
                    rule.resourceType,
                    rule.delta,
                    $"GE_AbilityResourceRule.{trigger}");
            }
        }

        private static bool MatchesRuleTag(
            GameplayTag required,
            IReadOnlyList<GameplayTag> eventTags)
        {
            if (string.IsNullOrEmpty(required.TagName))
                return true;
            if (eventTags == null)
                return false;
            for (int i = 0; i < eventTags.Count; i++)
                if (string.Equals(
                        required.TagName,
                        eventTags[i].TagName,
                        StringComparison.Ordinal))
                    return true;
            return false;
        }

        private AbilityTagEvaluation EvaluateOwnerTags(AbilityActivationRules activation)
        {
            if (!HasAllTags(
                    activation.requiredTagIds,
                    matchHierarchy: true,
                    nameof(activation.requiredTagIds)))
                return AbilityTagEvaluation.MissingRequired;
            if (HasAnyTag(
                    activation.blockedTagIds,
                    matchHierarchy: true,
                    nameof(activation.blockedTagIds)))
                return AbilityTagEvaluation.Blocked;
            return EvaluateTagRequirement(activation.ownerTagRequirement, _tags);
        }

        private AbilityTagEvaluation EvaluateSourceTags(
            AbilityActivationRules activation,
            GameplayEventData? triggerEvent)
        {
            if (activation?.sourceTagRequirement == null
                || activation.sourceTagRequirement.IsEmpty)
                return AbilityTagEvaluation.Pass;
            if (!triggerEvent.HasValue
                || !AbilitySystemComponent.TryResolve(
                    triggerEvent.Value.Instigator,
                    out AbilitySystemComponent source)
                || source == null
                || source.Tags == null)
                return AbilityTagEvaluation.MissingRequired;
            return EvaluateTagRequirement(
                activation.sourceTagRequirement,
                new UPlayGroundAbilityOwnerPorts(source));
        }

        private static AbilityTagEvaluation EvaluateTargetTags(
            AbilityActivationRules activation,
            GameActor target,
            GameplayEventData? triggerEvent)
        {
            if (target == null
                && triggerEvent.HasValue
                && AbilitySystemComponent.TryResolve(
                    triggerEvent.Value.Target,
                    out AbilitySystemComponent eventTarget))
            {
                target = eventTarget.GetComponent<GameActor>();
            }
            if (activation?.targetTagRequirement == null
                || activation.targetTagRequirement.IsEmpty)
                return AbilityTagEvaluation.Pass;
            if (target == null
                || target.AbilitySystem == null
                || target.AbilitySystem.Tags == null)
                return AbilityTagEvaluation.MissingRequired;
            return EvaluateTagRequirement(
                activation.targetTagRequirement,
                new UPlayGroundAbilityOwnerPorts(target.AbilitySystem));
        }

        private static AbilityActivationResult ToActivationResult(
            AbilityTagEvaluation evaluation)
        {
            return evaluation == AbilityTagEvaluation.MissingRequired
                ? AbilityActivationResult.MissingRequiredTag
                : AbilityActivationResult.BlockedByTag;
        }

        private AbilityTagEvaluation EvaluateOwnerTags(AbilityVariantCondition condition)
        {
            if (!HasAllTags(
                    condition.requiredTagIds,
                    matchHierarchy: true,
                    nameof(condition.requiredTagIds)))
                return AbilityTagEvaluation.MissingRequired;
            if (HasAnyTag(
                    condition.blockedTagIds,
                    matchHierarchy: true,
                    nameof(condition.blockedTagIds)))
                return AbilityTagEvaluation.Blocked;
            return EvaluateTagRequirement(condition.ownerTagRequirement, _tags);
        }

        private static AbilityTagEvaluation EvaluateTagRequirement(
            AbilityTagRequirement requirement,
            IAbilityTagPort tags)
        {
            if (requirement == null || requirement.IsEmpty)
                return AbilityTagEvaluation.Pass;

            bool matchHierarchy =
                requirement.matchMode == AbilityTagMatchMode.Hierarchy;
            if (!HasAllTags(
                    requirement.requireAll,
                    tags,
                    matchHierarchy,
                    nameof(requirement.requireAll)))
                return AbilityTagEvaluation.MissingRequired;
            if ((requirement.requireAny?.Count ?? 0) > 0
                && !HasAnyTag(
                    requirement.requireAny,
                    tags,
                    matchHierarchy,
                    nameof(requirement.requireAny)))
                return AbilityTagEvaluation.MissingRequired;
            if (HasAnyTag(
                    requirement.blockAny,
                    tags,
                    matchHierarchy,
                    nameof(requirement.blockAny)))
                return AbilityTagEvaluation.Blocked;
            if (requirement.expression != null
                && !EvaluateTagExpression(requirement.expression, tags))
            {
                // 중첩 표현식은 실패 원인을 미충족/차단으로 나눌 수 없으므로
                // 평면 조건과 동일하게 fail-closed로 미충족 처리한다.
                return AbilityTagEvaluation.MissingRequired;
            }
            return AbilityTagEvaluation.Pass;
        }

        private static bool EvaluateTagExpression(
            AbilityTagExpression expression,
            IAbilityTagPort tags)
        {
            // 태그 Port 구현이 다시 Ability 조건을 평가하는 경우 공유 어댑터의 Bind 대상이
            // 오염되지 않도록 재진입 호출만 일회성 어댑터로 격리한다.
            if (_isEvaluatingTagExpression)
                return expression.Evaluate(new AbilityTagPortQuerySource(tags));

            _tagQuerySource ??= new AbilityTagPortQuerySource();
            _isEvaluatingTagExpression = true;
            try
            {
                return expression.Evaluate(_tagQuerySource.Bind(tags));
            }
            finally
            {
                _isEvaluatingTagExpression = false;
            }
        }

        private bool HasAllTags(
            List<GameplayTag> tags,
            bool matchHierarchy,
            string fieldName)
        {
            return HasAllTags(tags, _tags, matchHierarchy, fieldName);
        }

        private static bool HasAllTags(
            List<GameplayTag> tags,
            IAbilityTagPort tagPort,
            bool matchHierarchy,
            string fieldName)
        {
            if (tags == null) return true;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], fieldName, i);
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && !tagPort.Has(tags[i].TagName, matchHierarchy))
                    return false;
            }
            return true;
        }

        private bool HasAnyTag(
            List<GameplayTag> tags,
            bool matchHierarchy,
            string fieldName)
        {
            return HasAnyTag(tags, _tags, matchHierarchy, fieldName);
        }

        private static bool HasAnyTag(
            List<GameplayTag> tags,
            IAbilityTagPort tagPort,
            bool matchHierarchy,
            string fieldName)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], fieldName, i);
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && tagPort.Has(tags[i].TagName, matchHierarchy))
                    return true;
            }
            return false;
        }

        private static void EnsureRegisteredOrEmpty(
            GameplayTag tag,
            string fieldName,
            int index)
        {
            if (string.IsNullOrEmpty(tag.TagName) || tag.IsValid()) return;
            throw new InvalidOperationException(
                $"{fieldName}[{index}]에 Registry 미등록 GameplayTag가 있습니다: "
                + $"'{tag.TagName}'");
        }

        private bool MatchesDistance(AbilityActivationRules activation, GameActor target)
        {
            float distance = Vector3.Distance(_owner.transform.position, target.transform.position);
            if (distance < Mathf.Max(0f, activation.minDistance)) return false;
            return activation.maxDistance <= 0f || distance <= activation.maxDistance;
        }

        private GameActor ResolveTarget(GameplayAbilitySO definition, GameActor target)
        {
            return definition?.activation?.targetRelation == AbilityTargetRelation.Self
                ? _owner
                : target;
        }

        private bool MatchesTargetRelation(
            AbilityTargetRelation relation,
            GameActor target)
        {
            if (target == null) return false;
            if (relation == AbilityTargetRelation.Self)
                return ReferenceEquals(target, _owner);
            if (ReferenceEquals(target, _owner))
                return false;

            bool ownerPlayer = _owner.HasActorType(ActorType.Player);
            bool targetPlayer = target.HasActorType(ActorType.Player);
            bool ownerMonster = _owner.HasActorType(ActorType.Monster);
            bool targetMonster = target.HasActorType(ActorType.Monster);
            bool sameFaction = ownerPlayer && targetPlayer
                               || ownerMonster && targetMonster;
            return relation == AbilityTargetRelation.Ally
                ? sameFaction
                : !sameFaction
                  && (ownerPlayer || ownerMonster)
                  && (targetPlayer || targetMonster);
        }

        private static bool MatchesGround(AbilityGroundCondition condition, bool grounded) =>
            condition == AbilityGroundCondition.Any
            || (condition == AbilityGroundCondition.Grounded && grounded)
            || (condition == AbilityGroundCondition.Airborne && !grounded);

        internal void Tick()
        {
            // 예산 초과로 이월된 대기 트리거를 반드시 소진시킨다.
            // 드레인이 신호 수신/락 해제 시점에만 걸리면, 다음 신호가 오기 전까지
            // 이월분이 처리되지 않은 채 남는다.
            TryDrainPendingTriggers();

            if (_cooldowns.RemoveExpired())
                StateChanged?.Invoke();

            if (_backgroundExecutions.Count == 0)
                return;
            _backgroundCompletionBuffer.Clear();
            foreach (ulong value in _backgroundExecutions)
            {
                if (!_executions.TryGetValue(value, out AbilityExecution execution))
                {
                    _backgroundCompletionBuffer.Add((
                        new AbilityExecutionHandle(value),
                        false,
                        "BackgroundExecutionMissing"));
                    continue;
                }
                float maximumDuration =
                    execution.Definition.persistence.backgroundMaxDurationSeconds;
                if (_abilitySystem.Runtime.Tasks.TryConsumeParentCompletion(
                        execution.Handle,
                        out AbilityTaskState taskState,
                        out string taskReason))
                {
                    bool succeeded = taskState == AbilityTaskState.Succeeded;
                    _backgroundCompletionBuffer.Add((
                        execution.Handle,
                        succeeded,
                        string.IsNullOrEmpty(taskReason)
                            ? succeeded ? "BackgroundCompleted" : "BackgroundTaskFailed"
                            : taskReason));
                }
                else if (maximumDuration > 0f
                         && Time.time >= execution.StartTime + maximumDuration)
                {
                    if (execution.TriggerSource == AbilityTriggerSource.OwnedTagPresent
                        && IsOwnedTriggerPresent(execution))
                    {
                        execution.StartTime = Time.time;
                    }
                    else
                    {
                        _backgroundCompletionBuffer.Add((
                            execution.Handle,
                            false,
                            "BackgroundTimeout"));
                    }
                }
                else if (execution.Definition.taskGraph?.Root == null)
                {
                    _backgroundCompletionBuffer.Add((
                        execution.Handle,
                        true,
                        "BackgroundCompleted"));
                }
            }
            for (int i = 0; i < _backgroundCompletionBuffer.Count; i++)
                EndExecution(
                    _backgroundCompletionBuffer[i].Handle,
                    _backgroundCompletionBuffer[i].Succeeded,
                    _backgroundCompletionBuffer[i].Reason);
            if (_backgroundCompletionBuffer.Count > 0)
                StateChanged?.Invoke();
            _backgroundCompletionBuffer.Clear();
        }

        internal void LateTick()
        {
            _stalePreparedExecutionBuffer.Clear();
            foreach (AbilityExecution execution in _executions.Values)
                if (execution.State == AbilityExecutionState.Prepared
                    && Time.frameCount > execution.PreparedFrame + 1)
                    _stalePreparedExecutionBuffer.Add(execution.Handle);
            for (int i = 0; i < _stalePreparedExecutionBuffer.Count; i++)
                Abort(_stalePreparedExecutionBuffer[i]);
            _stalePreparedExecutionBuffer.Clear();
        }

        internal void Dispose()
        {
            _isDisposing = true;
            UnsubscribeTriggerEvents();
            using AbilityListLock abilityListLock = LockAbilityList();
            CancelAllAbilities();
            _executions.Clear();
            _temporaryAbilities.Clear();
            _activeAbilityBlockTags.Clear();
            ClearTriggerRuntime();
        }

        private void EndExecution(
            AbilityExecutionHandle handle,
            bool completed,
            string reason)
        {
            using AbilityListLock abilityListLock = LockAbilityList();
            if (!handle.IsValid
                || !_executions.Remove(handle.Value, out AbilityExecution execution))
            {
                _backgroundExecutions.Remove(handle.Value);
                if (_primaryExecution == handle.Value)
                    _primaryExecution = 0;
                return;
            }

            RemoveAbilityBlocks(execution);
            CleanupExecution(execution);
            _abilitySystem.Runtime.Tasks.CancelParent(handle, reason);
            _abilitySystem.Runtime.Tasks.DiscardParentCompletion(handle);
            execution.State = completed
                ? AbilityExecutionState.Ended
                : AbilityExecutionState.Cancelled;
            if (_owner is PlayerActor player)
            {
                CombatTelemetrySession.NotifyPlayerAbilityEnded(
                    player,
                    handle.Value,
                    completed,
                    reason);
            }
            if (completed)
                ApplyEffects(execution.Definition.endEffects, _owner);
            _backgroundExecutions.Remove(handle.Value);
            if (_primaryExecution == handle.Value)
                _primaryExecution = 0;
        }

        private void RecordActivationResult(AbilityActivationResult result)
        {
            if (result == AbilityActivationResult.Success)
                return;
            _activationFailureCounts.TryGetValue(result, out int count);
            _activationFailureCounts[result] = count + 1;
        }

        private void NotifyPlayerAbilityStarted(AbilityExecution execution)
        {
            if (_owner is not PlayerActor player
                || execution?.Target is not MonsterActor target)
                return;

            string motionKey = null;
            if (UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                    execution.Variant,
                    out AbilityAttackInfo attackInfo)
                && attackInfo.motionKey.IsValid)
            {
                motionKey = attackInfo.motionKey.value;
            }

            CombatTelemetrySession.NotifyPlayerAbilityStarted(
                player,
                target,
                execution.Handle.Value,
                execution.Definition?.abilityId,
                execution.Variant?.variantId,
                motionKey);
        }

        private void RecordPlayerTelemetryFailure(
            GameplayAbilitySO definition,
            GameActor target,
            AbilityActivationResult result)
        {
            if (_owner is not PlayerActor player || target is not MonsterActor monster)
                return;

            CombatTelemetrySession.NotifyPlayerAbilityActivationFailed(
                player,
                monster,
                definition?.abilityId,
                result.ToString());
        }

        private sealed class PlayerAbilityInputPort : IAbilityInputPort
        {
            private readonly PlayerActor _player;

            public PlayerAbilityInputPort(PlayerActor player) => _player = player;

            public AbilityInputState GetSlotState(int slot)
            {
                global::UPlayGround.Input.InputCondition state =
                    _player?.PlayerController?.GetSkillInput(slot)
                    ?? global::UPlayGround.Input.InputCondition.None;
                return state switch
                {
                    global::UPlayGround.Input.InputCondition.Pressed =>
                        AbilityInputState.Pressed,
                    global::UPlayGround.Input.InputCondition.Handled =>
                        AbilityInputState.Held,
                    global::UPlayGround.Input.InputCondition.Canceled =>
                        AbilityInputState.Released,
                    _ => AbilityInputState.None,
                };
            }
        }

    }

    /// <summary>
    /// GAS가 전달한 모션 키를 실행 액터의 모션 데이터로 해석한다.
    /// </summary>
    public static class ActorAbilityMotionResolver
    {
        public static bool TryResolve(
            GameActor actor,
            AbilityAttackInfo attackInfo,
            out MotionSetAsset motionAsset)
        {
            motionAsset = null;
            // 모션 해석은 히트 페이즈(baseInfo) 유무와 무관하다. 모션 전용 Ability도
            // 여기서 해석되어야 하므로 baseInfo를 전제 조건으로 두지 않는다.
            if (actor?.Animator == null || attackInfo == null)
                return false;

            return attackInfo.motionKey.IsValid
                   && actor.Animator.TryResolveAbilityMotion(
                       attackInfo.motionKey,
                       out motionAsset);
        }
    }
}
