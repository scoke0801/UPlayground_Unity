using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationMotionSet", menuName = "UPlayGround/ActorData/Motion/Actor")]
    public class ActorAnimationMotionSet : ScriptableObject
    {
        public SerializedDictionary<AnimKey, MotionSetAsset> motionSets;
        
        public MotionSet GetMotionSet(AnimKey key)
        {
            motionSets.TryGetValue(key, out MotionSetAsset result);
            return result?.motionSet;
        }
    }
}   

