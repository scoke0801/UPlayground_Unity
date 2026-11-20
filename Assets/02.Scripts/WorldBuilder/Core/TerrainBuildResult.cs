using System;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>미터 높이와 true=표면인 Hole 셀을 독립된 불변 결과로 보관한다.</summary>
    public sealed class TerrainBuildResult
    {
        private readonly float[] _heights;
        private readonly bool[] _surfaces;
        public int SampleCount { get; }

        public TerrainBuildResult(int sampleCount, float[] heights, bool[] surfaces)
        {
            if (sampleCount < 2) throw new ArgumentOutOfRangeException(nameof(sampleCount));
            if (heights == null || surfaces == null) throw new ArgumentNullException(nameof(heights));
            if (heights.Length != checked(sampleCount * sampleCount) || surfaces.Length != checked((sampleCount - 1) * (sampleCount - 1)))
                throw new ArgumentException("결과 격자 크기가 일치하지 않습니다.");
            TerrainOperation.ValidateArray(heights, heights.Length, false);
            SampleCount = sampleCount;
            _heights = (float[])heights.Clone();
            _surfaces = (bool[])surfaces.Clone();
        }

        /// <summary>전역 [z,x] 표본의 미터 높이를 조회한다.</summary>
        public float GetHeight(int z, int x)
        {
            if ((uint)x >= (uint)SampleCount || (uint)z >= (uint)SampleCount) throw new ArgumentOutOfRangeException(nameof(x));
            return _heights[z * SampleCount + x];
        }

        /// <summary>별도 [z,x] 셀 격자의 표면 유무를 조회한다.</summary>
        public bool GetSurface(int z, int x)
        {
            int cells = SampleCount - 1;
            if ((uint)x >= (uint)cells || (uint)z >= (uint)cells) throw new ArgumentOutOfRangeException(nameof(x));
            return _surfaces[z * cells + x];
        }

        /// <summary>원본 결과를 보존하는 독립 높이 배열을 반환한다.</summary>
        public float[] CopyHeights() => (float[])_heights.Clone();
        /// <summary>원본 결과를 보존하는 독립 표면 배열을 반환한다.</summary>
        public bool[] CopySurfaces() => (bool[])_surfaces.Clone();

        /// <summary>시각이나 실행 순서가 포함되지 않은 정규 little-endian 결과 해시를 만든다.</summary>
        public string ComputeHash() => TerrainSnapshotCodec.ComputeSha256(TerrainSnapshotCodec.Encode(this));

        internal void CopySnapshotPayload(byte[] bytes, int offset)
        {
            int heightBytes = checked(_heights.Length * sizeof(float));
            Buffer.BlockCopy(_heights, 0, bytes, offset, heightBytes);
            if (!BitConverter.IsLittleEndian)
            {
                for (int i = offset; i < offset + heightBytes; i += sizeof(float))
                    Array.Reverse(bytes, i, sizeof(float));
            }

            offset += heightBytes;
            for (int i = 0; i < _surfaces.Length; i++)
                bytes[offset + i] = _surfaces[i] ? (byte)1 : (byte)0;
        }
    }
}
