using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Content.Tests
{
    /// <summary>볼륨 데이터와 하늘 재질의 연결, 지역별 부피 보간을 검사한다.</summary>
    public sealed class VolumetricCloudTests
    {
        [Test]
        public void 구름_부피는_지역_보간과_평가에서_끝점과_중간값을_보존한다()
        {
            var profile = ScriptableObject.CreateInstance<SkyProfileSO>();
            try
            {
                profile.cloudDepth = 1200;
                profile.cloudDensity = 1;
                profile.cloudErosion = 0.2f;
                profile.cloudFormationScale = 16000;
                profile.cloudEdgeWidth = 100;
                SkySample a = profile.Evaluate(0.4f);
                profile.cloudDepth = 2400;
                profile.cloudDensity = 3;
                profile.cloudErosion = 0.8f;
                profile.cloudFormationScale = 32000;
                profile.cloudEdgeWidth = 200;
                SkySample b = profile.Evaluate(0.4f);
                Assert.AreEqual(1200, SkySample.Lerp(a, b, 0).Depth);
                Assert.AreEqual(3, SkySample.Lerp(a, b, 1).Density);
                Assert.AreEqual(1800, SkySample.Lerp(a, b, 0.5f).Depth);
                Assert.AreEqual(2, SkySample.Lerp(a, b, 0.5f).Density);
                Assert.AreEqual(0.5f, SkySample.Lerp(a, b, 0.5f).Erosion, 0.0001f);
                Assert.AreEqual(24000, SkySample.Lerp(a, b, 0.5f).FormationScale);
                Assert.AreEqual(150, SkySample.Lerp(a, b, 0.5f).EdgeWidth);
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 본편과_공통_하늘은_실제_주기적_볼륨을_참조한다()
        {
            var volume = AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/10.Datas/World/Sky/CloudVolume.asset");
            Assert.IsNotNull(volume);
            Assert.AreEqual(128, volume.width);
            Assert.AreEqual(128, volume.height);
            Assert.AreEqual(128, volume.depth);
            Assert.AreEqual(TextureWrapMode.Repeat, volume.wrapMode);
            Assert.That(volume.mipmapCount, Is.GreaterThan(1));
            var pixels = volume.GetPixels(0);
            Vector4 min = Vector4.one, max = Vector4.zero;
            for (int i = 0; i < pixels.Length; i += 997)
            {
                var value = (Vector4)pixels[i];
                for (int channel = 0; channel < 4; channel++)
                {
                    Assert.That(value[channel], Is.InRange(0, 1));
                    min[channel] = Mathf.Min(min[channel], value[channel]);
                    max[channel] = Mathf.Max(max[channel], value[channel]);
                }
            }
            for (int channel = 0; channel < 4; channel++) Assert.That(max[channel] - min[channel], Is.GreaterThan(0.4f));
            foreach (string path in new[] { "Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinSky.mat", "Assets/10.Datas/World/Sky/DaySky_Clear.mat" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(material, path);
                Assert.AreEqual(volume, material.GetTexture("_CloudVolume"), path);
                Assert.AreEqual(1, material.GetFloat("_CloudVolumeEnabled"), path);
                Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader), path);
            }
        }

        [Test]
        public void 적운_군락은_높이가_다른_봉우리와_빈_하늘을_가지고_재질에_연결된다()
        {
            var formation = AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/10.Datas/World/Sky/CloudFormations.asset");
            Assert.IsNotNull(formation);
            Assert.AreEqual(256, formation.width);
            Assert.AreEqual(128, formation.height);
            Assert.AreEqual(256, formation.depth);
            var pixels = formation.GetPixels(1);
            int width = formation.width / 2, height = formation.height / 2, depth = formation.depth / 2;
            int highest = 0, lowest = height, occupied = 0, empty = 0;
            for (int z = 0; z < depth; z += 8)
            for (int x = 0; x < width; x += 8)
            {
                int top = -1;
                for (int y = 0; y < height; y++)
                    if (pixels[(z * height + y) * width + x].r > 0.51f) top = y;
                if (top < 0) { empty++; continue; }
                occupied++;
                highest = Mathf.Max(highest, top);
                lowest = Mathf.Min(lowest, top);
            }
            Assert.That(occupied, Is.GreaterThan(20));
            Assert.That(empty, Is.GreaterThan(20));
            Assert.That(highest, Is.GreaterThan(height * 0.7f));
            Assert.That(highest - lowest, Is.GreaterThan(height * 0.3f));
            foreach (string path in new[] { "Assets/10.Datas/World/RiverBasinWorld/VisualR1/RiverBasinSky.mat", "Assets/10.Datas/World/Sky/DaySky_Clear.mat" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.AreEqual(formation, material.GetTexture("_CloudFormation"), path);
                Assert.AreEqual(1, material.GetFloat("_CloudFormationEnabled"), path);
            }
        }
    }
}
