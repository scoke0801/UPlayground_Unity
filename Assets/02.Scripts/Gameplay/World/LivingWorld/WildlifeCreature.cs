using Animancer;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>서식지가 묶어서 갱신하는 비전투 생물의 배회·도주·귀환·착지를 처리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class WildlifeCreature : MonoBehaviour
    {
        private enum Activity { Roaming, Fleeing, Returning, TakingOff, Landing, Landed }
        [SerializeField] private WildlifeProfileSO _profile;
        [SerializeField] private AnimancerComponent _animation;
        [SerializeField] private NavMeshAgent _agent;
        private Vector3 _home, _target, _landingPoint;
        private float _decisionTime, _idleTime, _calmTime;
        private bool _isInitialized, _isSimulating, _hasLandingPoint;
        private Activity _activity;
        private NavMeshPath _path;
        private bool IsFleeing => _activity == Activity.Fleeing || _activity == Activity.TakingOff;
        public WildlifeProfileSO Profile => _profile;

        /// <summary>최초 서식 중심과 착지점을 보존하고 모션을 초기화한다.</summary>
        public void Initialize()
        {
            if (_isInitialized || _profile == null || _animation == null || _animation.Animator == null) return;
            _home = _target = transform.position;
            if (!_profile.IsFlying) _path = new NavMeshPath();
            if (_profile.IsFlying && _profile.CanLand) FindLandingPoint();
            _animation.ActionOnDisable = AnimancerComponent.DisableAction.Pause;
            _decisionTime = Random.Range(0, _profile.DecisionInterval);
            _idleTime = Random.Range(_profile.IdleSeconds.x, _profile.IdleSeconds.y);
            _calmTime = _profile.CalmSeconds;
            _isInitialized = true;
            SetSimulation(true);
            Play(_profile.IsFlying ? _profile.Move : _profile.Idle);
            if (_animation.States.Current != null)
                _animation.States.Current.NormalizedTime = Random.value;
        }

        /// <summary>플레이어 접근과 안전 유지 시간을 기준으로 이동·모션을 갱신한다.</summary>
        public void Tick(float deltaTime, Vector3 observer, bool hasObserver)
        {
            Initialize();
            if (!_isInitialized || deltaTime <= 0) return;
            float distance = hasObserver ? (observer - transform.position).sqrMagnitude : 0;
            SetSimulation(!hasObserver || distance <= _profile.SimulationDistance * _profile.SimulationDistance);
            if (!_isSimulating) return;
            _decisionTime -= deltaTime;
            _idleTime -= deltaTime;
            bool isThreatNear = hasObserver && HorizontalDistanceSquared(observer, transform.position)
                < _profile.CalmDistance * _profile.CalmDistance;
            _calmTime = isThreatNear ? _profile.CalmSeconds : Mathf.Max(0, _calmTime - deltaTime);
            if (_decisionTime <= 0)
            {
                _decisionTime = _profile.DecisionInterval;
                Decide(observer, hasObserver);
            }
            if (_profile.IsFlying) MoveInAir(deltaTime);
            else UpdateGroundAnimation();
        }

        private void Decide(Vector3 observer, bool hasObserver)
        {
            // 새가 고도를 올린 것만으로 위협을 잊고 플레이어 머리 위에 내려앉지 않게 수평 거리를 쓴다.
            bool isAlert = hasObserver && HorizontalDistanceSquared(observer, transform.position)
                < _profile.AlertDistance * _profile.AlertDistance;
            bool isHomeSafe = !hasObserver || HorizontalDistanceSquared(observer, _home)
                >= _profile.CalmDistance * _profile.CalmDistance;
            if (isAlert && !IsFleeing)
            {
                _calmTime = _profile.CalmSeconds;
                bool mustTakeOff = _hasLandingPoint && transform.position.y < _home.y - _profile.GroundClearance;
                _activity = mustTakeOff ? Activity.TakingOff : Activity.Fleeing;
                _target = new Vector3(transform.position.x, _home.y, transform.position.z);
                _idleTime = 0;
            }
            if (_activity == Activity.TakingOff)
            {
                if (!HasArrived()) return;
                _activity = Activity.Fleeing;
            }
            if (_activity == Activity.Fleeing)
            {
                if (_calmTime <= 0 && isHomeSafe)
                {
                    _activity = Activity.Returning;
                    TrySetDestination(_home);
                }
                else ChooseEscapeDestination(observer);
                return;
            }
            if (_activity == Activity.Returning)
            {
                if (!isHomeSafe)
                {
                    _activity = Activity.Fleeing;
                    _calmTime = _profile.CalmSeconds;
                    ChooseEscapeDestination(observer);
                    return;
                }
                float arrivalDistance = _profile.IsFlying ? 0.05f : Mathf.Max(0.05f, _agent != null ? _agent.stoppingDistance : 0.05f);
                if ((transform.position - _home).sqrMagnitude <= arrivalDistance * arrivalDistance)
                {
                    _activity = _hasLandingPoint ? Activity.Landing : Activity.Roaming;
                    if (_hasLandingPoint) _target = _landingPoint;
                    _idleTime = Random.Range(_profile.IdleSeconds.x, _profile.IdleSeconds.y);
                }
                else TrySetDestination(_home);
                return;
            }
            if (_activity == Activity.Landing)
            {
                if (HasArrived())
                {
                    _activity = Activity.Landed;
                    Play(_profile.Idle);
                }
                return;
            }
            if (_activity == Activity.Landed) return;
            if (_hasLandingPoint && _calmTime <= 0 && isHomeSafe)
            {
                _activity = Activity.Returning;
                TrySetDestination(_home);
                return;
            }
            if (!HasArrived() || _idleTime > 0) return;
            Vector2 random = Random.insideUnitCircle * _profile.RoamRadius;
            Vector3 destination = _home + new Vector3(random.x, 0, random.y);
            if (_profile.IsFlying) destination.y += Random.Range(0, _profile.FlightHeightVariation);
            TrySetDestination(destination);
            _idleTime = Random.Range(_profile.IdleSeconds.x, _profile.IdleSeconds.y);
        }

        private void ChooseEscapeDestination(Vector3 observer)
        {
            Vector3 away = transform.position - observer;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            away.Normalize();
            float radius = Mathf.Max(_profile.RoamRadius, _profile.FleeRadius);
            float currentDistance = HorizontalDistanceSquared(transform.position, observer);
            // 절벽이나 건물에 막힌 정면 대신 좌우의 도달 가능한 도주 지점을 검사한다.
            for (int i = 0; i < 5; i++)
            {
                float angle = i == 0 ? 0 : ((i + 1) / 2) * 35 * (i % 2 == 0 ? -1 : 1);
                Vector3 offset = transform.position + Quaternion.Euler(0, angle, 0) * away * _profile.RoamRadius - _home;
                offset.y = 0;
                Vector3 destination = _home + Vector3.ClampMagnitude(offset, radius);
                if (HorizontalDistanceSquared(destination, observer) <= currentDistance) continue;
                if (TrySetDestination(destination)) return;
            }
        }

        private bool TrySetDestination(Vector3 destination)
        {
            if (_profile.IsFlying)
            {
                _target = destination;
                return true;
            }
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
            float radius = _activity == Activity.Roaming ? _profile.RoamRadius : Mathf.Max(_profile.RoamRadius, _profile.FleeRadius);
            if (!NavMesh.SamplePosition(destination, out var hit, 2, filter)
                || HorizontalDistanceSquared(hit.position, _home) > radius * radius + 0.01f
                || !_agent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete)
                return false;
            _agent.speed = IsFleeing ? _profile.FleeSpeed : _profile.MoveSpeed;
            if (!_agent.SetPath(_path)) return false;
            _target = hit.position;
            return true;
        }

        private bool HasArrived()
        {
            if (_profile.IsFlying) return (transform.position - _target).sqrMagnitude <= 0.0025f;
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _agent.pathPending) return false;
            return HorizontalDistanceSquared(transform.position, _target)
                <= Mathf.Max(0.05f, _agent.stoppingDistance) * Mathf.Max(0.05f, _agent.stoppingDistance);
        }

        private void FindLandingPoint()
        {
            // 걷기 가능한 지면과 정적 충돌면을 함께 확인해 물·움직이는 액터 위 착지를 배제한다.
            var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            if (!Physics.Raycast(_home, Vector3.down, out var ground, _profile.LandingProbeDistance,
                    _profile.LandingMask, QueryTriggerInteraction.Ignore)
                || ground.rigidbody != null || Vector3.Angle(ground.normal, Vector3.up) > _profile.MaxLandingSlope
                || !NavMesh.SamplePosition(ground.point, out var navHit, 0.5f, filter)) return;
            _landingPoint = ground.point + Vector3.up * _profile.GroundClearance;
            _home = new Vector3(navHit.position.x, _landingPoint.y + _profile.FlightHeight, navHit.position.z);
            _hasLandingPoint = true;
        }

        private void MoveInAir(float deltaTime)
        {
            if (_activity == Activity.Landed) return;
            Vector3 direction = _target - transform.position;
            if (direction.sqrMagnitude <= 0.0025f) return;
            float speed = IsFleeing ? _profile.FleeSpeed : _profile.MoveSpeed;
            if (_activity == Activity.Landing) speed = _profile.LandingSpeed;
            float step = Mathf.Min(speed * deltaTime, direction.magnitude);
            if (Physics.SphereCast(transform.position, 0.1f, direction.normalized, out _, step,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // 막힌 귀환도 다음 판단에서 새 경유지를 고르게 하여 영구 정지를 피한다.
                _activity = Activity.Roaming;
                _target = transform.position;
                _idleTime = 0;
                _calmTime = _profile.CalmSeconds;
                return;
            }
            Vector3 forward = direction;
            if (_hasLandingPoint) forward.y = 0;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(forward), _profile.TurnSpeed * deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, _target, step);
            Play(IsFleeing ? _profile.Flee : _profile.Move);
        }

        private void UpdateGroundAnimation()
        {
            bool isMoving = _agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > 0.01f;
            if (isMoving) Play(IsFleeing ? _profile.Flee : _profile.Move);
            else Play(_profile.Feed != null && !IsFleeing ? _profile.Feed : _profile.Idle);
        }

        private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
        {
            Vector3 offset = first - second;
            return offset.x * offset.x + offset.z * offset.z;
        }

        private void Play(AnimationClip clip)
        {
            if (clip == null) return;
            var current = _animation.States.Current;
            if (current != null && current.IsPlaying && current.Clip == clip) return;
            _animation.Play(clip, _profile.CrossFade);
        }

        private void SetSimulation(bool isActive)
        {
            if (_isSimulating == isActive) return;
            _isSimulating = isActive;
            _animation.Animator.enabled = isActive;
            _animation.enabled = isActive;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = !isActive;
        }

        private void OnDisable() => SetSimulation(false);
    }
}
