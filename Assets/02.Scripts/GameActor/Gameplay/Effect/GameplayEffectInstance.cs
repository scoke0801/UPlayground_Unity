using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;

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
        public float DurationSeconds;
        public float RemainingSeconds;
        public float NextPeriodSeconds;
        public GameplayEffectHudVisibility HudVisibility;
        public ActiveGameplayEffectHandle GasHandle;
        public bool GrantsElement;
    }
}
