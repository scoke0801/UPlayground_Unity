using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;
using UPlayGround.UI;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 플레이어가 지나간 곳을 지도별로 기록하고 저장하며, 지도 UI에 안개 마스크를 공급한다.
    /// 미니맵 HUD가 숨겨져 있어도 기록이 이어져야 하므로 UI가 아닌 매니저가 기록한다.
    /// </summary>
    public sealed class MapExplorationManager : BaseManager<MapExplorationManager>, IManager, IUpdatableManager,
        ISaveable, IUIMapExplorationService
    {
        private const string MapConfigDatabaseKey = "MapConfigDatabase";
        private const string WorldKeyPrefix = "world:";

        private readonly Dictionary<string, ExplorationGrid> _grids = new();
        private readonly Dictionary<string, MapFogLayer> _layers = new();

        private MapConfigDatabaseSO _database;
        private bool _isDatabaseRequested;
        private float _recordTimer;

        // 기록 대상 지도는 지역이 바뀔 때만 다시 조회한다(조회 실패 경고의 반복 방지).
        private string _recordMapId;
        private string _recordWorldRootId;
        private MinimapIconConfigSO _recordConfig;
        private bool _recordUsesWorldMap;

        private string _lastStampKey;
        private Vector2 _lastStampPosition;

        private string _worldRootIdForKey;
        private string _worldKey;

        #region IManager

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit() { }

        public void Dispose()
        {
            ClearAll();
            _database = null;
            _isDatabaseRequested = false;
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType) => ResetRecordTarget();

        public void OnUpdate()
        {
            if (_database == null)
            {
                RequestDatabase();
                return;
            }

            MapFogSettingsSO settings = _database.FogSettings;
            if (settings == null) return;

            _recordTimer += Time.unscaledDeltaTime;
            if (_recordTimer < settings.recordInterval) return;
            _recordTimer = 0f;

            RecordPlayerPosition(settings);
        }

        private async void RequestDatabase()
        {
            if (_isDatabaseRequested) return;
            AssetManager assets = AssetManager.Instance;
            if (assets == null) return;

            _isDatabaseRequested = true;
            _database = await assets.TryLoadGlobalAsync<MapConfigDatabaseSO>(
                MapConfigDatabaseKey, nameof(MapExplorationManager), destroyCancellationToken);
            if (_database == null || _database.FogSettings == null)
                Debug.LogWarning("[MapExplorationManager] 지도 DB 또는 안개 설정이 없어 탐험 안개를 사용하지 않습니다.", this);
        }

        #endregion

        // ── 기록 ────────────────────────────────────────────────────

        private void RecordPlayerPosition(MapFogSettingsSO settings)
        {
            SceneManager scene = SceneManager.Instance;
            if (!CanRecord(scene)) return;

            PlayerActor player = GameObjectManager.Instance != null ? GameObjectManager.Instance.Player : null;
            if (player == null) return;

            ResolveRecordTarget(scene);
            if (_recordConfig == null || !_recordConfig.useExplorationFog) return;

            string key = ResolveKey(_recordMapId, _recordUsesWorldMap);
            if (string.IsNullOrEmpty(key)) return;

            Vector3 position = _recordUsesWorldMap
                ? player.transform.position
                : WorldCoordinates.ToSource(_recordMapId, player.transform.position);

            Reveal(key, _recordConfig, settings, new Vector2(position.x, position.z));
        }

        private static bool CanRecord(SceneManager scene)
        {
            if (scene == null || scene.IsLoading || !scene.ShowExplorationHud) return false;
            if (string.IsNullOrEmpty(scene.CurrentMapID)) return false;

            string sceneType = scene.CurrentSceneType;
            return sceneType != SceneType.Boot
                && sceneType != SceneType.Title
                && sceneType != SceneType.Loading;
        }

        private void ResolveRecordTarget(SceneManager scene)
        {
            string mapId = scene.CurrentMapID;
            string worldRootId = scene.WorldMapRootId;
            if (mapId == _recordMapId && worldRootId == _recordWorldRootId) return;

            _recordMapId = mapId;
            _recordWorldRootId = worldRootId;

            MinimapIconConfigSO worldConfig = scene.GetWorldMapConfig(mapId);
            _recordUsesWorldMap = worldConfig != null;
            _recordConfig = worldConfig != null ? worldConfig : _database.GetConfig(mapId);
        }

        /// <summary>지금 위치만 밝힌다. 이전 위치와 잇지 않으므로 순간 이동 경로는 드러나지 않는다.</summary>
        private void Reveal(string key, MinimapIconConfigSO config, MapFogSettingsSO settings, Vector2 position)
        {
            float longSide = GetMapLongSide(config);
            ExplorationGrid grid = GetOrCreateGrid(key, settings, longSide);

            float minMove = grid.CellSize * 0.5f;
            if (key == _lastStampKey && (position - _lastStampPosition).sqrMagnitude < minMove * minMove)
                return;

            _lastStampKey = key;
            _lastStampPosition = position;

            float radius = settings.ResolveRevealRadius(config, longSide);
            if (!grid.RevealCircle(position, radius, out RectInt changed)) return;
            if (!_layers.TryGetValue(key, out MapFogLayer layer)) return;

            layer.Paint(changed);
            layer.Upload();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 치트: 지금 있는 지도의 촬영 범위를 모두 탐험한 것으로 기록한다. 실제 기록이라 저장에도 남는다.
        /// </summary>
        public bool TryRevealCurrentMap(out string failureReason)
        {
            MapFogSettingsSO settings = _database != null ? _database.FogSettings : null;
            if (settings == null)
            {
                failureReason = "지도 DB 또는 안개 설정이 준비되지 않음";
                return false;
            }

            SceneManager scene = SceneManager.Instance;
            if (!CanRecord(scene))
            {
                failureReason = "탐험을 기록하는 지역이 아니거나 불러오는 중";
                return false;
            }

            // 기록·표시와 같은 대상과 저장 키를 써야 화면의 안개가 걷힌다.
            ResolveRecordTarget(scene);
            if (_recordConfig == null || !_recordConfig.useExplorationFog)
            {
                failureReason = "안개를 쓰는 지도가 아님";
                return false;
            }

            string key = ResolveKey(_recordMapId, _recordUsesWorldMap);
            if (string.IsNullOrEmpty(key))
            {
                failureReason = "지도 저장 키를 확인할 수 없음";
                return false;
            }

            ExplorationGrid grid = GetOrCreateGrid(key, settings, GetMapLongSide(_recordConfig));
            RectInt window = MapFogLayer.CalculateWindow(_recordConfig, grid.CellSize);
            if (grid.RevealRect(window) && _layers.TryGetValue(key, out MapFogLayer layer))
            {
                layer.Paint(window);
                layer.Upload();
            }

            failureReason = null;
            return true;
        }
#endif

        // ── UI 조회 ────────────────────────────────────────────────

        public bool TryGetFogView(string mapId, MinimapIconConfigSO config, bool isWorldMap, out MapFogView view)
        {
            view = default;
            if (config == null || !config.useExplorationFog) return false;

            MapFogSettingsSO settings = _database != null ? _database.FogSettings : null;
            if (settings == null || settings.fogMaterial == null) return false;

            SceneManager scene = SceneManager.Instance;
            if (scene == null || !scene.ShowExplorationHud) return false;

            string key = ResolveKey(mapId, isWorldMap);
            if (string.IsNullOrEmpty(key)) return false;

            ExplorationGrid grid = GetOrCreateGrid(key, settings, GetMapLongSide(config));
            RectInt window = MapFogLayer.CalculateWindow(config, grid.CellSize);

            if (!_layers.TryGetValue(key, out MapFogLayer layer) || !layer.Window.Equals(window))
            {
                layer?.Release();
                _layers[key] = layer = new MapFogLayer(grid, window);
            }

            view = new MapFogView(grid, layer.Mask, settings.fogMaterial, layer.WindowMin, layer.WindowSize);
            return true;
        }

        /// <summary>월드 지도와 지역 지도는 좌표계가 달라 저장 키를 분리한다.</summary>
        private string ResolveKey(string mapId, bool isWorldMap)
        {
            if (!isWorldMap) return mapId;

            string rootId = SceneManager.Instance != null ? SceneManager.Instance.WorldMapRootId : null;
            if (string.IsNullOrEmpty(rootId)) return null;

            if (rootId != _worldRootIdForKey)
            {
                _worldRootIdForKey = rootId;
                _worldKey = WorldKeyPrefix + rootId;
            }
            return _worldKey;
        }

        private ExplorationGrid GetOrCreateGrid(string key, MapFogSettingsSO settings, float mapLongSide)
        {
            if (!_grids.TryGetValue(key, out ExplorationGrid grid))
                _grids[key] = grid = new ExplorationGrid(settings.ResolveCellSize(mapLongSide));
            return grid;
        }

        private static float GetMapLongSide(MinimapIconConfigSO config)
        {
            MapFogLayer.CalculateCaptureBounds(config, out Vector2 min, out Vector2 max);
            return Mathf.Max(max.x - min.x, max.y - min.y);
        }

        // ── 저장 / 복원 (ISaveable) ─────────────────────────────────

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null) return;

            var data = new MapExplorationSaveData();
            foreach (var pair in _grids)
                data.layers.Add(pair.Value.Export(pair.Key));
            saveData.mapExploration = data;
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            ClearAll();

            var layers = saveData?.mapExploration?.layers;
            if (layers == null) return;

            foreach (var layer in layers)
            {
                if (layer == null || string.IsNullOrEmpty(layer.key)) continue;
                ExplorationGrid grid = ExplorationGrid.Import(layer);
                if (grid != null) _grids[layer.key] = grid;
            }
        }

        public void ResetForNewGame() => ClearAll();

        private void ClearAll()
        {
            foreach (var layer in _layers.Values)
                layer.Release();
            _layers.Clear();
            _grids.Clear();
            ResetRecordTarget();
        }

        private void ResetRecordTarget()
        {
            _recordMapId = null;
            _recordWorldRootId = null;
            _recordConfig = null;
            _lastStampKey = null;
            _recordTimer = 0f;
        }
    }
}
