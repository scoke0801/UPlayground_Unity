using Unity.Collections;
using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>UI가 안개를 그리는 데 필요한 값. 마스크 텍스처의 알파가 1이면 탐험한 곳이다.</summary>
    public readonly struct MapFogView
    {
        public readonly Texture2D Mask;
        public readonly Material Material;
        /// <summary>마스크 좌하단의 지도 좌표(XZ).</summary>
        public readonly Vector2 WindowMin;
        /// <summary>마스크가 덮는 지도 좌표 크기(XZ).</summary>
        public readonly Vector2 WindowSize;
        /// <summary>마스크의 원본 탐험 기록. 아이콘이 안개에 가렸는지 판정할 때 쓴다.</summary>
        public readonly ExplorationGrid Grid;

        public MapFogView(ExplorationGrid grid, Texture2D mask, Material material, Vector2 windowMin,
            Vector2 windowSize)
        {
            Grid = grid;
            Mask = mask;
            Material = material;
            WindowMin = windowMin;
            WindowSize = windowSize;
        }
    }

    /// <summary>탐험 격자 중 지도 촬영 범위를 덮는 구간을 마스크 텍스처로 유지한다.</summary>
    public sealed class MapFogLayer
    {
        private readonly ExplorationGrid _grid;
        private bool _isDirty;

        public MapFogLayer(ExplorationGrid grid, RectInt window)
        {
            _grid = grid;
            Window = window;
            Mask = new Texture2D(window.width, window.height, TextureFormat.Alpha8, mipChain: false, linear: true)
            {
                name = "MapFogMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            Paint(window);
            Upload();
        }

        public RectInt Window { get; }
        public Texture2D Mask { get; private set; }
        public Vector2 WindowMin => (Vector2)Window.min * _grid.CellSize;
        public Vector2 WindowSize => (Vector2)Window.size * _grid.CellSize;

        /// <summary>지도 설정의 촬영 범위(회전·뒤집기 포함)를 감싸는 격자 칸 범위를 구한다.</summary>
        public static RectInt CalculateWindow(MinimapIconConfigSO config, float cellSize)
        {
            CalculateCaptureBounds(config, out Vector2 min, out Vector2 max);
            // 가장자리 블러와 경계 흔들림이 잘리지 않도록 한 칸 여유를 둔다.
            int minX = Mathf.FloorToInt(min.x / cellSize) - 1;
            int minY = Mathf.FloorToInt(min.y / cellSize) - 1;
            int maxX = Mathf.CeilToInt(max.x / cellSize) + 1;
            int maxY = Mathf.CeilToInt(max.y / cellSize) + 1;
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>촬영 범위를 감싸는 축 정렬 영역(XZ).</summary>
        public static void CalculateCaptureBounds(MinimapIconConfigSO config, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector2((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f);
                Vector3 world = config.MapImagePosToWorld(corner, Vector2.one);
                var point = new Vector2(world.x, world.z);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        /// <summary>새로 밝혀진 칸 주변만 다시 칠한다. 업로드는 <see cref="Upload"/>가 한다.</summary>
        public void Paint(RectInt cells)
        {
            if (Mask == null) return;

            int minX = Mathf.Max(Window.xMin, cells.xMin - 1);
            int minY = Mathf.Max(Window.yMin, cells.yMin - 1);
            int maxX = Mathf.Min(Window.xMax, cells.xMax + 1);
            int maxY = Mathf.Min(Window.yMax, cells.yMax + 1);
            if (minX >= maxX || minY >= maxY) return;

            NativeArray<byte> pixels = Mask.GetRawTextureData<byte>();
            for (int y = minY; y < maxY; y++)
            {
                int row = (y - Window.yMin) * Window.width;
                for (int x = minX; x < maxX; x++)
                    pixels[row + x - Window.xMin] = SampleCoverage(_grid, x, y);
            }
            _isDirty = true;
        }

        /// <summary>격자의 계단 모양이 드러나지 않도록 3×3 가중 평균을 낸다.</summary>
        public static byte SampleCoverage(ExplorationGrid grid, int x, int y)
        {
            int sum = 0;
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (!grid.IsExplored(x + offsetX, y + offsetY)) continue;
                sum += (2 - Mathf.Abs(offsetX)) * (2 - Mathf.Abs(offsetY));
            }
            return (byte)(sum * 255 / 16);
        }

        public void Upload()
        {
            if (!_isDirty || Mask == null) return;
            Mask.Apply(updateMipmaps: false);
            _isDirty = false;
        }

        public void Release()
        {
            if (Mask == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(Mask);
            else UnityEngine.Object.DestroyImmediate(Mask);
            Mask = null;
        }
    }
}
