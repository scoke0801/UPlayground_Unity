using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationStringKeyMotionSet", menuName = "UPlayGround/애니메이션/Actor String")]
    public class ActorAnimationStringKeyMotionSet : ScriptableObject
    {
        public SerializedDictionary<string, MotionSetAsset> motionSets;
        
        public MotionSet GetMotionSet(string key)
        {
            motionSets.TryGetValue(key, out MotionSetAsset result);
            return result?.motionSet;
        }
    }
}   

