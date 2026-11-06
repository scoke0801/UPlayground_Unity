using Animancer;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>서식지가 묶어서 갱신하는 환경 생물의 배회·도주·귀환·착지를 처리한다.</summary>
    [DisallowMultipleComponent]
    public sealed partial class WildlifeCreature : MonoBehaviour
    {
        private const float AirObstacleRadius = 0.1f;
        private enum Activity { Roaming, Fleeing, Returning, TakingOff, Landing, Landed, Watching, Avoiding }
        private enum RestActivity { Rest, Feed, Observe }
        [SerializeField] private WildlifeProfileSO _profile;
        [SerializeField] private AnimancerComponent _animation;
        [SerializeField] private NavMeshAgent _agent;
        private Vector3 _home, _target, _landingPoint;
        private float _decisionTime, _idleTime, _calmTime;
        private bool _isInitialized, _isSimulating, _hasLandingPoint;
        private Activity _activity;
        private NavMeshPath _path;
        private WildlifeCreature[] _neighbours;
        private WildlifeHabitat _habitat;
        private readonly Vector3[] _pathCorners = new Vector3[64];
        private bool _isResting = true;
        private RestActivity _restActivity;
        private float _watchTime;
        private Vector3 _watchPosition;
        private int _forageStepsRemaining;
        private bool _isForaging;
        private float _airSpeed;
        private bool IsFleeing => _activity == Activity.Fleeing || _activity == Activity.TakingOff;
        public WildlifeProfileSO Profile => _profile;

        /// <summary>서식지의 캐시를 공유해 판단 주기에만 이웃을 검사한다.</summary>
        public void BindHabitat(WildlifeHabitat habitat, WildlifeCreature[] creatures)
        {
            _habitat = habitat;
            _neighbours = creatures;
        }

        /// <summary>최초 서식 중심과 착지점을 보존하고 모션을 초기화한다.</summary>
        public void Initialize()
        {
            if (_isInitialized || _profile == null || _animation == null || _animation.Animator == null) return;
            if (!_profile.IsFlying) _path = new NavMeshPath();
            if (_agent != null && _profile.UseStagedResponse)
            {
                _agent.acceleration = _profile.Acceleration;
                _agent.angularSpeed = _profile.TurnSpeed;
                if (_profile.UseStagedResponse) _agent.updateRotation = false;
            }
            if (!_profile.IsFlying && _profile.ProjectGroundMovement && _agent != null)
            {
                _agent.updatePosition = false;
                UpdateGroundPosition();
            }
            _home = _target = transform.position;
            if (_profile.IsFlying && _profile.CanLand) FindLandingPoint();
            _animation.ActionOnDisable = AnimancerComponent.DisableAction.Pause;
            _decisionTime = Random.Range(0, _profile.DecisionInterval);
            BeginRest();
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
            if (_isDead) { UpdateDeath(deltaTime); return; }
            if (_hitThreatSeconds > 0)
            {
                _hitThreatSeconds -= deltaTime;
                observer = _hitThreatPosition;
                hasObserver = true;
            }
            Initialize();
            if (!_isInitialized || deltaTime <= 0) return;
            float distance = hasObserver ? (observer - transform.position).sqrMagnitude : 0;
            SetSimulation(!hasObserver || distance <= _profile.SimulationDistance * _profile.SimulationDistance);
            if (!_isSimulating) return;
            _decisionTime -= deltaTime;
            UpdateRest(deltaTime);
            if (_activity == Activity.Watching) _watchTime += deltaTime;
            bool isThreatNear = hasObserver && HorizontalDistanceSquared(observer, transform.position)
                < _profile.CalmDistance * _profile.CalmDistance;
            _calmTime = isThreatNear ? _profile.CalmSeconds : Mathf.Max(0, _calmTime - deltaTime);
            if (_decisionTime <= 0)
            {
                _decisionTime = _profile.DecisionInterval;
                Decide(observer, hasObserver);
            }
            if (_profile.IsFlying)
            {
                MoveInAir(deltaTime);
                UpdateLandedHops(deltaTime);
            }
            else
            {
                if (_activity == Activity.Watching)
                {
                    Vector3 direction = _watchPosition - transform.position;
                    direction.y = 0;
                    if (direction.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                            Quaternion.LookRotation(direction), _profile.TurnSpeed * deltaTime);
                }
                UpdateGroundPosition();
                UpdateGroundSteering(deltaTime);
                UpdateGroundAnimation();
            }
        }

        private void Decide(Vector3 observer, bool hasObserver)
        {
            // 새가 고도를 올린 것만으로 위협을 잊고 플레이어 머리 위에 내려앉지 않게 수평 거리를 쓴다.
            bool isAlert = hasObserver && HorizontalDistanceSquared(observer, transform.position)
                < _profile.AlertDistance * _profile.AlertDistance;
            bool isHomeSafe = !hasObserver || HorizontalDistanceSquared(observer, _home)
                >= _profile.CalmDistance * _profile.CalmDistance;
            if (!_profile.IsFlying && _profile.UseStagedResponse && UpdateThreat(observer, hasObserver)) return;
            if (isAlert && !IsFleeing)
            {
                _calmTime = _profile.CalmSeconds;
                bool mustTakeOff = _hasLandingPoint && transform.position.y < _home.y - _profile.GroundClearance;
                _activity = mustTakeOff ? Activity.TakingOff : Activity.Fleeing;
                _isHopping = false;
                _target = GetTakeoffTarget(observer, hasObserver, mustTakeOff);
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
                    TryReturnHome();
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
                float homeRadius = Mathf.Max(arrivalDistance, Mathf.Min(_profile.RoamRadius, _profile.PersonalSpace * 2));
                if (HasArrived() && HorizontalDistanceSquared(transform.position, _home) <= homeRadius * homeRadius)
                {
                    _activity = _hasLandingPoint ? Activity.Landing : Activity.Roaming;
                    if (_hasLandingPoint) _target = _landingPoint;
                    _idleTime = Random.Range(_profile.IdleSeconds.x, _profile.IdleSeconds.y);
                    _isResting = false;
                }
                else TryReturnHome();
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
                TryReturnHome();
                return;
            }
            if (!HasArrived() || _idleTime > 0) return;
            Vector3 destination = ChooseRoamDestination();
            if (_profile.IsFlying) destination.y += Random.Range(0, _profile.FlightHeightVariation);
            if (TrySetDestination(destination))
            {
                _isResting = false;
                if (_forageStepsRemaining <= 0) _isForaging = false;
            }
        }

        private void UpdateRest(float deltaTime)
        {
            if (_activity != Activity.Roaming || !HasArrived()) return;
            if (!_isResting)
            {
                _isResting = true;
                BeginRest();
            }
            _idleTime = Mathf.Max(0, _idleTime - deltaTime);
            if (_idleTime <= 0 && _isForaging && _restActivity == RestActivity.Feed)
            {
                _restActivity = RestActivity.Observe;
                _idleTime = Random.Range(_profile.ObserveSeconds.x, _profile.ObserveSeconds.y);
            }
        }

        private void BeginRest()
        {
            if (_isForaging && _forageStepsRemaining > 0)
            {
                _restActivity = RestActivity.Feed;
                _forageStepsRemaining--;
            }
            else
            {
                _restActivity = (RestActivity)(((int)_restActivity + Random.Range(1, 3)) % 3);
                if (_restActivity == RestActivity.Feed && _profile.Feed == null) _restActivity = RestActivity.Rest;
                _isForaging = _restActivity == RestActivity.Feed && !_profile.IsFlying
                    && _profile.ForageSteps.y > 0 && _profile.ForageStepDistance.y > 0;
                _forageStepsRemaining = _isForaging
                    ? Random.Range(Mathf.Max(1, _profile.ForageSteps.x), _profile.ForageSteps.y + 1) - 1 : 0;
            }
            Vector2 duration = _profile.IdleSeconds;
            if (_restActivity == RestActivity.Feed) duration = _profile.FeedSeconds;
            if (_restActivity == RestActivity.Observe) duration = _profile.ObserveSeconds;
            _idleTime = Random.Range(duration.x, duration.y);
        }

        private Vector3 ChooseRoamDestination()
        {
            if (_isForaging && _forageStepsRemaining > 0)
            {
                Vector2 step = Random.insideUnitCircle.normalized
                    * Random.Range(_profile.ForageStepDistance.x, _profile.ForageStepDistance.y);
                Vector3 offset = transform.position + new Vector3(step.x, 0, step.y) - _home;
                offset.y = 0;
                // 먹이 탐색 중에는 무리 중심으로 당기지 않아 짧은 보폭을 보존한다.
                return _home + Vector3.ClampMagnitude(offset, _profile.RoamRadius);
            }
            Vector2 random = Random.insideUnitCircle * _profile.RoamRadius;
            return BlendGroupDestination(_home + new Vector3(random.x, 0, random.y));
        }

        private bool UpdateThreat(Vector3 observer, bool hasObserver)
        {
            float distance = hasObserver ? HorizontalDistanceSquared(observer, transform.position) : float.MaxValue;
            bool isNear = distance < _profile.AlertDistance * _profile.AlertDistance;
            if (_profile.FreezesWhenApproached && isNear)
            {
                StopToWatch(observer);
                return true;
            }
            if (isNear && distance < _profile.FleeDistance * _profile.FleeDistance)
            {
                _activity = Activity.Fleeing;
                _isForaging = false;
                _calmTime = _profile.CalmSeconds;
                ChooseEscapeDestination(observer);
                return true;
            }
            if (IsFleeing) return false;
            if (_activity == Activity.Avoiding)
            {
                if (_calmTime <= 0)
                {
                    _activity = Activity.Returning;
                    TryReturnHome();
                }
                else if (HasArrived()) ChooseEscapeDestination(observer);
                return true;
            }
            if (isNear)
            {
                StopToWatch(observer);
                if (_watchTime >= _profile.ObserveSeconds.x && distance < _profile.AvoidDistance * _profile.AvoidDistance)
                {
                    _activity = Activity.Avoiding;
                    ChooseEscapeDestination(observer);
                }
                return true;
            }
            if ((_activity == Activity.Roaming || _activity == Activity.Watching)
                && TryGetAlarmedNeighbour(out Vector3 alarmPosition))
            {
                StopToWatch(alarmPosition);
                _calmTime = _profile.CalmSeconds;
                return true;
            }
            if (_activity != Activity.Watching) return false;
            if (_calmTime > 0) return true;
            _activity = Activity.Roaming;
            _isResting = false;
            return false;
        }

        private void StopToWatch(Vector3 position)
        {
            if (_activity != Activity.Watching) _watchTime = 0;
            _watchPosition = position;
            _isForaging = false;
            _forageStepsRemaining = 0;
            _activity = Activity.Watching;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
            _target = transform.position;
        }

        private bool TryGetAlarmedNeighbour(out Vector3 position)
        {
            position = default;
            if (_neighbours == null || _profile.SocialAlertDistance <= 0) return false;
            float nearest = _profile.SocialAlertDistance * _profile.SocialAlertDistance;
            bool hasAlarm = false;
            for (int i = 0; i < _neighbours.Length; i++)
            {
                var other = _neighbours[i];
                // 경계만 하는 개체는 전파하지 않아 서로를 보고 영원히 경계하는 순환을 막는다.
                if (other == null || other == this || !other.isActiveAndEnabled || other._isDead
                    || other.Profile != _profile || !other.IsFleeing) continue;
                float distance = HorizontalDistanceSquared(transform.position, other.transform.position);
                if (distance >= nearest) continue;
                nearest = distance;
                position = other.transform.position;
                hasAlarm = true;
            }
            return hasAlarm;
        }

        private Vector3 BlendGroupDestination(Vector3 destination)
        {
            if (_neighbours == null || _profile.NeighbourRadius <= 0) return destination;
            Vector3 center = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _neighbours.Length; i++)
            {
                var other = _neighbours[i];
                if (other == null || other == this || !other.isActiveAndEnabled || other.Profile != _profile) continue;
                if (HorizontalDistanceSquared(other._home, _home) > _profile.NeighbourRadius * _profile.NeighbourRadius) continue;
                center += other.transform.position;
                count++;
            }
            if (count == 0) return destination;
            Vector3 offset = Vector3.Lerp(destination, center / count, _profile.Cohesion) - _home;
            offset.y = 0;
            return _home + Vector3.ClampMagnitude(offset, _profile.RoamRadius);
        }

        private bool HasPersonalSpace(Vector3 destination)
        {
            if (_neighbours == null) return true;
            for (int i = 0; i < _neighbours.Length; i++)
            {
                var other = _neighbours[i];
                if (other == null || other == this || !other.isActiveAndEnabled || other.Profile == null) continue;
                if (other.Profile.IsFlying != _profile.IsFlying) continue;
                float spacing = Mathf.Max(_profile.PersonalSpace, other.Profile.PersonalSpace);
                if (HorizontalDistanceSquared(destination, other.transform.position) < spacing * spacing) return false;
            }
            return true;
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

        private bool TryReturnHome()
        {
            if (TrySetDestination(_home)) return true;
            if (_profile.IsFlying) return false;
            // 동료가 최초 위치를 차지하면 귀환 상태에 영구 정지하지 않고 같은 생활권의 빈 곳을 쓴다.
            float radius = Mathf.Min(_profile.RoamRadius, _profile.PersonalSpace * 2);
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = Quaternion.Euler(0, i * 45, 0) * Vector3.forward * radius;
                if (TrySetDestination(_home + offset)) return true;
            }
            return false;
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
                || !HasPersonalSpace(hit.position)
                || !HasSafeGround(hit.position)
                || !_agent.CalculatePath(hit.position, _path) || _path.status != NavMeshPathStatus.PathComplete)
                return false;
            if (!HasSafePath(radius)) return false;
            _agent.speed = IsFleeing ? _profile.FleeSpeed : _profile.MoveSpeed;
            if (!_agent.SetPath(_path)) return false;
            _target = hit.position;
            return true;
        }

        private bool HasSafeGround(Vector3 position)
        {
            if (_habitat != null && !_habitat.CanOccupy(position)) return false;
            if (!_profile.ProjectGroundMovement) return true;
            float distance = _profile.LandingProbeDistance;
            return Physics.Raycast(position + Vector3.up * (distance * 0.5f), Vector3.down,
                out var ground, distance, _profile.LandingMask, QueryTriggerInteraction.Ignore)
                && ground.rigidbody == null
                && Mathf.Abs(ground.point.y - position.y) <= _profile.GroundHeightTolerance
                && Vector3.Angle(ground.normal, Vector3.up) <= _profile.MaxLandingSlope;
        }

        private bool HasSafePath(float radius)
        {
            int count = _path.GetCornersNonAlloc(_pathCorners);
            if (count >= _pathCorners.Length) return false;
            for (int i = 1; i < count; i++)
            {
                Vector3 start = _pathCorners[i - 1];
                Vector3 end = _pathCorners[i];
                int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / Mathf.Max(0.1f, _profile.PathProbeSpacing));
                for (int step = 1; step <= steps; step++)
                {
                    Vector3 point = Vector3.Lerp(start, end, (float)step / steps);
                    if (HorizontalDistanceSquared(point, _home) > radius * radius || !HasSafeGround(point)) return false;
                }
            }
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

        private void MoveInAirCore(float deltaTime)
        {
            if (_activity == Activity.Landed) return;
            Vector3 direction = _target - transform.position;
            if (direction.sqrMagnitude <= 0.0025f) { _airSpeed = 0; return; }
            float speed = IsFleeing ? _profile.FleeSpeed : _profile.MoveSpeed;
            if (_activity == Activity.Landing) speed = _profile.LandingSpeed;
            float acceleration = Mathf.Max(0.01f, _profile.Acceleration);
            speed = Mathf.Min(speed, Mathf.Sqrt(2 * acceleration * direction.magnitude));
            _airSpeed = Mathf.MoveTowards(_airSpeed, speed, acceleration * deltaTime);
            float step = Mathf.Min(_airSpeed * deltaTime, direction.magnitude);
            // 발 원점에 구의 바닥을 맞춰 낮은 착지 여유에서도 지면을 이동 장애물로 오인하지 않는다.
            Vector3 probeOrigin = transform.position + Vector3.up * AirObstacleRadius;
            if (Physics.SphereCast(probeOrigin, AirObstacleRadius, direction.normalized, out _, step,
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
            ApplyFlapRhythm(deltaTime);
        }

        private void UpdateGroundSteering(float deltaTime)
        {
            if (!_profile.UseStagedResponse || _agent == null || !_agent.enabled || !_agent.isOnNavMesh || !_agent.hasPath) return;
            Vector3 direction = _agent.steeringTarget - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude <= 0.001f) return;
            direction.Normalize();
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), _profile.TurnSpeed * deltaTime);
            // 큰 방향 전환에서는 먼저 몸을 돌려 발이 옆으로 밀리는 이동을 줄인다.
            float alignment = Mathf.Clamp01(Vector3.Dot(transform.forward, direction));
            _agent.speed = (IsFleeing ? _profile.FleeSpeed : _profile.MoveSpeed) * alignment;
        }
        private void UpdateGroundPosition()
        {
            if (!_profile.ProjectGroundMovement || _agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
            transform.position = ProjectGroundPosition(_agent.nextPosition);
        }

        private Vector3 ProjectGroundPosition(Vector3 position)
        {
            float probeDistance = _profile.LandingProbeDistance;
            if (Physics.Raycast(position + Vector3.up * (probeDistance * 0.5f), Vector3.down,
                    out var ground, probeDistance, _profile.LandingMask, QueryTriggerInteraction.Ignore))
                position.y = ground.point.y + _profile.GroundClearance;
            return position;
        }

        private void UpdateGroundAnimation()
        {
            bool isMoving = _agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.velocity.sqrMagnitude > 0.000001f;
            if (isMoving)
            {
                Play(IsFleeing ? _profile.Flee : _profile.Move);
                float cycleSpeed = IsFleeing ? _profile.RunCycleSpeed : _profile.WalkCycleSpeed;
                var state = _animation.States.Current;
                if (state != null && _profile.UseStagedResponse) state.Speed = Mathf.Clamp(_agent.velocity.magnitude / Mathf.Max(0.001f, cycleSpeed),
                    _profile.PlaybackSpeedRange.x, _profile.PlaybackSpeedRange.y);
                return;
            }
            AnimationClip clip = _profile.Idle;
            if (_activity == Activity.Roaming && _restActivity == RestActivity.Rest && _profile.Rest != null) clip = _profile.Rest;
            if (_activity == Activity.Watching || _restActivity == RestActivity.Observe) clip = _profile.Observe != null ? _profile.Observe : _profile.Idle;
            else if (_activity == Activity.Roaming && _restActivity == RestActivity.Feed && _profile.Feed != null) clip = _profile.Feed;
            Play(clip);
            if (_animation.States.Current != null) _animation.States.Current.Speed = 1;
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
