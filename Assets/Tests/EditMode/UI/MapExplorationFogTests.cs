using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>지도 탐험 격자의 기록·저장·지도 좌표 정합을 검증한다.</summary>
    public sealed class MapExplorationFogTests
    {
        [Test]
        public void 원_안의_칸만_밝히고_같은_자리는_변화가_없다()
        {
            var grid = new ExplorationGrid(10f);

            Assert.That(grid.RevealCircle(new Vector2(5, 5), 25f, out _), Is.True);
            Assert.That(grid.IsExploredAt(new Vector2(5, 5)), Is.True);
            Assert.That(grid.IsExploredAt(new Vector2(25, 5)), Is.True);
            Assert.That(grid.IsExploredAt(new Vector2(45, 5)), Is.False);
            Assert.That(grid.RevealCircle(new Vector2(5, 5), 25f, out _), Is.False);
        }

        [Test]
        public void 반경이_칸보다_작아도_서_있는_칸은_밝힌다()
        {
            var grid = new ExplorationGrid(10f);

            Assert.That(grid.RevealCircle(new Vector2(0.5f, 0.5f), 1f, out RectInt changed), Is.True);
            Assert.That(changed, Is.EqualTo(new RectInt(0, 0, 1, 1)));
        }

        [Test]
        public void 떨어진_두_지점_사이는_밝히지_않는다()
        {
            var grid = new ExplorationGrid(10f);
            grid.RevealCircle(Vector2.zero, 20f, out _);
            grid.RevealCircle(new Vector2(500, 0), 20f, out _);

            Assert.That(grid.IsExploredAt(new Vector2(250, 0)), Is.False);
        }

        [Test]
        public void 아이콘은_안개가_없으면_보이고_있으면_탐험한_곳에서만_보인다()
        {
            var grid = new ExplorationGrid(10f);
            grid.RevealCircle(new Vector2(5, 305), 20f, out _);

            Assert.That(UIMapFogOverlay.IsRevealed(null, new Vector3(900, 0, 900)), Is.True);
            // 지도 위치는 XZ를 쓴다. 높이(Y)는 판정에 영향이 없다.
            Assert.That(UIMapFogOverlay.IsRevealed(grid, new Vector3(5, 999, 305)), Is.True);
            Assert.That(UIMapFogOverlay.IsRevealed(grid, new Vector3(5, 305, 5)), Is.False);
        }

        [Test]
        public void 범위_전체_밝히기는_범위_안만_밝히고_저장_왕복에서_유지된다()
        {
            var grid = new ExplorationGrid(10f);
            var cells = new RectInt(-40, -3, 80, 38);

            Assert.That(grid.RevealRect(cells), Is.True);
            Assert.That(grid.RevealRect(cells), Is.False);

            var restored = ExplorationGrid.Import(grid.Export("world:Test"));
            for (int y = cells.yMin - 1; y <= cells.yMax; y++)
            for (int x = cells.xMin - 1; x <= cells.xMax; x++)
                Assert.That(restored.IsExplored(x, y), Is.EqualTo(cells.Contains(new Vector2Int(x, y))), $"{x},{y}");
        }

        [Test]
        public void 음수_좌표를_포함한_기록이_저장_왕복에서_유지된다()
        {
            var grid = new ExplorationGrid(4f);
            grid.RevealCircle(new Vector2(-300, -7), 30f, out _);
            grid.RevealCircle(new Vector2(900, 640), 30f, out _);

            var restored = ExplorationGrid.Import(grid.Export("world:Test"));

            Assert.That(restored.CellSize, Is.EqualTo(4f));
            for (int y = -20; y < 200; y++)
            for (int x = -100; x < 260; x++)
                Assert.That(restored.IsExplored(x, y), Is.EqualTo(grid.IsExplored(x, y)), $"{x},{y}");
        }

        [Test]
        public void 손상된_청크는_건너뛰고_나머지를_복원한다()
        {
            var grid = new ExplorationGrid(10f);
            grid.RevealCircle(new Vector2(5, 5), 5f, out _);
            MapExplorationLayerSaveData data = grid.Export("A");
            data.chunks.Add(new MapExplorationChunkSaveData { x = 9, y = 9, bits = "깨진값" });
            data.chunks.Add(new MapExplorationChunkSaveData { x = 8, y = 8, bits = "AAAA" });

            var restored = ExplorationGrid.Import(data);

            Assert.That(restored.IsExploredAt(new Vector2(5, 5)), Is.True);
        }

        [TestCase(false, false, 0f)]
        [TestCase(true, false, 0f)]
        [TestCase(false, true, 90f)]
        [TestCase(true, true, 33f)]
        public void 안개_마스크_위치가_지도_아이콘_좌표와_일치한다(bool flipX, bool flipY, float rotation)
        {
            var config = ScriptableObject.CreateInstance<MinimapIconConfigSO>();
            MapFogLayer layer = null;
            try
            {
                config.captureCenter = new Vector2(1200, 700);
                config.captureWorldSizeXY = new Vector2(2400, 2800);
                config.flipX = flipX;
                config.flipY = flipY;
                config.rotationDegrees = rotation;

                var grid = new ExplorationGrid(8f);
                var world = new Vector3(1700, 0, 250);
                grid.RevealCircle(new Vector2(world.x, world.z), 80f, out _);
                layer = new MapFogLayer(grid, MapFogLayer.CalculateWindow(config, grid.CellSize));
                var view = new MapFogView(grid, layer.Mask, null, layer.WindowMin, layer.WindowSize);

                // 아이콘이 찍히는 지도 UV에서 안개 마스크를 읽으면 탐험한 곳이어야 한다.
                Vector2 imageUv = config.WorldToMapImagePos(world, Vector2.one) + new Vector2(0.5f, 0.5f);
                Vector2 maskUv = UIMapFogOverlay.ImageUvToMaskUv(config, view, imageUv);

                Assert.That(layer.Mask.GetPixelBilinear(maskUv.x, maskUv.y).a, Is.GreaterThan(0.99f));
                Assert.That(layer.Mask.GetPixelBilinear(0.02f, 0.02f).a, Is.EqualTo(0f));
            }
            finally
            {
                layer?.Release();
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void 뒤늦게_밝힌_칸은_부분_갱신으로_마스크에_반영된다()
        {
            var grid = new ExplorationGrid(10f);
            var layer = new MapFogLayer(grid, new RectInt(0, 0, 32, 32));
            try
            {
                grid.RevealCircle(new Vector2(155, 155), 30f, out RectInt changed);
                layer.Paint(changed);
                layer.Upload();

                for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    Assert.That(layer.Mask.GetRawTextureData<byte>()[y * 32 + x],
                        Is.EqualTo(MapFogLayer.SampleCoverage(grid, x, y)), $"{x},{y}");
            }
            finally
            {
                layer.Release();
            }
        }

        [Test]
        public void 반경은_지도_크기에_비례하고_지도별_지정값이_우선한다()
        {
            var settings = ScriptableObject.CreateInstance<MapFogSettingsSO>();
            var config = ScriptableObject.CreateInstance<MinimapIconConfigSO>();
            try
            {
                Assert.That(settings.ResolveRevealRadius(config, 110f), Is.EqualTo(18f));
                Assert.That(settings.ResolveRevealRadius(config, 1800f), Is.EqualTo(54f).Within(0.01f));
                Assert.That(settings.ResolveRevealRadius(config, 4096f), Is.EqualTo(90f));

                config.fogRevealRadius = 40f;
                Assert.That(settings.ResolveRevealRadius(config, 4096f), Is.EqualTo(40f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(config);
            }
        }
    }
}
