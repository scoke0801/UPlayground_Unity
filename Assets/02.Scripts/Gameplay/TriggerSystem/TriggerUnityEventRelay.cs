using UnityEngine;
using UnityEngine.Events;

namespace UPlayGround.TriggerSystem
{
    [AddComponentMenu("UPlayGround/Trigger/Trigger Unity Event Relay")]
    public sealed class TriggerUnityEventRelay : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onSequenceStarted;
        [SerializeField] private UnityEvent _onSequenceCompleted;

        public UnityEvent OnStarted => _onSequenceStarted;
        public UnityEvent OnCompleted => _onSequenceCompleted;

        public void InvokeStarted()
        {
            _onSequenceStarted?.Invoke();
        }

        public void InvokeCompleted()
        {
            _onSequenceCompleted?.Invoke();
        }
    }
}
