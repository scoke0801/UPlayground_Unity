# Protagonist 저장·화자 계약 구현 스펙

> 문서 버전: **v1.1-implemented**<br>
> 작성일: **2026-08-12** / 구현일: **2026-08-14**<br>
> 상태: **P0 구현 완료**<br>
> 선행 문서: [10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md](10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md)

## 1. 목적

새 게임에서 선택한 캐릭터를 이 세이브의 서사 주인공으로 영구 고정한다. 파티 교체, 플레이어블 해금, 씬 이동, 저장·로드 뒤에도 메인 스토리의 화자명·초상화·자기 조우 판정이 현재 활성 캐릭터로 흔들리지 않아야 한다.

## 2. 용어 계약

| 용어 | 의미 | 변경 가능 시점 |
|---|---|---|
| `Player` | 현재 조작 중인 활성 캐릭터 | 파티 교체 시 |
| `Protagonist` | 새 게임에서 실제로 적용된 최초 선택 캐릭터 | 새 게임에서 한 번 확정, 이후 불변 |
| `NPC speakerId` | 대화 데이터가 지정한 일반 화자 | 노드별 |

`Player`와 `Protagonist`는 같은 캐릭터일 수 있지만 같은 개념이 아니다.

## 3. 구현 전 문제 (해결됨)

- `PartyManager._newGameStartingCharacter`는 실제 파티 적용 뒤 `None`으로 지워진다.
- `PartySaveData`는 roster·battleOrder·activeIndex만 저장한다.
- `DialogueSpeakerResolver`는 `당신`/`Player`를 현재 활성 캐릭터로만 해석한다.
- `UI_Dialogue`와 `DialogueManager` 이력도 활성 캐릭터만 전달한다.
- `{ProtagonistName}` 같은 본문 토큰 치환은 구현되어 있지 않다.
- `DialogueManager` 카메라는 speakerId를 월드 ActorId로 해석하므로, `Protagonist`를 일반 NPC ActorId처럼 처리하면 잘못된 액터를 찾거나 현재 활성 모델을 주인공처럼 촬영할 수 있다.

## 4. 저장 계약

### 4.1 저장 필드

`PartySaveData`에 다음 필드를 추가한다.

```csharp
/// <summary>새 게임에서 실제 적용된 서사 주인공. CharacterActorType 이름 문자열.</summary>
public string storyProtagonistType;
```

문자열을 쓰는 이유는 기존 roster/battleOrder 저장 방식과 맞추고 enum 순서 변경에 의한 숫자 손상을 피하기 위해서다.

`PartyManager` 런타임에는 다음 계약을 노출한다.

```csharp
private CharacterActorType _storyProtagonistType = CharacterActorType.None;
public CharacterActorType StoryProtagonistType => _storyProtagonistType;
```

서비스 계약:

```csharp
// UPlayGround.Contracts.IPartyService
CharacterActorType StoryProtagonistType { get; }

// UPlayGround.UI.IUIPartyService
CharacterActorType StoryProtagonistType { get; }
```

새 구체 Manager 의존을 Actor나 UI에 추가하지 않는다. 일반 소비자는 `Svc.Party`, UI는 `UISvc.Party`를 사용한다.

### 4.2 확정 시점

선택 UI에서 전달된 값을 즉시 저장하지 않는다. `PartyManager.ApplyNewGameStartingCharacter()`가 모델 존재 여부를 확인하고 실제 `selected` 또는 `fallback`을 roster와 battleOrder에 적용한 직후 그 **실제 적용값**을 Protagonist로 확정한다.

```text
선택 UI 값
→ 모델 유효성 검사
→ selected 또는 fallback을 파티에 적용
→ 같은 실제 타입을 _storyProtagonistType에 기록
→ _newGameStartingCharacter=None
```

선택값과 실제 모델이 다르면 fallback 캐릭터가 주인공이다. 존재하지 않는 선택값을 저장한 뒤 이름·초상화만 유령처럼 남기지 않는다.

### 4.3 불변 조건

다음 작업은 Protagonist를 변경하지 않는다.

- 활성 캐릭터 교체
- battleOrder 재정렬
- `UnlockCharacter`
- `_recruitableAs`에 의한 플레이어블 해금
- BossAssist 영입·장착
- 씬 이동
- 사이클 시작·정산·포기

새 게임 초기화만 `None`으로 되돌릴 수 있다.

### 4.4 로드와 구버전 폴백

1. `storyProtagonistType`을 enum 이름으로 파싱한다.
2. 유효하고 해당 파티 모델 데이터가 있으면 사용한다.
3. 누락·오염이면 `battleOrder` 첫 유효 타입을 사용한다.
4. 없으면 `roster` 첫 유효 타입을 사용한다.
5. 그래도 없고 Bokusei 모델이 있으면 Bokusei로 폴백한다.
6. 모두 실패하면 `None`을 유지하고 오류를 남긴다. 임의 enum 0값으로 캐스팅하지 않는다.
7. 폴백에 성공한 값은 다음 저장에 기록하여 반복 보정을 막는다.

구버전 세이브에서 현재 activeIndex를 우선하지 않는다. 로드 당시 우연히 활성인 캐릭터가 서사 주인공으로 굳는 것을 막기 위해 battleOrder 첫 항목을 우선한다.

## 5. speakerId 계약

대화 데이터에서 다음 ID를 예약한다.

| speakerId | 이름·초상화 대상 | 월드 화자 대상 |
|---|---|---|
| `Player` 또는 `당신` | 현재 활성 캐릭터 | 현재 PlayerActor |
| `Protagonist` | `StoryProtagonistType` | 조건부, 7절 참조 |
| 그 외 | 기존 node.speakerId와 node.portrait | 기존 binding table |

`Protagonist`는 캐릭터 이름이나 ActorId가 아니다. `SpeakerActorBindingTableSO`에 `Protagonist → 특정 ActorId`를 등록하지 않는다.

`DialogueSpeakerResolver`의 판정은 명시적으로 분리한다.

```csharp
public const string ProtagonistSpeakerId = "Protagonist";

public static bool IsActivePlayerSpeaker(DialogueNodeSO node);
public static bool IsProtagonistSpeaker(DialogueNodeSO node);
```

기존 `IsPlayerSpeaker` 이름을 유지한다면 의미를 `Player/당신만`으로 좁히고, Protagonist를 그 안에 섞지 않는다.

## 6. 표시명·초상화 해석

공용 resolver는 활성 타입과 주인공 타입을 모두 입력받는다.

```csharp
public static string ResolveSpeakerName(
    DialogueNodeSO node,
    PartyMemberDataSO memberData,
    CharacterActorType activeType,
    CharacterActorType protagonistType);

public static Sprite ResolvePortrait(
    DialogueNodeSO node,
    PartyMemberDataSO memberData,
    CharacterActorType activeType,
    CharacterActorType protagonistType);
```

해석 규칙:

1. `Player/당신`이면 activeType의 이름·전신 스프라이트.
2. `Protagonist`이면 protagonistType의 이름·전신 스프라이트.
3. 캐릭터 데이터가 없으면 node의 speakerId/portrait로 폴백.
4. Protagonist 값이 `None`이면 활성 캐릭터로 조용히 폴백하지 않고 한 번 경고한 뒤 node 데이터로 폴백한다. 잘못된 저장을 은폐하지 않는다.

다음 두 소비자는 반드시 같은 인자를 전달한다.

- `UI_Dialogue`: 화면 표시
- `DialogueManager.RecordNodeHistory`: 백로그 이름·초상화

화면과 백로그가 서로 다른 화자를 기록하면 완료로 보지 않는다.

## 7. 월드 화자와 카메라

`Protagonist` 화자라고 해서 현재 PlayerActor를 무조건 촬영하지 않는다.

| 조건 | P0 처리 |
|---|---|
| activeType == protagonistType | 현재 PlayerActor를 화자 transform으로 사용 가능 |
| activeType != protagonistType | 화자 transform과 speaker anchor를 만들지 않음. 현재 NPC/대화 카메라 유지 |
| 주인공 실체가 반드시 화면에 필요 | P0 데이터에서 사용 금지. P1 cinematic clone/명시적 스테이징 필요 |

`DialogueManager.ResolveActorId`보다 앞에서 `Protagonist` 예약 ID를 처리한다. binding table 폴백으로 흘려보내지 않는다.

이 계약은 자기 조우에서도 중요하다. 상대와 같은 모델이 화면에 있어도 주인공 대사의 이름·초상화는 `StoryProtagonistType`으로 해석하되, 현재 활성 캐릭터가 다른 경우 잘못된 PlayerActor를 클로즈업하지 않는다.

## 8. 본문 토큰 계약

확정 토큰:

| 토큰 | 치환 값 |
|---|---|
| `{ProtagonistName}` | `StoryProtagonistType`의 표시명 |
| `{PlayerName}` | 현재 활성 캐릭터 표시명 |

대화 본문과 선택지에 같은 치환 규칙을 적용한다. 토큰 치환은 색상 마크업 변환 전에 수행한다.

```text
원본 dialogueText/choiceText
→ DialogueTextResolver 토큰 치환
→ DialogueMarkup.ToRichText
→ Typewriter/UI/History
```

권장 신규 순수 유틸:

```csharp
public static class DialogueTextResolver
{
    public static string Resolve(
        string source,
        string activePlayerName,
        string protagonistName);
}
```

규칙:

- 알려진 토큰만 정확 일치로 치환한다.
- 미해결 토큰은 원문을 남기고 에디터/개발 빌드에서 경고한다.
- 정규식으로 임의 표현식을 실행하지 않는다.
- 저장 데이터의 이름을 직접 문자열 삽입하지 않고 `PartyMemberDataSO.GetName` 결과를 쓴다.
- 선택지 표시와 실제 선택 결과 데이터는 같은 원본을 공유하므로, 치환 문자열을 데이터에 역기록하지 않는다.

## 9. 자기 조우 판정

P0 자기 조우 판정의 기준은 표시명 문자열이 아니라 타입/Actor 매핑이다.

```text
StoryProtagonistType
→ 해당 CharacterActorType의 전투 ActorId 확인
→ 현재 CycleBossPlacement.actorId와 대조
→ 일치하면 자기 조우 최소 반응 분기
```

캐릭터 타입과 Monster ActorId의 명명 규칙이 항상 일치한다고 가정하지 않는다. 명시적 매핑 데이터 또는 이미 존재하는 Actor 정의 관계를 사용한다.

최소 반응의 주인공 노드는 `speakerId: Protagonist`를 사용한다. `Bokusei`나 `Player`로 하드코딩하지 않는다.

## 10. 저장·이벤트 순서

### 새 게임

```text
ResetForNewGame
→ 선택 캐릭터 보류
→ 시작 씬에서 PlayerActor/모델 데이터 준비
→ 실제 파티 적용
→ StoryProtagonistType 확정
→ OnStoryProtagonistChanged 1회(선택 사항)
→ 앵커 스토리 게이트 시작
```

Protagonist가 확정되기 전에 Protagonist 대화를 시작하지 않는다.

### 로드

```text
PartySaveData 역직렬화
→ roster/battleOrder 모델 해석
→ storyProtagonistType 복원/폴백
→ Dialogue/Story 소비자가 접근 가능
→ 대화 재개
```

대화 세션이 저장 대상이 아니라면 로드 중간 노드 재개는 범위 밖이지만, 로드 후 새로 시작되는 모든 대화는 복원된 Protagonist를 사용해야 한다.

## 11. 테스트 계약

### EditMode

1. `Player`는 activeType, `Protagonist`는 protagonistType 이름을 반환한다.
2. 같은 조건에서 각각 올바른 초상화를 반환한다.
3. 저장 문자열 parse 성공·누락·오염·모델 없음 폴백을 검증한다.
4. 파티 swap/unlock 후 `StoryProtagonistType`이 변하지 않는다.
5. `{ProtagonistName}`과 `{PlayerName}`이 본문·선택지에서 독립 치환된다.
6. 미해결 토큰이 삭제되지 않는다.

### PlayMode/수직 슬라이스

1. Bokusei가 아닌 캐릭터로 새 게임을 시작한다.
2. 초회차 앵커 대사의 화자명·초상화가 선택 캐릭터다.
3. 다른 캐릭터를 해금·활성화한 뒤 `Player` 대사는 활성 캐릭터, `Protagonist` 대사는 최초 선택 캐릭터로 표시된다.
4. 저장·로드 뒤 같은 결과를 유지한다.
5. 자기 조우 최소 반응의 주인공 발화가 활성 캐릭터에 빼앗기지 않는다.
6. activeType != protagonistType일 때 대화 카메라가 잘못된 PlayerActor를 주인공으로 촬영하지 않는다.

## 12. 구현 승인 시 변경 대상

- `Assets/02.Scripts/Data/Save/GameSaveData.cs`
- `Assets/02.Scripts/Manager/Party/PartyManager.cs`
- `Assets/02.Scripts/Contracts/GameServices.cs`
- `Assets/02.Scripts/UI/Contracts/UIServices.cs`
- `Assets/02.Scripts/Data/Dialogue/DialogueSpeakerResolver.cs`
- 신규 `DialogueTextResolver.cs`
- `Assets/02.Scripts/UI/Dialogue/UI_Dialogue.cs`
- `Assets/02.Scripts/Manager/Dialogue/DialogueManager.cs`
- 화자 resolver·파티 저장 테스트

대사 JSON과 자기 조우 그래프는 이 계약 구현·검증 뒤에 저작한다.
