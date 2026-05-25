using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    public abstract class TriggerConditionSO : ScriptableObject
    {
        public abstract bool Evaluate(TriggerContext context);
    }
}
