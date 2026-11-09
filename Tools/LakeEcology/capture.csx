// 임시 카메라로 수중·수면 위를 렌더하며 기존 카메라와 날씨 상태는 바꾸지 않는다.
if(EditorApplication.isCompiling)throw new Exception("컴파일 완료 후 실행");
System.IO.Directory.CreateDirectory("output/LakeEcology/Finished");
var cameraObject=new GameObject("LakeEcologyCapture");var camera=cameraObject.AddComponent<Camera>();
var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
var positions=new[]{new Vector3(1000,75.1f,750),new Vector3(1000,78.5f,750),new Vector3(1008,75.5f,764)};
var aims=new[]{new Vector3(998,74.5f,737),new Vector3(998,75,737),new Vector3(1011,75.5f,759)};
var names=new[]{"underwater","above-water","fish"};
try{
    camera.nearClipPlane=.05f;camera.farClipPlane=350;camera.fieldOfView=65;camera.targetTexture=target;
    var additional=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();additional.requiresDepthTexture=true;
    for(int i=0;i<positions.Length;i++){
        camera.transform.position=positions[i];camera.transform.LookAt(aims[i]);camera.Render();RenderTexture.active=target;
        pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();System.IO.File.WriteAllBytes("output/LakeEcology/Finished/"+names[i]+".png",pixels.EncodeToPNG());
    }
    return names;
}finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraObject);}
