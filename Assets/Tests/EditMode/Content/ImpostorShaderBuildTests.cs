using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Content.Tests
{
    /// <summary>원거리 나무 셰이더가 파이프라인 태그 불일치로 빌드에서 제거되는 회귀를 검사한다.</summary>
    public sealed class ImpostorShaderBuildTests
    {
        [Test]
        public void 원거리_나무의_파이프라인_태그는_빌드_파이프라인과_일치한다()
        {
            string path = AssetDatabase.GUIDToAssetPath("c83e0d6021200634a8b2ca4e998b498b");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader, Is.Not.Null);
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Assert.That(pipeline, Is.Not.Null);

            // 활성 SubShader의 폴백 결과 대신 빌드 스트리퍼가 읽는 직렬화 태그를 검사한다.
            var data = ShaderUtil.GetShaderData(shader);
            string tag = data.GetSerializedSubshader(0)
                .FindTagValue(new ShaderTagId("RenderPipeline")).name;
            Assert.That(tag, Is.EqualTo(pipeline.renderPipelineShaderTag).IgnoreCase,
                "태그가 다르면 빌드에서 모든 변형이 제거되어 원거리 나무가 마젠타로 표시됩니다.");
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
        }
    }
}
