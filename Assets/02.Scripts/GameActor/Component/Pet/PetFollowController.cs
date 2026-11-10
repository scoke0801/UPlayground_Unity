using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.Pet;
using UPlayGround.MovementController;

namespace UPlayGround.Components.Pet
{
    /// <summary>재사용 경로 버퍼로 추종 후보·진행·안전 복귀를 판단한다.</summary>
    public sealed class PetFollowController
    {
        private readonly PetMovementController _movement;
        private readonly PetFollowProfileSO _profile;
        private readonly NavMeshPath _path = new();
        private readonly NavMeshPath _probe = new();
        private readonly Vector3[] _corners = new Vector3[64];
        private readonly Collider[] _overlaps = new Collider[16];
        private Vector3 _lastHeading = Vector3.forward;
        private Vector3 _lastOwnerPosition;
        private Vector3 _pathTarget;
        private Vector3 _progressPosition;
        private float _progressRemaining;
        private float _stuckTime;
        private int _corner;
        private int _cornerCount;
        private bool _isMoving;
        private bool _hasOwnerPosition;
        private float _side = 1f;
        public bool NeedsRecovery { get; private set; }
        private NavMeshQueryFilter Filter => new() { agentTypeID = _profile.navAgentTypeId, areaMask = _profile.navAreaMask };

        public PetFollowController(PetMovementController movement, PetFollowProfileSO profile)
        { _movement = movement; _profile = profile; }

        public void Stop()
        {
            _isMoving = false; _cornerCount = 0; _stuckTime = 0f;
            NeedsRecovery = false;
            _movement.SetDesiredVelocity(Vector3.zero);
        }

        /// <summary>카메라 회전 대신 플레이어의 실제 이동 방향을 기준으로 뒤쪽을 유지한다.</summary>
        public bool TryGetFollowPoint(Transform owner, out Vector3 point)
        {
            Vector3 movement = _hasOwnerPosition ? owner.position - _lastOwnerPosition : owner.forward;
            movement.y = 0f;
            if (movement.sqrMagnitude > _profile.progressDistance * _profile.progressDistance)
                _lastHeading = movement.normalized;
            _lastOwnerPosition = owner.position; _hasOwnerPosition = true;
            Vector3 right = Vector3.Cross(Vector3.up, _lastHeading);
            Vector3 center = owner.position - _lastHeading * _profile.rearDistance;
            if (TrySafePoint(center + right * (_profile.sideDistance * _side), owner.position, out point)) return true;
            if (!TrySafePoint(center - right * (_profile.sideDistance * _side), owner.position, out point)) return false;
            _side = -_side;
            return true;
        }

        /// <summary>완전 경로와 캡슐 여유 공간이 있는 지면만 복귀 후보로 인정한다.</summary>
        public bool TrySafePoint(Vector3 candidate, Vector3 ownerGround, out Vector3 point)
        {
            point = default;
            if (!NavMesh.SamplePosition(candidate, out var sample, _profile.navSampleRadius, Filter)
                || Mathf.Abs(sample.position.y - candidate.y) > _profile.maxGroundDifference) return false;
            if (!Physics.Raycast(sample.position + Vector3.up * _profile.maxGroundDifference, Vector3.down,
                out var ground, _profile.maxGroundDifference * 2f, _profile.groundMask, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(ground.normal, Vector3.up) > _profile.maxSlope) return false;
            float radius = _profile.capsuleRadius;
            Vector3 lower = ground.point + Vector3.up * (radius + 0.02f);
            Vector3 upper = ground.point + Vector3.up * Mathf.Max(radius, _profile.capsuleHeight - radius);
            int overlaps = Physics.OverlapCapsuleNonAlloc(lower, upper, radius, _overlaps,
                _profile.obstructionMask, QueryTriggerInteraction.Ignore);
            if (overlaps >= _overlaps.Length || overlaps > 0) return false;
            if (!NavMesh.SamplePosition(ownerGround, out var owner, _profile.navSampleRadius, Filter)
                || !NavMesh.CalculatePath(owner.position, sample.position, Filter, _probe)
                || _probe.status != NavMeshPathStatus.PathComplete) return false;
            point = ground.point;
            return true;
        }

        public bool CanReach(Vector3 target)
        {
            if (!NavMesh.SamplePosition(_movement.transform.position, out var start, _profile.navSampleRadius, Filter)
                || !NavMesh.SamplePosition(target, out var end, _profile.navSampleRadius, Filter)
                || (end.position - target).sqrMagnitude > _profile.maxGroundDifference * _profile.maxGroundDifference)
                return false;
            return NavMesh.CalculatePath(start.position, end.position, Filter, _probe)
                && _probe.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>불완전 경로는 이동하지 않으며 막힘과 큰 이탈은 복귀 요청으로 전환한다.</summary>
        public bool Tick(Vector3 target, float deltaTime, float arrivalDistance, bool useHysteresis)
        {
            Vector3 current = _movement.transform.position;
            float distance = Vector3.Distance(current, target);
            float resume = useHysteresis ? Mathf.Max(arrivalDistance, _profile.resumeDistance) : arrivalDistance;
            if (distance <= arrivalDistance || !_isMoving && distance <= resume)
            { Stop(); NeedsRecovery = false; return true; }
            if (_cornerCount == 0 || (target - _pathTarget).sqrMagnitude >= _profile.pathRefreshDistance * _profile.pathRefreshDistance)
            {
                if (!BuildPath(current, target)) { Stop(); NeedsRecovery = true; return false; }
            }
            _isMoving = true;
            while (_corner < _cornerCount && (current - _corners[_corner]).sqrMagnitude <= _profile.cornerDistance * _profile.cornerDistance) _corner++;
            if (_corner >= _cornerCount)
            { Stop(); NeedsRecovery = true; return false; }
            float remaining = Vector3.Distance(current, _corners[_corner]);
            if ((current - _progressPosition).sqrMagnitude >= _profile.progressDistance * _profile.progressDistance
                && remaining < _progressRemaining - _profile.progressDistance)
            { _progressPosition = current; _stuckTime = 0f; }
            else _stuckTime += deltaTime;
            _progressRemaining = remaining;
            if (_stuckTime >= _profile.stuckSeconds)
            { Stop(); NeedsRecovery = true; return false; }
            float speed = distance > _profile.runDistance ? _profile.runSpeed : _profile.walkSpeed;
            _movement.SetMoveTarget(_corners[_corner], speed);
            NeedsRecovery = false;
            return true;
        }

        private bool BuildPath(Vector3 current, Vector3 target)
        {
            if (!NavMesh.SamplePosition(current, out var start, _profile.navSampleRadius, Filter)
                || !NavMesh.SamplePosition(target, out var end, _profile.navSampleRadius, Filter)
                || !NavMesh.CalculatePath(start.position, end.position, Filter, _path)
                || _path.status != NavMeshPathStatus.PathComplete) return false;
            int count = _path.GetCornersNonAlloc(_corners);
            if (count == 0 || count >= _corners.Length) return false;
            if (!_isMoving) { _progressPosition = current; _stuckTime = 0f; }
            _cornerCount = count; _corner = 0; _pathTarget = target;
            _progressRemaining = float.MaxValue;
            return true;
        }
    }
}
