using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Ability.Core
{
    public enum AbilityTaskState
    {
        Created,
        Activating,
        Active,
        Succeeded,
        Failed,
        Cancelled,
        Ended,
    }

    public readonly struct AbilityTaskHandle : IEquatable<AbilityTaskHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public AbilityTaskHandle(ulong value) => Value = value;
        public bool Equals(AbilityTaskHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AbilityTaskHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public sealed class AbilityTaskContext
    {
        public AbilityTaskContext(
            AbilitySystemRuntime owner,
            AbilityExecutionHandle parentAbility,
            IAbilityClock clock,
            Func<AbilitySystemHandle, AbilitySystemRuntime> systemResolver = null)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            ParentAbility = parentAbility;
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            SystemResolver = systemResolver;
        }

        public AbilitySystemRuntime Owner { get; }
        public AbilityExecutionHandle ParentAbility { get; }
        public IAbilityClock Clock { get; }
        public Func<AbilitySystemHandle, AbilitySystemRuntime> SystemResolver { get; }
    }

    public abstract class AbilityTaskInstance
    {
        private bool _completionRaised;

        protected AbilityTaskInstance(AbilityTaskContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public AbilityTaskHandle Handle { get; internal set; }
        public AbilityTaskContext Context { get; }
        public AbilityExecutionHandle ParentAbility => Context.ParentAbility;
        public AbilityTaskState State { get; private set; } = AbilityTaskState.Created;
        public string EndReason { get; private set; } = string.Empty;
        internal bool FailParentOnFailure { get; set; } = true;
        public event Action<AbilityTaskInstance> Completed;

        public void Activate()
        {
            if (State != AbilityTaskState.Created) return;
            State = AbilityTaskState.Activating;
            try
            {
                OnActivate();
                if (State == AbilityTaskState.Activating) State = AbilityTaskState.Active;
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        public void Tick()
        {
            if (State != AbilityTaskState.Active) return;
            try
            {
                OnTick();
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        public void Cancel(string reason = null)
        {
            if (IsTerminal(State)) return;
            State = AbilityTaskState.Cancelled;
            EndReason = reason ?? "ParentCancelled";
            End();
        }

        protected void Succeed(string reason = null)
        {
            if (IsTerminal(State)) return;
            State = AbilityTaskState.Succeeded;
            EndReason = reason ?? string.Empty;
            End();
        }

        protected void Fail(string reason)
        {
            if (IsTerminal(State)) return;
            State = AbilityTaskState.Failed;
            EndReason = reason ?? string.Empty;
            End();
        }

        protected virtual void OnActivate() { }
        protected virtual void OnTick() { }
        protected virtual void OnEnd() { }

        private void End()
        {
            try
            {
                OnEnd();
            }
            finally
            {
                if (!_completionRaised)
                {
                    _completionRaised = true;
                    Completed?.Invoke(this);
                }
            }
        }

        internal static bool IsTerminal(AbilityTaskState state) =>
            state is AbilityTaskState.Succeeded
                or AbilityTaskState.Failed
                or AbilityTaskState.Cancelled
                or AbilityTaskState.Ended;
    }

    public abstract class AbilityTaskDefinitionSO : ScriptableObject
    {
        public bool failParentOnFailure = true;
        public abstract AbilityTaskInstance CreateRuntime(AbilityTaskContext context);

        internal AbilityTaskInstance CreateRuntimeWithPolicy(AbilityTaskContext context)
        {
            AbilityTaskInstance task = CreateRuntime(context);
            if (task != null)
                task.FailParentOnFailure = failParentOnFailure;
            return task;
        }
    }

    public sealed class WaitDelayTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        [Min(0f)] public float duration;
        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitDelayTask(context, duration);
    }

    public sealed class WaitGameplayEventTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        public string eventTag;
        public bool matchHierarchy;
        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitGameplayEventTask(context, new AbilityTagId(eventTag), matchHierarchy);
    }

    public sealed class WaitTagTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        public string tag;
        public bool waitForAdded = true;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitTagTask(context, new AbilityTagId(tag), waitForAdded);
    }

    public enum AbilityAttributeThresholdComparison
    {
        GreaterOrEqual,
        LessOrEqual,
    }

    public sealed class WaitAttributeThresholdTaskDefinitionSO
        : AbilityTaskDefinitionSO
    {
        [AttributeIdSelector] public string attributeId;
        public AbilityAttributeThresholdComparison comparison;
        public float threshold;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitAttributeThresholdTask(
                context,
                new AttributeId(attributeId),
                comparison,
                threshold);
    }

    public sealed class WaitInputTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        [Min(0)] public int slot;
        public bool waitForRelease = true;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitInputTask(context, slot, waitForRelease);
    }

    public sealed class SequenceAbilityTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        [SerializeField] private List<AbilityTaskDefinitionSO> _children = new();
        public IReadOnlyList<AbilityTaskDefinitionSO> Children => _children;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context)
        {
            var factories = new List<Func<AbilityTaskContext, AbilityTaskInstance>>(_children.Count);
            for (int i = 0; i < _children.Count; i++)
            {
                AbilityTaskDefinitionSO child = _children[i];
                factories.Add(item =>
                    child != null ? child.CreateRuntimeWithPolicy(item) : null);
            }
            return new SequenceAbilityTask(context, factories);
        }
    }

    public sealed class ParallelAbilityTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        [SerializeField] private bool _completeOnAny;
        [SerializeField] private List<AbilityTaskDefinitionSO> _children = new();
        public bool CompleteOnAny => _completeOnAny;
        public IReadOnlyList<AbilityTaskDefinitionSO> Children => _children;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context)
        {
            var factories = new List<Func<AbilityTaskContext, AbilityTaskInstance>>(_children.Count);
            for (int i = 0; i < _children.Count; i++)
            {
                AbilityTaskDefinitionSO child = _children[i];
                factories.Add(item =>
                    child != null ? child.CreateRuntimeWithPolicy(item) : null);
            }
            return new ParallelAbilityTask(context, factories, _completeOnAny);
        }
    }

    public sealed class SelectBranchTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        public string conditionTag;
        public AbilityTaskDefinitionSO whenPresent;
        public AbilityTaskDefinitionSO whenAbsent;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context)
        {
            AbilityTaskDefinitionSO selected =
                context.Owner.Tags.Has(new AbilityTagId(conditionTag))
                    ? whenPresent
                    : whenAbsent;
            return new SelectBranchTask(
                context,
                selected != null ? selected.CreateRuntimeWithPolicy(context) : null);
        }
    }

    public sealed class LoopAbilityTaskDefinitionSO : AbilityTaskDefinitionSO
    {
        [Min(1)] public int maxIterations = 1;
        [Min(0f)] public float maxDuration;
        public AbilityTaskDefinitionSO child;

        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new LoopAbilityTask(
                context,
                item => child != null ? child.CreateRuntimeWithPolicy(item) : null,
                maxIterations,
                maxDuration);
    }

    public sealed class AbilityTaskContainer : IDisposable
    {
        private readonly AbilitySystemRuntime _owner;
        private readonly IAbilityClock _clock;
        private readonly Dictionary<ulong, AbilityTaskInstance> _active = new();
        private readonly Dictionary<ulong, HashSet<ulong>> _byParent = new();
        private readonly Dictionary<ulong, (AbilityTaskState State, string Reason)>
            _completedByParent = new();
        private ulong _nextHandle = 1;

        public AbilityTaskContainer(AbilitySystemRuntime owner, IAbilityClock clock)
        {
            _owner = owner;
            _clock = clock;
        }

        public int Count => _active.Count;

        public AbilityTaskHandle Start(
            AbilityExecutionHandle parent,
            AbilityTaskDefinitionSO definition,
            Func<AbilitySystemHandle, AbilitySystemRuntime> resolver = null)
        {
            if (!parent.IsValid || definition == null) return default;
            var context = new AbilityTaskContext(_owner, parent, _clock, resolver);
            return Start(parent, definition.CreateRuntimeWithPolicy(context));
        }

        public AbilityTaskHandle Start(
            AbilityExecutionHandle parent,
            AbilityTaskInstance task)
        {
            if (!parent.IsValid || task == null || !task.ParentAbility.Equals(parent))
                return default;
            ulong value = _nextHandle++;
            if (value == 0) value = _nextHandle++;
            var handle = new AbilityTaskHandle(value);
            task.Handle = handle;
            task.Completed += OnTaskCompleted;
            _active.Add(value, task);
            if (!_byParent.TryGetValue(parent.Value, out HashSet<ulong> tasks))
            {
                tasks = new HashSet<ulong>();
                _byParent.Add(parent.Value, tasks);
            }
            tasks.Add(value);
            Record("Started", task);
            task.Activate();
            return handle;
        }

        public void Tick()
        {
            if (_active.Count == 0) return;
            var snapshot = new List<AbilityTaskInstance>(_active.Values);
            for (int i = 0; i < snapshot.Count; i++)
                if (_active.ContainsKey(snapshot[i].Handle.Value)) snapshot[i].Tick();
        }

        public int CancelParent(AbilityExecutionHandle parent, string reason = null)
        {
            if (!parent.IsValid || !_byParent.TryGetValue(parent.Value, out HashSet<ulong> ids))
                return 0;
            var tasks = new List<AbilityTaskInstance>(ids.Count);
            foreach (ulong id in ids)
                if (_active.TryGetValue(id, out AbilityTaskInstance task)) tasks.Add(task);
            for (int i = 0; i < tasks.Count; i++) tasks[i].Cancel(reason);
            return tasks.Count;
        }

        public bool HasActiveForParent(AbilityExecutionHandle parent) =>
            parent.IsValid
            && _byParent.TryGetValue(parent.Value, out HashSet<ulong> ids)
            && ids.Count > 0;

        public bool TryConsumeParentCompletion(
            AbilityExecutionHandle parent,
            out AbilityTaskState state,
            out string reason)
        {
            state = default;
            reason = string.Empty;
            if (!parent.IsValid
                || HasActiveForParent(parent)
                || !_completedByParent.Remove(
                    parent.Value,
                    out (AbilityTaskState State, string Reason) completion))
                return false;
            state = completion.State;
            reason = completion.Reason;
            return true;
        }

        public void DiscardParentCompletion(AbilityExecutionHandle parent)
        {
            if (parent.IsValid)
                _completedByParent.Remove(parent.Value);
        }

        public void CopyActive(ICollection<AbilityTaskInstance> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (AbilityTaskInstance task in _active.Values) destination.Add(task);
        }

        public void Dispose()
        {
            var parents = new List<AbilityExecutionHandle>();
            foreach (ulong parent in _byParent.Keys) parents.Add(new AbilityExecutionHandle(parent));
            for (int i = 0; i < parents.Count; i++) CancelParent(parents[i], "ContainerDisposed");
        }

        private void OnTaskCompleted(AbilityTaskInstance task)
        {
            task.Completed -= OnTaskCompleted;
            _active.Remove(task.Handle.Value);
            AbilityTaskState parentState =
                task.State == AbilityTaskState.Failed
                && !task.FailParentOnFailure
                    ? AbilityTaskState.Succeeded
                    : task.State;
            AggregateParentCompletion(
                task.ParentAbility.Value,
                parentState,
                task.EndReason);
            if (_byParent.TryGetValue(task.ParentAbility.Value, out HashSet<ulong> ids))
            {
                ids.Remove(task.Handle.Value);
                if (ids.Count == 0)
                    _byParent.Remove(task.ParentAbility.Value);
            }
            Record(task.State.ToString(), task);
        }

        private void AggregateParentCompletion(
            ulong parent,
            AbilityTaskState state,
            string reason)
        {
            if (!_completedByParent.TryGetValue(
                    parent,
                    out (AbilityTaskState State, string Reason) current)
                || CompletionPriority(state) > CompletionPriority(current.State))
            {
                _completedByParent[parent] = (state, reason ?? string.Empty);
            }
        }

        private static int CompletionPriority(AbilityTaskState state) =>
            state switch
            {
                AbilityTaskState.Failed => 3,
                AbilityTaskState.Cancelled => 2,
                AbilityTaskState.Succeeded or AbilityTaskState.Ended => 1,
                _ => 0,
            };

        private void Record(string eventType, AbilityTaskInstance task) =>
            _owner.Debug.Record(
                AbilityDebugCategory.Task,
                eventType,
                abilityHandle: task.ParentAbility.Value,
                taskHandle: task.Handle.Value,
                result: task.State.ToString(),
                source: task.GetType().Name,
                message: task.EndReason);
    }

    public sealed class WaitDelayTask : AbilityTaskInstance
    {
        private readonly float _duration;
        private float _endTime;

        public WaitDelayTask(AbilityTaskContext context, float duration) : base(context) =>
            _duration = Math.Max(0f, duration);

        protected override void OnActivate()
        {
            _endTime = Context.Clock.Time + _duration;
            if (_duration <= 0f) Succeed();
        }

        protected override void OnTick()
        {
            if (Context.Clock.Time >= _endTime) Succeed();
        }
    }

    public sealed class WaitGameplayEventTask : AbilityTaskInstance
    {
        private readonly AbilityTagId _eventTag;
        private readonly bool _matchHierarchy;
        private IDisposable _subscription;
        public GameplayEventData ReceivedEvent { get; private set; }

        public WaitGameplayEventTask(
            AbilityTaskContext context,
            AbilityTagId eventTag,
            bool matchHierarchy = false) : base(context)
        {
            _eventTag = eventTag;
            _matchHierarchy = matchHierarchy;
        }

        protected override void OnActivate()
        {
            if (!_eventTag.IsValid)
            {
                Fail("Event Tag가 유효하지 않습니다.");
                return;
            }
            _subscription = Context.Owner.Events.Subscribe(_eventTag, OnEvent, _matchHierarchy);
        }

        protected override void OnEnd()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void OnEvent(GameplayEventData data)
        {
            ReceivedEvent = data;
            Succeed(data.EventTag.Value);
        }
    }

    public sealed class WaitTagTask : AbilityTaskInstance
    {
        private readonly AbilityTagId _tag;
        private readonly bool _waitForAdded;

        public WaitTagTask(
            AbilityTaskContext context,
            AbilityTagId tag,
            bool waitForAdded) : base(context)
        {
            _tag = tag;
            _waitForAdded = waitForAdded;
        }

        protected override void OnActivate()
        {
            if (!_tag.IsValid)
            {
                Fail("Tag가 유효하지 않습니다.");
                return;
            }
            Context.Owner.Tags.TagAdded += OnTagAdded;
            Context.Owner.Tags.TagRemoved += OnTagRemoved;
            if (Context.Owner.Tags.Has(_tag) == _waitForAdded)
                Succeed(_tag.Value);
        }

        protected override void OnEnd()
        {
            Context.Owner.Tags.TagAdded -= OnTagAdded;
            Context.Owner.Tags.TagRemoved -= OnTagRemoved;
        }

        private void OnTagAdded(AbilityTagId tag)
        {
            if (_waitForAdded && tag.Equals(_tag))
                Succeed(tag.Value);
        }

        private void OnTagRemoved(AbilityTagId tag)
        {
            if (!_waitForAdded && tag.Equals(_tag))
                Succeed(tag.Value);
        }
    }

    public sealed class WaitAttributeThresholdTask : AbilityTaskInstance
    {
        private readonly AttributeId _attributeId;
        private readonly AbilityAttributeThresholdComparison _comparison;
        private readonly float _threshold;

        public WaitAttributeThresholdTask(
            AbilityTaskContext context,
            AttributeId attributeId,
            AbilityAttributeThresholdComparison comparison,
            float threshold) : base(context)
        {
            _attributeId = attributeId;
            _comparison = comparison;
            _threshold = threshold;
        }

        protected override void OnActivate()
        {
            if (!_attributeId.IsValid)
            {
                Fail("Attribute ID가 유효하지 않습니다.");
                return;
            }
            Context.Owner.Attributes.AttributeChanged += OnAttributeChanged;
            EvaluateCurrent();
        }

        protected override void OnEnd() =>
            Context.Owner.Attributes.AttributeChanged -= OnAttributeChanged;

        private void OnAttributeChanged(AttributeChangedEvent change)
        {
            if (change.AttributeId == _attributeId)
                Evaluate(change.NewCurrent);
        }

        private void EvaluateCurrent()
        {
            if (Context.Owner.Attributes.TryGet(
                    _attributeId,
                    out GameplayAttributeValue value))
                Evaluate(value.CurrentValue);
        }

        private void Evaluate(float value)
        {
            bool reached =
                _comparison == AbilityAttributeThresholdComparison.GreaterOrEqual
                    ? value >= _threshold
                    : value <= _threshold;
            if (reached)
                Succeed(_attributeId.Value);
        }
    }

    public sealed class WaitInputTask : AbilityTaskInstance
    {
        private readonly int _slot;
        private readonly bool _waitForRelease;

        public WaitInputTask(
            AbilityTaskContext context,
            int slot,
            bool waitForRelease) : base(context)
        {
            _slot = Math.Max(0, slot);
            _waitForRelease = waitForRelease;
        }

        protected override void OnActivate()
        {
            if (Context.Owner.Input == null)
                Fail("IAbilityInputPort가 연결되지 않았습니다.");
        }

        protected override void OnTick()
        {
            AbilityInputState state = Context.Owner.Input.GetSlotState(_slot);
            if ((_waitForRelease && state == AbilityInputState.Released)
                || (!_waitForRelease && state == AbilityInputState.Pressed))
                Succeed(state.ToString());
        }
    }

    public sealed class ApplyGameplayEffectTask : AbilityTaskInstance
    {
        private readonly GameplayEffectSpec _spec;
        private readonly AbilitySystemRuntime _target;
        public GameplayEffectApplyOutcome Outcome { get; private set; }

        public ApplyGameplayEffectTask(
            AbilityTaskContext context,
            GameplayEffectSpec spec,
            AbilitySystemRuntime target) : base(context)
        {
            _spec = spec;
            _target = target;
        }

        protected override void OnActivate()
        {
            if (_target == null || _spec == null)
            {
                Fail("Effect Spec 또는 Target이 없습니다.");
                return;
            }
            Outcome = _target.Effects.Apply(_spec, Context.Owner);
            if (Outcome.Succeeded) Succeed();
            else Fail($"{Outcome.Result}: {Outcome.Error}");
        }
    }

    public sealed class SelectBranchTask : AbilityTaskInstance
    {
        private readonly AbilityTaskInstance _selected;

        public SelectBranchTask(
            AbilityTaskContext context,
            AbilityTaskInstance selected) : base(context) =>
            _selected = selected;

        protected override void OnActivate()
        {
            if (_selected == null)
            {
                Fail("선택된 Branch Task가 없습니다.");
                return;
            }
            _selected.Completed += OnCompleted;
            _selected.Activate();
        }

        protected override void OnTick() => _selected?.Tick();

        protected override void OnEnd()
        {
            if (_selected == null)
                return;
            _selected.Completed -= OnCompleted;
            if (!AbilityTaskInstance.IsTerminal(_selected.State))
                _selected.Cancel("BranchEnded");
        }

        private void OnCompleted(AbilityTaskInstance child)
        {
            child.Completed -= OnCompleted;
            if (child.State == AbilityTaskState.Succeeded)
                Succeed(child.EndReason);
            else if (child.State == AbilityTaskState.Cancelled)
                Cancel(child.EndReason);
            else if (!child.FailParentOnFailure)
                Succeed(child.EndReason);
            else
                Fail(child.EndReason);
        }
    }

    public sealed class LoopAbilityTask : AbilityTaskInstance
    {
        private readonly Func<AbilityTaskContext, AbilityTaskInstance> _factory;
        private readonly int _maxIterations;
        private readonly float _maxDuration;
        private AbilityTaskInstance _current;
        private int _completedIterations;
        private float _endTime;

        public LoopAbilityTask(
            AbilityTaskContext context,
            Func<AbilityTaskContext, AbilityTaskInstance> factory,
            int maxIterations,
            float maxDuration) : base(context)
        {
            _factory = factory;
            _maxIterations = Math.Max(1, maxIterations);
            _maxDuration = Math.Max(0f, maxDuration);
        }

        protected override void OnActivate()
        {
            _endTime = _maxDuration > 0f
                ? Context.Clock.Time + _maxDuration
                : float.PositiveInfinity;
            StartNext();
        }

        protected override void OnTick()
        {
            if (Context.Clock.Time >= _endTime)
            {
                Succeed("LoopDurationReached");
                return;
            }
            _current?.Tick();
        }

        protected override void OnEnd()
        {
            if (_current == null)
                return;
            _current.Completed -= OnChildCompleted;
            if (!AbilityTaskInstance.IsTerminal(_current.State))
                _current.Cancel("LoopEnded");
        }

        private void StartNext()
        {
            if (_completedIterations >= _maxIterations
                || Context.Clock.Time >= _endTime)
            {
                Succeed();
                return;
            }
            _current = _factory?.Invoke(Context);
            if (_current == null)
            {
                Fail("Loop Task 생성 실패");
                return;
            }
            _current.Completed += OnChildCompleted;
            _current.Activate();
        }

        private void OnChildCompleted(AbilityTaskInstance child)
        {
            child.Completed -= OnChildCompleted;
            if (child.State != AbilityTaskState.Succeeded)
            {
                if (child.State == AbilityTaskState.Cancelled)
                    Cancel(child.EndReason);
                else if (!child.FailParentOnFailure)
                {
                    _completedIterations++;
                    StartNext();
                }
                else
                    Fail(child.EndReason);
                return;
            }
            _completedIterations++;
            StartNext();
        }
    }

    public sealed class SequenceAbilityTask : AbilityTaskInstance
    {
        private readonly IReadOnlyList<Func<AbilityTaskContext, AbilityTaskInstance>> _factories;
        private int _index;
        private AbilityTaskInstance _current;

        public SequenceAbilityTask(
            AbilityTaskContext context,
            IReadOnlyList<Func<AbilityTaskContext, AbilityTaskInstance>> factories) : base(context) =>
            _factories = factories ?? Array.Empty<Func<AbilityTaskContext, AbilityTaskInstance>>();

        protected override void OnActivate() => StartNext();

        protected override void OnEnd()
        {
            if (_current != null)
            {
                _current.Completed -= OnChildCompleted;
                if (!AbilityTaskInstance.IsTerminal(_current.State)) _current.Cancel("SequenceEnded");
            }
        }

        private void StartNext()
        {
            if (_index >= _factories.Count)
            {
                Succeed();
                return;
            }
            _current = _factories[_index++](Context);
            if (_current == null)
            {
                Fail("Sequence Task 생성 실패");
                return;
            }
            _current.Completed += OnChildCompleted;
            _current.Activate();
        }

        protected override void OnTick() => _current?.Tick();

        private void OnChildCompleted(AbilityTaskInstance child)
        {
            child.Completed -= OnChildCompleted;
            if (child.State == AbilityTaskState.Succeeded
                || (child.State == AbilityTaskState.Failed
                    && !child.FailParentOnFailure))
                StartNext();
            else if (child.State == AbilityTaskState.Cancelled) Cancel(child.EndReason);
            else Fail(child.EndReason);
        }
    }

    public sealed class ParallelAbilityTask : AbilityTaskInstance
    {
        private readonly IReadOnlyList<Func<AbilityTaskContext, AbilityTaskInstance>> _factories;
        private readonly bool _completeOnAny;
        private readonly List<AbilityTaskInstance> _children = new();
        private int _succeeded;

        public ParallelAbilityTask(
            AbilityTaskContext context,
            IReadOnlyList<Func<AbilityTaskContext, AbilityTaskInstance>> factories,
            bool completeOnAny) : base(context)
        {
            _factories = factories ?? Array.Empty<Func<AbilityTaskContext, AbilityTaskInstance>>();
            _completeOnAny = completeOnAny;
        }

        protected override void OnActivate()
        {
            if (_factories.Count == 0)
            {
                Succeed();
                return;
            }

            for (int i = 0; i < _factories.Count; i++)
            {
                AbilityTaskInstance child = _factories[i](Context);
                if (child == null)
                {
                    Fail("Parallel Task 생성 실패");
                    return;
                }
                _children.Add(child);
                child.Completed += OnChildCompleted;
            }

            for (int i = 0; i < _children.Count; i++)
            {
                AbilityTaskInstance child = _children[i];
                if (!AbilityTaskInstance.IsTerminal(child.State))
                    child.Activate();
            }
        }

        protected override void OnTick()
        {
            for (int i = 0; i < _children.Count; i++) _children[i].Tick();
        }

        protected override void OnEnd()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                AbilityTaskInstance child = _children[i];
                child.Completed -= OnChildCompleted;
                if (!AbilityTaskInstance.IsTerminal(child.State)) child.Cancel("ParallelEnded");
            }
        }

        private void OnChildCompleted(AbilityTaskInstance child)
        {
            child.Completed -= OnChildCompleted;
            if (child.State != AbilityTaskState.Succeeded)
            {
                if (child.State == AbilityTaskState.Failed
                    && !child.FailParentOnFailure)
                {
                    _succeeded++;
                    if (_completeOnAny || _succeeded == _children.Count) Succeed();
                    return;
                }
                if (child.State == AbilityTaskState.Cancelled)
                    Cancel(child.EndReason);
                else
                    Fail(child.EndReason);
                return;
            }
            _succeeded++;
            if (_completeOnAny || _succeeded == _children.Count) Succeed();
        }
    }
}
