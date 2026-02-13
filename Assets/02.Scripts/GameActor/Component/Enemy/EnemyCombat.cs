using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 전투 시스템 - 공격 실행 및 콤보 관리
    /// </summary>
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private EnemyAttackDataSO _attackData;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayer;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        
        private int _currentComboIndex = 0;
        private bool _canCombo = false;
        private List<Collider> _hitTargets = new List<Collider>();
        
        public bool CanCombo => _canCombo;
        public EnemyAttackDataSO AttackData => _attackData;
        public int CurrentComboIndex => _currentComboIndex;
        
        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;
        }

        /// <summary>
        /// 일반 공격 실행
        /// </summary>
        public EnemyAttackInfo ExecuteAttack()
        {
            if (_attackData == null || _attackData.AttackList.Count == 0)
            {
                Debug.LogWarning("[EnemyCombat] 공격 데이터가 없습니다!");
                return null;
            }
            
            // 현재 콤보 인덱스의 공격 가져오기
            EnemyAttackInfo attackInfo = _attackData.AttackList[_currentComboIndex];
            
            Debug.Log($"[EnemyCombat] 공격 실행: {attackInfo.animKey} (Combo {_currentComboIndex + 1}/{_attackData.AttackList.Count})");
            
            return attackInfo;
        }

        /// <summary>
        /// 다음 콤보로 진행
        /// </summary>
        public void AdvanceCombo()
        {
            if (_attackData == null || _attackData.AttackList.Count == 0)
                return;
            
            _currentComboIndex++;
            
            // 콤보 끝에 도달하면 리셋
            if (_currentComboIndex >= _attackData.AttackList.Count)
            {
                ResetCombo();
            }
            
            Debug.Log($"[EnemyCombat] 콤보 진행: {_currentComboIndex + 1}/{_attackData.AttackList.Count}");
        }

        /// <summary>
        /// 콤보 리셋
        /// </summary>
        public void ResetCombo()
        {
            _currentComboIndex = 0;
            _canCombo = false;
            Debug.Log("[EnemyCombat] 콤보 리셋");
        }

        /// <summary>
        /// 콤보 윈도우 열기 (애니메이션 이벤트에서 호출)
        /// </summary>
        public void OpenComboWindow()
        {
            _canCombo = true;
        }

        /// <summary>
        /// 콤보 윈도우 닫기 (애니메이션 이벤트에서 호출)
        /// </summary>
        public void CloseComboWindow()
        {
            _canCombo = false;
        }

        /// <summary>
        /// 맞은 대상 초기화
        /// </summary>
        public void ClearHitTargets()
        {
            _hitTargets.Clear();
        }

        /// <summary>
        /// 공격 히트 체크 (애니메이션 이벤트에서 프레임마다 호출 또는 Update에서 체크)
        /// </summary>
        public void CheckAttackHit()
        {
            if (_attackData == null || _currentComboIndex >= _attackData.AttackList.Count)
                return;
            
            EnemyAttackInfo currentAttack = _attackData.AttackList[_currentComboIndex];
            
            Vector3 attackPosition = _attackOrigin.position + _attackOrigin.forward * currentAttack.attackOffset.z 
                                                            + _attackOrigin.right * currentAttack.attackOffset.x 
                                                            + _attackOrigin.up * currentAttack.attackOffset.y;
            
            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, currentAttack.attackRadius, _targetLayer);
            
            foreach (var hitCollider in hitColliders)
            {
                // 이미 맞은 타겟은 제외
                if (_hitTargets.Contains(hitCollider))
                    continue;
                
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage())
                {
                    // 데미지 적용
                    AttackData attackData = new AttackData
                    {
                        damage = currentAttack.damage,
                        criticalMultiplier = 1.0f,
                        hitPoint = attackPosition,
                        attackDirection = _attackOrigin.forward
                    };
                    
                    damageable.TakeDamage(attackData);
                    _hitTargets.Add(hitCollider);
                    
                    Debug.Log($"[EnemyCombat] {hitCollider.name}에게 {currentAttack.damage} 데미지!");
                }
            }
        }

        /// <summary>
        /// 현재 공격의 히트박스 위치 가져오기
        /// </summary>
        public Vector3 GetCurrentAttackPosition()
        {
            if (_attackData == null || _currentComboIndex >= _attackData.AttackList.Count)
                return _attackOrigin.position;
            
            EnemyAttackInfo currentAttack = _attackData.AttackList[_currentComboIndex];
            
            return _attackOrigin.position + _attackOrigin.forward * currentAttack.attackOffset.z 
                                          + _attackOrigin.right * currentAttack.attackOffset.x 
                                          + _attackOrigin.up * currentAttack.attackOffset.y;
        }

        /// <summary>
        /// 현재 공격의 히트박스 반경 가져오기
        /// </summary>
        public float GetCurrentAttackRadius()
        {
            if (_attackData == null || _currentComboIndex >= _attackData.AttackList.Count)
                return 0f;
            
            return _attackData.AttackList[_currentComboIndex].attackRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos || _attackData == null || _attackData.AttackList.Count == 0)
                return;
            
            Transform origin = _attackOrigin != null ? _attackOrigin : transform;
            
            // 현재 콤보의 공격 범위 표시
            if (_currentComboIndex < _attackData.AttackList.Count)
            {
                EnemyAttackInfo attack = _attackData.AttackList[_currentComboIndex];
                
                Vector3 attackPos = origin.position + origin.forward * attack.attackOffset.z 
                                                     + origin.right * attack.attackOffset.x 
                                                     + origin.up * attack.attackOffset.y;
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPos, attack.attackRadius);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin.position, attackPos);
            }
        }
#endif
    }
}