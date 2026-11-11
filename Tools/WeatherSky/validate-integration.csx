// 실제 Unity 로드 결과를 검사하며 대상 씬과 에셋은 저장하지 않는다.
string path = "Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened = !scene.IsValid() || !scene.isLoaded;
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
try
{
    var roots = scene.GetRootGameObjects();
    var environments = roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true)).ToArray();
    var skies = roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.SceneEnvironmentRendering>(true)).ToArray();
    if (environments.Length != 1 || skies.Length != 1) throw new System.InvalidOperationException("환경·하늘 작성자 개수 오류");
    var environment = environments[0];
    var data = new UnityEditor.SerializedObject(environment);
    var effects = data.FindProperty("_effects").objectReferenceValue as UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects;
    if (effects == null || data.FindProperty("_rainAnchor").objectReferenceValue == null)
        throw new System.InvalidOperationException("날씨 프리팹 연결 누락");
    var effectData = new UnityEditor.SerializedObject(effects);
    foreach (string field in new[] { "_snow", "_lightningBolt", "_lightningBranch", "_lightningLight", "_thunderAudio" })
        if (effectData.FindProperty(field).objectReferenceValue == null) throw new System.InvalidOperationException(field);
    if (effectData.FindProperty("_roofLayers").intValue == 0) throw new System.InvalidOperationException("지붕 충돌층 누락");
    var skyData = new UnityEditor.SerializedObject(skies[0]);
    if (skyData.FindProperty("_environment").objectReferenceValue != environment
        || skyData.FindProperty("_defaultSkyProfile").objectReferenceValue == null)
        throw new System.InvalidOperationException("지역 하늘 연결 누락");
    var binding = skies[0].GetComponent<UPlayGround.Gameplay.World.WorldLightingSceneBinding>();
    if (binding == null || binding.environment != environment || binding.moonLight == null)
        throw new System.InvalidOperationException("월드 조명 연결 누락");
    var text = data.FindProperty("_profile").objectReferenceValue as TextAsset;
    var schedule = JsonUtility.FromJson<UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule>(text.text);
    UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule.Sample sample;
    if (!schedule.TryEvaluate(690, out sample) || sample.Storm <= 0)
        throw new System.InvalidOperationException("본편 뇌우 시간표 오류");
    int missing = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
        .Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    var shaderErrors = new System.Collections.Generic.List<string>();
    foreach (string shaderName in new[] { "UPlayGround/Environment/Day Sky", "UPlayGround/Environment/Lightning Bolt", "UPlayGround/Environment/Weather Particle" })
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new System.InvalidOperationException(shaderName);
        shaderErrors.AddRange(UnityEditor.ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m => m.message));
    }
    var report = new { scene = path, missingScripts = missing, shaderErrors = shaderErrors.ToArray(),
        rain = sample.Rain, storm = sample.Storm, profile = text.name,
        previewRegistered = UPlayGround.EditorTools.UPlaygroundToolCatalog.Contains("World/날씨 미리보기"),
        mode = "EditMode 실제 에셋 로드·연결 검사; Play Mode 아님" };
    System.IO.Directory.CreateDirectory("output/WeatherSky");
    string details = "scene=" + path + "\nmissingScripts=" + missing + "\nshaderErrors=" + shaderErrors.Count
        + "\nrain=" + sample.Rain + "\nstorm=" + sample.Storm + "\nmode=EditMode 실제 에셋 로드·연결 검사; Play Mode 아님";
    System.IO.File.WriteAllText("output/WeatherSky/integration-validation.txt", details);
    if (missing != 0 || shaderErrors.Count > 0) throw new System.InvalidOperationException(details);
    return report;
}
finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
