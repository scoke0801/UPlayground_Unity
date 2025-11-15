# GameManager 시스템 사용 가이드

## 📌 개요

GameManager는 프로젝트의 모든 매니저를 총괄하는 최상위 매니저입니다.
- 매니저들의 초기화 순서 관리
- Unity 라이프사이클 이벤트 전파 (Update, FixedUpdate, LateUpdate)
- 매니저들의 생명주기 관리 (Init, Dispose)

## 🏗️ 구조

```
BaseManager<T>              # 싱글톤 베이스 클래스
    ↓
GameManager                 # 매니저들을 관리하는 최상위 매니저
    ↓
IManager                    # 모든 매니저가 구현할 인터페이스
    ↓
UIManager, SoundManager...  # 실제 기능 매니저들
```

## 🔧 사용 방법

### 1. 새 매니저 만들기

```csharp
public class MyCustomManager : BaseManager<MyCustomManager>, IManager
{
    // 필수 구현: IManager 인터페이스
    
    public void Init()
    {
        // 매니저 초기화 로직
        Debug.Log("MyCustomManager 초기화");
    }

    public void Dispose()
    {
        // 리소스 해제, 정리 작업
        Debug.Log("MyCustomManager 정리");
    }

    public void OnUpdate()
    {
        // 매 프레임 실행되는 로직
    }

    public void OnFixedUpdate()
    {
        // 물리 프레임마다 실행 (고정 시간 간격)
    }

    public void OnLateUpdate()
    {
        // Update 이후 실행 (카메라 작업 등)
    }

    // 커스텀 메서드들
    public void DoSomething()
    {
        Debug.Log("커스텀 기능 실행");
    }
}
```

### 2. GameManager에 매니저 등록

**방법 1: GameManager.cs에서 직접 등록**
```csharp
private void InitializeManagers()
{
    if (_isInitialized)
        return;

    Debug.Log("[GameManager] 매니저 초기화 시작");

    // 초기화 순서대로 등록
    RegisterManager(ResourceManager.Instance);
    RegisterManager(SoundManager.Instance);
    RegisterManager(UIManager.Instance);
    RegisterManager(MyCustomManager.Instance);

    _isInitialized = true;
}
```

**방법 2: 외부에서 동적 등록**
```csharp
public class GameInitializer : MonoBehaviour
{
    void Start()
    {
        // GameManager가 준비된 후 매니저 등록
        GameManager.Instance.RegisterManager(UIManager.Instance);
        GameManager.Instance.RegisterManager(SoundManager.Instance);
    }
}
```

### 3. 매니저 사용하기

```csharp
public class PlayerController : MonoBehaviour
{
    void Start()
    {
        // 방법 1: 직접 접근
        UIManager.Instance.ShowUI("MainMenu");
        SoundManager.Instance.PlayBGM("MainTheme");

        // 방법 2: GameManager를 통해 접근
        var uiManager = GameManager.Instance.GetManager<UIManager>();
        uiManager?.ShowUI("MainMenu");
    }
}
```

## 📋 IManager 인터페이스 메서드 설명

| 메서드 | 호출 시점 | 용도 |
|--------|-----------|------|
| `Init()` | 매니저 등록 시 1회 | 초기화, 리소스 로드 |
| `Dispose()` | 씬 전환 또는 종료 시 | 리소스 해제, 정리 |
| `OnUpdate()` | 매 프레임 | 일반 업데이트 로직 |
| `OnFixedUpdate()` | 고정 시간 간격 | 물리 연산, 타이머 |
| `OnLateUpdate()` | Update 이후 | 카메라 추적, UI 위치 갱신 |

## 🎯 사용 예시

### 예시 1: UI 표시

```csharp
void ShowInventory()
{
    UIManager.Instance.ShowUI("Inventory");
}
```

### 예시 2: 사운드 재생

```csharp
void OnPlayerAttack()
{
    SoundManager.Instance.PlaySFX("Sword_Swing");
}
```

### 예시 3: 매니저 간 통신

```csharp
public class QuestManager : BaseManager<QuestManager>, IManager
{
    public void CompleteQuest(string questId)
    {
        Debug.Log($"퀘스트 완료: {questId}");
        
        // UI 업데이트
        UIManager.Instance.ShowUI("QuestComplete");
        
        // 효과음 재생
        SoundManager.Instance.PlaySFX("Quest_Complete");
    }
    
    // IManager 인터페이스 구현...
}
```

## 🎮 Unity 에디터에서 설정

1. 빈 GameObject 생성
2. `GameManager` 컴포넌트 추가
3. 씬 시작 시 자동으로 매니저들 초기화

또는 자동 생성 옵션:
- GameManager는 BaseManager를 상속하므로 자동 생성됨
- `GameManager.Instance` 호출 시 씬에 없으면 자동 생성

## ⚠️ 주의사항

1. **초기화 순서**: 의존성이 있는 매니저는 순서를 고려해서 등록
   ```csharp
   // ResourceManager를 먼저 초기화한 후
   RegisterManager(ResourceManager.Instance);
   // UIManager가 리소스를 사용
   RegisterManager(UIManager.Instance);
   ```

2. **Update 성능**: 매 프레임 호출되므로 무거운 작업은 피하기
   ```csharp
   // 나쁜 예
   public void OnUpdate()
   {
       FindAllEnemies(); // 매 프레임 검색 (느림)
   }
   
   // 좋은 예
   private List<Enemy> _enemies;
   public void OnUpdate()
   {
       // 캐시된 리스트 사용
       foreach (var enemy in _enemies) { }
   }
   ```

3. **Null 체크**: 매니저 사용 전 항상 null 체크
   ```csharp
   var uiManager = GameManager.Instance.GetManager<UIManager>();
   if (uiManager != null)
   {
       uiManager.ShowUI("Menu");
   }
   ```

## 🔄 매니저 생명주기

```
게임 시작
    ↓
GameManager.Awake()
    ↓
InitializeManagers()
    ↓
각 매니저.Init() 호출
    ↓
게임 실행 (Update, FixedUpdate, LateUpdate 반복)
    ↓
씬 전환 또는 게임 종료
    ↓
각 매니저.Dispose() 호출
    ↓
GameManager.OnDestroy()
```

## 📁 파일 구조

```
Assets/Scripts/
├── Core/
│   ├── BaseManager.cs          # 싱글톤 베이스
│   ├── IManager.cs             # 매니저 인터페이스
│   └── GameManager.cs          # 게임 매니저
└── Managers/
    ├── UIManager.cs            # UI 관리
    ├── SoundManager.cs         # 사운드 관리
    ├── ResourceManager.cs      # 리소스 관리
    └── ...
```

## 💡 팁

- BaseManager는 DontDestroyOnLoad 옵션이 있어 씬 전환 시에도 유지됨
- 매니저가 필요 없는 메서드는 비워두면 됨 (호출은 되지만 성능 영향 미미)
- GameManager 없이 각 매니저를 단독으로 사용할 수도 있음
