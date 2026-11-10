// Unity MCP execute_code의 메서드 본문으로 실행한다. 외부 패키지 재수입 후 원숭이 몸체만 복구한다.
// 기존 lilToon 머티리얼은 유지해 후속 아트 튜닝을 덮어쓰지 않는다.
var shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/jp.lilxyzw.liltoon/Shader/lts.shader");
if (shader == null || !shader.isSupported)
    throw new System.InvalidOperationException("지원되는 lilToon 셰이더가 필요합니다.");

var materials = new System.Collections.Generic.List<Material>();
var textures = new System.Collections.Generic.List<Texture>();
for (int i = 0; i < 10; i++)
{
    string suffix = i.ToString("00");
    string path = "Assets/ExternalAssets/Character/Suriyun/Pspsps/Materials/Characters/Monkey/M_Chibi_Monkey_" + suffix + ".mat";
    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
    var texture = AssetDatabase.LoadAssetAtPath<Texture>(
        "Assets/ExternalAssets/Character/Suriyun/Pspsps/Textures/Monkey Color/T_Chibi_Monkey_" + suffix + ".png");
    if (material == null || texture == null)
        throw new System.InvalidOperationException("원숭이 머티리얼 또는 텍스처 누락: " + suffix);

    var serialized = new SerializedObject(material);
    var entries = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
    Texture original = null;
    for (int j = 0; j < entries.arraySize; j++)
    {
        var entry = entries.GetArrayElementAtIndex(j);
        if (entry.FindPropertyRelative("first").stringValue == "_MainTex")
            original = entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
    }
    if (original != texture)
        throw new System.InvalidOperationException("예상하지 않은 텍스처 연결: " + path);
    if (material.shader == shader)
        continue;
    if (material.shader != null && material.shader.name != "Hidden/InternalErrorShader")
        throw new System.InvalidOperationException("다른 셰이더로 변경된 머티리얼은 자동 복구하지 않습니다: " + path);
    materials.Add(material);
    textures.Add(texture);
}

Undo.IncrementCurrentGroup();
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("원숭이 몸체 lilToon 복구");
try
{
    for (int i = 0; i < materials.Count; i++)
    {
        var material = materials[i];
        Undo.RecordObject(material, "원숭이 몸체 lilToon 복구");
        material.shader = shader;
        material.shaderKeywords = new string[0];
        material.SetTexture("_MainTex", textures[i]);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_UseShadow", 1f);
        material.SetColor("_ShadowColor", new Color(0.82f, 0.82f, 0.82f, 1f));
        material.SetColor("_Shadow2ndColor", new Color(0.68f, 0.68f, 0.68f, 1f));
        material.SetColor("_ShadowBorderColor", Color.white);
        material.SetFloat("_Cull", 2f);
        material.renderQueue = 2000;
        material.SetOverrideTag("RenderType", "Opaque");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
    }
    Undo.CollapseUndoOperations(group);
}
catch
{
    Undo.RevertAllDownToGroup(group);
    foreach (var material in materials)
        AssetDatabase.SaveAssetIfDirty(material);
    throw;
}
return new { changed = materials.Count, total = 10, shader = shader.name };
