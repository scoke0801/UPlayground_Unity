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
        private Renderer _renderer;
        private FootstepWaterSurface _boundSurface;
        private WaterPrismGeometry _prisms;
        private int _prismRevision = -1;
        private MaterialPropertyBlock _marker;
        private static readonly int ManagedId = Shader.PropertyToID("_WaterManaged");

        public FootstepWaterSurface Surface => _surface;
        public UnderwaterProfileSO Profile => _profile;
        public Renderer SurfaceRenderer => _renderer;
        public static IReadOnlyList<UnderwaterRegion> ActiveRegions => Regions;
        public int PrismTriangleCount => GetPrisms()?.TriangleCount ?? 0;
        internal WaterPrismGeometry GetPrisms()
        {
            if (_surface == null || _surface.surfaceMesh == null) return null;
            if (_prisms == null || _prismRevision != _surface.MeshRevision)
            {
                _prisms ??= new WaterPrismGeometry();
                _prisms.Rebuild(_surface.queryMesh != null ? _surface.queryMesh : _surface.surfaceMesh.sharedMesh);
                _prismRevision = _surface.MeshRevision;
            }
            return _prisms;
        }
        [Tooltip("지붕·동굴 차폐를 베이크한 선형 노출 마스크. 없으면 비 파문을 끈다.")]
        public Texture2D rainExposure;
        [Range(0f, 1f)] public float rainIntensity;
        [SerializeField] private LivingWorld.EnvironmentController _weather;
        public float RainIntensity => _weather != null && _weather.IsReady ? _weather.RainIntensity : rainIntensity;

        /// <summary>기존 날씨 생산자의 강수·지붕 계약을 물에 연결한다.</summary>
        public void BindWeather(LivingWorld.EnvironmentController weather) => _weather = weather;
        public bool IsRainSheltered(Vector3 position) => _weather != null && _weather.IsRainSheltered(position, transform);

        /// <summary>저작 도구에서 기존 수면과 프로필을 연결하고 렌더러 캐시를 갱신한다.</summary>
        public void Bind(FootstepWaterSurface surface, UnderwaterProfileSO profile)
        {
            ReleaseRenderer();
            _surface = surface;
            _profile = profile;
            RefreshBinding();
        }

        /// <summary>연결 변경 때만 렌더러를 찾고 원본 머티리얼 대신 인스턴스 표식을 사용한다.</summary>
        public void RefreshBinding()
        {
            if (_boundSurface == _surface && _renderer != null && _surface != null && _surface.surfaceMesh != null
                && _renderer.gameObject == _surface.surfaceMesh.gameObject) return;
            ReleaseRenderer();
            _boundSurface = _surface;
            _prismRevision = -1;
            if (_surface == null || _surface.surfaceMesh == null) return;
            _renderer = _surface.surfaceMesh.GetComponent<Renderer>();
            if (_renderer == null) return;
            _marker ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_marker);
            _marker.SetFloat(ManagedId, 1f);
            _renderer.SetPropertyBlock(_marker);
        }

        private void ReleaseRenderer()
        {
            if (_renderer == null) return;
            _marker ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_marker);
            _marker.SetFloat(ManagedId, 0f);
            _renderer.SetPropertyBlock(_marker);
            _renderer = null;
            _boundSurface = null;
        }

        private void OnEnable()
        {
            if (_surface != null) _surface.RefreshMesh();
            RefreshBinding();
            if (!Regions.Contains(this)) Regions.Add(this);
        }

        private void OnDisable()
        {
            Regions.Remove(this);
            ReleaseRenderer();
        }

        /// <summary>수영이 선택한 수역의 프로필만 반환해 다른 높이의 수중 효과가 새지 않게 한다.</summary>
        public static bool TryResolve(Vector3 position, out UnderwaterProfileSO profile, out float surfaceY)
        {
            profile = null;
            surfaceY = float.NegativeInfinity;
            if (!FootstepWaterSurface.TryResolveSurface(position, out var surface, out Vector3 water)
                || water.y <= position.y) return false;
            for (int i = 0; i < Regions.Count; i++)
            {
                var region = Regions[i];
                if (region == null || !region.isActiveAndEnabled || region._profile == null
                    || region._surface != surface) continue;
                surfaceY = water.y;
                profile = region._profile;
                return true;
            }
            return profile != null;
        }
    }
}
