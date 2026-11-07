// A00 seam 검수용. 같은 독립 씬을 격리 렌더하고 전역 룩과 활성 씬을 복원한다.
string output="output/WorldThemeProduction/LakeShrine/SeamV3";
string stage=System.IO.File.Exists(output+"/applied.json")?"after":"before";
string folder=output+"/captures/"+stage;System.IO.Directory.CreateDirectory(folder);
if(System.IO.File.Exists(folder+"/poses.json"))throw new Exception("이미 완료한 동일 버전 사진을 덮어쓰지 않습니다.");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"),typeof(Dictionary<string,object>));
var poses=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
var previousActive=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var study=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity");
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(EditorApplication.isPlayingOrWillChangePlaymode||!study.isLoaded||!host.isLoaded)throw new Exception("열린 독립 호수/본편 Edit Mode 필요");
bool hostDirty=host.isDirty,studyDirty=study.isDirty;
UnityEngine.SceneManagement.SceneManager.SetActiveScene(study);
var sky=RenderSettings.skybox;float previousThickness=sky.GetFloat("_AtmosphereThickness");
var pipeline=QualitySettings.renderPipeline;
var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(study.path);
var camera=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var volume=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)).Single();
volume.enabled=false;volume.gameObject.layer=31;volume.enabled=true;extra.volumeLayerMask=1<<31;extra.volumeTrigger=camera.transform;
var rows=new List<object>();
try{
    QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset");
    sky.SetFloat("_AtmosphereThickness",1f);
    foreach(var pose in poses){
        string key=pose["key"].ToString();var p=(float[])read(pose["position"].ToString(),typeof(float[]));var t=(float[])read(pose["target"].ToString(),typeof(float[]));
        camera.transform.position=new Vector3(p[0],p[1],p[2]);camera.transform.rotation=Quaternion.LookRotation(new Vector3(t[0],t[1],t[2])-camera.transform.position);
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{
            camera.targetTexture=rt;camera.Render();camera.Render();
            UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,extra.volumeLayerMask);
            var adjustment=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.ColorAdjustments>();
            var bloom=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
            float thickness=RenderSettings.skybox.GetFloat("_AtmosphereThickness");
            if(Mathf.Abs(adjustment.saturation.value-15f)>.001f||Mathf.Abs(adjustment.contrast.value-2f)>.001f||Mathf.Abs(bloom.intensity.value-.08f)>.001f||Mathf.Abs(thickness-1f)>.001f||!RenderSettings.fog)throw new Exception("공통 실효 룩 불일치");
            RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(folder+"/"+key+".png",texture.EncodeToPNG());
            rows.Add(new{key,position=p,target=t,fov=camera.fieldOfView,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,fog=RenderSettings.fog,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,saturation=adjustment.saturation.value,contrast=adjustment.contrast.value,bloom=bloom.intensity.value,skyThickness=thickness,time="Editor 정지 낮",weather="고정",scope="독립 seam 정지 검수. 실제 TPS/Play 검증 아님"});
        }finally{camera.targetTexture=null;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
    }
}finally{
    QualitySettings.renderPipeline=pipeline;sky.SetFloat("_AtmosphereThickness",previousThickness);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    if(previousActive.IsValid()&&previousActive.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);
}
if(host.isDirty!=hostDirty||study.isDirty!=studyDirty)throw new Exception("촬영 중 열린 씬 dirty 상태 변경. 저장하지 않습니다.");
System.IO.File.WriteAllText(folder+"/poses.json",json(rows));return "A00 seam "+stage+" captures="+rows.Count;
