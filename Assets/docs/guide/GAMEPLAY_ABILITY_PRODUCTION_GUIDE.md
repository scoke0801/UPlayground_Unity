# Gameplay Ability 양산화 도구 사용 가이드

> 기준일: 2026-07-25  
> 대상: 기획자·1인 개발 제작 워크플로

## 1. 공용/파생 AbilitySet 구성

Unity 상단 메뉴에서 `UPlayGround > 툴 런처`를 연다. 런처의
`게임플레이 / 전투` 분류에서 `Ability 양산화 Wizard`를 실행한다.
검색창에서는 `Ability 양산화` 또는 `Production Wizard`로 찾을 수 있다.

기본 `작업 흐름`은 `Compose Ability Set`이다.

1. 공용 Set을 새로 만들려면 `공용 Base Set`을 비우고 공용으로 묶을 Ability를
   `추가할 Ability`에 넣는다.
2. 특수 몬스터 Set을 만들려면 `공용 Base Set`을 지정한다.
3. 특수 Ability가 더 필요하면 `추가할 Ability`에 넣는다.
4. 기존 Ability를 바꾸거나 빼려면 `Replace / Remove`를 추가한다.
5. 공용 Set은 `MonsterProfile`, 파생 Set은 특수 `ActorDefinition`에 연결한다.
6. `Set 구성 Preview`에서 경로, Base 포함 여부, Override 중복과 연결 조건을 확인한다.
7. `공용/파생 Set 적용`을 누른다.

Ability Editor에서 Ability를 여러 개 선택한 뒤 `선택으로 Set 구성`을 누르면 선택 목록이
Wizard에 자동으로 전달된다.

## 1.1 신규 Ability 생성

Wizard의 `작업 흐름`을 `Create Ability From Recipe`로 바꾼다.

1. 레시피를 선택한다.
2. 대상 `AbilitySetSO`, `MotionReferenceSO`, 공용 TaskGraph를 지정한다.
3. AbilitySet 연결 방식을 고른다.
   - `AdditionalAbilities`: 몬스터·보스 AI 풀과 공용 추가 Ability
   - `PlayerSkillSlot`: Ability/Ultimate/ElementalImbue 입력 슬롯
   - `PlayerCombatSequence`: Light/Heavy/Jump 등 전투 실행 순서
4. 표시명, 안정 Ability ID, 저장 루트와 거리·AI 값을 입력한다.
5. Effect가 필요하면 기존 Effect 공유 또는 Commit Effect 신규 생성 중 하나를 고른다.
6. `생성 계획 Preview`에서 생성·수정 경로와 오류를 확인한다.
7. `계획 적용`을 누른다.

경로 또는 ID 충돌은 숫자 suffix로 우회하지 않는다. 기존 슬롯을 바꾸려면
`기존 바인딩 교체`를 명시적으로 켜야 한다.

Wizard, Dashboard, Runtime Sandbox는 UI Toolkit 화면으로 구성되어 있다. 각 섹션의
설명과 Preview 결과를 위에서 아래 순서로 확인한다.

## 1.2 Ability Editor 복사와 복제

`UPlayGround > 툴 런처 > 게임플레이 / 전투 > Ability 에디터`에서 다음 기능을 쓴다.

- `＋ 생성`: Ability, Passive, Effect, Set 생성과 공용/파생 Set 구성,
  레시피 기반 신규 Ability 생성으로 진입한다.
- `Set 구성`: 현재 선택한 Ability를 Wizard의 공용/파생 Set 입력으로 전달한다.
- `복제`: 선택한 메인 Ability/Passive/Effect/Set 에셋 전체를
  새 파일로 복제한다.
  Ability/Passive/Effect의 안정 ID는 `.Copy`, `.Copy2` 형태로 중복을 피한다.
- `탭 복사`: 현재 선택한 탭에 표시되는 값만 메모리
  클립보드에 복사한다.
- `붙여넣기`: 같은 에셋 타입의 같은 탭에만 값을
  붙여넣는다. 예를 들어
  GameplayAbility의 `비용/쿨다운`을 복사해 다른 GameplayAbility의 같은 탭에 적용할
  수 있지만 Effect나 Variant 탭에는 붙여넣을 수 없다.

삭제는 상단 `⋯` 메뉴의 `선택 에셋 삭제…`에 있으며 참조 영향 확인 후에만
실행된다.

반복 작업 단축키:

- `Ctrl/Cmd+D`: 선택 에셋 복제
- `Ctrl/Cmd+Shift+C`: 현재 탭 값 복사
- `Ctrl/Cmd+Shift+V`: 탭 값 붙여넣기
- `Ctrl/Cmd+S`: 저장

탭 상단 도움말은 선택 에셋 타입과 탭에 따라 달라진다. 특히 Ability의 `Variant`와
AbilitySet의 `Variant(차지 단계)`는 서로 다른 개념으로 안내한다.

## 1.3 몬스터 공용 Set 운영 방향

동일 타입 몬스터는 `MonsterActorProfileSO.abilitySet`을 공용 Set으로 사용한다.
특수 몬스터는 공용 Ability를 직접 수정하지 않고 파생 Set에서 필요한 Ability만
Replace/Add/Remove한다.

```text
MonsterProfile 공용 AbilitySet
├── 일반 몬스터 A
├── 일반 몬스터 B
└── 특수 몬스터 파생 AbilitySet
    ├── Replace: 기본 강공 → 엘리트 강공
    └── Add: 특수 버프
```

공용 Set과 파생 Override는 런타임의 `AbilitySetSO` 유효 해석 API를 통해 ASC,
EnemyCombat과 BT에 동일하게 적용된다. 특수 몬스터용 공유 에셋의 필드를 직접 수정하지
말고, Ability/Payload 안전 Fork 후 파생 Set의 Replace 대상으로 연결한다.

## 2. 레시피 선택 기준

| 레시피 | 기본 연결 | 확인할 Motion 근거 |
|---|---|---|
| Player.Basic.Melee | Player Combat Sequence | Collision |
| Player.Skill.Projectile | Player Skill Slot | SpawnProjectile |
| Monster.Basic.Melee | Additional | Collision |
| Monster.Heavy.Telegraph | Additional | Collision, Telegraph |
| Combat.AreaAttack | Additional | AOE 또는 범위 판정 이벤트 |
| Support.HealOrBuff | Player Skill Slot | Commit/End Effect |

레시피는 MotionEvent를 새로 만들지 않는다. 생성 후 Dashboard에서 선택 Motion이
레시피 의도와 일치하는지 확인한다.

## 3. Motion 비교와 안전 복제

`UPlayGround > 툴 런처 > 게임플레이 / 전투 > Ability 제작 검증 대시보드`를 연다.

- `선택 Ability의 Motion 분석`: 기본 Motion의 길이, 이벤트 종류,
  Collision이 요구하는 HitPhase 수를 표시한다.
- `부족한 HitPhase만 추가`: 기존 HitPhase를 수정하거나 제거하지 않고 부족한 항목만
  Undo 가능한 변경으로 추가한다.
- `복제 Preview`와 `Ability Fork 적용`: Ability와 Motion Payload만 독립 복제하고
  TaskGraph, MotionReference, Effect는 공유한다.
- `역참조 Preview`: Effect, MotionReference 등 수정 후보를 사용하는 Ability/Set을
  먼저 확인한다.

무기 타입 계약이 없으면 MotionReference의 첫 override를 임의 선택하지 않고
기본 Motion만 분석한다.

## 4. 검증

Dashboard의 `선택 Ability 교차 검증`은 TaskGraph와 Motion/HitPhase 관계를 검사한다.
Issue 행을 누르면 관련 에셋으로 이동한다.

`프로젝트 전체 검증`은 기존 `AbilityDataValidator`를 그대로 사용한다.
자동 Fix는 실행하지 않는다.

## 5. Play Mode 샌드박스

`UPlayGround > 툴 런처 > 게임플레이 / 전투 > Ability Runtime Sandbox`를 연다.

1. 실제 Owner Actor 프리팹과 Ability를 선택한다.
2. Required 대상 Ability라면 Target Actor 프리팹도 선택한다.
3. `Play Mode에서 ASC 수직 슬라이스 실행`을 누른다.
4. Prepare, Variant, Commit, 실행 중 Task, 종료 후 Task/Effect 결과를 확인한다.

샌드박스는 임시 AbilitySet과 프리팹 인스턴스를 정리하고 Play Mode를 종료한다.
Motion/히트/카메라/UI는 실제 게임 장면의 수동 스모크를 별도로 수행한다.

## 6. 밸런스와 Replay

Dashboard에서 다음 순서로 사용한다.

1. `정적 예상값 계산`
2. 샌드박스나 전투 로그의 평균 피해·시간·Hit 수·종료 후 잔류값 입력
3. `정적 예상값과 실측 비교`
4. Encounter Replay JSON을 열어 공격 후보 비율, 평균 거리, 실패 수 비교
5. 필요하면 Replay 비교 CSV 저장
6. 큰 수정 전 `현재 전체 Snapshot 저장`
7. 수정 후 `기준 Snapshot과 현재 비교`

도구는 차이와 검토 필드를 제안할 뿐 밸런스 값을 자동 수정하지 않는다.

## 7. 금지 사항

- Dryad 공격 3개와 Training Dummy 공격 1개에 근거 없이 Motion을 연결하지 않는다.
- 공유 Effect나 MotionReference를 역참조 확인 없이 직접 수정하지 않는다.
- MotionSet/Ultimate 오류가 있는 상태에서 대량 저장·재직렬화하지 않는다.
- 생성 실패를 부분 성공으로 취급하지 않는다.
