var folder="output/LianLianHairFitR03/Diagnosis";System.IO.Directory.CreateDirectory(folder);
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var files=new List<string>();
foreach(string model in new[]{"LianLian_P09Compatible","LianLian_P09_Alternate","LianLian_P09_BodyOnly"})
foreach(float yaw in new[]{0f,70f,110f,180f}){
var preview=new PreviewRenderUtility();IDisposable pose=null;
try{
var source=scene.GetRootGameObjects().Single(r=>r.name==model);
var root=UnityEngine.Object.Instantiate(source);root.transform.position=Vector3.zero;root.transform.rotation=Quaternion.Euler(0,yaw,0);preview.AddSingleGO(root);
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
var target=head.position+Vector3.up*.08f;
preview.camera.orthographic=true;preview.camera.orthographicSize=.13f;
preview.camera.transform.position=target+new Vector3(0,.04f,.7f);preview.camera.transform.LookAt(target);
preview.camera.nearClipPlane=.005f;preview.camera.farClipPlane=5;
preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.20f,.30f,.42f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
preview.BeginStaticPreview(new Rect(0,0,1024,1024));preview.Render(true);var texture=preview.EndStaticPreview();var file=folder+"/"+model+"_"+yaw+".png";
System.IO.File.WriteAllBytes(file,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);files.Add(file);
}finally{if(pose!=null)pose.Dispose();preview.Cleanup();}}
return files;
