using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>수계 원본·소유 패치·native Terrain·수면/체적/FX를 한 트랜잭션으로 적용한다.</summary>
    public static class WorldWaterAuthoring
    {
        public static WaterBuildResult LastResult { get; private set; }
        public static Action<string> FailureInjection;
        public static WaterBuildResult Evaluate(WorldDefinition definition, WaterSystemDefinition source)
        {
            LastResult = null;
            source.ValidateIdentity(definition.WorldGuid); ValidateResources(source);
            if (definition.Planning != null && definition.Planning.planGuid == source.systemGuid) throw new InvalidOperationException("WaterIdentityConflict: 계획과 수계의 연산 소유자 GUID 충돌입니다.");
            var retained = new List<TerrainOperation>();
            foreach (var record in definition.Operations) if (record.ownerPlanGuid != source.systemGuid) retained.Add(record.ToCore(definition.SourceRevision));
            var bodies = new List<WaterBody>(); foreach (var record in source.bodies) bodies.Add(record.ToCore());
            ValidateExclusions(source, definition.Profile.CreateGrid());
            var zones = new List<PlanZone>();
            if (definition.Planning != null)
            {
                zones.AddRange(definition.Planning.reservations);
                foreach (var placed in definition.Planning.AppliedStructures) if (!definition.Planning.bridges.Exists(b => b.bridgeGuid == placed.id))
                    zones.Add(new PlanZone { guid = placed.id, boundary = PlanningGeometry.Footprint(placed) });
            }
            var preset = source.waterfallPreset;
            LastResult = WaterBuildEngine.Build(definition.WorldGuid, source.systemGuid, definition.ReadSource(), definition.Profile.CreateGrid(),
                definition.Profile.TileCells * definition.Profile.CreateGrid().SampleSpacing, retained, bodies, zones, source.waterfalls,
                preset != null ? preset.minimumDrop : 6, preset != null ? preset.poolMargin : 1, preset != null ? preset.socketTolerance : .05);
            if (!LastResult.HasErrors) WorldWaterPlanningValidation.Validate(definition, LastResult);
            return LastResult;
        }

        public static WaterBuildResult Regenerate(WorldDefinition definition, WaterSystemDefinition source) => Commit(definition, source, null);
        /// <summary>수위·이동·삭제를 포함한 명시적 편집은 이 콜백 안에서 수행해 SO도 함께 Undo한다.</summary>
        public static WaterBuildResult Edit(WorldDefinition definition, WaterSystemDefinition source, Action mutation) => Commit(definition, source, mutation);
        /// <summary>수위와 그 수역에 붙은 폭포 소켓 Y를 같은 Undo 작업으로 바꾼다. 다른 수역·교량은 이동하지 않는다.</summary>
        public static WaterBuildResult SetLevel(WorldDefinition definition, WaterSystemDefinition source, string guid, double level)
        {
            PlacementIdentity.RequireFinite(level);
            return Edit(definition, source, () =>
            {
                var body = source.bodies.Find(b => b.waterGuid == guid) ?? throw new InvalidOperationException("수계 ID가 없습니다.");
                if (body.kind == WaterBodyKind.River && (body.route == null || body.route.Length < 2)) throw new InvalidOperationException("강의 확정 경로가 필요합니다.");
                double delta = level - (body.kind == WaterBodyKind.River ? body.route[0].y : body.level);
                if (body.kind == WaterBodyKind.River) for (int i = 0; i < body.route.Length; i++) body.route[i].y += delta;
                else body.level = level;
                foreach (var edge in source.waterfalls)
                { if (edge.upstreamGuid == guid) edge.upperLip.y += delta; if (edge.downstreamGuid == guid) edge.lowerLanding.y += delta; }
            });
        }
        public static PlanPoint[] ProposeRoute(WorldDefinition definition, WaterSystemDefinition source, string guid)
        {
            source.ValidateIdentity(definition.WorldGuid);
            var record = source.bodies.Find(b => b.waterGuid == guid) ?? throw new InvalidOperationException("수계 ID가 없습니다.");
            var zones = definition.Planning != null ? definition.Planning.reservations : new List<PlanZone>();
            var field = TerrainOperationEvaluator.Build(definition.ReadSource(), definition.Profile.CreateGrid(), definition.ReadOperations());
            var path = WaterRouteSearch.Propose(record.ToCore(), definition.Profile.CreateGrid(), field, record.preset.searchStep, record.preset.maxVisited, zones, out var conflict);
            if (conflict != null) throw new InvalidOperationException(conflict.ToString()); return path;
        }
        public static void ValidateTerrainChange(WorldDefinition definition, TerrainBuildResult terrain, IReadOnlyList<TerrainOperationRecord> records)
        {
            var source = definition.WaterSystem; if (source == null) return;
            var result = new WaterBuildResult { Terrain = terrain };
            foreach (var record in source.bodies) { var body = record.ToCore(); WaterGeometry.Validate(body, definition.Profile.CreateGrid()); result.Bodies.Add(body); WaterBuildEngine.ValidateTerrain(body, definition.Profile.CreateGrid(), terrain, result); }
            WorldWaterPlanningValidation.Validate(definition, result, records);
            ThrowIfInvalid(result);
        }

        private static WaterBuildResult Commit(WorldDefinition definition, WaterSystemDefinition source, Action mutation)
        {
            if (definition == null || source == null || EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid))
                throw new InvalidOperationException("대상 월드의 미리보기를 Edit Mode에서 여세요.");
            source.ValidateIdentity(definition.WorldGuid);
            WorldWaterSceneBuilder.EnsureClean(definition);
            var created = new List<string>();
            using (var session = new WorldTerrainAuthoringSession(definition, WorldBuilderAuthoringAssets.FindTerrains(definition)))
            {
                session.EnsureReadyForBake(); var before = session.Terrain.CaptureTiles();
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                try
                {
                    Undo.RegisterCompleteObjectUndo(source, "수계 편집"); mutation?.Invoke();
                    var result = Evaluate(definition, source); ThrowIfInvalid(result);
                    var patches = SavePatches(definition, source, result, created);
                    FailureInjection?.Invoke("AfterAssets");
                    session.ReplaceOwnedOperations(source.systemGuid, patches, () =>
                    {
                        Undo.RegisterCompleteObjectUndo(source, "수계 적용 상태 기록");
                        definition.SetWaterSystem(source); source.SetApplied(result, SourceHash(source)); Undo.FlushUndoRecordObjects();
                    });
                    FailureInjection?.Invoke("AfterTerrain");
                    WorldWaterSceneBuilder.Apply(definition, source, result, session.Terrain.Terrains, created);
                    FailureInjection?.Invoke("AfterScene");
                    EditorUtility.SetDirty(definition); EditorUtility.SetDirty(source); AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(source);
                    Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); WorldPlanningUndoRestore.Track(definition); return result;
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group); WorldTerrainStateAdapter.RestoreTiles(before);
                    foreach (var terrain in session.Terrain.Terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                    EditorUtility.SetDirty(definition); EditorUtility.SetDirty(source); AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(source);
                    foreach (string path in created) AssetDatabase.DeleteAsset(path); throw;
                }
            }
        }
        private static List<TerrainOperationRecord> SavePatches(WorldDefinition definition, WaterSystemDefinition source, WaterBuildResult result, List<string> created)
        {
            var records = new List<TerrainOperationRecord>();
            string folder = "Assets/WorldBuilder/Worlds/" + definition.WorldGuid + "/Authoring/WaterPatches"; WorldBuilderAuthoringAssets.EnsureFolder(folder);
            foreach (var patch in result.Patches)
            {
                byte[] bytes = TerrainSnapshotCodec.EncodePatch(patch.Bounds.Width, patch.Bounds.Height, patch.Values, patch.Masks); string hash = WorldDefinition.HashBytes(bytes), path = folder + "/" + hash + ".bytes";
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    if (File.Exists(path)) throw new IOException("미식별 수계 패치를 덮어쓸 수 없습니다.");
                    File.WriteAllBytes(path, bytes); created.Add(path); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                var blob = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(path)));
                if (blob == null || WorldDefinition.HashBytes(blob.bytes) != hash) throw new IOException("수계 패치 타입/내용 충돌");
                records.Add(new TerrainOperationRecord { operationGuid = patch.OperationGuid, ownerPlanGuid = source.systemGuid, ownerElementGuid = patch.ElementGuid,
                    kind = TerrainOperationKind.SetAbsolute, automatic = true, stage = patch.Stage, x = patch.Bounds.X, z = patch.Bounds.Z, width = patch.Bounds.Width, height = patch.Bounds.Height,
                    patch = blob, patchSha256 = hash, sourceRevision = definition.SourceRevision });
            }
            return records;
        }
        private static void ThrowIfInvalid(WaterBuildResult result)
        { if (result.HasErrors) throw new InvalidOperationException("WaterConflict: " + string.Join(" / ", result.Conflicts)); }
        private static void ValidateExclusions(WaterSystemDefinition source, WorldGrid grid)
        {
            var ids = new HashSet<string> { source.systemGuid, source.worldGuid }; foreach (var body in source.bodies) if (!ids.Add(body.waterGuid)) throw new InvalidOperationException("waterGuid 중복/소유자 충돌");
            foreach (var exclusion in source.exclusions)
            {
                PlacementIdentity.ValidateGuid(exclusion.guid); PlanningGeometry.ValidatePolygon(exclusion.boundary, grid); PlacementIdentity.RequireFinite(exclusion.minY, exclusion.maxY);
                if (!ids.Add(exclusion.guid) || exclusion.minY >= exclusion.maxY) throw new InvalidOperationException("제외 체적 ID/고도 오류");
            }
            foreach (var edge in source.waterfalls) if (!ids.Add(edge.guid)) throw new InvalidOperationException("폭포/수역/제외 ID 충돌");
        }
        private static void ValidateResources(WaterSystemDefinition source)
        {
            foreach (var body in source.bodies)
            {
                var p = body.preset;
                if (p != null) PlacementIdentity.RequireFinite(p.normalScale, p.normalTiling, p.normalScroll, p.depthFade, p.foamWidth, p.foamStrength, p.waveDrift.x, p.waveDrift.y);
                if (p == null || p.material == null || p.material.shader == null || p.material.shader.name != "UPlayGround/WorldBuilder/Water" || !p.material.shader.isSupported || p.normalMap == null
                    || p.footstepProfile == null || p.normalScale < 0 || p.normalTiling <= 0 || p.normalScroll < 0 || p.depthFade <= 0 || p.foamWidth <= 0 || p.foamStrength < 0)
                    throw new InvalidOperationException("수계 URP 표현 리소스/수치 누락: " + body.waterGuid);
            }
            if (source.waterfalls.Count == 0) return;
            var f = source.waterfallPreset;
            if (f != null) PlacementIdentity.RequireFinite(f.minimumDrop, f.poolMargin, f.socketTolerance, f.nearDistance, f.farDistance, f.soundClusterRadius,
                f.soundVolume, f.mistRate, f.sprayRate, f.particleLifetime, f.particleSpeed, f.mistSize, f.spraySize, f.vfxBoundsPadding.x, f.vfxBoundsPadding.y, f.vfxBoundsPadding.z);
            if (f == null || f.curtainMaterial == null || f.foamMaterial == null || f.particleMaterial == null || f.audioClip == null || f.audioClip.samples < 1
                || f.minimumDrop <= 0 || f.poolMargin <= 0 || f.socketTolerance <= 0 || f.farDistance <= f.nearDistance || f.nearDistance < 0 || f.maxNearbySounds < 1
                || f.soundClusterRadius <= 0 || f.soundVolume < 0 || f.mistRate <= 0 || f.sprayRate <= 0 || f.particleLifetime <= 0 || f.particleSpeed <= 0
                || f.vfxBoundsPadding.x <= 0 || f.vfxBoundsPadding.y <= 0 || f.vfxBoundsPadding.z <= 0)
                throw new InvalidOperationException("폭포 프리셋의 Mesh/VFX/실제 AudioClip/거리 수치가 미확보입니다.");
        }
        private static string SourceHash(WaterSystemDefinition source)
        {
            // 적용 기록 필드는 제외해 재생성 자체가 원본 버전을 바꾸지 않게 한다.
            string text = source.systemGuid + source.worldGuid;
            foreach (var body in source.bodies) text += WorldWaterSceneBuilder.StableRecordJson(body) + WorldWaterSceneBuilder.StableJson(body.preset) + WorldWaterSceneBuilder.StableJson(body.preset.material);
            foreach (var edge in source.waterfalls) text += JsonUtility.ToJson(edge);
            foreach (var exclusion in source.exclusions) text += JsonUtility.ToJson(exclusion);
            if (source.waterfallPreset != null)
            {
                var preset = source.waterfallPreset;
                text += WorldWaterSceneBuilder.StableJson(preset) + WorldWaterSceneBuilder.StableJson(preset.curtainMaterial)
                    + WorldWaterSceneBuilder.StableJson(preset.foamMaterial) + WorldWaterSceneBuilder.StableJson(preset.particleMaterial);
            }
            return WorldDefinition.HashBytes(System.Text.Encoding.UTF8.GetBytes(text));
        }
    }
}
