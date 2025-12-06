
using System.Collections.Generic;
using UnityEngine;

public class GameObjectInteractionHandler
{
    private InteractionConfig _config;
    private bool _isInitialized = false;

    private UI_Base _activeIcon;
    private RectTransform _activeIconRect;
    
    private Camera _camera;
    public GameObjectInteractionHandler()
    {
        Init();
    }

    private async void Init()
    {
        _config = await GameManager.LoadAddressableAsync<InteractionConfig>("InteractionConfig");
        _isInitialized = true;
        
        _camera = Camera.main;
    }

    public void OnUpdate()
    {
        if (_isInitialized == false)
        {
            return;
        }

        Vector3 playerPosition = GameObjectManager.Instance.Player.transform.position;
        Collider[] nearbyObjects =
            Physics.OverlapSphere(playerPosition, _config.checkRadius, _config.interactableLayer);
        HashSet<Transform> currentObjects = new HashSet<Transform>();

        Transform closestObject = null;
        float closestDistance = float.MaxValue;

        foreach (Collider obj in nearbyObjects)
        {
            Transform targetTransform = obj.transform;

            float distance = Vector3.Distance(playerPosition, targetTransform.position);

            if (distance <= _config.activationDistance && distance < closestDistance)
            {
                closestObject = targetTransform;
                closestDistance = distance;
                Debug.Log("Find InteractionObject");
                ShowIcon(targetTransform);
                currentObjects.Add(targetTransform);
            }
        }

        if (closestObject != null)
        {
            ShowIcon(closestObject);
        }
        else if(_activeIcon != null && _activeIcon.IsVisible)
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
                _activeIcon.AnimationChange("On");
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
}