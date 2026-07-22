using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationMotionSet", menuName = "UPlayGround/애니메이션/Actor")]
    public class ActorAnimationMotionSet : ScriptableObject
    {
        [Tooltip("이 SO에 없는 키는 여기서 탐색 (공용 휴머노이드 모션 등)")]
        public ActorAnimationMotionSet fallbackMotionSet;

        [Tooltip("액터 상태가 사용하는 의미 슬롯 매핑입니다.")]
        public SerializedDictionary<GameplayTag, MotionSetAsset> motionSlots;

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

    }
}   

