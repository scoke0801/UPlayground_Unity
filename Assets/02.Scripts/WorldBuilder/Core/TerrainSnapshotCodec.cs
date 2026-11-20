using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>결과 및 수동 패치를 버전이 명시된 little-endian 바이너리로 보존한다.</summary>
    public static class TerrainSnapshotCodec
    {
        private const uint SnapshotMagic = 0x53544257;
        private const uint PatchMagic = 0x50544257;
        private const int Version = 1;

        /// <summary>차원·높이·표면을 버전이 있는 정규 바이트로 기록한다.</summary>
        public static byte[] Encode(TerrainBuildResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            const int headerBytes = 12;
            int samples = result.SampleCount;
            int cells = samples - 1;
            var bytes = new byte[checked(headerBytes + sizeof(float) * samples * samples + cells * cells)];
            using (var stream = new MemoryStream(bytes, writable: true))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(SnapshotMagic);
                writer.Write(Version);
                writer.Write(samples);
            }
            result.CopySnapshotPayload(bytes, headerBytes);
            return bytes;
        }

        /// <summary>완전한 결과만 복원하고 잘린 데이터와 후행 바이트를 거절한다.</summary>
        public static TerrainBuildResult Decode(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length < 12) throw new InvalidDataException("결과 헤더가 없습니다.");
            using (var reader = new BinaryReader(new MemoryStream(bytes, false)))
            {
                ValidateHeader(reader, SnapshotMagic);
                int size = reader.ReadInt32();
                long count = (long)size * size;
                if (size < 2 || count > int.MaxValue || 12L + 4L * count + (long)(size - 1) * (size - 1) != bytes.Length)
                    throw new InvalidDataException("결과 크기 또는 끝 위치가 유효하지 않습니다.");
                var heights = new float[checked(size * size)];
                var surfaces = new bool[checked((size - 1) * (size - 1))];
                for (int i = 0; i < heights.Length; i++) heights[i] = ReadFinite(reader);
                for (int i = 0; i < surfaces.Length; i++)
                {
                    byte value = reader.ReadByte();
                    if (value > 1) throw new InvalidDataException("표면 값은 0 또는 1이어야 합니다.");
                    surfaces[i] = value == 1;
                }
                return new TerrainBuildResult(size, heights, surfaces);
            }
        }

        /// <summary>직사각형 값과 마스크를 외부 패치 참조용으로 기록한다.</summary>
        public static byte[] EncodePatch(int width, int height, float[] values, float[] masks)
        {
            if (width < 1 || height < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (values == null || masks == null) throw new ArgumentNullException(nameof(values));
            int count = checked(width * height);
            TerrainOperation.ValidateArray(values, count, false);
            TerrainOperation.ValidateArray(masks, count, true);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(PatchMagic); writer.Write(Version); writer.Write(width); writer.Write(height);
                for (int i = 0; i < count; i++) writer.Write(values[i]);
                for (int i = 0; i < count; i++) writer.Write(masks[i]);
                return stream.ToArray();
            }
        }

        /// <summary>형식과 모든 수치 검사를 통과한 패치만 복원한다.</summary>
        public static void DecodePatch(byte[] bytes, out int width, out int height, out float[] values, out float[] masks)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length < 16) throw new InvalidDataException("패치 헤더가 없습니다.");
            using (var reader = new BinaryReader(new MemoryStream(bytes, false)))
            {
                ValidateHeader(reader, PatchMagic);
                int decodedWidth = reader.ReadInt32(), decodedHeight = reader.ReadInt32();
                long decodedCount = (long)decodedWidth * decodedHeight;
                if (decodedWidth < 1 || decodedHeight < 1 || decodedCount > int.MaxValue || 16L + 8L * decodedCount != bytes.Length)
                    throw new InvalidDataException("패치 크기 또는 끝 위치가 유효하지 않습니다.");
                int count = checked(decodedWidth * decodedHeight);
                var decodedValues = new float[count];
                var decodedMasks = new float[count];
                for (int i = 0; i < count; i++) decodedValues[i] = ReadFinite(reader);
                for (int i = 0; i < count; i++)
                {
                    decodedMasks[i] = ReadFinite(reader);
                    if (decodedMasks[i] < 0 || decodedMasks[i] > 1) throw new InvalidDataException("마스크 범위가 유효하지 않습니다.");
                }
                width = decodedWidth; height = decodedHeight; values = decodedValues; masks = decodedMasks;
            }
        }

        /// <summary>스냅샷과 원본에 저장하는 소문자 SHA-256 지문을 계산한다.</summary>
        public static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
#if UNITY_EDITOR_WIN
            // 4km 저작에서 수십 MiB의 버퍼를 해시할 때 Windows CNG의 SHA-256 공급자를 사용한다.
            const int sha256AlgorithmHandle = 0x00000041;
            if (bytes.Length >= 1024 * 1024)
            {
                var digest = new byte[32];
                if (BCryptHash(new IntPtr(sha256AlgorithmHandle), IntPtr.Zero, 0, bytes, bytes.Length, digest, digest.Length) == 0)
                    return ToHex(digest);
            }
#endif
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(bytes));
        }

        private static string ToHex(byte[] digest) => BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();

#if UNITY_EDITOR_WIN
        [DllImport("bcrypt.dll", ExactSpelling = true)]
        private static extern int BCryptHash(IntPtr algorithm, IntPtr secret, int secretLength,
            [In] byte[] input, int inputLength, [Out] byte[] output, int outputLength);
#endif

        private static void ValidateHeader(BinaryReader reader, uint magic)
        {
            if (reader.ReadUInt32() != magic || reader.ReadInt32() != Version) throw new InvalidDataException("바이너리 형식 또는 버전이 다릅니다.");
        }

        private static float ReadFinite(BinaryReader reader)
        {
            float value = reader.ReadSingle();
            if (!TerrainOperation.IsFinite(value)) throw new InvalidDataException("유한한 높이가 필요합니다.");
            return value;
        }
    }
}
