using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>런타임 재질 소유권·LOD 누락·실제 GPU 표면 반응을 검사한다.</summary>
    public sealed class ReactiveEnvironmentSurfaceTests
    {
        private const string ShaderName = "UPlayGround/Environment/Reactive Toon";

        [Test]
        public void EveryPassCompilesWithOpaqueAndNormalMapVariants()
        {
            var material = NewMaterial();
            try
            {
                foreach (string keyword in new[] { "", "_NORMALMAP", "LOD_FADE_CROSSFADE", "_ADDITIONAL_LIGHTS _CLUSTER_LIGHT_LOOP" })
                {
                    material.shaderKeywords = keyword.Length == 0 ? Array.Empty<string>() : keyword.Split(' ');
                    foreach (string pass in new[] { "Forward", "Outline", "ShadowCaster", "DepthOnly", "DepthNormalsOnly", "DepthNormals", "MotionVectors", "Meta" })
                        Assert.That(material.SetPass(material.FindPass(pass)), Is.True, pass + " " + keyword);
                }
                foreach (var message in ShaderUtil.GetShaderMessages(material.shader))
                    Assert.That(message.severity.ToString(), Is.Not.EqualTo("Error"), message.message);
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void LeaseSharesAllSlotsAndReleasesWithoutChangingSource()
        {
            var root = new GameObject("SurfaceLeaseTest");
            var material = NewMaterial();
            ReactiveEnvironmentSurface binding = null;
            try
            {
                var a = new GameObject("LOD0").AddComponent<MeshRenderer>(); a.transform.SetParent(root.transform);
                var b = new GameObject("LOD1").AddComponent<MeshRenderer>(); b.transform.SetParent(root.transform);
                a.sharedMaterials = new[] { material, material }; b.sharedMaterial = material;
                var group = root.AddComponent<LODGroup>();
                group.SetLODs(new[] { new LOD(0.5f, new Renderer[] { a }), new LOD(0.01f, new Renderer[] { b }) });
                Assert.That(ReactiveEnvironmentSurface.Validate(root.transform, new Renderer[] { a }), Is.Not.Null);
                Assert.That(ReactiveEnvironmentSurface.Validate(root.transform, new Renderer[] { a, b }), Is.Null);
                binding = new ReactiveEnvironmentSurface(new Renderer[] { a, b });
                binding.Apply(new EnvironmentReactionState { wetness = 0.7f, charAmount = 0.4f,
                    durability = 50, fuel = 10, heat = 0.8f, combustion = EnvironmentCombustion.Burning }, 100, 20);
                Assert.That(a.sharedMaterial, Is.SameAs(b.sharedMaterial));
                Assert.That(a.sharedMaterials[1], Is.SameAs(a.sharedMaterial));
                var block = new MaterialPropertyBlock();
                a.GetPropertyBlock(block, 0);
                Assert.That(block.GetVector("_EnvState0"), Is.EqualTo(new Vector4(0.7f,0.4f,0.5f,1)));
                int count = binding.ApplyCount;
                binding.Apply(new EnvironmentReactionState { wetness = 0.7f, charAmount = 0.4f,
                    durability = 50, fuel = 10, heat = 0.8f, combustion = EnvironmentCombustion.Burning }, 100, 20);
                Assert.That(binding.ApplyCount, Is.EqualTo(count));
                Assert.That(material.GetVector("_EnvState0"), Is.EqualTo(Vector4.zero));
                binding.Release();
                Assert.That(a.sharedMaterial, Is.SameAs(material));
                Assert.That(b.sharedMaterial, Is.SameAs(material));
            }
            finally { binding?.Release(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void ExplicitSlotsPreserveExcludedMaterialAndPropertyBlocks()
        {
            var root = new GameObject("ExplicitSlots");
            var material = NewMaterial();
            ReactiveEnvironmentSurface surface = null;
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[] { material, material };
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", Color.red);
                renderer.SetPropertyBlock(block);
                var bindings = new[] { new EnvironmentSurfaceBinding { renderer = renderer,
                    slots = new[] {0}, excludedSlots = new[] {1} } };
                Assert.That(ReactiveEnvironmentSurface.Validate(root.transform, bindings, null), Is.Null);
                surface = new ReactiveEnvironmentSurface(bindings);
                surface.Apply(new EnvironmentReactionState {wetness=1,durability=100,fuel=20},100,20);
                renderer.GetPropertyBlock(block,0);
                Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.red));
                Assert.That(block.GetVector("_EnvState0").x, Is.EqualTo(1));
                renderer.GetPropertyBlock(block,1); Assert.That(block.isEmpty, Is.True);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                surface.Release(); renderer.GetPropertyBlock(block,0); Assert.That(block.isEmpty, Is.True);
                renderer.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(Color.red));
                block.Clear(); block.SetColor("_BaseColor",Color.blue); renderer.SetPropertyBlock(block,0);
                surface.Apply(new EnvironmentReactionState {wetness=1,durability=100,fuel=20},100,20);
                surface.Release(); renderer.GetPropertyBlock(block,0);
                Assert.That(block.GetColor("_BaseColor"),Is.EqualTo(Color.blue));
                bindings[0].excludedSlots = new int[0];
                Assert.That(ReactiveEnvironmentSurface.Validate(root.transform, bindings, null), Is.Not.Null);
            }
            finally { surface?.Release(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void GpuWetnessDarkensAndExtinguishingRemovesOnlyEmission()
        {
            var material = NewMaterial();
            try
            {
                material.SetVector("_EnvState0", Vector4.zero);
                Color[] dry = Render(material);
                material.SetVector("_EnvState0", new Vector4(1,0,0,0));
                Color[] wet = Render(material);
                Assert.That(CountPixels(dry), Is.GreaterThan(100));
                Assert.That(Brightness(wet), Is.LessThan(Brightness(dry)));
                material.SetVector("_EnvState0", new Vector4(0,0.5f,0.3f,1));
                Color[] burning = Render(material);
                material.SetVector("_EnvState0", new Vector4(0,0.5f,0.3f,0));
                Color[] extinguished = Render(material);
                Assert.That(Brightness(burning), Is.GreaterThan(Brightness(extinguished) + 0.01f));
                Assert.That(material.GetVector("_EnvState0").y, Is.EqualTo(0.5f));
                Assert.That(material.GetVector("_EnvState0").z, Is.EqualTo(0.3f));
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void OnlyExplicitFoliageCoverageMayRemoveTheSilhouette()
        {
            var material = NewMaterial();
            try
            {
                material.SetFloat("_AllowFuelCoverage", 1); material.SetFloat("_FuelCoverage", 0);
                material.SetFloat("_SurfaceRole", 0);
                Assert.That(CountPixels(Render(material)), Is.GreaterThan(100));
                material.SetFloat("_SurfaceRole", 1);
                Assert.That(CountPixels(Render(material)), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void FoliageIgnoresWoodDamageAndBurningEdges()
        {
            var material = NewMaterial();
            try
            {
                material.SetFloat("_SurfaceRole", 1);
                material.SetVector("_EnvState0", Vector4.zero);
                Color[] dry = Render(material);
                material.SetVector("_EnvState0", new Vector4(0, 0, 1, 1));
                CollectionAssert.AreEqual(dry, Render(material), "잎은 목재 손상과 붉은 경계선을 합성하지 않는다");
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [UPlaygroundTool("Environment/CaptureToonSamples")]
        public static void CaptureSamples()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "output/ReactiveEnvironment/Toon");
            Directory.CreateDirectory(folder);
            var material = NewMaterial();
            try
            {
                var samples = new[] { Vector4.zero, new Vector4(1,0,0,0), new Vector4(0,0.6f,0.4f,0), new Vector4(0,0.6f,0.4f,1) };
                string[] names = { "dry", "wet", "charred", "burning" };
                for (int i = 0; i < samples.Length; i++)
                {
                    material.SetVector("_EnvState0", samples[i]);
                    var texture = new Texture2D(64,64,TextureFormat.RGBA32,false,true);
                    try
                    {
                        Color[] colors=Render(material);
                        for(int pixel=0;pixel<colors.Length;pixel++) colors[pixel]=colors[pixel].gamma;
                        texture.SetPixels(colors); texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,names[i]+".png"), texture.EncodeToPNG());
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        private static Material NewMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            Assert.That(shader, Is.Not.Null); Assert.That(shader.isSupported, Is.True);
            var material = new Material(shader);
            material.SetFloat("_Cull", 0);
            material.SetColor("_BaseColor", new Color(0.65f,0.4f,0.2f,1));
            return material;
        }

        private static Color[] Render(Material material)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-0.8f,-0.8f,0), new Vector3(0.8f,-0.8f,0), new Vector3(0.8f,0.8f,0), new Vector3(-0.8f,0.8f,0) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            var target = RenderTexture.GetTemporary(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false,true);
            var previous = RenderTexture.active;
            string[] globals = { "_MainLightPosition", "_MainLightColor", "unity_LightData", "_WorldSpaceCameraPos", "unity_FogParams" };
            var saved = new Vector4[globals.Length];
            for (int i = 0; i < globals.Length; i++) saved[i] = Shader.GetGlobalVector(globals[i]);
            var commands = new CommandBuffer();
            bool asynchronous = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true,true,Color.clear);
                Matrix4x4 view = Matrix4x4.Scale(new Vector3(1,1,-1)) * Matrix4x4.Translate(new Vector3(0,0,3));
                commands.SetViewProjectionMatrices(view, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1,1,-1,1,0.1f,10),true));
                commands.SetGlobalVector("_MainLightPosition", new Vector4(0,0,-1,0));
                commands.SetGlobalVector("_MainLightColor", Vector4.one);
                commands.SetGlobalVector("unity_LightData", new Vector4(0,0,1,0));
                commands.SetGlobalVector("_WorldSpaceCameraPos", new Vector4(0,0,-3,1));
                commands.SetGlobalVector("unity_FogParams", new Vector4(0,0,0,1));
                commands.DrawMesh(mesh,Matrix4x4.identity,material,0,material.FindPass("Forward"));
                Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,64,64),0,0); texture.Apply();
                return texture.GetPixels();
            }
            finally
            {
                for (int i = 0; i < globals.Length; i++) Shader.SetGlobalVector(globals[i],saved[i]);
                ShaderUtil.allowAsyncCompilation = asynchronous;
                commands.Release(); RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        private static int CountPixels(Color[] colors) { int count = 0; foreach (Color c in colors) if (c.a > 0.5f) count++; return count; }
        private static float Brightness(Color[] colors)
        { float sum = 0; foreach (Color c in colors) sum += c.r + c.g + c.b; return sum / colors.Length; }
    }
}
