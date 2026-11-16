// 임시 URP 카메라로 촬영하고 씬·품질 설정을 보존한다.
System.IO.Directory.CreateDirectory("output/lake-waterfall/r3");
var go=new GameObject("LakeWaterfallReviewCamera"){hideFlags=HideFlags.HideAndDontSave};
var camera=go.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.farClipPlane=2000;camera.nearClipPlane=.08f;
var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.requiresDepthTexture=true;data.requiresColorTexture=true;
var shots=new[]{
new[]{new Vector3(1111,96,1043),new Vector3(1144,88,1028)},
new[]{new Vector3(1143.5f,80.1f,1027.8f),new Vector3(1147,80,1032.4f)},
new[]{new Vector3(1146,80.7f,1032.3f),new Vector3(1139,80,1020)}
};
var names=new[]{"after-side","cave-chest","cave-exit"};
try {
    foreach(var particle in GameObject.Find("LakeWaterfall_Northeast").GetComponentsInChildren<ParticleSystem>())particle.Simulate(3,true,true,true);
    for(int i=0;i<shots.Length;i++) {
        camera.transform.position=shots[i][0];camera.transform.LookAt(shots[i][1]);
        var rt=RenderTexture.GetTemporary(1400,1000,24);var previous=RenderTexture.active;
        try {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1400,1000),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("output/lake-waterfall/r3/"+names[i]+".png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        } finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
    }
} finally {UnityEngine.Object.DestroyImmediate(go);}
return names;
