var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 연속 월드 Edit Mode가 필요합니다.");
string folder = "output/OpenWorld/DungeonArtReview";
string meshFolder = "Assets/10.Datas/World/OpenWorld/DungeonArt";
var environment = GameObject.Find("AuthoredEnvironment");
var previousRoot = environment.transform.Find("던전 공간 재저작");
var terrain = environment.GetComponentInChildren<Terrain>();
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string,Type,object> read = (s,t) => parse.Invoke(null, new object[] { s,t });
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/dungeon-art-plan.json"), typeof(Dictionary<string,object>));
int version = Convert.ToInt32(plan["version"]);
if (System.IO.File.Exists(folder+"/application-v"+version+".txt")) throw new Exception("이 저작 버전은 이미 적용했습니다. 재실행하지 마세요.");
if (previousRoot != null && version != 2) throw new Exception("기존 저작을 덮어쓸 수 없습니다.");
var sites = (Dictionary<string,object>[])read(plan["sites"].ToString(), typeof(Dictionary<string,object>[]));
var rows = (Dictionary<string,object>[])read(plan["placements"].ToString(), typeof(Dictionary<string,object>[]));
var patches = (Dictionary<string,object>[])read(plan["patches"].ToString(), typeof(Dictionary<string,object>[]));
var lightRows = (Dictionary<string,object>[])read(plan["lights"].ToString(), typeof(Dictionary<string,object>[]));
var sources = sites.ToDictionary(s => s["id"].ToString(), s => previousRoot == null ? GameObject.Find(s["old"].ToString()) : previousRoot.Find(s["name"].ToString()).gameObject);
foreach (var item in sources)
    if (item.Value == null || item.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length > 0 || item.Value.GetComponentsInChildren<UPlayGround.GameActor>(true).Length > 0)
        throw new Exception("교체 대상과 진행 개체 경계를 확인해야 합니다: " + item.Key);
var assetPaths = rows.Select(r => r["path"].ToString()).Concat(sites.Where(s => s.ContainsKey("prefab")).Select(s => s["prefab"].ToString())).Append(plan["floorMaterialSource"].ToString()).Distinct();
var assets = assetPaths.ToDictionary(p => p, p => AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach (var asset in assets)
{
    if (asset.Value == null || asset.Value.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh == null) || asset.Value.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name.Contains("InternalError"))))
        throw new Exception("원본 메시·재질 검사 실패: " + asset.Key);
    if (asset.Value.GetComponentsInChildren<MonoBehaviour>(true).Any(c => c == null || !c.GetType().FullName.StartsWith("UnityEngine.Rendering.Universal.")))
        throw new Exception("장식 프리팹에 미검토 동작 스크립트가 있습니다: " + asset.Key);
}
Func<string> identities = () => string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c => c.Guid + "|" + c.transform.position.ToString("F4")).OrderBy(s => s));
string before = identities();
if (before != System.IO.File.ReadAllText(folder + "/identities-before.txt")) throw new Exception("중단 이후 진행 개체가 바뀌었습니다. 기준을 다시 조사해야 합니다.");
System.IO.Directory.CreateDirectory(folder + "/backup");
// 중단 이후 다른 환경 작업이 저장됐을 때도 최초 백업과 현재 저장본을 모두 보존한다.
string latestBackup = folder + "/backup/LakeVillageWorld.before-v"+version+".unity";
if (!System.IO.File.Exists(latestBackup)) System.IO.File.Copy(scene.path, latestBackup);
System.IO.Directory.CreateDirectory(meshFolder);
AssetDatabase.Refresh();
Func<GameObject,Bounds> bounds = go => {
    var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
    var result = renderers[0].bounds;
    foreach (var renderer in renderers) result.Encapsulate(renderer.bounds);
    return result;
};
Func<float,float,float> ground = (x,z) => terrain.SampleHeight(new Vector3(x,0,z)) + terrain.transform.position.y;
Undo.IncrementCurrentGroup();
int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("표시한 여섯 던전의 공간과 경계 재저작");
var root = new GameObject("던전 공간 재저작_시공");
root.transform.SetParent(environment.transform,false);
Undo.RegisterCreatedObjectUndo(root,"던전 저작 루트");
var groups = new Dictionary<string,Transform>();
var generated = new List<string>();
int disabledLights = 0;
try
{
    foreach (var site in sites)
    {
        string id = site["id"].ToString();
        var group = new GameObject(site["name"].ToString());
        group.transform.SetParent(root.transform,false);
        groups.Add(id,group.transform);
        if (!site.ContainsKey("prefab")) continue;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(assets[site["prefab"].ToString()],group.transform);
        var p = (float[])read(site["position"].ToString(),typeof(float[]));
        go.transform.position = new Vector3(p[0],p[1],p[2]);
        int enabled = 0;
        foreach (var light in go.GetComponentsInChildren<Light>(true))
        {
            light.shadows = LightShadows.None;
            if (light.type == LightType.Directional) { light.enabled = false; disabledLights++; continue; }
            if (light.enabled && enabled++ >= 6) { light.enabled = false; disabledLights++; }
            light.intensity = Mathf.Min(light.intensity,5);
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }
    foreach (var row in rows)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[row["site"].ToString()]);
        go.name = row["name"] + "_" + go.name;
        if (row["kind"].ToString() == "passage")
            foreach (var t in go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MOD_Gateway_Door_01_large_"))) t.gameObject.SetActive(false);
        var scale = (float[])read(row["scale"].ToString(),typeof(float[]));
        go.transform.localScale = new Vector3(scale[0],scale[1],scale[2]);
        go.transform.rotation = Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);
        var b = bounds(go);
        float x = Convert.ToSingle(row["x"]), z = Convert.ToSingle(row["z"]);
        float embed = row["kind"].ToString() == "structure" ? -.14f : 0;
        go.transform.position += new Vector3(x-b.center.x,ground(x,z)-b.min.y+embed+Convert.ToSingle(row["offset"]),z-b.center.z);
        if (row["kind"].ToString() == "decoration")
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }
    var material = assets[plan["floorMaterialSource"].ToString()].GetComponentInChildren<MeshRenderer>().sharedMaterial;
    for (int index=0; index<patches.Length; index++)
    {
        var patch = patches[index];
        var polygon = (float[][])read(patch["polygon"].ToString(),typeof(float[][]));
        var points = polygon.Select(p => new Vector2(p[0],p[1])).ToArray();
        var center = new Vector2(points.Average(p=>p.x),points.Average(p=>p.y));
        float baseY = ground(center.x,center.y);
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        Action<Vector2,Vector2,Vector2,int> subdivide = null;
        subdivide = (a,b,c,depth) => {
            if (depth > 0)
            {
                var ab=(a+b)*.5f; var bc=(b+c)*.5f; var ca=(c+a)*.5f;
                subdivide(a,ab,ca,depth-1); subdivide(ab,b,bc,depth-1); subdivide(ca,bc,c,depth-1); subdivide(ab,bc,ca,depth-1);
                return;
            }
            foreach (var p in new[]{a,c,b})
            {
                triangles.Add(vertices.Count);
                vertices.Add(new Vector3(p.x-center.x,ground(p.x,p.y)-baseY+.07f,p.y-center.y));
                uv.Add(new Vector2(p.x*.25f,p.y*.25f));
            }
        };
        for(int i=0;i<points.Length;i++) subdivide(center,points[i],points[(i+1)%points.Length],5);
        var mesh = new Mesh { name="지형을 따르는 끊어진 석재 포장" };
        mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = meshFolder + "/Paving_" + index.ToString("D2") + ".asset";
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (savedMesh != null)
        {
            if (previousRoot == null || version != 2) throw new Exception("생성 메시 경로가 이미 있습니다: " + path);
            Undo.RecordObject(savedMesh,"석재 포장 파손 윤곽 보정"); EditorUtility.CopySerialized(mesh,savedMesh);
            UnityEngine.Object.DestroyImmediate(mesh); mesh=savedMesh; EditorUtility.SetDirty(mesh);
        }
        else { AssetDatabase.CreateAsset(mesh,path); generated.Add(path); }
        var go = new GameObject("끊어진 석재 포장_"+index.ToString("D2"));
        go.transform.SetParent(groups[patch["site"].ToString()],false);
        go.transform.position = new Vector3(center.x,baseY,center.y);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    foreach (var row in lightRows)
    {
        float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);
        var go = new GameObject("화로의 따뜻한 반사광");
        go.transform.SetParent(groups[row["site"].ToString()],false);
        go.transform.position = new Vector3(x,ground(x,z)+Convert.ToSingle(row["height"]),z);
        var light=go.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.56f,.22f);
        light.intensity=Convert.ToSingle(row["intensity"]); light.range=Convert.ToSingle(row["range"]); light.shadows=LightShadows.None;
    }
    if (before != identities()) throw new Exception("진행 개체 변경 감지");
    foreach (var source in sources.Values) Undo.DestroyObjectImmediate(source);
    if (previousRoot != null) Undo.DestroyObjectImmediate(previousRoot.gameObject);
    root.name="던전 공간 재저작";
    Physics.SyncTransforms();
    int missing = root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    if (missing != 0) throw new Exception("신규 배치 Missing Script: " + missing);
    if (before != identities()) throw new Exception("교체 후 진행 개체 변경 감지");
    foreach (var t in root.GetComponentsInChildren<Transform>(true))
        if (t.GetComponent<Renderer>()!=null && t.GetComponent<ParticleSystem>()==null)
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Where(m=>m!=null && AssetDatabase.GetAssetPath(m).StartsWith(meshFolder)).Distinct()) AssetDatabase.SaveAssetIfDirty(mesh);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("던전 배치 씬 저장 실패");
    Undo.CollapseUndoOperations(undoGroup);
    string result="여섯 구역 교체 / 신규 개별 프리팹="+rows.Length+" / 원본 던전=2 / 지형 추종 포장="+patches.Length+" / 동시 조명 제한으로 비활성="+disabledLights+" / 진행 ID·좌표 동일 / NavMesh 재생성 대기";
    System.IO.File.WriteAllText(folder+"/application.txt",result);
    System.IO.File.WriteAllText(folder+"/application-v"+version+".txt",result);
    return result;
}
catch (Exception error)
{
    Undo.RevertAllDownToGroup(undoGroup);
    foreach (var path in generated) AssetDatabase.DeleteAsset(path);
    System.IO.File.WriteAllText(folder+"/application-failure.txt",error.ToString());
    throw;
}
