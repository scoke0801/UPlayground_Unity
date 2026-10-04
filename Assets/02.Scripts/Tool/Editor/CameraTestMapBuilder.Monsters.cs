using System;
using System.IO;
using System.Linq;
using KinematicCharacterController;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor
{
    public static partial class CameraTestMapBuilder
    {
        private const string MonsterDirectory = ModelDirectory + "/Monsters";
        private const string MonsterPrefabDirectory = "Assets/03.Prefabs/Actor/Monster/CameraTestMap";

        [Serializable] private sealed class MonsterLayout
        {
            public MonsterPlacement[] monsters;
            public MonsterMaterial[] materials;
        }

        [Serializable] private sealed class MonsterPlacement
        {
            public string name;
            public Vector3 position;
            public bool isAirborne;
            public float radius;
            public float height;
            public float centerY;
        }

        [Serializable] private sealed class MonsterMaterial
        {
            public string name;
            public Color color;
            public float emission;
        }

        /// <summary>기존 시험장 배치를 보존하며 Blender 몬스터 표적 두 종을 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/카메라/카메라 테스트 몬스터 연결")]
        public static void AddMonsterTargets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 몬스터를 연결하세요.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                if (EnsureMonsterTargets(scene))
                {
                    ConfigureRuntime(scene);
                    ValidateScene(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("몬스터 배치 씬을 저장하지 못했습니다.");
                }
                else ValidateScene(scene);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static MonsterLayout ReadMonsterLayout()
        {
            return JsonUtility.FromJson<MonsterLayout>(File.ReadAllText(MonsterDirectory + "/Monsters.layout.json"));
        }

        private static bool EnsureMonsterTargets(Scene scene)
        {
            MonsterLayout layout = ReadMonsterLayout();
            bool changed = false;
            foreach (MonsterPlacement entry in layout.monsters)
            {
                string path = MonsterPrefabDirectory + "/MonsterActor_" + entry.name + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    EnsureMonsterController(entry.name, MonsterDirectory + "/" + entry.name + ".fbx");
                if (FindMonsterInstance(scene, path) != null)
                    continue;
                if (prefab == null)
                    prefab = CreateMonsterPrefab(entry, layout.materials, path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "Target_" + entry.name;
                instance.transform.SetPositionAndRotation(entry.position, Quaternion.Euler(0, 180, 0));
                instance.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                changed = true;
            }
            return changed;
        }

        private static MonsterActor FindMonsterInstance(Scene scene, string path)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonsterActor actor in root.GetComponentsInChildren<MonsterActor>(true))
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actor.gameObject) == path)
                    return actor;
            }
            return null;
        }

        private static GameObject CreateMonsterPrefab(MonsterPlacement entry, MonsterMaterial[] palette, string path)
        {
            Directory.CreateDirectory(MonsterPrefabDirectory);
            AssetDatabase.Refresh();
            string modelPath = MonsterDirectory + "/" + entry.name + ".fbx";
            ConfigureMonsterModel(modelPath, palette);
            // 훈련 액터의 초기화·무적·서비스 계약을 상속하고 시각 모델만 대체한다.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(TargetPath));
            instance.SetActive(false);
            try
            {
                instance.name = "MonsterActor_" + entry.name;
                foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    renderer.enabled = false;
                var actor = new SerializedObject(instance.GetComponent<MonsterActor>());
                actor.FindProperty("_actorId").stringValue = "CameraTest." + entry.name;
                actor.FindProperty("_isInvincible").boolValue = true;
                actor.ApplyModifiedPropertiesWithoutUndo();
                var decal = actor.FindProperty("_lockOnDecal").objectReferenceValue as GameObject;
                if (decal != null)
                {
                    decal.transform.position = instance.transform.TransformPoint(Vector3.up * entry.centerY);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(decal.transform);
                }
                var motor = instance.GetComponent<KinematicCharacterMotor>();
                motor.SetCapsuleDimensions(entry.radius, entry.height, entry.centerY);
                // 공중 표적은 고도를 고정한다. 보이지 않는 발판을 두면 카메라 충돌 검증을 오염시킨다.
                motor.enabled = !entry.isAirborne;
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(modelPath), instance.transform);
                model.name = "BlenderVisual";
                foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                AnimatorController controller = EnsureMonsterController(entry.name, modelPath);
                Animator animator = model.GetComponent<Animator>();
                if (animator == null)
                    animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                if (prefab == null)
                    throw new IOException("몬스터 프리팹 저장 실패: " + path);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static AnimatorController EnsureMonsterController(string name, string modelPath)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(value => !value.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (clip == null)
                throw new InvalidOperationException("Blender 대기 애니메이션 누락: " + modelPath);
            string path = MonsterDirectory + "/" + name + ".controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            // 이전 생성이 중단됐어도 비어 있는 Controller를 정상 에셋으로 재사용하지 않는다.
            if (controller.layers.Length == 0)
                controller.AddLayer("Base Layer");
            if (controller.layers[0].stateMachine.states.Length == 0)
                controller.AddMotion(clip);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static void ConfigureMonsterModel(string path, MonsterMaterial[] palette)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Blender FBX를 먼저 내보내세요: " + path);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (MonsterMaterial entry in palette)
            {
                string materialPath = MonsterDirectory + "/" + entry.name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = entry.name };
                    material.SetColor("_BaseColor", entry.color);
                    material.SetFloat("_Smoothness", 0.35f);
                    if (entry.emission > 0)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", entry.color * entry.emission);
                    }
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), material);
            }
            importer.SaveAndReimport();
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootHeightY = true;
                clip.lockRootRotation = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ValidateMonsterTargets(Scene scene)
        {
            foreach (MonsterPlacement entry in ReadMonsterLayout().monsters)
            {
                string path = MonsterPrefabDirectory + "/MonsterActor_" + entry.name + ".prefab";
                MonsterActor actor = FindMonsterInstance(scene, path);
                if (actor == null)
                    throw new InvalidOperationException("몬스터 표적 누락: " + path);
                var motor = actor.GetComponent<KinematicCharacterMotor>();
                if (motor == null || motor.enabled == entry.isAirborne ||
                    Mathf.Abs(motor.Capsule.height - entry.height) > 0.05f)
                    throw new InvalidOperationException("몬스터 고도/충돌체 설정 오류: " + actor.name);
                Transform model = actor.transform.Find("BlenderVisual");
                if (model == null)
                    throw new InvalidOperationException("Blender 모델 누락: " + actor.name);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = default;
                bool hasVisibleMesh = false;
                foreach (Renderer renderer in renderers)
                {
                    if (!renderer.enabled) continue;
                    if (!hasVisibleMesh) bounds = renderer.bounds;
                    else bounds.Encapsulate(renderer.bounds);
                    hasVisibleMesh = true;
                    if (renderer.sharedMaterials.Any(value => value == null || value.shader == null))
                        throw new InvalidOperationException("몬스터 머티리얼 누락: " + renderer.name);
                }
                if (!hasVisibleMesh || bounds.size.y < entry.height * 0.7f || bounds.size.y > entry.height * 1.4f)
                    throw new InvalidOperationException("몬스터 모델 크기/축 오류: " + actor.name + ", " + bounds.size);
                if (entry.isAirborne && bounds.min.y < 2)
                    throw new InvalidOperationException("공중 표적이 지면과 너무 가깝습니다: " + actor.name);
                Animator animator = model.GetComponent<Animator>();
                var controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                if (controller == null || controller.layers.Length == 0 ||
                    controller.layers[0].stateMachine.defaultState == null ||
                    controller.layers[0].stateMachine.defaultState.motion == null)
                    throw new InvalidOperationException("몬스터 대기 애니메이션 연결 누락: " + actor.name);
            }
        }
    }
}
