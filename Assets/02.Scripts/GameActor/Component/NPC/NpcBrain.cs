using UnityEngine;
using UPlayGround.Manager;
using Random = UnityEngine.Random;

namespace UPlayGround.Components
{
    /// <summary>
    /// NPC 행동 설정.
    /// EnemyAIController과 달리 전투 로직 없이 배회·대기 데이터만 담습니다.
    /// </summary>
    public class NpcBrain : MonoBehaviour
    {
        [Header("배회 설정")]
        [Tooltip("스폰 위치 기준 배회 반경")]
        [SerializeField] private float _patrolRadius = 5f;
        [Tooltip("목적지 도착 후 대기 시간 (초)")]
        [SerializeField] private float _patrolWaitTime = 3f;
        [Tooltip("false면 Idle 상태만 유지하고 배회하지 않음")]
        [SerializeField] private bool _enableWander = true;
        [Tooltip("배회 보행 속도 배율. 최대 보행 속도에 곱한다. 낮을수록 느긋하게 걷는다.")]
        [Range(0.1f, 1f), SerializeField] private float _wanderSpeedMultiplier = 0.6f;
        [Tooltip("목적지에 이 거리 안으로 들어오면 도착으로 본다(수평 거리, m).")]
        [Min(0.1f), SerializeField] private float _wanderArrivalDistance = 0.6f;
        [Tooltip("대기 시간의 개체별 무작위 편차 비율")]
        [Range(0f, 1f), SerializeField] private float _patrolWaitVariation = 0.45f;

        [Header("대기 중 주변 살피기")]
        [SerializeField] private bool _enableIdleLook = true;
        [Tooltip("한 방향을 살핀 뒤 다음 방향을 고르기까지의 시간 범위")]
        [SerializeField] private Vector2 _idleLookInterval = new Vector2(3f, 7f);
        [Tooltip("배치 방향을 기준으로 좌우를 살피는 최대 각도")]
        [Range(0f, 90f), SerializeField] private float _idleLookAngle = 40f;
        [Min(0f), SerializeField] private float _idleTurnSpeed = 25f;

        [Header("플레이어 인식")]
        [Tooltip("대기 중 플레이어가 가까이 오면 몸을 돌려 바라본다. 작업 중인 NPC는 작업 방향을 유지한다.")]
        [SerializeField] private bool _enablePlayerAwareness = true;
        [Min(0f), SerializeField] private float _awarenessRadius = 6f;
        [Tooltip("인식을 풀 때 반경에 더하는 여유. 경계에서 시선이 깜빡이는 것을 막는다.")]
        [Min(0f), SerializeField] private float _awarenessReleaseMargin = 1.5f;
        [Tooltip("플레이어를 향해 돌아서는 속도(도/초). 배회·대기 시선보다 빠르게 반응한다.")]
        [Min(0f), SerializeField] private float _noticeTurnSpeed = 110f;
        [Tooltip("플레이어를 알아채기까지의 반응 지연 범위(초). 주민마다 다르게 반응하게 한다.")]
        [SerializeField] private Vector2 _noticeDelayRange = new Vector2(0.2f, 0.8f);

        [Header("생활 제스처")]
        [Tooltip("플레이어가 인사 반경에 들어와 알아챘을 때 한 번 재생한다. 비우면 인사하지 않는다.")]
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _greetingMotion;
        [Min(0f), SerializeField] private float _greetRadius = 4f;
        [Tooltip("같은 주민이 다시 인사하기까지의 최소 시간(초). 지나칠 때마다 손을 흔드는 어색함을 막는다.")]
        [Min(0f), SerializeField] private float _greetCooldownSeconds = 45f;
        [Tooltip("대기 중 간헐적으로 재생하는 짧은 생활 동작. 플레이어를 바라보는 동안에는 재생하지 않는다.")]
        [SerializeField] private UPlayGround.Animation.MotionSetAsset[] _idleGestures = System.Array.Empty<UPlayGround.Animation.MotionSetAsset>();
        [Tooltip("다음 생활 동작까지의 시간 범위(초). 주민마다 무작위로 어긋난다.")]
        [SerializeField] private Vector2 _idleGestureInterval = new Vector2(14f, 32f);

        [Header("고정 작업")]
        [Tooltip("배회하지 않을 때 생활 방향으로 돌아온 뒤 재생할 작업 모션")]
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _workMotion;
        [Min(0f), SerializeField] private float _workFacingTolerance = 3f;

        private Vector3 _spawnPosition;
        private Quaternion _homeRotation;
        private Quaternion _idleFacing;
        private Quaternion _idleLookRotation;
        private float _idleLookTimer;

        private const float AwarenessPollSeconds = 0.25f;
        private Transform _playerTransform;
        private float _awarenessTimer;
        private float _noticeDelayLeft = -1f;
        private bool _isNoticingPlayer;
        private bool _isGreetingPending;
        private bool _hasGreetedThisApproach;
        private float _nextGreetTime;
        private float _gestureTimer;

        /// <summary>플레이어를 알아채고 바라보는 중인지. 연출·반응 확장 지점이다.</summary>
        public bool IsNoticingPlayer => _isNoticingPlayer;

        public float PatrolRadius   => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public bool  EnableWander   => _enableWander;
        public float WanderSpeedMultiplier => _wanderSpeedMultiplier;
        public float WanderArrivalDistance => _wanderArrivalDistance;
        public UPlayGround.Animation.MotionSetAsset WorkMotion => _workMotion;
        public Quaternion HomeRotation => _homeRotation;
        public float WorkFacingTolerance => _workFacingTolerance;

        /// <summary>주민들이 같은 프레임에 출발하지 않도록 이번 대기 시간을 선택한다.</summary>
        public float SamplePatrolWaitTime()
        {
            float variation = Mathf.Clamp01(_patrolWaitVariation);
            return Mathf.Max(0f, _patrolWaitTime) * Random.Range(1f - variation, 1f + variation);
        }

        /// <summary>대기 장소의 방향을 기준으로 첫 시선과 행동 시점을 분산한다.</summary>
        public void BeginIdleLook(Quaternion facing)
        {
            _idleFacing = facing;
            ChooseIdleLook();
        }

        /// <summary>기존 NPC 상태 틱에서만 다음 시선을 고른다. 플레이어 인식도 이 틱에서 갱신한다.</summary>
        public void TickIdleLook(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _gestureTimer -= deltaTime;
            TickPlayerAwareness(deltaTime);
            if (_isNoticingPlayer || !_enableIdleLook) return;
            _idleLookTimer -= deltaTime;
            if (_idleLookTimer <= 0f) ChooseIdleLook();
        }

        /// <summary>발이 고정된 대기에서 급회전하지 않도록 회전 속도를 제한한다.</summary>
        public Quaternion RotateIdleLook(Quaternion rotation, float deltaTime)
        {
            if (_isNoticingPlayer && TryGetFlatDirectionToPlayer(out Vector3 toPlayer))
                return Quaternion.RotateTowards(rotation, Quaternion.LookRotation(toPlayer),
                    Mathf.Max(0f, _noticeTurnSpeed) * Mathf.Max(0f, deltaTime));
            if (!_enableIdleLook) return rotation;
            return Quaternion.RotateTowards(rotation, _idleLookRotation,
                Mathf.Max(0f, _idleTurnSpeed) * Mathf.Max(0f, deltaTime));
        }

        private void TickPlayerAwareness(float deltaTime)
        {
            if (!_enablePlayerAwareness) { _isNoticingPlayer = false; return; }

            _awarenessTimer -= deltaTime;
            if (_awarenessTimer > 0f) return;
            _awarenessTimer = AwarenessPollSeconds;

            bool hasPlayer = TryGetFlatDirectionToPlayer(out _, out float sqrDistance);
            bool isPlayerNear = hasPlayer && sqrDistance <= GetAwarenessSqrRadius(_isNoticingPlayer);
            if (!isPlayerNear)
            {
                if (_isNoticingPlayer) ChooseIdleLook();
                _isNoticingPlayer = false;
                _hasGreetedThisApproach = false;
                _noticeDelayLeft = -1f;
                return;
            }

            if (_isNoticingPlayer)
            {
                RequestGreetingIfDue(sqrDistance);
                return;
            }
            if (_noticeDelayLeft < 0f)
                _noticeDelayLeft = Random.Range(Mathf.Min(_noticeDelayRange.x, _noticeDelayRange.y),
                    Mathf.Max(_noticeDelayRange.x, _noticeDelayRange.y));
            _noticeDelayLeft -= AwarenessPollSeconds;
            _isNoticingPlayer = _noticeDelayLeft <= 0f;
        }

        private void RequestGreetingIfDue(float sqrDistance)
        {
            if (_greetingMotion == null || _hasGreetedThisApproach
                || sqrDistance > _greetRadius * _greetRadius || Time.time < _nextGreetTime)
                return;

            _hasGreetedThisApproach = true;
            _isGreetingPending = true;
            _nextGreetTime = Time.time + _greetCooldownSeconds;
        }

        /// <summary>
        /// 지금 재생할 생활 동작(인사 우선, 없으면 간헐적 대기 제스처)을 한 번 꺼낸다.
        /// 꺼내는 즉시 소비되므로 재생 여부는 호출한 상태가 책임진다.
        /// </summary>
        public bool TryTakeLifeGesture(out UPlayGround.Animation.MotionSetAsset motion)
        {
            motion = null;
            if (_isGreetingPending)
            {
                _isGreetingPending = false;
                motion = _greetingMotion;
                return motion != null;
            }

            if (_isNoticingPlayer || _gestureTimer > 0f || _idleGestures == null || _idleGestures.Length == 0)
                return false;

            _gestureTimer = Random.Range(Mathf.Min(_idleGestureInterval.x, _idleGestureInterval.y),
                Mathf.Max(_idleGestureInterval.x, _idleGestureInterval.y));
            motion = _idleGestures[Random.Range(0, _idleGestures.Length)];
            return motion != null;
        }

        private float GetAwarenessSqrRadius(bool isAlreadyNoticing)
        {
            float radius = _awarenessRadius + (isAlreadyNoticing ? _awarenessReleaseMargin : 0f);
            return radius * radius;
        }

        private bool TryGetFlatDirectionToPlayer(out Vector3 direction) => TryGetFlatDirectionToPlayer(out direction, out _);

        private bool TryGetFlatDirectionToPlayer(out Vector3 direction, out float sqrDistance)
        {
            direction = Vector3.forward;
            sqrDistance = 0f;
            if (_playerTransform == null
                && (!Services.TryGet<IActorQueryService>(out var actors) || (_playerTransform = actors.PlayerTransform) == null))
                return false;

            Vector3 delta = _playerTransform.position - transform.position;
            delta.y = 0f;
            sqrDistance = delta.sqrMagnitude;
            if (sqrDistance < 0.01f) return false;
            direction = delta / Mathf.Sqrt(sqrDistance);
            return true;
        }

        private void ChooseIdleLook()
        {
            float angle = Mathf.Clamp(_idleLookAngle, 0f, 90f);
            _idleLookRotation = _idleFacing * Quaternion.Euler(0f, Random.Range(-angle, angle), 0f);
            float minimum = Mathf.Max(0.1f, Mathf.Min(_idleLookInterval.x, _idleLookInterval.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(_idleLookInterval.x, _idleLookInterval.y));
            _idleLookTimer = Random.Range(minimum, maximum);
        }

        private void Awake()
        {
            _spawnPosition = transform.position;
            _homeRotation = transform.rotation;
            // 주민이 같은 순간에 같은 동작을 하지 않도록 첫 제스처 시점을 분산한다.
            _gestureTimer = Random.Range(0f, Mathf.Max(_idleGestureInterval.x, _idleGestureInterval.y));
        }

        /// <summary>저장된 생활 배치의 배회 중심과 작업 방향을 함께 복원한다.</summary>
        public void SetHomePose(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _homeRotation = rotation;
            BeginIdleLook(rotation);
        }

        /// <summary>생활 배치 복원 시 그 자리에서 수행할 작업을 바꾼다.</summary>
        public void SetWorkMotion(UPlayGround.Animation.MotionSetAsset motion) => _workMotion = motion;

        /// <summary>지역 스토리에서 기존 NPC를 고정 배치로 재사용할 때 호출한다.</summary>
        public void ConfigureStationary(Vector3 position)
        {
            _enableWander = false;
            _spawnPosition = position;
        }

        public Vector3 GetRandomWanderPoint()
        {
            Vector2 circle = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(circle.x, 0f, circle.y);
        }
    }
}
