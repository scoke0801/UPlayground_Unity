// LandingStateSO.cs (신규)
using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Landing", menuName = "FSM/States/Landing")]
    public class LandingStateSO : StateSO
    {
        [Header("Settings")]
        public ClipTransition LandAnim; 
        
        public override void OnEnter(CharacterBrain brain)
        {
            // 착지 애니메이션 재생
            var animState = brain.Animancer.Play(LandAnim);

            if (animState.Events(brain, out AnimancerEvent.Sequence events))
            {
                // 애니메이션 끝나면 기본 상태(Locomotion)로 복귀
                events.OnEnd = () => 
                {
                    brain.ChangeState(brain.DefaultState);
                };
            }
            // LandAnim이 null이거나 이벤트 설정 실패 시, 즉시 복귀
            else
            {
                brain.ChangeState(brain.DefaultState);
            }
        }
    }
}