using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트
    /// </summary>
    [Serializable]
    public class BeginCollisionEvent : MotionEventBase
    {
        public string colliderName;
        public float damage = 10f;
        public LayerMask targetLayers = -1;

        public override string GetDisplayName() => "Collision";

        public override string GetShortLabel()
        {
            if (!string.IsNullOrEmpty(colliderName))
                return $"Collision: {colliderName}";
            return "Collision";
        }

        public override void Execute(GameObject target)
        {
            // 충돌 판정 로직 구현
            Debug.Log($"Collision Active: {colliderName}, Damage={damage}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}