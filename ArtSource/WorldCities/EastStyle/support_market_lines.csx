var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if (!scene.IsValid() || !scene.isLoaded || Application.isPlaying || EditorApplication.isCompiling)
    throw new System.Exception("항구 Edit Mode에서 실행하세요.");
var environment = scene.GetRootGameObjects().First(g => g.name == "CityEnvironment");
var life = environment.transform.Find("HarborLife");
if (life.Find("MarketLineSupports") != null) throw new System.Exception("이미 지지대가 있습니다.");
var lines = life.Cast<Transform>().Where(t => t.name == "MarketLine").ToArray();
if (lines.Length != 3) throw new System.Exception("장식 줄 3개를 확인해야 합니다.");
var terrain = environment.GetComponentInChildren<Terrain>();
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/TGV_Beam_01A.prefab");
if (prefab == null) throw new System.Exception("목재 기둥 원본이 없습니다.");
Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("시장 장식 줄 양 끝 지지대");
var report = new System.Collections.Generic.List<string>();
try {
    var supports = new GameObject("MarketLineSupports");
    Undo.RegisterCreatedObjectUndo(supports, "시장 장식 지지대 생성");
    supports.transform.SetParent(life, false);
    for (int i = 0; i < lines.Length; i++) {
        var line = lines[i];
        var bounds = line.GetComponent<MeshFilter>().sharedMesh.bounds;
        var ends = new[] { line.TransformPoint(new Vector3(0, 0, bounds.min.z)), line.TransformPoint(new Vector3(0, 0, bounds.max.z)) };
        for (int side = 0; side < ends.Length; side++) {
            var end = ends[side];
            float ground = terrain.SampleHeight(end) + terrain.transform.position.y;
            var post = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(post, "장식 줄 기둥 생성");
            post.name = "MarketLinePost_" + i + "_" + side;
            post.transform.SetParent(supports.transform, false);
            post.transform.localRotation = Quaternion.identity;
            post.transform.localScale = Vector3.one;
            var renderers = post.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var renderer in renderers) b.Encapsulate(renderer.bounds);
            post.transform.localScale = new Vector3(.24f / b.size.x, (end.y - ground + .25f) / b.size.y, .24f / b.size.z);
            b = renderers[0].bounds;
            foreach (var renderer in renderers) b.Encapsulate(renderer.bounds);
            post.transform.position += new Vector3(end.x - b.center.x, ground - .05f - b.min.y, end.z - b.center.z);
            foreach (var t in post.GetComponentsInChildren<Transform>(true)) {
                t.gameObject.layer = LayerMask.NameToLayer("Ground");
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(post.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(post);
            b = renderers[0].bounds;
            foreach (var renderer in renderers) b.Encapsulate(renderer.bounds);
            if (!b.Contains(end) || Mathf.Abs(b.min.y - ground) > .06f)
                throw new System.Exception("줄 끝 연결 또는 기둥 접지 실패: " + post.name);
            report.Add(post.name + " 줄 끝=" + end + ", 지면=" + ground.ToString("F3"));
        }
    }
    Physics.SyncTransforms();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    Undo.CollapseUndoOperations(group);
} catch {
    Undo.RevertAllDownToGroup(group);
    throw;
}
System.IO.Directory.CreateDirectory("output/CitadelPlacementFix");
System.IO.File.WriteAllLines("output/CitadelPlacementFix/market-line-supports.txt", report.ToArray());
return string.Join("\n", report.ToArray());
