// 나무에 연결된 프로젝트 소유 ShaderGraph 파생2종만 같은 생성기 계약으로 이관한다.
if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return "BUSY";
var patch = typeof(UPlayGround.EditorTools.VegetationReactionShaderAuthoring).GetMethod("EnsureGraphPositions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
var paths = new[] { "5bf02d37cc25a0745988227a855e652f", "cddbef5a918a4a4a9beba91b949025d6" }.Select(g => "Assets/06.Shaders/Environment/Vegetation/" + g + ".shader").ToArray();
foreach (string path in paths)
{
    string text = System.IO.File.ReadAllText(path);
    string result = (string)patch.Invoke(null, new object[] { text });
    result = result.Replace("ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)))", "ClipVegetationFuel(IN.WorldSpacePosition)");
    if (result.Contains("ClipVegetationFuel(TransformObjectToWorld(float3(0,0,0)))")) throw new System.Exception("수명 소거 피벗 좌표 잔류");
    if (result == text) continue;
    System.IO.File.WriteAllText(path, result);
    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
}
return paths;
