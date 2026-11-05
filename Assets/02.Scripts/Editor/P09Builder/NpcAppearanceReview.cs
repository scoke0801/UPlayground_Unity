using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UPlayGround.Data.Actor;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>저장된 NPC 프리팹을 수정 없이 URP로 촬영하고 연결·계층 비용을 기록한다.</summary>
    public static class NpcAppearanceReview
    {
        public const string Output = "output/NpcAppearanceReview";
        private static CharacterBuildConfig[] _configs;
        private static int _next;
        private static int _end;

        /// <summary>288종을 Editor 업데이트마다 하나씩 촬영한다. 런처에는 등록하지 않는다.</summary>
        [UPlaygroundTool("p09.npc-appearance.capture")]
        public static void CaptureAll() => StartCapture(0, 288);

        /// <summary>지정 범위만 촬영하여 렌더 설정 및 실패 지점의 재현에 사용한다.</summary>
        public static void StartCapture(int start, int count)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || _configs != null)
                throw new InvalidOperationException("촬영은 Edit Mode에서 한 번만 실행하세요.");
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                throw new InvalidOperationException("URP 렌더 파이프라인이 필요합니다.");
            var configs = NpcAppearanceGenerator.CreateConfigs(
                AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath)).ToArray();
            if (configs.Length != 288 || start < 0 || count < 1 || start + count > configs.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            Directory.CreateDirectory(Output + "/raw");
            Directory.CreateDirectory(Output + "/audit");
            _configs = configs;
            _next = start;
            _end = start + count;
            EditorApplication.update += CaptureNext;
            AssemblyReloadEvents.beforeAssemblyReload += StopCapture;
        }

        private static void StopCapture()
        {
            EditorApplication.update -= CaptureNext;
            AssemblyReloadEvents.beforeAssemblyReload -= StopCapture;
            _configs = null;
        }

        private static void CaptureNext()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Play Mode 진입으로 촬영을 중단했습니다.");
                Capture(_configs[_next]);
                _next++;
                File.WriteAllText(Output + "/progress.txt", $"{_next}/{_end}\nUnity {Application.unityVersion}\n{UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name}\n");
                if (_next == _end) StopCapture();
            }
            catch (Exception exception)
            {
                File.WriteAllText(Output + "/capture-error.txt", exception.ToString());
                StopCapture();
                Debug.LogException(exception);
            }
        }

        private static void Capture(CharacterBuildConfig config)
        {
            string id = config.ManualName;
            string path = $"{NpcAppearanceGenerator.PrefabRoot}/Npc/{id}/{id}.prefab";
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            double loadMs = timer.Elapsed.TotalMilliseconds;
            if (prefab == null) throw new InvalidOperationException(path);
            var audit = Inspect(prefab, config);
            audit.loadMs = loadMs;
            timer.Restart();
            var preview = new PreviewRenderUtility();
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview.camera.scene);
                audit.instantiateMs = timer.Elapsed.TotalMilliseconds;
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    skin.updateWhenOffscreen = true;
                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.22f, 0.23f, 0.25f);
                camera.fieldOfView = 30;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20;
                camera.allowHDR = false;
                preview.lights[0].intensity = 1.3f;
                preview.lights[0].transform.rotation = Quaternion.Euler(25, 155, 0);
                preview.lights[1].intensity = 0.8f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340, 205, 0);
                preview.ambientColor = new Color(0.55f, 0.55f, 0.55f);
                // 저장된 공통 바인드 포즈를 유지한다. 프리셋 재적용은 실제 생성물의 오류를 가릴 수 있다.
                Render(preview, new Vector3(0, 0.82f, 0), 3.0f, 384, 576, Output + "/raw/" + id + "_body.png");
                Render(preview, new Vector3(0, 1.46f, 0), 0.90f, 384, 384, Output + "/raw/" + id + "_face.png");
                if (id.EndsWith("_01", StringComparison.Ordinal))
                {
                    Render(preview, new Vector3(0, 0.82f, 0), 3.0f, 384, 576, Output + "/raw/" + id + "_side.png", 90);
                    Render(preview, new Vector3(0, 0.82f, 0), 3.0f, 384, 576, Output + "/raw/" + id + "_back.png", 180);
                }
                File.WriteAllText(Output + "/audit/" + id + ".json", JsonUtility.ToJson(audit, true));
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                preview.Cleanup();
            }
        }

        private static void Render(PreviewRenderUtility preview, Vector3 target, float distance,
            int width, int height, string path, float yaw = 0)
        {
            var camera = preview.camera;
            camera.transform.position = target + Quaternion.Euler(0, yaw, 0) * Vector3.forward * distance;
            camera.transform.LookAt(target);
            // 기존 P09 촬영과 동일하게 첫 GPU 업로드 프레임을 버린다.
            for (int pass = 0; pass < 2; pass++)
            {
                preview.BeginStaticPreview(new Rect(0, 0, width, height));
                preview.Render(true);
                var texture = preview.EndStaticPreview();
                try { if (pass == 1) File.WriteAllBytes(path, texture.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
        }

        private static ReviewAudit Inspect(GameObject prefab, CharacterBuildConfig config)
        {
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var actor = prefab.GetComponent<NpcActor>();
            var serialized = new SerializedObject(actor);
            var definition = serialized.FindProperty("_definition").objectReferenceValue as ActorDefinitionSO;
            var data = serialized.FindProperty("_data").objectReferenceValue as NpcActorSO;
            var dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { prefab });
            return new ReviewAudit
            {
                id = config.ManualName,
                prefabPath = AssetDatabase.GetAssetPath(prefab),
                prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)),
                prefabBytes = new FileInfo(AssetDatabase.GetAssetPath(prefab)).Length,
                transforms = transforms.Length,
                inactiveTransforms = transforms.Count(x => !IsActiveInPrefab(x)),
                renderers = renderers.Length,
                inactiveRenderers = renderers.Count(x => !IsActiveInPrefab(x.transform)),
                visibleRenderers = renderers.Count(x => IsActiveInPrefab(x.transform) && x.enabled),
                missingScripts = transforms.Sum(x => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)),
                linksMatch = definition != null && definition.prefab == prefab && definition.npcData == data && data != null && definition.actorId == config.ManualName,
                definitionPath = AssetDatabase.GetAssetPath(definition),
                npcDataPath = AssetDatabase.GetAssetPath(data),
                hasDialogue = data != null && data.dialogueGraph != null,
                hasMerchant = data != null && data.merchantCatalog != null,
                hierarchyNativeBytes = transforms.Sum(x => Profiler.GetRuntimeMemorySizeLong(x.gameObject))
                    + prefab.GetComponentsInChildren<Component>(true).Where(x => x != null).Sum(Profiler.GetRuntimeMemorySizeLong),
                dependencyNativeBytes = dependencies.Where(x => x != null).Sum(Profiler.GetRuntimeMemorySizeLong),
                dependencyCount = dependencies.Length,
                expectedHair = config.HairStyleSo?.name,
                expectedHairColor = config.HairColorSo?.name,
                expectedFace = config.FaceTypeSo?.name,
                expectedSkin = config.SkinColorSo?.name,
                expectedEye = config.EyeColorSo?.name,
                expectedBust = config.BustSizeSo?.name,
                expectedBeard = config.FacialHairId,
                activeRenderers = renderers.Where(x => x.enabled && IsActiveInPrefab(x.transform)).Select(x => new RendererAudit
                {
                    name = x.name,
                    mesh = x is SkinnedMeshRenderer skin ? skin.sharedMesh?.name : x.GetComponent<MeshFilter>()?.sharedMesh?.name,
                    materials = x.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray()
                }).ToArray(),
                bustBones = transforms.Where(x => x.name == "bust_01_R" || x.name == "bust_01_L")
                    .Select(x => x.name + ":" + x.localScale.ToString("F3")).ToArray()
            };
        }

        private static bool IsActiveInPrefab(Transform transform)
        {
            // Prefab Asset의 activeInHierarchy는 씬 인스턴스와 달리 false이므로 저장된 부모 체인을 검사한다.
            for (var current = transform; current != null; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        [Serializable]
        private sealed class ReviewAudit
        {
            public string id, prefabPath, prefabGuid, definitionPath, npcDataPath;
            public long prefabBytes, hierarchyNativeBytes, dependencyNativeBytes;
            public int transforms, inactiveTransforms, renderers, inactiveRenderers, visibleRenderers, missingScripts, dependencyCount;
            public bool linksMatch, hasDialogue, hasMerchant;
            public double loadMs, instantiateMs;
            public string expectedHair, expectedHairColor, expectedFace, expectedSkin, expectedEye, expectedBust;
            public int expectedBeard;
            public RendererAudit[] activeRenderers;
            public string[] bustBones;
        }

        [Serializable]
        private sealed class RendererAudit
        {
            public string name, mesh;
            public string[] materials;
        }
    }
}
