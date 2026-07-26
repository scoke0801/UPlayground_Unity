using UnityEngine;
using System.Collections.Generic;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Manager;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Projectile;

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

        /// <summary>
        /// 기존 프리팹의 직렬화 값을 새 ProjectileRuntime이 소비할 임시 Definition으로 변환한다.
        /// 프리팹의 MonoScript GUID를 보존하기 위한 호환 저작 셸이며 실제 런타임 Update는 실행하지 않는다.
        /// </summary>
        public virtual ProjectileDefinitionSO CreateCompatibilityDefinition()
        {
            ProjectileDefinitionSO definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.name = $"{name}_Compatibility";
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.visualPrefab = gameObject;
            definition.hitEffectKey = string.Empty;
            definition.detachTrailOnReturn = trailEffect != null;
            definition.motion = new StationaryProjectileMotion();
            definition.lifetime = Mathf.Max(0.01f, lifeTime);
            definition.collisionRadius = 0.25f;
            definition.destroyOnHit = destroyOnHit;
            definition.inheritOwnerTimeScale = true;
            definition.prewarmCount = 4;
            definition.maxPoolSize = 64;
            return definition;
        }
        protected float DeltaTime
        {
            get
            {
                if (owner == null)
                    return Time.deltaTime;
                if (owner is IDamageable damageableOwner && !damageableOwner.IsAlive())
                    return Time.deltaTime;
                return owner.DeltaTime;
            }
        }
        
        public virtual void Initialize(Vector3 startPos, Vector3 dir, float dmg, float speed,
            GameActor ownerObject, float duration, LayerMask layer, string hitParticleName,
            AttackData attackTemplate = null)
        {
            transform.position = startPos;
            direction = dir.normalized;
            owner = ownerObject;
            
            // 신규 저작은 Ability 히트 페이즈에서 만든 스냅샷을 사용한다.
            // 레거시 이벤트(hitPhaseIndex < 0)는 기존 damage/Hit 기본값을 유지한다.
            attackData = attackTemplate != null
                ? PlayerAttackController.Copy(attackTemplate)
                : new AttackData
                {
                    damage = dmg,
                    reactionType = AttackReactionType.Hit,
                };
            attackData.attackDirection = direction;
            attackData.isProjectile = true;

            if (!string.IsNullOrWhiteSpace(hitParticleName))
                attackData.hitParticleName = hitParticleName;

            hitLayers = layer;
            currentLifeTime = 0f;
            lifeTime = duration;
            isActive = true;
            _hitTargets.Clear();
            hitEffectKey = attackData.hitParticleName;
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

            currentLifeTime += DeltaTime;
            UpdateMovement();

            // 마지막 이동 프레임의 스윕/착탄 판정을 수행한 뒤 만료한다.
            if (isActive && currentLifeTime >= lifeTime)
                OnExpire();
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
            
            bool deliverable = damageable != null
                               && (owner.HasActorType(ActorType.Monster)
                                   ? damageable.IsAlive()
                                   : damageable.CanTakeDamage());
            if (deliverable)
            {
                _hitTargets.Add(damageable);
                
                // AttackData 업데이트
                attackData.hitTarget = hitObject;
                attackData.hitPoint = hitCollider.ClosestPoint(transform.position);
                attackData.attacker = owner;
                
                // 데미지 적용
                UPlayGround.Combat.CombatResult result =
                    damageable.ReceiveHit(UPlayGround.Combat.HitRequest.FromAttackData(attackData));

                if (owner.HasActorType(ActorType.Player) && _ownerPlayerCombat != null)
                {
                    // P4: 근접과 동일한 attacker-side 피드백으로 통일(데미지 숫자/히트 VFX/히트스톱/카메라/바이탈오브/킬캠).
                    _ownerPlayerCombat.ShowExternalHitFeedback(result);
                    _ownerPlayerCombat.ApplyExternalAttackImpact(attackData);
                    _ownerPlayerCombat.NotifyAttackHit(attackData);
                }
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

            Destroy(gameObject);
        }
    }
}
