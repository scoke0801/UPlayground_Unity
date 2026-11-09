// 반복 렌더의 SSAO 잡음을 기준선으로 분리해 출수 영향과 수중 패스 적용을 검사한다.
if(EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)throw new Exception("컴파일 완료 필요");
var renderer=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
var feature=renderer.rendererFeatures.OfType<UPlayGround.Gameplay.World.UnderwaterRendererFeature>().Single();bool active=feature.isActive;
var go=new GameObject("UnderwaterRenderCheck");var camera=go.AddComponent<Camera>();var additional=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();additional.requiresDepthTexture=true;
var target=new RenderTexture(320,180,24);var texture=new Texture2D(320,180,TextureFormat.RGB24,false);var previous=RenderTexture.active;
try{
    camera.targetTexture=target;camera.nearClipPlane=.05f;camera.farClipPlane=200;
    var results=new List<string>();
    foreach(float height in new[]{78.5f,75.1f}){
        camera.transform.position=new Vector3(1000,height,750);camera.transform.LookAt(new Vector3(998,74.5f,737));
        feature.SetActive(false);camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,320,180),0,0);texture.Apply();var before=texture.GetPixels32();
        camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,320,180),0,0);texture.Apply();var baseline=texture.GetPixels32();
        feature.SetActive(true);camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,320,180),0,0);texture.Apply();var after=texture.GetPixels32();
        int changed=0;double noise=0,difference=0;
        for(int i=0;i<before.Length;i++){
            if(!baseline[i].Equals(after[i]))changed++;
            noise+=Math.Abs(before[i].r-baseline[i].r)+Math.Abs(before[i].g-baseline[i].g)+Math.Abs(before[i].b-baseline[i].b);
            difference+=Math.Abs(after[i].r-baseline[i].r)+Math.Abs(after[i].g-baseline[i].g)+Math.Abs(after[i].b-baseline[i].b);
        }
        noise/=before.Length*3;difference/=before.Length*3;
        if(height>77 && difference>noise*2+.1)throw new Exception("수면 위 변화가 반복 렌더 잡음을 초과: "+difference+" baseline="+noise);
        if(height<77 && changed<before.Length/2)throw new Exception("수중 효과가 충분한 픽셀에 적용되지 않음: "+changed);
        results.Add(height+"m changedPixels="+changed+"/"+before.Length+" meanByteDelta="+difference+" baselineNoise="+noise);
    }
    var shaders=new[]{"Hidden/UPlayGround/LakeUnderwater","UPlayGround/Water/LakeUnderside","UPlayGround/Water/SuspendedParticles"};
    foreach(string name in shaders){var shader=Shader.Find(name);if(shader==null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))throw new Exception("셰이더 오류 "+name);}
    System.IO.Directory.CreateDirectory("output/LakeEcology/Finished");System.IO.File.WriteAllLines("output/LakeEcology/Finished/render-check.txt",results);return results;
}finally{feature.SetActive(active);RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);}
