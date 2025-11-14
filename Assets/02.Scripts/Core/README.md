# 애니메이션 시스템

## 개요

이 프로젝트는 Animancer를 기반으로 언리얼 엔진 스타일의 애니메이션 시스템을 구현합니다.

## 주요 클래스

### Animation Data
- **AnimationSequence.cs**: 단일 애니메이션 클립 (언리얼의 Animation Sequence)
- **AnimationMontage.cs**: 복합 애니메이션 (언리얼의 Animation Montage)

### Runtime Components
- **AnimationMontagePlayer.cs**: Montage 재생 컴포넌트
- **AdvancedCharacterAnimationController.cs**: 통합 애니메이션 컨트롤러

## 사용 방법

자세한 사용법은 [AnimationSystemGuide.md](./AnimationSystemGuide.md)를 참조하세요.

## 빠른 시작

1. **GameObject에 컴포넌트 추가**:
   ```
   - AnimancerComponent
   - AnimationMontagePlayer
   - AdvancedCharacterAnimationController
   ```

2. **Animation Sequence 생성**:
   ```
   Project → Create → Animation → Animation Sequence
   ```

3. **Animation Montage 생성**:
   ```
   Project → Create → Animation → Animation Montage
   ```

4. **스크립트에서 사용**:
   ```csharp
   // 이동
   controller.UpdateMovement(speed, isRunning);
   
   // 공격
   controller.PlayAttack();
   
   // 스킬
   controller.PlaySkill("FireballCast");
   ```

## 예제 스크립트

- **AnimationSystemExample.cs**: 기본 사용 예시
- **MontageEventExample.cs**: 이벤트 활용 예시

## 기존 시스템과의 차이

### 구 시스템 (CharacterAnimationController)
- 간단한 Play() 메서드만 제공
- 섹션, 이벤트 등 고급 기능 없음
- **[DEPRECATED]** - 새 프로젝트에서는 사용하지 마세요

### 신 시스템 (AdvancedCharacterAnimationController)
- Animation Sequence + Montage 지원
- 섹션 기반 동적 애니메이션 제어
- 이벤트 시스템
- 레이어 시스템
- 콤보, 스킬 등 복잡한 액션 구현 가능

## 참고 문서

- [완전한 가이드](./AnimationSystemGuide.md)
- [Animancer 공식 문서](https://kybernetik.com.au/animancer/docs/)
