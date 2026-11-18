using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>건물2곳·교량·타일 경계 도로를 분리된 합성 월드에서 재현한다.</summary>
    public static class WorldPlanningValidationFixture
    {
        public const string WorldId = "d404d404d404d404d404d404d404d404";
        public const string WorldFolder = "Assets/WorldBuilder/Worlds/" + WorldId;
        public const string DataFolder = "Assets/10.Datas/WorldBuilder/P04Validation";
        public const string ScenePath = WorldFolder + "/P04Validation.unity";
        public const string DefinitionPath = DataFolder + "/Definition.asset";
        public static WorldDefinition CreateValidationScene()
        {
            var definition = Create(WorldId, WorldFolder, DataFolder, true, NewSceneMode.Additive);
            EnsureCameraRules(); BuildNavigation(definition); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); return definition;
        }
        public static WorldDefinition Create(string id, string folder, string dataFolder, bool fullSize, NewSceneMode mode)
        {
            if (Directory.Exists(folder) || Directory.Exists(dataFolder)) throw new IOException("기존 검증 월드를 덮어쓸 수 없습니다.");
            var previous = SceneManager.GetActiveScene(); Scene scene = default;
            try
            {
                WorldBuilderAuthoringAssets.EnsureFolder(folder + "/Terrain"); WorldBuilderAuthoringAssets.EnsureFolder(dataFolder);
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode); SceneManager.SetActiveScene(scene);
                int size = fullSize ? 2048 : 128, samples = size + 1, tileCells = fullSize ? 512 : 64;
                var profile = ScriptableObject.CreateInstance<WorldProfile>(); profile.Configure(size, samples, 0, 700, tileCells); AssetDatabase.CreateAsset(profile, dataFolder + "/Profile.asset");
                int waterX0 = fullSize ? 500 : 60, waterX1 = fullSize ? 524 : 68, waterZ0 = fullSize ? 180 : 30, waterZ1 = fullSize ? 320 : 100;
                var values = new ushort[samples * samples];
                for (int z = 0; z < samples; z++) for (int x = 0; x < samples; x++) values[z * samples + x] = (ushort)(x >= waterX0 && x <= waterX1 && z >= waterZ0 && z <= waterZ1 ? 187 : 936);
                var field = new HeightField(samples, samples, values); File.WriteAllBytes(folder + "/Base.bytes", Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South));
                AssetDatabase.ImportAsset(folder + "/Base.bytes", ImportAssetOptions.ForceSynchronousImport);
                var terrains = WorldBuilderSampleImporter.CreateTerrains(field, profile, scene, folder + "/Terrain"); WorldBuilderSampleImporter.ApplyPreviewSurface(terrains, folder + "/Terrain");
                var wall = Material(folder + "/Wall.mat", new Color(.45f, .42f, .36f)); var roof = Material(folder + "/Roof.mat", new Color(.32f, .13f, .08f));
                var wood = Material(folder + "/Wood.mat", new Color(.43f, .28f, .13f)); var roadMaterial = Material(folder + "/Road.mat", new Color(.22f, .18f, .12f));
                var house = new GameObject("WB_P04_House_Prototype");
                Cube(house.transform, "바닥", new Vector3(0, -.125f, 0), new Vector3(8, .25f, 8), wall);
                Cube(house.transform, "왼쪽 벽", new Vector3(-3.75f, 2.5f, 0), new Vector3(.5f, 5, 8), wall);
                Cube(house.transform, "오른쪽 벽", new Vector3(3.75f, 2.5f, 0), new Vector3(.5f, 5, 8), wall);
                Cube(house.transform, "뒤쪽 벽", new Vector3(0, 2.5f, -3.75f), new Vector3(8, 5, .5f), wall);
                Cube(house.transform, "입구 왼쪽", new Vector3(-3, 2.5f, 3.75f), new Vector3(2, 5, .5f), wall);
                Cube(house.transform, "입구 오른쪽", new Vector3(3, 2.5f, 3.75f), new Vector3(2, 5, .5f), wall);
                Cube(house.transform, "지붕", new Vector3(0, 5.25f, 0), new Vector3(8, .5f, 8), roof);
                var housePrefab = PrefabUtility.SaveAsPrefabAsset(house, folder + "/WB_P04_House_Prototype.prefab"); UnityEngine.Object.DestroyImmediate(house);
                float span = fullSize ? 32 : 16;
                var bridge = new GameObject("WB_P04_Bridge_Prototype"); Cube(bridge.transform, "상판", new Vector3(0, -.25f, 0), new Vector3(6, .5f, span), wood);
                Cube(bridge.transform, "왼쪽 난간", new Vector3(-2.8f, .55f, 0), new Vector3(.2f, 1.1f, span), wood);
                Cube(bridge.transform, "오른쪽 난간", new Vector3(2.8f, .55f, 0), new Vector3(.2f, 1.1f, span), wood);
                var gate = bridge.AddComponent<WorldBridgeAccessGate>(); var barriers = new Collider[2]; var obstacles = new NavMeshObstacle[2];
                for (int i = 0; i < 2; i++)
                {
                    var entry = new GameObject("진입 준비 장벽 " + i); entry.transform.SetParent(bridge.transform, false); entry.transform.localPosition = new Vector3(0, 1, (i == 0 ? -1 : 1) * (span / 2 - .5f)); entry.transform.localScale = new Vector3(6, 2, .5f);
                    barriers[i] = entry.AddComponent<BoxCollider>(); obstacles[i] = entry.AddComponent<NavMeshObstacle>(); obstacles[i].shape = NavMeshObstacleShape.Box; obstacles[i].size = Vector3.one; obstacles[i].carving = true;
                }
                var serializedGate = new SerializedObject(gate); SetReferences(serializedGate.FindProperty("_entryBarriers"), barriers); SetReferences(serializedGate.FindProperty("_entryObstacles"), obstacles); serializedGate.ApplyModifiedPropertiesWithoutUndo();
                var bridgePrefab = PrefabUtility.SaveAsPrefabAsset(bridge, folder + "/WB_P04_Bridge_Prototype.prefab"); UnityEngine.Object.DestroyImmediate(bridge);
                var prop = new GameObject("P04 검증 소품"); Cube(prop.transform, "소품", new Vector3(0, .5f, 0), new Vector3(1, 1, 1), wood);
                var propPrefab = PrefabUtility.SaveAsPrefabAsset(prop, folder + "/Prop.prefab"); UnityEngine.Object.DestroyImmediate(prop);
                var catalog = ScriptableObject.CreateInstance<PlacementCatalog>(); AssetDatabase.CreateAsset(catalog, dataFolder + "/Catalog.asset");
                var serialized = new SerializedObject(catalog); var entries = serialized.FindProperty("_entries"); entries.arraySize = 3;
                SetEntry(entries.GetArrayElementAtIndex(0), housePrefab, new Vector2(4, 4)); SetEntry(entries.GetArrayElementAtIndex(1), bridgePrefab, new Vector2(3, span / 2)); SetEntry(entries.GetArrayElementAtIndex(2), propPrefab, new Vector2(.5f, .5f)); serialized.ApplyModifiedPropertiesWithoutUndo();
                var buildingPreset = ScriptableObject.CreateInstance<StructurePlanningPreset>(); buildingPreset.catalog = catalog; buildingPreset.prefabGuid = GuidFor(housePrefab); buildingPreset.plotMargin = 4; buildingPreset.falloff = 8;
                buildingPreset.supportPoints = new[] { new Vector3(-3, 0, -3), new Vector3(3, 0, -3), new Vector3(-3, 0, 3), new Vector3(3, 0, 3), Vector3.zero }; buildingPreset.entranceSockets = new[] { new Vector3(0, 0, 8) }; AssetDatabase.CreateAsset(buildingPreset, dataFolder + "/Structure.asset");
                var bridgePreset = ScriptableObject.CreateInstance<BridgePlanningPreset>(); bridgePreset.catalog = catalog; bridgePreset.prefabGuid = GuidFor(bridgePrefab); bridgePreset.span = span; AssetDatabase.CreateAsset(bridgePreset, dataFolder + "/Bridge.asset");
                var roadPreset = ScriptableObject.CreateInstance<RoadPlanningPreset>(); roadPreset.material = roadMaterial; AssetDatabase.CreateAsset(roadPreset, dataFolder + "/Road.asset");
                var rule = ScriptableObject.CreateInstance<PlacementRuleAsset>(); rule.layerGuid = Id(20); rule.ruleGuid = Id(21); rule.prefabGuid = GuidFor(propPrefab); rule.region = fullSize ? new Rect(256, 0, 512, 512) : new Rect(0, 0, 128, 128); rule.cellSize = fullSize ? 32 : 12; rule.spacing = 2; rule.maxSupportDelta = 1; AssetDatabase.CreateAsset(rule, dataFolder + "/Props.asset");
                var camera = new GameObject("P04 검증 카메라").AddComponent<Camera>(); camera.transform.position = fullSize ? new Vector3(730, 310, -40) : new Vector3(145, 110, -35); camera.transform.LookAt(fullSize ? new Vector3(512, 11, 240) : new Vector3(64, 11, 64)); camera.farClipPlane = 3000;
                var light = new GameObject("P04 검증 햇빛").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);
                string scenePath = folder + "/P04Validation.unity"; if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("P04 씬 저장 실패");
                var definition = ScriptableObject.CreateInstance<WorldDefinition>(); definition.Initialize(id, profile, AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "/Base.bytes"), AssetDatabase.AssetPathToGUID(scenePath)); AssetDatabase.CreateAsset(definition, dataFolder + "/Definition.asset");
                var def = new SerializedObject(definition); def.FindProperty("_placementCatalog").objectReferenceValue = catalog; def.FindProperty("_placementRules").arraySize = 1; def.FindProperty("_placementRules").GetArrayElementAtIndex(0).objectReferenceValue = rule; def.ApplyModifiedPropertiesWithoutUndo();
                WorldPlacementAuthoring.Regenerate(definition); PlacementRecord pin = null; foreach (var p in definition.Placements) if (pin == null || p.z < pin.z) pin = p;
                if (pin == null) throw new InvalidOperationException("P03 보존 검증 후보가 없습니다."); WorldPlacementAuthoring.ApplyOverride(definition, WorldPlacementAuthoring.CreatePin(pin));
                var plan = ScriptableObject.CreateInstance<WorldPlan>(); plan.InitializeForWorld(id); AssetDatabase.CreateAsset(plan, dataFolder + "/Plan.asset");
                double split = fullSize ? 512 : 64, min = fullSize ? 256 : 0, max = fullSize ? 768 : 128, depth = fullSize ? 512 : 128;
                plan.regions.Add(new WorldPlan.RegionRecord { regionGuid = Id(1), label = "서쪽 접근 구역", boundary = Rect(min, 0, split, depth) }); plan.regions.Add(new WorldPlan.RegionRecord { regionGuid = Id(2), label = "동쪽 접근 구역", boundary = Rect(split, 0, max, depth) });
                var anchorA = fullSize ? new PlanPoint(400, 11, 220) : new PlanPoint(32, 11, 32); var anchorB = fullSize ? new PlanPoint(640, 11, 280) : new PlanPoint(96, 11, 96);
                plan.pois.Add(new PoiPlan { guid = Id(3), regionGuid = Id(1), anchor = anchorA, hasConfirmedElevation = true, isLocked = true }); plan.pois.Add(new PoiPlan { guid = Id(4), regionGuid = Id(2), anchor = anchorB, hasConfirmedElevation = true, isLocked = true });
                plan.structures.Add(new WorldPlan.StructureRecord { structureGuid = Id(5), poiGuid = Id(3), preset = buildingPreset }); plan.structures.Add(new WorldPlan.StructureRecord { structureGuid = Id(6), poiGuid = Id(4), preset = buildingPreset, yaw = 180 });
                double bridgeZ = fullSize ? 250 : 64;
                plan.bridges.Add(new WorldPlan.BridgeRecord { bridgeGuid = Id(7), waterPlanGuid = Id(9), preset = bridgePreset, hasConfirmedWaterLevel = true, plannedWaterLevel = 8,
                    startSocket = new Vector3((float)(split - span / 2), 12, (float)bridgeZ), endSocket = new Vector3((float)(split + span / 2), 12, (float)bridgeZ), waterZone = new PlanZone { guid = Id(10), boundary = Points(Rect(waterX0, waterZ0, waterX1, waterZ1)) } });
                plan.roads.Add(new WorldPlan.RoadRecord { roadGuid = Id(8), startStructureGuid = Id(5), endStructureGuid = Id(6), requiredBridgeGuid = Id(7), preset = roadPreset });
                WorldPlanningAuthoring.Regenerate(definition, plan);
                AssetDatabase.SaveAssetIfDirty(catalog); AssetDatabase.SaveAssetIfDirty(rule); AssetDatabase.SaveAssetIfDirty(plan); AssetDatabase.SaveAssetIfDirty(definition); foreach (var terrain in terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                EditorSceneManager.SaveScene(scene); return definition;
            }
            catch
            {
                if (scene.IsValid() && scene.isLoaded) { if (SceneManager.sceneCount == 1) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); else EditorSceneManager.CloseScene(scene, true); }
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                AssetDatabase.DeleteAsset(folder); AssetDatabase.DeleteAsset(dataFolder); throw;
            }
        }
        public static WorldPlanningNavigationPreview BuildNavigation(WorldDefinition definition)
        {
            if (SceneManager.GetActiveScene().path != AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid)) throw new InvalidOperationException("대상 검증 씬을 여세요.");
            string path = "Assets/WorldBuilder/Worlds/" + definition.WorldGuid + "/ValidationNavMesh.asset";
            if (File.Exists(path)) throw new IOException("기존 NavMesh를 덮어쓰지 않습니다.");
            var sources = new List<NavMeshBuildSource>(); var markups = new List<NavMeshBuildMarkup>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var gate in root.GetComponentsInChildren<WorldBridgeAccessGate>(true))
                foreach (var collider in gate.EntryBarriers) markups.Add(new NavMeshBuildMarkup { root = collider.transform, ignoreFromBuild = true });
            bool full = definition.Profile.WorldSize > 128; var center = full ? new Vector3(512, 20, 256) : new Vector3(64, 20, 64); var bounds = new Bounds(center, full ? new Vector3(512, 100, 512) : new Vector3(128, 100, 128));
            Physics.SyncTransforms(); NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var scene = SceneManager.GetActiveScene();
            sources.RemoveAll(source => source.component != null && source.component.gameObject.scene != scene);
            var settings = NavMesh.GetSettingsByIndex(0); var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("검증 NavMesh 생성 실패"); AssetDatabase.CreateAsset(data, path);
            var preview = new GameObject("P04 접근 검증 NavMesh").AddComponent<WorldPlanningNavigationPreview>(); preview.navigation = data;
            var plan = definition.Planning; var a = plan.pois.Find(p => p.guid == Id(3)).anchor; var b = plan.pois.Find(p => p.guid == Id(4)).anchor;
            preview.entranceA = new Vector3((float)a.x, (float)a.y, (float)a.z + 2); preview.entranceB = new Vector3((float)b.x, (float)b.y, (float)b.z - 2);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); return preview;
        }
        private static void EnsureCameraRules()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/Camera/CameraOccluderNameRules.asset"); if (asset == null) throw new InvalidOperationException("카메라 이름 규칙 원본이 없습니다.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(asset, "WorldBuilder 구조물 카메라 규칙"); var serialized = new SerializedObject(asset); var rules = serialized.FindProperty("_rules");
                AddRule(rules, "WB_P04_House_", 0, true); AddRule(rules, "WB_P04_Bridge_", 1, false); AddRule(rules, "WB_P04_Road_", 1, false); serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(asset); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); AssetDatabase.SaveAssetIfDirty(asset); throw; }
        }
        private static void AddRule(SerializedProperty rules, string pattern, int mode, bool roof)
        {
            for (int i = 0; i < rules.arraySize; i++) if (rules.GetArrayElementAtIndex(i).FindPropertyRelative("namePattern").stringValue == pattern)
            {
                var existing = rules.GetArrayElementAtIndex(i); if (existing.FindPropertyRelative("mode").enumValueIndex != mode || !existing.FindPropertyRelative("isPrefix").boolValue || existing.FindPropertyRelative("coversInterior").boolValue != roof) throw new InvalidOperationException("기존 WorldBuilder 카메라 규칙과 충돌합니다."); return;
            }
            int index = rules.arraySize; rules.InsertArrayElementAtIndex(index); var item = rules.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("namePattern").stringValue = pattern; item.FindPropertyRelative("isPrefix").boolValue = true; item.FindPropertyRelative("mode").enumValueIndex = mode; item.FindPropertyRelative("coversInterior").boolValue = roof; item.FindPropertyRelative("note").stringValue = "P04 검증 구조물. 원본 규칙은 유지.";
        }
        private static Material Material(string path, Color color)
        { var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("URP Lit 필요"); var material = new Material(shader); material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0); AssetDatabase.CreateAsset(material, path); return material; }
        private static void Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; }
        private static void SetReferences<T>(SerializedProperty array, T[] values) where T : UnityEngine.Object
        { array.arraySize = values.Length; for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
        private static void SetEntry(SerializedProperty entry, GameObject prefab, Vector2 extents)
        { entry.FindPropertyRelative("prefabGuid").stringValue = GuidFor(prefab); entry.FindPropertyRelative("prefab").objectReferenceValue = prefab; entry.FindPropertyRelative("halfExtents").vector2Value = extents; entry.FindPropertyRelative("pivotHeight").floatValue = 0; entry.FindPropertyRelative("supportsNonUniformScale").boolValue = false; }
        private static Vector2[] Rect(double x0, double z0, double x1, double z1) => new[] { new Vector2((float)x0, (float)z0), new Vector2((float)x1, (float)z0), new Vector2((float)x1, (float)z1), new Vector2((float)x0, (float)z1) };
        private static PlanPoint[] Points(Vector2[] values) { var result = new PlanPoint[values.Length]; for (int i = 0; i < result.Length; i++) result[i] = new PlanPoint(values[i].x, 0, values[i].y); return result; }
        private static string GuidFor(UnityEngine.Object asset) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        public static string Id(int value) => value.ToString("x32");
    }
}
