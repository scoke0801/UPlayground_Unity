# 대화 다이얼로그 시스템 고도화 스펙

> 작성일: 2026-07-23
> 대상 버전: Unity 6 (6000.0.60f1), TextMeshPro, URP
> 분류: TODO 구현 스펙
> 적용 범위: Main/Monologue/System 대화 UI, 재생 제어(정지·자동·스킵), 대화 이력, 인라인 텍스트 색상
> 관련 코드:
>
> - `Assets/02.Scripts/Manager/Dialogue/DialogueManager.cs` (DialogueRunner 포함)
> - `Assets/02.Scripts/Data/Dialogue/DialogueNodeSO.cs`
> - `Assets/02.Scripts/Data/Dialogue/SpeakerColorTableSO.cs`
> - `Assets/02.Scripts/UI/Dialogue/UI_Dialogue.cs`
> - `Assets/02.Scripts/UI/Dialogue/UI_MonologueDialogue.cs`
> - `Assets/02.Scripts/UI/Dialogue/UI_SystemDialogue.cs`
> - `Assets/02.Scripts/Contracts/GameServices.cs` (`IDialogueService`)
> - `Assets/02.Scripts/UI/Contracts/UIServices.cs` (`IUIDialogueService`)

---

## 1. 목적

레퍼런스 스크린샷의 좌상단 컨트롤 바(⏸ 정지 / 💬 대화 이력)와 명조·페르소나류 대화 UX를 기준으로, 현재의 "이벤트 구독 → 한 글자씩 출력 → 클릭으로 진행" 수준의 대화 UI를 다음 5개 기능으로 확장한다.

1. **정지(Pause)** — 대화 진행·자동 재생을 일시 정지하고 재생 제어 상태를 노출
2. **자동 재생(Auto)** — 타이핑 완료 후 일정 시간 뒤 자동으로 다음 노드로 진행, 토글 유지
3. **이전 대화내역 보기(Backlog)** — 지금까지 출력된 대사를 화자·본문 단위로 되짚어 보는 스크롤 로그
4. **스킵(Skip)** — 현재 타이핑 즉시 완성, 나아가 그래프를 선택지/끝까지 빨리감기
5. **인라인 색상(Rich Text)** — 한 대사 안에서 특정 단어·수치에 색상을 부여 (예: `우리 용병단은 총 <color>4명</color>이다!`)

이 문서는 위 기능을 **채널 공용 재생 제어 계층**으로 통합하는 것을 목표로 하며, 채널별 UI(Main/Monologue/System)가 이를 공유하도록 설계한다.

---

## 2. 현재 구조와 제약

### 2.1 흐름 제어

- `DialogueManager`가 채널별 `DialogueRunner`(내부 FSM)를 보유한다. Main/System은 단일 실행, Monologue는 큐 순차.
- `DialogueRunner.Advance()` / `SelectChoice()`가 노드 전이를 담당하며, 노드 진입 시 `NotifyNodeEnter` 이벤트로 UI에 통지한다.
- UI(`UI_Dialogue` 등)는 이벤트를 구독해 **뷰만** 그린다. 흐름 제어는 매니저에 위임한다. 이 경계는 유지한다.

### 2.2 반드시 해결해야 할 결함

| 항목 | 현재 상태 | 문제 |
|------|-----------|------|
| 타이핑 방식 | `dialogueBodyText.text += c` 한 글자씩 문자열 누적 | **리치 텍스트와 근본적으로 충돌.** `<color=...>` 태그가 본문 문자처럼 한 글자씩 노출됨. 인라인 색상 구현의 선결 과제 |
| 자동 진행 | `autoAdvanceDuration`이 Monologue엔 배선, **Main(`UI_Dialogue`)엔 미배선** | Main 채널은 노드에 값을 넣어도 자동 진행 안 됨. 자동 재생 기능의 토대가 채널별로 불일치 |
| 재생 상태 | 전역 정지/자동/스킵 상태 없음 | 각 기능이 개별 코루틴 지역 변수에만 존재. 컨트롤 바가 참조할 공용 상태 부재 |
| 대화 이력 | 저장소 없음 | 지나간 대사를 되짚을 데이터가 남지 않음 |
| 타이핑 코드 중복 | `UI_Dialogue`, `UI_MonologueDialogue`가 거의 동일한 `TypeText` 코루틴을 각자 보유 | 고도화 시 두 곳을 동일하게 수정해야 함. 공용화 필요 |

### 2.3 설계 원칙

1. **매니저=흐름·상태, UI=뷰** 경계 유지. 재생 제어 상태는 매니저(또는 매니저가 소유한 재생 컨트롤러)에 두고 UI는 구독·명령만 한다.
2. 타이핑·자동·스킵·색상 파싱은 **채널 공용 컴포넌트**로 추출해 Main/Monologue가 공유한다.
3. 데이터(`DialogueNodeSO`)는 하위 호환을 유지한다. 신규 필드는 기본값이 기존 동작과 같아야 한다.

---

## 3. 기능별 설계

### 3.1 인라인 텍스트 색상 (선결 과제)

색상은 다른 기능(타이핑·스킵·이력)이 모두 의존하므로 **가장 먼저** 처리한다.

**저작 형식.** 대사 본문에 태그를 직접 쓰지 않고, 색상 키를 참조하는 커스텀 마크업을 사용해 색을 데이터로 중앙 관리한다.

```
우리 용병단은 류트 널 포함해서 총 [c:emphasis]4명[/c]이다!
```

- `[c:key]...[/c]` 마크업의 `key`는 `SpeakerColorTableSO`를 확장한 **색상 팔레트**에서 조회한다(§3.1.1).
- 저작 편의를 위해 TMP 원시 태그(`<color=#RRGGBB>`)도 그대로 허용하되, 팔레트 키 방식을 권장 경로로 문서화한다. 팔레트 키를 쓰면 톤 조정이 에셋 한 곳에서 끝난다.
- 파싱 단계에서 `[c:key]` → `<color=#RRGGBB>`, `[/c]` → `</color>`로 치환해 TMP 리치 텍스트 문자열을 만든다.

**타이핑을 `maxVisibleCharacters`로 전환 (핵심).**

```csharp
// 1) 전체 리치 텍스트를 한 번에 설정 (태그 포함)
bodyText.text = DialogueMarkup.ToRichText(node.dialogueText, palette);
bodyText.ForceMeshUpdate();
int total = bodyText.textInfo.characterCount; // 태그 제외한 '보이는' 글자 수

// 2) 보이는 글자 수만 0→total로 증가
bodyText.maxVisibleCharacters = 0;
for (int i = 0; i <= total; i++)
{
    bodyText.maxVisibleCharacters = i;
    yield return WaitTyping(speed); // §3.2의 정지/속도 반영
}
```

이 방식은 리치 텍스트 태그를 절대 노출하지 않으며, 스킵(§3.4)은 `maxVisibleCharacters = total` 한 줄로 끝난다. 기존 `text += c` 경로는 완전히 대체한다.

#### 3.1.1 색상 팔레트 확장

`SpeakerColorTableSO`는 화자→색만 있으므로, **의미 키→색** 팔레트를 추가한다. 별도 SO를 신설하거나 기존 SO에 `List<NamedColorEntry> palette`를 더한다(권장: 신설 `DialoguePaletteSO`로 책임 분리, Addressables 키 고정).

```csharp
public sealed class DialoguePaletteSO : ScriptableObject
{
    public const string AddressableKey = "DialoguePalette";
    // key 예: "emphasis"(주황), "item"(청록), "danger"(적) ...
    [SerializeField] List<NamedColorEntry> entries;
    public bool TryGet(string key, out Color color);
}
```

- 등록되지 않은 키는 기본색으로 폴백하고 에디터에서만 경고 로그를 남긴다(런타임 스팸 금지).
- `DialogueManager`가 컬러 테이블과 동일하게 Addressables로 로드해 UI에 제공한다(`IUIDialogueService`에 getter 추가).

### 3.2 정지(Pause)

레퍼런스의 ⏸ 아이콘. **대화 컨텍스트의 정지**로 정의한다(게임 전체 일시정지 메뉴와 구분).

- 재생 컨트롤러에 `bool IsPaused` 상태를 둔다. 정지 시:
  - 타이핑 진행 대기(`WaitTyping`)가 멈춘다.
  - 자동 재생 카운트다운이 멈춘다.
  - 입력 기반 수동 진행(클릭/키)은 정책 선택: **정지 중엔 진행 입력도 무시**를 기본으로 한다(정지의 의미가 명확).
- 컨트롤 바 버튼과 별개로, `Time.timeScale`을 건드리지 않는다. 대화 카메라 녹화 재생(§DialogueManager `PushDialogueCameraRecording`)이 timeScale 의존일 수 있으므로 결합하지 않는다. 필요 시 정지 상태를 카메라 재생에 전달하는 것은 별도 후속 과제로 남긴다.
- 재생 컨트롤러가 매니저 소유이므로, 채널 전환·대화 종료 시 정지 상태를 리셋한다.

### 3.3 자동 재생(Auto)

- 재생 컨트롤러에 `bool IsAuto` 토글을 둔다(대화 세션 간 유지 여부는 설정값; 기본 세션 내 유지, 대화 종료 시 유지).
- 자동 재생 시 진행 규칙:
  1. 타이핑 완료를 기다린다.
  2. `max(node.autoAdvanceDuration, globalAutoDelay)` 만큼 대기 후 `Advance()`.
  3. `NodeType.Choice` 노드에서는 **자동 진행하지 않고 멈춘다**(선택은 플레이어 몫).
- `autoAdvanceDuration` 배선을 **Main 채널에도** 추가해 채널 간 동작을 통일한다(현재 Monologue만 있음).
- 전역 자동 딜레이는 설정(`SettingsManager`)에서 조정 가능하게 노출한다(예: 느림/보통/빠름).

### 3.4 스킵(Skip)

두 단계로 구분한다.

1. **타이핑 스킵(약):** 타이핑 중 진행 입력 1회 → `maxVisibleCharacters = total`로 즉시 완성(다음 노드로 넘어가지 않음). 이는 현재도 흔히 기대되는 동작이며 신규로 명시 구현한다.
2. **대화 스킵(강):** 컨트롤 바/전용 입력 → `DialogueRunner`가 **선택지 또는 End를 만날 때까지** 노드를 연속 진행한다. Runner에 `SkipToBreak()`를 추가한다:
   - `Talk`/`Event`/`Condition` 노드는 이벤트 액션(`eventActions`)을 **정상 실행하며** 통과한다(플래그·퀘스트 부작용 보존).
   - `Choice` 노드에서 멈추고 선택지를 제시한다.
   - `End`에서 정상 종료한다.
   - 스킵 중 UI 타이핑은 생략하고 마지막 통과 노드만 즉시 표기하거나, 곧바로 다음 상태(선택지/종료)로 전환한다.
- 스킵 안전장치: 무한 루프 방지를 위해 최대 노드 전이 횟수 상한(예: 512)과 순환 감지를 둔다.

### 3.5 이전 대화내역 보기(Backlog)

- **저장 위치:** `DialogueManager`(또는 재생 컨트롤러)가 링 버퍼 `List<DialogueLogEntry>`를 보유한다. `DialogueRunner`가 `Talk`/`Choice` 노드 진입을 매니저에 통지하는 기존 경로에서 함께 기록한다.
- **엔트리 구조:**
  ```csharp
  public readonly struct DialogueLogEntry
  {
      public string SpeakerName;   // 해석된 표시명(플레이어 활성 캐릭터 반영)
      public string RichBody;      // 색상 태그 포함 최종 문자열
      public DialogueChannel Channel;
      public Sprite Portrait;      // 선택: 로그에 소형 초상화
  }
  ```
  - 화자명·초상화 해석 로직은 현재 `UI_Dialogue.ResolveSpeakerName/ResolvePortrait`에 있다. 로그가 뷰와 동일한 해석을 쓰도록, 해석 결과를 이벤트 페이로드에 포함하거나 해석 유틸을 공용화한다.
- **용량:** 런 단위 상한(예: 최근 100개). 대화 세션 시작 시 유지/초기화 정책은 프로젝트 관례에 맞춘다(권장: 사이클/씬 유지, 세이브엔 미포함).
- **UI:** 컨트롤 바 💬 버튼 → 스크롤 로그 패널(`UI_Base` 상속, `UI_DialogueBacklog`). 최신이 아래. 열면 자동 재생·타이핑을 정지(§3.2 재사용)하고, 닫으면 이전 상태 복원.
- 색상 태그가 그대로 로그에 살아 있어야 하므로(§3.1) 리치 문자열을 저장한다.

---

## 4. 아키텍처 변경

### 4.1 신규 컴포넌트

| 이름 | 위치(제안) | 책임 |
|------|-----------|------|
| `DialoguePlaybackController` | `Manager/Dialogue/` | 정지·자동·스킵의 전역 상태 소유, Runner에 스킵 명령, 이력 기록. 매니저가 소유 |
| `DialogueMarkup` (static) | `Data/Dialogue/` | `[c:key]`↔TMP 태그 변환, 보이는 글자 수 계산 헬퍼 |
| `DialoguePaletteSO` | `Data/Dialogue/` | 의미 키→색 팔레트 (§3.1.1) |
| `DialogueTypewriter` | `UI/Dialogue/` | `maxVisibleCharacters` 기반 타이핑 공용 컴포넌트. Main/Monologue가 공유 |
| `UI_DialogueBacklog` | `UI/Dialogue/` | 이력 스크롤 패널 |
| `UI_DialogueControlBar` | `UI/Dialogue/` | ⏸/자동/스킵/💬 버튼. 컨트롤러 상태 구독·명령 |

### 4.2 계약(인터페이스) 확장

`IUIDialogueService`(UI 소비)에 재생 제어를 추가한다. UI는 이 계약만 참조하고 매니저 싱글톤을 직접 참조하지 않는다(모듈 경계 준수).

```csharp
public interface IUIDialogueService : IGameService
{
    // 기존 이벤트/멤버 유지 ...
    DialoguePaletteSO Palette { get; }

    // 재생 제어
    bool IsPaused { get; }
    bool IsAuto   { get; }
    void SetPaused(bool paused);
    void SetAuto(bool auto);
    void RequestSkip();           // 대화 스킵(강)
    void CompleteTyping();        // 타이핑 스킵(약)

    // 이력
    IReadOnlyList<DialogueLogEntry> History { get; }
    event Action OnHistoryChanged;
    event Action<bool> OnPauseChanged;
    event Action<bool> OnAutoChanged;
}
```

- `DialogueRunner`에 `SkipToBreak()` 추가(§3.4).
- `DialogueNodeSO`: 기존 필드 유지. 필요 시 `autoAdvanceDuration` 의미를 "이 노드 개별 자동 딜레이 하한"으로 문서화(전역 자동과 max 결합).

### 4.3 입력

- `UIAction`에 `DialogueSkip`, `DialogueToggleAuto`, `DialogueBacklog`를 추가하고 `PlayerInputActions.inputactions`에 KBM/패드 바인딩을 부여한다.
- 진행 입력(`DialogueNext`)은 유지하되 UI 계층에서 "타이핑 중이면 완성, 완성이면 진행"으로 분기(§3.4-1).
- 게임패드 UI 내비게이션은 별도 스펙(`GAMEPAD_UI_INPUT_REBINDING_SYSTEM_SPEC.md`)과 정합. 컨트롤 바/이력 패널은 포커스 대상에 포함한다.

---

## 5. 구현 단계

**Phase 1 — 타이핑 리팩터 + 인라인 색상 (선결)**
1. `DialogueMarkup`, `DialoguePaletteSO` 신설, Addressables 등록.
2. `DialogueTypewriter` 공용 컴포넌트 작성(`maxVisibleCharacters`).
3. `UI_Dialogue`/`UI_MonologueDialogue`의 `TypeText`를 공용 컴포넌트로 교체. `text += c` 제거.
4. Main 채널 `autoAdvanceDuration` 배선 추가.
   - 검증: 색상 태그가 노출되지 않고, 타이핑 완료 시 색이 정상 표기.

**Phase 2 — 재생 제어(정지·자동·스킵)**
5. `DialoguePlaybackController` 신설, 매니저에 연결. `IUIDialogueService` 확장.
6. `DialogueRunner.SkipToBreak()` + 순환/상한 가드.
7. `UI_DialogueControlBar` 작성, 버튼 배선. 입력 액션 추가.
   - 검증: 정지 중 진행 정지, 자동은 선택지에서 정지, 스킵은 이벤트 액션 보존하며 선택지/끝에서 정지.

**Phase 3 — 대화 이력**
8. 화자/초상화 해석 공용화 → `DialogueLogEntry` 기록.
9. `UI_DialogueBacklog` 스크롤 패널, 열림 시 정지 연동.
   - 검증: 지나간 대사 색상 포함 재확인, 상한 초과 시 오래된 항목 폐기.

**Phase 4 — 설정·게임패드 정합**
10. 전역 자동 딜레이·타이핑 속도 설정 노출, 게임패드 포커스 정합.

---

## 6. 테스트 관점

- **EditMode:** `DialogueMarkup` 파싱(중첩·미종료 태그·미등록 키 폴백), `SkipToBreak`가 선택지/End에서 멈추고 이벤트 액션을 실행하는지, 이력 링 버퍼 상한.
- **PlayMode(수직 슬라이스):** 색상 대사 타이핑→완성→진행, 자동 재생 토글, 정지 중 진행 차단, 이력 열기/닫기 상태 복원.
- 리치 텍스트 타이핑 회귀: 태그 문자가 화면에 절대 노출되지 않을 것(가장 흔한 결함).

---

## 7. 리스크·주의

1. **타이핑 방식 전환이 전제.** 색상·스킵·이력이 모두 `maxVisibleCharacters` 기반에 의존하므로 Phase 1을 건너뛰면 나머지가 리치 텍스트에서 깨진다.
2. **이벤트 액션 부작용 보존.** 스킵 시 `eventActions`(플래그/퀘스트)를 반드시 실행. 누락 시 진행 상태 붕괴.
3. **정지와 카메라 녹화 재생 결합 금지.** `Time.timeScale`을 만지면 대화 카메라·전역 시스템에 파급. 정지는 대화 재생 상태로 한정.
4. **모듈 경계.** UI는 `IUIDialogueService`만 참조. 컨트롤러/이력을 매니저 소유로 두고 계약으로만 노출.
5. **데이터 하위 호환.** 기존 대사 에셋은 태그 없이 그대로 동작해야 하며, 신규 필드 기본값이 현행 동작과 동일해야 한다.
