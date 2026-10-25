var path="Assets/01.Scenes/Tests/Equipment/Bokusei_P09_Review.unity";
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened=!scene.IsValid()||!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
var preview=new PreviewRenderUtility();var poses=new List<IDisposable>();
try{
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Male_Idle.asset");
var instances=scene.GetRootGameObjects().Where(o=>o.GetComponent<Animator>()!=null).ToArray();
if(instances.Length!=2)throw new Exception("검수 씬 모델 수 불일치");
foreach(var original in instances){
var root=UnityEngine.Object.Instantiate(original);preview.AddSingleGO(root);
var face=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Male_Face_01");
if(face.sharedMesh!=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset").mesh)throw new Exception("씬 재로드 후 얼굴 유실");
var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});poses.Add(pose);helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.name=="P09 Pose Preview")f.sharedMesh.RecalculateBounds();
}
var camera=preview.camera;camera.fieldOfView=30;camera.transform.position=new Vector3(0,.95f,4.3f);camera.transform.LookAt(new Vector3(0,.85f,0));camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.21f);
preview.ambientColor=new Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
preview.BeginStaticPreview(new Rect(0,0,1280,960));preview.Render(true);var tex=preview.EndStaticPreview();System.IO.File.WriteAllBytes("output/BokuseiP09/Review.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
return new{scene=path,reloadedModels=instances.Length,capture="output/BokuseiP09/Review.png"};
}finally{foreach(var pose in poses)pose.Dispose();preview.Cleanup();UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
