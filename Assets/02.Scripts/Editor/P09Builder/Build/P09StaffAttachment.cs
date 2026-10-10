using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>스태프의 손 소켓과 발도 계약을 빌더·미리보기에서 동일하게 구성한다.</summary>
    internal static class P09StaffAttachment
    {
        public const string MotionSetPath =
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_StaffAnimationSet.asset";
        public const string MotionRoot =
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Staff/Humanoid_Staff_";

        /// <summary>선택된 스태프에 명시적 바인딩과 선택적인 손 보정값을 저장한다.</summary>
        public static void Apply(GameObject root, CharacterBuildConfig config)
        {
            var staff = FindActive(root);
            if (staff == null) return;
            if (staff.sourceCount != 2 || staff.GetSource(0).sourceTransform == null ||
                staff.GetSource(1).sourceTransform == null)
                throw new InvalidOperationException("스태프에는 손(0)·등(1) 소스가 필요합니다.");

            var binding = staff.GetComponent<WeaponSocketBinding>();
            if (binding == null) binding = staff.gameObject.AddComponent<WeaponSocketBinding>();
            binding.equipPosition = EquipPosition.RightHand;
            binding.weaponType = WeaponType.Staff;
            binding.matchAnyWeaponType = false;
            binding.constraint = staff;
            // 공용 손 타깃은 검 등도 사용하므로 스태프 제약의 손 오프셋만 바꾼다.
            if (config.OverrideStaffGrip)
            {
                staff.SetTranslationOffset(0, config.StaffGripPosition);
                staff.SetRotationOffset(0, config.StaffGripRotation);
            }
            staff.weight = 1f;
            staff.locked = true;
            staff.constraintActive = true;
        }

        /// <summary>메쉬가 선택된 스태프 슬롯만 반환한다.</summary>
        public static ParentConstraint FindActive(GameObject root)
        {
            return root.GetComponentsInChildren<ParentConstraint>(true)
                .FirstOrDefault(c => c.name == "Staff" && c.gameObject.activeInHierarchy &&
                    c.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
        }

        /// <summary>에디터 단일 프레임 샘플링 후 손 소스를 즉시 반영한다.</summary>
        public static void PosePreview(ParentConstraint staff)
        {
            if (staff == null || staff.sourceCount < 2) return;
            var hand = staff.GetSource(0);
            if (hand.sourceTransform == null) return;
            hand.weight = 1f;
            staff.SetSource(0, hand);
            var back = staff.GetSource(1);
            back.weight = 0f;
            staff.SetSource(1, back);
            // PreviewRenderUtility에는 일반 PlayerLoop의 제약 평가가 보장되지 않는다.
            staff.transform.SetPositionAndRotation(
                hand.sourceTransform.TransformPoint(staff.GetTranslationOffset(0)),
                hand.sourceTransform.rotation * Quaternion.Euler(staff.GetRotationOffset(0)));
        }
    }
}
