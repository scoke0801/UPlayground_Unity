// mcp__UnityMCP__execute_code 본문(codedom, C# 6). item과 soPath만 바꾼다.
// 이미 있는 머티리얼·프리팹은 덮어쓰지 않고 EXISTS를 반환한다.
// 실행 중 MCP 연결이 끊기면 다시 실행하지 말고, 응답 복귀 후 결과 조회만 한다.
const string item = "<ItemId>";
string soPath = "Assets/10.Datas/Item/<경로>.asset";
string fbx = "Assets/08.Models/DropItems/" + item + ".fbx";
string tex = "Assets/08.Models/DropItems/Textures/" + item + "_Albedo.png";
string matPath = "Assets/08.Models/DropItems/Materials/" + item + ".mat";
string prefabPath = "Assets/03.Prefabs/Item/World/WorldItem_" + item + ".prefab";
if (System.IO.File.Exists(matPath) || System.IO.File.Exists(prefabPath)) return "EXISTS";
AssetDatabase.ImportAsset(tex, ImportAssetOptions.ForceUpdate);
AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
var ti = (TextureImporter)AssetImporter.GetAtPath(tex);
ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.maxTextureSize = 512;
ti.alphaSource = TextureImporterAlphaSource.None; ti.wrapMode = TextureWrapMode.Clamp;
ti.textureCompression = TextureImporterCompression.Compressed; ti.SaveAndReimport();
var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
mi.globalScale = 1f; mi.useFileScale = true; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
mi.importCameras = false; mi.importLights = false; mi.addCollider = false; mi.materialImportMode = ModelImporterMaterialImportMode.None;
mi.SaveAndReimport();
foreach (var f in new[] { "Assets/08.Models/DropItems/Materials", "Assets/03.Prefabs/Item", "Assets/03.Prefabs/Item/World" })
{
    if (!AssetDatabase.IsValidFolder(f)) { var i = f.LastIndexOf('/'); AssetDatabase.CreateFolder(f.Substring(0, i), f.Substring(i + 1)); }
}
var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.25f); mat.SetFloat("_Metallic", 0f);
AssetDatabase.CreateAsset(mat, matPath);
var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
inst.name = "WorldItem_" + item;
foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = mat;
var prefab = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
UnityEngine.Object.DestroyImmediate(inst);
var so = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>(soPath);
var ser = new SerializedObject(so);
ser.FindProperty("worldPrefab").objectReferenceValue = prefab;
ser.ApplyModifiedProperties();
AssetDatabase.SaveAssets();
var mf = prefab.GetComponentInChildren<MeshFilter>();
var sb = new System.Text.StringBuilder();
sb.Append("tris=" + (mf.sharedMesh.triangles.Length / 3) + " bounds=" + mf.sharedMesh.bounds.size.ToString("F3") + " components=");
foreach (var c in prefab.GetComponentsInChildren<Component>(true)) sb.Append(c.GetType().Name + ",");
sb.Append(" so id=" + so.itemId + " wp=" + (so.worldPrefab ? so.worldPrefab.name : "null"));
return sb.ToString();
