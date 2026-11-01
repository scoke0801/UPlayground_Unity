var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var resultPath = "output/OpenWorld/LakeWorldReview/shore-application.txt";
if (System.IO.File.Exists(resultPath)) return System.IO.File.ReadAllText(resultPath);
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
    throw new Exception("저장된 월드의 Edit Mode가 필요합니다.");
if (!System.IO.File.Exists("output/OpenWorld/LakeWorldReview/Terrain.before.asset") || !System.IO.File.Exists("output/OpenWorld/LakeWorldReview/LakeVillageWorld.before.unity"))
    throw new Exception("원본 백업을 확인해야 합니다.");
var terrain = Terrain.activeTerrain;
var data = terrain.terrainData;
var origin = terrain.transform.position;
var bank = GameObject.Find("LakeSouthBank_R2");
if (bank == null) throw new Exception("기존 남쪽 바닥을 찾을 수 없습니다.");
float sx = data.size.x / (data.heightmapResolution - 1), sz = data.size.z / (data.heightmapResolution - 1);
int x0 = Mathf.FloorToInt((880 - origin.x) / sx), x1 = Mathf.CeilToInt((1255 - origin.x) / sx);
int z0 = Mathf.FloorToInt((550 - origin.z) / sz), z1 = Mathf.CeilToInt((1120 - origin.z) / sz);
int width = x1 - x0 + 1, height = z1 - z0 + 1;
var heights = data.GetHeights(x0, z0, width, height);
var distance = new float[height, width];
for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
    distance[z, x] = heights[z, x] * data.size.y + origin.y >= 80.4f ? 0 : 10000;
float diagonal = Mathf.Sqrt(sx * sx + sz * sz);
for (int z = 0; z < height; z++) for (int x = 0; x < width; x++) {
    float d = distance[z, x];
    if (x > 0) d = Mathf.Min(d, distance[z, x - 1] + sx);
    if (z > 0) d = Mathf.Min(d, distance[z - 1, x] + sz);
    if (z > 0 && x > 0) d = Mathf.Min(d, distance[z - 1, x - 1] + diagonal);
    if (z > 0 && x + 1 < width) d = Mathf.Min(d, distance[z - 1, x + 1] + diagonal);
    distance[z, x] = d;
}
for (int z = height - 1; z >= 0; z--) for (int x = width - 1; x >= 0; x--) {
    float d = distance[z, x];
    if (x + 1 < width) d = Mathf.Min(d, distance[z, x + 1] + sx);
    if (z + 1 < height) d = Mathf.Min(d, distance[z + 1, x] + sz);
    if (z + 1 < height && x + 1 < width) d = Mathf.Min(d, distance[z + 1, x + 1] + diagonal);
    if (z + 1 < height && x > 0) d = Mathf.Min(d, distance[z + 1, x - 1] + diagonal);
    distance[z, x] = d;
}
int changed = 0;
for (int z = 0; z < height; z++) for (int x = 0; x < width; x++) {
    float wx = origin.x + (x0 + x) * sx, wz = origin.z + (z0 + z) * sz;
    float radius = Mathf.Sqrt(Mathf.Pow((wx - 1067) / 175f, 2) + Mathf.Pow((wz - 836) / 285f, 2));
    float lakeWeight = Mathf.SmoothStep(0, 1, Mathf.Clamp01((1.08f - radius) / .12f));
    float old = heights[z, x] * data.size.y + origin.y;
    float floor = Mathf.Max(73.5f, 80.4f - distance[z, x] * .32f);
    float newHeight = Mathf.Lerp(old, Mathf.Max(old, floor), lakeWeight);
    float rect = Mathf.Min(Mathf.Min(wx - 945, 1205 - wx), Mathf.Min(wz - 580, 735 - wz));
    float bankWeight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(rect / 26f));
    float bankHeight = 80.85f + (Mathf.PerlinNoise(wx * .035f, wz * .035f) - .5f) * .16f;
    newHeight = Mathf.Lerp(newHeight, Mathf.Max(newHeight, bankHeight), bankWeight);
    if (newHeight > old + .01f) changed++;
    heights[z, x] = (newHeight - origin.y) / data.size.y;
}
data.SetHeights(x0, z0, heights);
terrain.Flush();
AssetDatabase.SaveAssetIfDirty(data);
Undo.DestroyObjectImmediate(bank);
var plants = GameObject.Find("생활과 주민/LakeAbundanceArt/ShoreAndHousehold/ShorelineClusters").transform;
int movedLilies = 0;
foreach (Transform plant in plants) {
    if (!plant.name.ToLowerInvariant().Contains("waterlily")) continue;
    var pos = plant.position;
    if (terrain.SampleHeight(pos) + origin.y < 76.65f) continue;
    bool found = false;
    for (float radius = 4; radius <= 250 && !found; radius += 4) for (int i = 0; i < 24; i++) {
        float angle = i * Mathf.PI * 2 / 24;
        var candidate = pos + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        float ellipse = Mathf.Pow((candidate.x - 1067) / 135f, 2) + Mathf.Pow((candidate.z - 836) / 230f, 2);
        if (ellipse >= .92f) continue;
        if (terrain.SampleHeight(candidate) + origin.y >= 76.65f) continue;
        plant.position = candidate;
        PrefabUtility.RecordPrefabInstancePropertyModifications(plant);
        movedLilies++; found = true; break;
    }
    if (!found) throw new Exception("수련의 수면 위치를 찾지 못했습니다: " + plant.name);
}
EditorUtility.SetDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
string result = "남쪽 던전 바닥 제거, 높이 표본 " + changed + "개 복원, 수련 " + movedLilies + "개 이동. NavMesh 재생성 필요.";
System.IO.File.WriteAllText(resultPath, result);
return result;
