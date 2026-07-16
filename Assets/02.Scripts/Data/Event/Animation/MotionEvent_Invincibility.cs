using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 무적 상태 이벤트.
    /// startTime에 무적 ON, endTime에 무적 OFF.
    /// Player/Monster 모두 지원 (SetInvincible 공통 호출)
    /// </summary>
    [Serializable]
    [MotionEventMeta("Invincibility", Category = "Combat", CategoryOrder = 0,
        Description = "피격 무적 구간을 설정합니다.",
        Aliases = new[] { "iframe", "invincible", "무적", "회피" },
        Icon = "🛡", Color = new[] { 0.98f, 0.55f, 0.15f })]
    public class InvincibilityEvent : MotionEventBase
    {
        public override string GetDisplayName() => "Invincibility";
        public override string GetShortLabel() => "Invincible";

        public override void Execute(GameObject target) => SetInvincible(target, true);
        public override void OnCompleteEvent(GameObject target) => SetInvincible(target, false);

        private void SetInvincible(GameObject target, bool invincible)
        {
            var actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            switch (actor)
            {
                case PlayerActor player:
                    player.SetInvincible(invincible);
                    break;
                case MonsterActor monster:
                    monster.SetInvincible(invincible);
                    break;
            }
        }
    }
}