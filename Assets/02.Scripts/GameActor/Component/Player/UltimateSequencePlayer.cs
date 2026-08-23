using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;
using UnityEngine.SceneManagement;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Cinematic;
using UPlayGround.MovementController;

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
        private PlayerActor _caster;
        private ActorAnimator _animator;
        private UltimateSequenceAsset _activeAsset;
        private MotionSetAsset _activeMotionAsset;
        // 무기 서브 Animator는 캐릭터 에셋을 해석하지 못하므로 해석 전 Motion Key도 함께 보관한다.
        private MotionKey _activeMotionKey;
        private MotionSet _activeMotionSet;
        private bool _isRestoring;
        private bool _isAnimatorSubscribed;
        // 레터박스는 에셋 상태가 아니라 실제 Show 여부로 해제한다.
        // 재생 중 에셋 필드가 바뀌어도 잔존하지 않도록 인스턴스 플래그로 기억한다.
        private bool _letterboxShown;
        private float _letterboxExitDuration;
        private readonly UltimateGameplayLockContext _lockContext = new();
        private readonly UltimatePlacementContext _placementContext = new();
        private UltimateRuntimeContext _runtimeContext;
        private Coroutine _startRoutine;
        private Coroutine _completionRoutine;
        private AbilityExecutionHandle _abilityExecution;
        private PlayerUltimateState _sequenceState;
        private readonly HashSet<UltimateTimelineEvent> _executedTimelineEvents = new();
        private readonly HashSet<UltimateTimelineEvent> _activeTimelineEvents = new();
        private readonly List<GameObject> _stageTargetBuffer = new();

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
                && _activeAsset.targetPolicy.interruptWhenTargetLost
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

        public bool CanPlay(
            UltimateSequenceAsset asset,
            MotionSetAsset motionAsset,
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

            if (motionAsset == null
                || motionAsset.motionSet == null
                || !motionAsset.motionSet.IsValid())
            {
                error = "유효한 실행 MotionSetAsset이 필요합니다.";
                return false;
            }

            if (asset.ownerType != _caster.CharacterType)
            {
                error = $"에셋 소유자({asset.ownerType})와 현재 캐릭터({_caster.CharacterType})가 다릅니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 궁극기 에디터의 명시적 미리보기 경로. Ability 비용과 쿨타임을 건드리지 않는다.
        /// 실제 인게임 입력은 반드시 <see cref="PlayPrepared"/>를 사용한다.
        /// 비용·쿨타임을 완전히 우회하므로 에디터에서만 동작한다.
        /// (호출부 <c>PlayerCombat.PreviewUltimate</c>가 런타임 코드라 #if UNITY_EDITOR로 잘라낼 수 없어
        ///  런타임 가드로 막는다. 빌드에서는 항상 false를 반환한다.)
        /// </summary>
        public bool PlayPreview(
            UltimateSequenceAsset asset,
            Transform manualTarget = null)
        {
            if (!Application.isEditor)
            {
                Debug.LogWarning(
                    "[UltimateSequence] 미리보기 경로는 에디터에서만 사용할 수 있습니다.",
                    this);
                return false;
            }

            return PlayInternal(
                asset,
                asset != null ? asset.motionSet : null,
                default,
                manualTarget);
        }

        /// <summary>
        /// GAS가 선택한 Ultimate Variant의 Prepared 실행과 Motion을 받아 시퀀스를 시작한다.
        /// 비용·쿨타임 Commit과 종료는 이 실행 핸들 하나로 관리한다.
        /// </summary>
        public bool PlayPrepared(
            UltimateSequenceAsset asset,
            MotionSetAsset motionAsset,
            AbilityExecutionHandle abilityExecution,
            Transform manualTarget = null,
            MotionKey motionKey = default)
        {
            if (!abilityExecution.IsValid)
            {
                Debug.LogWarning("[UltimateSequence] 유효한 Prepared Ability 실행이 없습니다.", this);
                return false;
            }

            return PlayInternal(
                asset,
                motionAsset,
                abilityExecution,
                manualTarget,
                motionKey);
        }

        /// <summary>
        /// 시퀀스 시작 실패 시의 Ability 정리 책임은 이 클래스가 가진다.
        /// 실행 핸들을 넘겨받은 뒤 실패하면 <see cref="FailStart"/> → <see cref="Restore"/> 경로에서
        /// <c>EndAbility(handle, false)</c>로 반드시 종료하므로, 호출부가 추가로 Abort할 필요는 없다.
        /// (현재 호출부 <c>PlayerCombat.RequestUltimate</c>는 false 반환 시 Abort를 한 번 더 호출한다.
        ///  <c>ActorAbilitySystem.Abort</c>는 이미 종료된 stale 핸들을 무시하므로 무해하지만
        ///  이중 종료 계약이므로 호출부 정리는 후속 과제로 남긴다.)
        /// 단, 핸들을 넘겨받기 전 단계인 <see cref="CanPlay"/> 거부는 아무것도 소유하지 않은 상태이므로
        /// 호출부가 Abort로 정리해야 한다.
        /// </summary>
        private bool PlayInternal(
            UltimateSequenceAsset asset,
            MotionSetAsset motionAsset,
            AbilityExecutionHandle abilityExecution,
            Transform manualTarget,
            MotionKey motionKey = default)
        {
            if (!CanPlay(asset, motionAsset, out string error))
            {
                Debug.LogWarning($"[UltimateSequence] 실행 거부: {error}", this);
                return false;
            }

            _abilityExecution = abilityExecution;
            _activeAsset = asset;
            _activeMotionAsset = motionAsset;
            _activeMotionKey = motionKey;
            _activeMotionSet = motionAsset.motionSet;
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
                asset.lockSettings,
                asset.uiSettings);

            if (_abilityExecution.IsValid
                && _caster.Abilities.Commit(_abilityExecution)
                != AbilityActivationResult.Success)
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

            if (!completed)
            {
                Restore(UltimateSequenceEndReason.Interrupted, false);
                return;
            }

            if (_runtimeContext != null)
            {
                float previousTime = _runtimeContext.ElapsedTime;
                float completedTime = Mathf.Max(previousTime, motionSet.TotalDuration);
                _runtimeContext.ElapsedTime = completedTime;
                TickTimelineEvents(previousTime, completedTime);
            }

            if (_completionRoutine == null)
                _completionRoutine = StartCoroutine(CompleteAfterFinalPoseRoutine(motionSet));
        }

        private System.Collections.IEnumerator CompleteAfterFinalPoseRoutine(MotionSet motionSet)
        {
            // ActorAnimator는 LateUpdate에서 마지막 포즈를 샘플링한 직후 완료 이벤트를 보낸다.
            // CinematicPoseMirror도 LateUpdate에서 원본 포즈를 복사하지만 두 컴포넌트 사이의
            // 실행 순서는 보장되지 않는다. Mirror가 먼저 실행된 프레임에 완료 이벤트가 오면
            // yield return null 한 번만으로는 다음 Update에서 무대를 먼저 제거해, 복제 캐릭터가
            // 마지막 포즈를 복사할 기회가 없다. 다음 프레임의 LateUpdate를 온전히 통과시킨 뒤
            // 그 다음 Update에서 복구해야 Ultimate Motion의 최종 포즈가 잘리지 않는다.
            // WaitForEndOfFrame은 카메라가 렌더하지 않는 프레임(배치/헤드리스 모드 등)에서 재개되지 않아
            // Restore가 영원히 호출되지 않는 잠금이 발생한다. 두 번의 yield return null은 렌더러 유무와
            // 관계없이 최소 한 번의 전체 LateUpdate 구간을 보장한다.
            yield return null;
            yield return null;

            _completionRoutine = null;
            if (IsPlaying && _activeMotionSet == motionSet)
                Restore(UltimateSequenceEndReason.Completed, false);
        }

        private System.Collections.IEnumerator BeginSequenceRoutine()
        {
            yield return _placementContext.Apply(
                _runtimeContext,
                _activeAsset.placementSettings);

            _startRoutine = null;
            if (_activeAsset == null)
                yield break;

            ShowLetterbox();
            TryEnterCinematicStage();

            if (_caster.PlayerController == null)
            {
                FailStart("플레이어 상태 컨트롤러가 없습니다.");
                yield break;
            }

            _sequenceState = new PlayerUltimateState(_caster.PlayerController);
            _caster.PlayerController.TransitionToState(_sequenceState);
            if (_caster.PlayerController.CurrentState != _sequenceState)
            {
                _sequenceState = null;
                FailStart("Ultimate 전용 상태로 전환하지 못했습니다.");
                yield break;
            }

            // 궁극기는 직전 공격 상태의 워프/공격속도 재생 배율을 이어받지 않는다.
            // 상태 이탈이 잠겨 있거나 입력 잠금으로 상태 갱신이 멈춘 프레임에는
            // PlayerAttackState.OnExit의 속도 복구가 실행되지 않을 수 있다. 이때
            // ActorAnimator의 디렉터 시간은 정상 속도로 흐르지만 Animancer Graph만
            // 직전 WarpPlayRateScale로 느리게 재생되어, 타임라인이 실제 포즈보다 먼저
            // 끝나는 간헐적 조기 종료가 발생한다. 새 MotionSet을 시작하기 직전에
            // 두 시계를 동일한 기본 배율로 명시적으로 맞춘다.
            _animator.MotionTimelineSpeed = 1f;
            _animator.Speed = _caster.LocalTimeScale;

            if (_animator.PlayMotionSetAsset(
                    _activeMotionAsset,
                    _activeMotionKey,
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
            _activeMotionAsset = null;
            _activeMotionKey = default;
            _activeMotionSet = null;

            if (_startRoutine != null)
            {
                StopCoroutine(_startRoutine);
                _startRoutine = null;
            }

            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
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

            if (_runtimeContext != null && _runtimeContext.StageTicket.IsValid)
            {
                Svc.CinematicStage?.Exit(
                    _runtimeContext.StageTicket,
                    MapStageExitReason(reason));
                _runtimeContext.StageTicket = default;
            }

            // 레터박스는 연출 스테이지 Exit(암전 등 퇴장 트랜지션)가 시작된 뒤에 걷는다.
            // 해제 조건은 에셋 상태가 아니라 실제 Show 여부이므로 재생 중 에셋이 바뀌어도 반드시 해제된다.
            HideLetterbox();

            if (stopMotion
                && _animator != null
                && _animator.IsPlayingMotionSet
                && _animator.CurrentMotionSet == endedMotionSet)
            {
                _animator.StopMotionSet();
            }

            _lockContext.Release();
            _placementContext.Restore();
            if (_abilityExecution.IsValid)
            {
                _caster?.Abilities?.EndAbility(
                    _abilityExecution,
                    reason == UltimateSequenceEndReason.Completed);
                _abilityExecution = default;
            }
            if (_runtimeContext != null)
                _runtimeContext.IsInterrupted = reason != UltimateSequenceEndReason.Completed;
            _runtimeContext = null;

            PlayerUltimateState sequenceState = _sequenceState;
            _sequenceState = null;
            sequenceState?.Release();

            if (reason != UltimateSequenceEndReason.Disabled
                && _caster != null
                && _caster.IsAlive()
                && _animator != null)
            {
                if (_caster.PlayerController != null
                    && _caster.PlayerController.CurrentState is not PlayerIdleState)
                {
                    _caster.PlayerController.TransitionToState(ActorStateId.Idle);
                }
                else
                {
                    _animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, endedAsset.motionFadeDuration);
                }
            }

            _isRestoring = false;
            OnSequenceEnded?.Invoke(endedAsset, reason);
        }

        /// <summary>
        /// Ultimate가 재생되는 동안 상태 머신과 지연 애니메이션 콜백이 새 모션을
        /// 덮어쓰지 못하게 하는 실행 전용 상태. 시퀀스 복구 경로만 명시적으로 해제한다.
        /// </summary>
        private sealed class PlayerUltimateState : PlayerActorState
        {
            private bool _canExit;

            public override ActorStateId StateId => ActorStateId.Ultimate;
            protected override ActorStateTag StateTagsCore
                => ActorStateTag.Combat | ActorStateTag.InterruptLocked;

            public PlayerUltimateState(ActorMovementController controller) : base(controller)
            {
            }

            public override bool CanTransitionState(ActorStateId fromState) => true;

            public override bool BlocksExitTo(GameActorState newState)
                => !_canExit && newState?.StateId != ActorStateId.Death;

            public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
            {
                currentVelocity = Vector3.zero;
            }

            public void Release() => _canExit = true;
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

        private void TryEnterCinematicStage()
        {
            CinematicStageSettings settings = _activeAsset?.cinematicStage;
            if (settings?.enabled != true || settings.stage == null || _runtimeContext == null)
                return;

            // 타깃 정책이 확정한 대상 전체를 무대로 옮긴다. 주 대상만 옮기면 범위형 궁극기에서
            // 나머지 대상이 원래 공간에 남아 연출과 히트 대상이 어긋난다.
            _stageTargetBuffer.Clear();
            for (int i = 0; i < _runtimeContext.Targets.Count; i++)
            {
                GameObject target = ResolveTargetActor(_runtimeContext.Targets[i]);
                if (target != null && !_stageTargetBuffer.Contains(target))
                    _stageTargetBuffer.Add(target);
            }

            if (CinematicStageRuntimeUtility.TryEnterWithTargets(
                    settings.stage,
                    this,
                    _caster.gameObject,
                    _stageTargetBuffer,
                    out CinematicStageTicket ticket))
            {
                _runtimeContext.StageTicket = ticket;
            }
            _stageTargetBuffer.Clear();
        }

        private static GameObject ResolveTargetActor(Transform target)
        {
            if (target == null)
                return null;

            return target.GetComponentInParent<GameActor>()?.gameObject ?? target.gameObject;
        }

        private void ShowLetterbox()
        {
            UltimateLetterboxSettings settings = _activeAsset?.letterbox;
            if (settings?.enabled != true)
                return;

            Svc.CinematicStage?.ShowLetterbox(settings);
            // Show 시점의 exitDuration을 캐시해 두어야, 이후 에셋 값이 바뀌어도 같은 연출로 해제된다.
            _letterboxShown = true;
            _letterboxExitDuration = settings.exitDuration;
        }

        private void HideLetterbox()
        {
            if (!_letterboxShown)
                return;

            _letterboxShown = false;
            Svc.CinematicStage?.HideLetterbox(_letterboxExitDuration);
            _letterboxExitDuration = 0f;
        }

        private static CinematicStageExitReason MapStageExitReason(
            UltimateSequenceEndReason reason)
        {
            return reason switch
            {
                UltimateSequenceEndReason.Completed => CinematicStageExitReason.Completed,
                UltimateSequenceEndReason.SceneChanged => CinematicStageExitReason.SceneChanged,
                UltimateSequenceEndReason.Disabled => CinematicStageExitReason.Disabled,
                UltimateSequenceEndReason.Failed => CinematicStageExitReason.Failed,
                _ => CinematicStageExitReason.Interrupted
            };
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
