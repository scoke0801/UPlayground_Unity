using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Data.Item;
using Random = UnityEngine.Random;

namespace UPlayGround
{
    public class ItemActor : GameActor
    {
        [SerializeField] private float _arcHeight = 5.0f;
        [SerializeField] private float _moveSpeed = 5.0f;
        
        [SerializeField] private GameObject _getParticle;
        
        private float _playerColliderHeight = 1.0f;
        private Transform _player;

        private ItemInstance _itemInstance;
        
        protected override void Start()
        {
            base.Start();
            
            _player = ActorSvc.Objects.Player.transform;
            Collider playerCollider = _player.gameObject.GetComponent<Collider>();
            if (playerCollider != null)
            {
                _playerColliderHeight = playerCollider.bounds.size.y * 0.5f;
            }
            StartCoroutine(SpreadAndMoveToPlayer());
        }

        public void Init(ItemInstance itemInstance)
        {
            _itemInstance = itemInstance;
        }

        IEnumerator SpreadAndMoveToPlayer()
        {
            // 1. 확산 범위 조절 (너무 크면 멀리 튐)
            Vector3 spreadDirection = Random.insideUnitSphere * 2.0f; 
            Vector3 spreadPosition = transform.position + spreadDirection;

            // 2. Y값 고정 대신 상대적 높이 부여
            spreadPosition.y = transform.position.y + 1.5f; 
    
            float spreadTime = 0.3f;
            float elapsedTime = 0.0f;
            Vector3 startPosition = transform.position;
    
            while (elapsedTime < spreadTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / spreadTime;
        
                // 3. spreadPosition으로 수정!!
                transform.position = Vector3.Lerp(startPosition, spreadPosition, t);
                yield return null;
            }

            StartCoroutine(MoveToPlayer(transform.position)); // 현재 위치에서 시작
        }

        IEnumerator MoveToPlayer(Vector3 startPosition)
        {
            float journeyTime = 0.0f;
            float elapsedTime = 0.0f;
            Vector3 endPosition;

            Vector3 targetPosition = _player.position;
            targetPosition.y += _playerColliderHeight;
            
            while (true)
            {
                endPosition = _player.position + new Vector3(0.0f, _playerColliderHeight, 0.0f);

                journeyTime = Vector3.Distance(startPosition, endPosition) / _moveSpeed;
                elapsedTime = 0.0f;

                while (elapsedTime < journeyTime)
                {
                    endPosition = _player.position + new Vector3(0.0f, _playerColliderHeight, 0.0f);

                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / journeyTime;

                    float height = Mathf.Sin(Mathf.PI * t) * _arcHeight;
                    Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);

                    currentPos.y += height;

                    transform.position = currentPos;

                    yield return null;
                }

                targetPosition = _player.position;
                targetPosition.y += _playerColliderHeight;
                if (Vector3.Distance(transform.position, targetPosition) < 5.0f)
                {
                    break;
                }
            }

            Instantiate(_getParticle, endPosition, Quaternion.identity);

            bool routedToCycleLedger = ActorSvc.CycleRemains?.TryAddUnsettledMaterial(
                _itemInstance.data.itemId,
                _itemInstance.count) == true;
            if (!routedToCycleLedger)
            {
                ActorSvc.UI?.ShowItemAcquisition(_itemInstance.data);
                Svc.Inventory.AddItem(_itemInstance.data.itemId, itemInstance: _itemInstance);
            }

            ActorSvc.UI?.RefreshInventoryIfVisible();
            Destroy(gameObject);
        }
    }
}
