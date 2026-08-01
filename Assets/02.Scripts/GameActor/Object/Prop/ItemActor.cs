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
        protected override bool RequiresCombatVisuals => false;

        // 아이템마다 비행이 동일해 보이지 않도록 아래 값들은 인스턴스별로 무작위 추출한다.
        [Header("확산(튀어오름)")]
        [SerializeField] private Vector2 _spreadRadiusRange = new Vector2(1.0f, 2.5f);
        [SerializeField] private Vector2 _spreadHeightRange = new Vector2(0.8f, 2.0f);
        [SerializeField] private Vector2 _spreadTimeRange = new Vector2(0.25f, 0.45f);

        [Header("플레이어로 비행")]
        // 아치 최고 높이 범위(포물선 궤적의 봉우리)
        [SerializeField] private Vector2 _arcHeightRange = new Vector2(2.5f, 5.0f);
        // 거리에 상관없이 플레이어에게 도달하는 고정 비행 시간의 범위(초)
        [SerializeField] private Vector2 _flightDurationRange = new Vector2(0.4f, 0.7f);
        // 직선이 아닌 곡선 비행을 위한 좌우 휘어짐 정도
        [SerializeField] private Vector2 _lateralCurveRange = new Vector2(0.5f, 2.0f);
        // 여러 아이템이 동시에 도착하지 않도록 흩어주는 출발 지연 범위(초)
        [SerializeField] private Vector2 _launchDelayRange = new Vector2(0.0f, 0.2f);

        [SerializeField] private GameObject _getParticle;

        private float _playerColliderHeight = 1.0f;
        private Transform _player;

        // 인스턴스별로 확정된 비행 파라미터
        private float _arcHeight;
        private float _flightDuration;
        private float _lateralCurve;

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

            // 아이템마다 비행 궤적이 다르게 보이도록 인스턴스별 파라미터를 확정한다.
            _arcHeight = Random.Range(_arcHeightRange.x, _arcHeightRange.y);
            _flightDuration = Random.Range(_flightDurationRange.x, _flightDurationRange.y);
            // 좌우 휘어짐은 방향까지 무작위(왼쪽/오른쪽)로 준다.
            _lateralCurve = Random.Range(_lateralCurveRange.x, _lateralCurveRange.y)
                            * (Random.value < 0.5f ? -1.0f : 1.0f);

            StartCoroutine(SpreadAndMoveToPlayer());
        }

        public void Init(ItemInstance itemInstance)
        {
            _itemInstance = itemInstance;
        }

        IEnumerator SpreadAndMoveToPlayer()
        {
            // 1. 확산 방향/거리를 무작위로 (수평 방향은 원형, 거리는 범위 내에서)
            Vector2 horizontal = Random.insideUnitCircle.normalized
                                 * Random.Range(_spreadRadiusRange.x, _spreadRadiusRange.y);
            Vector3 spreadPosition = transform.position + new Vector3(horizontal.x, 0.0f, horizontal.y);

            // 2. 튀어오르는 높이도 아이템마다 다르게
            spreadPosition.y = transform.position.y + Random.Range(_spreadHeightRange.x, _spreadHeightRange.y);

            float spreadTime = Random.Range(_spreadTimeRange.x, _spreadTimeRange.y);
            float elapsedTime = 0.0f;
            Vector3 startPosition = transform.position;

            while (elapsedTime < spreadTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / spreadTime;
                // 위로 튀었다가 살짝 떨어지는 느낌을 주기 위해 SmoothStep 사용
                transform.position = Vector3.Lerp(startPosition, spreadPosition, Mathf.SmoothStep(0.0f, 1.0f, t));
                yield return null;
            }

            // 여러 아이템이 동시에 도착하지 않도록 출발을 살짝 늦춘다.
            float launchDelay = Random.Range(_launchDelayRange.x, _launchDelayRange.y);
            if (launchDelay > 0.0f)
            {
                yield return new WaitForSeconds(launchDelay);
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

                // 거리와 무관하게 고정된 시간 안에 도달하도록 한다.
                journeyTime = _flightDuration;
                elapsedTime = 0.0f;

                while (elapsedTime < journeyTime)
                {
                    endPosition = _player.position + new Vector3(0.0f, _playerColliderHeight, 0.0f);

                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / journeyTime;

                    Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, t);

                    // 봉우리가 있는 수직 아치
                    currentPos.y += Mathf.Sin(Mathf.PI * t) * _arcHeight;

                    // 진행 방향 기준 좌우로 휘어지는 수평 곡선(직선 비행 탈피)
                    Vector3 flatDir = endPosition - startPosition;
                    flatDir.y = 0.0f;
                    if (flatDir.sqrMagnitude > 0.0001f)
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, flatDir.normalized);
                        currentPos += side * (Mathf.Sin(Mathf.PI * t) * _lateralCurve);
                    }

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
