// 성벽을 관통하던 우선 이동 영역을 실제 북문·서쪽 외곽 통로에 맞춘 일회성 씬 저작 기록이다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (s, t) => parse.Invoke(null, new object[] { s, t });
var plan = (Dictionary<string, object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/shade-castle-navigation.json"), typeof(Dictionary<string, object>));
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != (string)plan["scene"])
    throw new Exception("저장된 월드 Edit Mode 필요");
var world = GameObject.Find("AuthoredEnvironment");
var preferences = world.transform.Find("NavigationPreferences");
if (preferences.Find("그늘성 정문 우회 길") != null) throw new Exception("이미 적용됨: 검사 기록부터 확인");
var center = (float[])read(plan["replacedVolumeCenter"].ToString(), typeof(float[]));
var previous = preferences.GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifierVolume>().Single(v => Vector3.Distance(v.transform.position, new Vector3(center[0], center[1], center[2])) < .01f);
var points = (float[][])read(plan["points"].ToString(), typeof(float[][]));
var surface = world.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var previousData = surface.navMeshData;
string navPath = (string)plan["navMeshAsset"];
if (AssetDatabase.LoadMainAssetAtPath(navPath) != null || System.IO.File.Exists(navPath)) throw new Exception("출력 NavMesh 존재: 덮어쓰기 금지");
Func<string> identities = () => string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c => c.Guid + "|" + c.transform.position.ToString("F4")).OrderBy(s => s));
string before = identities();
string folder = "output/OpenWorld/ShadeCastle";
System.IO.Directory.CreateDirectory(folder + "/backup");
string backup = folder + "/backup/LakeVillageWorld.before.unity";
if (System.IO.File.Exists(backup)) throw new Exception("백업 존재: 이전 실행 상태 확인");
System.IO.File.Copy(scene.path, backup);
System.IO.File.Copy(scene.path + ".meta", backup + ".meta");
System.IO.File.WriteAllText(folder + "/identities-before.txt", before);
Undo.IncrementCurrentGroup();
int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("그늘성 정문 경로 연결");
var report = new System.Text.StringBuilder();
try
{
    Undo.RecordObject(surface, "그늘성 NavMesh 교체");
    Undo.DestroyObjectImmediate(previous.gameObject);
    var root = new GameObject("그늘성 정문 우회 길");
    Undo.RegisterCreatedObjectUndo(root, "그늘성 우선 이동 영역");
    root.transform.SetParent(preferences, false);
    for (int i = 1; i < points.Length; i++)
    {
        var a = new Vector3(points[i - 1][0], center[1], points[i - 1][1]);
        var b = new Vector3(points[i][0], center[1], points[i][1]);
        var go = new GameObject("PreferredGround_" + i);
        go.transform.SetParent(root.transform, false);
        go.transform.SetPositionAndRotation((a + b) * .5f, Quaternion.LookRotation(b - a));
        var volume = go.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
        volume.area = UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");
        volume.size = new Vector3(Convert.ToSingle(plan["width"]), Convert.ToSingle(plan["height"]), Vector3.Distance(a, b) + Convert.ToSingle(plan["width"]));
    }
    Physics.SyncTransforms();
    System.IO.File.WriteAllText(folder + "/repair-phase.txt", "우선 이동 영역 수정 / NavMesh 생성 시작");
    surface.BuildNavMesh();
    if (surface.navMeshData == null) throw new Exception("NavMesh 생성 실패");
    var terrain = world.GetComponentInChildren<Terrain>();
    var regional = (Dictionary<string, object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/regional-cover-plan.json"), typeof(Dictionary<string, object>));
    var routes = (Dictionary<string, object>[])read(regional["routes"].ToString(), typeof(Dictionary<string, object>[]));
    int index = 0, passed = 0;
    foreach (var route in routes)
    {
        var line = (float[][])read(route["points"].ToString(), typeof(float[][]));
        var hits = new List<Vector3>();
        foreach (var point in line)
        {
            var p = new Vector3(point[0], 0, point[1]);
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(p, out hit, 4, -1)) throw new Exception("경로 표본 누락 " + index);
            hits.Add(hit.position);
        }
        bool complete = true;
        for (int i = 1; i < hits.Count; i++) foreach (bool reverse in new[] { false, true })
        {
            var path = new UnityEngine.AI.NavMeshPath();
            var a = hits[reverse ? i : i - 1];
            var b = hits[reverse ? i - 1 : i];
            bool ok = UnityEngine.AI.NavMesh.CalculatePath(a, b, -1, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
            complete &= ok;
            if (index == 5 || !ok) report.AppendLine("경로 " + index + " " + a + " -> " + b + " " + path.status + " / corners=" + string.Join(";", path.corners.Select(p => p.ToString("F2"))));
        }
        report.AppendLine("경로 " + index++ + " 양방향=" + complete);
        if (complete) passed++;
    }
    report.AppendLine("경로 통과=" + passed + "/" + routes.Length + " / 진행 ID·좌표 보존=" + (identities() == before));
    System.IO.File.WriteAllText(folder + "/repair-audit.txt", report.ToString());
    if (passed != routes.Length || identities() != before) throw new Exception("회귀 검사 실패: repair-audit.txt 확인");
    AssetDatabase.CreateAsset(surface.navMeshData, navPath);
    EditorUtility.SetDirty(surface);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
    Undo.CollapseUndoOperations(undoGroup);
    System.IO.File.WriteAllText(folder + "/repair-phase.txt", "완료: 씬·새 NavMesh 저장 / 경로 " + passed + "/" + routes.Length);
    return report.ToString();
}
catch (Exception error)
{
    surface.RemoveData();
    surface.navMeshData = previousData;
    surface.AddData();
    Undo.RevertAllDownToGroup(undoGroup);
    System.IO.File.WriteAllText(folder + "/repair-phase.txt", "실패: 메모리 변경 롤백 / " + error);
    throw;
}
