using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>원본 배치의 NPC 외형을 저장 없이 촬영하여 말투 감사 근거로 남긴다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P00 외형 조사")]
        public static void CaptureBaselineCast()
        {
            RequireEditMode();
            for (int index = 0; index < UnityEngine.SceneManagement.SceneManager.sceneCount; index++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 외형 조사를 중단합니다.");
            Directory.CreateDirectory(RemovalDirectory + "/Cast");
            var scene = EditorSceneManager.OpenScene(SourceScenePath);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (NpcActor actor in root.GetComponentsInChildren<NpcActor>(true))
                        CaptureBaselineActor(actor);
            }
            finally { EditorSceneManager.OpenScene(ReadLayout().scenePath); }
        }

        private static void CaptureBaselineActor(NpcActor actor)
        {
            // 원본의 비활성 주민도 외형 감사에 포함하며 씬을 저장하지 않는다.
            for (Transform parent = actor.transform; parent != null; parent = parent.parent)
                parent.gameObject.SetActive(true);
            Renderer[] renderers = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0) return;
            Transform[] nodes = actor.GetComponentsInChildren<Transform>(true);
            int[] layers = new int[nodes.Length];
            for (int index = 0; index < nodes.Length; index++)
            {
                layers[index] = nodes[index].gameObject.layer;
                nodes[index].gameObject.layer = 31;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var node = new GameObject("P00CastCamera");
            Camera camera = node.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.18f, 0.2f, 0.24f);
            camera.fieldOfView = 35;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30;
            Vector3 targetPoint = bounds.center;
            float distance = Mathf.Max(2, bounds.size.y * 1.9f);
            camera.transform.position = targetPoint + actor.transform.forward * distance + Vector3.up * 0.2f;
            camera.transform.LookAt(targetPoint);
            var lightNode = new GameObject("P00CastLight");
            Light light = lightNode.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = distance * 3;
            light.intensity = 10;
            light.transform.position = camera.transform.position;
            RenderTexture target = RenderTexture.GetTemporary(512, 640, 24);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(512, 640, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 512, 640), 0, 0);
                texture.Apply();
                File.WriteAllBytes(RemovalDirectory + "/Cast/" + actor.name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                for (int index = 0; index < nodes.Length; index++) nodes[index].gameObject.layer = layers[index];
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(lightNode);
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
