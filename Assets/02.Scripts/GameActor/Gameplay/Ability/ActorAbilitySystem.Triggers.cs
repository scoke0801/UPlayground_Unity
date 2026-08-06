using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Combat;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Ability
{
    public readonly struct AbilityTriggerRequest
    {
        public readonly GameplayAbilitySO Ability;
        public readonly AbilityVariantDefinition Variant;
        public readonly GameplayTag TriggerTag;
        public readonly AbilityTriggerSource Source;
        public readonly AbilityTagMatchMode MatchMode;
        public readonly GameplayEventData? TriggerEvent;
        internal readonly int TriggerIndex;

        public AbilityTriggerRequest(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            GameplayTag triggerTag,
            AbilityTriggerSource source,
            AbilityTagMatchMode matchMode,
            GameplayEventData? triggerEvent)
            : this(
                ability,
                variant,
                triggerTag,
                source,
                matchMode,
                triggerEvent,
                -1)
        {
        }

        internal AbilityTriggerRequest(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            GameplayTag triggerTag,
            AbilityTriggerSource source,
            AbilityTagMatchMode matchMode,
            GameplayEventData? triggerEvent,
            int triggerIndex)
        {
            Ability = ability;
            Variant = variant;
            TriggerTag = triggerTag;
            Source = source;
            MatchMode = matchMode;
            TriggerEvent = triggerEvent;
            TriggerIndex = triggerIndex;
        }
    }

    public sealed partial class ActorAbilitySystem
    {
        private const int MaxTriggerDrainBudget = 64;

        /// <summary>
        /// 대기 큐 상한. 예산 초과분은 폐기 대신 이월하므로 큐가 무한히 자라지 않도록
        /// 상한을 둔다. 정상 전투에서 도달할 수 없는 값이어야 한다.
        /// </summary>
        private const int MaxPendingTriggerQueueSize = 512;

        private bool _pendingQueueOverflowReported;

        /// <summary>
        /// 0보다 크면 현재 활성화가 트리거 경로에서 들어온 것이다.
        /// Request 전용 Ability의 실행 데이터 검증 완화를 이 경로로만 한정하는 데 쓴다.
        /// </summary>
        private int _triggerPathDepth;

        private enum PendingTriggerKind
        {
            Activate,
            CancelPresent,
        }

        private readonly struct TriggerEntry
        {
            public readonly GameplayAbilitySO Ability;
            public readonly AbilityTriggerDefinition Trigger;
            public readonly int TriggerIndex;

            public TriggerEntry(
                GameplayAbilitySO ability,
                AbilityTriggerDefinition trigger,
                int triggerIndex)
            {
                Ability = ability;
                Trigger = trigger;
                TriggerIndex = triggerIndex;
            }
        }

        private readonly struct PendingTrigger
        {
            public readonly PendingTriggerKind Kind;
            public readonly TriggerEntry Entry;
            public readonly AbilityTagId SignalTag;
            public readonly GameplayEventData? TriggerEvent;

            public PendingTrigger(
                TriggerEntry entry,
                AbilityTagId signalTag,
                GameplayEventData? triggerEvent)
            {
                Kind = PendingTriggerKind.Activate;
                Entry = entry;
                SignalTag = signalTag;
                TriggerEvent = triggerEvent;
            }

            public PendingTrigger(AbilityTagId removedTag)
            {
                Kind = PendingTriggerKind.CancelPresent;
                Entry = default;
                SignalTag = removedTag;
                TriggerEvent = null;
            }
        }

        private readonly struct AbilityListLock : IDisposable
        {
            private readonly ActorAbilitySystem _owner;

            public AbilityListLock(ActorAbilitySystem owner)
            {
                _owner = owner;
                _owner._listLockDepth++;
            }

            public void Dispose()
            {
                _owner?.ReleaseAbilityListLock();
            }
        }

        private readonly Dictionary<string, List<TriggerEntry>> _exactTriggers =
            new(StringComparer.Ordinal);
        private readonly List<TriggerEntry> _hierarchyTriggers = new();
        private readonly Dictionary<(GameplayAbilitySO Ability, int TriggerIndex), float>
            _lastTriggerTime = new();
        private readonly Queue<PendingTrigger> _pendingTriggers = new();
        private readonly List<TriggerEntry> _matchedTriggers = new();
        private int _listLockDepth;
        private int _triggerSuppressionDepth;
        private bool _triggerEventsSubscribed;
        private bool _isDisposing;

        public event Action<AbilityTriggerRequest> AbilityTriggerRequested;
        public event Action<AbilityTriggerRequest, AbilityExecutionHandle>
            AbilityTriggerAccepted;
        public event Action<AbilityExecutionHandle> AbilityTriggerCancelRequested;
        public event Action<GameplayAbilitySO, AbilityActivationResult>
            AbilityTriggerRejected;

        public void IssueTriggerEvent(
            GameplayTag eventTag,
            GameActor instigator = null,
            GameActor target = null,
            object payload = null)
        {
            if (!eventTag.IsValid() || _abilitySystem?.Runtime == null)
                return;
            var data = new GameplayEventData(
                new AbilityTagId(eventTag.TagName),
                instigator?.AbilitySystem?.Runtime?.Handle ?? default,
                target?.AbilitySystem?.Runtime?.Handle
                    ?? _abilitySystem.Runtime.Handle,
                payload: payload);
            _abilitySystem.Runtime.Events.Send(data);
        }

        public bool TryGetRequestTriggerAbility(
            GameplayTag eventTag,
            out GameplayAbilitySO ability)
        {
            ability = null;
            if (!eventTag.IsValid())
                return false;

            if (_exactTriggers.TryGetValue(
                    eventTag.TagName,
                    out List<TriggerEntry> exact))
            {
                for (int i = 0; i < exact.Count; i++)
                {
                    AbilityTriggerDefinition trigger = exact[i].Trigger;
                    if (trigger.source == AbilityTriggerSource.GameplayEvent
                        && trigger.mode == AbilityTriggerActivationMode.Request)
                    {
                        ability = exact[i].Ability;
                        return ability != null;
                    }
                }
            }

            var eventTagId = new AbilityTagId(eventTag.TagName);
            for (int i = 0; i < _hierarchyTriggers.Count; i++)
            {
                TriggerEntry entry = _hierarchyTriggers[i];
                if (entry.Trigger.source == AbilityTriggerSource.GameplayEvent
                    && entry.Trigger.mode == AbilityTriggerActivationMode.Request
                    && eventTagId.IsChildOf(
                        new AbilityTagId(entry.Trigger.triggerTag.TagName)))
                {
                    ability = entry.Ability;
                    return ability != null;
                }
            }
            return false;
        }

        public void IssueTriggerEvent(
            GameplayTag eventTag,
            in HitContext context) =>
            IssueTriggerEvent(
                eventTag,
                context.Attacker,
                context.Victim,
                context);

        private AbilityListLock LockAbilityList() => new(this);

        private void ReleaseAbilityListLock()
        {
            if (_listLockDepth <= 0)
                return;
            _listLockDepth--;
            if (_listLockDepth == 0)
                DrainPendingTriggers();
        }

        private void SubscribeTriggerEvents()
        {
            if (_triggerEventsSubscribed || _abilitySystem?.Runtime == null)
                return;
            _abilitySystem.Runtime.Tags.TagAdded += OnTagAddedForTrigger;
            _abilitySystem.Runtime.Tags.TagRemoved += OnTagRemovedForTrigger;
            _abilitySystem.Runtime.Events.EventSent += OnEventForTrigger;
            _triggerEventsSubscribed = true;
        }

        private void UnsubscribeTriggerEvents()
        {
            if (!_triggerEventsSubscribed || _abilitySystem?.Runtime == null)
                return;
            _abilitySystem.Runtime.Tags.TagAdded -= OnTagAddedForTrigger;
            _abilitySystem.Runtime.Tags.TagRemoved -= OnTagRemovedForTrigger;
            _abilitySystem.Runtime.Events.EventSent -= OnEventForTrigger;
            _triggerEventsSubscribed = false;
        }

        /// <summary>
        /// 트리거 인덱스만 다시 만든다. 대기 큐(_pendingTriggers)와 재트리거 이력
        /// (_lastTriggerTime)은 건드리지 않는다 — 임시 Ability 부여/회수처럼 인덱스만
        /// 바뀌는 상황에서 아직 처리되지 않은 트리거가 사라지거나
        /// retriggerIntervalSeconds 게이트가 초기화되면 안 되기 때문이다.
        /// 큐와 이력을 실제로 비워야 하는 지점(AbilitySet 교체, 억제 종료, Dispose)은
        /// ClearTriggerRuntime 또는 명시적 Clear를 사용한다.
        /// </summary>
        private void RebuildTriggerIndex()
        {
            _exactTriggers.Clear();
            _hierarchyTriggers.Clear();

            var indexed = new HashSet<GameplayAbilitySO>();
            if (_abilitySet != null)
            {
                IReadOnlyList<GameplayAbilitySO> abilities =
                    _abilitySet.GetRuntimeAbilities();
                for (int i = 0; i < abilities.Count; i++)
                    IndexAbilityTriggers(abilities[i], indexed);
            }

            foreach (KeyValuePair<GameplayAbilitySO, int> pair in _temporaryAbilities)
                if (pair.Value > 0)
                    IndexAbilityTriggers(pair.Key, indexed);
        }

        private void IndexAbilityTriggers(
            GameplayAbilitySO ability,
            HashSet<GameplayAbilitySO> indexed)
        {
            if (ability == null
                || !indexed.Add(ability)
                || ability.triggers == null)
                return;

            for (int i = 0; i < ability.triggers.Count; i++)
            {
                AbilityTriggerDefinition trigger = ability.triggers[i];
                if (trigger == null
                    || string.IsNullOrWhiteSpace(trigger.triggerTag.TagName))
                    continue;

                var entry = new TriggerEntry(ability, trigger, i);
                if (trigger.matchMode == AbilityTagMatchMode.Hierarchy)
                {
                    _hierarchyTriggers.Add(entry);
                    continue;
                }

                string tagId = trigger.triggerTag.TagName;
                if (!_exactTriggers.TryGetValue(tagId, out List<TriggerEntry> entries))
                {
                    entries = new List<TriggerEntry>();
                    _exactTriggers.Add(tagId, entries);
                }
                entries.Add(entry);
            }
        }

        private void OnTagAddedForTrigger(AbilityTagId tag)
        {
            EnqueueMatchingTriggers(
                tag,
                AbilityTriggerSource.OwnedTagAdded,
                triggerEvent: null,
                includeOwnedTagPresent: true);
        }

        private void OnTagRemovedForTrigger(AbilityTagId tag)
        {
            if (_isDisposing || _triggerSuppressionDepth > 0)
                return;
            EnqueuePendingTrigger(new PendingTrigger(tag), tag);
            TryDrainPendingTriggers();
        }

        /// <summary>
        /// 대기 큐에 넣는 단일 진입점. 큐 상한을 넘으면 새 항목을 버린다.
        /// 상한을 넘는 상황은 밀린 백로그가 아니라 트리거가 스스로를 재발행하는
        /// 폭주이므로, 오래된 항목(=실제 게임플레이 사건)을 지키고 새 항목을 버린다.
        /// </summary>
        private void EnqueuePendingTrigger(in PendingTrigger pending, AbilityTagId signalTag)
        {
            if (_pendingTriggers.Count >= MaxPendingTriggerQueueSize)
            {
                if (!_pendingQueueOverflowReported)
                {
                    _pendingQueueOverflowReported = true;
                    Debug.LogError(
                        $"[AbilityTrigger] 대기 큐가 상한 {MaxPendingTriggerQueueSize}건에 도달해 "
                        + $"'{signalTag.Value}' 트리거를 버렸습니다. "
                        + "트리거가 자기 자신을 재발행하는 순환이 없는지 확인하세요.",
                        _owner);
                }
                return;
            }

            _pendingTriggers.Enqueue(pending);
        }

        private void OnEventForTrigger(GameplayEventData data)
        {
            EnqueueMatchingTriggers(
                data.EventTag,
                AbilityTriggerSource.GameplayEvent,
                data,
                includeOwnedTagPresent: false);
        }

        private void EnqueueMatchingTriggers(
            AbilityTagId signalTag,
            AbilityTriggerSource source,
            GameplayEventData? triggerEvent,
            bool includeOwnedTagPresent)
        {
            if (_isDisposing || _triggerSuppressionDepth > 0 || !signalTag.IsValid)
                return;

            _matchedTriggers.Clear();
            if (_exactTriggers.TryGetValue(signalTag.Value, out List<TriggerEntry> exact))
                AddMatchingSourceEntries(exact, source, includeOwnedTagPresent);
            for (int i = 0; i < _hierarchyTriggers.Count; i++)
            {
                TriggerEntry entry = _hierarchyTriggers[i];
                if (!MatchesSource(entry.Trigger.source, source, includeOwnedTagPresent)
                    || !signalTag.IsChildOf(
                        new AbilityTagId(entry.Trigger.triggerTag.TagName)))
                    continue;
                _matchedTriggers.Add(entry);
            }

            _matchedTriggers.Sort((left, right) =>
                right.Trigger.priority.CompareTo(left.Trigger.priority));
            for (int i = 0; i < _matchedTriggers.Count; i++)
                EnqueuePendingTrigger(
                    new PendingTrigger(_matchedTriggers[i], signalTag, triggerEvent),
                    signalTag);
            _matchedTriggers.Clear();
            TryDrainPendingTriggers();
        }

        private void AddMatchingSourceEntries(
            List<TriggerEntry> entries,
            AbilityTriggerSource source,
            bool includeOwnedTagPresent)
        {
            for (int i = 0; i < entries.Count; i++)
                if (MatchesSource(
                        entries[i].Trigger.source,
                        source,
                        includeOwnedTagPresent))
                    _matchedTriggers.Add(entries[i]);
        }

        private static bool MatchesSource(
            AbilityTriggerSource entrySource,
            AbilityTriggerSource signalSource,
            bool includeOwnedTagPresent)
        {
            return entrySource == signalSource
                   || includeOwnedTagPresent
                   && signalSource == AbilityTriggerSource.OwnedTagAdded
                   && entrySource == AbilityTriggerSource.OwnedTagPresent;
        }

        private void TryDrainPendingTriggers()
        {
            if (_listLockDepth == 0)
                DrainPendingTriggers();
        }

        private void DrainPendingTriggers()
        {
            if (_listLockDepth != 0 || _pendingTriggers.Count == 0)
                return;
            if (_isDisposing)
            {
                _pendingTriggers.Clear();
                return;
            }

            _listLockDepth++;
            int processed = 0;
            try
            {
                while (_pendingTriggers.Count > 0
                       && processed < MaxTriggerDrainBudget)
                {
                    PendingTrigger pending = _pendingTriggers.Dequeue();
                    processed++;
                    try
                    {
                        if (pending.Kind == PendingTriggerKind.CancelPresent)
                            ProcessOwnedTagPresentCancellation(pending.SignalTag);
                        else
                            ProcessTriggerActivation(pending);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"[AbilityTrigger] '{pending.Entry.Ability?.abilityId}' 트리거 처리 중 오류가 발생했습니다.\n{exception}",
                            _owner);
                    }
                }

                // 예산 초과분은 폐기하지 않고 다음 드레인으로 이월한다.
                // 여기서 Clear()하면 피격 리액션처럼 같은 큐를 타는 게임플레이 사건이
                // 다인 전투·AOE 상황에서 조용히 사라진다.
                // 폭주(트리거가 스스로를 재발행)는 EnqueuePendingTrigger의 큐 상한이 막는다.
                if (_pendingTriggers.Count > 0)
                {
                    Debug.LogWarning(
                        $"[AbilityTrigger] 한 번의 처리 예산 {MaxTriggerDrainBudget}건을 초과했습니다. "
                        + $"남은 {_pendingTriggers.Count}건은 다음 처리로 이월합니다. "
                        + $"(다음 대기 태그: {_pendingTriggers.Peek().SignalTag.Value})",
                        _owner);
                }
            }
            finally
            {
                _listLockDepth--;
            }
        }

        private void ProcessTriggerActivation(PendingTrigger pending)
        {
            TriggerEntry entry = pending.Entry;
            GameplayAbilitySO ability = entry.Ability;
            AbilityTriggerDefinition trigger = entry.Trigger;
            if (ability == null
                || trigger == null
                || IsSelfGrantedTrigger(ability, trigger)
                || IsAbilityActive(ability))
                return;

            RecordTriggerDebug(entry, "Detected", pending.SignalTag.Value);

            var intervalKey = (ability, entry.TriggerIndex);
            if (trigger.source != AbilityTriggerSource.OwnedTagPresent
                && trigger.retriggerIntervalSeconds > 0f
                && _lastTriggerTime.TryGetValue(intervalKey, out float lastTime)
                && Time.time < lastTime + trigger.retriggerIntervalSeconds)
                return;

            GameActor target = ability.activation?.targetPolicy
                is AbilityTargetPolicy.Optional or AbilityTargetPolicy.Required
                    ? ResolveTriggerTarget(pending.TriggerEvent)
                    : null;
            if (trigger.mode == AbilityTriggerActivationMode.Request)
            {
                ProcessTriggerRequest(
                    entry,
                    pending.TriggerEvent,
                    target);
                return;
            }
            if (ability.concurrency != AbilityConcurrencyPolicy.Background)
                return;

            AbilityActivationResult prepared = TryPrepareAbility(
                ability,
                IsOwnerGrounded(),
                target,
                out AbilityExecutionHandle handle,
                out _,
                pending.TriggerEvent);
            if (prepared != AbilityActivationResult.Success)
            {
                RecordTriggerDebug(entry, "Rejected", prepared.ToString());
                return;
            }

            if (_executions.TryGetValue(handle.Value, out AbilityExecution execution))
            {
                execution.TriggerTag = trigger.triggerTag;
                execution.TriggerSource = trigger.source;
                execution.TriggerActivationMode = trigger.mode;
                execution.TriggerMatchMode = trigger.matchMode;
                execution.TriggerEvent = pending.TriggerEvent;
            }

            AbilityActivationResult committed = Commit(handle);
            if (committed == AbilityActivationResult.Success)
            {
                RecordTriggerDebug(entry, "Committed", pending.SignalTag.Value, handle.Value);
                if (trigger.source != AbilityTriggerSource.OwnedTagPresent)
                    _lastTriggerTime[intervalKey] = Time.time;
                return;
            }
            Abort(handle);
            RecordTriggerDebug(entry, "Rejected", committed.ToString(), handle.Value);
        }

        private void ProcessTriggerRequest(
            TriggerEntry entry,
            GameplayEventData? triggerEvent,
            GameActor target)
        {
            if (_primaryExecution != 0
                && !entry.Trigger.allowPreemption)
            {
                ReportTriggerRejected(
                    entry.Ability,
                    AbilityActivationResult.ConflictingAbility);
                return;
            }

            _triggerPathDepth++;
            try
            {
                ProcessTriggerRequestCore(entry, triggerEvent, target);
            }
            finally
            {
                _triggerPathDepth--;
            }
        }

        private void ProcessTriggerRequestCore(
            TriggerEntry entry,
            GameplayEventData? triggerEvent,
            GameActor target)
        {
            AbilityActivationResult evaluation = EvaluateAbility(
                entry.Ability,
                IsOwnerGrounded(),
                target,
                out AbilityVariantDefinition variant,
                triggerEvent);
            if (evaluation != AbilityActivationResult.Success)
            {
                RecordTriggerDebug(entry, "Rejected", evaluation.ToString());
                AbilityTriggerRejected?.Invoke(entry.Ability, evaluation);
                return;
            }

            Action<AbilityTriggerRequest> requested = AbilityTriggerRequested;
            if (requested == null)
            {
                ReportTriggerRejected(
                    entry.Ability,
                    AbilityActivationResult.StateTransitionRejected);
                return;
            }

            // 구독자는 이 호출 안에서 동기적으로 TryPrepareAbility를 부른다.
            // 호출자(ProcessTriggerRequest)가 _triggerPathDepth를 올려 둔 상태이므로
            // Request 전용 라우터 Ability의 실행 데이터 검증 완화가 여기서만 적용된다.
            requested.Invoke(new AbilityTriggerRequest(
                entry.Ability,
                variant,
                entry.Trigger.triggerTag,
                entry.Trigger.source,
                entry.Trigger.matchMode,
                triggerEvent,
                entry.TriggerIndex));
            RecordTriggerDebug(entry, "Requested", entry.Trigger.triggerTag.TagName);
        }

        public void ReportTriggerRejected(
            GameplayAbilitySO ability,
            AbilityActivationResult reason)
        {
            RecordActivationResult(reason);
            _abilitySystem?.Runtime?.Debug.Record(
                AbilityDebugCategory.Ability,
                "TriggerRejected",
                result: reason.ToString(),
                source: ability?.abilityId);
            AbilityTriggerRejected?.Invoke(ability, reason);
        }

        public bool BindActiveExecutionToTrigger(in AbilityTriggerRequest request)
        {
            foreach (AbilityExecution execution in _executions.Values)
            {
                if (execution.State != AbilityExecutionState.Active
                    || execution.Definition != request.Ability)
                    continue;
                return BindActiveExecutionToTrigger(execution.Handle, request);
            }
            return false;
        }

        /// <summary>
        /// 카테고리 트리거처럼 요청을 받은 Ability와 실제 실행 Ability가 다른 경우에도
        /// 정확한 실행 핸들에 트리거 출처를 귀속시킨다.
        /// </summary>
        public bool BindActiveExecutionToTrigger(
            AbilityExecutionHandle handle,
            in AbilityTriggerRequest request)
        {
            if (!handle.IsValid
                || !_executions.TryGetValue(handle.Value, out AbilityExecution execution)
                || execution.State != AbilityExecutionState.Active)
                return false;

            execution.TriggerTag = request.TriggerTag;
            execution.TriggerSource = request.Source;
            execution.TriggerActivationMode = AbilityTriggerActivationMode.Request;
            execution.TriggerMatchMode = request.MatchMode;
            execution.TriggerEvent = request.TriggerEvent;
            RecordAcceptedRequestTriggerInterval(request);
            AbilityTriggerAccepted?.Invoke(request, handle);
            return true;
        }

        private void RecordAcceptedRequestTriggerInterval(
            in AbilityTriggerRequest request)
        {
            if (request.Ability?.triggers == null
                || request.TriggerIndex < 0
                || request.TriggerIndex >= request.Ability.triggers.Count)
                return;

            AbilityTriggerDefinition trigger =
                request.Ability.triggers[request.TriggerIndex];
            if (trigger == null
                || trigger.mode != AbilityTriggerActivationMode.Request
                || trigger.source != request.Source
                || trigger.matchMode != request.MatchMode
                || trigger.triggerTag != request.TriggerTag
                || trigger.source == AbilityTriggerSource.OwnedTagPresent)
                return;

            _lastTriggerTime[(request.Ability, request.TriggerIndex)] = Time.time;
        }

        private void ProcessOwnedTagPresentCancellation(AbilityTagId removedTag)
        {
            var cancelled = new List<AbilityExecutionHandle>();
            foreach (AbilityExecution execution in _executions.Values)
            {
                if (execution.State != AbilityExecutionState.Active
                    || execution.TriggerSource != AbilityTriggerSource.OwnedTagPresent
                    || !MatchesTriggerTag(
                        removedTag,
                        execution.TriggerTag,
                        execution.TriggerMatchMode)
                    || IsOwnedTriggerPresent(execution))
                    continue;
                cancelled.Add(execution.Handle);
            }

            for (int i = 0; i < cancelled.Count; i++)
            {
                AbilityExecutionHandle handle = cancelled[i];
                if (_executions.TryGetValue(handle.Value, out AbilityExecution execution)
                    && execution.TriggerActivationMode
                    == AbilityTriggerActivationMode.Request)
                {
                    if (AbilityTriggerCancelRequested != null)
                        AbilityTriggerCancelRequested.Invoke(handle);
                    else
                        EndExecution(handle, false, "TriggerTagLost");
                }
                else
                {
                    EndExecution(handle, false, "TriggerTagLost");
                }
            }
        }

        private bool IsOwnedTriggerPresent(AbilityExecution execution)
        {
            if (execution == null
                || string.IsNullOrEmpty(execution.TriggerTag.TagName))
                return false;
            return _tags.Has(
                execution.TriggerTag.TagName,
                execution.TriggerMatchMode == AbilityTagMatchMode.Hierarchy);
        }

        private static bool MatchesTriggerTag(
            AbilityTagId signalTag,
            GameplayTag triggerTag,
            AbilityTagMatchMode matchMode)
        {
            var expected = new AbilityTagId(triggerTag.TagName);
            return matchMode == AbilityTagMatchMode.Hierarchy
                ? signalTag.IsChildOf(expected)
                : signalTag.Equals(expected);
        }

        private static bool IsSelfGrantedTrigger(
            GameplayAbilitySO ability,
            AbilityTriggerDefinition trigger)
        {
            List<GameplayTag> granted =
                ability.activation?.executionGrantedTagIds;
            if (granted == null)
                return false;
            for (int i = 0; i < granted.Count; i++)
                if (string.Equals(
                        granted[i].TagName,
                        trigger.triggerTag.TagName,
                        StringComparison.Ordinal))
                    return true;
            return false;
        }

        private bool IsAbilityActive(GameplayAbilitySO ability)
        {
            foreach (AbilityExecution execution in _executions.Values)
                if (execution.State == AbilityExecutionState.Active
                    && execution.Definition == ability)
                    return true;
            return false;
        }

        private bool IsOwnerGrounded()
        {
            return _owner is not PlayerActor player
                   || player.PlayerController?.Motor == null
                   || player.PlayerController.Motor.GroundingStatus.IsStableOnGround;
        }

        private static GameActor ResolveTriggerTarget(GameplayEventData? triggerEvent)
        {
            if (!triggerEvent.HasValue
                || !AbilitySystemComponent.TryResolve(
                    triggerEvent.Value.Target,
                    out AbilitySystemComponent component))
                return null;
            return component.GetComponent<GameActor>();
        }

        private void ClearTriggerRuntime()
        {
            _exactTriggers.Clear();
            _hierarchyTriggers.Clear();
            _lastTriggerTime.Clear();
            _pendingTriggers.Clear();
            _matchedTriggers.Clear();
            _pendingQueueOverflowReported = false;
        }

        /// <summary>
        /// 대기 트리거와 재트리거 이력을 비운다. 인덱스는 유지한다.
        /// 이전 상태 기준의 대기·이력이 무효가 되는 지점에서만 호출한다.
        /// </summary>
        private void ClearPendingTriggerState()
        {
            _pendingTriggers.Clear();
            _lastTriggerTime.Clear();
            _pendingQueueOverflowReported = false;
        }

        private void BeginTriggerSuppression() => _triggerSuppressionDepth++;

        private void EndTriggerSuppression()
        {
            if (_triggerSuppressionDepth > 0)
                _triggerSuppressionDepth--;
            if (_triggerSuppressionDepth == 0)
            {
                ClearPendingTriggerState();
                RebuildTriggerIndex();
            }
        }

        private void RecordTriggerDebug(
            in TriggerEntry entry,
            string eventType,
            string message,
            ulong handle = 0)
        {
            _abilitySystem?.Runtime?.Debug.Record(
                AbilityDebugCategory.Ability,
                $"Trigger{eventType}",
                abilityHandle: handle,
                result: entry.Trigger?.mode.ToString(),
                source: entry.Ability?.abilityId,
                message: $"{entry.Trigger?.source}:{entry.Trigger?.triggerTag.TagName} {message}");
        }
    }
}
