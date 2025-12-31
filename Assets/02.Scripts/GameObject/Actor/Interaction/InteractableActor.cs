using System;
using UnityEngine;

namespace Actor
{
    public class InteractableActor : BaseActor
    {
        [SerializeField] private InteractableActorSO _dataSO;
        [SerializeField] private ItemActor _itemActorPrefab;
        public bool IsInteraction { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp => _dataSO.hp;
        
        // 이벤트 선언
        public event Action<InteractableActor> OnInteractionStarted;
        public event Action<InteractableActor, int, int> OnHpChanged; // (actor, currentHp, maxHp)
        public event Action<InteractableActor> OnDestroyed;

        private void OnEnable()
        {
            Hp = _dataSO.hp;
        }

        public virtual void Interaction()
        {
            IsInteraction = true;
            
            OnHit(0);
            
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

            var items = ItemManager.Instance.GetDropItemList(_dataSO.dropItems);
            for (int i = 0; i <items.Count; ++i)
            {
                var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
                go.Init(itemSO: items[i], interactableData: _dataSO);
                
                Debug.Log($"ID: {items[i].itemId}, Name: {items[i].itemName}, Description: {items[i].itemDescription}");
            }
            
            GameObjectManager.Instance.ShowFX("ItemArriedToPlayerPos", transform.position);
            
            // 파괴 이벤트 발생
            OnDestroyed?.Invoke(this);
            
            Destroy(gameObject);
        }
    }
}
