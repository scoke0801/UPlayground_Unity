if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets" || !System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
// 현재 검토 씬의 실제 렌더. 게임 입력/TPS 검증과 구분한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"))throw new Exception("검토 씬 Edit Mode 필요");
string stage=System.IO.File.ReadAllText("Tools/WorldThemes/HarborReedR1/stage.txt").Trim();
string folder="output/HarborReedR1/"+stage;System.IO.Directory.CreateDirectory(folder);
var terrain=Terrain.activeTerrain;
var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
var cameraObject=UnityEngine.Object.Instantiate(source.gameObject);cameraObject.name="TemporaryArtCapture";
var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=true;extra.volumeTrigger=camera.transform;
var savedSun=RenderSettings.sun; var savedSunColor=savedSun==null?Color.white:savedSun.color; var savedSunIntensity=savedSun==null?1f:savedSun.intensity; var savedSunRotation=savedSun==null?Quaternion.identity:savedSun.transform.rotation; var oldSky=RenderSettings.skybox;var captureSky=new Material(oldSky);captureSky.SetFloat("_UseCloudClock",1);captureSky.SetFloat("_CloudClock",0);RenderSettings.skybox=captureSky;
var oldPipeline=QualitySettings.renderPipeline;var oldActive=RenderTexture.active;
var waterRenderer=GameObject.Find("CoastalWater").GetComponent<Renderer>();var oldWater=waterRenderer.sharedMaterial;var captureWater=new Material(oldWater);captureWater.SetVector("_WaveVelocity",Vector4.zero);captureWater.SetFloat("_FoamSpeed",0);waterRenderer.sharedMaterial=captureWater;
GameObject creekObject=null;var creekRenderer=creekObject==null?null:creekObject.GetComponent<Renderer>();var creekOriginal=creekRenderer==null?null:creekRenderer.sharedMaterial;var creekCapture=creekOriginal==null?null:new Material(creekOriginal);if(creekCapture!=null){if(creekCapture.HasProperty("_GlobalSpeed"))creekCapture.SetFloat("_GlobalSpeed",0);if(creekCapture.HasProperty("_WaveVelocity"))creekCapture.SetVector("_WaveVelocity",Vector4.zero);if(creekCapture.HasProperty("_FoamSpeed"))creekCapture.SetFloat("_FoamSpeed",0);creekRenderer.sharedMaterial=creekCapture;}
var binding=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Single(c=>c!=null&&c.GetType().Name=="SceneEnvironmentRendering");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var bt=binding.GetType();
var runtimeField=bt.GetField("_runtimeSky",flags);var regionField=bt.GetField("_orderedRegions",flags);var fogField=bt.GetField("_hasFogSnapshot",flags);var sunField=bt.GetField("_boundSun",flags);
var oldRuntime=runtimeField.GetValue(binding);var oldRegions=regionField.GetValue(binding);var oldFogFlag=fogField.GetValue(binding);var oldSun=sunField.GetValue(binding);
float oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance,oldFogDensity=RenderSettings.fogDensity;var oldFogColor=RenderSettings.fogColor;
runtimeField.SetValue(binding,captureSky);regionField.SetValue(binding,bt.GetField("_skyRegions",flags).GetValue(binding));fogField.SetValue(binding,true);sunField.SetValue(binding,RenderSettings.sun);
var positions=new[]{new Vector3(1848,52,-327),new Vector3(1898,53,-327),new Vector3(1905,53,-350)};
var targets=new[]{new Vector3(1902,48,-345),new Vector3(1940,50,-342),new Vector3(1860,49,-322)};
var keys=new[]{"entry","activity","return"};
var rows=new List<string>();
var capturePipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset"));
capturePipeline.hideFlags=HideFlags.HideAndDontSave;
var pipelineSerialized=new SerializedObject(capturePipeline);var drawer=pipelineSerialized.FindProperty("m_GPUResidentDrawerMode");if(drawer!=null){drawer.intValue=0;pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();}
var rendererMaterials=new Dictionary<Renderer,Material[]>();var materialCopies=new Dictionary<Material,Material>();
var lodStates=new Dictionary<LODGroup,bool>();
foreach(var group in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){lodStates[group]=group.animateCrossFading;group.animateCrossFading=false;}
foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
var originals=renderer.sharedMaterials;rendererMaterials[renderer]=originals;
renderer.sharedMaterials=originals.Select(m=>{if(m==null)return m;if(materialCopies.ContainsKey(m))return materialCopies[m];var copy=new Material(m);copy.hideFlags=HideFlags.HideAndDontSave;copy.DisableKeyword("_USE_WPO");for(int j=0;j<copy.shader.GetPropertyCount();j++){string property=copy.shader.GetPropertyName(j);var type=copy.shader.GetPropertyType(j);if(property.ToLowerInvariant().Contains("wind")&&(type==UnityEngine.Rendering.ShaderPropertyType.Float||type==UnityEngine.Rendering.ShaderPropertyType.Range))copy.SetFloat(property,0);}materialCopies[m]=copy;return copy;}).ToArray();
}
rows.Add("CloudClock=0; water speed=0; cloned renderer materials: WPO disabled, wind float properties=0; animated LOD crossfade disabled. Shader internal time, terrain detail wind and static dither not fully controlled. Leaf/shadow pixel differences are not art evidence.");
try{
QualitySettings.renderPipeline=capturePipeline;
for(int i=0;i<positions.Length;i++){
var p=positions[i]; if(p.y==0)p.y=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/RedForestVisualR1/Terrain.asset").GetInterpolatedHeight(p.x/2400f,(p.z+700)/2800f)+2.2f;
var sample=bt.GetMethod("EvaluateRegionalSky",flags).Invoke(binding,new object[]{.5f,p});bt.GetMethod("ApplySkySample",flags).Invoke(binding,new[]{sample});
camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(targets[i]-p));camera.fieldOfView=60;camera.aspect=16f/9f;
var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+keys[i]+".png",tex.EncodeToPNG());rows.Add(keys[i]+" | "+p.ToString("F5")+" | "+targets[i].ToString("F5")+" | FOV60 1280x720 Editor static");}
finally{camera.targetTexture=null;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
}finally{foreach(var pair in rendererMaterials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var pair in materialCopies)UnityEngine.Object.DestroyImmediate(pair.Value);foreach(var pair in lodStates)if(pair.Key!=null)pair.Key.animateCrossFading=pair.Value;if(creekRenderer!=null){creekRenderer.sharedMaterial=creekOriginal;UnityEngine.Object.DestroyImmediate(creekCapture);}waterRenderer.sharedMaterial=oldWater;UnityEngine.Object.DestroyImmediate(captureWater);runtimeField.SetValue(binding,oldRuntime);regionField.SetValue(binding,oldRegions);fogField.SetValue(binding,oldFogFlag);sunField.SetValue(binding,oldSun);RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogDensity=oldFogDensity;RenderSettings.fogColor=oldFogColor;QualitySettings.renderPipeline=oldPipeline;UnityEngine.Object.DestroyImmediate(capturePipeline);if(savedSun!=null){savedSun.color=savedSunColor;savedSun.intensity=savedSunIntensity;savedSun.transform.rotation=savedSunRotation;}RenderSettings.skybox=oldSky;UnityEngine.Object.DestroyImmediate(captureSky);UnityEngine.Object.DestroyImmediate(cameraObject);}
System.IO.File.WriteAllLines(folder+"/poses.txt",rows);return rows;














