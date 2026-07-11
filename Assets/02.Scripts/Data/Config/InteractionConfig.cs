using UnityEngine;

namespace UPlayGround.Data.Config
{
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "UPlayGround/설정/Interaction")]
    public class InteractionConfig : ScriptableObject
    {
        public float checkRadius = 5.0f;
        public LayerMask interactableLayer;

        public float activationDistance = 3.0f;

    }
}
