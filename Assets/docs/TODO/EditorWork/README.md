# Unity 에디터 수작업 목록

TODO 문서와 현재 프로젝트 에셋을 대조해, 코드 작성이 아니라 Unity 에디터에서 직접 처리해야 하는 작업을 모은다.

## 권장 작업 순서

1. [사운드 시스템 에디터 작업](./SOUND_SYSTEM_EDITOR_TASKS.md)
   - 현재 `SoundDatabase`와 `AudioMixer` 에셋이 없어 key 기반 사운드와 믹서 라우팅이 동작하지 않는다.
2. [제작 시스템 에디터 작업](./CRAFTING_SYSTEM_EDITOR_TASKS.md)
   - `RecipeDatabase`는 준비되어 있지만 실제 제작 UI 프리팹과 슬롯 프리팹 구성이 필요하다.
3. [퀘스트 추적 HUD 에디터 작업](./QUEST_TRACKING_HUD_EDITOR_TASKS.md)
   - HUD 완료 알림과 퀘스트 메뉴 추적 조작을 연결한다.
4. [디버그 기즈모 시스템 에디터 작업](./DEBUG_GIZMO_SYSTEM_EDITOR_TASKS.md)
   - 설정 에셋과 Addressables 등록은 선택 사항이며, 기본값만으로도 동작한다.

## 현재 상태 요약

| 영역 | 코드 | 에셋/프리팹 | 사용자 작업 |
|---|---|---|---|
| 퀘스트 | HUD 추적·완료 이벤트 API 구현됨 | 기본 HUD 텍스트 연결됨, 완료 패널 없음 | 완료 패널 제작, 추적 버튼/상태 UI 연결, 플레이 검증 |
| 제작 | `RecipeManager`, `UI_Crafting`, 에디터 도구 구현됨 | DB와 Addressables 등록 완료, 기능 UI/슬롯 프리팹 미완성 | 제작 UI 재구성, 슬롯 프리팹 제작, 데이터 ID 보정 |
| 사운드 | Stage 1~5·7 구현됨 | `SoundDatabase`·`AudioMixer` 없음 | 에셋 생성, 키/클립 등록, 믹서 그룹·파라미터 구성, 플레이 검증 |
| 기즈모 | 중앙 매니저·창 구현됨 | 설정 에셋 없음 | 필요 시 설정 에셋 생성/등록, 표시·빌드 제외 검증 |

## 공통 완료 기준

- [ ] Unity Console 컴파일 에러 0개
- [ ] 프리팹 변경 사항 Apply 완료
- [ ] Addressables 주소의 대소문자와 코드 상수가 정확히 일치
- [ ] 플레이 모드에서 기능별 체크리스트 통과
- [ ] 새 에셋과 `.meta` 파일이 버전 관리 대상에 포함됨
