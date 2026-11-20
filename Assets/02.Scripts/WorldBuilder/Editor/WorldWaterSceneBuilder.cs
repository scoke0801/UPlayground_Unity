using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>검증된 수면·체적·별도 폭포 ribbon을 소유 계층 안에만 생성한다.</summary>
    internal static class WorldWaterSceneBuilder
    {
        internal static void EnsureClean(WorldDefinition definition)
        {
            int roots = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var query in root.GetComponentsInChildren<WorldWaterQuery>(true))
            {
                if (query.definition != definition) continue; roots++;
                if (query.source != definition.WaterSystem || query.appliedHash != query.source.AppliedHash || query.hierarchyHash != HierarchyHash(query))
                    throw new InvalidOperationException("UncapturedWaterEdits: 수계 파생 계층의 수동 변경을 먼저 Undo하거나 소유 계층 밖으로 옮기세요. " + DescribeDifference(query));
            }
            if (definition.WaterSystem != null && !string.IsNullOrEmpty(definition.WaterSystem.AppliedHash) && roots != 1)
                throw new InvalidOperationException("UncapturedWaterEdits: 적용된 수계 계층이 누락/중복됐습니다.");
        }
        internal static void Apply(WorldDefinition definition, WaterSystemDefinition source, WaterBuildResult result, Terrain[,] terrainGrid, List<string> created)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var old in root.GetComponentsInChildren<WorldWaterQuery>(true))
                if (old.definition == definition) Undo.DestroyObjectImmediate(old.gameObject);
            var container = Create("WorldBuilder Water", null); var query = Add<WorldWaterQuery>(container);
            query.definition = definition; query.source = source; query.appliedHash = source.AppliedHash;
            var terrains = new List<Terrain>(); foreach (var terrain in terrainGrid) terrains.Add(terrain);
            var volumes = new List<WaterVolume>();
            foreach (var body in result.Bodies)
            {
                var node = Create("Water_" + body.guid, container.transform); var volume = Add<WaterVolume>(node);
                volume.Initialize(body, source.exclusions.ToArray(), terrains.ToArray()); volumes.Add(volume);
                if (definition.Ecology != null) volume.BindEcology(definition);
                var preset = source.bodies.Find(b => b.waterGuid == body.guid).preset;
                var material = new Material(preset.material); preset.Apply(material); material.name = "P05 Water";
                material = SaveAsset(definition, material, StableJson(material), created);
                foreach (var chunk in result.Chunks)
                {
                    if (chunk.BodyGuid != body.guid) continue;
                    var mesh = Mesh(chunk, "Water"); mesh = SaveAsset(definition, mesh, MeshHash(mesh), created);
                    Surface(Create("Tile_" + chunk.TileX + "_" + chunk.TileZ, node.transform), mesh, material);
                }
                var adapter = Add<FootstepWaterSurface>(node); adapter.waterQuerySource = volume; adapter.priority = body.priority; adapter.profile = preset.footstepProfile;
                if (node.transform.childCount > 0) adapter.surfaceMesh = node.transform.GetChild(0).GetComponent<MeshFilter>();
                adapter.RefreshMesh();
            }
            Undo.RegisterCompleteObjectUndo(query, "수계 체적 목록 기록"); query.volumes = volumes.ToArray();
            var effects = Add<WorldWaterfallEffects>(container); effects.preset = source.waterfallPreset;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (effects.observer == null) effects.observer = root.GetComponentInChildren<Camera>();
            var emitters = new List<WorldWaterfallEffects.Emitter>();
            var ordered = new List<WaterfallEdge>(source.waterfalls); ordered.Sort((a, b) => string.CompareOrdinal(a.guid, b.guid));
            foreach (var edge in ordered) emitters.Add(CreateFall(definition, edge, source.waterfallPreset, container.transform, created));
            Undo.RegisterCompleteObjectUndo(effects, "폭포 emitter 목록 기록"); effects.emitters = emitters.ToArray();
            Undo.RegisterCompleteObjectUndo(query, "수계 계층 지문 기록"); query.hierarchyHash = HierarchyHash(query);
            string cache = "Library/WorldBuilder/" + definition.WorldGuid + "/WaterHierarchy"; Directory.CreateDirectory(cache);
            File.WriteAllText(cache + "/" + query.hierarchyHash + ".txt", HierarchySnapshot(query));
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        internal static void BindEcology(WorldDefinition definition)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var query in root.GetComponentsInChildren<WorldWaterQuery>(true))
            {
                if (query.definition != definition) continue;
                foreach (var volume in query.volumes)
                {
                    Undo.RegisterCompleteObjectUndo(volume, "동굴 물리 제외 연결");
                    volume.BindEcology(definition);
                }
                Undo.RegisterCompleteObjectUndo(query, "수계 계층 지문 기록");
                query.hierarchyHash = HierarchyHash(query);
            }
        }
        private static WorldWaterfallEffects.Emitter CreateFall(WorldDefinition definition, WaterfallEdge edge, WaterfallPreset preset, Transform parent, List<string> created)
        {
            var root = Create("Waterfall_" + edge.guid, parent); var upper = Create("UpperLip", root.transform); upper.transform.position = Vector(edge.upperLip);
            var landing = Create("LowerLanding", root.transform); landing.transform.position = Vector(edge.lowerLanding);
            var direction = WaterGeometry.HorizontalDirection(edge.lowerLanding - edge.upperLip); if (direction.Length < .001) direction = new PlanPoint(0, 0, 1);
            var across = PlanPoint.Cross(new PlanPoint(0, 1, 0), direction); int sections = Math.Max(2, (int)Math.Ceiling((edge.upperLip - edge.lowerLanding).Length));
            var chunk = new WaterChunk();
            for (int i = 0; i <= sections; i++)
            {
                double t = (double)i / sections; var center = PlanPoint.Lerp(edge.upperLip, edge.lowerLanding, t);
                for (int side = 0; side < 2; side++) chunk.Vertices.Add(new WaterVertex { Position = center + across * ((side - .5) * edge.width), U = side, V = t * (edge.upperLip - edge.lowerLanding).Length });
                if (i == 0) continue; int a = 2 * (i - 1); chunk.Triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var curtain = Mesh(chunk, "WaterfallCurtain");
            if (preset.curtainMaterial.HasProperty("_Wobble")) { var bounds = curtain.bounds; bounds.Expand(Vector3.right * (2 * Mathf.Abs(preset.curtainMaterial.GetFloat("_Wobble")))); curtain.bounds = bounds; }
            curtain = SaveAsset(definition, curtain, MeshHash(curtain), created);
            Surface(Create("Curtain", root.transform), curtain, preset.curtainMaterial);
            Foam(definition, upper.transform, across, direction, edge.width, preset.foamMaterial, created);
            Foam(definition, landing.transform, across, direction, edge.width, preset.foamMaterial, created);
            var mist = Particle(Create("Mist", landing.transform), preset, edge.width, true);
            var spray = Particle(Create("Spray", upper.transform), preset, edge.width, false);
            // 실제 ribbon 전체와 입자의 이동 거리를 덮는 명시 bounds다. 먼 카메라에서는 emission도0이 된다.
            foreach (var ps in new[] { mist, spray })
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                Undo.RegisterCompleteObjectUndo(renderer, "폭포 VFX 표현/bounds 기록"); renderer.sharedMaterial = preset.particleMaterial;
                var bounds = new Bounds(ps.transform.InverseTransformPoint(Vector(edge.upperLip)), Vector3.zero); bounds.Encapsulate(ps.transform.InverseTransformPoint(Vector(edge.lowerLanding)));
                float travel = preset.particleSpeed * preset.particleLifetime;
                float gravity = .5f * Physics.gravity.magnitude * preset.particleLifetime * preset.particleLifetime;
                float radius = (float)edge.width / 2 + travel + gravity + Mathf.Max(preset.mistSize, preset.spraySize);
                bounds.Expand(preset.vfxBoundsPadding * 2 + Vector3.one * radius * 2);
                renderer.localBounds = bounds;
            }
            var audio = Add<AudioSource>(landing); audio.clip = preset.audioClip; audio.loop = true; audio.playOnAwake = false; audio.spatialBlend = 1;
            audio.minDistance = preset.nearDistance; audio.maxDistance = preset.farDistance; audio.rolloffMode = AudioRolloffMode.Linear; audio.volume = preset.soundVolume;
            var mistRenderer = mist.GetComponent<ParticleSystemRenderer>(); var sprayRenderer = spray.GetComponent<ParticleSystemRenderer>();
            return new WorldWaterfallEffects.Emitter { guid = edge.guid, landing = landing.transform, mist = mist, spray = spray, audio = audio,
                mistRenderer = mistRenderer, sprayRenderer = sprayRenderer, mistLocalBounds = mistRenderer.localBounds, sprayLocalBounds = sprayRenderer.localBounds };
        }
        private static void Foam(WorldDefinition definition, Transform parent, PlanPoint across, PlanPoint direction, double width, Material material, List<string> created)
        {
            var chunk = new WaterChunk(); var center = WorldPlan.Point(parent.position) + new PlanPoint(0, .035, 0);
            foreach (var pair in new[] { (-.5, -1.0), (.5, -1.0), (-.5, 1.0), (.5, 1.0) })
                chunk.Vertices.Add(new WaterVertex { Position = center + across * (width * pair.Item1) + direction * pair.Item2, U = pair.Item1 + .5, V = pair.Item2 });
            chunk.Triangles.AddRange(new[] { 0, 2, 1, 1, 2, 3 }); var mesh = Mesh(chunk, "JunctionFoam"); mesh = SaveAsset(definition, mesh, MeshHash(mesh), created);
            // 메시 좌표는 전역이므로 소켓의 변환을 중복 적용하지 않는다.
            var node = Create("JunctionFoam", parent); node.transform.position = Vector3.zero; Surface(node, mesh, material);
        }
        private static ParticleSystem Particle(GameObject node, WaterfallPreset preset, double width, bool mist)
        {
            var ps = Add<ParticleSystem>(node); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = preset.particleLifetime; main.startSpeed = preset.particleSpeed; main.startSize = mist ? preset.mistSize : preset.spraySize;
            main.startColor = mist ? new Color(.8f, .9f, .95f, .12f) : new Color(.8f, .9f, 1, .5f);
            main.maxParticles = Mathf.CeilToInt(Mathf.Max(preset.mistRate, preset.sprayRate) * preset.particleLifetime * 2); main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;
            main.gravityModifier = mist ? 0 : 1;
            var emission = ps.emission; emission.rateOverTime = mist ? preset.mistRate : preset.sprayRate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3((float)width, 1, 1);
            Undo.RegisterCompleteObjectUndo(node.transform, "폭포 입자 방향 기록"); node.transform.rotation = Quaternion.Euler(mist ? -90 : 90, 0, 0); return ps;
        }
        private static Mesh Mesh(WaterChunk chunk, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = chunk.Vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            var vertices = new Vector3[chunk.Vertices.Count]; var uv = new Vector2[vertices.Length]; var flow = new Vector2[vertices.Length]; var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) { var v = chunk.Vertices[i]; vertices[i] = Vector(v.Position); uv[i] = new Vector2((float)v.U, (float)v.V); flow[i] = new Vector2((float)v.Flow.x, (float)v.Flow.z); colors[i] = Color.white; }
            mesh.vertices = vertices; mesh.uv = uv; mesh.uv2 = flow; mesh.colors = colors; mesh.triangles = chunk.Triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static T SaveAsset<T>(WorldDefinition definition, T asset, string content, List<string> created) where T : UnityEngine.Object
        {
            string folder = "Assets/WorldBuilder/Worlds/" + definition.WorldGuid + "/Authoring/WaterMeshes"; WorldBuilderAuthoringAssets.EnsureFolder(folder);
            string hash = WorldDefinition.HashBytes(System.Text.Encoding.UTF8.GetBytes(content)); string path = folder + "/" + typeof(T).Name + "_" + hash + ".asset";
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                string resolved = AssetDatabase.GUIDToAssetPath(guid);
                if (resolved != path) { UnityEngine.Object.DestroyImmediate(asset); throw new IOException("수계 자산 GUID의 경로가 다릅니다."); }
                var existing = AssetDatabase.LoadAssetAtPath<T>(resolved);
                if (existing == null || (existing is Mesh mesh && MeshHash(mesh) != content) || (existing is Material mat && StableJson(mat) != content)) { UnityEngine.Object.DestroyImmediate(asset); throw new IOException("파생 수계 자산 GUID/타입/내용 충돌"); }
                UnityEngine.Object.DestroyImmediate(asset); return existing;
            }
            if (File.Exists(path)) { UnityEngine.Object.DestroyImmediate(asset); throw new IOException("미식별 수계 자산을 덮어쓸 수 없습니다."); }
            AssetDatabase.CreateAsset(asset, path); created.Add(path); return asset;
        }
        private static string MeshHash(Mesh mesh)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                foreach (var v in mesh.vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
                foreach (var v in mesh.uv) { writer.Write(v.x); writer.Write(v.y); }
                foreach (var v in mesh.uv2) { writer.Write(v.x); writer.Write(v.y); }
                foreach (var v in mesh.normals) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
                foreach (var v in mesh.colors) { writer.Write(v.r); writer.Write(v.g); writer.Write(v.b); writer.Write(v.a); }
                var bounds = mesh.bounds; writer.Write(bounds.center.x); writer.Write(bounds.center.y); writer.Write(bounds.center.z); writer.Write(bounds.size.x); writer.Write(bounds.size.y); writer.Write(bounds.size.z);
                foreach (int i in mesh.triangles) writer.Write(i); writer.Flush(); return WorldDefinition.HashBytes(stream.ToArray());
            }
        }
        internal static string StableJson(UnityEngine.Object asset)
        {
            string json = NormalizeReferences(EditorJsonUtility.ToJson(asset));
            return asset is Material ? Regex.Replace(json, "\"m_Name\":\"[^\"]*\"", "\"m_Name\":\"\"") : json;
        }
        internal static string StableRecordJson(object record) => NormalizeReferences(JsonUtility.ToJson(record));
        private static string HierarchyHash(WorldWaterQuery query) => WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(HierarchySnapshot(query)));
        private static string HierarchySnapshot(WorldWaterQuery query)
        {
            var text = new StringBuilder();
            foreach (var transform in query.GetComponentsInChildren<Transform>(true))
            {
                text.Append(Path(transform)).Append(transform.gameObject.activeSelf);
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component == null) { text.Append("MissingScript"); continue; }
                    string json = EditorJsonUtility.ToJson(component);
                    if (component is WaterVolume && query.definition.Ecology == null)
                        json = Regex.Replace(json, ",\"_ecology\":\\{\"instanceID\":0\\}", "");
                    if (component == query) json = Regex.Replace(json, "\"hierarchyHash\":\"[^\"]*\"", "\"hierarchyHash\":\"\"");
                    text.Append(component.GetType().FullName).Append(NormalizeReferences(json));
                    if (component is MeshFilter filter && filter.sharedMesh != null) text.Append(MeshHash(filter.sharedMesh));
                    if (component is Renderer renderer) foreach (var material in renderer.sharedMaterials)
                        // 프리셋 원본 재질 편집은 정상 저작이다. 불변 파생 재질만 내용까지 보호한다.
                        if (material != null && AssetDatabase.GetAssetPath(material).StartsWith("Assets/WorldBuilder/Worlds/" + query.definition.WorldGuid + "/Authoring/WaterMeshes/", StringComparison.Ordinal))
                            text.Append(NormalizeReferences(EditorJsonUtility.ToJson(material)));
                }
            }
            return text.ToString();
        }
        private static string DescribeDifference(WorldWaterQuery query)
        {
            string path = "Library/WorldBuilder/" + query.definition.WorldGuid + "/WaterHierarchy/" + query.hierarchyHash + ".txt";
            if (!File.Exists(path)) return "계층 기준 캐시 없음";
            string before = File.ReadAllText(path), after = HierarchySnapshot(query); int index = 0;
            while (index < before.Length && index < after.Length && before[index] == after[index]) index++;
            int start = Math.Max(0, index - 60);
            return "차이 위치 " + index + " 이전=" + before.Substring(start, Math.Min(160, before.Length - start)) + " 현재=" + after.Substring(start, Math.Min(160, after.Length - start));
        }
        private static string NormalizeReferences(string json) => Regex.Replace(json, "\"instanceID\":(-?[0-9]+)", match =>
        {
            var value = EditorUtility.InstanceIDToObject(int.Parse(match.Groups[1].Value));
            if (value == null) return "\"reference\":\"null\"";
            string asset = AssetDatabase.GetAssetPath(value);
            if (!string.IsNullOrEmpty(asset) && !asset.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) return "\"reference\":\"" + AssetDatabase.AssetPathToGUID(asset) + ":" + value.name + "\"";
            var component = value as Component; var go = value as GameObject;
            return "\"reference\":\"" + (component != null ? Path(component.transform) + component.GetType().FullName : go != null ? Path(go.transform) : value.name) + "\"";
        });
        private static string Path(Transform transform) => transform.parent == null ? transform.name : Path(transform.parent) + "/" + transform.GetSiblingIndex() + ":" + transform.name;
        private static void Surface(GameObject node, Mesh mesh, Material material)
        { Add<MeshFilter>(node).sharedMesh = mesh; var renderer = Add<MeshRenderer>(node); renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        private static GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "수계 계층 생성");
            if (parent != null) Undo.SetTransformParent(go.transform, parent, "수계 부모 기록");
            Undo.RegisterCompleteObjectUndo(go.transform, "수계 로컬 변환 기록");
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one; return go;
        }
        private static T Add<T>(GameObject go) where T : Component { var component = Undo.AddComponent<T>(go); Undo.RegisterCompleteObjectUndo(component, "수계 구성 기록"); return component; }
        private static Vector3 Vector(PlanPoint p) => new Vector3((float)p.x, (float)p.y, (float)p.z);
    }
}
