// 다른 열린 씬을 저장하지 않고 RiverBasinWorld의 날씨 연결만 추가한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling)
    throw new System.InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
string path = "Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity";
string backup = "output/WeatherSky/backup/RiverBasinWorld.unity";
System.IO.Directory.CreateDirectory("output/WeatherSky/backup");
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened = !scene.IsValid() || !scene.isLoaded;
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
try
{
    if (scene.isDirty) throw new System.InvalidOperationException("대상 씬에 미저장 변경이 있습니다.");
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(path, backup);
    var roots = scene.GetRootGameObjects();
    var skies = roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.SceneEnvironmentRendering>(true)).ToArray();
    if (skies.Length != 1) throw new System.InvalidOperationException("하늘 작성자는 하나여야 합니다.");
    if (roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true)).Any())
        return "기존 환경 연결 보존";
    var sky = skies[0];
    var skyData = new UnityEditor.SerializedObject(sky);
    var material = skyData.FindProperty("_skySource").objectReferenceValue as Material;
    if (material == null) throw new System.InvalidOperationException("하늘 재질 누락");
    string profilePath = "Assets/10.Datas/World/Sky/Sky_RiverBasin.asset";
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>(profilePath);
    if (profile == null)
    {
        profile = ScriptableObject.CreateInstance<UPlayGround.Data.World.SkyProfileSO>();
        System.Func<Color, Color, Gradient> palette = (day, night) =>
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(night, 0), new GradientColorKey(night, 0.2f),
                new GradientColorKey(Color.Lerp(day, new Color(0.6f, 0.3f, 0.3f), 0.35f), 0.25f),
                new GradientColorKey(day, 0.33f), new GradientColorKey(day, 0.67f),
                new GradientColorKey(Color.Lerp(day, new Color(0.6f, 0.3f, 0.3f), 0.35f), 0.75f),
                new GradientColorKey(night, 0.84f), new GradientColorKey(night, 1)
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            return gradient;
        };
        profile.zenith = palette(material.GetColor("_ZenithColor"), new Color(0.008f, 0.018f, 0.055f));
        profile.middle = palette(material.GetColor("_MiddleColor"), new Color(0.025f, 0.04f, 0.085f));
        profile.horizon = palette(material.GetColor("_HorizonColor"), new Color(0.055f, 0.08f, 0.14f));
        profile.cloudLight = palette(material.GetColor("_CloudLightColor"), new Color(0.065f, 0.085f, 0.14f));
        profile.cloudShadow = palette(material.GetColor("_CloudShadowColor"), new Color(0.012f, 0.02f, 0.045f));
        profile.fog = palette(RenderSettings.fogColor, new Color(0.03f, 0.04f, 0.075f));
        profile.cloudCoverage = material.GetFloat("_CloudCoverage");
        profile.overcastCloudCoverage = 0.8f;
        profile.cloudSoftness = material.GetFloat("_CloudSoftness");
        profile.cloudHeight = material.GetFloat("_CloudHeight");
        profile.cloudScale = material.GetFloat("_CloudScale");
        profile.highCloudCoverage = material.GetFloat("_HighCloudCoverage");
        profile.cloudThickness = material.GetFloat("_CloudThickness");
        profile.cloudDetail = material.GetFloat("_CloudDetail");
        profile.sphericalProjection = material.GetFloat("_SphericalProjection");
        profile.cloudFlowMultiplier = 1;
        UnityEditor.AssetDatabase.CreateAsset(profile, profilePath);
    }
    var root = new GameObject("RiverBasin Weather");
    root.SetActive(false);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
    var environment = root.AddComponent<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/LivingWorld/Environment/EnvironmentEffects.prefab");
    var effectsObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    effectsObject.transform.SetParent(root.transform, false);
    var effects = effectsObject.GetComponent<UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects>();
    var effectData = new UnityEditor.SerializedObject(effects);
    effectData.FindProperty("_roofLayers").intValue = LayerMask.GetMask("Default", "Ground");
    effectData.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(effects);
    var data = new UnityEditor.SerializedObject(environment);
    data.FindProperty("_mapId").stringValue = "RiverBasinWorld";
    data.FindProperty("_profile").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/10.Datas/World/LivingWorld/Environment/RiverBasin_Weather_v1.json");
    data.FindProperty("_effects").objectReferenceValue = effects;
    data.FindProperty("_rainAnchor").objectReferenceValue = ((ParticleSystem)effectData.FindProperty("_rain").objectReferenceValue).transform;
    data.ApplyModifiedPropertiesWithoutUndo();
    skyData.FindProperty("_environment").objectReferenceValue = environment;
    skyData.FindProperty("_defaultSkyProfile").objectReferenceValue = profile;
    // 기존 원경 안개는 지역 하늘 연결과 별개로 보존한다.
    skyData.FindProperty("_applyRegionalFog").boolValue = false;
    skyData.ApplyModifiedPropertiesWithoutUndo();
    var observer = root.AddComponent<UPlayGround.Gameplay.World.LivingWorld.EnvironmentSceneBinding>();
    var observerData = new UnityEditor.SerializedObject(observer);
    observerData.FindProperty("_environment").objectReferenceValue = environment;
    observerData.FindProperty("_skyRendering").objectReferenceValue = sky;
    observerData.ApplyModifiedPropertiesWithoutUndo();
    var binding = sky.gameObject.GetComponent<UPlayGround.Gameplay.World.WorldLightingSceneBinding>();
    if (binding == null) binding = sky.gameObject.AddComponent<UPlayGround.Gameplay.World.WorldLightingSceneBinding>();
    binding.environment = environment;
    binding.skyRendering = sky;
    binding.sunLight = skyData.FindProperty("_sunLight").objectReferenceValue as Light;
    var moonObject = new GameObject("RiverBasin Moon");
    moonObject.transform.SetParent(root.transform, false);
    var moon = moonObject.AddComponent<Light>();
    moon.type = LightType.Directional;
    moon.intensity = 0;
    moon.enabled = false;
    binding.moonLight = moon;
    root.SetActive(true);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
    return new { scene = path, profile = profilePath, effects = effects.name };
}
finally
{
    if (previous.IsValid() && previous.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
