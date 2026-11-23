using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.UI.Guidance
{
    /// <summary>길 계산 결과. 안내할 길을 확인하지 못했으면 None이다.</summary>
    public enum GuidePathStatus
    {
        None = 0,
        Complete = 1,
    }

    /// <summary>
    /// 길안내 연출이 따라갈 월드 좌표 폴리라인. 점 버퍼는 재사용하므로 재계산해도 할당이 생기지 않는다.
    /// 연출은 이 클래스만 보고 길을 읽고, 길이 어디서 왔는지(NavMesh 등)는 알지 못한다.
    /// </summary>
    public sealed class GuidePath
    {
        private readonly List<Vector3> _points = new(64);
        private readonly List<float> _cumulativeDistances = new(64);

        public GuidePathStatus Status { get; private set; }
        public float TotalLength { get; private set; }
        public int PointCount => _points.Count;
        public bool IsComplete => Status == GuidePathStatus.Complete && _points.Count >= 2;
        public Vector3 StartPoint => _points.Count > 0 ? _points[0] : Vector3.zero;
        public Vector3 EndPoint => _points.Count > 0 ? _points[_points.Count - 1] : Vector3.zero;

        /// <summary>점·길이·상태를 모두 비운다.</summary>
        public void Clear()
        {
            _points.Clear();
            _cumulativeDistances.Clear();
            TotalLength = 0f;
            Status = GuidePathStatus.None;
        }

        /// <summary>점을 뒤에 붙인다. 직전 점과 같은 위치면 무시한다.</summary>
        public void AddPoint(Vector3 point)
        {
            if (_points.Count == 0)
            {
                _points.Add(point);
                _cumulativeDistances.Add(0f);
                return;
            }

            float segment = Vector3.Distance(_points[_points.Count - 1], point);
            if (segment < 0.001f)
                return;

            TotalLength += segment;
            _points.Add(point);
            _cumulativeDistances.Add(TotalLength);
        }

        /// <summary>점을 모두 붙인 뒤 호출한다. 점이 둘 이상이어야 Complete가 된다.</summary>
        public void Seal()
        {
            Status = _points.Count >= 2 ? GuidePathStatus.Complete : GuidePathStatus.None;
        }

        /// <summary>시작점에서 distance만큼 간 위치와 진행 방향을 돌려준다. 범위 밖은 양 끝으로 고정한다.</summary>
        public void Sample(float distance, out Vector3 position, out Vector3 tangent)
        {
            if (_points.Count < 2)
            {
                position = StartPoint;
                tangent = Vector3.forward;
                return;
            }

            distance = Mathf.Clamp(distance, 0f, TotalLength);
            int index = FindSegmentIndex(distance);
            float segmentStart = _cumulativeDistances[index];
            float segmentLength = _cumulativeDistances[index + 1] - segmentStart;
            float t = segmentLength > 0.0001f ? (distance - segmentStart) / segmentLength : 0f;

            Vector3 from = _points[index];
            Vector3 to = _points[index + 1];
            position = Vector3.Lerp(from, to, t);
            tangent = (to - from).normalized;
        }

        /// <summary>
        /// 월드 위치에서 가장 가까운 경로 위 지점의 시작점 기준 거리와 수평 이탈 거리를 돌려준다.
        /// searchFromDistance 이후 구간만 훑어 되돌아가는 지름길 구간에 끌리지 않게 한다.
        /// </summary>
        public float ProjectDistance(Vector3 worldPosition, float searchFromDistance, out float planarDeviation)
        {
            planarDeviation = float.MaxValue;
            if (_points.Count < 2)
                return 0f;

            int startIndex = FindSegmentIndex(Mathf.Clamp(searchFromDistance, 0f, TotalLength));
            float bestDistance = Mathf.Clamp(searchFromDistance, 0f, TotalLength);
            float bestSqr = float.MaxValue;

            for (int i = startIndex; i < _points.Count - 1; i++)
            {
                Vector3 a = _points[i];
                Vector3 b = _points[i + 1];
                Vector3 ab = b - a;
                ab.y = 0f;
                Vector3 ap = worldPosition - a;
                ap.y = 0f;

                float lengthSqr = ab.sqrMagnitude;
                float t = lengthSqr > 0.0001f ? Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSqr) : 0f;
                Vector3 closest = a + ab * t;
                closest.y = 0f;
                Vector3 flatPosition = worldPosition;
                flatPosition.y = 0f;

                float sqr = (flatPosition - closest).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestDistance = _cumulativeDistances[i] + (_cumulativeDistances[i + 1] - _cumulativeDistances[i]) * t;
                }
            }

            planarDeviation = Mathf.Sqrt(bestSqr);
            // 시작 구간의 앞부분에 투영돼도 이미 지나온 거리보다 뒤로 가지 않는다.
            return Mathf.Max(bestDistance, searchFromDistance);
        }

        // 누적 거리가 단조 증가하므로 이분 탐색으로 구간을 찾는다. 반환값은 항상 [0, 점 수-2].
        private int FindSegmentIndex(float distance)
        {
            int low = 0;
            int high = _points.Count - 2;
            while (low < high)
            {
                int mid = (low + high + 1) >> 1;
                if (_cumulativeDistances[mid] <= distance)
                    low = mid;
                else
                    high = mid - 1;
            }

            return low;
        }
    }
}
