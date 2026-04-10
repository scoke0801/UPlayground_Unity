# TPS 액션 RPG 프로젝트

Unity 기반의 3인칭 슈팅(TPS) 액션 RPG 게임 프로젝트입니다.

## 📋 프로젝트 개요

본 프로젝트는 **데이터 중심 설계(Data-Driven Design)**를 기반으로 한 확장 가능하고 유지보수가 용이한 TPS 액션 RPG 시스템입니다.

### 주요 특징

- ✅ **ScriptableObject 기반 데이터 관리** - 밸런싱 및 설정 변경 용이
- ✅ **계층적 액터 시스템** - 명확한 클래스 구조로 재사용성 극대화
- ✅ **Animancer 기반 애니메이션** - MotionSet 시스템으로 유연한 애니메이션 관리
- ✅ **싱글톤 매니저 패턴** - 중앙 집중식 시스템 관리
- ✅ **레이어 기반 UI 시스템** - 7개 레이어로 체계적인 UI 관리

---

## 🎮 주요 기능

### 1. 캐릭터 시스템
- **플레이어** - TPS 컨트롤, 카메라 시스템, 인벤토리, 스킬
- **적(Enemy)** - AI 전투 시스템, 드롭 아이템
- **NPC** - 대화, 퀘스트, 상점 기능

### 2. 전투 시스템
- 체력/마나/스태미나 관리
- 상태 이상 효과
- 무기 및 스킬 시스템
- 데미지 계산 및 크리티컬

### 3. 애니메이션 시스템
- **MotionSet** - 블렌딩, 방향성, 순차 재생
- **AnimationMontage** - 섹션, 노티파이
- **Slot System** - 상체/하체 분리 재생

### 4. UI 시스템
- 7개 레이어 캔버스 (Background → Top)
- 자동 정렬 및 충돌 방지
- 동적 UI 생성/제거

---

## 🏗️ 시스템 아키텍처

### 액터 계층 구조

```
BaseActor (기본 액터)
    ├── Character (캐릭터)
    │   ├── Player (플레이어)
    │   ├── NPC (비플레이어 캐릭터)
    │   └── Enemy (적)
    ├── Weapon (무기)
    ├── Item (아이템)
    ├── Projectile (투사체)
    └── EnvironmentObject (환경 오브젝트)
```

### 매니저 구조

```
GameManager (최상위 매니저)
    ├── ResourceManager (리소스 관리)
    ├── UIManager (UI 관리)
    ├── SoundManager (사운드 관리)
    ├── InputManager (입력 관리)
    └── ... (커스텀 매니저)
```

### 애니메이션 계층

```
AnimationClip (기본 단위)
    ↓
AnimationMontage (섹션 + 노티파이)
    ↓
MotionSet (그룹 관리 + 블렌딩)
```

---

## 📁 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── BaseManager.cs          # 싱글톤 베이스
│   │   ├── IManager.cs             # 매니저 인터페이스
│   │   └── GameManager.cs          # 게임 매니저
│   ├── Actors/
│   │   ├── BaseActor.cs            # 액터 베이스
│   │   ├── Character.cs            # 캐릭터 베이스
│   │   ├── Player.cs               # 플레이어
│   │   ├── NPC.cs                  # NPC
│   │   └── Enemy.cs                # 적
│   ├── Animation/
│   │   ├── MotionSet.cs            # 모션 세트
│   │   ├── MotionSetPlayer.cs      # 모션 재생기
│   │   ├── AnimationMontage.cs     # 몽타쥬
│   │   └── Editor/
│   │       ├── MotionSetEditor.cs
│   │       └── MotionSetWindow.cs
│   ├── Managers/
│   │   ├── UIManager.cs            # UI 관리
│   │   ├── SoundManager.cs         # 사운드 관리
│   │   └── ResourceManager.cs      # 리소스 관리
│   └── Data/
│       ├── CharacterStatsData.cs   # 캐릭터 스탯 데이터
│       ├── PlayerSettingsData.cs   # 플레이어 설정
│       ├── MovementSettingsData.cs # 이동 설정
│       └── NPCSettingsData.cs      # NPC 설정
└── Resources/
    ├── Data/                       # ScriptableObject 데이터
    ├── Prefabs/                    # 프리팹
    └── UI/                         # UI 프리팹
```

---

## 🚀 시작하기

### 필수 요구사항

- **Unity 2022.3 LTS** 이상
- **Animancer v8.x** (애니메이션 시스템)

### 설치 방법

1. Unity 프로젝트 생성
2. Animancer 패키지 임포트
3. 본 프로젝트 파일 임포트

### 기본 씬 설정

1. **GameManager 생성**
   ```
   빈 GameObject 생성 → GameManager 컴포넌트 추가
   또는 코드에서 GameManager.Instance 호출 시 자동 생성
   ```

2. **캔버스 설정**
   ```csharp
   // UIManager가 자동으로 7개 캔버스 생성
   UIManager.Instance.ShowUI(uiPrefab, CanvasLayer.Normal);
   ```

3. **플레이어 설정**
   ```
   Player 프리팹 배치
   PlayerSettingsData 할당
   CharacterStatsData 할당
   ```

---

## 📖 주요 시스템 가이드

### 시스템별 상세 가이드 문서

| 문서 | 설명 |
|------|------|
| [ACTOR_ID_SYSTEM_GUIDE.md](ACTOR_ID_SYSTEM_GUIDE.md) | Actor ID 시스템 — 데이터 정의, 런타임 스폰, 에디터 사용법 |
| [CRAFTING_SYSTEM_GUIDE.md](CRAFTING_SYSTEM_GUIDE.md) | 제작(Crafting) 시스템 — 레시피, 재료, 언락 조건 |
| [SAVE_SYSTEM_GUIDE.md](SAVE_SYSTEM_GUIDE.md) | 세이브/로드 시스템 |
| [UI_Base_Guide.md](UI_Base_Guide.md) | UI 베이스 시스템 — 레이어 구조, UI 생성/제거 |
| [GAMEMANAGER_README.md](GAMEMANAGER_README.md) | GameManager — 매니저 등록 및 초기화 순서 |

---

### 1. 새로운 캐릭터 만들기

#### Character 상속 클래스 작성

```csharp
public class CustomCharacter : Character
{
    protected override void Initialize()
    {
        base.Initialize();
        // 초기화 로직
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        // 매 프레임 로직
    }
}
```

#### ScriptableObject 데이터 생성

```
Assets → Create → RPG → Character → Stats Data
인스펙터에서 스탯 설정
캐릭터에 데이터 할당
```

---

### 2. MotionSet 시스템 사용

#### MotionSet 생성

```
Assets → Create → Animation → Motion Set
또는
Window → Animation → Motion Set Editor
```

#### Locomotion 예시

```csharp
public class CharacterLocomotion : MonoBehaviour
{
    [SerializeField] private MotionSet locomotionMotionSet;
    [SerializeField] private MotionSetPlayer motionSetPlayer;
    
    void Start()
    {
        // MotionSet 재생 시작
        motionSetPlayer.Play(locomotionMotionSet);
    }
    
    void Update()
    {
        float speed = GetMovementSpeed(); // 0~10
        // 속도에 따라 Idle → Walk → Run → Sprint 자동 블렌딩
        motionSetPlayer.UpdateBlendParameter(speed);
    }
}
```

#### 콤보 공격 예시

```csharp
public class CombatSystem : MonoBehaviour
{
    [SerializeField] private MotionSet combatComboMotionSet;
    [SerializeField] private MotionSetPlayer motionSetPlayer;
    
    void OnAttackInput()
    {
        // Sequential 모드로 설정된 경우
        motionSetPlayer.PlayNextSequential();
    }
}
```

---

### 3. UI 시스템 사용

#### UI 표시

```csharp
// 방법 1: 레이어 지정
UIManager.Instance.ShowUI(inventoryPrefab, CanvasLayer.Popup, "Inventory");

// 방법 2: 기본 레이어 사용
UIManager.Instance.ShowUI(hudPrefab, CanvasLayer.Normal);
```

#### UI 제거

```csharp
// 특정 UI 제거
UIManager.Instance.HideUI("Inventory");

// 레이어의 모든 UI 제거
UIManager.Instance.HideAllUIInLayer(CanvasLayer.Popup);

// 모든 UI 제거
UIManager.Instance.HideAllUI();
```

#### UI 레이어 구조

| 레이어 | SortingOrder | 용도 |
|--------|-------------|------|
| Background | 0 | 배경 UI |
| Scene | 100 | 씬 내 UI |
| Normal | 200 | 일반 UI (HUD) |
| Popup | 300 | 팝업 UI |
| System | 400 | 시스템 UI |
| Notification | 500 | 알림 UI |
| Top | 600 | 최상위 UI |

---

### 4. 매니저 시스템 사용

#### 새 매니저 생성

```csharp
public class CustomManager : BaseManager<CustomManager>, IManager
{
    public void Init()
    {
        Debug.Log("매니저 초기화");
    }
    
    public void Dispose()
    {
        Debug.Log("매니저 정리");
    }
    
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    
    public void CustomMethod()
    {
        Debug.Log("커스텀 기능 실행");
    }
}
```

#### GameManager에 등록

```csharp
// GameManager.cs
private void InitializeManagers()
{
    RegisterManager(ResourceManager.Instance);
    RegisterManager(UIManager.Instance);
    RegisterManager(CustomManager.Instance); // 새 매니저 추가
}
```

#### 사용

```csharp
// 직접 접근
CustomManager.Instance.CustomMethod();

// GameManager를 통한 접근
var manager = GameManager.Instance.GetManager<CustomManager>();
manager?.CustomMethod();
```

---

## 🎯 데이터 기반 설계

### ScriptableObject 활용

모든 설정은 ScriptableObject로 관리하여 다음 이점을 제공합니다:

- **밸런싱 용이** - 코드 수정 없이 데이터만 변경
- **재사용성** - 여러 캐릭터가 동일 데이터 공유 가능
- **에셋 관리** - 프로젝트 윈도우에서 직접 관리
- **에디터 통합** - 인스펙터에서 실시간 수정

### 데이터 종류

| 데이터 타입 | 파일명 | 용도 |
|------------|--------|------|
| CharacterStatsData | `*.asset` | HP, MP, 스태미나 등 |
| PlayerSettingsData | `*.asset` | 입력, 카메라 설정 |
| MovementSettingsData | `*.asset` | 이동 속도, 점프력 등 |
| NPCSettingsData | `*.asset` | AI, 대화, 상점 |

---

## 🎨 애니메이션 워크플로우

### 1. 기본 애니메이션 (Clip)
```
단순 재생이 필요한 경우
예: 대기, 일반 공격
```

### 2. 복잡한 애니메이션 (Montage)
```
섹션 분할 + 이벤트가 필요한 경우
예: 보스 공격 패턴, 스킬 시전
```

### 3. 그룹 관리 (MotionSet)
```
여러 애니메이션을 묶어서 관리
예: 이동 블렌딩, 콤보 공격, 8방향 이동
```

### MotionSet 재생 방식

| 모드 | 용도 | 예시 |
|------|------|------|
| Sequential | 순차 재생 | 콤보 공격 |
| Blend | 블렌딩 | 이동 속도 |
| Directional | 방향성 | 8방향 이동 |
| Random | 랜덤 | Idle 배리에이션 |
| Single | 단일 재생 | 특정 스킬 |

---

## ⚙️ 최적화 가이드

### Update 메서드 사용 가이드

```csharp
// ❌ 나쁜 예 - 매 프레임 비용이 큰 연산
public void OnUpdate()
{
    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
}

// ✅ 좋은 예 - 캐싱 및 조건부 실행
private List<Enemy> cachedEnemies;
private float updateInterval = 0.5f;
private float timer = 0f;

public void OnUpdate()
{
    timer += Time.deltaTime;
    if (timer >= updateInterval)
    {
        timer = 0f;
        UpdateEnemyList();
    }
}
```

### 물리 vs 일반 업데이트

```csharp
// FixedUpdate (50fps) - 물리 기반 작업
protected override void HandlePhysicsMovement()
{
    // 이동, 점프, 충돌 검사
}

// Update (가변 fps) - 입력 및 로직
protected override void UpdateGameLogic()
{
    // 입력 처리, 상태 업데이트
}

// LateUpdate - 카메라 작업
protected override void HandleLateUpdate()
{
    // 카메라 추적, UI 위치 갱신
}
```

---

## 🐛 디버깅 팁

### 1. 매니저 초기화 확인

```csharp
// GameManager가 제대로 초기화되었는지 확인
if (GameManager.Instance == null)
{
    Debug.LogError("GameManager가 초기화되지 않았습니다!");
}
```

### 2. 데이터 할당 검증

```csharp
// ScriptableObject가 할당되었는지 확인
if (statsData == null)
{
    Debug.LogError($"{gameObject.name}: CharacterStatsData가 할당되지 않았습니다!");
}
```

### 3. 이벤트 구독 해제

```csharp
// OnDestroy에서 이벤트 구독 해제
private void OnDestroy()
{
    if (player != null)
    {
        player.OnHPChanged -= HandleHPChanged;
    }
}
```

---

## 📝 코딩 컨벤션

### 네이밍 규칙

```csharp
// 클래스: PascalCase
public class PlayerController { }

// 메서드: PascalCase
public void Initialize() { }

// 프로퍼티: PascalCase
public float MaxHP { get; set; }

// private 필드: _camelCase
private float _currentHP;

// public 필드: camelCase
public float moveSpeed;

// 상수: UPPER_CASE
private const int MAX_INVENTORY_SIZE = 100;
```

### 주석 작성

```csharp
/// <summary>
/// 클래스/메서드 설명 (XML 주석)
/// </summary>
/// <param name="damage">데미지 양</param>
public void TakeDamage(float damage)
{
    // 간단한 설명
    CurrentHP -= damage;
}
```

---

## 🔧 확장 가이드

### 새로운 액터 타입 추가

1. BaseActor 또는 Character 상속
2. 필요한 메서드 오버라이드
3. ScriptableObject 데이터 생성
4. 프리팹 제작

### 새로운 매니저 추가

1. BaseManager 상속 + IManager 구현
2. GameManager에 등록
3. 초기화 순서 고려

### 새로운 UI 추가

1. UI 프리팹 제작
2. 적절한 캔버스 레이어 선택
3. UIManager.ShowUI() 호출

---

## 📚 참고 자료

### 외부 라이브러리

- [Animancer](https://kybernetik.com.au/animancer/) - 애니메이션 시스템

### Unity 문서

- [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html)
- [Physics](https://docs.unity3d.com/Manual/PhysicsSection.html)
- [UI System](https://docs.unity3d.com/Packages/com.unity.ugui@latest)

---

## 📄 라이선스

본 프로젝트는 교육 및 학습 목적으로 제작되었습니다.

---

## ✨ 업데이트 내역

### v1.0.0 (2025-11-15)
- 기본 액터 시스템 구현
- MotionSet 애니메이션 시스템 추가
- 매니저 시스템 구축
- UI 레이어 시스템 완성
- ScriptableObject 기반 데이터 관리

---

## 👥 기여

프로젝트 개선 제안이나 버그 리포트는 환영합니다!

---

**Made with ❤️ using Unity**
