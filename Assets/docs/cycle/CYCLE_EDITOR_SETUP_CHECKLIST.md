# Cycle P0 에디터 수동 설정 체크리스트

이 문서는 `02_WORLD_SPAWN_ENCOUNTER_SPEC.md`부터 `07_CYCLE_UI_TELEMETRY_VALIDATION_SPEC.md`까지의 런타임 코드 구현 후 Unity 에디터에서 직접 연결해야 하는 작업만 정리한다.

> 2026-07-14 코드/에셋 대조 검토 반영. 체크된 항목은 에셋 직렬화 값 또는 코드로 확인된 것이며, 플레이 모드 검증이 필요한 항목은 미체크로 남긴다.

## 1. 공통 설정 에셋

- [x] `Create > UPlayGround > 사이클 > 공통 설정`으로 `CycleConfigSO`를 생성한다. — `10.Datas/Cycle/P0/CycleConfig_P0.asset`
- [x] 사이클 1~3 난이도 항목을 각각 아래 값으로 확인한다.
  - Cycle 1: HP `1.00`, 공격 `1.00`, 보상 `Common`
  - Cycle 2: HP `1.35`, 공격 `1.18`, 보상 `Rare`
  - Cycle 3: HP `1.75`, 공격 `1.38`, 보상 `Heroic`
- [ ] `Unsettled Material Item Ids`에 사이클 중 즉시 인벤토리에 넣지 않을 재료 Item ID를 등록한다. ⚠️ 현재 비어 있음 — 재료 픽업이 원장으로 라우팅되지 않는다.
- [x] 경험치 손실률 `0.30`, 미정산 재료 드롭 활성, 장비 파편 손실 비활성을 확인한다.
- [x] `Create > UPlayGround > 사이클 > 월드 설정`으로 맵별 `CycleWorldConfigSO`를 생성한다. — `CycleWorld_lakeoflife.asset`
- [x] `Map Id`를 씬의 `SceneContext.MapID`와 정확히 일치시킨다. — `LakeOfLife` 일치 확인
- [x] 외곽/중앙 보스 Actor ID가 모두 `ActorDatabase`에 등록돼 있는지 확인한다. — MonsterHonoka/MonsterBokusei/MonsterHichi/MonsterLili
- [x] 외곽 보스 수 `3`, 동일 섹터 최대 수 `1`을 기본값으로 설정한다.

## 2. 사이클 플레이 씬 저작

- [ ] 씬 루트에 `CycleWorldContext`를 하나 배치한다.
- [ ] `Run Config`에 `CycleConfigSO`, `Config`에 해당 맵의 `CycleWorldConfigSO`를 연결한다.
- [ ] 플레이어 시작/외곽 보스 후보마다 `CycleSpawnPoint`를 배치한다.
- [ ] 모든 `Spawn Id`를 맵 안에서 유일하고 영구적인 문자열로 지정한다. 이후 이름을 변경하지 않는다.
- [ ] 역할 플래그를 `Player`, `OuterBoss`, `Respawn` 중 필요한 조합으로 지정한다.
- [ ] 플레이어 후보의 `Safety Radius`가 주변 외곽 후보를 적절히 제외하는지 Scene 기즈모로 확인한다.
- [ ] 외곽 후보의 `Sector Id`를 설정한다. 빈 문자열도 하나의 동일 섹터로 취급되므로 실제 섹터를 구분한다.
- [ ] 중앙 아레나에 `CentralBossSpawnPoint`를 정확히 하나 배치한다.
- [ ] 활성 부활 지점마다 `CycleRespawnPoint`를 추가한다.
- [ ] `CycleSpawnPoint`의 Respawn `Spawn Id`와 같은 오브젝트의 `CycleRespawnPoint.Respawn Id`를 동일하게 맞춘다.
- [ ] 탈출용 `PortalActor`의 `Is Cycle Exit Portal`을 활성화하고 목표 씬/도착 지점을 설정한다.
- [ ] 중앙 보스 처치 전 포털이 비활성이고 처치 후에만 활성화되는지 확인한다.
- [x] 사이클 시작 버튼·트리거·개발 치트에서 `CycleRunManager.Instance.StartNewCycle()`을 호출하도록 연결한다. 고정 시드 검증은 `StartCycle(cycleIndex, seed)`를 사용한다. — `UI_TitleMenu` 새 게임에서 `RequestStartNewCycleOnNextWorld()` 호출 연결됨

## 3. 캐릭터 무게 프로필

- [x] `Create > UPlayGround > 사이클 > 캐릭터 무게 프로필`로 Light/Standard/Heavy 프로필을 생성한다. — `CharacterWeight_{Light,Standard,Heavy}.asset`
- [x] Honoka: 이동 `1.15`, 템포 `1.25`, 피해 `0.70`, 브레이크 `0.55`, 회피 `0.45`초로 설정한다.
- [x] Bokusei: 이동/템포/피해/브레이크 `1.00`, 회피 `0.35`초로 설정한다.
- [x] H09: 이동 `0.82`, 템포 `0.68`, 피해 `1.80`, 브레이크 `2.10`, 회피 `0.24`초로 설정한다.
- [ ] `VitalRecoveryPolicySO`를 무게별로 생성해 일반 히트와 브레이크 특수공격의 확률·개수·회복 배율을 설정한다. ⚠️ 에셋 3종은 존재하나 `m_Script: {fileID: 0}`으로 스크립트 참조가 깨져 있음. `VitalRecoveryPolicySO`가 `CharacterWeightProfileSO.cs` 안의 두 번째 클래스라 MonoScript가 연결되지 않는다 → 클래스를 `VitalRecoveryPolicySO.cs`로 분리한 뒤 에셋을 재생성/재연결해야 한다. (`BossAssistDatabase_P0.asset`도 동일 문제)
- [x] 각 캐릭터 모델의 `CharacterModelData.Weight Profile`에 해당 프로필을 연결한다. — Player.prefab에서 Bokusei=Standard, Honoka=Light, H09=Heavy 연결 확인
- [ ] 공격 템포 값은 메타데이터다. MotionSet 전체 시간축을 실제로 조정하려면 애니메이션, 이벤트, 캔슬 윈도우가 함께 스케일되는 저작 경로를 별도로 검증한다.

## 4. 보스 어시스트 데이터와 프리팹

- [ ] 보스별 `BossAssistDefinitionSO`를 생성한다. ⚠️ 정의 에셋 0개, `BossAssistDatabase_P0.asset`의 `definitions: []` 비어 있음 + 스크립트 참조 깨짐(3절 참고)
- [ ] 저장용 `Assist Id`를 유일하고 변경되지 않는 문자열로 지정한다.
- [ ] `Source Boss Actor Id`를 `ActorDatabase` ID와 일치시킨다.
- [ ] 역할, 아이콘, 쿨다운, 최대 실행 시간, 배치 정책, 대상 필요 여부를 설정한다.
- [ ] 중앙 보스 영입을 허용하는 데이터만 `Recruitable From Central Boss`를 활성화한다.
- [ ] 어시스트 모델 프리팹은 일반 `MonsterActor` AI 프리팹을 그대로 사용하지 않는다.
- [ ] 프리팹에 이동/추적 AI, Hurtbox, 적 진영 충돌이 남아 있지 않은지 확인한다. 런타임에서도 Collider/Rigidbody 충돌은 비활성화된다.
- [ ] 실제 공격·버프·디버프 효과가 필요한 프리팹에는 `IBossAssistEffectExecutor` 구현 컴포넌트를 추가한다.
- [ ] 효과 구현은 완료 시 전달된 콜백을 정확히 한 번 호출하고 `Cancel()`에서 Hitbox·이벤트를 모두 정리한다.
- [ ] 회복형 단순 프로토타입은 `Heal Amount`만으로도 실행할 수 있다.
- [ ] 모든 정의를 `BossAssistDatabaseSO`에 등록한다.
- [ ] 씬에 `BossAssistBootstrap`을 배치하고 DB를 연결한다.
- [ ] `BossAssistManager.OnDuplicateRecruitRewardRequested`를 각인 파편 지급 시스템에 연결한다.

## 5. Input Actions

- [x] 프로젝트 Input Actions 에셋의 `PlayerAction` 액션 맵에 `BossAssist` 버튼 액션을 추가한다.
- [ ] 키보드와 게임패드 바인딩을 입력표에 맞게 지정한다. 코드에는 특정 키가 하드코딩되어 있지 않다. ⚠️ 키보드 `Q`만 바인딩됨, 게임패드 바인딩 누락
- [x] 기존 `CharacterSwap_1`~`CharacterSwap_4` 바인딩은 변경하지 않는다. — diff에서 스왑 바인딩 변경 없음 확인
- [x] 생성된 C# 래퍼를 사용 중이면 Input Actions 에셋 저장 후 래퍼를 재생성한다. — `PlayerInputActions.cs` 재생성됨
- [ ] 플레이 모드에서 어시스트 입력이 공격 입력 버퍼나 캐릭터 스왑과 충돌하지 않는지 확인한다.

## 6. 유해 프리팹

- [x] 상호작용 Collider와 시각 모델이 포함된 `RemainsActor` 프리팹을 만든다. — `10.Datas/Cycle/P0/RemainsActor_P0.prefab` (트리거 Collider 확인)
- [ ] 유해 프리팹은 공격·피격·적 어그로 대상이 되지 않도록 레이어와 Collider를 설정한다. ⚠️ 현재 레이어가 `Default(0)` — 상호작용/전투 레이어 규약에 맞는지 확인 필요
- [ ] `CycleWorldContext.Remains Prefab`에 프리팹을 연결한다.
- [ ] 상호작용 UI가 `IInteractable` 대상을 정상 표시하는지 확인한다.
- [ ] 활성 부활 지점이 없는 테스트도 수행해 시작점/사망 위치 폴백 동작을 확인한다.

## 7. 미니맵과 나침반

- [ ] 각 맵의 `MinimapIconConfigSO`에 다음 스프라이트를 지정한다. ⚠️ 필드는 추가되었으나 모든 스프라이트가 미지정(`fileID: 0`) — 개발 빌드에서 경고 발생 예정
  - `Unknown Boss`: 외곽/중앙 공통 `?`
  - `Discovered Outer Boss`
  - `Discovered Central Boss`
  - `Remains`
  - `Active Rest Point`
- [x] `Show Cycle Boss Markers`, `Show Remains Marker`를 활성화한다. — LakeOfLife 등 맵 config에서 활성 확인
- [ ] `Unknown Boss`가 등급·속성을 색으로 암시하지 않는지 확인한다.
- [ ] 나침반 Canvas에 `UI_CycleCompass`를 추가하고 Container, Image 프리팹, Icon Config를 연결한다.
- [ ] 나침반 아이콘 프리팹의 앵커·크기·레이캐스트 옵션을 HUD 규칙에 맞춘다.
- [ ] 미니맵에서 `?` 목적지를 직접 선택하는 UI가 있다면 `CycleTelemetrySession.RecordMarkerSelected(spawnId, worldPosition)`를 호출한다.

## 8. 사이클 HUD와 피드백 UI

- [ ] 게임플레이 HUD 아래에 `UI_CycleHud`를 추가하고 사이클 번호, 시드, 경과 시간 TMP 텍스트를 연결한다.
- [ ] `UI_CycleEncounterBanner`를 추가하고 제목 TMP와 CanvasGroup을 연결한다.
- [ ] `UI_BossAssistHud`를 추가하고 아이콘, 쿨다운 fill, 남은 초, CanvasGroup을 연결한다.
- [ ] 조우 배너 이벤트에 BGM 전환과 보스 HP바 표시를 프로젝트 연출 시스템에서 연결한다.
- [ ] 전멸/회수/재사망 알림은 `CycleRemainsManager`의 `OnRemainsCreated`, `OnRemainsRecovered`, `OnRemainsDiscarded` 이벤트에 연결한다. ⚠️ 알림 UI 클래스 자체가 미구현 — 현재 구독자는 텔레메트리뿐
- [ ] 정산 화면은 `CycleRunManager.OnSettlementCommitted` 이벤트에서 `CycleSettlementPlan`을 받아 표시한다. ⚠️ 정산 화면 UI 클래스 자체가 미구현
- [ ] 로스터가 가득 찬 영입은 `AssistRosterService.PendingRecruitAssistId`를 사용해 정산 화면에서 방출/포기 결정을 받도록 연결한다. ⚠️ 정산 화면 선행 필요

## 9. 검증과 텔레메트리

- [ ] Unity 메뉴 `UPlayGround > 사이클 > P0 현재 씬 검증`을 실행해 오류가 0개인지 확인한다.
- [ ] 고정 시드로 두 번 시작해 플레이어/외곽/중앙 `spawnId + actorId` 조합이 같은지 확인한다.
- [ ] 미발견 사이클 보스가 일반 적 아이콘으로 중복 표시되지 않는지 확인한다.
- [ ] 외곽 보스 영입 확률, 브레이크 마무리 보너스, 노히트 보너스, 실패 천장을 검증한다.
- [ ] 전멸 후 유해 하나 생성, 재전멸 시 기존 유해 폐기, 회수 시 경험치/재료 복원을 확인한다.
- [ ] 중앙 보스 처치만으로 미정산 재료가 인벤토리에 들어오지 않는지 확인한다.
- [ ] 탈출 포털을 연속 진입해도 정산이 한 번만 적용되는지 확인한다.
- [ ] 개발 빌드 텔레메트리 JSON은 `Application.persistentDataPath/cycle_telemetry`에서 확인한다.
- [ ] 스펙 `07`의 수동 검증 시나리오 A~E를 모두 수행한다.
