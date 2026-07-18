using System.Collections.Generic;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Effect
{
    public readonly struct GameplayEffectHandle
    {
        internal readonly ulong Value;
        public bool IsValid => Value != 0;
        internal GameplayEffectHandle(ulong value) => Value = value;
    }

    internal sealed class GameplayEffectInstance
    {
        public GameplayEffectHandle Handle;
        public GameplayEffectSO Definition;
        public GameActor Source;
        public int StackCount;
        public float RemainingSeconds;
        public float NextPeriodSeconds;
        public object ModifierSource;
        public GameplayTagSource TagSource;
        public readonly List<GameplayTagHandle> TagHandles = new();
    }
}
