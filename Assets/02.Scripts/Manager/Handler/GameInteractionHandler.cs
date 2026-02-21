using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UPlayGround.Manager.Handler
{
    public class GameInteractionHandler : GameHandlerBase
    {
        private IInteractable _currentClosestInteractable;
        
        private PlayerActor _player;
        
        private UI_Base _activeIcon;
        private RectTransform _activeIconRect;
        
        private InteractionConfig _config;
        private Camera _camera;
        
        private Coroutine _waitEventCoroutine;
        
        public IInteractable CurrentClosestInteractable => _currentClosestInteractable;
        
        public override void Init()
        {
            _camera = Camera.main;
            _currentClosestInteractable = null;
        }

        public override void AfterInit()
        {
        }

        public override void Dispose()
        {
            
        }

        public override void Update()
        {
            float delta = Time.deltaTime;
            if (_player == null)
            {
                _player = GameObjectManager.Instance.Player;
            }
            
            if (_player == null || _player.IsInCombat == true)
            {
                RemoveIcon();
                return;
            }
            
            // Player 주변에 인터렉션 가능한 대상 조회
            FindClosestInteractable(_player.transform.position);
           
            if (_currentClosestInteractable != null && _currentClosestInteractable.IsInteracting() == false)
            {
                ShowIcon(_currentClosestInteractable.GetActor().transform);
            }
            else
            {
                RemoveIcon();
            }
        }

        public void StartInteraction()
        {
            if (_currentClosestInteractable == null)
            {
                return;
            }

            _currentClosestInteractable.Interact(_player);
        }

        public void StopInteraction()
        {
            if (_waitEventCoroutine != null)
            {
                GameObjectManager.Instance.StopCoroutine(_waitEventCoroutine);
            }
            
            if (_currentClosestInteractable == null)
            {
                return;
            }
            
            _currentClosestInteractable.StopInteract();
            
            UIManager.Instance.HideUI("InteractionHPBoard");
        }
        
        private void FindClosestInteractable(Vector3 playerPosition)
        {
            if (_player == null)
            {
                return;
            }

            if (_currentClosestInteractable != null
                && _currentClosestInteractable.IsInteracting() == true)
            {
                return;
            }
            
            // Player 주변의 콜라이더들 조회
            Collider[] colliders = Physics.OverlapSphere(playerPosition, 
                _player.InteractionRadius, _player.InteractionLayer);
            
            IInteractable bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;

            foreach (var collider in colliders)
            {
                // IInteractable 인터페이스를 가지고 있는지 확인
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable == null || !interactable.CanInteract()) continue;

                // 가장 가까운 대상 지정 (성능을 위해 sqrMagnitude 사용)
                Vector3 directionToTarget = collider.transform.position - playerPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = interactable;
                }
            }

            // 현재 타겟 업데이트
            _currentClosestInteractable = bestTarget;
        }
        private void RemoveIcon()
        {
            if(_activeIcon != null && _activeIcon.IsVisible)
            {
                _activeIcon.AnimationChange("Out");
            }
        }

        private void ShowIcon(Transform targetTransform)
        {
            if (_activeIcon != null)
            {
                UpdateIconPosition(targetTransform);
                if (_activeIcon.IsVisible == false)
                {
                    _activeIcon.Show();
                }
                return;
            }

            GameObject iconObject = UIManager.Instance.ShowUI("InteractionKeyUI");
            if (iconObject != null)
            {
                _activeIcon = iconObject.GetComponentInChildren<UI_Base>();
                _activeIconRect = iconObject.GetComponentInChildren<RectTransform>();

                UpdateIconPosition(targetTransform);
            }
        }
        private void UpdateIconPosition(Transform targetTransform)
        {
            if (_activeIcon == null || _activeIcon.IsVisible == false)
            {
                return;
            }

            if (_camera == null)
            {
                return;
            }
            Vector3 screenPositon = _camera.WorldToScreenPoint(new Vector3(
                targetTransform.position.x,
                targetTransform.position.y + 1.5f,
                targetTransform.position.z));
        
            _activeIconRect.position = screenPositon;
            _activeIconRect.localScale = Vector3.one;
        }

        public void SetWaitEvent(Action callback)
        {
            _waitEventCoroutine = GameObjectManager.Instance.StartCoroutine(ExecuteAfterRandomTime(callback));
        }
        
        private IEnumerator ExecuteAfterRandomTime(Action action)
        {
            // 1. 랜덤한 시간 계산
            float waitTime = Random.Range(3, 7);
            Debug.Log($"{waitTime}초 뒤에 이벤트가 발생합니다.");

            // 2. 설정된 시간만큼 대기
            yield return new WaitForSeconds(waitTime);

            // 3. 이벤트 실행
            action?.Invoke();
            Debug.Log("이벤트가 발생했습니다!");
        }
    }
}