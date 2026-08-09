using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UPlayGround.World.Generation.Tests
{
    public sealed class CycleRouteLatticePlannerTests
    {
        [Test]
        public void OpenCorridor_UsesCenterLine()
        {
            Assert.That(
                CycleRouteLatticePlanner.TryBuild(6, 3, _ => true, out IReadOnlyList<CycleRouteLatticeNode> path, out string error),
                Is.True,
                error);

            Assert.That(path.Count, Is.EqualTo(7));
            Assert.That(path.All(node => node.LateralIndex == 0), Is.True);
        }

        [Test]
        public void BlockedCenter_UsesDeterministicLateralDetour()
        {
            static bool CanTraverse(CycleRouteLatticeEdge edge)
            {
                return edge.To.LongitudinalIndex != 2 || edge.To.LateralIndex != 0;
            }

            Assert.That(
                CycleRouteLatticePlanner.TryBuild(5, 2, CanTraverse, out IReadOnlyList<CycleRouteLatticeNode> first, out string firstError),
                Is.True,
                firstError);
            Assert.That(
                CycleRouteLatticePlanner.TryBuild(5, 2, CanTraverse, out IReadOnlyList<CycleRouteLatticeNode> second, out string secondError),
                Is.True,
                secondError);

            Assert.That(first[0].LateralIndex, Is.Zero);
            Assert.That(first[^1].LateralIndex, Is.Zero);
            Assert.That(first[2].LateralIndex, Is.EqualTo(-1));
            Assert.That(
                second.Select(node => $"{node.LongitudinalIndex}:{node.LateralIndex}"),
                Is.EqualTo(first.Select(node => $"{node.LongitudinalIndex}:{node.LateralIndex}")));
        }

        [Test]
        public void FullyBlockedLayer_ReturnsFailure()
        {
            bool CanTraverse(CycleRouteLatticeEdge edge) => edge.To.LongitudinalIndex != 3;

            Assert.That(
                CycleRouteLatticePlanner.TryBuild(6, 2, CanTraverse, out IReadOnlyList<CycleRouteLatticeNode> path, out string error),
                Is.False);
            Assert.That(path, Is.Null);
            Assert.That(error, Does.Contain("경로를 찾지 못했습니다"));
        }
    }
}
