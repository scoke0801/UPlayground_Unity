using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Save;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 지도 한 장의 탐험 기록. 지도 이미지가 아닌 월드 XZ 원점에 고정한 희소 격자라서
    /// 미니맵을 다시 촬영해 범위가 바뀌어도 저장된 탐험 위치가 어긋나지 않는다.
    /// </summary>
    public sealed class ExplorationGrid
    {
        private const int ChunkShift = 5;
        private const int ChunkSize = 1 << ChunkShift;
        private const int ChunkMask = ChunkSize - 1;
        private const int ChunkByteLength = ChunkSize * sizeof(uint);

        // 청크 좌표 → 행 단위 비트마스크
        private readonly Dictionary<Vector2Int, uint[]> _chunks = new();

        public ExplorationGrid(float cellSize)
        {
            CellSize = Mathf.Max(0.1f, cellSize);
        }

        /// <summary>한 칸의 월드 길이. 저장된 기록은 생성 당시 크기를 유지한다.</summary>
        public float CellSize { get; }

        public bool IsExplored(int x, int y) =>
            _chunks.TryGetValue(new Vector2Int(x >> ChunkShift, y >> ChunkShift), out var rows)
            && (rows[y & ChunkMask] & (1u << (x & ChunkMask))) != 0;

        public bool IsExploredAt(Vector2 position) => IsExplored(ToCell(position.x), ToCell(position.y));

        public int ToCell(float coordinate) => Mathf.FloorToInt(coordinate / CellSize);

        /// <summary>
        /// 원 안에 중심이 들어오는 칸과 현재 위치의 칸을 밝힌다.
        /// 새로 밝힌 칸이 있으면 true와 그 칸들을 감싸는 범위를 돌려준다.
        /// </summary>
        public bool RevealCircle(Vector2 center, float radius, out RectInt changed)
        {
            int minX = ToCell(center.x - radius), maxX = ToCell(center.x + radius);
            int minY = ToCell(center.y - radius), maxY = ToCell(center.y + radius);
            int ownX = ToCell(center.x), ownY = ToCell(center.y);
            float sqRadius = radius * radius;

            int changedMinX = int.MaxValue, changedMinY = int.MaxValue;
            int changedMaxX = int.MinValue, changedMaxY = int.MinValue;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = (y + 0.5f) * CellSize - center.y;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x + 0.5f) * CellSize - center.x;
                    bool isOwnCell = x == ownX && y == ownY;
                    if (!isOwnCell && dx * dx + dy * dy > sqRadius) continue;
                    if (!Mark(x, y)) continue;

                    changedMinX = Mathf.Min(changedMinX, x);
                    changedMinY = Mathf.Min(changedMinY, y);
                    changedMaxX = Mathf.Max(changedMaxX, x);
                    changedMaxY = Mathf.Max(changedMaxY, y);
                }
            }

            bool hasChange = changedMaxX >= changedMinX;
            changed = hasChange
                ? new RectInt(changedMinX, changedMinY,
                    changedMaxX - changedMinX + 1, changedMaxY - changedMinY + 1)
                : default;
            return hasChange;
        }

        /// <summary>범위 안의 칸을 모두 밝힌다. 새로 밝힌 칸이 있으면 true.</summary>
        public bool RevealRect(RectInt cells)
        {
            bool hasChange = false;
            for (int y = cells.yMin; y < cells.yMax; y++)
            for (int x = cells.xMin; x < cells.xMax; x++)
                hasChange |= Mark(x, y);
            return hasChange;
        }

        private bool Mark(int x, int y)
        {
            var key = new Vector2Int(x >> ChunkShift, y >> ChunkShift);
            if (!_chunks.TryGetValue(key, out var rows))
                _chunks[key] = rows = new uint[ChunkSize];

            uint bit = 1u << (x & ChunkMask);
            if ((rows[y & ChunkMask] & bit) != 0) return false;
            rows[y & ChunkMask] |= bit;
            return true;
        }

        public MapExplorationLayerSaveData Export(string key)
        {
            var data = new MapExplorationLayerSaveData { key = key, cellSize = CellSize };
            foreach (var pair in _chunks)
            {
                var bytes = new byte[ChunkByteLength];
                Buffer.BlockCopy(pair.Value, 0, bytes, 0, ChunkByteLength);
                data.chunks.Add(new MapExplorationChunkSaveData
                {
                    x = pair.Key.x,
                    y = pair.Key.y,
                    bits = Convert.ToBase64String(bytes),
                });
            }
            return data;
        }

        /// <summary>손상된 청크는 건너뛴다. 탐험 기록 일부 때문에 세이브 전체 로드를 막지 않는다.</summary>
        public static ExplorationGrid Import(MapExplorationLayerSaveData data)
        {
            if (data == null || data.cellSize <= 0f) return null;

            var grid = new ExplorationGrid(data.cellSize);
            if (data.chunks == null) return grid;

            foreach (var chunk in data.chunks)
            {
                if (chunk == null || string.IsNullOrEmpty(chunk.bits)) continue;

                byte[] bytes;
                try { bytes = Convert.FromBase64String(chunk.bits); }
                catch (FormatException) { continue; }
                if (bytes.Length != ChunkByteLength) continue;

                var rows = new uint[ChunkSize];
                Buffer.BlockCopy(bytes, 0, rows, 0, ChunkByteLength);
                grid._chunks[new Vector2Int(chunk.x, chunk.y)] = rows;
            }
            return grid;
        }
    }
}
