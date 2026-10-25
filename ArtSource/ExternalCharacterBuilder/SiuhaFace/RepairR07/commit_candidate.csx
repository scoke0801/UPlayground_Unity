// 기존 에셋은 GUID와 fileID를 유지한다. 실패하면 작업 직전의 전용 백업에서 복구한다.
const string profilePath="Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset";
const string scenePath="Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity";
const string hairFolder="Assets/08.Models/Characters/SiuhaP09/HairFitsR07";
var prefabPaths=new[]{"Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab","Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09_Alternate.prefab"};
if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Edit Mode 컴파일 완료 후 실행");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
if(!scene.isLoaded||scene.isDirty)throw new Exception("대상 Review 씬이 로드되어 있고 미저장 변경이 없어야 합니다.");
var roots=scene.GetRootGameObjects().Where(g=>prefabPaths.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g))).ToArray();
if(roots.Length!=3)throw new Exception("Review 씬의 인스턴스 수가 다릅니다.");
var candidate=Resources.FindObjectsOfTypeAll<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>().Single(p=>p.name=="Siuha_R07_Candidate");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>(profilePath);candidate.ValidateProfile();
var facePath=AssetDatabase.GetAssetPath(profile.mesh);var targets=new[]{facePath,profilePath,prefabPaths[0],prefabPaths[1],scenePath};
foreach(string path in targets){
var backup="output/SiuhaP09RepairR07/backup/"+path;
if(!System.IO.File.Exists(backup)||!System.IO.File.ReadAllBytes(path).SequenceEqual(System.IO.File.ReadAllBytes(backup)))throw new Exception("백업 이후 대상이 변경됨: "+path);
}
if(AssetDatabase.IsValidFolder(hairFolder))throw new Exception("피팅 에셋이 이미 존재합니다. 중복 저장 중단.");
// 두 프리팹에 모든 후보를 먼저 적용해 부분 저장 전에 연결 계약을 확인한다.
foreach(string path in prefabPaths){var root=PrefabUtility.LoadPrefabContents(path);try{candidate.Apply(root);}finally{PrefabUtility.UnloadPrefabContents(root);}}
var originalFace=UnityEngine.Object.Instantiate(profile.mesh);var originalProfile=UnityEngine.Object.Instantiate(profile);
try{
AssetDatabase.CreateFolder("Assets/08.Models/Characters/SiuhaP09","HairFitsR07");
var fitted=new List<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh>();
foreach(var item in candidate.fittedMeshes){
var copy=UnityEngine.Object.Instantiate(item.mesh);copy.hideFlags=HideFlags.None;copy.name=item.sourceMesh.name+"_Siuha";
var path=hairFolder+"/"+copy.name+".asset";AssetDatabase.CreateAsset(copy,path);
fitted.Add(new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh{rendererPath=item.rendererPath,sourceMesh=item.sourceMesh,mesh=copy});
}
string faceName=profile.mesh.name;EditorUtility.CopySerialized(candidate.mesh,profile.mesh);profile.mesh.name=faceName;profile.mesh.hideFlags=HideFlags.None;EditorUtility.SetDirty(profile.mesh);AssetDatabase.SaveAssetIfDirty(profile.mesh);
profile.fittedMeshes=fitted.ToArray();profile.hairAttachments=candidate.hairAttachments;profile.ValidateProfile();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
foreach(string path in prefabPaths){var root=PrefabUtility.LoadPrefabContents(path);try{profile.Apply(root);bool success;PrefabUtility.SaveAsPrefabAsset(root,path,out success);if(!success)throw new Exception("프리팹 저장 실패: "+path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
foreach(var root in roots){
profile.Apply(root);
foreach(string path in profile.rendererPaths.Concat(profile.fittedMeshes.Select(f=>f.rendererPath)))PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform.Find(path).GetComponent<SkinnedMeshRenderer>());
foreach(var attachment in profile.hairAttachments){var target=root.transform.Find(attachment.constraintPath);PrefabUtility.RecordPrefabInstancePropertyModifications(target);PrefabUtility.RecordPrefabInstancePropertyModifications(target.GetComponent<UnityEngine.Animations.ParentConstraint>());}
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath))throw new Exception("Review 씬 저장 실패");
SceneView.RepaintAll();return "얼굴·전체 헤어 13종·프로필·프리팹 2개·Review 인스턴스 3개 저장. 기존 GUID 유지.";
}catch{
// 로드된 객체도 먼저 복구해 이후 Unity 저장이 백업 복구를 덮지 않도록 한다.
EditorUtility.CopySerialized(originalFace,profile.mesh);EditorUtility.CopySerialized(originalProfile,profile);
foreach(string path in targets){System.IO.File.Copy("output/SiuhaP09RepairR07/backup/"+path,path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
// 새 폴더는 실패 조사용으로 남기며, 원본 복구와 무관한 에셋 삭제는 수행하지 않는다.
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
var restoredScene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(restoredScene);
throw;
}finally{UnityEngine.Object.DestroyImmediate(originalFace);UnityEngine.Object.DestroyImmediate(originalProfile);}
