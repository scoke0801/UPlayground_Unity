var output="output/ReineP09";System.IO.Directory.CreateDirectory(output);
foreach(float yaw in new[]{0f,35f,90f})
{
    var preview=new UnityEditor.PreviewRenderUtility();
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Spi_Reien/CharacterPrefab/reien_original_pf.prefab");
        var root=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(root);
        root.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        foreach(var skin in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
        {
            if(skin.name!="ME_chr_reien_face_01"){skin.gameObject.SetActive(false);continue;}
            for(int s=0;s<skin.sharedMesh.blendShapeCount;s++)skin.SetBlendShapeWeight(s,0);
        }
        var camera=preview.camera;camera.fieldOfView=10;
        var center=new UnityEngine.Vector3(0,1.52f,0);
        camera.transform.position=center+UnityEngine.Vector3.forward*2.1f;camera.transform.LookAt(center);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,640,800));preview.Render(true);var tex=preview.EndStaticPreview();
        System.IO.File.WriteAllBytes(output+"/Source_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
    }
    finally{preview.Cleanup();}
}
return "원본 얼굴 3방향 캡처 저장";


