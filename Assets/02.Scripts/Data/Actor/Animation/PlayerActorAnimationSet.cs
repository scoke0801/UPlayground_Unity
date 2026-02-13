using Animancer;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor.Animation
{
    [CreateAssetMenu(fileName = "PlayerActorAnimationSet", menuName = "UPlayGround/ActorData/PlayerActorAnimationSet")]
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
        private void OnEnable()
        {
            _swordAnimationSet?.InitializeDictionary();
            _greatSwordAnimationSet?.InitializeDictionary();
            _shieldAnimationSet?.InitializeDictionary();
            _staffAnimationSet?.InitializeDictionary();
            _bowAnimationSet?.InitializeDictionary();
            
            _unarmedAnimationSet?.InitializeDictionary();
        }
        public ClipTransition GetClipTransition(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationSet animSet = GetAnimationSet(weaponType);

            if (animSet != null)
            {
                ClipTransition transition = animSet.GetClipTransition(key);
                if (transition != null)
                    return transition;
            }
            
            return _unarmedAnimationSet?.GetClipTransition(key);
        }

        public AnimationClip GetAnimationClip(WeaponType weaponType, AnimKey key)
        {
            ActorAnimationSet animSet = GetAnimationSet(weaponType);
            if (animSet != null)
            {
                AnimationClip clip = animSet.GetAnimationClip(key);
                if (clip != null)
                    return clip;
            }
            
            return _unarmedAnimationSet?.GetAnimationClip(key);
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

            return _unarmedAnimationSet;
        }
    }
}