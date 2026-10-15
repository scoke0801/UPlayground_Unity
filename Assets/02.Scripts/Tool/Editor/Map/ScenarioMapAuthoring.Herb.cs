using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Components;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private static string HerbDirectory => Array.IndexOf(Environment.GetCommandLineArgs(), "-p04Regression") >= 0
            ? "output/ScenarioGameplay/P04/HerbRegression" : "output/ScenarioGameplay/P03/HerbRoute";
        private const string HerbFlowPath = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/FLOW_ScenarioHerbRoute.asset";
        private const string HerbSourcePrefab = "Assets/03.Prefabs/Story/Story_LakeSideQuestRoute.prefab";
        private const string WrenchController = "Assets/ExternalAssets/Character/Suiha/prefab/Gimmicks/Modules/Wrench/FX_Wrench.controller";

        /// <summary>외부 컨트롤러 단독 로드와 씬 사용처를 저장 없이 격리 조사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/약초 연결 사전 조사")]
        public static void InspectHerbRoute()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Directory.CreateDirectory(HerbDirectory);
            var lines = new List<string>();
            string phase = "controller";
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception)
                    lines.Add(phase + ": " + type + ": " + message);
            };
            Application.logMessageReceived += capture;
            try
            {
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(WrenchController);
                lines.Add("controller=" + controller + "; subassets=" + AssetDatabase.LoadAllAssetsAtPath(WrenchController).Length);
                var probe = new GameObject("WrenchControllerIsolation");
                var animatorProbe = probe.AddComponent<Animator>();
                animatorProbe.runtimeAnimatorController = controller;
                animatorProbe.Rebind();
                animatorProbe.Update(0);
                UnityEngine.Object.DestroyImmediate(probe);
                phase = "scene";
                var scene = EditorSceneManager.OpenScene(ReadLayout().scenePath);
                Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                foreach (var point in new[] { new Vector3(994,0,482), new Vector3(1015,0,557), new Vector3(1008,0,552), new Vector3(1004,0,546), new Vector3(1000,0,540) })
                {
                    Vector3 grounded = point;
                    grounded.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                    bool nav = NavMesh.SamplePosition(grounded, out NavMeshHit hit, 3, NavMesh.AllAreas);
                    lines.Add("ground=" + grounded.ToString("F3") + "; nav=" + nav + "; navPoint=" + hit.position.ToString("F3"));
                }
                foreach (var root in scene.GetRootGameObjects())
                foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                    if (animator.runtimeAnimatorController != null && AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) == WrenchController)
                        lines.Add("controller use=" + GetTransformPath(animator.transform) + "; enabled=" + animator.enabled + "; active=" + animator.gameObject.activeInHierarchy);
                foreach (var actor in UnityEngine.Object.FindObjectsByType<GatheringActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    lines.Add("gather=" + GetTransformPath(actor.transform) + "; position=" + actor.transform.position.ToString("F3"));
            }
            finally
            {
                Application.logMessageReceived -= capture;
                File.WriteAllLines(HerbDirectory + "/Inspection.txt", lines);
            }
        }

        [Serializable] private sealed class HerbPlacement
        {
            public string name, sceneEntityId;
            public Vector3 position;
        }
        [Serializable] private sealed class HerbRecipe { public HerbPlacement[] placements; }

        /// <summary>약초 노드와 P00에서 보관한 채집 ID만 시나리오 씬에 복원한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/약초 연결 복구")]
        public static void ApplyHerbRoute()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            var layout = ReadLayout();
            var scene = EditorSceneManager.OpenScene(layout.scenePath);
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(HerbFlowPath);
            var errors = new List<string>();
            if (graph == null || !graph.Validate(errors)) throw new InvalidOperationException(string.Join(";", errors));
            var recipe = JsonUtility.FromJson<HerbRecipe>(File.ReadAllText("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/HerbRoute.json"));
            string backup = HerbDirectory + "/ApplyBefore.unity";
            File.Copy(layout.scenePath, backup, true);
            string navigationPath = layout.assetDirectory + "/Navigation.asset";
            string navigationBackup = HerbDirectory + "/NavigationBefore.asset";
            File.Copy(navigationPath, navigationBackup, true);
            try
            {
                var root = GameObject.Find("ScenarioHerbRoute");
                if (root == null)
                {
                    root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HerbSourcePrefab), scene);
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    root.name = "ScenarioHerbRoute";
                    var keep = new HashSet<string>(recipe.placements.Select(p => p.name));
                    foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                        if (!keep.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
                    foreach (var runner in root.GetComponents<FlowGraphRunner>()) UnityEngine.Object.DestroyImmediate(runner);
                    root.SetActive(false);
                    root.AddComponent<FlowGraphRunner>().SetGraph(graph);
                }
                root.SetActive(false);
                var herbRunner = root.GetComponent<FlowGraphRunner>();
                herbRunner.SetGraph(graph);
                var report = new List<string>();
                foreach (var placement in recipe.placements)
                {
                    var target = root.transform.Find(placement.name);
                    if (target == null) throw new InvalidOperationException("약초 개체 누락: " + placement.name);
                    var point = GroundPoint(placement.position, terrain);
                    if (!NavMesh.SamplePosition(point, out var hit, 2, NavMesh.AllAreas))
                        throw new InvalidOperationException("약초 접근 NavMesh 누락: " + placement.name);
                    target.position = point;
                    if (!string.IsNullOrEmpty(placement.sceneEntityId)) target.GetComponent<SceneEntityId>().EditorSetGuid(placement.sceneEntityId);
                    var volume = target.GetComponent<FlowGraphTriggerVolume>();
                    if (volume != null)
                    {
                        var serialized = new SerializedObject(volume);
                        serialized.FindProperty("_runner").objectReferenceValue = herbRunner;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    report.Add(placement.name + " | " + placement.sceneEntityId + " | " + point.ToString("F3") + " | nav=" + hit.position.ToString("F3"));
                }
                root.SetActive(true);
                var hazel = GameObject.Find("NPC/Npc_Hazel").GetComponent<NpcActor>();
                var hazelSerialized = new SerializedObject(hazel);
                hazelSerialized.FindProperty("_data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.NpcActorSO>(
                    "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/NPC_Hazel_Scenario.asset");
                hazelSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(hazel);
                var table = GameObject.Find("AuthoredEnvironment/LandscapeDistrict/Hazel_Worktable");
                if (table == null) throw new InvalidOperationException("헤이즐 작업대 누락");
                var modifier = table.GetComponent<NavMeshModifier>();
                int blockedArea = NavMesh.GetAreaFromName("Not Walkable");
                if (modifier == null || !modifier.overrideArea || modifier.area != blockedArea)
                {
                    modifier ??= table.AddComponent<NavMeshModifier>();
                    modifier.overrideArea = true;
                    modifier.area = blockedArea;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(modifier);
                    BakeNavigation(layout, GameObject.Find("AuthoredEnvironment"));
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                File.WriteAllLines(HerbDirectory + "/Placement.txt", report);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.Copy(backup, layout.scenePath, true);
                File.Copy(navigationBackup, navigationPath, true);
                throw;
            }
        }
    }
}
