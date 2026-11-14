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
            _support ??= new PetSupportController(Catalog.skills, CanReactToSupport);
            _support.SetPet(_actor);
            _support.Bind(player, _save);
            _support.Tick(_actor, _save);
        }
        private bool CanReactToSupport() => _save.isAccompanying && _suspensions.Count == 0
            && !SceneManager.Instance.IsLoading && !SaveManager.Instance.IsPreparingSceneLoad
            && Svc.Dialogue?.IsDialogueActive != true && Svc.Input?.IsPlayerActionInputSuppressed != true
            && !(Svc.Input?.CurrentLayer > UPlayGround.InputDefine.InputLayer.Level_0);
        public bool CanSupport => _support?.IsAvailable == true;
        public int SupportActivationVersion => _support?.ActivationVersion ?? 0;
        public string SelectedSupportId => _save.SelectedSupportId;
    }
}
