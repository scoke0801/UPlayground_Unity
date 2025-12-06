using UnityEngine;
using Animancer;
using Interaction.Enum;
using UnityEngine.Serialization;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_LifeInteraction", menuName = "FSM/States/LifeInteraction")]
    public class LifeInteractionStateSO : StateSO
    {
        [FormerlySerializedAs("interactionType")] [Header("Settings")]
        public LifeInteractionType lifeInteractionType;
        
        [Header("Transitions")]
        public StateSO LocomotionState; // 상승 완료 후 전환할 상태
        
        public override void OnEnter(CharacterBrain brain)
        {
            ClipTransition interactionAnim = brain.AnimData.GetClipTransition(GetAnimKey());
            
            if (interactionAnim == null) { Debug.LogError($"[{interactionAnim}] 클립이 없습니다!"); return; }
            
            // 1. 애니메이션 재생
            brain.Animancer.Play(interactionAnim, 0f);
            
            GameObjectManager.Instance.OnStartInteraction();
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                brain.ChangeState(brain.DefaultState);
            }
        }

        public override void OnExit(CharacterBrain brain)
        {
            GameObjectManager.Instance.OnEndInteraction();
        }

        private string GetAnimKey()
        {
            return lifeInteractionType.ToString();
        }
    }
}