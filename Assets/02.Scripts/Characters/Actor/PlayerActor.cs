using UnityEngine;

namespace Actor
{
    public class PlayerActor : BaseActor
    {
        public void Hit()
        {
            InteractableActor target = GameObjectManager.Instance.GetCurrentInteractionTarget();
            if (target != null)
            {
                target.OnHit(30);
            }
        }
    }
}
