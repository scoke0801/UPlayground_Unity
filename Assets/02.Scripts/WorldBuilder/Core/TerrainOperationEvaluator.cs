using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>전역 좌표와 고정 순서로 저작 명령을 평가해 타일 순서의 영향을 없앤다.</summary>
    public static class TerrainOperationEvaluator
    {
        /// <summary>불변 원본에서 활성 명령 전체를 정규 순서로 다시 평가한다.</summary>
        public static TerrainBuildResult Build(HeightField source, WorldGrid grid, IReadOnlyList<TerrainOperation> ops)
        {
            TerrainOperation[] ordered = Validate(source, grid, ops);
            SampleRect full = SampleRect.Full(grid.SampleCount);
            Evaluate(source, grid, ordered, full, out float[] heights, out bool[] surfaces);
            ValidateRange(heights, full, full, grid);
            int cells = grid.SampleCount - 1;
            var cellSurfaces = new bool[checked(cells * cells)];
            for (int z = 0; z < cells; z++) Array.Copy(surfaces, z * grid.SampleCount, cellSurfaces, z * cells, cells);
            return new TerrainBuildResult(grid.SampleCount, heights, cellSurfaces);
        }

        /// <summary>변경 영향 범위와 읽기 halo만 원본부터 재평가하고 이전 결과의 해당 범위를 교체한다.</summary>
        public static TerrainBuildResult Rebuild(HeightField source, WorldGrid grid, IReadOnlyList<TerrainOperation> ops,
            TerrainBuildResult previous, SampleRect dirty)
        {
            TerrainOperation[] ordered = Validate(source, grid, ops);
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (previous.SampleCount != grid.SampleCount) throw new ArgumentException("이전 결과 격자가 다릅니다.", nameof(previous));
            ValidateBounds(dirty, grid.SampleCount);
            int halo = DirtyDependencyGraph.GetHeightHalo(ordered);
            SampleRect output = dirty.Expand(halo, grid.SampleCount, grid.SampleCount);
            SampleRect read = output.Expand(halo, grid.SampleCount, grid.SampleCount);
            Evaluate(source, grid, ordered, read, out float[] heights, out bool[] surfaces);
            ValidateRange(heights, read, output, grid);
            float[] mergedHeights = previous.CopyHeights();
            bool[] mergedSurfaces = previous.CopySurfaces();
            for (int z = output.Z; z < output.Z + output.Height; z++)
            for (int x = output.X; x < output.X + output.Width; x++)
                mergedHeights[z * grid.SampleCount + x] = heights[(z - read.Z) * read.Width + x - read.X];
            int cells = grid.SampleCount - 1;
            for (int z = dirty.Z; z < Math.Min(cells, dirty.Z + dirty.Height); z++)
            for (int x = dirty.X; x < Math.Min(cells, dirty.X + dirty.Width); x++)
                mergedSurfaces[z * cells + x] = surfaces[(z - read.Z) * read.Width + x - read.X];
            return new TerrainBuildResult(grid.SampleCount, mergedHeights, mergedSurfaces);
        }

        /// <summary>비활성 명령을 포함한 모든 필드를 정규 순서로 기록해 저작 변경을 식별한다.</summary>
        public static string ComputeStackHash(IReadOnlyList<TerrainOperation> ops)
        {
            TerrainOperation[] ordered = OrderAndValidateIds(ops);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(1); writer.Write(ordered.Length);
                for (int i = 0; i < ordered.Length; i++) WriteOperation(writer, ordered[i]);
                return TerrainSnapshotCodec.ComputeSha256(stream.ToArray());
            }
        }

        private static TerrainOperation[] Validate(HeightField source, WorldGrid grid, IReadOnlyList<TerrainOperation> ops)
        {
            if (source == null || grid == null) throw new ArgumentNullException(nameof(source));
            if (source.Width != grid.SampleCount || source.Height != grid.SampleCount) throw new ArgumentException("원본과 월드 격자가 다릅니다.");
            TerrainOperation[] ordered = OrderAndValidateIds(ops);
            for (int i = 0; i < ordered.Length; i++)
                ValidateBounds(ordered[i].Bounds, ordered[i].Kind == TerrainOperationKind.HoleCut ? grid.SampleCount - 1 : grid.SampleCount);
            return ordered;
        }

        private static void ValidateBounds(SampleRect bounds, int size)
        {
            if (bounds.Width < 1 || bounds.Height < 1 || (long)bounds.X + bounds.Width > size || (long)bounds.Z + bounds.Height > size)
                throw new ArgumentOutOfRangeException(nameof(bounds), "명령 영역이 격자를 벗어납니다.");
        }

        private static TerrainOperation[] OrderAndValidateIds(IReadOnlyList<TerrainOperation> ops)
        {
            if (ops == null) throw new ArgumentNullException(nameof(ops));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var ordered = new TerrainOperation[ops.Count];
            for (int i = 0; i < ops.Count; i++)
            {
                TerrainOperation op = ops[i] ?? throw new ArgumentException("빈 명령은 허용하지 않습니다.", nameof(ops));
                if (!ids.Add(op.OperationGuid)) throw new ArgumentException("명령 ID가 중복되었습니다.", nameof(ops));
                ordered[i] = op;
            }
            Array.Sort(ordered, Compare);
            return ordered;
        }

        private static int Compare(TerrainOperation a, TerrainOperation b)
        {
            int result = a.Stage.CompareTo(b.Stage);
            if (result == 0) result = a.Priority.CompareTo(b.Priority);
            if (result == 0) result = a.Sequence.CompareTo(b.Sequence);
            return result == 0 ? StringComparer.Ordinal.Compare(a.OperationGuid, b.OperationGuid) : result;
        }

        private static void Evaluate(HeightField source, WorldGrid grid, TerrainOperation[] ops, SampleRect read,
            out float[] heights, out bool[] surfaces)
        {
            int count = checked(read.Width * read.Height);
            heights = new float[count];
            surfaces = new bool[count];
            var protection = new TerrainProtection[count];
            for (int z = 0; z < read.Height; z++)
            for (int x = 0; x < read.Width; x++)
            {
                int index = z * read.Width + x;
                heights[index] = (float)grid.SampleToHeight(source[z + read.Z, x + read.X]);
                surfaces[index] = true;
            }
            for (int i = 0; i < ops.Length; i++)
                if (ops[i].Enabled && ops[i].Kind == TerrainOperationKind.ProtectRegion) ApplyProtection(ops[i], read, protection);
            float[] scratch = null;
            for (int i = 0; i < ops.Length; i++)
            {
                TerrainOperation op = ops[i];
                if (!op.Enabled || op.Kind == TerrainOperationKind.ProtectRegion) continue;
                if (op.Kind == TerrainOperationKind.Smooth)
                {
                    if (scratch == null) scratch = new float[count];
                    for (int iteration = 0; iteration < op.Iterations; iteration++)
                    {
                        Array.Copy(heights, scratch, count);
                        Apply(op, read, heights, scratch, surfaces, protection);
                        float[] swap = heights; heights = scratch; scratch = swap;
                    }
                }
                else Apply(op, read, heights, heights, surfaces, protection);
            }
        }

        private static void ApplyProtection(TerrainOperation op, SampleRect read, TerrainProtection[] protection)
        {
            int endZ = Math.Min(read.Z + read.Height, op.Bounds.Z + op.Bounds.Height);
            int endX = Math.Min(read.X + read.Width, op.Bounds.X + op.Bounds.Width);
            for (int z = Math.Max(read.Z, op.Bounds.Z); z < endZ; z++)
            for (int x = Math.Max(read.X, op.Bounds.X); x < endX; x++)
                if (op.GetMask((z - op.Bounds.Z) * op.Bounds.Width + x - op.Bounds.X) > .5f)
                    protection[(z - read.Z) * read.Width + x - read.X] |= op.Protection;
        }

        private static void Apply(TerrainOperation op, SampleRect read, float[] input, float[] output,
            bool[] surfaces, TerrainProtection[] protection)
        {
            int endZ = Math.Min(read.Z + read.Height, op.Bounds.Z + op.Bounds.Height);
            int endX = Math.Min(read.X + read.Width, op.Bounds.X + op.Bounds.Width);
            TerrainProtection blocked = op.Kind == TerrainOperationKind.HoleCut ? TerrainProtection.Holes : TerrainProtection.Height;
            for (int z = Math.Max(read.Z, op.Bounds.Z); z < endZ; z++)
            for (int x = Math.Max(read.X, op.Bounds.X); x < endX; x++)
            {
                int index = (z - read.Z) * read.Width + x - read.X;
                if (op.Automatic && (protection[index] & blocked) != 0) continue;
                int local = (z - op.Bounds.Z) * op.Bounds.Width + x - op.Bounds.X;
                float mask = op.GetMask(local);
                if (mask == 0) continue;
                if (op.Kind == TerrainOperationKind.HoleCut) { if (mask > .5f) surfaces[index] = false; continue; }
                if (op.Kind == TerrainOperationKind.AddDelta) output[index] = input[index] + op.GetValue(local) * mask;
                else
                {
                    float target = op.Kind == TerrainOperationKind.Smooth ? Average(input, read, x, z, op.Radius) : op.GetValue(local);
                    output[index] = mask == 1 ? target : input[index] + (target - input[index]) * mask;
                }
            }
        }

        private static float Average(float[] input, SampleRect read, int x, int z, int radius)
        {
            double total = 0;
            int count = 0;
            for (int sampleZ = Math.Max(read.Z, z - radius); sampleZ <= Math.Min(read.Z + read.Height - 1, z + radius); sampleZ++)
            for (int sampleX = Math.Max(read.X, x - radius); sampleX <= Math.Min(read.X + read.Width - 1, x + radius); sampleX++)
            {
                total += input[(sampleZ - read.Z) * read.Width + sampleX - read.X];
                count++;
            }
            return (float)(total / count);
        }

        private static void ValidateRange(float[] heights, SampleRect read, SampleRect output, WorldGrid grid)
        {
            // 미터 float 반올림으로 원본 끝값이 경계를 넘는 것은 허용하되 클램프하지 않는다.
            float min = (float)grid.OriginY, max = (float)(grid.OriginY + grid.VerticalRange);
            for (int z = output.Z; z < output.Z + output.Height; z++)
            for (int x = output.X; x < output.X + output.Width; x++)
            {
                float value = heights[(z - read.Z) * read.Width + x - read.X];
                if (!TerrainOperation.IsFinite(value) || value < min || value > max)
                    throw new ArgumentOutOfRangeException(nameof(heights), "최종 높이가 월드 고도 범위를 벗어납니다.");
            }
        }

        private static void WriteOperation(BinaryWriter writer, TerrainOperation op)
        {
            writer.Write(op.OperationGuid); writer.Write((int)op.Kind);
            writer.Write(op.Bounds.X); writer.Write(op.Bounds.Z); writer.Write(op.Bounds.Width); writer.Write(op.Bounds.Height);
            writer.Write(op.Value); writer.Write(op.Stage); writer.Write(op.Priority); writer.Write(op.Sequence);
            writer.Write(op.Enabled); writer.Write(op.Automatic); writer.Write(op.Radius); writer.Write(op.Iterations); writer.Write((int)op.Protection);
            WriteArray(writer, op.Values); WriteArray(writer, op.Masks);
        }

        private static void WriteArray(BinaryWriter writer, float[] values)
        {
            writer.Write(values == null ? -1 : values.Length);
            if (values == null) return;
            for (int i = 0; i < values.Length; i++) writer.Write(values[i]);
        }
    }
}
