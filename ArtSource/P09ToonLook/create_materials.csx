// 최초 저작용. 기존 재질을 덮어쓰지 않으며 재조정은 생성된 머티리얼 인스펙터에서 한다.
string folder = "Assets/10.Datas/Actor/ScenarioResidents/ToonLook";
string sourceRoot = "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/";
string referenceRoot = "Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/03_Materials/";
string[] sourcePaths = { "Female_Body/P09_Female_Skin.mat", "Female_Body/P09_Female_Skin.mat", "Armor/P09_Female_Armor_005.mat", "Common/Hair/P09_Hair_Brown.mat" };
string[] names = { "P09_Female_Skin_Toon", "P09_Female_Skin_Face_Toon", "P09_Female_Armor_005_Toon", "P09_Hair_Brown_Toon" };
var sources = new Material[4];
var materials = new Material[4];
for (int i = 0; i < 4; i++)
{
    sources[i] = AssetDatabase.LoadAssetAtPath<Material>(sourceRoot + sourcePaths[i]);
    if (sources[i] == null || System.IO.File.Exists(folder + "/" + names[i] + ".mat"))
        throw new System.InvalidOperationException("원본 누락 또는 기존 출력 충돌: " + names[i]);
}
if (System.IO.File.Exists(folder + "/P09_Daon_ToonLook.asset"))
    throw new System.InvalidOperationException("기존 프로파일을 덮어쓰지 않습니다.");
string[] shadowFloats = { "_UseShadow", "_ShadowStrength", "_ShadowBorder", "_ShadowBlur", "_ShadowBorderRange", "_ShadowEnvStrength", "_ShadowNormalStrength", "_ShadowReceive", "_Shadow2ndBorder", "_Shadow2ndBlur", "_Shadow2ndNormalStrength", "_Shadow2ndReceive" };
string[] shadowColors = { "_ShadowColor", "_Shadow2ndColor", "_ShadowBorderColor" };
var created = new System.Collections.Generic.List<string>();
try
{
    for (int i = 0; i < 4; i++)
    {
        materials[i] = new Material(sources[i]) { name = names[i] };
        materials[i].SetFloat("_OutlineWidth", sources[i].GetFloat("_OutlineWidth") * 0.5f);
        if (i < 3)
        {
            string referencePath = i == 0 ? "Mat_Character/Mat_Bokusei_Body.mat" : i == 1 ? "Mat_Character/Mat_Bokusei_Face.mat" : "Mat_Cloth/Mat_Bokusei_Cloth.mat";
            var reference = AssetDatabase.LoadAssetAtPath<Material>(referenceRoot + referencePath);
            if (reference == null) throw new System.InvalidOperationException("기준 재질 누락");
            foreach (string property in shadowFloats) materials[i].SetFloat(property, reference.GetFloat(property));
            foreach (string property in shadowColors) materials[i].SetColor(property, reference.GetColor(property));
        }
        else
        {
            materials[i].SetFloat("_ShadowBlur", 0.1f);
            materials[i].SetFloat("_UseBacklight", 1f);
            var reference = AssetDatabase.LoadAssetAtPath<Material>(referenceRoot + "Mat_Character/Mat_Bokusei_Hair.mat");
            foreach (string property in new[] { "_BacklightBorder", "_BacklightBlur", "_BacklightDirectivity", "_BacklightViewStrength", "_BacklightReceiveShadow" })
                materials[i].SetFloat(property, reference.GetFloat(property));
            materials[i].SetColor("_BacklightColor", reference.GetColor("_BacklightColor"));
        }
        foreach (string property in sources[i].GetTexturePropertyNames())
            if (materials[i].GetTexture(property) != sources[i].GetTexture(property))
                throw new System.InvalidOperationException("텍스처 보존 실패: " + property);
    }
    UPlayGround.Editor.P09Builder.PathConfig.EnsureFolderExists(folder);
    for (int i = 0; i < 4; i++)
    {
        string path = folder + "/" + names[i] + ".mat";
        AssetDatabase.CreateAsset(materials[i], path);
        created.Add(path);
    }
    var profile = ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>();
    profile.bindings = new UPlayGround.Editor.P09Builder.P09ToonLookProfileSO.MaterialBinding[4];
    for (int i = 0; i < 4; i++)
        profile.bindings[i] = new UPlayGround.Editor.P09Builder.P09ToonLookProfileSO.MaterialBinding
        { source = sources[i], replacement = materials[i], rendererPrefix = i == 1 ? "Female_Face_" : "" };
    profile.ValidateBindings();
    string profilePath = folder + "/P09_Daon_ToonLook.asset";
    AssetDatabase.CreateAsset(profile, profilePath);
    created.Add(profilePath);
    foreach (string path in created) AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(path));
}
catch
{
    foreach (string path in created) AssetDatabase.DeleteAsset(path);
    foreach (var material in materials)
        if (material != null && !EditorUtility.IsPersistent(material)) UnityEngine.Object.DestroyImmediate(material);
    throw;
}
return created;
