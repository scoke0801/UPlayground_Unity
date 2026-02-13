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
}
