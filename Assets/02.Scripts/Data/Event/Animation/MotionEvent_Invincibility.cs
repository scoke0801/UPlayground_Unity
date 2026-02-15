using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 무적 상태 이벤트
    /// </summary>
    [Serializable]
    public class InvincibilityEvent : MotionEventBase
    {
        public bool canCancelByInput = false;

        public override string GetDisplayName() => "Invincibility";

        public override string GetShortLabel() => "Invincible";

        public override void Execute(GameObject target)
        {
            Debug.Log("Invincibility Active");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}