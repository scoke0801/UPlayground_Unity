var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if (!scene.IsValid() || !scene.isLoaded || Application.isPlaying || EditorApplication.isCompiling)
    throw new System.Exception("돌등성 Edit Mode에서 실행하세요.");
var environment = scene.GetRootGameObjects().First(g => g.name == "CityEnvironment");
var art = environment.transform.Find("StonegateEastArt");
var awnings = art.Cast<Transform>().Where(t => t.name == "MarketTileAwning").ToArray();
if (awnings.Length != 4 || art.Find("CitadelSquareWell") != null || awnings.Any(t => t.Find("AwningFrame") != null))
    throw new System.Exception("우물과 차양의 현재 구성을 먼저 확인하세요.");
string beamPath = "Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/TGV_Beam_01A.prefab";
string wellPath = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/OutdoorProps/PF_Farm_Well_01.prefab";
var beamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(beamPath);
var wellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wellPath);
if (beamPrefab == null || wellPrefab == null) throw new System.Exception("배치 원본이 없습니다.");
System.Func<GameObject, Bounds> GetBounds = obj => {
    var lod = obj.GetComponent<LODGroup>();
    var renderers = lod == null ? obj.GetComponentsInChildren<Renderer>() : lod.GetLODs()[0].renderers;
    var bounds = renderers[0].bounds;
    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
    return bounds;
};
System.Action<GameObject> Record = obj => {
    foreach (var t in obj.GetComponentsInChildren<Transform>(true)) {
        t.gameObject.layer = LayerMask.NameToLayer("Ground");
        PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
    }
    PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
};
Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("광장 우물과 장터 차양 지지 구조");
try {
    System.Func<Transform, string, Vector3, Vector3, GameObject> Beam = (parent, name, bottom, size) => {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(beamPrefab, scene);
        Undo.RegisterCreatedObjectUndo(obj, "차양 목재 구조 생성");
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        var b = GetBounds(obj);
        obj.transform.localScale = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
        b = GetBounds(obj);
        obj.transform.position += parent.TransformPoint(bottom) - new Vector3(b.center.x, b.min.y, b.center.z);
        Record(obj);
        return obj;
    };
    foreach (var awning in awnings) {
        // 뒤쪽 두 기둥만 있고 지붕과도 떨어져 있던 구조를 네 기둥과 폐합 보로 교체한다.
        foreach (var old in awning.Cast<Transform>().Where(t => t.name == "AwningPost").ToArray())
            Undo.DestroyObjectImmediate(old.gameObject);
        var frame = new GameObject("AwningFrame");
        Undo.RegisterCreatedObjectUndo(frame, "차양 지지 구조 생성");
        frame.transform.SetParent(awning, false);
        for (int side = 0; side < 2; side++) {
            float z = side == 0 ? -1.6f : 1.6f;
            for (int end = 0; end < 2; end++)
                Beam(frame.transform, "AwningPost_" + side + "_" + end, new Vector3(end == 0 ? -2.7f : 2.7f, 0, z), new Vector3(.24f, 3.5f, .24f));
            Beam(frame.transform, "AwningCrossbeam_" + side, new Vector3(0, 3.5f, z), new Vector3(5.75f, .25f, .24f));
            Beam(frame.transform, "AwningSideBeam_" + side, new Vector3(side == 0 ? -2.7f : 2.7f, 3.5f, 0), new Vector3(.24f, .25f, 3.45f));
        }
    }
    var well = (GameObject)PrefabUtility.InstantiatePrefab(wellPrefab, scene);
    Undo.RegisterCreatedObjectUndo(well, "광장 우물 배치");
    well.name = "CitadelSquareWell";
    well.transform.SetParent(art, false);
    well.transform.rotation = Quaternion.Euler(0, 20, 0) * wellPrefab.transform.rotation;
    var wellFilter = well.GetComponent<LODGroup>().GetLODs()[0].renderers[0].GetComponent<MeshFilter>();
    var stoneSlot = System.Array.FindIndex(wellFilter.GetComponent<Renderer>().sharedMaterials, m => m.name == "M_Farm_StoneTrim");
    if (stoneSlot < 0) throw new System.Exception("우물 석재 기단을 찾지 못했습니다.");
    var stoneIndices = wellFilter.sharedMesh.GetIndices(stoneSlot);
    var wellVertices = wellFilter.sharedMesh.vertices;
    float stoneBottom = stoneIndices.Min(i => wellFilter.transform.TransformPoint(wellVertices[i]).y);
    var wellBounds = GetBounds(well);
    // 원본 기둥은 땅에 묻는 말뚝까지 포함한다. 기둥 끝을 바닥으로 잡으면 석조 몸체가 뜬다.
    well.transform.localScale *= 3.5f / (wellBounds.max.y - stoneBottom);
    wellBounds = GetBounds(well);
    stoneBottom = stoneIndices.Min(i => wellFilter.transform.TransformPoint(wellVertices[i]).y);
    // 중앙 축의 왼편 통행 폭을 남기고 기존 광장 바닥에 접지한다.
    well.transform.position += new Vector3(183.5f - wellBounds.center.x, 10.04f - stoneBottom, 191 - wellBounds.center.z);
    Record(well);
    if (well.GetComponentsInChildren<Collider>().Length == 0) throw new System.Exception("우물 충돌체가 없습니다.");
    Physics.SyncTransforms();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    Undo.CollapseUndoOperations(group);
    return "차양 4곳: 네 기둥과 보 연결. 광장: 지붕과 두레박 구조가 있는 석조 우물 배치. " + GetBounds(well);
} catch {
    Undo.RevertAllDownToGroup(group);
    throw;
}
