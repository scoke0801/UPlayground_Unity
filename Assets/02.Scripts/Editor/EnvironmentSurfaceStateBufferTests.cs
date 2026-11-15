using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools.Tests
{
    public sealed class EnvironmentSurfaceStateBufferTests
    {
        [Test]
        public void NativeRendererIndexAvoidsPropertyBlocksAndPreservesAnotherOwner()
        {
            Assert.That(Application.isPlaying, Is.False);
            var bufferType = typeof(ReactiveEnvironmentSurface).Assembly.GetType(
                "UPlayGround.Gameplay.World.ReactiveEnvironment.EnvironmentSurfaceStateBuffer", true);
            const BindingFlags statics = BindingFlags.Static | BindingFlags.NonPublic;
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var reset = bufferType.GetMethod("Reset", statics);
            var register = bufferType.GetMethod("Register", statics);
            Assert.That((int)bufferType.GetField("_active", statics).GetValue(null), Is.Zero);
            if (!(bool)bufferType.GetProperty("IsSupported", statics).GetValue(null)) Assert.Ignore("구조화 버퍼 미지원 장치");
            var catalog = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>(
                "Assets/10.Datas/Environment/Vegetation/Catalog.asset");
            var material = new Material(catalog.surfaces[0].reactive);
            var root = new GameObject("렌더러 인덱스 소유권");
            ReactiveEnvironmentSurface surface = null;
            reset.Invoke(null, null);
            try
            {
                register.Invoke(null, new object[] { Vector4.one, Vector4.zero, 1f });
                var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                surface = new ReactiveEnvironmentSurface(new Renderer[] { renderer });
                // Editor 안에서 런타임 임대 경로만 실행하며 실제 씬/서비스를 시작하지 않는다.
                typeof(ReactiveEnvironmentSurface).GetField("_useSharedBuffer", fields).SetValue(surface, true);
                typeof(ReactiveEnvironmentSurface).GetField("_useRendererUserValue", fields).SetValue(surface, true);
                typeof(ReactiveEnvironmentSurface).GetField("_rendererOriginals", fields).SetValue(surface,
                    new[] { new MaterialPropertyBlock() });
                var state = new UPlayGround.Core.ReactiveEnvironment.EnvironmentReactionState { wetness = 0.4f, fuel = 20, durability = 100 };
                surface.Apply(state, 100, 20);
                Assert.That(renderer.GetShaderUserValue(), Is.EqualTo(0x80000001u), "인덱스0 오독을 숨기지 않는다.");
                Assert.That(renderer.HasPropertyBlock(), Is.False, "정상 상태는 GPU Resident Drawer를 유지한다.");
                surface.Release();
                Assert.That(renderer.GetShaderUserValue(), Is.Zero);
                surface.Apply(state, 100, 20);
                var foreign = new MaterialPropertyBlock();
                foreign.SetFloat("_ForeignOwner", 123);
                renderer.SetPropertyBlock(foreign);
                renderer.SetShaderUserValue(91);
                surface.Release();
                renderer.GetPropertyBlock(foreign);
                Assert.That(foreign.GetFloat("_ForeignOwner"), Is.EqualTo(123), "대기 중 추가된 다른 소유 MPB를 지우지 않는다.");
                Assert.That(renderer.GetShaderUserValue(), Is.EqualTo(91));
                renderer.SetPropertyBlock(null);
                renderer.SetShaderUserValue(37);
                surface.Apply(state, 100, 20);
                Assert.That(renderer.GetShaderUserValue(), Is.EqualTo(37), "다른 소유 값은 MPB 경로로 보존한다.");
                Assert.That(renderer.HasPropertyBlock(), Is.True);
                surface.Release();
                Assert.That(renderer.GetShaderUserValue(), Is.EqualTo(37));
                Assert.That(renderer.HasPropertyBlock(), Is.False);
            }
            finally
            {
                surface?.Release(); reset.Invoke(null, null);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void GpuUploadGrowthReuseAndFinalReleasePreserveState()
        {
            var type = typeof(ReactiveEnvironmentSurface).Assembly.GetType(
                "UPlayGround.Gameplay.World.ReactiveEnvironment.EnvironmentSurfaceStateBuffer", true);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            if (!(bool)type.GetProperty("IsSupported", flags).GetValue(null)) Assert.Ignore("구조화 버퍼 미지원 장치");
            var reset = type.GetMethod("Reset", flags);
            var register = type.GetMethod("Register", flags);
            var release = type.GetMethod("Release", flags);
            var epoch = type.GetProperty("Epoch", flags);
            var bufferField = type.GetField("_buffer", flags);
            var indices = new List<int>();
            reset.Invoke(null, null);
            uint originalEpoch = (uint)epoch.GetValue(null);
            try
            {
                for (int i = 0; i < 137; i++)
                    indices.Add((int)register.Invoke(null, new object[] { new Vector4(i, .2f, .3f, 1), new Vector4(.8f, 0, 0, 0), .7f }));
                type.GetMethod("Set", flags).Invoke(null, new object[] { indices[100], new Vector4(999, 0, 0, 0), Vector4.one, .25f });
                type.GetMethod("Flush", flags).Invoke(null, null);
                var managed = (Array)type.GetField("_values", flags).GetValue(null);
                var readback = Array.CreateInstance(managed.GetType().GetElementType(), managed.Length);
                ((ComputeBuffer)bufferField.GetValue(null)).GetData(readback);
                var stateType = managed.GetType().GetElementType();
                Assert.That((Vector4)stateType.GetField("surface").GetValue(readback.GetValue(0)), Is.EqualTo(new Vector4(0, .2f, .3f, 1)));
                Assert.That(((Vector4)stateType.GetField("surface").GetValue(readback.GetValue(100))).x, Is.EqualTo(999));
                Assert.That(((Vector4)stateType.GetField("thermal").GetValue(readback.GetValue(100))).y, Is.EqualTo(.25f));
                release.Invoke(null, new object[] { indices[42], originalEpoch });
                release.Invoke(null, new object[] { indices[42], originalEpoch });
                int reused = (int)register.Invoke(null, new object[] { Vector4.one, Vector4.zero, 1f });
                Assert.That(reused, Is.EqualTo(indices[42]));
                Assert.That(Shader.IsKeywordEnabled("_ENVIRONMENT_REACTION_BUFFER"), Is.True);
                foreach (int index in indices) release.Invoke(null, new object[] { index, originalEpoch });
                Assert.That(bufferField.GetValue(null), Is.Null);
                Assert.That(Shader.IsKeywordEnabled("_ENVIRONMENT_REACTION_BUFFER"), Is.False);
                Assert.That((uint)epoch.GetValue(null), Is.Not.EqualTo(originalEpoch));
                Assert.That(type.GetMethod("IsValid", flags).Invoke(null, new object[] { 0, originalEpoch }), Is.False);
            }
            finally { reset.Invoke(null, null); }
        }
    }
}
