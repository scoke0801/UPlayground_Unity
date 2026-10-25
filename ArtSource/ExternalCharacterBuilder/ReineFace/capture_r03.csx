// 저장된 프리팹의 실제 스키닝을 사용해 부착 위치와 여러 조명의 얼굴 명암을 검수한다.
var output="output/ReineP09_r03";
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
foreach(string model in new[]{"Compatible","_Alternate"})foreach(float yaw in new[]{0f,55f,90f,180f}){
var preview=new PreviewRenderUtility();
try{
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Reine_P09"+model+".prefab"));preview.AddSingleGO(root);root.transform.rotation=Quaternion.Euler(0,yaw,0);
using(var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root}))helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
var center=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position+new Vector3(0,.065f,0);
var camera=preview.camera;camera.fieldOfView=6;camera.transform.position=center+Vector3.forward*3.1f;camera.transform.LookAt(center);camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.21f);
preview.ambientColor=new Color(.35f,.35f,.35f);preview.lights[0].intensity=1.2f;preview.lights[1].intensity=.35f;
foreach(string lighting in yaw==0?new[]{"Key","Front","Reverse","TopView"}:new[]{"Key"}){
preview.lights[0].transform.rotation=Quaternion.Euler(25,lighting=="Front"?180:lighting=="Reverse"?210:150,0);
if(lighting=="TopView"){camera.transform.position=center+new Vector3(0,1.5f,2.71f);camera.transform.LookAt(center);}
preview.BeginStaticPreview(new Rect(0,0,768,1024));preview.Render(true);var texture=preview.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Final"+model+"_"+yaw+"_"+lighting+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
}
}finally{preview.Cleanup();}
}
return "저장된 주/대체 프리팹: 정면·55도·90도·후면·상단, 정면/좌우 조명 14장";
