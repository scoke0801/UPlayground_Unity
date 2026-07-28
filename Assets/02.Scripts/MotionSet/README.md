# UPlayGround MotionSet

Animancer 기반 MotionSet 타임라인, 이벤트 실행 및 저작 프레임워크입니다.

프로젝트 내부 asmdef 모듈로 관리하며 Core와 Animancer 런타임은 UPlayground의
GameActor, Manager, Camera, UI 구현에 의존하지 않습니다.

## 요구 사항

- Unity 6.0 이상
- Animancer Pro 8.3.1

## asmdef 구조

- `UPlayGround.MotionSet.Core` — 데이터, Resolver, 이벤트 실행 계약
- `UPlayGround.MotionSet.Animancer` — Animancer 재생 커널과 범용 호스트
- `UPlayGround.MotionSet.Editor` — fallback Inspector와 이벤트 카탈로그
- `UPlayGround.MotionSet.Core.Tests` — Core EditMode 테스트

## 빠른 사용

1. Animator와 AnimancerComponent가 있는 GameObject에 `MotionSetPlayer`를 추가합니다.
2. MotionSetAsset을 할당합니다.
3. `Play()`를 호출하거나 `Play On Enable`을 켭니다.
4. 이벤트 대상을 바꾸려면 `SetEventTarget(GameObject)`를 호출합니다.

프로젝트 구체 이벤트는 `MotionEventBase`를 상속합니다. 필요하면
`MotionEventDescriptorAttribute`로 Editor 표시 이름과 범주를 지정할 수 있습니다.

## 다른 프로젝트로 이식

`Assets/02.Scripts/MotionSet` 폴더를 `.meta`와 함께 복사하고 Animancer를 설치하면 됩니다.
asmdef 이름과 네임스페이스를 유지하면 소비 프로젝트는 프로젝트별 어댑터만 추가하면 됩니다.
UPM 배포가 실제로 필요해지는 시점에는 이 폴더 구조를 그대로 패키지 루트로 감쌀 수 있습니다.
