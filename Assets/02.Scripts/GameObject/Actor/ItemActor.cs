using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Actor
{
    public class ItemActor : BaseActor
    {
        [SerializeField] private float _spreadRadius = 10.0f;
        [SerializeField] private float _arcHeight = 5.0f;
        [SerializeField] private float _moveSpeed = 5.0f;
        
        [SerializeField] private GameObject _getParticle;
        
        private float _playerColliderHeight = 1.0f;
        private Transform _player;

        private ItemInstance _itemInstance;
        
        private void Start()
        {
            _player = GameObjectManager.Instance.PlayerBrain.transform;
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
            Vector3 spreadDirection = Random.insideUnitSphere * _spreadRadius;
            Vector3 spreadPosition = transform.position + spreadDirection;

            spreadPosition.y = Mathf.Max(spreadPosition.y, 5.0f);
            
            float spreadTime = 0.3f;
            float elapsedTime = 0.0f;

            Vector3 startPosition = transform.position;
            
            while (elapsedTime < spreadTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / spreadTime;
                
                transform.position = Vector3.Lerp(startPosition, spreadDirection, t);

                yield return null;
            }

            StartCoroutine(MoveToPlayer(spreadPosition));
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
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / journeyTime;

                    float height = Mathf.Sin(Mathf.PI * t) * _arcHeight;
                    Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);

                    currentPos.y += height;

                    transform.position = currentPos;

                    // endPosition = _player.position;
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

            var ui = UIManager.Instance.ShowUI("ItemAcquisitionList");
            if (ui != null)
            {
                ui.GetComponent<UI_ItemAcquisitionList>().SetItem(_itemInstance.data);
            }
            
            InventoryManager.Instance.AddItem(_itemInstance.data.itemId, itemInstance: _itemInstance);
            Destroy(gameObject);
        }
    }
}