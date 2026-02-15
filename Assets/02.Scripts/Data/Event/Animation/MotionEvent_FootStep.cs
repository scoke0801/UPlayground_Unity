using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 발자국 이벤트 (지형별 사운드)
    /// </summary>
    [Serializable]
    public class FootstepEvent : MotionEventBase
    {
        public enum Foot { Left, Right }
        public Foot foot;
        [Range(0f, 1f)] public float volume = 0.5f;

        public override string GetDisplayName() => "Footstep";

        public override string GetShortLabel() => $"Foot: {foot}";

        public override void Execute(GameObject target)
        {
            Debug.Log($"Footstep: {foot}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}