// 임시 URP 카메라로 촬영하고 씬·품질 설정을 보존한다.
System.IO.Directory.CreateDirectory("output/lake-waterfall/r2");
var go=new GameObject("LakeWaterfallReviewCamera"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.farClipPlane=2000;camera.nearClipPlane=.08f;
var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.requiresDepthTexture=true;data.requiresColorTexture=true;
var shots=new[]{
    new[]{new Vector3(1090,94,970),new Vector3(1143,88,1024)},
    new[]{new Vector3(1117,80,990),new Vector3(1143,89,1026)},
    new[]{new Vector3(1153,115,1008),new Vector3(1142,87,1025)},
    new[]{new Vector3(1111,96,1043),new Vector3(1144,88,1028)},
    new[]{new Vector3(1159,119,1048),new Vector3(1157,101,1054)}
};
var names=new[]{"after-vista","after-front","after-overhead","after-side","source-head"};
try {
    foreach(var particle in GameObject.Find("LakeWaterfall_Northeast").GetComponentsInChildren<ParticleSystem>())particle.Simulate(3,true,true,true);
    for(int i=0;i<shots.Length;i++) {
        camera.transform.position=shots[i][0];camera.transform.LookAt(shots[i][1]);
        var rt=RenderTexture.GetTemporary(1400,1000,24);var previous=RenderTexture.active;
        try {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1400,1000),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("output/lake-waterfall/r2/"+names[i]+".png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        } finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
    }
} finally {UnityEngine.Object.DestroyImmediate(go);}
return names;
