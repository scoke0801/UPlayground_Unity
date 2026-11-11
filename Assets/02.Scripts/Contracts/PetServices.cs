using System;
using UPlayGround.Data.Pet;

namespace UPlayGround.Manager
{
    public enum PetRuntimeState { Unavailable, Loading, Ready, Suspended, Failed }

    /// <summary>파티 세션이 소유하는 펫 선택과 중첩 중단 계약.</summary>
    public interface IPetService : IGameService
    {
        event Action Changed;
        PetCatalogSO Catalog { get; }
        PetRuntimeState State { get; }
        string FailureReason { get; }
        PetSaveData GetSnapshot();
        bool TrySelect(string petId);
        bool TrySetAppearance(string appearanceId);
        bool CanEdit(out string reason);
        bool TrySetModel(string modelId);
        bool TrySetSupport(string skillId);
        bool TrySetAutoPickup(bool enabled);
        PetUpgradeResult TryUpgrade(PetUpgradeRequest request);
        bool HasUnsavedChanges { get; }
        bool RetrySave();
        int GetSkillLevel(string skillId);
        float PickupInterval { get; }
        float PickupRadius { get; }
        float LootLeash { get; }
        bool IsAutoPickupEnabled { get; }
        void SetAccompanying(bool accompanying);
        IDisposable Suspend(object owner);
    }
    public enum PetUpgradeResult { Rejected, CommittedAndSaved, CommittedUnsaved }
}
