// 승인된 표시 이름만 바꾼다. DroughtVillage와 기존 도착·발견 ID는 저장 호환을 위해 보존한다.
if (Application.isPlaying || UnityEditor.EditorApplication.isCompiling)
    throw new System.Exception("컴파일이 끝난 Edit Mode에서 실행하세요.");
const string scenePath = "Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity";
const string regionPath = "Assets/10.Datas/World/LivingWorld/Maps/DroughtVillage/RegionInfo.asset";
var original = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
bool opened = !scene.IsValid() || !scene.isLoaded;
if (!opened && scene.isDirty) throw new System.Exception("호수 씬에 미저장 변경이 있습니다.");
var region = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.MapRegionInfoSO>(regionPath);
if (region == null || UnityEditor.EditorUtility.IsDirty(region))
    throw new System.Exception("지역 데이터가 없거나 미저장 변경이 있습니다.");
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
UnityEditor.Undo.IncrementCurrentGroup();
int undo = UnityEditor.Undo.GetCurrentGroup();
try
{
    var portal = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.PortalActor>(true))
        .Single(p => p.name == "Exit_ToDrought");
    var settings = new UnityEditor.SerializedObject(portal);
    if (settings.FindProperty("_targetSceneName").stringValue != "DroughtVillage")
        throw new System.Exception("여울마을 출구 목적지가 예상과 다릅니다.");
    int index = region.portals.FindIndex(p => p.arrivalId == "livingworld.drought.from_lake");
    if (index < 0) throw new System.Exception("여울마을 입구 도착 항목이 없습니다.");
    UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { portal, region }, "여울마을 표시 이름");
    settings.FindProperty("_mapLabel").stringValue = "여울마을로 가는 길";
    settings.ApplyModifiedProperties();
    region.continentName = "여울마을";
    region.showContinentName = true;
    region.recommendedLevelMin = 1;
    region.recommendedLevelMax = 1;
    region.description = "생명의 호수 가까이, 개울을 따라 자리 잡은 작은 마을.\n가뭄으로 물이 귀해진 이곳에서 주민들은 우물과 밭을 돌보며 살아간다.";
    var entry = region.portals[index];
    entry.label = "여울마을 입구";
    region.portals[index] = entry;
    UnityEditor.EditorUtility.SetDirty(region);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))
        throw new System.Exception("호수 씬 저장에 실패했습니다.");
    UnityEditor.AssetDatabase.SaveAssetIfDirty(region);
    UnityEditor.Undo.CollapseUndoOperations(undo);
    return new { region.continentName, region.showContinentName, region.description,
        level = region.GetRecommendedLevelText(), entry.label, entry.arrivalId, entry.activationId,
        sign = settings.FindProperty("_mapLabel").stringValue };
}
catch
{
    UnityEditor.Undo.RevertAllDownToGroup(undo);
    throw;
}
finally
{
    if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    if (original.IsValid() && original.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
}
