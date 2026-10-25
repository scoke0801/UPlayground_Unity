// 현재 헤어와 얼굴 형태를 고정하고 눈 텍스처·피부 재질을 분리 비교한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 모드가 필요합니다.");
string output="output/P09DaonFace";System.IO.Directory.CreateDirectory(output);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab");
foreach(string variant in new[]{"Before","Eyes","Skin","Combined","Applied"})
foreach(bool side in new[]{false,true})
{
    var preview=new PreviewRenderUtility();var materials=new List<Material>();
    try
    {
        var root=UnityEngine.Object.Instantiate(prefab);root.SetActive(true);preview.AddSingleGO(root);
        var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
        var slots=face.sharedMaterials;
        if(variant!="Applied")
        {
            slots[0]=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Face_Toon.mat");
            slots[1]=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Common/Eye/P09_Eye_Yellow.mat");
        }
        if(variant=="Eyes"||variant=="Combined")
        {
            var eye=new Material(slots[1]);materials.Add(eye);
            eye.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/Characters/DaonFace/Daon_Eye_Hazel.png"));
            eye.SetVector("_MainTexHSVG",new Vector4(0f,.78f,1f,1f));
            slots[1]=eye;
        }
        if(variant=="Skin"||variant=="Combined")
        {
            var skin=new Material(slots[0]);materials.Add(skin);
            skin.SetColor("_Color",new Color(1f,.97f,.94f,1f));
            skin.SetColor("_ShadowColor",new Color(.90f,.75f,.73f,1f));
            skin.SetFloat("_ShadowBorder",.46f);skin.SetFloat("_ShadowBlur",.5f);
            skin.SetFloat("_BumpScale",.5f);skin.SetFloat("_UseReflection",0f);
            skin.SetFloat("_UseRim",0f);skin.SetFloat("_UseRimShade",0f);
            slots[0]=skin;
        }
        face.sharedMaterials=slots;
        var head=face.bones.Single(b=>b.name=="Head");var target=head.position+Vector3.up*.075f;
        var camera=preview.camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.23f,.25f);
        camera.fieldOfView=30;camera.nearClipPlane=.01f;camera.farClipPlane=100;
        camera.transform.position=target+root.transform.forward*.65f;camera.transform.LookAt(target);
        preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=camera.transform.rotation*Quaternion.Euler(25,side?-80:-25,0);
        preview.lights[1].intensity=side?.2f:.8f;preview.ambientColor=side?new Color(.25f,.25f,.25f):new Color(.55f,.55f,.55f);
        for(int pass=0;pass<2;pass++)
        {
            preview.BeginStaticPreview(new Rect(0,0,768,960));preview.Render(true);var image=preview.EndStaticPreview();
            try{if(pass==1)System.IO.File.WriteAllBytes(output+"/"+variant+(side?"_SideLight":"_Soft")+".png",image.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
    }
    finally{preview.Cleanup();foreach(var material in materials)UnityEngine.Object.DestroyImmediate(material);}
}
return "얼굴 비교·실제 적용본 10장 생성. 저장된 재질·프리팹은 변경하지 않음.";
