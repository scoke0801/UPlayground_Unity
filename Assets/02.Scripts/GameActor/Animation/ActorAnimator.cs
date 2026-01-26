using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Enum;

namespace UPlayGround.GameActor.Animation
{
    public class ActorAnimator : MonoBehaviour
    {
        [SerializeField] private ActorAnimationSet _animationSet;
        
        private AnimancerComponent _animator;

        private Base.GameActor _actor;
        
        public void Init(Base.GameActor actor)
        {
            _animator = GetComponent<AnimancerComponent>();
            _actor = actor;

            _animator.Layers[0].ApplyFootIK = true;
            _animator.Layers[0].ApplyAnimatorIK = true;
        }

        public AnimancerState PlayAnimation(AnimKey key, float fadeDuration = 0.0f)
        {
            ClipTransition transition = _animationSet.GetClipTransition(key);
            if (transition == null)
            {
                return null;
            }
            
            return _animator.Play(transition, fadeDuration);
        }

        public void SetAnimationParameter(string key, float value)
        {
            // _animator.Parameters.SetValue(key, value);
        }
    }
}