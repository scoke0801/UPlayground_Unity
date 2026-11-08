// PreviewScene만 렌더해 미저장 Host와 겹치지 않으며 전역 설정을 바꾸지 않는다.
string scenePath = "Assets/01.Scenes/ArtReview/WorldThemes/Theme_LookWater.unity";
string folder = "output/WorldThemeProduction/A01/captures";
System.IO.Directory.CreateDirectory(folder);
var preview = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
var previousPipeline = QualitySettings.renderPipeline;
var trialPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset");
var camera = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
camera.scene = preview;
camera.overrideSceneCullingMask = UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);
var initialPosition = camera.transform.position;
var initialRotation = camera.transform.rotation;
var extra = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var previewVolume = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)).Single();
previewVolume.gameObject.layer = 31;
extra.volumeLayerMask = 1 << 31;
extra.volumeTrigger = camera.transform;
var fixtures = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).First(t => t.name == "TrialFixtures_ExcludeFromIntegration");
var trialSky = RenderSettings.skybox;
float skyThickness = trialSky != null && trialSky.HasProperty("_AtmosphereThickness") ? trialSky.GetFloat("_AtmosphereThickness") : -1f;
var rows = new List<object>();
try
{
    QualitySettings.renderPipeline = trialPipeline;
    if (trialSky != null && trialSky.HasProperty("_AtmosphereThickness")) trialSky.SetFloat("_AtmosphereThickness",1f);
    var poses = new [] {
        new { key = "water-depth-lanes", position = initialPosition+new Vector3(0f,11f,2f), target = new Vector3(1017f,initialPosition.y-4f,596f) },
        new { key = "water-low-angle", position = new Vector3(1017f,initialPosition.y-2f,575f), target = new Vector3(1017f,initialPosition.y-3f,595f) },
        new { key = "foliage-comparison", position = new Vector3(1018f,81f,542f), target = new Vector3(1020f,81f,565f) },
        new { key = "detail-six-baseline", position = new Vector3(1134f,65.2f,268f), target = new Vector3(1134f,63.5f,276f) },
        new { key = "asset-rock-close", position = new Vector3(1036f,81f,559f), target = new Vector3(1040f,80.2f,565f) },
        new { key = "asset-flower-close", position = new Vector3(1030f,79.25f,564.4f), target = new Vector3(1030f,79f,565f) }
    };
    foreach(var pose in poses)
    {
        bool isWater = pose.key.StartsWith("water");
        foreach(Transform item in fixtures) item.gameObject.SetActive(isWater ? item.name.EndsWith("DepthTrial") || item.name.StartsWith("Depth_") : !(item.name.EndsWith("DepthTrial") || item.name.StartsWith("Depth_")));
        camera.transform.position = pose.position; camera.transform.rotation = Quaternion.LookRotation(pose.target-pose.position);
        var rt = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
        var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            System.IO.File.WriteAllBytes(folder+"/"+pose.key+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        }
        var p = camera.transform.position; var e = camera.transform.eulerAngles;
        UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform, extra.volumeLayerMask);
        var adjustments = UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.ColorAdjustments>();
        var bloom = UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
        rows.Add(new { file = pose.key+".png", position = new float[]{p.x,p.y,p.z}, rotation = new float[]{e.x,e.y,e.z}, fov = camera.fieldOfView, previewSceneMask = camera.overrideSceneCullingMask, mode = "Editor 정지 PreviewScene", actualPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "None" : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name, postProcessing = extra.renderPostProcessing, effectiveSaturation = adjustments.saturation.value, effectiveContrast = adjustments.contrast.value, effectiveBloom = bloom.intensity.value, time = "정지", fog = RenderSettings.fog, skyThickness = trialSky == null || !trialSky.HasProperty("_AtmosphereThickness") ? -1f : trialSky.GetFloat("_AtmosphereThickness") });
    }
}
finally
{
    QualitySettings.renderPipeline = previousPipeline;
    if (trialSky != null && skyThickness >= 0f) trialSky.SetFloat("_AtmosphereThickness",skyThickness);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var serialize = jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});
string result = (string)serialize.Invoke(null,new object[]{rows});
System.IO.File.WriteAllText(folder+"/poses.json",result);
return result;
