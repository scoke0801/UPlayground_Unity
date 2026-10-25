const string path="Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened=!scene.isLoaded;
if(scene.isLoaded&&scene.isDirty)throw new Exception("검수 씬에 미저장 사용자 변경이 있습니다.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
var before=scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Siuha_P09")).ToDictionary(g=>g.name,g=>g.transform.localToWorldMatrix);
try{
var review=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Siuha_P09_BodyOnly");
if(review==null){
review=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab"),scene);
review.name="Siuha_P09_BodyOnly";review.transform.position=new Vector3(1.95f,0,0);
var paths=new[]{"Mesh/Female/Body/Female_Body_Head/Female_Face_02","Mesh/Female/Body/Female_Body_Chest","Mesh/Female/Body/Female_Body_Leg"};
foreach(var renderer in review.GetComponentsInChildren<Renderer>(true))renderer.gameObject.SetActive(false);
foreach(string bodyPath in paths){var target=review.transform.Find(bodyPath);if(target==null)throw new Exception("기본 Body 경로 누락");for(var t=target;t!=null&&t!=review.transform;t=t.parent)t.gameObject.SetActive(true);}
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
using(var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{review}))helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
foreach(var t in review.GetComponentsInChildren<Transform>(true)){PrefabUtility.RecordPrefabInstancePropertyModifications(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
}
foreach(var pair in before)if(scene.GetRootGameObjects().Single(g=>g.name==pair.Key).transform.localToWorldMatrix!=pair.Value)throw new Exception("기존 모델 배치 변경");
var visible=review.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.name).OrderBy(n=>n).ToArray();
if(!visible.SequenceEqual(new[]{"Female_Body_Chest","Female_Body_Leg","Female_Face_02"}))throw new Exception("기본 몸체 외 렌더러 활성화: "+string.Join(",",visible));
var camera=scene.GetRootGameObjects().Select(g=>g.GetComponent<Camera>()).First(c=>c!=null);
camera.orthographic=true;camera.orthographicSize=1.05f;camera.transform.position=new Vector3(.65f,1.05f,4.2f);camera.transform.LookAt(new Vector3(.65f,.82f,0));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new Exception("검수 씬 저장 실패");
var loaded=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{
var restored=loaded.GetRootGameObjects().Single(g=>g.name=="Siuha_P09_BodyOnly");
var names=restored.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.name).OrderBy(n=>n).ToArray();
if(!names.SequenceEqual(visible))throw new Exception("저장 후 기본 몸체 가시성 불일치");
var expected=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset").mesh;
if(restored.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Female_Face_02").sharedMesh!=expected)throw new Exception("씬 얼굴 참조 불일치");
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(loaded);}
if(SceneView.lastActiveSceneView!=null){SceneView.lastActiveSceneView.rotation=Quaternion.Euler(5,180,0);SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(.65f,.82f,0),new Vector3(3.7f,1.8f,.5f)),true);}
SceneView.RepaintAll();
var report="의상 모델 2개 배치 보존, BodyOnly(Head/Chest/Leg) 추가. 카메라에 3개 모델 배치. 저장 후 PreviewScene 재로드로 3개 가시성·얼굴 참조 확인.";
System.IO.File.WriteAllText("ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR03/scene_validation.txt",report);return report;
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
