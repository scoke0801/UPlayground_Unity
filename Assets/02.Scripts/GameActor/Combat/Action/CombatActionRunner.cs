using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Combat
{
    public class CombatActionRunner : MonoBehaviour
    {
        private GameActor _owner;

        public CombatActionInstance CurrentAction { get; private set; }
        public bool HasAction => CurrentAction != null;

        private void Awake()
        {
            _owner = GetComponent<GameActor>();
        }

        public void StartLegacyAction(AttackData attackData)
        {
            if (attackData == null)
                return;

            var definition = new CombatActionDefinition(
                attackData.animKey,
                attackData,
                attackData);
            CurrentAction = new CombatActionInstance(_owner, definition);
            HandleTimelineEvent(CombatTimelineEventType.ActionStarted, attackData.hitPhaseIndex);
        }

        public void HandleTimelineEvent(CombatTimelineEventType type, int hitPhaseIndex = 0)
        {
            if (CurrentAction == null)
                return;

            CurrentAction.ApplyTimelineEvent(new CombatTimelineEvent(type, hitPhaseIndex, Time.time));

            if (type == CombatTimelineEventType.ActionEnded)
                CurrentAction = null;
        }
    }
}
