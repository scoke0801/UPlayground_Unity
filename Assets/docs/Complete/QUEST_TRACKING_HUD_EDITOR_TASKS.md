# 퀘스트 추적 HUD 에디터 작업

## 목적

퀘스트 메뉴에서 퀘스트를 추적하면 `UI_HUD_Quest`에 해당 퀘스트가 표시되고, 퀘스트 완료 시 HUD에 짧은 달성 UI가 뜨도록 프리팹과 버튼 이벤트를 연결한다.

코드 구현은 완료되어 있으며, 이 문서는 Unity 에디터에서 남은 연결 작업만 다룬다.

## 대상 프리팹

- `Assets/03.Prefabs/UI/HUD/Quest/UI_HUD_Quest.prefab`
- `Assets/03.Prefabs/UI/Scene/Quest/UI_Scene_QuestMenu.prefab`
- 퀘스트 목록/슬롯 프리팹이 별도로 분리되어 있다면 해당 슬롯 프리팹

## 1. UI_HUD_Quest 완료 알림 패널 추가

`UI_HUD_Quest.prefab`을 연다.

루트 또는 기존 퀘스트 표시 패널과 같은 계층에 다음 오브젝트를 추가한다.

```text
UI_HUD_Quest
└── QuestCompletePanel
    ├── QuestCompleteTitleText
    └── QuestCompleteNameText
```

권장 구성:

- `QuestCompletePanel`
  - `RectTransform`: HUD에서 퀘스트 표시 영역 근처 또는 화면 상단/우측
  - `CanvasGroup` 추가
  - 기본 Active 상태는 꺼둔다
- `QuestCompleteTitleText`
  - TextMeshProUGUI
  - 문구는 런타임에 `퀘스트 달성`으로 설정됨
- `QuestCompleteNameText`
  - TextMeshProUGUI
  - 완료된 퀘스트 이름이 런타임에 설정됨

`UI_HUD_Quest` 컴포넌트 인스펙터 연결:

- `Quest Complete Panel` → `QuestCompletePanel`
- `Quest Complete Canvas Group` → `QuestCompletePanel`의 `CanvasGroup`
- `Quest Complete Title Text` → `QuestCompleteTitleText`
- `Quest Complete Name Text` → `QuestCompleteNameText`
- `Quest Complete Show Seconds` → 기본 `2.5`초 권장

오브젝트 이름을 위와 동일하게 만들면 자동 캐싱도 동작하지만, 프리팹 안정성을 위해 인스펙터에 직접 연결한다.

## 2. 기존 퀘스트 HUD 텍스트 연결 확인

`UI_HUD_Quest` 컴포넌트에서 아래 필드가 연결되어 있는지 확인한다.

- `Quest Title Text`
- `Quest Desc Text`

미연결이면 프리팹 내 기존 텍스트 오브젝트를 연결한다. 이름이 각각 `QuestTitleText`, `QuestDescText`라면 자동 캐싱도 동작한다.

## 3. 퀘스트 메뉴 추적 버튼 연결

`UI_Scene_QuestMenu.prefab` 또는 퀘스트 슬롯 프리팹에서 추적 버튼을 찾는다.

버튼 `OnClick`에 다음 중 하나를 연결한다.

- 일반 추적 버튼: `UI_Scene_QuestMenu.TrackQuest(string questId)`
- 토글 버튼: `UI_Scene_QuestMenu.ToggleTrackQuest(string questId)`
- 추적 해제 버튼: `UI_Scene_QuestMenu.UntrackQuest()`

권장 방식은 `ToggleTrackQuest(string questId)`이다. 같은 퀘스트를 다시 누르면 추적 해제되고, 다른 퀘스트를 누르면 추적 대상이 바뀐다.

퀘스트 슬롯이 동적으로 생성되는 구조라면 슬롯 스크립트에서 버튼 클릭 시 현재 슬롯의 `questId`를 `QuestManager.Instance.TrackQuest(questId)` 또는 `ToggleTrackQuest(questId)`로 전달한다.

## 4. 퀘스트 메뉴 표시 상태 작업

퀘스트 슬롯 UI에 추적 중 상태를 표시할 수 있으면 다음 상태를 추가한다.

- 추적 중 아이콘 또는 텍스트
- 추적 버튼 선택 상태
- 추적 해제 상태

상태 판정은 런타임 코드에서 다음 API를 사용한다.

```csharp
QuestManager.Instance.IsQuestTracked(questId)
```

이 작업은 필수는 아니다. HUD 표시는 추적 API만 호출되면 동작한다.

## 5. 완료 알림 연출 확인

`QuestCompletePanel`에 Animator를 붙여도 되고, 단순 `CanvasGroup` 표시만 사용해도 된다.

현재 코드 동작:

- `QuestEvent.QuestCompleted` 수신
- HUD 내용을 먼저 새로고침
- `QuestCompletePanel` 활성화
- 제목/퀘스트 이름 텍스트 설정
- `Quest Complete Show Seconds` 이후 패널 비활성화

애니메이션을 넣을 경우에도 패널은 코드에서 Active on/off를 제어하므로, Animator 초기 상태가 비활성 패널 표시와 충돌하지 않도록 한다.

## 6. 플레이 모드 검증

다음 순서로 확인한다.

1. 퀘스트를 2개 이상 수락한다.
2. 퀘스트 메뉴에서 두 번째 퀘스트를 추적한다.
3. HUD `HudQuest`에 선택한 퀘스트 제목과 목표가 표시되는지 확인한다.
4. 목표 진행도를 올렸을 때 HUD 카운트가 갱신되는지 확인한다.
5. 추적 중인 퀘스트를 완료한다.
6. `퀘스트 달성` UI와 완료 퀘스트 이름이 표시되는지 확인한다.
7. 완료 후 다른 활성 퀘스트가 있으면 HUD가 다음 퀘스트로 전환되는지 확인한다.
8. 저장 후 로드했을 때 추적 퀘스트가 유지되는지 확인한다.

## 참고

- 새 UI 프리팹 키는 추가하지 않았다. 기존 `HudQuest` 프리팹 내부 확장만 필요하다.
- `UIKeyType` 재생성은 필요 없다.
- Addressables 항목 추가도 필요 없다.
- `QuestCompletePanel`을 만들지 않으면 완료 이벤트는 수신되지만 달성 UI는 표시되지 않는다.
