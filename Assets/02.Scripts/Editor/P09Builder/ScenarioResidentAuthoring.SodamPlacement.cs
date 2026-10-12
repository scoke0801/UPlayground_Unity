using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string SodamIntegrationOutput = Output + "/Sodam/Integration";
        private const string SpeakerBindingPath = "Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset";

        [Serializable] private sealed class ResidentAppearanceConnection
        {
            public string appearance;
        }

        /// <summary>검수된 소담 외형을 NPC로 등록하고 대화·약초 사건·생활 배치를 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/소담 NPC 연결")]
        public static void ConnectSodamResident()
        {
            RequireLifeEditor();
            AssetDatabase.ImportAsset(SpeakerBindingPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var bindingEntries = new SerializedObject(Require<SpeakerActorBindingTableSO>(SpeakerBindingPath)).FindProperty("entries");
            int serializedCount = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(SpeakerBindingPath), @"(?m)^\s*- speakerId:").Count;
            if (bindingEntries.arraySize != serializedCount)
                throw new InvalidOperationException($"화자 바인딩 파일 {serializedCount}개 / Unity {bindingEntries.arraySize}개 불일치. 기존 연결을 복구한 뒤 실행하세요.");
            var resident = JsonUtility.FromJson<Resident>(File.ReadAllText(DataRoot + "/Sodam.json"));
            string connectionJson = File.ReadAllText(DataRoot + "/SodamConnection.json");
            var connection = JsonUtility.FromJson<ResidentAppearanceConnection>(connectionJson);
            var finish = JsonUtility.FromJson<FinishRecipe>(connectionJson);
            var database = Require<ActorDatabase>(DatabasePath);
            var approved = Require<GameObject>(connection.appearance);
            Require<ActorAnimationMotionSet>(resident.motionSet);
            string[] outputs = { resident.PrefabPath, resident.DefinitionPath, resident.NpcPath, finish.completedDialoguePath };
            bool hasRegistration = File.Exists(resident.PrefabPath);
            if (hasRegistration)
            {
                ValidateSodamRegistration(resident, approved);
                ValidateLife(EditorSceneManager.OpenScene(ScenePath), JsonUtility.FromJson<LifeRecipe>(File.ReadAllText(LifeRecipePath)));
                Debug.Log("[Sodam] 기존 등록·외형·생활 배치 재검증 통과. 재생성하지 않았습니다.");
                return;
            }
            if (outputs.Any(File.Exists) || database.Contains(resident.id))
                throw new InvalidOperationException("소담 신규 생성 경로 또는 ID가 이미 존재합니다.");
            string portraitPath = PortraitPath(resident);
            if (!File.Exists(portraitPath)) throw new InvalidOperationException("소담 초상화 누락");
            string[] preserved = { DatabasePath, PortraitTablePath, SpeakerBindingPath, ScenePath, portraitPath + ".meta" };
            string backup = SodamIntegrationOutput + "/Runs/" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            var originals = preserved.Where(File.Exists).ToDictionary(path => path, File.ReadAllBytes);
            foreach (var pair in originals) File.WriteAllBytes(backup + "/" + Array.IndexOf(preserved, pair.Key), pair.Value);
            File.WriteAllLines(backup + "/Paths.txt", preserved);
            BuildContext context = null;
            try
            {
                var config = new CharacterBuildConfig
                {
                    ActorKind = BuilderActorKind.Npc, Sex = BuilderSex.Female, UseMagicaCloth = false,
                    UseManualName = true, ManualName = resident.id,
                    Stats = new StatsAssignment { createNewNpcData = true, npcDisplayName = resident.displayName,
                        npcDescription = resident.description, npcEnableWander = false, npcWanderWaitTime = resident.waitSeconds }
                };
                context = new BuildContext(config) { PrefabName = resident.id,
                    PrefabFolder = Path.GetDirectoryName(resident.PrefabPath).Replace('\\', '/') };
                // 외형 적용 단계를 생략해 검수된 메시·재질·Avatar 참조를 그대로 유지한다.
                context.RootInstance = (GameObject)PrefabUtility.InstantiatePrefab(approved);
                context.RootInstance.name = resident.id;
                new AttachActorComponentsStep().Execute(context);
                new GenerateActorDescStep().Execute(context);
                new AssignStatsStep().Execute(context);
                var animator = context.RootInstance.GetComponentInChildren<ActorAnimator>(true);
                if (animator == null) animator = context.RootInstance.AddComponent<ActorAnimator>();
                var serialized = new SerializedObject(animator);
                serialized.FindProperty("_motionSet").objectReferenceValue = Require<ActorAnimationMotionSet>(resident.motionSet);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PathConfig.EnsureFolderExists(context.PrefabFolder);
                var prefab = PrefabUtility.SaveAsPrefabAsset(context.RootInstance, resident.PrefabPath);
                if (prefab == null) throw new InvalidOperationException("소담 프리팹 저장 실패");
                context.Bag["finalPrefabAsset"] = prefab;
                new SyncActorDatabaseStep().Execute(context);
                foreach (var asset in context.GeneratedDescs) AssetDatabase.SaveAssetIfDirty(asset);
                AssetDatabase.SaveAssetIfDirty(database);
                AssetDatabase.SaveAssetIfDirty(prefab);
                ProbeSodamConnectionFailure("registered");
                var definition = Require<ActorDefinitionSO>(resident.DefinitionPath);
                definition.npcData.dialogueGraph = CreateDialogue(resident);
                ApplyFinishedDialogue(finish);
                foreach (var node in definition.npcData.dialogueGraph.nodes.Where(x => x.nodeType == NodeType.Talk))
                {
                    node.shotType = finish.shot;
                    EditorUtility.SetDirty(node);
                }
                EditorUtility.SetDirty(definition.npcData);
                AssetDatabase.SaveAssetIfDirty(definition.npcData.dialogueGraph);
                AssetDatabase.SaveAssetIfDirty(definition.npcData);
                ConnectSodamSpeaker(resident, portraitPath);
                UnityEngine.Object.DestroyImmediate(context.RootInstance);
                context.RootInstance = null;
                EditorSceneManager.OpenScene(ScenePath);
                ApplyResidentLife();
                ProbeSodamConnectionFailure("placed");
                ValidateSodamRegistration(resident, Require<GameObject>(connection.appearance));
                File.WriteAllText(SodamIntegrationOutput + "/Registration.txt",
                    "NPC·DB 등록 1개, 외형 메시·재질·Avatar 동일, Missing Script 0, 생활 모션·대화·초상화·화자 연결 통과. 씬 저장·재로드 검증 통과.");
                Debug.Log("[Sodam] NPC 등록·생활 배치·대화 연결 완료");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (string path in outputs.Reverse())
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                // Unity가 메모리 매핑한 YAML 핸들을 놓은 뒤 원본 바이트를 복구한다.
                AssetDatabase.ReleaseCachedFileHandles();
                foreach (var pair in originals)
                {
                    if (!File.Exists(pair.Key) || !File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value))
                    {
                        // Windows에서는 매핑된 파일의 길이를 변경할 수 없으므로 같은 볼륨에서 교체한다.
                        string staged = pair.Key + ".sodam-restore";
                        File.WriteAllBytes(staged, pair.Value);
                        if (File.Exists(pair.Key)) File.Replace(staged, pair.Key, null);
                        else File.Move(staged, pair.Key);
                    }
                }
                foreach (var pair in originals)
                    if (!pair.Key.EndsWith(".meta")) AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                Require<ActorDatabase>(DatabasePath).InvalidateLookup();
                if (outputs.Any(File.Exists) || originals.Any(pair => !File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)))
                    throw new InvalidOperationException("소담 연결 실패 후 원본 복구 검증 실패: " + backup);
                File.WriteAllText(backup + "/Rollback.txt", "신규 출력 제거 및 기존 파일 바이트 동일 검사 통과");
                throw;
            }
            finally
            {
                if (context?.RootInstance != null) UnityEngine.Object.DestroyImmediate(context.RootInstance);
                context?.DiscardStagedAssetBackups();
            }
        }

        private static void ProbeSodamConnectionFailure(string stage)
        {
            if (Environment.GetCommandLineArgs().Contains("-sodamFail=" + stage))
                throw new InvalidOperationException("소담 복구 검사용 실패: " + stage);
        }

        private static void ConnectSodamSpeaker(Resident resident, string portraitPath)
        {
            AssetDatabase.ImportAsset(portraitPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var table = Require<SpeakerPortraitTableSO>(PortraitTablePath);
            ConnectPortrait(table, resident.displayName, Require<Sprite>(portraitPath));
            AssetDatabase.SaveAssetIfDirty(table);
            var binding = Require<SpeakerActorBindingTableSO>(SpeakerBindingPath);
            var serialized = new SerializedObject(binding);
            var entries = serialized.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("speakerId").stringValue == resident.displayName)
                    throw new InvalidOperationException("소담 화자 바인딩이 이미 존재합니다.");
            int index = entries.arraySize++;
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("speakerId").stringValue = resident.displayName;
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("actorId").stringValue = resident.id;
            entries.GetArrayElementAtIndex(index).FindPropertyRelative("standInActorId").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(binding);
        }

        private static void ValidateSodamRegistration(Resident resident, GameObject approved)
        {
            var definition = Require<ActorDefinitionSO>(resident.DefinitionPath);
            var saved = Require<GameObject>(resident.PrefabPath);
            if (Require<ActorDatabase>(DatabasePath).All.Count(x => x != null && x.actorId == resident.id) != 1
                || definition.actorId != resident.id || saved.GetComponent<NpcActor>()?.ActorId != resident.id
                || definition.prefab != saved || definition.npcData != Require<NpcActorSO>(resident.NpcPath))
                throw new InvalidOperationException("소담 NPC 등록 참조 오류");
            foreach (var child in saved.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new InvalidOperationException("소담 Missing Script");
            var originals = approved.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (saved.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != originals.Length)
                throw new InvalidOperationException("소담 렌더러 수 변경");
            foreach (var original in originals)
            {
                string path = AnimationUtility.CalculateTransformPath(original.transform, approved.transform);
                var renderer = saved.transform.Find(path)?.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null || renderer.sharedMesh != original.sharedMesh
                    || !renderer.sharedMaterials.SequenceEqual(original.sharedMaterials))
                    throw new InvalidOperationException("소담 검수 외형 참조 변경: " + path);
            }
            if (saved.GetComponentInChildren<Animator>().avatar != approved.GetComponentInChildren<Animator>().avatar)
                throw new InvalidOperationException("소담 Avatar 변경");
            var animator = new SerializedObject(saved.GetComponentInChildren<ActorAnimator>(true));
            if (animator.FindProperty("_motionSet").objectReferenceValue != Require<ActorAnimationMotionSet>(resident.motionSet))
                throw new InvalidOperationException("소담 생활 모션 연결 오류");
            if (definition.npcData.dialogueGraph == null || !definition.npcData.dialogueGraph.TryValidatePlayback(out _))
                throw new InvalidOperationException("소담 대화 연결 오류");
            if (Require<SpeakerPortraitTableSO>(PortraitTablePath).Entries.Count(x => x.speakerId == resident.displayName
                && x.portrait == Require<Sprite>(PortraitPath(resident))) != 1)
                throw new InvalidOperationException("소담 초상화 연결 오류");
            var bindings = new SerializedObject(Require<SpeakerActorBindingTableSO>(SpeakerBindingPath)).FindProperty("entries");
            int matches = 0;
            for (int i = 0; i < bindings.arraySize; i++)
            {
                var entry = bindings.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("speakerId").stringValue != resident.displayName) continue;
                if (entry.FindPropertyRelative("actorId").stringValue != resident.id)
                    throw new InvalidOperationException("소담 화자 액터 불일치");
                matches++;
            }
            if (matches != 1) throw new InvalidOperationException("소담 화자 바인딩 누락·중복");
        }
    }
}
