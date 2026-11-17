using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Tool.Editor.Build;

namespace UPlayGround.BuildManager.Tests
{
    /// <summary>씬 전용 Renderer의 비활성 효과가 빌드에서 제거되는 회귀를 검사한다.</summary>
    public sealed class BuildManagerRenderingTests
    {
        [Test]
        public void QualityPipeline_KeepsSsaoOffVariantsForSceneRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipeline));
            var data = new SerializedObject(pipeline);
            Assert.That(data.FindProperty("m_DefaultRendererIndex").intValue, Is.Zero);
            Assert.That(data.FindProperty("m_PrefilteringModeScreenSpaceOcclusion").intValue, Is.EqualTo(1));

            Type preprocessor = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                preprocessor = assembly.GetType("UnityEditor.Rendering.Universal.ShaderBuildPreprocessor");
                if (preprocessor != null) break;
            }
            Assert.That(preprocessor, Is.Not.Null);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            MethodInfo gather = preprocessor.GetMethod("GetSupportedShaderFeaturesFromAsset", flags);
            Assert.That(gather, Is.Not.Null);
            ParameterInfo[] parameters = gather.GetParameters();
            var arguments = new object[]
            {
                pipeline,
                Activator.CreateInstance(parameters[1].ParameterType.GetElementType()),
                Activator.CreateInstance(parameters[2].ParameterType.GetElementType()),
                true, false, false,
            };
            FieldInfo strip2D = preprocessor.GetField("s_Strip2DPasses", flags);
            object previousStrip2D = strip2D.GetValue(null);
            try
            {
                gather.Invoke(null, arguments);
                Assert.That(arguments[5], Is.False,
                    "Quality RP의 모든 Renderer에 SSAO가 있으면 네이티브 사전 필터가 off 변형을 제거합니다.");
                bool hasOn = false, hasOff = false;
                foreach (object features in (IEnumerable)arguments[1])
                {
                    bool usesSsao = features.ToString().Contains("ScreenSpaceOcclusion");
                    hasOn |= usesSsao;
                    hasOff |= !usesSsao && !features.ToString().Contains("DBufferMRT");
                }
                Assert.That(hasOn && hasOff, Is.True, "기본 화면과 씬 전용 화면의 효과 조합을 모두 보존해야 합니다.");
            }
            finally
            {
                strip2D.SetValue(null, previousStrip2D);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Fingerprint_TracksNonAddressableRenderingInputs(bool player)
        {
            const string sourcePath = "Assets/Settings/PC_RPAsset.asset";
            string folder = "Assets/__RenderingFingerprint_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            var settings = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings>();
            var profile = ScriptableObject.CreateInstance<BuildManagerProfileSO>();
            Func<string> getHash = () => player
                ? BuildManagerFingerprint.ComputePlayerSettings(profile)
                : BuildManagerFingerprint.ComputeContent(settings);
            try
            {
                string path = folder + "/Pipeline.asset";
                var pipeline = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(sourcePath));
                AssetDatabase.CreateAsset(pipeline, path);
                string before = getHash();
                var data = new SerializedObject(pipeline);
                data.FindProperty("m_ShadowDistance").floatValue += 1;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(pipeline);
                Assert.That(getHash(), Is.Not.EqualTo(before),
                    "Addressables에서 참조하지 않는 RP 변경도 기존 셰이더 콘텐츠의 재사용을 막아야 합니다.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
