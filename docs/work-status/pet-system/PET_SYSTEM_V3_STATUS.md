# 펫 시스템 V3 — 전투 지원·성장 고도화 상태

- 설계(확정): [PET_SYSTEM_V3_DESIGN.html](../../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V3_DESIGN.html)
- 상태: **P1 구현·범위 자동 검증 완료. 최종 저장 보완 후 EditMode 46/46·PlayMode 9/9 통과, 펫 GA/GE 30개 오류 0·HUD 참조 정상. 전수 GAS 검사는 범위 밖 오류 7·경고 599로 실패. 실제 플레이 인수는 사용자 테스트 대기. P2·P3 미수행.**
- 갱신일: 2026-11-14

## 확정 결정

- 신호 반응형 지원(명령 입력·신규 단축키·직접 피해 없음) — 사용자
- 유대는 전투·생활·퀘스트 세 원천으로 점진 적립, 목표 시간 미정(SO 조정) — 사용자
- 유대 단위는 펫 전체 공통(세이브당 하나, `sharedBondPoints` 신규, 기존 펫별 `bondPoints`는 보존만, 마이그레이션은 최댓값) — 사용자
- 분기 보류, 브레이크 신호 P3 연기, 대시 무적 회피 제외 — 검토 추천 채택(되돌리기 쉬움)

## P0 조사 결과 (정적 조사, Unity 실행 없음)

| 항목 | 결과 |
|---|---|
| 파티 교체 | 같은 `PlayerActor` 위에서 모델만 교체. 펫 효과 15종은 `PersistOnPlayerActor`라 교체 후에도 남음. 중복 우려 해소 |
| 현행 펫 수치 | 공격 +4~12%/8초, 방어 +6~18%/8초, 회복 4~8% 최대 체력, 공용 쿨다운 30초 |
| 겹치는 파티 패시브 | 퍼펙트 회피 집중 공격 +6%/5초, 퍼펙트 가드 강화 방어 +5%/5초 |
| 원숭이 모션 | Idle·Walk·Run·Sit(탑승)만 등록. 반응 클립 없음. FBX 임베드 클립은 미확인 |
| 반영한 설계 변경 | 공용 쿨다운 폐지·슬롯별로 분리, 회복 수치·쿨다운 재설정, 반응은 FX 중심 |

## 미확인 (구현 중 확인)

- FBX `Chibi_Monkey.fbx` 임베드 클립 목록 (Unity에서 확인)
- 잠으로 날을 넘길 때 일일 상한이 일찍 초기화되는 문제의 보완 충분성(P2)
- 위 수치의 실제 체감 (P1 사용자 테스트)

## 단계

| 단계 | 상태 |
|---|---|
| P0 조사 | 완료(정적) |
| P1 트리거 데이터화·마이그레이션·쿨다운 재구성 | 구현·최종 범위 자동 검증 완료·사용자 테스트 대기(전수 GAS 범위 밖 오류 별도) |
| P2 유대 3원천·2슬롯·메뉴 | 미수행 |
| P3 브레이크 중계·교란·분기 재검토 | 미수행 |

## P1 반영 (2026-11-14)

- `PetSupportTriggerPolicy`: HealthBelow·PerfectDefense·HeavyHitTaken만 도입. 임계값·내부 쿨다운·지원 가능 필수·전투 필요·허용 피격 태그를 SO에서 관리한다. 체력은 기존 0.25초 점검만 사용한다.
- `PetSupportController`: PlayerActor 완벽 회피/가드 이벤트와 GAS EventSent를 구독·해제한다. 선택한 지원 하나만 평가하고 지원 불가 시 신호를 폐기한다. 직접 피해·게이지 충전·대기열은 추가하지 않았다.
- 기존 15 GA/15 GE를 재사용한다. 기세 10초·버팀 15초·응급 회복 45초, 전역 최소 간격 8초. 공격·방어 수치/8초 지속은 유지하고 회복만 최대 체력 8/10/12/14/16%로 변경했다.
- 슬롯별 독립 그룹은 지원 ID에 고정한다. 선택·강화·펫 변경으로 쿨다운을 우회하지 않도록 미장착 지원의 남은 시간도 저장한다. 정책 내부 쿨다운을 카탈로그 인스펙터에서 수정하면 GA 파생값이 함께 바뀐다.
- 세이브 v3의 `supportSlots`는 슬롯 1개만 실행한다. `selectedSupportId`는 호환 필드이며 슬롯 1로 이전한다. 알 수 없는 ID/쿨다운과 기존 펫별 bondPoints를 보존한다. sharedBondPoints·Ledger·유대 수집·두 번째 슬롯은 만들지 않았다.
- 표시 이름·기존 메뉴의 발동 조건 안내·플레이어 FX와 고정 HUD 지원 아이콘/불가 표시/점멸을 연결했다. 반응 모션은 제작하지 않았다. 펫 위치 FX 키는 비워 두며 연출 확장은 P2다.

### 설계 구체화와 확인된 경계

- Astra 검토로 Heavy·KnockBack·Stun·Airborne·Knockdown·Grab을 큰 피격으로 결정했다. Light·Hit·Pull은 제외한다. 실제 발행 GAS 태그 기준이며 받은 피해를 취소하지 않는다. [설계 §5·§8](../../../Assets/docs/design/ui-player/pet-system/PET_SYSTEM_V3_DESIGN.html#structure)에 반영했다.
- 버팀은 기존 방어 Attribute 강화다. 설계 표의 ‘경직 완화’를 신규 경직 취소 효과로 확장하지 않았다(사용자 P1 범위가 현행 방어 수치 유지로 한정).
- 실제 `Combat.Defense`는 0~0.6 비율 속성이다. 기존 Percent +18%는 기준 방어 0.2를 0.236으로 만든다. 설계의 파티 패시브 합산 +23%는 Percent 계수의 합산 상한이며 피해 감소율 +23%p가 아니다. 기본 방어 0에서는 상승량도 0인 현행 특성을 유지했다. 버팀의 체감 유효성은 PV03에서 확인한다.
- Hot Reload는 타입·필드 추가 및 대상 allowlist 밖 변경이므로 미지원. Bridge 상태 `completed/Ok`, `Disabled` 확인 후 기존 Editor 정상 컴파일을 사용했다. 새 Editor/Player/Addressables 빌드·커밋은 하지 않았다.
- 도중 범위 밖 ProjectileRuntime의 컴파일 오류 2건을 관측했으나 별도 동시 수정으로 해소됐다. 해당 파일은 변경하지 않았다.

### 검증 진행

- 정상 컴파일 후 새 P1 타입·메서드 조회 성공, Console 컴파일 오류 0 확인.
- Unity API로 GA/GE·HUD 이전 후 실제 그룹과 HUD 컴포넌트 조회 성공. 실행 응답 연결이 중간에 닫혔지만 디스크 diff와 후속 Unity 조회로 적용을 확인했으며 중복 실행하지 않았다.
- AbilitySet 역참조 조사: 펫 GA를 참조하는 정규 AbilitySet 0개. 기존 임시 부여 경로를 유지한다.
- FX `PlayerHeal`·`playerGuardFX`·`playerFullChargeFX`는 모두 존재하며 파티클 loop=false 확인. 시각적 적합성은 사용자 확인 대기다.
- 첫 EditMode는 43건 중 42 통과·1 실패였다. 방어를 100→118로 기대한 기존 테스트가 레지스트리 방어 상한 0.6과 맞지 않아 실패했다. 효과를 바꾸지 않고 기준값 0.2→0.236 검사로 수정했다. [첫 실행 원본](../../../output/harness/20261114-145934-b51e08a6/editmode-first.json). 최종 재검증 결과는 아래에 갱신한다.
- 저장 경계 최종 보완 전 관련 EditMode **46/46 통과**, 실패·건너뜀 0건. [Test Runner 원본](../../../output/harness/20261114-145934-b51e08a6/editmode-before-save-fix.json). 대상: PetV2RulesTests·PetSaveRulesTests·PetSupportAbilityTests·PetUpgradeTransactionTests·PetV2AssetTests.
- 펫 GA/GE 선택 검사 **30개·오류 0건**. [원본](../../../output/harness/20261114-145934-b51e08a6/pet-data-validation.txt).
- 전체 GAS 검사는 **오류 7·경고 599로 실패**. 모두 펫 폴더 밖 오류이며 자동 Fix·범위 밖 수정은 하지 않았다. [전체 원본](../../../output/harness/20261114-145934-b51e08a6/gas-validation.txt). 오류: PlayerDefaultAttackData의 Ability/Ultimate HitPhase 누락 2건, CharacterPassiveDatabase의 패시브 ID 중복 4건·Triggered Effect 참조 누락 1건.
- PlayMode는 초기화 대기·동시 외부 테스트·리로드가 겹쳤다. 펫 필터로 요청한 MCP job `7c9250a596ec451bbdc562c9430d453e`에 FaceStripPlayModeTests 2건 완료가 연결되어 [필터 불일치 원본](../../../output/harness/20261114-145934-b51e08a6/playmode-mismatched-result.json)을 보존했으며 펫 통과로 인정하지 않았다. 이후 실행 상태를 읽기 전용으로 확인하고 펫 필터로 재실행했다. `clear_stuck`·강제 Play 종료·Editor 재시작·전역 설정 수동 변경은 하지 않았다.
- 실제 첫 펫 PlayMode 실행은 9/9건이 준비 단계에서 실패했다. 테스트용 PetBrain 생성 시 숨김으로 꺼지는 모터를 안전 지상 합류 상태로 복구하지 않았던 픽스처 오류다. 테스트 준비만 수정했으며 [첫 실패 관측 요약](../../../output/harness/20261114-145934-b51e08a6/playmode-first.json)을 보존한다. 원본 상세는 리로드 뒤 복원되지 않아 관측 요약과 원본을 구분한다.
- 저장 경계 최종 보완 전 펫 PlayMode **9/9 통과**, 실패·건너뜀 0건. [Test Runner 작업 기록](../../../output/harness/20261114-145934-b51e08a6/playmode-before-save-fix.json), job `5d91685358404984ab0577d84d74e024`. 리로드 후 상세 result는 복원되지 않았으나 `succeeded`·completed=total=9·실패 목록 0·마지막 펫 fixture 이름이 보존되어 확인됐다. 완벽 회피/가드→기세, 35% 경계→회복, 큰 피격→버팀, 지원 불가 신호 폐기, 파티 교체 효과 유지·지원별/전역 간격, 활성 효과 재발동 거절, 구독 해제, 저장 복원을 검사했다. 합성 접지·이벤트 기반 수직 검사이며 실제 전투 입력/시각 품질 인수는 아니다.

### 최종 저장 경계 보완·이전 재검증 차단 기록

- 마지막 정적 검토에서 `Capture`가 잔여 초만 저장하고 GAS 사용 가능 횟수를 기본 0으로 두는 문제를 확인했다. `AbilityCooldownRuntime.Normalize`는 NextChargeTime<=0에서 횟수를 회복하지 않으므로 잔여 0인 지원은 복원 후에도 준비 상태를 명확히 보존해야 한다.
- `PetSupportController.Capture`에 availableCharges·maxCharges·rechargeDurationSeconds 보존을 추가했다. EditMode에 준비 횟수=1 검사를, PlayMode 저장 복원 검사에 ‘전역 간격 종료 후 저장 당시 준비된 버팀이 발동’ 단언을 추가했다. 이 보완은 위 46/46·9/9 실행 **이후**이며 해당 통과를 새 소스의 통과로 승계하지 않는다.
- 소스 저장 시각 15:36:43~44, 관련 Actor/Ability.Tests/Ability.PlayModeTests DLL 갱신 시각 15:37:55를 확인했다. 이는 보조 파일 근거이며 최종 Test Runner 재실행·Editor 반영 확인을 대신하지 않는다.
- 재실행 도중 다른 작업의 컴파일·테스트가 계속 관측됐다. 마지막에는 MCP가 `Connection closed`·`Timeout`·`No Unity Editor instances found`를 반환했다. 별도 Bridge status `74146f8d560e4892aaf19a17ad7ad823`도 5초 내 응답을 관측하지 못했다. Editor PID 21792는 유지되어 새 Editor를 열거나 종료하지 않았다. 전역 옵션·잠금 파일·Library·Temp를 조작하지 않았다.
- **당시 다음 행동(아래 재개 검증에서 해소):** 같은 Editor의 정상 응답이 복구되면 먼저 실제 실행 중인 작업/필터가 없는지 확인하고 정상 컴파일 상태와 Console을 재조회한다. 위 EditMode 5 fixture와 `PetSupportSignalPlayModeTests` 9건을 최종 소스에서 다시 실행한다. 외부 fixture 결과나 초기화 0건은 펫 통과로 인정하지 않는다.

### 재개 검증 (2026-11-14 19시 이후)

- 사용자 재개 요청 후 현재 변경을 다시 확인했다. 기존 펫 소스·데이터의 최종 준비표 입력 해시가 그대로 일치한다. 다른 전투·UI 작업 변경은 보존한다.
- 새로 열린 동일 프로젝트 Editor(PID 31180)의 초기화가 끝나고 MCP `UPlayground@fddd0702` 연결이 등록됐다. 추가 Editor를 실행하거나 기존 Editor를 종료하지 않았다. 실제 프로젝트 경로·Edit Mode·컴파일 완료·`scriptCompilationFailed=false`와 Console 오류 0을 확인한 후 검사를 시작했다.
- **최종 저장 보완 후 EditMode 46/46 통과**, 실패·건너뜀 0. job `09a40fe374a841649e0ca0f11fe6540a`. [최신 실행 원본](../../../output/harness/pet-v3-p1-recheck-20261114/editmode.json). 준비된 지원의 사용 가능 횟수 보존 단언을 포함한다.
- **펫 GA/GE 30개 오류 0**, 정책/GA 쿨다운 일치, 전역 간격 8초, HUD 아이콘·상태 참조 유효. [최신 데이터/HUD 검사](../../../output/harness/pet-v3-p1-recheck-20261114/data-hud.json). 저장·재직렬화 없이 읽기 전용으로 검사했다.
- 결과 회수 시 사용자가 CombatTestMap을 플레이 중인 상태가 확인됐다. 현행 에이전트 Play 종료 제한을 존중해 강제 종료하지 않고 직접 종료를 요청했다. 이후 실제 Edit Mode 복귀·컴파일 오류 없음을 확인한 뒤 동일 Editor의 Test Runner로 진행했다.
- **최종 저장 보완 후 PlayMode 9/9 통과**, 실패·건너뜀 0. job `c261f67c44ab4de69591c1d77fc44519`. [최신 실행 원본](../../../output/harness/pet-v3-p1-recheck-20261114/playmode.json). 펫 fixture의 마지막 테스트명과 9건 전체 성공을 확인했다. 전역 간격 종료 후 저장 당시 준비된 버팀이 실제 발동하는 최종 추가 단언도 포함한다. 이전 9/9의 승계가 아닌 최신 소스 재실행 결과다.
- 최종 후속 MCP 조회에서 Edit Mode·컴파일 중 아님·`scriptCompilationFailed=false`를 재확인했다. 소스 변경 없이 검증만 수행했다.
- **전수 GAS 재검사: 오류 7·경고 599로 실패.** [최신 원본](../../../output/harness/pet-v3-p1-recheck-20261114/gas-validation.txt). 오류 7건은 기존과 동일하게 PlayerDefaultAttackData의 HitPhase 누락 2건과 CharacterPassiveDatabase의 중복 ID 4건·Triggered Effect 참조 누락 1건이며 펫 폴더 밖이다. 사용자 범위를 넘는 수정을 하지 않았다. P1 범위 검사 통과와 프로젝트 전수 검사 실패를 구분한다.
- **재개 단계 종료:** 이전 Editor/MCP 차단과 최종 저장 보완의 자동 재검증 미완료 항목은 해소했다. 남은 P1 인수는 PV01~06의 실제 입력·반응 체감·FX/HUD 시각 품질이며 모두 사용자 테스트 대기다. P2·P3와 범위 밖 GAS 오류 수정은 이번 재개 범위에 포함하지 않았다.
- [재개 검증 인계·부산물 보존 기록](../../../output/harness/pet-v3-p1-recheck-20261114/review.md). 재생성/삭제할 임시 산출물은 없으며 기존 사용자 변경·추적 밖 HUD와 meta·다른 작업의 InitTestScene을 보존했다. 이번 검사로 기존 최종 준비표 입력 해시는 변하지 않았다. 설계 변경·코드/에셋 추가 수정·커밋·Player/Addressables 빌드는 없다.

### 변경 파일과 부산물

- [전체 변경 파일 목록](../../../output/harness/20261114-145934-b51e08a6/changed-files.txt). 런타임: PetSkillCatalogSO·PetSaveData·PetSupportController·PetManager 3파일·PetServices. UI: 펫 메뉴 3파일·UIPetSupportIndicator. 저작: PetSystemAuthoring.V2/V3. 데이터: PetSkills·기존 15 GA/15 GE. 테스트: 관련 EditMode 5 fixture·신규 PlayMode fixture·Content/PlayMode asmdef 참조. 설계·이 상태·플레이어 위키.
- `Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab`은 기존 `.gitignore:661`의 로컬 대상이다. HUD 지원 아이콘을 Unity API로 추가했으며 원본·`.meta`를 보존한다. Git 추적 파일이라고 보고하지 않는다. `PetSystem.ApplyV3P1`이 기존 HUD를 보존하면서 빠진 아이콘만 추가하는 재적용 경로다.
- GA/GE 저장 때 생긴 무관한 기본값 필드(environmentHeatImpulse 등)와 줍기 2종의 기본 정책 재직렬화만 식별해 원복했다. 지원 정책·표시 이름·쿨다운·회복량은 유지한다.
- 준비표·첫 실패·최종 결과·검증 원본은 `output/harness/20261114-145934-b51e08a6/`에 위키 근거로 보존한다. 새 테스트 씬은 Test Runner가 정리하며 다른 작업의 InitTestScene·사용자 파일은 임의 삭제하지 않는다. 별도 중간 에셋·구매 원본·클립 생성은 없다.
- [최종 검증 인계·정리 기록](../../../output/harness/20261114-145934-b51e08a6/review.md), [저장 보완 전 스냅샷](../../../output/harness/20261114-153556-5bbb0b8b/plan.json), [최종 소스 스냅샷](../../../output/harness/20261114-154227-560d8e68/plan.json). 최종 스냅샷 입력 해시 일치와 관련 diff --check를 확인했다. 해시 일치를 검증 통과로 해석하지 않는다.

### 사용자 테스트 명세 (모두 사용자 테스트 대기)

준비: Unity Editor의 기존 플레이 씬에서 펫을 동행하고 안전한 지상에서 지원 하나를 선택한다. 저장 백업이 있는 테스트 슬롯을 사용한다. 새 빌드를 만들지 않는다. 키보드/마우스 또는 패드 종류를 결과에 기록한다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| PV01 | 기세 선택 → 완벽 회피/완벽 가드 → 대시 무적 회피와 일반 공격 상쇄 비교 | 완벽 회피/가드에만 FX·아이콘 점멸·공격 강화. 대시 무적/상쇄에는 미발동. 10초 내 재발동 없음 |
| PV02 | 응급 회복 선택 → 체력 35% 초과/이하 비교 → 전투 밖 체력 결손 유지 | 35% 이하에서만 8~16% 회복, 45초 대기. 35% 초과 상시 회복 없음 |
| PV03 | 버팀 선택 → 일반/큰 피격 비교 | 위 6종 큰 피격 뒤 방어 강화 8초·15초 대기. 현재 피해·잡기는 취소하지 않음 |
| PV04 | 지원 직후 파티 캐릭터 교체 → 효과·쿨다운 확인 → 메뉴 선택 변경·저장/로드 | 남은 효과·지원별 쿨다운·전역 간격 유지. 변경으로 리필되지 않음 |
| PV05 | 거리 초과·벽 뒤·수영·활공 동승·중단 중 신호 → 안전한 지상 복귀 | 불가 표시. 버린 신호가 복귀 후 발동하지 않고 쿨다운도 소비하지 않음. 새 신호에만 반응 |
| PV06 | 완벽 회피 직후 도움의 시점·FX·HUD를 반복 관찰 | 반응을 알아볼 수 있고 타이머 지원보다 만족스러운지 사용자 판단. 위치·겹침·점멸 가독성 포함 |

실패 회신: `ID / 통과·실패·미수행 / 실제 증상 / 지원·레벨·체력 / 입력 장치 / 필요 시 Console·영상`. 자동 신호 수직 검사와 실제 전투 조작·시각 품질은 별개다.
