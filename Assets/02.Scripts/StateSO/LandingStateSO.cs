using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Landing", menuName = "UP/FSM/States/Landing")]
    public class LandingStateSO : StateSO
    {
        public override void OnEnter(CharacterBrain brain)
        {          
            Debug.Log("LandingStateSO.OnEnter");
            ITransition landAnim = brain.AnimData.GetAnimation(AnimKey.Land);
            if (landAnim == null) { Debug.LogError($"[{AnimKey.Land}] 클립이 없습니다!"); return; }
            
            
            // 착지 애니메이션 재생
            var state = brain.Animancer.Play(landAnim);
            state.OwnedEvents.OnEnd = () => brain.ChangeState(brain.DefaultState);
        }
        
    }
}