using UnityEngine;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    public sealed class WorldCaveModule : MonoBehaviour
    {
        public string caveGuid, moduleId, surfaceId;
        public Collider[] floorColliders;

        public bool TryGetFloor(Vector2 xz, out Vector3 floor)
        {
            floor = default;
            float nearest = float.PositiveInfinity;
            var ray = new Ray(new Vector3(xz.x, transform.position.y + 1000, xz.y), Vector3.down);
            foreach (var candidate in floorColliders)
            {
                if (candidate == null || !candidate.Raycast(ray, out var hit, 2000) || hit.distance >= nearest || hit.normal.y <= 0) continue;
                nearest = hit.distance;
                floor = hit.point;
            }
            return float.IsFinite(nearest);
        }
    }
}

