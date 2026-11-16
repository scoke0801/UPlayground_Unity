// 개체별 저작 입력으로 기존 생활 행동을 보완한다. NPC·위치·이야기 플래그를 새로 생성하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new Exception("컴파일 완료된 편집 모드가 필요합니다.");
const string folder = "output/NpcDailyAnimations/WorldActivityPass";
const string motionFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/";
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
var plan = (Dictionary<string, object>[])parse.Invoke(null, new object[] {
    System.IO.File.ReadAllText("Tools/OpenWorld/lake-resident-activities-plan.json"), typeof(Dictionary<string, object>[]) });
var scenes = plan.Select(row => row["scene"].ToString()).Distinct()
    .Select(UnityEngine.SceneManagement.SceneManager.GetSceneByName).ToArray();
if (scenes.Any(s => !s.isLoaded || s.isDirty)) throw new Exception("대상 지역을 모두 열고 기존 씬 변경을 보존해야 합니다.");
var actors = scenes.SelectMany(s => s.GetRootGameObjects()).SelectMany(g => g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).ToArray();
var presentations = scenes.SelectMany(s => s.GetRootGameObjects()).SelectMany(g => g.GetComponentsInChildren<UPlayGround.Gameplay.Interaction.NpcLifePresentation>(true)).ToArray();
var selected = new List<UPlayGround.NpcActor>();
var motions = new Dictionary<string, UPlayGround.Animation.MotionSetAsset>();
foreach (var row in plan) {
    string scene = row["scene"].ToString(), guid = row["guid"].ToString(), name = row["name"].ToString();
    var matches = actors.Where(a => a.gameObject.scene.name == scene && (guid == "none" ? a.name == name
        : a.GetComponent<UPlayGround.Components.SceneEntityId>() != null && a.GetComponent<UPlayGround.Components.SceneEntityId>().Guid == guid)).ToArray();
    if (matches.Length != 1 || matches[0].GetComponent<UPlayGround.Components.NpcBrain>() == null)
        throw new Exception("개체 식별 모호·누락: " + scene + "/" + name);
    selected.Add(matches[0]);
    foreach (string key in new[] { "motion", "gesture" }) {
        string path = row[key].ToString(); if (path == "" || motions.ContainsKey(path)) continue;
        var asset = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionFolder + path + ".asset");
        if (asset == null || asset.motionSet == null || asset.motionSet.motions.Any(m => m.motionClip == null))
            throw new Exception("사용할 모션·클립 누락: " + path);
        motions.Add(path, asset);
    }
}
if (selected.Distinct().Count() != selected.Count || selected.Count != actors.Length)
    throw new Exception("NPC 누락 또는 중복 계획입니다.");
Func<string> identities = () => string.Join("\n", actors.Select(a => a.gameObject.scene.name + "|" + a.ActorId + "|"
    + (a.GetComponent<UPlayGround.Components.SceneEntityId>() == null ? "none" : a.GetComponent<UPlayGround.Components.SceneEntityId>().Guid)
    + "|" + a.transform.position.ToString("F5") + "|" + a.transform.rotation.ToString("F5")).OrderBy(s => s));
string before = identities();
Func<GameObject, Bounds> visualBounds = go => {
    var rs = go.GetComponentsInChildren<MeshRenderer>(true);
    if (rs.Length == 0) throw new Exception("작업물 메시 누락: " + go.name);
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
};
const string tablePath = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/Interior/Furniture/PF_Farm_WoodTable_01.prefab";
foreach (var row in plan.Where(r => r["mode"].ToString() == "work"))
    foreach (string path in new[] { tablePath, row["stockPrefab"].ToString() })
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new Exception("작업대·상품 원본 누락: " + path);
System.IO.Directory.CreateDirectory(folder + "/backup");
foreach (var s in scenes) {
    string backup = folder + "/backup/" + s.name + ".unity";
    if (!System.IO.File.Exists(backup)) System.IO.File.Copy(s.path, backup);
}
var log = new List<string> { "scene\tname\tmode\trole\twander\tradius\twork\tpresentationPolicies" };
Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("연결 월드 주민 생활 행동 보완");
try {
    for (int i = 0; i < plan.Length; i++) {
        var row = plan[i]; var actor = selected[i]; var brain = actor.GetComponent<UPlayGround.Components.NpcBrain>();
        string mode = row["mode"].ToString(); int policies = 0;
        if (mode != "preserve") {
            Undo.RecordObject(brain, "주민 생활 행동"); var so = new SerializedObject(brain);
            bool mobile = mode == "browse" || mode == "phase" && row["motion"].ToString() == "";
            so.FindProperty("_enableWander").boolValue = mobile;
            so.FindProperty("_patrolRadius").floatValue = Convert.ToSingle(row["radius"]);
            so.FindProperty("_patrolWaitTime").floatValue = Convert.ToSingle(row["waitSeconds"]);
            so.FindProperty("_minimumWanderDistance").floatValue = 0.9f;
            so.FindProperty("_wanderArrivalDistance").floatValue = 0.25f;
            so.FindProperty("_wanderSpeedMultiplier").floatValue = 0.45f;
            string work = row["motion"].ToString();
            if (work != "") so.FindProperty("_workMotion").objectReferenceValue = motions[work];
            var gestures = so.FindProperty("_idleGestures"); gestures.arraySize = 1;
            gestures.GetArrayElementAtIndex(0).objectReferenceValue = motions[row["gesture"].ToString()];
            so.FindProperty("_idleGestureInterval").vector2Value = new Vector2(8f, 19f);
            so.ApplyModifiedProperties(); PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
            if (mode == "work") {
                var scene = actor.gameObject.scene;
                var surfaces = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "ResidentWorkSurfaces");
                if (surfaces == null) {
                    surfaces = new GameObject("ResidentWorkSurfaces");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(surfaces, scene);
                    Undo.RegisterCreatedObjectUndo(surfaces, "주민 작업면");
                }
                string stationName = row["guid"].ToString() == "none" ? actor.name : row["guid"].ToString();
                if (surfaces.transform.Find(stationName) == null) {
                    var station = new GameObject(stationName); station.transform.SetParent(surfaces.transform);
                    Undo.RegisterCreatedObjectUndo(station, "주민 작업대·실물 상품");
                    var center = actor.transform.TransformPoint(new Vector3(0f, 0.365f, 0.6f));
                    var rotation = actor.transform.rotation;
                    foreach (bool stock in new[] { false, true }) {
                        var wrapper = new GameObject(stock ? "상품" : "작업대"); wrapper.transform.SetParent(station.transform);
                        Undo.RegisterCreatedObjectUndo(wrapper, "작업 소품");
                        string path = stock ? row["stockPrefab"].ToString() : tablePath;
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
                        Undo.RegisterCreatedObjectUndo(instance, "기존 모델 연결"); instance.transform.SetParent(wrapper.transform, false);
                        var b = visualBounds(instance);
                        float stockHeight = Convert.ToSingle(row["stockHeight"]);
                        var size = stock ? new Vector3(0.29f, stockHeight, 0.24f) : new Vector3(0.72f, 0.73f, 0.44f);
                        var scale = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
                        wrapper.transform.localScale = scale;
                        var target = stock ? actor.transform.TransformPoint(new Vector3(0f, 0.733f + stockHeight * 0.5f, 0.55f)) : center;
                        wrapper.transform.SetPositionAndRotation(target - rotation * Vector3.Scale(b.center, scale), rotation);
                        foreach (var c in instance.GetComponentsInChildren<Collider>(true)) if (stock) c.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    }
                }
                var motor = actor.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
                var capsule = actor.GetComponent<CapsuleCollider>();
                Undo.RecordObjects(new UnityEngine.Object[] { motor, capsule }, "작업면 보행 여유");
                var motorData = new SerializedObject(motor); motorData.FindProperty("CapsuleRadius").floatValue = 0.2f;
                motorData.ApplyModifiedProperties(); capsule.radius = 0.2f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
            }
            // 기존 사건이 몸을 옮길 때 새 HomePose를 쓰며, 완료 후 착석은 다시 보행하지 않는다.
            foreach (var p in presentations) {
                if (p.Actor != actor) continue;
                Undo.RecordObject(p, "사건 전후 주민 행동"); var state = new SerializedObject(p);
                state.FindProperty("_configureWander").boolValue = true;
                state.FindProperty("_beforeCanWander").boolValue = mobile;
                state.FindProperty("_afterCanWander").boolValue = mode == "browse"
                    && state.FindProperty("_afterWorkMotion").objectReferenceValue == null;
                if (work != "") state.FindProperty("_beforeWorkMotion").objectReferenceValue = motions[work];
                state.ApplyModifiedProperties(); PrefabUtility.RecordPrefabInstancePropertyModifications(p); policies++;
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(actor.gameObject.scene);
        }
        log.Add(actor.gameObject.scene.name + "\t" + actor.name + "\t" + mode + "\t" + row["role"]
            + "\t" + brain.EnableWander + "\t" + brain.PatrolRadius + "\t" + (brain.WorkMotion == null ? "none" : brain.WorkMotion.name) + "\t" + policies);
    }
    if (before != identities()) throw new Exception("배치·회전·Actor ID·SceneEntityId 변경");
    foreach (var scene in scenes) if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllLines(folder + "/applied.tsv", log);
    System.IO.File.WriteAllText(folder + "/identities.txt", before);
} catch {
    Undo.RevertAllDownToGroup(group);
    foreach (var scene in scenes) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    throw;
}
return "NPC " + plan.Length + "명 전수 계획 적용, 기존 몸·배치·진행 ID 보존";
