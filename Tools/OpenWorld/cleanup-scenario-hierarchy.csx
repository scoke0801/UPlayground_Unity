var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 LakeVillageWorld의 Edit Mode에서만 실행합니다.");
var folder = "output/OpenWorld/ScenarioCleanup";
System.IO.Directory.CreateDirectory(folder + "/backup");
if (System.IO.File.Exists(folder + "/backup/LakeVillageWorld.unity"))
{
    if (!System.IO.File.ReadAllBytes(scene.path).SequenceEqual(System.IO.File.ReadAllBytes(folder + "/backup/LakeVillageWorld.unity"))) throw new Exception("백업 이후 씬이 변경되었습니다. 재실행하지 마세요.");
}
else System.IO.File.Copy(scene.path, folder + "/backup/LakeVillageWorld.unity");
var roots = scene.GetRootGameObjects();
var transforms = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
var positions = transforms.ToDictionary(t => t, t => t.position);
var rotations = transforms.ToDictionary(t => t, t => t.rotation);
var scales = transforms.ToDictionary(t => t, t => t.lossyScale);
var active = transforms.ToDictionary(t => t, t => t.gameObject.activeInHierarchy);
var scripts = roots.SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c => c != null).ToArray();
var scriptData = scripts.ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
int missing = transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
if (missing != 0) throw new Exception("Missing Script를 먼저 복구해야 합니다.");
var environment = roots.Single(g => g.name == "AuthoredEnvironment").transform;
var terrain = environment.GetComponentInChildren<Terrain>();
var lake = environment.Find("월드 구간/호수와 사당");
var walls = lake.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<MeshRenderer>() != null && (t.name.StartsWith("폐허_") || t.name.StartsWith("Temple_Wall"))).ToArray();
var wallNames = new HashSet<string>(walls.Select(t => t.name.Replace("_R2", "")));
var wallColliders = environment.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("_Collision") && wallNames.Contains(t.name.Replace("_Collision", ""))).ToArray();
var remnants = walls.Concat(wallColliders).ToArray();
var removed = new HashSet<Transform>(remnants.SelectMany(t => t.GetComponentsInChildren<Transform>(true)));
var removedObjects = new HashSet<UnityEngine.Object>(remnants.SelectMany(t => t.GetComponentsInChildren<Component>(true)).Cast<UnityEngine.Object>().Concat(remnants.Select(t => (UnityEngine.Object)t.gameObject)));
foreach (var script in scripts)
{
    var property = new SerializedObject(script).GetIterator();
    while (property.Next(true))
        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null && removedObjects.Contains(property.objectReferenceValue))
            throw new Exception("옛 석벽에 외부 참조가 있습니다: " + script.name + "." + property.propertyPath);
}
var corrected = new HashSet<Transform>();
var changes = new List<string>();
Func<Transform, string> path = null;
path = t => t.parent == null ? t.name : path(t.parent) + "/" + t.name;
Undo.IncrementCurrentGroup();
int undo = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("시나리오 지하 소품 및 하이어라키 정리");
try
{
    // 옛 평지의 연속 석벽은 급경사에 접지해도 단절되므로 참조가 없는 잔재만 제거한다.
    foreach (var wall in remnants)
    {
        changes.Add("제거 | " + path(wall));
        Undo.DestroyObjectImmediate(wall.gameObject);
    }
    // 지하 던전과 수면은 높이가 의도된 대상이므로 사당 구역의 장식만 보정한다.
    foreach (Transform category in lake)
    foreach (Transform target in category)
    {
        if (target.name == "LakeWater" || target.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) continue;
        var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) continue;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float ground = terrain.SampleHeight(bounds.center) + terrain.transform.position.y;
        if (bounds.max.y >= ground - 3) continue;
        var before = target.position;
        Undo.RecordObject(target, "지하 장식 접지");
        target.position += Vector3.up * (ground - bounds.min.y - 0.12f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        foreach (var child in target.GetComponentsInChildren<Transform>(true)) corrected.Add(child);
        changes.Add(path(target) + " | " + before.ToString("F4") + " -> " + target.position.ToString("F4"));
    }
    // 포털은 모델의 자식만 움직이면 상호작용 Collider가 지하에 남으므로 묶음 전체를 옮긴다.
    var portal = roots.Single(g => g.name == "WorldExpansionActors").transform.Find("0e5e7cb473d759d78bcbaad42bdd354b");
    var portalActor = portal.GetComponentInChildren<UPlayGround.PortalActor>(true);
    var collider = portalActor.GetComponent<BoxCollider>();
    var portalGround = portalActor.transform.position;
    portalGround.y = terrain.SampleHeight(portalGround) + terrain.transform.position.y;
    UnityEngine.AI.NavMeshHit hit;
    if (!UnityEngine.AI.NavMesh.SamplePosition(portalGround, out hit, 2, UnityEngine.AI.NavMesh.AllAreas))
        throw new Exception("포털 접지점의 기존 NavMesh를 찾지 못했습니다.");
    float portalDelta = hit.position.y - collider.bounds.min.y + 0.05f;
    if (portalDelta > 3)
    {
        var before = portal.position;
        Undo.RecordObject(portal, "지하 포털 접지");
        portal.position += Vector3.up * portalDelta;
        foreach (var child in portal.GetComponentsInChildren<Transform>(true)) corrected.Add(child);
        changes.Add(path(portal) + " | " + before.ToString("F4") + " -> " + portal.position.ToString("F4"));
    }
    var arrival = roots.SelectMany(g => g.GetComponentsInChildren<UPlayGround.SceneArrivalPoint>(true)).Single(c => c.Id == "0e5e7cb473d759d78bcbaad42bdd354b").transform;
    var arrivalGround = arrival.position;
    arrivalGround.y = terrain.SampleHeight(arrivalGround) + terrain.transform.position.y;
    UnityEngine.AI.NavMeshHit arrivalHit;
    if (!UnityEngine.AI.NavMesh.SamplePosition(arrivalGround, out arrivalHit, 2, UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("포털 도착점의 NavMesh를 찾지 못했습니다.");
    Undo.RecordObject(arrival, "지하 포털 도착점 접지");
    var arrivalBefore = arrival.position;
    arrival.position = new Vector3(arrival.position.x, arrivalHit.position.y + 0.1f, arrival.position.z);
    corrected.Add(arrival);
    changes.Add("도착점 | " + arrivalBefore.ToString("F4") + " -> " + arrival.position.ToString("F4"));
    var groups = new Dictionary<string, Transform>();
    Func<string, Transform> group = null;
    group = name =>
    {
        Transform found;
        if (groups.TryGetValue(name, out found)) return found;
        int split = name.LastIndexOf('/');
        var parent = split < 0 ? null : group(name.Substring(0, split));
        var go = new GameObject(split < 0 ? name : name.Substring(split + 1));
        Undo.RegisterCreatedObjectUndo(go, "분류 그룹 생성");
        if (parent != null) go.transform.SetParent(parent, false);
        groups.Add(name, go.transform);
        return go.transform;
    };
    var destinations = new Dictionary<string, string>
    {
        {"Directional Light", "조명과 카메라"}, {"MainCamera", "조명과 카메라"}, {"ScenarioLighting", "조명과 카메라"},
        {"Player", "플레이 액터"}, {"Enemy", "플레이 액터"}, {"NPC", "플레이 액터"},
        {"Trigger", "시나리오/공통"}, {"QualityInteractions", "탐험 콘텐츠/상호작용"}, {"WorldExpansionActors", "탐험 콘텐츠/월드 배치"},
        {"ScenarioResidents", "생활과 주민"}, {"ResidentLife", "생활과 주민"}, {"LivingWorld", "생활과 주민"},
        {"WorldResidents_r01", "생활과 주민"}, {"ScenarioFarm", "생활과 주민"}, {"LakeAbundanceArt", "생활과 주민"},
        {"FieldWildlife", "탐험 콘텐츠/야생동물"}, {"ExplorationDiscoveries", "탐험 콘텐츠/발견과 보상"},
        {"Exit_ToDrought", "지역 이동/여울마을"}, {"Arrival_FromDrought", "지역 이동/여울마을"},
        {"CityTravel_Harbor", "지역 이동/항구"}, {"CityTravel_Castle", "지역 이동/돌등성"},
        {"R07_remaining_SourceStockState", "시나리오/붉은나무/진행 상태"}, {"R09_receipt_SourceStockState", "시나리오/붉은나무/진행 상태"}
    };
    foreach (var root in roots)
    {
        string destination;
        if (!destinations.TryGetValue(root.name, out destination))
        {
            if (root.name.StartsWith("RedTreeR")) destination = "시나리오/붉은나무";
            else if (root.name.StartsWith("[STORY]")) destination = "시나리오/동료 만남";
            else if (new[] {"ScenarioHerbRoute", "ScenarioFirstQuest", "ScenarioFirstCombat", "ScenarioOptionalEvents"}.Contains(root.name)) destination = "시나리오/초반과 선택 이벤트";
            else if (root.name == "RegionPreparationBarrier")
            {
                float x = root.transform.GetChild(0).position.x;
                destination = "지역 이동/준비 장벽/" + (x > 1500 ? "항구" : x < 800 ? "돌등성" : "여울마을");
            }
            else continue;
        }
        Undo.SetTransformParent(root.transform, group(destination), "월드 위치를 유지하며 분류");
        changes.Add(root.name + " -> " + destination);
    }
    foreach (var target in transforms)
    {
        if (removed.Contains(target)) continue;
        if (target == null || target.gameObject.activeInHierarchy != active[target]) throw new Exception("기존 오브젝트 또는 활성 상태 변경");
        if (!corrected.Contains(target) && Vector3.Distance(target.position, positions[target]) > 0.001f) throw new Exception("예상 밖 위치 변경: " + path(target));
        if (Quaternion.Angle(target.rotation, rotations[target]) > 0.01f || Vector3.Distance(target.lossyScale, scales[target]) > 0.001f) throw new Exception("회전 또는 크기 변경: " + path(target));
    }
    foreach (var script in scripts)
        if (script == null || EditorJsonUtility.ToJson(script) != scriptData[script]) throw new Exception("직렬화된 스크립트 값 또는 참조 변경");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undo);
    System.IO.File.WriteAllLines(folder + "/changes.txt", changes);
    return new { removedWalls = walls.Length, removedWallColliders = wallColliders.Length, groundedDecorations = changes.Count(c => c.Contains(" -> (") ) - 2, preservedTransforms = transforms.Length - removed.Count, preservedScripts = scripts.Length, missingScripts = missing, rootCount = scene.rootCount, scene = scene.path };
}
catch
{
    Undo.RevertAllDownToGroup(undo);
    throw;
}
