using UnityEngine;
using Steamworks;

/// <summary>
/// Steam Input 완전 통합 테스트 예제
/// GameObject에 추가하면 바로 작동합니다
/// </summary>
public class SteamInputTouchpadTest : MonoBehaviour
{
    [Header("디버그 설정")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float minSwipeDistance = 0.15f;
    
    private InputHandle_t[] controllers;
    private InputActionSetHandle_t gameplaySet;
    private InputAnalogActionHandle_t touchpadPosAction;
    private InputDigitalActionHandle_t touchpadActiveAction;
    
    private bool wasTouching = false;
    private Vector2 touchStartPos;
    private float touchStartTime;
    
    void Start()
    {
        // Steam 초기화 확인
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam이 초기화되지 않았습니다!");
            Debug.LogError("1. Steam 클라이언트가 실행 중인지 확인");
            Debug.LogError("2. steam_appid.txt 파일이 있는지 확인");
            enabled = false;
            return;
        }
        
        Debug.Log("✓ Steam 초기화 완료");
        
        // 액션 핸들 초기화
        try
        {
            gameplaySet = SteamInput.GetActionSetHandle("gameplay");
            touchpadPosAction = SteamInput.GetAnalogActionHandle("TouchpadPosition");
            touchpadActiveAction = SteamInput.GetDigitalActionHandle("TouchpadActive");
            
            Debug.Log("✓ Steam Input 액션 핸들 초기화 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"액션 핸들 초기화 실패: {e.Message}");
            Debug.LogError("actions.vdf 파일을 확인하세요");
            enabled = false;
        }
    }
    
    void Update()
    {
        if (!SteamManager.Initialized) return;
        
        // 컨트롤러 가져오기
        controllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        int count = SteamInput.GetConnectedControllers(controllers);
        
        if (count == 0)
        {
            if (showDebugInfo && Time.frameCount % 300 == 0) // 5초마다
            {
                Debug.LogWarning("컨트롤러가 연결되지 않았습니다");
            }
            //return;
        }
        
        // 첫 번째 컨트롤러 사용
        InputHandle_t controller = controllers[0];
        
        // 컨트롤러 정보 (최초 1회만 출력)
        if (Time.frameCount == 60 && showDebugInfo)
        {
            var controllerType = SteamInput.GetInputTypeForHandle(controller);
            Debug.Log($"✓ 컨트롤러 연결: {controllerType}");
        }
        
        // 액션 셋 활성화
        SteamInput.ActivateActionSet(controller, gameplaySet);
        
        // 터치패드 데이터 읽기
        InputAnalogActionData_t posData = SteamInput.GetAnalogActionData(controller, touchpadPosAction);
        InputDigitalActionData_t activeData = SteamInput.GetDigitalActionData(controller, touchpadActiveAction);
        
        // byte를 bool로 명시적 변환
        bool isTouching = activeData.bState != 0;
        Vector2 currentPos = new Vector2(posData.x, posData.y);
        
        // 터치 상태 디버그 (터치 중일 때만)
        if (showDebugInfo && isTouching)
        {
            Debug.Log($"터치 위치: X={currentPos.x:F3}, Y={currentPos.y:F3}");
        }
        
        // 터치 시작
        if (isTouching && !wasTouching)
        {
            touchStartPos = currentPos;
            touchStartTime = Time.time;
            
            if (showDebugInfo)
                Debug.Log($"<color=cyan>터치 시작: {touchStartPos}</color>");
        }
        // 터치 종료 - 스와이프 판정
        else if (!isTouching && wasTouching)
        {
            float duration = Time.time - touchStartTime;
            Vector2 swipeVector = currentPos - touchStartPos;
            float distance = swipeVector.magnitude;
            
            if (showDebugInfo)
                Debug.Log($"<color=yellow>터치 종료: 거리={distance:F3}, 시간={duration:F2}초</color>");
            
            if (distance >= minSwipeDistance && duration <= 0.5f)
            {
                string direction = GetSwipeDirectionText(swipeVector);
                float speed = distance / duration;
                
                Debug.Log($"<color=lime>✓ 스와이프 감지!</color>");
                Debug.Log($"  방향: {direction}");
                Debug.Log($"  속도: {speed:F2} units/sec");
                Debug.Log($"  거리: {distance:F3}");
            }
        }
        
        wasTouching = isTouching;
    }
    
    private string GetSwipeDirectionText(Vector2 swipe)
    {
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            return swipe.x > 0 ? "→ 오른쪽" : "← 왼쪽";
        else
            return swipe.y > 0 ? "↑ 위" : "↓ 아래";
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Box("Steam Input 터치패드 테스트");
        
        if (!SteamManager.Initialized)
        {
            GUILayout.Label("<color=red>Steam 초기화 안됨</color>");
        }
        else
        {
            GUILayout.Label("<color=lime>Steam 초기화 완료</color>");
            
            if (controllers != null && controllers.Length > 0)
            {
                var type = SteamInput.GetInputTypeForHandle(controllers[0]);
                GUILayout.Label($"컨트롤러: {type}");
                GUILayout.Label($"터치 상태: {(wasTouching ? "터치 중" : "대기")}");
            }
            else
            {
                GUILayout.Label("<color=yellow>컨트롤러를 연결하세요</color>");
            }
        }
        
        GUILayout.Label($"\n최소 스와이프 거리: {minSwipeDistance:F2}");
        GUILayout.Label("터치패드를 스와이프 해보세요!");
        
        GUILayout.EndArea();
    }
}