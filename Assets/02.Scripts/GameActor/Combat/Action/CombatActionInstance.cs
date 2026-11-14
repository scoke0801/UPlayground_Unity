using UnityEngine;

namespace UPlayGround.Combat
{
    public sealed class CombatActionInstance
    {
        private static ulong _nextActionId;
        public readonly ulong ActionId;
        public readonly GameActor Owner;
        public readonly float StartedTime;
        public int CurrentPhaseIndex { get; private set; }
        public bool IsCollisionActive { get; private set; }
        public string ActiveHitboxGroupId { get; private set; }

        public CombatActionInstance(GameActor owner)
        {
            Owner = owner;
            ActionId = ++_nextActionId;
            StartedTime = Time.time;
        }

        public void ApplyTimelineEvent(CombatTimelineEvent timelineEvent)
        {
            switch (timelineEvent.Type)
            {
                case CombatTimelineEventType.ActionStarted:
                    CurrentPhaseIndex = timelineEvent.HitPhaseIndex;
                    IsCollisionActive = false;
                    ActiveHitboxGroupId = null;
                    break;
                case CombatTimelineEventType.BeginCollision:
                    IsCollisionActive = true;
                    CurrentPhaseIndex = timelineEvent.HitPhaseIndex;
                    break;
                case CombatTimelineEventType.EndCollision:
                    IsCollisionActive = false;
                    ActiveHitboxGroupId = null;
                    break;
                case CombatTimelineEventType.HitPhaseChanged:
                    CurrentPhaseIndex = timelineEvent.HitPhaseIndex;
                    break;
            }
        }

        public void SetHitboxGroup(string hitboxGroupId)
        {
            ActiveHitboxGroupId = string.IsNullOrWhiteSpace(hitboxGroupId)
                ? null
                : hitboxGroupId.Trim();
        }
    }
}
