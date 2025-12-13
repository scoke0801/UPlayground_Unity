using UnityEngine;
using Steamworks;
using UnityEngine.InputSystem;

public class SteamInputSwipeHandler : MonoBehaviour
{
    [Header("Steam Input Actions")]
    // Steam Input 대시보드에서 정의한 Action 이름과 일치해야 합니다.
    public string actionNameUp = "SwipeUp"; 
    public string actionNameDown = "SwipeDown";
    public string actionNameLeft = "SwipeLeft";
    public string actionNameRight = "SwipeRight";

    // Action 핸들 캐싱
    private InputDigitalActionData_t _digitalData;
    private InputHandle_t _inputHandle;
    private InputActionSetHandle_t _actionSetHandle;
    
    private InputDigitalActionHandle_t _handleUp;
    private InputDigitalActionHandle_t _handleDown;
    private InputDigitalActionHandle_t _handleLeft;
    private InputDigitalActionHandle_t _handleRight;

    private bool _steamInitialized = false;

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamManager가 초기화되지 않았습니다. Steam 클라이언트를 켜주세요.");
            return;
        }

        _steamInitialized = true;
        // Spacewar의 정의된 이름을 사용해야 핸들을 받아옵니다.
        _actionSetHandle = SteamInput.GetActionSetHandle("DARK SOULS PS5 CONTROL"); 

// 터치패드 상하좌우를 테스트하기 위해 Spacewar의 기존 액션에 임시로 매핑
        _handleUp = SteamInput.GetDigitalActionHandle("MenuUp");
        _handleDown = SteamInput.GetDigitalActionHandle("MenuDown");
        _handleLeft = SteamInput.GetDigitalActionHandle("MenuLeft");
        _handleRight = SteamInput.GetDigitalActionHandle("MenuRight");
        
    }

    void Update()
    {
        if (!_steamInitialized) return;

        // SteamInput 프레임 업데이트
        SteamInput.RunFrame();

        // 연결된 컨트롤러들의 입력을 확인
        int controllerCount = SteamInput.GetConnectedControllers(new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT]);
        
        if (controllerCount > 0)
        {
            // 첫 번째 컨트롤러 가져오기 (실제 구현에선 루프 필요 가능)
            InputHandle_t[] handles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
            SteamInput.GetConnectedControllers(handles);
            _inputHandle = handles[0];

            // Action Set 활성화
            SteamInput.ActivateActionSet(_inputHandle, _actionSetHandle);

            // 입력 확인
            CheckSwipe(_handleUp, "Up");
            CheckSwipe(_handleDown, "Down");
            CheckSwipe(_handleLeft, "Left");
            CheckSwipe(_handleRight, "Right");
        }
    }

    private void CheckSwipe(InputDigitalActionHandle_t actionHandle, string dirName)
    {
        InputDigitalActionData_t data = SteamInput.GetDigitalActionData(_inputHandle, actionHandle);
        
        // bState가 true이면 해당 방향으로 스와이프(터치) 중인 것임
        if (data.bState != 0 && data.bActive != 0)
        {
            // 한 번만 실행되게 하려면 이전 프레임 상태 비교 로직 추가 필요
            // 여기서는 심플하게 로그만 출력
            Debug.Log($"Touchpad Swipe Detected: {dirName}");
            
            // TODO: 여기서 실제 게임 로직 연결
        }
    }
}