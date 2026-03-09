using System;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    [RequireComponent(typeof(MotionEventExecutor))]
    public class ActorAnimator : MonoBehaviour
    {
        [Header("Actor Setting")]
        [SerializeField] private ActorAnimationMotionSet _motionSet;

        [SerializeField] private AvatarMask _upperBodyMask;
        
        [Header("SubAnimator Setting")]
        [Tooltip("애니메이션에 종속적으로 실행되는 애니메이터, 무기 등")]
        [SerializeField] private ActorAnimator _subAnimator;
        
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

        // ── Loop/Freeze 상태 ──
        private LoopEvent _activeLoopEvent;
        private int _loopRemainingCount;
        private float _freezeTimer;
        private bool _isFrozen;
        private bool _isInfiniteLooping;
        private float _infiniteLoopElapsed; // InfiniteLoop 진입 후 경과 시간
        
        public event Action OnMotionSetCompleted;
        public AnimancerComponent GetAnimancerComponent() => _animator;
        public Animator GetAnimator => _animator.Animator;
        
        /// <summary>
        /// 전체 애니메이터 재생 속도
        /// </summary>
        public float Speed
        {
            get => _animator != null ? _animator.Graph.Speed : 1.0f;
            set
            {
                if (_animator != null)
                    _animator.Graph.Speed = value;
                
                if (_subAnimator != null)
                    _subAnimator.Speed = value;
            }
        }

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
            if (_isPlayingMotionSet
                && _lastPlayedKey == key)
            {
                return _currentState;
            }
            
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

            if (_subAnimator != null)
            {
                _subAnimator.PlayMotion(key, fadeDuration, layerIndex);
            }
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
            ResetLoopState();
            
            if (_subAnimator != null)
            {
                _subAnimator.StopMotionSet();
            }
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

            float deltaTime = _actor != null ? _actor.DeltaTime : Time.deltaTime;

            // ── Freeze 중이면 시간을 흘리지 않고 타이머만 소모 ──
            if (_isFrozen)
            {
                _freezeTimer -= deltaTime;
                if (_freezeTimer <= 0f)
                {
                    _isFrozen = false;
                    // Freeze 해제 시 애니메이션 속도 복원
                    if (_currentState != null)
                        _currentState.Speed = GetCurrentMotion()?.playbackSpeed ?? 1f;
                }
                // Freeze 중에도 이벤트 업데이트는 수행 (다른 이벤트가 동작할 수 있도록)
                _eventExecutor?.UpdateTime(_globalTime);
                return;
            }

            _globalTime += deltaTime;

            // MotionSet 종료 체크
            if (_globalTime >= _currentMotionSet.TotalDuration)
            {
                StopMotionSet();
                OnMotionSetCompleted?.Invoke();
                return;
            }

            // 현재 모션 인덱스 계산
            if (_currentMotionSet.GetMotionAtTime(_globalTime, out int newIndex, out float localTime))
            {
                if (newIndex != _currentMotionIndex)
                {
                    // 모션이 바뀌면 이전 모션의 Loop 상태 리셋
                    ResetLoopState();
                    _currentMotionIndex = newIndex;
                    PlayMotionAtIndex(_currentMotionIndex, 0f, _currentMotionLayerIndex);
                }

                // ── Loop/Freeze 이벤트 감지 및 처리 ──
                ProcessLoopEvents(localTime);
            }

            _eventExecutor?.UpdateTime(_globalTime);
        }

        /// <summary>
        /// 현재 모션의 LoopEvent를 감지하고 타임라인을 조작한다.
        /// - Loop: localTime이 endTime을 넘으면 startTime으로 되감기 (남은 횟수만큼)
        /// - Freeze: localTime이 startTime을 넘으면 애니메이션 정지 + 타이머 시작
        /// </summary>
        private void ProcessLoopEvents(float localTime)
        {
            var motion = GetCurrentMotion();
            if (motion?.events == null) return;

            foreach (var evt in motion.events)
            {
                if (evt is not LoopEvent loopEvt) continue;

                switch (loopEvt.mode)
                {
                    case LoopEventMode.Loop:
                        HandleLoopMode(loopEvt, localTime);
                        break;
                    case LoopEventMode.InfiniteLoop:
                        HandleInfiniteLoopMode(loopEvt, localTime);
                        break;
                    case LoopEventMode.Freeze:
                        HandleFreezeMode(loopEvt, localTime);
                        break;
                }
            }
        }

        private void HandleLoopMode(LoopEvent loopEvt, float localTime)
        {
            // 아직 이 이벤트의 endTime에 도달하지 않았으면 무시
            if (localTime < loopEvt.endTime) return;

            // 처음 도달: 루프 카운터 초기화
            if (_activeLoopEvent != loopEvt)
            {
                _activeLoopEvent = loopEvt;
                _loopRemainingCount = loopEvt.loopCount;
            }

            if (_loopRemainingCount <= 0) return;

            // globalTime을 되감아 startTime 구간으로 복귀
            float loopDuration = loopEvt.endTime - loopEvt.startTime;
            _globalTime -= loopDuration;
            _loopRemainingCount--;

            // Animancer 클립 시간도 되감기
            if (_currentState != null)
            {
                var motion = GetCurrentMotion();
                float clipLocalStart = motion != null
                    ? motion.ClipStartTime + loopEvt.startTime * (motion.playbackSpeed > 0 ? motion.playbackSpeed : 1f)
                    : 0f;
                _currentState.Time = clipLocalStart;
            }
        }

        private void HandleFreezeMode(LoopEvent loopEvt, float localTime)
        {
            if (_isFrozen) return;
            if (_activeLoopEvent == loopEvt) return; // 이미 처리된 Freeze
            if (localTime < loopEvt.startTime) return;

            // Freeze 시작
            _activeLoopEvent = loopEvt;
            _isFrozen = true;
            _freezeTimer = loopEvt.freezeDuration;

            if (_currentState != null)
                _currentState.Speed = 0f;
        }

        private void HandleInfiniteLoopMode(LoopEvent loopEvt, float localTime)
        {
            if (!_isInfiniteLooping && localTime >= loopEvt.endTime)
            {
                // 첫 도달: 무한 루프 상태 진입
                _activeLoopEvent = loopEvt;
                _isInfiniteLooping = true;
                _infiniteLoopElapsed = 0f;
            }

            if (!_isInfiniteLooping || _activeLoopEvent != loopEvt) return;

            // Duration 경과 시 자동 해제
            float deltaTime = _actor != null ? _actor.DeltaTime : Time.deltaTime;
            _infiniteLoopElapsed += deltaTime;

            var motion = GetCurrentMotion();
            if (motion != null && _infiniteLoopElapsed >= motion.Duration)
            {
                BreakInfiniteLoop();
                return;
            }

            if (localTime >= loopEvt.endTime)
            {
                // endTime 도달할 때마다 startTime으로 되감기
                float loopDuration = loopEvt.endTime - loopEvt.startTime;
                _globalTime -= loopDuration;

                if (_currentState != null)
                {
                    float spd = motion?.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                    _currentState.Time = (motion?.ClipStartTime ?? 0f) + loopEvt.startTime * spd;
                }
            }
        }

        /// <summary>
        /// 외부에서 InfiniteLoop를 해제한다.
        /// 해제 후 모션은 endTime 이후 구간부터 정상 진행된다.
        /// </summary>
        public void BreakInfiniteLoop()
        {
            if (!_isInfiniteLooping) return;
            _isInfiniteLooping = false;
            _activeLoopEvent = null;
        }

        /// <summary>
        /// 현재 InfiniteLoop 상태인지 확인
        /// </summary>
        public bool IsInfiniteLooping => _isInfiniteLooping;

        private void ResetLoopState()
        {
            _activeLoopEvent = null;
            _loopRemainingCount = 0;
            _freezeTimer = 0f;
            _isFrozen = false;
            _isInfiniteLooping = false;
            _infiniteLoopElapsed = 0f;
        }

        private Motion GetCurrentMotion()
        {
            if (_currentMotionSet?.motions == null) return null;
            if (_currentMotionIndex < 0 || _currentMotionIndex >= _currentMotionSet.motions.Count) return null;
            return _currentMotionSet.motions[_currentMotionIndex];
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