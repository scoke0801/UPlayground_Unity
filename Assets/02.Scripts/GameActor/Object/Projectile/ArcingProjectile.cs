using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Projectile;

namespace UPlayGround
{ 
    /// <summary>
    /// 포물선 궤적 발사체 (활시위 → 공중 → 대상 낙하).
    /// 시작점과 타겟 위치 사이를 정점 높이만큼 솟은 곡선으로 비행한다.
    /// 타겟 위치는 SetTargetPosition으로 주입한다(MotionEvent_SpawnProjectile에서 설정).
    /// </summary>
    public class ArcingProjectile : BaseProjectile
    {
        [Header("Arc Motion")]
        [Tooltip("시작-착탄 중간 지점 위로 추가될 정점 높이")]
        [SerializeField] private float _arcHeight = 5f;
        [Tooltip("궤적 진행 곡선 (0→1). 시간 대비 수평 진행률.")]
        [SerializeField] private AnimationCurve _progressCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Tooltip("타겟 위치를 지정하지 못한 경우 direction 방향으로 사용할 폴백 사거리")]
        [SerializeField] private float _fallbackRange = 15f;

        [Header("Collision")]
        [SerializeField] private float _collisionRadius = 0.5f;

        [Header("Visual")]
        [Tooltip("진행 방향으로 회전. 활/투창처럼 끝이 비행 방향을 향함")]
        [SerializeField] private bool _alignToVelocity = true;

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private Vector3 _previousPosition;
        private float _elapsedTime;
        private float _travelSpeed;
        private float _flightDuration;
        private bool _hasExplicitTarget;

        private void Awake()
        {
            _projectileType = ProjectileType.ArcingProjectile;
        }

        public override void Initialize(Vector3 startPos, Vector3 dir, float dmg, float speed,
            GameActor ownerObject, float duration, LayerMask layer, string hitParticleName,
            UPlayGround.Data.AttackData attackTemplate = null)
        {
            base.Initialize(startPos, dir, dmg, speed, ownerObject, duration, layer, hitParticleName, attackTemplate);

            _startPos = startPos;
            _previousPosition = startPos;
            _elapsedTime = 0f;
            _travelSpeed = Mathf.Max(0f, speed);
            _hasExplicitTarget = false;

            // 폴백 타겟: direction 방향으로 _fallbackRange 만큼 떨어진 지점 (수평 기준)
            Vector3 horizontalDir = dir; horizontalDir.y = 0f;
            if (horizontalDir.sqrMagnitude < 0.0001f) horizontalDir = transform.forward;
            _targetPos = startPos + horizontalDir.normalized * _fallbackRange;
            RecalculateFlightDuration();
        }

        /// <summary>착탄 위치를 명시적으로 설정. SpawnProjectile 이벤트가 타겟 해석 후 호출.</summary>
        public void SetTargetPosition(Vector3 worldTarget)
        {
            _targetPos = worldTarget;
            _hasExplicitTarget = true;
            RecalculateFlightDuration();
        }

        public bool HasExplicitTarget => _hasExplicitTarget;

        public override ProjectileDefinitionSO CreateCompatibilityDefinition()
        {
            ProjectileDefinitionSO definition = base.CreateCompatibilityDefinition();
            definition.motion = new ArcProjectileMotion
            {
                speed = 15f,
                arcHeight = Mathf.Max(0f, _arcHeight),
                flightTimeMode = ProjectileArcFlightTimeMode.Speed,
                fixedFlightTime = Mathf.Max(0.01f, lifeTime),
                progressCurve = _progressCurve,
            };
            definition.collisionRadius = Mathf.Max(0.01f, _collisionRadius);
            return definition;
        }

        protected override void UpdateMovement()
        {
            _previousPosition = transform.position;
            _elapsedTime += DeltaTime;

            float t = Mathf.Clamp01(_elapsedTime / _flightDuration);
            float curved = _progressCurve.Evaluate(t);

            // 포물선: 수평 보간 + (4t(1-t)) 가중 정점 가산
            Vector3 basePos = Vector3.Lerp(_startPos, _targetPos, curved);
            float arc = 4f * curved * (1f - curved) * _arcHeight;
            Vector3 newPos = basePos + Vector3.up * arc;

            transform.position = newPos;

            // 속도 벡터 방향 정렬
            if (_alignToVelocity)
            {
                Vector3 velocity = newPos - _previousPosition;
                if (velocity.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(velocity.normalized);
            }

            CheckCollision();
        }

        private void RecalculateFlightDuration()
        {
            float distance = Vector3.Distance(_startPos, _targetPos);
            _flightDuration = _travelSpeed > 0.0001f
                ? Mathf.Max(0.01f, distance / _travelSpeed)
                : Mathf.Max(0.01f, lifeTime);

            // duration은 최소 수명으로 유지하되, speed로 계산한 착탄 전에 만료되지 않게 한다.
            lifeTime = Mathf.Max(lifeTime, _flightDuration);
        }

        private void CheckCollision()
        {
            Vector3 move = transform.position - _previousPosition;
            float distance = move.magnitude;
            if (distance <= 0f) return;

            if (Physics.SphereCast(_previousPosition, _collisionRadius, move.normalized,
                    out RaycastHit hit, distance, hitLayers))
            {
                OnHit(hit.collider);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _collisionRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_startPos, _targetPos);
            Gizmos.DrawWireSphere(_targetPos, 0.3f);
        }
    }
}
