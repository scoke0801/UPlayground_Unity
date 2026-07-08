using System.Collections.Generic;
using Interaction.Enum;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableActorSO", menuName = "UPlayGround/액터/Interactable")]
public class InteractableActorSO : ScriptableObject
{
    public string actorName;
    public string description;

    public InteractionObjectType interactionObjectType;

    public int hp;
    
    public List<ItemDropList> dropItems = new List<ItemDropList>();

    public bool showInfoUI = true;
    public bool showShakeEffect = true;

    [Header("낚시 (FISHING_ZONE 전용)")]
    [Tooltip("낚시 성공 N회 후 소진 상태로 만든다. 0 이하면 무제한.")]
    [Min(0)] public int fishingDepleteCatchCount = 0;

    [Header("회복 (REST_POINT 전용)")]
    public bool reviveDowned = true;     // HP 0 멤버도 부활
    // 풀 회복 고정이라 ratio 필드는 생략. 추후 부분회복 원하면 [Range(0,1)] healRatio 추가.
}
