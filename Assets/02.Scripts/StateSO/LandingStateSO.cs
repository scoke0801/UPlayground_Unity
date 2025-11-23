using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Landing", menuName = "FSM/States/Landing")]
    public class LandingStateSO : StateSO
    {
        [Header("Settings")]
        public string LandAnimKey = "Land";
        
        public override void OnEnter(CharacterBrain brain)
        {
            ClipTransition landAnim = brain.AnimData.GetClipTransition(LandAnimKey);
            if (landAnim.Clip == null) { Debug.LogError($"[{LandAnimKey}] 클립이 없습니다!"); return; }
            
            // 착지 애니메이션 재생
            
            var state = brain.Animancer.Play(landAnim);
            state.OwnedEvents.OnEnd = () => 
            {
                brain.ChangeState(brain.DefaultState);
            };
        }
    }
}