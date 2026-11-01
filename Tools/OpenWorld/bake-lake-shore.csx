var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var resultPath = "output/OpenWorld/LakeWorldReview/navigation-application.txt";
if (System.IO.File.Exists(resultPath)) return System.IO.File.ReadAllText(resultPath);
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
    throw new Exception("저장된 월드의 Edit Mode가 필요합니다.");
var surface = GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if (surface == null || !System.IO.File.Exists("output/OpenWorld/LakeWorldReview/HarborRevisionNavigation.before.asset"))
    throw new Exception("NavMesh 또는 백업을 확인해야 합니다.");
string folder = "Assets/10.Datas/World/OpenWorld/LakeWorldReview";
if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld", "LakeWorldReview");
string assetPath = folder + "/LakeWorldNavigation.asset";
if (AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(assetPath) != null) throw new Exception("NavMesh 대상 에셋이 이미 있습니다.");
surface.BuildNavMesh();
if (surface.navMeshData == null) throw new Exception("NavMesh 생성 실패");
AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
EditorUtility.SetDirty(surface);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
string result = "기존 NavMesh를 보존하고 변경된 월드의 NavMesh를 " + assetPath + "에 생성했습니다.";
System.IO.File.WriteAllText(resultPath, result);
return result;
