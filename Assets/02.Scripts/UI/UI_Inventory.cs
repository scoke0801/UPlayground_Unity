using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class UI_Inventory : UI_Base
{
    [Header("UI 버튼")]
    private InputManager inputManager;
    
    private void Awake()
    {
        inputManager = InputManager.Instance;
    }
    
    private void OnEnable()
    {
        
    }
    
    private void OnDisable()
    {
    }
    
}
