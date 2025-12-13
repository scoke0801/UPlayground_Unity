
using System.Collections.Generic;
using Actor;
using UnityEngine;

public class GameObjectInteractionHandler
{
    private InteractionConfig _config;
    private bool _isInitialized = false;

    private UI_Base _activeIcon;
    private RectTransform _activeIconRect;
    
    private Camera _camera;

    private Transform _closestObject;
    
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
        HashSet<Transform> currentObjects = new HashSet<Transform>();

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
                currentObjects.Add(targetTransform);
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
            actor.Interaction();
        }
    }

    private void OnInteractionOut()
    {
        _closestObject = null;
    }
}