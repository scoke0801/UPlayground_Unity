using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Manager
{
    public partial class PartyManager
    {
        private const string PlayerCharacterCatalogAddress =
            "PlayerCharacterCatalog";

        private sealed class ResidentPlayerModel
        {
            public string Address;
            public IAssetLease<GameObject> AssetLease;
            public GameObject Instance;
            public CharacterModelData ModelData;
        }

        private PlayerCharacterCatalogSO _characterCatalog;
        private readonly Dictionary<CharacterActorType, PlayerCharacterDefinitionSO>
            _characterDefinitions = new();
        private readonly Dictionary<CharacterActorType, IAssetLease<PlayerCharacterDefinitionSO>>
            _characterDefinitionLeases = new();
        private readonly Dictionary<CharacterActorType, UniTaskCompletionSource<PlayerCharacterDefinitionSO>>
            _definitionLoadSources = new();
        private readonly Dictionary<CharacterActorType, ResidentPlayerModel>
            _residentPlayerModels = new();
        private readonly SemaphoreSlim _modelResidencyGate = new(1, 1);
        private CancellationTokenSource _modelStreamingCancellation;
        private PlayerActor _preparingPlayer;
        private PlayerActor _preparedPlayer;
        private bool _isPlayerPreparationRunning;
        private bool _isPlayerPreparationReady;

        /// <summary>모델 로드 없이 캐릭터 게임플레이 정의를 조회한다.</summary>
        public PlayerCharacterDefinitionSO GetCharacterDefinition(
            CharacterActorType type)
        {
            _characterDefinitions.TryGetValue(type, out var definition);
            return definition;
        }

        private async UniTask LoadPartyConfigurationAsync(
            CancellationToken cancellationToken)
        {
            await LoadConfigSOAsync(cancellationToken);
            _characterCatalog =
                await AssetManager.Instance.LoadGlobalAsync<PlayerCharacterCatalogSO>(
                    PlayerCharacterCatalogAddress,
                    nameof(PartyManager),
                    cancellationToken);
        }

        private void BeginScenePlayerPreparation(PlayerActor explicitPlayer = null)
        {
            PlayerActor player = explicitPlayer != null
                ? explicitPlayer
                : ResolvePlayerActor();
            if (player == null)
                return;

            if (_preparedPlayer == player && _isPlayerPreparationReady)
                return;
            if (_preparingPlayer == player && _isPlayerPreparationRunning)
                return;

            CancelScenePlayerPreparation();
            _preparingPlayer = player;
            _preparedPlayer = null;
            _isPlayerPreparationRunning = true;
            _isPlayerPreparationReady = false;
            _modelStreamingCancellation = new CancellationTokenSource();
            PrepareScenePlayerAsync(player, _modelStreamingCancellation.Token)
                .Forget(HandlePlayerPreparationException);
        }

        private async UniTask PrepareScenePlayerAsync(
            PlayerActor player,
            CancellationToken cancellationToken)
        {
            try
            {
                var definitionTypes = new List<CharacterActorType>();
                var modelTypes = new List<CharacterActorType>();
                CollectPreparationTypes(definitionTypes, modelTypes);

                for (int i = 0; i < definitionTypes.Count; i++)
                    await EnsureCharacterDefinitionAsync(
                        definitionTypes[i], cancellationToken);

                for (int i = 0; i < modelTypes.Count; i++)
                    await EnsurePlayerModelResidentAsync(
                        player, modelTypes[i], cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                if (player == null || _preparingPlayer != player)
                    return;

                BindPreparedScenePlayer(player);
                _preparedPlayer = player;
                _isPlayerPreparationReady = true;
            }
            finally
            {
                if (_preparingPlayer == player)
                    _isPlayerPreparationRunning = false;
            }
        }

        private void BindPreparedScenePlayer(PlayerActor player)
        {
            UnsubscribeCombatEvents();
            _player = player;
            BuildPartyFromScene();
            if (_player == null || _battleOrder.Count == 0)
                return;

            InitializePartyStates();
            SubscribeCombatEvents();
            NotifyActivePlayerChanged();
            TryApplyPendingPartyLoad();
            ReconcileResidentPlayerModels();

            Debug.Log(
                $"[PartyManager] 파티 준비 완료: 보유 {_roster.Count}명 / " +
                $"출전 {_battleOrder.Count}/{_maxBattleSize}, 활성={ActiveCharacterType}");
        }

        private void CollectPreparationTypes(
            List<CharacterActorType> definitionTypes,
            List<CharacterActorType> modelTypes)
        {
            if (_pendingPartyLoad != null)
            {
                AddParsedCharacters(_pendingPartyLoad.roster, definitionTypes);
                AddParsedCharacters(_pendingPartyLoad.battleOrder, definitionTypes);
                AddParsedCharacters(_pendingPartyLoad.battleOrder, modelTypes);
                if (TryParseCharacter(
                        _pendingPartyLoad.storyProtagonistType,
                        out CharacterActorType protagonistType))
                {
                    AddUnique(definitionTypes, protagonistType);
                    AddUnique(modelTypes, protagonistType);
                }
                return;
            }

            if (_newGameStartingCharacter != CharacterActorType.None)
            {
                AddUnique(definitionTypes, _newGameStartingCharacter);
                AddUnique(modelTypes, _newGameStartingCharacter);
                return;
            }

            if (_hasRuntimePartyComposition && _battleOrder.Count > 0)
            {
                for (int i = 0; i < _roster.Count; i++)
                    AddUnique(definitionTypes, _roster[i]);
                for (int i = 0; i < _battleOrder.Count; i++)
                    AddUnique(modelTypes, _battleOrder[i]);
                AddUnique(modelTypes, _storyProtagonistType);
                return;
            }

            int modelLimit = Mathf.Max(
                1,
                _config != null ? _config.maxBattleSize : _maxBattleSize);
            for (int i = 0; i < (_characterCatalog?.entries?.Count ?? 0); i++)
            {
                PlayerCharacterCatalogSO.Entry entry =
                    _characterCatalog.entries[i];
                if (entry == null
                    || entry.characterType == CharacterActorType.None)
                {
                    continue;
                }

                AddUnique(definitionTypes, entry.characterType);
                if (modelTypes.Count < modelLimit)
                    AddUnique(modelTypes, entry.characterType);
            }
        }

        private async UniTask<PlayerCharacterDefinitionSO>
            EnsureCharacterDefinitionAsync(
                CharacterActorType type,
                CancellationToken cancellationToken)
        {
            if (type == CharacterActorType.None)
                return null;
            if (_characterDefinitions.TryGetValue(type, out var loaded))
                return loaded;
            if (_definitionLoadSources.TryGetValue(type, out var pending))
                return await pending.Task.AttachExternalCancellation(
                    cancellationToken);
            if (_characterCatalog == null
                || !_characterCatalog.TryGetDefinitionAddress(
                    type, out string address))
            {
                throw new InvalidOperationException(
                    $"플레이어 캐릭터 정의 주소가 없습니다: {type}");
            }

            var source =
                new UniTaskCompletionSource<PlayerCharacterDefinitionSO>();
            _definitionLoadSources.Add(type, source);
            try
            {
                IAssetLease<PlayerCharacterDefinitionSO> lease =
                    await Svc.Asset.AcquireGlobalAsync<PlayerCharacterDefinitionSO>(
                        address,
                        $"{nameof(PartyManager)}.Definition.{type}",
                        cancellationToken);
                PlayerCharacterDefinitionSO definition = lease.Asset;
                if (definition.characterType != type)
                {
                    lease.Dispose();
                    throw new InvalidOperationException(
                        $"플레이어 캐릭터 정의 타입 불일치: 요청={type}, " +
                        $"에셋={definition.characterType}, 주소={address}");
                }

                _characterDefinitionLeases.Add(type, lease);
                _characterDefinitions.Add(type, definition);
                Svc.Inventory?.SeedCharacterEquipmentIfAbsent(
                    type,
                    definition.startingEquipment);
                source.TrySetResult(definition);
                return definition;
            }
            catch (Exception exception)
            {
                source.TrySetException(exception);
                throw;
            }
            finally
            {
                _definitionLoadSources.Remove(type);
            }
        }

        private async UniTask EnsurePlayerModelResidentAsync(
            PlayerActor player,
            CharacterActorType type,
            CancellationToken cancellationToken)
        {
            if (player == null || type == CharacterActorType.None)
                return;

            await _modelResidencyGate.WaitAsync(cancellationToken);
            try
            {
                PlayerSwapBehaviour swap =
                    player.GetComponent<PlayerSwapBehaviour>();
                if (swap == null)
                    throw new InvalidOperationException(
                        "PlayerActor에 PlayerSwapBehaviour가 없습니다.");
                if (swap.GetModelData(type) != null)
                    return;

                PlayerCharacterDefinitionSO definition =
                    await EnsureCharacterDefinitionAsync(type, cancellationToken);
                string address = definition.modelAddress?.Trim();
                if (string.IsNullOrWhiteSpace(address))
                    throw new InvalidOperationException(
                        $"플레이어 모델 주소가 없습니다: {type}");

                if (_residentPlayerModels.TryGetValue(
                        type, out ResidentPlayerModel resident))
                {
                    if (resident.Address == address
                        && resident.AssetLease?.Asset != null)
                    {
                        CreateResidentModelInstance(
                            player, swap, type, definition, resident);
                        return;
                    }

                    ReleaseResidentPlayerModel(type, resident);
                }

                IAssetLease<GameObject> assetLease =
                    await Svc.Asset.AcquireGlobalAsync<GameObject>(
                        address,
                        $"{nameof(PartyManager)}.Model.{type}",
                        cancellationToken);
                resident = new ResidentPlayerModel
                {
                    Address = address,
                    AssetLease = assetLease,
                };
                _residentPlayerModels[type] = resident;
                CreateResidentModelInstance(
                    player, swap, type, definition, resident);
            }
            finally
            {
                _modelResidencyGate.Release();
            }
        }

        private static void CreateResidentModelInstance(
            PlayerActor player,
            PlayerSwapBehaviour swap,
            CharacterActorType type,
            PlayerCharacterDefinitionSO definition,
            ResidentPlayerModel resident)
        {
            if (resident.Instance != null)
                UnityEngine.Object.Destroy(resident.Instance);

            GameObject instance = UnityEngine.Object.Instantiate(
                resident.AssetLease.Asset,
                swap.ModelRoot,
                false);
            instance.name = $"PlayerModel_{type}";
            instance.SetActive(false);
            CharacterModelData model =
                instance.GetComponent<CharacterModelData>()
                ?? instance.GetComponentInChildren<CharacterModelData>(true);
            if (model == null || model.characterType != type)
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"로드한 모델의 CharacterModelData가 잘못되었습니다: {type}");
            }

            model.AssignDefinition(definition);
            if (!swap.RegisterModel(model))
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"플레이어 모델 등록에 실패했습니다: {type}");
            }

            resident.Instance = instance;
            resident.ModelData = model;
        }

        private bool ArePlayerModelsResident(
            IReadOnlyList<CharacterActorType> types)
        {
            PlayerSwapBehaviour swap =
                _player != null
                    ? _player.GetComponent<PlayerSwapBehaviour>()
                    : null;
            if (swap == null || types == null)
                return false;

            for (int i = 0; i < types.Count; i++)
                if (types[i] != CharacterActorType.None
                    && swap.GetModelData(types[i]) == null)
                    return false;
            return true;
        }

        private async UniTask<bool> EnsureBattleModelsReadyAsync(
            IReadOnlyList<CharacterActorType> types,
            CancellationToken cancellationToken = default)
        {
            if (_player == null || types == null || types.Count == 0)
                return false;

            for (int i = 0; i < types.Count; i++)
                await EnsurePlayerModelResidentAsync(
                    _player, types[i], cancellationToken);
            return ArePlayerModelsResident(types);
        }

        /// <summary>모델 준비가 끝난 뒤 출전 명단 전체를 원자적으로 교체한다.</summary>
        public async UniTask<bool> SetBattleOrderAsync(
            IReadOnlyList<CharacterActorType> newOrder,
            CancellationToken cancellationToken = default)
        {
            if (!await EnsureBattleModelsReadyAsync(
                    newOrder, cancellationToken))
                return false;
            bool changed = SetBattleOrder(newOrder);
            if (changed)
                ReconcileResidentPlayerModels();
            return changed;
        }

        /// <summary>모델 준비가 끝난 뒤 캐릭터를 출전 빈 슬롯에 추가한다.</summary>
        public async UniTask<bool> AddToBattleAsync(
            CharacterActorType type,
            CancellationToken cancellationToken = default)
        {
            var requested = new[] { type };
            if (!await EnsureBattleModelsReadyAsync(
                    requested, cancellationToken))
                return false;
            return AddToBattle(type);
        }

        /// <summary>모델 준비가 끝난 뒤 지정 출전 슬롯을 교체한다.</summary>
        public async UniTask<bool> ReplaceBattleSlotAsync(
            int slotIndex,
            CharacterActorType type,
            CancellationToken cancellationToken = default)
        {
            var requested = new[] { type };
            if (!await EnsureBattleModelsReadyAsync(
                    requested, cancellationToken))
                return false;
            bool changed = ReplaceBattleSlot(slotIndex, type);
            if (changed)
                ReconcileResidentPlayerModels();
            return changed;
        }

        private async UniTask CompleteCharacterUnlockAsync(
            CharacterActorType type,
            bool prepareForBattle)
        {
            CancellationToken cancellationToken =
                _modelStreamingCancellation?.Token ?? CancellationToken.None;
            await EnsureCharacterDefinitionAsync(type, cancellationToken);
            if (!prepareForBattle
                || _player == null
                || !_roster.Contains(type)
                || _battleOrder.Contains(type)
                || _battleOrder.Count >= _maxBattleSize)
            {
                return;
            }

            await EnsurePlayerModelResidentAsync(
                _player, type, cancellationToken);
            if (_rosterService.AddToBattle(type, _maxBattleSize))
            {
                OnBattleOrderChanged?.Invoke();
                Debug.Log(
                    $"[PartyManager] {type} 모델 준비 및 출전 자동 편입 완료 " +
                    $"(BattleOrder {_battleOrder.Count}/{_maxBattleSize})");
            }
        }

        private void ReconcileResidentPlayerModels()
        {
            var keep = new HashSet<CharacterActorType>(_battleOrder);
            AddUnique(keep, _storyProtagonistType);
            AddUnique(keep, ActiveCharacterType);
            var release = new List<CharacterActorType>();
            foreach (var pair in _residentPlayerModels)
                if (!keep.Contains(pair.Key))
                    release.Add(pair.Key);
            for (int i = 0; i < release.Count; i++)
                ReleaseResidentPlayerModel(
                    release[i], _residentPlayerModels[release[i]]);
        }

        private void ReleaseResidentPlayerModel(
            CharacterActorType type,
            ResidentPlayerModel resident)
        {
            if (resident == null)
                return;
            if (resident.ModelData != null)
            {
                PlayerSwapBehaviour swap = resident.ModelData
                    .GetComponentInParent<PlayerSwapBehaviour>();
                swap?.UnregisterModel(resident.ModelData);
            }
            IAssetLease<GameObject> assetLease = resident.AssetLease;
            if (resident.Instance != null)
            {
                resident.Instance.SetActive(false);
                UnityEngine.Object.Destroy(resident.Instance);
                ReleaseModelLeaseAfterDestroyAsync(assetLease).Forget();
            }
            else
            {
                assetLease?.Dispose();
            }
            _residentPlayerModels.Remove(type);
        }

        private static async UniTaskVoid ReleaseModelLeaseAfterDestroyAsync(
            IAssetLease<GameObject> assetLease)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            assetLease?.Dispose();
        }

        private void CancelScenePlayerPreparation()
        {
            _modelStreamingCancellation?.Cancel();
            _modelStreamingCancellation?.Dispose();
            _modelStreamingCancellation = null;
            _isPlayerPreparationRunning = false;
            _isPlayerPreparationReady = false;
            _preparingPlayer = null;
        }

        private void DisposePlayerCharacterStreaming()
        {
            CancelScenePlayerPreparation();
            var residents = new List<KeyValuePair<CharacterActorType, ResidentPlayerModel>>(
                _residentPlayerModels);
            for (int i = 0; i < residents.Count; i++)
                ReleaseResidentPlayerModel(
                    residents[i].Key, residents[i].Value);

            foreach (IAssetLease<PlayerCharacterDefinitionSO> lease in
                     _characterDefinitionLeases.Values)
                lease?.Dispose();
            _characterDefinitionLeases.Clear();
            _characterDefinitions.Clear();
            _definitionLoadSources.Clear();
            _characterCatalog = null;
            _preparedPlayer = null;
        }

        private void ResetPlayerCharacterStreamingForNewGame()
        {
            CancelScenePlayerPreparation();
            var residents = new List<KeyValuePair<CharacterActorType, ResidentPlayerModel>>(
                _residentPlayerModels);
            for (int i = 0; i < residents.Count; i++)
                ReleaseResidentPlayerModel(
                    residents[i].Key, residents[i].Value);

            foreach (IAssetLease<PlayerCharacterDefinitionSO> lease in
                     _characterDefinitionLeases.Values)
                lease?.Dispose();
            _characterDefinitionLeases.Clear();
            _characterDefinitions.Clear();
            _definitionLoadSources.Clear();
            _preparedPlayer = null;
        }

        private void HandlePlayerPreparationException(Exception exception)
        {
            if (exception is OperationCanceledException)
                return;
            _isPlayerPreparationReady = false;
            Debug.LogException(exception);
        }

        private static void AddParsedCharacters(
            IReadOnlyList<string> source,
            ICollection<CharacterActorType> target)
        {
            if (source == null)
                return;
            for (int i = 0; i < source.Count; i++)
                if (TryParseCharacter(
                        source[i], out CharacterActorType type))
                    AddUnique(target, type);
        }

        private static void AddUnique(
            ICollection<CharacterActorType> target,
            CharacterActorType type)
        {
            if (type != CharacterActorType.None && !target.Contains(type))
                target.Add(type);
        }
    }
}
