string stage="Before";var output="output/HichiBareNeckFix";System.IO.Directory.CreateDirectory(output);
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
foreach(float yaw in new[]{0f,55f,90f,125f,180f}){
    var preview=new PreviewRenderUtility();IDisposable pose=null;
    try{
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(root);
        var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));
        foreach(UPlayGround.Editor.P09Builder.BuilderArmorSlot slot in Enum.GetValues(typeof(UPlayGround.Editor.P09Builder.BuilderArmorSlot)))config.ArmorSelections.Set(slot,null);
        var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
        foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(r.name=="Female_Body_Chest"||r.name=="Female_Body_Arm")r.gameObject.SetActive(true);
        root.transform.rotation=Quaternion.Euler(0,yaw,0);pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
        var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
        var camera=preview.camera;var target=head+new Vector3(0,-.015f,0);camera.fieldOfView=10;camera.transform.position=target+Vector3.forward*2.3f;camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.21f);
        preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new Rect(0,0,900,900));preview.Render(true);var image=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/"+stage+"_"+yaw+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
    }finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return stage+" 의상 없는 목·어깨 5방향";
