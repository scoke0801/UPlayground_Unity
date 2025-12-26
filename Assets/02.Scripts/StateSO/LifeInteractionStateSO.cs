using UnityEngine;
using Animancer;
using Interaction.Enum;
using UnityEngine.Serialization;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_LifeInteraction", menuName = "UP/FSM/States/LifeInteraction")]
    public class LifeInteractionStateSO : StateSO
    {
        [FormerlySerializedAs("interactionType")] [Header("Settings")]
        public LifeInteractionType lifeInteractionType;
        
        [Header("Transitions")]
        public StateSO LocomotionState; // 상승 완료 후 전환할 상태
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition interactionAnim = brain.AnimData.GetAnimation(GetAnimKey());
            
            if (interactionAnim == null) { Debug.LogError($"[{interactionAnim}] 클립이 없습니다!"); return; }
            
            // 애니메이션 재생
            brain.Animancer.Play(interactionAnim, 0f);
            
            GameObjectManager.Instance.OnStartInteraction();
            
            // 인터랙션 대상을 바라보도록 회전
            RotateTowardsTarget(brain);
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

        private void RotateTowardsTarget(CharacterBrain brain)
        {
            Actor.InteractableActor target = GameObjectManager.Instance.GetCurrentInteractionTarget();
            if (target != null)
            {
                Vector3 directionToTarget = target.transform.position - brain.transform.position;
                directionToTarget.y = 0; // Y축 회전만 적용
                
                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    brain.Rb.MoveRotation(targetRotation);
                }
            }
        }

        private AnimKey GetAnimKey()
        {
            // [TODO] 인터렉션 타입에 맞게 수정 필요
            return AnimKey.WoodCut;
        }
    }
}
