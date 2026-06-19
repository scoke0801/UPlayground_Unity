using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationMotionSet", menuName = "UPlayGround/애니메이션/Actor")]
    public class ActorAnimationMotionSet : ScriptableObject
    {
        [Tooltip("이 SO에 없는 키는 여기서 탐색 (공용 휴머노이드 모션 등)")]
        public ActorAnimationMotionSet fallbackMotionSet;

        public SerializedDictionary<AnimKey, MotionSetAsset> motionSets;

        public MotionSet GetMotionSet(AnimKey key, int depth = 0)
        {
            if (depth > 8) return null;
            if (motionSets != null && motionSets.TryGetValue(key, out MotionSetAsset result) && result != null)
                return result.motionSet;
            return fallbackMotionSet?.GetMotionSet(key, depth + 1);
        }
    }
}   

