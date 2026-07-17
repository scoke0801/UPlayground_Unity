# 퀘스트 추적 HUD 에디터 작업

## 현재 확인된 상태

- `UI_HudQuest.prefab`의 `QuestTitleText`, `QuestDescText`와 스크립트 필드는 이미 연결되어 있다.
- 완료 알림용 `QuestCompletePanel`과 관련 참조는 프리팹에 없다.
- `UI_QuestMenu`에는 `TrackQuest`, `ToggleTrackQuest`, `UntrackQuest` API가 있다.
- 현재 `UI_QuestMenu` 스크립트는 퀘스트 목록을 동적으로 생성하지 않는다. 목록/슬롯 런타임 구성이 필요하다면 별도 코드 작업이 선행되어야 한다.
- 새 UI Addressables 키나 `UIKeyType` 재생성은 필요 없다.

## 대상

- `Assets/03.Prefabs/UI/HUD/Quest/UI_HudQuest.prefab`
- `Assets/03.Prefabs/UI/Scene/Quest/UI_QuestMenu.prefab`

## 1. 완료 알림 패널 제작

`UI_HudQuest.prefab`에 다음 계층을 추가한다.

```text
UI_HudQuest
└── QuestCompletePanel
    ├── QuestCompleteTitleText
    └── QuestCompleteNameText
```

- [ ] `QuestCompletePanel`에 `CanvasGroup` 추가
- [ ] HUD의 퀘스트 영역 근처 또는 화면 우측 상단에 배치
- [ ] 기본 Active를 끔
- [ ] `QuestCompleteTitleText`를 `TextMeshProUGUI`로 생성
- [ ] `QuestCompleteNameText`를 `TextMeshProUGUI`로 생성

`UI_HudQuest` 인스펙터 연결:

- [ ] `Quest Complete Panel` → `QuestCompletePanel`
- [ ] `Quest Complete Canvas Group` → 패널의 `CanvasGroup`
- [ ] `Quest Complete Title Text` → `QuestCompleteTitleText`
- [ ] `Quest Complete Name Text` → `QuestCompleteNameText`
- [ ] `Quest Complete Show Seconds` → `2.5`

오브젝트 이름 기반 자동 탐색이 있어도 프리팹 안정성을 위해 직접 연결한다.

## 2. 완료 연출 설정

- [ ] 단순 표시만 쓸 경우 패널의 최종 알파를 `1`로 확인
- [ ] Animator를 추가할 경우 패널 Active on/off를 코드가 담당한다는 점을 유지
- [ ] Animator 초기 상태가 패널을 임의로 다시 활성화하지 않도록 설정
- [ ] 긴 퀘스트 이름이 잘리지 않도록 TMP Auto Size 또는 줄바꿈 범위 확인

## 3. 퀘스트 메뉴 추적 조작 연결

현재 메뉴 프리팹에서 실제로 사용할 추적 버튼 또는 토글을 정한다.

- [ ] 선택된 퀘스트를 추적하는 버튼 추가 또는 기존 버튼 용도 확정
- [ ] 고정 퀘스트 버튼이면 `OnClick`에 `UI_QuestMenu.ToggleTrackQuest(string)` 연결
- [ ] 함수 인수에 실제 `questId` 문자열 입력
- [ ] 별도 해제 버튼을 둘 경우 `UI_QuestMenu.UntrackQuest()` 연결

동적 퀘스트 슬롯을 사용할 경우 Unity 인스펙터의 고정 문자열로 처리하지 말고, 슬롯 코드가 현재 데이터의 `questId`를 전달해야 한다. 현재 메뉴에는 이 슬롯 생성 코드가 없으므로 에디터 연결만으로는 동적 목록이 완성되지 않는다.

## 4. 추적 상태 표시

선택 사항이지만 메뉴 사용성을 위해 권장한다.

- [ ] 추적 중 아이콘 또는 텍스트 추가
- [ ] 추적 버튼의 선택/해제 시각 상태 제작
- [ ] 다른 퀘스트 추적 시 이전 슬롯 상태가 해제되는지 확인

상태 판정 API:

```csharp
QuestManager.Instance.IsQuestTracked(questId)
```

## 5. 플레이 모드 검증

- [ ] 퀘스트 2개 이상 수락
- [ ] 두 번째 퀘스트를 추적했을 때 HUD 제목과 목표가 변경됨
- [ ] 목표 진행 시 HUD 카운트가 갱신됨
- [ ] 같은 퀘스트를 다시 선택하면 추적 해제됨
- [ ] 추적 중 퀘스트 완료 시 `퀘스트 달성`과 퀘스트 이름이 약 2.5초 표시됨
- [ ] 완료 후 다른 활성 퀘스트가 있으면 HUD가 다음 표시 대상으로 전환됨
- [ ] 활성 퀘스트가 없을 때 HUD가 숨겨짐
- [ ] 저장 후 로드해도 추적 대상이 유지됨
- [ ] Console 에러와 이벤트 중복 호출이 없음

## 완료 판정

- 완료 패널이 프리팹에 존재하고 모든 직렬화 필드가 연결되어야 한다.
- 고정 퀘스트 UI라면 버튼으로 추적/해제가 가능해야 한다.
- 동적 목록이 목표라면 별도 슬롯 코드 구현 전에는 이 작업을 완료로 표시하지 않는다.
