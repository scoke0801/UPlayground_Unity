using System;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>남서 원점·+X 동·+Z 북 좌표를 엔진과 분리해 계산한다.</summary>
    public sealed class WorldGrid
    {
        public double WorldSize { get; }
        public int SampleCount { get; }
        public double OriginY { get; }
        public double VerticalRange { get; }
        public double SampleSpacing => WorldSize / (SampleCount - 1);

        public WorldGrid(double worldSize, int sampleCount, double originY, double verticalRange)
        {
            if (!IsFinite(worldSize) || worldSize <= 0 || sampleCount < 2) throw new ArgumentOutOfRangeException(nameof(worldSize));
            if (!IsFinite(originY) || !IsFinite(verticalRange) || verticalRange <= 0) throw new ArgumentOutOfRangeException(nameof(verticalRange));
            WorldSize = worldSize;
            SampleCount = sampleCount;
            OriginY = originY;
            VerticalRange = verticalRange;
        }

        /// <summary>전역 X 또는 Z 표본 좌표를 남서 기준 미터로 변환한다.</summary>
        public double SampleToWorld(double sample)
        {
            if (!IsFinite(sample) || sample < 0 || sample > SampleCount - 1) throw new ArgumentOutOfRangeException(nameof(sample));
            return sample * SampleSpacing;
        }

        /// <summary>남서 기준 미터를 보간 가능한 전역 표본 좌표로 변환한다.</summary>
        public double WorldToSample(double position)
        {
            if (!IsFinite(position) || position < 0 || position > WorldSize) throw new ArgumentOutOfRangeException(nameof(position));
            return position / SampleSpacing;
        }

        /// <summary>원본 양자화 표본을 월드 고도로 변환한다.</summary>
        public double SampleToHeight(ushort sample) => OriginY + sample / 65535.0 * VerticalRange;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
