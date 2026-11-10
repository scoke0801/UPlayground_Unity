// 낙구 연결 전후에 동일한 다섯 구도를 사용한다.
string output="output/RiverBasinWorld/VisualR3/lip-continuity/before";
System.IO.Directory.CreateDirectory(output);
var go=new GameObject("낙구 검수 카메라"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=55;camera.farClipPlane=1800;camera.nearClipPlane=.08f;
var previousPipeline=QualitySettings.renderPipeline;
QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinPipeline.asset");
var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;
var positions=new[]{new Vector3(307,80,-121),new Vector3(307,72,-119),new Vector3(305,64,-137),new Vector3(324,72,-117),new Vector3(307,66,-159)};
var targets=new[]{new Vector3(307,68,-107),new Vector3(307,68,-109),new Vector3(307,65,-111),new Vector3(307,67,-110),new Vector3(307,56,-147)};
var names=new[]{"01_top","02_lip","03_front","04_side","05_lower"};
try{for(int i=0;i<positions.Length;i++){
 camera.transform.position=positions[i];camera.transform.rotation=Quaternion.LookRotation(targets[i]-positions[i]);
 var rt=RenderTexture.GetTemporary(1280,800,24);var previousTarget=RenderTexture.active;Texture2D texture=null;
 try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(1280,800,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);System.IO.File.WriteAllBytes(output+"/"+names[i]+".png",texture.EncodeToPNG());}
 finally{RenderTexture.active=previousTarget;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
}}finally{QualitySettings.renderPipeline=previousPipeline;UnityEngine.Object.DestroyImmediate(go);}
return output;
