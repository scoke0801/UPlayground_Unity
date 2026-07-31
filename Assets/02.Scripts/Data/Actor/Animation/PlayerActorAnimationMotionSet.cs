using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

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

        public MotionSetAsset GetMotionSetAsset(WeaponType weaponType, GameplayTag slot)
        {
            ActorAnimationMotionSet motionSet = GetActorAnimationMotionSet(weaponType);
            MotionSetAsset motion = motionSet?.GetMotionSetAsset(slot);
            if (motion != null)
                return motion;

            if (weaponType == WeaponType.NoWeapon)
                return null;
            return GetActorAnimationMotionSet(WeaponType.NoWeapon)?.GetMotionSetAsset(slot);
        }

        public MotionSet GetMotionSet(WeaponType weaponType, GameplayTag slot) =>
            GetMotionSetAsset(weaponType, slot)?.motionSet;

        public MotionSetAsset GetAbilityMotionAsset(
            WeaponType weaponType,
            AbilityMotionKey key)
        {
            ActorAnimationMotionSet motionSet =
                GetActorAnimationMotionSet(weaponType);
            MotionSetAsset motion = motionSet?.GetAbilityMotionAsset(key);
            if (motion != null)
                return motion;

            if (weaponType == WeaponType.NoWeapon)
                return null;
            return GetActorAnimationMotionSet(WeaponType.NoWeapon)
                ?.GetAbilityMotionAsset(key);
        }
    }
}
