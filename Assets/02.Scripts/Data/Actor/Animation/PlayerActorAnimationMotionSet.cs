
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "PlayerActorAnimationMotionSet", menuName = "UPlayGround/ActorData/Motion/PlayerActor")]
    public class PlayerActorAnimationMotionSet : ScriptableObject
    {
        public SerializedDictionary<WeaponType, ActorAnimationMotionSet> motionSets;

        public ActorAnimationMotionSet GetActorAnimationMotionSet(WeaponType weaponType)
        {
            motionSets.TryGetValue(weaponType, out ActorAnimationMotionSet result);
            return result;
        }

        public MotionSet GetMotionSet(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationMotionSet motionSet = GetActorAnimationMotionSet(weaponType);
            if (motionSet != null)
            {
                return motionSet.GetMotionSet(key);
            }

            if (weaponType != WeaponType.NoWeapon)
            {
                motionSet = GetActorAnimationMotionSet(WeaponType.NoWeapon);
                return motionSet?.GetMotionSet(key);
            }
            return null;
        }
    }
}