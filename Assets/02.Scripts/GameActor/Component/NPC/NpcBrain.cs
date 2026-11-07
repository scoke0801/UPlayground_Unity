using UnityEngine;
using UnityEngine.AI;
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
        [Tooltip("현재 위치에서 이 거리 이상 떨어진 목적지만 선택한다. 반경이 너무 작으면 대기한다.")]
        [Min(0.1f), SerializeField] private float _minimumWanderDistance = 1.2f;
        [Tooltip("배회 시작점과 목적지를 NavMesh에 맞출 때 허용할 거리")]
        [Min(0.1f), SerializeField] private float _wanderNavMeshSampleDistance = 1f;
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
        [Tooltip("대기 중 플레이어가 가까이 오면 머리만 돌려 바라본다. 작업 중인 NPC는 작업 방향을 유지한다.")]
        [SerializeField] private bool _enablePlayerAwareness = true;
        [Min(0f), SerializeField] private float _awarenessRadius = 6f;
        [Tooltip("인식을 풀 때 반경에 더하는 여유. 경계에서 시선이 깜빡이는 것을 막는다.")]
        [Min(0f), SerializeField] private float _awarenessReleaseMargin = 1.5f;
        [Tooltip("머리가 플레이어를 향하는 속도(도/초). 배회·대기 시선보다 빠르게 반응한다.")]
        [Min(0f), SerializeField] private float _noticeTurnSpeed = 110f;
        [Tooltip("플레이어를 알아채기까지의 반응 지연 범위(초). 주민마다 다르게 반응하게 한다.")]
        [SerializeField] private Vector2 _noticeDelayRange = new Vector2(0.2f, 0.8f);
        [Tooltip("플레이어를 잠깐 바라본 뒤 자기 행동으로 돌아가기까지의 시간")]
        [SerializeField] private Vector2 _attentionDuration = new Vector2(2f, 4f);
        [Tooltip("가까이 머무르는 플레이어에게 다시 시선을 주기까지의 시간")]
        [SerializeField] private Vector2 _attentionCooldown = new Vector2(10f, 18f);

        [Header("머리 시선")]
        [Tooltip("몸의 정면 기준 머리가 돌아갈 수 있는 최대 각도. 뒤쪽 플레이어는 추적하지 않는다.")]
        [Range(0f, 80f), SerializeField] private float _maxHeadLookAngle = 60f;
        [Min(0f), SerializeField] private float _headLookFadeSpeed = 4f;

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
        private UPlayGround.Animation.MotionSetAsset _lastIdleGesture;
        private bool _isLifeGesturePlaying;
        private float _attentionRemaining;
        private float _attentionCooldownLeft;
        private NpcActor _actor;
        private Animator _lookAnimator;
        private Transform _head;
        private bool _isIdleLooking;
        private Quaternion _headFacing;
        private float _headLookWeight;

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
            _headFacing = facing;
            _isIdleLooking = true;
            BindHeadLook();
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

        /// <summary>이동·대화·반응 상태가 대기 시선을 이어받지 않도록 해제한다.</summary>
        public void EndIdleLook()
        {
            _isIdleLooking = false;
            _isLifeGesturePlaying = false;
            _isNoticingPlayer = false;
            _isGreetingPending = false;
            _noticeDelayLeft = -1f;
        }

        /// <summary>몸 회전에 적용하지 않고 머리 IK가 사용할 시선 방향만 보간한다.</summary>
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

            _attentionCooldownLeft = Mathf.Max(0f, _attentionCooldownLeft - deltaTime);
            if (_isNoticingPlayer)
            {
                _attentionRemaining -= deltaTime;
                if (_attentionRemaining <= 0f)
                {
                    _isNoticingPlayer = false;
                    _isGreetingPending = false;
                    _noticeDelayLeft = -1f;
                    _attentionCooldownLeft = SamplePositiveRange(_attentionCooldown);
                    ChooseIdleLook();
                }
            }

            _awarenessTimer -= deltaTime;
            if (_awarenessTimer > 0f) return;
            _awarenessTimer = AwarenessPollSeconds;

            bool hasPlayer = TryGetFlatDirectionToPlayer(out Vector3 direction, out float sqrDistance)
                && Vector3.Angle(transform.forward, direction) <= _maxHeadLookAngle;
            bool isPlayerNear = hasPlayer && sqrDistance <= GetAwarenessSqrRadius(_isNoticingPlayer);
            if (!isPlayerNear)
            {
                if (_isNoticingPlayer) ChooseIdleLook();
                _isNoticingPlayer = false;
                _isGreetingPending = false;
                _hasGreetedThisApproach = false;
                _noticeDelayLeft = -1f;
                return;
            }

            if (_isNoticingPlayer)
            {
                RequestGreetingIfDue(sqrDistance);
                return;
            }
            if (_attentionCooldownLeft > 0f) return;
            if (_noticeDelayLeft < 0f)
                _noticeDelayLeft = Random.Range(Mathf.Min(_noticeDelayRange.x, _noticeDelayRange.y),
                    Mathf.Max(_noticeDelayRange.x, _noticeDelayRange.y));
            _noticeDelayLeft -= AwarenessPollSeconds;
            _isNoticingPlayer = _noticeDelayLeft <= 0f;
            if (_isNoticingPlayer)
            {
                _attentionRemaining = SamplePositiveRange(_attentionDuration);
                RequestGreetingIfDue(sqrDistance);
            }
        }

        private static float SamplePositiveRange(Vector2 range)
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(range.x, range.y));
            return Random.Range(minimum, Mathf.Max(minimum, Mathf.Max(range.x, range.y)));
        }

        /// <summary>생활 동작 자체의 고개 연출과 시선 IK가 충돌하지 않게 한다.</summary>
        public void SetLifeGesturePlaying(bool isPlaying) => _isLifeGesturePlaying = isPlaying;

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
            // 같은 동작의 중복 슬롯도 제외해 배열 가중치가 연속 반복으로 보이지 않게 한다.
            int candidates = 0;
            for (int i = 0; i < _idleGestures.Length; i++)
            {
                var candidate = _idleGestures[i];
                if (candidate == null || candidate == _lastIdleGesture) continue;
                if (Random.Range(0, ++candidates) == 0) motion = candidate;
            }
            // 한 종류뿐이면 재생 간격마다 같은 동작을 허용한다.
            if (motion == null && _lastIdleGesture != null
                && System.Array.IndexOf(_idleGestures, _lastIdleGesture) >= 0) motion = _lastIdleGesture;
            _lastIdleGesture = motion;
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
            _actor = GetComponent<NpcActor>();
            _spawnPosition = transform.position;
            _homeRotation = transform.rotation;
            // 주민이 같은 순간에 같은 동작을 하지 않도록 첫 제스처 시점을 분산한다.
            _gestureTimer = Random.Range(0f, Mathf.Max(_idleGestureInterval.x, _idleGestureInterval.y));
        }

        private void BindHeadLook()
        {
            Animator animator = _actor != null && _actor.Animator != null
                ? _actor.Animator.GetAnimator : GetComponentInChildren<Animator>();
            if (_lookAnimator == animator) return;
            if (_lookAnimator != null && _lookAnimator.gameObject != gameObject)
            {
                var oldRelay = _lookAnimator.GetComponent<NpcHeadLookRelay>();
                if (oldRelay != null) oldRelay.Owner = null;
            }
            _lookAnimator = animator;
            _head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            _headLookWeight = 0f;
            if (_head == null || animator.gameObject == gameObject) return;
            var relay = animator.GetComponent<NpcHeadLookRelay>();
            if (relay == null) relay = animator.gameObject.AddComponent<NpcHeadLookRelay>();
            relay.Owner = this;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_lookAnimator != null && _lookAnimator.gameObject == gameObject)
                ProcessHeadLook(layerIndex);
        }

        internal void ProcessHeadLook(int layerIndex)
        {
            if (layerIndex != 0 || _head == null || _lookAnimator == null) return;
            bool canLook = isActiveAndEnabled && _isIdleLooking && !_isLifeGesturePlaying
                && (_actor == null || (!_actor.IsRoutinePaused && !_actor.IsInteracting() && !_actor.IsDialogueStaged))
                && (_enableWander || _workMotion == null) && (_enableIdleLook || _isNoticingPlayer);
            float deltaTime = Time.deltaTime;
            _headLookWeight = Mathf.MoveTowards(_headLookWeight, canLook ? 1f : 0f,
                Mathf.Max(0f, _headLookFadeSpeed) * deltaTime);
            if (canLook) _headFacing = RotateIdleLook(_headFacing, deltaTime);
            Vector3 direction = Vector3.RotateTowards(transform.forward, _headFacing * Vector3.forward,
                Mathf.Clamp(_maxHeadLookAngle, 0f, 80f) * Mathf.Deg2Rad, 0f);
            // 몸통과 눈은 원래 애니메이션에 맡겨 작업 자세와 표정을 보존한다.
            _lookAnimator.SetLookAtWeight(_headLookWeight, 0f, 1f, 0f, 0f);
            _lookAnimator.SetLookAtPosition(_head.position + direction);
        }

        private void OnDisable()
        {
            EndIdleLook();
            _headLookWeight = 0f;
        }

        /// <summary>저장된 생활 배치의 배회 중심과 작업 방향을 함께 복원한다.</summary>
        public void SetHomePose(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _homeRotation = rotation;
            _idleFacing = rotation;
            _headFacing = rotation;
            ChooseIdleLook();
        }

        /// <summary>생활 배치 복원 시 그 자리에서 수행할 작업을 바꾼다.</summary>
        public void SetWorkMotion(UPlayGround.Animation.MotionSetAsset motion) => _workMotion = motion;

        /// <summary>지역 스토리에서 기존 NPC를 고정 배치로 재사용할 때 호출한다.</summary>
        public void ConfigureStationary(Vector3 position)
        {
            _enableWander = false;
            _spawnPosition = position;
        }

        /// <summary>배회 영역 안에서 충분히 떨어져 있고 직선 보행 가능한 다음 목적지를 선택한다.</summary>
        public bool TryGetWanderPoint(Vector3 currentPosition, out Vector3 destination)
        {
            destination = currentPosition;
            if (!_enableWander || _patrolRadius <= _wanderArrivalDistance) return false;
            float minimumDistance = Mathf.Max(_minimumWanderDistance, _wanderArrivalDistance);
            bool hasNavMesh = NavMesh.SamplePosition(currentPosition, out var start,
                _wanderNavMeshSampleDistance, NavMesh.AllAreas);
            // NavMesh가 없는 기존 씬은 종전처럼 KCC 충돌로 이동을 제한한다.
            const int PointAttempts = 12;
            for (int i = 0; i < PointAttempts; i++)
            {
                Vector2 circle = Random.insideUnitCircle * _patrolRadius;
                Vector3 candidate = _spawnPosition + new Vector3(circle.x, 0f, circle.y);
                if (hasNavMesh)
                {
                    if (!NavMesh.SamplePosition(candidate, out var hit, _wanderNavMeshSampleDistance, NavMesh.AllAreas)
                        || NavMesh.Raycast(start.position, hit.position, out _, NavMesh.AllAreas)) continue;
                    candidate = hit.position;
                }
                else candidate.y = currentPosition.y;
                Vector3 fromHome = Vector3.ProjectOnPlane(candidate - _spawnPosition, Vector3.up);
                Vector3 travel = Vector3.ProjectOnPlane(candidate - currentPosition, Vector3.up);
                if (fromHome.sqrMagnitude > _patrolRadius * _patrolRadius
                    || travel.sqrMagnitude <= minimumDistance * minimumDistance) continue;
                destination = candidate;
                return true;
            }
            return false;
        }
    }
    /// <summary>자식 모델의 IK 콜백을 NPC 행동 컴포넌트로 전달한다.</summary>
    internal sealed class NpcHeadLookRelay : MonoBehaviour
    {
        internal NpcBrain Owner;

        private void OnAnimatorIK(int layerIndex)
        {
            if (Owner != null) Owner.ProcessHeadLook(layerIndex);
        }
    }
}
