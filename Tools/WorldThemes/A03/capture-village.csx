// 본편 미저장 상태를 유지하면서 별도 PreviewScene의 같은 카메라로만 비교한다.
string scenePath="Assets/01.Scenes/ArtReview/WorldThemes/Theme_VillageFields.unity",output="output/WorldThemeProduction/VillageFields";
string stage=System.IO.File.Exists(output+"/applied.json")?"after":"before";
string folder=output+"/captures/"+stage;System.IO.Directory.CreateDirectory(folder);
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>deserialize.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var poses=(Dictionary<string,object>[])read(System.IO.File.ReadAllText(output+"/capture-poses.json"),typeof(Dictionary<string,object>[]));
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");bool hostDirty=host.isDirty;
var previousActive=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var loadedReview=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!loadedReview.IsValid()||!loadedReview.isLoaded)throw new Exception("A03 로드 상태 필요");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(loadedReview);
var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
var previousPipeline=QualitySettings.renderPipeline;var rows=new List<object>();
try{
 QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset");
 var camera=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);camera.fieldOfView=60f;
 var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=true;extra.volumeLayerMask=1<<31;extra.volumeTrigger=camera.transform;
 var volume=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)).Single();volume.enabled=false;volume.gameObject.layer=31;volume.enabled=true;
 var comparison=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="CandidateComparison_ExcludeFromIntegration");comparison.gameObject.SetActive(false);
 Action<string,Vector3,Vector3> capture=(key,position,target)=>{
  camera.transform.position=position;camera.transform.rotation=Quaternion.LookRotation(target-position);
  var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  float saturation=0,contrast=0,bloom=0;
  try{
   camera.targetTexture=rt;UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,extra.volumeLayerMask);camera.Render();camera.Render();
   UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,extra.volumeLayerMask);
   var adjustment=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.ColorAdjustments>();var glow=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();saturation=adjustment.saturation.value;contrast=adjustment.contrast.value;bloom=glow.intensity.value;
   if(Mathf.Abs(saturation-15f)>.01f||Mathf.Abs(contrast-2f)>.01f||Mathf.Abs(bloom-.08f)>.001f)throw new Exception("실효 Volume이 A01 조건과 다릅니다. 사진 제외: "+saturation+"/"+contrast+"/"+bloom);
   RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes(folder+"/"+key+".png",image.EncodeToPNG());
  }finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
  var sky=RenderSettings.skybox;float skyThickness=sky!=null&&sky.HasProperty("_AtmosphereThickness")?sky.GetFloat("_AtmosphereThickness"):-1f;if(Mathf.Abs(skyThickness-1f)>.001f)throw new Exception("실효 하늘 두께가 A01과 다릅니다.");
  var angles=camera.transform.eulerAngles;rows.Add(new{file=key+".png",position=new[]{position.x,position.y,position.z},rotation=new[]{angles.x,angles.y,angles.z},fov=camera.fieldOfView,mode="Editor 정지 PreviewScene",postProcessing=extra.renderPostProcessing,effectiveSaturation=saturation,effectiveContrast=contrast,effectiveBloom=bloom,previewSceneMask=camera.overrideSceneCullingMask,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,fog=RenderSettings.fog,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,skyThickness=skyThickness,skyMaterial=AssetDatabase.GetAssetPath(sky),time="고정 낮",weather="기준선",hostDirtyBefore=hostDirty,hostDirtyAfter=host.isDirty});
 };
 foreach(var pose in poses){var p=(float[])read(pose["position"].ToString(),typeof(float[]));var t=(float[])read(pose["target"].ToString(),typeof(float[]));capture(pose["key"].ToString(),new Vector3(p[0],p[1],p[2]),new Vector3(t[0],t[1],t[2]));}
 if(stage=="before"){
  comparison.gameObject.SetActive(true);
  foreach(Transform candidate in comparison){foreach(Transform item in comparison)item.gameObject.SetActive(item==candidate);var rs=candidate.GetComponentsInChildren<MeshRenderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float distance=Mathf.Max(2f,Mathf.Max(b.size.x,b.size.y)*2f);capture("asset_"+candidate.name,b.center+new Vector3(0,Mathf.Max(.6f,b.size.y*.3f),-distance),b.center);}
 }
 if(host.isDirty!=hostDirty)throw new Exception("촬영 중 본편 dirty 상태 변경");
}finally{QualitySettings.renderPipeline=previousPipeline;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);}
System.IO.File.WriteAllText(folder+"/poses.json",json(rows));
return "A03 "+stage+" valid PP captures="+rows.Count+"; host preserved";
