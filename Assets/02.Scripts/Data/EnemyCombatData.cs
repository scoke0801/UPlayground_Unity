using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy 전투 설정 데이터
/// 공격력, 공격 패턴 등 전투 관련 파라미터
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Combat Data", menuName = "RPG/Enemy/Combat Data")]
public class EnemyCombatData : ScriptableObject
{
    [Header("기본 전투 설정")]
    [Tooltip("기본 공격력")]
    public float baseAttackDamage = 20f;
    [Tooltip("레벨당 공격력 증가")]
    public float attackDamagePerLevel = 5f;
    
    [Header("공격 패턴")]
    [Tooltip("사용 가능한 공격 패턴 리스트")]
    public List<AttackPattern> attackPatterns = new List<AttackPattern>();
    
    [Header("방어 설정")]
    [Tooltip("피격 시 무적 시간")]
    public float hitInvincibilityTime = 0.1f;
}
