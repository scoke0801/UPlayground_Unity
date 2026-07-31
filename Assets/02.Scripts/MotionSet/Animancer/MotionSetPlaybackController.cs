using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    /// <summary>
    /// GameActor 없이 AnimancerComponent와 MotionEventExecutor만으로 MotionSet을 재생하는 커널.
    /// MonoBehaviour 생명주기는 호스트가 Update/LateUpdate 호출로 연결한다.
    /// </summary>
    public sealed class MotionSetPlaybackController : IDisposable
    {
        private sealed class LayerPlayback
        {
            public MotionLayer Data;
            public int LayerIndex;
            public int MotionIndex = -1;
            public AnimancerState State;
            public bool Completed;
        }

        private readonly AnimancerComponent _animancer;
        private readonly MotionEventExecutor _eventExecutor;
        private readonly IMotionTimeSource _timeSource;
        private readonly UnityEngine.Object _logContext;
        private readonly List<LayerPlayback> _layerPlaybacks = new();
        private readonly HashSet<int> _activeLayerIndices = new();
        private readonly HashSet<MotionEventBase> _consumedControlEvents = new();

        private MotionSetAsset _sourceAsset;
        private MotionSet _motionSet;
        private AnimancerState _currentState;
        private int _motionIndex;
        private int _baseLayerIndex;
        private float _globalTime;
        private float _lastLocalTime = -0.001f;
        private float _timelineSpeed = 1f;
        private float _effectiveTimelineRate = 1f;
        private string _currentSectionId;
        private string _nextSectionOverrideId;
        private bool _completeAfterLateUpdate;

        private MotionEventBase _activeControlEvent;
        private int _remainingLoops;
        private float _freezeTimer;
        private bool _isFrozen;
        private bool _isInfiniteLooping;
        private bool _suppressControlEvents;

        public event Action Completed;
        public event Action<MotionSet, MotionSetEndReason> Ended;

        public MotionSetAsset SourceAsset => _sourceAsset;
        public MotionSet CurrentMotionSet => _motionSet;
        public AnimancerState CurrentState => _currentState;
        public int CurrentMotionIndex => _motionIndex;
        public int BaseLayerIndex => _baseLayerIndex;
        public float CurrentTime => _globalTime;
        public string CurrentSectionId => _currentSectionId;
        public bool IsPlaying { get; private set; }
        public bool IsFrozen => _isFrozen;
        public bool IsInfiniteLooping => _isInfiniteLooping;

        public float TimelineSpeed
        {
            get => _timelineSpeed;
            set => _timelineSpeed = Mathf.Clamp(value, 0.1f, 5f);
        }

        public MotionSetPlaybackController(
            AnimancerComponent animancer,
            MotionEventExecutor eventExecutor = null,
            IMotionTimeSource timeSource = null,
            UnityEngine.Object logContext = null)
        {
            _animancer = animancer != null
                ? animancer
                : throw new ArgumentNullException(nameof(animancer));
            _eventExecutor = eventExecutor;
            _timeSource = timeSource;
            _logContext = logContext != null ? logContext : animancer;
        }

        public bool Play(MotionSetAsset asset, float fadeDuration = 0f, int layerIndex = 0)
            => asset != null && PlayInternal(asset, asset.motionSet, fadeDuration, layerIndex);

        public bool Play(MotionSet motionSet, float fadeDuration = 0f, int layerIndex = 0)
            => PlayInternal(null, motionSet, fadeDuration, layerIndex);

        private bool PlayInternal(
            MotionSetAsset sourceAsset,
            MotionSet motionSet,
            float fadeDuration,
            int layerIndex)
        {
            if (motionSet == null || !motionSet.IsValid())
                return false;
            if (!MotionTimelineResolver.TryValidateSectionLayout(motionSet, out string error))
            {
                Debug.LogError(
                    $"MotionSet '{motionSet.motionSetName}' 재생 거부: {error}",
                    _logContext);
                return false;
            }

            int nextBaseLayerIndex = motionSet.baseLayerIndex > 0
                ? motionSet.baseLayerIndex
                : Mathf.Max(0, layerIndex);
            float resolvedFade = Mathf.Max(0f, fadeDuration);
            if (IsPlaying)
            {
                bool preserveBaseLayer =
                    resolvedFade > 0f &&
                    _baseLayerIndex == nextBaseLayerIndex;
                StopInternal(
                    MotionSetEndReason.Interrupted,
                    resolvedFade,
                    preserveBaseLayer);
            }

            _sourceAsset = sourceAsset;
            _motionSet = motionSet;
            _motionIndex = -1;
            _baseLayerIndex = nextBaseLayerIndex;
            _globalTime = 0f;
            _lastLocalTime = -0.001f;
            _currentSectionId = null;
            _nextSectionOverrideId = null;
            IsPlaying = true;
            ResetControlState();

            _eventExecutor?.PlayMotionSet(motionSet);
            if (motionSet.GetMotionAtTime(0f, out int initialIndex, out _))
                PlayMotionAtIndex(initialIndex, resolvedFade);
            StartLayers(resolvedFade);
            if (_currentState == null && _layerPlaybacks.Count == 0)
            {
                Stop(MotionSetEndReason.Invalidated);
                return false;
            }

            UpdateCurrentSection(0f, true);
            return true;
        }

        public void Update()
        {
            Update(_timeSource?.DeltaTime ?? Time.deltaTime);
        }

        public void Update(float deltaTime)
        {
            if (!IsPlaying || _motionSet == null || _completeAfterLateUpdate)
                return;

            deltaTime = Mathf.Max(0f, deltaTime);
            if (_isFrozen)
            {
                _freezeTimer -= deltaTime;
                SetLayersPaused(true);
                if (_freezeTimer <= 0f)
                {
                    _isFrozen = false;
                    RestorePlaybackSpeed();
                    SetLayersPaused(false);
                }
                return;
            }

            float normalized = _motionSet.TotalDuration > 0f
                ? Mathf.Clamp01(_globalTime / _motionSet.TotalDuration)
                : 0f;
            float playbackRate = EvaluateCurve(
                MotionCurveChannel.PlaybackRate,
                null,
                normalized,
                1f);
            float stretchRate = MotionTimelineResolver.EvaluateTimeStretchRate(
                _motionSet,
                _globalTime,
                _timelineSpeed);
            float stretchCurve = EvaluateCurve(
                MotionCurveChannel.TimeStretch,
                null,
                normalized,
                1f);
            _effectiveTimelineRate = stretchRate *
                                     Mathf.Max(0f, playbackRate) *
                                     Mathf.Max(0f, stretchCurve);
            ApplyCurrentPlaybackSpeed();
            _globalTime += deltaTime * _effectiveTimelineRate;

            if (HandleSectionBoundary())
                return;

            if (_globalTime >= _motionSet.TotalDuration)
            {
                ScheduleCompletion(_motionSet.TotalDuration);
                return;
            }
            UpdateLayers(_globalTime);

            if (!_motionSet.GetMotionAtTime(_globalTime, out int newIndex, out float localTime))
                return;

            if (newIndex != _motionIndex)
            {
                Motion oldMotion = GetCurrentMotion();
                if (oldMotion != null)
                    ProcessControlEvents(_lastLocalTime, oldMotion.Duration);

                _motionIndex = newIndex;
                PlayMotionAtIndex(newIndex, _motionSet.InternalBlendDuration);
                ApplyCurrentPlaybackSpeed();
                _motionSet.GetMotionAtTime(_globalTime, out _, out localTime);
                _lastLocalTime = 0f;
            }

            ProcessControlEvents(_lastLocalTime, localTime);
            if (_motionSet.GetMotionAtTime(_globalTime, out _, out float finalLocalTime))
                _lastLocalTime = finalLocalTime;
        }

        /// <summary>
        /// Animancer 본 평가가 끝난 LateUpdate에서 호출한다.
        /// </summary>
        public void LateUpdate()
        {
            if (!IsPlaying)
                return;

            float eventTime = _completeAfterLateUpdate
                ? _globalTime
                : _motionSet != null && _motionSet.HasPlaybackLayers
                    ? _globalTime
                    : GetPoseDrivenGlobalTime();
            _eventExecutor?.UpdateTime(eventTime);
            _eventExecutor?.FlushDeferredEvents();

            if (_completeAfterLateUpdate && IsPlaying)
                Complete();
        }

        public void Stop(MotionSetEndReason reason = MotionSetEndReason.Stopped, float fadeDuration = 0f)
        {
            StopInternal(reason, fadeDuration, false);
        }

        private void StopInternal(
            MotionSetEndReason reason,
            float fadeDuration,
            bool preserveBaseLayer)
        {
            if (!IsPlaying && _motionSet == null)
                return;

            MotionSet endedMotionSet = _motionSet;
            _eventExecutor?.Stop();
            if (!preserveBaseLayer)
                FadeOrStopLayer(_baseLayerIndex, fadeDuration);
            StopLayers(fadeDuration);

            IsPlaying = false;
            _sourceAsset = null;
            _motionSet = null;
            _currentState = null;
            _globalTime = 0f;
            _currentSectionId = null;
            _nextSectionOverrideId = null;
            _completeAfterLateUpdate = false;
            _effectiveTimelineRate = 1f;
            ResetControlState();
            Ended?.Invoke(endedMotionSet, reason);
        }

        public bool TrySetNextSection(string fromSectionId, string nextSectionId)
        {
            if (!IsPlaying ||
                !string.Equals(_currentSectionId, fromSectionId, StringComparison.Ordinal) ||
                !MotionTimelineResolver.TryGetSection(_motionSet, nextSectionId, out _))
                return false;
            _nextSectionOverrideId = nextSectionId;
            return true;
        }

        public bool TryJumpToSection(string sectionId)
        {
            if (!IsPlaying ||
                !MotionTimelineResolver.TryGetSection(
                    _motionSet,
                    sectionId,
                    out MotionSectionRange range))
                return false;

            SeekTime(range.startTime);
            _currentSectionId = sectionId;
            _eventExecutor?.EnterSection();
            return true;
        }

        public void SeekNormalized(float normalizedTime)
        {
            if (_motionSet == null)
                return;
            SeekTime(Mathf.Clamp01(normalizedTime) * _motionSet.TotalDuration);
        }

        public void SeekTime(float time)
        {
            if (_motionSet == null)
                return;

            _globalTime = Mathf.Clamp(time, 0f, Mathf.Max(0f, _motionSet.TotalDuration));
            if (_motionSet.GetMotionAtTime(_globalTime, out int index, out float localTime))
            {
                _motionIndex = index;
                PlayMotionAtIndex(index, 0f);
                Motion motion = GetCurrentMotion();
                if (_currentState != null && motion != null)
                {
                    float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                    _currentState.Time = motion.ClipStartTime + localTime * speed;
                }
                _lastLocalTime = localTime;
            }

            SeekLayers(_globalTime);
            _eventExecutor?.SeekTo(_globalTime);
            UpdateCurrentSection(_globalTime, false);
            ResetControlState();
        }

        public bool BreakInfiniteLoop()
        {
            if (!_isInfiniteLooping)
                return false;
            if (_activeControlEvent != null)
                _consumedControlEvents.Add(_activeControlEvent);
            _isInfiniteLooping = false;
            _activeControlEvent = null;
            return true;
        }

        public void SuppressControlEvents()
        {
            _suppressControlEvents = true;
            BreakInfiniteLoop();
            _isFrozen = false;
            RestorePlaybackSpeed();
            SetLayersPaused(false);
        }

        private void Complete()
        {
            Stop(MotionSetEndReason.Completed);
            Completed?.Invoke();
        }

        private void ScheduleCompletion(float boundaryTime)
        {
            if (_motionSet == null || _completeAfterLateUpdate)
                return;

            _globalTime = Mathf.Clamp(
                boundaryTime,
                0f,
                Mathf.Max(0f, _motionSet.TotalDuration));
            float sampleTime = Mathf.Max(0f, _globalTime - 0.0001f);
            SampleBasePose(sampleTime);
            UpdateLayers(sampleTime);
            SetLayersPaused(true);
            _completeAfterLateUpdate = true;
        }

        private void SampleBasePose(float globalTime)
        {
            if (!_motionSet.GetMotionAtTime(globalTime, out int index, out float localTime))
                return;
            if (_currentState == null || index != _motionIndex)
                PlayMotionAtIndex(index, 0f);

            Motion motion = GetCurrentMotion();
            if (_currentState == null || motion == null)
                return;

            float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            _currentState.Time = motion.ClipStartTime + localTime * speed;
            _currentState.Speed = 0f;
        }

        private bool HandleSectionBoundary()
        {
            if (string.IsNullOrEmpty(_currentSectionId) ||
                !MotionTimelineResolver.TryGetSection(
                    _motionSet,
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
                    SetLayersPaused(true);
                    return true;
                case MotionSectionEndPolicy.LoopSelf:
                    return TryJumpToSection(section.id);
            }

            string nextId = !string.IsNullOrEmpty(_nextSectionOverrideId)
                ? _nextSectionOverrideId
                : MotionTimelineResolver.ResolveDefaultNextSectionId(_motionSet, section);
            _nextSectionOverrideId = null;
            if (string.IsNullOrEmpty(nextId) ||
                !MotionTimelineResolver.TryGetSection(
                    _motionSet,
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

        private void UpdateCurrentSection(float time, bool enter)
        {
            if (MotionTimelineResolver.TryGetSectionAtTime(
                    _motionSet,
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

        private void ProcessControlEvents(float start, float end)
        {
            if (_suppressControlEvents)
                return;
            Motion motion = GetCurrentMotion();
            if (motion?.events == null)
                return;

            for (int i = 0; i < motion.events.Count; i++)
            {
                MotionEventBase motionEvent = motion.events[i];
                if (motionEvent is not IMotionTimelineControlEvent control ||
                    _consumedControlEvents.Contains(motionEvent))
                    continue;

                bool crossedStart = motionEvent.startTime >= start && motionEvent.startTime <= end;
                bool crossedEnd = motionEvent.endTime >= start && motionEvent.endTime <= end;
                switch (control.Mode)
                {
                    case MotionTimelineControlMode.Freeze:
                        if (crossedStart)
                        {
                            _activeControlEvent = motionEvent;
                            _freezeTimer = Mathf.Max(0f, control.FreezeDuration);
                            _isFrozen = _freezeTimer > 0f;
                            _consumedControlEvents.Add(motionEvent);
                            if (_isFrozen && _currentState != null)
                                _currentState.Speed = 0f;
                        }
                        break;

                    case MotionTimelineControlMode.InfiniteLoop:
                        if (crossedStart)
                        {
                            _activeControlEvent = motionEvent;
                            _isInfiniteLooping = true;
                        }
                        if (_isInfiniteLooping && ReferenceEquals(_activeControlEvent, motionEvent) && crossedEnd)
                            SeekCurrentMotionLocal(motionEvent.startTime);
                        break;

                    default:
                        if (crossedStart && !ReferenceEquals(_activeControlEvent, motionEvent))
                        {
                            _activeControlEvent = motionEvent;
                            _remainingLoops = Mathf.Max(0, control.LoopCount);
                        }
                        if (ReferenceEquals(_activeControlEvent, motionEvent) &&
                            crossedEnd &&
                            _remainingLoops > 0)
                        {
                            _remainingLoops--;
                            SeekCurrentMotionLocal(motionEvent.startTime);
                        }
                        else if (ReferenceEquals(_activeControlEvent, motionEvent) &&
                                 crossedEnd &&
                                 _remainingLoops <= 0)
                        {
                            _consumedControlEvents.Add(motionEvent);
                            _activeControlEvent = null;
                        }
                        break;
                }
            }
        }

        private void SeekCurrentMotionLocal(float localTime)
        {
            _globalTime = GetAccumulatedDurationBeforeCurrentMotion() + Mathf.Max(0f, localTime);
            Motion motion = GetCurrentMotion();
            if (_currentState != null && motion != null)
            {
                float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                _currentState.Time = motion.ClipStartTime + localTime * speed;
            }
            _lastLocalTime = localTime;
            SeekLayers(_globalTime);
            _eventExecutor?.SeekTo(_globalTime);
        }

        private float GetPoseDrivenGlobalTime()
        {
            Motion motion = GetCurrentMotion();
            if (_currentState == null || motion == null)
                return _globalTime;

            float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            float localPose = (_currentState.Time - motion.ClipStartTime) / speed;
            return Mathf.Clamp(
                GetAccumulatedDurationBeforeCurrentMotion() +
                Mathf.Clamp(localPose, 0f, motion.Duration),
                0f,
                _motionSet.TotalDuration);
        }

        private float GetAccumulatedDurationBeforeCurrentMotion()
        {
            float duration = 0f;
            if (_motionSet?.motions == null)
                return duration;
            for (int i = 0; i < _motionIndex && i < _motionSet.motions.Count; i++)
                duration += _motionSet.motions[i]?.Duration ?? 0f;
            return duration;
        }

        private Motion GetCurrentMotion()
        {
            if (_motionSet?.motions == null ||
                _motionIndex < 0 ||
                _motionIndex >= _motionSet.motions.Count)
                return null;
            return _motionSet.motions[_motionIndex];
        }

        private void PlayMotionAtIndex(int index, float fadeDuration)
        {
            if (_motionSet?.motions == null || index < 0 || index >= _motionSet.motions.Count)
                return;
            Motion motion = _motionSet.motions[index];
            if (motion == null || !motion.IsValid())
                return;

            _motionIndex = index;
            AnimancerLayer layer = _animancer.Layers[_baseLayerIndex];
            _currentState = layer.Play(motion.motionClip, fadeDuration);
            _currentState.Time = motion.ClipStartTime;
            _currentState.Speed = motion.playbackSpeed;
        }

        private void RestorePlaybackSpeed()
        {
            ApplyCurrentPlaybackSpeed();
        }

        private void ApplyCurrentPlaybackSpeed()
        {
            if (_currentState == null)
                return;

            float motionSpeed = GetCurrentMotion()?.playbackSpeed ?? 1f;
            _currentState.Speed = motionSpeed * _effectiveTimelineRate;
        }

        private void ResetControlState()
        {
            _activeControlEvent = null;
            _remainingLoops = 0;
            _freezeTimer = 0f;
            _isFrozen = false;
            _isInfiniteLooping = false;
            _suppressControlEvents = false;
            _consumedControlEvents.Clear();
        }

        private void StartLayers(float fadeDuration)
        {
            StopLayers();
            if (_motionSet?.layers == null)
                return;

            foreach (MotionLayer data in _motionSet.layers)
            {
                if (data == null || !data.IsValid())
                    continue;
                int layerIndex = Mathf.Max(1, data.animancerLayerIndex);
                if (layerIndex == _baseLayerIndex || !_activeLayerIndices.Add(layerIndex))
                    continue;

                AnimancerLayer layer = _animancer.Layers[layerIndex];
                layer.Mask = data.avatarMask;
                layer.IsAdditive = data.blendMode == MotionLayerBlendMode.Additive;
                if (fadeDuration > 0f)
                    layer.StartFade(Mathf.Clamp01(data.weight), fadeDuration);
                else
                    layer.Weight = Mathf.Clamp01(data.weight);

                LayerPlayback playback = new()
                {
                    Data = data,
                    LayerIndex = layerIndex,
                };
                _layerPlaybacks.Add(playback);
                PlayLayerMotion(playback, 0, 0f);
            }
            UpdateLayers(0f);
        }

        private void UpdateLayers(float globalTime)
        {
            foreach (LayerPlayback playback in _layerPlaybacks)
            {
                if (playback.Completed || playback.Data == null)
                    continue;
                float duration = playback.Data.TotalDuration;
                if (duration <= 0f)
                {
                    CompleteLayer(playback);
                    continue;
                }

                float synchronizedTime = MotionTimelineResolver.ResolveSynchronizedTime(
                    _motionSet,
                    playback.Data,
                    globalTime);
                if (synchronizedTime >= duration)
                {
                    if (playback.Data.holdLastFrame)
                        SampleLayer(playback, Mathf.Max(0f, duration - 0.0001f), true);
                    else
                        CompleteLayer(playback);
                    continue;
                }

                float normalized = Mathf.Clamp01(synchronizedTime / duration);
                float weight = playback.Data.weightCurve != null
                    ? playback.Data.weightCurve.Evaluate(normalized)
                    : EvaluateCurve(
                        MotionCurveChannel.LayerWeight,
                        playback.Data.channelId,
                        normalized,
                        playback.Data.weight);
                _animancer.Layers[playback.LayerIndex].Weight = Mathf.Clamp01(weight);
                SampleLayer(playback, synchronizedTime, false);
            }
        }

        private float EvaluateCurve(
            MotionCurveChannel channel,
            string targetId,
            float normalizedTime,
            float fallback)
        {
            if (_motionSet?.curves == null)
                return fallback;

            // 빈 targetId는 전역 트랙 규약이다. 직렬화된 문자열은 null이 아니라 ""이므로
            // Ordinal 비교만 쓰면 인스펙터에서 저작한 전역 커브가 영원히 매칭되지 않는다.
            bool wantsGlobal = string.IsNullOrEmpty(targetId);
            foreach (MotionCurveTrack track in _motionSet.curves)
                if (track != null &&
                    track.enabled &&
                    track.channel == channel &&
                    (wantsGlobal
                        ? string.IsNullOrEmpty(track.targetId)
                        : string.Equals(track.targetId, targetId, StringComparison.Ordinal)))
                    return track.Evaluate(normalizedTime, fallback);
            return fallback;
        }

        private void SampleLayer(LayerPlayback playback, float time, bool hold)
        {
            if (!playback.Data.GetMotionAtTime(time, out int index, out float localTime))
                return;
            if (index != playback.MotionIndex || playback.State == null)
            {
                // MotionLayer에는 자체 블렌드 설정이 없으므로 소유 MotionSet 값을 쓴다.
                float blendDuration = playback.MotionIndex >= 0
                    ? _motionSet.InternalBlendDuration
                    : 0f;
                PlayLayerMotion(playback, index, blendDuration);
            }
            if (playback.State == null)
                return;

            Motion motion = playback.Data.motions[index];
            float speed = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
            playback.State.Time = motion.ClipStartTime + localTime * speed;
            playback.State.Speed = hold ? 0f : motion.playbackSpeed;
        }

        private void PlayLayerMotion(LayerPlayback playback, int index, float fadeDuration)
        {
            if (playback.Data?.motions == null || index < 0 || index >= playback.Data.motions.Count)
                return;
            Motion motion = playback.Data.motions[index];
            if (motion == null || !motion.IsValid())
                return;

            playback.State = _animancer.Layers[playback.LayerIndex].Play(
                motion.motionClip,
                fadeDuration);
            playback.State.Time = motion.ClipStartTime;
            playback.State.Speed = motion.playbackSpeed;
            playback.MotionIndex = index;
        }

        private void CompleteLayer(LayerPlayback playback)
        {
            _animancer.Layers[playback.LayerIndex].Stop();
            playback.State = null;
            playback.Completed = true;
        }

        private void SetLayersPaused(bool paused)
        {
            foreach (LayerPlayback playback in _layerPlaybacks)
            {
                if (playback.State == null ||
                    playback.Data?.motions == null ||
                    playback.MotionIndex < 0 ||
                    playback.MotionIndex >= playback.Data.motions.Count)
                    continue;
                Motion motion = playback.Data.motions[playback.MotionIndex];
                playback.State.Speed = paused ? 0f : motion?.playbackSpeed ?? 1f;
            }
        }

        private void SeekLayers(float globalTime)
        {
            foreach (LayerPlayback playback in _layerPlaybacks)
            {
                float duration = playback.Data?.TotalDuration ?? 0f;
                playback.Completed = false;
                if (duration <= 0f ||
                    globalTime >= duration && !playback.Data.holdLastFrame)
                {
                    CompleteLayer(playback);
                    continue;
                }
                SampleLayer(
                    playback,
                    Mathf.Clamp(globalTime, 0f, Mathf.Max(0f, duration - 0.0001f)),
                    globalTime >= duration && playback.Data.holdLastFrame);
            }
        }

        private void FadeOrStopLayer(int layerIndex, float fadeDuration)
        {
            if (layerIndex < 0 || layerIndex >= _animancer.Layers.Count)
                return;
            if (fadeDuration > 0f)
                _animancer.Layers[layerIndex].StartFade(0f, fadeDuration);
            else
                _animancer.Layers[layerIndex].Stop();
        }

        private void StopLayers(float fadeDuration = 0f)
        {
            foreach (LayerPlayback playback in _layerPlaybacks)
                if (playback != null &&
                    playback.LayerIndex > 0 &&
                    playback.LayerIndex < _animancer.Layers.Count)
                    FadeOrStopLayer(playback.LayerIndex, fadeDuration);
            _layerPlaybacks.Clear();
            _activeLayerIndices.Clear();
        }

        public void Dispose()
        {
            Stop();
            Completed = null;
            Ended = null;
        }
    }
}
