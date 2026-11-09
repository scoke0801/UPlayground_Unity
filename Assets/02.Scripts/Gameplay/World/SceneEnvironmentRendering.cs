using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UPlayGround.Gameplay.World
{
    /// <summary>활성 월드 씬의 렌더 파이프라인과 하늘을 적용하고 소유권 해제 시 복원한다.</summary>
    [DisallowMultipleComponent]
    public sealed partial class SceneEnvironmentRendering : MonoBehaviour
    {
        [SerializeField] private RenderPipelineAsset _renderPipeline;
        [SerializeField] private Material _skySource;
        [SerializeField, Range(0f, 5f)] private float _atmosphereThickness = 1f;
        [Tooltip("하늘의 태양 방향. 비우면 씬의 RenderSettings.sun을 사용한다.")]
        [SerializeField] private Light _sunLight;

        private static SceneEnvironmentRendering _owner;
        private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int SunDirectionId = Shader.PropertyToID("_SunDirection");
        private static readonly int SunVisibilityId = Shader.PropertyToID("_SunVisibility");
        [NonSerialized] private RenderPipelineAsset _previousPipeline;
        [NonSerialized] private Material _previousSky;
        [NonSerialized] private Material _runtimeSky;
        [NonSerialized] private bool _hasOwnership;
        [NonSerialized] private Light _boundSun;
        [NonSerialized] private bool _hasSunParameters;

        /// <summary>이 씬이 명시한 렌더 설정을 현재 소유하는지 반환한다.</summary>
        public bool HasRenderingOwnership => _hasOwnership;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            RefreshRendering();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            RestoreRendering();
        }

        private void OnActiveSceneChanged(Scene previous, Scene current) => RefreshRendering();

        /// <summary>활성 씬에서만 직렬화된 환경 설정을 런타임에 적용한다.</summary>
        public void RefreshRendering()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || gameObject.scene != SceneManager.GetActiveScene())
            {
                RestoreRendering();
                return;
            }

            if (_hasOwnership || (_renderPipeline == null && _skySource == null))
                return;

            if (_owner != null)
                _owner.RestoreRendering();

            _previousPipeline = QualitySettings.renderPipeline;
            _previousSky = RenderSettings.skybox;
            _owner = this;
            _hasOwnership = true;
            try
            {
                if (_renderPipeline != null)
                    QualitySettings.renderPipeline = _renderPipeline;
                if (_skySource == null)
                    return;

                // 공유 재질과 씬 직렬화를 바꾸지 않고 이 월드의 하늘에만 적용한다.
                _runtimeSky = new Material(_skySource) { name = _skySource.name + " (Scene Runtime)", hideFlags = HideFlags.HideAndDontSave };
                if (_runtimeSky.HasProperty(AtmosphereThicknessId))
                    _runtimeSky.SetFloat(AtmosphereThicknessId, _atmosphereThickness);
                _boundSun = _sunLight != null ? _sunLight : RenderSettings.sun;
                _hasSunParameters = _runtimeSky.HasProperty(SunDirectionId) && _runtimeSky.HasProperty(SunVisibilityId);
                RenderSettings.skybox = _runtimeSky;
                ApplySunDirection();
                InitializeRegionalSky();
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            }
            catch
            {
                RestoreRendering();
                throw;
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_owner == this && RenderSettings.skybox == _runtimeSky)
            {
                ApplySunDirection();
                ApplyRegionalSky();
            }
        }

        private void ApplySunDirection()
        {
            if (!_hasSunParameters || _runtimeSky == null)
                return;

            if (_boundSun == null)
            {
                _runtimeSky.SetVector(SunDirectionId, new Vector4(0f, 1f, 0f, 0f));
                _runtimeSky.SetFloat(SunVisibilityId, 0f);
                return;
            }

            Vector3 direction = -_boundSun.transform.forward;
            _runtimeSky.SetVector(SunDirectionId, new Vector4(direction.x, direction.y, direction.z, 1f));
            _runtimeSky.SetFloat(SunVisibilityId, _boundSun.isActiveAndEnabled && _boundSun.intensity > 0f ? 1f : 0f);
        }

        /// <summary>이 컴포넌트가 소유한 전역 설정만 복원하고 런타임 하늘을 해제한다.</summary>
        public void RestoreRendering()
        {
            if (!_hasOwnership)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (_owner == this)
            {
                RestoreRegionalSky();
                if (_renderPipeline != null && QualitySettings.renderPipeline == _renderPipeline)
                    QualitySettings.renderPipeline = _previousPipeline;
                // 씬 전환으로 이미 새 RenderSettings가 설치됐으면 이전 하늘을 덮지 않는다.
                if (_runtimeSky != null && RenderSettings.skybox == _runtimeSky)
                    RenderSettings.skybox = _previousSky;
                _owner = null;
            }

            if (_runtimeSky != null)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeSky);
                else
                    DestroyImmediate(_runtimeSky);
            }
            _runtimeSky = null;
            _previousSky = null;
            _previousPipeline = null;
            _boundSun = null;
            _hasSunParameters = false;
            _hasOwnership = false;
        }
    }
}
