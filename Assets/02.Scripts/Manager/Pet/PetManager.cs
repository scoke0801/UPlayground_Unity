using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Save;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>펫 보유 상태와 씬·플레이어 준비를 조립한다. 이동 판단은 액터가 소유한다.</summary>
    public sealed class PetManager : BaseManager<PetManager>, IManager, IAsyncInitializableManager,
        IUpdatableManager, ISaveable, IPetService
    {
        public const string CatalogKey = "PetCatalog";
        private PetSaveData _save = new();
        private readonly HashSet<object> _suspensions = new();
        private PetActor _actor;
        private PlayerActor _owner;
        private bool _hasSave;
        private bool _ready;
        private float _nextDecision;
        private string _sceneType;
        public event Action Changed;
        public PetCatalogSO Catalog { get; private set; }
        public PetRuntimeState State { get; private set; } = PetRuntimeState.Unavailable;
        public string FailureReason { get; private set; }

        public void Init() { SaveManager.Instance.RegisterSaveable(this); SetState(PetRuntimeState.Loading); }
        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                Catalog = await Svc.Asset.LoadGlobalAsync<PetCatalogSO>(CatalogKey, cancellationToken);
                if (Catalog == null) throw new InvalidOperationException("펫 카탈로그가 없습니다.");
                if (!_hasSave) SeedNewGame();
                SetState(PetRuntimeState.Suspended);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { SetState(PetRuntimeState.Failed, exception.Message); }
        }
        public void AfterInit() { _ready = true; _sceneType = SceneManager.Instance.CurrentSceneType; Reconcile(); }
        public void OnUpdate()
        {
            if (Time.unscaledTime < _nextDecision) return;
            _nextDecision = Time.unscaledTime + 0.25f;
            Reconcile();
        }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType)
        {
            Despawn(); _sceneType = sceneType;
            if (Catalog != null) SetState(PetRuntimeState.Suspended);
            Reconcile();
        }
        public PetSaveData GetSnapshot() => PetSaveRules.Normalize(_save);

        public bool TrySelect(string petId)
        {
            if (Catalog?.Find(petId) == null || FindOwned(petId) == null) return false;
            if (_save.selectedPetId != petId) Despawn();
            _save.selectedPetId = petId; _save.isAccompanying = true;
            SetState(PetRuntimeState.Suspended); Changed?.Invoke(); Reconcile();
            return true;
        }
        public bool TrySetAppearance(string appearanceId)
        {
            var owned = FindOwned(_save.selectedPetId);
            var definition = Catalog?.Find(_save.selectedPetId);
            if (owned == null || definition == null || !owned.unlockedAppearances.Contains(appearanceId)) return false;
            var appearance = definition.FindAppearance(appearanceId);
            if (appearance?.appearanceId != appearanceId) return false;
            owned.appearanceId = appearanceId;
            _actor?.SetAppearance(definition, appearanceId); Changed?.Invoke(); return true;
        }
        public void SetAccompanying(bool accompanying)
        {
            _save.isAccompanying = accompanying && FindOwned(_save.selectedPetId) != null;
            if (!_save.isAccompanying) Despawn();
            else SetState(PetRuntimeState.Suspended);
            Changed?.Invoke(); Reconcile();
        }
        public IDisposable Suspend(object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var key = new object(); _suspensions.Add(key); Reconcile();
            return new ActorRuntimeLease(() => { _suspensions.Remove(key); Reconcile(); });
        }

        private void Reconcile()
        {
            if (!_ready || Catalog == null) return;
            var definition = Catalog.Find(_save.selectedPetId);
            if (!_save.isAccompanying || FindOwned(_save.selectedPetId) == null)
            { Despawn(); SetState(PetRuntimeState.Unavailable); return; }
            if (definition == null || definition.actor == null || definition.followProfile == null)
            { Despawn(); SetState(PetRuntimeState.Failed, "선택한 펫의 콘텐츠를 찾을 수 없습니다."); return; }
            var player = ActorSvc.Objects?.Player;
            bool gameplay = _sceneType == SceneType.GamePlay || _sceneType == SceneType.Test;
            bool suspended = !gameplay || player == null || !player.IsAlive()
                || _suspensions.Count > 0 || SceneManager.Instance.IsLoading
                || SaveManager.Instance.IsPreparingSceneLoad || Svc.Dialogue?.IsDialogueActive == true
                || Svc.Input?.IsPlayerActionInputSuppressed == true
                || Svc.Input?.CurrentLayer > InputLayer.Level_0 || Time.timeScale <= 0f;
            if (_actor != null && _owner != player) Despawn();
            if (suspended)
            { _actor?.SetSuspended(true); SetState(PetRuntimeState.Suspended); return; }
            if (_actor == null)
            {
                if (State == PetRuntimeState.Failed) return;
                if (player.ActorController?.Motor == null || !player.ActorController.Motor.GroundingStatus.IsStableOnGround)
                { SetState(PetRuntimeState.Suspended); return; }
                var spawned = ActorSpawnManager.Instance.SpawnActor(definition.actor.actorId,
                    player.transform.position, player.transform.rotation);
                if (spawned is not PetActor pet)
                {
                    if (spawned != null) Destroy(spawned.gameObject);
                    SetState(PetRuntimeState.Failed, "펫 액터를 생성하지 못했습니다."); return;
                }
                _actor = pet; _owner = player;
                pet.Configure(definition, player, FindOwned(definition.petId).appearanceId);
                AgentTickManager.Instance.Register(pet, pet);
            }
            _actor.SetSuspended(false);
            SetState(_actor.HasSafeGround ? PetRuntimeState.Ready : PetRuntimeState.Suspended);
        }

        private void Despawn()
        {
            if (_actor != null)
            {
                _actor.CancelActions();
                AgentTickManager.Instance.Unregister(_actor, _actor);
                Destroy(_actor.gameObject);
            }
            _actor = null; _owner = null;
        }
        private PetOwnedEntry FindOwned(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _save.owned.Count; i++) if (_save.owned[i].petId == id) return _save.owned[i];
            return null;
        }
        private void SeedNewGame()
        {
            _save = new();
            if (Catalog == null) return;
            foreach (string id in Catalog.initiallyUnlocked)
            {
                var definition = Catalog.Find(id);
                if (definition == null || FindOwned(id) != null) continue;
                var owned = new PetOwnedEntry { petId = id, appearanceId = definition.FindAppearance(null)?.appearanceId };
                foreach (var appearance in definition.appearances)
                    if (appearance != null && !owned.unlockedAppearances.Contains(appearance.appearanceId))
                        owned.unlockedAppearances.Add(appearance.appearanceId);
                _save.owned.Add(owned);
            }
            if (FindOwned(Catalog.defaultPetId) != null)
            { _save.selectedPetId = Catalog.defaultPetId; _save.isAccompanying = true; }
        }
        private void SetState(PetRuntimeState state, string reason = null)
        {
            if (State == state && FailureReason == reason) return;
            State = state; FailureReason = reason; Changed?.Invoke();
        }
        public void ExportSaveData(GameSaveData data) => data.pet = GetSnapshot();
        public void ImportSaveData(GameSaveData data)
        { Despawn(); _hasSave = true; _save = PetSaveRules.Normalize(data.pet); FailureReason = null; SetState(PetRuntimeState.Suspended); }
        public void ResetForNewGame()
        { Despawn(); _hasSave = false; SeedNewGame(); FailureReason = null; SetState(PetRuntimeState.Suspended); }
        public void Dispose() { Despawn(); _ready = false; _suspensions.Clear(); Changed = null; Catalog = null; }

    }
}
