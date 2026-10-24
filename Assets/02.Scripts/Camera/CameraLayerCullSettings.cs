using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>작은 배경의 표시 거리를 카메라별 네이티브 컬링에 맡긴다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraLayerCullSettings : MonoBehaviour
    {
        // GRD는 레이어별 거리를 무시하므로 대상 배경에는 DisallowGPUDrivenRendering이 필요하다.
        [SerializeField] private LayerMask _layers;
        [SerializeField, Min(1f)] private float _distance = 160f;
        private UnityEngine.Camera _camera;
        private float[] _previousDistances;
        private float[] _appliedDistances;
        private bool _previousSpherical;
        private int _appliedMask;

        private void OnEnable() => Apply();
        private void OnDisable() => Restore();

        /// <summary>선택한 레이어만 제한하고 나머지 카메라 설정은 유지한다.</summary>
        public void Apply()
        {
            Restore();
            if (_layers.value == 0) return;
            if (_camera == null) _camera = GetComponent<UnityEngine.Camera>();
            _previousDistances = _camera.layerCullDistances;
            _previousSpherical = _camera.layerCullSpherical;
            _appliedDistances = (float[])_previousDistances.Clone();
            _appliedMask = _layers.value;
            for (int layer = 0; layer < _appliedDistances.Length; layer++)
            {
                if ((_appliedMask & (1 << layer)) != 0)
                    _appliedDistances[layer] = Mathf.Max(1f, _distance);
            }
            _camera.layerCullDistances = _appliedDistances;
            // 회전만으로 같은 풀이 나타났다 사라지지 않도록 구면 거리를 사용한다.
            _camera.layerCullSpherical = true;
        }

        /// <summary>비활성화 시 이 컴포넌트가 적용한 거리만 원래 값으로 돌린다.</summary>
        public void Restore()
        {
            if (_camera != null && _previousDistances != null)
            {
                float[] current = _camera.layerCullDistances;
                for (int layer = 0; layer < current.Length; layer++)
                {
                    if ((_appliedMask & (1 << layer)) != 0 &&
                        Mathf.Approximately(current[layer], _appliedDistances[layer]))
                        current[layer] = _previousDistances[layer];
                }
                _camera.layerCullDistances = current;
                _camera.layerCullSpherical = _previousSpherical;
            }
            _previousDistances = null;
            _appliedDistances = null;
            _appliedMask = 0;
        }
    }
}
