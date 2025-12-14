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
                
                Vector3 targetPosition = target.transform.position;
                var targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    targetPosition.y += targetCollider.bounds.extents.y * 0.5f;
                }
                
                GameObjectManager.Instance.ShowFX("InteractionObjectHitFX", targetPosition);
            }
        }
    }
}
