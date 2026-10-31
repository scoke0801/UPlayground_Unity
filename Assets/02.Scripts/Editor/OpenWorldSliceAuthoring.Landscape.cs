using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        [Serializable]
        private sealed class LandscapeRoute
        {
            public string id, experience;
            public Vector3[] points;
            public float halfWidth = 3.2f;
            public bool followTerrain;
        }

        [Serializable]
        private sealed class Landform
        {
            public string id;
            public Vector3[] points;
            public float innerWidth, outerWidth;
            public bool exposedBed;
            public float widthVariation;
            public float heightBlendDistance = 60;
        }

        [Serializable]
        private sealed class LandscapeProp
        {
            public string id, prefab;
            public Vector3 position, scale = Vector3.one;
            public float yaw, groundOffset;
            public float pitch;
            public float slopeEmbedDepth;
            public bool useAbsoluteHeight, alignBounds, solidBounds;
            public bool castShadows = true;
            public int particleLimit;
            public Color particleColor = Color.white;
        }

        [Serializable]
        private sealed class LandscapeRecipe
        {
            public Vector3 worldOrigin, worldSize;
            public int worldResolution = 2049;
            public float edgeTransition = 60;
            public float reliefAmplitude, reliefFrequency;
            public string groundCoverPrefab;
            public float groundCoverDistance = 110;
            public int groundCoverDensity = 2;
            public float westHeight, eastHeight, westX, eastX;
            public float transitionStart, transitionEnd;
            public string grassLayer, earthLayer, rockLayer, riverLayer;
            public Landform[] landforms;
            public LandscapeProp[] props = Array.Empty<LandscapeProp>();
            public string manualArtPrefab;
            public bool includeArt;
            public RegionRect northernFoundation;
            public float foundationTransition;
            public float maximumRoadEarthwork = 0.6f;
            public GroundPlatform[] platforms = Array.Empty<GroundPlatform>();
            public ExplorationPickup[] pickups = Array.Empty<ExplorationPickup>();
            public ExplorationGatherable[] gatherables = Array.Empty<ExplorationGatherable>();
            public ExplorationEncounter[] encounters = Array.Empty<ExplorationEncounter>();
            public DungeonTerrainPatch[] dungeonGround = Array.Empty<DungeonTerrainPatch>();
        }

        [Serializable]
        private sealed class GroundPlatform
        {
            public RegionRect bounds;
            public float height, transition;
        }

        [Serializable]
        private sealed class ExplorationPickup
        {
            public string id, item, model;
            public Vector3 position;
            public int count = 1;
            public bool useAbsoluteHeight;
        }

        [Serializable]
        private sealed class ExplorationEncounter
        {
            public string id, prefab;
            public Vector3 position;
            public float yaw;
        }

        [Serializable]
        private sealed class ExplorationGatherable
        {
            public string id, model, interaction;
            public Vector3 position, scale = Vector3.one, colliderSize = Vector3.one;
            public float yaw;
        }

        private static IEnumerable<Vector3[]> GetRoadLines(CityRecipe recipe)
        {
            foreach (var city in recipe.cities) yield return city.road;
            foreach (var route in recipe.routes) yield return route.points;
        }

        private static void ValidateLandscape(CityRecipe recipe)
        {
            var landscape = recipe.landscape;
            if (landscape == null || landscape.transitionEnd <= landscape.transitionStart
                || landscape.eastX <= landscape.westX || landscape.landforms == null)
                throw new InvalidOperationException("월드 지형 recipe의 범위가 올바르지 않습니다.");
            if (landscape.worldSize != Vector3.zero && (landscape.worldSize.x <= 0 || landscape.worldSize.y <= 0
                || landscape.worldSize.z <= 0 || landscape.edgeTransition <= 0
                || landscape.worldResolution < 33 || landscape.worldResolution > 4097
                || !Mathf.IsPowerOfTwo(landscape.worldResolution - 1)))
                throw new InvalidOperationException("확장 월드의 크기·해상도·접합 폭을 확인하세요.");
            foreach (var city in recipe.cities)
                if (city.preservedGround == null || city.groundTransition <= 0)
                    throw new InvalidOperationException("도시 기초 보존 영역이 필요합니다: " + city.mapId);
            foreach (var form in landscape.landforms)
            {
                if (form.outerWidth <= form.innerWidth || form.innerWidth < 0)
                    throw new InvalidOperationException("지형 전이 폭이 올바르지 않습니다: " + form.id);
                ValidateLine(form.points, form.id);
            }
            foreach (var route in recipe.routes) ValidateLine(route.points, route.id);
            if (landscape.maximumRoadEarthwork < 0 || landscape.maximumRoadEarthwork > 1)
                throw new InvalidOperationException("도로 정지량은 0~1m여야 합니다. 큰 높이차는 산체와 노선을 수정하세요.");
            foreach (var platform in landscape.platforms)
                if (platform.bounds == null || platform.bounds.width <= 0 || platform.bounds.height <= 0 || platform.transition <= 0)
                    throw new InvalidOperationException("구조물 기초 범위와 지형 전이 폭을 확인하세요.");
            var pickupIds = new HashSet<string>();
            foreach (var pickup in landscape.pickups)
                if (string.IsNullOrEmpty(pickup.id) || !pickupIds.Add(pickup.id) || pickup.count <= 0
                    || AssetDatabase.LoadAssetAtPath<Data.Item.ItemSO>(pickup.item) == null
                    || AssetDatabase.LoadAssetAtPath<GameObject>(pickup.model) == null)
                    throw new InvalidOperationException("탐험 보상의 고유 ID·아이템·모델을 확인하세요: " + pickup.id);
            foreach (var encounter in landscape.encounters)
                if (string.IsNullOrEmpty(encounter.id) || !pickupIds.Add(encounter.id)
                    || AssetDatabase.LoadAssetAtPath<GameObject>(encounter.prefab)?.GetComponent<MonsterActor>() == null)
                    throw new InvalidOperationException("탐험 조우의 고유 ID·몬스터 프리팹을 확인하세요: " + encounter.id);
            foreach (var gathering in landscape.gatherables)
                if (string.IsNullOrEmpty(gathering.id) || !pickupIds.Add(gathering.id)
                    || AssetDatabase.LoadAssetAtPath<GameObject>(gathering.model) == null
                    || AssetDatabase.LoadAssetAtPath<Data.Actor.InteractableActorSO>(gathering.interaction) == null
                    || gathering.colliderSize.x <= 0 || gathering.colliderSize.y <= 0 || gathering.colliderSize.z <= 0)
                    throw new InvalidOperationException("탐험 채집의 ID·모델·상호작용·충돌을 확인하세요: " + gathering.id);
            foreach (var prop in landscape.props)
                if (string.IsNullOrEmpty(prop.id) || AssetDatabase.LoadAssetAtPath<GameObject>(prop.prefab) == null)
                    throw new InvalidOperationException("환경 프리팹을 찾을 수 없습니다: " + prop.id);
            if (!string.IsNullOrEmpty(landscape.manualArtPrefab)
                && AssetDatabase.LoadAssetAtPath<GameObject>(landscape.manualArtPrefab) == null)
                throw new InvalidOperationException("수작업 환경 프리팹을 찾을 수 없습니다.");
            if (!string.IsNullOrEmpty(landscape.groundCoverPrefab)
                && AssetDatabase.LoadAssetAtPath<GameObject>(landscape.groundCoverPrefab)?.GetComponent<MeshRenderer>() == null)
                throw new InvalidOperationException("지표 식생은 루트 MeshRenderer를 가진 Detail 프리팹이 필요합니다.");
        }

        private static void ValidateLine(Vector3[] points, string label)
        {
            if (points == null || points.Length < 2) throw new InvalidOperationException("경로 점이 부족합니다: " + label);
            for (int i = 1; i < points.Length; i++)
                if (new Vector2(points[i].x - points[i - 1].x, points[i].z - points[i - 1].z).sqrMagnitude < 0.01f)
                    throw new InvalidOperationException("경로에 중복 점이 있습니다: " + label);
        }

        private static float GetLineDistance(Vector3 position, Vector3[] points, out float height)
        {
            float distance = float.MaxValue;
            height = 0;
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 a = points[i - 1], b = points[i];
                var direction = new Vector2(b.x - a.x, b.z - a.z);
                var offset = new Vector2(position.x - a.x, position.z - a.z);
                float progress = Mathf.Clamp01(Vector2.Dot(offset, direction) / direction.sqrMagnitude);
                float candidate = (offset - direction * progress).magnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                height = Mathf.Lerp(a.y, b.y, progress);
            }
            return distance;
        }

        private static float SampleLandscape(Vector3 position, LandscapeRecipe recipe)
        {
            float height = Mathf.Lerp(recipe.westHeight, recipe.eastHeight,
                Mathf.SmoothStep(0, 1, Mathf.InverseLerp(recipe.westX, recipe.eastX, position.x)));
            foreach (var form in recipe.landforms)
            {
                float distance = GetLandformDistance(position, form, out float target);
                distance /= 1 + form.widthVariation * (Mathf.PerlinNoise(position.x * 0.006f, position.z * 0.006f) - 0.5f);
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(form.innerWidth, form.outerWidth, distance));
                height = Mathf.Lerp(height, target, blend);
            }
            height += recipe.reliefAmplitude * (Mathf.PerlinNoise(position.x * recipe.reliefFrequency,
                position.z * recipe.reliefFrequency) - 0.5f) * 2;
            return height;
        }

        private static float GetLandformDistance(Vector3 position, Landform form, out float height)
        {
            float distance = float.MaxValue, weightedHeight = 0, totalWeight = 0;
            float smoothing = Mathf.Max(1, form.heightBlendDistance);
            for (int i = 1; i < form.points.Length; i++)
            {
                Vector3 a = form.points[i - 1], b = form.points[i];
                var direction = new Vector2(b.x - a.x, b.z - a.z);
                var offset = new Vector2(position.x - a.x, position.z - a.z);
                float progress = Mathf.Clamp01(Vector2.Dot(offset, direction) / direction.sqrMagnitude);
                float squaredDistance = (offset - direction * progress).sqrMagnitude;
                distance = Mathf.Min(distance, squaredDistance);
                // 최근접 선분이 바뀌는 이등분선의 높이 점프가 산체 전체에 절개선을 만들지 않게 한다.
                float weight = 1 / (squaredDistance + smoothing * smoothing);
                weight *= weight;
                weightedHeight += Mathf.Lerp(a.y, b.y, progress) * weight;
                totalWeight += weight;
            }
            height = weightedHeight / totalWeight;
            return Mathf.Sqrt(distance);
        }

        private static float GetGroundPreservation(Vector3 source, CityPlacement city)
        {
            return GetRectanglePreservation(source, city.preservedGround, city.groundTransition);
        }

        private static void ReshapeNorthTransition(Terrain terrain, CityRecipe recipe)
        {
            var data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            for (int z = 0; z < resolution; z++)
            {
                float worldZ = terrain.transform.position.z + z * data.size.z / (resolution - 1);
                if (worldZ >= recipe.landscape.transitionEnd) break;
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(
                    recipe.landscape.transitionStart, recipe.landscape.transitionEnd, worldZ));
                for (int x = 0; x < resolution; x++)
                {
                    Vector3 p = terrain.transform.position + new Vector3(x * data.size.x / (resolution - 1), 0, worldZ - terrain.transform.position.z);
                    float target = (SampleLandscape(p, recipe.landscape) - terrain.transform.position.y) / data.size.y;
                    heights[z, x] = Mathf.Lerp(heights[z, x], target, blend * (1 - GetNorthernPreservation(p, recipe.landscape)));
                }
            }
            data.SetHeights(0, 0, heights);
            PaintLandscape(terrain, recipe);
        }

        private static float GetNorthernPreservation(Vector3 position, LandscapeRecipe recipe)
        {
            return GetRectanglePreservation(position, recipe.northernFoundation, recipe.foundationTransition);
        }

        private static float GetRectanglePreservation(Vector3 position, RegionRect bounds, float transition)
        {
            if (bounds == null) return 0;
            Rect area = bounds.Value;
            float dx = Mathf.Max(area.xMin - position.x, 0, position.x - area.xMax);
            float dz = Mathf.Max(area.yMin - position.z, 0, position.z - area.yMax);
            return 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(0.01f, transition)));
        }

        private static void PaintLandscape(Terrain terrain, CityRecipe recipe)
        {
            var data = terrain.terrainData;
            var layers = data.terrainLayers.ToList();
            var paths = new[] { recipe.landscape.grassLayer, recipe.landscape.earthLayer, recipe.landscape.rockLayer, recipe.landscape.riverLayer };
            var indices = new int[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(paths[i]);
                if (layer == null) throw new InvalidOperationException("지표 레이어 누락: " + paths[i]);
                if (!layers.Contains(layer)) layers.Add(layer);
                indices[i] = layers.IndexOf(layer);
            }
            data.terrainLayers = layers.ToArray();
            var weights = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            for (int z = 0; z < data.alphamapHeight; z++)
            for (int x = 0; x < data.alphamapWidth; x++)
            {
                float u = x / (float)(data.alphamapWidth - 1), v = z / (float)(data.alphamapHeight - 1);
                var p = terrain.transform.position + new Vector3(u * data.size.x, 0, v * data.size.z);
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(recipe.landscape.transitionStart, recipe.landscape.transitionEnd, p.z));
                foreach (var city in recipe.cities) blend *= 1 - GetGroundPreservation(city.ToSource(p), city);
                if (blend <= 0) continue;
                float rock = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(22, 42, data.GetSteepness(u, v)));
                float earth = Mathf.Lerp(0.12f, 0.55f, Mathf.InverseLerp(63, 46, terrain.SampleHeight(p) + terrain.transform.position.y));
                for (int layer = 0; layer < data.alphamapLayers; layer++) weights[z, x, layer] *= 1 - blend;
                weights[z, x, indices[0]] += blend * (1 - rock) * (1 - earth);
                weights[z, x, indices[1]] += blend * (1 - rock) * earth;
                weights[z, x, indices[2]] += blend * rock;
                foreach (var form in recipe.landscape.landforms)
                {
                    if (!form.exposedBed) continue;
                    float distance = GetLineDistance(p, form.points, out _);
                    float bed = blend * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(form.innerWidth, form.outerWidth * 0.6f, distance)));
                    for (int layer = 0; layer < data.alphamapLayers; layer++) weights[z, x, layer] *= 1 - bed;
                    weights[z, x, indices[3]] += bed;
                }
            }
            data.SetAlphamaps(0, 0, weights);
        }

        private static float GetRoadHalfWidth(Vector3 position, CityRecipe recipe)
        {
            float distance = float.MaxValue, width = recipe.roadHalfWidth;
            foreach (var city in recipe.cities) distance = Mathf.Min(distance, GetLineDistance(position, city.road, out _));
            foreach (var route in recipe.routes)
            {
                float candidate = GetLineDistance(position, route.points, out _);
                if (candidate >= distance) continue;
                distance = candidate;
                width = route.halfWidth;
            }
            return width;
        }

        private static float BlendRoadHeight(Vector3 position, CityRecipe recipe, out float height)
        {
            float weightedHeight = 0, totalWeight = 0, influence = 0;
            foreach (var city in recipe.cities)
                AccumulateRoadHeight(position, city.road, recipe.roadHalfWidth, recipe.roadShoulder,
                    ref weightedHeight, ref totalWeight, ref influence);
            foreach (var route in recipe.routes)
            {
                // 탐험로는 산체를 따라간다. 길의 저작 Y로 산을 얇게 밀어 올리지 않는다.
                if (route.followTerrain) continue;
                AccumulateRoadHeight(position, route.points, route.halfWidth, recipe.roadShoulder,
                    ref weightedHeight, ref totalWeight, ref influence);
            }
            height = totalWeight > 0 ? weightedHeight / totalWeight : 0;
            return influence;
        }

        private static void AccumulateRoadHeight(Vector3 position, Vector3[] points, float width, float shoulder,
            ref float weightedHeight, ref float totalWeight, ref float influence)
        {
            float distance = GetLineDistance(position, points, out float target);
            float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(width, width + shoulder, distance));
            // 두 경로의 최근접 구간이 바뀌는 선에서 높이가 불연속으로 바뀌지 않도록 함께 보간한다.
            float weight = blend * blend * blend * blend;
            weightedHeight += target * weight;
            totalWeight += weight;
            influence = Mathf.Max(influence, blend);
        }

        private static void BuildLandscapeArt(Transform environment, Terrain[] terrains, CityRecipe recipe)
        {
            if (!recipe.landscape.includeArt) return;
            ApplyDungeonGround(terrains, recipe.landscape);
            PaintDungeonGround(terrains, recipe);
            var root = new GameObject("AuthoredLandscape").transform;
            root.SetParent(environment, false);
            var report = new List<string>();
            foreach (var prop in recipe.landscape.props)
            {
                if (!TryGetGround(terrains, prop.position, out float height))
                    throw new InvalidOperationException("환경 배치가 지면 범위 밖입니다: " + prop.id);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prop.prefab);
                var item = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
                item.name = prop.id;
                item.transform.localScale = prop.scale;
                if (prop.useAbsoluteHeight) height = prop.position.y;
                item.transform.SetPositionAndRotation(new Vector3(prop.position.x, height + prop.groundOffset, prop.position.z),
                    Quaternion.Euler(prop.pitch, prop.yaw, 0));
                if (prop.alignBounds)
                {
                    var bounds = GetPropMeshBounds(item);
                    item.transform.position += new Vector3(prop.position.x - bounds.center.x,
                        height + prop.groundOffset - bounds.min.y, prop.position.z - bounds.center.z);
                }
                if (prop.slopeEmbedDepth > 0)
                {
                    var bounds = GetPropMeshBounds(item);
                    float lowestGround = height;
                    foreach (float x in new[] {bounds.min.x, bounds.center.x, bounds.max.x})
                    foreach (float z in new[] {bounds.min.z, bounds.center.z, bounds.max.z})
                        if (TryGetGround(terrains, new Vector3(x, 0, z), out float sample))
                            lowestGround = Mathf.Min(lowestGround, sample);
                    item.transform.position += Vector3.up * Mathf.Min(0, lowestGround - bounds.min.y - prop.slopeEmbedDepth);
                }
                if (prop.solidBounds)
                {
                    var bounds = GetPropMeshBounds(item);
                    // 양면 보행을 막아야 하는 얇은 벽 모듈은 단면 MeshCollider에 의존하지 않는다.
                    var support = new GameObject("WallCollision");
                    support.transform.position = bounds.center;
                    support.transform.SetParent(item.transform, true);
                    support.AddComponent<BoxCollider>().size = Vector3.Max(bounds.size, Vector3.one * 0.3f);
                }
                if (item.GetComponentsInChildren<Components.SceneEntityId>(true).Length > 0)
                    throw new InvalidOperationException("장식 recipe에 저장 개체를 복제할 수 없습니다: " + prop.id);
                if (!prop.castShadows)
                    foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (prop.particleLimit > 0)
                    foreach (var particles in item.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = particles.main;
                        var emission = particles.emission;
                        emission.rateOverTimeMultiplier *= prop.particleLimit / (float)Mathf.Max(1, main.maxParticles);
                        main.maxParticles = prop.particleLimit;
                        main.startColor = prop.particleColor;
                        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    }
                report.Add(prop.id + " | " + prop.prefab + " | " + item.transform.position + " | 접지 오프셋=" + prop.groundOffset);
            }
            if (!string.IsNullOrEmpty(recipe.landscape.manualArtPrefab))
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(recipe.landscape.manualArtPrefab), root);
            BuildExplorationPickups(environment, terrains, recipe.landscape);
            File.WriteAllLines(ReportFolder + "/landscape-art.txt", report);
        }

        private static Bounds GetPropMeshBounds(GameObject item)
        {
            // 정지한 ParticleSystemRenderer의 원점 bounds는 구조물의 접지 기준이 아니다.
            var renderers = item.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("접지할 메시가 없습니다: " + item.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void BuildExplorationPickups(Transform environment, Terrain[] terrains, LandscapeRecipe recipe)
        {
            // 장식과 소비되는 개체를 분리해 기존 지역 소유권·월드 소비 저장 계약을 사용한다.
            var root = new GameObject("ExplorationDiscoveries").transform;
            root.SetParent(environment.parent, false);
            foreach (var pickup in recipe.pickups)
            {
                if (!TryGetGround(terrains, pickup.position, out float height))
                    throw new InvalidOperationException("탐험 보상이 지형 밖입니다: " + pickup.id);
                var go = new GameObject(pickup.id);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(pickup.position.x, pickup.useAbsoluteHeight ? pickup.position.y : height, pickup.position.z);
                int layer = LayerMask.NameToLayer("InteractableObject");
                if (layer < 0) throw new InvalidOperationException("InteractableObject 레이어가 없습니다.");
                go.layer = layer;
                var collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.8f; collider.center = Vector3.up * 0.6f;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pickup.model), go.transform);
                visual.transform.localPosition = Vector3.zero;
                foreach (var childCollider in visual.GetComponentsInChildren<Collider>()) childCollider.enabled = false;
                var actor = go.AddComponent<DropItemActor>();
                actor.Init(AssetDatabase.LoadAssetAtPath<Data.Item.ItemSO>(pickup.item), pickup.count,
                    AssetDatabase.LoadAssetAtPath<Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/DropItem.asset"));
                go.AddComponent<Components.SceneEntityId>().EditorSetGuid(pickup.id);
            }
            BuildExplorationGatherables(root, terrains, recipe);
            foreach (var encounter in recipe.encounters)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(encounter.prefab), root);
                go.name = encounter.id;
                go.transform.SetPositionAndRotation(encounter.position, Quaternion.Euler(0, encounter.yaw, 0));
                var id = go.GetComponent<Components.SceneEntityId>();
                if (id == null) id = go.AddComponent<Components.SceneEntityId>();
                id.EditorSetGuid(encounter.id);
            }
        }

        private static void BuildExplorationGatherables(Transform root, Terrain[] terrains, LandscapeRecipe recipe)
        {
            foreach (var gathering in recipe.gatherables)
            {
                if (!TryGetGround(terrains, gathering.position, out float height))
                    throw new InvalidOperationException("탐험 채집이 지형 밖입니다: " + gathering.id);
                var go = new GameObject(gathering.id);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(gathering.position.x, height, gathering.position.z);
                go.layer = LayerMask.NameToLayer("InteractableObject");
                var collider = go.AddComponent<BoxCollider>();
                collider.size = gathering.colliderSize;
                collider.center = Vector3.up * gathering.colliderSize.y * 0.5f;
                // 소진되는 Collider는 NavMesh의 고정 장벽으로 굽지 않는다. 보행 우회 공간은 항상 남긴다.
                collider.isTrigger = true;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(gathering.model), go.transform);
                visual.transform.localScale = gathering.scale;
                visual.transform.rotation = Quaternion.Euler(0, gathering.yaw, 0);
                visual.transform.position = go.transform.position;
                var bounds = GetPropMeshBounds(visual);
                visual.transform.position += new Vector3(go.transform.position.x - bounds.center.x,
                    height - bounds.min.y, go.transform.position.z - bounds.center.z);
                foreach (var childCollider in visual.GetComponentsInChildren<Collider>()) childCollider.enabled = false;
                go.AddComponent<GatheringActor>().Init(
                    AssetDatabase.LoadAssetAtPath<Data.Actor.InteractableActorSO>(gathering.interaction));
                go.AddComponent<Components.SceneEntityId>().EditorSetGuid(gathering.id);
            }
        }
    }
}
