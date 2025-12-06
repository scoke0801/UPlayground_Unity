
using System.Collections.Generic;
using Animancer;
using UnityEngine;

public class GameObjectInteractionHandler
{
    private InteractionConfig _config;
    private bool _isInitialized = false;
    private GameObject _iconPrefab; 
    
    private Dictionary<Transform, GameObject> activeIcons = new Dictionary<Transform, GameObject>();
    
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
        Collider[] nearbyObjects = Physics.OverlapSphere(playerPosition, _config.checkRadius, _config.interactableLayer);
        HashSet<Transform> currentObjects = new HashSet<Transform>();

        foreach (Collider obj in nearbyObjects)
        {
            Transform targetTransform = obj.transform;

            float distance = Vector3.Distance(playerPosition, targetTransform.position);

            if (distance <= _config.activationDistance)
            {
                Debug.Log("Find InteractionObject");
                ShowIcon(targetTransform);
                currentObjects.Add(targetTransform);
            }
        }
        
        List<Transform> toRemove = new List<Transform>();
        foreach (var iconEntry in activeIcons)
        {
            if (!currentObjects.Contains(iconEntry.Key))
            {
                iconEntry.Value.GetComponentInChildren<UI_InteractionKey>().AnimationChange("Out");
                GameObject.Destroy(iconEntry.Value);
                toRemove.Add(iconEntry.Key);
            }
        }

        foreach (Transform transformToRemove in toRemove)
        {
            activeIcons.Remove(transformToRemove);
        }
        
    }

    private void ShowIcon(Transform targetTransform)
    {
        if (activeIcons.ContainsKey(targetTransform))
        {
            UpdateIconPosition(targetTransform, activeIcons[targetTransform]);
            return;
        }

        GameObject icon = UIManager.Instance.ShowUI("InteractionKeyUI", CanvasLayer.Normal);
        activeIcons[targetTransform] = icon;
        
        UpdateIconPosition(targetTransform, icon);
    }

    private void UpdateIconPosition(Transform targetTransform, GameObject icon)
    {
        Vector3 screenPositon = _camera.WorldToScreenPoint(new Vector3(
            targetTransform.position.x,
            targetTransform.position.y + 1.5f,
            targetTransform.position.z));
        var rt = icon.GetComponent<RectTransform>();
        rt.position = screenPositon;
        rt.localScale = Vector3.one;
    }
}