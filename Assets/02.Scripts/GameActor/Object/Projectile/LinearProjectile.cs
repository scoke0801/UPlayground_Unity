using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 직선 이동 발사체 (검기, 화살, 에너지탄 등)
    /// </summary>
    public class LinearProjectile : BaseProjectile
    {
        [Header("Linear Movement")]
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _acceleration = 5f; // 초당 추가될 속도
        [SerializeField] private float _maxSpeed = 50f;
        [SerializeField] private float collisionRadius = 0.5f;
        
        [Header("Juice Effects")]
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1); // 속도 배율
        [SerializeField] private bool useSizeCurve = true;
        [SerializeField] private Vector3 _rotationSpeed = new Vector3(0, 0, 720f);
        
        private float _currentSpeed;
        private float _elapsedTime;
        private Vector3 _initialScale;
        private Vector3 _previousPosition;
        
        private void Awake()
        {
            _projectileType = ProjectileType.AOEProjectile;
        }
        
        public override void Initialize(Vector3 startPos, Vector3 dir, float dmg, float speed, GameActor ownerObject, float duration, LayerMask layer, string hitParticleName)
        {
            base.Initialize(startPos, dir, dmg, speed, ownerObject, duration, layer, hitParticleName);
            _previousPosition = startPos;
            _currentSpeed = speed > 0f ? speed : _speed;
            _elapsedTime = 0f;
            _initialScale = transform.localScale;
        }

        public void InitLinearProjectile()
        {
            
        }
        
        protected override void UpdateMovement()
        {
            _elapsedTime += Time.deltaTime;
            _previousPosition = transform.position;

            // 가속도 적용 (선형 가속 + 커브 배율)
            _currentSpeed += _acceleration * Time.deltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, _maxSpeed);
            float finalSpeed = _currentSpeed * speedCurve.Evaluate(_elapsedTime / lifeTime);

            // 위치 업데이트
            transform.position += finalSpeed * Time.deltaTime * direction;

            if (useSizeCurve)
            {
                float scaleRatio = speedCurve.Evaluate(_elapsedTime / lifeTime);
                transform.localScale = _initialScale * Mathf.Max(0.01f, scaleRatio);
            }
            
            // 회전 효과
            transform.Rotate(_rotationSpeed * Time.deltaTime, Space.Self);
            
            CheckCollision();
        }

        private void CheckCollision()
        {
            Vector3 moveDirection = transform.position - _previousPosition;
            float moveDistance = moveDirection.magnitude;
            
            if (moveDistance <= 0) return;

            if (Physics.SphereCast(_previousPosition, collisionRadius, moveDirection.normalized,
                    out RaycastHit hit, moveDistance, hitLayers))
            {   
                OnHit(hit.collider);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;
                
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_previousPosition, transform.position);
        }
    }
}
