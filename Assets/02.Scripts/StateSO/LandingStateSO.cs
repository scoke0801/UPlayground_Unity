using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Landing", menuName = "UP/FSM/States/Landing")]
    public class LandingStateSO : StateSO
    {
        private bool _isAnimEnd = false;
        public override void OnEnter(CharacterBrain brain)
        {          
            Debug.Log("LandingStateSO.OnEnter");
            ITransition landAnim = brain.AnimData.GetAnimation(AnimKey.Land);
            if (landAnim == null) { Debug.LogError($"[{AnimKey.Land}] 클립이 없습니다!"); return; }
            
            // 착지 애니메이션 재생
            
            var state = brain.Animancer.Play(landAnim);
            // if (state.Events(brain, out AnimancerEvent.Sequence events))
            // {
            //     events.Add(0.9f, () => brain.ChangeState(brain.DefaultState));
            // }

            _isAnimEnd = false;
            state.OwnedEvents.OnEnd = () => _isAnimEnd = true;
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            if (!_isAnimEnd) return;
            
            brain.Motor.BaseVelocity.y = 0;
            brain.ChangeState(brain.DefaultState);
        }
        
    }
}