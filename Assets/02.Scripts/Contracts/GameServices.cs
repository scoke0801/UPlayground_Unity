using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Config;
using UPlayGround.Data.Codex;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Data.Sound;
using UPlayGround.Data.Stat;
using UPlayGround.Dialogue;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.CameraSystem;

namespace UPlayGround.Manager
{
    public interface IInputService : IGameService
    {
        event Action<ActiveInputDevice> OnActiveDeviceChanged;
        InputLayer CurrentLayer { get; }
        InputBuffer InputBuffer { get; }
        ActiveInputDevice ActiveDevice { get; }
        GamepadBrand GamepadBrand { get; }
        bool IsPlayerActionInputSuppressed { get; }
        InputAction GetAction(string mapName, string actionName);
        bool GetAction(string mapName, string actionName, out InputAction action);
        void ShowCursor(bool isShow, bool isForce = false);
        void RefreshInputLayer();
        void SuppressPlayerActionInputBriefly(float seconds = 0.05f, int frameCount = 1);
        void SetPlayerActionInputSuppressed(bool suppressed);
        void RegisterInputEvent(
            string mapName,
            string actionName,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled,
            Func<bool> checkFunc,
            Action cancelCallback,
            InputLayer inputLayer);
        void UnRegisterInputEvent(
            string mapName,
            string actionName,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled);
    }

    public interface IHitStopService : IGameService
    {
        bool IsHitStopping { get; }
        void Execute(float duration, float timeScale = 0.1f);
        void Stop();
    }

    public interface IVitalOrbService : IGameService
    {
        void TrySpawn(VitalOrbTrigger trigger, Vector3 spawnPosition);
        void TrySpawnByPolicy(
            VitalOrbTrigger trigger,
            Vector3 spawnPosition,
            float probability,
            int count,
            float healScale);
    }

    public interface ISettingsService : IGameService
    {
        SettingsData Data { get; }
        bool IsLoaded { get; }
    }

    public interface IGameTimeService : IGameService
    {
        event Action<int, float> OnGameMinuteChanged;
        bool IsPaused { get; }
        bool IsSlowed { get; }
        int CurrentDay { get; }
        float MinuteOfDay { get; }
        DayPeriod CurrentDayPeriod { get; }
        int Request(float scale);
        void UpdateRequestScale(int id, float scale);
        void Release(int id);
        void SetPause(bool pause);
        string FormatPlayTime();
    }

    public interface IElementRandomSeedService : IGameService
    {
        int NewGameElementSeed { get; }
    }

    public interface ICameraViewService : IGameService
    {
        CameraModeType CurrentCameraMode { get; }
        UnityEngine.Camera GetMainCamera();
    }

    public interface IAssetService : IGameService
    {
        UniTask<T> LoadGlobalAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;

        UniTask<T> LoadGlobalAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;

        UniTask<T> LoadSceneAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }

    public interface IWorldActor
    {
        string ActorId { get; }
        ActorType ActorType { get; }
        MonsterActorGrade Grade { get; }
        Transform Transform { get; }
        bool IsAlive { get; }
        bool TryGetSocket(ActorSocketType socketType, out Transform socket);
        void LockOn();
        void UnLockOn();
    }

    public interface IPlayerInputSuppressible
    {
        bool IsInputSuppressed { get; }
        void SetInputSuppressed(bool suppressed);
    }

    public interface IActorQueryService : IGameService
    {
        IWorldActor Player { get; }
        Transform PlayerTransform { get; }
        IEnumerable<IWorldActor> AllActors { get; }
        IWorldActor FindActor(string actorId);
    }

    public interface IPartyService : IGameService
    {
        CharacterActorType ActiveCharacterType { get; }
        bool SwapEvadeEnableHitStop { get; }
        float SwapEvadeHitStopDuration { get; }
        float SwapEvadeHitStopTimeScale { get; }
        CameraShakeIdType SwapEvadeCameraShakeKey { get; }
        string SwapEvadeFxKey { get; }
        ActorSocketType SwapEvadeFxSocket { get; }
        Vector3 SwapEvadeFxOffset { get; }
        bool SwapEvadeSpawnDodgeVitalOrb { get; }
        bool EnableResidualAttackOnSwap { get; }
        float ResidualAttackMaxLifetime { get; }
        float ResidualAttackMinVisibleLifetime { get; }
        float ResidualAttackFadeOutDuration { get; }
        Color ResidualAttackDissolveColor { get; }
        Texture ResidualAttackDissolveNoiseMask { get; }
        float ResidualAttackDissolveNoiseStrength { get; }
        Vector4 ResidualAttackDissolveNoiseScrollRotate { get; }
        bool ResidualAttackAllowHitStop { get; }
        bool ResidualAttackUseRootMotion { get; }
        float ResidualAttackRootMotionMaxDistance { get; }
        LayerMask ResidualAttackRootMotionBlocker { get; }
        int ResidualAttackMaxCount { get; }
        bool ResidualAttackReturnToSameCharacterRunner { get; }
        float ResidualAttackReturnPositionMaxAge { get; }
        float ResidualAttackFeedbackMinInterval { get; }
        float ResidualAttackHitStopDuration { get; }
        float ResidualAttackHitStopTimeScale { get; }
        bool ResidualAttackShowCharacterOnDamageFloater { get; }
        bool PreserveComboStatePerCharacter { get; }
        float ComboStateMaxCarryTime { get; }
        bool IsSkillUnlocked(CharacterActorType type, GrowthSkillType skillType);
        int GetUnlockedComboLength(CharacterActorType type, GrowthComboType comboType, int dataLength);
        bool UnlockCharacter(CharacterActorType type);
        void AwardBattleExp(long amount);
        void HealAllParty(bool reviveDowned);
        bool TrySwitchToNextAliveAfterActiveDeath();
        IReadOnlyDictionary<StatType, float> GetGrowthStats(CharacterActorType type);
        PartyMemberGrowthSO GetGrowthData(CharacterActorType type);
        int GetLevel(CharacterActorType type);
        CombatElement GetCombatElement(CharacterActorType type);
        GameplayAbilitySO GetElementalImbueAbility(CharacterActorType type);
        IReadOnlyDictionary<GrowthAttributeType, int> GetGrowthInvestments(CharacterActorType type);
    }

    public interface IPassiveModifierReader : IGameService
    {
        CharacterPassiveSetSO GetPassiveSet(CharacterActorType type);
        float GetActiveMultiplier(PassiveModifierType type);
        float GetActiveSkillCooldownMultiplier(PlayerSkillSlot slot);
        float GetCharacterMultiplier(
            CharacterActorType characterType,
            PassiveModifierType type);
        float GetBattlePartyMultiplier(PassiveModifierType type);
    }

    public interface IMonsterCodexReader : IGameService
    {
        float GetExpMultiplier(string actorId);
        float GetDamageDealtMultiplier(string actorId);
        float GetDamageTakenMultiplier(string actorId);
    }

    public interface IMonsterCodexService : IMonsterCodexReader
    {
        void RecordKill(string actorId, CombatElement element);
        float GetRecordRatio(string actorId);
        bool IsDiscovered(string actorId);
        CombatElement GetDiscoveredElement(string actorId);
        IReadOnlyList<MonsterCodexEntryView> GetAllEntries();
    }

    public interface IInventoryService : IGameService
    {
        int Gold { get; set; }
        void AddItem(int itemId, ItemInstance itemInstance);
        void SeedCharacterEquipmentIfAbsent(
            CharacterActorType type,
            IReadOnlyList<EquipmentSO> startItems);
        List<EquipmentSO> GetEquippedEquipment(CharacterActorType type);
        List<ItemInstance> GetEquippedItemInstances(CharacterActorType type);
    }

    public interface IItemService : IGameService
    {
        bool IsItemDBLoaded { get; }
        ItemSO GetItemData(int itemKey);
        ItemSO GetItemData(ItemIdType itemKey);
        List<ItemInstance> GetDropItemList(List<ItemDropList> itemDropList);
    }

    public interface IDialogueService : IGameService
    {
        event Action OnDialogueEnd;
        void StartDialogue(DialogueGraphSO graph);
    }

    public interface ISoundService : IGameService
    {
        void Play(string key, Vector3? position = null, float volumeScale = 1f);
        void PlaySfx(string key, Vector3 position, float volumeScale = 1f);
        void PlayUi(string key, float volumeScale = 1f);
        void PlayClip(
            AudioClip clip,
            SoundBusType bus,
            Vector3? position = null,
            float volumeScale = 1f);
    }
}
