using TMPro;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>게임 시간 배율과 무관한 실제 프레임 속도를 표시하는 진단용 텍스트.</summary>
    public sealed class UIFrameRateDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField, Min(0.1f)] private float _refreshInterval = 0.5f;
        private double _sampleStartedAt;
        private int _frameCount;

        private void OnEnable()
        {
            _sampleStartedAt = Time.realtimeSinceStartupAsDouble;
            _frameCount = 0;
            if (_text != null) _text.SetText("-- FPS | -- ms");
        }

        private void Update()
        {
            if (_text == null) return;
            _frameCount++;
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = now - _sampleStartedAt;
            if (elapsed < Mathf.Max(0.1f, _refreshInterval)) return;

            // 진단값에는 트윈을 적용하지 않아 실제 프레임 저하를 가리지 않는다.
            _text.SetText("{0:0} FPS | {1:0.0} ms", (float)(_frameCount / elapsed),
                (float)(elapsed * 1000d / _frameCount));
            _sampleStartedAt = now;
            _frameCount = 0;
        }
    }
}
