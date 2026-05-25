using System.Collections;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Set Global Flag")]
    public sealed class SetFlagTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _value = true;

        public override bool CanExecute(TriggerContext context)
        {
            return !string.IsNullOrEmpty(_key) && GlobalFlagManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            GlobalFlagManager.Instance?.SetFlag(_key, _value);
            yield break;
        }
    }
}
