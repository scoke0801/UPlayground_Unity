using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Enum;

namespace UPlayGround.Animation
{
    public class ActorAnimator : MonoBehaviour
    {
        [SerializeField] private ActorAnimationSet _animationSet;
        
        protected AnimancerComponent _animator;

        protected GameActor _actor;
        
        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }
        
        public virtual void Init(GameActor actor)
        {
            _animator = GetComponent<AnimancerComponent>();
            _actor = actor;

            _animator.Layers[0].ApplyFootIK = true;
            _animator.Layers[0].ApplyAnimatorIK = true;
        }

        public virtual AnimancerState PlayAnimation(AnimKey key, float fadeDuration = 0.0f)
        {
            ClipTransition transition = _animationSet.GetClipTransition(key);
            if (transition == null)
            {
                return null;
            }
            
            return _animator.Play(transition, fadeDuration);
        }
        
        /// <summary>
        /// 특정 AnimKey의 AnimationClip duration 가져오기
        /// </summary>
        public virtual float GetAnimationDuration(AnimKey key)
        {
            var clip = _animationSet.GetAnimationClip(key);
            
            if (clip == null)
            {
                Debug.LogWarning($"[ActorAnimator] AnimKey '{key}'에 해당하는 클립을 찾을 수 없습니다.");
                return 0f;
            }
            
            return clip.length;
        }
        
        /// <summary>
        /// 현재 재생 중인 애니메이션의 남은 시간
        /// </summary>
        public float GetRemainingTime()
        {
            if (_animator.States.Current == null)
                return 0f;
                
            var state = _animator.States.Current;
            float remaining = state.Length - state.Time;
            return Mathf.Max(0f, remaining);
        }
        
        /// <summary>
        /// 현재 애니메이션의 정규화된 시간 (0~1)
        /// </summary>
        public float GetNormalizedTime()
        {
            if (_animator.States.Current == null)
                return 0f;
                
            var state = _animator.States.Current;
            return state.NormalizedTime;
        }
        
        /// <summary>
        /// 애니메이션이 거의 끝났는지 체크
        /// </summary>
        public bool IsAnimationNearEnd(float threshold = 0.9f)
        {
            return GetNormalizedTime() >= threshold;
        }
        
        public void SetAnimationParameter(string key, float value)
        {
            // _animator.Parameters.SetValue(key, value);
        }

        public void ApplyRootMotion(bool enable)
        {
            _animator.Animator.applyRootMotion = enable;
        }
        
        void OnAnimatorMove()
        {
            // 루트모션 델타 저장
            DeltaPosition = _animator.Animator.deltaPosition;
            DeltaRotation = _animator.Animator.deltaRotation;
        }
    }
}