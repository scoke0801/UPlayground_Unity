var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="SaltwindHarbor"||Application.isPlaying)throw new System.Exception("항구 Edit Mode 필요");
var water=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="CoastalWater");
var renderer=water.GetComponent<Renderer>();
var scale=water.localScale; var material=renderer.sharedMaterial;
var main=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.name=="MainCamera");
var go=new GameObject("SeaCapture");go.hideFlags=HideFlags.HideAndDontSave;
var camera=go.AddComponent<Camera>();camera.CopyFrom(main);camera.enabled=false;
var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
var previous=RenderTexture.active;bool fog=RenderSettings.fog;
try {
    camera.targetTexture=target;
    var positions=new[]{new Vector3(180,10,250),new Vector3(235,13,300),new Vector3(180,80,200),new Vector3(180,10,250)};
    var aims=new[]{new Vector3(180,10,900),new Vector3(280,5,340),new Vector3(180,5,660),new Vector3(700,10,700)};
    var names=new[]{"horizon","shore","overview","diagonal"};
    for(int stage=0;stage<2;stage++) {
        water.localScale=stage==0?new Vector3(100,1,70):scale;
        renderer.sharedMaterial=stage==0?AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/SaltwindHarbor/Sea.mat"):material;
        for(int i=0;i<4;i++) {
            camera.transform.position=positions[i];camera.transform.LookAt(aims[i]);
            camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            System.IO.File.WriteAllBytes("output/HarborSea/"+(stage==0?"Before":"After")+"/runtime-settings-"+names[i]+".png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
} finally {
    water.localScale=scale;renderer.sharedMaterial=material;RenderSettings.fog=fog;
    camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(go);
}
return "Editor 8 captures: actual camera far/FOV and scene fog preserved";
