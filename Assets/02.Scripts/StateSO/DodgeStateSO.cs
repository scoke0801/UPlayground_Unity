using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Dodge", menuName = "UP/FSM/States/Dodge")]
    public class DodgeStateSO : StateSO
    {
        [Header("Settings")]
        public float DodgeSpeed = 10f;   
        public float InvincibleDuration = 0.5f; 

        private Vector3 _dodgeDir;
        
        [SerializeField] private float fadeDuration = 0.1f;

        public override void OnEnter(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
                _dodgeDir = brain.InputDirection;
            else
                _dodgeDir = brain.transform.forward;

            brain.transform.rotation = Quaternion.LookRotation(_dodgeDir);
            
            ITransition dodgeAnim = brain.AnimData.GetAnimation(AnimKey.Dodge);
            if (dodgeAnim == null) { Debug.LogError($"[{AnimKey.Dodge}] 클립이 없습니다!"); return; }
            
            var state = brain.Animancer.Play(dodgeAnim, fadeDuration);
            
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            //brain.Rb.linearVelocity = new Vector3(_dodgeDir.x * DodgeSpeed, brain.Rb.linearVelocity.y, _dodgeDir.z * DodgeSpeed);
        }
    }
}