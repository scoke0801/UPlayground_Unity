using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Move_Stop", menuName = "UP/FSM/States/Move Stop")]
    public class StopStateSO : StateSO
    {
        [SerializeField] private float fadeDuration = 0.1f;
        [SerializeField] private LocomotionStateSO locomotionState;

        public override void OnEnter(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            AnimKey stopKey;

            // 속도 구간별 적절한 Stop 애니메이션 선택
            if (lastSpeed > locomotionState.MoveSpeed) stopKey = AnimKey.Move_Stop_Sprinting;
            else if (lastSpeed > locomotionState.walkSpeed) stopKey = AnimKey.Move_Stop_Running;
            else stopKey = AnimKey.Move_Stop_Walking;

            var anim = brain.AnimData.GetAnimation(stopKey);
            var state = brain.Animancer.Play(anim, fadeDuration);
            
            // 애니메이션이 끝나면 다시 Locomotion(Idle)으로 복귀
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.Add(0.85f, () => brain.ChangeState(brain.DefaultState));
                //events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }
    }
}