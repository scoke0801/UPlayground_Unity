using UnityEngine;
using Steamworks;

/// <summary>
/// Steam Input API 사용을 간편하게 해주는 헬퍼 클래스
/// </summary>
public static class SteamInputHelper
{
    /// <summary>
    /// InputDigitalActionData_t의 bState를 bool로 변환
    /// </summary>
    public static bool IsActive(this InputDigitalActionData_t data)
    {
        return data.bState != 0;
    }
    
    /// <summary>
    /// InputAnalogActionData_t를 Vector2로 변환
    /// </summary>
    public static Vector2 ToVector2(this InputAnalogActionData_t data)
    {
        return new Vector2(data.x, data.y);
    }
    
    /// <summary>
    /// 연결된 모든 컨트롤러 가져오기
    /// </summary>
    public static InputHandle_t[] GetAllControllers()
    {
        if (!SteamManager.Initialized) return new InputHandle_t[0];
        
        InputHandle_t[] controllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        int count = SteamInput.GetConnectedControllers(controllers);
        
        if (count == 0) return new InputHandle_t[0];
        
        // 실제 연결된 개수만큼 배열 생성
        InputHandle_t[] result = new InputHandle_t[count];
        System.Array.Copy(controllers, result, count);
        return result;
    }
    
    /// <summary>
    /// 첫 번째 컨트롤러 가져오기
    /// </summary>
    public static InputHandle_t GetFirstController()
    {
        var controllers = GetAllControllers();
        return controllers.Length > 0 ? controllers[0] : default;
    }
    
    /// <summary>
    /// 컨트롤러 타입 이름 가져오기
    /// </summary>
    public static string GetControllerTypeName(InputHandle_t controller)
    {
        if (!SteamManager.Initialized) return "Unknown";
        
        var inputType = SteamInput.GetInputTypeForHandle(controller);
        return inputType.ToString();
    }
    
    /// <summary>
    /// 디지털 액션이 눌렸는지 확인
    /// </summary>
    public static bool GetDigitalAction(InputHandle_t controller, InputDigitalActionHandle_t action)
    {
        if (!SteamManager.Initialized) return false;
        
        var data = SteamInput.GetDigitalActionData(controller, action);
        return data.IsActive();
    }
    
    /// <summary>
    /// 아날로그 액션 값 가져오기
    /// </summary>
    public static Vector2 GetAnalogAction(InputHandle_t controller, InputAnalogActionHandle_t action)
    {
        if (!SteamManager.Initialized) return Vector2.zero;
        
        var data = SteamInput.GetAnalogActionData(controller, action);
        return data.ToVector2();
    }
}