using System.Collections.Generic;
using UnityEngine;
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
        public AbilityTargetReservation TargetReservation { get; }
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
            TargetReservation = new AbilityTargetReservation(
                definition?.targeting?.mode ?? AbilityTargetingMode.None,
                target,
                target != null ? target.transform.position : owner.transform.position,
                target != null
                    ? (target.transform.position - owner.transform.position).normalized
                    : owner.transform.forward,
                preparedFrame);
            PreparedFrame = preparedFrame;
            State = AbilityExecutionState.Prepared;
        }
    }

    public readonly struct AbilityTargetReservation
    {
        public AbilityTargetingMode Mode { get; }
        public GameActor Target { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public int ConfirmedFrame { get; }
        public bool IsConfirmed => ConfirmedFrame >= 0;

        public AbilityTargetReservation(
            AbilityTargetingMode mode,
            GameActor target,
            Vector3 position,
            Vector3 direction,
            int confirmedFrame)
        {
            Mode = mode;
            Target = target;
            Position = position;
            Direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
            ConfirmedFrame = confirmedFrame;
        }
    }
}
