using UPlayGround.Dialogue;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/Dialogue/Action/SetFlag", fileName = "Action_SetFlag_")]
    public class SetFlagActionSO : DialogueActionSO
    {
        public string flagKey;
        public bool value = true;

        public override void Execute()
        {
            GlobalFlagManager.Instance.SetFlag(flagKey, value);
        }
    }
}
