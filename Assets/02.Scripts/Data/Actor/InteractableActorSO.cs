using Interaction.Enum;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableActorSO", menuName = "ActorData/InteractableActorSO")]
public class InteractableActorSO : ScriptableObject
{
    public string actorName;
    public string description;

    public InteractionObjectType interactionObjectType;

    public int hp;
}
