using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Components.LivingWorld
{
    /// <summary>승인된 두 자리와 접근 경로에 한정해 짧은 비언어 교류를 연결한다.</summary>
    public sealed class NpcSocialPoint : MonoBehaviour
    {
        [SerializeField] private bool _isPlacementReady;
        [SerializeField] private NpcLifeController[] _participants = Array.Empty<NpcLifeController>();
        [SerializeField] private NpcRoutine.Step _first = new();
        [SerializeField] private NpcRoutine.Step _second = new();
        [SerializeField] private MotionSetAsset _greeting;
        [SerializeField] private MotionSetAsset _response;
        private NpcSocialSession _session;
        private readonly Dictionary<(NpcLifeController, NpcLifeController), float> _pairReadyAt = new();

        /// <summary>동시 제안은 안정 ID 순서로 중재하고 두 참여 동의와 자리를 함께 확보한다.</summary>
        public bool TryStart(NpcLifeController proposer)
        {
            if (!_isPlacementReady || !isActiveAndEnabled || _session != null || proposer == null
                || !proposer.CanSocialize || _greeting == null || _response == null
                || _participants.Length != 2 || string.IsNullOrEmpty(proposer.ResidentId)
                || Array.IndexOf(_participants, proposer) < 0) return false;
            for (int i = 0; i < _participants.Length; i++)
            {
                var other = _participants[i];
                if (other == null || other == proposer || !other.CanSocialize
                    || other.NeighborhoodId != proposer.NeighborhoodId
                    || string.CompareOrdinal(proposer.ResidentId, other.ResidentId) >= 0
                    || (other.Routine.ActorPosition - proposer.Routine.ActorPosition).sqrMagnitude
                        > proposer.Profile.socialRadius * proposer.Profile.socialRadius) continue;
                var key = (proposer, other);
                if (_pairReadyAt.TryGetValue(key, out float until) && Time.time < until) continue;
                var session = new NpcSocialSession(this, proposer, other, _greeting, _response);
                // 제안 순서가 바뀌어도 주민에게 저작된 접근 경로와 자리는 유지한다.
                bool usesFirstSeat = _participants[0] == proposer;
                if (!session.TryBegin(usesFirstSeat ? _first : _second, usesFirstSeat ? _second : _first)) continue;
                _session = session;
                return true;
            }
            return false;
        }

        internal void Release(NpcSocialSession session, (NpcLifeController, NpcLifeController) key, float cooldown)
        {
            if (ReferenceEquals(_session, session)) _session = null;
            // 저작된 소수 후보의 쌍만 보관해 전체 주민의 조합이 누적되지 않는다.
            _pairReadyAt[key] = Time.time + cooldown;
        }

        private void OnDisable() { _session?.Cancel(); _session = null; _pairReadyAt.Clear(); }
    }

    /// <summary>진행자 한 명의 기존 상태 틱으로 두 주민의 접근·교대·취소를 진행한다.</summary>
    public sealed class NpcSocialSession
    {
        private readonly NpcSocialPoint _point;
        private readonly NpcLifeController _first, _second;
        private readonly MotionSetAsset _greeting, _response;
        private readonly float _responseAt, _duration;
        private float _elapsed, _socialElapsed;
        private bool _hasStarted, _hasResponded, _isCancelled;

        public NpcSocialSession(NpcSocialPoint point, NpcLifeController first, NpcLifeController second,
            MotionSetAsset greeting, MotionSetAsset response)
        {
            _point = point; _first = first; _second = second; _greeting = greeting; _response = response;
            _responseAt = greeting?.motionSet != null ? Mathf.Max(0f, greeting.motionSet.TotalDuration) : 0f;
            float responseSeconds = response?.motionSet != null ? Mathf.Max(0f, response.motionSet.TotalDuration) : 0f;
            // 프로필의 체류 시간이 짧아도 저작된 다단 모션을 중간에 자르지 않는다.
            _duration = Mathf.Max(Mathf.Min(first.Profile.socialDuration, second.Profile.socialDuration), _responseAt + responseSeconds);
        }

        /// <summary>어느 한 동의·예약·상태 진입이라도 실패하면 양쪽 전체를 반환한다.</summary>
        public bool TryBegin(NpcRoutine.Step first, NpcRoutine.Step second)
        {
            if (!_first.AcceptSession(this)) return false;
            if (!_second.AcceptSession(this)) { Cancel(); return false; }
            if (!_first.Routine.TryReserveSocial(first) || !_second.Routine.TryReserveSocial(second))
            { Cancel(); return false; }
            if (!_first.Routine.BeginReserved() || !_second.Routine.BeginReserved())
            { Cancel(); return false; }
            return true;
        }

        /// <summary>진행자 소실·양쪽 제어권 상실은 같은 취소 경로를 사용한다.</summary>
        public void Tick(NpcLifeController caller, float deltaTime)
        {
            if (_isCancelled) return;
            if (_point == null || !_point.isActiveAndEnabled || _first == null || _second == null
                || !_first.isActiveAndEnabled || !_second.isActiveAndEnabled
                || !_first.Routine.CanRun || !_second.Routine.CanRun
                || !_first.Routine.HasReservation || !_second.Routine.HasReservation)
            { Cancel(); return; }
            if (caller != _first) return;
            _elapsed += deltaTime;
            if (!_hasStarted)
            {
                if (_elapsed >= Mathf.Min(_first.Profile.socialArrivalTimeout, _second.Profile.socialArrivalTimeout))
                { Cancel(); return; }
                if (!_first.Routine.IsWorking || !_second.Routine.IsWorking) return;
                _hasStarted = true;
                _first.Routine.PlayExpression(_greeting);
            }
            _socialElapsed += deltaTime;
            if (!_hasResponded && _socialElapsed >= _responseAt)
            { _hasResponded = true; _second.Routine.PlayExpression(_response); }
            if (_socialElapsed >= _duration) Cancel();
        }

        /// <summary>재진입 취소와 과거 세션의 늦은 통지를 무시하고 두 자리부터 해제한다.</summary>
        public void Cancel()
        {
            if (_isCancelled) return;
            _isCancelled = true;
            if (_first != null) { _first.EndSession(this); _first.Routine.CancelRoutine(); }
            if (_second != null) { _second.EndSession(this); _second.Routine.CancelRoutine(); }
            if (_point != null && _first != null && _second != null)
                _point.Release(this, (_first, _second),
                    Mathf.Max(_first.Profile.socialCooldown, _second.Profile.socialCooldown));
        }
    }
}
