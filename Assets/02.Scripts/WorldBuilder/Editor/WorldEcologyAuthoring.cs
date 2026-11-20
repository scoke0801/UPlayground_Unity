using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KinematicCharacterController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    public static class WorldEcologyAuthoring
    {
        public static Action<string> FailureInjection;

        public static void Regenerate(WorldDefinition definition, WorldEcology source, IReadOnlyList<int> tileOrder = null) =>
            Commit(definition, source, null, tileOrder);

        public static void Move(WorldDefinition definition, WorldEcology source, string caveGuid, Vector3 delta) =>
            Commit(definition, source, () =>
            {
                var cave = FindCave(source, caveGuid);
                foreach (var module in cave.modules) module.position += delta;
                foreach (var prop in cave.props) prop.worldXZ += new Vector2(delta.x, delta.z);
            }, null);

        public static void Delete(WorldDefinition definition, WorldEcology source, string caveGuid) =>
            Commit(definition, source, () => source.caves.Remove(FindCave(source, caveGuid)), null);

        private static CaveLayout FindCave(WorldEcology source, string caveGuid) =>
            source.caves.Find(c => c.caveGuid == caveGuid) ?? throw new CaveConflict(caveGuid, default, "동굴 ID가 없습니다.");

        private static void Commit(WorldDefinition definition, WorldEcology source, Action edit, IReadOnlyList<int> tileOrder)
        {
            var scene = SceneManager.GetActiveScene();
            if (definition == null || source == null || EditorApplication.isPlayingOrWillChangePlaymode ||
                scene.path != AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid))
                throw new InvalidOperationException("대상 월드 미리보기를 Edit Mode에서 여세요.");
            RequireAsset(source);
            if (source.worldGuid != definition.WorldGuid || source.ecologyGuid == definition.WorldGuid ||
                source.ecologyGuid == definition.Planning?.planGuid || source.ecologyGuid == definition.WaterSystem?.systemGuid ||
                (definition.Ecology != null && (definition.Ecology != source || definition.EcologyOwner != source.ecologyGuid)))
                throw new CaveConflict(source.ecologyGuid, default, "월드·생태 소유자 ID 충돌");
            PlacementIdentity.ValidateGuid(source.ecologyGuid);
            var terrains = WorldBuilderAuthoringAssets.FindTerrains(definition);
            using var session = new WorldTerrainAuthoringSession(definition, terrains);
            session.EnsureReadyForBake();
            var preview = EnsureClean(definition, terrains, source.defaultLayer);
            WorldWaterSceneBuilder.EnsureClean(definition);
            var before = session.Terrain.CaptureTiles();
            var vegetationBefore = WorldEcologyTerrain.Capture(terrains);
            var created = new List<string>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("월드 동굴·생태 적용");
            try
            {
                Undo.RegisterCompleteObjectUndo(source, "동굴·생태 원본");
                edit?.Invoke();
                ValidateVegetation(definition, source);
                var grid = definition.Profile.CreateGrid();
                var ops = definition.Operations.Where(r => r.ownerPlanGuid != source.ecologyGuid).Select(r => r.ToCore(definition.SourceRevision)).ToList();
                var terrain = TerrainOperationEvaluator.Build(definition.ReadSource(), grid, ops);
                var caves = CaveEvaluation.Build(definition, source, terrain, ops);
                var reservations = new List<PlacementReservation>();
                var records = new List<TerrainOperationRecord>();
                foreach (var cut in caves.Cuts)
                {
                    records.Add(SaveCut(definition, source, cut, created));
                    var footprint = PlanningGeometry.Footprint(cut.Module.Footprint);
                    var bounds = WorldEcologyMask.Bounds(footprint);
                    reservations.Add(new PlacementReservation {
                        guid = PlanningIdentity.Derive(definition.WorldGuid, cut.Module.Cave.caveGuid, cut.Module.Source.moduleId, "entrance"),
                        ownerPlanGuid = source.ecologyGuid, boundary = footprint,
                        minX = bounds.minX, minZ = bounds.minZ, maxX = bounds.maxX, maxZ = bounds.maxZ
                    });
                }
                FailureInjection?.Invoke("AfterAssets");
                bool first = definition.Ecology == null;
                session.ReplaceOwnedOperations(source.ecologyGuid, records, () =>
                {
                    definition.SetEcology(source, reservations, caves.DryVolumes.ToArray());
                    Undo.FlushUndoRecordObjects();
                }, updateEcology: false);
                FailureInjection?.Invoke("AfterTerrain");
                if (first) WorldWaterSceneBuilder.BindEcology(definition);
                var finalTerrain = session.Terrain.Read();
                VerifyCuts(grid, finalTerrain, caves);
                CaveEvaluation.Build(definition, source, finalTerrain, definition.ReadOperations());
                var output = WorldVegetationEvaluation.Build(definition, source, finalTerrain, tileOrder);
                WorldEcologyTerrain.Apply(terrains, output, source.detailResolution, source.treeDistance, source.detailDistance);
                ApplyScene(definition, source, caves, output, terrains, preview);
                FailureInjection?.Invoke("AfterScene");
                Save(definition, source, terrains);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                WorldPlanningUndoRestore.Track(definition);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            catch (Exception failure)
            {
                try
                {
                    Undo.RevertAllDownToGroup(group);
                    WorldTerrainStateAdapter.RestoreTiles(before);
                    foreach (var snapshot in vegetationBefore) snapshot.Restore();
                    Save(definition, source, terrains);
                    foreach (string path in created)
                    {
                        if (AssetDatabase.DeleteAsset(path)) continue;
                        if (File.Exists(path)) File.Delete(path);
                        if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
                    }
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException("P06 적용 오류 뒤 이전 상태 복구에 실패했습니다.", failure, rollbackFailure);
                }
                throw;
            }
        }

        internal static void RefreshApplied(WorldDefinition definition, TerrainBuildResult terrain, Terrain[,] terrains)
        {
            if (definition.Ecology == null) return;
            var source = definition.Ecology;
            var preview = EnsureClean(definition, terrains);
            if (preview == null) throw new InvalidOperationException("EcologyConflict: 적용 계층이 없습니다.");
            ValidateVegetation(definition, source);
            var grid = definition.Profile.CreateGrid();
            var operations = definition.ReadOperations();
            var caves = CaveEvaluation.Build(definition, source, terrain, operations);
            var groups = preview.GetComponentsInChildren<WorldCaveGroup>(true);
            if (groups.Length != source.caves.Count || source.caves.Any(c => !groups.Any(g => g.caveGuid == c.caveGuid && g.sourceHash == CaveHash(c))))
                throw new InvalidOperationException("EcologyConflict: 변경한 동굴 원본을 먼저 P06에서 적용하세요.");
            VerifyCuts(grid, terrain, caves);
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cut in caves.Cuts)
            {
                string id = CutId(definition, cut);
                expected.Add(id);
                var record = definition.Operations.FirstOrDefault(r => r.operationGuid == id && r.ownerPlanGuid == source.ecologyGuid);
                if (record == null || record.patchSha256 != CutHash(cut))
                    throw cut.Module.Conflict("현재 지형에서 입구 Hole 범위가 바뀝니다. 동굴 원본과 지형을 맞춰야 합니다.");
            }
            if (definition.Operations.Any(r => r.ownerPlanGuid == source.ecologyGuid && !expected.Contains(r.operationGuid)))
                throw new InvalidOperationException("EcologyConflict: 소유 HoleCut이 남아 있습니다.");
            ValidateColliders(source, caves, preview);
            var output = WorldVegetationEvaluation.Build(definition, source, terrain);
            var before = WorldEcologyTerrain.Capture(terrains);
            try
            {
                WorldEcologyTerrain.Apply(terrains, output, source.detailResolution, source.treeDistance, source.detailDistance);
                Undo.RegisterCompleteObjectUndo(preview, "식생 적용 상태");
                preview.trees = output.Trees.ToArray();
                preview.terrainOutputHash = WorldEcologyTerrain.Hash(terrains);
                preview.hierarchyHash = HierarchyHash(preview);
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }
            catch
            {
                foreach (var snapshot in before) snapshot.Restore();
                throw;
            }
        }

        private static void VerifyCuts(WorldGrid grid, TerrainBuildResult terrain, CaveEvaluation caves)
        {
            foreach (var cut in caves.Cuts)
            for (int z = 0; z < cut.Bounds.Height; z++)
            for (int x = 0; x < cut.Bounds.Width; x++)
                if (cut.Masks[z * cut.Bounds.Width + x] > .5f && terrain.GetSurface(cut.Bounds.Z + z, cut.Bounds.X + x))
                    throw new CaveConflict(cut.Module.Source.moduleId, new Vector3((float)grid.SampleToWorld(cut.Bounds.X + x),
                        cut.Module.Low, (float)grid.SampleToWorld(cut.Bounds.Z + z)), "입구 지표가 HoleCut 이후 남아 있습니다.");
        }

        private static string CutId(WorldDefinition definition, CaveEvaluation.Cut cut) =>
            PlanningIdentity.Derive(definition.WorldGuid, cut.Module.Cave.caveGuid, cut.Module.Source.moduleId, "cave-hole");

        private static byte[] CutBytes(CaveEvaluation.Cut cut) =>
            TerrainSnapshotCodec.EncodePatch(cut.Bounds.Width, cut.Bounds.Height, new float[cut.Masks.Length], cut.Masks);

        private static string CutHash(CaveEvaluation.Cut cut) => WorldDefinition.HashBytes(CutBytes(cut));

        private static TerrainOperationRecord SaveCut(WorldDefinition definition, WorldEcology source, CaveEvaluation.Cut cut, List<string> created)
        {
            byte[] bytes = CutBytes(cut);
            string hash = WorldDefinition.HashBytes(bytes);
            string folder = "Assets/WorldBuilder/Worlds/" + definition.WorldGuid + "/Authoring/CaveCuts";
            WorldBuilderAuthoringAssets.EnsureFolder(folder);
            string path = folder + "/" + hash + ".bytes";
            string guid = AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets);
            if (string.IsNullOrEmpty(guid))
            {
                if (File.Exists(path + ".meta")) throw new IOException("HoleCut 경로에 소유자가 확인되지 않은 메타 파일이 있습니다.");
                string temp = folder + "/." + Guid.NewGuid().ToString("N") + ".tmp";
                bool ownsTemp = false;
                try
                {
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        ownsTemp = true;
                        if (FailureInjection == null) file.Write(bytes, 0, bytes.Length);
                        else
                        {
                            file.WriteByte(bytes[0]);
                            FailureInjection("DuringCutWrite");
                            file.Write(bytes, 1, bytes.Length - 1);
                        }
                    }
                    File.Move(temp, path);
                    created.Add(path);
                }
                catch (Exception failure)
                {
                    if (ownsTemp)
                    {
                        try { File.Delete(temp); }
                        catch (IOException cleanupFailure)
                        {
                            throw new AggregateException("HoleCut 임시 파일을 정리하지 못했습니다.", failure, cleanupFailure);
                        }
                    }
                    throw;
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                guid = AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets);
            }
            string resolved = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(resolved);
            if (resolved != path || asset == null || WorldDefinition.HashBytes(asset.bytes) != hash) throw cut.Module.Conflict("HoleCut 패치 GUID·경로·내용 불일치");
            return new TerrainOperationRecord {
                operationGuid = CutId(definition, cut), ownerPlanGuid = source.ecologyGuid, ownerElementGuid = cut.Module.Cave.caveGuid,
                kind = TerrainOperationKind.HoleCut, automatic = true, stage = 45, x = cut.Bounds.X, z = cut.Bounds.Z,
                width = cut.Bounds.Width, height = cut.Bounds.Height, patch = asset, patchSha256 = hash, sourceRevision = definition.SourceRevision
            };
        }

        internal static WorldEcologyPreview EnsureClean(WorldDefinition definition, Terrain[,] terrains, TerrainLayer defaultLayer = null)
        {
            var previews = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorldEcologyPreview>(true))
                .Where(p => p.definition == definition).ToArray();
            if (previews.Length == 0 && definition.Ecology == null)
            {
                foreach (var terrain in terrains)
                {
                    var data = terrain.terrainData;
                    var layers = data.terrainLayers;
                    if (data.treeInstanceCount > 0 || data.treePrototypes.Length > 0 || data.detailPrototypes.Length > 0 || layers.Length > 1 ||
                        (layers.Length == 1 && ((layers[0] != defaultLayer && !AssetDatabase.GetAssetPath(layers[0]).EndsWith("/PreviewSurface.terrainlayer", StringComparison.Ordinal)) ||
                            data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight).Cast<float>().Any(w => w != 1))))
                        throw new InvalidOperationException("UncapturedEcologyEdits: 기존 지표 페인트·나무·Details가 있습니다.");
                }
                return null;
            }
            if (previews.Length != 1 || previews[0].hierarchyHash != HierarchyHash(previews[0]) ||
                previews[0].terrainOutputHash != WorldEcologyTerrain.Hash(terrains))
                throw new InvalidOperationException("UncapturedEcologyEdits: 동굴 계층·alphamap·식생의 미캡처 편집 또는 소유 계층 누락/중복");
            return previews[0];
        }

        private static void ApplyScene(WorldDefinition definition, WorldEcology source, CaveEvaluation caves, WorldVegetationEvaluation output,
            Terrain[,] terrains, WorldEcologyPreview preview)
        {
            if (preview == null)
            {
                var root = new GameObject("WorldBuilder Ecology");
                Undo.RegisterCreatedObjectUndo(root, "월드 동굴·생태");
                preview = Undo.AddComponent<WorldEcologyPreview>(root);
            }
            var byCave = preview.GetComponentsInChildren<WorldCaveGroup>(true).ToDictionary(g => g.caveGuid, StringComparer.Ordinal);
            foreach (var cave in source.caves.OrderBy(c => c.caveGuid, StringComparer.Ordinal))
            {
                string hash = CaveHash(cave);
                if (byCave.Remove(cave.caveGuid, out var existing))
                {
                    if (existing.sourceHash == hash) continue;
                    Undo.DestroyObjectImmediate(existing.gameObject);
                }
                var root = new GameObject("Cave_" + cave.caveGuid);
                Undo.RegisterCreatedObjectUndo(root, "동굴 계층 생성");
                Undo.SetTransformParent(root.transform, preview.transform, "동굴 계층 부모");
                var owner = Undo.AddComponent<WorldCaveGroup>(root);
                Undo.RegisterCompleteObjectUndo(owner, "동굴 소유자");
                owner.caveGuid = cave.caveGuid;
                owner.sourceHash = hash;
                var modules = new Dictionary<string, WorldCaveModule>(StringComparer.Ordinal);
                foreach (var module in caves.Modules.Where(m => m.Cave == cave))
                {
                    var item = module.Source;
                    var node = Instantiate(item.preset.prefab, root.transform, item.position, module.Rotation);
                    var marker = node.GetComponent<WorldCaveModule>();
                    Undo.RegisterCompleteObjectUndo(marker, "동굴 모듈 ID");
                    marker.caveGuid = cave.caveGuid; marker.moduleId = item.moduleId; marker.surfaceId = item.surfaceId;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
                    modules.Add(item.surfaceId, marker);
                    if (!module.Entrance) continue;
                    var portal = module.Ports.Values.Single(p => p.socket.exterior).portal;
                    Instantiate(item.preset.collarPrefab, root.transform, portal.position, Quaternion.LookRotation(portal.outward));
                }
                Physics.SyncTransforms();
                foreach (var prop in cave.props)
                {
                    if (!modules[prop.surfaceId].TryGetFloor(prop.worldXZ, out Vector3 floor))
                        throw new CaveConflict(prop.placementId, new Vector3(prop.worldXZ.x, 0, prop.worldXZ.y), "동굴 바닥 Collider의 지지점을 찾지 못했습니다.");
                    Instantiate(prop.prefab, root.transform, floor + Vector3.up * prop.heightOffset, Quaternion.Euler(0, prop.yaw, 0));
                }
            }
            foreach (var old in byCave.Values) Undo.DestroyObjectImmediate(old.gameObject);
            ValidateColliders(source, caves, preview);
            Undo.RegisterCompleteObjectUndo(preview, "동굴·식생 적용 상태");
            preview.definition = definition;
            preview.dryVolumes = caves.DryVolumes.ToArray();
            preview.modules = preview.GetComponentsInChildren<WorldCaveModule>(true);
            preview.portals = caves.Portals.ToArray();
            preview.trees = output.Trees.ToArray();
            preview.terrainOutputHash = WorldEcologyTerrain.Hash(terrains);
            preview.hierarchyHash = HierarchyHash(preview);
        }

        private static void ValidateColliders(WorldEcology source, CaveEvaluation caves, WorldEcologyPreview preview)
        {
            if (caves.Modules.Count == 0) return;
            var motor = source.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true);
            float radius = motor.Capsule.radius, height = motor.Capsule.height;
            var instances = preview.GetComponentsInChildren<WorldCaveModule>(true).ToDictionary(m => m.moduleId, StringComparer.Ordinal);
            var physics = preview.gameObject.scene.GetPhysicsScene();
            var overlaps = new Collider[16];
            Physics.SyncTransforms();
            foreach (var module in caves.Modules)
            {
                var instance = instances[module.Source.moduleId];
                var center = new Vector3((float)module.Footprint.x, 0, (float)module.Footprint.z);
                foreach (var port in module.Ports.Values)
                {
                    if (port.socket.exterior)
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            var outside = port.portal.position + port.portal.outward * (i * radius * .75f);
                            var low = outside + Vector3.up * (radius + .13f);
                            var high = outside + Vector3.up * (height - radius + .13f);
                            int hitCount = physics.OverlapCapsule(low, high, radius - .015f, overlaps, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
                            for (int j = 0; j < hitCount; j++)
                                if (!(overlaps[j] is TerrainCollider) && !instance.floorColliders.Contains(overlaps[j]))
                                    throw new CaveConflict(port.portal.id, outside, "외부 포털 접근을 막는 Collider · " + overlaps[j].name);
                            if (hitCount == overlaps.Length) throw new CaveConflict(port.portal.id, outside, "외부 포털 충돌 결과 한도 초과");
                        }
                    }
                    var point = port.portal.position - port.portal.outward * .05f;
                    int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(point.x, point.z), new Vector2(center.x, center.z)) / .35f));
                    Vector3 previous = default;
                    for (int i = 0; i <= count; i++)
                    {
                        var sample = Vector3.Lerp(point, center, i / (float)count);
                        if (!instance.TryGetFloor(new Vector2(sample.x, sample.z), out Vector3 floor) ||
                            Mathf.Abs(floor.y - module.Floor(sample.x, sample.z)) > Mathf.Max(.03f, module.Source.preset.positionTolerance) ||
                            (i > 0 && (Mathf.Abs(floor.y - previous.y) > motor.MaxStepHeight ||
                                Mathf.Atan2(Mathf.Abs(floor.y - previous.y), Vector2.Distance(new Vector2(floor.x, floor.z), new Vector2(previous.x, previous.z))) * Mathf.Rad2Deg > motor.MaxStableSlopeAngle)))
                            throw new CaveConflict(port.portal.id, sample, "실제 바닥 Collider 지지·단차·경사 오류");
                        var bottom = floor + Vector3.up * (radius + .07f);
                        var top = floor + Vector3.up * (height - radius + .07f);
                        int hits = physics.OverlapCapsule(bottom, top, radius - .015f, overlaps, motor.CollidableLayers, QueryTriggerInteraction.Ignore);
                        for (int j = 0; j < hits; j++)
                            if (!instance.floorColliders.Contains(overlaps[j]) && !(overlaps[j] is TerrainCollider && port.socket.exterior && i == 0))
                                throw new CaveConflict(port.portal.id, floor, "실제 이동 캡슐이 Collider와 겹칩니다 · " + overlaps[j].name);
                        if (hits == overlaps.Length) throw new CaveConflict(port.portal.id, floor, "이동 캡슐 충돌 결과 한도 초과");
                        previous = floor;
                    }
                }
            }
        }

        private static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var root = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (root == null) throw new InvalidOperationException("동굴 프리팹 인스턴스 생성 실패");
            Undo.RegisterCreatedObjectUndo(root, "동굴 프리팹");
            Undo.RegisterCompleteObjectUndo(root.transform, "동굴 위치");
            root.transform.SetPositionAndRotation(position, rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
            return root;
        }

        private static string CaveHash(CaveLayout cave)
        {
            var text = new StringBuilder(WorldWaterSceneBuilder.StableRecordJson(cave));
            foreach (var item in cave.modules)
            {
                text.Append(WorldWaterSceneBuilder.StableJson(item.preset));
                text.Append(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(item.preset.prefab)));
                if (item.preset.collarPrefab != null) text.Append(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(item.preset.collarPrefab)));
            }
            return WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(text.ToString()));
        }

        private static string HierarchyHash(WorldEcologyPreview preview)
        {
            var text = new StringBuilder();
            foreach (var transform in preview.GetComponentsInChildren<Transform>(true))
            {
                text.Append(transform.name).Append(transform.gameObject.activeSelf);
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component == null) { text.Append("MissingScript"); continue; }
                    string json = WorldWaterSceneBuilder.StableJson(component);
                    if (component == preview) json = Regex.Replace(json, "\"hierarchyHash\":\"[^\"]*\"", "\"hierarchyHash\":\"\"");
                    text.Append(component.GetType().FullName).Append(json);
                }
            }
            return WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(text.ToString()));
        }

        internal static void RequireAsset(UnityEngine.Object value)
        {
            if (value == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id) ||
                string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) ||
                !AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).Any(asset =>
                    asset == value && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string currentGuid, out long currentId) && currentGuid == guid && currentId == id))
                throw new InvalidOperationException("생태 자산 GUID·경로·타입을 확인할 수 없습니다.");
        }

        private static void ValidateVegetation(WorldDefinition definition, WorldEcology source)
        {
            RequireAsset(source.defaultLayer);
            if (source.alphaResolution < 16 || source.alphaResolution > 512 || !Mathf.IsPowerOfTwo(source.alphaResolution) ||
                source.detailResolution < 8 || source.detailResolution > 512 || source.detailResolution % 8 != 0 ||
                !float.IsFinite(source.humidity) || source.humidity < 0 || source.humidity > 1 ||
                !float.IsFinite(source.biomeBlendDistance) || source.biomeBlendDistance < 0 ||
                !float.IsFinite(source.treeDistance) || source.treeDistance < 0 || !float.IsFinite(source.detailDistance) || source.detailDistance < 0)
                throw new InvalidOperationException("생태 해상도·습도·바이옴 전이·표시 거리 범위 오류");
            var grid = definition.Profile.CreateGrid();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var biome in source.biomes)
            {
                RequireAsset(biome); RequireAsset(biome.layer);
                PlacementIdentity.ValidateGuid(biome.biomeId);
                if (!ids.Add(biome.biomeId)) throw new InvalidOperationException("바이옴 ID 중복");
                foreach (var range in new[] { biome.height, biome.slope, biome.shoreDistance, biome.humidity })
                {
                    PlacementIdentity.RequireFinite(range.x, range.y);
                    if (range.x > range.y) throw new InvalidOperationException("바이옴 조건 범위 오류");
                }
                if (biome.mask != null && biome.mask.Length > 0)
                    PlanningGeometry.ValidatePolygon(biome.mask.Select(p => new PlanPoint(p.x, 0, p.y)).ToArray(), grid);
            }
            var prefabs = new HashSet<GameObject>();
            foreach (var vegetation in source.vegetation)
            {
                RequireAsset(vegetation); RequireAsset(vegetation.prefab);
                PlacementIdentity.ValidateGuid(vegetation.prototypeId);
                if (!ids.Add(vegetation.prototypeId) || !prefabs.Add(vegetation.prefab) || !source.biomes.Contains(vegetation.biome))
                    throw new InvalidOperationException("식생 ID·리소스 중복 또는 바이옴 누락");
                PlacementIdentity.RequireFinite(vegetation.density, vegetation.spacing, vegetation.footprintRadius, vegetation.scale.x, vegetation.scale.y);
                if (vegetation.density < 0 || vegetation.density > 1 || vegetation.spacing <= 0 || vegetation.footprintRadius <= 0 ||
                    vegetation.scale.x <= 0 || vegetation.scale.x > vegetation.scale.y ||
                    (vegetation.path == VegetationPath.TerrainTree && Math.Pow(Math.Ceiling(grid.WorldSize / vegetation.spacing), 2) > 1000000))
                    throw new InvalidOperationException("식생 밀도·간격·크기·후보 한도 오류");
                if (vegetation.path == VegetationPath.Prefab)
                {
                    RequireAsset(vegetation.placementRule);
                    if (definition.PlacementCatalog == null || definition.PlacementCatalog.Resolve(vegetation.placementRule.prefabGuid) != vegetation.prefab)
                        throw new InvalidOperationException("식생 P03 카탈로그 참조 불일치");
                }
            }
        }

        private static void Save(WorldDefinition definition, WorldEcology source, Terrain[,] terrains)
        {
            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssetIfDirty(definition);
            AssetDatabase.SaveAssetIfDirty(source);
            foreach (var terrain in terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
        }
    }
}

