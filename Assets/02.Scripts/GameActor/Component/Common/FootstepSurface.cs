using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>콜라이더와 명시적 바닥 프로필을 등록하며 Terrain은 베이크한 재질 지도를 사용한다.</summary>
    public sealed class FootstepSurface : MonoBehaviour
    {
        public Collider[] colliders;
        public FootstepSurfaceProfileSO profile;
        public Terrain terrain;
        public FootstepTerrainMapSO terrainMap;
        private static readonly Dictionary<Collider, FootstepSurface> Surfaces = new();

        private void OnEnable()
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) Surfaces[colliders[i]] = this;
        }

        private void OnDisable()
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null && Surfaces.TryGetValue(colliders[i], out var owner) && owner == this)
                    Surfaces.Remove(colliders[i]);
        }

        /// <summary>접촉한 콜라이더의 명시적 재질을 조회한다.</summary>
        public static FootstepSurfaceProfileSO Resolve(Collider collider, Vector3 point)
        {
            if (collider == null || !Surfaces.TryGetValue(collider, out var surface)) return null;
            if (surface.terrain == null || surface.terrainMap == null) return surface.profile;
            Vector3 local = surface.terrain.transform.InverseTransformPoint(point);
            Vector3 size = surface.terrain.terrainData.size;
            return surface.terrainMap.Resolve(new Vector2(local.x / size.x, local.z / size.z)) ?? surface.profile;
        }
    }
}
