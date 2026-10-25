profile.mesh=mesh;
var temporary=new List<Mesh>{mesh};
profile.fittedMeshes=profile.fittedMeshes.Select(f=>new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh{rendererPath=f.rendererPath,sourceMesh=f.sourceMesh,mesh=UnityEngine.Object.Instantiate(f.sourceMesh)}).ToArray();
foreach(var fit in profile.fittedMeshes){
var candidate=fit.mesh;temporary.Add(candidate);string name=fit.rendererPath.EndsWith("Hair_03")?"Hair_03":"Hair_11";
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead(sourceFolder+"/"+name+"_fitted.bin"))){int count=reader.ReadInt32();var points=new Vector3[count];for(int i=0;i<count;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());candidate.vertices=points;}
candidate.RecalculateNormals();candidate.RecalculateTangents();candidate.RecalculateBounds();
}
var output="output/SiuhaP09RepairR03";System.IO.Directory.CreateDirectory(output);
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
try{
foreach(int hair in new[]{3,11})foreach(bool after in new[]{false,true})foreach(float yaw in new[]{0f,75f,150f}){
var preview=new PreviewRenderUtility();IDisposable pose=null;
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+(hair==3?"Compatible":"_Alternate")+".prefab"));preview.AddSingleGO(root);root.transform.rotation=Quaternion.Euler(0,yaw,0);
if(after){foreach(var f in profile.fittedMeshes)root.transform.Find(f.rendererPath).GetComponent<SkinnedMeshRenderer>().sharedMesh=f.sourceMesh;profile.Apply(root);}
pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});pose.Dispose();pose=null;
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var target=head.position+new Vector3(0,.055f,0);var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.20f;camera.transform.position=target+Vector3.forward*2;camera.transform.LookAt(target);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.28f,.36f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.45f;
preview.BeginStaticPreview(new Rect(0,0,640,640));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Proportion_"+(after?"After":"Before")+"_"+hair+"_"+yaw+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
}finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
}finally{foreach(var m in temporary)UnityEngine.Object.DestroyImmediate(m);UnityEngine.Object.DestroyImmediate(profile);}
return "비율 후보 전후 12장 저장";
