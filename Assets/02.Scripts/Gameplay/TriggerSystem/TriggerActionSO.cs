using System.Collections;
using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    public abstract class TriggerActionSO : ScriptableObject
    {
        public virtual bool CanExecute(TriggerContext context) => true;
        public virtual bool ConsumesTrigger(TriggerContext context) => true;
        public abstract IEnumerator Execute(TriggerContext context);
    }
}
