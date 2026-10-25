// 디스크의 씬을 별도 프리뷰 씬으로 읽어 사용자의 열린 씬을 보존한다.
var path="Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{
var roots=scene.GetRootGameObjects();
var models=roots.Where(r=>r.GetComponent<Animator>()!=null).ToArray();
if(models.Length!=3)throw new Exception("검수 모델 개수 불일치");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset");
foreach(var model in models){
if(!model.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.sharedMesh==profile.mesh))throw new Exception("씬 얼굴 참조 누락");
if(model.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))!=0)throw new Exception("Missing Script");
foreach(var attachment in profile.hairAttachments){
var target=model.transform.Find(attachment.constraintPath);var constraint=target.GetComponent<UnityEngine.Animations.ParentConstraint>();
if(target.localScale!=attachment.localScale||Vector3.Distance(constraint.GetTranslationOffset(0),attachment.positionOffset)>.000001f)throw new Exception("저장된 헤어 설정 불일치: "+model.name);
}}
var body=models.Single(m=>m.name=="LianLian_P09_BodyOnly");
var visible=body.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.name).OrderBy(n=>n).ToArray();
var expected=new[]{"Female_Face_02","Female_Body_Chest","Female_Body_Leg"}.OrderBy(n=>n).ToArray();
if(!visible.SequenceEqual(expected))throw new Exception("BodyOnly 표시 부위 불일치: "+string.Join(",",visible));
if(!roots.Any(r=>r.GetComponent<Camera>()!=null)||!roots.Any(r=>r.GetComponent<Light>()!=null))throw new Exception("검수 카메라·조명 누락");
var result="디스크 씬 재로드 통과: 모델 3개, BodyOnly Face02/Chest/Leg만 표시, 저장된 헤어 위치·배율 일치, 얼굴 참조 유지, Missing Script 0, 카메라·조명 존재. 사용자 열린 씬 보존.";
System.IO.File.WriteAllText("output/LianLianHairFitR03/reload.txt",result);return result;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
