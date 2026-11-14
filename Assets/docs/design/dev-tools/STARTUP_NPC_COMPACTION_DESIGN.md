# 시작 경로용 고정 NPC 비활성 메시 정리

- 상태/측정: [시작 경로 프로파일링](../../../../docs/work-status/startup-profiling.md)
- 기준: [NPC 외형 저작](../../guide/NPC_APPEARANCE_AUTHORING_GUIDE.md), [Editor 데이터 도구](../../../../.claude/rules/editor-data-tools.md)

## 목적과 경계

고정 외형 NPC가 사용하지 않는 메시의 렌더러까지 디더 재질을 준비하는 비용을 줄인다. 첫 카메라 근접 때 생성하도록 미루지 않는다. 활성 외형·모션·골격·본·소켓·행동·상호작용과 기존 GUID/유지 local fileID는 보존한다. 구매 모델·공유 메시·재질·텍스처는 수정하지 않는다.

기존 `NpcAppearancePrototype`의 순수 비활성 SkinnedMeshRenderer 잎만 후보로 삼는다. 활성 노드, 자식/다른 컴포넌트가 있는 노드, 런타임 외형 교체가 있는 모델은 제외한다. 애니메이션 바인딩과 유지 컴포넌트의 incoming ObjectReference를 검사해 참조받는 후보는 고정점까지 되살린다. 부모의 GameObject 활성화 곡선도 보호한다.

## 실행 순서

1. 현재 정규 원문/meta·GUID·dirty·변형 관계를 확인한다. 기존 staging 24종과 구매 원본은 보존한다.
2. 새 `Assets/03.Prefabs/Characters/StartupProfilingStaging/`에 남녀 사본을 생성한다. 기존 파일은 거절한다. 검사 근거는 `Logs/StartupProfiling/NpcCompaction/`에 보존한다.
3. 삭제 전후/저장 재로드 후 활성 renderer·mesh/material·골격/avatar·유지 ObjectReference를 비교한다. idle/walk/talk의 정면/측면 렌더, 같은 조건의 생성 및 재질 준비 비용도 비교한다.
4. 정규 반영 전에는 source GUID/path와 유지 local fileID·직렬화 참조, 씬/다른 에셋의 외부 참조를 추가로 검사한다. 사본 파일을 정규 파일에 덮어쓰거나 정규 prefab을 재생성하지 않는다.
5. 정규 원문/meta를 백업하고 입력 해시가 같은 경우에만 해당 잎을 같은 정규 경로에서 제거한다. Undo group을 유지하며 저장 실패/후검사 실패 시 그룹 전체와 기록한 원문을 복구한다. 중간 쓰기 실패를 주입해 원문/meta 해시 복구를 먼저 증명한다.
6. 같은 경로·Profiler 영역·기록/내보내기·캐시 조건의 전후 실행과 기존 관련 회귀 검사를 수행한다. 각 단계 상태/근거/링크를 갱신한다.

## 판정

staging 보존·이미지 동일·분리 측정은 정규 반영이나 전체 로딩 개선의 증거가 아니다. canonical 반영 게이트가 실패하면 원본을 유지하고 원인을 남긴다. 실제 입력·카메라·대화·파티 교체·미술 품질은 사용자 테스트 대기로 구분한다. Player/Addressables 빌드·커밋은 하지 않는다.
