var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 월드 Edit Mode 필요");
string folder = "output/OpenWorld/ShadeCastle";
System.IO.Directory.CreateDirectory(folder);
var report = new System.Text.StringBuilder();
System.IO.File.WriteAllText(folder + "/audit-phase.txt", "저장된 씬 검사 시작");
var world = GameObject.Find("AuthoredEnvironment");
var terrain = world.GetComponentInChildren<Terrain>();
var surface = world.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
if (AssetDatabase.GetAssetPath(surface.navMeshData) != "Assets/10.Datas/World/OpenWorld/ShadeCastleNavigation.asset")
    throw new Exception("그늘성 수정 NavMesh 연결 누락");
Func<Vector3, Vector3> sample = p =>
{
    p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
    UnityEngine.AI.NavMeshHit hit;
    if (!UnityEngine.AI.NavMesh.SamplePosition(p, out hit, 4, -1)) throw new Exception("NavMesh 표본 누락 " + p);
    return hit.position;
};
var inside = sample(new Vector3(1540, 0, 510));
var outside = sample(new Vector3(1410, 0, 420));
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (s, t) => parse.Invoke(null, new object[] { s, t });
var regional = (Dictionary<string, object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/regional-cover-plan.json"), typeof(Dictionary<string, object>));
var routes = (Dictionary<string, object>[])read(regional["routes"].ToString(), typeof(Dictionary<string, object>[]));
int routeCount = 0;
foreach (var route in routes)
{
    System.IO.File.WriteAllText(folder + "/audit-phase.txt", "기존 경로 검사 " + routeCount);
    var points = (float[][])read(route["points"].ToString(), typeof(float[][]));
    var hits = points.Select(p => sample(new Vector3(p[0], 0, p[1]))).ToArray();
    for (int i = 1; i < hits.Length; i++) foreach (bool reverse in new[] { false, true })
    {
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(hits[reverse ? i : i - 1], hits[reverse ? i - 1 : i], -1, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            throw new Exception("기존 경로 회귀 " + routeCount);
    }
    routeCount++;
}
report.AppendLine("기존 경로 양방향 통과=" + routeCount + "/" + routes.Length);
System.IO.File.WriteAllText(folder + "/audit-phase.txt", "기존 경로 완료 / 그늘성·참조 검사");
foreach (bool reverse in new[] { false, true })
{
    var a = reverse ? outside : inside;
    var b = reverse ? inside : outside;
    var path = new UnityEngine.AI.NavMeshPath();
    bool complete = UnityEngine.AI.NavMesh.CalculatePath(a, b, -1, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    report.AppendLine(a + " -> " + b + " " + path.status + " / corners=" + string.Join(";", path.corners.Select(p => p.ToString("F2"))));
    if (!complete) throw new Exception("원래 실패 좌표 재검사 실패");
}
// 서쪽 성벽이 유지되고 경로가 정문으로 우회하는지 검사한다.
var westInside = sample(new Vector3(1435, 0, 420));
var westOutside = sample(new Vector3(1410, 0, 420));
UnityEngine.AI.NavMeshHit wallHit;
bool wallBlocked = UnityEngine.AI.NavMesh.Raycast(westInside, westOutside, out wallHit, -1);
bool physicalWall = Physics.Linecast(westInside + Vector3.up, westOutside + Vector3.up, -1, QueryTriggerInteraction.Ignore);
report.AppendLine("서쪽 성벽 유지 / NavMesh 직선 차단=" + wallBlocked + " / 물리 직선 차단=" + physicalWall);
if (!wallBlocked || !physicalWall) throw new Exception("서쪽 성벽 경계 유실");
var ids = string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c => c.Guid + "|" + c.transform.position.ToString("F4")).OrderBy(s => s));
bool same = ids == System.IO.File.ReadAllText(folder + "/identities-before.txt");
int missing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
report.AppendLine("진행 ID·좌표 보존=" + same + " / 전체 씬 Missing Script=" + missing);
if (!same || missing != 0) throw new Exception("참조·진행 ID 회귀");
int accessCount = 0;
foreach (string line in System.IO.File.ReadAllLines("output/OpenWorld/RegionalCover/access-audit.txt"))
{
    var coordinates = System.Text.RegularExpressions.Regex.Matches(line, @"\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)");
    if (coordinates.Count == 0) continue;
    if (coordinates.Count != 2) throw new Exception("접근 경로 기록 형식 변경");
    var endpoints = new List<Vector3>();
    foreach (System.Text.RegularExpressions.Match coordinate in coordinates)
        endpoints.Add(sample(new Vector3(float.Parse(coordinate.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 0, float.Parse(coordinate.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture))));
    foreach (bool reverse in new[] { false, true })
    {
        var path = new UnityEngine.AI.NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(endpoints[reverse ? 1 : 0], endpoints[reverse ? 0 : 1], -1, path) || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
            throw new Exception("지역·밭 접근 회귀 " + line);
    }
    accessCount++;
}
report.AppendLine("지역·밭 접근 양방향 통과=" + accessCount + "/14");
if (accessCount != 14) throw new Exception("접근 경로 검사 누락");
// NavMesh의 계단 허용치와 실제 KCC 통과는 별개이므로 문턱 높이를 근거로 기록한다.
for (float z = 523; z <= 538; z += .5f)
{
    var p = new Vector3(1540, 61.48f, z);
    var supports = Physics.RaycastAll(p + Vector3.up * 3, Vector3.down, 5, -1, QueryTriggerInteraction.Ignore).Where(h => Vector3.Dot(h.normal, Vector3.up) > .7f).OrderByDescending(h => h.point.y).ToArray();
    if (supports.Length > 0) report.AppendLine("문턱 지지 z=" + z + " / y=" + supports[0].point.y.ToString("F3") + " / " + supports[0].collider.name);
}
System.IO.File.WriteAllText(folder + "/saved-audit.txt", report.ToString());
System.IO.File.WriteAllText(folder + "/audit-phase.txt", "완료: 저장된 씬 경로·참조·접근 검사");
return report.ToString();
