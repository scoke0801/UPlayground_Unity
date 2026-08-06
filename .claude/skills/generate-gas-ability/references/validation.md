# GAS 데이터 검증 체크리스트

## 목차

- [변경 전](#변경-전)
- [에셋 단위](#에셋-단위)
- [연결 단위](#연결-단위)
- [자동 검증](#자동-검증)
- [Play Mode](#play-mode)
- [Diff 안전 검사](#diff-안전-검사)

## 변경 전

- `git status --short`로 기존 사용자 변경을 기록한다.
- Unity Console의 컴파일 오류와 Missing Script 여부를 확인한다.
- 대상 AbilitySet, 실제 소유 Actor/Profile, Actor MotionSet을 특정한다.
- 동일 역할의 정상 Ability 한 개를 비교 기준으로 정한다.
- 공유 참조의 역참조를 확인한다.

기준선이 이미 실패하면 새 변경과 기존 실패를 구분해 기록한다. 타입 오류가 있으면 ScriptableObject 저장·대량 Import·재직렬화를 먼저 수행하지 않는다.

## 에셋 단위

### GameplayAbilitySO

- `abilityId`가 비어 있지 않고 프로젝트 전역에서 고유하다.
- 비용/쿨다운/거리 값이 음수가 아니고 대상 정책과 일치한다.
- 일반 Ability의 TaskGraph와 Root가 유효하다.
- Variant가 하나 이상이며 ID, 조건, 우선순위가 의도대로다.
- 같은 조건의 낮은 우선순위 Variant가 영구 가려지지 않는다.
- 실행 가능한 Variant가 하나 이상이다.
- Effect 참조가 null이 아니다.
- 태그가 Registry에 존재한다.

### Motion Payload

- `attackInfo`와 필요한 `baseInfo`가 null이 아니다.
- MotionKey가 유효하며 실제 소유 Actor MotionSet에서 해석된다.
- 공격/Ultimate 카테고리와 HitPhase 존재 여부가 일치한다.
- Motion Collision 이벤트의 최대 `hitPhaseIndex`가 Payload 범위 안에 있다.
- HitPhase와 MotionEvent/HitBox의 `hitboxGroupId`가 일치한다.
- 몬스터 `aiSelectable` 공격은 HitPhase, Motion 매핑, 거리, 레벨, 가중치가 모두 유효하다.

### GameplayEffectSO

- `effectId`가 고유하다.
- Duration/Periodic 시간, stacking key, max stack이 유효하다.
- Attribute ID가 Registry에 존재한다.
- granted/required/blocked/immunity/dispel 태그가 유효하고 순환하지 않는다.
- HUD 노출 Effect의 이름·아이콘·시간/스택 표시가 수명주기와 맞는다.

## 연결 단위

- AbilitySet base 참조에 순환이 없다.
- Player slot과 combat binding이 중복되지 않는다.
- Replace 원본이 Base의 유효 Ability이며 Replace 대상이 null이 아니다.
- Remove에 사용되지 않는 replacement가 남지 않는다.
- Request 전용 라우터가 입력/전투 슬롯 또는 AI 자동 선택에 노출되지 않는다.
- MonsterActorProfile에 AbilitySet이 있고 BT 선택 가능한 공격이 하나 이상이다.
- ActorDefinition의 유효 AbilitySet과 프로필 AbilitySet이 일치한다.
- 몬스터 AI 공격 MotionKey가 다른 액터가 아니라 자기 프리팹 MotionSet에서 해석된다.
- 플레이어는 현재 무기 세트와 `NoWeapon` 폴백에서 의도한 Motion을 해석한다.

## 자동 검증

Unity에서 다음 순서로 실행한다.

1. Ability Editor의 선택 에셋 검증
2. Ability Production Dashboard의 Motion 분석과 선택 Ability 교차 검증
3. Telegraph, Collision, HitBox를 바꿨으면 툴 런처의 `전투 데이터 검증기` 실행
4. Dashboard의 프로젝트 전체 검증 (`AbilityDataValidator.ValidateAll()`)
5. `Tools > Ability > 테스트 > EditMode 실행`
6. 런타임 경로 변경 시 `Tools > Ability > 테스트 > PlayMode 수직 슬라이스 실행`

특히 다음 테스트의 실패 내용을 모아서 해결한다.

- `AbilityDataIntegrityTests`
- `AbilitySetCompositionTests`
- `AbilityProductionPlannerTests`
- `PlayerCombatAbilityDataViewTests`
- `MonsterAbilitySetIntegrationTests`
- 태그 또는 런타임 변경과 직접 관련된 Ability System/Task/Effect 테스트

`MonsterAbilitySetIntegrationTests`의 Motion/HitPhase 오류를 건너뛰거나 첫 실패에서 숨기지 않는다. 전체 누락 목록을 확인한다.

CLI 보조 컴파일은 변경한 경계에 맞춰 실행한다.

```powershell
dotnet build UPlayGround.Ability.Core.csproj --no-restore
dotnet build UPlayGround.Data.csproj --no-restore
dotnet build UPlayGround.Ability.UPlayGround.csproj --no-restore
dotnet build UPlayGround.Actor.csproj --no-restore
dotnet build UPlayGround.Ability.Tests.csproj --no-restore
```

생성된 `.csproj`가 최신일 때만 CLI 결과를 Unity 컴파일의 보조 근거로 사용한다. `dotnet build`는 Unity Test Runner 실행을 대체하지 않는다.

## Play Mode

- 활성화 실패 사유, 비용, 쿨다운, charge를 확인한다.
- 예상 Variant와 Motion이 선택되는지 확인한다.
- Collision 횟수, HitPhase별 피해/경직/브레이크, HitBox 그룹을 확인한다.
- Commit/Variant owner/target/end Effect 적용 시점과 대상을 확인한다.
- 종료·취소·캐릭터 교체 후 Task, Effect, Tag가 잔류하지 않는지 확인한다.
- 몬스터는 BT 후보 선택, 거리 게이트, 자기 MotionSet 해석을 확인한다.
- 태그 Trigger는 중복 발화, 선점, retrigger, OwnedTagPresent 제거 취소를 확인한다.
- 선점형 Request는 기존 주 실행 중에도 요청이 발행되고 `CancelExisting`으로 기존 실행이 정리된 뒤 새 실행이 Commit되는지 확인한다.

Runtime Sandbox는 ASC 수직 슬라이스를 확인하지만 실제 게임 씬의 상태 머신, Motion, 히트, 카메라, UI 스모크를 대체하지 않는다.

## Diff 안전 검사

- `git diff -- Assets/10.Datas/Ability Assets/Resources/GameplayTagRegistry.asset`을 확인한다.
- 프리팹을 연결했다면 `git diff -- Assets/03.Prefabs`를 확인한다.
- 새 `.asset`과 `.meta`가 함께 존재하고 GUID가 중복되지 않는지 확인한다.
- 의도하지 않은 자동 재직렬화, 대량 YAML 순서 변경, null managed reference, VFX 유실이 없는지 확인한다.
- 검증이 만든 자동 변경만 식별해 처리하고 기존 사용자 데이터 변경은 보존한다.
- 사용자 요청이 없으면 커밋하지 않는다.
