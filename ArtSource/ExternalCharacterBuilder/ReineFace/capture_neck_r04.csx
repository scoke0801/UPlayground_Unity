// 미저장 검수 씬과 분리된 인스턴스에서 장비 없는 목과 두피를 촬영한다.
var output="output/ReineP09_r04";
System.IO.Directory.CreateDirectory(output);
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));
foreach(UPlayGround.Editor.P09Builder.BuilderArmorSlot slot in Enum.GetValues(typeof(UPlayGround.Editor.P09Builder.BuilderArmorSlot)))config.ArmorSelections.Set(slot,null);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new List<string>();
foreach(int hair in new[]{3})
foreach(float pitch in new[]{0f})
foreach(float yaw in new[]{0f,90f,150f})
{
    var preview=new PreviewRenderUtility();IDisposable pose=null;
    try
    {
        var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(instance);
        config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+hair.ToString("00")+".asset");
        var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=instance;
        new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
        instance.transform.Find("Mesh/Hair").gameObject.SetActive(false);instance.transform.rotation=Quaternion.Euler(0,yaw,0);
        pose=(IDisposable)Activator.CreateInstance(helper,new object[]{instance});
        var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
        var headBone=instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        headBone.rotation=Quaternion.AngleAxis(pitch,instance.transform.right)*headBone.rotation;
        foreach(var constraint in instance.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))helper.GetMethod("EvaluateConstraint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{constraint});
        foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>()){
            var filter=skin.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.name=="P09 Pose Preview");
            if(filter!=null)skin.BakeMesh(filter.sharedMesh);
        }
        foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=12;var target=head+new Vector3(0,.055f,0);
        camera.transform.position=target+new Vector3(0,-.12f,1.65f);camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
        preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new Rect(0,0,768,768));preview.Render(true);var texture=preview.EndStaticPreview();
        var file=output+"/Bald_Hair"+hair+"_Pitch"+pitch+"_View"+yaw+".png";System.IO.File.WriteAllBytes(file,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return files;




