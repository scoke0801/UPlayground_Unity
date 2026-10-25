var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if (!scene.IsValid() || !scene.isLoaded || Application.isPlaying || EditorApplication.isCompiling)
    throw new System.Exception("돌등성 Edit Mode에서 실행하세요.");
var environment = scene.GetRootGameObjects().First(g => g.name == "CityEnvironment");
if (environment.transform.Find("CitadelCourtyardDressing") != null) throw new System.Exception("이미 생활 공간이 배치되어 있습니다.");
string golden = "Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/";
string farm = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/";
string nature = "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/";
var terrain = environment.GetComponentInChildren<Terrain>();
System.Func<GameObject, Bounds> GetBounds = obj => {
    var lod = obj.GetComponent<LODGroup>();
    var renderers = lod == null ? obj.GetComponentsInChildren<Renderer>() : lod.GetLODs()[0].renderers;
    var b = renderers[0].bounds;
    foreach (var r in renderers) b.Encapsulate(r.bounds);
    return b;
};
Undo.IncrementCurrentGroup();
int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("돌등성 광장과 장터 생활 공간");
var report = new System.Collections.Generic.List<string>();
try {
    var root = new GameObject("CitadelCourtyardDressing");
    Undo.RegisterCreatedObjectUndo(root, "생활 공간 배치");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
    root.transform.SetParent(environment.transform, false);
    System.Func<string, Transform> Group = name => {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, "생활 공간 묶음");
        g.transform.SetParent(root.transform, false);
        return g.transform;
    };
    System.Func<string, Transform, Vector3, float, float, GameObject> Place = (path, parent, bottom, yaw, scale) => {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new System.Exception("원본 없음: " + path);
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(obj, "생활 소품 배치");
        obj.transform.SetParent(parent, false);
        obj.transform.rotation = Quaternion.Euler(0, yaw, 0) * prefab.transform.rotation;
        obj.transform.localScale = prefab.transform.localScale * scale;
        var b = GetBounds(obj);
        obj.transform.position += bottom - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var t in obj.GetComponentsInChildren<Transform>(true)) {
            t.gameObject.layer = LayerMask.NameToLayer("Ground");
            PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        foreach (var light in obj.GetComponentsInChildren<Light>(true)) {
            light.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return obj;
    };
    System.Func<Vector2, Vector3> Ground = p => new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0, p.y)) + terrain.transform.position.y + .015f, p.y);

    var gardens = Group("CourtyardGardens");
    var treePoints = new[] {new Vector2(165,187), new Vector2(201,202), new Vector2(164,206), new Vector2(201,184), new Vector2(143,151), new Vector2(220,155), new Vector2(142.5f,215), new Vector2(222,224), new Vector2(148,133), new Vector2(215,132), new Vector2(155,194), new Vector2(207,208), new Vector2(158,174), new Vector2(205,173), new Vector2(166,214), new Vector2(214,230)};
    for (int i = 0; i < treePoints.Length; i++) {
        string treeName = i < 2 ? "BroadleafTree_03_Green" : i >= 10 ? "BroadleafTree_02_Green" : i % 2 == 0 ? "BroadleafTree_03_Green" : "BroadleafTree_04_Green";
        var tree = Place(nature + treeName + ".prefab", gardens, Ground(treePoints[i]), i * 37, 1);
        var b = GetBounds(tree);
        float height = i >= 10 ? 8.5f : i < 4 ? 7.5f : 6.5f;
        tree.transform.localScale *= height / b.size.y;
        b = GetBounds(tree);
        tree.transform.position += Ground(treePoints[i]) - new Vector3(b.center.x, b.min.y, b.center.z);
        tree.transform.position -= Vector3.up * .12f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
        for (int j = 0; j < 3; j++) {
            var p = treePoints[i] + new Vector2((j - 1) * .85f, 1.2f + (j % 2) * .35f);
            var flower = Place(nature + (j == 1 ? "Bush_01_02" : "FlowerMeadow_White") + ".prefab", gardens, Ground(p), i * 29 + j * 51, j == 1 ? .45f : .55f);
            foreach (var collider in flower.GetComponentsInChildren<Collider>()) {
                collider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
    }
    report.Add("광장과 주거지 나무 16그루, 하부 관목·꽃 48개");

    var square = Group("WellSquareSeating");
    var beds = new[] { new Vector2(174.5f,187), new Vector2(193,187), new Vector2(173.5f,200), new Vector2(192,204.5f) };
    for (int i = 0; i < beds.Length; i++) {
        var planter = Place(farm + "Props/OutdoorProps/PF_Farm_Planter_01.prefab", square, Ground(beds[i]) + Vector3.up * .03f, i % 2 == 0 ? 0 : 180, 1.15f);
        var b = GetBounds(planter);
        var scale = planter.transform.localScale;
        planter.transform.localScale = new Vector3(scale.x * 3.2f / b.size.x, scale.y * .65f / b.size.y, scale.z * 1.3f / b.size.z);
        b = GetBounds(planter);
        planter.transform.position += Ground(beds[i]) + Vector3.up * .03f - new Vector3(b.center.x, b.min.y, b.center.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(planter.transform);
        b = GetBounds(planter);
        for (int j = 0; j < 10; j++) {
            float x = Mathf.Lerp(b.min.x + .35f, b.max.x - .35f, (j % 5) / 4f);
            float z = b.center.z + (j < 5 ? -.27f : .27f);
            Place(nature + (i % 2 == 0 ? "FlowerMeadow_White" : "FlowerMeadow_Orange") + ".prefab", square, new Vector3(x, b.max.y - .04f, z), i * 27 + j * 60, 1.2f);
        }
    }
    Place(farm + "Props/OutdoorProps/PF_Farm_Bench_01.prefab", square, Ground(new Vector2(176.5f,202)), 180, 1.1f);
    Place(farm + "Props/OutdoorProps/PF_Farm_Bench_01.prefab", square, Ground(new Vector2(188,202.5f)), 180, 1.1f);
    Place(golden + "Props/Exterior Props/TGV_Lamp_Post_01A.prefab", square, Ground(new Vector2(175,205)), 20, .85f);
    Place(golden + "Props/Exterior Props/TGV_Lamp_Post_01A.prefab", square, Ground(new Vector2(195,186)), 200, .85f);
    report.Add("우물 쉼터: 화단 4개, 꽃 40개, 벤치 2개, 등주 2개");

    var market = Group("MarketDisplays");
    var tables = new[] { new Vector2(165.1f,143.7f), new Vector2(164.3f,158.2f), new Vector2(197,150.7f), new Vector2(197,165.4f) };
    for (int i = 0; i < tables.Length; i++) {
        var table = Place(golden + "Buildings/Interior Props/TGV_Table_01A.prefab", market, Ground(tables[i]) + Vector3.up * .04f, 0, .85f);
        var b = GetBounds(table);
        for (int j = 0; j < 4; j++) {
            string goods = i < 2 ? golden + "Props/Food/TGV_Bread_0" + (j % 3 + 1) + "A.prefab" : golden + "Props/Food/TGV_Jar_01" + (j % 2 == 0 ? "A" : "B") + ".prefab";
            var item = Place(goods, market, new Vector3(Mathf.Lerp(b.min.x + .25f, b.max.x - .25f, j / 3f), b.max.y + .01f, b.center.z), j * 39, i < 2 ? .8f : .65f);
            foreach (var collider in item.GetComponentsInChildren<Collider>()) {
                collider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
        Place(golden + "Props/Exterior Props/TGV_Sack_02A.prefab", market, Ground(tables[i] + new Vector2(-1.6f, .6f)), i * 29, .8f);
        Place(farm + "Props/FarmTools/PF_Farm_Basket_01.prefab", market, Ground(tables[i] + new Vector2(1.35f, .75f)), i * 17, .7f);
    }
    report.Add("장터 차양 아래 진열대 4개, 상품 16개, 자루·바구니 8개");

    var yards = Group("ResidentialYards");
    var yardPoints = new[] { new Vector2(145.5f,157), new Vector2(221,162), new Vector2(142,208), new Vector2(223,201), new Vector2(151,223), new Vector2(209,220) };
    for (int i = 0; i < yardPoints.Length; i++) {
        var p = yardPoints[i];
        Place(golden + "Props/Exterior Props/TGV_Wood_Barrel_01A.prefab", yards, Ground(p), i * 35, .9f);
        var crate = Place(golden + "Props/Exterior Props/TGV_Wood_Crate_01A.prefab", yards, Ground(p + new Vector2(1.2f, .25f)), i * 35 + 13, .85f);
        var b = GetBounds(crate);
        Place(golden + "Props/Exterior Props/TGV_Sack_01A.prefab", yards, new Vector3(b.center.x, b.max.y + .01f, b.center.z), i * 19, .55f);
        Place(farm + "Props/Pots/PF_Farm_Pot_C_01.prefab", yards, Ground(p + new Vector2(-.8f, .8f)), i * 13, .85f);
    }
    Place(golden + "Props/Exterior Props/TGV_WheelBarrow_01A.prefab", yards, Ground(new Vector2(211,209)), 25, 1);
    Place(golden + "Props/Exterior Props/TGV_Wood_Fence_01A.prefab", yards, Ground(new Vector2(144,154)), 90, 1);
    Place(golden + "Props/Exterior Props/TGV_Wood_Fence_01A.prefab", yards, Ground(new Vector2(221,159)), 90, 1);
    report.Add("집 앞 생활 물품 6묶음, 손수레 1개, 낮은 울타리 2개");
    Physics.SyncTransforms();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    Undo.CollapseUndoOperations(undoGroup);
} catch {
    Undo.RevertAllDownToGroup(undoGroup);
    throw;
}
System.IO.Directory.CreateDirectory("output/CitadelPlacementFix");
System.IO.File.WriteAllLines("output/CitadelPlacementFix/dressing.txt", report.ToArray());
return string.Join("\n", report.ToArray());
