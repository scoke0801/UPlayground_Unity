using UnityEngine;

/// <summary>
/// 캐릭터 기본 스탯 데이터
/// ScriptableObject로 관리하여 재사용 및 밸런싱 용이
/// </summary>
[CreateAssetMenu(fileName = "New Character Stats", menuName = "RPG/Character/Stats Data")]
public class CharacterStatsData : ScriptableObject
{
    [Header("체력")]
    [Tooltip("최대 체력")]
    public float maxHP = 100f;
    [Tooltip("초당 체력 회복량")]
    public float hpRegenRate = 5f;
    
    [Header("마나")]
    [Tooltip("최대 마나")]
    public float maxMP = 50f;
    [Tooltip("초당 마나 회복량")]
    public float mpRegenRate = 10f;
    
    [Header("스태미나")]
    [Tooltip("최대 스태미나")]
    public float maxStamina = 100f;
    [Tooltip("초당 스태미나 회복량")]
    public float staminaRegenRate = 20f;
    
    [Header("스태미나 소모")]
    [Tooltip("점프 시 소모")]
    public float jumpStaminaCost = 20f;
    [Tooltip("구르기 시 소모")]
    public float rollStaminaCost = 30f;
    [Tooltip("달리기 초당 소모")]
    public float runStaminaCostPerSecond = 10f;
}
