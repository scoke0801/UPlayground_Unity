# GameplayTag 시스템 가이드

## 개요

GameplayTag는 `State.Combat.Attack`처럼 `.`으로 계층을 표현하는 데이터 기반 태그다.
프로젝트에서 사용할 수 있는 값은 `GameplayTagRegistrySO`에 등록된 항목으로 제한한다.

핵심 원칙:

- 태그 정의의 단일 원본은 `Assets/Resources/GameplayTagRegistry.asset`이다.
- 태그 추가·설명·색상 변경은 데이터 편집이며 C# 생성이나 재컴파일을 요구하지 않는다.
- 직렬화 필드는 `GameplayTag`를 사용하고 Inspector에서는 Registry 기반 검색·계층형 선택 UI를 제공한다.
- 자유 문자열과 enum을 저작 데이터로 사용하지 않는다.
- 미등록 직렬화 값은 Inspector에서 오류로 표시하고 빌드 전 검증에서 차단한다.

## 구성

```text
Assets/Resources/GameplayTagRegistry.asset
    └─ tagName / description / color
              │
              ├─ GameplayTagPropertyDrawer
              │    └─ 검색 + 점(.) 계층 선택
              │
              ├─ GameplayTagRegistry
              │    └─ TryResolve / GetRequired / IsRegistered
              │
              └─ GameplayTagRegistryBuildValidator
                   └─ 중복·빈 값·직렬화된 미등록 값 검사
```

주요 파일:

- `Assets/02.Scripts/Data/Gameplay/GameplayTag.cs`
- `Assets/02.Scripts/Data/Gameplay/GameplayTagRegistrySO.cs`
- `Assets/02.Scripts/Data/Editor/Gameplay/GameplayTagPropertyDrawer.cs`
- `Assets/02.Scripts/Gameplay/Tag/Editor/GameplayTagRegistryEditorWindow.cs`
- `Assets/02.Scripts/Gameplay/Tag/Editor/GameplayTagRegistryBuildValidator.cs`
- `Assets/02.Scripts/GameActor/Gameplay/Tag/GameplayTagContainer.cs`

## 태그 추가

1. 도구 검색에서 `태그 레지스트리 에디터`를 연다.
2. 항목을 추가하고 `tagName`, `description`, `color`를 입력한다.
3. `검증` 후 `저장`한다.

저장된 값은 `GameplayTag` PropertyDrawer를 다시 열 때 즉시 검색 대상에 포함된다.
코드 생성, enum 수정, Unity 스크립트 재컴파일은 없다.

태그 이름 규칙:

- `.`으로 계층을 구분한다. 예: `State.Combat.Counter`
- 앞뒤 공백을 사용하지 않는다.
- 대소문자를 구분한다.
- 같은 이름을 중복 등록하지 않는다.

## 데이터 필드 사용

```csharp
[SerializeField] private GameplayTag _requiredTag;
[SerializeField] private List<GameplayTag> _grantedTags = new();
```

Inspector 드롭다운은 다음 기능을 제공한다.

- 태그 이름과 설명 검색
- `State / Combat / Attack` 형태의 계층 탐색
- `(없음)` 선택과 빠른 지우기
- Registry에 없는 기존 값의 오류 표시

태그 값은 Inspector에서 직접 문자열로 입력하지 않는다.

## 태그 사용처 검색

Registry 에디터에서 태그를 선택하고 `사용처`를 누르거나 도구 검색에서
`태그 사용처 검색`을 연다.

검색 대상:

- Registry 정의
- `Assets/01.Scenes`, `Assets/03.Prefabs`, `Assets/10.Datas`,
  `Assets/Resources`의 직렬화된 `_tagName`
- `Assets/02.Scripts`, `Assets/Tests`의 정확히 일치하는 C# 문자열

`하위 태그 포함`을 켜면 `State.Combat` 검색 시
`State.Combat.Attack` 같은 하위 태그의 사용처도 함께 표시한다.
결과의 `열기` 버튼으로 해당 파일과 줄을 바로 열 수 있다.

## 안전한 태그 이름 변경

Registry 목록의 태그 이름은 직접 수정하지 않는다.
행의 `Rename` 또는 툴바의 `이름 변경`을 사용한다.

Rename 절차:

1. 기존 태그와 새 태그 이름을 확인한다.
2. 부모 태그라면 `하위 태그도 같은 접두사로 함께 변경` 여부를 선택한다.
3. 변경 대상 정의와 사용처 미리보기를 확인한다.
4. `안전하게 변경`을 실행한다.

Rename은 다음 항목을 한 번에 갱신한다.

- Registry의 대상 정의
- SO·프리팹·씬·Resources에 직렬화된 정확한 `_tagName`
- 정확히 일치하는 C# 태그 문자열

변경 결과가 기존 태그와 충돌하거나 이름 규칙에 맞지 않으면 실행하지 않는다.
변경 전 대상 파일을 원본 바이트로 보관하며 도중에 예외가 발생하면 전부 복구한다.
코드 고정 태그 사용처가 변경된 경우에만 Unity 스크립트 재컴파일이 발생한다.

## 런타임 사용

데이터에서 전달받은 값은 그대로 사용한다.

```csharp
if (_requiredTag.IsValid() && actor.Tags.HasTag(_requiredTag))
{
    // 등록된 태그와 정확히 일치
}

if (actor.Tags.HasTagInHierarchy(_requiredTag))
{
    // 동일 태그 또는 하위 태그 보유
}
```

외부 문자열을 받아야 하는 경계에서는 반드시 Registry로 해석한다.

```csharp
if (GameplayTagRegistry.TryResolve(rawTagName, out GameplayTag tag))
    actor.Tags.AddTag(tag);
```

등록되지 않은 문자열은 `TryResolve`가 `false`를 반환한다.
반드시 존재해야 하는 프로젝트 표준 태그에는 `GetRequired`를 사용할 수 있다.

```csharp
GameplayTag combatTag =
    GameplayTagRegistry.GetRequired("State.Combat");
```

`GameplayTags`와 `MotionTags`의 정적 필드는 상태 코드처럼 컴파일 타임에 고정된 의미 슬롯을 읽기 위한 편의 API다.
Registry가 실제 원본이며, 콘텐츠 데이터에 새 태그를 추가하기 위해 이 정적 필드를 늘릴 필요는 없다.

## 컨테이너

`GameplayTagContainer`는 액터가 현재 보유한 태그를 관리한다.

```csharp
container.AddTag(tag);
container.RemoveTag(tag);
container.HasTag(tag);
container.HasTagInHierarchy(parentTag);
container.RemoveTagsWithParent(parentTag);
container.Clear();
```

`OnTagAdded`와 `OnTagRemoved`를 구독할 수 있다.
상태 진입에서 추가한 태그는 상태 종료에서 반드시 제거하고, 풀링 액터는 재사용 진입점에서 잔존 태그를 정리한다.

## 검증

수동 검증 도구:

`UPlayGround/게임플레이/게임플레이 태그/등록 무결성 검증`

검증 항목:

- Registry 에셋이 정확히 하나인지
- 빈 태그 이름과 중복 이름이 없는지
- 이름 앞뒤에 공백이 없는지
- 씬·프리팹·데이터·Resources에 직렬화된 모든 `_tagName`이 Registry에 등록되어 있는지

같은 검증은 Player Build 전에 자동 실행되며 오류가 있으면 빌드를 중단한다.

## 변경 시 주의 사항

- Registry에서 사용 중인 태그를 바로 삭제하거나 이름을 바꾸면 기존 직렬화 값이 미등록 상태가 된다. 먼저 참조를 교체한 뒤 검증한다.
- 태그 이름은 Registry Inspector나 YAML에서 직접 바꾸지 말고 전용 Rename을 사용한다.
- `GameplayTag._tagName` 필드명은 기존 에셋 호환을 위해 변경하지 않는다.
- `GameplayTag` 생성자를 공개하거나 문자열 암시 변환을 다시 추가하지 않는다.
- 저작용 `GameplayTagId` enum과 Registry→C# 코드 생성 파이프라인을 다시 도입하지 않는다.
- Core의 `AbilityTagId`는 모듈 비의존 경계 타입이다. 프로젝트 데이터는 어댑터에서 Registry 검증 후 Core 문자열 값으로 전달한다.
