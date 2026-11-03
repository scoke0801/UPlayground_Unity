var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("연속 월드 Edit Mode가 필요합니다.");
string folder = "output/OpenWorld/DungeonArtReview/captures";
System.IO.Directory.CreateDirectory(folder);
var terrain = GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
var utility = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t => t != null);
var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var mode = pipeline.GetType().GetProperty("gpuResidentDrawerMode");
var previousMode = mode.GetValue(pipeline, null);
var go = new GameObject("DungeonArtCapture") { hideFlags = HideFlags.HideAndDontSave };
var camera = go.AddComponent<Camera>();
camera.enabled = false;
camera.scene = scene;
camera.useOcclusionCulling = false;
camera.fieldOfView = 62;
camera.farClipPlane = 3000;
camera.nearClipPlane = .1f;
string[] names = { "quarry", "chain", "hall", "crypt", "fort", "waystation", "ring" };
float[,] centers = { { 542,1090 }, { 590,860 }, { 330,550 }, { 1525,1075 }, { 1845,615 }, { 2025,175 }, { 1900,910 } };
float[,] views = { { 575,1050 }, { 590,822 }, { 330,510 }, { 1590,1060 }, { 1845,565 }, { 2018,120 }, { 1850,912 } };
try
{
    mode.SetValue(pipeline, Enum.Parse(mode.PropertyType, "Disabled"), null);
    for (int i = 0; i < names.Length; i++)
    {
        var center = new Vector3(centers[i,0], 0, centers[i,1]);
        center.y = terrain.SampleHeight(center) + terrain.transform.position.y;
        camera.transform.SetPositionAndRotation(center + new Vector3(55,95,-100), Quaternion.LookRotation(new Vector3(-55,-95,100)));
        var texture = (Texture2D)utility.GetMethod("Render").Invoke(null, new object[] { camera, new Vector2Int(1280,900), true, 1000f });
        try { System.IO.File.WriteAllBytes(folder + "/" + names[i] + "-overview.png", texture.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        var p = new Vector3(views[i,0], 0, views[i,1]);
        p.y = terrain.SampleHeight(p) + terrain.transform.position.y + 3;
        camera.transform.SetPositionAndRotation(p, Quaternion.LookRotation(center + Vector3.up * 3 - p));
        texture = (Texture2D)utility.GetMethod("Render").Invoke(null, new object[] { camera, new Vector2Int(1280,800), false, 1000f });
        try { System.IO.File.WriteAllBytes(folder + "/" + names[i] + "-entry.png", texture.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }
    camera.orthographic = true;
    camera.orthographicSize = 1200;
    camera.transform.SetPositionAndRotation(new Vector3(1230,2400,610), Quaternion.Euler(90,0,0));
    var map = (Texture2D)utility.GetMethod("Render").Invoke(null, new object[] { camera, new Vector2Int(720,834), true, 1000f });
    try { System.IO.File.WriteAllBytes(folder + "/world.png", map.EncodeToPNG()); }
    finally { UnityEngine.Object.DestroyImmediate(map); }
}
finally
{
    UnityEngine.Object.DestroyImmediate(go);
    mode.SetValue(pipeline, previousMode, null);
}
return folder;
