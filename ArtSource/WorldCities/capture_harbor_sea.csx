var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.name != "SaltwindHarbor" || Application.isPlaying) throw new System.Exception("항구 Edit Mode 필요");
var water = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "CoastalWater");
string stage = water.localScale.x > 100 ? "After" : "Before";
string folder = "output/HarborSea/" + stage;
System.IO.Directory.CreateDirectory(folder);
var utility = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t => t != null);
var go = new GameObject("HarborSeaReviewCamera"); go.hideFlags = HideFlags.HideAndDontSave;
var camera = go.AddComponent<Camera>(); camera.enabled = false; camera.fieldOfView = 65;
camera.farClipPlane = 1000; camera.clearFlags = CameraClearFlags.Skybox;
try {
    var positions = new[] { new Vector3(180,10,250),new Vector3(235,13,300),new Vector3(180,80,200),new Vector3(180,10,250) };
    var targets = new[] { new Vector3(180,10,900),new Vector3(280,5,340),new Vector3(180,5,660),new Vector3(700,10,700) };
    var names = new[] { "horizon","shore","overview","diagonal" };
    for(int i=0;i<positions.Length;i++) {
        camera.transform.position=positions[i]; camera.transform.LookAt(targets[i]);
        var image=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,720),true,1000f});
        System.IO.File.WriteAllBytes(folder+"/"+names[i]+".png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
    }
} finally { UnityEngine.Object.DestroyImmediate(go); }
return stage + ": 4 Editor captures, far=1000m";
