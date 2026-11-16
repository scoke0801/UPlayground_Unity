using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Combat.Tests
{
    /// <summary>잔상 텍스처 조회가 텍스처 없는 셰이더와 기존 머티리얼을 처리하는지 검사한다.</summary>
    public sealed class AfterimageTextureTests
    {
        private static Texture Resolve(Material material)
        {
            var runner = typeof(AfterimageEvent).GetNestedType("AfterimageEventRunner", BindingFlags.NonPublic);
            return (Texture)runner.GetMethod("ResolveMainTexture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { material });
        }

        [Test]
        public void NullMaterialReturnsNull()
        {
            Assert.IsNull(Resolve(null));
        }

        [Test]
        public void ShaderWithoutTextureReturnsNullWithoutError()
        {
            var shader = Shader.Find("Hidden/InternalErrorShader");
            Assert.IsNotNull(shader);
            var material = new Material(shader);
            try
            {
                Assert.IsFalse(material.HasProperty("_BaseMap"));
                Assert.IsFalse(material.HasProperty("_MainTex"));
                Assert.IsNull(Resolve(material));
                UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [TestCase("Universal Render Pipeline/Unlit", "_BaseMap")]
        [TestCase("Unlit/Texture", "_MainTex")]
        public void ExistingTextureIsPreserved(string shaderName, string property)
        {
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader);
            var material = new Material(shader);
            try
            {
                material.SetTexture(property, Texture2D.blackTexture);
                Assert.AreSame(Texture2D.blackTexture, Resolve(material));
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
