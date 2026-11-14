using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Data.Save;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>맵별 미획득 보상을 보존하고 플레이어와 펫의 지급 요청을 단일 확정한다.</summary>
    public sealed class GroundDropManager : BaseManager<GroundDropManager>, IManager,
        IAsyncInitializableManager, IUpdatableManager, ISaveable, IGroundDropService
    {
        public const string SettingsKey = "GroundDropSettings";
        private readonly GroundDropLedger _ledger = new();
        private readonly Dictionary<string, GroundDropRecord> _records = new();
        private readonly Dictionary<string, Transform> _representations = new();
        private readonly HashSet<string> _sources = new(StringComparer.Ordinal);
        private readonly List<GroundDropView> _visible = new();
        private readonly HashSet<string> _placed = new();
        private readonly Dictionary<string, Vector3> _launchOrigins = new();
        private float _nextRefresh;
        private string _map;
        private bool _isReady;
        public GroundDropSettingsSO Settings { get; private set; }
        public event Action<string> Collected;
        public IReadOnlyList<GroundDropView> VisibleDrops => _visible;

        public void Init() => SaveManager.Instance.RegisterSaveable(this);
        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Settings = await Svc.Asset.LoadGlobalAsync<GroundDropSettingsSO>(SettingsKey, cancellationToken);
            if (Settings == null || Settings.dropPrefab == null)
                throw new InvalidOperationException("바닥 드롭 설정 또는 프리팹이 없습니다.");
        }
        public void AfterInit() { _isReady = true; RefreshWorld(); }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) => RefreshWorld();
        public void OnUpdate()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1f;
            RefreshWorld();
        }

        /// <summary>선지급 없이 추첨 결과를 확정·보존하고 안전한 바닥에 표현한다.</summary>
        public void SpawnItems(IReadOnlyList<ItemInstance> items, Vector3 position, string sourceId = null, Transform source = null)
        {
            string map = source != null ? World.RegionOwnership.Resolve(source) : SceneManager.Instance.CurrentMapID;
            if (string.IsNullOrEmpty(map)) map = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string rewardSource = string.IsNullOrEmpty(sourceId) ? Guid.NewGuid().ToString("N") : sourceId;
            string sourceKey = map + ":" + rewardSource;
            if (_sources.Contains(sourceKey)) return;
            if (!Services.TryGet<IResolvedItemInventoryService>(out var inventory))
                throw new InvalidOperationException("드롭 아이템 확정 서비스가 준비되지 않았습니다.");
            List<ItemSaveEntry> resolved = inventory.ResolveDropItems(items);
            for (int i = 0; i < resolved.Count; i++)
            {
                Vector3 ground = FindDropPosition(position, map, resolved.Count);
                var record = new GroundDropRecord
                {
                    dropId = Guid.NewGuid().ToString("N"), sourceId = rewardSource, mapId = map,
                    position = new SerializableVector3(WorldCoordinates.ToSource(map, ground)), item = resolved[i]
                };
                _ledger.Register(record);
                _records.Add(record.dropId, record);
                _launchOrigins[record.dropId] = position;
            }
            if (resolved.Count > 0) _sources.Add(sourceKey);
            RefreshWorld();
        }

        /// <summary>씬 배치 아이템도 같은 획득·저장 경로에 등록한다.</summary>
        public string RegisterPlaced(string sourceId, ItemInstance item, Transform representation)
        {
            if (representation == null || item?.data == null) return null;
            string map = World.RegionOwnership.Resolve(representation);
            if (string.IsNullOrEmpty(map)) return null;
            string id = map + ":placed:" + sourceId;
            if (!_records.TryGetValue(id, out var record))
            {
                var inventory = Services.Get<IResolvedItemInventoryService>();
                var resolved = inventory.ResolveDropItems(new[] { item });
                if (resolved.Count == 0) return null;
                record = new GroundDropRecord
                {
                    dropId = id, sourceId = "placed:" + sourceId, mapId = map,
                    item = resolved[0], position = new SerializableVector3(WorldCoordinates.ToSource(map, representation.position))
                };
                _ledger.Register(record);
                _records.Add(id, record);
                for (int i = 1; i < resolved.Count; i++)
                {
                    var extra = new GroundDropRecord
                    {
                        dropId = Guid.NewGuid().ToString("N"), sourceId = record.sourceId,
                        mapId = map, position = record.position, item = resolved[i]
                    };
                    _ledger.Register(extra); _records.Add(extra.dropId, extra);
                }
            }
            _placed.Add(id);
            if (record.collected) { representation.gameObject.SetActive(false); return id; }
            if (_representations.TryGetValue(id, out var old) && old != null && old != representation)
                Destroy(old.gameObject);
            _representations[id] = representation;
            _ledger.MarkAvailable(id);
            RebuildVisible();
            return id;
        }

        public void ResetPlaced(string dropId)
        {
            if (!_ledger.Remove(dropId)) return;
            _records.Remove(dropId); _placed.Remove(dropId); _representations.Remove(dropId);
            RebuildVisible();
        }

        /// <summary>수동 획득은 입력·거리·지형 시야를 요청 순간 다시 확인한다.</summary>
        public bool CanPlayerCollect(string id)
        {
            if (!_records.TryGetValue(id, out var record) || record.collected || Settings == null) return false;
            var player = ActorSvc.Objects?.Player;
            if (player == null || !player.IsAlive() || Time.timeScale <= 0f
                || Svc.Input == null || Svc.Input.CurrentLayer > InputLayer.Level_0
                || Svc.Input.IsPlayerActionInputSuppressed || Svc.Dialogue?.IsDialogueActive == true
                || SceneManager.Instance.IsLoading) return false;
            if (!_representations.TryGetValue(id, out var view) || view == null || !view.gameObject.activeInHierarchy)
                return false;
            if (view.TryGetComponent<GroundDropRepresentation>(out var landing) && !landing.IsSettled) return false;
            Vector3 position = view.position;
            return (position - player.transform.position).sqrMagnitude <= Settings.pickupDistance * Settings.pickupDistance
                && !Physics.Linecast(player.transform.position + Vector3.up, position + Vector3.up * Settings.surfaceOffset,
                    Settings.visibilityMask, QueryTriggerInteraction.Ignore);
        }

        public bool TryPlayerCollect(string id) => CanPlayerCollect(id) && Collect(id, null);
        public bool TryReserve(string id, object owner, float duration) => _ledger.TryReserve(id, owner, Time.time, duration);
        public void Release(string id, object owner) => _ledger.Release(id, owner);
        public bool TryPetCollect(string id, object owner, Vector3 collectorPosition, float arrivalDistance)
        {
            if (owner == null || arrivalDistance <= 0f || !_representations.TryGetValue(id, out var view) || view == null)
                return false;
            var player = ActorSvc.Objects?.Player;
            if (player == null || !player.IsAlive() || Time.timeScale <= 0f || SceneManager.Instance.IsLoading
                || Svc.Dialogue?.IsDialogueActive == true || Svc.Input.IsPlayerActionInputSuppressed
                || Svc.Input.CurrentLayer > InputLayer.Level_0) return false;
            if (view.TryGetComponent<GroundDropRepresentation>(out var landing) && !landing.IsSettled) return false;
            return (view.position - collectorPosition).sqrMagnitude <= arrivalDistance * arrivalDistance
                && !Physics.Linecast(collectorPosition + Vector3.up * Settings.surfaceOffset,
                    view.position + Vector3.up * Settings.surfaceOffset, Settings.visibilityMask, QueryTriggerInteraction.Ignore)
                && Collect(id, owner);
        }

        private bool Collect(string id, object owner)
        {
            if (!Services.TryGet<IResolvedItemInventoryService>(out var inventory)
                || !_ledger.TryCollect(id, inventory.TryStoreResolvedItem, owner, Time.time)) return false;
            GroundDropRecord record = _records[id];
            record.collected = true;
            bool showAcquisition = true;
            Vector3 feedbackPosition = WorldCoordinates.ToWorld(record.mapId, record.position.ToVector3());
            if (_representations.Remove(id, out var representation) && representation != null)
            {
                if (_placed.Contains(id) && representation.TryGetComponent<DropItemActor>(out var placed))
                {
                    showAcquisition = placed.ShowsAcquisitionUI;
                    placed.PlayCollectEffects();
                    if (ActorSvc.InteractionPersistence?.TryConsume(placed) != true) representation.gameObject.SetActive(false);
                }
                else
                {
                    feedbackPosition = representation.position;
                    Destroy(representation.gameObject);
                }
            }
            RebuildVisible();
            // 확정 후 피드백 실패가 아이템 재지급으로 이어지지 않도록 순서를 지킨다.
            try
            {
                Collected?.Invoke(id);
                inventory.NotifyResolvedItemAcquired(record.item);
                if (!_placed.Contains(id) && !string.IsNullOrEmpty(Settings.pickupFxKey))
                    ActorSvc.Objects?.ShowFX(Settings.pickupFxKey, feedbackPosition, duration: Settings.pickupFxLifetime);
                if (showAcquisition) ActorSvc.UI?.ShowItemAcquisition(Svc.Item.GetItemData(record.item.itemId), record.item.count);
                ActorSvc.UI?.RefreshInventoryIfVisible();
            }
            catch (Exception exception) { Debug.LogException(exception); }
            return true;
        }

        private void RefreshWorld()
        {
            if (!_isReady || Settings == null || SceneManager.Instance.IsLoading || SaveManager.Instance.IsPreparingSceneLoad) return;
            string map = SceneManager.Instance.CurrentMapID;
            if (_map != map) { ClearRepresentations(); _map = map; }
            if (string.IsNullOrEmpty(map)) return;
            var placed = DropItemActor.ActiveDrops;
            for (int i = placed.Count - 1; i >= 0; i--) placed[i]?.RegisterGroundDrop(this);
            foreach (var record in _records.Values)
            {
                if (record.collected || record.mapId != map || _placed.Contains(record.dropId)) continue;
                if (_representations.TryGetValue(record.dropId, out var existing) && existing != null) continue;
                ItemSO item = Svc.Item?.GetItemData(record.item.itemId);
                if (item == null || !TryFindGround(WorldCoordinates.ToWorld(map, record.position.ToVector3()), out var position)) continue;
                var instance = Instantiate(Settings.dropPrefab, position, Quaternion.identity);
                if (instance.TryGetComponent<GroundDropRepresentation>(out var representation))
                {
                    // 방금 생성된 드롭만 원천에서 튀어나오고, 맵 복원으로 다시 만든 드롭은 제자리에서 나타난다.
                    Vector3? launch = _launchOrigins.Remove(record.dropId, out var origin) ? origin : null;
                    representation.Initialize(item, Settings, launch);
                }
                _representations[record.dropId] = instance.transform;
                _ledger.MarkAvailable(record.dropId);
            }
            RebuildVisible();
        }

        private bool TryFindGround(Vector3 origin, out Vector3 position)
        {
            position = origin;
            if (!Physics.Raycast(origin + Vector3.up * Settings.groundProbeHeight, Vector3.down,
                out var hit, Settings.groundProbeHeight + Settings.groundProbeDepth,
                Settings.groundMask, QueryTriggerInteraction.Ignore)) return false;
            position = hit.point + Vector3.up * Settings.surfaceOffset;
            return true;
        }

        private Vector3 FindDropPosition(Vector3 origin, string map, int dropCount)
        {
            // 간격을 확보하지 못해도 보상을 버리지 않고 안전한 후보 중 가장 빈 곳을 쓴다.
            if (!TryFindGround(origin, out var center)) center = origin;
            float spacing = Mathf.Max(0.01f, Settings.minimumDropSpacing);
            float radius = Mathf.Min(Mathf.Max(0f, Settings.scatterRadius),
                spacing * Mathf.Sqrt(Mathf.Max(1, dropCount)));
            Vector3 best = center;
            float bestClearance = FindNearestDropSqrDistance(center, map);
            const int candidateCount = 24;
            for (int attempt = 0; attempt < candidateCount; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (!TryFindScatterGround(center, candidate, out var ground)) continue;
                float clearance = FindNearestDropSqrDistance(ground, map);
                if (clearance >= spacing * spacing) return ground;
                if (clearance <= bestClearance) continue;
                best = ground;
                bestClearance = clearance;
            }
            return best;
        }

        private bool TryFindScatterGround(Vector3 center, Vector3 candidate, out Vector3 position)
        {
            position = candidate;
            if (!Physics.Raycast(candidate + Vector3.up * Settings.groundProbeHeight, Vector3.down,
                out var hit, Settings.groundProbeHeight + Settings.groundProbeDepth,
                Settings.groundMask, QueryTriggerInteraction.Ignore)) return false;
            position = hit.point + Vector3.up * Settings.surfaceOffset;
            if (Mathf.Abs(position.y - center.y) > Settings.maximumScatterHeightDifference
                || Vector3.Angle(hit.normal, Vector3.up) > Settings.maximumScatterSlope) return false;
            // 모델 중심 높이에서 원천과 연결되는 점만 사용해 벽 뒤로 흩어지지 않게 한다.
            Vector3 lift = Vector3.up * Settings.tokenHoverHeight;
            return !Physics.Linecast(center + lift, position + lift,
                Settings.groundMask | Settings.visibilityMask, QueryTriggerInteraction.Ignore);
        }

        private float FindNearestDropSqrDistance(Vector3 position, string map)
        {
            float nearest = float.PositiveInfinity;
            foreach (var record in _records.Values)
            {
                if (record.collected || record.mapId != map) continue;
                Vector3 other = WorldCoordinates.ToWorld(map, record.position.ToVector3());
                Vector2 delta = new(position.x - other.x, position.z - other.z);
                nearest = Mathf.Min(nearest, delta.sqrMagnitude);
            }
            return nearest;
        }

        private void RebuildVisible()
        {
            _visible.Clear();
            foreach (var pair in _representations)
            {
                if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy) continue;
                var record = _records[pair.Key];
                if (!record.collected) _visible.Add(new GroundDropView(pair.Key, pair.Value, Svc.Item.GetItemData(record.item.itemId), record.item.count));
            }
        }

        private void ClearRepresentations()
        {
            foreach (var pair in _representations)
                if (pair.Value != null && !_placed.Contains(pair.Key)) Destroy(pair.Value.gameObject);
            _representations.Clear(); _visible.Clear(); _placed.Clear(); _launchOrigins.Clear();
        }
        public void ExportSaveData(GameSaveData data) => data.groundDrops = _ledger.Export();
        public void ImportSaveData(GameSaveData data)
        {
            ClearRepresentations();
            _ledger.Import(data.groundDrops);
            _records.Clear(); _sources.Clear();
            foreach (var record in _ledger.Export())
            { _records.Add(record.dropId, record); _sources.Add(record.mapId + ":" + record.sourceId); }
            _nextRefresh = 0f;
        }
        public void ResetForNewGame() { ImportSaveData(new GameSaveData()); }
        public void Dispose() { ClearRepresentations(); _records.Clear(); _sources.Clear(); Settings = null; _isReady = false; }
    }
}
