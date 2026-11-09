var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("전용 씬 필요");
string output="output/RiverBasinWorld/VisualR1/after";System.IO.Directory.CreateDirectory(output);
var terrains=Terrain.activeTerrains;Func<float,float,float> ground=(x,z)=>{var t=terrains.First(a=>x>=a.transform.position.x&&x<=a.transform.position.x+512&&z>=a.transform.position.z&&z<=a.transform.position.z+512);return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;};
var go=new GameObject("ArtR1ReviewCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.fieldOfView=62;camera.farClipPlane=1800;camera.nearClipPlane=.08f;
var previousPipeline=QualitySettings.renderPipeline;var scenePipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinPipeline.asset");if(scenePipeline!=null)QualitySettings.renderPipeline=scenePipeline;
var cameraData=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;cameraData.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;cameraData.antialiasingQuality=UnityEngine.Rendering.Universal.AntialiasingQuality.High;
var positions=new[]{new Vector3(239,0,4),new Vector3(267,0,-2),new Vector3(309,79,-15.36f),new Vector3(350,0,-4),new Vector3(332,0,-49),new Vector3(219,0,9)};
var targets=new[]{new Vector3(284,77,-13),new Vector3(325,77,-14),new Vector3(326,77,3),new Vector3(291,77,-15),new Vector3(303,75,-13),new Vector3(280,0,-8)};
var names=new[]{"01_fork","02_encounter","03_crossing","04_return","05_river","06_original_fork"};
try{for(int i=0;i<names.Length;i++){
 if(i!=2)positions[i].y=ground(positions[i].x,positions[i].z)+2;if(i==5)targets[i].y=ground(targets[i].x,targets[i].z);targets[i].y+=1.4f;
 camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);
 var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,800,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
 try{camera.Render();RenderTexture.active=rt;var texture=new Texture2D(1280,800,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();System.IO.File.WriteAllBytes(output+"/"+names[i]+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);}finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
}}finally{QualitySettings.renderPipeline=previousPipeline;UnityEngine.Object.DestroyImmediate(go);}
return output;
