using System;
using System.Collections.Generic;

namespace UPlayGround.World.Generation
{
    /// <summary>
    /// 시작점과 도착점을 잇는 측면 격자의 한 노드.
    /// 물리 좌표와 무관한 정수 인덱스만 소유해 순수 계획기에서 결정론적으로 사용할 수 있다.
    /// </summary>
    public readonly struct CycleRouteLatticeNode : IEquatable<CycleRouteLatticeNode>
    {
        public CycleRouteLatticeNode(int longitudinalIndex, int lateralIndex)
        {
            LongitudinalIndex = longitudinalIndex;
            LateralIndex = lateralIndex;
        }

        public int LongitudinalIndex { get; }
        public int LateralIndex { get; }

        public bool Equals(CycleRouteLatticeNode other)
        {
            return LongitudinalIndex == other.LongitudinalIndex &&
                   LateralIndex == other.LateralIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is CycleRouteLatticeNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LongitudinalIndex, LateralIndex);
        }
    }

    /// <summary>
    /// 측면 격자에서 한 단계 전진하는 방향성 간선.
    /// </summary>
    public readonly struct CycleRouteLatticeEdge : IEquatable<CycleRouteLatticeEdge>
    {
        public CycleRouteLatticeEdge(CycleRouteLatticeNode from, CycleRouteLatticeNode to)
        {
            From = from;
            To = to;
        }

        public CycleRouteLatticeNode From { get; }
        public CycleRouteLatticeNode To { get; }

        public bool Equals(CycleRouteLatticeEdge other)
        {
            return From.Equals(other.From) && To.Equals(other.To);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleRouteLatticeEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To);
        }
    }

    /// <summary>
    /// 직선 전진 축과 제한된 측면 축으로 구성된 DAG에서 최소 비용 경로를 찾는다.
    /// 씬과 Physics에는 의존하지 않고 호출자가 제공한 간선 통과 판정만 사용한다.
    /// </summary>
    public static class CycleRouteLatticePlanner
    {
        private const int Unreachable = int.MinValue;
        private const float DiagonalCost = 1.41421356f;
        private const float CenterDistancePenalty = 0.001f;

        public static bool TryBuild(
            int longitudinalSegmentCount,
            int lateralStepCount,
            Func<CycleRouteLatticeEdge, bool> canTraverse,
            out IReadOnlyList<CycleRouteLatticeNode> path,
            out string error)
        {
            path = null;
            error = null;
            if (longitudinalSegmentCount <= 0)
            {
                error = "경로 격자의 전진 구간 수는 1 이상이어야 합니다.";
                return false;
            }

            if (lateralStepCount < 0)
            {
                error = "경로 격자의 측면 단계 수는 0 이상이어야 합니다.";
                return false;
            }

            if (canTraverse == null)
            {
                error = "경로 격자의 간선 통과 판정이 없습니다.";
                return false;
            }

            int width = lateralStepCount * 2 + 1;
            int centerIndex = lateralStepCount;
            int[,] predecessors = new int[longitudinalSegmentCount + 1, width];
            for (int layer = 0; layer <= longitudinalSegmentCount; layer++)
            for (int index = 0; index < width; index++)
                predecessors[layer, index] = Unreachable;

            float[] currentCosts = CreateUnreachableCosts(width);
            currentCosts[centerIndex] = 0f;
            int[] lateralOrder = CreateCenterFirstOrder(lateralStepCount);
            int[] neighborOffsets = { 0, -1, 1 };

            for (int layer = 0; layer < longitudinalSegmentCount; layer++)
            {
                float[] nextCosts = CreateUnreachableCosts(width);
                for (int orderIndex = 0; orderIndex < lateralOrder.Length; orderIndex++)
                {
                    int lateral = lateralOrder[orderIndex];
                    int currentIndex = lateral + centerIndex;
                    float currentCost = currentCosts[currentIndex];
                    if (float.IsPositiveInfinity(currentCost)) continue;

                    for (int neighborIndex = 0; neighborIndex < neighborOffsets.Length; neighborIndex++)
                    {
                        int nextLateral = lateral + neighborOffsets[neighborIndex];
                        if (nextLateral < -lateralStepCount || nextLateral > lateralStepCount) continue;

                        CycleRouteLatticeEdge edge = new(
                            new CycleRouteLatticeNode(layer, lateral),
                            new CycleRouteLatticeNode(layer + 1, nextLateral));
                        if (!canTraverse(edge)) continue;

                        float edgeCost = nextLateral == lateral ? 1f : DiagonalCost;
                        float candidateCost = currentCost + edgeCost +
                                              Math.Abs(nextLateral) * CenterDistancePenalty;
                        int nextIndex = nextLateral + centerIndex;
                        // 같은 비용이면 중앙에서 바깥으로, 음수에서 양수 순인 최초 경로를 유지한다.
                        // 이 순서를 명시해 Physics 콜백 결과가 같을 때 플랫폼별 Dictionary 순서에 의존하지 않는다.
                        if (candidateCost >= nextCosts[nextIndex]) continue;

                        nextCosts[nextIndex] = candidateCost;
                        predecessors[layer + 1, nextIndex] = lateral;
                    }
                }

                currentCosts = nextCosts;
            }

            if (float.IsPositiveInfinity(currentCosts[centerIndex]))
            {
                error = $"측면 {lateralStepCount}단계 안에서 시작점과 도착점을 잇는 경로를 찾지 못했습니다.";
                return false;
            }

            CycleRouteLatticeNode[] result = new CycleRouteLatticeNode[longitudinalSegmentCount + 1];
            int resolvedLateral = 0;
            for (int layer = longitudinalSegmentCount; layer >= 0; layer--)
            {
                result[layer] = new CycleRouteLatticeNode(layer, resolvedLateral);
                if (layer == 0) break;

                int predecessor = predecessors[layer, resolvedLateral + centerIndex];
                if (predecessor == Unreachable)
                {
                    error = $"경로 격자 역추적에 실패했습니다: layer {layer}, lateral {resolvedLateral}";
                    return false;
                }

                resolvedLateral = predecessor;
            }

            path = result;
            return true;
        }

        private static float[] CreateUnreachableCosts(int count)
        {
            float[] result = new float[count];
            for (int i = 0; i < result.Length; i++) result[i] = float.PositiveInfinity;
            return result;
        }

        private static int[] CreateCenterFirstOrder(int lateralStepCount)
        {
            int[] result = new int[lateralStepCount * 2 + 1];
            result[0] = 0;
            int cursor = 1;
            for (int step = 1; step <= lateralStepCount; step++)
            {
                result[cursor++] = -step;
                result[cursor++] = step;
            }

            return result;
        }
    }
}
