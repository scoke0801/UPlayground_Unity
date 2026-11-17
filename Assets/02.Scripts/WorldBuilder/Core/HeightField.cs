using System;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>남쪽 첫 행의 R16 원본을 불변으로 보관해 공유 경계의 양자화를 유지한다.</summary>
    public sealed class HeightField
    {
        private readonly ushort[] _samples;
        public int Width { get; }
        public int Height { get; }

        public HeightField(int width, int height, ushort[] samples)
        {
            if (width < 2 || height < 2) throw new ArgumentOutOfRangeException(nameof(width));
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (samples.Length != checked(width * height)) throw new ArgumentException("높이 표본 수가 규격과 다릅니다.", nameof(samples));
            Width = width;
            Height = height;
            _samples = (ushort[])samples.Clone();
        }

        public ushort this[int z, int x]
        {
            get
            {
                if ((uint)x >= Width || (uint)z >= Height) throw new ArgumentOutOfRangeException(nameof(x));
                return _samples[z * Width + x];
            }
        }

        /// <summary>전역 원본에서 겹치는 경계 한 줄을 포함해 타일을 추출한다.</summary>
        public HeightField ExtractTile(int tileX, int tileZ, int tileCells)
        {
            if (tileCells < 1 || tileX < 0 || tileZ < 0) throw new ArgumentOutOfRangeException(nameof(tileCells));
            int startX = checked(tileX * tileCells), startZ = checked(tileZ * tileCells);
            if (startX > Width - 1 - tileCells || startZ > Height - 1 - tileCells) throw new ArgumentOutOfRangeException(nameof(tileX));
            int size = checked(tileCells + 1);
            var values = new ushort[checked(size * size)];
            for (int z = 0; z < size; z++)
                Array.Copy(_samples, (startZ + z) * Width + startX, values, z * size, size);
            return new HeightField(size, size, values);
        }

        /// <summary>타일 배열[z,x]를 합치며 불일치 경계는 봉합하지 않고 거부한다.</summary>
        public static HeightField JoinTiles(HeightField[,] tiles)
        {
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            int countZ = tiles.GetLength(0), countX = tiles.GetLength(1);
            if (countX == 0 || countZ == 0 || tiles[0, 0] == null) throw new ArgumentException("타일이 없습니다.", nameof(tiles));
            int size = tiles[0, 0].Width, cells = size - 1;
            int width = checked(countX * cells + 1), height = checked(countZ * cells + 1);
            var values = new ushort[checked(width * height)];
            var assigned = new bool[values.Length];
            for (int tz = 0; tz < countZ; tz++)
            for (int tx = 0; tx < countX; tx++)
            {
                var tile = tiles[tz, tx];
                if (tile == null || tile.Width != size || tile.Height != size) throw new ArgumentException("정사각 타일 규격이 다릅니다.", nameof(tiles));
                for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                {
                    int index = (tz * cells + z) * width + tx * cells + x;
                    ushort sample = tile[z, x];
                    if (assigned[index] && values[index] != sample) throw new ArgumentException("공유 경계의 R16 값이 다릅니다.", nameof(tiles));
                    values[index] = sample;
                    assigned[index] = true;
                }
            }
            return new HeightField(width, height, values);
        }
    }
}
