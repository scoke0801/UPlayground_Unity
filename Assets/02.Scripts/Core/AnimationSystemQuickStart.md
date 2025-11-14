# 애니메이션 시스템 빠른 시작 가이드

## Animancer API 수정 사항

### 레이어 생성
```csharp
// ❌ 잘못된 방법
var layer = animancer.CreateLayer();

// ✅ 올바른 방법
var layer = animancer.Layers[1]; // 접근하면 자동으로 생성됨
```

### 마스크 설정
```csharp
// ❌ 잘못된 방법
layer.SetMask(avatarMask);

// ✅ 올바른 방법
layer.Mask = avatarMask;
```

### 애니메이션 중지
```csharp
// ❌ 잘못된 방법
animancer.Stop(state, fadeTime);

// ✅ 올바른 방법
state.StartFade(0, fadeTime);
```

## 기본 사용법

### 1. 컴포넌트 설정

GameObject에 다음 컴포넌트 추가:
```
- AnimancerComponent (필수)
- AnimationMontagePlayer (Montage 사용 시)
- AdvancedCharacterAnimationController (통합 컨트롤러)
```

### 2. Animation Sequence 생성

```
Project 창 → 우클릭 → Create → Animation → Animation Sequence
```

필드 설정:
- **Clip**: AnimationClip 할당
- **Loop**: true/false
- **Playback Speed**: 1.0 (기본값)

### 3. Animation Montage 생성

```
Project 창 → 우클릭 → Create → Animation → Animation Montage
```

Sections 설정 예시 (공격 콤보):
```
Section 1:
- Section Name: "Attack1"
- Clip: Attack1 애니메이션
- Next Section: "" (수동 제어)

Section 2:
- Section Name: "Attack2"
- Clip: Attack2 애니메이션
- Next Section: "" (수동 제어)

Section 3:
- Section Name: "Attack3"
- Clip: Attack3 애니메이션
- Next Section: "" (수동 제어)
```

### 4. Inspector 설정

`AdvancedCharacterAnimationController`:
- **Animation Sequences**: Idle, Walk, Run 할당
- **Animation Montages**: Attack, Jump, Skill 등 할당
- **Upper Body Mask**: (옵션) 상체 전용 마스크

### 5. 코드 사용

#### 기본 이동
```csharp
public class MyCharacter : MonoBehaviour
{
    [SerializeField] private AdvancedCharacterAnimationController animController;
    
    void Update()
    {
        float speed = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        animController.UpdateMovement(speed, isRunning);
    }
}
```

#### 공격 (단일)
```csharp
void Attack()
{
    if (animController.CanAttack())
    {
        animController.PlayAttack();
    }
}
```

#### 공격 콤보 (섹션 활용)
```csharp
private int comboStep = 0;

void OnAttackInput()
{
    if (!animController.CanAttack()) return;
    
    switch (comboStep)
    {
        case 0:
            animController.PlayAttack("Attack1");
            break;
        case 1:
            animController.JumpToMontageSection("Attack2");
            break;
        case 2:
            animController.JumpToMontageSection("Attack3");
            break;
    }
    
    comboStep++;
}

// Montage 완료 시 콤보 리셋
void Start()
{
    var montagePlayer = GetComponent<AnimationMontagePlayer>();
    montagePlayer.OnMontageEnded += (montage) => {
        comboStep = 0;
    };
}
```

#### 이벤트 처리
```csharp
void Start()
{
    var montagePlayer = GetComponent<AnimationMontagePlayer>();
    montagePlayer.OnMontageEvent += OnAnimationEvent;
}

void OnAnimationEvent(string eventName, string parameter)
{
    switch (eventName)
    {
        case "AttackHit":
            PerformHitDetection();
            break;
            
        case "PlaySound":
            AudioManager.Play(parameter);
            break;
            
        case "SpawnEffect":
            SpawnEffect(parameter);
            break;
    }
}
```

## 레이어 시스템 (상체/하체 분리)

### AvatarMask 생성
1. Project 창 → Create → Avatar Mask
2. "Upper Body Mask" 이름 지정
3. Inspector에서 상체 본만 활성화

### 사용
```csharp
// 상체 전용 애니메이션 재생 (마스크 사용)
animController.PlayUpperBodyAnimation(aimClip, upperBodyMask);

// 동시에 하체는 이동 애니메이션
animController.UpdateMovement(speed, isRunning);
```

## 일반적인 패턴

### 점프 (섹션 활용)
```csharp
Montage 구조:
- "JumpStart": 점프 시작
- "InAir": 공중 (무한 루프)
- "Land": 착지

// 점프
void Jump()
{
    animController.PlayJump();
    // "JumpStart" → "InAir" 자동 전환
}

// 착지 감지
void OnGrounded()
{
    if (animController.IsPlayingMontage())
    {
        animController.JumpToMontageSection("Land");
    }
}
```

### 리로드 (루프 섹션)
```csharp
Montage 구조:
- "Start": 탄창 꺼내기
- "Loop": 탄알 장전 (무한 루프, LoopCount = -1)
- "End": 탄창 넣기

// 리로드 시작
void StartReload()
{
    animController.PlaySkill("Start");
    // "Start" 끝나면 "Loop"로 자동 전환
}

// 탄알 다 채웠을 때
void OnAmmoFull()
{
    animController.JumpToMontageSection("End");
}
```

### 스킬 캐스팅
```csharp
Montage 구조:
- "Cast": 캐스팅 동작
- "Hold": 유지 동작 (루프)
- "Release": 발사 동작

Events:
- "SpawnProjectile" (Release 섹션의 0.2초)

// 스킬 시작
void StartCastSkill()
{
    animController.PlaySkill("Cast");
}

// 차징 (Cast가 끝나면 자동으로 Hold로)
void Update()
{
    if (Input.GetKeyUp(KeyCode.Q))
    {
        animController.JumpToMontageSection("Release");
    }
}

// 이벤트 처리
void OnMontageEvent(string name, string param)
{
    if (name == "SpawnProjectile")
    {
        SpawnProjectile();
    }
}
```

## 디버깅

### Animancer Inspector 활용
Play Mode에서 AnimancerComponent 확인:
- **States**: 현재 재생 중인 애니메이션
- **Weight**: 각 애니메이션의 가중치
- **Time**: 현재 재생 시간
- **Speed**: 재생 속도

### 로그 확인
```csharp
Debug.Log($"State: {animController.GetCurrentState()}");
Debug.Log($"Can Move: {animController.CanMove()}");
Debug.Log($"Playing Montage: {animController.IsPlayingMontage()}");
```

## 자주 하는 실수

### ❌ 잘못된 방법
```csharp
// 1. 레이어를 생성하려고 시도
animancer.CreateLayer(); // ❌ 이 메서드는 없음

// 2. Stop을 잘못 사용
animancer.Stop(state, fadeTime); // ❌ 2개 인자 없음

// 3. 마스크를 잘못 설정
layer.SetMask(mask); // ❌ 이 메서드는 없음
```

### ✅ 올바른 방법
```csharp
// 1. 레이어는 접근만 하면 자동 생성
var layer = animancer.Layers[1];

// 2. StartFade 사용
state.StartFade(0, fadeTime);

// 3. Mask 프로퍼티 사용
layer.Mask = avatarMask;
```

## 추가 리소스

- **AnimationSystemExample.cs**: 실전 예시 코드
- **AdvancedCharacterAnimationController.cs**: 구현 참고
- [Animancer 공식 문서](https://kybernetik.com.au/animancer/docs/)
