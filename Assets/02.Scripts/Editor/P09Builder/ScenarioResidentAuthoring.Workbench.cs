using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UPlayGround.EditorTools;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Event;
using UPlayGround.Tool.Editor.Map;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string WorkbenchOutput = Output + "/Workbench";
        private const string AlchemyFolder = "Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Actions/Crafting/Alchemy";

        [Serializable] private sealed class WorkbenchRecipe
        {
            public string[] propNames;
            public string actorPath, motionPath, handPropPath, herbPrefab;
            public float handPropSize, capsuleRadius, capsuleHeight, blendDuration;
            public WorkbenchMotion[] motions;
        }

        [Serializable] private sealed class WorkbenchMotion
        {
            public string name, clip, sourceMotion;
            public float speed = 1, duration, holdStart, holdEnd;
        }

        /// <summary>작업대 접촉과 소담의 손 작업을 적용하고 저장·재로드에서 참조를 검증한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/약초 작업대 보정")]
        public static void ApplyHerbWorkbench()
        {
            RequireLifeEditor();
            var recipe = JsonUtility.FromJson<WorkbenchRecipe>(File.ReadAllText(DataRoot + "/HerbWorkbench.json"));
            string backup = WorkbenchOutput + "/Runs/" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            string[] paths = { ScenePath, recipe.motionPath, recipe.handPropPath };
            var originals = paths.Where(File.Exists).ToDictionary(path => path, File.ReadAllBytes);
            foreach (var pair in originals) File.WriteAllBytes(backup + "/" + Array.IndexOf(paths, pair.Key), pair.Value);
            File.WriteAllLines(backup + "/Paths.txt", paths);
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                ScenarioMapAuthoring.ApplyWorkbenchProps(scene, recipe.propNames);
                var actor = FindLifeTransform(scene, recipe.actorPath);
                if (actor == null) throw new InvalidOperationException("작업 주민 누락");
                var residentRecipe = JsonUtility.FromJson<LifeRecipe>(File.ReadAllText(LifeRecipePath));
                Terrain terrain = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Terrain>()).Single();
                foreach (string actorPath in new[] { recipe.actorPath, "NPC/Npc_Hazel" })
                {
                    var resident = residentRecipe.residents.Single(x => x.targetPath == actorPath);
                    var target = FindLifeTransform(scene, actorPath);
                    target.SetPositionAndRotation(GroundLifePoint(terrain, resident.position), Quaternion.Euler(0, resident.yaw, 0));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
                foreach (var change in residentRecipe.changes.Where(x => x.actorPath == recipe.actorPath || x.actorPath == "NPC/Npc_Hazel"))
                    ApplyLifeChange(scene, terrain, change);
                var animator = actor.GetComponentInChildren<Animator>();
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                Vector3 grip = hand.InverseTransformPoint(Vector3.Lerp(hand.position, finger.position, 0.7f));
                GameObject prop = CreateWorkbenchHandProp(recipe);
                var work = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(recipe.motionPath);
                if (work == null)
                {
                    work = ScriptableObject.CreateInstance<MotionSetAsset>();
                    AssetDatabase.CreateAsset(work, recipe.motionPath);
                }
                string sectionId = work.motionSet?.sections?.FirstOrDefault()?.id;
                work.motionSet = MotionSet.CreateAuthored("소담 약초 분류·전달", MotionSectionEndPolicy.LoopSelf);
                if (!string.IsNullOrEmpty(sectionId)) work.motionSet.sections[0].id = sectionId;
                work.motionSet.internalBlendDuration = recipe.blendDuration;
                foreach (var entry in recipe.motions)
                {
                    var clip = string.IsNullOrEmpty(entry.sourceMotion)
                        ? AssetDatabase.LoadAllAssetsAtPath(entry.clip).OfType<AnimationClip>().Single(x => !x.name.StartsWith("__preview__"))
                        : Require<MotionSetAsset>(entry.sourceMotion).motionSet.motions[0].motionClip;
                    if (!clip.humanMotion || entry.speed <= 0) throw new InvalidOperationException("작업 클립 설정 오류");
                    var motion = new UPlayGround.Animation.Motion { motionName = entry.name, motionClip = clip, playbackSpeed = entry.speed };
                    if (entry.duration > 0) motion.clipEndTime = Mathf.Min(entry.duration, clip.length);
                    if (entry.holdEnd > entry.holdStart)
                        motion.events.Add(new BeginParticleEvent { particlePrefab = prop, spawnPointName = hand.name,
                            offset = grip, startTime = entry.holdStart, endTime = entry.holdEnd, destroyOnFinish = true });
                    if (entry.holdEnd > motion.Duration) throw new InvalidOperationException("손 소품 시간이 모션 길이를 넘습니다.");
                    work.motionSet.motions.Add(motion);
                }
                EditorUtility.SetDirty(work);
                AssetDatabase.SaveAssetIfDirty(work);
                var brain = actor.GetComponent<NpcBrain>();
                var serialized = new SerializedObject(brain);
                serialized.FindProperty("_workMotion").objectReferenceValue = work;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
                var motor = actor.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
                motor.SetCapsuleDimensions(recipe.capsuleRadius, recipe.capsuleHeight, recipe.capsuleHeight * 0.5f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor.Capsule);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (Environment.GetCommandLineArgs().Contains("-herbFail=stored")) throw new InvalidOperationException("약초 작업대 복구 검사");
                scene = EditorSceneManager.OpenScene(ScenePath);
                ValidateLife(scene, residentRecipe);
                var table = FindLifeTransform(scene, "AuthoredEnvironment/LandscapeDistrict/Hazel_Worktable");
                Bounds surface = WorkbenchBounds(table.gameObject);
                var report = new List<string>();
                foreach (string name in new[] { "Hazel_Jar", "Hazel_Bottle", "Sodam_HerbTray" })
                {
                    Bounds bounds = WorkbenchBounds(FindLifeTransform(scene, "AuthoredEnvironment/LandscapeDistrict/" + name).gameObject);
                    float gap = bounds.min.y - surface.max.y;
                    if (Mathf.Abs(gap) > 0.01f || bounds.min.x < surface.min.x || bounds.max.x > surface.max.x
                        || bounds.min.z < surface.min.z || bounds.max.z > surface.max.z)
                        throw new InvalidOperationException("상판 접촉 오류: " + name);
                    report.Add(name + " 상판 간격=" + gap.ToString("F5"));
                }
                if (FindLifeTransform(scene, recipe.actorPath).GetComponent<NpcBrain>().WorkMotion != work)
                    throw new InvalidOperationException("저장 후 작업 모션 참조 누락");
                File.WriteAllLines(WorkbenchOutput + "/Applied.txt", report);
                Debug.Log("[HerbWorkbench] 작업대 접촉·모션 저장·재로드 검증 통과");
                if (Environment.GetCommandLineArgs().Contains("-herbSmoke")) ScenarioMapAuthoring.RunResidentLifeSmoke();
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                AssetDatabase.ReleaseCachedFileHandles();
                foreach (string path in paths)
                {
                    if (!originals.TryGetValue(path, out var bytes)) { if (File.Exists(path)) AssetDatabase.DeleteAsset(path); continue; }
                    string staged = path + ".herb-restore";
                    File.WriteAllBytes(staged, bytes);
                    File.Replace(staged, path, null);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                if (originals.Any(pair => !File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)))
                    throw new InvalidOperationException("작업대 원본 복구 실패: " + backup);
                File.WriteAllText(backup + "/Rollback.txt", "실행 직전 바이트 복구 확인");
                throw;
            }
        }

        private static GameObject CreateWorkbenchHandProp(WorkbenchRecipe recipe)
        {
            var source = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(recipe.herbPrefab));
            try
            {
                source.name = "Sodam_HandHerbs";
                var bounds = WorkbenchBounds(source);
                source.transform.localScale *= recipe.handPropSize / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                foreach (var collider in source.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                return PrefabUtility.SaveAsPrefabAsset(source, recipe.handPropPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        /// <summary>기존 약초 작업 모션의 손 궤적과 작업대 실제 메시 경계를 저장 없이 조사한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/약초 작업 모션 조사")]
        public static async void InspectHerbWorkbench()
        {
            RequireLifeEditor();
            EditorApplication.update += QueueElderRigUpdate;
            int exitCode = 1;
            try
            {
            Directory.CreateDirectory(WorkbenchOutput);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var report = new List<string>();
            foreach (string name in new[] { "Hazel_Worktable", "Hazel_Jar", "Hazel_Bottle", "Hazel_Stool" })
            {
                var node = FindLifeTransform(scene, "AuthoredEnvironment/LandscapeDistrict/" + name);
                if (node == null) throw new InvalidOperationException("작업장 소품 누락: " + name);
                var bounds = WorkbenchBounds(node.gameObject);
                report.Add(name + " position=" + node.position.ToString("F4") + " min=" + bounds.min.ToString("F4") + " max=" + bounds.max.ToString("F4"));
            }
            var resident = JsonUtility.FromJson<Resident>(File.ReadAllText(DataRoot + "/Sodam.json"));
            string[] clips = {
                AlchemyFolder + "/Alchemy_Play_1.fbx",
                AlchemyFolder + "/Alchemy_Play_3.fbx",
                AlchemyFolder + "/Alchemy_All.fbx",
                "Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/Actions/Item/Item_Give.fbx",
                "Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/NPC/Standing/Standing_Idle_1_Give.fbx",
                "Assets/ExternalAssets/AnimationOnly/VanillaLoopStudio/FreeSampleAnimationSet/Art/Animations/ItemPickupSet/Mannequin/A_ItemPickup_fromIdle_RH_100cm.fbx"
            };
            foreach (string clipPath in clips)
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(clipPath)
                    .OfType<AnimationClip>().Single(x => !x.name.StartsWith("__preview__"));
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(resident.PrefabPath));
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = root.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController = null;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var graph = PlayableGraph.Create("약초 손 궤적 조사");
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    AnimationPlayableOutput.Create(graph, "소담", animator).SetSourcePlayable(playable);
                    graph.Play();
                    report.Add(clip.name + " length=" + clip.length + " human=" + clip.humanMotion + " loop=" + clip.isLooping);
                    for (int sample = 0; sample < 9; sample++)
                    {
                        playable.SetTime(clip.length * sample / 8f);
                        graph.Evaluate(0);
                        await UpdateElderRigFrames();
                        report.Add(sample + " left=" + root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftHand).position).ToString("F4")
                            + " right=" + root.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position).ToString("F4"));
                        if (sample % 2 == 0)
                            CaptureWorkbenchPose(root, WorkbenchOutput + "/" + clip.name + "_" + sample + ".png");
                    }
                }
                finally
                {
                    graph.Destroy();
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            File.WriteAllLines(WorkbenchOutput + "/Inspection.txt", report);
            exitCode = 0;
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                EditorApplication.update -= QueueElderRigUpdate;
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        private static void CaptureWorkbenchPose(GameObject source, string path)
        {
            var pose = new GameObject("검수 포즈");
            var meshes = new List<Mesh>();
            try
            {
                // 복제한 Animator가 포즈를 초기화하지 않도록 평가된 메시를 촬영한다.
                foreach (var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh();
                    skin.BakeMesh(mesh);
                    meshes.Add(mesh);
                    var part = new GameObject(skin.name);
                    part.transform.SetParent(pose.transform);
                    part.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                    part.transform.localScale = skin.transform.lossyScale;
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                }
                Capture(pose, path, false, 35, false, true);
            }
            finally
            {
                if (pose != null) UnityEngine.Object.DestroyImmediate(pose);
                foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Bounds WorkbenchBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("메시 누락: " + root.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
