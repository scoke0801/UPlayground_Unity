using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Dodge", menuName = "FSM/States/Dodge")]
    public class DodgeStateSO : StateSO
    {
        [Header("Settings")]
        public ClipTransition DodgeAnim; 
        public float DodgeSpeed = 10f;   
        public float InvincibleDuration = 0.5f; 

        private Vector3 _dodgeDir;

        public override void OnEnter(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
                _dodgeDir = brain.InputDirection;
            else
                _dodgeDir = brain.transform.forward;

            brain.transform.rotation = Quaternion.LookRotation(_dodgeDir);

            var state = brain.Animancer.Play(DodgeAnim);

            // [수정] Animancer 이벤트 할당 문법 수정
            // state.Events.OnEnd 방식 대신 아래 방식을 사용하세요.
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            brain.Rb.linearVelocity = new Vector3(_dodgeDir.x * DodgeSpeed, brain.Rb.linearVelocity.y, _dodgeDir.z * DodgeSpeed);
        }
    }
}