using System;

namespace WorldBuilder.Core
{
    // Reference codec only. No Unity dependency. RAW header/metadata is external.
    // Returned/written normalized samples are canonical: row 0 = south, index = z*width+x.
    public static class Raw16HeightmapCodec
    {
        public static float[] DecodeLittleEndian(
            byte[] bytes, int width, int height, bool sourceRow0IsNorth)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            int count = GetSampleCount(width, height);
            int expectedBytes = checked(count * 2);
            if (bytes.Length != expectedBytes)
                throw new ArgumentException("RAW byte count does not match metadata.", nameof(bytes));

            var values = new float[count];
            for (int z = 0; z < height; ++z)
            {
                int fileRow = sourceRow0IsNorth ? height - 1 - z : z;
                for (int x = 0; x < width; ++x)
                {
                    int offset = 2 * (fileRow * width + x);
                    int sample = bytes[offset] | (bytes[offset + 1] << 8);
                    values[z * width + x] = sample / 65535.0f;
                }
            }
            return values;
        }

        public static byte[] EncodeLittleEndian(
            float[] values, int width, int height, bool targetRow0IsNorth)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            int count = GetSampleCount(width, height);
            if (values.Length != count)
                throw new ArgumentException("Sample count does not match metadata.", nameof(values));

            var bytes = new byte[checked(count * 2)];
            for (int z = 0; z < height; ++z)
            {
                int fileRow = targetRow0IsNorth ? height - 1 - z : z;
                for (int x = 0; x < width; ++x)
                {
                    float value = values[z * width + x];
                    if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                        throw new ArgumentOutOfRangeException(nameof(values),
                            "Normalized heights must be finite and within [0,1].");
                    int sample = (int)Math.Round(value * 65535.0,
                        MidpointRounding.AwayFromZero);
                    int offset = 2 * (fileRow * width + x);
                    bytes[offset] = (byte)(sample & 0xff);
                    bytes[offset + 1] = (byte)((sample >> 8) & 0xff);
                }
            }
            return bytes;
        }

        private static int GetSampleCount(int width, int height)
        {
            if (width < 2) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));
            return checked(width * height);
        }
    }
}
