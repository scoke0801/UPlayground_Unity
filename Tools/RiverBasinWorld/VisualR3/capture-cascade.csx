// R3b 폭포 검수 구도: 사용자 지적 구도(다리 위 하류 조망), 하류 정면, 측면 근경.
string OUT="cascade";
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string output="output/RiverBasinWorld/VisualR3/"+OUT;System.IO.Directory.CreateDirectory(output);
var go=new GameObject("cam"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.fieldOfView=62;camera.farClipPlane=1800;camera.nearClipPlane=.08f;
var prevP=QualitySettings.renderPipeline;QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinPipeline.asset");
var cd=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cd.renderPostProcessing=true;cd.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
var shots=new[]{new[]{new Vector3(307,96,-60),new Vector3(307,58,-140)},new[]{new Vector3(305,58,-178),new Vector3(307,60,-120)},new[]{new Vector3(322,66,-128),new Vector3(305,63,-110)},new[]{new Vector3(296,62,-166),new Vector3(308,55,-147)}};
var names=new[]{"c1_overview","c2_front","c3_side","c4_lower"};
try{for(int i=0;i<shots.Length;i++){camera.transform.position=shots[i][0];camera.transform.rotation=Quaternion.LookRotation(shots[i][1]-shots[i][0]);var rt=RenderTexture.GetTemporary(1280,800,24);camera.targetTexture=rt;camera.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tx=new Texture2D(1280,800,TextureFormat.RGBA32,false);tx.ReadPixels(new Rect(0,0,1280,800),0,0);System.IO.File.WriteAllBytes(output+"/"+names[i]+".png",tx.EncodeToPNG());RenderTexture.active=prev;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tx);}}finally{QualitySettings.renderPipeline=prevP;UnityEngine.Object.DestroyImmediate(go);}
return output;
