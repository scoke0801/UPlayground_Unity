using UnityEngine;
using UPlayGround.Contracts;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>수영 구현 완료로 오인하지 않게 조회/입수 상태만 보여 주는 검증용 probe.</summary>
    public sealed class WorldWaterQueryProbe : MonoBehaviour
    {
        public WorldWaterQuery query;
        public bool hasWater, isSubmerged;
        public string bodyId;
        public float surfaceY, depth;
        public Vector3 flow;
        private void FixedUpdate()
        {
            hasWater = query != null && query.TrySample(transform.position, out _);
            if (!hasWater) { isSubmerged = false; bodyId = null; return; }
            query.TrySample(transform.position, out WaterSample sample);
            bodyId = sample.BodyId; surfaceY = sample.SurfaceY; depth = sample.Depth; flow = sample.Flow; isSubmerged = transform.position.y <= surfaceY;
        }
    }
}
