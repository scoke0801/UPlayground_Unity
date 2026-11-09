using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World
{
    /// <summary>수영과 같은 수면 영역에 수중 미술 프로필을 연결한다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class UnderwaterRegion : MonoBehaviour
    {
        [SerializeField] private FootstepWaterSurface _surface;
        [SerializeField] private UnderwaterProfileSO _profile;
        private static readonly List<UnderwaterRegion> Regions = new();

        private void OnEnable()
        {
            if (_surface != null) _surface.RefreshMesh();
            if (!Regions.Contains(this)) Regions.Add(this);
        }

        private void OnDisable() => Regions.Remove(this);

        /// <summary>수면 위·외부를 제외하고 가장 높은 활성 수면의 프로필을 선택한다.</summary>
        public static bool TryResolve(Vector3 position, out UnderwaterProfileSO profile, out float surfaceY)
        {
            profile = null;
            surfaceY = float.NegativeInfinity;
            for (int i = 0; i < Regions.Count; i++)
            {
                var region = Regions[i];
                if (region == null || !region.isActiveAndEnabled || region._profile == null
                    || region._surface == null || !region._surface.isActiveAndEnabled) continue;
                if (!region._surface.TrySampleHeight(position, out Vector3 water)) continue;
                if (water.y <= position.y || water.y <= surfaceY) continue;
                surfaceY = water.y;
                profile = region._profile;
            }
            return profile != null;
        }
    }
}
