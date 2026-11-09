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
        // 걷는 접근(약 1.5m/s 미만)은 느긋한 접근으로 보고 도주 가속 판정에서 뺀다.
        private const float MinimumClosingSpeed = 2f;
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
        private float _feedBurstTime, _headUpTime;
        private float _previousObserverDistance = -1;
        // 개체마다 고정된 성향. 같은 프로필이라도 대담한 개체와 겁 많은 개체, 도주 방향이 다른 개체가 섞인다.
        private float _boldness = 1, _scatterBias;
        private float _zigZagSign = 1;
        // 동료 도주를 본 뒤 따라 도주하기까지 남은 시간(<0은 대기 없음)과 그 동료가 피한 위협 위치.
        private float _alarmDelay = -1, _socialFleeSeconds;
        private Vector3 _alarmThreat, _lastThreatPosition;
        private Vector3 _watchPosition;
        private int _forageStepsRemaining;
        private bool _isForaging;
        private float _airSpeed;
        private Vector3 _previousGroundPosition;
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
            _boldness = 1 + Random.Range(-_profile.BoldnessVariance, _profile.BoldnessVariance);
            _scatterBias = Random.Range(-1f, 1f);
            _zigZagSign = Random.value < 0.5f ? -1 : 1;
            if (_agent != null && _profile.UseStagedResponse)
            {
                _agent.acceleration = _profile.Acceleration;
                _agent.angularSpeed = _profile.TurnSpeed;
            }
            if (_agent != null && !_profile.IsFlying) _agent.updateRotation = false;
            if (!_profile.IsFlying && _profile.ProjectGroundMovement && _agent != null)
            {
                _agent.updatePosition = false;
                UpdateGroundPosition();
            }
            _home = _target = transform.position;
            _previousGroundPosition = transform.position;
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
            UpdateSocialFlee(deltaTime);
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
                // 경계 모션에는 발을 옮기는 턴이 없으므로 멈춘 몸을 위협 쪽으로 돌리지 않는다.
                UpdateGroundPosition();
                float distanceMoved = Mathf.Sqrt(HorizontalDistanceSquared(transform.position, _previousGroundPosition));
                _previousGroundPosition = transform.position;
                float speed = distanceMoved / deltaTime;
                UpdateGroundSteering(deltaTime, speed);
                UpdateGroundAnimation(speed);
            }
        }

        private void Decide(Vector3 observer, bool hasObserver)
        {
            // 새가 고도를 올린 것만으로 위협을 잊고 플레이어 머리 위에 내려앉지 않게 수평 거리를 쓴다.
            float alertDistance = _profile.AlertDistance * _boldness;
            bool isAlert = hasObserver && HorizontalDistanceSquared(observer, transform.position)
                < alertDistance * alertDistance;
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
                else ChooseEscapeDestination(ResolveThreat(observer, hasObserver));
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
            UpdateFeedVigilance(deltaTime);
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
            _headUpTime = 0;
            _feedBurstTime = Random.Range(_profile.FeedBurstSeconds.x, _profile.FeedBurstSeconds.y);
        }

        /// <summary>섭식 중 일정 간격으로 머리를 들어 주변을 살핀다. 먹이 먹는 동물은 먹는 내내 고개를 숙이지 않는다.</summary>
        private void UpdateFeedVigilance(float deltaTime)
        {
            if (_profile.FeedBurstSeconds.y <= 0 || _restActivity != RestActivity.Feed) return;
            if (_headUpTime > 0)
            {
                _headUpTime -= deltaTime;
                if (_headUpTime <= 0) _feedBurstTime = Random.Range(_profile.FeedBurstSeconds.x, _profile.FeedBurstSeconds.y);
                return;
            }
            _feedBurstTime -= deltaTime;
            if (_feedBurstTime <= 0) _headUpTime = Random.Range(_profile.VigilanceSeconds.x, _profile.VigilanceSeconds.y);
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
            float alertDistance = _profile.AlertDistance * _boldness;
            float avoidDistance = _profile.AvoidDistance * _boldness;
            float fleeDistance = _profile.FleeDistance * _boldness;
            bool isNear = distance < alertDistance * alertDistance;
            // 접근 속도는 매 판단마다 갱신해야 오래된 거리와 비교하지 않는다.
            bool isApproachingFast = IsApproachingFast(distance) && isNear;
            if (_profile.FreezesWhenApproached && isNear)
            {
                StopToWatch(observer);
                return true;
            }
            if (isNear && (distance < fleeDistance * fleeDistance || isApproachingFast))
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
            if (!isNear) QueueSocialFlee();
            if (isNear)
            {
                StopToWatch(observer);
                if (_watchTime >= _profile.ObserveSeconds.x && distance < avoidDistance * avoidDistance)
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

        /// <summary>
        /// 판단 주기 사이 거리 변화로 접근 속도를 구해 충돌 예상 시간이 짧으면 true.
        /// 거리 임계값만 쓰면 빨리 달려오는 위협 앞에서 반응할 시간이 없어 굼떠 보인다.
        /// </summary>
        private bool IsApproachingFast(float squaredDistance)
        {
            if (squaredDistance >= float.MaxValue)
            {
                _previousObserverDistance = -1;
                return false;
            }
            float distance = Mathf.Sqrt(squaredDistance);
            float previous = _previousObserverDistance;
            _previousObserverDistance = distance;
            if (_profile.FleeTimeToContact <= 0 || previous < 0) return false;
            float closingSpeed = (previous - distance) / _profile.DecisionInterval;
            return closingSpeed > MinimumClosingSpeed && distance / closingSpeed < _profile.FleeTimeToContact;
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

        /// <summary>동료를 따라 도주 중이고 플레이어가 경계 밖이면 동료가 피한 위협을, 아니면 플레이어를 위협으로 본다.</summary>
        private Vector3 ResolveThreat(Vector3 observer, bool hasObserver)
        {
            if (_socialFleeSeconds <= 0) return observer;
            float alertDistance = _profile.AlertDistance * _boldness;
            bool isObserverNear = hasObserver && HorizontalDistanceSquared(observer, transform.position) < alertDistance * alertDistance;
            return isObserverNear ? observer : _alarmThreat;
        }

        /// <summary>도주하는 동료가 가까이 있으면 반응 지연을 걸어 둔다.</summary>
        private void QueueSocialFlee()
        {
            if (_alarmDelay >= 0 || _profile.SocialFleeDistance <= 0 || _neighbours == null) return;
            if (_activity != Activity.Roaming && _activity != Activity.Watching) return;
            float nearest = _profile.SocialFleeDistance * _profile.SocialFleeDistance;
            bool hasAlarm = false;
            for (int i = 0; i < _neighbours.Length; i++)
            {
                var other = _neighbours[i];
                if (other == null || other == this || !other.isActiveAndEnabled || other._isDead
                    || other.Profile != _profile || !other.IsFleeing) continue;
                float distance = HorizontalDistanceSquared(transform.position, other.transform.position);
                if (distance >= nearest) continue;
                nearest = distance;
                _alarmThreat = other._lastThreatPosition;
                hasAlarm = true;
            }
            if (hasAlarm) _alarmDelay = Mathf.Max(0.001f, Random.Range(_profile.AlarmDelaySeconds.x, _profile.AlarmDelaySeconds.y));
        }

        private void UpdateSocialFlee(float deltaTime)
        {
            if (_socialFleeSeconds > 0) _socialFleeSeconds -= deltaTime;
            if (_alarmDelay < 0) return;
            if (IsFleeing) { _alarmDelay = -1; return; }
            _alarmDelay -= deltaTime;
            if (_alarmDelay > 0) return;
            _alarmDelay = -1;
            if (_activity != Activity.Roaming && _activity != Activity.Watching) return;
            _activity = Activity.Fleeing;
            _isForaging = false;
            _forageStepsRemaining = 0;
            _calmTime = _profile.CalmSeconds;
            _socialFleeSeconds = _profile.CalmSeconds;
            ChooseEscapeDestination(_alarmThreat);
        }

        private void ChooseEscapeDestination(Vector3 observer)
        {
            _lastThreatPosition = observer;
            Vector3 away = transform.position - observer;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            away.Normalize();
            // 지그재그는 판단마다 좌우를 바꾸고, 흩어짐은 개체마다 고정된 편차라 무리가 사방으로 퍼진다.
            _zigZagSign = -_zigZagSign;
            away = Quaternion.Euler(0, _profile.ZigZagAngle * _zigZagSign + _profile.ScatterAngle * _scatterBias, 0) * away;
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
            _agent.speed = GetGroundMoveSpeed();
            if (_profile.UseStagedResponse)
                _agent.acceleration = IsFleeing && _profile.FleeAcceleration > 0 ? _profile.FleeAcceleration : _profile.Acceleration;
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

        private void UpdateGroundSteering(float deltaTime, float actualSpeed)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || !_agent.hasPath) return;
            Vector3 direction = _agent.steeringTarget - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude <= 0.001f) return;
            direction.Normalize();
            float moveSpeed = GetGroundMoveSpeed();
            RotateGroundBody(direction, deltaTime, actualSpeed, moveSpeed);
            float alignment = Mathf.Clamp01(Vector3.Dot(transform.forward, direction));
            float minimumRatio = IsFleeing ? _profile.FleeTurnMoveSpeedRatio : _profile.TurnMoveSpeedRatio;
            _agent.speed = moveSpeed * Mathf.Max(minimumRatio, alignment);
        }

        /// <summary>현재 행동에 맞는 지상 목표 속도. 도주 > 회피 > 평상시 순으로 빠르다.</summary>
        private float GetGroundMoveSpeed()
        {
            if (IsFleeing) return _profile.FleeSpeed;
            if (_activity == Activity.Avoiding && _profile.AvoidSpeed > 0) return _profile.AvoidSpeed;
            return _profile.MoveSpeed;
        }

        private void RotateGroundBody(Vector3 direction, float deltaTime, float actualSpeed, float moveSpeed)
        {
            // 회전량과 보행 재생이 같은 실제 이동량을 쓰므로 발이 멈추면 몸도 회전하지 않는다.
            if (actualSpeed > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), _profile.TurnSpeed * deltaTime
                    * Mathf.Clamp01(actualSpeed / moveSpeed));
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

        private void UpdateGroundAnimation(float actualSpeed)
        {
            bool isMoving = actualSpeed > 0.001f;
            if (isMoving)
            {
                Play(IsFleeing ? _profile.Flee : _profile.Move);
                float cycleSpeed = IsFleeing ? _profile.RunCycleSpeed : _profile.WalkCycleSpeed;
                var state = _animation.States.Current;
                if (state != null && _profile.UseStagedResponse) state.Speed = Mathf.Clamp(actualSpeed / Mathf.Max(0.001f, cycleSpeed),
                    _profile.PlaybackSpeedRange.x, _profile.PlaybackSpeedRange.y);
                return;
            }
            AnimationClip clip = _profile.Idle;
            if (_activity == Activity.Roaming && _restActivity == RestActivity.Rest && _profile.Rest != null) clip = _profile.Rest;
            if (_activity == Activity.Watching || _restActivity == RestActivity.Observe) clip = _profile.Observe != null ? _profile.Observe : _profile.Idle;
            else if (_activity == Activity.Roaming && _restActivity == RestActivity.Feed && _profile.Feed != null)
                clip = _headUpTime > 0 ? (_profile.Observe != null ? _profile.Observe : _profile.Idle) : _profile.Feed;
            if (IsThreatening()) clip = _profile.Threaten;
            Play(clip);
            if (_animation.States.Current != null) _animation.States.Current.Speed = 1;
        }

        /// <summary>도망치지 않고 정면으로 대치할 거리 안이면 위협 모션을 쓴다.</summary>
        private bool IsThreatening()
        {
            if (_profile.Threaten == null || _profile.ThreatenDistance <= 0 || _activity != Activity.Watching) return false;
            float distance = _profile.ThreatenDistance * _boldness;
            return HorizontalDistanceSquared(_watchPosition, transform.position) < distance * distance;
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
            _previousGroundPosition = transform.position;
            _animation.Animator.enabled = isActive;
            _animation.enabled = isActive;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = !isActive;
        }

        private void OnDisable() => SetSimulation(false);
    }
}
