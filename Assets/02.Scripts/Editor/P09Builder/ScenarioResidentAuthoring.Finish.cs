using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        [Serializable] private sealed class FinishRecipe
        {
            public FinishProp[] props;
            public string[] dialoguePaths;
            public DialogueShotType shot;
            public string completedDialoguePath, completedFlag, completedSpeaker, completedLine;
        }

        [Serializable] private sealed class FinishProp
        {
            public string sourcePath, targetPath;
            public Vector3 position;
            public float yaw;
        }

        /// <summary>주민의 확정 작업복·대화 구도·사건 소품을 백업 후 적용한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/주민 생활 연출 마감")]
        public static void FinishResidentPresentation()
        {
            RequireLifeEditor();
            var recipe = JsonUtility.FromJson<FinishRecipe>(File.ReadAllText(DataRoot + "/ResidentFinish.json"));
            var resident = JsonUtility.FromJson<Resident>(File.ReadAllText(DataRoot + "/JunAppearance.json"));
            var paths = recipe.dialoguePaths.Concat(new[] { recipe.completedDialoguePath, ScenePath, PortraitTablePath, resident.PrefabPath, resident.PresetPath }).Distinct().ToArray();
            string backup = LifeOutput + "/Runs/" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            for (int i = 0; i < paths.Length; i++) File.Copy(paths[i], backup + "/" + i);
            File.WriteAllLines(backup + "/Paths.txt", paths);
            try
            {
                var catalog = new P09AssetCatalog();
                catalog.Refresh();
                var preset = Require<BuildPreset>(resident.PresetPath);
                // 기능·등록은 기존 빌더 결과를 유지하고 외형 파츠만 갱신한다.
                preset.config.ArmorSelections.Set(BuilderArmorSlot.Leg, SelectPart(catalog.Legs, resident.legArmor));
                preset.description = resident.description;
                var root = PrefabUtility.LoadPrefabContents(resident.PrefabPath);
                try
                {
                    AppearanceApplier.Apply(root, preset.config, catalog);
                    ValidateElderRoot(root);
                    if (PrefabUtility.SaveAsPrefabAsset(root, resident.PrefabPath) == null)
                        throw new InvalidOperationException("준 작업복 저장 실패");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                EditorUtility.SetDirty(preset);
                AssetDatabase.SaveAssetIfDirty(preset);
                ApplyFinishedDialogue(recipe);
                foreach (string path in recipe.dialoguePaths)
                {
                    var graph = Require<DialogueGraphSO>(path);
                    foreach (var node in graph.nodes.Where(x => x != null && x.nodeType == NodeType.Talk))
                    {
                        node.shotType = recipe.shot;
                        EditorUtility.SetDirty(node);
                    }
                    if (!graph.TryValidatePlayback(out string error)) throw new InvalidOperationException(error);
                    AssetDatabase.SaveAssetIfDirty(graph);
                }
                var scene = EditorSceneManager.OpenScene(ScenePath);
                var terrain = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Terrain>()).Single();
                foreach (var prop in recipe.props)
                {
                    Transform target = FindLifeTransform(scene, prop.targetPath);
                    if (target == null)
                    {
                        var source = FindLifeTransform(scene, prop.sourcePath)
                            ?? throw new InvalidOperationException("마감 소품 원본 누락: " + prop.sourcePath);
                        target = UnityEngine.Object.Instantiate(source.gameObject).transform;
                        target.name = prop.targetPath.Split('/').Last();
                        target.SetParent(RequireLifePath(scene, prop.targetPath[..prop.targetPath.LastIndexOf('/')]), true);
                    }
                    Vector3 point = prop.position;
                    point.y += terrain.SampleHeight(point) + terrain.transform.position.y;
                    target.SetPositionAndRotation(point, Quaternion.Euler(0, prop.yaw, 0));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                ApplyResidentLife();
                Directory.CreateDirectory(LifeOutput + "/Models");
                foreach (int yaw in new[] { 0, 45, 90 })
                {
                    var preview = UnityEngine.Object.Instantiate(Require<GameObject>(resident.PrefabPath));
                    try { Capture(preview, LifeOutput + "/Models/NPC_Jun_" + yaw + ".png", false, yaw); }
                    finally { UnityEngine.Object.DestroyImmediate(preview); }
                }
                File.WriteAllText(backup + "/Validated.txt", "작업복 리그·메시·Missing Script, 대화 연결, 주민·사건 씬 저장/재로드 통과. 화면과 플레이 검증은 별도.");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                for (int i = 0; i < paths.Length; i++)
                {
                    File.Copy(backup + "/" + i, paths[i], true);
                    AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
                }
                EditorSceneManager.OpenScene(ScenePath);
                throw;
            }
        }

        private static void ApplyFinishedDialogue(FinishRecipe recipe)
        {
            var graph = Require<DialogueGraphSO>(recipe.completedDialoguePath);
            var branch = graph.nodes.SingleOrDefault(x => x.nodeId == "work_completed");
            if (branch == null)
            {
                branch = ScriptableObject.CreateInstance<DialogueNodeSO>();
                branch.name = branch.nodeId = "work_completed";
                branch.nodeType = NodeType.Condition;
                branch.falseNextNodeId = graph.startNodeId;
                var condition = ScriptableObject.CreateInstance<FlagConditionSO>();
                condition.name = "WorkCompletedFlag";
                branch.condition = condition;
                AssetDatabase.AddObjectToAsset(condition, graph);
                AssetDatabase.AddObjectToAsset(branch, graph);
                graph.nodes.Add(branch);
            }
            var flag = branch.condition as FlagConditionSO
                ?? throw new InvalidOperationException("기존 작업 완료 조건 타입이 다릅니다.");
            flag.flagKey = recipe.completedFlag;
            flag.expectedValue = true;
            var talk = graph.nodes.SingleOrDefault(x => x.nodeId == "after_work");
            if (talk == null)
            {
                talk = ScriptableObject.CreateInstance<DialogueNodeSO>();
                talk.name = talk.nodeId = "after_work";
                AssetDatabase.AddObjectToAsset(talk, graph);
                graph.nodes.Add(talk);
            }
            talk.nodeType = NodeType.Talk;
            talk.speakerId = recipe.completedSpeaker;
            talk.dialogueText = recipe.completedLine;
            talk.nextNodeId = "end";
            branch.trueNextNodeId = talk.nodeId;
            graph.startNodeId = branch.nodeId;
            foreach (var asset in new UnityEngine.Object[] { graph, branch, flag, talk }) EditorUtility.SetDirty(asset);
        }

    }
}
