using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation
{
    [RequireComponent(typeof(MotionEventExecutor))]
    public class ActorAnimator : MonoBehaviour
    {
        [Header("Actor Setting")]
        [SerializeField] private ActorAnimationMotionSet _motionSet;

        [SerializeField] protected AvatarMask _upperBodyMask;

        [Tooltip("상체 오버레이(이동하며 마시기 등)에 사용할 Animancer 레이어 인덱스. 이 레이어에 _upperBodyMask가 적용된다.")]
        [SerializeField] protected int _upperBodyLayerIndex = 1;

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
        protected GameplayTag _lastPlayedSlot;
        protected MotionSetAsset _currentMotionAsset;
        protected string _currentMotionDisplayKey = "-";
        protected bool _isPlayingMotionSet;
        private int _externalPreviewLockCount;

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
        private bool _suppressLoopEvents; // 현재 MotionSet의 Loop/Freeze 이벤트를 전부 무시 (다음 재생 시 자동 해제)
        private float _motionTimelineSpeed = 1f;
        private float _effectiveTimelineRate = 1f;
        private bool _completeAfterLateUpdate;
        private string _currentSectionId;
        private string _nextSectionOverrideId;
        
        public event Action OnMotionSetCompleted;
        public event Action<MotionSet, bool> OnMotionSetEnded;
        public event Action<MotionSet, MotionSetEndReason> OnMotionSetEndedWithReason;
        public AnimancerComponent GetAnimancerComponent() => _animator;
        public Animator GetAnimator => _animator.Animator;

        public readonly struct MotionPlaybackSnapshot
        {
            public readonly bool    IsValid;
            public readonly GameplayTag Slot;
            public readonly MotionSetAsset SourceAsset;
            public readonly string DisplayKey;
            public readonly float   NormalizedTime;
            public readonly int     LayerIndex;
            public readonly float   GraphSpeed;

            public MotionPlaybackSnapshot(
                bool isValid,
                GameplayTag slot,
                MotionSetAsset sourceAsset,
                string displayKey,
                float normalizedTime,
                int layerIndex,
                float graphSpeed)
            {
                IsValid        = isValid;
                Slot           = slot;
                SourceAsset    = sourceAsset;
                DisplayKey     = displayKey;
                NormalizedTime = normalizedTime;
                LayerIndex     = layerIndex;
                GraphSpeed     = graphSpeed;
            }

            public static MotionPlaybackSnapshot Empty =>
                new MotionPlaybackSnapshot(false, default, null, "-", 0f, 0, 1f);
        }

        public readonly struct AnimationDebugSnapshot
        {
            public readonly bool IsValid;
            public readonly bool IsPlayingMotionSet;
            public readonly GameplayTag Slot;
            public readonly MotionSetAsset SourceAsset;
            public readonly string DisplayKey;
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
                GameplayTag slot,
                MotionSetAsset sourceAsset,
                string displayKey,
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
                Slot = slot;
                SourceAsset = sourceAsset;
                DisplayKey = displayKey;
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
                new AnimationDebugSnapshot(false, false, default, null, "-", "-", -1, 0, "-", "-", 0f, 0f, 0f, 0f, 0f, 0, 0f, 1f, false, false, -1, "-");
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

        /// <summary>
        /// MotionSet 디렉터 시간 배율. Graph.Speed와 별개이며 공격속도처럼
        /// 모션 전환, 종료, 타임라인 이벤트까지 함께 빨라져야 하는 경우 사용한다.
        /// </summary>
        public float MotionTimelineSpeed
        {
            get => _motionTimelineSpeed;
            set
            {
                _motionTimelineSpeed = Mathf.Clamp(value, 0.1f, 5f);
                if (_subAnimator != null)
                    _subAnimator.MotionTimelineSpeed = _motionTimelineSpeed;
            }
        }

        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; } = Quaternion.identity;

        // Animator(Update)와 KCC(FixedUpdate)의 서로 다른 시간축을 잇는 누적 버퍼.
        // OnAnimatorMove가 여러 번 호출돼도 다음 KCC 스텝에서 정확히 한 번 소비한다.
        private RootMotionStepBuffer _rootMotionBuffer = RootMotionStepBuffer.Create();
        public Vector3 RootMotionStepDeltaPosition => _rootMotionBuffer.StepPosition;
        public Quaternion RootMotionStepDeltaRotation => _rootMotionBuffer.StepRotation;

        /// <summary>
        /// fallbackMotionSet이 연결되어 있으면 공통 Humanoid 모션(8방향 등)을 사용할 수 있음.
        /// </summary>
        public bool HasFallbackMotionSet => _motionSet != null && _motionSet.fallbackMotionSet != null;
        public ActorAnimationMotionSet MotionSet => _motionSet;

        // ── 에디터 프리뷰용 상체 오버레이 정보 접근자 ──
        // 애니메이션 에디터가 지정 레이어 프리뷰 시 마스크/레이어 인덱스를 재현하는 데 사용한다.
        public AvatarMask UpperBodyMask     => _upperBodyMask;
        public int        UpperBodyLayerIndex => _upperBodyLayerIndex;

        // ── 모션워프 지연 캐싱 키 구성용 접근자 ──
        // delta-warp 가 윈도우 총 루트모션을 (motionSetName, motionIndex, window) 키로 캐시할 때 사용.
        public string CurrentMotionSetName => _currentMotionSet?.motionSetName;
        public MotionSet CurrentMotionSet => _currentMotionSet;
        public float CurrentMotionSetTime => _globalTime;
        public int    CurrentMotionIndex   => _currentMotionIndex;
        public bool   IsPlayingMotionSet   => _isPlayingMotionSet;
        public bool   IsExternalPreviewActive => _externalPreviewLockCount > 0;
        public string CurrentSectionId => _currentSectionId;

        /// <summary>
        /// 현재 재생 중인 타임라인에서 현재 시점(_globalTime) 이후 처음으로 시작되는 T 이벤트까지
        /// 남은 시간(초)을 반환한다. globalStartTimeOffset은 PlayMotionSet 시점에 이미 계산되어 있다.
        /// 예: Danger Ring 수축 시간을 다음 Collision 이벤트까지로 자동 산출.
        /// MotionTimelineSpeed를 반영한 실제 남은 시간으로 반환한다.
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

            if (found) seconds = Mathf.Max(0f, best - now) / Mathf.Max(0.1f, _motionTimelineSpeed);
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
            if (animancer.Animator != null)
                animancer.Animator.applyRootMotion = true;

            animancer.Layers[0].ApplyFootIK    = true;
            animancer.Layers[0].ApplyAnimatorIK = true;

            if (_upperBodyMask != null)
                animancer.Layers.SetMask(Mathf.Max(1, _upperBodyLayerIndex), _upperBodyMask);
        }

        /// <summary>
        /// 재생할 Base 타임라인 레이어를 결정한다.
        /// MotionSet에 baseLayerIndex가 지정돼 있으면(>0) 그 레이어를, 아니면 호출측이 요청한 레이어를 쓴다.
        /// </summary>
        protected int ResolveBaseLayerIndex(MotionSet motionSet, int requestedLayerIndex)
            => motionSet != null && motionSet.baseLayerIndex > 0
                ? motionSet.baseLayerIndex
                : requestedLayerIndex;

        /// <summary>
        /// 오버레이 레이어(>0)에 마스크가 없으면 액터 상체 마스크를 재사용해, 마스크된 부위만 재생되도록 한다.
        /// baseLayerIndex가 액터 상체 레이어와 달라도 같은 마스크를 적용한다.
        /// </summary>
        protected void EnsureOverlayMask(int layerIndex)
        {
            if (_animator == null || layerIndex <= 0 || _upperBodyMask == null)
                return;
            AnimancerLayer layer = _animator.Layers[layerIndex];
            if (layer.Mask == null)
                layer.Mask = _upperBodyMask;
        }

        /// <summary>
        /// AnimKey에 해당하는 MotionSet을 해석한다.
        /// 액터별로 모션 소스가 다르므로(플레이어는 무기별 세트) 하위 클래스가 재정의한다.
        /// </summary>
        protected virtual MotionSetAsset ResolveMotionSetAsset(GameplayTag slot)
            => _motionSet != null ? _motionSet.GetMotionSetAsset(slot) : null;

        protected virtual MotionSet ResolveMotionSet(GameplayTag slot)
            => ResolveMotionSetAsset(slot)?.motionSet;

        protected virtual MotionSetAsset ResolveAbilityMotionAsset(
            AbilityMotionKey key) =>
            _motionSet != null
                ? _motionSet.GetAbilityMotionAsset(key)
                : null;

        public bool TryResolveAbilityMotion(
            AbilityMotionKey key,
            out MotionSetAsset asset)
        {
            MotionSetAsset resolved = ResolveAbilityMotionAsset(key);
            // 실패하면 out을 비운다. 반환값을 보지 않고 out만 쓰는 호출부에
            // 재생 불가능한 에셋(빈 motionSet, 깨진 섹션 레이아웃)이 흘러가면 안 된다.
            if (!HasMotion(resolved))
            {
                asset = null;
                return false;
            }

            asset = resolved;
            return true;
        }


        public virtual void Init(GameActor actor)
        {
            _actor = actor;
        }

        private void Update()
        {
            if (IsExternalPreviewActive)
                return;

            // 타임라인 업데이트 (MotionSet 재생 중일 때만)
            if (_isPlayingMotionSet)
            {
                UpdateTimeline();
            }
        }

        private void LateUpdate()
        {
            if (IsExternalPreviewActive)
                return;

            // Animancer(본) 평가가 끝난 이후 시점.
            if (_isPlayingMotionSet)
            {
                // 이벤트 발화는 _globalTime(손수 누적한 디렉터 시계)이 아니라
                // 실제 평가된 포즈 시간으로 판정한다. 그래프가 이미 이번 프레임 포즈를
                // 만든 LateUpdate에서 _currentState.Time을 역산하므로, 히트스톱·타임스케일·
                // 프레임 변동과 무관하게 이벤트가 항상 동일한 모션 시점에 발화한다.
                _eventExecutor?.UpdateTime(
                    _completeAfterLateUpdate
                        ? _globalTime
                        : _currentMotionSet != null && _currentMotionSet.HasPlaybackLayers
                        ? _globalTime
                        : GetPoseDrivenGlobalTime());

                // 이번 프레임 발화가 결정된 공간 샘플링 이벤트(SlashVFX 등)를 여기서 실행해,
                // 블레이드 본을 항상 이번 프레임 최종 포즈로 샘플링한다.
                _eventExecutor?.FlushDeferredEvents();

                if (_completeAfterLateUpdate && _isPlayingMotionSet)
                    CompleteMotionSet();
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

        public virtual bool HasMotion(GameplayTag slot, bool checkWeapon = false)
        {
            MotionSetAsset asset = ResolveMotionSetAsset(slot);
            return asset != null &&
                   asset.motionSet != null &&
                   asset.motionSet.IsValid() &&
                   MotionTimelineResolver.TryValidateSectionLayout(asset.motionSet, out _);
        }

        public bool HasMotion(MotionSetAsset asset) =>
            asset != null &&
            asset.motionSet != null &&
            asset.motionSet.IsValid() &&
            MotionTimelineResolver.TryValidateSectionLayout(asset.motionSet, out _);

        public virtual AnimancerState PlayMotion(GameplayTag slot, float fadeDuration = 0f, int layerIndex = 0)
        {
            MotionSetAsset asset = ResolveMotionSetAsset(slot);
            AnimancerState state = PlayResolvedMotion(
                asset,
                asset != null ? asset.motionSet : null,
                slot,
                slot.TagName,
                fadeDuration,
                layerIndex,
                out bool started);
            if (started)
                _subAnimator?.PlayMotion(slot, fadeDuration, _currentMotionLayerIndex);
            return state;
        }

        /// <summary>
        /// 의미 슬롯 등록을 거치지 않고 외부 MotionSet 에셋을 직접 재생한다.
        /// Payload·궁극기·시네마틱처럼 일반 상태 모션 테이블과 생명주기가 다른 재생 단위에서 사용한다.
        /// </summary>
        public AnimancerState PlayMotion(MotionSetAsset asset, float fadeDuration = 0f, int layerIndex = 0)
        {
            AnimancerState state = PlayResolvedMotion(
                asset,
                asset != null ? asset.motionSet : null,
                default,
                asset != null ? asset.name : "-",
                fadeDuration,
                layerIndex,
                out bool started);
            if (started)
                _subAnimator?.PlayMotion(asset, fadeDuration, _currentMotionLayerIndex);
            return state;
        }

        // 기존 외부 호출 호환용. 신규 코드는 PlayMotion(MotionSetAsset)을 사용한다.
        public AnimancerState PlayMotionSetAsset(MotionSetAsset asset, float fadeDuration = 0f, int layerIndex = 0)
        {
            return PlayMotion(asset, fadeDuration, layerIndex);
        }

        public AnimancerState PlayMotionSet(MotionSet motionSet, float fadeDuration = 0f, int layerIndex = 0)
        {
            return PlayResolvedMotion(
                null,
                motionSet,
                default,
                !string.IsNullOrEmpty(motionSet?.motionSetName) ? motionSet.motionSetName : "Direct MotionSet",
                fadeDuration,
                layerIndex,
                out _);
        }

        /// <summary>
        /// 태그 슬롯·에셋 직접 참조·런타임 MotionSet 재생이 공유하는 단일 시작 경로다.
        /// </summary>
        protected AnimancerState PlayResolvedMotion(
            MotionSetAsset sourceAsset,
            MotionSet motionSet,
            GameplayTag slot,
            string displayKey,
            float fadeDuration,
            int layerIndex,
            out bool started)
        {
            started = false;
            if (IsExternalPreviewActive)
                return null;
            if (motionSet == null || !motionSet.IsValid())
                return null;
            if (!MotionTimelineResolver.TryValidateSectionLayout(motionSet, out string sectionError))
            {
                Debug.LogError(
                    $"[{name}] MotionSet '{displayKey}' 재생 거부: {sectionError}",
                    sourceAsset != null ? sourceAsset : this);
                return null;
            }

            bool sameSource = sourceAsset != null
                ? _currentMotionAsset == sourceAsset
                : _currentMotionAsset == null && ReferenceEquals(_currentMotionSet, motionSet);
            if (_isPlayingMotionSet
                && sameSource
                && _lastPlayedSlot == slot)
                return GetRepresentativePlaybackState();

            int effectiveLayer = ResolveBaseLayerIndex(motionSet, layerIndex);
            float resolvedFade = Mathf.Max(0f, fadeDuration);
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                bool preserveBaseLayer =
                    resolvedFade > 0f &&
                    _currentMotionLayerIndex == effectiveLayer;
                StopMotionSet(
                    MotionSetEndReason.Interrupted,
                    resolvedFade,
                    preserveBaseLayer);
            }

            _currentMotionAsset = sourceAsset;
            _currentMotionSet = motionSet;
            _currentMotionIndex = -1;
            _globalTime = 0f;
            _lastLocalTime = -0.001f;
            _isPlayingMotionSet = true;
            _completeAfterLateUpdate = false;
            _effectiveTimelineRate = 1f;
            _lastPlayedSlot = slot;
            _currentMotionDisplayKey = string.IsNullOrEmpty(displayKey) ? "-" : displayKey;
            _infiniteLoopStageIndex = -1;
            _currentSectionId = null;
            _nextSectionOverrideId = null;

            EnsureOverlayMask(effectiveLayer);
            _currentMotionLayerIndex = effectiveLayer;

            _eventExecutor?.PlayMotionSet(_currentMotionSet);
            if (motionSet.GetMotionAtTime(0f, out int initialIndex, out _))
                PlayMotionAtIndex(initialIndex, resolvedFade, effectiveLayer);
            StartPlaybackLayers(resolvedFade);
            if (_currentState == null && _motionLayerPlaybacks.Count == 0)
            {
                StopMotionSet(MotionSetEndReason.Invalidated);
                return null;
            }

            UpdateCurrentSection(0f, true);
            started = true;
            return GetRepresentativePlaybackState();
        }

        /// <summary>
        /// 외부 저작 도구가 같은 Animancer 그래프를 직접 제어하는 동안 런타임 모션 재생을 막는다.
        /// 중첩 호출을 허용하며 마지막 소유자가 해제할 때 정상 런타임 제어로 돌아간다.
        /// </summary>
        public void BeginExternalPreview()
        {
            _externalPreviewLockCount++;
            if (_externalPreviewLockCount != 1)
                return;

            if (_isPlayingMotionSet)
                StopMotionSet(MotionSetEndReason.Interrupted);
        }

        public void EndExternalPreview()
        {
            _externalPreviewLockCount = Mathf.Max(0, _externalPreviewLockCount - 1);
        }

        public bool TryPlay(in MotionPlaybackRequest request)
        {
            if (request.asset?.motionSet == null)
                return false;

            float blendIn = request.blendInOverride ?? 0f;
            AnimancerState state = PlayMotion(request.asset, blendIn);
            if (state == null)
                return false;

            _motionTimelineSpeed = request.playRate > 0f ? request.playRate : 1f;
            if (string.IsNullOrEmpty(request.startSectionId) ||
                TryJumpToSection(request.startSectionId))
                return true;

            StopMotionSet(MotionSetEndReason.Invalidated);
            return false;
        }

        public bool TryGetCurrentSection(out string sectionId)
        {
            sectionId = _currentSectionId;
            return !string.IsNullOrEmpty(sectionId);
        }

        public bool TrySetNextSection(string fromSectionId, string nextSectionId)
        {
            if (_currentMotionSet == null ||
                _currentSectionId != fromSectionId ||
                !MotionTimelineResolver.TryGetSection(_currentMotionSet, nextSectionId, out _))
                return false;
            _nextSectionOverrideId = nextSectionId;
            return true;
        }

        public bool TryJumpToSection(string sectionId)
        {
            if (!_isPlayingMotionSet ||
                !MotionTimelineResolver.TryGetSection(_currentMotionSet, sectionId, out MotionSectionRange range))
                return false;

            _eventExecutor?.ExitActiveEvents();
            SeekCurrentMotionSetTime(range.startTime, _currentMotionLayerIndex);
            _currentSectionId = sectionId;
            _nextSectionOverrideId = null;
            _eventExecutor?.EnterSection();
            return true;
        }


        // ── 상체 오버레이 레이어 ──
        // 하체 로코모션(Layer 0 디렉터)을 그대로 유지한 채, 상체 마스크가 적용된 레이어에만
        // 단발 모션을 얹는다. "이동하며 마시기"처럼 하체는 이동, 상체는 별도 동작이 필요할 때 사용한다.
        // MotionSet 디렉터(_currentMotionSet/_globalTime/이벤트)를 건드리지 않으므로, 이벤트·타임라인이
        // 없는 단순 단일 클립 모션(Drink 등) 전용이다. 완료 판정은 반환된 길이로 호출측이 타이머 처리한다.

        /// <summary>
        /// 상체 오버레이 레이어에 AnimKey의 첫 모션 클립을 1회 재생하고 실제 재생 길이(초)를 반환한다.
        /// 재생에 실패하면 0을 반환한다. 디렉터를 사용하지 않으므로 OnMotionSetCompleted는 발화하지 않는다.
        /// </summary>
        // 직전에 상체 오버레이를 재생한 레이어 인덱스. StopUpperBodyOverlay가 같은 레이어를 끄도록 기억한다.
        private int _lastUpperBodyOverlayLayer = -1;

        public float PlayUpperBodyOverlay(GameplayTag slot, float fadeDuration = 0.15f)
        {
            if (IsExternalPreviewActive)
                return 0f;

            return PlayUpperBodyOverlay(ResolveMotionSet(slot), fadeDuration, slot);
        }

        private float PlayUpperBodyOverlay(MotionSet set, float fadeDuration, GameplayTag slot)
        {
            if (_animator == null)
                return 0f;

            if (set == null || !set.IsValid())
                return 0f;

            Motion motion = (set.motions != null && set.motions.Count > 0) ? set.motions[0] : null;
            if (motion == null || !motion.IsValid())
                return 0f;

            // MotionSet에 baseLayerIndex가 지정돼 있으면 그 레이어를, 아니면 액터 상체 레이어를 쓴다.
            int layerIndex = set.baseLayerIndex > 0
                ? set.baseLayerIndex
                : Mathf.Max(1, _upperBodyLayerIndex);

            AnimancerLayer layer = _animator.Layers[layerIndex];
            if (layer.Mask == null)
            {
                if (_upperBodyMask != null)
                    layer.Mask = _upperBodyMask; // 지정 레이어에도 상체 마스크 재사용
                else
                    Debug.LogWarning(
                        $"[{name}] 상체 오버레이 레이어 {layerIndex}에 AvatarMask가 없어 전신을 덮습니다. " +
                        "ActorAnimator의 Upper Body Mask를 할당하세요.", this);
            }

            _lastUpperBodyOverlayLayer = layerIndex;

            layer.StartFade(1f, fadeDuration);
            AnimancerState state = layer.Play(motion.motionClip, fadeDuration);
            state.Time  = motion.ClipStartTime;
            state.Speed = motion.playbackSpeed;
            state.Events(this).OnEnd = null; // 종료/전환은 호출측 타이머로 판단

            if (_subAnimator != null)
            {
                _subAnimator.PlayUpperBodyOverlay(slot, fadeDuration);
            }

            return motion.Duration; // 재생 구간·속도가 반영된 실제 재생 길이
        }

        /// <summary>
        /// 상체 오버레이 레이어 가중치를 0으로 페이드해 하체(Layer 0) 포즈로 복귀시킨다.
        /// </summary>
        public void StopUpperBodyOverlay(float fadeDuration = 0.15f)
        {
            if (IsExternalPreviewActive)
                return;

            if (_animator == null)
                return;

            // 재생 시 사용한 레이어를 끈다. 미재생/모름이면 액터 상체 레이어로 폴백.
            int layerIndex = _lastUpperBodyOverlayLayer > 0
                ? _lastUpperBodyOverlayLayer
                : Mathf.Max(1, _upperBodyLayerIndex);
            if (layerIndex < _animator.Layers.Count)
                _animator.Layers[layerIndex].StartFade(0f, fadeDuration);

            _subAnimator?.StopUpperBodyOverlay(fadeDuration);
        }

        public MotionPlaybackSnapshot CapturePlaybackSnapshot()
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null)
                return MotionPlaybackSnapshot.Empty;

            float total = _currentMotionSet.TotalDuration;
            float normalizedTime = total > 0f
                ? Mathf.Clamp01(_globalTime / total)
                : 0f;

            return new MotionPlaybackSnapshot(
                true,
                _lastPlayedSlot,
                _currentMotionAsset,
                _currentMotionDisplayKey,
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
                    default,
                    null,
                    "Clip",
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
                _lastPlayedSlot,
                _currentMotionAsset,
                _currentMotionDisplayKey,
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
            if (!IsMovementPlaybackSlot(_lastPlayedSlot))
                return MotionPlaybackSnapshot.Empty;

            return CapturePlaybackSnapshot();
        }


        public static bool IsMovementPlaybackSlot(GameplayTag slot)
        {
            return slot.IsChildOf(MotionTags.Locomotion)
                   || slot.IsChildOf(MotionTags.Stop)
                   || slot.IsChildOf(MotionTags.Turn)
                   || slot.IsChildOf(MotionTags.Air)
                   || slot.IsChildOf(MotionTags.Crouch)
                   || slot == MotionTags.Dodge
                   || slot == MotionTags.Dash;
        }

        public bool RestorePlaybackSnapshot(MotionPlaybackSnapshot snapshot, float fadeDuration = 0f)
        {
            if (!snapshot.IsValid
                || (snapshot.SourceAsset == null
                    && !snapshot.Slot.IsValid()))
                return false;

            AnimancerState state = snapshot.SourceAsset != null
                ? PlayMotion(snapshot.SourceAsset, fadeDuration, snapshot.LayerIndex)
                : PlayMotion(snapshot.Slot, fadeDuration, snapshot.LayerIndex);
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
            StopMotionSet(MotionSetEndReason.Stopped);
        }

        public void StopMotionSet(float blendOutDuration)
        {
            StopMotionSet(MotionSetEndReason.Stopped, blendOutDuration);
        }

        public void StopMotionSet(float? blendOutOverride)
        {
            StopMotionSet(MotionSetEndReason.Stopped, blendOutOverride);
        }

        private void StopMotionSet(
            MotionSetEndReason reason,
            float? blendOutOverride = null,
            bool preserveBaseLayer = false)
        {
            if (!_isPlayingMotionSet) return;

            MotionSet endedMotionSet = _currentMotionSet;
            bool completed = reason == MotionSetEndReason.Completed;
            float blendOut = Mathf.Max(0f, blendOutOverride ?? 0f);

            // 이벤트 강제 종료
            _eventExecutor?.Stop();
            // 정상 완료는 상태 전환이 다음 모션을 재생할 때까지 마지막 Base 포즈를 유지한다.
            // 명시적 Hold Section은 재생 상태 자체를 유지하므로 이 경로에 들어오지 않는다.
            if (!completed && !preserveBaseLayer)
                FadeOrStopLayer(_currentMotionLayerIndex, blendOut);

            // Base 마지막 포즈를 유지하더라도 병렬 레이어까지 남겨 두면 다음 상태를 덮는다.
            // 완료 포즈 유지와 추가 트랙 수명은 분리해서 처리한다.
            StopPlaybackLayers(blendOut);

            _isPlayingMotionSet = false;
            _currentMotionSet = null;
            _currentState = null;
            _globalTime = 0f;
            _currentMotionIndex = 0;
            _currentSectionId = null;
            _nextSectionOverrideId = null;
            _completeAfterLateUpdate = false;
            _effectiveTimelineRate = 1f;
            ResetLoopState();
            
            if (_subAnimator != null)
            {
                _subAnimator.StopMotionSet();
            }

            OnMotionSetEnded?.Invoke(endedMotionSet, completed);
            OnMotionSetEndedWithReason?.Invoke(endedMotionSet, reason);
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
        public virtual float GetMotionSetDuration(GameplayTag slot) =>
            ResolveMotionSetAsset(slot)?.motionSet?.TotalDuration ?? 0f;
        
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

        // applyRootMotion은 ApplyAnimancerSetup에서 상시 true로 고정한다.
        // 상태별로 껐다 켜던 ApplyRootMotion(bool)은 제거됐다 — 런타임에 이걸 끄면
        // OnAnimatorMove가 델타를 내놓지 않아 루트모션 기반 상태 전체가 조용히 정지한다.

        /// <summary>KCC 물리 스텝이 시작될 때 누적된 루트모션을 소비 스냅샷으로 옮긴다.</summary>
        public void BeginRootMotionStep()
        {
            _rootMotionBuffer.BeginStep();
        }

        /// <summary>KCC를 사용하지 않는 잔류 애니메이터가 누적 델타를 한 번만 꺼낸다.</summary>
        public void ConsumePendingRootMotion(out Vector3 position, out Quaternion rotation)
        {
            _rootMotionBuffer.ConsumePending(out position, out rotation);
        }

        /// <summary>KCC 물리 스텝 종료 후 같은 델타가 재사용되지 않도록 비운다.</summary>
        public void EndRootMotionStep()
        {
            _rootMotionBuffer.EndStep();
        }

        public Vector3 GetRootMotionStepVelocity(float deltaTime)
            => deltaTime > 0.000001f
                ? RootMotionStepDeltaPosition / deltaTime
                : Vector3.zero;

        public void FlushRootMotion()
        {
            DeltaPosition = Vector3.zero;
            DeltaRotation = Quaternion.identity;
            _rootMotionBuffer.Flush();
        }

        private void UpdateTimeline()
        {
            if (!_isPlayingMotionSet ||
                _currentMotionSet == null ||
                _completeAfterLateUpdate)
                return;

            float deltaTime = _actor != null ? _actor.DeltaTime : Time.deltaTime;

            // ── Freeze 중이면 시간을 흘리지 않고 타이머만 소모 ──
            if (_isFrozen)
            {
                SetPlaybackLayersPaused(true);
                _freezeTimer -= deltaTime;
                if (_freezeTimer <= 0f)
                {
                    _isFrozen = false;
                    // Freeze 해제 시 애니메이션 속도 복원
                    ApplyCurrentPlaybackSpeed();
                    SetPlaybackLayersPaused(false);
                }
                // Freeze 중 이벤트 갱신은 LateUpdate에서 포즈 시간으로 수행된다.
                // (Speed=0이므로 포즈가 멈춰 이벤트도 자연히 정지한다)
                return;
            }

            float timelineNormalized = _currentMotionSet.TotalDuration > 0f
                ? Mathf.Clamp01(_globalTime / _currentMotionSet.TotalDuration)
                : 0f;
            float playbackRateCurve = EvaluateCurve(
                MotionCurveChannel.PlaybackRate,
                null,
                timelineNormalized,
                1f);
            float stretchRate = MotionTimelineResolver.EvaluateTimeStretchRate(
                _currentMotionSet,
                _globalTime,
                _motionTimelineSpeed);
            float stretchCurve = EvaluateCurve(
                MotionCurveChannel.TimeStretch,
                null,
                timelineNormalized,
                1f);
            _effectiveTimelineRate = stretchRate *
                                     Mathf.Max(0f, playbackRateCurve) *
                                     Mathf.Max(0f, stretchCurve);
            ApplyCurrentPlaybackSpeed();
            _globalTime += deltaTime * _effectiveTimelineRate;
            if (HandleSectionBoundary())
                return;

            // MotionSet 종료 체크
            if (_globalTime >= _currentMotionSet.TotalDuration)
            {
                ScheduleCompletion(_currentMotionSet.TotalDuration);
                return;
            }
            UpdatePlaybackLayers(_globalTime);

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
                    PlayMotionAtIndex(
                        _currentMotionIndex,
                        _currentMotionSet.InternalBlendDuration,
                        _currentMotionLayerIndex);
                    ApplyCurrentPlaybackSpeed();
                    
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

            // 이벤트 발화는 LateUpdate에서 실제 포즈 시간으로 판정한다(디렉터/포즈 클럭 분리).
        }

        bool HandleSectionBoundary()
        {
            if (string.IsNullOrEmpty(_currentSectionId) ||
                !MotionTimelineResolver.TryGetSection(
                    _currentMotionSet,
                    _currentSectionId,
                    out MotionSectionRange range) ||
                _globalTime < range.endTime)
                return false;

            MotionSection section = range.section;
            switch (section.endPolicy)
            {
                case MotionSectionEndPolicy.Stop:
                    ScheduleCompletion(range.endTime);
                    return true;

                case MotionSectionEndPolicy.Hold:
                    _globalTime = Mathf.Max(range.startTime, range.endTime - 0.001f);
                    if (_currentState != null)
                        _currentState.Speed = 0f;
                    SetPlaybackLayersPaused(true);
                    return true;

                case MotionSectionEndPolicy.LoopSelf:
                    return TryJumpToSection(section.id);
            }

            string nextId = !string.IsNullOrEmpty(_nextSectionOverrideId)
                ? _nextSectionOverrideId
                : MotionTimelineResolver.ResolveDefaultNextSectionId(_currentMotionSet, section);
            _nextSectionOverrideId = null;
            if (string.IsNullOrEmpty(nextId) ||
                !MotionTimelineResolver.TryGetSection(
                    _currentMotionSet,
                    nextId,
                    out MotionSectionRange nextRange))
                return false;

            if (Mathf.Abs(nextRange.startTime - range.endTime) <= 0.001f)
            {
                _currentSectionId = nextId;
                _eventExecutor?.EnterSection();
                return false;
            }
            return TryJumpToSection(nextId);
        }

        void CompleteMotionSet()
        {
            StopMotionSet(MotionSetEndReason.Completed);
            OnMotionSetCompleted?.Invoke();
        }

        void ScheduleCompletion(float boundaryTime)
        {
            if (_currentMotionSet == null || _completeAfterLateUpdate)
                return;

            _globalTime = Mathf.Clamp(
                boundaryTime,
                0f,
                Mathf.Max(0f, _currentMotionSet.TotalDuration));
            float sampleTime = Mathf.Max(0f, _globalTime - 0.0001f);
            SampleBasePose(sampleTime);
            UpdatePlaybackLayers(sampleTime);
            SetPlaybackLayersPaused(true);
            _completeAfterLateUpdate = true;
        }

        void SampleBasePose(float globalTime)
        {
            if (!_currentMotionSet.GetMotionAtTime(
                    globalTime,
                    out int motionIndex,
                    out float localTime))
                return;
            if (_currentState == null || motionIndex != _currentMotionIndex)
                PlayMotionAtIndex(motionIndex, 0f, _currentMotionLayerIndex);

            Motion motion = GetCurrentMotion();
            if (_currentState == null || motion == null)
                return;

            float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            _currentState.Time = motion.ClipStartTime + localTime * speed;
            _currentState.Speed = 0f;
        }

        void UpdateCurrentSection(float time, bool enter)
        {
            if (MotionTimelineResolver.TryGetSectionAtTime(
                    _currentMotionSet,
                    time,
                    out MotionSectionRange range))
            {
                _currentSectionId = range.section.id;
                if (enter)
                    _eventExecutor?.EnterSection();
            }
            else
            {
                _currentSectionId = null;
            }
        }

        /// <summary>
        /// 이벤트 발화용 글로벌 시간을 실제 Animancer 포즈(_currentState.Time)에서 역산한다.
        /// 손수 누적한 _globalTime(디렉터 클럭)은 Graph.Speed와 _localTimeScale이 어긋나는
        /// 히트스톱/타임스케일 구간에서 실제 포즈와 드리프트한다. 이 메서드는 그래프가 실제로
        /// 평가한 포즈에 잠긴 시간을 돌려주므로, 이벤트가 항상 동일한 모션 시점에 발화한다.
        /// 반드시 그래프 평가가 끝난 LateUpdate에서 호출해야 이번 프레임 최종 포즈를 반영한다.
        /// 오프셋 규칙은 MotionEventExecutor.CalculateEventOffsets(이전 모션 Duration 누적)와 동일하다.
        /// </summary>
        private float GetPoseDrivenGlobalTime()
        {
            if (_currentState == null || _currentMotionSet == null)
                return _globalTime;

            Motion motion = GetCurrentMotion();
            if (motion == null)
                return _globalTime;

            float spd = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            // 클립 로컬 시간 → 모션 로컬 시간(재생 속도 역보정). 미세 오버슈트는 [0, Duration]으로 클램프.
            float localPose = (_currentState.Time - motion.ClipStartTime) / spd;
            localPose = Mathf.Clamp(localPose, 0f, motion.Duration);

            return GetAccumulatedDurationBeforeCurrentMotion() + localPose;
        }

        /// <summary>
        /// 현재 모션 인덱스 이전 모션들의 Duration 합. 이벤트 오프셋 누적 규칙과 동일하다.
        /// </summary>
        private float GetAccumulatedDurationBeforeCurrentMotion()
        {
            if (_currentMotionSet?.motions == null)
                return 0f;

            float accumulated = 0f;
            int index = 0;
            foreach (Motion motion in _currentMotionSet.motions)
            {
                if (index >= _currentMotionIndex)
                    break;
                if (motion != null)
                    accumulated += motion.Duration;
                index++;
            }
            return accumulated;
        }

        /// <summary>
        /// 현재 모션의 LoopEvent를 감지하고 타임라인을 조작한다.
        /// </summary>
        private void ProcessLoopEvents(float start, float end)
        {
            if (_suppressLoopEvents) return;

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
        /// 현재 재생 중인 MotionSet의 Loop/Freeze/InfiniteLoop 이벤트를 전부 무시한다.
        /// 시간 점프 없이 타임라인이 그대로 통과하며, 다음 MotionSet 재생 시 자동 해제된다.
        /// 재생 시작 시 초기화되므로 반드시 PlayMotion 직후에 호출할 것.
        /// </summary>
        public void SuppressLoopEvents()
        {
            _suppressLoopEvents = true;
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
            _suppressLoopEvents     = false;
            _lastLocalTime          = -0.001f;
        }

        private void SeekCurrentMotionSet(float normalizedTime, int layerIndex)
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null)
                return;

            float totalDuration = _currentMotionSet.TotalDuration;
            if (totalDuration <= 0f)
                return;

            SeekCurrentMotionSetTime(normalizedTime * totalDuration, layerIndex);
        }

        private void SeekCurrentMotionSetTime(float time, int layerIndex)
        {
            if (!_isPlayingMotionSet || _currentMotionSet == null)
                return;

            float totalDuration = _currentMotionSet.TotalDuration;
            if (totalDuration <= 0f)
                return;

            _globalTime = Mathf.Clamp(time, 0f, Mathf.Max(0f, totalDuration - 0.001f));
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
            SeekPlaybackLayers(_globalTime);
        }

        private Motion GetCurrentMotion()
        {
            if (_currentMotionSet?.motions == null) return null;
            if (_currentMotionIndex < 0 || _currentMotionIndex >= _currentMotionSet.motions.Count) return null;
            return _currentMotionSet.motions[_currentMotionIndex];
        }
        
        private void OnAnimatorMove()
        {
            if (_animator?.Animator == null)
                return;

            // 원본 델타는 프리뷰/비물리 소비자 호환을 위해 유지한다.
            DeltaPosition = _animator.Animator.deltaPosition;
            DeltaRotation = _animator.Animator.deltaRotation;
            _rootMotionBuffer.Accumulate(DeltaPosition, DeltaRotation);
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

        private void ApplyCurrentPlaybackSpeed()
        {
            if (_currentState == null)
                return;

            float motionSpeed = GetCurrentMotion()?.playbackSpeed ?? 1f;
            _currentState.Speed = motionSpeed * _effectiveTimelineRate;
        }

        private AnimancerState GetRepresentativePlaybackState()
        {
            if (_currentState != null)
                return _currentState;

            foreach (MotionLayerPlayback playback in _motionLayerPlaybacks)
                if (playback?.state != null)
                    return playback.state;
            return null;
        }

        private sealed class MotionLayerPlayback
        {
            public MotionLayer data;
            public int animancerLayerIndex;
            public int motionIndex = -1;
            public AnimancerState state;
            public bool completed;
        }

        private readonly List<MotionLayerPlayback> _motionLayerPlaybacks = new();
        private readonly HashSet<int> _activeMotionLayerIndices = new();

        private void StartPlaybackLayers(float fadeDuration)
        {
            StopPlaybackLayers();
            if (_animator == null || _currentMotionSet?.layers == null)
                return;

            foreach (MotionLayer data in _currentMotionSet.layers)
            {
                if (data == null || !data.IsValid())
                    continue;

                MotionLayerPlayback conflicting = FindConcurrencyConflict(data);
                if (conflicting != null)
                {
                    if (data.interruptionPolicy == MotionInterruptionPolicy.RejectWhilePlaying)
                    {
                        Debug.LogWarning(
                            $"MotionSet '{_currentMotionSet.motionSetName}'의 동시성 그룹 " +
                            $"'{data.concurrencyGroupId}'이 이미 재생 중이어서 '{data.layerName}'을 건너뜁니다.",
                            this);
                        continue;
                    }
                    if (data.interruptionPolicy == MotionInterruptionPolicy.InterruptSameGroup)
                        CompletePlaybackLayer(conflicting);
                }

                int layerIndex = Mathf.Max(1, data.animancerLayerIndex);
                if (layerIndex == _currentMotionLayerIndex)
                {
                    Debug.LogWarning(
                        $"MotionSet '{_currentMotionSet.motionSetName}'의 '{data.layerName}' 레이어가 " +
                        $"Base 재생 레이어 인덱스 {layerIndex}와 충돌하여 건너뜁니다.",
                        this);
                    continue;
                }
                if (!_activeMotionLayerIndices.Add(layerIndex))
                {
                    Debug.LogWarning(
                        $"MotionSet '{_currentMotionSet.motionSetName}'의 재생 레이어 인덱스 {layerIndex}가 중복되어 " +
                        $"'{data.layerName}' 레이어를 건너뜁니다.",
                        this);
                    continue;
                }

                AnimancerLayer animancerLayer = _animator.Layers[layerIndex];
                animancerLayer.Mask = data.avatarMask;
                animancerLayer.IsAdditive = data.blendMode == MotionLayerBlendMode.Additive;
                animancerLayer.SetDebugName(string.IsNullOrEmpty(data.layerName)
                    ? $"Motion Layer {layerIndex}"
                    : data.layerName);
                if (fadeDuration > 0f)
                    animancerLayer.StartFade(Mathf.Clamp01(data.weight), fadeDuration);
                else
                    animancerLayer.Weight = Mathf.Clamp01(data.weight);

                var playback = new MotionLayerPlayback
                {
                    data = data,
                    animancerLayerIndex = layerIndex,
                };
                _motionLayerPlaybacks.Add(playback);
                PlayLayerMotion(playback, 0, 0f);
            }

            UpdatePlaybackLayers(0f);
        }

        private void UpdatePlaybackLayers(float globalTime)
        {
            for (int i = 0; i < _motionLayerPlaybacks.Count; i++)
            {
                MotionLayerPlayback playback = _motionLayerPlaybacks[i];
                if (playback.completed || playback.data == null)
                    continue;

                float duration = playback.data.TotalDuration;
                if (duration <= 0f)
                {
                    CompletePlaybackLayer(playback);
                    continue;
                }

                float synchronizedTime = MotionTimelineResolver.ResolveSynchronizedTime(
                    _currentMotionSet,
                    playback.data,
                    globalTime);
                if (synchronizedTime >= duration)
                {
                    if (playback.data.holdLastFrame)
                        SamplePlaybackLayer(playback, Mathf.Max(0f, duration - 0.0001f), true);
                    else
                        CompletePlaybackLayer(playback);
                    continue;
                }

                float normalizedTime = Mathf.Clamp01(synchronizedTime / duration);
                float weight = playback.data.weightCurve != null
                    ? playback.data.weightCurve.Evaluate(normalizedTime)
                    : EvaluateCurve(
                        MotionCurveChannel.LayerWeight,
                        playback.data.channelId,
                        normalizedTime,
                        playback.data.weight);
                _animator.Layers[playback.animancerLayerIndex].Weight = Mathf.Clamp01(weight);
                SamplePlaybackLayer(playback, Mathf.Max(0f, synchronizedTime), false);
            }
        }

        MotionLayerPlayback FindConcurrencyConflict(MotionLayer candidate)
        {
            if (candidate == null || string.IsNullOrEmpty(candidate.concurrencyGroupId))
                return null;
            foreach (MotionLayerPlayback playback in _motionLayerPlaybacks)
                if (!playback.completed &&
                    playback.data != null &&
                    string.Equals(
                        playback.data.concurrencyGroupId,
                        candidate.concurrencyGroupId,
                        StringComparison.Ordinal))
                    return playback;
            return null;
        }

        float EvaluateCurve(
            MotionCurveChannel channel,
            string targetId,
            float normalizedTime,
            float fallback)
        {
            if (_currentMotionSet?.curves == null)
                return fallback;

            // 빈 targetId는 전역 트랙 규약이다. 직렬화된 문자열은 null이 아니라 ""이므로
            // Ordinal 비교만 쓰면 인스펙터에서 저작한 전역 커브가 영원히 매칭되지 않는다.
            bool wantsGlobal = string.IsNullOrEmpty(targetId);
            foreach (MotionCurveTrack track in _currentMotionSet.curves)
            {
                if (track == null ||
                    !track.enabled ||
                    track.channel != channel)
                    continue;

                bool matched = wantsGlobal
                    ? string.IsNullOrEmpty(track.targetId)
                    : string.Equals(track.targetId, targetId, StringComparison.Ordinal);
                if (!matched)
                    continue;

                return track.Evaluate(normalizedTime, fallback);
            }
            return fallback;
        }

        private void SamplePlaybackLayer(MotionLayerPlayback playback, float time, bool hold)
        {
            if (!playback.data.GetMotionAtTime(time, out int motionIndex, out float localTime))
                return;
            if (motionIndex != playback.motionIndex || playback.state == null)
            {
                // MotionLayer에는 자체 블렌드 설정이 없으므로 소유 MotionSet 값을 쓴다.
                float blendDuration = playback.motionIndex >= 0
                    ? _currentMotionSet.InternalBlendDuration
                    : 0f;
                PlayLayerMotion(playback, motionIndex, blendDuration);
            }

            Motion motion = playback.data.motions[motionIndex];
            if (playback.state == null || motion == null)
                return;

            float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            playback.state.Time = motion.ClipStartTime + localTime * speed;
            playback.state.Speed = hold ? 0f : motion.playbackSpeed;
        }

        private void PlayLayerMotion(MotionLayerPlayback playback, int motionIndex, float fadeDuration)
        {
            if (playback.data?.motions == null ||
                motionIndex < 0 ||
                motionIndex >= playback.data.motions.Count)
                return;

            Motion motion = playback.data.motions[motionIndex];
            if (motion == null || !motion.IsValid())
                return;

            AnimancerLayer layer = _animator.Layers[playback.animancerLayerIndex];
            playback.state = layer.Play(motion.motionClip, fadeDuration);
            playback.state.Time = motion.ClipStartTime;
            playback.state.Speed = motion.playbackSpeed;
            playback.state.Events(this).OnEnd = null;
            playback.motionIndex = motionIndex;
        }

        private void CompletePlaybackLayer(MotionLayerPlayback playback)
        {
            _animator.Layers[playback.animancerLayerIndex].Stop();
            playback.state = null;
            playback.completed = true;
        }

        private void SetPlaybackLayersPaused(bool paused)
        {
            foreach (MotionLayerPlayback playback in _motionLayerPlaybacks)
            {
                if (playback.state == null || playback.data?.motions == null ||
                    playback.motionIndex < 0 || playback.motionIndex >= playback.data.motions.Count)
                    continue;

                Motion motion = playback.data.motions[playback.motionIndex];
                playback.state.Speed = paused ? 0f : motion?.playbackSpeed ?? 1f;
            }
        }

        private void SeekPlaybackLayers(float globalTime)
        {
            foreach (MotionLayerPlayback playback in _motionLayerPlaybacks)
            {
                float duration = playback.data?.TotalDuration ?? 0f;
                playback.completed = false;

                if (duration <= 0f || globalTime >= duration && !playback.data.holdLastFrame)
                {
                    CompletePlaybackLayer(playback);
                    continue;
                }

                SamplePlaybackLayer(
                    playback,
                    Mathf.Clamp(globalTime, 0f, Mathf.Max(0f, duration - 0.0001f)),
                    globalTime >= duration && playback.data.holdLastFrame);
            }
        }

        private void FadeOrStopLayer(int layerIndex, float fadeDuration)
        {
            if (_animator == null || layerIndex < 0 || layerIndex >= _animator.Layers.Count)
                return;
            if (fadeDuration > 0f)
                _animator.Layers[layerIndex].StartFade(0f, fadeDuration);
            else
                _animator.Layers[layerIndex].Stop();
        }

        private void StopPlaybackLayers(float fadeDuration = 0f)
        {
            if (_animator != null)
            {
                foreach (MotionLayerPlayback playback in _motionLayerPlaybacks)
                {
                    if (playback != null && playback.animancerLayerIndex > 0 &&
                        playback.animancerLayerIndex < _animator.Layers.Count)
                        FadeOrStopLayer(playback.animancerLayerIndex, fadeDuration);
                }
            }

            _motionLayerPlaybacks.Clear();
            _activeMotionLayerIndices.Clear();
        }

        void OnDestroy()
        {
            FlushRootMotion();
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
            FlushRootMotion();
            if (_isPlayingMotionSet)
            {
                StopMotionSet();
            }
        }
    }
}
