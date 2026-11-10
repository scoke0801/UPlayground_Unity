using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    /// <summary>펫의 이동 목표를 KCC 속도·회전 콜백으로만 적용한다.</summary>
    public sealed class PetMovementController : ActorMovementController
    {
        private PetIdleState _idle;
        private PetMoveState _move;
        private Vector3 _waypoint;
        private float _targetSpeed;
        private bool _hasWaypoint;
        public PetFollowProfileSO Profile { get; private set; }
        public Vector3 DesiredVelocity { get; private set; }

        public void Configure(PetFollowProfileSO profile)
        {
            Profile = profile;
            Motor.SetCapsuleDimensions(profile.capsuleRadius, profile.capsuleHeight, profile.capsuleHeight * 0.5f);
            _idle ??= new PetIdleState(this);
            _move ??= new PetMoveState(this);
            SetDesiredVelocity(Vector3.zero);
            if (CurrentState == null) TransitionToState(_idle);
        }

        public void SetDesiredVelocity(Vector3 velocity)
        {
            _hasWaypoint = false;
            DesiredVelocity = velocity;
            if (_idle == null) return;
            GameActorState state = velocity.sqrMagnitude > 0.001f ? _move : _idle;
            if (!ReferenceEquals(CurrentState, state)) TransitionToState(state);
        }

        /// <summary>저주기 경로 결정을 받아 실제 방향과 제동은 KCC 주기로 갱신한다.</summary>
        public void SetMoveTarget(Vector3 waypoint, float speed)
        {
            _waypoint = waypoint; _targetSpeed = speed; _hasWaypoint = true;
            RefreshDesiredVelocity();
            if (_move != null && !ReferenceEquals(CurrentState, _move)) TransitionToState(_move);
        }

        public void RefreshDesiredVelocity()
        {
            if (!_hasWaypoint || Profile == null) return;
            Vector3 direction = _waypoint - Motor.TransientPosition;
            direction = Vector3.ProjectOnPlane(direction, Motor.CharacterUp);
            float distance = Mathf.Max(0f, direction.magnitude - Profile.cornerDistance * 0.5f);
            float speed = Mathf.Min(_targetSpeed, Mathf.Sqrt(2f * Profile.acceleration * distance));
            DesiredVelocity = direction.normalized * speed;
        }

        public override bool IsColliderValidForCollisions(Collider coll)
        {
            if (coll == null || Profile == null) return false;
            return (Profile.groundMask.value & (1 << coll.gameObject.layer)) != 0
                && base.IsColliderValidForCollisions(coll);
        }
    }
}
