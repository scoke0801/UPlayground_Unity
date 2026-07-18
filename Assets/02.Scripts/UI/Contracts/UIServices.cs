using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public interface IUIRefreshable
    {
        void Refresh();
    }

    public interface IUIPartyService : IGameService
    {
        event Action<PlayerActor> OnSwapCompleted;
        event Action<CharacterActorType> OnCharacterUnlocked;
        event Action OnRosterChanged;
        event Action OnBattleOrderChanged;
        event Action<CharacterActorType> OnPartyProgressionChanged;
        event Action<CharacterActorType, long, long> OnExpChanged;
        event Action<CharacterActorType, int> OnLevelUp;
        event Action<CharacterActorType, int> OnGrowthPointsChanged;
        event Action<CharacterActorType, GrowthUnlockMilestone> OnGrowthUnlock;
        event Action<CharacterActorType, float, float> OnPartySkillGaugeChanged;
        event Action<CharacterActorType, float, float> OnSwapCooldownChanged;
        event Action OnPartyHealthRefreshed;

        PlayerActor ActiveCharacter { get; }
        CharacterActorType ActiveCharacterType { get; }
        int ActiveIndex { get; }
        int MaxBattleSize { get; }
        IReadOnlyList<CharacterActorType> Roster { get; }
        IReadOnlyList<CharacterActorType> BattleOrder { get; }
        bool IsSwapOnCooldown { get; }
        float SwapCooldownDuration { get; }
        PartyMemberDataSO PartyMemberDataSO { get; }

        int GetLevel(CharacterActorType type);
        long GetExp(CharacterActorType type);
        long GetRequiredExp(CharacterActorType type);
        int GetGrowthPoints(CharacterActorType type);
        int GetGrowthRank(CharacterActorType type, GrowthAttributeType attribute);
        int GetEffectiveGrowthRank(CharacterActorType type, GrowthAttributeType attribute);
        PartyMemberGrowthSO GetGrowthData(CharacterActorType type);
        PartyCombatPowerResult GetEffectiveCombatPower(CharacterActorType type);
        long GetPartyCombatPower(IReadOnlyList<CharacterActorType> order = null);
        float GetSwapCooldownRemaining(CharacterActorType type);
        bool TryInvestGrowthPoint(CharacterActorType type, GrowthAttributeType attribute);
        bool CanSwapTo(int targetIndex);
        bool RequestSwapTo(int targetIndex);
        bool AddToBattle(CharacterActorType type);
        bool RemoveFromBattle(CharacterActorType type);
        bool ReplaceBattleSlot(int slotIndex, CharacterActorType type);
        bool SetBattleOrder(IReadOnlyList<CharacterActorType> newOrder);
        void PrepareNewGameStartingCharacter(CharacterActorType type);
    }

    public interface IUIActorRegistryService : IGameService
    {
        event Action<GameActor> OnActorRegistered;
        event Action<GameActor> OnActorUnregistered;
        PlayerActor Player { get; }
        IReadOnlyList<GameActor> AllActors { get; }
        IActorInteractionService InteractionHandler { get; }
    }

    public interface IUIDialogueService : IGameService
    {
        event Action<DialogueNodeSO> OnMainNodeEnter;
        event Action<DialogueNodeSO> OnMonologueNodeEnter;
        event Action<DialogueNodeSO> OnSystemNodeEnter;
        event Action<List<ChoiceData>> OnChoicePresented;
        event Action OnDialogueEnd;
        SpeakerColorTableSO ColorTable { get; }
        void Advance(DialogueChannel channel = DialogueChannel.Main);
        void SelectChoice(int index);
    }

    public interface IUIQuestService : IGameService
    {
        bool IsDBLoaded { get; }
        bool IsQuestTrackingSuppressed { get; }
        IEnumerable<QuestRuntimeData> GetActiveQuests();
        QuestRuntimeData GetActiveQuestRuntime(string questId);
        QuestRuntimeData GetTrackedQuestRuntime();
        QuestSO GetQuestData(string questId);
        List<QuestSO> GetAvailableQuests();
        List<QuestSO> GetCompletedQuests();
        List<QuestSO> GetFailedQuests();
        bool IsQuestTracked(string questId);
        bool TrackQuest(string questId);
        bool UntrackQuest();
        bool CompleteQuest(string questId);
        bool AbandonQuest(string questId);
    }

    public interface IUIInventoryService : IGameService
    {
        event Action OnInventoryChanged;
        event Action OnPartyEquipmentChanged;
        Dictionary<int, ItemInstance> ItemDict { get; }
        int Gold { get; set; }
        int MaxSlots { get; }
        float MaxWeight { get; }
        int GetItemCount(int itemId);
        bool TryGetConsumableCooldown(int itemId, out float remaining, out float duration);
        float GetItemWeight(int itemId);
        float GetTotalWeight();
        ItemInstance GetItem(int itemId);
        ItemInstance GetInventoryItemBySlotKey(int inventorySlotKey);
        bool HasItem(int itemId);
        bool RemoveItem(int itemId, int count);
        List<CharacterActorType> GetEquippingCharacters(int inventorySlotKey);
        int GetEquippedItem(CharacterActorType type, EquipPosition slot);
        InventoryActionResult TryUnequipItem(CharacterActorType type, EquipPosition slot);
        bool CanEquipItem(CharacterActorType type, int itemId);
        bool CanEquipItem(CharacterActorType type, EquipmentSO equipment);
        bool CanEquipItem(CharacterActorType type, EquipmentSO equipment, EquipPosition slot);
        InventoryActionResult TryEquipItem(int itemId);
        InventoryActionResult TryEquipItem(CharacterActorType type, int itemId);
        InventoryActionResult TryEquipInventorySlot(CharacterActorType type, int inventorySlotKey);
        InventoryActionResult TryEquipInventorySlot(
            CharacterActorType type,
            int inventorySlotKey,
            EquipPosition targetSlot);
        InventoryActionResult TryUseItem(int itemId, int count = 1);
        InventoryActionResult TryDropItem(int itemId, int count = 1);
    }

    public interface IUIRecipeService : IGameService
    {
        event Action<int> OnRecipeUnlocked;
        event Action<int> OnCraftingStarted;
        event Action<int, int> OnCraftingCompleted;
        event Action OnCraftingCancelled;
        bool IsDBLoaded { get; }
        bool CanCraft(int recipeId, int quantity = 1);
        CraftAvailabilityReason GetCraftAvailabilityReason(int recipeId, int quantity = 1);
        bool TryStartCrafting(int recipeId, int quantity = 1);
        void CancelCrafting();
        RecipeData GetRecipeData(int recipeId);
        List<IngredientData> GetIngredients(int recipeId);
        List<IngredientData> GetEffectiveIngredients(int recipeId, int quantity = 1);
        float GetCraftingProgress();
        bool IsCrafting();
        List<int> GetUnlockedRecipeIDs();
        int GetMaxCraftableQuantity(int recipeId);
        Dictionary<int, bool> GetIngredientAvailability(int recipeId, int quantity = 1);
    }

    public interface IUISaveService : IGameService
    {
        void SaveGame(int slot = 0);
        void ResetForNewGame();
        bool LoadGameToScene(int slot = 0);
        bool HasSaveFile(int slot = 0);
        void DeleteSaveFile(int slot = 0);
        Sprite GetSlotThumbnail(int slot);
        SaveSlotInfo GetSaveSlotInfo(int slot);
        List<int> GetSlotIndicesForMenu(bool includeNextEmptySlot);
        int GetMostRecentSlot();
    }

    public interface IUISceneService : IGameService
    {
        string CurrentSceneType { get; }
        string CurrentMapID { get; }
        void LoadScene(string sceneName);
        void LoadScene(string sceneName, string arrivalId);
        void LoadScene(string sceneName, Vector3 arrivalPosition);
    }

    public interface IUICycleRunService : IGameService
    {
        event Action<CycleRunState> OnPhaseChanged;
        event Action<CycleBossPlacement> OnBossDiscovered;
        CycleRunState Current { get; }
        bool IsActive { get; }
        void RequestStartNewCycleOnNextWorld(int? requestedSeed = null);
    }

    public interface IUISettingsService : ISettingsService
    {
        IReadOnlyList<string> ResolutionOptions { get; }
        IReadOnlyList<string> QualityOptions { get; }
        int GetCurrentResolutionOptionIndex();
        void SetResolutionOption(int index);
        void ApplyCurrentSettings(AudioMixer mixerOverride = null);
    }

    public interface IUIRuntimeService : IGameService
    {
        GameObject ShowUI(GameObject uiPrefab, CanvasLayer layer, string uiName = null);
        GameObject ShowUI(string uiKey, CanvasLayer? layer = null);
        GameObject ShowUI(UIKeyType uiKey, CanvasLayer? layer = null);
        GameObject GetUIPrefabEntry(string uiKey);
        void HideUI(string uiName);
        void HideUI(UIKeyType uiKey);
        void HideAllUI();
        GameObject GetActiveUI(string uiName);
        GameObject GetActiveUI(UIKeyType uiKey);
        T GetUI<T>(string uiName) where T : UI_Base;
        T GetUI<T>(UIKeyType uiKey) where T : UI_Base;
        T GetUI<T>() where T : UI_Base;
    }

    public static class UISvc
    {
        public static IUIPartyService Party => Services.Get<IUIPartyService>();
        public static IUIActorRegistryService Actors => Services.Get<IUIActorRegistryService>();
        public static IUIDialogueService Dialogue => Services.Get<IUIDialogueService>();
        public static IUIQuestService Quest => Services.Get<IUIQuestService>();
        public static IUIInventoryService Inventory => Services.Get<IUIInventoryService>();
        public static IUIRecipeService Recipe => Services.Get<IUIRecipeService>();
        public static IUISaveService Save => Services.Get<IUISaveService>();
        public static IUISceneService Scene => Services.Get<IUISceneService>();
        public static IUICycleRunService Cycle => Services.Get<IUICycleRunService>();
        public static IUISettingsService Settings => Services.Get<IUISettingsService>();
        public static IUIRuntimeService UI => Services.Get<IUIRuntimeService>();
    }
}
