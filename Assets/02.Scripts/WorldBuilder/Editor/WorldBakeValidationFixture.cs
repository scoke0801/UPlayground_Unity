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
    public static class WorldBakeValidationFixture
    {
        public const string WorldId = "a707a707a707a707a707a707a707a707";
        public const string DataFolder = "Assets/10.Datas/WorldBuilder/P07Validation";
        public const string ScenePath = "Assets/WorldBuilder/Worlds/" + WorldId + "/P07Validation.unity";

        public static WorldDefinition CreateValidationScene() => Create(WorldId, DataFolder, fullSize: true);

        public static WorldDefinition Create(string id, string dataFolder, bool fullSize, string sceneName = "P07Validation")
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬부터 저장하세요.");
            string folder = WorldBakePipeline.WorldRoot(id);
            if (Directory.Exists(folder) || Directory.Exists(dataFolder))
                throw new IOException("기존 검증 월드는 덮어쓰지 않습니다.");
            try
            {
                var definition = WorldWaterValidationFixture.Create(id, folder, dataFolder, fullSize, NewSceneMode.Single);
                var active = SceneManager.GetActiveScene();
                string error = AssetDatabase.MoveAsset(active.path, folder + "/" + sceneName + ".unity");
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
                var ecology = WorldEcologyValidationFixture.CreateInput(definition, dataFolder, fullSize);
                WorldEcologyAuthoring.Regenerate(definition, ecology);
                BuildLayout(definition, ecology, dataFolder, fullSize);
                if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("P07 씬 저장 실패");
                return definition;
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.DeleteAsset(dataFolder);
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
                throw;
            }
        }

        private static void BuildLayout(WorldDefinition definition, WorldEcology ecology, string folder, bool fullSize)
        {
            var layout = ScriptableObject.CreateInstance<WorldNavigationLayout>();
            layout.worldGuid = definition.WorldGuid;
            layout.playerPrefab = ecology.playerPrefab;
            var agent = UnityEngine.AI.NavMesh.GetSettingsByIndex(0);
            layout.agentTypeId = agent.agentTypeID;
            float split = fullSize ? 512 : 64;
            var left = WorldNavigationBake.TileId(0, fullSize ? 0 : 1);
            var right = WorldNavigationBake.TileId(1, fullSize ? 0 : 1);
            var bridge = definition.Planning.bridges.Single();
            var bridgeLink = new Crossing { id = Id(701), bridgeId = bridge.bridgeGuid, fromSurface = left, toSurface = right,
                from = bridge.startSocket - Vector3.right * .65f, to = bridge.endSocket + Vector3.right * .65f, width = 2 };
            var a = definition.Planning.pois.Find(poi => poi.guid == WorldPlanningValidationFixture.Id(3)).anchor;
            var b = definition.Planning.pois.Find(poi => poi.guid == WorldPlanningValidationFixture.Id(4)).anchor;
            var bridgeRoute = new RequiredRoute { id = Id(702), requiredCrossingId = bridgeLink.id,
                fromSurface = left, toSurface = right,
                from = fullSize ? new Vector3((float)a.x, (float)a.y, (float)a.z + 2) : bridgeLink.from - Vector3.right * 2,
                to = new Vector3((float)b.x, (float)b.y, (float)b.z - 2) };
            var preview = UnityEngine.Object.FindFirstObjectByType<WorldEcologyPreview>();
            var portals = preview.portals.Where(p => p.position.y > 8).OrderBy(p => p.position.x).ToArray();
            var west = Link(portals[0], WorldNavigationBake.TileId(0, 0), Id(703));
            var east = Link(portals[1], WorldNavigationBake.TileId(1, 0), Id(704));
            var routes = new System.Collections.Generic.List<RequiredRoute> { bridgeRoute,
                PortalRoute(west, split, Id(705)), PortalRoute(east, split, Id(706)) };
            var links = new System.Collections.Generic.List<Crossing> { bridgeLink, west, east };
            if (!fullSize)
                links.Add(new Crossing { id = Id(707), fromSurface = WorldNavigationBake.TileId(0, 0), toSurface = left,
                    from = new Vector3(44, 10, 62.4f), to = new Vector3(44, 10, 65.6f), width = 2 });
            layout.crossings = links.ToArray();
            layout.routes = routes.ToArray();
            AssetDatabase.CreateAsset(layout, folder + "/NavigationLayout.asset");

            Crossing Link(WorldCavePortal p, string surface, string id)
            {
                Vector3 inside = p.position - p.outward * 1.2f;
                if (!preview.TryGetFloor(p.surfaceId, new Vector2(inside.x, inside.z), out Vector3 floor))
                    throw new InvalidDataException("검증 포털 바닥이 없습니다.");
                return new Crossing { id = id, portalId = p.id, fromSurface = surface, toSurface = WorldNavigationBake.CaveId(p.caveGuid),
                    from = p.position + p.outward * 1.5f, to = floor, width = 1.5f };
            }

            RequiredRoute PortalRoute(Crossing crossing, float x, string id) => new()
            {
                id = id, fromSurface = crossing.fromSurface, toSurface = crossing.toSurface, requiredCrossingId = crossing.id,
                from = crossing.from, to = new Vector3(x, 1, crossing.to.z)
            };
        }

        private static string Id(int value) => value.ToString("D32");
    }
}


