using Animancer;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "PlayerActorAnimationSet", menuName = "UP/ActorData/PlayerActorAnimationSet")]
    public class PlayerActorAnimationSet : ScriptableObject
    {
        [Header("무기")]
        [SerializeField] private ActorAnimationSet _swordAnimationSet;
        [SerializeField] private ActorAnimationSet _greatSwordAnimationSet;
        [SerializeField] private ActorAnimationSet _shieldAnimationSet;
        [SerializeField] private ActorAnimationSet _staffAnimationSet;
        [SerializeField] private ActorAnimationSet _bowAnimationSet;

        [Header("공용")]
        [SerializeField] private ActorAnimationSet _unarmedAnimationSet; 
        
        public ClipTransition GetClipTransition(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationSet animSet = GetAnimationSet(weaponType);
            return animSet == null ? animSet.GetClipTransition(key) : null;
        }

        public AnimationClip GetAnimationClip(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationSet animSet = GetAnimationSet(weaponType);
            return animSet == null ? animSet.GetAnimationClip(key) : null;
        }

        private ActorAnimationSet GetAnimationSet(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword: return _swordAnimationSet;
                case WeaponType.GreatSword: return _greatSwordAnimationSet;
                case WeaponType.Bow: return _bowAnimationSet;
                case WeaponType.Shield: return _shieldAnimationSet;
                case WeaponType.Staff: return _staffAnimationSet;
            }

            return null;
        }
    }
}