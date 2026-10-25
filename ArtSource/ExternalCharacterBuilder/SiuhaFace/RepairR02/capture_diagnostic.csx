var output="output/SiuhaP09RepairR02";System.IO.Directory.CreateDirectory(output);
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));
foreach(UPlayGround.Editor.P09Builder.BuilderArmorSlot slot in Enum.GetValues(typeof(UPlayGround.Editor.P09Builder.BuilderArmorSlot)))config.ArmorSelections.Set(slot,null);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
foreach(string mode in new[]{"Head","Neck"})foreach(float yaw in new[]{0f,55f,110f}){
var preview=new PreviewRenderUtility();IDisposable pose=null;
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));preview.AddSingleGO(root);
var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(skin.name.StartsWith("Hair_"))skin.gameObject.SetActive(false);
root.transform.rotation=Quaternion.Euler(0,yaw,0);pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var target=head.position+new Vector3(0,mode=="Head"?.055f:-.012f,0);
var camera=preview.camera;camera.fieldOfView=mode=="Head"?11:6;camera.transform.position=target+new Vector3(0,-.045f,1.65f);camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
preview.ambientColor=new Color(.28f,.28f,.28f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(20,100,0);preview.lights[1].intensity=.15f;
preview.BeginStaticPreview(new Rect(0,0,768,768));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Bare"+mode+"_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
}finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return "두피·목 근접 측광 6장 저장";
