using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>낮 하늘 프리셋을 생성하고 현재 씬에 되돌릴 수 있는 형태로 연결한다.</summary>
    public static class DaySkyAuthoringTool
    {
        public const string MaterialPath = "Assets/10.Datas/World/Sky/DaySky_Clear.mat";
        private const string TexturePath = "Assets/10.Datas/World/Sky/DaySky_CloudShape.png";
        private const string ShaderPath = "Assets/06.Shaders/DaySky.shader";
        private const int TextureSize = 512;

        /// <summary>기존 저작값을 유지하고 없는 구름 텍스처와 낮 프리셋만 생성한다.</summary>
        [UPlaygroundTool("World/낮 하늘 프리셋 생성")]
        public static void CreatePreset()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서 생성해야 합니다.");

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("낮 하늘 셰이더의 임포트와 컴파일을 먼저 확인해야 합니다.");

            if (!AssetDatabase.IsValidFolder("Assets/10.Datas/World/Sky"))
                AssetDatabase.CreateFolder("Assets/10.Datas/World", "Sky");
            if (!File.Exists(TexturePath))
                CreateCloudTexture();

            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
                return;

            var material = new Material(shader) { name = "DaySky_Clear" };
            material.SetTexture("_CloudMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        /// <summary>현재 씬의 태양에 하늘 복원 바인딩을 연결하며 씬 저장은 호출자가 결정한다.</summary>
        [UPlaygroundTool("World/현재 씬 낮 하늘 적용")]
        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서 적용해야 합니다.");
            Light sun = RenderSettings.sun;
            if (sun == null || sun.type != LightType.Directional || sun.gameObject.scene != SceneManager.GetActiveScene())
                throw new InvalidOperationException("현재 씬의 방향광을 RenderSettings.sun에 지정해야 합니다.");

            CreatePreset();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("낮 하늘 적용");
            try
            {
                var binding = sun.GetComponent<SceneEnvironmentRendering>();
                if (binding == null)
                    binding = Undo.AddComponent<SceneEnvironmentRendering>(sun.gameObject);
                Undo.RecordObject(binding, "낮 하늘 연결");
                var serialized = new SerializedObject(binding);
                serialized.FindProperty("_skySource").objectReferenceValue = material;
                serialized.FindProperty("_sunLight").objectReferenceValue = sun;
                serialized.ApplyModifiedProperties();
                // 다른 로드된 씬까지 미저장 상태로 만들지 않도록 현재 씬 설정만 기록한다.
                Undo.RecordObject(Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings"), "씬 하늘 교체");
                RenderSettings.skybox = material;
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void CreateCloudTexture()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true);
            try
            {
                var colors = new Color32[TextureSize * TextureSize];
                for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / TextureSize, v = (float)y / TextureSize;
                    float shape = SampleCloud(u, v, 17);
                    float detail = SampleCloud(u, v, 83);
                    colors[y * TextureSize + x] = new Color(shape, detail, 0f, 1f);
                }
                texture.SetPixels32(colors);
                texture.Apply();
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static float SampleCloud(float u, float v, int seed)
        {
            float value = 0f, weight = 0.57f, total = 0f;
            for (int octave = 0; octave < 4; octave++)
            {
                value += SamplePeriodicNoise(u, v, 4 << octave, seed + octave * 31) * weight;
                total += weight;
                weight *= 0.5f;
            }
            return Mathf.Clamp01(0.5f + (value / total - 0.5f) * 1.6f);
        }

        private static float SamplePeriodicNoise(float u, float v, int period, int seed)
        {
            float px = u * period, py = v * period;
            int x = Mathf.FloorToInt(px), y = Mathf.FloorToInt(py);
            float fx = Mathf.SmoothStep(0f, 1f, px - x), fy = Mathf.SmoothStep(0f, 1f, py - y);
            float a = Mathf.Lerp(Hash(x % period, y % period, seed), Hash((x + 1) % period, y % period, seed), fx);
            float b = Mathf.Lerp(Hash(x % period, (y + 1) % period, seed), Hash((x + 1) % period, (y + 1) % period, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint hash = (uint)(x * 374761393 + y * 668265263 + seed * 144269);
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                return (hash ^ (hash >> 16)) / (float)uint.MaxValue;
            }
        }
    }
}
