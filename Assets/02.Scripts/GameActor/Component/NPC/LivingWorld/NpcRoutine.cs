using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Components.LivingWorld
{
    /// <summary>기존 NPC에 시간대별 작업·이동·휴식을 연결하는 씬 바인딩. 서사와 보상은 소유하지 않는다.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcRoutine : MonoBehaviour
    {
        /// <summary>이전 작업점에서 출발하는 검증된 경유점과 작업 체류 시간이다.</summary>
        [Serializable]
        public sealed class Step
        {
            public NpcWorkPoint point;
            public Transform[] waypoints = Array.Empty<Transform>();
            [Min(0.1f)] public float seconds = 15f;
        }

        [SerializeField] private NpcActor _actor;
        [SerializeField] private string _mapId;
        [Tooltip("서사 배치가 허용한 뒤에만 일과를 시작한다. 준비 전에는 false를 유지한다.")]
        [SerializeField] private bool _isPlacementReady;
        [SerializeField] private string _requiredFlag;
        [SerializeField] private string _blockedFlag;
        [SerializeField] private Step[] _steps = Array.Empty<Step>();
        [SerializeField] private Step _rest = new();
        [SerializeField] private Transform _propSocket;
        [SerializeField] private int _prepareMinute = 360;
        [SerializeField] private int _workMinute = 540;
        [SerializeField] private int _restMinute = 1080;
        [SerializeField] private int _nightMinute = 1320;
        [Tooltip("시작과 중단 후 재시도의 주민별 시간차")]
        [Min(0f), SerializeField] private float _startDelaySeconds;
        [Min(0.1f), SerializeField] private float _retrySeconds = 5f;
        [Min(0.1f), SerializeField] private float _travelTimeout = 20f;
        [Min(0.1f), SerializeField] private float _stuckSeconds = 2f;
        [Min(0.01f), SerializeField] private float _progressDistance = 0.1f;
        [Min(0.05f), SerializeField] private float _arrivalDistance = 0.35f;
        [Min(0.05f), SerializeField] private float _heightTolerance = 0.5f;
        [Min(0.1f), SerializeField] private float _speedMultiplier = 0.6f;
        [Min(0f), SerializeField] private float _fadeSeconds = 0.25f;

        private NpcMovementController _controller;
        private NpcBrain _brain;
        private IGameTimeService _time;
        private IGlobalFlagService _flags;
        private Coroutine _binding;
        private IDisposable _simulationLease;
        private Step _activeStep;
        private ulong _reservation;
        private GameObject _prop;
        private int _cursor, _waypoint, _period = -1;
        private float _retryAt, _elapsed, _stuckElapsed, _bestDistance;
        private bool _isWorking, _needsShelter, _isRiskActive, _useRest;
        private long _environmentRevision = -1;
        private object _environmentProducer;
        private int _resumeAfterFrame;

        public bool HasReservation => _activeStep?.point != null && _activeStep.point.IsOwnedBy(this, _reservation);
        public bool IsWorking => _isWorking && HasReservation;
        /// <summary>배치가 준비된 일과만 일반 배회를 대신한다. 일시 중단 중에도 이동 소유권은 유지한다.</summary>
        public bool HasMovementControl => isActiveAndEnabled && _isPlacementReady;
        public bool CanRun => HasMovementControl && _actor != null && _actor.isActiveAndEnabled
            && _time != null && _flags != null && !_actor.IsRoutinePaused
            && !_actor.IsInteracting() && !_actor.IsDialogueStaged
            && (!Services.TryGet<IDialogueService>(out var dialogue) || !dialogue.IsDialogueActive)
            && (string.IsNullOrEmpty(_requiredFlag) || _flags.GetFlag(_requiredFlag))
            && (string.IsNullOrEmpty(_blockedFlag) || !_flags.GetFlag(_blockedFlag));
        public float SpeedMultiplier => _speedMultiplier;
        public float ArrivalDistance => _arrivalDistance;
        public float FadeSeconds => _fadeSeconds;
        public bool NeedsShelter => _needsShelter;
        public Transform Target => !HasReservation ? null : _waypoint < _activeStep.waypoints.Length
            ? _activeStep.waypoints[_waypoint] : _activeStep.point.transform;

        private void OnEnable()
        {
            _period = -1;
            _cursor = 0;
            _useRest = false;
            _retryAt = Time.time + _startDelaySeconds;
            _binding = StartCoroutine(BindServices());
            if (_actor != null) TryBindActor(_actor);
        }

        /// <summary>프리팹에 몸을 복제하지 않고 씬의 기존 주민 한 명만 연결한다.</summary>
        public bool TryBindActor(NpcActor actor)
        {
            if (actor == null || !isActiveAndEnabled) return false;
            var controller = actor.GetComponent<NpcMovementController>();
            var brain = actor.GetComponent<NpcBrain>();
            if (controller == null || brain == null || controller.Routine != null && controller.Routine != this) return false;
            DetachActor();
            _actor = actor;
            _controller = controller;
            _brain = brain;
            _controller.Routine = this;
            _actor.OnRoutineInterrupted += CancelRoutine;
            return true;
        }

        private IEnumerator BindServices()
        {
            while (!Services.TryGet(out _time) || !Services.TryGet(out _flags)) yield return null;
            _time.OnGameMinuteChanged += OnMinuteChanged;
            _flags.OnFlagsReloaded += OnFlagsReloaded;
            _flags.OnFlagChanged += OnFlagChanged;
            OnMinuteChanged(_time.CurrentDay, _time.MinuteOfDay);
            _binding = null;
        }

        /// <summary>I가 서사 배치와 보행 구간을 연결한 뒤 시작 가능 여부를 전달한다.</summary>
        public void SetPlacementReady(bool ready)
        {
            _isPlacementReady = ready;
            CancelRoutine();
        }

        /// <summary>환경 생산자 교체 시 이전 생산자의 늦은 통지를 차단한다.</summary>
        public void BindEnvironment(object producer)
        {
            if (ReferenceEquals(producer, _environmentProducer)) return;
            _environmentProducer = producer;
            _environmentRevision = -1;
        }

        /// <summary>현재 생산자만 해제하며 새 생산자의 입력을 지우지 않는다.</summary>
        public void ReleaseEnvironment(object producer)
        {
            if (ReferenceEquals(producer, _environmentProducer)) BindEnvironment(null);
        }

        /// <summary>D 또는 독립 검사 입력을 소비한다. 날씨를 생성하거나 저장하지 않는다.</summary>
        public bool ApplyEnvironment(object producer, string mapId, long revision, bool isReady, bool needsShelter)
        {
            if (producer == null || !ReferenceEquals(producer, _environmentProducer)
                || mapId != _mapId || revision <= _environmentRevision) return false;
            _environmentRevision = revision;
            // 미준비 통지는 진행 중인 안전 행동과 마지막 준비 상태를 유지한다.
            if (!isReady || _needsShelter == needsShelter) return true;
            _needsShelter = needsShelter;
            CancelRoutine();
            return true;
        }

        /// <summary>위험 발생 시 소품·예약을 먼저 정리한다. 진행 중 대화는 강제로 덮어쓰지 않는다.</summary>
        public void SetRiskActive(bool active)
        {
            if (_isRiskActive == active) return;
            _isRiskActive = active;
            CancelRoutine();
        }

        private void OnMinuteChanged(int day, float minute)
        {
            int period = minute < _prepareMinute || minute >= _nightMinute ? 3
                : minute < _workMinute ? 0 : minute < _restMinute ? 1 : 2;
            if (_period == period) return;
            _period = period;
            _cursor = 0;
            _useRest = false;
            CancelRoutine();
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (key == _requiredFlag || key == _blockedFlag) CancelRoutine();
        }

        private void OnFlagsReloaded()
        {
            _cursor = 0;
            _useRest = false;
            // 환경 생산자가 복원 순서와 무관하게 마지막 준비 상태를 소유한다.
            CancelRoutine();
            _resumeAfterFrame = Time.frameCount + 2;
        }

        /// <summary>Idle 또는 Wander에서 예약을 얻은 경우에만 일과 상태로 진입한다.</summary>
        public bool TryStart()
        {
            if (!CanRun || _controller == null || _controller.Motor == null || Time.time < _retryAt
                || Time.frameCount < _resumeAfterFrame) return false;
            if (HasReservation) return true;
            bool rest = _isRiskActive || _needsShelter || _period >= 2 || _useRest || _steps.Length == 0;
            Step step = rest ? _rest : _steps[_period == 0 ? 0 : _cursor % _steps.Length];
            if ((_isRiskActive || _needsShelter) && (_rest?.point == null || !_rest.point.IsSheltered))
            { _retryAt = Time.time + _retrySeconds; return false; }
            if (!TryReserve(step))
            {
                _useRest = true;
                if (rest || !TryReserve(_rest)) { _retryAt = Time.time + _retrySeconds; return false; }
            }
            if (Services.TryGet<IActorSimulationService>(out var simulation))
                _simulationLease = simulation.AcquireActiveLease(_actor, this, "NpcRoutine");
            if (_controller.TransitionToState(new NpcRoutineState(_controller, this))) return true;
            CancelRoutine();
            return false;
        }

        private bool TryReserve(Step step)
        {
            if (step?.point == null || step.point.MapId != _mapId || step.waypoints == null) return false;
            if (float.IsNaN(step.seconds) || float.IsInfinity(step.seconds) || step.seconds <= 0f) return false;
            for (int i = 0; i < step.waypoints.Length; i++) if (step.waypoints[i] == null) return false;
            if (step.point.HeldPropPrefab != null && _propSocket == null) return false;
            if (!step.point.TryReserve(this, out _reservation)) return false;
            _activeStep = step;
            _waypoint = 0;
            _elapsed = _stuckElapsed = 0f;
            _bestDistance = float.PositiveInfinity;
            _isWorking = false;
            return true;
        }

        /// <summary>기존 이동 상태의 틱에서 도착·정체·작업 종료를 검사한다.</summary>
        public bool Tick(float deltaTime)
        {
            if (!CanRun || !HasReservation || Target == null) { CancelRoutine(); return false; }
            _elapsed += deltaTime;
            if (_isWorking)
            {
                if (_elapsed < _activeStep.seconds) return true;
                bool wasRest = ReferenceEquals(_activeStep, _rest);
                CancelRoutine();
                if (!wasRest && _steps.Length > 0) _cursor = (_cursor + 1) % _steps.Length;
                _useRest = !wasRest && _cursor == 0;
                return false;
            }
            Vector3 offset = Target.position - _controller.Motor.TransientPosition;
            float distance = offset.magnitude;
            _stuckElapsed += deltaTime;
            if (distance + _progressDistance < _bestDistance)
            { _bestDistance = distance; _stuckElapsed = 0f; }
            if (_elapsed >= _travelTimeout || _stuckElapsed >= _stuckSeconds)
            { _useRest = true; CancelRoutine(); return false; }
            if (Vector3.ProjectOnPlane(offset, _controller.Motor.CharacterUp).magnitude > _arrivalDistance
                || Mathf.Abs(Vector3.Dot(offset, _controller.Motor.CharacterUp)) > _heightTolerance) return true;
            if (!_controller.Motor.GroundingStatus.IsStableOnGround) return true;
            if (_waypoint < _activeStep.waypoints.Length)
            { _waypoint++; _bestDistance = float.PositiveInfinity; _stuckElapsed = 0f; return true; }
            if (Quaternion.Angle(_controller.Motor.TransientRotation, Target.rotation) > _brain.WorkFacingTolerance)
            { _stuckElapsed = 0f; return true; }
            _isWorking = true;
            _elapsed = 0f;
            if (_activeStep.point.HeldPropPrefab != null)
                _prop = Instantiate(_activeStep.point.HeldPropPrefab, _propSocket, false);
            if (_activeStep.point.Motion != null) _actor.Animator?.PlayMotion(_activeStep.point.Motion, _fadeSeconds);
            else _actor.Animator?.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, _fadeSeconds);
            return true;
        }

        /// <summary>대화·취소·로드·비활성화에서 임시 소유권만 정리한다. 완료 사실은 쓰지 않는다.</summary>
        public void CancelRoutine()
        {
            if (_activeStep?.point != null) _activeStep.point.Release(this, _reservation);
            _activeStep = null;
            _isWorking = false;
            if (_prop != null) { _prop.SetActive(false); Destroy(_prop); }
            _prop = null;
            _simulationLease?.Dispose();
            _simulationLease = null;
            _retryAt = Mathf.Max(_retryAt, Time.time + _retrySeconds + _startDelaySeconds);
        }

        /// <summary>정면 충돌 시 밀기를 계속하지 않고 다음 시도에서 휴식점을 우선한다.</summary>
        public void ReportBlocked()
        {
            _useRest = true;
            CancelRoutine();
        }

        private void DetachActor()
        {
            CancelRoutine();
            if (_actor != null) _actor.OnRoutineInterrupted -= CancelRoutine;
            if (_controller != null && _controller.Routine == this) _controller.Routine = null;
            _controller = null;
        }

        private void OnDisable()
        {
            DetachActor();
            if (_binding != null) StopCoroutine(_binding);
            _binding = null;
            if (_time != null) _time.OnGameMinuteChanged -= OnMinuteChanged;
            if (_flags != null)
            {
                _flags.OnFlagsReloaded -= OnFlagsReloaded;
                _flags.OnFlagChanged -= OnFlagChanged;
            }
            _time = null;
            _flags = null;
        }
    }
}
