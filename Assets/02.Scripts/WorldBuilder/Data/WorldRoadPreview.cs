using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>편집한 Spline의 미캡처 변경과 원래 도로 메타데이터를 구분한다.</summary>
    public sealed class WorldRoadPreview : MonoBehaviour
    {
        public string roadGuid;
        public RoadKnot[] appliedKnots;
        public string meshHash;
        public int chunkCount;
    }
}
