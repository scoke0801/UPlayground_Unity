using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 설정 데이터
/// NPC 타입별 행동, 대화, 퀘스트, 상점 설정 관리
/// </summary>
[CreateAssetMenu(fileName = "New NPC Settings", menuName = "RPG/NPC/Settings Data")]
public class NPCSettingsData : ScriptableObject
{
    [Header("기본 설정")]
    [Tooltip("NPC 타입")]
    public NPCType npcType = NPCType.Normal;
    [Tooltip("상호작용 범위")]
    public float interactionRange = 3f;
    
    [Header("AI 설정")]
    [Tooltip("순찰 범위")]
    public float patrolRange = 5f;
    [Tooltip("NPC 이동 속도")]
    public float npcWalkSpeed = 2f;
    [Tooltip("대기 시간 최소")]
    public float idleTimeMin = 2f;
    [Tooltip("대기 시간 최대")]
    public float idleTimeMax = 5f;
    
    [Header("대화 시스템")]
    [Tooltip("기본 대화 목록")]
    public List<string> dialogues = new List<string>();
    
    [Header("퀘스트 시스템")]
    [Tooltip("제공 가능한 퀘스트")]
    public List<Quest> availableQuests = new List<Quest>();
    
    [Header("상점 시스템 (상인 NPC용)")]
    [Tooltip("판매 아이템")]
    public List<ShopItem> shopItems = new List<ShopItem>();
    [Tooltip("아이템 구매 시 가격 비율 (%)")]
    public int shopBuyBackPercentage = 50;
}
