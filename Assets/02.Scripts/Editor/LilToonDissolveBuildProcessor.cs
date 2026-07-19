using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UPlayGround.Editor
{
    /// <summary>
    /// lilToon 빌드 최적화가 런타임 디졸브 및 카메라 근접 디더용 셰이더 코드를 제거하지 않도록
    /// 필요한 기능을 보존한다.
    /// </summary>
    public sealed class LilToonDissolveBuildProcessor : IPreprocessBuildWithReport
    {
        private const string KeepAliveMaterialPath = "Assets/Resources/Rendering/LilToonDissolveKeepAlive.mat";
        private const string MultiKeepAliveMaterialPath = "Assets/Resources/Rendering/LilToonMultiDitherKeepAlive.mat";
        private const string CutoutShaderName = "Hidden/lilToonCutout";
        private const string CutoutOutlineShaderName = "Hidden/lilToonCutoutOutline";
        private const string CutoutPassShaderName = "Hidden/ltspass_cutout";
        private const string MultiShaderName = "_lil/lilToonMulti";
        private const string MultiOutlineShaderName = "Hidden/lilToonMultiOutline";
        private const string DissolveKeyword = "GEOM_TYPE_BRANCH_DETAIL";
        private const string DitherKeyword = "ETC1_EXTERNAL_ALPHA";
        private const string AlphaMaskKeyword = "_COLOROVERLAY_ON";
        private const string MultiCutoutKeyword = "UNITY_UI_ALPHACLIP";
        private static readonly int DissolveParamsID = Shader.PropertyToID("_DissolveParams");
        private static readonly int UseDitherID = Shader.PropertyToID("_UseDither");
        private static readonly int AlphaMaskModeID = Shader.PropertyToID("_AlphaMaskMode");
        private static readonly int AlphaMaskScaleID = Shader.PropertyToID("_AlphaMaskScale");
        private static readonly int AlphaMaskValueID = Shader.PropertyToID("_AlphaMaskValue");

        public int callbackOrder => 101;

        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureKeepAliveMaterial();
            ForceLilToonDissolveFeature();
        }

        private static void EnsureKeepAliveMaterial()
        {
            ConfigureKeepAliveMaterial(KeepAliveMaterialPath);
            ConfigureKeepAliveMaterial(MultiKeepAliveMaterialPath);
        }

        private static void ConfigureKeepAliveMaterial(string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Debug.LogWarning($"[LilToonDissolveBuildProcessor] keep-alive 머티리얼을 찾을 수 없습니다. path={materialPath}");
                return;
            }

            bool isMulti = material.shader != null &&
                           (material.shader.name == MultiShaderName ||
                            material.shader.name == MultiOutlineShaderName);
            if (isMulti)
            {
                material.DisableKeyword(DissolveKeyword);
                material.EnableKeyword(MultiCutoutKeyword);
            }
            else
            {
                material.EnableKeyword(DissolveKeyword);
            }

            material.EnableKeyword(DitherKeyword);
            material.EnableKeyword(AlphaMaskKeyword);
            if (material.HasProperty(DissolveParamsID))
            {
                material.SetVector(
                    DissolveParamsID,
                    isMulti
                        ? Vector4.zero
                        : new Vector4(3f, 1f, 0f, 0.1f));
            }
            if (material.HasProperty(UseDitherID))
                material.SetFloat(UseDitherID, 1f);
            if (material.HasProperty(AlphaMaskModeID))
                material.SetFloat(AlphaMaskModeID, 2f);
            if (material.HasProperty(AlphaMaskScaleID))
                material.SetFloat(AlphaMaskScaleID, 0f);
            if (material.HasProperty(AlphaMaskValueID))
                material.SetFloat(AlphaMaskValueID, 1f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static void ForceLilToonDissolveFeature()
        {
            Type lilToonSettingType = FindType("lilToonSetting");
            if (lilToonSettingType == null)
            {
                Debug.LogWarning("[LilToonDissolveBuildProcessor] lilToonSetting 타입을 찾을 수 없어 디졸브 셰이더 보존 처리를 건너뜁니다.");
                return;
            }

            object shaderSetting = CreateShaderSetting(lilToonSettingType);
            if (shaderSetting == null)
                return;

            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_DISSOLVE");
            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_DissolveMask");
            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_DissolveNoiseMask");
            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_DITHER");
            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_ALPHAMASK");
            SetFeature(lilToonSettingType, shaderSetting, "LIL_FEATURE_AlphaMask");

            var shaders = new List<Shader>();
            AddShader(shaders, CutoutShaderName);
            AddShader(shaders, CutoutOutlineShaderName);
            // lilToonCutout 계열은 실제 렌더링을 이 공용 UsePass 셰이더에 위임한다.
            // 래퍼만 복원하면 빌드 최적화가 패스에서 디더/AlphaMask 코드를 제거할 수 있다.
            AddShader(shaders, CutoutPassShaderName);
            // Multi는 lil_replace_keywords.hlsl에서 LIL_IGNORE_SHADERSETTING을
            // 선언하며 Resources의 전용 머티리얼/ShaderVariantCollection으로
            // 필요한 변형을 보존한다. ApplyShaderSetting 대상으로 넘기면 빌드마다
            // 패키지 셰이더만 불필요하게 다시 임포트한다.

            InvokeApplyShaderSetting(lilToonSettingType, shaderSetting, shaders);
        }

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static object CreateShaderSetting(Type lilToonSettingType)
        {
            MethodInfo initializeMethod = lilToonSettingType.GetMethod(
                "InitializeShaderSetting",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (initializeMethod == null)
            {
                Debug.LogWarning("[LilToonDissolveBuildProcessor] lilToon InitializeShaderSetting 메서드를 찾을 수 없습니다.");
                return null;
            }

            object shaderSetting = null;
            object[] args = { shaderSetting };
            initializeMethod.Invoke(null, args);
            return args[0];
        }

        private static void SetFeature(Type lilToonSettingType, object shaderSetting, string fieldName)
        {
            FieldInfo field = lilToonSettingType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(shaderSetting, true);
        }

        private static void AddShader(List<Shader> shaders, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null && !shaders.Contains(shader))
                shaders.Add(shader);
        }

        private static void InvokeApplyShaderSetting(Type lilToonSettingType, object shaderSetting, List<Shader> shaders)
        {
            MethodInfo applyMethod = lilToonSettingType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "ApplyShaderSetting" && method.GetParameters().Length == 4);

            if (applyMethod != null && shaders.Count > 0)
            {
                applyMethod.Invoke(null, new object[] { shaderSetting, "[UPlayGround] Preserve lilToon Dissolve", shaders, false });
                return;
            }

            applyMethod = lilToonSettingType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "ApplyShaderSetting" && method.GetParameters().Length == 2);

            if (applyMethod != null)
            {
                applyMethod.Invoke(null, new object[] { shaderSetting, "[UPlayGround] Preserve lilToon Dissolve" });
                return;
            }

            Debug.LogWarning("[LilToonDissolveBuildProcessor] lilToon ApplyShaderSetting 메서드를 찾을 수 없습니다.");
        }
    }
}
