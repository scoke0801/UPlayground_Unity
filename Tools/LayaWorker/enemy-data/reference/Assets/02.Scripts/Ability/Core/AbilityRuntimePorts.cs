namespace UPlayGround.Ability.Core
{
    public interface IAbilityClock
    {
        float Time { get; }
        int Frame { get; }
    }

    public interface IAbilityResourcePort
    {
        bool TryGet(string resourceId, out float current, out float maximum);
        bool TrySet(string resourceId, float value);
    }

    public enum AbilityInputState
    {
        None,
        Pressed,
        Held,
        Released,
    }

    public interface IAbilityInputPort
    {
        AbilityInputState GetSlotState(int slot);
    }

    public readonly struct AbilityTagHandle
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public AbilityTagHandle(ulong value) => Value = value;
    }

    public interface IAbilityTagPort
    {
        bool Has(string tagId);
        bool Has(string tagId, bool matchHierarchy);
        AbilityTagHandle Add(string tagId, string sourceType, ulong sourceId);
        bool Remove(AbilityTagHandle handle);
    }

    public enum AbilityModifierOperation
    {
        Add,
        Percent,
        Multiply,
        Override,
    }

    public readonly struct AbilityModifierHandle
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public AbilityModifierHandle(ulong value) => Value = value;
    }

    public interface IAbilityStatPort
    {
        AbilityModifierHandle AddModifier(
            string statId,
            AbilityModifierOperation operation,
            float magnitude,
            string sourceType,
            ulong sourceId);
        bool RemoveModifier(AbilityModifierHandle handle);
    }

    public interface IAbilityExecutionPort
    {
        AbilityActivationResult TryBegin(
            AbilityExecutionHandle handle,
            AbilityExecutionPayloadSO payload);
        void End(AbilityExecutionHandle handle, bool completed);
    }
}
