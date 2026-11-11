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
        public bool CanSupport => _brain?.CanSupport ?? false;
        public int GroundRecallCount => _brain?.GroundRecallCount ?? 0;
        public int SpawnPlacementCount => _brain?.SpawnPlacementCount ?? 0;
        public int BoardingCount => _brain?.BoardingCount ?? 0;
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
        public void SetSuspended(bool suspended, bool hide = true) => _brain?.SetSuspended(suspended, hide);
        public void PreserveGroundPose(Vector3 position, Quaternion rotation) => _brain?.PreserveGroundPose(position, rotation);
        public void ManagedTick(float deltaTime) => _brain?.ManagedTick(deltaTime);
        public void CancelActions() => _brain?.Dispose();
        protected override void OnDestroy() { CancelActions(); base.OnDestroy(); }
    }
}
