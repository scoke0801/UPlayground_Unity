using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    /// <summary>
    /// 무기 프리팹에 배치하는 보조손 그립 마커.
    /// 양손무기(그레이트소드/창/스태프/활)에서 보조손(off-hand)이 밀착할 지점/회전을 정의한다.
    /// 디자이너가 무기 메시 위 그립 위치에 빈 Transform으로 시각 배치.
    ///
    /// 이 마커의 월드 포즈는 ParentConstraint로 주손 본에 강체 부착되므로,
    /// WeaponIKController가 부착 시점에 "주손 본 기준 로컬 오프셋"으로 캐시해
    /// OnAnimatorIK에서 콘스트레인트 해석 없이 현재 프레임 그립 좌표를 역산한다.
    /// (설계서 docs/WEAPON_IK_SYSTEM_DESIGN.md §3, §5.1)
    /// </summary>
    public class WeaponGripPoint : MonoBehaviour
    {
        [Tooltip("이 그립을 잡는 보조손. 보통 LeftHand (주손이 RightHand인 무기 기준).")]
        public EquipPosition gripHand = EquipPosition.LeftHand;

        [Range(0f, 1f)]
        [Tooltip("그립 IK 기본 weight. WeaponDefinitionSO.offHandWeight 가 있으면 그쪽이 우선될 수 있음.")]
        public float defaultWeight = 1f;
    }
}
