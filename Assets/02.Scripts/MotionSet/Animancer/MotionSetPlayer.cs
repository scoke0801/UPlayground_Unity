using System;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    /// <summary>
    /// GameActor 없이 MotionSetAsset을 재생하는 범용 MonoBehaviour 호스트.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    [RequireComponent(typeof(MotionEventExecutor))]
    public sealed class MotionSetPlayer : MonoBehaviour, IMotionEventTargetProvider, IMotionTimeSource
    {
        [Header("Playback")]
        [SerializeField] private MotionSetAsset _motionSet;
        [SerializeField] private bool _playOnEnable;
        [SerializeField, Min(0f)] private float _fadeDuration;
        [SerializeField, Min(0)] private int _layerIndex;
        [SerializeField, Range(0.1f, 5f)] private float _timelineSpeed = 1f;

        [Header("Target")]
        [Tooltip("비어 있으면 부모 provider를 거쳐 이 GameObject를 이벤트 대상으로 사용합니다.")]
        [SerializeField] private GameObject _eventTarget;

        private AnimancerComponent _animancer;
        private MotionEventExecutor _eventExecutor;
        private MotionSetPlaybackController _playback;

        public event Action Completed;
        public event Action<MotionSet, MotionSetEndReason> Ended;

        public MotionSetPlaybackController Playback => _playback;
        public MotionSetAsset MotionSet
        {
            get => _motionSet;
            set => _motionSet = value;
        }

        public GameObject MotionEventTarget =>
            _eventTarget != null ? _eventTarget : gameObject;

        float IMotionTimeSource.DeltaTime => Time.deltaTime;

        private void Awake()
        {
            _animancer = GetComponent<AnimancerComponent>();
            _eventExecutor = GetComponent<MotionEventExecutor>();

            // _eventTarget이 비어 있을 때 덮어쓰면 Executor에 인스펙터로 지정된 대상까지 지워진다.
            // 이 경우 Executor는 provider 경로로 이 컴포넌트를 찾아 같은 대상을 해석한다.
            if (_eventTarget != null)
                _eventExecutor.SetTargetObject(_eventTarget);

            _playback = new MotionSetPlaybackController(
                _animancer,
                _eventExecutor,
                this,
                this);
            _playback.Completed += HandleCompleted;
            _playback.Ended += HandleEnded;
            _playback.TimelineSpeed = _timelineSpeed;
        }

        private void OnEnable()
        {
            if (_playOnEnable && _motionSet != null)
                Play();
        }

        private void Update()
        {
            if (_playback == null)
                return;
            _playback.TimelineSpeed = _timelineSpeed;
            _playback.Update();
        }

        private void LateUpdate()
        {
            _playback?.LateUpdate();
        }

        public bool Play()
            => _playback != null &&
               _playback.Play(_motionSet, _fadeDuration, _layerIndex);

        public bool Play(MotionSetAsset motionSet, float fadeDuration = 0f, int layerIndex = 0)
        {
            _motionSet = motionSet;
            return _playback != null &&
                   _playback.Play(motionSet, fadeDuration, layerIndex);
        }

        public bool Play(MotionSet motionSet, float fadeDuration = 0f, int layerIndex = 0)
            => _playback != null &&
               _playback.Play(motionSet, fadeDuration, layerIndex);

        public void Stop(float fadeDuration = 0f)
        {
            _playback?.Stop(MotionSetEndReason.Stopped, fadeDuration);
        }

        public void SetEventTarget(GameObject target)
        {
            _eventTarget = target;
            _eventExecutor?.SetTargetObject(target);
        }

        public bool TryJumpToSection(string sectionId)
            => _playback != null && _playback.TryJumpToSection(sectionId);

        public bool TrySetNextSection(string fromSectionId, string nextSectionId)
            => _playback != null &&
               _playback.TrySetNextSection(fromSectionId, nextSectionId);

        public bool BreakInfiniteLoop()
            => _playback != null && _playback.BreakInfiniteLoop();

        private void HandleCompleted()
        {
            Completed?.Invoke();
        }

        private void HandleEnded(MotionSet motionSet, MotionSetEndReason reason)
        {
            Ended?.Invoke(motionSet, reason);
        }

        private void OnDestroy()
        {
            if (_playback == null)
                return;
            _playback.Completed -= HandleCompleted;
            _playback.Ended -= HandleEnded;
            _playback.Dispose();
            _playback = null;
        }
    }
}
