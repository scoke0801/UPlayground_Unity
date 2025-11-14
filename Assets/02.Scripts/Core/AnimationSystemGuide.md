# 언리얼 스타일 애니메이션 시스템 가이드

## 개요

Animancer를 사용하여 언리얼 엔진의 Animation Sequence와 Animation Montage 시스템을 유니티에서 구현했습니다.

## 주요 컴포넌트

### 1. AnimationSequence (ScriptableObject)
- **용도**: 단일 애니메이션 클립과 메타데이터를 포함하는 애셋
- **언리얼 대응**: Animation Sequence
- **사용 예시**: Idle, Walk, Run 등 기본 이동 애니메이션

**생성 방법**:
```
Project 창 → 우클릭 → Create → Animation → Animation Sequence
```

**설정 항목**:
- `Clip`: 실제 AnimationClip
- `Loop`: 루프 여부
- `Playback Speed`: 재생 속도
- `Enable Root Motion`: 루트 모션 활성화

### 2. AnimationMontage (ScriptableObject)
- **용도**: 여러 애니메이션을 섹션으로 나누어 조합하고 동적으로 제어
- **언리얼 대응**: Animation Montage
- **사용 예시**: 공격 콤보, 점프, 스킬, 리로드 등 복잡한 액션 애니메이션

**생성 방법**:
```
Project 창 → 우클릭 → Create → Animation → Animation Montage
```

**주요 기능**:
- **Sections**: 애니메이션을 여러 섹션으로 분할
  - 각 섹션은 독립적인 AnimationClip 보유
  - 섹션 간 점프 가능
  - 특정 섹션 반복 가능
- **Events**: 특정 타이밍에 이벤트 발생
  - 공격 판정, 사운드, 이펙트 등
- **Slots**: 신체 부위별 애니메이션 제어 (상체/하체 분리)

### 3. AnimationMontagePlayer (Component)
- **용도**: AnimationMontage를 재생하는 컴포넌트
- Animancer와 통합하여 동작

**주요 메서드**:
```csharp
// Montage 재생
void PlayMontage(AnimationMontage montage, string startSection = null)

// 특정 섹션으로 점프
void JumpToSection(string sectionName)

// Montage 중지
void StopMontage(float blendOutTime = -1)

// 재생 속도 조절
void SetPlayRate(float rate)
```

**이벤트**:
```csharp
// Montage 종료 시
event MontageEndedDelegate OnMontageEnded

// Montage 이벤트 발생 시
event MontageEventDelegate OnMontageEvent
```

### 4. AdvancedCharacterAnimationController (Component)
- **용도**: 통합 애니메이션 컨트롤러
- AnimationSequence와 AnimationMontage를 모두 관리
- 캐릭터의 모든 애니메이션을 제어

## 사용 예시

### 기본 설정

1. 캐릭터 GameObject에 컴포넌트 추가:
   - `AnimancerComponent`
   - `AnimationMontagePlayer`
   - `AdvancedCharacterAnimationController`

2. Animation Sequence 생성 (Idle, Walk, Run):
```
Assets → Create → Animation → Animation Sequence
- 이름: Idle_Sequence
- Clip: Idle 애니메이션 클립 할당
- Loop: true
```

3. Animation Montage 생성 (Attack):
```
Assets → Create → Animation → Animation Montage
- 이름: Attack_Montage
- Sections 추가:
  * Section 1: "Start" - 공격 시작 애니메이션
  * Section 2: "Loop" - 공격 중간 (반복 가능)
  * Section 3: "End" - 공격 종료 애니메이션
```

### 코드 사용 예시

#### 1. 기본 이동 (Animation Sequence)
```csharp
// Idle
controller.PlayIdle();

// 이동
controller.UpdateMovement(moveSpeed, isRunning);
```

#### 2. 공격 (Animation Montage)
```csharp
// 기본 공격
controller.PlayAttack();

// 특정 콤보 섹션부터 시작
controller.PlayAttack("Combo2");

// 다음 콤보로 연결
controller.JumpToMontageSection("Combo3");
```

#### 3. 스킬 사용
```csharp
// 스킬 실행
controller.PlaySkill("FireballCast");

// 스킬 이벤트 처리
private void OnMontageEventTriggered(string eventName, string parameter)
{
    if (eventName == "SpawnProjectile")
    {
        SpawnFireball();
    }
}
```

#### 4. 점프 (섹션 활용)
```csharp
// 점프 Montage 구조:
// - "JumpStart": 점프 시작
// - "InAir": 공중 루프 (무한 반복)
// - "Land": 착지

// 점프 시작
controller.PlayJump();

// 착지 시 (별도 감지 로직 필요)
if (isGrounded)
{
    controller.JumpToMontageSection("Land");
}
```

### Montage 섹션 설정 예시

#### 리로드 애니메이션
```
Sections:
1. "Start" - 탄창 꺼내기
   - Next Section: "Loop"
   
2. "Loop" - 탄알 장전 (반복)
   - Loop: true
   - Loop Count: -1 (무한)
   - Next Section: "" (수동으로 End로 이동)
   
3. "End" - 탄창 넣기
   - Next Section: ""
```

코드:
```csharp
// 리로드 시작
controller.PlaySkill("Start");

// 탄알 다 채웠을 때
if (ammoFull)
{
    controller.JumpToMontageSection("End");
}
```

#### 공격 콤보
```
Sections:
1. "Attack1" - 첫 번째 공격
   - Next Section: "" (대기)
   
2. "Attack2" - 두 번째 공격
   - Next Section: "" (대기)
   
3. "Attack3" - 세 번째 공격 (피니셔)
   - Next Section: ""
```

코드:
```csharp
private int comboStep = 0;

void OnAttackInput()
{
    string[] comboSections = { "Attack1", "Attack2", "Attack3" };
    
    if (comboStep < comboSections.Length)
    {
        if (comboStep == 0)
            controller.PlayAttack(comboSections[0]);
        else
            controller.JumpToMontageSection(comboSections[comboStep]);
        
        comboStep++;
    }
}

void OnMontageCompleted(AnimationMontage montage)
{
    comboStep = 0; // 콤보 리셋
}
```

## 이벤트 시스템

### Montage Event 설정
1. AnimationMontage 애셋 열기
2. Inspector에서 Events 섹션 확장
3. 이벤트 추가:
   - Event Name: "AttackHit"
   - Trigger Time: 0.5 (초)
   - Event Parameter: "Sword" (옵션)

### 이벤트 처리
```csharp
// AdvancedCharacterAnimationController에서
private void OnMontageEventTriggered(string eventName, string parameter)
{
    switch (eventName)
    {
        case "AttackHit":
            PerformAttackCheck();
            break;
            
        case "PlaySound":
            AudioManager.PlaySound(parameter);
            break;
            
        case "SpawnEffect":
            EffectManager.Spawn(parameter, transform.position);
            break;
    }
}
```

## 레이어 시스템

상체/하체를 분리하여 동시에 다른 애니메이션 재생:

```csharp
// 하체: 이동 애니메이션
controller.UpdateMovement(speed, isRunning);

// 상체: 공격 애니메이션 (동시 재생)
controller.PlayUpperBodyAnimation(aimClip);
```

## 성능 최적화

1. **Animation Sequence 재사용**: 같은 클립을 여러 곳에서 사용 가능
2. **Montage 프리로드**: 자주 사용하는 Montage는 미리 로드
3. **이벤트 최소화**: 필요한 이벤트만 등록
4. **블렌딩 시간 조정**: 너무 긴 블렌딩은 반응성 저하

## 언리얼 엔진과의 비교

| 기능 | 언리얼 | 유니티 (이 구현) |
|------|--------|------------------|
| Animation Sequence | Animation Sequence | AnimationSequence (ScriptableObject) |
| Animation Montage | Animation Montage | AnimationMontage (ScriptableObject) |
| Sections | Montage Sections | MontageSection |
| Slots | Animation Slots | Animancer Layers |
| Events | Animation Notifies | MontageEvent |
| Blend Spaces | Blend Trees | LinearMixerTransition |
| Play Montage | Play Montage (BP) | PlayMontage() |
| Jump to Section | Montage Jump To Section | JumpToSection() |

## 디버깅

### Animancer 인스펙터 활용
1. Play Mode에서 AnimancerComponent 확인
2. 현재 재생 중인 애니메이션 확인
3. 가중치(Weight), 속도(Speed), 시간(Time) 실시간 확인

### 로그 확인
```csharp
Debug.Log($"현재 상태: {controller.GetCurrentState()}");
Debug.Log($"Montage 재생 중: {controller.IsPlayingMontage()}");
Debug.Log($"이동 가능: {controller.CanMove()}");
```

## 추가 확장 가능 기능

1. **Root Motion 지원**
   - AnimationSequence에 Enable Root Motion 옵션 추가
   - AnimancerComponent의 Apply Root Motion 활성화

2. **IK (Inverse Kinematics)**
   - Animancer의 IK 시스템 통합
   - 발 IK, 손 IK 등 구현

3. **Animation Retargeting**
   - 동일한 Skeleton을 사용하는 다른 캐릭터에 애니메이션 재사용

4. **Networked Multiplayer**
   - Montage 재생 상태 동기화
   - 섹션 점프 동기화
