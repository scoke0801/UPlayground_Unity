using System;

namespace UPlayGround.WorldBuilder.Core
{
    public enum HeightmapRowOrigin { South, North }

    /// <summary>명시적인 행 방향으로 헤더 없는 R16 little-endian을 교환한다.</summary>
    public static class Raw16HeightmapCodec
    {
        /// <summary>파일 행을 남쪽부터 증가하는 불변 높이 필드로 해석한다.</summary>
        public static HeightField Decode(byte[] bytes, int width, int height, HeightmapRowOrigin rowOrigin)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            ValidateDimensions(width, height, rowOrigin);
            if (bytes.Length != checked(width * height * 2)) throw new ArgumentException("RAW 바이트 수가 규격과 다릅니다.", nameof(bytes));
            var samples = new ushort[checked(width * height)];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int row = rowOrigin == HeightmapRowOrigin.North ? height - 1 - z : z;
                int offset = 2 * (row * width + x);
                samples[z * width + x] = (ushort)(bytes[offset] | bytes[offset + 1] << 8);
            }
            return new HeightField(width, height, samples);
        }

        /// <summary>float 변환 없이 R16 원본의 모든 비트를 내보낸다.</summary>
        public static byte[] Encode(HeightField field, HeightmapRowOrigin rowOrigin)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            ValidateDimensions(field.Width, field.Height, rowOrigin);
            var bytes = new byte[checked(field.Width * field.Height * 2)];
            for (int z = 0; z < field.Height; z++)
            for (int x = 0; x < field.Width; x++)
            {
                int row = rowOrigin == HeightmapRowOrigin.North ? field.Height - 1 - z : z;
                int offset = 2 * (row * field.Width + x);
                ushort sample = field[z, x];
                bytes[offset] = (byte)sample;
                bytes[offset + 1] = (byte)(sample >> 8);
            }
            return bytes;
        }

        /// <summary>Unity에서 읽은 정규화 높이를 범위 검증 후 R16으로 양자화한다.</summary>
        public static HeightField FromNormalized(float[,] heights)
        {
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            int height = heights.GetLength(0), width = heights.GetLength(1);
            ValidateDimensions(width, height, HeightmapRowOrigin.South);
            var samples = new ushort[checked(width * height)];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                float value = heights[z, x];
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(heights), "유한한 0~1 높이만 허용합니다.");
                samples[z * width + x] = (ushort)Math.Round(value * 65535.0, MidpointRounding.AwayFromZero);
            }
            return new HeightField(width, height, samples);
        }

        private static void ValidateDimensions(int width, int height, HeightmapRowOrigin rowOrigin)
        {
            if (width < 2 || height < 2) throw new ArgumentOutOfRangeException(nameof(width));
            if (rowOrigin != HeightmapRowOrigin.South && rowOrigin != HeightmapRowOrigin.North) throw new ArgumentOutOfRangeException(nameof(rowOrigin));
        }
    }
}
