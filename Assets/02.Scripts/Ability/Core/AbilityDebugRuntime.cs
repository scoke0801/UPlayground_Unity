using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public enum AbilityDebugCategory
    {
        Ability,
        Task,
        Effect,
        Tag,
        Attribute,
        GameplayEvent,
        Cue,
        Trace,
    }

    public readonly struct AbilityDebugEvent
    {
        public ulong Sequence { get; }
        public int Frame { get; }
        public float Time { get; }
        public AbilityDebugCategory Category { get; }
        public string EventType { get; }
        public ulong AbilityHandle { get; }
        public ulong TaskHandle { get; }
        public ulong EffectHandle { get; }
        public AttributeId AttributeId { get; }
        public float OldValue { get; }
        public float NewValue { get; }
        public string Result { get; }
        public string Source { get; }
        public string Message { get; }

        public AbilityDebugEvent(
            ulong sequence,
            int frame,
            float time,
            AbilityDebugCategory category,
            string eventType,
            ulong abilityHandle = 0,
            ulong taskHandle = 0,
            ulong effectHandle = 0,
            AttributeId attributeId = default,
            float oldValue = 0f,
            float newValue = 0f,
            string result = null,
            string source = null,
            string message = null)
        {
            Sequence = sequence;
            Frame = frame;
            Time = time;
            Category = category;
            EventType = eventType ?? string.Empty;
            AbilityHandle = abilityHandle;
            TaskHandle = taskHandle;
            EffectHandle = effectHandle;
            AttributeId = attributeId;
            OldValue = oldValue;
            NewValue = newValue;
            Result = result ?? string.Empty;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    public sealed class AbilityDebugRecorder
    {
        private readonly AbilityDebugEvent[] _buffer;
        private readonly IAbilityClock _clock;
        private int _start;
        private int _count;
        private ulong _sequence;

        public AbilityDebugRecorder(IAbilityClock clock, int capacity = 512)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _buffer = new AbilityDebugEvent[Math.Max(1, capacity)];
        }

        public bool Enabled { get; set; }
        public int Count => _count;
        public int Capacity => _buffer.Length;

        public void Record(
            AbilityDebugCategory category,
            string eventType,
            ulong abilityHandle = 0,
            ulong taskHandle = 0,
            ulong effectHandle = 0,
            AttributeId attributeId = default,
            float oldValue = 0f,
            float newValue = 0f,
            string result = null,
            string source = null,
            string message = null)
        {
            if (!Enabled) return;
            var item = new AbilityDebugEvent(
                ++_sequence,
                _clock.Frame,
                _clock.Time,
                category,
                eventType,
                abilityHandle,
                taskHandle,
                effectHandle,
                attributeId,
                oldValue,
                newValue,
                result,
                source,
                message);
            int index = (_start + _count) % _buffer.Length;
            if (_count == _buffer.Length)
            {
                _buffer[_start] = item;
                _start = (_start + 1) % _buffer.Length;
            }
            else
            {
                _buffer[index] = item;
                _count++;
            }
        }

        public void CopyTo(ICollection<AbilityDebugEvent> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < _count; i++)
                destination.Add(_buffer[(_start + i) % _buffer.Length]);
        }
    }

    [Flags]
    public enum AbilityDebugCaptureOptions
    {
        None = 0,
        Attributes = 1 << 0,
        Tags = 1 << 1,
        Events = 1 << 2,
        Effects = 1 << 3,
        Tasks = 1 << 4,
        All = Attributes | Tags | Events | Effects | Tasks,
    }

    public readonly struct ActiveGameplayEffectDebugState
    {
        public ulong ActiveHandle { get; }
        public ulong SpecHandle { get; }
        public string EffectId { get; }
        public int StackCount { get; }
        public float DurationSeconds { get; }
        public float RemainingSeconds { get; }
        public float PeriodSeconds { get; }

        public ActiveGameplayEffectDebugState(ActiveGameplayEffect active)
        {
            ActiveHandle = active.Handle.Value;
            SpecHandle = active.Spec.Handle.Value;
            EffectId = active.Spec.Definition.EffectId;
            StackCount = active.StackCount;
            DurationSeconds = active.DurationSeconds;
            RemainingSeconds = active.RemainingSeconds;
            PeriodSeconds = active.PeriodSeconds;
        }
    }

    public readonly struct AbilityTaskDebugState
    {
        public ulong TaskHandle { get; }
        public ulong ParentAbilityHandle { get; }
        public string TaskType { get; }
        public AbilityTaskState State { get; }
        public string EndReason { get; }

        public AbilityTaskDebugState(AbilityTaskInstance task)
        {
            TaskHandle = task.Handle.Value;
            ParentAbilityHandle = task.ParentAbility.Value;
            TaskType = task.GetType().Name;
            State = task.State;
            EndReason = task.EndReason;
        }
    }

    public sealed class AbilitySystemDebugSnapshot
    {
        public AbilitySystemHandle AbilitySystemHandle { get; internal set; }
        public string OwnerId { get; internal set; }
        public int Frame { get; internal set; }
        public float Time { get; internal set; }
        public IReadOnlyDictionary<AttributeId, GameplayAttributeValue> Attributes { get; internal set; }
        public IReadOnlyList<AbilityTagId> Tags { get; internal set; }
        public IReadOnlyList<AbilityDebugEvent> Events { get; internal set; }
        public IReadOnlyList<ActiveGameplayEffectDebugState> Effects { get; internal set; }
        public IReadOnlyList<AbilityTaskDebugState> Tasks { get; internal set; }
    }

    public interface IAbilitySystemDebugSource
    {
        AbilitySystemDebugSnapshot CaptureDebugSnapshot(AbilityDebugCaptureOptions options);
    }
}
