using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy 보상 설정 데이터
/// 경험치, 골드, 드롭 아이템 관리
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Reward Data", menuName = "RPG/Enemy/Reward Data")]
public class EnemyRewardData : ScriptableObject
{
    [Header("경험치")]
    [Tooltip("기본 경험치")]
    public int baseExperience = 50;
    [Tooltip("레벨당 경험치 증가")]
    public int experiencePerLevel = 25;
    
    [Header("골드")]
    [Tooltip("최소 골드 드롭")]
    public int minGoldDrop = 10;
    [Tooltip("최대 골드 드롭")]
    public int maxGoldDrop = 50;
    [Tooltip("레벨당 골드 증가 (%)")]
    public float goldIncreasePerLevel = 10f;
    
    [Header("아이템 드롭")]
    [Tooltip("드롭 가능한 아이템 리스트")]
    public List<DropItem> dropItems = new List<DropItem>();
}
