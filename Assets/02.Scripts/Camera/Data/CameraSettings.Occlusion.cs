using UnityEngine;

namespace UPlayGround.Data
{
    public partial class CameraSettings
    {
        [Header("=== 충돌 · 가림 페이드 ===")]
        [Tooltip("켜면 지형·바닥 외의 장애물은 카메라를 밀지 않고 디더로 흐려진다. 끄면 모든 장애물이 카메라 암을 접는다.")]
        public bool enableOcclusionFade = true;
        [Tooltip("카메라→플레이어(락온 시 대상 포함) 시야선을 훑는 구의 반지름(m). 플레이어 몸폭보다 약간 넓게 둔다.")]
        [Min(0.05f)] public float occlusionProbeRadius = 0.4f;
        [Tooltip("시야선 끝(대상 쪽)에서 제외할 거리(m). 대상 바로 뒤의 벽이 흐려지지 않게 한다.")]
        [Min(0f)] public float occlusionTargetMargin = 0.35f;
        [Tooltip("가려진 오브젝트가 가장 흐려졌을 때 남는 가시율. 0이면 완전히 사라진다. 16단계 디더로 양자화된다.")]
        [Range(0f, 0.9f)] public float occlusionMinVisibility = 0.15f;
        [Tooltip("가려진 오브젝트가 흐려지는 시간(초).")]
        [Min(0.01f)] public float occlusionFadeOutTime = 0.12f;
        [Tooltip("시야선에서 벗어난 오브젝트가 원래대로 돌아오는 시간(초).")]
        [Min(0.01f)] public float occlusionFadeInTime = 0.3f;
        [Tooltip("시야선에서 벗어난 뒤 복귀를 시작하기까지의 유예(초). 모서리에서 깜박이는 것을 막는다.")]
        [Min(0f)] public float occlusionReleaseDelay = 0.25f;
        [Tooltip("동시에 흐려질 수 있는 오브젝트 덩어리 수(렌더링 레이어 슬롯, 최대 8). 초과분은 흐려지지 않고 그대로 보인다.")]
        [Range(1, 8)] public int occlusionMaxGroups = 6;
        [Tooltip("오브젝트 이름으로 Solid/Fade/지붕 제공을 지정하는 규칙. 직접 붙인 CameraOccluder가 없는 오브젝트에 적용된다.")]
        public CameraOccluderNameRulesSO occlusionNameRules;
        [Tooltip("켜면 플레이어는 지붕 밖인데 카메라 끝점이 지붕 아래(건물 내부)에 들어갈 때 암을 지붕 밖까지만 줄인다. 플레이어도 지붕 아래면 적용하지 않는다. 지붕은 '지붕 제공'으로 지정된 콜라이더(CameraOccluder 또는 이름 규칙)만 인정한다.")]
        public bool enableOcclusionInteriorGuard = true;
        [Tooltip("위쪽에 천장이 있는지 확인하는 레이캐스트 길이(m). 키 큰 성채·탑의 지붕까지 닿아야 한다.")]
        [Min(1f)] public float occlusionCoverCheckHeight = 40f;
        [Tooltip("천장 판정 레이를 중심에서 사방으로 벌리는 거리(m). 5개 중 2개 이상이 맞으면 지붕 아래로 본다.")]
        [Min(0f)] public float occlusionCoverSpread = 0.8f;
        [Tooltip("카메라 끝점에서 피벗 쪽으로 지붕 밖 지점을 찾는 간격(m).")]
        [Min(0.1f)] public float occlusionInteriorStep = 0.5f;
        [Tooltip("한 덩어리가 포함할 수 있는 렌더러 상한. 초과하면 월드 전체를 흐리는 사고를 막기 위해 Solid로 취급한다.")]
        [Min(1)] public int occlusionMaxRenderersPerGroup = 64;
        [Tooltip("디더 점 크기(화면 픽셀).")]
        [Range(1, 4)] public int occlusionDitherPixelScale = 1;
        [Tooltip("이 두께(m) 이하이고 가로로 넓은 콜라이더는 바닥·슬랩으로 보고 Solid로 취급한다.")]
        [Min(0f)] public float occlusionFlatSolidMaxThickness = 0.6f;
        [Tooltip("바닥·슬랩으로 인정할 최소 가로 길이(m). 이보다 작은 얇은 소품은 바닥이 아니므로 Fade로 둔다.")]
        [Min(0f)] public float occlusionFlatSolidMinExtent = 2f;
    }
}
