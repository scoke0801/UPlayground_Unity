using UnityEngine;

/// <summary>
/// Enemy AI 행동 설정 데이터
/// 감지, 추적, 공격 범위 등 AI 파라미터 관리
/// </summary>
[CreateAssetMenu(fileName = "New Enemy AI Data", menuName = "RPG/Enemy/AI Data")]
public class EnemyAIData : ScriptableObject
{
    [Header("감지 설정")]
    [Tooltip("적 감지 범위")]
    public float detectionRange = 10f;
    [Tooltip("추적 범위 (이 범위를 벗어나면 추적 중단)")]
    public float chaseRange = 15f;
    [Tooltip("목표를 잃은 후 대기 시간")]
    public float lostTargetTime = 3f;
    
    [Header("순찰 설정")]
    [Tooltip("순찰 범위")]
    public float patrolRange = 5f;
    [Tooltip("순찰 대기 시간 (최소)")]
    public float patrolWaitTimeMin = 1f;
    [Tooltip("순찰 대기 시간 (최대)")]
    public float patrolWaitTimeMax = 3f;
    
    [Header("공격 설정")]
    [Tooltip("공격 시작 범위")]
    public float attackRange = 2f;
    [Tooltip("공격 쿨다운")]
    public float attackCooldown = 2f;
}
