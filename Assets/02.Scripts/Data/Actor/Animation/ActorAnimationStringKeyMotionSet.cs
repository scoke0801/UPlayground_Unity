using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "ActorAnimationStringKeyMotionSet", menuName = "UPlayGround/ActorData/Motion/ActorString")]
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

