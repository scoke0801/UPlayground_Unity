using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    public static class WorldEcologyValidationFixture
    {
        public const string WorldId = "f606f606f606f606f606f606f6060906";
        public const string WorldFolder = "Assets/WorldBuilder/Worlds/" + WorldId;
        public const string DataFolder = "Assets/10.Datas/WorldBuilder/P06Validation";
        public const string ScenePath = WorldFolder + "/P06Validation.unity";
        public const string MainCave = "00000000000000000000000000000600";

        public static WorldDefinition CreateValidationScene()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 저장한 뒤 P06 검증 씬을 생성하세요.");
            if (Directory.Exists(WorldFolder) || Directory.Exists(DataFolder)) throw new IOException("기존 P06 검증 월드를 덮어쓸 수 없습니다.");
            bool prepared = false;
            try
            {
                var definition = WorldWaterValidationFixture.Create(WorldId, WorldFolder, DataFolder, true, NewSceneMode.Single);
                var scene = SceneManager.GetActiveScene();
                string error = AssetDatabase.MoveAsset(scene.path, ScenePath);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                var ecology = CreateInput(definition, DataFolder, true);
                AddBuilding(definition, DataFolder);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("P06 기본 씬 저장 실패");
                prepared = true;
                WorldEcologyAuthoring.Regenerate(definition, ecology);
                var camera = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).First(c => c != null);
                camera.gameObject.name = "P06 검증 카메라";
                camera.transform.position = new Vector3(456, 34, 335);
                camera.transform.LookAt(new Vector3(508, 7, 297));
                camera.fieldOfView = 48;
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("P06 씬 저장 실패");
                return definition;
            }
            catch
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.path.StartsWith(WorldFolder, StringComparison.Ordinal)) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (previous.Length > 0 && previous.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
                if (!prepared)
                {
                    AssetDatabase.DeleteAsset(WorldFolder);
                    AssetDatabase.DeleteAsset(DataFolder);
                }
                throw;
            }
        }

        public static WorldEcology CreateInput(WorldDefinition definition, string folder, bool fullSize)
        {
            var source = ScriptableObject.CreateInstance<WorldEcology>();
            source.worldGuid = definition.WorldGuid;
            source.ecologyGuid = Id(610);
            source.seed = 606;
            source.playerPrefab = Resource<GameObject>("866caeb41bd1cfa45b23a00d9f309ebb");
            source.defaultLayer = Layer(folder, "Earth", "1eadc7f2ad03e2147bd5e69afef232cc");
            var meadow = Biome(source, folder, "Meadow", Id(611), Layer(folder, "Meadow", "d4a7c7da6a1607c4cb8e965ff01e5891"), 0,
                fullSize ? new Rect(356, 164, 292, 180) : new Rect(0, 0, 128, 128));
            var riparian = Biome(source, folder, "Riparian", Id(612), Layer(folder, "Riparian", "0fc5169db7a8bf24bb9c06ecd39771c4"), 5,
                fullSize ? new Rect(444, 158, 132, 180) : new Rect(0, 0, 128, 128));
            riparian.shoreDistance = new Vector2(1, 17);
            riparian.humidity = new Vector2(.4f, 1);
            Save(riparian);
            var forest = Biome(source, folder, "Forest", Id(613), Layer(folder, "Forest", "3892c2a3e7a647a4ea95ecdbef547b15"), 2,
                fullSize ? new Rect(536, 212, 96, 104) : new Rect(72, 68, 48, 50));
            AddVegetation(source, folder, "Broadleaf", Id(614), meadow, VegetationPath.TerrainTree, "5d6ff116fb5dd6343a053c078b5cdc2b", 14, .6f, 2);
            AddVegetation(source, folder, "ForestBroadleaf", Id(615), forest, VegetationPath.TerrainTree, "03e2340891d61e94b93df31e86c00f99", 13, .8f, 2);
            AddVegetation(source, folder, "Grass", Id(616), meadow, VegetationPath.Detail, "a3660b5bb89bf14449bbced81d56cb94", 2.5f, .45f, .5f);
            AddVegetation(source, folder, "RiparianGrass", Id(617), riparian, VegetationPath.Detail, "07aabd073296d6542ba7d2c7fe4ed76b", 2.5f, .6f, .5f);
            var shrub = AddVegetation(source, folder, "Shrub", Id(618), riparian, VegetationPath.Prefab, "9af567e72a97f044297c5f8063f6a5f3", 6, .65f, 1);
            AddCatalog(definition.PlacementCatalog, shrub.prefab, new Vector2(.8f, .8f));
            Save(definition.PlacementCatalog);
            var rule = ScriptableObject.CreateInstance<PlacementRuleAsset>();
            rule.layerGuid = Id(619); rule.ruleGuid = Id(620); rule.prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shrub.prefab));
            rule.ruleTag = "vegetation"; rule.region = fullSize ? new Rect(440, 160, 144, 180) : new Rect(0, 0, 128, 128); rule.cellSize = 8;
            AssetDatabase.CreateAsset(rule, folder + "/ShrubRule.asset");
            shrub.placementRule = rule;
            Save(shrub);

            var stone = Resource<Material>("3fbb2d6e103698f4e94c3e1bab243e14");
            var collar = MakeCollar(folder);
            var entrance = Module(source, folder, "Entrance", Id(621), CaveModuleKind.Entrance, 6, fullSize ? 20 : 10, 9, 0, stone, collar);
            var tunnel = Module(source, folder, "Straight", Id(622), CaveModuleKind.Straight, 6, fullSize ? 32 : 12, 0, 0, stone);
            Module(source, folder, "Bend", Id(623), CaveModuleKind.Bend, 8, 8, 0, 0, stone);
            Module(source, folder, "Branch", Id(624), CaveModuleKind.Branch, 8, 8, 0, 0, stone);
            Module(source, folder, "Room", Id(625), CaveModuleKind.Room, 10, 10, 0, 0, stone);
            Module(source, folder, "EndCap", Id(626), CaveModuleKind.EndCap, 6, 5, 0, 0, stone);
            var cave = new CaveLayout { caveGuid = MainCave };
            float center = fullSize ? 512 : 64, z = fullSize ? 303 : 15;
            float offset = (entrance.footprintSize.y + tunnel.footprintSize.y) / 2;
            var west = new CaveModuleLayout { moduleId = Id(601), surfaceId = Id(604), preset = entrance, position = new Vector3(center - offset, 1, z), yaw = 90 };
            var middle = new CaveModuleLayout { moduleId = Id(602), surfaceId = Id(605), preset = tunnel, position = new Vector3(center, 1, z), yaw = 90 };
            var east = new CaveModuleLayout { moduleId = Id(603), surfaceId = Id(606), preset = entrance, position = new Vector3(center + offset, 1, z), yaw = 270 };
            cave.modules.AddRange(new[] { west, middle, east });
            cave.connections.Add(new CaveConnection { moduleA = west.moduleId, socketA = Id(632), moduleB = middle.moduleId, socketB = Id(631) });
            cave.connections.Add(new CaveConnection { moduleA = middle.moduleId, socketA = Id(632), moduleB = east.moduleId, socketB = Id(632) });
            source.caves.Add(cave);
            AssetDatabase.CreateAsset(source, folder + "/Ecology.asset");
            return source;
        }

        private static void AddBuilding(WorldDefinition definition, string folder)
        {
            var preset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StructurePlanningPreset>(folder + "/Structure.asset"));
            preset.requiresAccess = false;
            AssetDatabase.CreateAsset(preset, folder + "/LookoutStructure.asset");
            var plan = definition.Planning;
            plan.pois.Add(new PoiPlan { guid = Id(651), regionGuid = WorldPlanningValidationFixture.Id(1), anchor = new PlanPoint(421, 11, 326),
                hasConfirmedElevation = true, isLocked = true });
            plan.structures.Add(new WorldPlan.StructureRecord { structureGuid = Id(652), poiGuid = Id(651), preset = preset });
            WorldPlanningAuthoring.Regenerate(definition, plan);
        }

        private static TerrainLayer Layer(string folder, string name, string guid)
        {
            var layer = UnityEngine.Object.Instantiate(Resource<TerrainLayer>(guid));
            layer.name = name; layer.smoothnessSource = TerrainLayerSmoothnessSource.Constant; layer.smoothness = 0; layer.metallic = 0;
            AssetDatabase.CreateAsset(layer, folder + "/" + name + ".terrainlayer");
            return layer;
        }

        private static BiomePreset Biome(WorldEcology source, string folder, string name, string id, TerrainLayer layer, int priority, Rect rect)
        {
            var biome = ScriptableObject.CreateInstance<BiomePreset>();
            biome.biomeId = id; biome.layer = layer; biome.priority = priority; biome.slope = new Vector2(0, 25);
            biome.mask = new[] { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax) };
            source.biomes.Add(biome);
            AssetDatabase.CreateAsset(biome, folder + "/" + name + "Biome.asset");
            return biome;
        }

        private static VegetationPreset AddVegetation(WorldEcology source, string folder, string name, string id, BiomePreset biome, VegetationPath path,
            string resource, float spacing, float density, float radius)
        {
            var item = ScriptableObject.CreateInstance<VegetationPreset>();
            item.prototypeId = id; item.biome = biome; item.path = path; item.prefab = Resource<GameObject>(resource);
            item.spacing = spacing; item.density = density; item.footprintRadius = radius;
            source.vegetation.Add(item);
            AssetDatabase.CreateAsset(item, folder + "/" + name + ".asset");
            return item;
        }

        private static CaveModulePreset Module(WorldEcology source, string folder, string name, string id, CaveModuleKind kind, float width, float length,
            float start, float end, Material material, GameObject collar = null)
        {
            var preset = ScriptableObject.CreateInstance<CaveModulePreset>();
            preset.catalogId = id; preset.kind = kind; preset.footprintSize = new Vector2(width, length);
            preset.floorAtMinZ = start; preset.floorAtMaxZ = end; preset.clearHeight = 2.7f; preset.clearWidth = 4;
            preset.collarPrefab = collar; preset.cover = .3f;
            float x = width / 2, z = length / 2;
            var root = new GameObject(name);
            try
            {
                var marker = root.AddComponent<WorldCaveModule>();
                var slab = Prism(root.transform, folder, name + "_Floor", -x, x, -z, z, start - .22f, end - .22f, .22f, material);
                marker.floorColliders = new Collider[] { slab };
                bool front = kind is not CaveModuleKind.EndCap and not CaveModuleKind.Bend;
                bool side = kind is CaveModuleKind.Bend or CaveModuleKind.Branch;
                Prism(root.transform, folder, name + "_West", -x, -x + .55f, -z, z, start, end, 3.1f, material);
                if (!side) Prism(root.transform, folder, name + "_East", x - .55f, x, -z, z, start, end, 3.1f, material);
                else
                {
                    Prism(root.transform, folder, name + "_EastA", x - .55f, x, -z, -2, start, end, 3.1f, material);
                    Prism(root.transform, folder, name + "_EastB", x - .55f, x, 2, z, start, end, 3.1f, material);
                }
                if (!front) Prism(root.transform, folder, name + "_Cap", -x, x, z - .45f, z, end, end, 3.1f, material);
                float roofStart = kind == CaveModuleKind.Entrance ? -z + length * .28f : -z;
                Prism(root.transform, folder, name + "_Roof", -x, x, roofStart, z,
                    Mathf.Lerp(start, end, (roofStart + z) / length) + 2.7f, end + 2.7f, .4f, material);
                preset.sockets.Add(Socket(Id(631), new Vector3(0, start, -z), 180, kind == CaveModuleKind.Entrance));
                if (front) preset.sockets.Add(Socket(Id(632), new Vector3(0, end, z), 0));
                if (side) preset.sockets.Add(Socket(Id(633), new Vector3(x, 0, 0), 90));
                preset.prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            source.caveCatalog.Add(preset);
            AssetDatabase.CreateAsset(preset, folder + "/" + name + ".asset");
            return preset;
        }

        private static MeshCollider Prism(Transform parent, string folder, string name, float x0, float x1, float z0, float z1,
            float y0, float y1, float thickness, Material material)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.isStatic = true;
            var points = new[] {
                new Vector3(x0,y0,z0), new Vector3(x0,y1,z1), new Vector3(x1,y1,z1), new Vector3(x1,y0,z0),
                new Vector3(x0,y0+thickness,z0), new Vector3(x0,y1+thickness,z1), new Vector3(x1,y1+thickness,z1), new Vector3(x1,y0+thickness,z0)
            };
            int[] indices = { 0,3,2,1, 4,5,6,7, 0,1,5,4, 3,7,6,2, 0,4,7,3, 1,2,6,5 };
            var mesh = new Mesh { name = name };
            mesh.vertices = indices.Select(i => points[i]).ToArray();
            mesh.uv = Enumerable.Range(0, 6).SelectMany(_ => new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }).ToArray();
            mesh.triangles = Enumerable.Range(0, 6).SelectMany(i => new[] { i*4, i*4+1, i*4+2, i*4, i*4+2, i*4+3 }).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, folder + "/" + name + ".asset");
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            node.AddComponent<MeshRenderer>().sharedMaterial = material;
            return AddCollider(node, mesh);
        }

        private static MeshCollider AddCollider(GameObject node, Mesh mesh)
        {
            var collider = node.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            return collider;
        }

        private static GameObject MakeCollar(string folder)
        {
            var root = new GameObject("RockCollar");
            try
            {
                var rock = Resource<GameObject>("c57c6d86541c2ce4d945cda785dff148");
                for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++)
                {
                    var prop = (GameObject)PrefabUtility.InstantiatePrefab(rock, root.transform);
                    prop.transform.localPosition = new Vector3(side * (3.1f + i * .3f), -.18f, -i * 1.5f);
                    prop.transform.localRotation = Quaternion.Euler(0, i * 55 + side * 15, 0);
                    prop.transform.localScale = Vector3.one * (1.15f + i * .15f);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, folder + "/RockCollar.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static CaveSocket Socket(string id, Vector3 floor, float yaw, bool exterior = false) =>
            new() { id = id, profile = "stone-4x2.7", floor = floor, yaw = yaw, exterior = exterior, width = 4, height = 2.7f };

        private static void AddCatalog(PlacementCatalog catalog, GameObject prefab, Vector2 extents)
        {
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("_entries");
            int index = entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("prefabGuid").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("halfExtents").vector2Value = extents;
            entry.FindPropertyRelative("pivotHeight").floatValue = 0;
            entry.FindPropertyRelative("supportsNonUniformScale").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T Resource<T>(string guid) where T : UnityEngine.Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null || AssetDatabase.AssetPathToGUID(path) != guid) throw new IOException("검증 소스 GUID·경로·타입 누락: " + guid);
            return asset;
        }

        private static void Save(params UnityEngine.Object[] assets)
        {
            foreach (var asset in assets) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
        }

        private static string Id(int value) => value.ToString("D32");
    }
}

