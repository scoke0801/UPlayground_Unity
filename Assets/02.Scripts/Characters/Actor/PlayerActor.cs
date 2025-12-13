using UnityEngine;

namespace Actor
{
    public class PlayerActor : BaseActor
    {
        // [TODO] 다른 방법으로 인터렉션 대상을 정리해야지 이렇게 하면 안된다..
        public static InteractableActor TargetActor = null;
        
        public void Hit()
        {
            if (TargetActor != null)
            {
                TargetActor.OnHit(30);
            }
        }
    }
}