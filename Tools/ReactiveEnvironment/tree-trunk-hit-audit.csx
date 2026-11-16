// 시각 범위 상자가 피격 입력에서 제외됐고 기존 물리 충돌/배치 ID가 유지되는지 확인한다.
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return "BUSY";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("LakeVillageWorld");
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).Where(t => t.Definition != null && t.Definition.reaction.canBreak && UnityEditor.AssetDatabase.GetAssetPath(t.Definition).StartsWith("Assets/10.Datas/Environment/TreeDestruction/")).ToArray();
int valid = 0, withoutPhysicalMesh = 0; var errors = new System.Collections.Generic.List<string>();
foreach (var target in all)
{
    var proxy = target.transform.Find("Intact/EnvironmentReactionHit");
    var capsule = proxy != null ? proxy.GetComponent<CapsuleCollider>() : null;
    var boxes = proxy != null ? proxy.GetComponents<BoxCollider>() : new BoxCollider[0];
    if (capsule == null || !capsule.enabled || !capsule.isTrigger || capsule.radius != target.Definition.destruction.trunkRadius
        || proxy.gameObject.layer != LayerMask.NameToLayer("InteractableObject") || !target.CanReceiveCollider(capsule)
        || boxes.Any(b => b.enabled || target.CanReceiveCollider(b))) { errors.Add(target.name + ": 줄기/상자 입력 오류"); continue; }
    var physical = target.transform.Find("Intact").GetComponentsInChildren<MeshCollider>(true);
    // 물리 Collider가 원래 없는 낙엽 장식도 있으므로 존재/활성을 강제하지 않는다. 원형 보존은 별도 전후 대조한다.
    if (physical.Length == 0) withoutPhysicalMesh++;
    if (physical.Any(c => target.CanReceiveCollider(c))) { errors.Add(target.name + ": 원래 물리 충돌/피격 분리 오류"); continue; }
    if (!target.GetComponent<UPlayGround.Components.SceneEntityId>().HasGuid) { errors.Add(target.name + ": ID 누락"); continue; }
    valid++;
}
var summary = new { total = all.Length, valid = valid, invalid = errors.Count, withoutPhysicalMesh = withoutPhysicalMesh, errors = errors.Take(20).ToArray(), sceneDirty = scene.isDirty };
var serialize = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText("output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/audit.json", (string)serialize.Invoke(null, new object[] { summary }));
return summary;
