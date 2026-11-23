using UnityEngine;
using UnityEngine.AI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>
    /// NavMesh 경로로 안내 경로를 만든다. 완전한 경로(PathComplete)만 인정하고,
    /// 부분 경로·시작/목표를 NavMesh에 올리지 못한 경우는 길이 없는 것으로 본다.
    /// 부분 경로의 끝까지만 보여 주면 막다른 길로 오인하게 되기 때문이다.
    /// </summary>
    public sealed class NavMeshGuidePathProvider : IGuidePathProvider
    {
        private readonly NavMeshPath _navMeshPath = new();
        private readonly float _sampleDistance;
        private readonly int _subdivisions;
        private readonly int _areaMask;

        public NavMeshGuidePathProvider(float sampleDistance, int smoothSubdivisions, int areaMask = NavMesh.AllAreas)
        {
            _sampleDistance = Mathf.Max(0.1f, sampleDistance);
            _subdivisions = Mathf.Max(1, smoothSubdivisions);
            _areaMask = areaMask;
        }

        public bool TryBuild(Vector3 from, Vector3 to, GuidePath result)
        {
            result.Clear();

            if (!NavMesh.SamplePosition(from, out NavMeshHit startHit, _sampleDistance, _areaMask)
                || !NavMesh.SamplePosition(to, out NavMeshHit endHit, _sampleDistance, _areaMask))
                return false;

            if (!NavMesh.CalculatePath(startHit.position, endHit.position, _areaMask, _navMeshPath)
                || _navMeshPath.status != NavMeshPathStatus.PathComplete)
                return false;

            int cornerCount = _navMeshPath.GetCornersNonAlloc(s_corners);
            Vector3[] corners = s_corners;
            // 버퍼가 가득 찼다면 모서리가 잘렸을 수 있다. 목적지 앞에서 끊긴 길을 완성 경로로 오인하지 않도록 전체를 다시 받는다.
            if (cornerCount >= s_corners.Length)
            {
                corners = _navMeshPath.corners;
                cornerCount = corners.Length;
            }

            if (cornerCount < 2)
                return false;

            AppendSmoothedCorners(corners, cornerCount, result);
            result.Seal();
            return result.IsComplete;
        }

        // GetCornersNonAlloc는 호출 스레드(메인) 전용이므로 정적 버퍼를 공유해도 안전하다.
        private static readonly Vector3[] s_corners = new Vector3[256];

        // 모서리를 Catmull-Rom으로 이어 직각으로 꺾이는 길을 부드러운 곡선으로 만든다.
        private void AppendSmoothedCorners(Vector3[] corners, int cornerCount, GuidePath result)
        {
            result.AddPoint(corners[0]);
            for (int i = 0; i < cornerCount - 1; i++)
            {
                Vector3 p0 = corners[Mathf.Max(i - 1, 0)];
                Vector3 p1 = corners[i];
                Vector3 p2 = corners[i + 1];
                Vector3 p3 = corners[Mathf.Min(i + 2, cornerCount - 1)];

                for (int step = 1; step <= _subdivisions; step++)
                    result.AddPoint(CatmullRom(p0, p1, p2, p3, step / (float)_subdivisions));
            }
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1)
                           + (-p0 + p2) * t
                           + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                           + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
