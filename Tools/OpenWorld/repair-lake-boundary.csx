var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var resultPath = "output/OpenWorld/LakeWorldReview/boundary-application.txt";
if (System.IO.File.Exists(resultPath)) return System.IO.File.ReadAllText(resultPath);
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
    throw new Exception("저장된 월드의 Edit Mode가 필요합니다.");
var boundary = GameObject.Find("DeepLake");
var collider = boundary.GetComponent<MeshCollider>();
var source = collider.sharedMesh;
var verts = source.vertices;
var indices = source.triangles;
if (indices.Length % 12 != 0) throw new Exception("호수 경계 메시 형식이 예상과 다릅니다.");
var terrain = Terrain.activeTerrain;
var kept = new List<int>();
int removed = 0;
for (int i = 0; i < indices.Length; i += 12) {
    var p = boundary.transform.TransformPoint(verts[indices[i]]);
    var q = boundary.transform.TransformPoint(verts[indices[i + 2]]);
    var center = (p + q) * .5f;
    bool inSouth = center.x >= 945 && center.x <= 1205 && center.z >= 580 && center.z <= 735;
    bool onLand = terrain.SampleHeight(p) + terrain.transform.position.y > 79.3f && terrain.SampleHeight(q) + terrain.transform.position.y > 79.3f;
    if (inSouth && onLand) { removed++; continue; }
    for (int j = 0; j < 12; j++) kept.Add(indices[i + j]);
}
if (removed == 0 || kept.Count == 0) throw new Exception("변경할 경계 또는 보존할 경계가 없습니다.");
string path = "Assets/10.Datas/World/OpenWorld/LakeWorldReview/LakeBoundary.asset";
if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) throw new Exception("경계 대상 에셋이 이미 있습니다.");
var mesh = UnityEngine.Object.Instantiate(source);
mesh.name = "LakeBoundary_LandRestored";
mesh.triangles = kept.ToArray();
mesh.RecalculateBounds();
AssetDatabase.CreateAsset(mesh, path);
collider.sharedMesh = mesh;
PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
string result = "남쪽 육지를 가로지르는 호수 경계 " + removed + "개 구간 제거, 수변 경계 보존. NavMesh 재생성 필요.";
System.IO.File.WriteAllText(resultPath, result);
return result;
