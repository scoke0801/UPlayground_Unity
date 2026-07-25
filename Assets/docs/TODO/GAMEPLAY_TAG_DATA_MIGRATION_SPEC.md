# GameplayTag 데이터화 마이그레이션 스펙

> 갱신일: 2026-07-25  
> 현재 상태: Registry 단일 원본과 데이터 기반 저작 전환 완료, 런타임 인터닝은 후속 검토

## 완료된 목표

- 저작용 `GameplayTagId` enum 제거
- Registry→C# 자동 생성 파이프라인 제거
- `GameplayTagRegistrySO`를 프로젝트 태그 정의의 단일 원본으로 전환
- Ability, Effect, Variant, Combo Route의 태그 필드를 `GameplayTag`로 통일
- Registry 기반 검색·계층형 `GameplayTagPropertyDrawer` 적용
- 미등록 문자열 생성 차단과 외부 문자열의 Registry 해석 강제
- Registry 중복·빈 값 및 직렬화된 미등록 `_tagName` 빌드 검증
- Registry/직렬화/C# 사용처 검색 도구
- 하위 태그 포함 충돌 검사·실패 복구형 안전 Rename
- State, Combo, Motion 태그를 하나의 Registry에 등록
- Core 어댑터 경계의 enum↔string 왕복 제거

현재 구조와 사용법은
`Assets/docs/Complete/GAMEPLAY_TAG_SYSTEM_GUIDE.md`를 기준으로 한다.

## 현재 원칙

1. Registry 에셋은 `Assets/Resources/GameplayTagRegistry.asset` 하나만 사용한다.
2. 콘텐츠 태그 추가는 데이터 변경이며 코드 생성이나 재컴파일을 요구하지 않는다.
3. SO·프리팹·씬 필드는 `GameplayTag`로 직렬화하고 PropertyDrawer에서 선택한다.
4. 외부 문자열은 `GameplayTagRegistry.TryResolve`를 통과해야 한다.
5. 코드에 고정된 상태 의미 슬롯만 `GameplayTags` 또는 `MotionTags` 편의 필드로 참조한다.
6. 정적 편의 필드는 Registry의 대체 원본이 아니며 콘텐츠 태그 추가 시 수정하지 않는다.

## 후속 검토 항목

### 안정 ID와 별칭

현재 안전 Rename은 Registry 정의와 발견 가능한 직렬화/C# 사용처를
트랜잭션으로 함께 치환한다. 장기 저장 데이터나 외부 DLC처럼 프로젝트
검색 범위 밖의 참조까지 이전 이름을 해석해야 할 필요가 생기면 다음을
별도 설계한다.

- Registry 항목별 안정 ID
- 이전 이름 aliases
- 외부 데이터 마이그레이션 버전
- 참조가 남은 태그 삭제 차단

### 런타임 인터닝

현재 `GameplayTag`와 Core `AbilityTagId`는 문자열 값을 보관한다.
Profiler에서 실제 병목이 확인될 때만 다음을 검토한다.

- Registry 로드 시 정수 핸들 테이블 구성
- 부모 핸들 사전 계산
- 컨테이너와 Core 집계기의 핸들 기반 비교
- `UPlayGround.Ability.Core`의 프로젝트 비의존 경계를 유지하는 resolver 포트

인터닝은 저작 모델을 enum이나 코드 생성 방식으로 되돌리는 이유가 되어서는 안 된다.

## 검증 기준

- Unity 스크립트 컴파일 오류 0
- Registry 에셋 정확히 1개
- 빈 이름·중복 이름·앞뒤 공백 0
- 직렬화된 미등록 `_tagName` 0
- Ability EditMode 및 PlayMode 수직 슬라이스 회귀 없음
- Registry 항목 추가만 수행했을 때 스크립트 컴파일이 발생하지 않음
