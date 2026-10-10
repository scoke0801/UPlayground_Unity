using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.UI;
using UPlayGround.EditorTools;
using UPlayGround.Group;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string WorldPath = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario.world.json";
        [Serializable] private sealed class WorldLayout
        {
            public Clearing[] zones;
            public Route[] routes;
            public WorldMonster[] monsters;
            public WorldProp[] props;
            public WorldPortal[] portals;
            public WorldInteraction[] interactions;
            public View[] views;
            public int meleeSlots;
            public int rangedSlots;
            public float breather;
            public float alertRadius;
            public float interactionClearance;
            public float portalClearance;
        }
        [Serializable] private sealed class WorldMonster
        {
            public string id;
            public string prefab;
            public string zone;
            public Vector3 position;
            public float yaw;
        }
        [Serializable] private sealed class WorldProp
        {
            public string name;
            public string prefab;
            public bool lightShadows;
            public Vector3 position;
            public Vector3 size;
            public bool collision;
            public bool meshCollision;
            public bool preserveAspect;
            public bool levelFootprint;
            public float foundationInset;
            public float foundationBlend = 4f;
            public float yaw;
        }
        [Serializable] private sealed class WorldPortal
        {
            public string id;
            public string label;
            public string destinationId;
            public Vector3 position;
            public Vector3 arrival;
            public bool startsActivated;
            public string prefab;
            public bool lightShadows;
        }
        [Serializable] private sealed class WorldInteraction
        {
            public string id;
            public string name;
            public string kind;
            public string data;
            public string prefab;
            public Vector3 position;
            public Vector3 size;
        }
        [Serializable] private sealed class WorldReport
        {
            public int monsters;
            public int portals;
            public int interactions;
            public int connectedRoutes;
            public List<string> errors = new();
        }

        /// <summary>기존 진행선을 보존하면서 동굴·던전 이동·서식 구역·생활 상호작용을 배치한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 월드 확장")]
        public static void ExpandScenarioWorld()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 먼저 저장하세요.");
            Layout layout = ReadLayout();
            var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
            InspectWorldMonsters();
            var audit = JsonUtility.FromJson<WorldAudit>(File.ReadAllText(ReportDirectory + "/WorldMonsterAudit.json"));
            var usable = new HashSet<string>();
            foreach (MonsterAudit item in audit.monsters) if (item.issues.Count == 0) usable.Add(item.prefab);
            var placed = new HashSet<string>();
            foreach (WorldMonster monster in world.monsters)
            {
                if (!usable.Contains(monster.prefab)) throw new InvalidOperationException("실행 데이터가 불완전한 몬스터: " + monster.prefab);
                placed.Add(monster.prefab);
            }
            foreach (string prefab in usable)
                if (!placed.Contains(prefab)) throw new InvalidOperationException("사용 가능한 몬스터 배치 누락: " + prefab);
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            GameObject environment = FindQualityTransform(scene, "AuthoredEnvironment").gameObject;
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            string backup = BackupQualityAssets(layout);
            string regionPath = layout.assetDirectory + "/RegionInfo.asset";
            File.Copy(regionPath, backup + "/RegionInfo.asset");
            try
            {
                RemoveWorldRoot(scene, "WorldExpansionActors");
                Transform old = environment.transform.Find("WorldExpansionGeometry");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var root = new GameObject("WorldExpansionActors").transform;
                var geometry = new GameObject("WorldExpansionGeometry").transform;
                geometry.SetParent(environment.transform);
                ClearWorldVegetation(environment.transform, layout, world);
                foreach (WorldProp prop in world.props) PlaceWorldProp(prop, terrain, geometry);
                PlaceWorldMonsters(world, terrain, root);
                PlaceWorldInteractions(world, terrain, root);
                PlaceWorldPortals(world, layout, terrain, root);
                Route[] originalRoutes = layout.routes;
                var allRoutes = new List<Route>(originalRoutes);
                allRoutes.AddRange(world.routes);
                layout.routes = allRoutes.ToArray();
                PaintQualityRoutes(layout, terrain);
                layout.routes = originalRoutes;
                Physics.SyncTransforms();
                BakeNavigation(layout, environment);
                Validate();
                ValidateWorld(world, terrain, root);
                SaveQualityScene(scene, layout);
                AssetDatabase.SaveAssets();
                CaptureMapBackground(layout, true);
                foreach (View view in world.views)
                {
                    view.position = GroundPoint(view.position, terrain) + Vector3.up * view.position.y;
                    view.target = GroundPoint(view.target, terrain) + Vector3.up * view.target.y;
                }
                CapturePreviewImages(false, world.views);
                Debug.Log("[ScenarioMap] 월드 확장 저장 완료");
            }
            catch
            {
                File.Copy(backup + "/RegionInfo.asset", regionPath, true);
                AssetDatabase.ImportAsset(regionPath, ImportAssetOptions.ForceUpdate);
                RestoreQualityBackup(layout, backup);
                throw;
            }
        }

        private static void RemoveWorldRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ClearWorldVegetation(Transform environment, Layout layout, WorldLayout world)
        {
            var scatterNames = new HashSet<string>();
            foreach (Scatter scatter in layout.scatters) scatterNames.Add(scatter.name);
            foreach (Transform child in environment.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) continue;
                // 기존 퀘스트·수작업 랜드마크는 보존하고 산포 군집 안의 개별 식생만 비운다.
                if (child.parent == null || child.parent.parent != environment || !scatterNames.Contains(child.parent.name)) continue;
                bool clear = false;
                foreach (Clearing zone in world.zones)
                    if (Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(zone.center.x, zone.center.z)) < zone.radius)
                        clear = true;
                foreach (Route route in world.routes)
                    for (int i = 1; i < route.points.Length; i++)
                        if (WorldSegmentDistance(child.position, route.points[i - 1], route.points[i]) < route.width + 4) clear = true;
                foreach (WorldInteraction interaction in world.interactions)
                    if (WorldSegmentDistance(child.position, interaction.position, interaction.position) < world.interactionClearance) clear = true;
                foreach (WorldPortal portal in world.portals)
                    if (WorldSegmentDistance(child.position, portal.position, portal.position) < world.portalClearance) clear = true;
                if (clear)
                {
                    child.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                }
            }
        }

        private static float WorldSegmentDistance(Vector3 point, Vector3 first, Vector3 second)
        {
            point.y = first.y = second.y = 0;
            Vector3 delta = second - first;
            float along = Mathf.Clamp01(Vector3.Dot(point - first, delta) / Mathf.Max(0.001f, delta.sqrMagnitude));
            return Vector3.Distance(point, first + delta * along);
        }

        private static GameObject PlaceWorldProp(WorldProp prop, Terrain terrain, Transform parent)
        {
            GameObject node = (GameObject)PrefabUtility.InstantiatePrefab(RequireEnvironmentPrefab(prop.prefab), parent);
            node.name = prop.name;
            node.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds bounds = GetPropGeometryBounds(node);
            Vector3 scale = new(prop.size.x / bounds.size.x, prop.size.y / bounds.size.y, prop.size.z / bounds.size.z);
            if (prop.preserveAspect) scale = Vector3.one * Mathf.Min(scale.x, scale.y, scale.z);
            node.transform.localScale = Vector3.Scale(node.transform.localScale, scale);
            node.transform.rotation = Quaternion.Euler(0, prop.yaw, 0);
            bounds = GetPropGeometryBounds(node);
            Vector3 ground = GroundPoint(prop.position, terrain) + Vector3.up * prop.position.y;
            if (prop.levelFootprint) LevelPropFootprint(terrain, ground, bounds.size, prop.foundationBlend);
            ground.y -= prop.foundationInset;
            node.transform.position += ground - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (Collider collider in node.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Light light in node.GetComponentsInChildren<Light>(true))
            {
                if (!prop.lightShadows) light.shadows = LightShadows.None;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            }
            if (prop.meshCollision)
            {
                foreach (MeshFilter filter in node.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    LODGroup lodGroup = filter.GetComponentInParent<LODGroup>();
                    if (lodGroup != null && !Array.Exists(lodGroup.GetLODs()[0].renderers,
                        renderer => renderer != null && renderer.gameObject == filter.gameObject)) continue;
                    filter.gameObject.layer = LayerMask.NameToLayer("Ground");
                    var collider = filter.GetComponent<MeshCollider>();
                    if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    collider.enabled = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter.gameObject);
                }
            }
            else if (prop.collision)
            {
                var collision = new GameObject(prop.name + "_Collision");
                collision.transform.SetParent(parent);
                collision.transform.position = ground + Vector3.up * bounds.size.y * 0.5f;
                collision.layer = LayerMask.NameToLayer("Ground");
                collision.AddComponent<BoxCollider>().size = bounds.size;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(node.transform);
            foreach (Collider collider in node.GetComponentsInChildren<Collider>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            return node;
        }

        private static void PlaceWorldMonsters(WorldLayout world, Terrain terrain, Transform root)
        {
            var groups = new Dictionary<string, Transform>();
            foreach (WorldMonster entry in world.monsters)
            {
                if (!groups.TryGetValue(entry.zone, out Transform group))
                {
                    group = new GameObject(entry.zone).transform;
                    group.SetParent(root);
                    var settings = new SerializedObject(group.gameObject.AddComponent<MonsterGroupController>());
                    settings.FindProperty("_meleeSlotCap").intValue = world.meleeSlots;
                    settings.FindProperty("_rangedSlotCap").intValue = world.rangedSlots;
                    settings.FindProperty("_breatherDuration").floatValue = world.breather;
                    settings.FindProperty("_playerBreatherDuration").floatValue = world.breather;
                    settings.FindProperty("_alertRadius").floatValue = world.alertRadius;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    groups.Add(entry.zone, group);
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab);
                var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                node.name = entry.id;
                node.SetActive(true);
                node.transform.SetPositionAndRotation(GroundPoint(entry.position, terrain), Quaternion.Euler(0, entry.yaw, 0));
                var identity = node.GetComponent<SceneEntityId>() ?? node.AddComponent<SceneEntityId>();
                identity.EditorSetGuid(entry.id);
                PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
            }
        }

        private static void PlaceWorldInteractions(WorldLayout world, Terrain terrain, Transform root)
        {
            Transform parent = new GameObject("Interactions").transform;
            parent.SetParent(root);
            foreach (WorldInteraction entry in world.interactions)
            {
                var data = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(entry.data);
                if (data == null) throw new InvalidOperationException("상호작용 데이터 누락: " + entry.data);
                var node = new GameObject(entry.name);
                node.transform.SetParent(parent);
                node.transform.position = GroundPoint(entry.position, terrain);
                node.layer = LayerMask.NameToLayer("InteractableObject");
                var collider = node.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(2.5f, 2, 2.5f);
                collider.center = Vector3.up;
                MonoBehaviour actor = entry.kind == "Rest" ? node.AddComponent<RestPointActor>() : node.AddComponent<GatheringActor>();
                var settings = new SerializedObject(actor);
                settings.FindProperty(entry.kind == "Rest" ? "_data" : "_interactableData").objectReferenceValue = data;
                settings.ApplyModifiedPropertiesWithoutUndo();
                node.AddComponent<SceneEntityId>().EditorSetGuid(entry.id);
                PlaceWorldProp(new WorldProp { name = "Visual", prefab = entry.prefab, position = entry.position, size = entry.size }, terrain, node.transform);
            }
        }

        private static void PlaceWorldPortals(WorldLayout world, Layout layout, Terrain terrain, Transform root)
        {
            var region = AssetDatabase.LoadAssetAtPath<MapRegionInfoSO>(layout.assetDirectory + "/RegionInfo.asset");
            var arrivals = new Dictionary<string, Transform>();
            foreach (WorldPortal entry in world.portals)
            {
                var arrival = new GameObject(entry.id + "_Arrival").transform;
                arrival.SetParent(root);
                arrival.position = GroundPoint(entry.arrival, terrain) + Vector3.up * 0.1f;
                var settings = new SerializedObject(arrival.gameObject.AddComponent<SceneArrivalPoint>());
                settings.FindProperty("_id").stringValue = entry.id;
                settings.ApplyModifiedPropertiesWithoutUndo();
                arrivals.Add(entry.id, arrival);
            }
            foreach (WorldPortal entry in world.portals)
            {
                if (!arrivals.ContainsKey(entry.destinationId)) throw new InvalidOperationException("포탈 목적지 누락: " + entry.id);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab);
                var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                node.name = entry.id;
                node.transform.position = GroundPoint(entry.position, terrain);
                var portal = node.GetComponentInChildren<PortalActor>(true);
                foreach (Light light in node.GetComponentsInChildren<Light>(true))
                {
                    if (!entry.lightShadows) light.shadows = LightShadows.None;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                }
                if (portal == null) throw new InvalidOperationException("PortalActor 누락: " + entry.prefab);
                var settings = new SerializedObject(portal);
                settings.FindProperty("_portalType").enumValueIndex = (int)PortalType.InMapTeleport;
                settings.FindProperty("_destinationPoint").objectReferenceValue = arrivals[entry.destinationId];
                settings.FindProperty("_mapArrivalPoint").objectReferenceValue = arrivals[entry.id];
                settings.FindProperty("_activationId").stringValue = entry.id;
                settings.FindProperty("_targetArrivalId").stringValue = entry.id;
                settings.FindProperty("_mapLabel").stringValue = entry.label;
                settings.FindProperty("_requiresActivation").boolValue = true;
                settings.FindProperty("_startsActivated").boolValue = entry.startsActivated;
                settings.FindProperty("_isActive").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node.transform);
                region.portals.RemoveAll(item => item.activationId == entry.id);
                region.portals.Add(new MapRegionInfoSO.PortalEntry { label = entry.label, worldPosition = node.transform.position,
                    targetSceneName = layout.mapId, arrivalId = entry.id, activationId = entry.id, requiresActivation = true, startsActivated = entry.startsActivated });
            }
            EditorUtility.SetDirty(region);
        }

        private static void ValidateWorld(WorldLayout world, Terrain terrain, Transform root)
        {
            var report = new WorldReport();
            var routes = new ValidationReport();
            foreach (Route route in world.routes) ValidateRoute(route, terrain, routes);
            report.connectedRoutes = routes.connectedRoutes;
            report.errors.AddRange(routes.errors);
            var ids = new HashSet<string>();
            foreach (SceneEntityId identity in UnityEngine.Object.FindObjectsByType<SceneEntityId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!ids.Add(identity.Guid)) report.errors.Add("중복 저장 ID: " + identity.Guid);
            foreach (WorldMonster entry in world.monsters)
            {
                if (!NavMesh.SamplePosition(GroundPoint(entry.position, terrain), out _, 2, NavMesh.AllAreas)) report.errors.Add("몬스터 NavMesh 누락: " + entry.id);
                report.monsters++;
            }
            foreach (WorldInteraction entry in world.interactions)
            {
                if (!NavMesh.SamplePosition(GroundPoint(entry.position, terrain), out _, 2, NavMesh.AllAreas)) report.errors.Add("상호작용 접근 누락: " + entry.id);
                report.interactions++;
            }
            foreach (WorldPortal entry in world.portals)
            {
                Vector3 arrival = GroundPoint(entry.arrival, terrain);
                if (!NavMesh.SamplePosition(arrival, out _, 1.5f, NavMesh.AllAreas)) report.errors.Add("도착점 NavMesh 누락: " + entry.id);
                foreach (PortalActor portal in root.GetComponentsInChildren<PortalActor>())
                    if (portal.GetComponent<Collider>().bounds.Contains(arrival + Vector3.up)) report.errors.Add("포탈 도착점이 트리거 내부: " + entry.id);
                report.portals++;
            }
            File.WriteAllText(ReportDirectory + "/WorldExpansion.json", JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new InvalidOperationException(string.Join("\n", report.errors));
        }
    }
}
