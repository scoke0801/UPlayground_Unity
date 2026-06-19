using System;
using System.Collections.Generic;
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

        [SerializeField] protected AvatarMask _upperBodyMask;
        
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
        private float _lastLocalTime; // 이전 프레임의 로컬 타임
        private LoopEvent _activeLoopEvent;
        private HashSet<LoopEvent> _brokenLoopEvents = new HashSet<LoopEvent>(); // BreakInfiniteLoop로 해제된 이벤트 목록 (재진입 방지)
        private int _loopRemainingCount;
        private float _freezeTimer;
        private bool _isFrozen;
        private bool _isInfiniteLooping;
        private float _infiniteLoopElapsed; // InfiniteLoop 진입 후 경과 시간
        private int _infiniteLoopStageIndex = -1; // 현재까지 진입한 InfiniteLoop 순번 (0-based, 미진입 시 -1)
        
        public event Action OnMotionSetCompleted;
        public event Action<MotionSet, bool> OnMotionSetEnded;
        public AnimancerComponent GetAnimancerComponent() => _animator;
        public Animator GetAnimator => _animator.Animator;

        public readonly struct MotionPlaybackSnapshot
        {
            public readonly bool    IsValid;
            public readonly AnimKey Key;
            public readonly float   NormalizedTime;
            public readonly int     LayerIndex;
            public readonly float   GraphSpeed;

            public MotionPlaybackSnapshot(
                bool isValid,
                AnimKey key,
                float normalizedTime,
                int layerIndex,
                float graphSpeed)
            {
                IsValid        = isValid;
                Key            = key;
                NormalizedTime = normalizedTime;
                LayerIndex     = layerIndex;
                GraphSpeed     = graphSpeed;
            }

            public static MotionPlaybackSnapshot Empty =>
                new MotionPlaybackSnapshot(false, AnimKey.None, 0f, 0, 1f);
        }

        public readonly struct AnimationDebugSnapshot
        {
            public readonly bool IsValid;
            public readonly bool IsPlayingMotionSet;
            public readonly AnimKey Key;
            public readonly string MotionSetName;
            public readonly int MotionIndex;
            public readonly int MotionCount;
            public readonly string MotionName;
            public readonly string ClipName;
            public readonly float GlobalTime;
            public readonly float TotalDuration;
            public readonly float LocalTime;
            public readonly float MotionDuration;
            public readonly float NormalizedTime;
            public readonly int LayerIndex;
            public readonly float StateSpeed;
            public readonly float GraphSpeed;
            public readonly bool IsFrozen;
            public readonly bool IsInfiniteLooping;
            public readonly int InfiniteLoopStageIndex;
            public readonly string ActiveEvents;

            public AnimationDebugSnapshot(
                bool isValid,
                bool isPlayingMotionSet,
                AnimKey key,
                string motionSetName,
                int motionIndex,
                int motionCount,
                string motionName,
                string clipName,
                float globalTime,
                float totalDuration,
                float localTime,
                float motionDuration,
                float normalizedTime,
                int layerIndex,
                float stateSpeed,
                float graphSpeed,
                bool isFrozen,
                bool isInfiniteLooping,
                int infiniteLoopStageIndex,
                string activeEvents)
            {
                IsValid = isValid;
                IsPlayingMotionSet = isPlayingMotionSet;
                Key = key;
                MotionSetName = motionSetName;
                MotionIndex = motionIndex;
                MotionCount = motionCount;
                MotionName = motionName;
                ClipName = clipName;
                GlobalTime = globalTime;
                TotalDuration = totalDuration;
                LocalTime = localTime;
                MotionDuration = motionDuration;
                NormalizedTime = normalizedTime;
                LayerIndex = layerIndex;
                StateSpeed = stateSpeed;
                GraphSpeed = graphSpeed;
                IsFrozen = isFrozen;
                IsInfiniteLooping = isInfiniteLooping;
                InfiniteLoopStageIndex = infiniteLoopStageIndex;
                ActiveEvents = activeEvents;
            }

            public static AnimationDebugSnapshot Empty =>
                new AnimationDebugSnapshot(false, false, AnimKey.None, "-", -1, 0, "-", "-", 0f, 0f, 0f, 0f, 0f, 0, 0f, 1f, false, false, -1, "-");
        }
        
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

        /// <summary>
        /// fallbackMotionSet이 연결되어 있으면 공통 Humanoid 모션(8방향 등)을 사용할 수 있음.
        /// </summary>
        public bool HasFallbackMotionSet => _motionSet != null && _motionSet.fallbackMotionSet != null;
        public ActorAnimationMotionSet MotionSet => _motionSet;

        // ── 모션워프 지연 캐싱 키 구성용 접근자 ──
        // delta-warp 가 윈도우 총 루트모션을 (motionSetName, motionIndex, window) 키로 캐시할 때 사용.
        public string CurrentMotionSetName => _currentMotionSet?.motionSetName;
        public MotionSet CurrentMotionSet => _currentMotionSet;
        public float CurrentMotionSetTime => _globalTime;
        public int    CurrentMotionIndex   => _currentMotionIndex;
        public bool   IsPlayingMotionSet   => _isPlayingMotionSet;

        /// <summary>
        /// 현재 재생 중인 타임라인에서 현재 시점(_globalTime) 이후 처음으로 시작되는 T 이벤트까지
        /// 남은 시간(초)을 반환한다. globalStartTimeOffset은 PlayMotionSet 시점에 이미 계산되어 있다.
        /// 예: Danger Ring 수축 시간을 다음 Collision 이벤트까지로 자동 산출.
        /// 그래프 Speed ≈ 1 가정(히트스톱 등으로 어긋나도 호출부의 Collision 스냅이 보정).
        /// </summary>
        public bool TryGetTimeUntilNextEvent<T>(out float seconds) where T : MotionEventBase
        {
            seconds = 0f;
            if (!_isPlayingMotionSet || _currentMotionSet == null) return false;

            float now   = _globalTime;
            float best  = float.MaxValue;
            bool  found = false;

            void Consider(MotionEventBase evt)
            {
                if (evt is not T) return;
                float abs = evt.startTime + evt.globalStartTimeOffset;
                if (abs >= now && abs < best)
                {
                    best  = abs;
                    found = true;
                }
            }

            if (_currentMotionSet.globalEvents != null)
                foreach (var evt in _currentMotionSet.globalEvents) Consider(evt);

            if (_currentMotionSet.motions != null)
                foreach (var motion in _currentMotionSet.motions)
                    if (motion?.events != null)
                        foreach (var evt in motion.events) Consider(evt);

            if (found) seconds = Mathf.Max(0f, best - now);
            return found;
        }

        /// <summary>
        /// 두 이벤트 타입(T1, T2) 중 현재 시점 이후 더 먼저 시작되는 쪽까지 남은 시간(초)을 반환한다.
        /// 한쪽만 존재하면 그쪽 시간, 둘 다 없으면 false.
        /// 예: Danger Ring이 근접 공격(Collision)과 원거리 공격(SpawnProjectile)을 동일 규칙으로 목표 삼도록.
        /// </summary>
        public bool TryGetTimeUntilNextEvent<T1, T2>(out float seconds)
            where T1 : MotionEventBase
            where T2 : MotionEventBase
        {
            bool has1 = TryGetTimeUntilNextEvent<T1>(out float t1);
            bool has2 = TryGetTimeUntilNextEvent<T2>(out float t2);

            if (has1 && has2) seconds = Mathf.Min(t1, t2);
            else if (has1)    seconds = t1;
            else if (has2)    seconds = t2;
            else            { seconds = 0f; return false; }

            return true;
        }

        private void Awake()
        {
            _animator      = GetComponentInChildren<AnimancerComponent>();
            _eventExecutor = GetComponent<MotionEventExecutor>();

            if (_animator != null)
                ApplyAnimancerSetup(_animator);
        }

        /// <summary>
        /// AnimancerComponent에 공통 레이어 설정을 적용한다.
        /// 모델 교체 시 새 AnimancerComponent에도 재호출된다.
        /// </summary>
        protected void ApplyAnimancerSetup(AnimancerComponent animancer)
        {
            animancer.Layers[0].ApplyFootIK    = true;
            animancer.Layers[0].ApplyAnimatorIK = true;

            if (_upperBodyMask != null)
                animancer.Layers.SetMask(1, _upperBodyMask);
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

            // 새 MotionSet 유효성을 먼저 확인. 무효라면 기존 재생을 끊지 않고 즉시 null 반환.
            // (기존에는 StopMotionSet 후 무효를 발견해 재생 중이던 모션이 끊기는 버그가 있었다)
            MotionSet nextMotionSet = _motionSet != null ? _motionSet.GetMotionSet(key) : null;
            if (nextMotionSet == null || nextMotionSet.IsValid() == false)
            {
                return null;
            }

            // 기존 MotionSet이 재생 중이었다면 안전하게 정리
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                StopMotionSet();
            }

            _currentMotionSet = nextMotionSet;

            _currentMotionIndex = 0;
            _globalTime = 0f;
            _lastLocalTime = -0.001f;
            _isPlayingMotionSet = true;
            _lastPlayedKey = key;
            _infiniteLoopStageIndex = -1; // 새로운 MotionSet 시작 시에만 리셋

            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(_currentMotionSet);

            // 첫 번째 모션 재생
            PlayMotionAtIndex(0, fadeDuration, layerIndex);

            PlaySubAnimatorMotion(key, fadeDuration, layerIndex);
            return _currentState;
        }

        /// <summary>
        /// AnimKey 등록을 거치지 않고 외부 MotionSet 에셋을 직접 재생한다.
        /// 궁극기/시네마틱처럼 일반 상태 모션 테이블과 생명주기가 다른 재생 단위에서 사용한다.
        /// </summary>
        public AnimancerState PlayMotionSetAsset(MotionSetAsset asset, float fadeDuration = 0f, int layerIndex = 0)
        {
            return PlayMotionSet(asset != null ? asset.motionSet : null, fadeDuration, layerIndex);
        }

        public AnimancerState PlayMotionSet(MotionSet motionSet, float fadeDuration = 0f, int layerIndex = 0)
        {
            if (motionSet == null || !motionSet.IsValid())
                return null;

            if (_isPlayingMotionSet && _currentMotionSet != null)
                StopMotionSet();

            _currentMotionSet = motionSet;
            _currentMotionIndex = 0;
            _currentMotionLayerIndex = layerIndex;
            _globalTime = 0f;
            _lastLocalTime = -0.001f;
            _isPlayingMotionSet = true;
            _lastPlayedKey = AnimKey.None;
            _infiniteLoopStageIndex = -1;

            _eventExecutor?.PlayMotionSet(_currentMotionSet);
            PlayMotionAtIndex(0, fadeDuration, layerIndex);
            return _currentState;
        }

        protected void PlaySubAnimatorMotion(AnimKey key, float fadeDuration = 0.0f, int layerIndex = 0)
        {
            if (_subAnimator == null) return;

            _subAnimator.PlayMotion(key, fadeDuration, layerIndex);
        }

        public MotionPlaybackSnapshot CapturePlaybackSnapshot()
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null || _lastPlayedKey == AnimKey.None)
                return MotionPlaybackSnapshot.Empty;

            float total = _currentMotionSet.TotalDuration;
            float normalizedTime = total > 0f
                ? Mathf.Clamp01(_globalTime / total)
                : 0f;

            return new MotionPlaybackSnapshot(
                true,
                _lastPlayedKey,
                normalizedTime,
                _currentMotionLayerIndex,
                Speed);
        }

        public AnimationDebugSnapshot CaptureDebugSnapshot()
        {
            if (_animator == null)
                return AnimationDebugSnapshot.Empty;

            AnimancerState state = _currentState != null ? _currentState : _animator.States.Current;
            float graphSpeed = _animator.Graph.Speed;

            if (!_isPlayingMotionSet || _currentMotionSet == null)
            {
                string clipName = state?.Clip != null ? state.Clip.name : "-";
                return new AnimationDebugSnapshot(
                    state != null,
                    false,
                    AnimKey.None,
                    "-",
                    -1,
                    0,
                    "-",
                    clipName,
                    state?.Time ?? 0f,
                    state?.Length ?? 0f,
                    state?.Time ?? 0f,
                    state?.Length ?? 0f,
                    state?.NormalizedTime ?? 0f,
                    0,
                    state?.Speed ?? 0f,
                    graphSpeed,
                    false,
                    false,
                    -1,
                    "-");
            }

            Motion motion = GetCurrentMotion();
            string motionName = !string.IsNullOrEmpty(motion?.motionName)
                ? motion.motionName
                : motion?.motionClip != null ? motion.motionClip.name : "-";
            string currentClipName = state?.Clip != null
                ? state.Clip.name
                : motion?.motionClip != null ? motion.motionClip.name : "-";
            float totalDuration = _currentMotionSet.TotalDuration;
            float localTime = 0f;
            if (!_currentMotionSet.GetMotionAtTime(_globalTime, out _, out localTime))
                localTime = _lastLocalTime;

            return new AnimationDebugSnapshot(
                true,
                true,
                _lastPlayedKey,
                string.IsNullOrEmpty(_currentMotionSet.motionSetName) ? "-" : _currentMotionSet.motionSetName,
                _currentMotionIndex,
                _currentMotionSet.motions?.Count ?? 0,
                motionName,
                currentClipName,
                _globalTime,
                totalDuration,
                localTime,
                motion?.Duration ?? 0f,
                totalDuration > 0f ? Mathf.Clamp01(_globalTime / totalDuration) : 0f,
                _currentMotionLayerIndex,
                state?.Speed ?? 0f,
                graphSpeed,
                _isFrozen,
                _isInfiniteLooping,
                _infiniteLoopStageIndex,
                BuildActiveEventSummary());
        }

        private string BuildActiveEventSummary()
        {
            if (_currentMotionSet == null)
                return "-";

            List<MotionEventBase> activeEvents = _currentMotionSet.GetActiveEventsAt(_globalTime);
            if (activeEvents == null || activeEvents.Count == 0)
                return "-";

            List<string> labels = new List<string>(activeEvents.Count);
            foreach (MotionEventBase evt in activeEvents)
            {
                if (evt == null) continue;
                labels.Add(evt.GetShortLabel());
            }

            return labels.Count > 0 ? string.Join(", ", labels) : "-";
        }

        public MotionPlaybackSnapshot CaptureMovementPlaybackSnapshot()
        {
            if (!IsMovementPlaybackKey(_lastPlayedKey))
                return MotionPlaybackSnapshot.Empty;

            return CapturePlaybackSnapshot();
        }

        public static bool IsMovementPlaybackKey(AnimKey key)
        {
            return key switch
            {
                AnimKey.Walk or
                AnimKey.Run or
                AnimKey.Sprint or
                AnimKey.Dodge or
                AnimKey.Dash or
                AnimKey.Jump or
                AnimKey.Fall or
                AnimKey.Land or
                AnimKey.DoubleJump or
                AnimKey.Crouch_Idle or
                AnimKey.Crouch_Walk or
                AnimKey.Idle_To_Crouch or
                AnimKey.Crouch_To_Idle or
                AnimKey.Move_Stop_Walking or
                AnimKey.Move_Stop_Running or
                AnimKey.Move_Stop_Sprinting or
                AnimKey.Move_Stop_Walking_L45 or
                AnimKey.Move_Stop_Walking_R45 or
                AnimKey.Move_Stop_Running_L45 or
                AnimKey.Move_Stop_Running_R45 or
                AnimKey.Move_Stop_Sprinting_L45 or
                AnimKey.Move_Stop_Sprinting_R45 or
                AnimKey.Stand_Idle_Turn_L45 or
                AnimKey.Stand_Idle_Turn_R45 or
                AnimKey.Stand_Idle_Turn_L90 or
                AnimKey.Stand_Idle_Turn_R90 or
                AnimKey.Stand_Idle_Turn_180 or
                AnimKey.Walk_Turn_L45 or
                AnimKey.Walk_Turn_R45 or
                AnimKey.Walk_Turn_L90 or
                AnimKey.Walk_Turn_R90 or
                AnimKey.Walk_Turn_180 or
                AnimKey.Run_Turn_L45 or
                AnimKey.Run_Turn_R45 or
                AnimKey.Run_Turn_L90 or
                AnimKey.Run_Turn_R90 or
                AnimKey.Run_Turn_180 or
                AnimKey.Sprint_Turn_L45 or
                AnimKey.Sprint_Turn_R45 or
                AnimKey.Sprint_Turn_L90 or
                AnimKey.Sprint_Turn_R90 or
                AnimKey.Sprint_Turn_180 or
                AnimKey.Walk_Slow or
                AnimKey.Walk_Slow_B or
                AnimKey.Walk_Slow_B_L45 or
                AnimKey.Walk_Slow_B_R45 or
                AnimKey.Walk_Slow_F_L45 or
                AnimKey.Walk_Slow_F_R45 or
                AnimKey.Walk_Slow_F_L90 or
                AnimKey.Walk_Slow_F_R90 or
                AnimKey.Walk_B or
                AnimKey.Walk_B_L45 or
                AnimKey.Walk_B_R45 or
                AnimKey.Walk_F_L45 or
                AnimKey.Walk_F_R45 or
                AnimKey.Walk_F_L90 or
                AnimKey.Walk_F_R90 or
                AnimKey.Run_B or
                AnimKey.Run_B_L45 or
                AnimKey.Run_B_R45 or
                AnimKey.Run_F_L45 or
                AnimKey.Run_F_R45 or
                AnimKey.Run_F_L90 or
                AnimKey.Run_F_R90 => true,
                _ => false,
            };
        }

        public bool RestorePlaybackSnapshot(MotionPlaybackSnapshot snapshot, float fadeDuration = 0f)
        {
            if (!snapshot.IsValid || snapshot.Key == AnimKey.None)
                return false;

            var state = PlayMotion(snapshot.Key, fadeDuration, snapshot.LayerIndex);
            if (state == null || _currentMotionSet == null)
                return false;

            SeekCurrentMotionSet(Mathf.Clamp01(snapshot.NormalizedTime), snapshot.LayerIndex);
            Speed = snapshot.GraphSpeed > 0f ? snapshot.GraphSpeed : 1f;
            return true;
        }
        
        /// <summary>
        /// MotionSet 안전하게 정지
        /// </summary>
        public void StopMotionSet()
        {
            StopMotionSet(false);
        }

        private void StopMotionSet(bool completed)
        {
            if (!_isPlayingMotionSet) return;

            MotionSet endedMotionSet = _currentMotionSet;

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

            OnMotionSetEnded?.Invoke(endedMotionSet, completed);
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
                StopMotionSet(true);
                OnMotionSetCompleted?.Invoke();
                return;
            }

            // 현재 모션 인덱스 계산
            if (_currentMotionSet.GetMotionAtTime(_globalTime, out int newIndex, out float localTime))
            {
                if (newIndex != _currentMotionIndex)
                {
                    // 모션이 바뀌기 전, 이전 모션의 남은 구간 처리
                    var oldMotion = GetCurrentMotion();
                    if (oldMotion != null)
                    {
                        ProcessLoopEvents(_lastLocalTime, oldMotion.Duration);
                    }

                    _currentMotionIndex = newIndex;
                    PlayMotionAtIndex(_currentMotionIndex, 0f, _currentMotionLayerIndex);
                    
                    // 새 모션의 localTime 재계산 및 시작점 초기화
                    _currentMotionSet.GetMotionAtTime(_globalTime, out _, out localTime);
                    _lastLocalTime = 0f;
                }

                // ── Loop/Freeze 이벤트 감지 및 처리 ──
                ProcessLoopEvents(_lastLocalTime, localTime);
                
                // 최종 결과 반영
                if (_currentMotionSet.GetMotionAtTime(_globalTime, out _, out float finalLocalTime))
                {
                    _lastLocalTime = finalLocalTime;
                }
            }

            _eventExecutor?.UpdateTime(_globalTime);
        }

        /// <summary>
        /// 현재 모션의 LoopEvent를 감지하고 타임라인을 조작한다.
        /// </summary>
        private void ProcessLoopEvents(float start, float end)
        {
            var motion = GetCurrentMotion();
            if (motion?.events == null) return;

            foreach (var evt in motion.events)
            {
                if (evt is not LoopEvent loopEvt) continue;

                bool triggered = false;

                // 이미 활성화된 루프/무한루프라면 현재 시간이 endTime을 넘었는지만 체크 (구간 스킵 방지)
                if ((loopEvt.mode == LoopEventMode.Loop && _activeLoopEvent == loopEvt && _loopRemainingCount > 0) ||
                    (loopEvt.mode == LoopEventMode.InfiniteLoop && _isInfiniteLooping && _activeLoopEvent == loopEvt))
                {
                    if (end >= loopEvt.endTime) triggered = true;
                }
                else
                {
                    // 신규 진입 시에는 startTime을 기준으로 체크 (Freeze와 동일)
                    if (loopEvt.startTime >= start && loopEvt.startTime <= end) triggered = true;
                }

                if (triggered)
                {
                    switch (loopEvt.mode)
                    {
                        case LoopEventMode.Loop:
                            HandleLoopMode(loopEvt, end);
                            break;
                        case LoopEventMode.InfiniteLoop:
                            HandleInfiniteLoopMode(loopEvt, end);
                            break;
                        case LoopEventMode.Freeze:
                            HandleFreezeMode(loopEvt, end);
                            break;
                    }
                }
            }
        }

        private void HandleLoopMode(LoopEvent loopEvt, float localTime)
        {
            // 루프 카운터 초기화 (새로운 루프 이벤트 진입 시)
            if (_activeLoopEvent != loopEvt)
            {
                _activeLoopEvent = loopEvt;
                _loopRemainingCount = loopEvt.loopCount;
            }

            float duration = loopEvt.endTime - loopEvt.startTime;
            if (duration <= 0.0001f)
            {
                // 시작/종료 시간이 같을 경우, 루프 횟수만큼 즉시 차감하고 시간을 고정
                if (_loopRemainingCount > 0 && localTime >= loopEvt.startTime)
                {
                    _globalTime -= (localTime - loopEvt.startTime);
                    localTime = loopEvt.startTime;
                    _loopRemainingCount--;
                }
            }
            else
            {
                // 현재 시간이 루프 구간 안으로 들어올 때까지 반복 되감기 (미세 구간 대응)
                while (_loopRemainingCount > 0 && localTime >= loopEvt.endTime)
                {
                    _globalTime -= duration;
                    localTime -= duration;
                    _loopRemainingCount--;
                }
            }

            // Animancer 클립 시간도 되감기
            if (_currentState != null)
            {
                var motion = GetCurrentMotion();
                float spd = motion?.playbackSpeed > 0 ? motion.playbackSpeed : 1f;
                _currentState.Time = (motion?.ClipStartTime ?? 0f) + localTime * spd;
            }
        }

        private void HandleFreezeMode(LoopEvent loopEvt, float localTime)
        {
            if (_isFrozen) return;
            if (_activeLoopEvent == loopEvt) return; // 이미 처리된 Freeze (같은 프레임 중복 방지)

            // Freeze 시작
            _activeLoopEvent = loopEvt;
            _isFrozen = true;
            _freezeTimer = loopEvt.freezeDuration;

            if (_currentState != null)
                _currentState.Speed = 0f;
        }

        private void HandleInfiniteLoopMode(LoopEvent loopEvt, float localTime)
        {
            // BreakInfiniteLoop로 명시적으로 해제된 이벤트는 재진입하지 않는다
            if (_brokenLoopEvents.Contains(loopEvt)) return;

            bool isFirstEntry = !_isInfiniteLooping;
            if (isFirstEntry)
            {
                // 첫 도달: 무한 루프 상태 진입
                _activeLoopEvent = loopEvt;
                _isInfiniteLooping = true;
                _infiniteLoopElapsed = 0f;
                _infiniteLoopStageIndex++; // 스테이지 인덱스 증가 (0-based)

                Debug.Log($"InfiniteLoopStageIndex: {_infiniteLoopStageIndex}");
            }
            else
            {
                _infiniteLoopElapsed += _actor != null ? _actor.DeltaTime : Time.deltaTime;
            }

            float duration = loopEvt.endTime - loopEvt.startTime;
            if (duration <= 0.0001f)
            {
                // startTime = endTime인 경우:
                // _currentState.Time을 매 프레임 강제 세팅하면 Animancer의 deltaPosition 계산이
                // 흔들려 시각적 떨림이 발생한다. Speed = 0으로 포즈를 고정하는 방식을 사용한다.
                _globalTime -= (localTime - loopEvt.startTime);

                if (_currentState != null)
                {
                    // 첫 진입 시 한 번만 정확한 프레임으로 스냅
                    if (isFirstEntry)
                    {
                        var motion = GetCurrentMotion();
                        float spd = motion?.playbackSpeed > 0 ? motion.playbackSpeed : 1f;
                        _currentState.Time = (motion?.ClipStartTime ?? 0f) + loopEvt.startTime * spd;
                    }
                    _currentState.Speed = 0f;
                }
            }
            else
            {
                // 무한 루프이므로 구간 안으로 들어올 때까지 반복 되감기
                while (localTime >= loopEvt.endTime)
                {
                    _globalTime -= duration;
                    localTime -= duration;
                }

                if (_currentState != null)
                {
                    var motion = GetCurrentMotion();
                    float spd = motion?.playbackSpeed > 0 ? motion.playbackSpeed : 1f;
                    _currentState.Time = (motion?.ClipStartTime ?? 0f) + localTime * spd;
                }
            }
        }

        /// <summary>
        /// 외부에서 현재 InfiniteLoop를 해제한다.
        /// 해제 후 모션은 endTime 이후 구간부터 즉시 진행된다.
        /// </summary>
        public void BreakInfiniteLoop()
        {
            if (!_isInfiniteLooping || _activeLoopEvent == null) return;

            // Speed = 0으로 고정됐을 수 있으므로 정상 속도로 복원
            if (_currentState != null)
            {
                var motion = GetCurrentMotion();
                _currentState.Speed = motion?.playbackSpeed > 0 ? motion.playbackSpeed : 1f;
            }

            // 현재 루프의 종료 지점으로 시간을 점프시켜 대기 시간을 스킵한다.
            if (_currentMotionSet.GetMotionAtTime(_globalTime, out _, out float localTime))
            {
                float gap = _activeLoopEvent.endTime - localTime;
                if (gap > 0)
                {
                    _globalTime += gap;
                }
            }

            _brokenLoopEvents.Add(_activeLoopEvent);
            _isInfiniteLooping = false;
            _activeLoopEvent   = null;
        }

        /// <summary>
        /// 현재 모션에 있는 모든 InfiniteLoop 이벤트를 한 번에 차단하고,
        /// 루프 구간이 있다면 마지막 루프의 종료 지점으로 점프한다.
        /// </summary>
        public void BreakAllInfiniteLoops()
        {
            float lastLoopEndTime = -1f;
            var motion = GetCurrentMotion();
            
            if (motion?.events != null)
            {
                foreach (var evt in motion.events)
                {
                    if (evt is LoopEvent { mode: LoopEventMode.InfiniteLoop } loopEvt)
                    {
                        _brokenLoopEvents.Add(loopEvt);
                        if (loopEvt.endTime > lastLoopEndTime)
                            lastLoopEndTime = loopEvt.endTime;
                    }
                }
            }

            // 활성 루프가 있거나 루프 구간 내에 있다면 마지막 루프 끝으로 점프
            if (lastLoopEndTime > 0)
            {
                if (_currentMotionSet.GetMotionAtTime(_globalTime, out _, out float localTime))
                {
                    float gap = lastLoopEndTime - localTime;
                    if (gap > 0)
                    {
                        _globalTime += gap;
                    }
                }
            }

            // Speed = 0으로 고정됐을 수 있으므로 정상 속도로 복원
            if (_currentState != null)
            {
                _currentState.Speed = motion?.playbackSpeed > 0 ? motion.playbackSpeed : 1f;
            }

            _isInfiniteLooping = false;
            _activeLoopEvent   = null;
        }

        /// <summary>
        /// 현재 InfiniteLoop 상태인지 확인
        /// </summary>
        public bool IsInfiniteLooping => _isInfiniteLooping;

        /// <summary>
        /// 현재까지 진입한 InfiniteLoop 순번 (0-based).
        /// 첫 번째 루프 = 0, 두 번째 루프 = 1 ...
        /// 아직 어떤 루프에도 진입하지 않은 경우 -1.
        /// </summary>
        public int InfiniteLoopStageIndex => _infiniteLoopStageIndex;

        private void ResetLoopState()
        {
            _activeLoopEvent    = null;
            _brokenLoopEvents.Clear();
            _loopRemainingCount     = 0;
            _freezeTimer            = 0f;
            _isFrozen               = false;
            _isInfiniteLooping      = false;
            _infiniteLoopElapsed    = 0f;
            _infiniteLoopStageIndex = -1;
            _lastLocalTime          = -0.001f;
        }

        private void SeekCurrentMotionSet(float normalizedTime, int layerIndex)
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null)
                return;

            float totalDuration = _currentMotionSet.TotalDuration;
            if (totalDuration <= 0f)
                return;

            _globalTime = Mathf.Clamp(normalizedTime * totalDuration, 0f, Mathf.Max(0f, totalDuration - 0.001f));
            ResetLoopState();

            if (_currentMotionSet.GetMotionAtTime(_globalTime, out int motionIndex, out float localTime))
            {
                PlayMotionAtIndex(motionIndex, 0f, layerIndex);

                var motion = GetCurrentMotion();
                if (_currentState != null && motion != null)
                {
                    float playbackSpeed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                    _currentState.Time = motion.ClipStartTime + localTime * playbackSpeed;
                    _currentState.Speed = motion.playbackSpeed;
                }

                _lastLocalTime = localTime;
            }

            _eventExecutor?.SeekTo(_globalTime);
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
