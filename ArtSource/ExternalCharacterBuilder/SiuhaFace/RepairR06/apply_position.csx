// Hair11을 뒤로 20mm, 아래로 4mm 이동하여 이마의 뜬 간격을 줄인다. 스케일과 메시는 보존한다.
const string scenePath="Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity";
const string attachmentPath="Mesh/Hair/Hair_11/Skeleton_P09_Hair_11/Constraint_Head";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Edit Mode 컴파일 완료 후 실행해야 합니다.");
if(scene.isLoaded&&scene.isDirty)throw new Exception("대상 씬의 미저장 변경을 먼저 보존해야 합니다.");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
profile.ValidateProfile();
var index=Array.FindIndex(profile.hairAttachments,a=>a.constraintPath==attachmentPath);
if(index<0||profile.hairAttachments[index].localScale!=Vector3.one)throw new Exception("헤어 부착 계약 불일치");
var offset=new Vector3(.020f,.008f,0);
Action<GameObject> apply=root=>{
    var target=root.transform.Find(attachmentPath);
    var constraint=target.GetComponent<UnityEngine.Animations.ParentConstraint>();
    if(target.localScale!=Vector3.one||root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localScale!=Vector3.one)throw new Exception("기존 머리·헤어 스케일 불일치");
    constraint.SetTranslationOffset(0,offset);
    target.position=constraint.GetSource(0).sourceTransform.TransformPoint(offset);
    if(PrefabUtility.IsPartOfPrefabInstance(target)){PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
};
var paths=new[]{"Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab","Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09_Alternate.prefab"};
foreach(var path in paths){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null||asset.transform.Find(attachmentPath)==null)throw new Exception("대상 프리팹 누락: "+path);}
profile.hairAttachments[index].positionOffset=offset;
EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
foreach(var path in paths){var root=PrefabUtility.LoadPrefabContents(path);try{apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
    var roots=scene.GetRootGameObjects().Where(r=>r.name.StartsWith("Siuha_P09")).ToArray();
    if(roots.Length!=3)throw new Exception("검수 씬 모델 수 불일치");
    foreach(var root in roots)apply(root);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath))throw new Exception("검수 씬 저장 실패");
    SceneView.RepaintAll();
}finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
return "Siuha Hair11 뒤로 20mm, 아래로 4mm 이동. 프로필·프리팹 2개·씬 3개 저장. 스케일·메시·Hair03 보존.";
