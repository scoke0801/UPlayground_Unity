using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class AmbientFaunaAuthoring
    {
        /// <summary>본편 지형·저작 크기에서 Editor 상태 주입 화면을 보존하고 임시 GPU 자원을 반환한다.</summary>
        public static string CaptureWorldHabitat(string groupName, float hour)
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행하세요.");
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "LakeVillageWorld") throw new InvalidOperationException("본편 씬이 필요합니다.");
            var group = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AmbientFaunaGroup>(true)).Single(g => g.name == groupName);
            var host = group.GetComponentInParent<AmbientFaunaHabitat>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var pipeline = QualitySettings.renderPipeline;
            var cameraRoot = new GameObject("Fauna evidence camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraRoot.AddComponent<UnityEngine.Camera>();
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                var binding = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.SceneEnvironmentRendering>(true)).Single();
                QualitySettings.renderPipeline = (RenderPipelineAsset)new SerializedObject(binding).FindProperty("_renderPipeline").objectReferenceValue;
                typeof(AmbientFaunaGroup).GetField("_habitat", flags).SetValue(group, host);
                typeof(AmbientFaunaHabitat).GetField("_hour", flags).SetValue(host, hour);
                if (!group.TryPrepare(out string error) || !host.TryRegister(group)) throw new InvalidOperationException(error);

                camera.fieldOfView = 48;
                Vector3 center = group.WorldBounds.center;
                camera.transform.position = center + new Vector3(0, 0.7f, -3.5f);
                camera.transform.LookAt(center);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Directory.CreateDirectory(CompletionEvidence);
                File.WriteAllBytes(CompletionEvidence + "/world-" + groupName + "-off.png", image.EncodeToPNG());
                for (int frame = 0; frame < 30; frame++)
                {
                    typeof(AmbientFaunaGroup).GetField("_lastFrame", flags).SetValue(group, -1);
                    group.ApplyWorldInputs(0.1f, center + Vector3.back * 10, true, new Vector3(0.3f, 0, 0.1f));
                }
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                string path = CompletionEvidence + "/world-" + groupName + "-on.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                File.WriteAllText(CompletionEvidence + "/world-" + groupName + ".json", JsonUtility.ToJson(new WorldCaptureRecord {
                    Group = groupName, Hour = hour, Capacity = group.AllocatedCapacity,
                    Position = center, CameraPosition = camera.transform.position,
                    Mode = "Editor RenderGraph / fauna hour and wind injected / scene lighting unchanged" }, true));
                return path;
            }
            finally
            {
                group.SetSuspended(true); host.Unregister(group);
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
                QualitySettings.renderPipeline = pipeline;
            }
        }

        [Serializable] private sealed class WorldCaptureRecord
        {
            public string Group, Mode;
            public float Hour;
            public int Capacity;
            public Vector3 Position, CameraPosition;
        }
    }
}
