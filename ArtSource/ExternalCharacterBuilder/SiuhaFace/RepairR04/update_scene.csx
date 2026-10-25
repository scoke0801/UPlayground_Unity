const string path="Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
if(scene.isLoaded&&scene.isDirty)throw new Exception("대상 씬에 미저장 사용자 변경이 있습니다.");
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
var original=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
foreach(var root in scene.GetRootGameObjects().Where(g=>g.name.StartsWith("Siuha_P09"))){
foreach(string name in new[]{"Hair_03","Hair_11"}){var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);skin.sharedMesh=original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name).sharedMesh;PrefabUtility.RecordPrefabInstancePropertyModifications(skin);}
foreach(var attachment in profile.hairAttachments){var c=root.transform.Find(attachment.constraintPath).GetComponent<UnityEngine.Animations.ParentConstraint>();c.SetTranslationOffset(0,attachment.positionOffset);c.transform.localScale=Vector3.one;c.transform.position=c.GetSource(0).sourceTransform.TransformPoint(attachment.positionOffset);PrefabUtility.RecordPrefabInstancePropertyModifications(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c.transform);}
if(root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localScale!=Vector3.one)throw new Exception("머리 본 스케일 불일치");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new Exception("씬 저장 실패");
return "기존 3개 모델·BodyOnly 가시성·배치 보존. 헤어 원본 연결과 위치 수정 저장.";
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
