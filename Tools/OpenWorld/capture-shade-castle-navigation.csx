var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("월드 Edit Mode 필요");
string folder = "output/OpenWorld/ShadeCastle/captures";
System.IO.Directory.CreateDirectory(folder);
var utility = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t => t != null);
var terrain = GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var mode = pipeline.GetType().GetProperty("gpuResidentDrawerMode");
var previousMode = mode.GetValue(pipeline, null);
var go = new GameObject("ShadeCastleCapture") { hideFlags = HideFlags.HideAndDontSave };
var camera = go.AddComponent<Camera>();
camera.enabled = false;
camera.scene = scene;
camera.farClipPlane = 1500;
camera.nearClipPlane = .1f;
camera.fieldOfView = 70;
try
{
    mode.SetValue(pipeline, Enum.Parse(mode.PropertyType, "Disabled"), null);
    var positions = new[] { new Vector3(1540, 0, 548), new Vector3(1540, 0, 516), new Vector3(1410, 0, 550), new Vector3(1405, 0, 440) };
    var targets = new[] { new Vector3(1540, 64, 520), new Vector3(1540, 64, 540), new Vector3(1540, 64, 550), new Vector3(1405, 64, 540) };
    var names = new[] { "north-gate-outside", "north-gate-inside", "northwest-perimeter", "west-perimeter" };
    for (int i = 0; i < positions.Length; i++)
    {
        var p = positions[i];
        p.y = terrain.SampleHeight(p) + terrain.transform.position.y + 1.7f;
        camera.transform.SetPositionAndRotation(p, Quaternion.LookRotation(targets[i] - p));
        var texture = (Texture2D)utility.GetMethod("Render").Invoke(null, new object[] { camera, new Vector2Int(1280, 800), false, 1000f });
        try { System.IO.File.WriteAllBytes(folder + "/" + names[i] + ".png", texture.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }
}
finally
{
    UnityEngine.Object.DestroyImmediate(go);
    mode.SetValue(pipeline, previousMode, null);
}
return folder;
