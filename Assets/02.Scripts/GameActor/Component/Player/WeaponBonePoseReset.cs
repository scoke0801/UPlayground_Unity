using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 무기 더미 본(R_Hand_Weapon 등)의 기본 로컬 포즈를 매 프레임 선복원한다.
    ///
    /// 베이크된 클립(WeaponBoneBakeEditorWindow 산출물)이 이 본의 곡선을 갖고 있으면
    /// Update 이후의 애니메이션 평가 단계에서 이 값을 덮어쓰고,
    /// 곡선이 없는 클립 재생 중에는 마지막 베이크 포즈가 남는 대신 기본 포즈로 돌아온다.
    /// 무기 더미 본 GameObject에 직접 부착해서 사용.
    /// </summary>
    public class WeaponBonePoseReset : MonoBehaviour
    {
        private Vector3 _defaultLocalPosition;
        private Quaternion _defaultLocalRotation;

        private void Awake()
        {
            _defaultLocalPosition = transform.localPosition;
            _defaultLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            transform.localPosition = _defaultLocalPosition;
            transform.localRotation = _defaultLocalRotation;
        }
    }
}
