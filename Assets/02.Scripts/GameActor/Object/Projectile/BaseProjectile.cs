using UnityEngine;
using System.Collections.Generic;
using UPlayGround.Data;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;

namespace UPlayGround
{
    /// <summary>
    /// 모든 발사체의 기본 클래스
    ///  AttackData 시스템 사용
    /// </summary>
    public abstract class BaseProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] protected float lifeTime = 5f;
        [SerializeField] protected LayerMask hitLayers;
        [SerializeField] protected bool destroyOnHit = true;
        
        [Header("VFX")]
        [SerializeField] protected ParticleSystem trailEffect;
        [SerializeField] protected GameObject projectileModel;

        protected ProjectileType _projectileType = ProjectileType.None;
        
        protected List<IDamageable> _hitTargets = new List<IDamageable>();

        protected bool isActive = true;
        protected float currentLifeTime;
        protected Vector3 direction;
        protected AttackData attackData;
        protected GameActor owner;
        protected string hitEffectKey;
        
        public bool IsActive => isActive;
        public ProjectileType ProjectileType => _projectileType;
        
        public virtual void Initialize(Vector3 startPos, Vector3 dir, float dmg, float speed,
            GameActor ownerObject, float duration, LayerMask layer, string hitParticleName)
        {
            transform.position = startPos;
            direction = dir.normalized;
            owner = ownerObject;
            
            // AttackData 구성
            attackData = new AttackData
            {
                damage = dmg,
                attackDirection = direction,
                reactionType = AttackReactionType.Hit
            };
            
            hitLayers = layer;
            currentLifeTime = 0f;
            lifeTime = duration;
            isActive = true;
            _hitTargets.Clear();
            hitEffectKey = hitParticleName;
            
            if (trailEffect != null)
                trailEffect.Play();
                
            if (projectileModel != null)
                projectileModel.SetActive(true);
            
            if (ownerObject.ActorType == ActorType.Monster)
            {
                InitFromMonsterActor(ownerObject as MonsterActor);
            }
            else if (ownerObject.ActorType == ActorType.Player)
            {
                InitFromPlayer(ownerObject as PlayerActor);
            }
        }

        protected virtual void InitFromPlayer(PlayerActor ownerObject)
        {
        }

        protected virtual void InitFromMonsterActor(MonsterActor ownerObject)
        {
        }

        protected virtual void Update()
        {
            if (!isActive)
                return;

            currentLifeTime += Time.deltaTime;
            
            if (currentLifeTime >= lifeTime)
            {
                OnExpire();
                return;
            }

            UpdateMovement();
        }

        /// <summary>
        /// 발사체별 이동 로직 (자식 클래스에서 구현)
        /// </summary>
        protected abstract void UpdateMovement();

        /// <summary>
        /// 히트 처리
        /// </summary>
        protected virtual void OnHit(Collider hitCollider)
        {
            if (!isActive || owner == null)
                return;

            GameObject hitObject = hitCollider.gameObject;
            
            // 자기 자신 제외
            if (hitObject == owner || hitObject.transform.IsChildOf(owner.transform))
                return;
            
            // IDamageable 찾기
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hitCollider.GetComponentInParent<IDamageable>();
            
            // 이미 맞춘 대상은 스킵
            if (damageable != null && _hitTargets.Contains(damageable))
                return;
            
            if (damageable != null && damageable.CanTakeDamage())
            {
                _hitTargets.Add(damageable);
                
                // AttackData 업데이트
                attackData.hitTarget = hitObject;
                attackData.hitPoint = hitCollider.ClosestPoint(transform.position);
                attackData.attacker = owner;
                
                // 데미지 적용
                damageable.TakeDamage(attackData);
                
                // 이펙트 표시
                GameObjectManager.Instance.ShowFX(hitEffectKey, attackData.hitPoint);
                //
                // // 카메라 쉐이크
                // CameraManager.Instance.StartShake("LiteHit");
                //
                // // 히트 스탑
                // GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Light);
                //
                Debug.Log($"[Projectile] 히트! Target: {hitObject.name}, Damage: {attackData.damage}");
            }

            if (destroyOnHit)
            {
                Deactivate();
            }
        }

        protected virtual void OnExpire()
        {
            Deactivate();
        }

        public virtual void Deactivate()
        {
            isActive = false;
            
            if (trailEffect != null)
                trailEffect.Stop();
            
            //if (projectileModel != null)
            //    projectileModel.SetActive(false);
                
            Destroy(gameObject);
        }
    }
}