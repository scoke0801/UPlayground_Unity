using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.UI;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string RemovalDirectory = BaselineDirectory + "/Removal";

        [Serializable] private sealed class RemovalManifest
        {
            public string scene;
            public RemovalTarget[] targets;
            public BaselineFile[] files;
            public BaselineFile[] protectedFiles;
        }

        [Serializable] private sealed class RemovalTarget
        {
            public string path;
            public string globalObjectId;
            public string reason;
        }

        [Serializable] private sealed class BaselineFile
        {
            public string path;
            public string sha256;
        }

        /// <summary>감사한 ID 목록만 제거하고 실패하면 삭제 직전 파일로 복구한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P00 배치 정리")]
        public static void RemoveScenarioBaselinePlacements()
        {
            RequireEditMode();
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 배치 정리를 중단합니다.");
            var manifest = JsonUtility.FromJson<RemovalManifest>(File.ReadAllText(RemovalDirectory + "/RemovalManifest.json"));
            Layout layout = ReadLayout();
            if (manifest.scene != layout.scenePath || manifest.scene == SourceScenePath)
                throw new InvalidOperationException("제거 목록의 대상 씬이 다릅니다.");
            VerifyBaselineHashes(manifest.files);
            VerifyBaselineHashes(manifest.protectedFiles);
            Scene scene = EditorSceneManager.OpenScene(manifest.scene);
            var targets = ResolveRemovalTargets(scene, manifest);
            AssertNoSurvivingReferences(scene, targets);
            Dictionary<string, string> identities = CaptureIdentities(scene);
            CaptureBaselineView(scene, "Before");
            try
            {
                foreach (GameObject target in targets) UnityEngine.Object.DestroyImmediate(target);
                AssertBaselineReferences(scene);
                foreach (BaselineFile file in manifest.files)
                {
                    string staged = RemovalDirectory + "/staged/" + file.path;
                    if (File.Exists(staged)) File.Copy(staged, file.path, true);
                }
                var region = AssetDatabase.LoadAssetAtPath<MapRegionInfoSO>(layout.assetDirectory + "/RegionInfo.asset");
                if (region == null) throw new InvalidOperationException("시나리오 RegionInfo 누락");
                region.flowGraphs.Clear();
                EditorUtility.SetDirty(region);
                AssetDatabase.SaveAssetIfDirty(region);
                SaveQualityScene(scene, layout);
                scene = EditorSceneManager.OpenScene(manifest.scene);
                AssertRemovalResult(scene, manifest);
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                        if (!identities.TryGetValue(GetTransformPath(identity.transform), out string expected) || expected != identity.Guid)
                            throw new InvalidOperationException("보존 개체의 ID가 바뀌었습니다: " + GetTransformPath(identity.transform));
                VerifyBaselineHashes(manifest.protectedFiles);
                CaptureBaselineView(scene, "After");
                InspectBaselineScene(manifest.scene, "ScenarioScene.after.json");
                File.WriteAllText(RemovalDirectory + "/RemovalResult.json",
                    "{\"removedRoots\":" + targets.Count + ",\"reloaded\":true,\"brokenReferences\":0,\"preservedIds\":true}");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (BaselineFile file in manifest.files)
                {
                    File.Copy(RemovalDirectory + "/backup/" + file.path, file.path, true);
                    AssetDatabase.ImportAsset(file.path, ImportAssetOptions.ForceUpdate);
                }
                throw;
            }
        }

        private static List<GameObject> ResolveRemovalTargets(Scene scene, RemovalManifest manifest)
        {
            var targets = new List<GameObject>();
            foreach (RemovalTarget target in manifest.targets)
            {
                if (!GlobalObjectId.TryParse(target.globalObjectId, out GlobalObjectId id))
                    throw new InvalidOperationException("유효하지 않은 ID: " + target.globalObjectId);
                var instance = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                if (instance == null || instance.scene != scene || GetTransformPath(instance.transform) != target.path)
                    throw new InvalidOperationException("제거 대상 ID/경로 불일치: " + target.path);
                targets.Add(instance);
            }
            return targets;
        }

        private static void AssertNoSurvivingReferences(Scene scene, List<GameObject> roots)
        {
            var removed = new HashSet<UnityEngine.Object>();
            foreach (GameObject root in roots)
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    removed.Add(node.gameObject);
                    foreach (Component component in node.GetComponents<Component>()) removed.Add(component);
                }
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || removed.Contains(component)) continue;
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue != null && removed.Contains(property.objectReferenceValue))
                            throw new InvalidOperationException("남는 개체에서 삭제 대상을 참조합니다: " + GetTransformPath(component.transform) + "/" + property.propertyPath);
                }
        }

        private static void AssertBaselineReferences(Scene scene)
        {
            var report = new BaselineScene { scene = scene.path };
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true)) InspectBaselineObject(node, report);
            if (report.errors.Count > 0) throw new InvalidOperationException(string.Join("\n", report.errors));
        }

        private static void AssertRemovalResult(Scene scene, RemovalManifest manifest)
        {
            var paths = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentsInChildren<FlowGraphRunner>(true).Length != 0)
                    throw new InvalidOperationException("구 Flow 실행기가 남았습니다.");
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true)) paths.Add(GetTransformPath(node));
            }
            foreach (RemovalTarget target in manifest.targets)
                if (paths.Contains(target.path)) throw new InvalidOperationException("구 배치 잔존: " + target.path);
            AssertBaselineReferences(scene);
        }

        private static void VerifyBaselineHashes(BaselineFile[] files)
        {
            using var sha = SHA256.Create();
            foreach (BaselineFile file in files)
            {
                string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file.path))).Replace("-", "").ToLowerInvariant();
                if (actual != file.sha256) throw new InvalidOperationException("감사 이후 파일이 변경되었습니다: " + file.path);
            }
        }

        private static void CaptureBaselineView(Scene scene, string label)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("기준선 캡처에는 그래픽 장치가 필요합니다.");
            var node = new GameObject("P00CaptureCamera");
            var camera = node.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(new Vector3(1050, 175, 380), Quaternion.Euler(55, -20, 0));
            camera.farClipPlane = 1500;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(RemovalDirectory + "/" + label + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
