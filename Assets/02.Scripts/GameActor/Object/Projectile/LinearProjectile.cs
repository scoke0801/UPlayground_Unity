using UnityEngine;

namespace UPlayGround
{
    /// <summary>
    /// 직선 이동 발사체 (검기, 화살, 에너지탄 등)
    /// </summary>
    public class LinearProjectile : BaseProjectile
    {
        [Header("Linear Movement")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private bool useContinuousCollision = true;
        [SerializeField] private float collisionRadius = 0.5f;

        private Vector3 previousPosition;

        public override void Initialize(Vector3 startPos, Vector3 dir, float dmg, GameObject ownerObject)
        {
            base.Initialize(startPos, dir, dmg, ownerObject);
            previousPosition = startPos;
        }

        protected override void UpdateMovement()
        {
            previousPosition = transform.position;
            Vector3 movement = direction * speed * Time.deltaTime;
            transform.position += movement;

            CheckCollision();
        }

        private void CheckCollision()
        {
            if (!useContinuousCollision)
            {
                // Sphere Overlap 체크
                Collider[] hits = Physics.OverlapSphere(transform.position, collisionRadius, hitLayers);
                if (hits.Length > 0)
                {
                    OnHit(hits[0]);
                }
            }
            else
            {
                // 연속 충돌 감지 (빠른 발사체용)
                Vector3 moveDirection = transform.position - previousPosition;
                float moveDistance = moveDirection.magnitude;

                if (Physics.SphereCast(previousPosition, collisionRadius, moveDirection.normalized, 
                    out RaycastHit hit, moveDistance, hitLayers))
                {
                    OnHit(hit.collider);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;
                
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(previousPosition, transform.position);
        }
    }
}