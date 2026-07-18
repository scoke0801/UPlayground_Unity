using System.Collections.Generic;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;

namespace UPlayGround.Gameplay.Ability
{
    public sealed class AbilityExecution
    {
        public AbilityExecutionHandle Handle { get; }
        public GameplayAbilitySO Definition { get; }
        public AbilityVariantDefinition Variant { get; }
        public GameActor Owner { get; }
        public GameActor Target { get; }
        public int PreparedFrame { get; }
        public float StartTime { get; internal set; }
        public AbilityExecutionState State { get; internal set; }
        internal readonly List<AbilityTagHandle> GrantedTagHandles = new();
        internal readonly List<Effect.GameplayEffectHandle> TemporaryEffectHandles = new();

        internal AbilityExecution(
            AbilityExecutionHandle handle,
            GameplayAbilitySO definition,
            AbilityVariantDefinition variant,
            GameActor owner,
            GameActor target,
            int preparedFrame)
        {
            Handle = handle;
            Definition = definition;
            Variant = variant;
            Owner = owner;
            Target = target;
            PreparedFrame = preparedFrame;
            State = AbilityExecutionState.Prepared;
        }
    }
}
