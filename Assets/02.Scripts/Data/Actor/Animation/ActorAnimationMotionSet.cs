using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationMotionSet", menuName = "UPlayGround/애니메이션/Actor")]
    public class ActorAnimationMotionSet : ScriptableObject
    {
        [Tooltip("이 SO에 없는 키는 여기서 탐색 (공용 휴머노이드 모션 등)")]
        public ActorAnimationMotionSet fallbackMotionSet;

        [Header("공격 모션")]
        [Tooltip("공격 MotionReference를 해석할 무기 타입입니다. 다른 무기의 override를 이 ActorMotionSet에 노출하지 않습니다.")]
        public WeaponType attackWeaponType = WeaponType.NoWeapon;

        [Tooltip("이 액터 모션 세트에서 함께 저작할 공격 Ability 모음입니다. 공격 실행의 단일 소스는 Ability Payload의 MotionReference이며, 애니메이션 에디터는 이 연결을 통해 공격 MotionSet을 표시합니다.")]
        public AbilitySetSO attackAbilitySet;

        // GameplayTag 슬롯 이전의 정수 키 데이터다. 런타임에서는 사용하지 않지만,
        // 다른 필드를 베이크해 에셋을 저장할 때 기존 참조가 소실되지 않도록 보존한다.
        [SerializeField, HideInInspector]
        private SerializedDictionary<int, MotionSetAsset> motionSets;

        [Header("상태 모션")]
        [Tooltip("액터 상태가 사용하는 의미 슬롯 매핑입니다.")]
        public SerializedDictionary<GameplayTag, MotionSetAsset> motionSlots;

        [Header("로코모션 루트모션 베이크")]
        [Tooltip("Turn 슬롯별 베이크된 총 루트 yaw(도)입니다. 런타임에서 추정하지 않습니다.")]
        public SerializedDictionary<GameplayTag, float> motionRootYaw;

        [Tooltip("로코모션 슬롯별 베이크된 기준 이동 속도(m/s)입니다.")]
        public SerializedDictionary<GameplayTag, float> motionReferenceSpeed;

        public MotionSetAsset GetMotionSetAsset(GameplayTag slot, int depth = 0)
        {
            if (depth > 8 || !slot.IsValid()) return null;
            if (motionSlots != null
                && motionSlots.TryGetValue(slot, out MotionSetAsset result)
                && result != null)
                return result;
            return fallbackMotionSet?.GetMotionSetAsset(slot, depth + 1);
        }

        public MotionSet GetMotionSet(GameplayTag slot, int depth = 0) =>
            GetMotionSetAsset(slot, depth)?.motionSet;

        public bool TryGetMotionRootYaw(GameplayTag slot, out float yaw, int depth = 0)
        {
            yaw = 0f;
            if (depth > 8 || !slot.IsValid())
                return false;
            if (motionRootYaw != null
                && motionRootYaw.TryGetValue(slot, out yaw)
                && Mathf.Abs(yaw) > 0.001f)
                return true;
            return fallbackMotionSet != null
                   && fallbackMotionSet.TryGetMotionRootYaw(slot, out yaw, depth + 1);
        }

        public bool TryGetMotionReferenceSpeed(
            GameplayTag slot,
            out float speed,
            int depth = 0)
        {
            speed = 0f;
            if (depth > 8 || !slot.IsValid())
                return false;
            if (motionReferenceSpeed != null
                && motionReferenceSpeed.TryGetValue(slot, out speed)
                && speed > 0.001f)
                return true;
            return fallbackMotionSet != null
                   && fallbackMotionSet.TryGetMotionReferenceSpeed(
                       slot,
                       out speed,
                       depth + 1);
        }

    }
}   

