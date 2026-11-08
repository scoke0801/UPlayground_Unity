// 사용자가 승인한 LakeVillageWorld 몬스터 인접 Bake. 완료 후 재실행하지 않는다.
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string folder = "output/OpenWorld/MonsterProximityBake";
string assetPath = "Assets/10.Datas/Map/Placement/PlacementData_LakeVillageWorld_Monsters.asset";
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty
    || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new Exception("저장된 LakeVillageWorld와 정상 컴파일 상태가 필요합니다.");
if (System.IO.File.Exists(assetPath) || System.IO.File.Exists(folder + "/result.txt"))
    throw new Exception("이미 Bake 결과가 있습니다. 중복 적용하지 않습니다.");
if (!System.IO.File.Exists(folder + "/backup/LakeVillageWorld.before.unity.gz"))
    throw new Exception("작업 전 씬 백업이 필요합니다.");
var roots = scene.GetRootGameObjects();
var allMonsters = roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).ToArray();
if (allMonsters.Length != 482) throw new Exception("검토 이후 몬스터 수가 바뀌었습니다.");
var database = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
// 프리팹 원본과 다른 미등록 개별 ID는 씬 연출 소유다. 일반 필드 ID로 치환하지 않는다.
var staged = new HashSet<UPlayGround.MonsterActor>(allMonsters.Where(m =>
{
    if (string.IsNullOrEmpty(m.ActorId) || database.Contains(m.ActorId)) return false;
    var source = PrefabUtility.GetCorrespondingObjectFromSource(m);
    return source == null || source.ActorId != m.ActorId;
}));
if (staged.Count != 11) throw new Exception("검토 이후 씬 연출 몬스터 구성이 바뀌었습니다.");
var monsters = allMonsters.Where(m => !staged.Contains(m)).ToArray();
var utility = typeof(UPlayGround.Tool.Editor.Map.WorldPlacementBakeUtility);
var privateStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
var register = utility.GetMethod("TryRegisterRuntimeDataTarget", privateStatic);
var createRecord = utility.GetMethod("CreateRecord", privateStatic);
var configure = utility.GetMethod("ConfigureMonsterProximity", privateStatic);
var identities = roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
if (identities.Any(i => !i.HasGuid) || identities.GroupBy(i => i.Guid).Any(g => g.Count() > 1))
    throw new Exception("빈 ID 또는 중복 ID가 있습니다.");
foreach (var monster in monsters)
{
    var definition = monster.Definition ?? database.GetDefinition(monster.ActorId);
    if (definition == null || definition.prefab == null || definition.prefab.GetComponent<UPlayGround.MonsterActor>() == null)
        throw new Exception("복원 가능한 몬스터 정의가 없습니다: " + monster.name);
    if (monster.GetComponent<UPlayGround.Components.SceneEntityId>() == null)
        throw new Exception("씬 저장 ID가 없습니다: " + monster.name);
    var prefabTypes = new HashSet<Type>(definition.prefab.GetComponents<UnityEngine.Component>().Where(c => c != null).Select(c => c.GetType()));
    foreach (var component in monster.GetComponents<UnityEngine.Component>())
        if (component == null || (!prefabTypes.Contains(component.GetType()) && !(component is UPlayGround.Components.SceneEntityId)))
            throw new Exception("원본 프리팹에 없는 개별 컴포넌트를 먼저 보존해야 합니다: " + monster.name);
}
var removedObjects = new HashSet<UnityEngine.Object>(monsters.SelectMany(m => m.GetComponentsInChildren<UnityEngine.Component>(true)).Cast<UnityEngine.Object>()
    .Concat(monsters.SelectMany(m => m.GetComponentsInChildren<Transform>(true)).Select(t => (UnityEngine.Object)t.gameObject)));
foreach (var component in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
{
    if (component == null || removedObjects.Contains(component)) continue;
    var serialized = new SerializedObject(component);
    var property = serialized.GetIterator();
    while (property.Next(true))
        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null && removedObjects.Contains(property.objectReferenceValue))
            throw new Exception("씬에서 몬스터를 직접 참조합니다: " + component.name + " / " + property.propertyPath);
}
System.IO.Directory.CreateDirectory(folder);
Func<UPlayGround.MonsterActor, string> snapshot = m => m.GetComponent<UPlayGround.Components.SceneEntityId>().Guid + "|" + m.ActorId + "|"
    + m.transform.position.ToString("F6") + "|" + m.transform.rotation.ToString("F6") + "|" + m.transform.localScale.ToString("F6") + "|"
    + (m.GetComponent<UPlayGround.Components.EnemyDetection>() == null ? "-1" : m.GetComponent<UPlayGround.Components.EnemyDetection>().MaxChaseDistanceFromAnchor.ToString(System.Globalization.CultureInfo.InvariantCulture));
System.IO.File.WriteAllLines(folder + "/monsters-before.txt", monsters.Select(snapshot));
System.IO.File.WriteAllLines(folder + "/staged-preserved.txt", staged.Select(snapshot));
System.IO.File.WriteAllLines(folder + "/identities-before.txt", identities.Select(i => i.Guid + "|" + i.transform.position.ToString("F6")));
System.IO.File.WriteAllLines(folder + "/loaders-before.txt", roots.SelectMany(r => r.GetComponentsInChildren<UPlayGround.Manager.World.RuntimePlacementLoader>(true))
    .Select(l => l.name + "|" + AssetDatabase.GetAssetPath(l.PlacementData) + "|" + (l.PlacementData == null ? 0 : l.PlacementData.Records.Count)));
using (var undo = new UPlayGround.Tool.Editor.Map.PlacementUndoScope("월드 몬스터 인접 스폰 Bake"))
{
    var records = new List<UPlayGround.Data.World.WorldPlacementRecord>();
    foreach (var monster in monsters)
    {
        var originalGuid = monster.GetComponent<UPlayGround.Components.SceneEntityId>().Guid;
        if (!(bool)register.Invoke(null, new object[] { monster.gameObject, database }))
            throw new Exception("RuntimeData 등록 실패: " + monster.name);
        var record = (UPlayGround.Data.World.WorldPlacementRecord)createRecord.Invoke(null,
            new object[] { monster.GetComponent<UPlayGround.Components.WorldPlacementMetadata>(), database });
        if (record == null || record.sceneEntityGuid != originalGuid || !record.spawnWhenNearby || record.prefab != null)
            throw new Exception("ID 또는 인접 생성 데이터 검증 실패: " + monster.name);
        if (monster.Definition != null && record.actorId != monster.Definition.actorId)
            throw new Exception("저작한 정의 대신 다른 액터 수치를 선택했습니다: " + monster.name);
        records.Add(record);
    }
    var data = ScriptableObject.CreateInstance<UPlayGround.Data.World.WorldPlacementDataSO>();
    data.spawnMonstersByProximity = true;
    data.SetRecords(records);
    configure.Invoke(null, new object[] { data, database });
    if (data.Records.Count != monsters.Length || data.MonsterSpawnAreas.Sum(a => a.recordIndices.Count) != monsters.Length)
        throw new Exception("Bake 구역 레코드 수 불일치");
    AssetDatabase.CreateAsset(data, assetPath);
    Undo.RegisterCreatedObjectUndo(data, "몬스터 배치 데이터 생성");
    var loaderObject = new GameObject("RuntimeMonsterPlacementLoader");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(loaderObject, scene);
    Undo.RegisterCreatedObjectUndo(loaderObject, "몬스터 배치 로더 생성");
    var loader = loaderObject.AddComponent<UPlayGround.Manager.World.RuntimePlacementLoader>();
    loader.EditorSetPlacementData(data);
    foreach (var monster in monsters) Undo.DestroyObjectImmediate(monster.gameObject);
    if (roots.Where(r => r != null).SelectMany(r => r.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).Any(m => !staged.Contains(m)))
        throw new Exception("씬에 일반 필드 몬스터 실체가 남았습니다.");
    var remainingIds = new HashSet<string>(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true))
        .Select(i => i.Guid + "|" + i.transform.position.ToString("F6")));
    foreach (var record in records) remainingIds.Add(record.sceneEntityGuid + "|" + record.position.ToString("F6"));
    foreach (var row in System.IO.File.ReadAllLines(folder + "/identities-before.txt"))
        if (!remainingIds.Contains(row)) throw new Exception("기존 진행 ID 또는 좌표가 유실되었습니다: " + row);
    EditorUtility.SetDirty(data);
    AssetDatabase.SaveAssetIfDirty(data);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    undo.Complete();
    System.IO.File.WriteAllText(folder + "/result.txt", "Bake 완료\n몬스터=" + records.Count + "\n구역=" + data.MonsterSpawnAreas.Count
        + "\n데이터=" + assetPath + "\n씬=" + scene.path + "\n기존 ID·좌표 보존=통과\n씬 일반 필드 몬스터 실체=0\n씬 연출 몬스터 보존=" + staged.Count + "\n기존 플레이어·주민 로더=보존\n");
}
