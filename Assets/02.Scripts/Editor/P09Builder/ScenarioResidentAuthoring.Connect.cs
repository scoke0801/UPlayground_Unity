using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string ScenePath = "Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity";
        private const string PortraitTablePath = "Assets/10.Datas/Dialogue/SpeakerPortraitTable.asset";

        /// <summary>확정된 주민의 기본 대화·초상화와 작업 전면 배치를 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/시나리오 주민 연결")]
        public static void ConnectResidents()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("편집 모드가 필요합니다.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("미저장 씬이 있습니다.");
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(DataRoot + "/ScenarioResidents.json"));
            Require<SpeakerPortraitTableSO>(PortraitTablePath);
            foreach (var resident in recipe.residents)
            {
                Require<ActorDefinitionSO>(resident.DefinitionPath);
                if (!File.Exists(PortraitPath(resident))) throw new InvalidOperationException("확정 초상화 누락: " + resident.id);
                if (resident.dialogueLines == null || resident.dialogueLines.Length == 0) throw new InvalidOperationException("대사 누락");
            }
            string backup = Output + "/ConnectBackup/" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, backup + "/Scene.unity");
            File.Copy(PortraitTablePath, backup + "/SpeakerPortraitTable.asset");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            // 씬 전환이 참조되지 않은 에셋을 해제하므로 사용할 씬을 연 뒤 다시 읽는다.
            var table = Require<SpeakerPortraitTableSO>(PortraitTablePath);
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            var parent = scene.GetRootGameObjects().SingleOrDefault(x => x.name == "ScenarioResidents");
            if (parent == null) parent = new GameObject("ScenarioResidents");
            foreach (var resident in recipe.residents)
            {
                string portraitPath = PortraitPath(resident);
                AssetDatabase.ImportAsset(portraitPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                Sprite portrait = Require<Sprite>(portraitPath);
                ConnectPortrait(table, resident.displayName, portrait);
                var definition = Require<ActorDefinitionSO>(resident.DefinitionPath);
                definition.npcData.dialogueGraph = CreateDialogue(resident);
                EditorUtility.SetDirty(definition.npcData);
                AssetDatabase.SaveAssetIfDirty(definition.npcData);
                var identities = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SceneEntityId>(true))
                    .Where(x => x.Guid == resident.sceneEntityId).ToArray();
                if (identities.Length > 1) throw new InvalidOperationException("주민 배치 ID 중복");
                GameObject instance;
                if (identities.Length == 1) instance = identities[0].gameObject;
                else
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(definition.prefab, scene);
                    instance.transform.SetParent(parent.transform, true);
                    instance.AddComponent<SceneEntityId>().EditorSetGuid(resident.sceneEntityId);
                }
                Vector3 position = resident.position;
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
                if (!NavMesh.SamplePosition(position, out var hit, 2, NavMesh.AllAreas))
                    throw new InvalidOperationException("주민 작업점의 NavMesh 누락: " + resident.id);
                instance.transform.SetPositionAndRotation(hit.position, Quaternion.Euler(0, resident.yaw, 0));
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
            AssetDatabase.SaveAssetIfDirty(table);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene(ScenePath);
            table = Require<SpeakerPortraitTableSO>(PortraitTablePath);
            foreach (var resident in recipe.residents)
            {
                int count = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SceneEntityId>(true)).Count(x => x.Guid == resident.sceneEntityId);
                if (count != 1 || table.Entries.Count(x => x.speakerId == resident.displayName && x.portrait != null) != 1)
                    throw new InvalidOperationException("재로드 후 배치·화자 검증 실패");
            }
            File.WriteAllText(Output + "/Connection.txt", "씬 저장·재로드: 주민 " + recipe.residents.Length + "명, 고유 배치 ID·대화 그래프·기본 화자 Sprite 연결 통과. Play Mode 검증은 별도.");
        }

        private static string PortraitPath(Resident resident)
            => "Assets/04.Images/UI/Portrait/Npc/" + resident.id + "_Dialogue_Portrait.png";

        private static DialogueGraphSO CreateDialogue(Resident resident)
        {
            string path = DataRoot + "/DLG_" + resident.id + ".asset";
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
            if (graph != null)
            {
                if (!graph.TryValidatePlayback(out var message)) throw new InvalidOperationException(message);
                return graph;
            }
            graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
            graph.graphId = "dlg_" + resident.id.ToLowerInvariant();
            graph.graphName = resident.displayName + "의 일상";
            graph.startNodeId = "talk_0";
            AssetDatabase.CreateAsset(graph, path);
            for (int i = 0; i <= resident.dialogueLines.Length; i++)
            {
                var node = ScriptableObject.CreateInstance<DialogueNodeSO>();
                node.name = node.nodeId = i == resident.dialogueLines.Length ? "end" : "talk_" + i;
                node.nodeType = i == resident.dialogueLines.Length ? NodeType.End : NodeType.Talk;
                if (node.nodeType == NodeType.Talk)
                {
                    node.speakerId = resident.displayName;
                    node.dialogueText = resident.dialogueLines[i];
                    node.nextNodeId = i == resident.dialogueLines.Length - 1 ? "end" : "talk_" + (i + 1);
                }
                graph.nodes.Add(node);
                AssetDatabase.AddObjectToAsset(node, graph);
            }
            if (!graph.TryValidatePlayback(out var error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            return graph;
        }

        private static void ConnectPortrait(SpeakerPortraitTableSO table, string speaker, Sprite portrait)
        {
            var serialized = new SerializedObject(table);
            var entries = serialized.FindProperty("entries");
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("speakerId").stringValue == speaker)
                {
                    if (index >= 0) throw new InvalidOperationException("중복 화자 키: " + speaker);
                    index = i;
                }
            if (index < 0) index = entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("speakerId").stringValue = speaker;
            entry.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
