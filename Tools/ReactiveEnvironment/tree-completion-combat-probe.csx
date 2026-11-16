// 본편 나무의 복제본을 기존 독립 fixture로 옮겨 실제 Collider 해석→HitRequest→저장 경로를 검사한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
var originals = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).Where(t => t.Definition != null && t.Definition.destruction != null && t.Definition.destruction.completionPolicy == UPlayGround.Data.ReactiveEnvironment.EnvironmentCompletionPolicy.ClearAfterSettlement).GroupBy(t => t.Definition).Select(g => g.First()).ToArray();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
System.Action<object, string, object> set = (target, name, value) => target.GetType().GetField(name, flags).SetValue(target, value);
System.Func<object, string, object[], object> invoke = (target, name, values) => {
    try { return target.GetType().GetMethod(name, flags).Invoke(target, values); }
    catch (System.Reflection.TargetInvocationException error) { throw new System.Exception(name + ": " + error.GetBaseException().ToString(), error); }
};
var report = new System.Collections.Generic.List<object>();
foreach (var original in originals)
for (int scenario = 0; scenario < 3; scenario++)
{
    float scale = scenario == 0 ? 0.5f : scenario == 1 ? 2 : 1;
    bool blocked = scenario == 2;
    var fixture = new UPlayGround.EditorTools.Tests.ReactiveEnvironmentIntegrationTests();
    fixture.SetUp();
    try
    {
        var fixtureScene = (UnityEngine.SceneManagement.Scene)fixture.GetType().GetField("_scene", flags).GetValue(fixture);
        var world = (UPlayGround.Manager.WorldStateManager)fixture.GetType().GetField("_world", flags).GetValue(fixture);
        var fixtureTarget = (UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject)fixture.GetType().GetField("_target", flags).GetValue(fixture);
        var root = UnityEngine.Object.Instantiate(original.gameObject); root.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, fixtureScene);
        root.transform.SetPositionAndRotation(new Vector3(20000, 4000, 20000), Quaternion.identity);
        root.transform.localScale = Vector3.one * scale;
        root.GetComponent<UPlayGround.Components.SceneEntityId>().RuntimeSetGuid("tree-probe-" + original.Definition.name + "-" + scenario);
        var target = root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
        set(target, "_weather", fixtureTarget.GetType().GetField("_weather", flags).GetValue(fixtureTarget));
        invoke(target, "Awake", new object[0]);
        if (!target.IsAuthoringValid() || !world.RegisterEnvironment(target)) throw new System.Exception("복제본 등록 실패");
        set(target, "_isBound", true); root.SetActive(true);
        var floor = new GameObject("검사 지면"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floor, fixtureScene);
        floor.transform.position = root.transform.position + Vector3.down * 0.5f;
        floor.layer = LayerMask.NameToLayer("Ground"); floor.AddComponent<BoxCollider>().size = new Vector3(200, 1, 200);
        if (blocked)
        {
            var wall = new GameObject("낙하 막힘 검사"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, fixtureScene);
            wall.transform.position = root.transform.position + Vector3.up * 5;
            wall.layer = LayerMask.NameToLayer("Ground"); wall.AddComponent<BoxCollider>().size = Vector3.one * 20;
        }
        Physics.SyncTransforms();
        var hit = (Collider)new UnityEditor.SerializedObject(target).FindProperty("_hitColliders").GetArrayElementAtIndex(0).objectReferenceValue;
        if (UPlayGround.Combat.CombatHitDetector.ResolveDamageable(hit) != target) throw new System.Exception("전투 Collider 해석 실패");
        var attack = new UPlayGround.Data.AttackData { damage = 25, attackDirection = Vector3.forward, hitPoint = root.transform.position + Vector3.up, hitTarget = hit.gameObject,
            environment = new UPlayGround.Data.ReactiveEnvironment.EnvironmentHitPayload { kind = UPlayGround.Core.ReactiveEnvironment.EnvironmentImpactKind.Slash } };
        for (int i = 0; i < 4; i++)
        {
            var request = UPlayGround.Combat.HitRequest.FromAttackData(attack);
            target.GetType().GetMethod("ReceiveHit").Invoke(target, new object[] { request });
            if (target.State.durability != 100 - (i + 1) * 25) throw new System.Exception("실제 피격 내구도 감소 실패");
            if (target.State.isBroken != (i == 3)) throw new System.Exception("파괴 임계값 오류");
        }
        var intact = root.transform.Find("Intact"); var remnant = root.transform.Find("Remnant");
        if (intact.gameObject.activeSelf || !remnant.gameObject.activeSelf) throw new System.Exception("파괴 형상 전환 실패");
        if (hit.gameObject.activeInHierarchy) throw new System.Exception("원형 피격 Collider 잔존");
        var pivot = remnant.Find("CutPivot"); var fallback = remnant.Find("BlockedLog");
        if (fallback.gameObject.activeSelf != blocked) throw new System.Exception("낙하/막힘 분기 오류: " + original.Definition.name + " scale=" + scale + " expectedBlocked=" + blocked);
        invoke(world, "FixedTickEnvironment", new object[] { 0.5f });
        var save = new UPlayGround.Data.Save.WorldStateSaveData(); invoke(world, "ExportEnvironment", new object[] { save });
        var record = save.reactiveEnvironment.Find(r => r.guid == target.StableId);
        if (!record.state.isBroken || record.destruction.usedFallback != blocked || !blocked && record.destruction.progress <= 0) throw new System.Exception("파괴 저장 누락");
        Quaternion savedPose = pivot.localRotation;
        var serializer = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        string json = (string)serializer.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { save });
        var restored = (UPlayGround.Data.Save.WorldStateSaveData)serializer.GetMethod("DeserializeObject", new[] { typeof(string), typeof(System.Type) }).Invoke(null, new object[] { json, typeof(UPlayGround.Data.Save.WorldStateSaveData) });
        invoke(world, "FixedTickEnvironment", new object[] { 0.5f });
        invoke(world, "ImportEnvironment", new object[] { restored });
        if (!target.State.isBroken || Quaternion.Angle(savedPose, pivot.localRotation) > 0.01f) throw new System.Exception("JSON 파괴 포즈 복원 실패");
        var drops = fixture.GetType().GetField("_drops", flags).GetValue(fixture);
        for (int step = 0; step < 80; step++)
        {
            invoke(world, "FixedTickEnvironment", new object[] { 0.05f });
            invoke(world, "TickEnvironment", new object[] { 0.05f });
        }
        var completed = new UPlayGround.Data.Save.WorldStateSaveData(); invoke(world, "ExportEnvironment", new object[] { completed });
        var finalRecord = completed.reactiveEnvironment.Find(r => r.guid == target.StableId);
        if (finalRecord.destruction.completion.phase != UPlayGround.Data.Save.EnvironmentCompletionPhase.Cleared
            || !finalRecord.rewardCommitted || target.HasPendingFixedTick || remnant.gameObject.activeSelf
            || root.GetComponentsInChildren<Renderer>(true).Any(r => r.gameObject.activeInHierarchy)
            || root.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && c.gameObject.activeInHierarchy)
            || root.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true).Any(o => o.enabled && o.gameObject.activeInHierarchy))
            throw new System.Exception("잔해/충돌/Nav/보상 완료 실패: " + original.Definition.name);
        int rewardCalls = (int)drops.GetType().GetField("calls").GetValue(drops);
        int createdRewards = (int)drops.GetType().GetField("created").GetValue(drops);
        if (createdRewards != 1) throw new System.Exception("보상 생성 1회 실패: " + createdRewards);
        invoke(world, "ImportEnvironment", new object[] { completed });
        invoke(world, "TickEnvironment", new object[] { 0.2f });
        if ((int)drops.GetType().GetField("calls").GetValue(drops) != rewardCalls || remnant.gameObject.activeSelf)
            throw new System.Exception("단말 복원 중복 실패");
        report.Add(new { cleared = true, createdRewards = createdRewards, rewardCalls = rewardCalls, terminalReload = true, variant = original.Definition.name, scale = scale, blocked = blocked, colliderResolved = true, hitDurabilities = new[] { 75, 50, 25, 0 }, broken = true, jsonPoseRestored = true, progress = record.destruction.progress });
    }
    finally { fixture.TearDown(); }
}
var serialize = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
string result = (string)serialize.Invoke(null, new object[] { report });
System.IO.Directory.CreateDirectory("output/ReactiveEnvironment/TreeCompletion-20261115");
System.IO.File.WriteAllText("output/ReactiveEnvironment/TreeCompletion-20261115/combat-probe.json", result);
return result;
