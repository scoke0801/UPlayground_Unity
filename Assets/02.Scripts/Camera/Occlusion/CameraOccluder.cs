using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>카메라가 장애물을 만났을 때의 처리 방식.</summary>
    public enum OccluderMode
    {
        /// <summary>레이어·형태·렌더러 유무로 자동 판정한다.</summary>
        Auto = 0,
        /// <summary>카메라 암을 접어 막는다. 지형·바닥처럼 카메라가 통과하면 안 되는 대상.</summary>
        Solid = 1,
        /// <summary>카메라는 통과시키고 가려진 렌더러를 디더로 흐리게 한다.</summary>
        Fade = 2,
        /// <summary>카메라 충돌과 가림 페이드 모두에서 제외한다.</summary>
        Ignore = 3,
    }

    /// <summary>
    /// 오브젝트·프리팹 단위로 카메라 가림 처리 방식을 지정하는 저작용 컴포넌트.
    /// 없으면 <see cref="OccluderMode.Auto"/>로 판정하므로 기존 씬은 그대로 동작한다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UPlayGround/카메라/Camera Occluder")]
    public sealed class CameraOccluder : MonoBehaviour
    {
        [Tooltip("Auto: 레이어·형태로 자동 판정. Solid: 카메라가 밀림. Fade: 통과 후 흐려짐. Ignore: 완전 무시.")]
        [SerializeField] private OccluderMode _mode = OccluderMode.Auto;

        [Tooltip("켜면 충돌체가 닿은 부분이 아니라 이 오브젝트 하위 렌더러 전체를 한 덩어리로 흐리게 한다. 건물 한 채처럼 함께 사라져야 하는 대상에 쓴다.")]
        [SerializeField] private bool _fadeWholeHierarchy;

        [Tooltip("켜면 이 오브젝트 하위 콜라이더를 '지붕·천장'으로 인정한다. 플레이어는 지붕 밖인데 카메라가 지붕 아래(건물 내부)에 들어가면 카메라 실내 가드가 암을 줄인다. 건물 프리팹 루트에 켠다. 나무 캐노피 등에는 켜지 않는다.")]
        [SerializeField] private bool _coversInterior;

        /// <summary>지정된 처리 방식.</summary>
        public OccluderMode Mode => _mode;

        /// <summary>하위 렌더러 전체를 한 덩어리로 페이드할지 여부.</summary>
        public bool FadeWholeHierarchy => _fadeWholeHierarchy;

        /// <summary>하위 콜라이더를 실내 가드의 지붕·천장으로 인정할지 여부.</summary>
        public bool CoversInterior => _coversInterior;
    }
}
