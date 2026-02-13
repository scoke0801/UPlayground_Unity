using System;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Enum;

namespace UPlayGround.Animation
{
    [RequireComponent(typeof(MotionEventExecutor))]
    public class ActorAnimator : MonoBehaviour
    {
        [Header("Actor Setting")]
        [SerializeField] private ActorAnimationSet _animationSet;
        [SerializeField] private ActorAnimationMotionSet _motionSet;
        
        [Header("Event Executor")]
        [SerializeField] protected MotionEventExecutor _eventExecutor;

        [Space]
        
        protected AnimancerComponent _animator;

        protected GameActor _actor;
        
        protected int _currentMotionIndex;
        protected float _globalTime;
        protected MotionSet _currentMotionSet;

        protected AnimancerState _currentState;

        
        public AnimancerComponent GetAnimancerComponent() => _animator;
        
        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }
        
        public virtual void Init(GameActor actor)
        {
            _animator = GetComponent<AnimancerComponent>();
            _eventExecutor = GetComponent<MotionEventExecutor>();
            
            _actor = actor;

            _animator.Layers[0].ApplyFootIK = true;
            _animator.Layers[0].ApplyAnimatorIK = true;
        }

        private void Update()
        {
            // 타임라인 업데이트
            UpdateTimeline();
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

        // [TODO] 스트링 기반으로 바꿔볼까
        public virtual AnimancerState PlayMotion(string motionName, float fadeDuration = 0.0f)
        {
            return null;
        }

        public virtual AnimancerState PlayMotion(AnimKey key, float fadeDuration = 0.0f)
        {
            _currentMotionSet = _motionSet.GetMotionSet(key);
            if (_currentMotionSet == null || _currentMotionSet.IsValid() == false)
            {
                return null;
            }
            
            _currentMotionIndex = 0;
            _globalTime = 0f;
            
            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(_currentMotionSet);
            
            // 첫 번째 모션 재생
            PlayMotionAtIndex(0, fadeDuration);

            return _currentState;
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

        private void UpdateTimeline()
        {
            _globalTime += Time.deltaTime;
            _eventExecutor.UpdateTime(_globalTime);
        }
        
        private void OnAnimatorMove()
        {
            // 루트모션 델타 저장
            DeltaPosition = _animator.Animator.deltaPosition;
            DeltaRotation = _animator.Animator.deltaRotation;
        }
        
        /// <summary>
        /// 특정 인덱스의 모션 재생
        /// </summary>
        protected void PlayMotionAtIndex(int index, float fadeDuration)
        {
            if (_currentMotionSet.motions == null || 
                index < 0 || 
                index >= _currentMotionSet.motions.Count)
            {
                return;
            }

            var motion = _currentMotionSet.motions[index];
            if (motion == null || !motion.IsValid())
            {
                _currentMotionIndex++;
                PlayMotionAtIndex(_currentMotionIndex, fadeDuration);
                return;
            }

            _currentMotionIndex = index;
            
            // Animancer로 애니메이션 재생
            _currentState = _animator.Play(motion.motionClip, fadeDuration);
            
            // 모션 종료 시 다음 모션으로 전환
            var endAction = _currentState.Events(this).OnEnd;
            if (endAction != null)
            {
                endAction += () =>
                {
                    OnMotionEnd(fadeDuration);
                };
            }
           
        }

        /// <summary>
        /// 모션 종료 콜백
        /// </summary>
        void OnMotionEnd(float fadeDuration)
        {
            _currentMotionIndex++;
            
            // 다음 모션이 있으면 재생, 없으면 종료
            if (_currentMotionIndex < _currentMotionSet.motions.Count)
            {
                PlayMotionAtIndex(_currentMotionIndex, 0.0f);
            }
            else
            {
                _currentState = null;
            }
        }

    }
}