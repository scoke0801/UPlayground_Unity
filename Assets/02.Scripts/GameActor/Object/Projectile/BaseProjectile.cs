using UnityEngine;
using System.Collections.Generic;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

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
        protected PlayerCombat _ownerPlayerCombat;
        
        public bool IsActive => isActive;
        public ProjectileType ProjectileType => _projectileType;
        
        public virtual void Initialize(Vector3 startPos, Vector3 dir, float dmg, float speed,
            GameActor ownerObject, float duration, LayerMask layer, string hitParticleName)
        {
            transform.position = startPos;
            direction = dir.normalized;
            owner = ownerObject;
            
            // AttackData 구성
            // TODO(DangerRing): defenseType이 기본 Parryable로 들어간다. 원거리 Unblockable 공격을 만들려면
            //   Initialize에 defenseType을 인자로 받아 스킬(EnemyAttackInfo.defenseType)에서 전달할 것.
            //   (근접은 EnemyCombat.CheckMeleeAttackHit에서 이미 복사 중)
            attackData = new AttackData
            {
                damage = dmg,
                attackDirection = direction,
                reactionType = AttackReactionType.Hit
            };
            // P4: 설정된 히트 FX가 attacker-side 연출(ShowExternalHitFeedback)에 반영되도록 복사. 비우면 기본 FX 사용.
            if (!string.IsNullOrWhiteSpace(hitParticleName))
                attackData.hitParticleName = hitParticleName;

            hitLayers = layer;
            currentLifeTime = 0f;
            lifeTime = duration;
            isActive = true;
            _hitTargets.Clear();
            hitEffectKey = hitParticleName;
            _ownerPlayerCombat = null;
            
            if (trailEffect != null)
                trailEffect.Play();
                
            if (projectileModel != null)
                projectileModel.SetActive(true);
            
            if (ownerObject.HasActorType(ActorType.Monster))
            {
                InitFromMonsterActor(ownerObject as MonsterActor);
            }
            else if (ownerObject.HasActorType(ActorType.Player))
            {
                InitFromPlayer(ownerObject as PlayerActor);
            }
        }

        protected virtual void InitFromPlayer(PlayerActor ownerObject)
        {
            _ownerPlayerCombat = ownerObject != null ? ownerObject.GetCombat() : null;

            // 투사체 히트가 어떤 종류의 공격으로 집계될지 결정.
            // 스폰 시점의 PlayerCombat.CurrentAttackData.attackKind를 상속한다.
            // (스킬 모션에서 발사된 투사체는 SkillAttack → 게이지 충전 0과 일치)
            if (_ownerPlayerCombat != null && _ownerPlayerCombat.CurrentAttackData != null)
            {
                attackData.attackKind = _ownerPlayerCombat.CurrentAttackData.attackKind;
            }
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

                if (owner.HasActorType(ActorType.Player) && _ownerPlayerCombat != null)
                {
                    // P4: 근접과 동일한 attacker-side 피드백으로 통일(데미지 숫자/히트 VFX/히트스톱/카메라/바이탈오브/킬캠).
                    _ownerPlayerCombat.ShowExternalHitFeedback(attackData);
                    _ownerPlayerCombat.ApplyExternalAttackImpact(attackData);
                    _ownerPlayerCombat.NotifyAttackHit(attackData);
                }
                // 이펙트 표시
                // GameObjectManager.Instance.ShowFX(hitEffectKey, attackData.hitPoint);
                //
                // // 카메라 쉐이크
                // CameraManager.Instance.StartShake("LiteHit");
                //
                // // 히트 스탑
                // GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.Light);
                //
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