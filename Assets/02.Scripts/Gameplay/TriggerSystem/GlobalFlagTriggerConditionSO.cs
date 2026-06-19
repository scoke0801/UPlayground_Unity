using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/Global Flag")]
    public sealed class GlobalFlagTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _expected = true;

        public override bool Evaluate(TriggerContext context)
        {
            if (string.IsNullOrEmpty(_key) || GlobalFlagManager.Instance == null)
                return false;

            return GlobalFlagManager.Instance.GetFlag(_key) == _expected;
        }
    }
}
