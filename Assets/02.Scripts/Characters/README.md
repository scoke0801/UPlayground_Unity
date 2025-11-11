# TPS 액션 RPG 애니메이션 시스템

Animancer Lite를 기반으로 한 간단한 애니메이션 처리 컴포넌트입니다.

## 구성 요소

### 1. CharacterAnimationController
- Animancer를 사용한 애니메이션 제어
- 기본 애니메이션: Idle, Walk, Run, Jump, Attack
- 자동 블렌딩과 상태 관리

### 2. SimpleCharacterController
- 테스트용 캐릭터 컨트롤러
- WASD 이동, 점프, 공격 기능
- 애니메이션과 자동 연동

## 설정 방법

### 1. 캐릭터 오브젝트 준비
1. 캐릭터 모델 오브젝트 생성
2. `AnimancerComponent` 추가
3. `CharacterController` 추가
4. `CharacterAnimationController` 스크립트 추가
5. `SimpleCharacterController` 스크립트 추가 (테스트용)

### 2. 애니메이션 클립 할당
`CharacterAnimationController`의 Inspector에서:
- **기본 애니메이션**
  - Idle Clip: 대기 애니메이션
  - Walk Clip: 걷기 애니메이션  
  - Run Clip: 달리기 애니메이션
- **액션 애니메이션**
  - Jump Clip: 점프 애니메이션
  - Attack Clip: 공격 애니메이션
- **설정**
  - Walk Speed: 걷기 속도 기준값 (기본: 2)
  - Run Speed: 달리기 속도 기준값 (기본: 5)
  - Transition Duration: 애니메이션 전환 시간 (기본: 0.3초)

### 3. 캐릭터 컨트롤러 설정
`SimpleCharacterController`의 Inspector에서:
- Walk Speed: 실제 걷기 속도
- Run Speed: 실제 달리기 속도
- Jump Height: 점프 높이
- Gravity: 중력 값
- Rotation Speed: 회전 속도

## 사용법

### 기본 조작
- **WASD**: 이동
- **Shift + WASD**: 달리기
- **Space**: 점프
- **좌클릭**: 공격

### 테스트 키 (에디터 전용)
- **1**: 강제로 Idle 애니메이션
- **2**: 강제로 Walk 애니메이션
- **3**: 강제로 Run 애니메이션

### 코드에서 사용하기

```csharp
// 애니메이션 컨트롤러 참조
CharacterAnimationController animController = GetComponent<CharacterAnimationController>();

// 기본 애니메이션 재생
animController.PlayIdle();
animController.PlayWalk();
animController.PlayRun();

// 속도에 따른 자동 애니메이션
float moveSpeed = 3.0f;
animController.PlayMovement(moveSpeed); // 속도에 따라 Walk/Run 자동 선택

// 액션 애니메이션 (원샷)
animController.PlayJump();  // 완료 후 자동으로 Idle 복귀
animController.PlayAttack(); // 완료 후 자동으로 Idle 복귀

// 상태 확인
AnimationState currentState = animController.GetCurrentState();
float progress = animController.GetCurrentAnimationProgress();
bool isPlaying = animController.IsPlaying();

// 애니메이션 속도 조정
animController.SetAnimationSpeed(1.5f); // 1.5배속 재생
```

## 확장 방법

### 새로운 애니메이션 추가
1. `AnimationState` enum에 새로운 상태 추가
2. Inspector에 새로운 클립 필드 추가
3. 해당 애니메이션을 재생하는 public 메소드 추가

예시:
```csharp
// 1. enum 추가
public enum AnimationState
{
    Idle, Walking, Running, Jumping, Attacking,
    Blocking // 새로운 상태 추가
}

// 2. 필드 추가
[SerializeField] private AnimationClip blockClip;

// 3. 메소드 추가
public void PlayBlock()
{
    if (blockClip != null)
    {
        currentState = animancer.Play(blockClip, transitionDuration);
        currentAnimationState = AnimationState.Blocking;
    }
}
```

### 애니메이션 이벤트 활용
```csharp
// 애니메이션 이벤트 등록 예시
var attackState = animancer.Play(attackClip, transitionDuration);
attackState.Events.Add(0.5f, () => {
    // 애니메이션 50% 지점에서 데미지 적용
    ApplyDamage();
});
```

## 주의사항

1. **Animancer 의존성**: 이 시스템은 Animancer Lite가 필요합니다.
2. **애니메이션 클립**: 모든 애니메이션 클립이 할당되어야 정상 작동합니다.
3. **CharacterController**: 물리 기반 이동을 위해 CharacterController가 필요합니다.
4. **테스트 키**: 에디터에서만 작동하며, 빌드시에는 제거됩니다.

## 트러블슈팅

### 애니메이션이 재생되지 않는 경우
1. AnimancerComponent가 올바르게 추가되었는지 확인
2. 해당 애니메이션 클립이 Inspector에 할당되었는지 확인
3. Console에서 오류 메시지 확인

### 부드럽지 않은 전환
1. Transition Duration 값을 늘려보세요 (0.5초 정도)
2. 애니메이션 클립의 Loop Time 설정 확인

### 이동이 어색한 경우
1. Walk Speed와 Run Speed 값을 애니메이션에 맞게 조정
2. CharacterController의 설정값들 확인

## 향후 개선사항

- [ ] 애니메이션 블렌드 트리 지원
- [ ] 레이어 기반 애니메이션 (상체/하체 분리)
- [ ] 애니메이션 이벤트 시스템 확장
- [ ] 상태머신 패턴 적용
- [ ] 애니메이션 압축 및 최적화

---

*최소한의 기능으로 시작하여 필요에 따라 점진적으로 확장하는 것을 권장합니다.*
