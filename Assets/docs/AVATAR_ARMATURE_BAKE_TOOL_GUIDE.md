# Avatar Armature Bake Tool 가이드

## 목적

`Avatar Armature Bake Tool`은 Modular Avatar/NDMF 플러그인을 프로젝트에 계속 유지하지 않고도, 아바타용 헤어·의상 본을 게임용 캐릭터 프리팹에 붙이기 위한 Editor 전용 베이크 툴이다.

메뉴 위치:

`Tools > UPlayGround > Avatar Armature Bake Tool`

툴 파일:

`Assets/02.Scripts/Editor/AvatarArmatureBakeTool.cs`

## 기본 원칙

작업은 반드시 원본 프리팹이 아니라 씬에 배치한 복사본에서 먼저 실행한다.

정상 동작을 확인한 뒤 별도 프리팹으로 저장하고, 이후 Modular Avatar/NDMF/VRC SDK 의존성을 제거한다.

## Hichi + Twin Bun Braids 헤어 적용 절차

이 헤어는 전신 의상처럼 `Hip/Spine/Chest/Head` 계층을 아바타 본에 대체 병합하는 구조가 아니다.

헤어 쪽 구조는 다음처럼 `head` 아래에 헤어 전용 본이 달려 있다.

```text
Hichi_Twin Bun Braids
  Armature
    head
      Bun.L
      Bun.R
      Tail.L
      Tail.R
      ...
```

따라서 `head` 본을 Hichi 본체의 `Head` 본으로 대체하면 안 된다. 헤어의 `head` 루트를 Hichi의 실제 `Head` 본 아래에 그대로 자식으로 붙여야 한다.

설정:

- `Avatar Object`: `MonsterActor_Hichi`
- `Hair/Outfit Object`: `Hichi_Twin Bun Braids`
- `Target Root`: Hichi 본체의 실제 `Head` 본
- `Source Root`: `Hichi_Twin Bun Braids/Armature/head`
- `Auto Detect Prefix/Suffix`: 끔
- `Prefix`: 빈 값
- `Suffix`: 빈 값
- `Keep Source Root As Child (hair/accessory)`: 켬

`Keep Source Root As Child (hair/accessory)`가 켜져 있으면 다음 처리를 하지 않는다.

- `SkinnedMeshRenderer.bones` 변경
- `SkinnedMeshRenderer.rootBone` 변경
- Mesh bindpose 변경
- 중복 Source 본 삭제

이 모드는 헤어·악세서리처럼 자체 본 트리를 보존해야 하는 에셋에 사용한다.

## 실패 사례

다음 설정은 사용하면 안 된다.

- `Target Root`: Hichi의 `Armature` 또는 `Hip`
- `Source Root`: 헤어의 `Armature`
- `Retarget SkinnedMeshRenderer bones/rootBone`: 켬
- `Delete duplicate source bones after retarget`: 켬

이 설정으로 실행하면 헤어의 기준 본이 Hichi 본체 본으로 대체되고, `bindposes`가 잘못 갱신되어 헤어 메시가 긴 삼각형처럼 찢어진다.

문제가 발생하면 Unity에서 `Ctrl + Z`로 되돌리거나, 작업용 복사본을 다시 배치해서 진행한다.

## 전신 의상 적용 시

전신 의상은 헤어와 다르게 Source 본 계층이 아바타 본 계층을 복사한 형태일 수 있다.

예:

```text
Outfit
  Armature
    Hip
      Spine
        Chest
          Neck
            Head
```

이런 경우에는 대체 병합 모드를 사용할 수 있다.

설정:

- `Target Root`: 아바타의 `Hip` 또는 의상 계층과 대응되는 본 루트
- `Source Root`: 의상의 `Hip` 또는 대응되는 본 루트
- `Auto Detect Prefix/Suffix`: 필요 시 사용
- `Retarget SkinnedMeshRenderer bones/rootBone`: 켬
- `Delete duplicate source bones after retarget`: 켬
- `Keep Source Root As Child (hair/accessory)`: 끔

단, 본 이름이 다르거나 의상 쪽 본 스케일이 크게 다르면 Blender에서 먼저 정리하는 편이 안전하다.

## Modular Avatar 제거 순서

1. 별도 작업본에서 헤어/의상 베이크
2. Scene View와 Game View에서 위치, 스케일, 애니메이션 추종 확인
3. 기존 헤어 메시가 겹치면 비활성화
4. 결과 프리팹 저장
5. Modular Avatar 컴포넌트가 남은 프리팹/씬이 없는지 확인
6. `Packages`에서 `nadena.dev.modular-avatar`, `nadena.dev.ndmf` 제거
7. 임시로 복사한 `Assets/Plugins/NDMFDependencies`가 더 이상 필요 없으면 제거

## 체크리스트

- 헤어 루트가 Hichi `Head` 아래에 있는가
- 헤어 `SkinnedMeshRenderer`의 본 참조가 깨지지 않았는가
- 기존 `hichi_hair`가 새 헤어와 겹치지 않는가
- 공격, 회피, 대시, 피격, 사망 모션에서 헤어가 머리를 따라오는가
- MagicaCloth2로 물리를 재구성할 경우 PhysBone 의존성을 제거했는가
