var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 월드 Edit Mode 필요");
string folder = "output/OpenWorld/ShadeCastle";
if (System.IO.File.Exists(folder + "/threshold-phase.txt")) throw new Exception("문턱 보정 실행 기록 존재: 결과부터 확인");
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var plan = (Dictionary<string, object>)json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { System.IO.File.ReadAllText("Tools/OpenWorld/shade-castle-navigation.json"), typeof(Dictionary<string, object>) });
var gate = GameObject.Find((string)plan["gatePath"]);
var surface = GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var saved = surface.navMeshData;
if (gate == null || AssetDatabase.GetAssetPath(saved) != (string)plan["navMeshAsset"]) throw new Exception("정문·NavMesh 연결 누락");
var original = gate.transform.position;
var updated = UnityEngine.Object.Instantiate(saved);
Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("그늘성 정문 문턱 접지");
Undo.RecordObject(gate.transform, "정문 기초 높이");
gate.transform.position += Vector3.up * Convert.ToSingle(plan["gateVerticalOffset"]);
Physics.SyncTransforms();
System.IO.File.WriteAllText(folder + "/threshold-phase.txt", "정문 높이 보정 / NavMesh 증분 생성 중");
try
{
    var operation = surface.UpdateNavMesh(updated);
    operation.completed += _ =>
    {
        try
        {
            surface.RemoveData();
            surface.navMeshData = updated;
            surface.AddData();
            var inside = new Vector3(1540, 61.59f, 510);
            var outside = new Vector3(1410, 61.53f, 420);
            foreach (bool reverse in new[] { false, true })
            {
                var path = new UnityEngine.AI.NavMeshPath();
                if (!UnityEngine.AI.NavMesh.CalculatePath(reverse ? outside : inside, reverse ? inside : outside, -1, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    throw new Exception("문턱 보정 후 안팎 경로 실패");
            }
            surface.RemoveData();
            EditorUtility.CopySerialized(updated, saved);
            surface.navMeshData = saved;
            surface.AddData();
            EditorUtility.SetDirty(saved);
            AssetDatabase.SaveAssetIfDirty(saved);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("문턱 보정 씬 저장 실패");
            Undo.CollapseUndoOperations(group);
            System.IO.File.WriteAllText(folder + "/threshold-phase.txt", "완료: 정문 " + original.ToString("F4") + " -> " + gate.transform.position.ToString("F4") + " / 증분 NavMesh·씬 저장");
        }
        catch (Exception error)
        {
            surface.RemoveData();
            surface.navMeshData = saved;
            surface.AddData();
            Undo.RevertAllDownToGroup(group);
            System.IO.File.WriteAllText(folder + "/threshold-phase.txt", "실패: " + error);
        }
        finally { UnityEngine.Object.DestroyImmediate(updated); }
    };
}
catch (Exception error)
{
    Undo.RevertAllDownToGroup(group);
    UnityEngine.Object.DestroyImmediate(updated);
    System.IO.File.WriteAllText(folder + "/threshold-phase.txt", "시작 실패: " + error);
    throw;
}
return "NavMesh 증분 생성 예약: threshold-phase.txt로 완료 확인";
