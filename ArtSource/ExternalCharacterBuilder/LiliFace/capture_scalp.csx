// 헤어가 가리는 오류를 놓치지 않도록 같은 각도에서 머리카락과 무채색 두피를 따로 확인한다.
var output="output/LiliP09ScalpFix";System.IO.Directory.CreateDirectory(output);
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Lili_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));
var profile=UnityEngine.Object.Instantiate(config.FaceMeshProfile);config.FaceMeshProfile=profile;
var candidate=Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(m=>m.name=="Lili_P09_RepairCandidate");
if(candidate!=null)profile.mesh=candidate;
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new List<string>();
try{
foreach(bool clay in new[]{false,true})
foreach(float yaw in new[]{-35f,35f,90f})
foreach(float elevation in new[]{-.18f,.18f})
{
    var preview=new PreviewRenderUtility();IDisposable pose=null;Material surface=null;
    try{
        var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(instance);
        var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=instance;
        new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
        instance.transform.rotation=Quaternion.Euler(0,yaw,0);
        pose=(IDisposable)Activator.CreateInstance(helper,new object[]{instance});
        var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
        foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        if(clay){
            surface=new Material(Shader.Find("Universal Render Pipeline/Lit"));surface.color=new Color(.6f,.6f,.6f);
            foreach(var view in instance.GetComponentsInChildren<MeshRenderer>()){
                view.sharedMaterials=Enumerable.Repeat(surface,view.sharedMaterials.Length).ToArray();
                if(view.transform.parent.name.StartsWith("Hair_"))view.enabled=false;
            }
        }
        var head=instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=8;var target=head+new Vector3(0,.115f,0);
        camera.transform.position=target+new Vector3(0,elevation,1.65f);camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
        preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new Rect(0,0,768,768));preview.Render(true);var texture=preview.EndStaticPreview();
        var file=output+"/"+(clay?"Clay":"Hair")+"_View"+yaw+"_Height"+elevation+".png";
        System.IO.File.WriteAllBytes(file,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();if(surface!=null)UnityEngine.Object.DestroyImmediate(surface);}
}
}finally{UnityEngine.Object.DestroyImmediate(profile);}
return files;
