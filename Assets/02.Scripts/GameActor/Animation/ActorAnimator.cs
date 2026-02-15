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

        // 애니메이션 전환 추적
        protected AnimKey _lastPlayedKey;
        protected bool _isPlayingMotionSet;
        
        public AnimancerComponent GetAnimancerComponent() => _animator;
        public Animator GetAnimator => _animator.Animator;
        
        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }

        private void Awake()
        {
            _animator = GetComponent<AnimancerComponent>();
            _eventExecutor = GetComponent<MotionEventExecutor>();

            _animator.Layers[0].ApplyFootIK = true;
            _animator.Layers[0].ApplyAnimatorIK = true;
        }
        public virtual void Init(GameActor actor)
        {
            _actor = actor;
        }

        private void Update()
        {
            // 타임라인 업데이트 (MotionSet 재생 중일 때만)
            if (_isPlayingMotionSet)
            {
                UpdateTimeline();
            }
        }

        // [TODO] 스트링 기반으로 바꿔볼까
        public virtual AnimancerState PlayMotion(string motionName, float fadeDuration = 0.0f)
        {
            return null;
        }

        public virtual bool HasMotion(AnimKey key, bool checkWeapon = false)
        {
            if ( _motionSet == null)
            {
                return false;
            }
            
            return (_motionSet.GetMotionSet(key) != null);
        }
        public virtual AnimancerState PlayMotion(AnimKey key, float fadeDuration = 0.0f)
        {
            // 기존 MotionSet이 재생 중이었다면 안전하게 정리
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                StopMotionSet();
            }
            
            _currentMotionSet = _motionSet.GetMotionSet(key);
            if (_currentMotionSet == null || _currentMotionSet.IsValid() == false)
            {
                return null;
            }
            
            _currentMotionIndex = 0;
            _globalTime = 0f;
            _isPlayingMotionSet = true;
            _lastPlayedKey = key;

            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(_currentMotionSet);

            // 첫 번째 모션 재생
            PlayMotionAtIndex(0, fadeDuration);

            return _currentState;
        }
        
        /// <summary>
        /// MotionSet 안전하게 정지
        /// </summary>
        public void StopMotionSet()
        {
            if (!_isPlayingMotionSet) return;

            // 이벤트 강제 종료
            _eventExecutor?.Stop();

            _isPlayingMotionSet = false;
            _currentMotionSet = null;
            _currentState = null;
            _globalTime = 0f;
            _currentMotionIndex = 0;
        }
        /// <summary>
        /// 현재 재생 중인 애니메이션 강제 정지 (안전장치)
        /// </summary>
        public void StopCurrentAnimation()
        {
            if (_isPlayingMotionSet)
            {
                StopMotionSet();
            }

            if (_animator != null && _animator.IsPlaying())
            {
                _animator.Stop();
            }
        }
        
        /// <summary>
        /// MotionSet의 총 재생 시간 가져오기
        /// </summary>
        public virtual float GetMotionSetDuration(AnimKey key)
        {
            var motionSet = _motionSet.GetMotionSet(key);
            return motionSet?.TotalDuration ?? 0f;
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
        
        // <summary>
        /// MotionSet의 정규화된 시간 (0~1)
        /// </summary>
        public float GetMotionSetNormalizedTime()
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null)
                return 0f;

            float totalDuration = _currentMotionSet.TotalDuration;
            return totalDuration > 0 ? _globalTime / totalDuration : 0f;
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
            if (_animator?.Animator != null)
            {
                _animator.Animator.applyRootMotion = enable;
            }
        }

        private void UpdateTimeline()
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null) return;

            _globalTime += Time.deltaTime;

            // MotionSet 종료 체크
            if (_globalTime >= _currentMotionSet.TotalDuration)
            {
                StopMotionSet();
                return;
            }

            // 이벤트 실행기 업데이트
            _eventExecutor?.UpdateTime(_globalTime);
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
                StopMotionSet();
            }
        }
        void OnDestroy()
        {
            if (_isPlayingMotionSet)
            {
                StopMotionSet();
            }
        }
        /// <summary>
        /// 비활성화 시 안전하게 정리
        /// </summary>
        void OnDisable()
        {
            if (_isPlayingMotionSet)
            {
                StopMotionSet();
            }
        }
    }
}