# DualSense 연결 문제 해결 가이드

## 문제: FindDualSense()에서 항상 null 반환

DualSense가 연결되어 있는데도 Unity에서 인식하지 못하는 경우 아래 단계를 따라 문제를 해결하세요.

---

## 🔍 1단계: 디바이스 확인

### 방법 1: InputDeviceDebugWindow 사용

1. **씬에 빈 GameObject 생성**
   - Hierarchy 우클릭 → Create Empty
   - 이름: "InputDebugger"

2. **컴포넌트 추가**
   - `InputDeviceDebugWindow` 스크립트 추가
   - Play 모드 실행

3. **F12 키로 디버그 창 열기**
   - 우측에 디바이스 정보 창이 표시됨
   - 연결된 모든 입력 디바이스 확인

4. **확인 사항**
   ```
   ✓ "총 X개 디바이스 연결" 메시지 확인
   ✓ DualSense 관련 디바이스 이름 확인
   ✓ 디바이스 타입 확인 (DualSenseGamepadHID인지)
   ```

### 방법 2: Unity Input Debugger 사용

1. **Input Debugger 열기**
   - 메뉴: `Window > Analysis > Input Debugger`

2. **Devices 탭 확인**
   - 좌측에 연결된 디바이스 목록 확인
   - "Wireless Controller" 또는 "DualSense" 찾기

3. **디바이스 클릭하여 상세 정보 확인**
   - Layout 정보 확인
   - Product ID, Vendor ID 확인

---

## 🔧 2단계: 일반적인 해결 방법

### 해결책 1: 다른 프로그램 종료

DualSense를 가로채는 프로그램들을 종료하세요:

```
❌ 종료해야 할 프로그램:
   - DS4Windows
   - Steam (Big Picture Mode)
   - Epic Games Launcher
   - Origin
   - Playnite
   - Parsec
```

**방법:**
1. 작업 관리자 실행 (Ctrl + Shift + Esc)
2. 위 프로그램들 종료
3. Unity 재시작

### 해결책 2: USB 직접 연결

Bluetooth 연결 시 문제가 있을 수 있습니다.

1. **USB 케이블로 직접 연결**
2. **Windows에서 인식 확인**
   - 설정 > 장치 > Bluetooth 및 기타 디바이스
   - "Wireless Controller" 표시 확인

### 해결책 3: Input System 패키지 업데이트

1. **Package Manager 열기**
   - 메뉴: `Window > Package Manager`

2. **Input System 패키지 선택**
   - 좌측 목록에서 "Input System" 찾기

3. **버전 확인 및 업데이트**
   - 권장 버전: 1.4.0 이상
   - "Update" 버튼 클릭 (있는 경우)

### 해결책 4: Unity 재시작

1. Unity Editor 완전히 종료
2. 컨트롤러 재연결
3. Unity 재시작

---

## 🧪 3단계: 테스트 및 확인

### 1. DualSenseTouchpadTest로 테스트

```csharp
// 씬에 GameObject 추가
GameObject testObj = new GameObject("DualSenseTest");
testObj.AddComponent<DualSenseTouchpadTest>();
```

Play 모드에서 확인:
- **좌측 상단 UI**: 연결 상태 표시
- **Console 로그**: 검색 과정 상세 출력
- **F1 키**: 수동 재검색
- **F2 키**: 디바이스 목록 출력
- **F3 키**: 상세 정보 출력

### 2. 수동 테스트 코드

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class ManualTest : MonoBehaviour
{
    void Start()
    {
        // 방법 1
        var ds1 = DualSenseGamepadHID.current;
        Debug.Log($"방법 1: {(ds1 != null ? ds1.name : "null")}");
        
        // 방법 2
        var ds2 = Gamepad.current;
        Debug.Log($"방법 2: {(ds2 != null ? ds2.name : "null")}");
        Debug.Log($"방법 2 타입: {(ds2 != null ? ds2.GetType().Name : "null")}");
        
        // 방법 3
        Debug.Log($"총 디바이스: {InputSystem.devices.Count}개");
        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"  - {device.name} ({device.GetType().Name})");
        }
    }
}
```

---

## 💡 4단계: 타입별 대응

Unity가 DualSense를 다른 타입으로 인식할 수 있습니다.

### 케이스 1: DualSenseGamepadHID로 인식 ✅

```
✓ 가장 이상적인 상황
✓ 모든 기능 사용 가능
✓ 터치패드 버튼 접근 가능
```

### 케이스 2: DualShockGamepad로 인식 ⚠️

```
⚠️ DualSense를 DualShock으로 인식
⚠️ 일부 기능 제한
✓ 기본 게임패드 기능은 사용 가능
```

**대응 코드:**
```csharp
var gamepad = Gamepad.current;
if (gamepad != null)
{
    // 일반 Gamepad로 사용
    if (gamepad is DualSenseGamepadHID ds)
    {
        // DualSense 전용 기능
        bool touchpadPressed = ds.touchpadButton.isPressed;
    }
    else
    {
        // 기본 게임패드 기능만 사용
        bool southButton = gamepad.buttonSouth.isPressed;
    }
}
```

### 케이스 3: Gamepad로만 인식 ⚠️

```
⚠️ 기본 Gamepad 타입으로만 인식
✓ 기본 버튼/스틱 사용 가능
❌ 터치패드 기능 불가
```

---

## 🔍 5단계: 고급 디버깅

### Input System 로그 활성화

```csharp
// 스크립트에 추가
void Start()
{
    InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", false);
    UnityEngine.InputSystem.InputSystem.onDeviceChange += (device, change) =>
    {
        Debug.Log($"[InputSystem] {change}: {device.name} ({device.GetType().Name})");
    };
}
```

### HID 정보 확인

```csharp
void Start()
{
    foreach (var device in InputSystem.devices)
    {
        Debug.Log($"Device: {device.name}");
        Debug.Log($"  Type: {device.GetType().FullName}");
        Debug.Log($"  Layout: {device.layout}");
        Debug.Log($"  Description: {device.description.ToJson()}");
        Debug.Log($"  Enabled: {device.enabled}");
    }
}
```

---

## 📋 체크리스트

테스트 전 확인:

- [ ] DualSense가 USB 또는 Bluetooth로 연결됨
- [ ] Windows 장치 관리자에서 "Wireless Controller" 확인됨
- [ ] DS4Windows, Steam 등 종료됨
- [ ] Unity Input System 패키지 설치됨 (1.4.0+)
- [ ] InputDeviceDebugWindow 또는 DualSenseTouchpadTest 씬에 추가됨
- [ ] Play 모드에서 F12 키로 디버그 창 확인
- [ ] Console에서 "DualSense 검색 결과" 로그 확인

---

## 🆘 최종 해결책

위 방법으로도 해결되지 않는 경우:

### 1. Windows 레지스트리 확인
- HID 드라이버가 제대로 설치되어 있는지 확인
- 장치 관리자에서 "Wireless Controller" 우클릭 → 드라이버 업데이트

### 2. Unity 프로젝트 설정
```
Project Settings > Player > Configuration
- Active Input Handling: Input System Package (New)
또는
- Active Input Handling: Both
```

### 3. 컨트롤러 초기화
1. DualSense 전원 끄기
2. USB 연결 해제
3. 재부팅
4. USB로 재연결

### 4. 대체 방법 사용
- 일반 Gamepad로 사용 (터치패드 제외)
- DS4Windows로 가상 Xbox 컨트롤러 생성
- Native 플러그인 사용 (DualSenseAPI)

---

## 📞 추가 지원

문제가 계속되면:
1. Unity Input Debugger 스크린샷
2. Console 로그 전체 복사
3. 컨트롤러 연결 방법 (USB/Bluetooth)
4. Unity 버전 및 Input System 버전

정보를 제공하면 더 정확한 해결책을 제시할 수 있습니다.
