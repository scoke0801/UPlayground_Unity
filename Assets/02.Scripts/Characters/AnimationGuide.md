# TPS 액션 RPG 애니메이션 시스템 사용 가이드

## 오류 수정 완료

Animancer Lite v8에서 이벤트 시스템이 변경되어 코루틴 기반으로 애니메이션 완료를 감지하도록 수정되었습니다.

### 수정된 부분

**이전 (오류 발생)**:
```csharp
jumpState.Events.OnEnd = () => PlayIdle(); // ❌ 오류
```

**수정 후 (정상 작동)**:
```csharp
StartCoroutine(WaitForAnimationEnd(jumpState, () => PlayIdle())); // ✅ 정상
```

## 빠른 테스트 방법

1. **캐릭터 설정**:
   - 빈 GameObject 생성
   - `CharacterController` 컴포넌트 추가
   - `AnimancerComponent` 컴포넌트 추가
   - `CharacterAnimationController` 스크립트 추가
   - `SimpleCharacterController` 스크립트 추가

2. **애니메이션 클립 할당**:
   - Inspector에서 각 애니메이션 클립을 드래그&드롭으로 할당
   - 없는 클립은 비워두어도 됨 (null 체크가 되어 있음)

3. **테스트 실행**:
   - Play 버튼 누르기
   - **WASD**: 이동 (자동으로 Walk/Run 애니메이션)
   - **Shift+WASD**: 달리기
   - **Space**: 점프 (완료 후 자동으로 Idle)
   - **좌클릭**: 공격 (완료 후 자동으로 Idle)

## 주요 기능

### 자동 상태 관리
- 이동 속도에 따라 자동으로 Walk/Run 선택
- Jump, Attack 애니메이션 완료 후 자동으로 Idle 복귀
- 부드러운 애니메이션 블렌딩

### 디버그 기능
- 게임 화면에 실시간 상태 표시
- 테스트 키로 강제 애니메이션 실행 (1,2,3키)
- Console에 애니메이션 상태 로그

### 코드 사용 예시

```csharp
// 애니메이션 컨트롤러 참조
CharacterAnimationController animController = GetComponent<CharacterAnimationController>();

// 기본 사용
animController.PlayIdle();
animController.PlayMovement(moveSpeed); // 속도에 따른 자동 선택

// 원샷 애니메이션 (완료 후 자동으로 Idle)
animController.PlayJump();
animController.PlayAttack();

// 상태 확인
bool isJumping = animController.GetCurrentState() == CharacterAnimationController.AnimationState.Jumping;
float progress = animController.GetCurrentAnimationProgress();
```

## 확장 방법

### 새로운 애니메이션 추가
1. `AnimationState` enum에 상태 추가
2. 해당 클립 필드 추가
3. 재생 메소드 구현

```csharp
// 예: 방어 애니메이션 추가
public enum AnimationState
{
    // 기존...
    Defending // 새로 추가
}

[SerializeField] private AnimationClip defendClip;

public void PlayDefend()
{
    if (defendClip != null)
    {
        currentState = animancer.Play(defendClip, transitionDuration);
        currentAnimationState = AnimationState.Defending;
    }
}
```

## 문제 해결

### 컴파일 오류
- ✅ `Events` 관련 오류: 이미 수정됨 (코루틴 사용)
- ✅ using 문 추가됨: `System.Collections`, `System`

### 런타임 문제
- **애니메이션이 재생되지 않음**: 클립이 할당되었는지 확인
- **부드럽지 않은 전환**: `transitionDuration` 값 조정 (0.5초 정도)
- **이동이 어색함**: 이동 속도와 애니메이션 속도 맞춤

### 성능 최적화
- 필요하지 않은 애니메이션 클립은 null로 유지
- 디버그 기능은 빌드시 자동으로 제거됨 (`#if UNITY_EDITOR`)

---

**이제 오류 없이 정상 작동합니다! 🎉**
