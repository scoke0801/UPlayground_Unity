using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 발자국 이벤트 (지형별 사운드)
    /// </summary>
    [Serializable]
    [MotionEventMeta("Footstep", Category = "VFX / SFX", CategoryOrder = 10,
        Description = "지형 기반 발자국 사운드를 재생합니다.",
        Aliases = new[] { "foot", "step", "walk", "발소리", "발자국" },
        Icon = "👣", Color = new[] { 0.65f, 0.55f, 1.00f })]
    public class FootstepEvent : MotionEventBase
    {
        public enum Foot { Left, Right }
        public Foot foot;
        public string soundKey = "footstep_default";
        [Range(0f, 1f)] public float volume = 0.5f;

        public override string GetDisplayName() => "Footstep";

        public override string GetShortLabel() => $"Foot: {foot}";

        public override void Execute(GameObject target)
        {
            if (target == null || string.IsNullOrWhiteSpace(soundKey))
                return;

            SoundManager.Instance?.PlaySfx(soundKey, target.transform.position, volume);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
