using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Manager.World;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        private const string CityRecipePath = "Tools/OpenWorld/city-layout.json";
        private static readonly string[] WorldMapIds = { "LakeOfLifeScenario", "DroughtVillage", "StonegateCitadel", "SaltwindHarbor" };

        [Serializable]
        private sealed class CityRecipe
        {
            public Vector3 southOrigin, southSize;
            public int heightResolution;
            public float roadHalfWidth, roadShoulder;
            public float navigationVoxelSize;
            public string navigationOffRoadArea;
            public float navigationPreferredWidth = 16;
            public RegionRect[] navigationOpenAreas = Array.Empty<RegionRect>();
            public CityPlacement[] cities;
            public PropRelocation[] relocations = Array.Empty<PropRelocation>();
            public LandscapeRecipe landscape;
            public LandscapeRoute[] routes = Array.Empty<LandscapeRoute>();
        }

        [Serializable]
        private sealed class PropRelocation
        {
            public string path;
            public Vector2 position;
        }

        [Serializable]
        private sealed class RegionRect
        {
            public float x, y, width, height;
            public Rect Value => new Rect(x,y,width,height);
        }

        [Serializable]
        private sealed class CityPlacement
        {
            public string mapId, sourcePath, regionPath, sceneKey;
            public Vector3 offset;
            public float yaw;
            public RegionRect bounds;
            public Vector3[] road;
            public RegionRect preservedGround;
            public float groundTransition = 30;
            public float sourceGroundCeiling;
            public RegionRect raisedFoundation;
            public float raisedFoundationTransition = 16;
            public RegionRect seaBounds;
            public float seaHeight;
            public string[] groundedProps = Array.Empty<string>();
            public float groundedPropOffset;
            public Vector3 ToWorld(Vector3 point) => offset + Quaternion.Euler(0, yaw, 0) * point;
            public Vector3 ToSource(Vector3 point) => Quaternion.Euler(0, -yaw, 0) * (point - offset);
        }

        private static CityRecipe LoadCityRecipe()
        {
            var recipe = JsonUtility.FromJson<CityRecipe>(File.ReadAllText(CityRecipePath));
            if (recipe?.cities == null || recipe.cities.Length != 2 || recipe.roadHalfWidth <= 0)
                throw new InvalidOperationException("도시 연결 recipe가 올바르지 않습니다.");
            foreach (var city in recipe.cities)
                if (!File.Exists(city.sourcePath) || city.road == null || city.road.Length < 2
                    || city.bounds == null || city.bounds.width <= 0 || city.bounds.height <= 0)
                    throw new InvalidOperationException("도시 원본 또는 연결 길이 없습니다: " + city.mapId);
            ValidateLandscape(recipe);
            return recipe;
        }

        private static void BuildCities(Scene host, ContinuousWorldController world, CityRecipe recipe,
            List<Scene> scenes, Dictionary<Components.SceneEntityId, string> ids)
        {
            var sourceTerrains = new List<Terrain>();
            var environment = host.GetRootGameObjects().Single(r => r.name == "AuthoredEnvironment").transform;
            var manifest = new List<string> { "원본 지역→월드: 회전 후 offset, 원본 씬·ID 보존", File.ReadAllText(CityRecipePath) };
            foreach (var city in recipe.cities)
            {
                CopySourceScene(city.sourcePath, city.regionPath);
                var scene = EditorSceneManager.OpenScene(city.regionPath, OpenSceneMode.Additive);
                scenes.Add(scene);
                sourceTerrains.Add(Components<Terrain>(scene).Single());
                manifest.Add(city.sourcePath + " SHA256=" + Hash(city.sourcePath));
                foreach (var id in Components<Components.SceneEntityId>(scene))
                {
                    ids.Add(id, id.Guid);
                    manifest.Add(city.mapId + " | " + id.Guid + " | " + id.name + " | "
                        + id.transform.position.ToString("F4") + " -> " + city.ToWorld(id.transform.position).ToString("F4"));
                }
            }

            Terrain lake = Components<Terrain>(host).Single();
            Terrain south = CreateSouthTerrain(host, environment, lake, sourceTerrains, recipe);
            ReshapeNorthTransition(lake, recipe);
            world.SurfaceRecoveryBounds = new Rect(recipe.southOrigin.x, recipe.southOrigin.z,
                recipe.southSize.x, recipe.landscape.transitionEnd - recipe.southOrigin.z);
            world.SurfaceRecoveryTerrains = new[] { lake, south };
            StampRoads(lake, recipe);
            StampRoads(south, recipe);
            var regions = world.Regions.ToList();
            for (int i = 0; i < recipe.cities.Length; i++)
            {
                CityPlacement city = recipe.cities[i];
                Scene scene = scenes[i];
                TerrainData sourceData = sourceTerrains[i].terrainData;
                UnityEngine.Object.DestroyImmediate(sourceTerrains[i].gameObject);
                foreach (var nav in Components<Unity.AI.Navigation.NavMeshSurface>(scene)) UnityEngine.Object.DestroyImmediate(nav);
                foreach (var root in scene.GetRootGameObjects())
                {
                    root.transform.position = city.ToWorld(root.transform.position);
                    root.transform.rotation = Quaternion.Euler(0, city.yaw, 0) * root.transform.rotation;
                }
                Transform art = scene.GetRootGameObjects().Single(r => r.name == "CityEnvironment").transform;
                // 생활 콘텐츠는 원래 지역 씬에 남겨 mapId 소유권과 언로드 수명을 유지한다.
                foreach (var id in art.GetComponentsInChildren<Components.SceneEntityId>(true))
                    if (id.GetComponentInParent<Components.SceneEntityId>() == id) id.transform.SetParent(null, true);
                foreach (var actor in art.GetComponentsInChildren<GameActor>(true)) actor.transform.SetParent(null, true);
                art.SetParent(null, true);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(art.gameObject, host);
                art.SetParent(environment, true);
                art.name = city.mapId + "_Environment";
                LimitSea(art, city);
                GroundCityNature(art, south, sourceData, city);
                GroundCityProps(scene, south, city);
                BuildContent(host, scene, city.mapId);
                foreach (var runner in Components<FlowGraph.FlowGraphRunner>(scene)) runner.OriginSceneName = city.mapId;
                var region = new ContinuousWorldRegion {
                    MapId = city.mapId, LegacySceneName = city.mapId, SceneKey = city.sceneKey,
                    SourceOffset = city.offset, SourceYaw = city.yaw, Bounds = city.bounds.Value,
                    SafeEntryPosition = city.road[city.road.Length - 3],
                    NeighborEntryPosition = city.road[city.road.Length - 1],
                    Grounds = new Collider[] {lake.GetComponent<TerrainCollider>(), south.GetComponent<TerrainCollider>()},
                    PreloadDistance = 80, UnloadDistance = 110
                };
                region.EntryBarrier = CreateEntryBarrier(host, region.Bounds);
                regions.Add(region);
            }
            world.Regions = regions.ToArray();
            foreach(var relocation in recipe.relocations)
            {
                Transform prop=environment.Find(relocation.path);
                if(prop==null || prop.GetComponentsInChildren<Components.SceneEntityId>(true).Length>0)
                    throw new InvalidOperationException("이동할 장식 소품을 확인하세요: "+relocation.path);
                Vector3 before=prop.position;
                Vector3 after=new Vector3(relocation.position.x,before.y,relocation.position.y);
                after.y+=lake.SampleHeight(after)-lake.SampleHeight(before);
                prop.position=after;
                manifest.Add("소품 이동 | "+relocation.path+" | "+before+" -> "+after);
            }
            // 편집 중 옮긴 도시 Collider의 물리 bounds도 새 월드 좌표로 갱신한다.
            MergeWorldGround(lake, south, world, recipe);
            Physics.SyncTransforms();
            ClearRoadObstacles(environment, recipe);
            RepairVegetation(environment, new[] {lake}, recipe);
            BuildLandscapeArt(environment, new[] {lake}, recipe);
            File.WriteAllLines(ReportFolder + "/city-manifest.txt", manifest);
        }

        private static Terrain CreateSouthTerrain(Scene host, Transform environment, Terrain lake,
            List<Terrain> sources, CityRecipe recipe)
        {
            var data = new TerrainData { heightmapResolution = recipe.heightResolution, size = recipe.southSize,
                alphamapResolution = 512, baseMapResolution = 1024 };
            int resolution = data.heightmapResolution;
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                Vector3 p = recipe.southOrigin + new Vector3(x * data.size.x / (resolution - 1), 0, z * data.size.z / (resolution - 1));
                float height = SampleLandscape(p, recipe.landscape);
                for (int index = 0; index < recipe.cities.Length; index++)
                {
                    var city = recipe.cities[index];
                    Vector3 local = city.ToSource(p);
                    float blend = GetGroundPreservation(local, city);
                    if (blend <= 0) continue;
                    var source = sources[index];
                    float original = source.terrainData.GetInterpolatedHeight(Mathf.Clamp01(local.x / source.terrainData.size.x),
                        Mathf.Clamp01(local.z / source.terrainData.size.z));
                    if (city.sourceGroundCeiling > 0)
                    {
                        float support = GetRectanglePreservation(local, city.raisedFoundation, city.raisedFoundationTransition);
                        original = Mathf.Lerp(Mathf.Min(original, city.sourceGroundCeiling), original, support);
                    }
                    original += city.offset.y;
                    height = Mathf.Lerp(height, original, blend);
                }
                heights[z,x] = (height - recipe.southOrigin.y) / data.size.y;
            }
            data.SetHeights(0, 0, heights);
            CopyCityGround(data, sources, recipe);
            string path = DataFolder + "/SouthTerrain.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (existing == null) { AssetDatabase.CreateAsset(data, path); existing = data; }
            else CopyGeneratedGround(data, existing);
            var root = Terrain.CreateTerrainGameObject(existing);
            root.name = "CityGround";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, host);
            root.transform.SetParent(environment, false);
            root.transform.position = recipe.southOrigin;
            var terrain = root.GetComponent<Terrain>();
            terrain.materialTemplate = lake.materialTemplate;
            PaintLandscape(terrain, recipe);
            return terrain;
        }

        private static void CopyCityGround(TerrainData target, List<Terrain> sources, CityRecipe recipe)
        {
            var layers = sources.SelectMany(t => t.terrainData.terrainLayers).Distinct().ToArray();
            target.terrainLayers = layers;
            var colors = sources.Select(t => t.terrainData.GetAlphamaps(0,0,t.terrainData.alphamapWidth,t.terrainData.alphamapHeight)).ToArray();
            var maps = sources.Select(t => t.terrainData.terrainLayers.Select(l => Array.IndexOf(layers,l)).ToArray()).ToArray();
            var result = new float[target.alphamapHeight,target.alphamapWidth,layers.Length];
            for (int z=0; z<target.alphamapHeight; z++)
            for (int x=0; x<target.alphamapWidth; x++)
            {
                Vector3 p = recipe.southOrigin + new Vector3(x*target.size.x/(target.alphamapWidth-1),0,z*target.size.z/(target.alphamapHeight-1));
                int index = p.x < recipe.cities[0].bounds.Value.xMax ? 0 : 1;
                var data = sources[index].terrainData;
                Vector3 local = recipe.cities[index].ToSource(p);
                int sx = Mathf.RoundToInt(Mathf.Clamp01(local.x/data.size.x)*(data.alphamapWidth-1));
                int sz = Mathf.RoundToInt(Mathf.Clamp01(local.z/data.size.z)*(data.alphamapHeight-1));
                for (int layer=0;layer<maps[index].Length;layer++) result[z,x,maps[index][layer]]=colors[index][sz,sx,layer];
            }
            target.SetAlphamaps(0,0,result);
        }

        private static float RoadDistance(Vector3 p, CityRecipe recipe, out float height)
        {
            float distance = float.MaxValue;
            height = 0;
            foreach (var points in GetRoadLines(recipe))
            for (int i=1;i<points.Length;i++)
            {
                Vector3 a = points[i-1], b = points[i];
                Vector2 delta = new Vector2(b.x-a.x,b.z-a.z), offset = new Vector2(p.x-a.x,p.z-a.z);
                float t = Mathf.Clamp01(Vector2.Dot(offset,delta)/delta.sqrMagnitude);
                float current = (offset-delta*t).magnitude;
                if (current >= distance) continue;
                distance = current;
                height = Mathf.Lerp(a.y,b.y,t);
            }
            return distance;
        }

        private static void StampRoads(Terrain terrain, CityRecipe recipe)
        {
            var data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            var points = GetRoadLines(recipe).SelectMany(line => line).ToArray();
            float margin = recipe.roadHalfWidth + recipe.roadShoulder;
            var roadBounds = Rect.MinMaxRect(points.Min(p => p.x) - margin, points.Min(p => p.z) - margin,
                points.Max(p => p.x) + margin, points.Max(p => p.z) + margin);
            var heights = data.GetHeights(0,0,resolution,resolution);
            for (int z=0;z<resolution;z++)
            for (int x=0;x<resolution;x++)
            {
                Vector3 p = terrain.transform.position+new Vector3(x*data.size.x/(resolution-1),0,z*data.size.z/(resolution-1));
                if (!roadBounds.Contains(new Vector2(p.x, p.z))) continue;
                float blend = BlendRoadHeight(p, recipe, out float height);
                blend *= 1 - GetNorthernPreservation(p, recipe.landscape);
                heights[z,x]=Mathf.Lerp(heights[z,x],(height-terrain.transform.position.y)/data.size.y,blend);
            }
            data.SetHeights(0,0,heights);
            var road = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_Dirt_02.terrainlayer");
            var layers = data.terrainLayers.ToList();
            if (!layers.Contains(road)) { layers.Add(road); data.terrainLayers=layers.ToArray(); }
            int roadIndex=layers.IndexOf(road);
            var weights=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
            for(int z=0;z<data.alphamapHeight;z++)
            for(int x=0;x<data.alphamapWidth;x++)
            {
                Vector3 p=terrain.transform.position+new Vector3(x*data.size.x/(data.alphamapWidth-1),0,z*data.size.z/(data.alphamapHeight-1));
                if (!roadBounds.Contains(new Vector2(p.x, p.z))) continue;
                float distance=RoadDistance(p,recipe,out _);
                float width = GetRoadHalfWidth(p, recipe);
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width-0.6f,width+1,distance));
                for(int layer=0;layer<data.alphamapLayers;layer++) weights[z,x,layer]*=1-blend;
                weights[z,x,roadIndex]+=blend;
            }
            data.SetAlphamaps(0,0,weights);
        }

        private static void LimitSea(Transform art, CityPlacement city)
        {
            foreach (var renderer in art.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.bounds.size.x < 1000 && renderer.bounds.size.z < 1000) continue;
                if (renderer.sharedMaterials.All(m => m == null || m.shader == null
                    || (m.shader.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) < 0
                        && m.shader.name.IndexOf("sea", StringComparison.OrdinalIgnoreCase) < 0)))
                    throw new InvalidOperationException("광역 렌더러의 용도를 확인해야 합니다: " + renderer.name);
                if (city.seaBounds == null) throw new InvalidOperationException("해안 수면 범위가 필요합니다: " + city.mapId);
                var transform = renderer.transform;
                Vector3 scale = transform.localScale;
                scale.x *= city.seaBounds.width / renderer.bounds.size.x;
                scale.z *= city.seaBounds.height / renderer.bounds.size.z;
                transform.localScale = scale;
                Rect sea = city.seaBounds.Value;
                transform.position = new Vector3(sea.center.x, city.seaHeight, sea.center.y);
            }
        }

        private static void GroundCityNature(Transform art, Terrain ground, TerrainData original, CityPlacement city)
        {
            var roots = new HashSet<Transform>();
            foreach (var renderer in art.GetComponentsInChildren<MeshRenderer>(true))
            {
                var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);
                Transform item = instance != null ? instance.transform : renderer.transform;
                string label = item.name.ToLowerInvariant();
                if (IsVegetation(item) || label.Contains("rock")) roots.Add(item);
            }
            foreach (Transform child in art) if (IsVegetation(child)) roots.Add(child);
            foreach (var child in roots.Where(r => !HasAncestor(r.parent, roots)))
            {
                if (child.GetComponentsInChildren<Components.SceneEntityId>(true).Length != 0) continue;
                Vector3 source = city.ToSource(child.position);
                float originalHeight=original.GetInterpolatedHeight(source.x/original.size.x,source.z/original.size.z)+city.offset.y;
                // 원본에서 저작한 뿌리 기준과 바위 매몰량을 유지한 채 지형 변화량만 더한다.
                child.position+=Vector3.up*(ground.SampleHeight(child.position)+ground.transform.position.y-originalHeight);
            }
        }

        private static void GroundCityProps(Scene scene, Terrain ground, CityPlacement city)
        {
            foreach (string path in city.groundedProps)
            {
                int separator = path.IndexOf('/');
                string rootName = separator < 0 ? path : path.Substring(0, separator);
                var root = scene.GetRootGameObjects().Single(r => r.name == rootName).transform;
                Transform prop = separator < 0 ? root : root.Find(path.Substring(separator + 1));
                if (prop == null) throw new InvalidOperationException("접지 대상이 없습니다: " + path);
                Vector3 position = prop.position;
                position.y = ground.SampleHeight(position) + ground.transform.position.y + city.groundedPropOffset;
                prop.position = position;
            }
        }

        private static void ClearRoadObstacles(Transform environment, CityRecipe recipe)
        {
            var removed=new List<string>();
            foreach(var collider in environment.GetComponentsInChildren<Collider>(true))
            {
                if(collider is TerrainCollider || collider.isTrigger || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                if(collider.GetComponentInParent<GameActor>()!=null || collider.GetComponentInParent<Components.SceneEntityId>()!=null) continue;
                Bounds bounds=collider.bounds;
                if(RoadDistance(bounds.center,recipe,out float h)>recipe.roadHalfWidth+Mathf.Max(bounds.extents.x,bounds.extents.z)) continue;
                if(bounds.max.y<h+0.15f || bounds.min.y>h+3) continue;
                if(!IntersectsRoad(bounds,recipe))continue;
                GameObject root=PrefabUtility.GetOutermostPrefabInstanceRoot(collider.gameObject)??collider.gameObject;
                if(root.transform==environment || root.GetComponentsInChildren<Components.SceneEntityId>(true).Length>0) continue;
                string path=AnimationUtility.CalculateTransformPath(root.transform,null);
                bool boundary=path.Contains("/PlayBoundaries/") || path.Contains("/MapBoundaries/");
                string label=root.name.ToLowerInvariant();
                bool natural=IsVegetation(root.transform)||label.Contains("rock")||label.Contains("stone")||label.Contains("log");
                if(!boundary && !natural)
                    throw new InvalidOperationException("연결 길이 기존 구조물을 통과합니다. 경로를 수정하세요: "+path);
                removed.Add(path);
                root.SetActive(false);
            }
            File.WriteAllLines(ReportFolder+"/city-road-clearance.txt",removed.Distinct());
        }

        private static bool IntersectsRoad(Bounds bounds, CityRecipe recipe)
        {
            foreach(var points in GetRoadLines(recipe))
            for(int i=1;i<points.Length;i++)
            {
                int steps=Mathf.CeilToInt(Vector3.Distance(points[i-1],points[i]));
                for(int step=0;step<=steps;step++)
                {
                    Vector3 p=Vector3.Lerp(points[i-1],points[i],step/(float)steps)+Vector3.up;
                    if(bounds.SqrDistance(p)<recipe.roadHalfWidth*recipe.roadHalfWidth)return true;
                }
            }
            return false;
        }
    }
}
