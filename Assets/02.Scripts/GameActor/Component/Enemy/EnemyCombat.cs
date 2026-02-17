using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enum;
using UPlayGround.Manager;

namespace UPlayGround.Component
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private EnemyAttackDataSO _attackData;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayer;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        
        private EnemyAttackInfo _currentSkill;
        private List<Collider> _hitTargets = new List<Collider>();
        private Dictionary<EnemyAttackInfo, float> _skillCooldowns = new Dictionary<EnemyAttackInfo, float>();
        
        private bool _isCollisionEnabled;

        public EnemyAttackDataSO AttackData => _attackData;
        public EnemyAttackInfo CurrentSkill => _currentSkill;
        public bool IsPossibleCollide => _isCollisionEnabled;
        
        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        private void UpdateCooldowns()
        {
            List<EnemyAttackInfo> keysToUpdate = new List<EnemyAttackInfo>(_skillCooldowns.Keys);
            
            foreach (var skill in keysToUpdate)
            {
                _skillCooldowns[skill] -= Time.deltaTime;
                if (_skillCooldowns[skill] <= 0)
                {
                    _skillCooldowns.Remove(skill);
                }
            }
        }

        /// <summary>
        /// 현재 거리에서 사용 가능한 스킬 선택 및 실행
        /// </summary>
        public EnemyAttackInfo SelectAndExecuteAttack(float distanceToTarget)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
            {
                Debug.LogWarning("[EnemyCombat] 공격 데이터가 없습니다!");
                return null;
            }
            
            // 거리 기반 사용 가능한 스킬 필터링
            List<EnemyAttackInfo> availableSkills = _attackData.GetAvailableSkillsAtRange(distanceToTarget);
            
            // 쿨다운 중인 스킬 제외
            availableSkills.RemoveAll(skill => _skillCooldowns.ContainsKey(skill));
            
            if (availableSkills.Count == 0)
            {
                Debug.LogWarning($"[EnemyCombat] 거리 {distanceToTarget:F1}m에서 사용 가능한 스킬이 없습니다!");
                return null;
            }
            
            // 가중치 기반 스킬 선택
            _currentSkill = _attackData.SelectRandomSkill(availableSkills);

            if (_currentSkill != null)
            {
                // 스킬 쿨다운 시작
                _skillCooldowns[_currentSkill] = _currentSkill.cooldown;

            }

            return _currentSkill;
        }

        /// <summary>
        /// 근접 공격 히트 체크
        /// </summary>
        public void CheckMeleeAttackHit()
        {
            if (_currentSkill == null || _currentSkill.baseInfo.attackType != AttackType.Melee)
                return;
            
            Vector3 attackPosition = _attackOrigin.position + 
                                    _attackOrigin.forward * _currentSkill.baseInfo.attackOffset.z + 
                                    _attackOrigin.right * _currentSkill.baseInfo.attackOffset.x + 
                                    _attackOrigin.up * _currentSkill.baseInfo.attackOffset.y;
            
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, _currentSkill.baseInfo.attackRadius, _targetLayer);
            
            foreach (var hitCollider in hitColliders)
            {
                if (_hitTargets.Contains(hitCollider))
                    continue;
                
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage())
                {
                    AttackData attackData = new AttackData
                    {
                        damage = _currentSkill.baseInfo.damage,
                        criticalMultiplier = 1.0f,
                        hitPoint = hitCollider.ClosestPoint(attackPosition),
                        attackDirection = _attackOrigin.forward,
                        reactionType = _currentSkill.baseInfo.reactionType,
                        hitParticleName =  _currentSkill.baseInfo.hitParticleName
                    };
                    
                    damageable.TakeDamage(attackData);
                    _hitTargets.Add(hitCollider);
                    
                    GameObjectManager.Instance.ShowFX(
                        _currentSkill.baseInfo.hitParticleName,
                        attackData.hitPoint);
                }
            }
        }

        public void ClearHitTargets()
        {
            _hitTargets.Clear();
        }

        public void SetEnableCollision(bool isCollisionEnable)
        {
            _isCollisionEnabled = isCollisionEnable;
        }
        
        public Vector3 GetCurrentAttackPosition()
        {
            if (_currentSkill == null)
                return _attackOrigin.position;
            
            return _attackOrigin.position + 
                   _attackOrigin.forward * _currentSkill.baseInfo.attackOffset.z + 
                   _attackOrigin.right * _currentSkill.baseInfo.attackOffset.x + 
                   _attackOrigin.up * _currentSkill.baseInfo.attackOffset.y;
        }

        public float GetCurrentAttackRadius()
        {
            if (_currentSkill == null)
                return 0f;
            
            return _currentSkill.baseInfo.attackRadius;
        }
    }
}