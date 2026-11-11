// 기존 재질·팔레트를 보존하고 적운 군락 밀도장과 수직 치수만 연결한다.
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
var shader = Shader.Find("UPlayGround/Environment/Day Sky");
if (UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("하늘 셰이더 오류");
if (!Enumerable.Range(0, shader.GetPropertyCount()).Any(i => shader.GetPropertyName(i) == "_CloudFormation"))
    throw new System.InvalidOperationException("새 군락 셰이더의 임포트가 아직 끝나지 않았습니다.");
string path = "Assets/10.Datas/World/Sky/CloudFormations.asset";
string backup = "output/WeatherSky/CloudHeight/backup/";
System.IO.Directory.CreateDirectory(backup);
var formation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture3D>(path);
var generated = new Texture3D(256, 128, 256, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8_UNorm,
    UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain);
generated.name = "CloudFormations";
generated.wrapMode = TextureWrapMode.Repeat;
generated.filterMode = FilterMode.Trilinear;
generated.SetPixelData<byte>(System.IO.File.ReadAllBytes("output/WeatherSky/CloudHeight/formations.rg"), 0);
generated.Apply(true, false);
if (formation == null) UnityEditor.AssetDatabase.CreateAsset(generated, path);
else
{
    UnityEditor.EditorUtility.CopySerialized(generated, formation);
    UnityEngine.Object.DestroyImmediate(generated);
    UnityEditor.EditorUtility.SetDirty(formation);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(formation);
}
formation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture3D>(path);
if (formation == null) throw new System.InvalidOperationException("군락 밀도장 로드 실패");
var materials = new System.Collections.Generic.List<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/10.Datas/World" }))
{
    string materialPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    if (material == null || material.shader != shader) continue;
    if (!System.IO.File.Exists(backup + guid + ".mat")) System.IO.File.Copy(materialPath, backup + guid + ".mat");
    material.SetTexture("_CloudFormation", formation);
    material.SetFloat("_CloudFormationEnabled", 1);
    material.SetFloat("_CloudFormationScale", 24000);
    material.SetFloat("_CloudEdgeWidth", 60);
    material.SetFloat("_CloudDepth", 4200);
    material.SetFloat("_CloudRaySteps", 96);
    UnityEditor.EditorUtility.SetDirty(material);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(material);
    materials.Add(materialPath);
}
var profiles = new System.Collections.Generic.List<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:SkyProfileSO", new[] { "Assets/10.Datas/World" }))
{
    string profilePath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>(profilePath);
    if (!System.IO.File.Exists(backup + guid + ".asset")) System.IO.File.Copy(profilePath, backup + guid + ".asset");
    profile.cloudDepth = 4200;
    profile.cloudFormationScale = 24000;
    profile.cloudEdgeWidth = 60;
    UnityEditor.EditorUtility.SetDirty(profile);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
    profiles.Add(profilePath);
}
System.IO.File.WriteAllLines("output/WeatherSky/CloudHeight/materials.txt", materials);
System.IO.File.WriteAllLines("output/WeatherSky/CloudHeight/profiles.txt", profiles);
return new { materials, profiles, formation = formation.name, width = formation.width, height = formation.height, depth = formation.depth };
