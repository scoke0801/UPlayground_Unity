using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UIManager 확장 메서드
/// UI_Base와 연동하여 더 편리하게 사용
/// </summary>
public static class UIManagerExtensions
{
    /// <summary>
    /// UI_Base를 상속받은 UI 표시 (제네릭 버전)
    /// </summary>
    public static T ShowUI<T>(this UIManager manager, GameObject uiPrefab, CanvasLayer layer, string uiName = null) where T : UI_Base
    {
        GameObject uiInstance = manager.ShowUI(uiPrefab, layer, uiName);
        
        if (uiInstance == null)
            return null;

        T uiComponent = uiInstance.GetComponent<T>();
        
        if (uiComponent != null)
        {
            uiComponent.Initialize();
            uiComponent.Show();
        }
        else
        {
            Debug.LogError($"[UIManager] UI 프리팹에 {typeof(T)} 컴포넌트가 없습니다.");
        }

        return uiComponent;
    }

    /// <summary>
    /// UI_Base를 상속받은 UI 가져오기
    /// </summary>
    public static T GetUI<T>(this UIManager manager, string uiName) where T : UI_Base
    {
        GameObject uiObj = manager.GetActiveUI(uiName);
        
        if (uiObj != null)
        {
            return uiObj.GetComponent<T>();
        }

        return null;
    }
}
