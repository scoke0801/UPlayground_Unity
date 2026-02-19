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

        [SerializeField] private AvatarMask _upperBodyMask;
        
        [Header("Event Executor")]
        [SerializeField] protected MotionEventExecutor _eventExecutor;

        [Space]
        
        protected AnimancerComponent _animator;
        protected GameActor _actor;
        
        protected int _currentMotionIndex;
        protected int _currentMotionLayerIndex;
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

            if (_upperBodyMask != null)
            {
                _animator.Layers.SetMask(1, _upperBodyMask);
            }
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

        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (_animator.Layers.Count > layerIndex)
            {
                _animator.Layers[layerIndex].Weight = weight;
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
        public virtual AnimancerState PlayMotion(AnimKey key, float fadeDuration = 0.0f, int layerIndex = 0)
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
            PlayMotionAtIndex(0, fadeDuration, layerIndex);

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
        public void StopCurrentAnimation(int layerIndex = 0)
        {
            if (_isPlayingMotionSet && layerIndex == 0)
            {
                StopMotionSet();
            }

            if (_animator != null && _animator.IsPlaying())
            {
                _animator.Layers[layerIndex].Stop();
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
        /// 현재 재생 중인 MotionSet 의 남은 시간.
        /// MotionSet 재생 중이 아닐 경우 현재 클립의 남은 시간을 반환한다.
        /// </summary>
        public float GetRemainingTime()
        {
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                float remaining = _currentMotionSet.TotalDuration - _globalTime;
                return Mathf.Max(0f, remaining);
            }

            if (_animator.States.Current == null) return 0f;

            var state = _animator.States.Current;
            // clipEndTime 이 설정된 모션이 있을 경우를 대비해 현재 모션 기준으로 계산
            if (_currentMotionSet != null &&
                _currentMotionIndex >= 0 &&
                _currentMotionIndex < _currentMotionSet.motions.Count)
            {
                var motion = _currentMotionSet.motions[_currentMotionIndex];
                if (motion != null && motion.IsValid())
                {
                    float clipRemaining = motion.ClipEndTime - state.Time;
                    return Mathf.Max(0f, clipRemaining);
                }
            }

            return Mathf.Max(0f, state.Length - state.Time);
        }

        /// <summary>
        /// 현재 애니메이션의 정규화된 시간 (0~1).
        /// MotionSet 재생 중이면 MotionSet 전체 기준으로 반환한다.
        /// </summary>
        public float GetNormalizedTime()
        {
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                float total = _currentMotionSet.TotalDuration;
                return total > 0f ? Mathf.Clamp01(_globalTime / total) : 0f;
            }

            if (_animator.States.Current == null) return 0f;
            return _animator.States.Current.NormalizedTime;
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

            // 현재 globalTime에 해당하는 모션 인덱스 계산
            if (_currentMotionSet.GetMotionAtTime(_globalTime, out int newIndex, out float localTime))
            {
                // 모션 인덱스가 바뀐 경우 → 다음 모션으로 전환
                if (newIndex != _currentMotionIndex)
                {
                    _currentMotionIndex = newIndex;
                    PlayMotionAtIndex(_currentMotionIndex, 0f, _currentMotionLayerIndex);
                }
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
        /// 특정 인덱스의 모션 재생.
        /// clipStartTime / clipEndTime / playbackSpeed 를 반영한다.
        /// 모션 전환은 UpdateTimeline 이 globalTime 기반으로 처리하므로 OnEnd 콜백을 사용하지 않는다.
        /// </summary>
        protected void PlayMotionAtIndex(int index, float fadeDuration, int layerIndex = 0)
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
                // 유효하지 않은 모션은 건너뜀 (globalTime 기반 전환이 다음 인덱스를 처리)
                return;
            }

            _currentMotionIndex = index;

            var layer = _animator.Layers[layerIndex];
            if (_currentMotionLayerIndex != layerIndex)
            {
                layer.StartFade(1.0f, fadeDuration);
            }
            _currentMotionLayerIndex = layerIndex;

            // 클립 재생 — clipStartTime 부터 시작
            _currentState = layer.Play(motion.motionClip, fadeDuration);
            _currentState.Time  = motion.ClipStartTime;
            _currentState.Speed = motion.playbackSpeed;

            // OnEnd 콜백 제거 — 종료/전환은 UpdateTimeline 이 globalTime 으로 판단
            _currentState.Events(this).OnEnd = null;
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