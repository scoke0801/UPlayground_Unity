# UI_Base 시스템 사용 가이드

## 📌 개요

`UI_Base`는 모든 UI의 기반이 되는 추상 클래스입니다.
- UIManager와 연동하여 생명주기 자동 관리
- 페이드 효과, ESC 키 처리 등 기본 기능 제공
- 간편한 상속으로 커스텀 UI 구현

---

## 🏗️ 구조

```
UIManager (매니저)
    ↓
UI_Base (추상 기본 클래스)
    ↓
UI_Popup, UI_MainMenu, UI_Inventory... (실제 UI 클래스)
```

---

## 🔧 UI_Base 주요 기능

### 1. **생명주기 관리**

```csharp
Initialize()    // 최초 1회 초기화
Show()          // UI 표시
Hide()          // UI 숨김
Close()         // UI 제거
```

### 2. **가상 메서드 (오버라이드 가능)**

```csharp
OnInit()        // 초기화 로직 (버튼 바인딩 등)
OnShow()        // 표시될 때 (애니메이션, 데이터 갱신)
OnHide()        // 숨겨질 때
OnClose()       // 닫힐 때 (저장, 정리)
OnDispose()     // 파괴될 때 (이벤트 해제)
```

### 3. **페이드 효과**

```csharp
FadeIn(duration, onComplete)    // 페이드 인
FadeOut(duration, onComplete)   // 페이드 아웃
```

### 4. **유틸리티**

```csharp
SetInteractable(bool)  // UI 상호작용 활성화/비활성화
```

### 5. **자동 기능**

- ESC 키로 닫기 (선택적)
- CanvasGroup 자동 추가 (페이드용)
- 컴포넌트 자동 캐싱

---

## 📝 커스텀 UI 만들기

### Step 1: UI_Base 상속

```csharp
using UnityEngine;
using UnityEngine.UI;

public class UI_MainMenu : UI_Base
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    protected override void OnInit()
    {
        base.OnInit();

        // 버튼 이벤트 바인딩
        _startButton.onClick.AddListener(OnStartClicked);
        _optionsButton.onClick.AddListener(OnOptionsClicked);
        _quitButton.onClick.AddListener(OnQuitClicked);

        Debug.Log("[UI_MainMenu] 초기화 완료");
    }

    protected override void OnShow()
    {
        base.OnShow();

        // 페이드 인 효과
        FadeIn(0.3f);
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        // 이벤트 해제
        _startButton.onClick.RemoveAllListeners();
        _optionsButton.onClick.RemoveAllListeners();
        _quitButton.onClick.RemoveAllListeners();
    }

    private void OnStartClicked()
    {
        Debug.Log("게임 시작");
        // 게임 씬 로드 등
    }

    private void OnOptionsClicked()
    {
        Debug.Log("옵션 열기");
        // 옵션 UI 표시
    }

    private void OnQuitClicked()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
```

### Step 2: 프리팹 설정

1. Unity에서 Canvas 오브젝트 생성
2. 위에서 만든 `UI_MainMenu` 스크립트 추가
3. Inspector에서 설정:
   - `Layer`: 적절한 캔버스 레이어 선택 (Normal, Popup 등)
   - `Can Close With Esc`: ESC 키로 닫을지 여부
4. UI 요소 배치 (버튼, 텍스트 등)
5. 프리팹으로 저장

### Step 3: 코드에서 사용

```csharp
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject _mainMenuPrefab;

    void Start()
    {
        // 방법 1: 기본 사용
        GameObject menuUI = UIManager.Instance.ShowUI(_mainMenuPrefab, CanvasLayer.Normal, "MainMenu");
        UI_MainMenu menu = menuUI.GetComponent<UI_MainMenu>();
        menu.Initialize();
        menu.Show();

        // 방법 2: 확장 메서드 사용 (더 간편!)
        UI_MainMenu menu2 = UIManager.Instance.ShowUI<UI_MainMenu>(_mainMenuPrefab, CanvasLayer.Normal, "MainMenu");
    }
}
```

---

## 💡 사용 예시

### 예시 1: 간단한 팝업

```csharp
// 확인 팝업 표시
UI_Popup popup = UIManager.Instance.ShowUI<UI_Popup>(popupPrefab, CanvasLayer.Popup);
popup.Setup(
    title: "저장",
    message: "진행 상황을 저장하시겠습니까?",
    onConfirm: () => SaveGame(),
    onCancel: () => Debug.Log("저장 취소")
);
```

### 예시 2: 인벤토리 토글

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.I))
    {
        if (UIManager.Instance.IsUIActive("Inventory"))
        {
            UIManager.Instance.HideUI("Inventory");
        }
        else
        {
            UIManager.Instance.ShowUI<UI_Inventory>(inventoryPrefab, CanvasLayer.Normal, "Inventory");
        }
    }
}
```

### 예시 3: 페이드 효과

```csharp
UI_Base ui = UIManager.Instance.ShowUI<UI_Base>(uiPrefab, CanvasLayer.Normal);

// 페이드 인
ui.FadeIn(0.5f, () => Debug.Log("페이드 인 완료"));

// 3초 후 페이드 아웃
Invoke(() => 
{
    ui.FadeOut(0.5f, () => ui.Close());
}, 3f);
```

### 예시 4: 알림 메시지

```csharp
void ShowNotification(string message)
{
    UI_Popup notification = UIManager.Instance.ShowUI<UI_Popup>(
        notificationPrefab, 
        CanvasLayer.Notification, 
        "Notification"
    );
    
    notification.Setup("알림", message);
    
    // 3초 후 자동으로 닫기
    StartCoroutine(CloseAfterDelay(notification, 3f));
}

IEnumerator CloseAfterDelay(UI_Base ui, float delay)
{
    yield return new WaitForSeconds(delay);
    ui.Close();
}
```

---

## 🎯 베스트 프랙티스

### 1. **항상 OnInit에서 초기화**

```csharp
protected override void OnInit()
{
    base.OnInit();
    
    // ✅ 좋은 예: 버튼 바인딩
    _button.onClick.AddListener(OnButtonClicked);
    
    // ✅ 좋은 예: 데이터 로드
    LoadInitialData();
}
```

### 2. **OnDispose에서 정리**

```csharp
protected override void OnDispose()
{
    base.OnDispose();
    
    // ✅ 좋은 예: 이벤트 해제
    _button.onClick.RemoveAllListeners();
    
    // ✅ 좋은 예: 코루틴 정지
    StopAllCoroutines();
}
```

### 3. **페이드 효과 활용**

```csharp
protected override void OnShow()
{
    base.OnShow();
    
    // ✅ 자연스러운 등장
    FadeIn(0.3f);
}

public override void Close()
{
    // ✅ 자연스러운 퇴장
    FadeOut(0.3f, () => base.Close());
}
```

### 4. **레이어 적절히 사용**

```csharp
// ✅ 배경 이미지
[SerializeField] protected CanvasLayer _layer = CanvasLayer.Background;

// ✅ 일반 메뉴
[SerializeField] protected CanvasLayer _layer = CanvasLayer.Normal;

// ✅ 팝업 다이얼로그
[SerializeField] protected CanvasLayer _layer = CanvasLayer.Popup;

// ✅ 시스템 메시지
[SerializeField] protected CanvasLayer _layer = CanvasLayer.System;

// ✅ 알림
[SerializeField] protected CanvasLayer _layer = CanvasLayer.Notification;
```

---

## ⚠️ 주의사항

### 1. **Initialize()는 자동 호출됨**

```csharp
// ❌ 나쁜 예
UI_Base ui = UIManager.Instance.ShowUI<UI_Base>(prefab, layer);
ui.Initialize(); // 불필요! ShowUI가 이미 호출함
ui.Show();       // 불필요! ShowUI가 이미 호출함

// ✅ 좋은 예
UI_Base ui = UIManager.Instance.ShowUI<UI_Base>(prefab, layer);
// 바로 사용 가능
```

### 2. **base 메서드 호출 잊지 않기**

```csharp
// ✅ 좋은 예
protected override void OnInit()
{
    base.OnInit(); // 반드시 호출!
    
    // 커스텀 로직...
}
```

### 3. **컴포넌트는 SerializeField로 할당**

```csharp
// ✅ 좋은 예
[SerializeField] private Button _button;

// ❌ 나쁜 예 (런타임에 찾는 것은 느림)
private Button _button;
void Start() 
{
    _button = GetComponentInChildren<Button>();
}
```

---

## 📋 체크리스트

UI를 만들 때 다음을 확인하세요:

- [ ] UI_Base 상속
- [ ] OnInit에서 초기화 (버튼 바인딩 등)
- [ ] OnDispose에서 정리 (이벤트 해제 등)
- [ ] 적절한 CanvasLayer 설정
- [ ] ESC 키 처리 여부 설정
- [ ] 프리팹으로 저장
- [ ] UIManager를 통해 표시

---

## 🔗 관련 문서

- [UIManager 가이드](Manager_규칙.md)
- [캔버스 레이어 시스템](UIManager.cs)
- [애니메이션 시스템](애니메이션_관련.md)

---

## 📁 파일 구조

```
Assets/Scripts/
├── Core/
│   └── UIManager.cs           # UI 매니저
├── UI/
│   ├── Base/
│   │   ├── UI_Base.cs         # UI 기본 클래스
│   │   └── UIManagerExtensions.cs
│   ├── Popup/
│   │   └── UI_Popup.cs        # 팝업 UI
│   ├── Menu/
│   │   ├── UI_MainMenu.cs     # 메인 메뉴
│   │   └── UI_OptionsMenu.cs  # 옵션 메뉴
│   └── Game/
│       ├── UI_Inventory.cs    # 인벤토리
│       └── UI_HUD.cs          # HUD
└── Examples/
    └── UIUsageExample.cs      # 사용 예시
```
