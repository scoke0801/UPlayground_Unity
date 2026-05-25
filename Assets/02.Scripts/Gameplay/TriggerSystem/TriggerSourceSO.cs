using System;
using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    public abstract class TriggerSourceSO : ScriptableObject
    {
        public virtual void Subscribe(TriggerComposer composer, Action<TriggerContext> onFire) { }
        public virtual void Unsubscribe(TriggerComposer composer, Action<TriggerContext> onFire) { }

        public virtual void HandleTriggerEnter(TriggerComposer composer, Collider other, Action<TriggerContext> onFire) { }
        public virtual void HandleTriggerExit(TriggerComposer composer, Collider other, Action<TriggerContext> onFire) { }
    }
}
