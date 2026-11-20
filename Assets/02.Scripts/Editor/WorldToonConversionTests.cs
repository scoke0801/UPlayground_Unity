using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Rendering;
using UPlayGround.Tool.Editor.WorldToon;

namespace UPlayGround.Editor.Tests
{
    /// <summary>WorldToon 변환 계약: 보호·미지원 차단, 반복 변환의 중복 없음, 수정된 생성본 보호, Terrain 4레이어 제한.</summary>
    public sealed class WorldToonConversionTests
    {
        private const string TempRoot = "Assets/__WorldToonTestTemp";
        private WorldToonStyleProfileSO _style;

        [SetUp]
        public void SetUp()
        {
            _style = ScriptableObject.CreateInstance<WorldToonStyleProfileSO>();
            if (!AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.CreateFolder("Assets", "__WorldToonTestTemp");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_style);
            AssetDatabase.DeleteAsset(TempRoot);
        }

        private static Material CreateSourceAsset(string shaderName, string name)
        {
            Shader shader = Shader.Find(shaderName);
            Assume.That(shader, Is.Not.Null, shaderName + " 셰이더가 프로젝트에 없음");
            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, $"{TempRoot}/{name}.mat");
            return material;
        }

        [Test]
        public void LilToonMaterial_IsProtected_AndNotCopied()
        {
            Material source = CreateSourceAsset("lilToon", "ProtectedSource");
            string before = WorldToonMaterialConverter.ComputeContentHash(source);
            var record = new WorldToonConversionRecord();

            WorldToonMaterialAnalysis analysis = WorldToonMaterialAnalyzer.Analyze(source);
            WorldToonMaterialConverter.CopyResult result = WorldToonMaterialConverter.CreateCopy(analysis, WorldToonPreset.MeshLite, _style, record, TempRoot);

            Assert.That(analysis.Status, Is.EqualTo(WorldToonConversionStatus.Protected));
            Assert.That(result.Material, Is.Null);
            Assert.That(record.generated, Is.Empty);
            Assert.That(WorldToonMaterialConverter.ComputeContentHash(source), Is.EqualTo(before));
        }

        [Test]
        public void TransparentLit_IsUnsupported()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "TransparentSource");
            source.SetFloat("_Surface", 1f);
            source.renderQueue = 3000;

            WorldToonMaterialAnalysis analysis = WorldToonMaterialAnalyzer.Analyze(source);

            Assert.That(analysis.Status, Is.EqualTo(WorldToonConversionStatus.Unsupported));
        }

        [Test]
        public void RepeatedConversion_ReusesSameAsset_AndKeepsSourceUnchanged()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "OpaqueSource");
            source.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.2f, 1f));
            string sourceHash = WorldToonMaterialConverter.ComputeContentHash(source);
            var record = new WorldToonConversionRecord();

            var first = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot);
            var second = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot);

            Assert.That(first.Material, Is.Not.Null);
            Assert.That(second.IsReused, Is.True);
            Assert.That(AssetDatabase.GetAssetPath(second.Material), Is.EqualTo(AssetDatabase.GetAssetPath(first.Material)));
            Assert.That(record.generated.Count, Is.EqualTo(1));
            Assert.That(AssetDatabase.FindAssets("t:Material", new[] { TempRoot + "/MeshLite" }).Length, Is.EqualTo(1));
            Assert.That(WorldToonMaterialConverter.ComputeContentHash(source), Is.EqualTo(sourceHash));
            Assert.That(first.Material.shader.name, Is.EqualTo(WorldToonVariantManifest.MeshShader));
            Color copied = first.Material.GetColor("_BaseColor");
            Assert.That(Vector4.Distance(copied, new Color(0.3f, 0.5f, 0.2f, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void PresetKeywords_FollowManifestOnly()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "PresetSource");
            var record = new WorldToonConversionRecord();

            Material standard = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshStandard, _style, record, TempRoot).Material;
            Material far = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshFar, _style, record, TempRoot).Material;

            Assert.That(standard.IsKeywordEnabled(WorldToonVariantManifest.AdditionalLightsKeyword), Is.True);
            Assert.That(standard.IsKeywordEnabled(WorldToonVariantManifest.ReceiveShadowsOffKeyword), Is.False);
            Assert.That(far.IsKeywordEnabled(WorldToonVariantManifest.ReceiveShadowsOffKeyword), Is.True);
            Assert.That(far.IsKeywordEnabled(WorldToonVariantManifest.AdditionalLightsKeyword), Is.False);
        }

        [Test]
        public void CutoutSource_UsesAlphaTestQueueAndKeyword()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "CutoutSource");
            source.SetFloat("_AlphaClip", 1f);
            source.EnableKeyword("_ALPHATEST_ON");
            source.SetFloat("_Cutoff", 0.33f);
            var record = new WorldToonConversionRecord();

            Material copy = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot).Material;

            Assert.That(copy.IsKeywordEnabled(WorldToonVariantManifest.AlphaTestKeyword), Is.True);
            Assert.That(copy.renderQueue, Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.AlphaTest));
            Assert.That(copy.GetFloat("_Cutoff"), Is.EqualTo(0.33f).Within(1e-4f));
        }

        [Test]
        public void HandEditedGeneratedMaterial_IsNotOverwritten()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "EditedSource");
            var record = new WorldToonConversionRecord();
            Material copy = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot).Material;

            copy.SetFloat("_BandSoftness", 0.3f);
            AssetDatabase.SaveAssetIfDirty(copy);
            var again = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot);

            Assert.That(again.IsConflict, Is.True);
            Assert.That(copy.GetFloat("_BandSoftness"), Is.EqualTo(0.3f).Within(1e-4f));
        }

        [Test]
        public void TerrainWithFiveLayers_IsBlocked_AndFourIsReady()
        {
            Texture2D diffuse = Texture2D.whiteTexture;
            var layers = new TerrainLayer[5];
            for (int i = 0; i < layers.Length; i++)
                layers[i] = new TerrainLayer { diffuseTexture = diffuse };

            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 10, 32) };
            var terrainObject = Terrain.CreateTerrainGameObject(data);
            try
            {
                var terrain = terrainObject.GetComponent<Terrain>();
                data.terrainLayers = layers;
                Assert.That(WorldToonTerrainValidator.Analyze(terrain).Status, Is.EqualTo(WorldToonConversionStatus.Unsupported));
                Assert.That(WorldToonTerrainValidator.Analyze(terrain).Warnings[0], Does.StartWith("WT-TERRAIN-001"));

                data.terrainLayers = new[] { layers[0], layers[1], layers[2], layers[3] };
                terrain.materialTemplate = null;
                Assert.That(WorldToonTerrainValidator.Analyze(terrain).Status, Is.EqualTo(WorldToonConversionStatus.Ready));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(terrainObject);
                foreach (TerrainLayer layer in layers)
                    UnityEngine.Object.DestroyImmediate(layer);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void SkinnedSlot_IsBlockedUntilReviewed()
        {
            Material source = CreateSourceAsset("Universal Render Pipeline/Lit", "SkinnedSource");
            var record = new WorldToonConversionRecord();
            Material copy = WorldToonMaterialConverter.CreateCopy(WorldToonMaterialAnalyzer.Analyze(source), WorldToonPreset.MeshLite, _style, record, TempRoot).Material;
            var actor = new GameObject("SkinnedActor");
            try
            {
                var renderer = actor.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMaterial = source;

                string result = WorldToonMaterialConverter.ApplyToSlot(renderer, 0, copy, record);

                Assert.That(result, Does.Contain("WT-MAT-002"));
                Assert.That(renderer.sharedMaterial == source, Is.True);
                Assert.That(record.applied, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void Manifest_CoversEveryPreset()
        {
            foreach (WorldToonPreset preset in System.Enum.GetValues(typeof(WorldToonPreset)))
            {
                WorldToonVariantManifest.Entry entry = WorldToonVariantManifest.Get(preset);
                Assert.That(WorldToonVariantManifest.IsWorldToonShader(entry.ShaderName), Is.True, preset.ToString());
            }
        }
    }
}
