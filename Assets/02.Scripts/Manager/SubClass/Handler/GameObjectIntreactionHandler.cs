using System.Collections.Generic;
using Actor;
using FX;
using UnityEngine;

public class GameObjectInteractionHandler
{
    private InteractionConfig _config;
    private bool _isInitialized = false;

    private UI_Base _activeIcon;
    private RectTransform _activeIconRect;
    
    private Camera _camera;

    private Transform _closestObject;
    private InteractableActor _currentInteractingActor;
    
    public GameObjectInteractionHandler()
    {
        Init();
    }

    private async void Init()
    {
        _config = await GameManager.LoadAddressableAsync<InteractionConfig>("InteractionConfig");
        _isInitialized = true;
        
        _camera = Camera.main;
        _closestObject = null;
        
        GameObjectManager.Instance.OnInteractionOn += OnInteractionOn;
        GameObjectManager.Instance.OnInteractionOut += OnInteractionOut;
    }

    public void OnUpdate()
    {
        if (_isInitialized == false)
        {
            return;
        }

        if (GameObjectManager.Instance.IsPlayerInteracting())
        {
            return;
        }

        Vector3 playerPosition = GameObjectManager.Instance.Player.transform.position;
        Collider[] nearbyObjects =
            Physics.OverlapSphere(playerPosition, _config.checkRadius, _config.interactableLayer);

        _closestObject = null;
        float closestDistance = float.MaxValue;

        foreach (Collider obj in nearbyObjects)
        {
            Transform targetTransform = obj.transform;

            float distance = Vector3.Distance(playerPosition, targetTransform.position);

            if (distance <= _config.activationDistance && distance < closestDistance)
            {
                _closestObject = targetTransform;
                closestDistance = distance;
                Debug.Log("Find InteractionObject");
                ShowIcon(targetTransform);
            }
        }

        if (_closestObject != null)
        {
            ShowIcon(_closestObject);
        }
        else
        {
            RemoveIcon();
        }
    }

    public bool IsInteractionTargetExist()
    {
        return _closestObject != null;
    }
    
    public InteractableActor GetCurrentTarget()
    {
        return _currentInteractingActor;
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

        GameObject iconObject = UIManager.Instance.ShowUI("InteractionKeyUI", CanvasLayer.Normal);
        _activeIcon = iconObject.GetComponentInChildren<UI_Base>();
        _activeIconRect = iconObject.GetComponent<RectTransform>();
        
        UpdateIconPosition(targetTransform);
    }

    private void UpdateIconPosition(Transform targetTransform)
    {
        if (_activeIcon == null || _activeIcon.IsVisible == false)
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

    private void OnInteractionOn()
    {
        InteractableActor actor = _closestObject.GetComponent<InteractableActor>();
        if (actor != null)
        {
            _currentInteractingActor = actor;
            
            // 이벤트 구독
            actor.OnInteractionStarted += HandleInteractionStarted;
            actor.OnHpChanged += HandleHpChanged;
            actor.OnDestroyed += HandleActorDestroyed;
            
            actor.Interaction();
        }
    }

    private void OnInteractionOut()
    {
        UnsubscribeFromCurrentActor();
        _closestObject = null;
        _currentInteractingActor = null;
    }
    
    private void HandleInteractionStarted(InteractableActor actor)
    {
        // UI 표시
        UIManager.Instance.ShowUI("InteractionHPBoard", CanvasLayer.Normal);
        
        // 초기 HP 업데이트
        UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>("InteractionHPBoard");
        if (ui != null)
        {
            ui.BoardFill(actor.Hp, actor.MaxHp);
        }
    }
    
    private void HandleHpChanged(InteractableActor actor, int currentHp, int maxHp)
    {
        // HP UI 업데이트
        UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>("InteractionHPBoard");
        if (ui != null)
        {
            ui.Show();
            ui.BoardFill(currentHp, maxHp);
        }
    }
    
    private void HandleActorDestroyed(InteractableActor actor)
    {
        // 플레이어 상태를 기본 상태로 전환
        if (GameObjectManager.Instance.PlayerBrain != null)
        {
            GameObjectManager.Instance.PlayerBrain.ChangeState(
                GameObjectManager.Instance.PlayerBrain.DefaultState);
        }

        GameObject fx = GameObjectManager.Instance.ShowFX("ObjectDestroyFX", actor.transform.position);
        if (fx != null)
        {
            ActorDestroyParticle destroyParticle = fx.GetComponent<ActorDestroyParticle>();
            if (destroyParticle != null)
            {
                destroyParticle.OnParticle(actor.transform.GetChild(0).GetComponent<MeshRenderer>());
            }
        }
        // 인터랙션 종료 처리
        GameObjectManager.Instance.OnEndInteraction();
        
        UnsubscribeFromCurrentActor();
        _currentInteractingActor = null;
    }
    
    private void UnsubscribeFromCurrentActor()
    {
        if (_currentInteractingActor != null)
        {
            _currentInteractingActor.OnInteractionStarted -= HandleInteractionStarted;
            _currentInteractingActor.OnHpChanged -= HandleHpChanged;
            _currentInteractingActor.OnDestroyed -= HandleActorDestroyed;
        }
    }
}
