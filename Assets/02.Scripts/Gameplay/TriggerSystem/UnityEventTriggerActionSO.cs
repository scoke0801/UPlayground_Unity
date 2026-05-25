using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Unity Event")]
    public sealed class UnityEventTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private UnityEvent _event;

        public override IEnumerator Execute(TriggerContext context)
        {
            _event?.Invoke();
            yield break;
        }
    }
}
