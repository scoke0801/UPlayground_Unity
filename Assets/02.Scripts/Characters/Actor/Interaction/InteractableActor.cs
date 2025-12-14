using System;
using UnityEngine;

namespace Actor
{
    public class InteractableActor : BaseActor
    {
        [SerializeField] protected InteractableActorSO _dataSO;
        
        public bool IsInteraction { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp => _dataSO.hp;
        
        // 이벤트 선언
        public event Action<InteractableActor> OnInteractionStarted;
        public event Action<InteractableActor, int, int> OnHpChanged; // (actor, currentHp, maxHp)
        public event Action<InteractableActor> OnDestroyed;
        
        public virtual void Interaction()
        {
            Hp = _dataSO.hp;
            IsInteraction = true;
            
            // UI 관리는 외부에서 이벤트를 구독하여 처리
            OnInteractionStarted?.Invoke(this);
        }

        public virtual void OnHit(int damage)
        {
            Hp -= damage;
            Hp = Mathf.Max(0, Hp);
            
            // HP 변경 이벤트 발생
            OnHpChanged?.Invoke(this, Hp, MaxHp);
            
            if (Hp <= 0)
            {
                HandleDestroy();
            }
        }
        
        private void HandleDestroy()
        {
            IsInteraction = false;
            
            // 파괴 이벤트 발생
            OnDestroyed?.Invoke(this);
            
            Destroy(gameObject);
        }
    }
}
