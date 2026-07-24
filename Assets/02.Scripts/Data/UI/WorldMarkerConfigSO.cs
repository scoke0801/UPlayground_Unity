using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>
    /// 인게임 월드 마커(HUD 웨이포인트) 설정.
    /// 월드 위치에 붙어 화면에 투영되는 마커의 크기/거리 라벨/가장자리 클램프 등 튜닝 수치를 외부화한다.
    /// 데이터 소스는 <c>WorldMarkerRegistry</c>, 표시는 <c>UI_HudWorldMarker</c>가 담당한다.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMarkerConfig", menuName = "UPlayGround/UI/World Marker Config")]
    public class WorldMarkerConfigSO : ScriptableObject
    {
        [Header("표시 범위")]
        [Tooltip("이 거리(m)를 초과한 마커는 표시하지 않는다. 0 이하면 거리 제한 없음.")]
        public float maxDistance = 0f;

        [Tooltip("마커가 타겟 월드 위치에서 위로 얼마나 떠오를지(m). 액터 머리 위 표시용 오프셋.")]
        public float worldHeightOffset = 2f;

        [Header("거리 라벨")]
        [Tooltip("타겟까지 남은 거리를 텍스트로 표시할지 여부")]
        public bool showDistanceLabel = true;

        [Tooltip("거리 라벨 포맷. {0}에 반올림된 미터값이 들어간다.")]
        public string distanceFormat = "{0}m";

        [Tooltip("이 거리(m) 미만이면 거리 라벨을 숨긴다. 바로 앞 타겟의 '0m' 노이즈 방지. 0이면 항상 표시.")]
        public float hideDistanceLabelWithin = 3f;

        [Header("아이콘 크기 / 거리 페이드")]
        [Tooltip("마커 아이콘의 기본 크기 배율")]
        public float baseScale = 1f;

        [Tooltip("거리에 따라 아이콘 크기를 줄일지 여부(멀수록 작아짐)")]
        public bool scaleByDistance = false;

        [Tooltip("이 거리(m)에서 minScale에 도달한다. scaleByDistance가 켜져 있을 때만 사용.")]
        public float scaleFalloffDistance = 60f;

        [Tooltip("거리 페이드 시 도달하는 최소 크기 배율")]
        [Range(0.1f, 1f)]
        public float minScale = 0.5f;

        [Header("오프스크린 처리")]
        [Tooltip("타겟이 화면 밖일 때 마커를 화면 가장자리에 붙여 계속 보이게 할지 여부. 끄면 화면 밖에서 숨긴다.")]
        public bool clampToScreenEdge = true;

        [Tooltip("가장자리 클램프 시 화면 경계에서 안쪽으로 띄우는 여백(픽셀)")]
        public float edgeMargin = 60f;
    }
}
