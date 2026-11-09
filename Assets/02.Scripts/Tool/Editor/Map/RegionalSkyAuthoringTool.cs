using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>기존 씬을 저장하지 않고 하늘 프리셋과 재사용 가능한 비교 리그를 생성한다.</summary>
    public static class RegionalSkyAuthoringTool
    {
        private const string Root = "Assets/10.Datas/World/Sky/";
        public const string RigPath = Root + "RegionalSky_Review.prefab";

        /// <summary>기존 저작 에셋을 보존하고 누락된 두 프로필과 비교 리그만 생성한다.</summary>
        [UPlaygroundTool("World/지역 하늘 프리셋 생성")]
        public static void CreatePresets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("지역 하늘 프리셋은 Edit Mode에서 생성해야 합니다.");
            DaySkyAuthoringTool.CreatePreset();
            var clear = CreateProfile("Sky_LakeClear", false);
            var coastal = CreateProfile("Sky_CoastalHaze", true);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RigPath) != null) return;
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root = new GameObject("RegionalSky_Review");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var rendering = root.AddComponent<SceneEnvironmentRendering>();
                var regionObject = new GameObject("CoastalHaze_Region");
                regionObject.transform.SetParent(root.transform, false);
                regionObject.transform.localPosition = new Vector3(300, 0, 0);
                var region = regionObject.AddComponent<SkyRegionBinding>();
                region.profile = coastal;
                region.size = new Vector3(200, 2000, 400);
                region.blendDistance = 100;
                var data = new SerializedObject(rendering);
                var reflectionObject = new GameObject("SkyReflection");
                reflectionObject.transform.SetParent(root.transform, false);
                var reflection = reflectionObject.AddComponent<ReflectionProbe>();
                reflection.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                reflection.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                reflection.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                reflection.resolution = 64;
                reflection.cullingMask = 0;
                reflection.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
                reflection.size = new Vector3(2000, 2000, 2000);
                data.FindProperty("_skyReflectionProbe").objectReferenceValue = reflection;
                data.FindProperty("_skySource").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DaySkyAuthoringTool.MaterialPath);
                data.FindProperty("_defaultSkyProfile").objectReferenceValue = clear;
                data.FindProperty("_skyRegions").arraySize = 1;
                data.FindProperty("_skyRegions").GetArrayElementAtIndex(0).objectReferenceValue = region;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static SkyProfileSO CreateProfile(string name, bool coastal)
        {
            string path = Root + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<SkyProfileSO>(path);
            if (existing != null) return existing;
            var profile = ScriptableObject.CreateInstance<SkyProfileSO>();
            profile.name = name;
            profile.zenith = CreatePalette(coastal ? new Color(0.15f, 0.32f, 0.53f) : new Color(0.10f, 0.35f, 0.70f), new Color(0.30f, 0.23f, 0.40f));
            profile.middle = CreatePalette(coastal ? new Color(0.43f, 0.60f, 0.71f) : new Color(0.32f, 0.63f, 0.88f), new Color(0.72f, 0.41f, 0.38f));
            profile.horizon = CreatePalette(coastal ? new Color(0.69f, 0.77f, 0.78f) : new Color(0.80f, 0.88f, 0.90f), new Color(0.95f, 0.60f, 0.37f));
            profile.fog = CreatePalette(coastal ? new Color(0.57f, 0.68f, 0.72f) : new Color(0.70f, 0.81f, 0.86f), new Color(0.65f, 0.43f, 0.38f));
            profile.cloudLight = CreatePalette(coastal ? new Color(0.84f, 0.88f, 0.87f) : new Color(1, 0.97f, 0.89f), new Color(1, 0.65f, 0.42f));
            profile.cloudShadow = CreatePalette(coastal ? new Color(0.36f, 0.47f, 0.56f) : new Color(0.50f, 0.65f, 0.78f), new Color(0.35f, 0.26f, 0.40f));
            profile.cloudCoverage = coastal ? 0.36f : 0.30f;
            profile.overcastCloudCoverage = coastal ? 0.64f : 0.58f;
            profile.cloudSoftness = coastal ? 0.12f : 0.07f;
            profile.cloudHeight = coastal ? 1300 : 2100;
            profile.cloudScale = coastal ? 6500 : 5000;
            profile.highCloudCoverage = coastal ? 0.4f : 0.12f;
            profile.cloudThickness = coastal ? 0.8f : 0.5f;
            profile.haloStrength = coastal ? 0.08f : 0.16f;
            profile.lightMultiplier = coastal ? 0.9f : 1;
            profile.fogDensity = coastal ? 0.002f : 0.0007f;
            profile.fogStart = coastal ? 50 : 100;
            profile.fogEnd = coastal ? 850 : 1600;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssetIfDirty(profile);
            return profile;
        }

        private static Gradient CreatePalette(Color day, Color twilight)
        {
            var night = new Color(day.r * 0.08f, day.g * 0.10f, day.b * 0.18f, 1);
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(night, 0), new GradientColorKey(night, 0.19f),
                new GradientColorKey(twilight, 0.24f), new GradientColorKey(day, 0.32f),
                new GradientColorKey(day, 0.68f), new GradientColorKey(twilight, 0.77f),
                new GradientColorKey(night, 0.84f), new GradientColorKey(night, 1)
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            return gradient;
        }
    }
}
