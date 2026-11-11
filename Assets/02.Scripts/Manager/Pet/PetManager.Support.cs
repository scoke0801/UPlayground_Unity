using UPlayGround.Components.Pet;

namespace UPlayGround.Manager
{
    public sealed partial class PetManager
    {
        private PetSupportController _support;
        private void CaptureSupport() => _support?.Capture(_save);
        private void ClearSupportEffects() => _support?.ClearEffects(_save);
        private void TickSupport(PlayerActor player)
        {
            _support ??= new PetSupportController(Catalog.skills);
            _support.Bind(player, _save);
            _support.Tick(_actor, _save, GetSkillLevel(_save.selectedSupportId));
        }
    }
}
