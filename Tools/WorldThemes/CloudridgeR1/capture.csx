// 촬영은 B 검토 씬에서만 실행하고 모든 임시 렌더 설정을 복구한다.
if(Application.dataPath.Replace('\\','/')!="C:/UsingProject/UnityProject/UPlayground/Assets")throw new Exception("프로젝트 불일치");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null); Func<object,string> json=o=>(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{o}); var lease=(Dictionary<string,string>)jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock"),typeof(Dictionary<string,string>)});
if((string)lease["owner"]!="B")throw new Exception("B 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/CloudridgeR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("B Edit Mode 필요");
var plan=(Dictionary<string,float[]>)jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText("Tools/WorldThemes/CloudridgeR1/capture-plan.json"),typeof(Dictionary<string,float[]>)});
string stage=System.IO.File.ReadAllText("Tools/WorldThemes/CloudridgeR1/stage.txt").Trim();
string folder="output/CloudridgeR1/"+stage;System.IO.Directory.CreateDirectory(folder);
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var source=all.Select(t=>t.GetComponent<Camera>()).First(c=>c!=null);
var go=new GameObject("CloudridgeTemporaryCapture");go.hideFlags=HideFlags.HideAndDontSave;
var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
var extra=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
var sourceExtra=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
if(sourceExtra!=null)EditorUtility.CopySerialized(sourceExtra,extra);
extra.renderPostProcessing=true;extra.volumeTrigger=go.transform;
var originalSky=RenderSettings.skybox;var captureSky=new Material(originalSky);captureSky.hideFlags=HideFlags.HideAndDontSave;
var originalPipeline=QualitySettings.renderPipeline;var originalActive=RenderTexture.active;
var pipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset"));pipeline.hideFlags=HideFlags.HideAndDontSave;
var pipelineSo=new SerializedObject(pipeline);var drawer=pipelineSo.FindProperty("m_GPUResidentDrawerMode");if(drawer!=null){drawer.intValue=0;pipelineSo.ApplyModifiedPropertiesWithoutUndo();}
var binding=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).First(c=>c!=null&&c.GetType().Name=="SceneEnvironmentRendering");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var type=binding.GetType();
var fields=new[]{"_runtimeSky","_orderedRegions","_hasFogSnapshot","_boundSun"}.Select(n=>type.GetField(n,flags)).ToArray();
var values=fields.Select(f=>f.GetValue(binding)).ToArray();
float fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,fogDensity=RenderSettings.fogDensity;var fogColor=RenderSettings.fogColor;bool fog=RenderSettings.fog;var fogMode=RenderSettings.fogMode;
var sun=RenderSettings.sun;var sunColor=sun.color;float sunIntensity=sun.intensity;var sunRotation=sun.transform.rotation;
var materialCopies=new Dictionary<Material,Material>();var rendererMaterials=new Dictionary<Renderer,Material[]>();var lodStates=new Dictionary<LODGroup,bool>();
var rows=new List<object>();
try{
RenderSettings.skybox=captureSky;fields[0].SetValue(binding,captureSky);fields[1].SetValue(binding,type.GetField("_skyRegions",flags).GetValue(binding));fields[2].SetValue(binding,true);fields[3].SetValue(binding,sun);
QualitySettings.renderPipeline=pipeline;
foreach(var group in all.Select(t=>t.GetComponent<LODGroup>()).Where(g=>g!=null)){lodStates[group]=group.animateCrossFading;group.animateCrossFading=false;}
foreach(var renderer in all.SelectMany(t=>t.GetComponents<Renderer>())){
var originals=renderer.sharedMaterials;rendererMaterials[renderer]=originals;
renderer.sharedMaterials=originals.Select(m=>{if(m==null)return m;if(materialCopies.ContainsKey(m))return materialCopies[m];var materialSource=m;if(AssetDatabase.GetAssetPath(m)=="Assets/08.Models/PoorVillage/PoorHouse_Hut.fbx"){materialSource=AssetDatabase.LoadAllAssetsAtPath("Assets/10.Datas/World/ThemeStudies/CloudridgeR1/CaptureDiskHut.fbx").OfType<Material>().First(v=>v.name==m.name);}var copy=new Material(materialSource);copy.hideFlags=HideFlags.HideAndDontSave;copy.DisableKeyword("_USE_WPO");if(copy.shader!=null)for(int j=0;j<copy.shader.GetPropertyCount();j++){string property=copy.shader.GetPropertyName(j);var kind=copy.shader.GetPropertyType(j);if(property.ToLowerInvariant().Contains("wind")&&(kind==UnityEngine.Rendering.ShaderPropertyType.Float||kind==UnityEngine.Rendering.ShaderPropertyType.Range))copy.SetFloat(property,0);}if(copy.HasProperty("_WaveVelocity"))copy.SetVector("_WaveVelocity",Vector4.zero);if(copy.HasProperty("_FoamSpeed"))copy.SetFloat("_FoamSpeed",0);if(copy.HasProperty("_GlobalSpeed"))copy.SetFloat("_GlobalSpeed",0);materialCopies[m]=copy;return copy;}).ToArray();
}
foreach(var shot in plan){
float[] p=shot.Value.Take(3).ToArray();float[] target=shot.Value.Skip(3).Take(3).ToArray();var position=new Vector3(p[0],p[1],p[2]);var aim=new Vector3(target[0],target[1],target[2]);
var sample=type.GetMethod("EvaluateRegionalSky",flags).Invoke(binding,new object[]{.5f,position});type.GetMethod("ApplySkySample",flags).Invoke(binding,new[]{sample});
captureSky.SetFloat("_UseCloudClock",1);captureSky.SetFloat("_CloudClock",0);captureSky.SetVector("_CloudWindOffset",Vector4.zero);
camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim-position));camera.fieldOfView=shot.Value[6];camera.aspect=16f/9f;
var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+shot.Key+".png",tex.EncodeToPNG());rows.Add(new{key=shot.Key,position=p,target=target,fov=camera.fieldOfView,width=1280,height=720,time=.5f,postProcessing=true,fog=RenderSettings.fog,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance,sky=originalSky.name,cloudClock=captureSky.GetFloat("_CloudClock"),useCloudClock=captureSky.GetFloat("_UseCloudClock"),sunEuler=new[]{sun.transform.eulerAngles.x,sun.transform.eulerAngles.y,sun.transform.eulerAngles.z},sunIntensity=sun.intensity,weather="Edit Mode 지역 sample, 런타임 weather service 비실행",pipelineSource="Assets/Settings/LakeVillage_RPAsset.asset",drawerDisabledOnTemporaryCopy=true});}
finally{camera.targetTexture=null;RenderTexture.active=originalActive;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
}finally{
foreach(var pair in rendererMaterials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;
foreach(var pair in materialCopies)UnityEngine.Object.DestroyImmediate(pair.Value);
foreach(var pair in lodStates)if(pair.Key!=null)pair.Key.animateCrossFading=pair.Value;
for(int i=0;i<fields.Length;i++)fields[i].SetValue(binding,values[i]);
RenderSettings.fog=fog;RenderSettings.fogMode=fogMode;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;RenderSettings.fogDensity=fogDensity;RenderSettings.fogColor=fogColor;sun.color=sunColor;sun.intensity=sunIntensity;sun.transform.rotation=sunRotation;
QualitySettings.renderPipeline=originalPipeline;RenderSettings.skybox=originalSky;UnityEngine.Object.DestroyImmediate(pipeline);UnityEngine.Object.DestroyImmediate(captureSky);UnityEngine.Object.DestroyImmediate(go);
}
var result=new{shots=rows,controls="정오 지역 하늘, 고정 cloud clock, 임시 재질 wind float=0, WPO off, animated crossfade off; 종료 후 복구",uncontrolled="셰이더 내부 시간, Terrain detail 바람, 정적 디더 및 그림자 미세차. 잎/그림자 픽셀 차이는 개선 증거 제외",kind="Editor 정지 렌더, 실제 TPS 아님"};
System.IO.File.WriteAllText(folder+"/capture.json",json(result));return rows;




