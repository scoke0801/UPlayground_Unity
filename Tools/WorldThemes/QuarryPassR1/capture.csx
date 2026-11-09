if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("QuarryPassR1-A-20261108"))throw new Exception("프로젝트/잠금 불일치");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("A 검토 씬 Edit Mode 필요");
string stage=System.IO.File.ReadAllText("Tools/WorldThemes/QuarryPassR1/stage.txt").Trim();
string folder="output/QuarryPassR1/"+stage;System.IO.Directory.CreateDirectory(folder);
var reviewTerrain=Terrain.activeTerrain;var reviewData=reviewTerrain.terrainData;var strataRoot=GameObject.Find("QuarryA_ConnectedStrata");bool strataActive=strataRoot!=null&&strataRoot.activeSelf;var reviewMaterials=new Dictionary<Renderer,Material[]>();
var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
var go=UnityEngine.Object.Instantiate(source.gameObject);go.name="QuarryA_TemporaryCapture";var camera=go.GetComponent<Camera>();camera.enabled=false;
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=true;extra.volumeTrigger=camera.transform;
var sky=RenderSettings.skybox;var temporarySky=new Material(sky);temporarySky.SetFloat("_UseCloudClock",1);temporarySky.SetFloat("_CloudClock",0);
var oldPipeline=QualitySettings.renderPipeline;var oldRT=RenderTexture.active;
var pipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset"));pipeline.hideFlags=HideFlags.HideAndDontSave;
var serialized=new SerializedObject(pipeline);var drawer=serialized.FindProperty("m_GPUResidentDrawerMode");if(drawer!=null){drawer.intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();}
var binding=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Single(c=>c!=null&&c.GetType().Name=="SceneEnvironmentRendering");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var type=binding.GetType();
string[] fields={"_runtimeSky","_orderedRegions","_hasFogSnapshot","_boundSun"};var oldFields=fields.Select(f=>type.GetField(f,flags).GetValue(binding)).ToArray();
var oldFog=new Vector3(RenderSettings.fogStartDistance,RenderSettings.fogEndDistance,RenderSettings.fogDensity);var oldFogColor=RenderSettings.fogColor;var sun=RenderSettings.sun;var sunColor=sun.color;float sunIntensity=sun.intensity;
var poses=new[]{new Vector3(542,192.91904f,1040),new Vector3(450,184.64661f,1140),new Vector3(420,177.02922f,1186),new Vector3(548,180.23628f,1240),new Vector3(560,178.33786f,1330),new Vector3(580,176.21697f,1435),new Vector3(580,176.21697f,1435)};
var targets=new[]{new Vector3(350,218,1205),new Vector3(350,215,1200),new Vector3(346,220,1210),new Vector3(450,220,1375),new Vector3(450,220,1380),new Vector3(460,205,1360),new Vector3(850,170,1330)};
string[] keys={"entry","quarry-approach","quarry-face","pass-entry","pass-space","return","pass-outlook"};var rows=new List<object>();
try{
if(stage=="before"&&strataRoot!=null){
strataRoot.SetActive(false);reviewTerrain.terrainData=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
var originalMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/PoorVillage/Props/PoorProp_Stone_Field.mat");
foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("채석장_")||t.name.StartsWith("고개_"))){var r=t.GetComponent<Renderer>();if(r!=null){reviewMaterials[r]=r.sharedMaterials;r.sharedMaterials=Enumerable.Repeat(originalMaterial,r.sharedMaterials.Length).ToArray();}}
}
RenderSettings.skybox=temporarySky;QualitySettings.renderPipeline=pipeline;
type.GetField(fields[0],flags).SetValue(binding,temporarySky);type.GetField(fields[1],flags).SetValue(binding,type.GetField("_skyRegions",flags).GetValue(binding));type.GetField(fields[2],flags).SetValue(binding,true);type.GetField(fields[3],flags).SetValue(binding,sun);
for(int i=0;i<poses.Length;i++){
var sample=type.GetMethod("EvaluateRegionalSky",flags).Invoke(binding,new object[]{.5f,poses[i]});type.GetMethod("ApplySkySample",flags).Invoke(binding,new[]{sample});
camera.transform.SetPositionAndRotation(poses[i],Quaternion.LookRotation(targets[i]-poses[i]));camera.fieldOfView=60;camera.aspect=16f/9f;
var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+keys[i]+".png",tex.EncodeToPNG());rows.Add(new{key=keys[i],position=new[]{poses[i].x,poses[i].y,poses[i].z},target=new[]{targets[i].x,targets[i].y,targets[i].z},fov=60,width=1280,height=720,time=.5f,cloudClock=0,postProcessing=true,fog=RenderSettings.fog,wind="통제하지 않음; 잎·그림자 차이는 개선 근거 제외",mode="Editor static; TPS 아님"});}
finally{camera.targetTexture=null;RenderTexture.active=oldRT;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
}finally{
reviewTerrain.terrainData=reviewData;if(strataRoot!=null)strataRoot.SetActive(strataActive);foreach(var pair in reviewMaterials)pair.Key.sharedMaterials=pair.Value;
for(int i=0;i<fields.Length;i++)type.GetField(fields[i],flags).SetValue(binding,oldFields[i]);
RenderSettings.fogStartDistance=oldFog.x;RenderSettings.fogEndDistance=oldFog.y;RenderSettings.fogDensity=oldFog.z;RenderSettings.fogColor=oldFogColor;sun.color=sunColor;sun.intensity=sunIntensity;
QualitySettings.renderPipeline=oldPipeline;RenderSettings.skybox=sky;UnityEngine.Object.DestroyImmediate(pipeline);UnityEngine.Object.DestroyImmediate(temporarySky);UnityEngine.Object.DestroyImmediate(go);
}
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText(folder+"/poses.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{rows}));
return rows;
