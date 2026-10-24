// Unity MCP execute_code 메서드 본문. 원본 NPC와 현재 씬을 저장하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
    throw new System.InvalidOperationException("편집 모드의 컴파일 완료 후 실행하세요.");
string folder = "Assets/08.Models/Equipment/P09RangerVest";
string prefabFolder = "Assets/03.Prefabs/Characters/EquipmentReview";
System.IO.Directory.CreateDirectory(prefabFolder);
AssetDatabase.Refresh();
string modelPath = folder + "/P09_RangerVest.fbx";
AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
string texturePath = "Assets/04.Images/Equipment/P09RangerVest/ChestnutLeather_BaseColor.png";
AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
textureImporter.maxTextureSize = 2048;
textureImporter.sRGBTexture = true;
textureImporter.mipmapEnabled = true;
textureImporter.wrapMode = TextureWrapMode.Repeat;
textureImporter.SaveAndReimport();
var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Armor/P09_Male_Armor_002.mat");
var materials = new Material[3];
string[] names = { "RangerVest_Leather", "RangerVest_Edge", "RangerVest_Brass" };
for (int i = 0; i < names.Length; i++)
{
    string path = folder + "/" + names[i] + ".mat";
    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (material == null) { material = new Material(template); AssetDatabase.CreateAsset(material, path); }
    foreach (string property in material.GetTexturePropertyNames()) material.SetTexture(property, null);
    foreach (string property in new[] { "_UseBumpMap", "_UseMatCap", "_UseRim", "_UseEmission", "_UseMain2ndTex", "_UseMain3rdTex" })
        if (material.HasProperty(property)) material.SetFloat(property, 0);
    var color = i == 0 ? Color.white : i == 1 ? new Color(0.18f, 0.115f, 0.075f, 1) : new Color(0.62f, 0.46f, 0.22f, 1);
    foreach (string property in new[] { "_Color", "_BaseColor" }) if (material.HasProperty(property)) material.SetColor(property, color);
    foreach (string property in new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_OutlineTex" })
        if (material.HasProperty(property)) material.SetTexture(property, i == 0 ? texture : null);
    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", i == 2 ? 0.65f : 0);
    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", i == 2 ? 0.45f : 0.18f);
    material.name = names[i]; EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); materials[i] = material;
}
var avatar = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/NPC_Jun/NPC_Jun.prefab");
UnityEditor.EditorWindow tool = null;
string status = "";
float maxBindError = 0;
int vertexCount = 0;
try
{
    avatar.name = "NPC_P09_RangerVest";
    var outfit = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(outfit, avatar.scene);
    outfit.name = "RangerVest";
    outfit.transform.SetParent(avatar.transform, false);
    var renderer = outfit.GetComponentInChildren<SkinnedMeshRenderer>(true);
    renderer.sharedMaterials = materials;
    var beforeMesh = new Mesh(); renderer.BakeMesh(beforeMesh);
    var before = beforeMesh.vertices;
    for (int i = 0; i < before.Length; i++) before[i] = renderer.transform.TransformPoint(before[i]);
    UnityEngine.Object.DestroyImmediate(beforeMesh);
    var sourceRoot = renderer.rootBone;
    var targetRoot = avatar.transform.Find("Skeleton_P09_Body/root");
    if (sourceRoot == null || targetRoot == null) throw new System.InvalidOperationException("병합할 P09 root 누락");
    var type = typeof(UPlayGround.Editor.AvatarArmatureBakeTool);
    tool = (UnityEditor.EditorWindow)ScriptableObject.CreateInstance(type);
    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    var fields = new System.Collections.Generic.Dictionary<string, object> {
        {"_avatarObject",avatar}, {"_sourceObject",outfit}, {"_sourceRoot",sourceRoot}, {"_targetRoot",targetRoot},
        {"_keepSourceRootAsChild",false}, {"_autoDetectPrefixSuffix",false}, {"_prefix",""}, {"_suffix",""},
        {"_retargetSkinnedMeshes",true}, {"_deleteRetargetedDuplicateBones",true}, {"_saveBakedMeshesAsAssets",false}
    };
    foreach (var pair in fields) type.GetField(pair.Key, flags).SetValue(tool, pair.Value);
    type.GetMethod("Bake", flags).Invoke(tool, null);
    status = (string)type.GetField("_status", flags).GetValue(tool);
    if (renderer.rootBone != targetRoot) throw new System.InvalidOperationException("본 병합 실패: " + status);
    foreach (var bone in renderer.bones)
        if (bone == null || !bone.IsChildOf(targetRoot) && bone != targetRoot)
            throw new System.InvalidOperationException("착용자 외부 본 참조");
    var afterMesh = new Mesh(); renderer.BakeMesh(afterMesh);
    var after = afterMesh.vertices;
    for (int i = 0; i < before.Length; i++) maxBindError = Mathf.Max(maxBindError, Vector3.Distance(before[i], renderer.transform.TransformPoint(after[i])));
    UnityEngine.Object.DestroyImmediate(afterMesh);
    if (maxBindError > 0.0001f) throw new System.InvalidOperationException("병합 전후 메시 이동: " + maxBindError);
    var mesh = renderer.sharedMesh;
    string meshPath = folder + "/RangerVest_BakedMesh.asset";
    var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    if (existingMesh == null) AssetDatabase.CreateAsset(mesh, meshPath);
    else { EditorUtility.CopySerialized(mesh, existingMesh); UnityEngine.Object.DestroyImmediate(mesh); renderer.sharedMesh = existingMesh; EditorUtility.SetDirty(existingMesh); AssetDatabase.SaveAssetIfDirty(existingMesh); }
    vertexCount = renderer.sharedMesh.vertexCount;
    // 12개 Humanoid 자세의 실측 범위에 회전·블렌딩 여유를 포함한다.
    renderer.localBounds = new Bounds(new Vector3(0.01f, 0.97f, 0.04f), new Vector3(0.85f, 1.0f, 0.65f));
    foreach (var r in avatar.GetComponentsInChildren<Renderer>(true))
    {
        if (r.name == "Male_Armor_002_Chest") r.gameObject.SetActive(false);
        if (r.name == "Male_Body_Chest")
        {
            for (var t = r.transform; t != avatar.transform; t = t.parent) t.gameObject.SetActive(true);
        }
    }
    foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
            throw new System.InvalidOperationException("Missing Script: " + t.name);
    bool success;
    PrefabUtility.SaveAsPrefabAsset(avatar, prefabFolder + "/NPC_P09_RangerVest.prefab", out success);
    if (!success) throw new System.InvalidOperationException("착용 NPC 저장 실패");
    // 동일한 골격·메시를 가진 순수 모델도 보관하여 독립 씬에서 재생한다.
    var model = new GameObject("P09_RangerVest_Model");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model, avatar.scene);
    model.AddComponent<Animator>().avatar = avatar.GetComponent<Animator>().avatar;
    while (avatar.transform.childCount > 0) avatar.transform.GetChild(0).SetParent(model.transform, false);
    foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
        UnityEngine.Object.DestroyImmediate(behaviour);
    if (model.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
        throw new System.InvalidOperationException("순수 모델의 하위에 게임 동작 스크립트가 있습니다.");
    PrefabUtility.SaveAsPrefabAsset(model, prefabFolder + "/P09_RangerVest_Model.prefab", out success);
    if (!success) throw new System.InvalidOperationException("착용 모델 저장 실패");
}
finally
{
    if (tool != null) UnityEngine.Object.DestroyImmediate(tool);
    PrefabUtility.UnloadPrefabContents(avatar);
}
System.IO.File.WriteAllText("output/P09RangerVest/merge-validation.json", "{\"tool\":\"AvatarArmatureBakeTool\",\"vertices\":" + vertexCount + ",\"maxBindPositionErrorMeters\":" + maxBindError.ToString("R",System.Globalization.CultureInfo.InvariantCulture) + ",\"missingScripts\":0,\"externalBoneReferences\":0}");
return new { status, vertexCount, maxBindError, prefab = prefabFolder + "/NPC_P09_RangerVest.prefab" };
