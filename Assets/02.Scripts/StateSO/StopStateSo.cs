using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Move_Stop", menuName = "UP/FSM/States/Move Stop")]
    public class StopStateSO : StateSO
    {
        [SerializeField] private float fadeDuration = 0.1f;
        private bool isOverSpeed = false;

        public override void OnEnter(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            AnimKey stopKey;

            // 속도 구간별 적절한 Stop 애니메이션 선택
            if (lastSpeed > 20f) stopKey = AnimKey.Move_Stop_Sprinting;
            else if (lastSpeed > 10f) stopKey = AnimKey.Move_Stop_Running;
            else stopKey = AnimKey.Move_Stop_Walking;

            isOverSpeed = lastSpeed > 10f;

            var anim = brain.AnimData.GetAnimation(stopKey);
            var state = brain.Animancer.Play(anim, fadeDuration);
            
            // 애니메이션이 끝나면 다시 Locomotion(Idle)으로 복귀
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.Add(0.85f, () => brain.ChangeState(brain.DefaultState));
                //events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            if(isOverSpeed == false)
            {
                return;
            }
            
            // 정지 시 미끄러지는 물리 저항 구현
            Vector3 v = brain.Rb.linearVelocity;
            v.x *= 0.8f;
            v.z *= 0.8f;
            brain.Rb.linearVelocity = v;
        }
    }
}