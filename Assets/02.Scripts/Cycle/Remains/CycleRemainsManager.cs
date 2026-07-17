using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Cycle;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    public sealed class CycleRemainsManager : BaseManager<CycleRemainsManager>, IManager, ISaveable,
        ICycleRemainsService
    {
        [SerializeField] private RemainsActor _remainsPrefab;
        private readonly CycleLootLedger _ledger = new();
        private RemainsState _current;
        private RemainsActor _actor;
        private int _lastWipeFrame = -1;

        public CycleLootLedger Ledger => _ledger;
        public RemainsState Current => _current;
        public event Action<RemainsState> OnRemainsCreated;
        public event Action<RemainsState> OnRemainsRecovered;
        public event Action<RemainsState> OnRemainsDiscarded;

        public bool TryAddUnsettledMaterial(int itemId, int count)
        {
            if (!(CycleRunManager.Instance?.IsActive ?? false) || !(CycleRunManager.Instance.Config?.IsUnsettledMaterial(itemId) ?? false)) return false;
            _ledger.Add(itemId, count);
            return true;
        }

        public void Init() => SaveManager.Instance.RegisterSaveable(this);
        public void AfterInit() { }
        public void Configure(RemainsActor prefab) => _remainsPrefab = prefab;
        public void Dispose() => ClearRuntimeActor();
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType)
        {
            _actor = null;
            CycleRemainsMarkerRegistry.Clear();
            if (_current != null && !_current.recovered && _current.mapId == SceneManager.Instance?.CurrentMapID) SpawnRemainsActor();
        }

        public bool HandlePartyWipe(Vector3 deathPosition, Quaternion deathRotation)
        {
            if (!(CycleRunManager.Instance?.IsActive ?? false) || _lastWipeFrame == Time.frameCount) return false;
            _lastWipeFrame = Time.frameCount;
            if (_current != null && !_current.recovered) { InvokeSafe(OnRemainsDiscarded, _current); ClearCurrent(); }

            PartyManager party = PartyManager.Instance;
            if (party == null || party.BattleOrder.Count == 0) return false;
            bool dropUnsettledMaterials = CycleRunManager.Instance.Config?.dropUnsettledMaterials ?? true;
            RemainsState state = new()
            {
                remainsId = Guid.NewGuid().ToString("N"),
                mapId = SceneManager.Instance?.CurrentMapID,
                position = new SerializableVector3(deathPosition),
                rotation = new SerializableQuaternion(deathRotation),
                materials = dropUnsettledMaterials ? _ledger.Snapshot() : new List<CycleItemStack>(),
            };
            foreach (CharacterActorType type in party.BattleOrder)
            {
                long requested = (long)Math.Floor(party.GetExp(type) * (CycleRunManager.Instance.Config?.expLossRate ?? 0.30f));
                long removed = party.RemoveCurrentLevelExp(type, requested);
                if (removed > 0) state.lostExp.Add(new LostExpEntry { characterType = type, amount = removed });
            }
            if (dropUnsettledMaterials)
                _ledger.Clear();
            _current = state;
            SpawnRemainsActor();
            RespawnParty(deathPosition);
            InvokeSafe(OnRemainsCreated, _current);
            SaveManager.Instance?.TrySaveActiveSlot();
            return true;
        }

        public bool TryRecover(string remainsId)
        {
            if (_current == null || _current.recovered || _current.remainsId != remainsId) return false;
            foreach (LostExpEntry entry in _current.lostExp)
                if (entry == null || entry.characterType == CharacterActorType.None || entry.amount < 0) return false;
            foreach (CycleItemStack item in _current.materials)
                if (item == null || item.itemId <= 0 || item.count <= 0) return false;

            foreach (LostExpEntry entry in _current.lostExp) PartyManager.Instance.RestoreCurrentLevelExp(entry.characterType, entry.amount);
            foreach (CycleItemStack item in _current.materials) _ledger.Add(item.itemId, item.count);
            _current.recovered = true;
            RemainsState recovered = _current;
            ClearCurrent();
            InvokeSafe(OnRemainsRecovered, recovered);
            SaveManager.Instance?.TrySaveActiveSlot();
            return true;
        }

        public void DiscardRemains() { if (_current != null) InvokeSafe(OnRemainsDiscarded, _current); ClearCurrent(); }

        private void RespawnParty(Vector3 deathPosition)
        {
            PartyManager.Instance.HealAllParty(true);
            PlayerActor player = PartyManager.Instance.ActiveCharacter;
            if (player == null) return;
            Transform destination = FindNearestRespawn(deathPosition);
            Vector3 position = destination != null ? destination.position : deathPosition;
            Quaternion rotation = destination != null ? destination.rotation : Quaternion.identity;
            if (destination == null)
            {
                string playerSpawnId = CycleRunManager.Instance.CurrentLayout?.playerSpawnId;
                CycleSpawnPoint start = FindObjectsByType<CycleSpawnPoint>(FindObjectsSortMode.None)
                    .FirstOrDefault(point => point != null && point.SpawnId == playerSpawnId);
                if (start != null) { position = start.Position; rotation = start.Rotation; }
            }
            player.Respawn(position, rotation, 1f);
            CameraManager.Instance?.SnapToTarget(position);
        }

        private Transform FindNearestRespawn(Vector3 deathPosition)
        {
            HashSet<string> activeIds = new(CycleRunManager.Instance.CurrentLayout?.activeRespawnPointIds ?? new List<string>(), StringComparer.Ordinal);
            CycleRespawnPoint[] points = FindObjectsByType<CycleRespawnPoint>(FindObjectsSortMode.None);
            return points.Where(p => p != null && p.IsActive && activeIds.Contains(p.RespawnId))
                .OrderBy(p => (p.ArrivalPoint.position - deathPosition).sqrMagnitude)
                .ThenBy(p => p.RespawnId, StringComparer.Ordinal)
                .Select(p => p.ArrivalPoint).FirstOrDefault();
        }

        private void SpawnRemainsActor()
        {
            if (_current == null || _current.recovered || _actor != null) return;
            Vector3 position = _current.position.ToVector3();
            Quaternion rotation = _current.rotation.ToQuaternion();
            if (_remainsPrefab != null) _actor = Instantiate(_remainsPrefab, position, rotation);
            else { GameObject go = new("CycleRemains"); go.transform.SetPositionAndRotation(position, rotation); _actor = go.AddComponent<RemainsActor>(); }
            _actor.Initialize(_current.remainsId);
            CycleRemainsMarkerRegistry.Set(position);
        }

        private void ClearRuntimeActor()
        {
            if (_actor != null) Destroy(_actor.gameObject);
            _actor = null;
            CycleRemainsMarkerRegistry.Clear();
        }

        private void ClearCurrent() { ClearRuntimeActor(); _current = null; }

        internal void RestoreTransactionState(IEnumerable<CycleItemStack> ledger, RemainsState remains)
        {
            ClearCurrent();
            _ledger.Restore(ledger);
            _current = remains?.Clone();
            if (_current != null && !_current.recovered && _current.mapId == SceneManager.Instance?.CurrentMapID)
                SpawnRemainsActor();
        }

        private static void InvokeSafe(Action<RemainsState> handlers, RemainsState state)
        {
            Delegate[] listeners = handlers?.GetInvocationList();
            if (listeners == null) return;
            foreach (Delegate listener in listeners)
            {
                try { ((Action<RemainsState>)listener).Invoke(state); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.cycle ??= new CycleSaveData();
            saveData.cycle.unsettledMaterials = _ledger.Snapshot();
            saveData.cycle.remains = _current;
            _ledger.MarkSaved();
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            ClearRuntimeActor();
            _ledger.Restore(saveData?.cycle?.unsettledMaterials);
            _current = saveData?.cycle?.remains;
            if (_current != null && !_current.recovered && _current.mapId == SceneManager.Instance?.CurrentMapID) SpawnRemainsActor();
        }

        public void ResetForNewGame() { ClearCurrent(); _ledger.Clear(); _ledger.MarkSaved(); }
    }
}
