using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>전역 표본과 Unity의 중복 경계·네이티브 버퍼 사이를 동기화한다.</summary>
    public sealed class WorldTerrainStateAdapter
    {
        private readonly Terrain[,] _terrains;
        private readonly WorldProfile _profile;
        public Terrain[,] Terrains => _terrains;

        public WorldTerrainStateAdapter(WorldProfile profile, Terrain[,] terrains)
        {
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
            _terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            Validate();
        }

        /// <summary>브러시가 한 타일만 바꾼 경계는 캡처 기준과 대조해 한 값으로 합성한다.</summary>
        public TerrainBuildResult Read(TerrainBuildResult captureBaseline = null)
        {
            Validate();
            int n = _profile.SampleCount, cells = _profile.TileCells;
            var heights = new float[checked(n * n)];
            var assigned = new bool[heights.Length];
            var surfaces = new bool[checked((n - 1) * (n - 1))];
            for (int tz = 0; tz < _profile.TileCount; tz++)
            for (int tx = 0; tx < _profile.TileCount; tx++)
            {
                var data = _terrains[tz, tx].terrainData;
                var tileHeights = data.GetHeights(0, 0, cells + 1, cells + 1);
                for (int z = 0; z <= cells; z++)
                for (int x = 0; x <= cells; x++)
                {
                    int gx = tx * cells + x, gz = tz * cells + z, index = gz * n + gx;
                    float sample = _profile.OriginY + tileHeights[z, x] * _profile.VerticalRange;
                    if (assigned[index] && heights[index] != sample)
                    {
                        if (captureBaseline == null) throw new InvalidDataException("UncapturedTerrainEdits: 공유 경계가 다릅니다. Capture/Revert가 필요합니다.");
                        float baseline = captureBaseline.GetHeight(gz, gx);
                        if (heights[index] == baseline) heights[index] = sample;
                        else if (sample != baseline) throw new InvalidDataException("SculptConflict: 같은 경계에 서로 다른 수정이 있습니다.");
                    }
                    else heights[index] = sample;
                    assigned[index] = true;
                }
                var holes = data.GetHoles(0, 0, cells, cells);
                for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++) surfaces[(tz * cells + z) * (n - 1) + tx * cells + x] = holes[z, x];
            }
            return new TerrainBuildResult(n, heights, surfaces);
        }

        /// <summary>영향 영역과 접하는 모든 타일의 경계 한 줄까지 적용한다.</summary>
        public void Apply(TerrainBuildResult result, SampleRect? dirty = null)
        {
            Validate();
            if (result.SampleCount != _profile.SampleCount) throw new ArgumentException("지형 표본 수가 다릅니다.", nameof(result));
            var region = dirty ?? SampleRect.Full(result.SampleCount);
            int cells = _profile.TileCells;
            for (int tz = 0; tz < _profile.TileCount; tz++)
            for (int tx = 0; tx < _profile.TileCount; tx++)
            {
                var data = _terrains[tz, tx].terrainData;
                int x0 = Math.Max(tx * cells, region.X), z0 = Math.Max(tz * cells, region.Z);
                int x1 = Math.Min(tx * cells + cells + 1, region.X + region.Width), z1 = Math.Min(tz * cells + cells + 1, region.Z + region.Height);
                if (x0 >= x1 || z0 >= z1) continue;
                var heights = new float[z1 - z0, x1 - x0];
                for (int z = z0; z < z1; z++)
                for (int x = x0; x < x1; x++) heights[z - z0, x - x0] = (result.GetHeight(z, x) - _profile.OriginY) / _profile.VerticalRange;
                data.SetHeightsDelayLOD(x0 - tx * cells, z0 - tz * cells, heights);
                int holeX1 = Math.Min(tx * cells + cells, x1), holeZ1 = Math.Min(tz * cells + cells, z1);
                if (x0 < holeX1 && z0 < holeZ1)
                {
                    var surface = new bool[holeZ1 - z0, holeX1 - x0];
                    for (int z = z0; z < holeZ1; z++)
                    for (int x = x0; x < holeX1; x++) surface[z - z0, x - x0] = result.GetSurface(z, x);
                    data.SetHolesDelayLOD(x0 - tx * cells, z0 - tz * cells, surface);
                    data.SyncTexture(TerrainData.HolesTextureName);
                }
                data.SyncHeightmap();
                EditorUtility.SetDirty(data);
            }
        }

        /// <summary>실패 복구를 위해 중복 경계도 타일별 원래 값으로 보관한다.</summary>
        public List<TileSnapshot> CaptureTiles()
        {
            var snapshots = new List<TileSnapshot>();
            foreach (var terrain in _terrains)
            {
                var data = terrain.terrainData;
                snapshots.Add(new TileSnapshot(data, data.GetHeights(0, 0, _profile.TileCells + 1, _profile.TileCells + 1), data.GetHoles(0, 0, _profile.TileCells, _profile.TileCells)));
            }
            return snapshots;
        }

        /// <summary>Undo의 네이티브 버퍼 복구에 의존하지 않고 실패 전 상태를 복원한다.</summary>
        public static void RestoreTiles(IReadOnlyList<TileSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                var data = string.IsNullOrEmpty(snapshot.AssetGuid) ? snapshot.Data : AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GUIDToAssetPath(snapshot.AssetGuid));
                if (data == null) throw new InvalidDataException("복원할 TerrainData GUID가 유효하지 않습니다.");
                data.SetHeightsDelayLOD(0, 0, snapshot.Heights);
                data.SetHolesDelayLOD(0, 0, snapshot.Surfaces);
                data.SyncHeightmap();
                data.SyncTexture(TerrainData.HolesTextureName);
                EditorUtility.SetDirty(data);
            }
        }

        private void Validate()
        {
            var grid = _profile.CreateGrid();
            if (_terrains.GetLength(0) != _profile.TileCount || _terrains.GetLength(1) != _profile.TileCount) throw new InvalidDataException("타일 개수가 다릅니다.");
            var dataSet = new HashSet<TerrainData>();
            for (int z = 0; z < _profile.TileCount; z++)
            for (int x = 0; x < _profile.TileCount; x++)
            {
                var terrain = _terrains[z, x];
                if (terrain == null || terrain.terrainData == null || !dataSet.Add(terrain.terrainData)) throw new InvalidDataException("Terrain 또는 고유 TerrainData가 누락됐습니다.");
                var expected = new Vector3((float)grid.SampleToWorld(x * _profile.TileCells), _profile.OriginY, (float)grid.SampleToWorld(z * _profile.TileCells));
                var expectedSize = new Vector3(_profile.WorldSize / _profile.TileCount, _profile.VerticalRange, _profile.WorldSize / _profile.TileCount);
                if ((terrain.transform.position - expected).sqrMagnitude > .000001f || terrain.transform.rotation != Quaternion.identity || terrain.transform.lossyScale != Vector3.one || terrain.terrainData.size != expectedSize || terrain.terrainData.heightmapResolution != _profile.TileCells + 1)
                    throw new InvalidDataException("Terrain 좌표·회전·스케일·해상도가 WorldProfile과 다릅니다.");
            }
        }

        public sealed class TileSnapshot
        {
            public readonly TerrainData Data;
            public readonly string AssetGuid;
            public readonly float[,] Heights;
            public readonly bool[,] Surfaces;
            public TileSnapshot(TerrainData data, float[,] heights, bool[,] surfaces) { Data = data; AssetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)); Heights = heights; Surfaces = surfaces; }
        }
    }
}
