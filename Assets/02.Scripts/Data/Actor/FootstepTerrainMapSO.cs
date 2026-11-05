using System;
using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>Terrain의 우세 레이어를 미리 구워 런타임 알파맵 할당을 피한다.</summary>
    public sealed class FootstepTerrainMapSO : ScriptableObject
    {
        public int width;
        public int height;
        public FootstepSurfaceProfileSO[] surfaces = Array.Empty<FootstepSurfaceProfileSO>();
        public byte[] dominantLayers = Array.Empty<byte>();

        /// <summary>정규화된 지형 좌표의 발 디딤 재질을 조회한다.</summary>
        public FootstepSurfaceProfileSO Resolve(Vector2 coordinate)
        {
            if (width <= 0 || height <= 0 || dominantLayers == null
                || dominantLayers.Length != width * height || surfaces == null)
                return null;
            int x = Mathf.Clamp(Mathf.FloorToInt(coordinate.x * width), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(coordinate.y * height), 0, height - 1);
            int layer = dominantLayers[y * width + x];
            return layer < surfaces.Length ? surfaces[layer] : null;
        }
    }
}
