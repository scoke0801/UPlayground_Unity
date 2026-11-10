using UnityEngine;
using UPlayGround.Components.Pet;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround
{
    /// <summary>파티 슬롯·적 어그로·피격 대상과 독립된 동행 액터.</summary>
    public sealed class PetActor : GameActor, IManagedTick
    {
        private PetBrain _brain;
        private PetPresentation _presentation;
        public bool HasSafeGround => _brain?.HasSafeGround ?? false;
        protected override bool RequiresCombatVisuals => false;

        public void Configure(PetDefinitionSO definition, PlayerActor player, string appearance)
        {
            var movement = (PetMovementController)ActorController;
            movement.Configure(definition.followProfile);
            _presentation = GetComponent<PetPresentation>();
            _presentation?.ApplyAppearance(definition.FindAppearance(appearance));
            Services.TryGet<IGroundDropService>(out var drops);
            _brain = new PetBrain(this, player, movement, definition, drops, _presentation);
        }

        public void SetAppearance(PetDefinitionSO definition, string appearance) =>
            _presentation?.ApplyAppearance(definition.FindAppearance(appearance));
        public void SetSuspended(bool suspended) => _brain?.SetSuspended(suspended);
        public void ManagedTick(float deltaTime) => _brain?.ManagedTick(deltaTime);
        public void CancelActions() => _brain?.Cancel();
        protected override void OnDestroy() { CancelActions(); base.OnDestroy(); }
    }
}
