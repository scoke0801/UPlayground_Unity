// 원본 재질 GUID·팔레트·구름 운송을 보존하고 구름 부피 데이터만 연결한다.
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
var shader = Shader.Find("UPlayGround/Environment/Day Sky");
if (shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("하늘 셰이더 오류");
string volumePath = "Assets/10.Datas/World/Sky/CloudVolume.asset";
var volume = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture3D>(volumePath);
{
    byte[] bytes = System.IO.File.ReadAllBytes("output/WeatherSky/Clouds/cloud-volume.rgba");
    if (bytes.Length != 128 * 128 * 128 * 4) throw new System.InvalidOperationException("볼륨 데이터 크기 오류");
    var generated = new Texture3D(128, 128, 128, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain);
    generated.name = "CloudVolume";
    generated.wrapMode = TextureWrapMode.Repeat;
    generated.filterMode = FilterMode.Trilinear;
    generated.SetPixelData<byte>(bytes, 0);
    generated.Apply(true, false);
    if (volume == null) { volume = generated; UnityEditor.AssetDatabase.CreateAsset(volume, volumePath); }
    else { UnityEditor.EditorUtility.CopySerialized(generated, volume); UnityEngine.Object.DestroyImmediate(generated); UnityEditor.EditorUtility.SetDirty(volume); }
    UnityEditor.AssetDatabase.SaveAssetIfDirty(volume);
}
volume = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture3D>(volumePath);
if (volume == null) throw new System.InvalidOperationException("생성한 볼륨을 다시 로드하지 못했습니다.");
var changed = new System.Collections.Generic.List<string>();
string backup = "output/WeatherSky/Clouds/backup";
System.IO.Directory.CreateDirectory(backup);
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/10.Datas/World" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
    if (material == null || material.shader != shader) continue;
    string saved = backup + "/" + guid + ".mat";
    if (!System.IO.File.Exists(saved)) System.IO.File.Copy(path, saved);
    material.SetTexture("_CloudVolume", volume);
    material.SetFloat("_CloudVolumeEnabled", 1);
    material.SetFloat("_CloudDepth", 1600);
    material.SetFloat("_CloudDensity", 2.4f);
    material.SetFloat("_CloudErosion", 0.6f);
    material.SetFloat("_CloudRaySteps", 48);
    UnityEditor.EditorUtility.SetDirty(material);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(material);
    changed.Add(path);
}
System.IO.File.WriteAllLines("output/WeatherSky/Clouds/materials.txt", changed);
var profiles = new System.Collections.Generic.List<string>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:SkyProfileSO", new[] { "Assets/10.Datas/World" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>(path);
    string saved = backup + "/" + guid + ".asset";
    if (!System.IO.File.Exists(saved)) System.IO.File.Copy(path, saved);
    profile.cloudDepth = 1600;
    profile.cloudDensity = 2.4f;
    profile.cloudErosion = 0.6f;
    UnityEditor.EditorUtility.SetDirty(profile);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
    profiles.Add(path);
}
System.IO.File.WriteAllLines("output/WeatherSky/Clouds/profiles.txt", profiles);
return new { materials = changed, profiles = profiles, volume = volumePath, graphicsFormat = volume.graphicsFormat.ToString(), size = volume.width };
