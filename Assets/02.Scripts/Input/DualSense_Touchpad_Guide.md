# DualSense 터치패드 구현 가이드

## 📋 개요

Unity Input System을 확장하여 DualSense 컨트롤러의 터치패드 입력을 지원합니다.

## 📁 파일 구조

```
Assets/02.Scripts/Input/
├── DualSenseTouchpadReport.cs      # HID 리포트 구조체
├── DualSenseWithTouchpad.cs        # 커스텀 디바이스 클래스
└── TouchpadGestureDetector.cs      # 제스처 감지 예제
```

## 🚀 빠른 시작

### 1. 자동 등록

프로젝트에 파일을 추가하면 자동으로 DualSense 터치패드 레이아웃이 등록됩니다.

- **에디터**: `[InitializeOnLoad]` 속성으로 자동 등록
- **플레이 모드**: `[RuntimeInitializeOnLoadMethod]`로 자동 등록

### 2. 제스처 감지기 추가

1. 빈 GameObject 생성
2. `TouchpadGestureDetector` 컴포넌트 추가
3. Inspector에서 설정 조정:
   - **Swipe Threshold**: 스와이프로 인식하는 최소 거리 (기본: 0.15)
   - **Tap Max Duration**: 탭으로 인식하는 최대 시간 (기본: 0.3초)
   - **Show Debug Log**: 디버그 로그 표시 여부

### 3. DualSense 컨트롤러 연결

- USB 또는 Bluetooth로 DualSense 연결
- Unity에서 자동으로 감지됨
- 콘솔에 `[DualSense] 터치패드 지원 레이아웃 등록 완료` 메시지 확인

## 💻 코드 사용 예제

### 기본 사용

```csharp
using UnityEngine;
using Input;

public class MyController : MonoBehaviour
{
    private DualSenseWithTouchpad dualSense;
    
    void Start()
    {
        dualSense = DualSenseWithTouchpad.Current;
        
        if (dualSense != null)
        {
            Debug.Log("DualSense 연결됨!");
        }
    }
    
    void Update()
    {
        if (dualSense == null) return;
        
        // 터치 1 확인
        if (dualSense.touch1Active.isPressed)
        {
            Vector2 pos = dualSense.touchPosition1.ReadValue();
            Debug.Log($"터치 위치: {pos}");
        }
    }
}
```

### 스와이프 제스처

```csharp
private Vector2 touchStartPos;
private bool isTouching;

void Update()
{
    // 터치 시작
    if (dualSense.touch1Active.wasPressedThisFrame)
    {
        touchStartPos = dualSense.touchPosition1.ReadValue();
        isTouching = true;
    }
    
    // 터치 종료 - 스와이프 감지
    if (dualSense.touch1Active.wasReleasedThisFrame && isTouching)
    {
        Vector2 touchEndPos = dualSense.touchPosition1.ReadValue();
        Vector2 swipe = touchEndPos - touchStartPos;
        
        if (swipe.magnitude > 0.2f)
        {
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            {
                if (swipe.x > 0) OnSwipeRight();
                else OnSwipeLeft();
            }
            else
            {
                if (swipe.y > 0) OnSwipeUp();
                else OnSwipeDown();
            }
        }
        
        isTouching = false;
    }
}
```

### 멀티터치 (핀치)

```csharp
void Update()
{
    bool touch1 = dualSense.touch1Active.isPressed;
    bool touch2 = dualSense.touch2Active.isPressed;
    
    if (touch1 && touch2)
    {
        Vector2 pos1 = dualSense.touchPosition1.ReadValue();
        Vector2 pos2 = dualSense.touchPosition2.ReadValue();
        
        float distance = Vector2.Distance(pos1, pos2);
        Debug.Log($"두 손가락 거리: {distance}");
    }
}
```

## 🎮 지원 제스처

TouchpadGestureDetector가 기본 제공하는 제스처:

1. **탭 (Tap)**: 짧게 터치하고 떼기
2. **스와이프 (Swipe)**: 상하좌우로 밀기
3. **핀치 아웃 (Pinch Out)**: 두 손가락으로 벌리기 (확대)
4. **핀치 인 (Pinch In)**: 두 손가락으로 좁히기 (축소)

## 📊 터치패드 사양

- **해상도**: 1920 x 1080
- **정규화 범위**: 0.0 ~ 1.0 (Vector2)
- **동시 터치**: 최대 2개

## 🔧 커스터마이징

### 제스처 임계값 조정

```csharp
// TouchpadGestureDetector.cs에서
[SerializeField] private float swipeThreshold = 0.15f;  // 스와이프 감도
[SerializeField] private float tapMaxDuration = 0.3f;   // 탭 인식 시간
```

### 커스텀 제스처 추가

`TouchpadGestureDetector.cs`의 다음 메서드를 수정:

- `OnTap()`: 탭 제스처 처리
- `OnSwipe()`: 스와이프 제스처 처리
- `OnPinchOut()`: 핀치 아웃 처리
- `OnPinchIn()`: 핀치 인 처리

## 🐛 문제 해결

### DualSense가 인식되지 않음

1. **USB/Bluetooth 연결 확인**
2. **Input System 패키지 버전 확인**: 1.2.0 이상
3. **콘솔 로그 확인**: 등록 메시지 확인
4. **Windows에서 DS4Windows 종료**: 충돌 방지

### 터치패드가 반응하지 않음

1. **DualSenseWithTouchpad.Current가 null인지 확인**
2. **터치패드 버튼 눌림 확인**: 터치와 클릭은 다름
3. **디버그 로그 활성화**: `showDebugLog = true`

### 좌표가 이상함

- 터치패드는 0-1 범위로 정규화됨
- 원시 좌표: X(0-1920), Y(0-1080)
- 정규화 좌표: X(0-1), Y(0-1)

## 📚 참고 자료

- [Unity Input System 문서](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
- [HID 커스텀 레이아웃](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/HID.html)
- DualSense HID 리포트 구조: `DualSenseTouchpadReport.cs` 주석 참고

## 💡 활용 예제

### 1. UI 스크롤
```csharp
// 터치패드로 UI ScrollRect 스크롤
void Update()
{
    if (dualSense.touch1Active.isPressed)
    {
        Vector2 delta = dualSense.touchPosition1.ReadValue() - previousPos;
        scrollRect.verticalNormalizedPosition += delta.y * scrollSpeed;
        previousPos = dualSense.touchPosition1.ReadValue();
    }
}
```

### 2. 카메라 회전
```csharp
// 터치패드로 카메라 회전
void Update()
{
    if (dualSense.touch1Active.isPressed)
    {
        Vector2 pos = dualSense.touchPosition1.ReadValue();
        float rotationX = pos.x * 360f;
        float rotationY = pos.y * 180f - 90f;
        
        camera.transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
    }
}
```

### 3. 무기 선택
```csharp
// 스와이프로 무기 전환
private void OnSwipe(Vector2 swipe)
{
    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
    {
        if (swipe.x > 0) NextWeapon();
        else PreviousWeapon();
    }
}
```

## ✅ 체크리스트

- [ ] Unity Input System 패키지 설치됨
- [ ] DualSense 컨트롤러 연결됨
- [ ] 세 개의 스크립트 파일이 Input 폴더에 있음
- [ ] 콘솔에 레이아웃 등록 메시지 확인
- [ ] TouchpadGestureDetector 컴포넌트 추가됨
- [ ] 디버그 로그로 터치 입력 확인됨

---

**버전**: 1.0  
**최종 수정**: 2024년 12월 12일  
**호환성**: Unity 2021.3 이상, Input System 1.2.0 이상
