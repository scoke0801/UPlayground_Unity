using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "PlayerActorAnimationMotionSet", menuName = "UPlayGround/애니메이션/Player")]
    public class PlayerActorAnimationMotionSet : ScriptableObject
    {
        public SerializedDictionary<WeaponType, ActorAnimationMotionSet> motionSets;

        public ActorAnimationMotionSet GetActorAnimationMotionSet(WeaponType weaponType)
        {
            motionSets.TryGetValue(weaponType, out ActorAnimationMotionSet result);
            return result;
        }

        public ActorAnimationMotionSet GetDefaultMotionSet()
        {
            if (motionSets == null) return null;
            if (motionSets.TryGetValue(WeaponType.NoWeapon, out var noWeapon) && noWeapon != null)
                return noWeapon;
            foreach (var v in motionSets.Values)
                if (v != null) return v;
            return null;
        }

        public MotionSet GetMotionSet(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationMotionSet motionSet = GetActorAnimationMotionSet(weaponType);
            if (motionSet != null)
            {
                var motion = motionSet.GetMotionSet(key);
                if (motion != null)
                {
                    return motion;
                }
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