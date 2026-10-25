profile.mesh=mesh;var savedFits=profile.fittedMeshes;profile.fittedMeshes=new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh[0];
var output="output/SiuhaP09RepairR04";System.IO.Directory.CreateDirectory(output);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var offsets=new[]{new Vector3(0,-.015f,0),new Vector3(0,-.025f,0),new Vector3(.008f,-.015f,0),new Vector3(-.008f,-.015f,0)};
var records=new List<string>();
try{
foreach(string name in new[]{"P09","Daon","Lili","LianLian","Siuha_R03","Position0","Position1","Position2","Position3"})foreach(float yaw in new[]{0f,75f}){
var preview=new PreviewRenderUtility();IDisposable pose=null;
try{
GameObject root;
if(name=="P09"){
root=UnityEngine.Object.Instantiate(baseRoot);var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset");var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(preset.config));config.FaceMeshProfile=null;var context=new UPlayGround.Editor.P09Builder.BuildContext(config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
}else{
string sourceName=name.StartsWith("Position")||name=="Siuha_R03"?"Siuha":name;root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/"+sourceName+"_P09Compatible.prefab"));
}
preview.AddSingleGO(root);root.transform.rotation=Quaternion.Euler(0,yaw,0);
if(name.StartsWith("Position")){
int index=int.Parse(name.Substring(8));foreach(var f in savedFits)root.transform.Find(f.rendererPath).GetComponent<SkinnedMeshRenderer>().sharedMesh=f.sourceMesh;
profile.hairAttachments=new[]{new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",positionOffset=offsets[index],localScale=Vector3.one}};profile.Apply(root);
}
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var target=head.position+new Vector3(0,.055f,0);var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.21f;camera.transform.position=target+Vector3.forward*2;camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.45f;
preview.BeginStaticPreview(new Rect(0,0,640,640));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Compare_"+name+"_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
if(yaw==0)records.Add(name+" Head.scale="+head.localScale+" Hair="+string.Join(",",root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name.StartsWith("Hair_")).Select(r=>r.name+" "+r.sharedMesh.name)));
}finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
System.IO.File.WriteAllLines(output+"/comparison.txt",records);
}finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(profile);}
return records;
