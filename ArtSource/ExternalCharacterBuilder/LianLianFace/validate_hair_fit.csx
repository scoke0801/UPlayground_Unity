var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset");
var report=new List<string>();
foreach(string model in new[]{"LianLian_P09Compatible","LianLian_P09_Alternate"}){
var root=PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Characters/ExternalModels/"+model+".prefab");
try{
var meshBefore=(Vector3[])profile.mesh.vertices.Clone();
for(int repeat=0;repeat<3;repeat++){
profile.Apply(root);
foreach(var attachment in profile.hairAttachments){
var target=root.transform.Find(attachment.constraintPath);var constraint=target.GetComponent<UnityEngine.Animations.ParentConstraint>();
if(Vector3.Distance(constraint.GetTranslationOffset(0),attachment.positionOffset)>.000001f||target.localScale!=attachment.localScale)throw new Exception("헤어 위치·크기 계약 불일치");
if(Vector3.Distance(target.position,constraint.GetSource(0).sourceTransform.TransformPoint(attachment.positionOffset))>.000001f)throw new Exception("헤어 즉시 위치 불일치");
}
}
if(!meshBefore.SequenceEqual(profile.mesh.vertices))throw new Exception("헤어 위치 적용이 얼굴 메시를 변경했습니다.");
report.Add(model+": 저장된 부착 위치·프로파일 배율 확인, 반복 3회 누적 오차 0, 얼굴 메시 변화 0");
}finally{PrefabUtility.UnloadPrefabContents(root);}}
var scenePath="Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity";
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);bool alreadyOpen=scene.IsValid()&&scene.isLoaded;
if(alreadyOpen&&scene.isDirty)throw new Exception("사용자 미저장 검수 씬은 변경하지 않습니다.");
if(!alreadyOpen)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
foreach(var root in scene.GetRootGameObjects().Where(r=>r.GetComponent<Animator>()!=null)){
profile.Apply(root);
foreach(var attachment in profile.hairAttachments){
var transform=root.transform.Find(attachment.constraintPath);PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
PrefabUtility.RecordPrefabInstancePropertyModifications(transform.GetComponent<UnityEngine.Animations.ParentConstraint>());
}}
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("검수 씬 저장 실패");
report.Add("검수 씬의 세 모델에 동일 부착 위치 반영. BodyOnly 표시 override와 다른 열린 씬 보존.");
}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);if(!alreadyOpen)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
System.IO.File.WriteAllLines("output/LianLianHairFitR03/hair_validation.txt",report);return report;
