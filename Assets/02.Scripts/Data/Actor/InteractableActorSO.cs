using System.Collections.Generic;
using Interaction.Enum;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableActorSO", menuName = "UPlayGround/ActorData/InteractableActorSO")]
public class InteractableActorSO : ScriptableObject
{
    public string actorName;
    public string description;

    public InteractionObjectType interactionObjectType;

    public int hp;
    
    public List<ItemDropList> dropItems = new List<ItemDropList>();

    public bool showInfoUI = true;
    public bool showShakeEffect = true;

    [Header("회복 (REST_POINT 전용)")]
    public bool reviveDowned = true;     // HP 0 멤버도 부활
    // 풀 회복 고정이라 ratio 필드는 생략. 추후 부분회복 원하면 [Range(0,1)] healRatio 추가.
}
