# MotionSet 시스템

Unity + Animancer 기반의 제네릭 애니메이션 모션 세트 시스템

---

## 📋 목차

1. [개요](#개요)
2. [주요 기능](#주요-기능)
3. [설치 및 설정](#설치-및-설정)
4. [사용 방법](#사용-방법)
5. [재생 모드](#재생-모드)
6. [블렌딩 타입](#블렌딩-타입)
7. [API 레퍼런스](#api-레퍼런스)
8. [예시 코드](#예시-코드)

---

## 개요

MotionSet은 여러 애니메이션을 그룹으로 관리하고 다양한 방식으로 재생할 수 있는 시스템입니다.

### 특징

- ✅ **5가지 재생 모드**: Sequential, Blend, Directional, Random, Single
- ✅ **3가지 블렌딩 타입**: Linear, Cartesian, Directional
- ✅ **AnimationClip과 Montage 모두 지원**
- ✅ **Animancer 레이어 시스템 완벽 통합**
- ✅ **ScriptableObject 기반으로 재사용성 우수**
- ✅ **커스텀 에디터 도구 제공**

---

## 주요 기능

### 1. MotionSet (ScriptableObject)

애니메이션 그룹을 정의하는 에셋

```csharp
[CreateAssetMenu(menuName = "Animation/Motion Set")]
public class MotionSet : ScriptableObject
{
    public string motionSetName;
    public MotionPlayMode playMode;
    public MotionBlendType blendType;
    public List<MotionData> motions;
    public AnimationSlot targetSlotAsset;  // Slot 참조
    public Vector2 blendParameterRange;    // 블렌딩 범위
}
```

### 2. MotionData

개별 모션 정의

```csharp
[System.Serializable]
public class MotionData
{
    public MotionSourceType sourceType;     // Clip or Montage
    public AnimationClip clip;
    public AnimationMontage montage;
    
    public float threshold;                 // Linear/Cartesian X
    public float thresholdY;                // Cartesian Y
    public float directionAngle;            // Directional 각도
    
    public string motionName;
    public bool loopable;
}
```

### 3. MotionSetPlayer

MotionSet을 재생하는 플레이어 컴포넌트

```csharp
public class MotionSetPlayer : MonoBehaviour
{
    // 재생 제어
    public void Play(MotionSet motionSet);
    public void Stop(float fadeDuration = 0.25f);
    
    // Sequential 모드
    public void PlayNextSequential();
    public void PlayPreviousSequential();
    
    // Directional 모드
    public void PlayByDirection(Vector2 direction);
    
    // Blend 모드
    public void UpdateBlendParameter(float parameter);        // Linear
    public void UpdateBlendParameter(Vector2 parameter);      // Cartesian/Directional
    
    // 상태
    public bool IsPlaying { get; }
    public MotionSet CurrentMotionSet { get; }
    public MotionData CurrentMotion { get; }
    
    // 이벤트
    public event Action<MotionSet> OnMotionSetStarted;
    public event Action<MotionSet> OnMotionSetEnded;
    public event Action<MotionData> OnMotionChanged;
    public event Action<MotionData> OnMotionEnded;
}
```

---

## 설치 및 설정

### 1. 필수 요구사항

- Unity 2021.3 이상
- Animancer 8.x
- Input System (선택)

### 2. 설치

1. MotionSet 스크립트 임포트
2. AnimancerComponent 설치
3. (옵션) MontagePlayer 설치 (Montage 사용 시)

### 3. 기본 설정

```csharp
// GameObject에 컴포넌트 추가
GameObject character = new GameObject("Character");
character.AddComponent<Animator>();
var animancer = character.AddComponent<AnimancerComponent>();
var motionSetPlayer = character.AddComponent<MotionSetPlayer>();

// Inspector에서 Animancer 연결
```

---

## 사용 방법

### Step 1: MotionSet 생성

1. Project 창에서 우클릭
2. `Create → Animation → Motion Set`
3. Inspector에서 설정

### Step 2: 재생 방식 선택

- **Sequential**: 콤보 공격
- **Blend**: 이동 속도 블렌딩
- **Directional**: 8방향 이동
- **Random**: Idle 배리에이션
- **Single**: 단일 재생

### Step 3: 모션 추가

1. `➕ 모션 추가` 버튼 클릭
2. **Source Type** 선택 (Clip or Montage)
3. AnimationClip 또는 Montage 할당
4. 재생 방식에 맞게 파라미터 설정

### Step 4: 코드에서 재생

```csharp
[SerializeField] private MotionSetPlayer player;
[SerializeField] private MotionSet locomotionSet;

void Start()
{
    player.Play(locomotionSet);
}
```

---

## 재생 모드

### 1. Sequential (순차 재생)

모션을 순서대로 재생합니다.

**용도**: 콤보 공격, 스킬 체인

**사용법**:
```csharp
// 자동으로 다음 모션 재생
player.Play(combatComboSet);

// 수동으로 다음 콤보 실행
if (Input.GetKeyDown(KeyCode.Space))
    player.PlayNextSequential();
```

**설정**:
- 모션을 순서대로 추가
- `loopable = false` 권장

### 2. Blend (블렌딩)

파라미터 값에 따라 모션을 부드럽게 블렌딩합니다.

**용도**: 이동 속도에 따른 Idle→Walk→Run

**사용법**:
```csharp
player.Play(locomotionSet);

void Update()
{
    float speed = GetCurrentSpeed(); // 0~10
    player.UpdateBlendParameter(speed);
}
```

**설정**:
- Blend Type 선택 (Linear/Cartesian/Directional)
- 각 모션의 Threshold 설정
- Blend Parameter Range 설정

### 3. Directional (방향성)

입력 방향에 가장 가까운 애니메이션을 선택합니다.

**용도**: 8방향 이동

**사용법**:
```csharp
player.Play(directionalSet);

void Update()
{
    Vector2 input = GetInputDirection();
    if (input.magnitude > 0.1f)
        player.PlayByDirection(input.normalized);
}
```

**설정**:
- 각 모션의 Direction Angle 설정
- 📐 버튼으로 프리셋 선택 가능

### 4. Random (랜덤)

모션 리스트에서 랜덤하게 선택합니다.

**용도**: Idle 배리에이션

**사용법**:
```csharp
// 매번 랜덤 선택
player.Play(idleVariationsSet);

// 주기적으로 랜덤 재생
InvokeRepeating(nameof(PlayRandomIdle), 0f, 5f);
```

**설정**:
- 여러 배리에이션 모션 추가

### 5. Single (단일)

첫 번째 모션만 재생합니다.

**용도**: 일반 애니메이션

**사용법**:
```csharp
player.Play(singleMotionSet);
```

---

## 블렌딩 타입

### 1. Linear (1D 블렌딩)

하나의 파라미터로 블렌딩합니다.

**예시**: 속도에 따른 Idle→Walk→Run

```csharp
// MotionSet 설정
Idle:   threshold = 0
Walk:   threshold = 3
Run:    threshold = 6
Sprint: threshold = 10

// 코드
float speed = velocity.magnitude; // 0~10
player.UpdateBlendParameter(speed);
```

### 2. Cartesian (2D 블렌딩)

두 개의 파라미터로 블렌딩합니다.

**예시**: 전후좌우 자유 이동

```csharp
// MotionSet 설정
Forward:     threshold = 0,  thresholdY = 1
Right:       threshold = 1,  thresholdY = 0
Back:        threshold = 0,  thresholdY = -1
Left:        threshold = -1, thresholdY = 0

// 코드
Vector2 moveDir = new Vector2(horizontal, vertical);
player.UpdateBlendParameter(moveDir);
```

### 3. Directional (방향 블렌딩)

방향 벡터로 블렌딩합니다.

**예시**: 부드러운 방향 전환

```csharp
// MotionSet 설정
각 모션의 directionAngle 설정

// 코드
Vector2 direction = transform.forward;
player.UpdateBlendParameter(direction);
```

---

## API 레퍼런스

### MotionSet

#### 메서드

```csharp
// 레이어 인덱스 가져오기
int GetLayerIndex()

// 슬롯 이름 가져오기
string GetSlotName()

// 파라미터로 모션 찾기
MotionData GetMotionByParameter(float parameter)
MotionData GetMotionByParameter2D(Vector2 parameter)

// 방향으로 모션 찾기
MotionData GetMotionByDirection(Vector2 direction)

// 인덱스로 모션 찾기
MotionData GetMotionByIndex(int index)

// 랜덤 모션 가져오기
MotionData GetRandomMotion()
```

### MotionSetPlayer

#### 재생 제어

```csharp
void Play(MotionSet motionSet)
void Stop(float fadeDuration = 0.25f)
void SetSpeed(float speed)
```

#### Sequential 모드

```csharp
void PlayNextSequential()
void PlayPreviousSequential()
void PlaySequential(MotionSet motionSet, int index)
```

#### Directional 모드

```csharp
void PlayByDirection(Vector2 direction)
```

#### Blend 모드

```csharp
void UpdateBlendParameter(float parameter)        // Linear
void UpdateBlendParameter(Vector2 parameter)      // Cartesian/Directional
```

#### 프로퍼티

```csharp
bool IsPlaying { get; }
MotionSet CurrentMotionSet { get; }
MotionData CurrentMotion { get; }
int CurrentSequentialIndex { get; }
```

#### 이벤트

```csharp
event Action<MotionSet> OnMotionSetStarted
event Action<MotionSet> OnMotionSetEnded
event Action<MotionData> OnMotionChanged
event Action<MotionData> OnMotionEnded
```

---

## 예시 코드

### 예시 1: Locomotion (이동)

```csharp
public class PlayerLocomotion : MonoBehaviour
{
    [SerializeField] private MotionSetPlayer player;
    [SerializeField] private MotionSet locomotionSet;
    [SerializeField] private float maxSpeed = 10f;
    
    private void Start()
    {
        player.Play(locomotionSet);
    }
    
    private void Update()
    {
        // 입력
        Vector2 input = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // 속도 계산
        float speed = input.magnitude * maxSpeed;
        
        // 블렌딩 업데이트
        player.UpdateBlendParameter(speed);
    }
}
```

### 예시 2: Combat Combo (전투)

```csharp
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private MotionSetPlayer player;
    [SerializeField] private MotionSet combatComboSet;
    
    private bool canAttack = true;
    
    private void Start()
    {
        // 이벤트 구독
        player.OnMotionEnded += OnAttackEnded;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && canAttack)
        {
            if (!player.IsPlaying)
            {
                // 첫 공격 시작
                player.Play(combatComboSet);
                canAttack = false;
            }
            else
            {
                // 다음 콤보 실행
                player.PlayNextSequential();
            }
        }
    }
    
    private void OnAttackEnded(MotionData motion)
    {
        canAttack = true;
    }
}
```

### 예시 3: 8방향 이동

```csharp
public class DirectionalMovement : MonoBehaviour
{
    [SerializeField] private MotionSetPlayer player;
    [SerializeField] private MotionSet directionalSet;
    
    private void Start()
    {
        player.Play(directionalSet);
    }
    
    private void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        if (input.magnitude > 0.1f)
        {
            player.PlayByDirection(input.normalized);
        }
    }
}
```

### 예시 4: Idle 배리에이션

```csharp
public class IdleController : MonoBehaviour
{
    [SerializeField] private MotionSetPlayer player;
    [SerializeField] private MotionSet idleVariationsSet;
    [SerializeField] private float intervalMin = 3f;
    [SerializeField] private float intervalMax = 8f;
    
    private void Start()
    {
        StartCoroutine(PlayRandomIdle());
    }
    
    private IEnumerator PlayRandomIdle()
    {
        while (true)
        {
            float interval = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(interval);
            
            player.Play(idleVariationsSet);
        }
    }
}
```

---

## 프리셋

에디터에서 빠른 설정을 위한 프리셋 제공:

1. 🏃 **Locomotion**: Idle/Walk/Run/Sprint (Linear Blend)
2. ⚔️ **Combat Combo**: Attack1~4 (Sequential)
3. 🧭 **8방향 이동**: 8개 방향 애니메이션 (Directional)
4. 😴 **Idle 배리에이션**: 3개 랜덤 Idle (Random)

---

## 유틸리티

에디터 도구:

- 📊 **Threshold 자동 계산**: 균등 분배
- 🔄 **모션 정렬**: Threshold 기준 정렬
- 📝 **클립 이름으로 채우기**: 빈 이름 자동 입력
- 🗑️ **전체 초기화**: 모든 모션 삭제

---

## 문제 해결

### Q: Mixer가 작동하지 않습니다.

**A**: AnimancerComponent가 올바르게 설정되었는지 확인하세요.

### Q: Montage가 재생되지 않습니다.

**A**: MontagePlayer 컴포넌트가 MotionSetPlayer에 할당되었는지 확인하세요.

### Q: 블렌딩이 부자연스럽습니다.

**A**: Threshold 값이 올바르게 설정되었는지, 파라미터 범위가 적절한지 확인하세요.

---

## 라이센스

MIT License

---

## 연락처

문제나 제안사항이 있으시면 Issue를 등록해주세요.
