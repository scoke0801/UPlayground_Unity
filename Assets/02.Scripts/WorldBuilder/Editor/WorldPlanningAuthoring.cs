using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Unity.Mathematics;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>계획 SO·P02 패치·P03 배치·도로/구조물을 하나의 Undo 작업으로 확정한다.</summary>
    public static class WorldPlanningAuthoring
    {
        public static WorldPlanningResult LastResult { get; private set; }
        /// <summary>월드별 경로의 GUID를 확인하며 기존 계획을 덮어쓰지 않는다.</summary>
        public static WorldPlan CreatePlan(WorldDefinition definition)
        {
            if (definition.Planning != null) return definition.Planning;
            string folder = "Assets/10.Datas/WorldBuilder"; WorldBuilderAuthoringAssets.EnsureFolder(folder);
            string path = folder + "/" + definition.WorldGuid + "_Plan.asset";
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                var existing = AssetDatabase.LoadAssetAtPath<WorldPlan>(AssetDatabase.GUIDToAssetPath(guid));
                if (existing == null || existing.worldGuid != definition.WorldGuid) throw new InvalidOperationException("기존 계획 자산의 타입/소유 월드가 다릅니다.");
                return existing;
            }
            var plan = ScriptableObject.CreateInstance<WorldPlan>(); plan.InitializeForWorld(definition.WorldGuid); AssetDatabase.CreateAsset(plan, path); return plan;
        }
        public static WorldPlanningResult Evaluate(WorldDefinition definition, WorldPlan plan)
        {
            LastResult = null;
            var records = ReadUserOperations(definition, plan); var operations = new List<TerrainOperation>();
            foreach (var record in records) operations.Add(record.ToCore(definition.SourceRevision));
            var regions = new List<PlanRegion>(); foreach (var region in plan.regions) regions.Add(region.ToCore());
            var structures = new List<StructurePlan>(); foreach (var structure in plan.structures) { ValidateCatalogEntry(structure.preset.Entry); structures.Add(structure.ToCore()); }
            var roads = new List<RoadPlan>(); foreach (var road in plan.roads) { if (road.preset == null || road.preset.material == null) throw new InvalidOperationException("도로 표현 재질이 필요합니다."); roads.Add(road.ToCore()); }
            var bridges = new List<BridgePlan>(); foreach (var bridge in plan.bridges) { ValidateCatalogEntry(bridge.preset.Entry); bridges.Add(bridge.ToCore()); }
            var fixedPlacements = new List<PlacementRecord>(); foreach (var p in definition.Placements) if (p.isFixed) fixedPlacements.Add(p.Clone());
            var grid = definition.Profile.CreateGrid();
            LastResult = WorldPlanningEngine.Build(definition.WorldGuid, plan.planGuid, definition.ReadSource(), grid, definition.Profile.TileCells * grid.SampleSpacing,
                operations, regions, plan.pois, structures, roads, plan.reservations, bridges, fixedPlacements);
            return LastResult;
        }
        public static WorldPlanningResult Regenerate(WorldDefinition definition, WorldPlan plan) => Commit(definition, plan, null, null);
        /// <summary>적용 패치를 제외한 부지 내부 중앙 고도를 제안하며 원본과 씬은 수정하지 않는다.</summary>
        public static PlanPoint ProposePoiElevation(WorldDefinition definition, string poiGuid, PlanPoint anchor)
        {
            var plan = RequirePlan(definition); var operations = new List<TerrainOperation>();
            foreach (var record in ReadUserOperations(definition, plan)) operations.Add(record.ToCore(definition.SourceRevision));
            var terrain = TerrainOperationEvaluator.Build(definition.ReadSource(), definition.Profile.CreateGrid(), operations);
            var heights = new List<double>();
            foreach (var structure in plan.structures) if (structure.poiGuid == poiGuid)
            {
                var core = structure.ToCore();
                var footprint = new PlacementRecord { x = anchor.x + core.Offset.x, z = anchor.z + core.Offset.z, yaw = core.Yaw, halfX = core.HalfX, halfZ = core.HalfZ };
                heights.Add(WorldPlanningEngine.ProposeElevation(footprint, definition.Profile.CreateGrid(), terrain) - core.Offset.y);
            }
            if (heights.Count == 0) throw new InvalidOperationException("고도를 제안할 POI의 건물 footprint가 없습니다.");
            heights.Sort(); return new PlanPoint(anchor.x, heights[heights.Count / 2], anchor.z);
        }
        public static WorldPlanningResult MovePoi(WorldDefinition definition, string poiGuid, PlanPoint anchor)
        {
            var plan = RequirePlan(definition);
            return Commit(definition, plan, () =>
            {
                var poi = plan.pois.Find(p => p.guid == poiGuid) ?? throw new InvalidOperationException("대상 POI가 없습니다.");
                poi.anchor = anchor; poi.hasConfirmedElevation = true;
            }, null);
        }
        public static WorldPlanningResult CaptureSpline(WorldDefinition definition, string roadGuid)
        {
            var plan = RequirePlan(definition); WorldRoadPreview preview = null;
            foreach (var root in Roots(definition)) foreach (var road in root.GetComponentsInChildren<WorldRoadPreview>(true)) if (road.roadGuid == roadGuid) preview = road;
            if (preview == null) throw new InvalidOperationException("현재 계획의 도로 Spline을 선택하세요.");
            var knots = ReadSpline(preview.GetComponent<SplineContainer>());
            return Commit(definition, plan, () =>
            {
                var road = plan.roads.Find(r => r.roadGuid == roadGuid) ?? throw new InvalidOperationException("도로 ID가 없습니다.");
                road.knots = knots; road.isLocked = true;
            }, roadGuid);
        }
        public static WorldPlanningResult ClearGeneratedPlan(WorldDefinition definition)
        {
            var plan = RequirePlan(definition);
            return Commit(definition, plan, () => { plan.structures.Clear(); plan.roads.Clear(); plan.bridges.Clear(); }, null);
        }
        private static WorldPlan RequirePlan(WorldDefinition definition) => definition != null && definition.Planning != null ? definition.Planning : throw new InvalidOperationException("WorldPlan을 먼저 연결하세요.");
        private static WorldPlanningResult Commit(WorldDefinition definition, WorldPlan plan, Action mutation, string allowedSplineRoadGuid)
        {
            if (definition == null || plan == null || EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path) != definition.PreviewSceneGuid)
                throw new InvalidOperationException("대상 월드 미리보기를 Edit Mode에서 여세요.");
            if (definition.Planning != null && definition.Planning != plan) throw new InvalidOperationException("기존 계획은 자동 교체하지 않습니다.");
            EnsurePreviewClean(definition, plan, allowedSplineRoadGuid);
            var created = new List<string>();
            using (var session = new WorldTerrainAuthoringSession(definition, WorldBuilderAuthoringAssets.FindTerrains(definition)))
            {
                session.EnsureReadyForBake(); var before = session.Terrain.CaptureTiles();
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                try
                {
                    Undo.RegisterCompleteObjectUndo(plan, "월드 계획 저작"); plan.InitializeForWorld(definition.WorldGuid); mutation?.Invoke();
                    var result = Evaluate(definition, plan);
                    if (result.HasErrors) throw new InvalidOperationException("PlanningConflict: " + string.Join(" / ", result.Diagnostics.ConvertAll(d => d.elementGuid + " " + d.issue + " " + d.message)));
                    var patches = SavePatches(definition, plan, result, created);
                    session.ReplaceOwnedOperations(plan.planGuid, patches, () => { Undo.RegisterCompleteObjectUndo(plan, "계획 적용 상태 기록"); definition.SetPlanning(plan, result.Reservations); plan.SetApplied(result); Undo.FlushUndoRecordObjects(); });
                    ApplyScene(definition, plan, result, created);
                    EditorUtility.SetDirty(definition); EditorUtility.SetDirty(plan); AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(plan);
                    Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); WorldPlanningUndoRestore.Track(definition); return result;
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group); WorldTerrainStateAdapter.RestoreTiles(before);
                    foreach (var terrain in session.Terrain.Terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                    EditorUtility.SetDirty(definition); EditorUtility.SetDirty(plan); AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(plan);
                    foreach (string path in created) AssetDatabase.DeleteAsset(path);
                    throw;
                }
            }
        }
        private static List<TerrainOperationRecord> ReadUserOperations(WorldDefinition definition, WorldPlan plan)
        {
            var result = new List<TerrainOperationRecord>();
            foreach (var record in definition.Operations) if (record.ownerPlanGuid != plan.planGuid) result.Add(record.Clone());
            return result;
        }
        private static List<TerrainOperationRecord> SavePatches(WorldDefinition definition, WorldPlan plan, WorldPlanningResult result, List<string> created)
        {
            string folder = WorldFolder(definition) + "/Authoring/PlanningPatches"; WorldBuilderAuthoringAssets.EnsureFolder(folder);
            var records = new List<TerrainOperationRecord>();
            foreach (var patch in result.Patches)
            {
                byte[] bytes = TerrainSnapshotCodec.EncodePatch(patch.Bounds.Width, patch.Bounds.Height, patch.Values, patch.Masks);
                string hash = WorldDefinition.HashBytes(bytes), path = folder + "/" + hash + ".bytes";
                if (!File.Exists(path)) { File.WriteAllBytes(path, bytes); created.Add(path); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); }
                var blob = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (blob == null || WorldDefinition.HashBytes(blob.bytes) != hash) throw new IOException("계획 패치 무결성 오류: " + path);
                records.Add(new TerrainOperationRecord { operationGuid = patch.OperationGuid, ownerPlanGuid = plan.planGuid, ownerElementGuid = patch.ElementGuid,
                    kind = TerrainOperationKind.SetAbsolute, automatic = true, stage = patch.Stage, x = patch.Bounds.X, z = patch.Bounds.Z, width = patch.Bounds.Width, height = patch.Bounds.Height,
                    patch = blob, patchSha256 = hash, sourceRevision = definition.SourceRevision });
            }
            return records;
        }
        private static void ApplyScene(WorldDefinition definition, WorldPlan plan, WorldPlanningResult result, List<string> created)
        {
            foreach (var old in Roots(definition)) Undo.DestroyObjectImmediate(old.gameObject);
            var container = new GameObject("WorldBuilder Plan"); Undo.RegisterCreatedObjectUndo(container, "계획 계층 생성");
            var marker = Undo.AddComponent<WorldPlanningPreview>(container); Undo.RegisterCompleteObjectUndo(marker, "계획 소유권 기록"); marker.definition = definition; marker.plan = plan; marker.appliedHash = plan.AppliedHash;
            foreach (var structure in result.Structures)
            {
                GameObject prefab = ResolvePrefab(plan, structure.Placement.prefabGuid);
                var go = PrefabUtility.InstantiatePrefab(prefab, container.transform) as GameObject;
                if (go == null) throw new InvalidOperationException("구조물 생성 실패");
                Undo.RegisterCreatedObjectUndo(go, "구조물 생성"); Undo.RegisterFullObjectHierarchyUndo(go, "구조물 변환 기록");
                var p = structure.Placement; go.transform.SetPositionAndRotation(Vector(p.x, p.y, p.z), Quaternion.Euler(0, (float)p.yaw, 0)); go.transform.localScale = Vector3.one;
                var instance = Undo.AddComponent<WorldPlanningInstance>(go); Undo.RegisterCompleteObjectUndo(instance, "구조물 ID 기록"); instance.definition = definition; instance.planGuid = plan.planGuid; instance.record = p.Clone();
            }
            foreach (var road in result.Roads)
            {
                var record = plan.roads.Find(r => r.roadGuid == road.Guid);
                var go = new GameObject("WB_P04_Road_" + road.Guid); Undo.RegisterCreatedObjectUndo(go, "도로 생성"); Undo.SetTransformParent(go.transform, container.transform, "도로 부모 기록");
                var spline = Undo.AddComponent<SplineContainer>(go); Undo.RegisterCompleteObjectUndo(spline, "도로 Spline 기록");
                var native = new Spline(); foreach (var knot in road.Knots) native.Add(new BezierKnot(Float(knot.position), Float(knot.tangentIn), Float(knot.tangentOut)), TangentMode.Broken);
                spline.Splines = new[] { native };
                var preview = Undo.AddComponent<WorldRoadPreview>(go); Undo.RegisterCompleteObjectUndo(preview, "도로 저작 스냅샷 기록"); preview.roadGuid = road.Guid; preview.appliedKnots = ReadSpline(spline); preview.chunkCount = road.Chunks.Count;
                int index = 0;
                foreach (var chunk in road.Chunks)
                {
                    var mesh = SaveMesh(definition, chunk, created);
                    var child = new GameObject("Chunk_" + chunk.TileX + "_" + chunk.TileZ + "_" + index++); Undo.RegisterCreatedObjectUndo(child, "도로 청크 생성"); Undo.SetTransformParent(child.transform, go.transform, "도로 청크 부모 기록");
                    var filter = Undo.AddComponent<MeshFilter>(child); Undo.RegisterCompleteObjectUndo(filter, "도로 메시 기록"); filter.sharedMesh = mesh;
                    var renderer = Undo.AddComponent<MeshRenderer>(child); Undo.RegisterCompleteObjectUndo(renderer, "도로 재질 기록"); renderer.sharedMaterial = record.preset.material;
                    var collider = Undo.AddComponent<MeshCollider>(child); Undo.RegisterCompleteObjectUndo(collider, "도로 충돌 기록"); collider.sharedMesh = mesh;
                }
                Undo.RegisterCompleteObjectUndo(preview, "도로 메시 스냅샷 기록"); preview.meshHash = HashRoad(go);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        private static Mesh SaveMesh(WorldDefinition definition, RoadChunk chunk, List<string> created)
        {
            var mesh = new Mesh { name = "WorldBuilder Road", indexFormat = chunk.Vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            var vertices = new Vector3[chunk.Vertices.Length]; var normals = new Vector3[vertices.Length]; var tangents = new Vector4[vertices.Length]; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = chunk.Vertices[i]; vertices[i] = Vector(v.Position.x, v.Position.y, v.Position.z); normals[i] = Vector(v.Normal.x, v.Normal.y, v.Normal.z); tangents[i] = new Vector4((float)v.Tangent.x, (float)v.Tangent.y, (float)v.Tangent.z, -1); uv[i] = new Vector2((float)v.U, (float)v.V);
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.tangents = tangents; mesh.uv = uv; mesh.triangles = chunk.Triangles; mesh.RecalculateBounds();
            string folder = WorldFolder(definition) + "/Authoring/PlanningMeshes"; WorldBuilderAuthoringAssets.EnsureFolder(folder);
            string hash = HashMesh(mesh), path = folder + "/" + hash + ".asset";
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                string resolved = AssetDatabase.GUIDToAssetPath(guid); var existing = AssetDatabase.LoadAssetAtPath<Mesh>(resolved);
                UnityEngine.Object.DestroyImmediate(mesh);
                if (resolved != path || existing == null || HashMesh(existing) != hash) throw new IOException("파생 도로 메시 경로의 GUID/타입/내용이 다릅니다. 기존 자산을 보존합니다.");
                return existing;
            }
            if (File.Exists(path)) { UnityEngine.Object.DestroyImmediate(mesh); throw new IOException("아직 식별되지 않은 기존 도로 메시 파일을 덮어쓸 수 없습니다."); }
            AssetDatabase.CreateAsset(mesh, path); created.Add(path); return mesh;
        }
        private static string HashMesh(Mesh mesh)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                foreach (var v in mesh.vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
                foreach (var v in mesh.normals) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
                foreach (var v in mesh.tangents) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); writer.Write(v.w); }
                foreach (var v in mesh.uv) { writer.Write(v.x); writer.Write(v.y); }
                foreach (int triangle in mesh.triangles) writer.Write(triangle); writer.Flush(); return WorldDefinition.HashBytes(stream.ToArray());
            }
        }
        private static string HashRoad(GameObject root)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                foreach (Transform child in root.transform)
                {
                    if (child.childCount != 0 || child.GetComponents<Component>().Length != 4 || child.localPosition != Vector3.zero || child.localRotation != Quaternion.identity || child.localScale != Vector3.one) throw new InvalidOperationException("UncapturedPlanningEdits: 도로 청크 계층/변환 변경을 먼저 Undo하세요.");
                    var filter = child.GetComponent<MeshFilter>(); var collider = child.GetComponent<MeshCollider>(); var renderer = child.GetComponent<MeshRenderer>();
                    if (filter == null || renderer == null || collider == null || filter.sharedMesh == null || filter.sharedMesh != collider.sharedMesh || renderer.sharedMaterials.Length != 1 || !collider.enabled || !renderer.enabled)
                        throw new InvalidOperationException("UncapturedPlanningEdits: 도로 충돌/표현 변경을 먼저 Undo하세요.");
                    writer.Write(child.name); writer.Write(HashMesh(filter.sharedMesh)); writer.Write(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderer.sharedMaterial)));
                }
                writer.Flush(); return WorldDefinition.HashBytes(stream.ToArray());
            }
        }
        public static RoadKnot[] ReadSpline(SplineContainer container)
        {
            if (container == null || container.Splines.Count != 1 || container.Spline == null || container.Spline.Closed) throw new InvalidOperationException("열린 Spline1개만 도로로 캡처할 수 있습니다.");
            var result = new RoadKnot[container.Spline.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var k = container.Spline[i]; var up = math.mul(k.Rotation, new float3(0, 1, 0));
                if (math.dot(up, new float3(0, 1, 0)) < .9999f) throw new InvalidOperationException("P04 도로는 뱅킹 없는 상방 프레임만 지원합니다. knot roll/pitch 대신 접선으로 높이를 편집하세요.");
                var a = math.mul(k.Rotation, k.TangentIn); var b = math.mul(k.Rotation, k.TangentOut);
                result[i] = new RoadKnot { position = Point(k.Position), tangentIn = Point(a), tangentOut = Point(b) };
            }
            return result;
        }
        private static void EnsurePreviewClean(WorldDefinition definition, WorldPlan plan, string allowedSplineRoadGuid)
        {
            var expected = new Dictionary<string, PlacementRecord>(StringComparer.Ordinal); foreach (var p in plan.AppliedStructures) expected.Add(p.id, p);
            var seen = new HashSet<string>(StringComparer.Ordinal); var seenRoads = new HashSet<string>(StringComparer.Ordinal);
            var roots = Roots(definition);
            foreach (var root in roots)
            {
                if (root.plan != plan || root.appliedHash != plan.AppliedHash || root.GetComponents<Component>().Length != 2 || !Identity(root.transform)) throw new InvalidOperationException("UncapturedPlanningEdits: 계획 계층 메타데이터/변환 변경을 먼저 Undo하세요.");
                foreach (Transform child in root.transform)
                {
                    var instance = child.GetComponent<WorldPlanningInstance>(); var road = child.GetComponent<WorldRoadPreview>();
                    if (instance != null)
                    {
                        var p = instance.record;
                        if (p == null || !seen.Add(p.id) || !expected.TryGetValue(p.id, out var saved) || JsonUtility.ToJson(p) != JsonUtility.ToJson(saved) || instance.definition != definition || instance.planGuid != plan.planGuid)
                            throw new InvalidOperationException("UncapturedPlanningEdits: 구조물 정체성이 다릅니다.");
                        if (Vector3.Distance(child.position, Vector(p.x, p.y, p.z)) > .001f || Quaternion.Angle(child.rotation, Quaternion.Euler(0, (float)p.yaw, 0)) > .01f || child.localScale != Vector3.one)
                            throw new InvalidOperationException("UncapturedPlanningEdits: 구조물 변환을 Undo한 뒤 POI/프리셋에서 편집하세요.");
                        WorldPlacementAuthoring.EnsurePrefabUnmodified(child.gameObject, instance);
                    }
                    else if (road != null)
                    {
                        if (!seenRoads.Add(road.roadGuid)) throw new InvalidOperationException("도로 ID 중복입니다.");
                        if (!Identity(child) || child.GetComponents<Component>().Length != 3 || child.childCount != road.chunkCount || HashRoad(child.gameObject) != road.meshHash)
                            throw new InvalidOperationException("UncapturedPlanningEdits: 도로 계층/메시 변경을 먼저 Undo하세요.");
                        var current = ReadSpline(child.GetComponent<SplineContainer>());
                        if (road.roadGuid != allowedSplineRoadGuid && !SameKnots(current, road.appliedKnots)) throw new InvalidOperationException("UncapturedSplineEdits: 먼저 Spline 캡처/확정 또는 Undo하세요.");
                    }
                    else throw new InvalidOperationException("UncapturedPlanningEdits: 생성 컨테이너의 추가 객체를 밖으로 옮기세요.");
                }
            }
            if (!seenRoads.SetEquals(plan.AppliedRoadGuids) || seen.Count != expected.Count || (plan.AppliedHash != null && plan.AppliedHash.Length > 0 && roots.Count != 1)) throw new InvalidOperationException("UncapturedPlanningEdits: 구조물/계획 계층이 누락됐습니다.");
        }
        private static bool SameKnots(RoadKnot[] a, RoadKnot[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if ((a[i].position - b[i].position).Length > .00001 || (a[i].tangentIn - b[i].tangentIn).Length > .00001 || (a[i].tangentOut - b[i].tangentOut).Length > .00001) return false;
            return true;
        }
        private static bool Identity(Transform t) => t.localPosition == Vector3.zero && t.localRotation == Quaternion.identity && t.localScale == Vector3.one;
        private static List<WorldPlanningPreview> Roots(WorldDefinition definition)
        {
            var result = new List<WorldPlanningPreview>(); foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var root in go.GetComponentsInChildren<WorldPlanningPreview>(true)) if (root.definition == definition) result.Add(root); return result;
        }
        private static void ValidateCatalogEntry(PlacementCatalog.Entry entry)
        {
            if (entry.prefab == null || !PrefabUtility.IsPartOfPrefabAsset(entry.prefab) || AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.prefab)) != entry.prefabGuid) throw new InvalidOperationException("구조물 GUID와 프리팹 참조가 다릅니다.");
            if (entry.prefab.transform.localScale != Vector3.one || entry.prefab.transform.localRotation != Quaternion.identity) throw new InvalidOperationException("구조물 프리팹 root는 단위 배율·회전이어야 합니다.");
        }
        private static GameObject ResolvePrefab(WorldPlan plan, string guid)
        {
            foreach (var s in plan.structures) if (s.preset.prefabGuid == guid) return s.preset.Entry.prefab;
            foreach (var b in plan.bridges) if (b.preset.prefabGuid == guid) return b.preset.Entry.prefab;
            throw new InvalidOperationException("구조물 프리팹을 찾지 못했습니다.");
        }
        private static string WorldFolder(WorldDefinition definition) => "Assets/WorldBuilder/Worlds/" + definition.WorldGuid;
        private static Vector3 Vector(double x, double y, double z) => new Vector3((float)x, (float)y, (float)z);
        private static float3 Float(PlanPoint p) => new float3((float)p.x, (float)p.y, (float)p.z);
        private static PlanPoint Point(float3 p) => new PlanPoint(p.x, p.y, p.z);
    }
}

