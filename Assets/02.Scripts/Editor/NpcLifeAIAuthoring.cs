using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Data.Actor.NPC;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Interaction.LivingWorld;
using UPlayGround.Manager.World;

namespace UPlayGround.Editor
{
    /// <summary>기존 몸과 식별자를 보존하며 대표 생활 바인딩의 검수용 콘텐츠만 추가한다.</summary>
    public static partial class NpcLifeAIAuthoring
    {
        private const string ProfileFolder = "Assets/10.Datas/NpcLife/Profiles";
        private const string MotionFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/";
        private const string FarmPrefabs = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/";

        /// <summary>측정된 손 도구 범위에 기존 용기·목재 작업물을 연결한다.</summary>
        [UPlaygroundTool("NpcLifeAI.PrepareRepresentativeWorkProps")]
        public static void PrepareRepresentativeWorkProps()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("편집 모드의 컴파일 완료 후 실행해야 합니다.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().Single(g => g.name == "NpcLifeAI_Review");
            if (root.transform.Find("WorkContacts") != null) return;
            var points = UnityEngine.Object.FindObjectsByType<NpcWorkPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => p.gameObject.scene == scene).ToArray();
            var cook = points.Single(p => p.PointId == "lake.meal.prep");
            var repair = points.Single(p => p.PointId == "lake.repair.work");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("주민 작업물 접점 연결");
            try
            {
                var props = CreateChild(root.transform, "WorkContacts").transform;
                // 기존 조리대 앞 모서리에 도구 동선과 용기 전체가 들어가도록 접근점을 보정한다.
                Undo.RecordObject(cook.transform, "조리 접근점"); cook.transform.position += cook.transform.forward * 0.35f;
                Vector3 bowlCenter = cook.transform.TransformPoint(new Vector3(-0.01f, 0.738f, 0.46f));
                CreateFittedProp(props, "CookingBowl", "Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Food/TGV_Bowl_01A.prefab",
                    bowlCenter, cook.transform.rotation, new Vector3(0.44f, 0.176f, 0.44f));
                CreateFittedProp(props, "RepairTable", FarmPrefabs + "Props/Interior/Furniture/PF_Farm_WoodTable_01.prefab",
                    repair.transform.TransformPoint(new Vector3(0.23f, 0.35f, 0.56f)), repair.transform.rotation, new Vector3(0.72f, 0.70f, 0.42f));
                CreateFittedProp(props, "RepairTimber", FarmPrefabs + "Props/CraftingTable/PF_Farm_CraftingTable_Module_06.prefab",
                    repair.transform.TransformPoint(new Vector3(0.23f, 0.7275f, 0.46f)), repair.transform.rotation, new Vector3(0.38f, 0.055f, 0.25f));
                Physics.SyncTransforms();
                Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static GameObject CreateFittedProp(Transform parent, string name, string path, Vector3 center, Quaternion rotation, Vector3 size)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("기존 작업물 자산이 없습니다: " + path);
            var wrapper = CreateChild(parent, name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(instance, "기존 작업물 연결"); instance.transform.SetParent(wrapper.transform, false);
            Bounds bounds = GetVisualBounds(instance);
            Vector3 scale = new(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            wrapper.transform.localScale = scale;
            wrapper.transform.SetPositionAndRotation(center - rotation * Vector3.Scale(bounds.center, scale), rotation);
            return wrapper;
        }

        private static Bounds GetVisualBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("시각 메시가 없는 자산입니다.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
                throw new InvalidOperationException("작업물의 실측 크기가 유효하지 않습니다.");
            return bounds;
        }

        /// <summary>원본 주민을 바꾸지 않고 실제 리그의 손 도구 접점 표본을 기록한다.</summary>
        [UPlaygroundTool("NpcLifeAI.CaptureContactSamples")]
        public static void CaptureContactSamples()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorUtility.scriptCompilationFailed || AnimationMode.InAnimationMode())
                throw new InvalidOperationException("다른 실행·애니메이션 편집을 마친 뒤 검사해야 합니다.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("기존 미저장 씬 변경을 먼저 보존해야 합니다.");
            Directory.CreateDirectory("output/NpcLifeAI");
            const string backup = "output/NpcLifeAI/LakeVillageWorld.resume-before.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            var rows = new System.Collections.Generic.List<string>();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (string id in new[] { "lake.meal.prep", "lake.repair.work" })
                {
                    var point = UnityEngine.Object.FindObjectsByType<NpcWorkPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Single(p => p.gameObject.scene == scene && p.PointId == id);
                    NpcActor actor = null;
                    foreach (var routine in UnityEngine.Object.FindObjectsByType<NpcRoutine>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        var so = new SerializedObject(routine); var steps = so.FindProperty("_steps");
                        for (int i = 0; i < steps.arraySize; i++)
                            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("point").objectReferenceValue == point)
                                actor = so.FindProperty("_actor").objectReferenceValue as NpcActor;
                    }
                    if (actor == null || point.HeldPropPrefab == null) throw new InvalidOperationException("실제 몸/도구 연결이 없습니다: " + id);
                    var clone = UnityEngine.Object.Instantiate(actor.gameObject);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, preview);
                    clone.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                    var animator = clone.GetComponentInChildren<Animator>(true);
                    Vector3 modelPosition = animator.transform.localPosition;
                    Quaternion modelRotation = animator.transform.localRotation;
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var prop = UnityEngine.Object.Instantiate(point.HeldPropPrefab, hand, false);
                    AnimationMode.StartAnimationMode();
                    foreach (var motion in point.Motion.motionSet.motions)
                    {
                        if (motion.motionClip == null) continue;
                        for (int sample = 0; sample < 5; sample++)
                        {
                            AnimationMode.BeginSampling();
                            AnimationMode.SampleAnimationClip(animator.gameObject, motion.motionClip, motion.motionClip.length * sample / 5f);
                            AnimationMode.EndSampling();
                            if (animator.transform != clone.transform)
                                animator.transform.SetLocalPositionAndRotation(modelPosition, modelRotation);
                            // Humanoid 클립의 루트 커브가 프리뷰 몸을 원점으로 옮기므로 저작된 작업 자세로 환산한다.
                            clone.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
                            Vector3 bottom = new(0f, float.PositiveInfinity, 0f);
                            foreach (var mesh in prop.GetComponentsInChildren<MeshFilter>())
                                foreach (var vertex in mesh.sharedMesh.vertices)
                                {
                                    Vector3 world = mesh.transform.TransformPoint(vertex);
                                    if (world.y < bottom.y) bottom = world;
                                }
                            rows.Add(id + " " + motion.motionClip.name + " sample=" + sample
                                + " hand=" + hand.position.ToString("F4") + " toolBottom=" + bottom.ToString("F4"));
                        }
                    }
                    AnimationMode.StopAnimationMode();
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                EditorSceneManager.ClosePreviewScene(preview);
            }
            File.WriteAllLines("output/NpcLifeAI/contact-samples.txt", rows);
        }

        [UPlaygroundTool("NpcLifeAI.PrepareRepresentativeBindings")]
        public static void PrepareRepresentativeBindings()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일을 복구하고 편집 모드에서 실행해야 합니다.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var routines = UnityEngine.Object.FindObjectsByType<NpcRoutine>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.gameObject.scene == scene).ToArray();
            if (routines.Length != 4) throw new InvalidOperationException("대표 일과 4개가 있는 현장을 먼저 확인해야 합니다.");
            if (scene.GetRootGameObjects().Any(g => g.name == "NpcLifeAI_Review"))
                throw new InvalidOperationException("검수 콘텐츠가 이미 있습니다. 기존 연결을 보존합니다.");
            var loader = UnityEngine.Object.FindObjectsByType<RuntimePlacementLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(l => l.gameObject.scene == scene && l.PlacementData != null
                    && l.PlacementData.Records.Any(r => r.sceneEntityGuid == "9b15226e9d9f910a2feb8cb55a36e641"));
            Directory.CreateDirectory(ProfileFolder);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("대표 NPC 생활 연결");
            try
            {
                var root = new GameObject("NpcLifeAI_Review"); Undo.RegisterCreatedObjectUndo(root, "생활 검수 콘텐츠");
                var first = CreateVisitor(root.transform, "9b15226e9d9f910a2feb8cb55a36e641", new Vector3(975, 0, 414), new Vector3(979, 0, 416), "ResidentFarm", 1f);
                var second = CreateVisitor(root.transform, "3a977e6398fb451241196bfb96002711", new Vector3(986, 0, 417), new Vector3(982, 0, 416), "ResidentMarket", 3f);
                var social = Undo.AddComponent<NpcSocialPoint>(CreateChild(root.transform, "Social_FarmEdge"));
                Set(social, "_participants", new UnityEngine.Object[] { first.GetComponent<NpcLifeController>(), second.GetComponent<NpcLifeController>() });
                Set(social, "_greeting", LoadMotion("Npc_Greet"));
                Set(social, "_response", LoadMotion("Npc_Greet"));
                ConfigureStep(social, "_first", CreatePoint(root.transform, "life.social.first", new Vector3(980, 0, 415), 90f), 5f);
                ConfigureStep(social, "_second", CreatePoint(root.transform, "life.social.second", new Vector3(981.5f, 0, 415), 270f), 5f);
                Set(first.GetComponent<NpcLifeController>(), "_socialPoint", social);
                Set(second.GetComponent<NpcLifeController>(), "_socialPoint", social);
                var binding = Undo.AddComponent<NpcLifePlacementBinding>(root);
                Set(binding, "_loader", loader);
                var so = new SerializedObject(binding); var entries = so.FindProperty("_entries"); entries.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("sceneEntityGuid").stringValue = i == 0 ? "9b15226e9d9f910a2feb8cb55a36e641" : "3a977e6398fb451241196bfb96002711";
                    entry.FindPropertyRelative("routine").objectReferenceValue = i == 0 ? first : second;
                }
                so.ApplyModifiedProperties();
                foreach (var routine in routines)
                {
                    var serialized = new SerializedObject(routine);
                    var actor = serialized.FindProperty("_actor").objectReferenceValue as NpcActor;
                    if (actor == null) throw new InvalidOperationException("기존 몸 참조가 없습니다.");
                    // 역할은 기존 작업점 데이터로 결정한다. 런타임 이름 분기는 추가하지 않는다.
                    var point = serialized.FindProperty("_steps").GetArrayElementAtIndex(0).FindPropertyRelative("point").objectReferenceValue as NpcWorkPoint;
                    string role = point.PointId.Contains("meal") ? "WorkerCook" : point.PointId.Contains("repair") ? "WorkerRepair" : null;
                    if (role == null) continue;
                    var life = Undo.AddComponent<NpcLifeController>(routine.gameObject);
                    Set(life, "_profile", Profile(role, false)); Set(life, "_residentId", actor.ActorId); Set(life, "_neighborhoodId", "lake.village");
                    var steps = serialized.FindProperty("_steps");
                    steps.GetArrayElementAtIndex(role == "WorkerCook" ? 0 : 1).FindPropertyRelative("endMotion").objectReferenceValue
                        = LoadMotion("LivingWorld/" + (role == "WorkerCook" ? "LW_CookEnd" : "LW_RepairEnd"));
                    serialized.ApplyModifiedProperties();
                }
                var environment = Undo.AddComponent<NpcRoutineEnvironmentBinding>(root);
                Set(environment, "_routines", new UnityEngine.Object[] { first, second });
                Set(environment, "_consumerMapId", "LakeOfLifeScenario");
                var producer = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
                Set(environment, "_environment", producer);
                var gathering = UnityEngine.Object.FindObjectsByType<GatheringActor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(g => g.gameObject.scene == scene && (g.transform.position - first.transform.position).sqrMagnitude < 400f)
                    .OrderBy(g => (g.transform.position - first.transform.position).sqrMagnitude).FirstOrDefault();
                if (gathering != null)
                {
                    var stimulus = Undo.AddComponent<NpcLifeStimulusSource>(gathering.gameObject);
                    Set(stimulus, "_mapId", "LakeOfLifeScenario");
                    Set(stimulus, "_listeners", new UnityEngine.Object[] { first.GetComponent<NpcLifeController>(), second.GetComponent<NpcLifeController>() });
                }
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static NpcRoutine CreateVisitor(Transform parent, string guid, Vector3 home, Vector3 visit, string role, float delay)
        {
            var go = CreateChild(parent, role); go.transform.position = new Vector3(home.x, 61f, home.z);
            var routine = Undo.AddComponent<NpcRoutine>(go);
            Set(routine, "_mapId", "LakeOfLifeScenario"); Set(routine, "_startDelaySeconds", delay);
            var so = new SerializedObject(routine); var steps = so.FindProperty("_steps"); steps.arraySize = 1; so.ApplyModifiedProperties();
            ConfigureStep(routine, "_steps.Array.data[0]", CreatePoint(parent, "life.visit." + role, visit, 180f), 10f + delay);
            ConfigureStep(routine, "_rest", CreatePoint(parent, "life.rest." + role, home, 0f), 8f + delay);
            var life = Undo.AddComponent<NpcLifeController>(go);
            Set(life, "_profile", Profile(role, true)); Set(life, "_residentId", guid); Set(life, "_neighborhoodId", "lake.farm.edge");
            return routine;
        }

        private static NpcWorkPoint CreatePoint(Transform parent, string id, Vector3 position, float yaw)
        {
            var hit = Physics.RaycastAll(position + Vector3.up * 200f, Vector3.down, 400f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider is TerrainCollider).OrderByDescending(h => h.point.y).FirstOrDefault();
            if (hit.collider == null)
                throw new InvalidOperationException("작업점 지면을 찾을 수 없습니다: " + id);
            var go = CreateChild(parent, id); go.transform.SetPositionAndRotation(hit.point + Vector3.up * 0.05f, Quaternion.Euler(0f, yaw, 0f));
            var point = Undo.AddComponent<NpcWorkPoint>(go);
            Set(point, "_mapId", "LakeOfLifeScenario"); Set(point, "_pointId", id); Set(point, "_motion", LoadMotion("LivingWorld/LW_RestStand"));
            return point;
        }

        private static GameObject CreateChild(Transform parent, string name)
        { var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "생활 연결"); go.transform.SetParent(parent, false); return go; }

        private static NpcLifeProfileSO Profile(string role, bool social)
        {
            string path = ProfileFolder + "/" + role + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<NpcLifeProfileSO>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<NpcLifeProfileSO>(); profile.roleId = role; profile.canSocialize = social;
            profile.socialRadius = 16f; profile.noticeProbability = social ? 0.65f : 0.2f;
            AssetDatabase.CreateAsset(profile, path); return profile;
        }

        private static MotionSetAsset LoadMotion(string name)
        {
            var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(MotionFolder + name + ".asset");
            if (motion == null) throw new InvalidOperationException("모션이 없습니다: " + name);
            return motion;
        }

        private static void ConfigureStep(UnityEngine.Object target, string path, NpcWorkPoint point, float seconds)
        {
            var so = new SerializedObject(target); var step = so.FindProperty(path);
            step.FindPropertyRelative("point").objectReferenceValue = point; step.FindPropertyRelative("seconds").floatValue = seconds;
            so.ApplyModifiedProperties();
        }

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            Undo.RecordObject(target, "생활 연결"); var so = new SerializedObject(target); var property = so.FindProperty(field);
            if (value is UnityEngine.Object[] array)
            { property.arraySize = array.Length; for (int i = 0; i < array.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = array[i]; }
            else if (value == null) property.objectReferenceValue = null;
            else if (value is UnityEngine.Object obj) property.objectReferenceValue = obj;
            else if (value is string text) property.stringValue = text;
            else if (value is float number) property.floatValue = number;
            else if (value is bool flag) property.boolValue = flag;
            so.ApplyModifiedProperties();
        }
    }
}
