using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 화면 가장자리에 표시되는 적 방향 마커 1개. UI_HUD_OffscreenThreatIndicator가 풀링해서 사용한다.
    ///
    /// 프리팹 구성 권장:
    ///   - 루트: RectTransform (anchor/pivot = 중앙 0.5,0.5)
    ///   - 자식 Image: 방향 화살표. 스프라이트가 향하는 기본 방향은 Config.markerForwardAngleOffset로 보정
    ///     (오른쪽 +X 향함이면 0, 위 +Y 향함이면 -90).
    /// </summary>
    public class UIOffscreenThreatMarker : MonoBehaviour
    {
        [Tooltip("방향 화살표 이미지. 스프라이트 기본 향함은 Config.markerForwardAngleOffset로 보정한다(+X 향함이면 0).")]
        [SerializeField] private Image _arrow;

        private RectTransform _rect;
        private float _baseScale = 1f;
        private bool _pulsing;
        private float _pulseSpeed;
        private float _pulseAmount;
        private float _pulseTimer;

        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        /// <summary>
        /// 마커 1개의 위치/회전/색/등급을 한 번에 적용한다.
        /// </summary>
        /// <param name="anchoredPos">마커 컨테이너 기준 로컬 좌표(중앙 원점).</param>
        /// <param name="angleDeg">화살표 회전각(Z, 도). 화살표가 적 방향을 가리키도록 호출측에서 보정한 값.</param>
        /// <param name="color">등급별 색상.</param>
        /// <param name="scale">등급별 크기 배율.</param>
        /// <param name="pulse">공격 임박 여부. true면 펄스 애니메이션을 적용한다.</param>
        public void Apply(Vector2 anchoredPos, float angleDeg, Color color, float scale, bool pulse,
            float pulseSpeed, float pulseAmount)
        {
            Rect.anchoredPosition = anchoredPos;

            if (_arrow != null)
            {
                _arrow.color = color;
                _arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            }

            _baseScale   = scale;
            _pulsing     = pulse && pulseAmount > 0f && pulseSpeed > 0f;
            _pulseSpeed  = pulseSpeed;
            _pulseAmount = pulseAmount;

            if (!_pulsing)
                Rect.localScale = Vector3.one * _baseScale;
        }

        private void Update()
        {
            if (!_pulsing)
                return;

            // 히트스톱 중에도 동작하도록 unscaled 사용
            _pulseTimer += Time.unscaledDeltaTime * _pulseSpeed;
            float s = _baseScale * (1f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f) * _pulseAmount * 0.5f);
            Rect.localScale = Vector3.one * s;
        }

        public void SetActiveMarker(bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
    }
}
