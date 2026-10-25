var path="Assets/01.Scenes/Tests/Equipment/Reine_P09_Review.unity";
// 현재 작업 씬을 보존하고 저장된 씬을 별도 프리뷰로 검증한다.
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{
var roots=scene.GetRootGameObjects();
var models=roots.Where(r=>r.GetComponent<Animator>()!=null).ToArray();
if(models.Length!=3)throw new Exception("검수 모델 개수 불일치");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset");
foreach(var model in models){
if(!model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.sharedMesh==profile.mesh))throw new Exception("씬 얼굴 참조 누락");
if(model.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))!=0)throw new Exception("Missing Script");
}
if(!roots.Any(r=>r.GetComponent<Camera>()!=null)||!roots.Any(r=>r.GetComponent<Light>()!=null))throw new Exception("검수 카메라·조명 누락");
var body= models.Single(m=>m.name=="Reine_P09_BodyOnly");if(body.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)!=3)throw new Exception("기본 몸체 렌더러 개수 불일치");
var result="검수 씬 저장 후 재로드 통과: 모델 3개, 얼굴 프로파일 참조 유지, Missing Script 0, 카메라·조명 존재. 기존 활성 씬 보존.";
System.IO.File.WriteAllText("output/ReineP09/reload.txt",result);return result;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}


