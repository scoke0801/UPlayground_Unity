# 시작 경로의 GRD와 정적 배칭 대조

- 상태·실측: [시작 경로 프로파일링](../../../../docs/work-status/startup-profiling.md)
- 기준: [Editor 데이터 도구](../../../../.claude/rules/editor-data-tools.md), [테스트 지침](../../guide/TEST_VALIDATION_GUIDE.md)

## 목적과 경계

GPU Resident Drawer가 사용하는 프로젝트 소유 월드에서 정적 결합 메시의 분할 영향을 확인한다. 메시·재질·배치·부모·Collider·NavMesh·조명·그림자·LOD·컬링 거리·액터 ID를 보존한다. 구매 원본과 전역 Player/Quality 설정은 변경하지 않는다. Player/Addressables 빌드는 수행하지 않는다.

대상은 검증한 씬의 URP Lit MeshRenderer 중 BatchingStatic 비트가 설정되고 영구 원본 메시가 확인되는 개체다. 물리·조명 등 다른 static 비트를 유지한다. 액터/스테이지 이름으로 런타임 분기하거나 영구적인 런타임 메시 교체 코드를 추가하지 않는다.

## 사본 검증 게이트

1. 단일 Editor, Edit Mode, 대상 씬 비 dirty와 원문/meta 해시를 확인한다. 앞서 읽은 renderer/mesh 영구 ID 목록과 다시 일치해야 한다.
2. 원본은 ZIP에 보존한다. 별도 씬 사본에만 `BatchingStatic` 비트 해제를 저장한다. 같은 Editor의 Undo 그룹과 원래 플래그를 보존하며 실패/성공 후 원본 씬 메모리를 복구한다. 기존 파일은 덮어쓰지 않는다.
3. 사본의 YAML 문서 ID·나머지 필드·Prefab 수정 목록을 원문과 대조한다. 허용된 GameObject 플래그 변경과 이에 필요한 Prefab 플래그 override만 허용한다. 물리/조명 비트가 달라지거나 다른 참조·필드가 바뀌면 정규 반영을 중단한다. 검증을 편하게 하려고 알 수 없는 차이를 무시하지 않는다.
4. 사본을 Editor에서 다시 열어 대상 플래그·원본 메시·renderer ID·그 밖의 static 비트가 유지되는지 확인한다. 사본 생성과 검사 뒤에도 정규 원문/meta 해시는 동일해야 한다.

## 정규 반영 게이트

사본 검사를 통과한 바이트만 정규 경로에 적용한다. 원문/meta 백업과 입력 해시를 재확인하고 중간 쓰기 실패를 주입해 정확한 원문 복구를 먼저 증명한다. 적용 후 meta/GUID·허용 범위 외 필드·ID를 다시 확인한다. 실패 시 원문을 복구하고 Editor에서 재로드한다.

같은 경로와 계측 조건으로 실제 Boot→타이틀→새 게임→Raon→Loading→월드 완료, 마커 수집, 렌더 제출·드로우 수, 종료 해제를 각각 검증한다. Editor 수치를 Player 성능으로 해석하지 않는다. 실제 입력·카메라 이동·전체 맵 미술 품질은 사용자 테스트 대기로 남긴다. 재생성 도구가 다시 비트를 설정하는 경로와 GRD를 사용하지 않는 파이프라인에서는 재검토가 필요하다.
