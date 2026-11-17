using System;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>전역 표본 또는 별도의 Hole 셀 격자에서 반개방 영역을 나타낸다.</summary>
    public readonly struct SampleRect
    {
        public int X { get; }
        public int Z { get; }
        public int Width { get; }
        public int Height { get; }

        public SampleRect(int x, int z, int width, int height)
        {
            if (x < 0 || z < 0 || width < 1 || height < 1 || (long)x + width > int.MaxValue || (long)z + height > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "표본 영역이 유효하지 않습니다.");
            X = x; Z = z; Width = width; Height = height;
        }

        /// <summary>반개방 영역 안의 전역 좌표인지 확인한다.</summary>
        public bool Contains(int x, int z) => x >= X && z >= Z && x < (long)X + Width && z < (long)Z + Height;
        /// <summary>정사각 격자 전체의 표본 영역을 만든다.</summary>
        public static SampleRect Full(int sampleCount) => new SampleRect(0, 0, sampleCount, sampleCount);

        /// <summary>정수 넘침 없이 읽기 반경을 확장하고 격자 끝에서 자른다.</summary>
        public SampleRect Expand(int halo, int gridWidth, int gridHeight)
        {
            if (halo < 0 || gridWidth < 1 || gridHeight < 1 || (long)X + Width > gridWidth || (long)Z + Height > gridHeight)
                throw new ArgumentOutOfRangeException(nameof(halo));
            int left = (int)Math.Max(0L, (long)X - halo), bottom = (int)Math.Max(0L, (long)Z - halo);
            int right = (int)Math.Min(gridWidth, (long)X + Width + halo), top = (int)Math.Min(gridHeight, (long)Z + Height + halo);
            return new SampleRect(left, bottom, right - left, top - bottom);
        }

        /// <summary>이전·새 위치의 영향을 함께 포함하는 최소 영역을 만든다.</summary>
        public static SampleRect Union(SampleRect a, SampleRect b)
        {
            int x = Math.Min(a.X, b.X), z = Math.Min(a.Z, b.Z);
            return new SampleRect(x, z, Math.Max(a.X + a.Width, b.X + b.Width) - x, Math.Max(a.Z + a.Height, b.Z + b.Height) - z);
        }
    }

    public enum TerrainOperationKind { AddDelta, SetAbsolute, Flatten, Smooth, HoleCut, ProtectRegion }

    [Flags]
    public enum TerrainProtection { None = 0, Height = 1, Holes = 2, Placement = 4, Vegetation = 8 }

    /// <summary>원본과 외부 패치 배열에서 독립된 불변 저작 명령을 보관한다.</summary>
    public sealed class TerrainOperation
    {
        private readonly float[] _values;
        private readonly float[] _masks;
        public string OperationGuid { get; }
        public TerrainOperationKind Kind { get; }
        public SampleRect Bounds { get; }
        public float Value { get; }
        public int Stage { get; }
        public int Priority { get; }
        public int Sequence { get; }
        public bool Enabled { get; }
        public bool Automatic { get; }
        public int Radius { get; }
        public int Iterations { get; }
        public TerrainProtection Protection { get; }
        public float[] Values => _values == null ? null : (float[])_values.Clone();
        public float[] Masks => _masks == null ? null : (float[])_masks.Clone();

        public TerrainOperation(string operationGuid, TerrainOperationKind kind, SampleRect bounds, float value = 0,
            int stage = 40, int priority = 0, int sequence = 0, bool enabled = true, bool automatic = false,
            int radius = 1, int iterations = 1, TerrainProtection protection = TerrainProtection.Height,
            float[] values = null, float[] masks = null)
        {
            if (string.IsNullOrWhiteSpace(operationGuid)) throw new ArgumentException("명령 ID가 필요합니다.", nameof(operationGuid));
            if (!Enum.IsDefined(typeof(TerrainOperationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (((int)protection & ~15) != 0) throw new ArgumentOutOfRangeException(nameof(protection));
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (bounds.Width < 1 || bounds.Height < 1) throw new ArgumentOutOfRangeException(nameof(bounds));
            if (radius < 1 || radius > 8) throw new ArgumentOutOfRangeException(nameof(radius));
            if (iterations < 1 || iterations > 16) throw new ArgumentOutOfRangeException(nameof(iterations));
            int count = checked(bounds.Width * bounds.Height);
            ValidateArray(values, count, false);
            ValidateArray(masks, count, true);
            OperationGuid = operationGuid; Kind = kind; Bounds = bounds; Value = value;
            Stage = stage; Priority = priority; Sequence = sequence; Enabled = enabled; Automatic = automatic;
            Radius = radius; Iterations = iterations; Protection = protection;
            _values = values == null ? null : (float[])values.Clone();
            _masks = masks == null ? null : (float[])masks.Clone();
        }

        /// <summary>패치 값이 있으면 해당 값을, 없으면 고정 값을 반환한다.</summary>
        public float GetValue(int localIndex)
        {
            ValidateIndex(localIndex);
            return _values == null ? Value : _values[localIndex];
        }

        /// <summary>명시된 마스크가 없으면 전체 가중치로 평가한다.</summary>
        public float GetMask(int localIndex)
        {
            ValidateIndex(localIndex);
            return _masks == null ? 1 : _masks[localIndex];
        }

        private void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)(Bounds.Width * Bounds.Height)) throw new ArgumentOutOfRangeException(nameof(index));
        }

        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static void ValidateArray(float[] array, int count, bool mask)
        {
            if (array == null) return;
            if (array.Length != count) throw new ArgumentException("패치 배열 크기가 영역과 다릅니다.");
            for (int i = 0; i < array.Length; i++)
                if (!IsFinite(array[i]) || (mask && (array[i] < 0 || array[i] > 1)))
                    throw new ArgumentException("패치에 유효하지 않은 수치가 있습니다.");
        }
    }
}
