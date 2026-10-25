var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if (!scene.IsValid() || !scene.isLoaded || Application.isPlaying || EditorApplication.isCompiling)
    throw new System.Exception("돌등성 Edit Mode에서 실행하세요.");
var environment = scene.GetRootGameObjects().First(g => g.name == "CityEnvironment");
var art = environment.transform.Find("StonegateEastArt");
var terrain = environment.GetComponentInChildren<Terrain>();
var report = new System.Collections.Generic.List<string>();
System.Func<Transform, Bounds> GetBounds = t => {
    var renderers = t.GetComponentsInChildren<Renderer>();
    var bounds = renderers[0].bounds;
    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
    return bounds;
};
System.Action<Transform, Vector2> Move = (t, point) => {
    if (t == null) throw new System.Exception("배치 대상이 없습니다.");
    Undo.RecordObject(t, "돌등성 배치 간격 수정");
    var before = t.position;
    t.position = new Vector3(point.x, before.y, point.y);
    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
    report.Add(t.name + ": " + before + " → " + t.position);
};
System.Action<Transform> Remove = t => {
    if (t == null) return;
    report.Add("제거: " + t.name + " " + t.position);
    Undo.DestroyObjectImmediate(t.gameObject);
};

Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("돌등성 겹침과 나무 접지 수정");
try {
    Remove(environment.transform.Find("TownFountain"));
    Remove(environment.transform.Find("CitadelLife/MarketFinish/FountainWater"));
    Move(art.Find("JoinedHouse_WestB"), new Vector2(150, 209));
    Move(art.Find("JoinedHouse_EastB"), new Vector2(215, 206));

    // 같은 원본 나무를 두 번 변환해 만든 중복 인스턴스다.
    Remove(art.Find("CourtyardPine_2"));
    Move(art.Find("CourtyardPine_0"), new Vector2(143.4f, 156));
    Move(art.Find("CourtyardPine_3"), new Vector2(222, 209));
    Move(art.Find("CourtyardPine_8"), new Vector2(143, 143));
    Move(art.Find("CourtyardPine_9"), new Vector2(220, 145));
    Move(art.Find("CourtyardPine_10"), new Vector2(142.8f, 172));
    Move(art.Find("CourtyardPine_11"), new Vector2(224, 171));

    var trees = environment.GetComponentsInChildren<Transform>()
        .Where(t => t.gameObject.activeInHierarchy && (t.name == "WoodlandTree" ||
            t.name.StartsWith("BroadleafTree_") || t.name.StartsWith("CourtyardPine_"))).ToArray();
    int grounded = 0;
    foreach (var tree in trees) {
        var lod = tree.GetComponent<LODGroup>();
        var renderers = lod != null ? lod.GetLODs()[0].renderers : tree.GetComponentsInChildren<Renderer>();
        // 마지막 LOD의 impostor 사각형은 뿌리보다 낮다. 실제 첫 LOD의 메시만 기준으로 쓴다.
        var vertices = renderers.Where(r => r != null).Select(r => r.GetComponent<MeshFilter>())
            .Where(f => f != null && f.sharedMesh != null)
            .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
        if (vertices.Length == 0) throw new System.Exception("나무 메시 없음: " + tree.name);
        float bottom = vertices.Min(v => v.y);
        var central = vertices.Where(v => new Vector2(v.x - tree.position.x, v.z - tree.position.z).sqrMagnitude < .35f * .35f).ToArray();
        float contact = central.Length == 0 ? bottom : Mathf.Min(bottom + .35f, central.Min(v => v.y));
        float ground = terrain.SampleHeight(tree.position) + terrain.transform.position.y;
        float delta = ground - .05f - contact;
        if (Mathf.Abs(delta) < .08f) continue;
        Undo.RecordObject(tree, "실제 나무 뿌리를 지형에 접지");
        tree.position += Vector3.up * delta;
        PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
        report.Add("접지: " + tree.name + " 높이 보정=" + delta.ToString("F3"));
        grounded++;
    }

    var houses = art.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy &&
        (t.name.Contains("House") || t.name.StartsWith("RearLane") || t.name.StartsWith("Storehouse") || t.name == "MarketTileAwning")).ToArray();
    for (int i = 0; i < houses.Length; i++)
        for (int j = i + 1; j < houses.Length; j++)
            if (GetBounds(houses[i]).Intersects(GetBounds(houses[j])))
                throw new System.Exception("건물 겹침: " + houses[i].name + " / " + houses[j].name);
    report.Add("나무 검사=" + trees.Length + ", 접지 수정=" + grounded + ", 건물/차양 검사=" + houses.Length + ", 경계 겹침=0");
    Physics.SyncTransforms();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    Undo.CollapseUndoOperations(group);
} catch {
    Undo.RevertAllDownToGroup(group);
    throw;
}
System.IO.Directory.CreateDirectory("output/CitadelPlacementFix");
System.IO.File.WriteAllLines("output/CitadelPlacementFix/changes.txt", report.ToArray());
return string.Join("\n", report.ToArray());
