// 저장된 두 프리팹을 같은 카메라·조명으로 비교한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 모드가 필요합니다.");
string output="output/P09DaonPortrait";System.IO.Directory.CreateDirectory(output);
foreach(bool after in new[]{false,true})
foreach(float yaw in new[]{0f,35f,90f})
foreach(bool side in new[]{false,true})
{
    var preview=new PreviewRenderUtility();
    try
    {
        string path=after?"Assets/03.Prefabs/Characters/EquipmentReview/DaonPortrait/Npc_Daon_Portrait.prefab":"Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));root.SetActive(true);preview.AddSingleGO(root);
        var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
        if(!after)
        {
            face.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/08.Models/Characters/DaonHead/Female_Face_02.asset");
            var slots=face.sharedMaterials;
            slots[0]=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonHead/Daon_Head_Skin.mat");
            slots[1]=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonFace/Daon_Eye_Hazel.mat");
            face.sharedMaterials=slots;
        }
        var target=face.bones.Single(b=>b.name=="Head").position+Vector3.up*.075f;
        var camera=preview.camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.23f,.25f);
        camera.fieldOfView=30;camera.nearClipPlane=.01f;camera.farClipPlane=100;
        camera.transform.position=target+Quaternion.Euler(0,yaw,0)*root.transform.forward*.65f;camera.transform.LookAt(target);
        preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=camera.transform.rotation*Quaternion.Euler(25,side?-80:-25,0);
        preview.lights[1].intensity=side?.2f:.8f;preview.ambientColor=side?new Color(.25f,.25f,.25f):new Color(.55f,.55f,.55f);
        for(int pass=0;pass<2;pass++)
        {
            preview.BeginStaticPreview(new Rect(0,0,768,960));preview.Render(true);var image=preview.EndStaticPreview();
            try{if(pass==1)System.IO.File.WriteAllBytes(output+"/"+(after?"After":"Before")+"_"+yaw+(side?"_SideLight":"_Soft")+".png",image.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
    }
    finally{preview.Cleanup();}
}
return "요청한 각도·조명의 비교 캡처 생성";
