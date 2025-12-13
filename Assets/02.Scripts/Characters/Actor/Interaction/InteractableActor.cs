using UnityEngine;

namespace Actor
{
    public class InteractableActor : BaseActor
    {
        [SerializeField] private InteractableActorSO _dataSO;
        
        public bool IsInteraction {get; private set;}
        
        public int Hp {get; private set;}
        public virtual void Interaction()
        {
            // [TODO] 다른 방법으로 인터렉션 대상을 정리해야지 이렇게 하면 안된다..
            PlayerActor.TargetActor = this;
            
            UIManager.Instance.ShowUI("InteractionHPBoard", CanvasLayer.Normal);
            
            Hp = _dataSO.hp;
            IsInteraction = true;
        }

        public void OnHit(int damage)
        {
            Hp -= damage;
            HP_Init();
        }
        public virtual void HP_Init()
        {
            UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>("InteractionHPBoard");
            if (ui != null)
            {
                ui.BoardFill(Hp, _dataSO.hp);
            }
            
            if (Hp <= 0)
            {
                GameObjectManager.Instance.OnEndInteraction();
                
                Destroy(this.gameObject);
                
                // [TODO] 다른 방법으로 인터렉션 대상을 정리해야지 이렇게 하면 안된다..
                PlayerActor.TargetActor = null;
                return;
            }
        }
    }
}