# 가이드 팝업 시스템 가이드

이미지 또는 동영상을 포함한 튜토리얼/가이드 팝업을 페이지 단위로 표시하는 UI 시스템이다.

## 구성 파일

- `Assets/02.Scripts/Data/UI/GuidePopupDataSO.cs`
  - 가이드 팝업 데이터 ScriptableObject.
  - 여러 개의 `GuidePopupPage`를 리스트로 보관한다.
- `Assets/02.Scripts/UI/Scene/UI_Popup_Guide.cs`
  - 런타임 팝업 UI.
  - 이전/다음/닫기, 페이지 번호, 이미지/동영상 표시를 처리한다.
- `Assets/02.Scripts/UI/Scene/Editor/UIGuidePopupPrefabBuilder.cs`
  - `UI_Popup_Guide.prefab` 생성 및 `UIPrefabDatabase` 등록용 에디터 빌더.

## 프리팹 생성

Unity 상단 메뉴에서 실행한다.

```text
UPlayGround/UI/가이드 팝업 프리팹 빌드
```

실행 결과:

- `Assets/03.Prefabs/UI/Popup/UI_Popup_Guide.prefab` 생성 또는 갱신
- `Assets/10.Datas/Path/UIPrefabDatabase.asset`에 `GuidePopup` 키 등록

프리팹 구조나 SerializeField가 변경되면 이 메뉴를 다시 실행한다.

## 데이터 생성

Project 창에서 다음 메뉴로 데이터를 만든다.

```text
Create/UPlayGround/UI/Guide Popup Data
```

`GuidePopupDataSO`의 `Pages` 리스트에 페이지를 여러 개 추가할 수 있다.

각 페이지 필드:

- `Media Type`
  - `Image`: 스프라이트 이미지를 표시한다.
  - `Video`: `VideoClip`을 재생한다.
- `Image`
  - `Media Type = Image`일 때 표시할 Sprite.
- `Video`
  - `Media Type = Video`일 때 재생할 VideoClip.
- `Loop Video`
  - 동영상 반복 재생 여부.
- `Title`
  - 페이지 제목.
- `Body`
  - 페이지 본문. TextMeshPro rich text 사용 가능.

## 여러 페이지 사용

`Pages` 리스트에 페이지를 2개 이상 추가하면 팝업 하단에 `1/3`, `2/3`처럼 현재 페이지가 표시된다.

- `다음`: 다음 페이지로 이동
- 마지막 페이지의 `다음`: 팝업 닫기
- `이전`: 이전 페이지로 이동
- `X` 또는 Back 입력: 팝업 닫기

페이지가 바뀌면 이전 페이지의 동영상은 자동 정지된다.

## 런타임 호출

가이드 팝업을 띄운 뒤 `Setup`에 데이터를 넘긴다.

```csharp
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

public class GuidePopupExample : MonoBehaviour
{
    [SerializeField] private GuidePopupDataSO _guideData;

    public void OpenGuide()
    {
        var go = UIManager.Instance.ShowUI(UIKeyType.GuidePopup);
        go?.GetComponent<UI_Popup_Guide>()?.Setup(_guideData);
    }
}
```

특정 페이지부터 시작하려면 `startPageIndex`를 지정한다.

```csharp
go?.GetComponent<UI_Popup_Guide>()?.Setup(_guideData, startPageIndex: 1);
```

`startPageIndex`는 0부터 시작한다. `1`은 두 번째 페이지다.

표준 헬퍼를 사용하면 `ShowUI`와 `Setup`을 직접 나누지 않아도 된다.

```csharp
using UPlayGround.Data.UI;
using UPlayGround.UI.Guide;

public class GuidePopupExample : MonoBehaviour
{
    [SerializeField] private GuidePopupDataSO _guideData;

    public void OpenGuide()
    {
        GuidePopupRuntime.Open(_guideData);
    }
}
```

## 트리거에서 출력

`TriggerComposer`의 Action으로 가이드 팝업을 출력할 수 있다.

1. 씬 오브젝트에 `TriggerComposer`를 추가한다.
2. Source를 `플레이어가 영역에 들어오면`, `몬스터 그룹이 전멸하면` 등 원하는 타이밍으로 설정한다.
3. Action에서 `가이드 팝업 표시`를 생성한다.
4. 생성된 `ShowGuidePopupTriggerActionSO`의 `Guide Data`에 `GuidePopupDataSO`를 넣는다.
5. `Wait For Close`를 켜면 팝업이 닫힐 때까지 Sequence의 다음 Action 실행을 기다린다.

가이드 팝업은 표시 중 `BlocksLowerInput = true`로 하위 입력 레이어를 차단한다. 현재 프리팹은 `Popup` 레이어로 설정되어 있어 게임플레이 입력은 사용할 수 없고, 팝업의 다음/닫기 입력만 동작한다.

## 동영상과 GIF

Unity UI에서 GIF 직접 재생은 기본 지원이 안정적이지 않다.

권장 방식:

1. GIF를 mp4 또는 webm으로 변환한다.
2. Unity에 임포트해서 `VideoClip`으로 만든다.
3. 페이지의 `Media Type`을 `Video`로 설정한다.
4. `Video` 필드에 해당 클립을 넣는다.

짧은 반복 안내 영상은 `Loop Video`를 켜는 편이 좋다.

## 주의사항

- `GuidePopupDataSO`만 만들고 프리팹 빌더를 실행하지 않으면 `UIManager.ShowUI(UIKeyType.GuidePopup)`가 프리팹을 찾지 못한다.
- 동영상은 팝업용 안내 클립 기준으로 짧고 가벼운 해상도를 권장한다.
- `UI_Popup_Guide`은 표시 중 `GameTimeManager.SetPause(true)`를 호출한다. 팝업을 닫으면 다시 `false`로 복원한다.

## 최초 가이드 표시 타이밍 검증

`GameGuideManager`는 전투 상태 자체로 가이드를 미루지 않는다. 대화·시네마틱·영입 연출·로딩·다른 가이드·일시정지가 없으면 다음 매니저 갱신에서 표시한다. `PresentationSettleSeconds`는 실제로 이러한 상태를 기다린 경우에만 적용한다. 도시 길 안내의 별도 4m 이동 조건은 유지한다.

자동 검사 대상은 `FirstCompanionGuideTests`와 `CityRoadGuideTests`다. 실제 화면·입력 품질은 아래 사용자 테스트로 확인한다.

- 준비: Unity 6000.3.21f1의 `Assets/01.Scenes/GameLogic/Boot.unity`에서 Editor Play Mode로 시작한다. 기존 슬롯을 덮어쓰지 않는 별도 새 게임 슬롯과 게임패드를 사용한다. 각 최초 가이드를 이미 본 슬롯에서는 해당 항목을 판정할 수 없다.
- 상태: 아래 항목은 사용자 테스트 대기. 자동 검사만으로 화면 표시 시점과 패드 조작을 검증한 것으로 보지 않는다.

| ID | 실행 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| G01 | 새 게임에서 첫 적에게 감지되어 전투를 시작한다. | 전투 안내가 전투 종료를 기다리지 않고 표시된다. 적을 모두 잡은 뒤에야 나타나면 실패. |
| G02 | 첫 동료를 영입하고 필수 대화와 후속 대화를 끝낸다. | 대화·영입 연출 중에는 표시되지 않고, 종료 후 설정된 안정화 시간(기본 0.5초) 뒤 표시된다. 이동이나 다음 전투 종료를 요구하면 실패. |
| G03 | 장비 안내를 보지 않은 상태에서, 대화·연출·메뉴가 없는 전투 중 첫 장비를 획득한다. | 획득 직후 장비 안내가 표시된다. 전투 종료 또는 추가 이동까지 미뤄지면 실패. |
| G04 | 안내 대기 중 메뉴·대화·로딩이 있는 경로를 진행하고, 안내를 닫은 뒤 저장/로드한다. | 다른 화면과 겹치지 않으며 대기가 풀리면 순서대로 표시된다. 이미 본 안내는 로드 후 반복되지 않는다. 진행 성공과 Console 예외 유무를 각각 기록한다. |
| G05 | 각 안내에서 패드로 이전/다음 페이지를 이동하고 Cancel로 닫는다. | 초기 포커스가 있으며 일시정지 중에도 열기·닫기 트윈이 동작한다. 닫은 뒤 플레이 조작과 시간이 복원된다. |

실패 시 항목 ID, 최초 실행/로드 여부, 발생 당시 전투·대화·메뉴 상태, 입력 장치, 실제 지연 시간과 Console 로그를 전달한다. 회신 형식은 `ID / 통과·실패·미수행 / 실제 결과 / 화면·영상 또는 로그`다. Player 빌드는 이 검증에 필요하지 않다.
