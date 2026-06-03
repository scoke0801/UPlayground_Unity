using UnityEngine;

namespace UPlayGround.Combat
{
    public sealed class CombatActionInstance
    {
        public readonly GameActor Owner;
        public readonly CombatActionDefinition Definition;
        public readonly float StartedTime;
        public int CurrentPhaseIndex { get; private set; }
        public bool IsCollisionActive { get; private set; }

        public CombatActionInstance(GameActor owner, CombatActionDefinition definition)
        {
            Owner = owner;
            Definition = definition;
            StartedTime = Time.time;
        }

        public void ApplyTimelineEvent(CombatTimelineEvent timelineEvent)
        {
            switch (timelineEvent.Type)
            {
                case CombatTimelineEventType.BeginCollision:
                    IsCollisionActive = true;
                    CurrentPhaseIndex = timelineEvent.HitPhaseIndex;
                    break;
                case CombatTimelineEventType.EndCollision:
                    IsCollisionActive = false;
                    break;
                case CombatTimelineEventType.HitPhaseChanged:
                    CurrentPhaseIndex = timelineEvent.HitPhaseIndex;
                    break;
            }
        }
    }
}
