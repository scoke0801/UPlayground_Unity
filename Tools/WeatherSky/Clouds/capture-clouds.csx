// STAGE 토큰을 before/after로 바꿔 같은 구도의 하늘을 렌더한다. 씬 저장 없음.
string output = "output/WeatherSky/Clouds/STAGE";
System.IO.Directory.CreateDirectory(output);
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go = new GameObject("CloudCapture", typeof(Camera), typeof(Skybox));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
var camera = go.GetComponent<Camera>();
camera.scene = preview;
camera.clearFlags = CameraClearFlags.Skybox;
camera.cullingMask = 0;
camera.fieldOfView = 65;
camera.transform.position = new Vector3(0, 5, 0);
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinSky.mat");
var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>("Assets/10.Datas/World/Sky/Sky_RiverBasin.asset");
var material = new Material(source);
var target = new RenderTexture(1280, 720, 24);
var previous = RenderTexture.active;
try
{
    go.GetComponent<Skybox>().material = material;
    camera.targetTexture = target;
    material.SetFloat("_UseRegionalPalette", 1);
    material.SetFloat("_UseCloudClock", 1);
    material.SetFloat("_CloudClock", 480);
    material.SetVector("_CelestialRotation", new Vector4(0, 1, 0, 0));
    foreach (string name in new[] { "day", "away", "horizon", "zenith", "sunset", "night", "storm" })
    {
        var sample = profile.Evaluate(name == "sunset" ? 0.25f : name == "night" ? 0 : 0.5f);
        material.SetColor("_ZenithColor", sample.Zenith);
        material.SetColor("_MiddleColor", sample.Middle);
        material.SetColor("_HorizonColor", sample.Horizon);
        material.SetColor("_GroundColor", sample.Fog);
        material.SetColor("_CloudLightColor", name == "storm" ? Color.Lerp(sample.CloudLight, sample.CloudShadow, 0.5f) : sample.CloudLight);
        material.SetColor("_CloudShadowColor", sample.CloudShadow);
        material.SetFloat("_CloudCoverage", name == "storm" ? sample.OvercastCoverage : sample.Coverage);
        material.SetFloat("_CloudSoftness", sample.Softness);
        material.SetFloat("_CloudHeight", sample.Height);
        material.SetFloat("_CloudScale", sample.Scale);
        material.SetFloat("_CloudThickness", name == "storm" ? 1 : sample.Thickness);
        material.SetFloat("_CloudDetail", sample.Detail);
        material.SetFloat("_HighCloudCoverage", sample.HighCoverage);
        material.SetFloat("_PlanetRadius", sample.Radius);
        material.SetFloat("_SphericalProjection", sample.Projection);
        if (material.HasProperty("_CloudDepth"))
        {
            material.SetFloat("_CloudDepth", sample.Depth);
            material.SetFloat("_CloudDensity", sample.Density);
            material.SetFloat("_CloudErosion", sample.Erosion);
        }
        Vector3 sun = new Vector3(0.5f, name == "sunset" ? 0.06f : 0.65f, 0.57f).normalized;
        if (name == "night") sun = -sun;
        material.SetVector("_SunDirection", new Vector4(sun.x, sun.y, sun.z, 1));
        material.SetVector("_Lightning", Vector4.zero);
        float pitch = name == "horizon" ? -3 : name == "zenith" ? -75 : -18;
        camera.transform.rotation = Quaternion.Euler(pitch, name == "away" ? 210 : 30, 0);
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }
    return UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Select(m => m.message).ToArray();
}
finally
{
    RenderTexture.active = previous;
    camera.targetTexture = null;
    target.Release();
    UnityEngine.Object.DestroyImmediate(target);
    UnityEngine.Object.DestroyImmediate(material);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
