// 실제 지형을 높이 기준으로 함께 렌더한다. 원본 씬과 설정은 저장하지 않는다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode에서 촬영합니다.");
string path = "Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity";
var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened = !scene.IsValid() || !scene.isLoaded;
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
var previousPipeline = QualitySettings.renderPipeline;
GameObject go = null;
Material material = null;
RenderTexture target = null;
var previousTarget = RenderTexture.active;
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    QualitySettings.renderPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinPipeline.asset");
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>("Assets/10.Datas/World/Sky/Sky_RiverBasin.asset");
    var sample = profile.Evaluate(0.5f);
    material = new Material(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinSky.mat"));
    material.SetFloat("_UseRegionalPalette", 1);
    material.SetColor("_ZenithColor", sample.Zenith);
    material.SetColor("_MiddleColor", sample.Middle);
    material.SetColor("_HorizonColor", sample.Horizon);
    material.SetColor("_CloudLightColor", sample.CloudLight);
    material.SetColor("_CloudShadowColor", sample.CloudShadow);
    material.SetFloat("_CloudCoverage", sample.Coverage);
    material.SetFloat("_CloudHeight", sample.Height);
    material.SetFloat("_CloudFormationScale", sample.FormationScale);
    material.SetFloat("_CloudEdgeWidth", sample.EdgeWidth);
    material.SetFloat("_UseCloudClock", 1);
    material.SetFloat("_CloudClock", 480);
    var sun = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>(true))
        .FirstOrDefault(l => l.type == LightType.Directional && l.enabled);
    if (sun != null)
    {
        Vector3 direction = -sun.transform.forward;
        material.SetVector("_SunDirection", new Vector4(direction.x, direction.y, direction.z, 1));
    }
    go = new GameObject("구름 높이 검수", typeof(Camera), typeof(Skybox)) { hideFlags = HideFlags.HideAndDontSave };
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    var camera = go.GetComponent<Camera>();
    camera.enabled = false;
    camera.scene = scene;
    camera.farClipPlane = 1800;
    camera.nearClipPlane = 0.1f;
    camera.fieldOfView = 65;
    camera.clearFlags = CameraClearFlags.Skybox;
    camera.transform.position = new Vector3(307, 73, -135);
    camera.transform.rotation = Quaternion.Euler(-8, 30, 0);
    go.GetComponent<Skybox>().material = material;
    var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    data.renderPostProcessing = false;
    target = new RenderTexture(1280, 720, 24);
    camera.targetTexture = target;
    System.Action<string> capture = name =>
    {
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes("output/WeatherSky/CloudHeight/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    };
    foreach (bool after in new[] { false, true })
    {
        material.SetFloat("_CloudFormationEnabled", after ? 1 : 0);
        material.SetFloat("_CloudDepth", after ? sample.Depth : 1600);
        material.SetFloat("_CloudRaySteps", after ? 96 : 48);
        capture("world-" + (after ? "after" : "before"));
    }
    camera.transform.rotation = Quaternion.Euler(-8, 120, 0);
    capture("world-side");
    camera.transform.rotation = Quaternion.Euler(-30, 30, 0);
    capture("world-upper");
    return new { scene = path, position = camera.transform.position.ToString(), mode = "고정 Editor 렌더, Play Mode 아님" };
}
finally
{
    RenderTexture.active = previousTarget;
    if (go != null) UnityEngine.Object.DestroyImmediate(go);
    if (material != null) UnityEngine.Object.DestroyImmediate(material);
    if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
    QualitySettings.renderPipeline = previousPipeline;
    if (previousScene.IsValid() && previousScene.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
    if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
