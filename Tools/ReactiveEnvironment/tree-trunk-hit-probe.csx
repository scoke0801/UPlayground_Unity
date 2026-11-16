// 실제 본편 나무 복제본 두 개로 수관 상자 오탐과 줄기 capsule 수정을 같은 물리 질의에서 비교한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var bridge = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost")).FirstOrDefault(t => t != null);
var queued = bridge != null ? bridge.GetProperty("QueuedCommandCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic) : null;
if (queued != null && (int)queued.GetValue(null, null) > 1) return "BUSY other MCP";
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    var api = assembly.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi");
    var active = api != null ? api.GetMethod("IsTestExecutionActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) : null;
    if (active != null && (bool)active.Invoke(null, null)) return "BUSY tests";
}
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("LakeVillageWorld");
var samples = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).Where(t => t.Definition != null && t.Definition.reaction.canBreak).GroupBy(t => t.Definition).Select(g => g.First()).ToArray();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var collect = typeof(UPlayGround.Combat.CombatHitDetector).GetMethod("CollectShapeHits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var cache = (System.Collections.IDictionary)typeof(UPlayGround.Combat.CombatHitDetector).GetField("_damageableCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
var report = new System.Collections.Generic.List<object>();
foreach (var original in samples)
foreach (float scale in new[] { 0.5f, 1f, 2f })
{
    var fixture = new UPlayGround.EditorTools.Tests.ReactiveEnvironmentIntegrationTests(); fixture.SetUp();
    var roots = new System.Collections.Generic.List<GameObject>();
    var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    var physicsScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
    UPlayGround.Manager.World.RegionOwnership.Register(physicsScene, "TreeHitPhysicsProbe");
    try
    {
        var preview = (UnityEngine.SceneManagement.Scene)fixture.GetType().GetField("_scene", flags).GetValue(fixture);
        var world = (UPlayGround.Manager.WorldStateManager)fixture.GetType().GetField("_world", flags).GetValue(fixture);
        var reference = fixture.GetType().GetField("_target", flags).GetValue(fixture);
        var targets = new UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject[2];
        for (int i = 0; i < 2; i++)
        {
            var root = UnityEngine.Object.Instantiate(original.gameObject); root.SetActive(false); roots.Add(root);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, physicsScene);
            root.transform.SetPositionAndRotation(new Vector3(30000 + i * 10 * scale, 4000, 30000), Quaternion.identity); root.transform.localScale = Vector3.one * scale;
            root.GetComponent<UPlayGround.Components.SceneEntityId>().RuntimeSetGuid("trunk-hit-" + original.Definition.name + "-" + scale + "-" + i);
            var target = root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(); targets[i] = target;
            target.GetType().GetField("_weather", flags).SetValue(target, reference.GetType().GetField("_weather", flags).GetValue(reference));
            target.GetType().GetMethod("Awake", flags).Invoke(target, null);
            if (!world.RegisterEnvironment(target)) throw new System.Exception("복제본 등록 실패");
            target.GetType().GetField("_isBound", flags).SetValue(target, true); root.SetActive(true);
            // 이미 수정된 본편에서도 이전 Box 입력을 명시적으로 재현한다.
            var proxy = root.transform.Find("Intact/EnvironmentReactionHit"); var oldBox = proxy.GetComponent<BoxCollider>(); oldBox.enabled = true;
            var capsule = proxy.GetComponent<CapsuleCollider>(); if (capsule != null) capsule.enabled = false;
            target.GetType().GetField("_hitColliders", flags).SetValue(target, new Collider[] { oldBox });
        }
        Physics.SyncTransforms();
        var shape = UPlayGround.Combat.CombatHitboxShape.Sphere(roots[0].transform.position + Vector3.up * (1.1f * scale), 0.2f * scale);
        var hits = new System.Collections.Generic.List<UPlayGround.Combat.CombatHit>();
        var matched = new System.Collections.Generic.HashSet<UPlayGround.IDamageable>();
        collect.Invoke(null, new object[] { null, shape, (LayerMask)LayerMask.GetMask("InteractableObject"), new Collider[64], null, matched, hits, false, Vector3.forward, null });
        bool beforeBoth = matched.Contains(targets[0]) && matched.Contains(targets[1]);
        if (!beforeBoth) throw new System.Exception("기존 이웃 오탐 재현 실패: " + original.Definition.name);
        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i]; var proxy = target.transform.Find("Intact/EnvironmentReactionHit");
            var oldBox = proxy.GetComponent<BoxCollider>(); oldBox.enabled = false; cache.Remove(oldBox);
            var capsule = proxy.GetComponent<CapsuleCollider>(); if (capsule == null) capsule = proxy.gameObject.AddComponent<CapsuleCollider>();
            capsule.enabled = true; capsule.isTrigger = true; capsule.direction = 1; capsule.radius = target.Definition.destruction.trunkRadius;
            capsule.height = Mathf.Max(capsule.radius * 2, target.Definition.destruction.trunkLength + target.transform.Find("Remnant/CutPivot").localPosition.y);
            capsule.center = Vector3.up * capsule.height * 0.5f;
            target.GetType().GetField("_hitColliders", flags).SetValue(target, new Collider[] { capsule }.Concat(target.transform.Find("Remnant").GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger)).ToArray()); cache.Remove(capsule);
        }
        Physics.SyncTransforms(); matched.Clear(); hits.Clear();
        collect.Invoke(null, new object[] { null, shape, (LayerMask)LayerMask.GetMask("InteractableObject"), new Collider[64], null, matched, hits, false, Vector3.forward, null });
        if (matched.Count != 1 || !matched.Contains(targets[0]) || matched.Contains(targets[1])) throw new System.Exception("줄기 수정 후 이웃 오탐: " + original.Definition.name);
        var request = UPlayGround.Combat.HitRequest.FromAttackData(new UPlayGround.Data.AttackData { damage = 25, attackDirection = Vector3.forward, hitPoint = shape.Center, hitTarget = targets[0].transform.Find("Intact/EnvironmentReactionHit").gameObject });
        foreach (var target in targets) if (matched.Contains(target)) target.GetType().GetMethod("ReceiveHit").Invoke(target, new object[] { request });
        if (targets[0].State.durability != 75 || targets[1].State.durability != 100) throw new System.Exception("피해 대상 분리 실패");
        int afterTargets = matched.Count;
        var finish = UPlayGround.Combat.HitRequest.FromAttackData(new UPlayGround.Data.AttackData { damage = 75, attackDirection = Vector3.forward, hitPoint = shape.Center, hitTarget = request.HitTarget });
        targets[0].GetType().GetMethod("ReceiveHit").Invoke(targets[0], new object[] { finish });
        Physics.SyncTransforms(); matched.Clear(); hits.Clear();
        collect.Invoke(null, new object[] { null, shape, (LayerMask)LayerMask.GetMask("InteractableObject"), new Collider[64], null, matched, hits, false, Vector3.forward, null });
        if (!targets[0].State.isBroken || matched.Contains(targets[1]) || targets[1].State.durability != 100) throw new System.Exception("파괴된 첫 나무 주변에서 이웃 오탐");
        report.Add(new { variant = original.Definition.name, scale = scale, neighborDistance = 10 * scale, beforeBothHit = beforeBoth, afterTargets = afterTargets, firstDurabilityAfterHit = 75, neighborDurability = targets[1].State.durability, firstBroken = targets[0].State.isBroken, neighborHitFromStump = matched.Contains(targets[1]) });
    }
    finally
    {
        foreach (var root in roots) if (root != null) foreach (var collider in root.GetComponentsInChildren<Collider>(true)) cache.Remove(collider);
        foreach (var root in roots) if (root != null) UnityEngine.Object.DestroyImmediate(root);
        UPlayGround.Manager.World.RegionOwnership.Unregister(physicsScene);
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(physicsScene, true);
        fixture.TearDown();
    }
}
var serialize = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText("output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/probe.json", (string)serialize.Invoke(null, new object[] { report }));
return new { cases = report.Count, passed = true };
