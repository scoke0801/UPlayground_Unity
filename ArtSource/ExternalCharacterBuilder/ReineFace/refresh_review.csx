// 기존 씬 배치와 BodyOnly 가시성을 유지하고 얼굴·헤어 피팅만 갱신한다.
var path="Assets/01.Scenes/Tests/Equipment/Reine_P09_Review.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
bool opened=!scene.isLoaded;
if(!opened&&scene.isDirty)throw new Exception("Reine 검수 씬의 미저장 변경을 보존합니다.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset");
foreach(var root in scene.GetRootGameObjects().Where(r=>r.GetComponent<Animator>()!=null)){
profile.Apply(root);
if(root.name=="Reine_P09_BodyOnly"){
foreach(var r in root.GetComponentsInChildren<Renderer>(true)){r.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);}
foreach(var p in new[]{"Mesh/Female/Body/Female_Body_Head/Female_Face_02","Mesh/Female/Body/Female_Body_Chest","Mesh/Female/Body/Female_Body_Leg"}){
for(var t=root.transform.Find(p);t!=null&&t!=root.transform;t=t.parent){t.gameObject.SetActive(true);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}}
var hair=root.transform.Find("Mesh/Hair");hair.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(hair.gameObject);
}
foreach(var p in profile.rendererPaths)PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform.Find(p).GetComponent<SkinnedMeshRenderer>());
foreach(var f in profile.fittedMeshes)PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform.Find(f.rendererPath).GetComponent<SkinnedMeshRenderer>());
foreach(var a in profile.hairAttachments){var t=root.transform.Find(a.constraintPath);PrefabUtility.RecordPrefabInstancePropertyModifications(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t.GetComponent<UnityEngine.Animations.ParentConstraint>());}
}
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new Exception("검수 씬 저장 실패");
return "기존 검수 배치와 BodyOnly 가시성을 보존하고 얼굴·헤어 피팅 갱신";
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

