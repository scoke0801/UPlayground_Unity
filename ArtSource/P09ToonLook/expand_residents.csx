// 사용 목록에 명시된 기존 NPC의 재질 참조만 갱신한다. 빌더 재생성은 실행하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("컴파일 완료 후 편집 모드에서 실행하세요.");
string folder = "Assets/10.Datas/Actor/ScenarioResidents/ToonLook";
string output = "output/P09ToonLook";
string profilePath = folder + "/P09_Residents_ToonLook.asset";
var paths = System.IO.File.ReadAllLines(output + "/used-npcs.txt").Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
var daonProfile = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(folder + "/P09_Daon_ToonLook.asset");
daonProfile.ValidateBindings();
var profile = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(profilePath);
var bindings = profile != null ? profile.bindings.ToList() : daonProfile.bindings.ToList();
var materials = new Dictionary<Material, string>();
var prefabs = new Dictionary<string, GameObject>();
foreach (string path in paths)
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null || prefab.GetComponent<UPlayGround.NpcActor>() == null) throw new Exception("NPC 사전검사 실패: " + path);
    prefabs.Add(path, prefab);
    foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0) throw new Exception("Missing Script: " + path);
    foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
    {
        bool active = renderer.enabled;
        for (var t = renderer.transform; t != null; t = t.parent) active &= t.gameObject.activeSelf;
        if (!active) continue;
        foreach (var current in renderer.sharedMaterials)
        {
            if (current == null) continue;
            var known = bindings.FirstOrDefault(b => b.replacement == current);
            var source = known != null ? known.source : current;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (sourcePath.Contains("/Eye/")) continue;
            string role = sourcePath.Contains("/Materials/Armor/") ? "Cloth" : null;
            if (source.name.Contains("Skin")) role = "Skin";
            if ((renderer.name.StartsWith("Hair_") || renderer.name.Contains("FacialHair")) && source.name.Contains("Hair")) role = "Hair";
            if (source.name.Contains("Beard")) role = "Hair";
            if (role != null && source.shader == daonProfile.bindings[0].source.shader) materials[source] = role;
        }
    }
}
var backups = new Dictionary<string, byte[]>();
var created = new List<string>();
Action<string> backup = p => {
    if (!backups.ContainsKey(p) && System.IO.File.Exists(p))
    {
        backups.Add(p, System.IO.File.ReadAllBytes(p));
        string destination = output + "/Backup/" + p;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
        if (!System.IO.File.Exists(destination)) System.IO.File.WriteAllBytes(destination, backups[p]);
    }
};
var changed = new List<string>();
var bodyTemplate = daonProfile.bindings[0].replacement;
var faceTemplate = daonProfile.bindings[1].replacement;
var clothTemplate = daonProfile.bindings[2].replacement;
var hairTemplate = daonProfile.bindings[3].replacement;
var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Textures/P09_Female_Body_Diff_4K.png");
var originalDiffuse = daonProfile.bindings[0].source.GetTexture("_MainTex");
string[] shadowFloats = { "_UseShadow", "_ShadowStrength", "_ShadowBorder", "_ShadowBlur", "_ShadowBorderRange", "_ShadowEnvStrength", "_ShadowNormalStrength", "_ShadowReceive", "_Shadow2ndBorder", "_Shadow2ndBlur", "_Shadow2ndNormalStrength", "_Shadow2ndReceive" };
string[] shadowColors = { "_ShadowColor", "_Shadow2ndColor", "_ShadowBorderColor" };
try
{
    backup(AssetDatabase.GetAssetPath(faceTemplate));
    faceTemplate.SetFloat("_ShadowBorder", 0.4f);
    faceTemplate.SetFloat("_ShadowBlur", 0.45f);
    faceTemplate.SetFloat("_LightMaxLimit", 0.9f);
    faceTemplate.SetColor("_ShadowColor", new Color(0.92f, 0.78f, 0.76f, 1));
    foreach (string property in faceTemplate.GetTexturePropertyNames())
        if (faceTemplate.GetTexture(property) == originalDiffuse) faceTemplate.SetTexture(property, diffuse);
    EditorUtility.SetDirty(faceTemplate);
    AssetDatabase.SaveAssetIfDirty(faceTemplate);
    foreach (var pair in materials)
    {
        var source = pair.Key;
        string sourcePath = AssetDatabase.GetAssetPath(source);
        bool standard = sourcePath.StartsWith("Assets/ExternalAssets/Character/P09_Modular_Humanoid/");
        foreach (string prefix in pair.Value == "Skin" ? new[] { "", "Female_Face_", "Male_Face_" } : new[] { "" })
        {
            if (bindings.Any(b => b.source == source && (b.rendererPrefix ?? "") == prefix)) continue;
            bool isFace = prefix.Length > 0;
            // 같은 피부의 남녀 얼굴은 동일 재질을 공유한다.
            var existingFace = isFace ? bindings.FirstOrDefault(b => b.source == source && !string.IsNullOrEmpty(b.rendererPrefix)) : null;
            var replacement = existingFace != null ? existingFace.replacement : null;
            if (replacement == null)
            {
                string name = source.name + (isFace ? "_Face_Toon_" : "_Toon_") + AssetDatabase.AssetPathToGUID(sourcePath).Substring(0, 8);
                string path = folder + "/" + name + ".mat";
                if (System.IO.File.Exists(path)) throw new Exception("프로파일에 등록되지 않은 기존 출력: " + path);
                replacement = new Material(source) { name = name };
                replacement.SetFloat("_OutlineWidth", source.GetFloat("_OutlineWidth") * 0.5f);
                if (standard && pair.Value != "Hair")
                {
                    var template = isFace ? faceTemplate : pair.Value == "Skin" ? bodyTemplate : clothTemplate;
                    foreach (string property in shadowFloats) replacement.SetFloat(property, template.GetFloat(property));
                    foreach (string property in shadowColors) replacement.SetColor(property, template.GetColor(property));
                }
                if (isFace)
                {
                    replacement.SetFloat("_LightMaxLimit", Mathf.Min(source.GetFloat("_LightMaxLimit"), 0.9f));
                    if (standard && source.GetTexture("_MainTex") == originalDiffuse)
                        foreach (string property in replacement.GetTexturePropertyNames())
                            if (replacement.GetTexture(property) == originalDiffuse) replacement.SetTexture(property, diffuse);
                }
                if (standard && pair.Value == "Hair")
                {
                    foreach (string property in new[] { "_ShadowBlur", "_UseBacklight", "_BacklightBorder", "_BacklightBlur", "_BacklightDirectivity", "_BacklightViewStrength", "_BacklightReceiveShadow" })
                        replacement.SetFloat(property, hairTemplate.GetFloat(property));
                    replacement.SetColor("_BacklightColor", hairTemplate.GetColor("_BacklightColor"));
                }
                AssetDatabase.CreateAsset(replacement, path);
                created.Add(path);
            }
            bindings.Add(new UPlayGround.Editor.P09Builder.P09ToonLookProfileSO.MaterialBinding { source = source, replacement = replacement, rendererPrefix = prefix });
        }
    }
    if (profile == null)
    {
        profile = ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>();
        AssetDatabase.CreateAsset(profile, profilePath);
        created.Add(profilePath);
    }
    else backup(profilePath);
    profile.bindings = bindings.ToArray();
    profile.ValidateBindings();
    EditorUtility.SetDirty(profile);
    AssetDatabase.SaveAssetIfDirty(profile);
    foreach (string path in paths)
    {
        backup(path);
        string guid = AssetDatabase.AssetPathToGUID(path);
        var root = PrefabUtility.LoadPrefabContents(path);
        int count;
        try
        {
            var beforeSlots = root.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r, r => r.sharedMaterials.Length);
            count = profile.Apply(root);
            if (profile.Apply(root) != 0 || beforeSlots.Any(p => p.Key.sharedMaterials.Length != p.Value)) throw new Exception("반복 적용/슬롯 검사 실패: " + path);
            if (count > 0)
            {
                bool success;
                PrefabUtility.SaveAsPrefabAsset(root, path, out success);
                if (!success) throw new Exception("저장 실패: " + path);
                if (SessionState.GetBool("P09Toon.InjectFailure", false)) throw new Exception("의도된 저장 후 실패 복구 검사");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        if (AssetDatabase.AssetPathToGUID(path) != guid) throw new Exception("GUID 변경: " + path);
        var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (reloaded.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Any(m => profile.Resolve(r.name, m) != m))) throw new Exception("재로드 참조 검사 실패: " + path);
        changed.Add(path + "|slots=" + count + "|guid=" + guid + "|profile=" + profilePath);
    }
    foreach (string path in AssetDatabase.FindAssets("t:BuildPreset", new[] { "Assets/10.Datas" }).Select(AssetDatabase.GUIDToAssetPath))
    {
        var preset = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>(path);
        if (!paths.Any(p => System.IO.Path.GetFileNameWithoutExtension(p) == preset.config.ManualName)) continue;
        if (preset.config.ToonLookProfile == daonProfile || preset.config.ToonLookProfile == profile) continue;
        if (preset.config.ToonLookProfile != null) throw new Exception("기존 전용 프로파일을 보존해야 합니다: " + path);
        backup(path);
        preset.config.ToonLookProfile = profile;
        EditorUtility.SetDirty(preset);
        AssetDatabase.SaveAssetIfDirty(preset);
        changed.Add("Preset|" + path);
    }
    foreach (string path in created) AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(path));
}
catch
{
    foreach (var pair in backups) { System.IO.File.WriteAllBytes(pair.Key, pair.Value); AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceUpdate); }
    foreach (string path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
    throw;
}
System.IO.File.WriteAllLines(output + "/expansion-result.txt", changed.ToArray());
return new { targets = paths.Length, materialBindings = bindings.Count, created = created.Count, result = changed };
