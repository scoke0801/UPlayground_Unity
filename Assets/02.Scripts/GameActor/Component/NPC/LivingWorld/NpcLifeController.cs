using UnityEngine;
using UPlayGround.Data.Actor.NPC;

namespace UPlayGround.Components.LivingWorld
{
    /// <summary>기존 일과와 교류의 진입을 중재하며 이동은 Routine/KCC에 위임한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NpcRoutine))]
    public sealed class NpcLifeController : MonoBehaviour
    {
        [SerializeField] private NpcLifeProfileSO _profile;
        [SerializeField] private string _residentId;
        [SerializeField] private string _neighborhoodId;
        [SerializeField] private NpcSocialPoint _socialPoint;
        private NpcRoutine _routine;
        private float _nextDecision, _nextSocial, _socialReadyAt, _noticeUntil, _noticeReadyAt;
        private NpcSocialSession _session;
        private object _stimulusProducer;
        private long _stimulusRevision = -1;
        private Vector3 _noticePosition;
        private float _noticeStartsAt;
        private bool _hasNoticed;
        public NpcRoutine Routine => _routine;
        public NpcLifeProfileSO Profile => _profile;
        public string ResidentId => _residentId;
        public string NeighborhoodId => _neighborhoodId;
        public bool HasProfile => isActiveAndEnabled && _profile != null;
        internal bool HasTransientActivity => _session != null || Time.time < _noticeUntil;
        public string DecisionReason { get; private set; }
        public ulong Generation { get; private set; }
        public bool CanSocialize => HasProfile && _profile.canSocialize && _session == null
            && Time.time >= _socialReadyAt && _routine != null && _routine.CanYield;

        private void OnEnable()
        {
            _routine = GetComponent<NpcRoutine>();
            if (_routine.Life != null && _routine.Life != this) return;
            _routine.Life = this;
            _routine.Cancelled += OnCancelled;
            _nextDecision = Time.time + (_profile != null ? Random.value * _profile.initialSpread : 0f);
            _nextSocial = _nextDecision;
            _socialReadyAt = _nextDecision;
            _noticeUntil = 0f;
        }

        /// <summary>Idle 상태의 기존 틱에서만 판단하며 별도 Update를 만들지 않는다.</summary>
        public bool TryStart()
        {
            if (!HasProfile) return _routine.TryStartSelected();
            if (!_routine.CanRun) { DecisionReason = "상위 제어 또는 준비 대기"; CancelSocial(); return false; }
            if (Time.time < _noticeUntil)
            {
                if (!_hasNoticed && Time.time >= _noticeStartsAt)
                { _hasNoticed = true; _routine.LookAtInterest(_noticePosition); }
                return false;
            }
            if (Time.time < _nextDecision) return false;
            _nextDecision = Time.time + _profile.decisionInterval;
            if (CanSocialize && _socialPoint != null && Time.time >= _nextSocial)
            {
                _nextSocial = Time.time + _profile.socialSearchInterval;
                if (_socialPoint.TryStart(this)) { DecisionReason = "이웃 교류"; return true; }
            }
            DecisionReason = _routine.NeedsShelter ? "날씨 피신" : "시간대 일과";
            return _routine.TryStartSelected();
        }

        internal bool AcceptSession(NpcSocialSession session)
        {
            if (!CanSocialize) return false;
            _session = session;
            Generation++;
            return true;
        }

        internal void EndSession(NpcSocialSession session)
        {
            if (!ReferenceEquals(_session, session)) return;
            _session = null;
            Generation++;
            _socialReadyAt = Time.time + _profile.socialCooldown;
        }

        internal void TickSession(float deltaTime) => _session?.Tick(this, deltaTime);
        private void CancelSocial() => _session?.Cancel();
        private void OnCancelled() { Generation++; _noticeUntil = 0f; CancelSocial(); }

        /// <summary>생산자 교체 전 입력을 거부하기 위해 바인딩 토큰을 교체한다.</summary>
        public void BindStimulus(object producer)
        {
            if (ReferenceEquals(producer, _stimulusProducer)) return;
            _stimulusProducer = producer; _stimulusRevision = -1;
        }
        public void ReleaseStimulus(object producer)
        { if (ReferenceEquals(producer, _stimulusProducer)) BindStimulus(null); }

        /// <summary>맵·생산자·사건 순서·유효 시각을 확인한 소리만 소비한다.</summary>
        public bool ConsumeStimulus(object producer, string mapId, long eventId, Vector3 position,
            float occurredAt, float expiresAt, float importance)
        {
            if (producer == null || !ReferenceEquals(producer, _stimulusProducer) || _routine == null
                || mapId != _routine.MapId || eventId <= _stimulusRevision
                || !float.IsFinite(occurredAt) || !float.IsFinite(expiresAt) || !float.IsFinite(importance)
                || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)
                || occurredAt > Time.time || expiresAt <= Time.time || expiresAt <= occurredAt
                || importance <= 0f) return false;
            _stimulusRevision = eventId;
            return TryNotice(position);
        }

        /// <summary>기존 생산자가 검증한 국소 소리를 자유 활동의 짧은 관심으로 소비한다.</summary>
        public bool TryNotice(Vector3 position)
        {
            if (!HasProfile || !_routine.CanYield || _session != null || Time.time < _noticeReadyAt
                || (position - _routine.ActorPosition).sqrMagnitude > _profile.noticeRadius * _profile.noticeRadius
                || Random.value > _profile.noticeProbability) return false;
            _noticeReadyAt = Time.time + _profile.noticeCooldown;
            _noticeUntil = Time.time + _profile.noticeDelay + _profile.noticeSeconds;
            _noticeStartsAt = Time.time + _profile.noticeDelay;
            _noticePosition = position;
            _hasNoticed = false;
            DecisionReason = "주변 소리 관심 후 생활 재선택";
            return true;
        }

        private void OnDisable()
        {
            CancelSocial();
            if (_routine == null) return;
            _routine.Cancelled -= OnCancelled;
            if (_routine.Life == this) _routine.Life = null;
            _routine.CancelRoutine();
        }
    }
}
