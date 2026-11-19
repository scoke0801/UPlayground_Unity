using UnityEngine;

namespace UPlayGround.Contracts
{
    /// <summary>시각 파동과 분리한 물리 수면 조회. 성공은 XZ 수역·바닥 이상·조회 상한·제외 체적 검사를 모두 통과한 뜻이다.</summary>
    public interface IWaterQuery
    {
        bool TrySample(Vector3 position, out WaterSample sample);
        /// <summary>수직 광선은 시각 메시 대신 동일한 물리 함수의 표면을 교차 검사한다.</summary>
        bool TryRaycastDown(Vector3 origin, float distance, out WaterSample sample);
    }
    /// <summary>Depth는 수면에서 실제 지형 바닥까지의 양수 거리이며 발의 잠김 깊이와 구분한다.</summary>
    public struct WaterSample
    {
        public string BodyId;
        public float SurfaceY, Depth;
        public Vector3 Flow;
        public int Priority;
        public double SpecificityArea;
        public static bool Precedes(WaterSample a, WaterSample b)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            if (a.SpecificityArea != b.SpecificityArea) return a.SpecificityArea < b.SpecificityArea;
            return string.CompareOrdinal(a.BodyId, b.BodyId) < 0;
        }
    }
}
