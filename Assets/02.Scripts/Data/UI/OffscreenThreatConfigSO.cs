using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 오프스크린 적 공격 인디케이터 설정.
    /// 설계 문서: Assets/docs/TODO/OFFSCREEN_THREAT_INDICATOR_DESIGN.md
    /// 거리/링 반경/색상/펄스 등 모든 튜닝 수치를 외부화한다.
    /// </summary>
    [CreateAssetMenu(fileName = "OffscreenThreatConfig", menuName = "UPlayGround/UI/Offscreen Threat Config")]
    public class OffscreenThreatConfigSO : ScriptableObject
    {
        [Header("표시 범위")]
        [Tooltip("이 거리(m)를 초과한 적은 인디케이터를 표시하지 않는다. 0 이하면 거리 제한 없음.")]
        public float maxDistance = 40f;

        [Tooltip("화면 중앙 기준 가상 원형 테두리의 반경(마커 컨테이너 로컬 단위). 마커는 이 원 위에 배치된다.")]
        public float ringRadius = 300f;

        [Tooltip("마커 화살표 스프라이트의 기본 향하는 방향 보정각(도). 스프라이트가 오른쪽(+X)을 향하면 0, 위(+Y)를 향하면 -90.")]
        public float markerForwardAngleOffset = 0f;

        [Header("등급: 일반 공격")]
        [Tooltip("적이 일반 Attack 상태일 때 마커 색상")]
        public Color attackImminentColor = new Color(1f, 0.9f, 0.1f, 1f);

        [Tooltip("일반 공격 마커 크기 배율")]
        public float attackImminentScale = 1.2f;

        [Header("등급: 강한 공격")]
        [Tooltip("적이 강한 공격을 사용할 때 마커 색상")]
        public Color strongAttackColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("강한 공격 마커 크기 배율")]
        public float strongAttackScale = 1.35f;

        [Tooltip("공격 중 마커 펄스 속도(회/초)")]
        public float pulseSpeed = 3f;

        [Tooltip("펄스 시 추가 크기 비율 (0.2 = 기준 크기의 ±20%)")]
        [Range(0f, 1f)]
        public float pulseAmount = 0.2f;

        [Header("등급: 인식만 (보조 표시)")]
        [Tooltip("공격 상태가 아니어도 플레이어를 인식한 적을 약하게 표시할지 여부")]
        public bool showDetectedOnly = true;

        [Tooltip("인식만 한 적의 마커 색상")]
        public Color detectedColor = new Color(1f, 1f, 1f, 0.7f);

        [Tooltip("인식만 한 적의 마커 크기 배율")]
        public float detectedScale = 0.85f;
    }
}
