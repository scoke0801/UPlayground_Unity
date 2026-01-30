using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class URPMaterialConverter : EditorWindow
{
    private Vector2 scrollPosition;
    private List<Material> magentaMaterials = new List<Material>();
    private Dictionary<Material, Shader> originalShaders = new Dictionary<Material, Shader>();
    private Dictionary<Material, Shader> convertedShaders = new Dictionary<Material, Shader>();
    
    // URP 셰이더 매핑 테이블
    private readonly Dictionary<string, string> shaderMappings = new Dictionary<string, string>
    {
        {"Standard", "Universal Render Pipeline/Lit"},
        {"Standard (Specular setup)", "Universal Render Pipeline/Lit"},
        {"Unlit/Color", "Universal Render Pipeline/Unlit"},
        {"Unlit/Texture", "Universal Render Pipeline/Unlit"},
        {"Unlit/Transparent", "Universal Render Pipeline/Unlit"},
        {"Unlit/Transparent Cutout", "Universal Render Pipeline/Unlit"},
        {"Mobile/Diffuse", "Universal Render Pipeline/Simple Lit"},
        {"Mobile/Bumped Specular", "Universal Render Pipeline/Simple Lit"},
        {"Mobile/Bumped Diffuse", "Universal Render Pipeline/Simple Lit"},
        {"Legacy Shaders/Diffuse", "Universal Render Pipeline/Simple Lit"},
        {"Legacy Shaders/Specular", "Universal Render Pipeline/Lit"},
        {"Legacy Shaders/Bumped Diffuse", "Universal Render Pipeline/Lit"},
        {"Legacy Shaders/Bumped Specular", "Universal Render Pipeline/Lit"}
    };

    // 텍스처 프로퍼티 매핑 테이블 (Built-in → URP)
    private readonly Dictionary<string, Dictionary<string, string>> propertyMappings = new Dictionary<string, Dictionary<string, string>>
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
        },
        {
            "Legacy Shaders/Diffuse",
            new Dictionary<string, string>
            {
                {"_MainTex", "_BaseMap"},
                {"_Color", "_BaseColor"}
            }
        }
    };

    [MenuItem("Tools/URP/URP 머티리얼 변환기")]
    public static void ShowWindow()
    {
        GetWindow<URPMaterialConverter>("URP 머티리얼 변환기");
    }

    private void OnEnable()
    {
        FindMagentaMaterials();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("URP 머티리얼 변환 및 미리 보기 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 새로 고침 버튼
        if (GUILayout.Button("마젠타 머티리얼 스캔"))
        {
            FindMagentaMaterials();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"발견된 문제 머티리얼: {magentaMaterials.Count}개", EditorStyles.helpBox);
        
        if (magentaMaterials.Count > 0)
        {
            EditorGUILayout.Space();
            
            // 전체 변환 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모든 머티리얼 변환", GUILayout.Height(30)))
            {
                ConvertAllMaterials();
            }
            if (GUILayout.Button("변환 되돌리기", GUILayout.Height(30)))
            {
                RevertAllConversions();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            // 머티리얼 목록과 미리 보기
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < magentaMaterials.Count; i++)
            {
                DrawMaterialPreview(magentaMaterials[i], i);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("변환이 필요한 머티리얼이 없습니다!", MessageType.Info);
        }
    }

    private void DrawMaterialPreview(Material material, int index)
    {
        if (material == null) return;

        EditorGUILayout.BeginVertical("box");
        
        // 머티리얼 정보 헤더
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{index + 1}. {material.name}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        // 현재 셰이더 정보
        string currentShader = material.shader ? material.shader.name : "없음";
        EditorGUILayout.LabelField($"현재 셰이더: {currentShader}");
        
        // 권장 URP 셰이더
        string recommendedShader = GetRecommendedURPShader(currentShader);
        EditorGUILayout.LabelField($"권장 URP 셰이더: {recommendedShader}");
        
        EditorGUILayout.Space();
        
        // 미리 보기 섹션
        EditorGUILayout.LabelField("텍스처 미리 보기:", EditorStyles.boldLabel);
        
        // 메인 텍스처 미리 보기
        Texture mainTexture = material.mainTexture;
        if (mainTexture != null)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 원본 미리 보기
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("메인 텍스처", GUILayout.Width(100));
            Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            EditorGUI.DrawPreviewTexture(previewRect, mainTexture);
            EditorGUILayout.EndVertical();
            
            // 변환 후 예상 결과 (시뮬레이션)
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("변환 후 예상", GUILayout.Width(100));
            Rect convertedRect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            
            // URP 변환 후 색상 조정 시뮬레이션
            Color originalColor = GUI.color;
            if (IsConvertedMaterial(material))
            {
                GUI.color = Color.white; // 변환됨
            }
            else
            {
                GUI.color = new Color(1f, 0.8f, 1f); // 마젠타 색조 제거 시뮬레이션
            }
            EditorGUI.DrawPreviewTexture(convertedRect, mainTexture);
            GUI.color = originalColor;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("메인 텍스처가 없습니다.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // 개별 변환 버튼
        EditorGUILayout.BeginHorizontal();
        
        if (!IsConvertedMaterial(material))
        {
            if (GUILayout.Button($"변환: {material.name}"))
            {
                ConvertMaterial(material);
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("변환 완료");
            GUI.enabled = true;
            
            if (GUILayout.Button("되돌리기"))
            {
                RevertMaterial(material);
            }
        }
        
        if (GUILayout.Button("선택", GUILayout.Width(60)))
        {
            Selection.activeObject = material;
            EditorGUIUtility.PingObject(material);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void FindMagentaMaterials()
    {
        magentaMaterials.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Material");
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            
            if (material != null && IsProblemMaterial(material))
            {
                magentaMaterials.Add(material);
            }
        }
        
        Debug.Log($"마젠타 머티리얼 {magentaMaterials.Count}개를 발견했습니다.");
    }

    private bool IsProblemMaterial(Material material)
    {
        if (material.shader == null) return true;
        
        string shaderName = material.shader.name;
        
        // Built-in 셰이더들을 감지
        return shaderName.Contains("Standard") && !shaderName.Contains("Universal") ||
               shaderName.Contains("Legacy Shaders") ||
               shaderName.Contains("Mobile/") ||
               shaderName.StartsWith("Unlit/") && !shaderName.Contains("Universal") ||
               shaderName == "Hidden/InternalErrorShader"; // 마젠타 오류 셰이더
    }

    private string GetRecommendedURPShader(string originalShaderName)
    {
        if (shaderMappings.ContainsKey(originalShaderName))
        {
            return shaderMappings[originalShaderName];
        }
        return "Universal Render Pipeline/Lit"; // 기본 권장
    }

    private void ConvertMaterial(Material material)
    {
        if (material == null) return;
        
        Undo.RecordObject(material, $"Convert {material.name} to URP");
        
        // 원본 셰이더 백업
        if (!originalShaders.ContainsKey(material))
        {
            originalShaders[material] = material.shader;
        }
        
        string originalShaderName = material.shader.name;
        string targetShaderName = GetRecommendedURPShader(originalShaderName);
        
        Shader urpShader = Shader.Find(targetShaderName);
        if (urpShader != null)
        {
            // 기존 프로퍼티들을 백업
            var savedProperties = BackupMaterialProperties(material, originalShaderName);
            
            // 셰이더 변경
            material.shader = urpShader;
            convertedShaders[material] = urpShader;
            
            // 프로퍼티들을 새 셰이더에 맞게 복원
            RestoreMaterialProperties(material, savedProperties, originalShaderName);
            
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"{material.name}: {originalShaderName} → {targetShaderName} (텍스처 보존됨)");
        }
        else
        {
            Debug.LogWarning($"URP 셰이더를 찾을 수 없습니다: {targetShaderName}");
        }
    }

    private Dictionary<string, object> BackupMaterialProperties(Material material, string originalShaderName)
    {
        var properties = new Dictionary<string, object>();
        
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
            // 매핑 테이블에 있는 프로퍼티들 백업
            var mappings = propertyMappings[originalShaderName];
            foreach (var kvp in mappings)
            {
                BackupProperty(material, kvp.Key, properties);
            }
        }
        
        return properties;
    }

    private void BackupProperty(Material material, string propertyName, Dictionary<string, object> backup)
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
                    
                    // 텍스처 스케일과 오프셋도 저장
                    Vector2 scale = material.GetTextureScale(propertyName);
                    Vector2 offset = material.GetTextureOffset(propertyName);
                    backup[propertyName + "_Scale"] = scale;
                    backup[propertyName + "_Offset"] = offset;
                }
            }
            // 컬러 프로퍼티
            else if (propertyName.Contains("Color") || propertyName == "_Color")
            {
                Color color = material.GetColor(propertyName);
                backup[propertyName + "_Color"] = color;
            }
            // 플로트 프로퍼티
            else
            {
                float value = material.GetFloat(propertyName);
                backup[propertyName + "_Float"] = value;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"프로퍼티 백업 실패 {propertyName}: {e.Message}");
        }
    }

    private void RestoreMaterialProperties(Material material, Dictionary<string, object> savedProperties, string originalShaderName)
    {
        if (!propertyMappings.ContainsKey(originalShaderName))
        {
            // 매핑 정보가 없는 경우 일반적인 복원 시도
            RestoreCommonProperties(material, savedProperties);
            return;
        }

        var mappings = propertyMappings[originalShaderName];
        foreach (var mapping in mappings)
        {
            string oldProp = mapping.Key;
            string newProp = mapping.Value;
            
            RestoreProperty(material, savedProperties, oldProp, newProp);
        }
    }

    private void RestoreCommonProperties(Material material, Dictionary<string, object> savedProperties)
    {
        // 일반적인 매핑들
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

    private void RestoreProperty(Material material, Dictionary<string, object> savedProperties, string oldProp, string newProp)
    {
        if (!material.HasProperty(newProp)) return;

        try
        {
            // 텍스처 복원
            string textureKey = oldProp + "_Texture";
            if (savedProperties.ContainsKey(textureKey) && savedProperties[textureKey] is Texture texture)
            {
                material.SetTexture(newProp, texture);
                
                // 스케일과 오프셋 복원
                string scaleKey = oldProp + "_Scale";
                string offsetKey = oldProp + "_Offset";
                
                if (savedProperties.ContainsKey(scaleKey) && savedProperties[scaleKey] is Vector2 scale)
                {
                    material.SetTextureScale(newProp, scale);
                }
                
                if (savedProperties.ContainsKey(offsetKey) && savedProperties[offsetKey] is Vector2 offset)
                {
                    material.SetTextureOffset(newProp, offset);
                }
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

    private void ConvertAllMaterials()
    {
        if (EditorUtility.DisplayDialog("확인", 
            $"{magentaMaterials.Count}개의 머티리얼을 URP 셰이더로 변환하시겠습니까?\n" +
            "이 작업은 실행 취소할 수 있습니다.", "변환", "취소"))
        {
            foreach (Material material in magentaMaterials)
            {
                ConvertMaterial(material);
            }
            
            // 목록 새로 고침
            FindMagentaMaterials();
        }
    }

    private void RevertMaterial(Material material)
    {
        if (originalShaders.ContainsKey(material))
        {
            Undo.RecordObject(material, $"Revert {material.name}");
            material.shader = originalShaders[material];
            
            convertedShaders.Remove(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"{material.name} 변환을 되돌렸습니다.");
        }
    }

    private void RevertAllConversions()
    {
        if (EditorUtility.DisplayDialog("확인", 
            "모든 변환을 되돌리시겠습니까?", "되돌리기", "취소"))
        {
            List<Material> materialsToRevert = new List<Material>(convertedShaders.Keys);
            foreach (Material material in materialsToRevert)
            {
                RevertMaterial(material);
            }
            
            FindMagentaMaterials();
        }
    }

    private bool IsConvertedMaterial(Material material)
    {
        return convertedShaders.ContainsKey(material);
    }
}
