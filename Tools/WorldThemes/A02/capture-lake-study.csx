// 별도 PreviewScene에서 같은 카메라·안개·Volume으로 표면만 비교하고 사본도 원래 상태로 돌린다.
string scenePath="Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity";
string folder="output/WorldThemeProduction/LakeShrine";
System.IO.Directory.CreateDirectory(folder+"/captures");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(text,type)=>deserialize.Invoke(null,new object[]{text,type});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"),typeof(Dictionary<string,object>));
var painted=(Dictionary<string,object>[])read(System.IO.File.ReadAllText(folder+"/painted-cells.json"),typeof(Dictionary<string,object>[]));
var poses=(Dictionary<string,object>[])read(plan["capturePoses"].ToString(),typeof(Dictionary<string,object>[]));
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
var originalAlpha=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
var preview=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
var roots=preview.GetRootGameObjects();
var terrain=roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)).Single();
var data=terrain.terrainData;
if(UnityEditor.AssetDatabase.GetAssetPath(data)!="Assets/10.Datas/World/ThemeStudies/LakeShrine/Terrain.asset"||data==source)throw new Exception("전용 사본이 아닙니다.");
var savedAlpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
bool wasDirty=UnityEditor.EditorUtility.IsDirty(data);
var sourcePaths=source.terrainLayers.Select(l=>UnityEditor.AssetDatabase.GetAssetPath(l)).ToArray();
var sources=(Dictionary<string,object>)read(plan["surfaceSourceLayers"].ToString(),typeof(Dictionary<string,object>));
int sandIndex=Array.IndexOf(sourcePaths,sources["sand"].ToString()),earthIndex=Array.IndexOf(sourcePaths,sources["earth"].ToString()),rockIndex=Array.IndexOf(sourcePaths,sources["rock"].ToString());
int riverIndex=Array.FindIndex(data.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)=="Assets/10.Datas/World/ThemeStudies/LakeShrine/Layer_RiverBed.terrainlayer");
if(sandIndex<0||earthIndex<0||rockIndex<0||riverIndex<0)throw new Exception("표면 remap 실패");
var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single();
camera.scene=preview;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var volume=roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)).Single();
volume.enabled=false;volume.gameObject.layer=31;volume.enabled=true;extra.volumeLayerMask=1<<31;extra.volumeTrigger=camera.transform;
var additions=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Theme_LakeShrine_Environment");
var previousPipeline=QualitySettings.renderPipeline;
var trialSky=RenderSettings.skybox;
float previousThickness=trialSky!=null&&trialSky.HasProperty("_AtmosphereThickness")?trialSky.GetFloat("_AtmosphereThickness"):-1f;
var results=new List<object>();
string trialRequest=System.IO.File.Exists(folder+"/capture-trial.txt")?System.IO.File.ReadAllText(folder+"/capture-trial.txt").Trim():"Before";
if(!new[]{"Before","Ground6","RiverBed","Mixed","Final"}.Contains(trialRequest))throw new Exception("캡처 trial 값 오류");
try{
    QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset");
    if(trialSky!=null&&previousThickness>=0f)trialSky.SetFloat("_AtmosphereThickness",1f);
    foreach(string trial in new[]{trialRequest}){
        var alpha=(float[,,])savedAlpha.Clone();
        foreach(var row in painted){
            int x=Convert.ToInt32(row["x"]),z=Convert.ToInt32(row["z"]);float blend=trial=="Before"?0f:Convert.ToSingle(row["blend"]);
            float depth=Convert.ToSingle(row["depth"]);float riverFraction=(trial=="RiverBed"||trial=="Final")?1f:trial=="Mixed"?Mathf.Lerp(.82f,.18f,Mathf.Clamp01(depth/2.8f)):0f;
            float sand=Convert.ToSingle(row["sandWeight"]),earth=Convert.ToSingle(row["earthWeight"]),rock=Convert.ToSingle(row["rockWeight"]);
            for(int l=0;l<alpha.GetLength(2);l++){
                float target=l==sandIndex?sand*(1f-riverFraction):l==riverIndex?sand*riverFraction:l==earthIndex?earth:l==rockIndex?rock:0f;
                alpha[z,x,l]=Mathf.Lerp(l<originalAlpha.GetLength(2)?originalAlpha[z,x,l]:0f,target,blend);
            }
        }
        data.SetAlphamaps(0,0,alpha);terrain.Flush();additions.gameObject.SetActive(trial!="Before");
        foreach(var pose in poses){
            string key=pose["key"].ToString();if((trial=="RiverBed"||trial=="Mixed")&&key!="representative"&&key!="activity")continue;
            var p=(float[])read(pose["position"].ToString(),typeof(float[]));var target=(float[])read(pose["target"].ToString(),typeof(float[]));
            camera.transform.position=new Vector3(p[0],p[1],p[2]);camera.transform.rotation=Quaternion.LookRotation(new Vector3(target[0],target[1],target[2])-camera.transform.position);
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            string file="captures/"+(trial=="Final"?"Final_RiverBed_WaterV2":trial)+"_"+key+".png";
            try{
                camera.targetTexture=rt;camera.Render();
                UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,extra.volumeLayerMask);
                var effectiveAdjustment=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.ColorAdjustments>();
                var effectiveBloom=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
                if(Mathf.Abs(effectiveAdjustment.saturation.value-15f)>.001f||Mathf.Abs(effectiveAdjustment.contrast.value-2f)>.001f||Mathf.Abs(effectiveBloom.intensity.value-.08f)>.001f)throw new Exception("A01 공통 실효 PP 불일치: "+effectiveAdjustment.saturation.value+"/"+effectiveAdjustment.contrast.value+"/"+effectiveBloom.intensity.value);
                RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(folder+"/"+file,texture.EncodeToPNG());
            }finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
            UnityEngine.Rendering.VolumeManager.instance.Update(camera.transform,extra.volumeLayerMask);
            var adjustment=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.ColorAdjustments>();
            var bloom=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();
            results.Add(new{file=file,trial=trial,key=key,position=p,target=target,fov=camera.fieldOfView,fog=RenderSettings.fog,fogMode=RenderSettings.fogMode.ToString(),fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,postProcessing=extra.renderPostProcessing,saturation=adjustment.saturation.value,contrast=adjustment.contrast.value,bloom=bloom.intensity.value,pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name,time="Editor 정지 낮",weather="Host 고정",mode="PreviewScene 정지 렌더 — 실제 TPS/Play/성능 판정 아님"});
        }
    }
}finally{
    data.SetAlphamaps(0,0,savedAlpha);terrain.Flush();if(!wasDirty)UnityEditor.EditorUtility.ClearDirty(data);
    QualitySettings.renderPipeline=previousPipeline;if(trialSky!=null&&previousThickness>=0f)trialSky.SetFloat("_AtmosphereThickness",previousThickness);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
var serialize=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});
System.IO.File.WriteAllText(folder+"/captures/poses-"+trialRequest+".json",(string)serialize.Invoke(null,new object[]{results}));
return "LakeShrine 비교 사진 "+results.Count+"장. clone 표면 원상복구, 본편 쓰기 없음.";
