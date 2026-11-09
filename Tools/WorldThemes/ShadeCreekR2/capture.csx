// 현재 검토 씬의 실제 렌더. 게임 입력/TPS 검증과 구분한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR2.unity"&&scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"))throw new Exception("검토 씬 Edit Mode 필요");
string stage=System.IO.File.ReadAllText("Tools/WorldThemes/ShadeCreekR2/stage.txt").Trim();
string folder="output/ShadeCreekR2/"+stage;System.IO.Directory.CreateDirectory(folder);
var terrain=Terrain.activeTerrain;
var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single();
var cameraObject=UnityEngine.Object.Instantiate(source.gameObject);cameraObject.name="TemporaryArtCapture";
var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;
var extra=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=true;extra.volumeTrigger=camera.transform;
var oldSky=RenderSettings.skybox;var captureSky=new Material(oldSky);captureSky.SetFloat("_UseCloudClock",1);captureSky.SetFloat("_CloudClock",0);RenderSettings.skybox=captureSky;
var oldPipeline=QualitySettings.renderPipeline;var oldActive=RenderTexture.active;
var waterRenderer=GameObject.Find("LakeWater").GetComponent<Renderer>();var oldWater=waterRenderer.sharedMaterial;var captureWater=new Material(oldWater);captureWater.SetVector("_WaveVelocity",Vector4.zero);captureWater.SetFloat("_FoamSpeed",0);waterRenderer.sharedMaterial=captureWater;
var creekObject=GameObject.Find("ShadeCreekR1_NonNarrativeWaterStudy");var creekRenderer=creekObject==null?null:creekObject.GetComponent<Renderer>();var creekOriginal=creekRenderer==null?null:creekRenderer.sharedMaterial;var creekCapture=creekOriginal==null?null:new Material(creekOriginal);if(creekCapture!=null){if(creekCapture.HasProperty("_GlobalSpeed"))creekCapture.SetFloat("_GlobalSpeed",0);if(creekCapture.HasProperty("_WaveVelocity"))creekCapture.SetVector("_WaveVelocity",Vector4.zero);if(creekCapture.HasProperty("_FoamSpeed"))creekCapture.SetFloat("_FoamSpeed",0);creekRenderer.sharedMaterial=creekCapture;}
var binding=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Single(c=>c!=null&&c.GetType().Name=="SceneEnvironmentRendering");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var bt=binding.GetType();
var runtimeField=bt.GetField("_runtimeSky",flags);var regionField=bt.GetField("_orderedRegions",flags);var fogField=bt.GetField("_hasFogSnapshot",flags);var sunField=bt.GetField("_boundSun",flags);
var oldRuntime=runtimeField.GetValue(binding);var oldRegions=regionField.GetValue(binding);var oldFogFlag=fogField.GetValue(binding);var oldSun=sunField.GetValue(binding);
float oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance,oldFogDensity=RenderSettings.fogDensity;var oldFogColor=RenderSettings.fogColor;
runtimeField.SetValue(binding,captureSky);regionField.SetValue(binding,bt.GetField("_skyRegions",flags).GetValue(binding));fogField.SetValue(binding,true);sunField.SetValue(binding,RenderSettings.sun);
var positions=new[]{new Vector3(1640,0,750),new Vector3(1600,0,793),new Vector3(1645,0,850),new Vector3(1645,0,850)};
var targets=new[]{new Vector3(1622,79,790),new Vector3(1624,82,816),new Vector3(1700,89,870),new Vector3(1607,81,804)};
var keys=new[]{"entry","clearing","exit","return"};
var rows=new List<string>();
try{
QualitySettings.renderPipeline=Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>().Single(v=>v.name=="ShadeCreekCaptureRP");
for(int i=0;i<positions.Length;i++){
var p=positions[i]; if(p.y==0)p.y=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/RedForestVisualR1/Terrain.asset").GetInterpolatedHeight(p.x/2400f,(p.z+700)/2800f)+2.2f;
var sample=bt.GetMethod("EvaluateRegionalSky",flags).Invoke(binding,new object[]{.5f,p});bt.GetMethod("ApplySkySample",flags).Invoke(binding,new[]{sample});
camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(targets[i]-p));camera.fieldOfView=60;camera.aspect=16f/9f;
var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes(folder+"/"+keys[i]+".png",tex.EncodeToPNG());rows.Add(keys[i]+" | "+p.ToString("F5")+" | "+targets[i].ToString("F5")+" | FOV60 1280x720 Editor static");}
finally{camera.targetTexture=null;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
}finally{if(creekRenderer!=null){creekRenderer.sharedMaterial=creekOriginal;UnityEngine.Object.DestroyImmediate(creekCapture);}waterRenderer.sharedMaterial=oldWater;UnityEngine.Object.DestroyImmediate(captureWater);runtimeField.SetValue(binding,oldRuntime);regionField.SetValue(binding,oldRegions);fogField.SetValue(binding,oldFogFlag);sunField.SetValue(binding,oldSun);RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.fogDensity=oldFogDensity;RenderSettings.fogColor=oldFogColor;QualitySettings.renderPipeline=oldPipeline;RenderSettings.skybox=oldSky;UnityEngine.Object.DestroyImmediate(captureSky);UnityEngine.Object.DestroyImmediate(cameraObject);}
System.IO.File.WriteAllLines(folder+"/poses.txt",rows);return rows;











