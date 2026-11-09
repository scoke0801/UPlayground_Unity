// 지도 촬영에서 깊이·굴절 의존을 제거하고 실제 수면 메시와 지형의 교차를 보존한다.
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.isLoaded)
    throw new Exception("LakeVillageWorld를 Edit Mode에서 연 뒤 실행하세요.");
string imagePath = "Assets/10.Datas/World/LakeVillage/MapBackground.png";
string folder = "output/LakeVillageMapWater";
System.IO.Directory.CreateDirectory(folder);
if (!System.IO.File.Exists(folder + "/MapBackground.before.png"))
    System.IO.File.Copy(imagePath, folder + "/MapBackground.before.png");
var config = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/10.Datas/World/LakeVillage/MinimapConfig.asset"));
Vector2 center = config.FindProperty("captureCenter").vector2Value;
Vector2 worldSize = config.FindProperty("captureWorldSizeXY").vector2Value;
int width, height;
var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
importer.GetSourceTextureWidthAndHeight(out width, out height);
var surfaces = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>())
    .Where(renderer => renderer.sharedMaterials.Any(material => material && material.shader.name == "UPlayGround/Environment/Lake Water")).ToArray();
if (surfaces.Length == 0) throw new Exception("촬영할 수면이 없습니다.");
var originals = surfaces.Select(renderer => renderer.sharedMaterials).ToArray();
var shader = Shader.Find("Universal Render Pipeline/Unlit");
if (!shader) throw new Exception("지도용 수면 셰이더가 없습니다.");
var water = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
water.SetColor("_BaseColor", new Color(0.075f, 0.32f, 0.4f, 1f));
var cameraObject = new GameObject("MapWaterCapture") { hideFlags = HideFlags.HideAndDontSave };
var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var drawerProperty = pipeline.GetType().GetProperty("gpuResidentDrawerMode");
var savedDrawer = drawerProperty.GetValue(pipeline, null);
Texture2D texture = null;
try
{
    // 즉시 렌더에서도 임시 교체한 재질을 사용하게 한다.
    drawerProperty.SetValue(pipeline, Enum.Parse(drawerProperty.PropertyType, "Disabled"), null);
    foreach (var surface in surfaces)
        surface.sharedMaterials = surface.sharedMaterials.Select(material => material && material.shader.name == "UPlayGround/Environment/Lake Water" ? water : material).ToArray();
    var camera = cameraObject.AddComponent<Camera>();
    camera.enabled = false;
    camera.scene = scene;
    camera.orthographic = true;
    camera.orthographicSize = worldSize.y / 2;
    camera.aspect = worldSize.x / worldSize.y;
    camera.transform.SetPositionAndRotation(new Vector3(center.x, 700, center.y), Quaternion.Euler(90, 0, 0));
    camera.nearClipPlane = 0.2f;
    camera.farClipPlane = 2000;
    camera.cullingMask = ~0;
    camera.useOcclusionCulling = false;
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = RenderSettings.ambientGroundColor;
    var utility = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(type => type != null);
    texture = (Texture2D)utility.GetMethod("Render").Invoke(null, new object[] { camera, new Vector2Int(width, height), true, 1000f });
    System.IO.File.WriteAllBytes(imagePath, texture.EncodeToPNG());
    AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
}
finally
{
    for (int i = 0; i < surfaces.Length; i++)
        if (surfaces[i]) surfaces[i].sharedMaterials = originals[i];
    drawerProperty.SetValue(pipeline, savedDrawer, null);
    if (texture) UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(water);
}
string report = "지도 수면 재촬영\n이미지: " + imagePath + "\n해상도: " + width + " x " + height
    + "\n중심: " + center + " / 범위: " + worldSize + "\n수면: " + string.Join(", ", surfaces.Select(surface => surface.name).ToArray())
    + "\n촬영 중에만 불투명 지도 재질 적용. 원본 재질·GPU Resident Drawer 복원. 씬 저장 없음.\n";
System.IO.File.WriteAllText(folder + "/capture-result.txt", report);
return report;
