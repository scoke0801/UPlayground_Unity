using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class URPMaterialConverterMenu
{
    [MenuItem("Assets/URP 변환/선택된 머티리얼을 URP로 변환", false, 0)]
    public static void ConvertSelectedMaterials()
    {
        Material[] selectedMaterials = GetSelectedMaterials();
        
        if (selectedMaterials.Length == 0)
        {
            EditorUtility.DisplayDialog("알림", "선택된 머티리얼이 없습니다.", "확인");
            return;
        }

        if (EditorUtility.DisplayDialog("URP 변환 확인", 
            $"{selectedMaterials.Length}개의 머티리얼을 URP 셰이더로 변환하시겠습니까?", 
            "변환", "취소"))
        {
            ConvertMaterialsToURP(selectedMaterials);
        }
    }

    [MenuItem("Assets/URP 변환/선택된 머티리얼을 URP로 변환", true)]
    public static bool ConvertSelectedMaterialsValidation()
    {
        return GetSelectedMaterials().Length > 0;
    }

    [MenuItem("Assets/URP 변환/프로젝트 전체 머티리얼 스캔", false, 1)]
    public static void ScanAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int problemCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            
            if (material != null && IsBuiltInMaterial(material))
            {
                problemCount++;
            }
        }

        if (problemCount > 0)
        {
            bool convert = EditorUtility.DisplayDialog("스캔 결과", 
                $"{problemCount}개의 Built-in 셰이더 머티리얼을 발견했습니다.\n" +
                "모두 URP로 변환하시겠습니까?", 
                "변환", "취소");
                
            if (convert)
            {
                ConvertAllBuiltInMaterials();
            }
        }
        else
        {
            EditorUtility.DisplayDialog("스캔 결과", 
                "변환이 필요한 머티리얼이 없습니다!", "확인");
        }
    }

    private static Material[] GetSelectedMaterials()
    {
        var materials = new System.Collections.Generic.List<Material>();
        
        foreach (Object obj in Selection.objects)
        {
            if (obj is Material material)
            {
                materials.Add(material);
            }
        }
        
        return materials.ToArray();
    }

    private static void ConvertMaterialsToURP(Material[] materials)
    {
        int convertedCount = 0;
        
        foreach (Material material in materials)
        {
            if (ConvertMaterialToURP(material))
            {
                convertedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("변환 완료", 
            $"{convertedCount}/{materials.Length}개의 머티리얼이 변환되었습니다.", "확인");
    }

    private static void ConvertAllBuiltInMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;
        int totalCount = 0;

        EditorUtility.DisplayProgressBar("URP 변환 중", "머티리얼 변환을 진행하고 있습니다...", 0);

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (material != null && IsBuiltInMaterial(material))
                {
                    totalCount++;
                    if (ConvertMaterialToURP(material))
                    {
                        convertedCount++;
                    }
                }

                float progress = (float)i / guids.Length;
                EditorUtility.DisplayProgressBar("URP 변환 중", 
                    $"처리 중: {material?.name ?? "Unknown"}", progress);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("변환 완료", 
            $"{convertedCount}/{totalCount}개의 머티리얼이 URP로 변환되었습니다.", "확인");
    }

    private static bool ConvertMaterialToURP(Material material)
    {
        if (material?.shader == null) return false;

        string originalShaderName = material.shader.name;
        string urpShaderName = GetURPShaderMapping(originalShaderName);

        if (string.IsNullOrEmpty(urpShaderName)) return false;

        Shader urpShader = Shader.Find(urpShaderName);
        if (urpShader == null)
        {
            Debug.LogWarning($"URP 셰이더를 찾을 수 없습니다: {urpShaderName}");
            return false;
        }

        Undo.RecordObject(material, $"Convert {material.name} to URP");
        
        // 텍스처와 프로퍼티 백업
        var savedProperties = BackupMaterialProperties(material, originalShaderName);
        
        // 셰이더 변경
        material.shader = urpShader;
        
        // 프로퍼티 복원
        RestoreMaterialProperties(material, savedProperties, originalShaderName);
        
        EditorUtility.SetDirty(material);

        Debug.Log($"변환 완료: {material.name} ({originalShaderName} → {urpShaderName}) - 텍스처 보존됨");
        return true;
    }

    private static Dictionary<string, object> BackupMaterialProperties(Material material, string originalShaderName)
    {
        var properties = new Dictionary<string, object>();
        var propertyMappings = GetPropertyMappings();
        
        if (!propertyMappings.ContainsKey(originalShaderName))
        {
            // 매핑 정보가 없는 경우 일반적인 프로퍼티들 백업
            var commonProperties = new[]
            {
                "_MainTex", "_Color", "_BumpMap", "_BumpScale",
                "_MetallicGlossMap", "_Metallic", "_Glossiness", "_Smoothness",
                "_OcclusionMap", "_OcclusionStrength",
                "_EmissionMap", "_EmissionColor", "_Cutoff"
            };
            
            foreach (string propName in commonProperties)
            {
                BackupProperty(material, propName, properties);
            }
        }
        else
        {
            var mappings = propertyMappings[originalShaderName];
            foreach (var kvp in mappings)
            {
                BackupProperty(material, kvp.Key, properties);
            }
        }
        
        return properties;
    }

    private static void BackupProperty(Material material, string propertyName, Dictionary<string, object> backup)
    {
        if (!material.HasProperty(propertyName)) return;

        try
        {
            // 텍스처 프로퍼티
            if (propertyName.EndsWith("Tex") || propertyName.EndsWith("Map") || propertyName == "_MainTex")
            {
                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    backup[propertyName + "_Texture"] = texture;
                    backup[propertyName + "_Scale"] = material.GetTextureScale(propertyName);
                    backup[propertyName + "_Offset"] = material.GetTextureOffset(propertyName);
                }
            }
            // 컬러 프로퍼티
            else if (propertyName.Contains("Color") || propertyName == "_Color")
            {
                backup[propertyName + "_Color"] = material.GetColor(propertyName);
            }
            // 플로트 프로퍼티
            else
            {
                backup[propertyName + "_Float"] = material.GetFloat(propertyName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"프로퍼티 백업 실패 {propertyName}: {e.Message}");
        }
    }

    private static void RestoreMaterialProperties(Material material, Dictionary<string, object> savedProperties, string originalShaderName)
    {
        var propertyMappings = GetPropertyMappings();
        
        if (!propertyMappings.ContainsKey(originalShaderName))
        {
            // 일반적인 매핑 사용
            var commonMappings = new Dictionary<string, string>
            {
                {"_MainTex", "_BaseMap"},
                {"_Color", "_BaseColor"},
                {"_BumpMap", "_BumpMap"},
                {"_BumpScale", "_BumpScale"},
                {"_MetallicGlossMap", "_MetallicGlossMap"},
                {"_Metallic", "_Metallic"},
                {"_Glossiness", "_Smoothness"},
                {"_OcclusionMap", "_OcclusionMap"},
                {"_OcclusionStrength", "_OcclusionStrength"},
                {"_EmissionMap", "_EmissionMap"},
                {"_EmissionColor", "_EmissionColor"},
                {"_Cutoff", "_Cutoff"}
            };

            foreach (var mapping in commonMappings)
            {
                RestoreProperty(material, savedProperties, mapping.Key, mapping.Value);
            }
        }
        else
        {
            var mappings = propertyMappings[originalShaderName];
            foreach (var mapping in mappings)
            {
                RestoreProperty(material, savedProperties, mapping.Key, mapping.Value);
            }
        }
    }

    private static void RestoreProperty(Material material, Dictionary<string, object> savedProperties, string oldProp, string newProp)
    {
        if (!material.HasProperty(newProp)) return;

        try
        {
            // 텍스처 복원
            string textureKey = oldProp + "_Texture";
            if (savedProperties.ContainsKey(textureKey) && savedProperties[textureKey] is Texture texture)
            {
                material.SetTexture(newProp, texture);
                
                string scaleKey = oldProp + "_Scale";
                string offsetKey = oldProp + "_Offset";
                
                if (savedProperties.ContainsKey(scaleKey) && savedProperties[scaleKey] is Vector2 scale)
                    material.SetTextureScale(newProp, scale);
                
                if (savedProperties.ContainsKey(offsetKey) && savedProperties[offsetKey] is Vector2 offset)
                    material.SetTextureOffset(newProp, offset);
            }
            
            // 컬러 복원
            string colorKey = oldProp + "_Color";
            if (savedProperties.ContainsKey(colorKey) && savedProperties[colorKey] is Color color)
            {
                material.SetColor(newProp, color);
            }
            
            // 플로트 복원
            string floatKey = oldProp + "_Float";
            if (savedProperties.ContainsKey(floatKey) && savedProperties[floatKey] is float floatValue)
            {
                material.SetFloat(newProp, floatValue);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"프로퍼티 복원 실패 {oldProp} → {newProp}: {e.Message}");
        }
    }

    private static Dictionary<string, Dictionary<string, string>> GetPropertyMappings()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Standard",
                new Dictionary<string, string>
                {
                    {"_MainTex", "_BaseMap"},
                    {"_Color", "_BaseColor"},
                    {"_BumpMap", "_BumpMap"},
                    {"_BumpScale", "_BumpScale"},
                    {"_MetallicGlossMap", "_MetallicGlossMap"},
                    {"_Metallic", "_Metallic"},
                    {"_Glossiness", "_Smoothness"},
                    {"_OcclusionMap", "_OcclusionMap"},
                    {"_OcclusionStrength", "_OcclusionStrength"},
                    {"_EmissionMap", "_EmissionMap"},
                    {"_EmissionColor", "_EmissionColor"},
                    {"_Cutoff", "_Cutoff"}
                }
            },
            {
                "Standard (Specular setup)",
                new Dictionary<string, string>
                {
                    {"_MainTex", "_BaseMap"},
                    {"_Color", "_BaseColor"},
                    {"_BumpMap", "_BumpMap"},
                    {"_BumpScale", "_BumpScale"},
                    {"_SpecGlossMap", "_SpecGlossMap"},
                    {"_SpecColor", "_SpecColor"},
                    {"_Glossiness", "_Smoothness"},
                    {"_OcclusionMap", "_OcclusionMap"},
                    {"_OcclusionStrength", "_OcclusionStrength"},
                    {"_EmissionMap", "_EmissionMap"},
                    {"_EmissionColor", "_EmissionColor"}
                }
            },
            {
                "Unlit/Texture",
                new Dictionary<string, string>
                {
                    {"_MainTex", "_BaseMap"},
                    {"_Color", "_BaseColor"}
                }
            },
            {
                "Mobile/Diffuse",
                new Dictionary<string, string>
                {
                    {"_MainTex", "_BaseMap"},
                    {"_Color", "_BaseColor"}
                }
            }
        };
    }

    private static bool IsBuiltInMaterial(Material material)
    {
        if (material?.shader == null) return false;

        string shaderName = material.shader.name;
        return shaderName == "Standard" ||
               shaderName == "Standard (Specular setup)" ||
               shaderName.StartsWith("Legacy Shaders/") ||
               shaderName.StartsWith("Mobile/") ||
               (shaderName.StartsWith("Unlit/") && !shaderName.Contains("Universal")) ||
               shaderName == "Hidden/InternalErrorShader";
    }

    private static string GetURPShaderMapping(string originalShaderName)
    {
        switch (originalShaderName)
        {
            case "Standard":
            case "Standard (Specular setup)":
                return "Universal Render Pipeline/Lit";
                
            case "Unlit/Color":
            case "Unlit/Texture":
            case "Unlit/Transparent":
            case "Unlit/Transparent Cutout":
                return "Universal Render Pipeline/Unlit";
                
            case "Mobile/Diffuse":
            case "Mobile/Bumped Specular":
            case "Mobile/Bumped Diffuse":
            case "Legacy Shaders/Diffuse":
                return "Universal Render Pipeline/Simple Lit";
                
            case "Legacy Shaders/Specular":
            case "Legacy Shaders/Bumped Diffuse":
            case "Legacy Shaders/Bumped Specular":
                return "Universal Render Pipeline/Lit";
                
            default:
                // 알 수 없는 셰이더는 기본 Lit으로 변환
                if (!originalShaderName.Contains("Universal"))
                {
                    return "Universal Render Pipeline/Lit";
                }
                return null; // 이미 URP 셰이더
        }
    }
}
