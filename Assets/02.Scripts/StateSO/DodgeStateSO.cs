using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Dodge", menuName = "FSM/States/Dodge")]
    public class DodgeStateSO : StateSO
    {
        [Header("Settings")]
        public string DodgeAnimKey = "Dodge";
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
            
            ClipTransition dodgeAnim = brain.AnimData.GetClipTransition(DodgeAnimKey);
            if (dodgeAnim.Clip == null) { Debug.LogError($"[{DodgeAnimKey}] 클립이 없습니다!"); return; }
            
            var state = brain.Animancer.Play(dodgeAnim, fadeDuration);
            
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