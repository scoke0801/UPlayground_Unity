using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    [Flags]
    public enum WorldDirtyFlags
    {
        None = 0, Height = 1, Holes = 2, Slope = 4, Material = 8, Water = 16, Placement = 32,
        Navigation = 64, Proxy = 128, WaterPlan = 256, Structures = 512, VegetationMaterial = 1024
    }

    /// <summary>직접 변경에서 파생 재생성이 필요한 항목과 높이 읽기 반경을 계산한다.</summary>
    public static class DirtyDependencyGraph
    {
        /// <summary>변경 종류에 따른 파생 의존성의 폐쇄를 반환한다.</summary>
        public static WorldDirtyFlags Expand(WorldDirtyFlags flags)
        {
            if (((int)flags & ~2047) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            if ((flags & WorldDirtyFlags.WaterPlan) != 0) flags |= WorldDirtyFlags.Height | WorldDirtyFlags.Water;
            if ((flags & WorldDirtyFlags.Structures) != 0) flags |= WorldDirtyFlags.Height | WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation;
            if ((flags & WorldDirtyFlags.VegetationMaterial) != 0) flags |= WorldDirtyFlags.Material;
            if ((flags & WorldDirtyFlags.Height) != 0)
                flags |= WorldDirtyFlags.Slope | WorldDirtyFlags.Material | WorldDirtyFlags.Water | WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation | WorldDirtyFlags.Proxy;
            if ((flags & WorldDirtyFlags.Holes) != 0) flags |= WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation | WorldDirtyFlags.Proxy;
            return flags;
        }

        /// <summary>활성 Smooth의 누적 읽기 반경을 보수적으로 합산한다.</summary>
        public static int GetHeightHalo(IReadOnlyList<TerrainOperation> ops)
        {
            if (ops == null) throw new ArgumentNullException(nameof(ops));
            int halo = 0;
            for (int i = 0; i < ops.Count; i++)
            {
                TerrainOperation op = ops[i] ?? throw new ArgumentException("빈 명령은 허용하지 않습니다.", nameof(ops));
                if (op.Enabled && op.Kind == TerrainOperationKind.Smooth) halo = checked(halo + op.Radius * op.Iterations);
            }
            return halo;
        }
    }
}
