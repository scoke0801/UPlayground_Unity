using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Config;
using UPlayGround.Data.Codex;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Data.Sound;
using UPlayGround.Data.Stat;
using UPlayGround.Dialogue;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.Projectile;
using UPlayGround.Data.Reward;

namespace UPlayGround.Manager
{
    public enum RewardExperienceRecipient
    {
        BattleParty = 0,
        Character,
    }

    /// <summary>보상 경험치를 출전 파티 또는 지정 캐릭터 중 어디에 지급할지 나타낸다.</summary>
    public readonly struct RewardGrantTarget
    {
        public RewardExperienceRecipient ExperienceRecipient { get; }
        public CharacterActorType CharacterType { get; }

        private RewardGrantTarget(
            RewardExperienceRecipient experienceRecipient,
            CharacterActorType characterType)
        {
            ExperienceRecipient = experienceRecipient;
            CharacterType = characterType;
        }

        public static RewardGrantTarget BattleParty =>
            new(RewardExperienceRecipient.BattleParty, CharacterActorType.None);

        public static RewardGrantTarget Character(CharacterActorType characterType) =>
            new(RewardExperienceRecipient.Character, characterType);
    }

    public enum RewardGrantResult
    {
        Success = 0,
        InvalidData,
        ServiceUnavailable,
        InvalidRecipient,
        RecipientCannotGainExperience,
        InvalidItem,
        CapacityExceeded,
        ApplyFailed,
    }

    /// <summary>성공한 보상 지급 결과를 후속 피드백 시스템에 전달한다.</summary>
    public sealed class RewardGrantReceipt
    {
        public RewardGrantTarget Target { get; }
        public int Gold { get; }
        public long Experience { get; }
        public IReadOnlyList<ItemRewardData> Items { get; }

        public RewardGrantReceipt(RewardData reward, RewardGrantTarget target)
        {
            Target = target;
            Gold = reward?.gold ?? 0;
            Experience = reward?.exp ?? 0;

            if (reward?.items == null || reward.items.Count == 0)
            {
                Items = Array.Empty<ItemRewardData>();
                return;
            }

            var items = new ItemRewardData[reward.items.Count];
            for (int i = 0; i < reward.items.Count; i++)
            {
                ItemRewardData source = reward.items[i];
                items[i] = new ItemRewardData
                {
                    itemId = source.itemId,
                    count = source.count,
                };
            }

            Items = items;
        }
    }

    public interface IRewardService : IGameService
    {
        event Action<RewardGrantReceipt> OnRewardGranted;
        RewardGrantResult CanGrant(RewardData reward, RewardGrantTarget target);
        RewardGrantResult TryGrant(RewardData reward, RewardGrantTarget target);
    }

    public enum CharacterUnlockResult
    {
        AddedToBattle,
        PreparingBattle,
        AddedToRoster,
        AlreadyOwned,
        InvalidCharacter,
        ServiceNotReady,
        MissingModel,
    }

    public interface IProjectileService : IGameService
    {
        int CountActive { get; }
        int CountAll { get; }
        int CountInactive { get; }
        void Spawn(ProjectileSpawnRequest request);
        bool TryReflect(GameObject projectileObject, GameObject newOwner, Vector3 direction);
    }

    public interface IInputService : IGameService
    {
        event Action<ActiveInputDevice> OnActiveDeviceChanged;
        event Action OnBindingsChanged;

        /// <summary>
        /// 바인딩 구조가 바뀌어 InputActionState가 재생성됐다.
        /// InputAction/InputActionReference를 캐시하는 쪽은 여기서 다시 붙어야 한다.
        /// </summary>
        event Action OnBindingStructureChanged;
        event Action<InputRebindCaptureState> OnRebindCaptureChanged;
        InputLayer CurrentLayer { get; }
        InputBuffer InputBuffer { get; }
        ActiveInputDevice ActiveDevice { get; }
        GamepadBrand GamepadBrand { get; }
        bool IsPlayerActionInputSuppressed { get; }
        bool IsRebindCaptureActive { get; }
        InputAction GetAction(string mapName, string actionName);
        bool GetAction(string mapName, string actionName, out InputAction action);
        IReadOnlyList<InputBindingDescriptor> GetBindingDescriptors(InputBindingDeviceGroup deviceGroup);
        string CaptureBindingProfileSnapshot();
        bool RestoreBindingProfileSnapshot(string json);
        IDisposable BeginBindingProfileUpdate();
        void SaveBindingProfile(bool flushPlayerPrefs = true);
        bool TryApplyBinding(
            InputRebindCaptureResult capture,
            bool replaceConflict,
            out InputBindingConflictInfo conflict);
        bool ClearBinding(InputBindingTarget target);
        void ResetBinding(InputBindingTarget target);
        void ResetBindingsForAction(string mapName, string actionName);
        void ResetBindings(InputBindingDeviceGroup? deviceGroup = null);
        UniTask<InputRebindCaptureResult> CaptureBindingAsync(
            InputBindingTarget target,
            CancellationToken cancellationToken = default);
        void ShowCursor(bool isShow, bool isForce = false);
        void RefreshInputLayer();
        /// <summary>
        /// HUD 버튼처럼 물리 InputAction이 없는 명시적 UI 조작을 PlayerAction 1회 입력으로 전달한다.
        /// 현재 입력 레이어나 억제 상태가 플레이 입력을 허용하지 않으면 false를 반환한다.
        /// </summary>
        bool TryPerformPlayerAction(string actionName);
        void SuppressPlayerActionInputBriefly(float seconds = 0.05f, int frameCount = 1);
        void SetPlayerActionInputSuppressed(bool suppressed);
        /// <summary>실시간 전투 진동을 재생한다. 연속 요청의 합성·종료는 입력 서비스가 소유한다.</summary>
        void PlayCombatHaptic(float lowFrequency, float highFrequency, float duration);
        void StopHaptics();
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

        UniTask<IAssetLease<T>> AcquireGlobalAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }

    /// <summary>Addressable 에셋의 수명을 명시적으로 소유하는 해제 가능한 임대.</summary>
    public interface IAssetLease<out T> : IDisposable
        where T : UnityEngine.Object
    {
        T Asset { get; }
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

    /// <summary>Actor 구현을 노출하지 않고 전투 관계 판정에 필요한 소속만 제공한다.</summary>
    public interface ICombatAffiliationView
    {
        int CombatantRuntimeId { get; }
        string CombatFactionId { get; }
        CombatCreditOwner CombatCreditOwner { get; }
        bool IsCombatAvailable { get; }
    }

    public interface ICombatRelationService : IGameService
    {
        CombatRelation GetRelation(
            ICombatAffiliationView source,
            ICombatAffiliationView target);
        bool CanTarget(
            ICombatAffiliationView source,
            ICombatAffiliationView target);
        bool CanDamage(
            ICombatAffiliationView source,
            ICombatAffiliationView target,
            CombatTargetPolicy policy = CombatTargetPolicy.Hostile);
        CombatCreditOwner GetCreditOwner(ICombatAffiliationView actor);
        IDisposable OverrideAffiliation(
            ICombatAffiliationView actor,
            CombatFactionSO faction,
            CombatCreditOwner creditOwner);
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

    /// <summary>구체 Actor 구현을 노출하지 않고 스토리·콘텐츠가 런타임 액터를 생성하는 계약.</summary>
    public interface IActorSpawnService : IGameService
    {
        bool IsReady { get; }
        IWorldActor SpawnActor(string actorId, Vector3 position, Quaternion rotation);
    }

    public interface IPartyService : IGameService
    {
        CharacterActorType ActiveCharacterType { get; }
        CharacterActorType StoryProtagonistType { get; }
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
        bool UnlockCharacter(CharacterActorType type);
        bool IsCharacterUnlocked(CharacterActorType type);
        CharacterUnlockResult EnsureCharacterUnlocked(CharacterActorType type);
        void AwardBattleExp(long amount);
        bool AddExp(CharacterActorType type, long amount);
        bool IsMaxLevel(CharacterActorType type);
        void HealAllParty(bool reviveDowned);
        bool TrySwitchToNextAliveAfterActiveDeath();
        IReadOnlyDictionary<AttributeId, float> GetBaseStats(CharacterActorType type);
        PartyMemberGrowthSO GetGrowthData(CharacterActorType type);
        int GetLevel(CharacterActorType type);
        CombatElement GetCombatElement(CharacterActorType type);
        GameplayAbilitySO GetElementalImbueAbility(CharacterActorType type);
        IReadOnlyList<SkillStatModifierEntry> GetSkillStatModifiers(CharacterActorType type);
        float GetAbilityScalar(CharacterActorType type, string abilityId, AbilityScalarKind kind);
        bool IsAbilityUnlocked(CharacterActorType type, string abilityId);
        int GetUnlockedComboCount(
            CharacterActorType type,
            PlayerCombatAbilitySlot slot,
            IReadOnlyList<GameplayAbilitySO> abilities);
        float GetDodgeCooldownMultiplier(CharacterActorType type);
        PlayerCharacterDefinitionSO GetCharacterDefinition(
            CharacterActorType type);
    }

    public interface IPassiveModifierReader : IGameService
    {
        CharacterPassiveSetSO GetPassiveSet(CharacterActorType type);
        IReadOnlyList<PassiveAbilitySO> GetGrantedPassives(CharacterActorType type);
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
        int Gold { get; }
        bool TryAddGold(int amount);
        bool TrySpendGold(int amount);
        int GetItemCount(int itemId);
        bool CanAddItem(int itemId, int count);
        bool TryAddItem(int itemId, int count);
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
        List<ItemInstance> GetDropItemList(EnemyDropTableSO dropTable);
    }

    public interface IDialogueService : IGameService
    {
        event Action OnDialogueEnd;
        event Action<DialogueChannel> OnDialogueChannelEnd;

        /// <summary>
        /// 어느 채널이든 대화가 재생 중이거나 재생 대기 중인지 여부.
        /// 대화 연출 위에 다른 화면을 겹쳐 띄우면 안 되는 소비자가 사용한다.
        /// </summary>
        bool IsDialogueActive { get; }

        void StartDialogue(DialogueGraphSO graph);
        /// <param name="partnerOverride">
        /// 대화 상대를 인스턴스로 못박는다. 같은 actorId를 가진 개체가 월드에 여럿일 수 있으므로
        /// (씬 배치 중복) ID로 되찾으면 엉뚱한 개체가 상대로 잡힌다.
        /// </param>
        IDisposable TryStartDialogueTracked(
            DialogueGraphSO graph,
            Action onCompleted,
            IWorldActor partnerOverride = null,
            Action onCancelled = null);
    }

    public interface ISoundService : IGameService
    {
        void Play(string key, Vector3? position = null, float volumeScale = 1f);
        void PlaySfx(string key, Vector3 position, float volumeScale = 1f);

        /// <summary>
        /// 해당 key의 사운드 엔트리가 등록되어 있는지 조회한다(경고 로그 없음).
        /// 티어별 키가 아직 저작되지 않았을 때 상위 키로 폴백하려는 호출자를 위한 질의다.
        /// </summary>
        bool HasSound(string key);
        void PlayUi(string key, float volumeScale = 1f);
        void PlayClip(
            AudioClip clip,
            SoundBusType bus,
            Vector3? position = null,
            float volumeScale = 1f);
    }
}
