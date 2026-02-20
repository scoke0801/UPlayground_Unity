using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Enum;

namespace UPlayGround
{
    /// <summary>
    /// 범위 공격 발사체 (폭발, 충격파, 장판 등)
    /// </summary>
    public class AOEProjectile : BaseProjectile
    {
        [Header("AOE Settings")]
        [SerializeField] private float aoeRadius = 5f;
        [SerializeField] private float expansionSpeed = 10f;
        [SerializeField] private bool expandOverTime = false;
        [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.Linear(0, 1, 1, 0.3f);
        [SerializeField] private LayerMask _groundLayerMask;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDelay = 0f;
        [SerializeField] private bool attachToGround = true;

        [Header("Tick Settings")]
        [SerializeField] private float damageCooldown = 1f; // 대상별 데미지 간격
      
        private readonly Dictionary<IDamageable, float> _damageCooldowns = new Dictionary<IDamageable, float>();

        private float currentRadius;
        private bool hasTriggered;
        private float spawnTimer;

        public override void Initialize(Vector3 startPos, Vector3 dir, float dmg, GameActor ownerObject, float duration, LayerMask layer, string hitParticleName)
        {
            base.Initialize(startPos, dir, dmg, ownerObject, duration, layer, hitParticleName);
            
            currentRadius = 0f;
            hasTriggered = false;
            spawnTimer = 0f;

            _damageCooldowns.Clear();
            
            if (attachToGround)
            {
                AttachToGround(transform.position);
            }
        }

        protected override void InitFromMonsterActor(MonsterActor ownerObject)
        {
            base.InitFromMonsterActor(ownerObject);

            if (ownerObject == null || ownerObject.Combat == null)
            {
                return;
            }

            if (ownerObject.Combat.SkillTargetList.Count == 0)
            {
                return;
            }

            transform.position = ownerObject.Combat.SkillTargetList[0].GetTransform().position;
        }

        private void AttachToGround(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, _groundLayerMask))
            {
                transform.position = hit.point;
                transform.up = Vector3.up;
            }
        }

        protected override void Update()
        {
            UpdateDamageCooldowns();
            base.Update();
        }
        
        private void UpdateDamageCooldowns()
        {
            var keys = new List<IDamageable>(_damageCooldowns.Keys);
            foreach (var key in keys)
            {
                _damageCooldowns[key] -= Time.deltaTime;
                if (_damageCooldowns[key] <= 0f)
                {
                    _damageCooldowns.Remove(key);
                }
            }
        }
        
        protected override void UpdateMovement()
        {
            // 생성 딜레이
            if (spawnTimer < spawnDelay)
            {
                spawnTimer += Time.deltaTime;
                return;
            }

            if (!hasTriggered)
            {
                TriggerAOE();
                hasTriggered = true;
            }

            if (expandOverTime && currentRadius < aoeRadius)
            {
                currentRadius += expansionSpeed * Time.deltaTime;
                currentRadius = Mathf.Min(currentRadius, aoeRadius);
                
                // 확장되는 AOE는 지속적으로 체크
                CheckAOEDamage();
            }
        }

        private void TriggerAOE()
        {
            if (expandOverTime)
            {
                currentRadius = 0f;
            }
            else
            {
                currentRadius = aoeRadius;
                CheckAOEDamage();
                
                // 즉시 폭발 타입은 바로 종료
                if (destroyOnHit)
                {
                    Invoke(nameof(OnExpire), 0.5f);
                }
            }

            // 폭발 이펙트
            if (!string.IsNullOrEmpty(hitEffectKey))
            {
                UPlayGround.Manager.GameObjectManager.Instance.ShowFX(hitEffectKey, transform.position);
            }
        }

        private void CheckAOEDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, hitLayers);

            foreach (Collider hit in hits)
            {
                GameObject target = hit.gameObject;

                if (target.transform == owner.transform || target.transform.IsChildOf(owner.transform))
                    continue;

                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.CanTakeDamage())
                    continue;

                // 쿨타임 중인 대상 스킵
                if (_damageCooldowns.ContainsKey(damageable))
                    continue;

                // 거리 기반 데미지 감쇠
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damageMultiplier = damageFalloff.Evaluate(distance / aoeRadius);

                // AttackData 업데이트
                attackData.damage = attackData.damage * damageMultiplier;
                attackData.hitTarget = target;
                attackData.hitPoint = hit.ClosestPoint(transform.position);
                attackData.attackDirection = (target.transform.position - transform.position).normalized;

                damageable.TakeDamage(attackData);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position, aoeRadius);
                return;
            }
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, currentRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentRadius);
        }
    }
}