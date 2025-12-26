using UnityEngine;

[CreateAssetMenu(fileName = "InteractionConfig", menuName = "UP/SO/Config/InteractionConfig")]
public class InteractionConfig : ScriptableObject
{
    public float checkRadius = 5.0f;
    public LayerMask interactableLayer;

    public float activationDistance = 3.0f;

}
