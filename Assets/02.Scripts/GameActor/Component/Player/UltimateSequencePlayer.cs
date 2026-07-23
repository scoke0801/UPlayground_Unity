using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;
using UnityEngine.SceneManagement;

namespace UPlayGround.Components
{
    public enum UltimateSequenceEndReason
    {
        Completed,
        Interrupted,
        CasterDead,
        TargetLost,
        SceneChanged,
        Disabled,
        Failed
    }

    /// <summary>
    /// 궁극기 MotionSet과 CameraSnapshotProfile을 하나의 실행 단위로 소유한다.
    /// 1단계에서는 실행 검증, 자원 소비, 모션/카메라 시작, 완료/중단 복구만 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerActor))]
    public class UltimateSequencePlayer : PlayerActorComponent
    {
        [SerializeField] private List<UltimateSequenceAsset> _sequences = new();

        private PlayerActor _caster;
        private ActorAnimator _animator;
        private UltimateSequenceAsset _activeAsset;
        private MotionSet _activeMotionSet;
        private bool _isRestoring;
        private bool _isAnimatorSubscribed;
        private readonly UltimateGameplayLockContext _lockContext = new();
        private readonly UltimatePlacementContext _placementContext = new();
        private UltimateRuntimeContext _runtimeContext;
        private Coroutine _startRoutine;
        private readonly HashSet<UltimateTimelineEvent> _executedTimelineEvents = new();
        private readonly HashSet<UltimateTimelineEvent> _activeTimelineEvents = new();

        public bool IsPlaying => _activeAsset != null;
        public UltimateSequenceAsset ActiveAsset => _activeAsset;
        public UltimateRuntimeContext RuntimeContext => _runtimeContext;

        public event Action<UltimateSequenceAsset> OnSequenceStarted;
        public event Action<UltimateSequenceAsset, UltimateSequenceEndReason> OnSequenceEnded;

        private void Awake()
        {
            _caster = GetComponent<PlayerActor>();
            RefreshAnimator();
        }

        private void OnEnable()
        {
            SubscribeAnimator();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            Restore(UltimateSequenceEndReason.Disabled, true);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            UnsubscribeAnimator();
        }

        private void Update()
        {
            if (_runtimeContext == null
                || _activeAsset == null)
            {
                return;
            }

            if (_caster == null || !_caster.IsAlive())
            {
                Restore(UltimateSequenceEndReason.CasterDead, true);
                return;
            }

            if (_activeAsset.targetPolicy != null
                && _activeAsset.targetPolicy.requireTarget
                && !IsPrimaryTargetAlive())
            {
                Restore(UltimateSequenceEndReason.TargetLost, true);
                return;
            }

            if (_activeAsset.lockSettings?.lockCameraInput == true)
                CameraManager.Instance?.SetInputLock(true);

            if (_startRoutine != null
                || _animator == null
                || !_animator.IsPlayingMotionSet
                || _animator.CurrentMotionSet != _activeMotionSet)
            {
                return;
            }

            float previousTime = _runtimeContext.ElapsedTime;
            _runtimeContext.ElapsedTime = _activeAsset.timelineUseUnscaledTime
                ? previousTime + Mathf.Max(0f, Time.unscaledDeltaTime)
                : Mathf.Max(0f, _animator.CurrentMotionSetTime);
            TickTimelineEvents(previousTime, _runtimeContext.ElapsedTime);
        }

        public void RefreshAnimator()
        {
            ActorAnimator nextAnimator = _caster != null
                ? _caster.Animator
                : GetComponentInChildren<ActorAnimator>(true);

            if (_animator == nextAnimator)
                return;

            UnsubscribeAnimator();
            _animator = nextAnimator;
            SubscribeAnimator();
        }

        public UltimateSequenceAsset ResolveAsset(CharacterActorType characterType)
        {
            if (_sequences == null)
                return null;

            for (int i = 0; i < _sequences.Count; i++)
            {
                UltimateSequenceAsset asset = _sequences[i];
                if (asset != null && asset.ownerType == characterType)
                    return asset;
            }

            return null;
        }

        public void ConfigureSequences(List<UltimateSequenceAsset> sequences)
        {
            _sequences = sequences ?? new List<UltimateSequenceAsset>();
        }

        public bool CanPlay(
            UltimateSequenceAsset asset,
            bool ignoreResource,
            out string error)
        {
            if (IsPlaying)
            {
                error = "이미 궁극기 시퀀스를 재생 중입니다.";
                return false;
            }

            if (_caster == null || !_caster.IsAlive())
            {
                error = "시전자가 없거나 생존 상태가 아닙니다.";
                return false;
            }

            RefreshAnimator();
            if (_animator == null)
            {
                error = "ActorAnimator를 찾을 수 없습니다.";
                return false;
            }

            if (asset == null)
            {
                error = $"{_caster.CharacterType}에 연결된 UltimateSequenceAsset이 없습니다.";
                return false;
            }

            if (!asset.IsValid(out error))
                return false;

            if (asset.ownerType != _caster.CharacterType)
            {
                error = $"에셋 소유자({asset.ownerType})와 현재 캐릭터({_caster.CharacterType})가 다릅니다.";
                return false;
            }

            if (!ignoreResource
                && asset.consumeUltimateGauge
                && (_caster.SkillGauge == null
                    || !_caster.SkillGauge.CanUseSkill(PlayerAbilityResourceView.UltimateSkillSlot)))
            {
                error = "궁극기 게이지가 부족하거나 쿨타임 중입니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool Play(
            UltimateSequenceAsset asset = null,
            Transform manualTarget = null,
            bool ignoreResource = false)
        {
            asset ??= ResolveAsset(_caster != null
                ? _caster.CharacterType
                : CharacterActorType.None);

            if (!CanPlay(asset, ignoreResource, out string error))
            {
                Debug.LogWarning($"[UltimateSequence] 실행 거부: {error}", this);
                return false;
            }

            _activeAsset = asset;
            _activeMotionSet = asset.motionSet.motionSet;
            _runtimeContext = new UltimateRuntimeContext
            {
                Caster = _caster,
                Asset = asset
            };
            _executedTimelineEvents.Clear();
            _activeTimelineEvents.Clear();

            if (!UltimateTargetResolver.TryResolve(
                    _caster,
                    asset.targetPolicy,
                    manualTarget,
                    _runtimeContext,
                    out error))
            {
                FailStart(error);
                return false;
            }

            _lockContext.Acquire(
                _caster,
                _caster.GetCombat(),
                _runtimeContext,
                asset.lockSettings);

            if (!ignoreResource
                && asset.consumeUltimateGauge
                && !_caster.SkillGauge.ConsumeSkill(PlayerAbilityResourceView.UltimateSkillSlot))
            {
                FailStart("궁극기 자원 소비에 실패했습니다.");
                return false;
            }

            _startRoutine = StartCoroutine(BeginSequenceRoutine());
            return true;
        }

        public void Interrupt()
        {
            Restore(UltimateSequenceEndReason.Interrupted, true);
        }

        private void HandleMotionSetEnded(MotionSet motionSet, bool completed)
        {
            if (!IsPlaying || motionSet != _activeMotionSet)
                return;

            Restore(
                completed
                    ? UltimateSequenceEndReason.Completed
                    : UltimateSequenceEndReason.Interrupted,
                false);
        }

        private System.Collections.IEnumerator BeginSequenceRoutine()
        {
            yield return _placementContext.Apply(
                _runtimeContext,
                _activeAsset.placementSettings);

            _startRoutine = null;
            if (_activeAsset == null)
                yield break;

            if (_caster.PlayerController != null
                && _caster.PlayerController.CurrentState is not PlayerIdleState)
            {
                _caster.PlayerController.TransitionToState(
                    new PlayerIdleState(_caster.PlayerController));
            }

            if (_animator.PlayMotionSetAsset(
                    _activeAsset.motionSet,
                    _activeAsset.motionFadeDuration) == null)
            {
                FailStart("MotionSet 재생을 시작하지 못했습니다.");
                yield break;
            }

            if (_activeAsset.cameraProfile != null
                && (CameraManager.Instance == null
                    || !CameraManager.Instance.PushCameraSnapshotSequence(
                        _activeAsset.cameraProfile)))
            {
                FailStart("CameraSnapshotProfile 재생을 시작하지 못했습니다.");
                yield break;
            }

            OnSequenceStarted?.Invoke(_activeAsset);
            TickTimelineEvents(-0.001f, 0f);
        }

        private void TickTimelineEvents(float previousTime, float currentTime)
        {
            if (_activeAsset?.events == null || _runtimeContext == null)
                return;

            foreach (UltimateTimelineEvent timelineEvent in _activeAsset.events)
            {
                if (timelineEvent == null)
                    continue;

                bool shouldStart = timelineEvent.startTime > previousTime
                                   && timelineEvent.startTime <= currentTime;
                if (shouldStart && _executedTimelineEvents.Add(timelineEvent))
                {
                    timelineEvent.Execute(_runtimeContext);
                    if (timelineEvent.duration > 0f)
                        _activeTimelineEvents.Add(timelineEvent);
                }
            }

            if (_activeTimelineEvents.Count == 0)
                return;

            var completed = new List<UltimateTimelineEvent>();
            foreach (UltimateTimelineEvent timelineEvent in _activeTimelineEvents)
            {
                if (currentTime < timelineEvent.EndTime)
                    continue;
                timelineEvent.Complete(_runtimeContext);
                completed.Add(timelineEvent);
            }

            foreach (UltimateTimelineEvent timelineEvent in completed)
                _activeTimelineEvents.Remove(timelineEvent);
        }

        private void FailStart(string message)
        {
            Debug.LogError($"[UltimateSequence] {message}", this);
            Restore(UltimateSequenceEndReason.Failed, true);
        }

        private void Restore(UltimateSequenceEndReason reason, bool stopMotion)
        {
            if (_isRestoring || _activeAsset == null)
                return;

            _isRestoring = true;
            UltimateSequenceAsset endedAsset = _activeAsset;
            MotionSet endedMotionSet = _activeMotionSet;

            _activeAsset = null;
            _activeMotionSet = null;

            if (_startRoutine != null)
            {
                StopCoroutine(_startRoutine);
                _startRoutine = null;
            }

            if (_runtimeContext != null)
            {
                foreach (UltimateTimelineEvent timelineEvent in _activeTimelineEvents)
                    timelineEvent?.Complete(_runtimeContext);
            }
            _activeTimelineEvents.Clear();
            _executedTimelineEvents.Clear();

            if (endedAsset.cameraProfile != null)
                CameraManager.Instance?.StopCameraSnapshotSequence(endedAsset.cameraProfile);

            if (stopMotion
                && _animator != null
                && _animator.IsPlayingMotionSet
                && _animator.CurrentMotionSet == endedMotionSet)
            {
                _animator.StopMotionSet();
            }

            _lockContext.Release();
            _placementContext.Restore();
            if (_runtimeContext != null)
                _runtimeContext.IsInterrupted = reason != UltimateSequenceEndReason.Completed;
            _runtimeContext = null;

            if (reason != UltimateSequenceEndReason.Disabled
                && _caster != null
                && _caster.IsAlive()
                && _animator != null)
            {
                if (_caster.PlayerController != null
                    && _caster.PlayerController.CurrentState is not PlayerIdleState)
                {
                    _caster.PlayerController.TransitionToState(
                        new PlayerIdleState(_caster.PlayerController));
                }
                else
                {
                    _animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, endedAsset.motionFadeDuration);
                }
            }

            _isRestoring = false;
            OnSequenceEnded?.Invoke(endedAsset, reason);
        }

        private bool IsPrimaryTargetAlive()
        {
            Transform target = _runtimeContext?.PrimaryTarget;
            if (target == null)
                return false;

            MonsterActor monster = target.GetComponent<MonsterActor>()
                                   ?? target.GetComponentInParent<MonsterActor>();
            return monster != null && monster.IsAlive();
        }

        private void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            Restore(UltimateSequenceEndReason.SceneChanged, true);
        }

        private void SubscribeAnimator()
        {
            if (!isActiveAndEnabled || _animator == null || _isAnimatorSubscribed)
                return;

            _animator.OnMotionSetEnded += HandleMotionSetEnded;
            _isAnimatorSubscribed = true;
        }

        private void UnsubscribeAnimator()
        {
            if (_animator != null && _isAnimatorSubscribed)
                _animator.OnMotionSetEnded -= HandleMotionSetEnded;
            _isAnimatorSubscribed = false;
        }
    }
}
