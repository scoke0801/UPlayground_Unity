var output="output/SiuhaP09RepairR03";System.IO.Directory.CreateDirectory(output);
var saved=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var scales=new[]{Vector3.one,new Vector3(.88f,1f,.90f),new Vector3(.82f,.96f,.90f),new Vector3(.88f,.98f,.95f)};
var offsets=new[]{Vector3.zero,new Vector3(.012f,0,0),new Vector3(.018f,.003f,0),new Vector3(.018f,0,0)};
for(int version=0;version<scales.Length;version++)foreach(float yaw in new[]{0f,75f}){
var preview=new PreviewRenderUtility();var profile=UnityEngine.Object.Instantiate(saved);IDisposable pose=null;
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab"));preview.AddSingleGO(root);root.transform.rotation=Quaternion.Euler(0,yaw,0);
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
profile.hairAttachments=new[]{new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",localScale=scales[version],positionOffset=offsets[version]}};profile.Apply(root);
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var target=head.position+new Vector3(0,.055f,0);var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.20f;camera.transform.position=target+Vector3.forward*2;camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.45f;
preview.BeginStaticPreview(new Rect(0,0,640,640));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Attachment_"+version+"_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
}finally{if(pose!=null)pose.Dispose();UnityEngine.Object.DestroyImmediate(profile);preview.Cleanup();}
}
return "부착 위치·폭 4후보 × 2각도 저장";
