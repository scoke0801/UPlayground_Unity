using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UPlayGround.Data.EnumType;
using UPlayGround.EditorTools;
using UPlayGround.Debugging;
using UPlayGround.Components;

namespace UPlayGround.Tool.Editor
{
    /// <summary>블렌더 시험장을 실제 플레이어·훈련 표적·카메라 서비스와 연결한다.</summary>
    public static partial class CameraTestMapBuilder
    {
        public const string ScenePath = "Assets/01.Scenes/Test/CameraTestMap.unity";
        private const string ModelDirectory = "Assets/05.Models/CameraTestMap";
        private const string ModelPath = ModelDirectory + "/CameraTestMap.fbx";
        private const string PlayerPath = "Assets/03.Prefabs/Actor/Player/Player.prefab";
        private const string PlayerModelPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
        private const string TargetPath = "Assets/03.Prefabs/Actor/Monster/MonsterActor_Training_Dummy.prefab";

        [Serializable] private sealed class Layout
        {
            public Vector3 player;
            public TargetPlacement[] targets;
            public MaterialEntry[] materials;
        }

        [Serializable] private sealed class TargetPlacement
        {
            public string name;
            public Vector3 position;
        }

        [Serializable] private sealed class MaterialEntry
        {
            public string name;
            public Color color;
        }

        /// <summary>첫 실행 때 씬을 생성하고, 이후에는 배치 수정 내용을 보존하며 연다.</summary>
        [UPlaygroundTool("UPlayGround/월드/카메라/카메라 테스트 맵 열기")]
        public static void OpenOrCreate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 테스트 맵을 여세요.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (File.Exists(ScenePath))
            {
                Scene existing = EditorSceneManager.OpenScene(ScenePath);
                bool changed = EnsurePlayerModel(existing);
                changed |= EnsureMonsterTargets(existing);
                if (UnityEngine.Object.FindFirstObjectByType<CameraTestMapRuntime>() == null)
                {
                    ConfigureRuntime(existing);
                    changed = true;
                }
                if (changed)
                {
                    ConfigureRuntime(existing);
                    EditorSceneManager.SaveScene(existing);
                }
                ValidateScene(existing);
                return;
            }

            AssetDatabase.Refresh();
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(ModelDirectory + "/CameraTestMap.layout.json"));
            GameObject playerPrefab = RequirePrefab(PlayerPath);
            GameObject targetPrefab = RequirePrefab(TargetPath);
            ConfigureModel(layout);
            GameObject model = RequirePrefab(ModelPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment(model);
            CreateLightingAndCamera();
            var context = new GameObject("SceneContext").AddComponent<SceneContext>();
            context.SceneType = SceneType.Test;
            // 지역 진행 데이터를 연결하지 않는 독립 시험장이므로 MapID를 비운다.
            context.MapID = string.Empty;
            PlacePrefab(playerPrefab, "Player", layout.player);
            Transform targets = new GameObject("LockOnTargets").transform;
            foreach (TargetPlacement placement in layout.targets)
            {
                GameObject target = PlacePrefab(targetPrefab, placement.name, placement.position);
                target.transform.SetParent(targets, true);
                var actor = new SerializedObject(target.GetComponent<MonsterActor>());
                actor.FindProperty("_isInvincible").boolValue = true;
                actor.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsurePlayerModel(scene);
            EnsureMonsterTargets(scene);
            ConfigureRuntime(scene);
            ValidateScene(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("카메라 테스트 씬을 저장하지 못했습니다.");
            Debug.Log("[CameraTestMap] 생성 완료: " + ScenePath);
        }

        private static GameObject RequirePrefab(string path)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
                throw new InvalidOperationException("필수 에셋을 찾을 수 없습니다: " + path);
            return asset;
        }

        private static void ConfigureRuntime(Scene scene)
        {
            var targets = new System.Collections.Generic.List<MonsterActor>();
            PlayerActor player = null;
            SceneContext context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                targets.AddRange(root.GetComponentsInChildren<MonsterActor>(true));
                player = root.GetComponent<PlayerActor>() ?? player;
                context = root.GetComponent<SceneContext>() ?? context;
            }
            if (player == null || context == null || targets.Count == 0)
                throw new InvalidOperationException("시험장 실행에 필요한 액터/SceneContext가 없습니다.");
            context.enabled = false;
            player.gameObject.SetActive(false);
            foreach (MonsterActor target in targets)
                target.gameObject.SetActive(false);
            CameraTestMapRuntime runtime = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                runtime = root.GetComponent<CameraTestMapRuntime>() ?? runtime;
            if (runtime == null)
                runtime = new GameObject("CameraTestMapRuntime").AddComponent<CameraTestMapRuntime>();
            var serialized = new SerializedObject(runtime);
            serialized.FindProperty("_sceneContext").objectReferenceValue = context;
            serialized.FindProperty("_player").objectReferenceValue = player.gameObject;
            SerializedProperty entries = serialized.FindProperty("_targets");
            entries.arraySize = targets.Count;
            for (int i = 0; i < targets.Count; i++)
                entries.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool EnsurePlayerModel(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                PlayerActor player = root.GetComponent<PlayerActor>();
                if (player == null || player.GetComponentInChildren<CharacterModelData>(true) != null)
                    continue;
                // 고정 캐릭터 시험장은 스트리밍보다 먼저 Idle/FootIK가 시작되지 않도록 기존 모델을 내장한다.
                PlayerSwapBehaviour swap = player.GetComponent<PlayerSwapBehaviour>();
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(PlayerModelPath), swap.ModelRoot);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.SetActive(true);
                return true;
            }
            return false;
        }

        private static void ConfigureModel(Layout layout)
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("블렌더 FBX를 먼저 내보내세요: " + ModelPath);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit 셰이더를 찾을 수 없습니다.");
            foreach (MaterialEntry entry in layout.materials)
            {
                string path = ModelDirectory + "/" + entry.name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = entry.name };
                    material.SetColor("_BaseColor", entry.color);
                    material.SetFloat("_Smoothness", 0.2f);
                    AssetDatabase.CreateAsset(material, path);
                }
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name), material);
            }
            importer.SaveAndReimport();
        }

        private static void CreateEnvironment(GameObject model)
        {
            var environment = (GameObject)PrefabUtility.InstantiatePrefab(model);
            environment.name = "Blender_CameraTestMap";
            foreach (MeshFilter filter in environment.GetComponentsInChildren<MeshFilter>())
            {
                bool isSolid = filter.name.StartsWith("COL_", StringComparison.Ordinal);
                filter.gameObject.layer = LayerMask.NameToLayer(isSolid ? "Ground" : "Ignore Raycast");
                if (!isSolid)
                    continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }

        private static GameObject PlacePrefab(GameObject prefab, string name, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        private static void CreateLightingAndCamera()
        {
            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 3, -27), Quaternion.Euler(15, 0, 0));
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.14f, 0.2f, 0.28f);
            camera.gameObject.AddComponent<AudioListener>();
            Light sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.42f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.23f, 0.26f);
            RenderSettings.fog = false;
        }

        /// <summary>모델 축·충돌 레이어·표적 발판·누락 스크립트를 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/카메라/카메라 테스트 맵 검증")]
        public static void Validate()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("카메라 테스트 맵을 먼저 여세요.");
            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene)
        {
            Physics.SyncTransforms();
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(ModelDirectory + "/CameraTestMap.layout.json"));
            int colliders = 0;
            int monsters = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    foreach (TargetPlacement target in layout.targets)
                    {
                        if (renderer.name != "VIS_" + target.name)
                            continue;
                        Vector3 center = renderer.bounds.center;
                        if (Vector2.Distance(new Vector2(center.x, center.z),
                                new Vector2(target.position.x, target.position.z)) > 0.05f)
                            throw new InvalidOperationException($"FBX 표적 마커 좌표 불일치: {renderer.name}, 실제={center}, 기대={target.position}");
                    }
                }
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        throw new InvalidOperationException("Missing Script: " + child.name);
                }
                foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>())
                {
                    if (collider.sharedMesh == null || collider.gameObject.layer != LayerMask.NameToLayer("Ground"))
                        throw new InvalidOperationException("환경 충돌체 배선 오류: " + collider.name);
                    colliders++;
                }
                foreach (MonsterActor monster in root.GetComponentsInChildren<MonsterActor>(true))
                {
                    var motor = monster.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
                    if (motor != null && motor.enabled)
                        ValidateGround(monster.transform.position);
                    monsters++;
                }
                PlayerActor player = root.GetComponent<PlayerActor>();
                if (player != null)
                    ValidateGround(player.transform.position);
            }
            // 비대칭 위치의 높은 발판으로 FBX 축/스케일 오류를 잡는다.
            ValidateGround(new Vector3(1, 4.08f, 24));
            ValidateGround(new Vector3(0, 2.08f, 15));
            ValidateMonsterTargets(scene);
            int expectedTargets = layout.targets.Length + ReadMonsterLayout().monsters.Length;
            if (colliders < 15 || monsters != expectedTargets)
                throw new InvalidOperationException($"시험장 구성 누락: 충돌체={colliders}, 표적={monsters}");
            Debug.Log($"[CameraTestMap] 검증 통과: 환경 충돌체 {colliders}, 표적 {monsters}, Missing Script 0");
        }

        private static void ValidateGround(Vector3 position)
        {
            if (!Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit,
                    1, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore)
                || Mathf.Abs(hit.point.y - position.y) > 0.2f)
                throw new InvalidOperationException("표적/시작 위치의 발판 또는 FBX 좌표가 잘못되었습니다: " + position);
        }
    }
}
