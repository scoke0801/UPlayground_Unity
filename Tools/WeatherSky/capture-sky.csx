// 활성 씬을 바꾸지 않는 고정 하늘 렌더. 실제 플레이 품질 판정과 구분한다.
string output = "output/WeatherSky";
System.IO.Directory.CreateDirectory(output);
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go = new GameObject("SkyWeatherCapture", typeof(Camera), typeof(Skybox));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
var camera = go.GetComponent<Camera>();
camera.scene = preview;
camera.clearFlags = CameraClearFlags.Skybox;
camera.cullingMask = 0;
camera.fieldOfView = 65;
camera.transform.rotation = Quaternion.Euler(-24, 30, 0);
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinSky.mat");
var material = new Material(source);
var target = new RenderTexture(1280, 720, 24);
var previous = RenderTexture.active;
try
{
    go.GetComponent<Skybox>().material = material;
    camera.targetTexture = target;
    material.SetFloat("_UseRegionalPalette", 0);
    material.SetFloat("_UseCloudClock", 1);
    material.SetFloat("_CloudClock", 100);
    material.SetVector("_CelestialRotation", new Vector4(0, 1, 0, 0));
    foreach (string name in new[] { "day", "night-clear", "night-clouds", "night-lightning" })
    {
        bool day = name == "day";
        Vector3 sun = new Vector3(0.4f, 0.6f, 0.69f).normalized * (day ? 1 : -1);
        material.SetVector("_SunDirection", new Vector4(sun.x, sun.y, sun.z, 1));
        material.SetFloat("_CloudCoverage", name == "night-clear" ? 0 : day ? 0.36f : 0.8f);
        material.SetFloat("_HighCloudCoverage", name == "night-clear" ? 0 : 0.14f);
        material.SetVector("_Lightning", new Vector4(0.4f, 0.6f, 0.69f, name == "night-lightning" ? 1.5f : 0));
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/LivingWorld/Environment/EnvironmentEffects.prefab");
    var effectsRoot = UnityEngine.Object.Instantiate(prefab);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effectsRoot, preview);
    var effects = effectsRoot.GetComponent<UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects>();
    effects.BindShelters(System.Array.Empty<BoxCollider>(), 1);
    effects.Apply(new UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule.Sample { Snow = 1 }, Vector3.zero, true, false);
    var data = new UnityEditor.SerializedObject(effects);
    var snow = (ParticleSystem)data.FindProperty("_snow").objectReferenceValue;
    snow.Simulate(7, false, true, true);
    snow.Pause(false);
    camera.cullingMask = -1;
    camera.transform.rotation = Quaternion.Euler(-8, 30, 0);
    material.SetVector("_Lightning", Vector4.zero);
    System.Action<string> capture = name =>
    {
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(output + "/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    };
    capture("snow");
    effects.StopEffects();
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    effects.GetType().GetMethod("BeginStrike", flags).Invoke(effects, new object[] { Vector3.zero, 1f });
    var bolt = (LineRenderer)data.FindProperty("_lightningBolt").objectReferenceValue;
    var branch = (LineRenderer)data.FindProperty("_lightningBranch").objectReferenceValue;
    bolt.enabled = true; branch.enabled = true;
    bolt.startColor = bolt.endColor = branch.startColor = branch.endColor = new Color(0.65f, 0.78f, 1, 1);
    camera.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(bolt.GetPosition(0), bolt.GetPosition(24), 0.5f));
    Vector3 strike = effects.LightningDirection;
    material.SetVector("_Lightning", new Vector4(strike.x, strike.y, strike.z, 1.2f));
    capture("bolt");
    effects.StopEffects();
    UnityEngine.Object.DestroyImmediate(effectsRoot);
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
