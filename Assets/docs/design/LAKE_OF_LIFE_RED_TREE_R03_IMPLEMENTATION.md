# 붉은 나무 R01 잔여 확인 · R02 · R03 구현 기록

> 최신 실행은 12절. **R03 본편 연결·전환과 최종 Windows Player 라온/아린 완주를 수행했다.** 영입 직후 저장 및 모델 준비 전 교체 결함을 수정하고 최종 두 경로를 재검증했다. 관련 EditMode 28/28, 전체 Play Mode 24/24, 최종 빌드 오류 0·경고 22, 각 Player 실행 오류 0. 기존 전체 EditMode 실패·타이틀 프리뷰 표시 문제·실물 조작 품질 미검증은 남아 있으며 프로젝트 전체 통과 선언이 아니다.

> 11절 작성 당시 기록: 검증 씬의 두 주인공 완주와 취소·저장·씬 복원을 확인하고 본편 실행선을 전환했다. 전체 EditMode 923개 중 906 통과·17 실패, 당시 전체 Play Mode 24/24 및 R03 EditMode 19/19 통과. 후속 수정과 최종 Player 결과는 12절을 따른다.

## 1. 초기 준비 시점의 상태

1~8절은 준비 당시의 이력이다. 비활성·미연결·미수행이라는 문구를 현재 상태로 해석하지 않는다. 이후 실제 연결과 본편 전환은 9절 이후의 실행 기록을 따른다.

| 단계 | 수행 | 남은 필수 조건 |
|---|---|---|
| R01 잔여 확인 | 현행 파일의 씬 러너 7개, NPC 직접 대화 연결, Quest 자동 연계, Boot 선택/파티 코드, 화자·모델 경로, 기존 화린 발화 재대조. 라온·아린·화린 기존 초상화 이미지 열람 | 현행 Unity 공간·3D 모델·장비·에셋 로드·두 주인공 직접 플레이. R00의 과거 이미지/해시는 이 조건을 대체하지 않음 |
| R02 | [실행선·ID 대응표와 복사 슬롯 이관 설계](LAKE_OF_LIFE_RED_TREE_R02_TRANSITION.md), 복사 이관 API, 정상 동의/안전 사실 계약 보완 | 구 실행선 실제 퇴역, 생성기 재배선 방지 적용, 실제 원본/복사 슬롯 비교, 안전 재개 스폰 검수 |
| R03 | Flow 2개, 대화 7개, Quest 3개, 조우 정의 1개. 정상 동의/취소·보유 분기·저장 사실 재통지 경로 작성 | 실제 행동 생산자·배치·NPC 연결·Quest 활성화·안내/UI·복구 통합·Unity 테스트. S00~S02 완주 불가 |

공간 미확인 때문에 기존 씬을 새 Flow로 부분 교체하지 않았다. 새 퀘스트는 `isContentEnabled=0`이고 자동 새 게임 수락도 꺼져 있다. 기존 씬·프리팹·전투 Ability/BT/MotionSet은 수정하지 않았다. 구 보물·분신 실행선은 **아직 현행 씬에 남아 있다.** 새 본편에서 차단됐다고 보고하지 않는다.

## 2. 코드와 준비 데이터

| 파일/경로 | 변경 내용 |
|---|---|
| `Data/Save/RedTreeSaveMigration.cs` | 원본에서 읽은 복사본만 구 활성 퀘스트와 새 사건을 분리. 선택 주인공 불명확 시 실패. 새 완료 사실을 만들지 않음. 버전 플래그에 따른 재이관 멱등성 |
| `Manager/Save/SaveManager.cs`, `SaveManager.RedTree.cs` | partial 분리 및 `TryCreateRedTreeCopy`. 빈 대상 슬롯만 허용, 암호화 파일 재독해 후 덮어쓰기 없는 이동. 기존 ActiveSlot·원본·현재 플레이 유지. 초기화/자동 로드에는 연결하지 않음 |
| `Data/Story/RecruitmentEncounterDefinitionSO.cs`, `Contracts/RecruitmentEncounterServices.cs` | 영입 선행 안전 사실과 실패 결과 추가. 기존 정의는 빈 조건으로 기존 동작 유지 |
| `Manager/Story/StoryManager.RecruitmentEncounters.cs` | 정상 대화 증명 발급/영입 commit에서 선행 사실 확인, 실제 보유 확인 후 commit |
| `FlowGraph/Nodes/RecruitmentEncounterNodes.cs` | 이미 보유한 동료의 동의 대화 분기. 저장 재개 전용 Resume는 Dormant 전투를 시작하지 않음 |
| `FlowGraph/Nodes/RecruitCharacterByConsentNode.cs` | 전투 승리를 만들지 않는 비전투 합류. 정상 대화와 실제 보유 확인 후 사건 완료. 취소/실패는 완료 플래그 없음. 이번 화린 전투 Flow에서는 사용하지 않음 |
| `Gameplay/Encounter/Recruitment/Editor/RecruitmentEncounterAuthoringWindow.cs` | 기존 창에서 새 안전 사실 필드를 편집 가능하게 표시. 새 도구 진입점 없음 |
| `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/` | Flow 2, 대화 7, 조우 1. 기존 에셋 복제 시 새 콘텐츠 ID/GUID 부여, 기존 GUID 보존 |
| `Assets/10.Datas/Quest/RedTree/`, `QuestDatabase.asset` | 새 퀘스트 3개를 비활성 등록. 보상 0, Q02 다음 퀘스트 없음. DB의 기존 Refresh 기능은 비활성 항목을 제외하므로 활성 전환 때 등록을 다시 검사해야 함 |
| `Tests/EditMode/Content/RedTreeSaveMigrationTests.cs` | 8개 사례: 두 주인공/구 별칭, 기존 보유·기록 보존, 멱등성, 불명 주인공 거부, 다른 퀘스트 추적 보존 |
| `Tests/EditMode/FlowGraph/RedTreePreparedContentTests.cs` | 5개 사례: Unity 로드/managed reference/포트, 안전 확인 우회 금지, 행동 사실 위조 금지, 중복 보상·후속 자동 진행 금지 |

위 코드 경로는 `Assets/02.Scripts/` 기준이다. 신규 에셋의 실제 GUID 목록은 [에셋 목록](../../../output/RedTreeR02R03/asset-manifest.json)에 있다. 기존 에셋을 삭제하거나 GUID를 재발급하지 않았다.

## 3. 현재 진입점과 미연결 계약

**현재 Boot 진입은 기존 콘텐츠다. 새 도입의 플레이 가능한 시작·종료 지점은 아직 없다.** 새 Flow를 임의로 씬에 붙이거나 플래그를 주입해 전체 구현 통과로 간주하지 않는다.

씬 전환 준비가 끝난 뒤의 연결 계약:

- `FLOW_RedTreeIntroduction`: `MapReady`/`Resume`와 `Loaded`가 Q00 수락 후 저장 사실을 Q00→01→02 순서로 재통지한다. `Arrival`, `Meal`, `Care`, `Shelter`, `Handoff`는 실제 행동 사실을 확인한 뒤 정상 대화만 완료 처리한다.
- 상호작용용 `Interact_Arrival/Meal/Care/Shelter/Handoff`는 각각 `lake.red_tree.arrival/meal/care/shelter/handoff` 볼륨 ID를 소비한다. 기존 FlowGraphInteractable을 연결할 때 `_suppressPhysicsRouting=true`로 설정해야 접근만으로 대화가 열리지 않는다. 이 ID의 실제 씬 볼륨은 아직 없다.
- 행동 생산자 미연결: 짐 인도, 식사 인계와 자리, 돌봄 꾸러미 인계, 처치 자리 확보, 거처 사용, 안전 지점 도착 확인. 이 Flow는 이 사실들을 스스로 만들지 않는다. 각 행동의 기존 상호작용/전후 배치와 로드 복원 연결이 필요하다.
- `FLOW_RedTreeHwarinRescue`: 기존 Recruitment Anchor에 **새** 조우 정의/참가자 ID/트리거를 연결해야 한다. `Entry`의 `lake.red_tree.hwarin_entry`는 아직 씬 볼륨이 없는 바인딩 ID다. `Resume`는 Anchor의 저장 단계 재개용, `Consent`는 재상호작용용이다. `Loaded`는 이미 시작된 조우만 재개한다. `Safe`는 피난민 안전 사실 변경을 받는다.
- 전투 해결은 `escape_route_cleared`만 기록한다. `refugees_safe`는 안전 인계 상호작용이 별도로 생산해야 한다. 안전 사실 이후 정상 동의→실제 Roster→commit/finalize 순이다. Quest는 영입을 지급하지 않는다.
- 새 조우의 상대/적 종류·수·위치·참가자 ID는 아직 배치하지 않았다. 기존 조우 정의의 진영·전환 수치는 **재사용 후보**이며 이번 전투 감각 검수 결과가 아니다.
- 화린 활성 상태에서도 `Protagonist`는 최초 선택 인물이다. 기존 Party의 주인공 연출 lease와 종료 복원을 재사용한다. 실제 모델 교체 실패, 화린 대역 중복, 입력/HUD/카메라 복원은 아직 검증하지 않았다.
- 안전 조건이 있는 Anchor는 저장된 CombatResolved를 적용할 때 참가자 전투 정리와 접근 초기화까지만 수행한다. 안전 사실 확인 뒤 Flow의 `PrepareRecruitmentDialogueNode`가 접근을 시작한다. 7절의 코드 보완을 반영했으며 같은 씬 로드/사망/볼륨 내부 재개·중간 취소 후 재상호작용은 Unity 검증 대기다.

명시적 장면 스킵은 제공하지 않는다. 기존 일반 넘김과 Cancel만 사용하며 `continueWhenCancelled=0`이다. Cancel/Rejected는 완료 노드에 연결되지 않는다. 스킵이 전투·구조를 완료시키는 경로는 만들지 않았다.

## 4. 대사와 구현 경계

화린의 기존 발화는 반말이고, R01 해요체는 실물 검수 전 초안이었다. 준비된 S02 동의 대사만 “안쪽엔 아직 못 가 봤어”, “난 화린이야”, 보유 분기 “이번에도 같이 가자”로 일관되게 정리했다. 관계는 함께 퇴로를 확보한 동행 상대다. 외형에서 나이·성격·가족사를 추가하지 않았다. 현행 3D 모델·표정·음성과의 일치 검수는 여전히 남는다.

대화 7개는 행동 **후** 확인·동의용이다. 안내인의 인도 안내, 조이의 실제 자리 마련, 헤이즐의 처치, 소담/피난민 배치, 루시아의 실제 거처 개방은 이 대사 파일만으로 구현되지 않는다. 초상화는 기존 화자 테이블을 사용하며 실제 로드 검수가 필요하다. 릴리 화자/대역의 실물 연결도 미완료다.

Q02 끝은 릴리의 요청과 마을에서 채비하는 안내까지만 있다. 피해 마을로 향할 이유는 남기고 존재하지 않는 S03 목표/퀘스트를 자동 수락하지 않는다. 선택적 교체 안내는 기존 `EnsureCharacterUnlocked → GameMilestoneEvent.CharacterUnlocked → FirstTimeGuideConfig.Guide.Companion → GuidePopupData_FirstCompanion` 경로를 재사용한다. 기존 문안에 출전 편성/교체 설명이 있고 GameGuideManager는 대화·연출이 끝날 때까지 표시를 미룬다. 이미 보유하면 새 알림을 중복 발행하지 않는다. 코드·등록 확인이며 **실제 팝업 표시와 입력 검수는 미수행**이다. 새 Flow에는 교체 입력을 기다리는 진행 조건이 없다. 이후 장면과 루시아의 생사·처우는 구현/확정하지 않았다.

## 5. 검사 결과와 증거

| 구분 | 실제 결과 |
|---|---|
| 파일 조사 | [실행선 감사](../../../output/RedTreeR02R03/execution-audit.json), [작업 전 상태](../../../output/RedTreeR02R03/status-before.txt), [기존 변경 파일 해시](../../../output/RedTreeR02R03/user-files-before.json) 기록 |
| 파일 정적 검사 | 신규 에셋 13개 GUID 참조/충돌·퀘스트 비활성 등록, 두 Flow의 노드 54/18개와 연결 60/23개, 안전/돌봄 조건 우회 경로를 확인. 오류 0. Unity import/직렬화 왕복은 아님. [결과](../../../output/RedTreeR02R03/static-check.json) |
| 이미지 열람 | 기존 라온/아린/화린 초상화 PNG. Unity 현행 모델 검사 아님 |
| 보조 CLI 컴파일 | Data/FlowGraph/Content.Tests/FlowGraph.Tests 통과. 새 파일은 생성 csproj를 수정하지 않고 임시 MSBuild include로 포함. [Flow 로그](../../../output/RedTreeR02R03/compile-flow.log), [Content 테스트 컴파일](../../../output/RedTreeR02R03/compile-tests.log), [Flow 테스트 컴파일](../../../output/RedTreeR02R03/compile-flow-tests.log) |
| Assembly-CSharp 보조 컴파일 | 실패. 기존 Actor.csproj가 현재 없는 `PlayerAttackFacing.cs`, `PlayerAttackTranslation.cs`를 참조. 사용자 파일을 되살리거나 프로젝트를 임의 재생성하지 않음. 따라서 SaveManager/StoryManager 변경까지 컴파일 통과로 간주하지 않음. [로그](../../../output/RedTreeR02R03/compile-runtime.log) |
| 테스트 실행 시도 | Windows PowerShell 5.1에서 NUnit 메서드 호출 실패(netstandard 2.1 참조 어셈블리 실행 불가). 초기 스크립트의 성공 라벨은 유효하지 않아 실패 기록으로 정정. **13개 테스트 실행 통과 아님.** [기록](../../../output/RedTreeR02R03/pure-migration-tests.log) |
| Unity 로드/컴파일/EditMode/Editor Play Mode | 미수행. 열린 Unity 프로세스는 있지만 제어 도구 없음. 연결 복구/새 검증 도구 제작으로 대체하지 않음 |
| 직접 플레이·실물 패드·감각 | 미수행 |
| Windows Player | 미수행. 직렬화 필드/영입 전투 계약 변경이 포함되어 활성화 전 해당 가이드 검증 필요. Boot 성공만으로 대체 불가 |

작업 전 해시와 비교했을 때 기존 변경 상태의 씬·Data·Prefab 파일은 그대로였다. 이번 Data 변경은 QuestDatabase의 신규 참조 3개와 새 RedTree 폴더뿐이다. 작업 중 별도로 변경된 MotionWarp/Player 전투 코드 등은 해시 차이를 기록했으며 되돌리지 않았다. Unity 에셋 저장/재직렬화를 호출하지 않았으므로 검증 자동 변경으로 판정해 원복한 파일은 없다. 사용자가 시작한 변경과 이번 작업을 합쳐 전체 git diff가 깨끗하다고 주장하지 않는다.

## 6. 다음 실행 절차

1. 열린 Unity의 사용자 작업을 보존한 상태에서 S00~02 도착길·작업점·거처·구조/안전 지점, 두 주인공·화린·NPC·릴리의 모델/장비/화자와 기존 조우를 확인한다. 장면별 미확인을 해제한 뒤 배치와 전후 상태를 연결한다.
2. Unity가 생성 csproj를 갱신하고 새 스크립트를 import한 뒤 컴파일 오류 0을 확인한다. 오류 상태에서 에셋 일괄 저장/재직렬화하지 않는다. 새 EditMode 13개와 관련 영입/대화/퀘스트 테스트를 실행한다.
3. R02의 7개 러너·Anchor/Trigger/Interaction·NPC·퀘스트 자동 연계·재생성 도구를 한 전환 단위로 적용한다. 새 DB 활성화와 소비 경로를 함께 검사한다. 원본 맵과 옛 에셋은 보관한다.
4. Boot에서 라온/아린 각각 새 게임을 시작해 Roster/BattleOrder 1명, 주인공 일치, 인도→식사→돌봄→거처→실제 전투→피난민 안전→동의→실제 영입→선택 교체→릴리 안내를 검사한다. RT01~04, RS01~06/08~10, RQ01/03/06/07/09 및 전투 RQ04/05를 적용한다.
5. 구조 승리 후 Cancel→재상호작용, 안전 확인 전/후 저장, 영입 직후 저장·로드·재접촉, 활성 화린 상태의 주인공 발화, 사망/씬 이탈/볼륨 내부 로드를 각각 검사한다. 입력/HUD/카메라/대역/투사체/전투 보류 정리를 확인한다.
6. 테스트용 원본 파일의 해시를 기록하고 **비어 있는 별도 슬롯**에 `TryCreateRedTreeCopy`를 호출한다. 확인된 재개 mapId/sceneName만 전달한다. 원본 보존·주인공/Roster/성장/장비/보유 분기·구 추적 정리·중복 보상·재이관 멱등성을 비교한다. 실제 사용자 슬롯에서는 아직 실행하지 않았다.
7. 변경 위험에 해당하는 Windows Player 검증을 수행한다. 모든 검증 전후 씬/Data/Prefab diff를 확인하고 검증 자동 변경만 분리한다. 그 뒤 R03 완료와 R04 착수를 판단한다.

## 7. 후속 작업 — 안전 인계 전 접근·복원

현장 확인 결과가 아직 없고 현재 세션에도 Unity 제어 도구가 없어, R02의 일괄 전환 조건을 유지했다. 기존 씬·NPC·퀘스트 활성 상태·생성기는 전환하지 않았다. 행동 생산자와 생활/안전 배치도 미연결 상태다.

| 파일 | 반영 목적과 동작 |
|---|---|
| `Gameplay/Encounter/Recruitment/RecruitmentEncounterAnchor.cs` | 안전 조건이 있는 CombatResolved 복원은 참가자·투사체를 정리하고 이전 접근을 정지한다. 위치 이동은 안전 사실 확인 뒤 Flow의 명시적 준비 호출에서만 시작한다. 직접 준비 호출·준비 완료 조회·재접근에도 안전 조건을 적용한다. Cancel/비활성화는 접근 상태를 초기화하며 종료 뒤 도착한 콜백은 준비 완료를 되살리지 않는다. 안전 조건이 없는 기존 조우와 적대 조우의 전투 전 도입은 유지한다. |
| `Manager/Story/StoryManager.RecruitmentEncounters.cs` | 마지막 적 처리·저장된 마지막 처치 재개·CombatResolved 재개가 모두 안전 대기 배치를 적용한다. 안전 확인 전 서비스의 접근 준비·준비 완료 조회를 거부한다. 이미 있던 정상 대화 증명/commit 재검사는 유지한다. |
| `Tests/EditMode/FlowGraph/RecruitmentSafetyRecoveryTests.cs` | 9사례 추가: 안전 전 차단, 안전 후 허용/commit 재검사, 안전 전후 마지막 처치와 재개, 저장된 마지막 처치, 전투 전 도입, 기존 무조건 조우, 안전 전후 Anchor 복원 위치/이전 접근 초기화. 런타임 타입을 기존 reflection 방식으로 읽으며 asmdef 참조는 변경하지 않았다. |

이 검사는 실제 화린 전투·배치 완주가 아니다. Anchor 사례는 빈 참가자 배열과 비활성 테스트 액터로 **접근 시작·위치 이동 경계만** 검사한다. 실제 참가자 AI·투사체 정리와 카메라/입력 반환은 Play Mode에서 따로 확인해야 한다.

보조 컴파일은 현재 Unity 생성 프로젝트와 설치된 .NET SDK로 수행했다. 최종 `Assembly-CSharp` 오류 0/경고 64, `UPlayGround.FlowGraph.Tests` 오류 0/경고 12. 이전 작업의 누락된 Actor 소스 참조 오류는 이번에는 재현되지 않았다. 증분 재확인 중 공용 출력 DLL 잠금으로 1회 실패했고 직렬 빌드 재실행으로 통과했다. 신규 테스트는 임시 MSBuild include로 포함했으며 생성 csproj는 편집하지 않았다. 로그는 `output/RedTreeR03Recovery/compile-runtime-verified.log`, `compile-tests.log`에 있다. **Unity 컴파일·EditMode 9사례 실행·Play Mode·Player 검증은 미수행**이다.

작업 전후 씬/Data/Prefab 기존 파일 8,016개 해시 비교에서 변경·누락은 0개였다. 신규 데이터·프리팹·씬은 만들지 않았다. 사용자 저장 슬롯도 읽거나 쓰지 않았다. 보조 컴파일 출력 699개 파일(약 140MB)은 확인 후 삭제하고 로그·해시·결과 JSON만 `output/RedTreeR03Recovery/`에 보관했다.

현장 연결에 재사용할 실제 구현도 확인했다. 입력은 `FlowGraphInteractable`과 `FlowGraphTriggerVolume._suppressPhysicsRouting=true`, 저장 후 주민/소품 복원은 `NpcLifePresentation`을 사용한다. 이 복원기는 화면 밖 전환을 기다리므로 `SetFlag` 성공만으로 화면에서 식사 인계·처치·안전 도착을 완료했다고 판정하면 안 된다. 현장에서 행동 완료와 표시 완료의 시점, 안전 도착 판정 위치를 확정한 뒤 연결한다. 빈 작업점에 플래그만 켜는 생산자는 추가하지 않았다.

지금 실행 가능한 현장 확인과 후속 검사 절차는 [테스트 명세의 안전 복원 후속 항목](../guide/LAKE_OF_LIFE_RED_TREE_TEST_SPEC.md#r03-안전-복원-후속-검사)을 따른다. 두 주인공 완주·별도 복사 슬롯은 일괄 전환 후 실행하며, 아직 없는 새 도입을 Boot에서 검사하도록 요청하지 않는다.

## 8. MCP 연결 후 실제 Unity 검사

사용자가 MCP 테스트를 요청하여 설치된 Unity MCP v10.3.0의 stdio 서버로 `UPlayground@fddd0702`에 연결했다. Unity 6000.3.21f1 WindowsEditor의 열린 인스턴스에서 실행했으며 두 번째 Editor를 열지 않았다. 앞 절의 제어 도구 부재·Unity 테스트 미수행은 당시 기록이며, 아래 범위는 이번 실행 결과로 갱신한다.

| 검사 | 실제 결과 | 증거 (`output/RedTreeMcpValidation/`) |
|---|---|---|
| 안전 복원 EditMode | `RecruitmentSafetyRecoveryTests` 9/9 통과 | `response-12.json` |
| 준비 콘텐츠·저장 규칙 EditMode | `RedTreePreparedContentTests` 5, `RedTreeSaveMigrationTests` 8, `RecruitmentEncounterStateStoreTests` 10 모두 통과. 합계 32/32, 실패·스킵 0 | `response-12.json` |
| 취소·재진입 Editor Play Mode | `LakeNarrativeRecoveryTests` 5/5 통과. 중복 진입, 전투 홀드 정리, 취소 출력, 볼륨 재접근 검사 | `response-33.json` |
| 실제 씬 로드·참조 | `LakeOfLifeScenario` 추가 로드. GameObject 37,490개 Missing Script 0. 구 러너 7개 활성 연결, 새 RedTree 러너/볼륨 없음 | `response-18.json` |
| NPC 데이터 | 실제 로드된 NPC 12개의 데이터 참조 확인. 조이·루시아는 공용, 안내인·헤이즐 등은 Scenario 데이터 유지 | `response-30.json`의 `npcs` |
| 화자 테이블 | 안내인·조이·헤이즐·루시아·라온·화린의 명시 바인딩/초상화 로드 확인. `릴리`는 명시 Actor/대역 바인딩과 기본 초상화 없음 | `response-34.json` |
| 콘솔 | 검사 후 MCP Console 조회에서 error/warning 0건. 진행 중 컴파일 없음. 새 강제 재컴파일은 수행하지 않음 | `response-36.json`, `response-37.json` |
| 에셋 보존 | 씬/Data/Prefab 기존 파일 8,016개 전후 SHA256 변경·누락 0, 파일 수 동일. 추가 로드한 씬은 dirty=false 확인 후 닫음. 저장 슬롯 API 호출 없음 | `asset-integrity.json`, `response-32.json`, `response-35.json` |

Play Mode 5개는 작은 테스트 오브젝트와 대체 서비스를 쓰는 Flow 복구 검사다. 실제 화린 전투, 이동 AI, 카메라·입력 복원, 두 주인공 본편 완주를 통과한 것이 아니다. 저장 8개도 메모리 데이터 이관 규칙이며 암호화 파일·별도 슬롯 복사 검증은 남아 있다. `아린`의 명시 테이블 항목 없음은 `Protagonist`/Party 초상화 경로와 다르므로 주인공 표시 실패로 단정하지 않는다. 처음 사용한 영문 화자 조회와 빈 그래프 화자 추출(`response-30/31`)은 화자 누락 판정 근거에서 제외했다.

MCP 위치 지정 카메라로 마을 전경, 조이 식사 후보, 헤이즐/소담 작업점, 루시아 거처 후보, 기존 화린 조우 위치를 렌더링했다. `village-overview.png`, `joy-meal-area.png`, `hazel-care-area-front.png`, `lucia-shelter-area.png`, `hwarin-encounter-area.png`를 직접 열람했다. 조이 주변 식탁/소품, 헤이즐·소담의 작업대, 루시아 옆 건물과 진입 공간을 확인했다. 헤이즐 최초 촬영은 건물 내부에 가려져 반대쪽에서 다시 촬영했다. 이들은 **Edit Mode 월드 캡처**이며 플레이어 카메라·실제 통행·전후 행동 배치의 검증은 아니다. 화린 캡처는 조우 지형만 확인되며 전투 실물 검증으로 세지 않는다.

RC01/RC02는 통과, RC03/RC04는 부분 확인이다. 식사·돌봄·거처 행동 생산자, 피난민 안전 도착, 릴리 화자/대역 연결, 실제 모델·장비·전투와 RC05~08은 남는다. 구 실행선 일괄 전환, 두 주인공 완주, 별도 복사 슬롯 및 최종 Player 검증은 미수행이다. 이번에는 런타임·콘텐츠 코드를 변경하지 않고 검증 근거와 문서만 갱신했다.

## 9. 실제 행동 연결과 격리 플레이 — 2026-10-18

앞 절은 각 시점의 이력이다. 이번에는 Unity MCP로 아래 구현과 검사를 수행했다. **원본 `LakeOfLifeScenario`는 아직 구 실행선이며, `RedTreeR03Verification`에서 확인한 결과를 본편 통과로 환산하지 않는다.** 검증 씬의 RedTreeR03 루트는 저장 시 비활성이다. `output/RedTreeR03Integration/prepare-runtime.cs`가 정상 부팅 후 메모리상의 Quest DB에 새 세 퀘스트를 활성 등록하고 루트를 켠다. 원본 Quest 에셋 활성화·DB 전환은 아직 적용하지 않았다.

### 반영한 구현

- `NpcLifeInteractable`: 실제 근거리 플레이어 입력으로 소품 인계 또는 NPC 보행을 실행한다. 주민이 실제 목적지에 도착한 뒤에만 완료 사실을 기록한다. 취소·사망·비활성화·플래그 재로드는 이동 홀드를 해제한다. `DROP_ITEM` 상호작용 데이터가 있어야 플레이어 상태가 완료까지 기다릴 수 있다.
- `NpcLifePresentation`: 실제 행동 시 전후 표시를 먼저 적용하고 완료 플래그를 확정한다. 로드 복원용 위치 설정으로 도착 조건을 대신 통과하지 않는다. 비어 있거나 끊어진 소품 연결은 거부한다. 기존 저장 복원 경로를 유지한다.
- 검증 씬: 짐/식사/꾸러미/처치 자리/거처/피난민 두 명의 행동 7개, 확인 대화 5곳, 새 구조 조우를 연결했다. 첫 피난민의 실제 도착 `refugee_first_arrived`가 두 번째 인계의 선행 조건이다. 마지막 도착만 `refugees_safe`를 기록한다. 이 씬 안에서만 구 러너 7개와 옛 생활 배치를 끄고 NPC 전용 데이터 사본의 구 대화 연결을 제거했다.
- 릴리: 화자·대역 `MonsterLili`와 기존 `Player_Lili_Full` 초상화를 연결했다. 실제 대화에서 표시를 확인했다. 첫 촬영에서 대검이 얼굴을 가리는 OTS 구도가 발견돼 Handoff의 릴리 대사를 Closeup, 주인공 대사를 TwoShot으로 변경했다. 변경 후 화면 검수는 별도 확인이 필요하다.
- 기존 FirstRecruitment/Reunion/OptionalEvents/Herb/Integration 생성기는 대상 씬에 새 도입 Flow GUID가 있으면 변경 전에 실패하도록 보호했다. 현재 원본 씬에는 새 Flow가 없어 아직 본편 퇴역 상태는 아니다.

### 실행 결과

증거 기본 경로는 `output/RedTreeR03Integration/`다. 가상 게임패드 이동·상호작용·공격과 정상 대화 넘김 API를 사용했다. 진행 플래그·처치·HP를 주입하지 않았다. 아래 실제 플레이는 Boot 초기화 도구로 선택 캐릭터를 준비한 것이며 타이틀 선택 UI 조작을 포함하지 않는다.

| 검사 | 결과와 범위 | 증거 |
|---|---|---|
| 관련 EditMode | 38/38 통과: 기존 32 + 암호화 파일 복사 6 | `response-46.json` |
| 관련 Editor Play Mode 회귀 | 9/9 통과: Flow 복구 5 + 생활 표시 계약 4. 실제 NPC 보행 시험과는 별도 | `play-regression-final.json` |
| 라온 생활 구간 | 짐→응답→식사→응답→꾸러미·자리→응답→거처 보행→구조 요청 실행. 도착 대화 취소는 행동 사실을 유지하고 응답 사실을 만들지 않음 | `life-play-progress.json`, `life-resume-progress-2.json`, `life-*.png` |
| 생활 저장 복원 | 실제 플레이로 만든 슬롯 0을 보존하고 1로 복사·로드, 거처부터 재개. 이후 저장은 2~6 사용 | `RuntimeSaves/20261018_004937/` |
| 구조 접근·실제 전투 | 게임패드로 구조 현장 접근, 라온 공격·회피와 화린 AI 교전으로 두 적 처치. 전투 종료 시 `escape_route_cleared=true`, `refugees_safe=false` | `rescue-play-raon.txt`, `rescue-running.png`, `rescue-play-raon.png` |
| 사망·부활 | 첫 전투 시도 라온 사망. 완료 사실 없음. 실제 포탈 부활 버튼으로 살아 있는 Idle 복구, 이후 격리 저장본으로 재개. 전체 사망 복구 시나리오 통과를 뜻하지 않음 | `combat-raon.txt`, `combat-raon.png` |
| 안전 전 저장/로드 | 실제 전투 해결 후 슬롯 3 저장·같은 씬 로드 성공, 안전은 미완료 유지 | 슬롯 3, 후속 피난민 실행 기록 |
| 피난민 도착 | 두 NPC가 각자의 안전 지점까지 보행한 뒤에만 안전 완료 | `refugee-play-progress-3.json` |
| 동의 취소·안전 후 로드 | 취소 직후 `safe=true`, `consent=false`, Roster=Raon. 슬롯 4 저장·로드 뒤 정상 대화 넘김 3회로 실제 화린 합류 | `hwarin-consent-cancel.txt`, `consent-resume.txt` |
| 영입 후 저장·릴리 안내 | 슬롯 5 저장·로드 후 릴리 안내 정상 종료. 슬롯 6 저장 성공, Q00~02 Completed, Roster=Raon/Hwarin, 주인공=Raon | `lili-play-progress.json`, `lili-dialogue-*.png`, `raon-stage-complete.json` |
| 암호화 복사 이관 | Raon/Arin×구 진행 3종의 합성 원본 파일에서 실제 SaveManager API 실행. 원본 해시·주인공·파티·성장·장비 보존, 구 활성 추적 제거, 덮어쓰기 거부, 재이관 멱등성 확인. 실제 구 본편 플레이 저장의 씬 재개 검사는 아님 | `SaveCopies/*/result.json`, EditMode 결과 |

### 실패와 남은 검증

- 최초 거처 행동은 NPC의 `IWorldActor.IsAlive`가 false인 계약 때문에 중단됐다. NPC는 `IDamageable`이 아니므로 NPC 활성 상태로 계속 가능 여부를 판단하도록 수정했다. 재컴파일 후 Play Mode 9개와 실제 거처 보행 재검증 통과.
- 자동 경로의 첫 시도는 미아의 작업 자리에서 막혔다. 우회 경유점으로 생활 구간을 진행했다. 거처 저장 위치가 NavMesh 바깥에 걸친 경우도 기록하고 경로 질의 시작점만 보정했다. 플레이어 위치를 순간 이동시키지 않았다.
- 전투 후 피난민 이동 검사 2회는 전투 안내 팝업의 일시정지로 시간 초과했다. 팝업을 정상 닫는 절차를 포함한 3회차에서 통과했다. 팝업 중 정지를 이동 시스템 결함으로 판정하지 않는다.
- 로드 직후 Quest 서비스 상태는 복원되지만 HUD가 이전 목표를 표시한 사례가 있다. 이벤트 재통지 확인과 수정·재검증이 필요하다.
- 라온 무게 프로필 폴백, `UI_Click` 사운드 키 누락, 폰트의 `×` 누락, 영상 색 공간 폴백 경고를 관찰했다. 관련 없는 전투/공용 리소스를 임의 수정하지 않았다.
- 식사하는 주민·처치받는 주민의 연기, 거처의 실제 사용 표현, 새 NPC 외형·접지 품질은 추가 검수가 필요하다. 소품 교체와 보행 성공만으로 장면 완성도를 통과 처리하지 않는다.
- 아린 완주, 타이틀 선택, 취소 후 직접 재접근, 씬 이탈·재진입, 활성 화린의 주인공 대사, 실제 구 저장 복사본 재개, 본편 일괄 전환, 최종 Windows Player 빌드·실행은 아직 미수행이다.

원본 씬을 직접 일괄 변경하려던 첫 호출은 자동 승인 검토에서 중간 실패 롤백과 새 실행선 검증 근거가 부족하다는 이유로 거절됐다. 이후 원본을 유지하는 별도 검증 씬 생성 방식으로 진행했다. 본편 전환은 남은 필수 연결·검수와 복구 가능한 적용 단위가 갖춰진 뒤 수행해야 한다. 커밋하지 않았다.

## 10. 두 주인공 실행 후 발견한 복원 결함과 보완

9절의 미수행 목록은 당시 기록이다. 이후 아래 범위를 추가로 확인했다. 증거 경로는 계속 `output/RedTreeR03Integration/`이며, 가상 게임패드와 정상 대화·저장 API 검사다. 실제 사용자 저장은 사용하지 않았다.

| 검사 | 수행 결과 | 증거 |
|---|---|---|
| 아린 검증 씬 완주 | 생활 행동, 실제 공격·화린 AI 전투, 피난민 도착, 정상 동의, 화린 교체, 릴리 안내. 종료 시 Q00~02 완료, 주인공 Arin, 활성 Hwarin. 전투 입력 프로브는 안내 팝업 지연 후 이동 정체로 끝났으므로 후속 실제 조우 상태 확인을 별도로 기록 | `arin-life-progress.json`, `rescue-play-arin.txt`, `arin-refugee-progress.json`, `arin-consent.txt`, `arin-stage-complete.json` |
| 릴리 실물 | Closeup 변경만으로는 대검이 얼굴을 가리는 문제가 남았다. 전투 원본을 유지하고 `NPC_StoryLili` 대역과 ActorDefinition을 추가, ActorDatabase 및 화자 바인딩에 `StoryLili` 연결. NPC 여성 기본 모션과 무기 비활성 상태에서 얼굴·이름·초상화 표시 및 안내 완료 확인 | `lili-standin-2.png`, `lili-standin-progress.json` |
| 주민 보행 모션 | 새 NPC 3명의 ActorAnimator MotionSet 누락 발견. 남녀 NPC MotionSet을 검증 씬에 연결. 수정된 두 피난민의 실제 도착 재검사 | `arin-refugees-motion.png`, `refugee-recovery-progress.json` |
| 같은 씬 완료 로드 | 수정 전 Completed인데 참가자 3명이 다시 대치한 화면을 확인. StoryManager가 저장 단계의 현장 배치도 적용하고 Anchor의 완료 단계 진입 폴링을 차단하도록 수정. 슬롯 6 실제 로드 후 참가자 0명, 대화 없음, 파티와 생활 결과 7개 복원 | `lili-standin-1.png`(실패 화면), `completed-load-fixed.json`, `completed-load-fixed.png` |
| 안전 전 로드 | 슬롯 3에서 CombatResolved, 적 2명 비활성, 화린만 대기, safety=false. 실제 피난민 행동을 다시 수행해 안전 완료 | `refugee-recovery-progress.json` |
| 퀘스트 HUD 복원 | QuestManager의 로드 후 추적/해제 이벤트를 재통지. 실제 매니저·이벤트 구독 검사 통과, 완료 저장 로드 화면에서 옛 목표가 남지 않음 | `quest-load-tracking-check.json`, `completed-load-fixed.png` |
| 기존 P04 저장 복사 | Raon/Arin 기존 검증 슬롯 2를 새 디렉터리의 0으로 바이트 복사하고 빈 1로 이관, 1→2 재이관. 원본 SHA256, 주인공·Roster·출전·활성 인덱스·성장·ASC·Inventory·장비 보존, 구 리안 활성/추적 정리, 점유 대상 거부, 현재 ActiveSlot 불변 확인. R03 본편 씬 재개는 아직 별도 | `legacy-copy-results.json`, `LegacyCopies/*/result.json` |
| 관련 EditMode 재검사 | 안전 복원 12(동일 씬 로드 3 추가) + 준비 데이터 5 = 17/17. 첫 실행은 신규 테스트 컴파일 전 구 어셈블리로 9개 중 3개 실패했고 신규 테스트의 ISaveable 참조 컴파일 오류도 수정. 최종 결과만 17개 통과로 기재 | `recovery-load-tests.json`, `prepared-recovery-tests.json` |

동의 취소 후 직접 이탈·재접근은 실패를 재현했다(`consent-reapproach-failure.json`, `consent-reapproach-progress.json`). 안전 도착/로드 이벤트 진입에는 Anchor가 붙이는 완료 콜백이 없어 재시도가 열리지 않았다. R03 그래프의 Rejected 출력을 `WaitPlayerInVolume(requireExitBeforeArrival=true)`로 연결하고 다시 준비·정상 동의를 거치도록 수정했다. 새 테스트는 재접근 뒤에도 정상 동의 노드를 우회할 수 없음을 검사한다. 수정 후 실제 재접근 검사는 진행 중이다.

생활 표시에는 식사 자리로 이동하는 주민, 처치 자리에 앉는 주민, 거처에서 쉬는 주민을 연결했다. `NpcBrain.SetWorkMotion`과 NpcLifePresentation의 전후 작업 모션으로 로드 때 자세도 복원한다. 기존 NPC·모션 에셋을 복제/참조하며 원본 모델 GUID는 유지했다. 이 착석 배치는 새 변경이므로 앞선 두 주인공 완주만으로 통과 처리하지 않는다. 검증 씬 재실행이 필요하다.

새 도입 Q00 수락은 Available 상태에서만 실행하여 중복 수락 경고를 피한다. 수락 뒤 콘텐츠 버전 사실을 기록하고, 퀘스트 목표에는 생활·구조·안전·릴리 위치를 연결했다. 이 시점에는 새 퀘스트가 비활성이었다. 후속 본편 적용은 11절을 따른다.

## 11. 생활 착석 검수와 본편 일괄 전환

증거 경로는 `output/RedTreeR03Integration/`다. 아래 기록은 9~10절 이후 수행 결과이며 최종 통합 통과 선언이 아니다.

| 항목 | 결과 | 증거 |
|---|---|---|
| 새 생활 행동 | 실제 게임패드 보행·상호작용으로 식사·처치·거처 착석 및 구조 요청 완료. 거처 뒤 NavMesh 시작점 질의 실패는 질의점만 보정한 뒤 실제 보행으로 재개 | `life-seat-progress.json`, `life-seat-resume-progress.json`, `RuntimeSaves/20261018_104349/save_slot_0.sav` |
| 착석 품질·복원 | 식사 자리, 거처의 의자·짐 배치 확인. 낮은 처치 의자는 앉은 높이가 맞는 기존 의자 복제본으로 교체. 로드 후 세 주민의 `Npc_Chair_Sit_Idle`과 목적지 거리 0~0.013m 확인. 캡처는 위치 지정 관찰 카메라이며 실제 플레이 카메라 검사는 별도 | `meal-seat-runtime.png`, `shelter-seat-runtime.png`, `care-seat-fixed-load.png`, `life-seat-restored.json` |
| 동의 취소 재접근 | 실제 취소 후 볼륨 밖 보행, 진입 볼륨 내부로 재접근해 대화 재개. 정상 Advance 3회 뒤 consent=true, 조우 Completed, 주인공 Raon | `consent-retry-fixed-progress.json`, `consent-retry-volume-progress.json`, `consent-retry-success.json` |
| 완료 씬 재진입 | 실제 완료 슬롯 6을 `LoadGameToScene`으로 재로드. 검증 루트 활성 후 Completed, 참가자 0명, handoff=true, 대화 없음, 카메라 준비 | `scene-reentry-completed.json` |
| 본편 일괄 적용 | `LakeOfLifeScenario`의 구 러너 7개와 구 연결을 닫고 새 러너 2개·행동 7개 활성. 새 퀘스트 3개 활성, 퇴역 10개 비활성. 새 퀘스트의 DB 참조 확인. GameObject 43,286개 Missing Script 0 | `MainTransition/result.json`, `transition-main.cs` |
| 백업·롤백·GUID | 씬+퀘스트 14파일의 백업 해시 확인. 한 파일 적용 뒤 의도적 예외를 발생시켜 모든 원본 바이트 복원 확인 후 최종 적용. 씬 GUID `1d16ed83654f4f14e8628ca754f42067` 유지. 임시 검증 씬의 BuildSettings 등록 제거 | `MainTransition/manifest.json`, `rollback-probe.txt`, `*.before` |

11절 작성 시 최종 Player, 본편 타이틀 시작·완주, 복사 이관 슬롯의 본편 재개와 전체 회귀는 진행 중이었다. 후속 결과는 12절을 따른다. 전체 EditMode 첫 실행에서 기존 Dryad/Training Dummy 모션 누락 및 다른 모듈 실패와 함께, 신규 릴리 대역의 모션 참조 불일치와 퇴역 전 활성 상태를 요구하는 구 퀘스트 테스트가 발견됐다. 해당 실행을 통과로 기재하지 않는다.

전체 EditMode는 923개 중 906 통과·17 실패(`all-edit-completed.json`), 전체 Play Mode는 24/24 통과(`all-play-final.json`)했다. 릴리 대역에 NPC 기본 자세를 유지하고 원래 릴리 MotionSet을 상속하는 `MotionSet_StoryLili`를 연결했다. 기존 공격 매핑 47개가 같은 Motion을 해석한다. 몬스터 통합 재검사에는 릴리 누락이 없어졌고 Dryad/Training Dummy 오류는 남았다. 구 메인 퀘스트의 에셋·DB 등록 보존과 비활성, 새 퀘스트 활성 계약으로 테스트를 갱신했다. Play Mode 직후 안전 테스트 3개가 실패한 원인은 `ManagerLifecycle.ApplicationIsQuitting`의 잔류 상태였다. 테스트에서 이 값을 보존·격리·복구한 뒤 R03 안전/콘텐츠/퀘스트 19/19 통과(`r03-final-edit.json`). 중간 중복 필드 컴파일 오류는 제거했고 최종 컴파일 오류는 0이다.

프로젝트 프리팹 310개, ScriptableObject 경로 3,332개, 컴포넌트 12,850개, managed reference 4,933개, VFX/particle 참조 283개를 읽기 전용 검사했다(`reference-audit.json`). Missing Script·managed reference 타입 누락은 0이고 기존 참조 누락은 17건이다: H09 프리뷰 idle 1, UI sprite 2, ElementalImbue icon 5, DualBlade 저작용 clip 8, 구 스켈레톤 트리거 StoryEntry 1. `AbilityDataValidator.ValidateAll`은 1,194항목 중 Error 7건이며 기존 PlayerDefault Attack/Ultimate 빈 Variant와 패시브 데이터 문제다(`ability-validation.json`). R03 변경과 무관한 이 데이터를 임의 수정하지 않았다.

기존 씬/Data/Prefab 8,016파일 비교 결과에서 변경은 이번 씬·ActorDatabase·릴리 화자/초상화·퇴역/새 퀘스트·R03 그래프/대화/조우 정의에 한정됐고 누락은 없다(`asset-changes-current.json`). 검증용 BuildSettings 변경은 원본 SHA256 `DD7BBF102402C0A713F5F5CB8CA8864458CA855E878CE5F522838B376BFA16FE`로 복원했다. 새 Player 검사 소스는 `output/RedTreeR03Integration/RedTreePlayerProbe.cs`에 보관하며 Development 빌드의 명시적 인자로만 활성화한다. 타이틀 버튼/캐릭터 카드, 가상 게임패드, 정상 대화/저장 API를 쓰며 진행 플래그·처치·HP를 주입하지 않는다.

## 12. Windows Player 실행과 저장 경계 재검사

증거는 `output/RedTreeR03Integration/`에 보관한다. 아래 통과는 해당 실행 시점의 코드에만 적용한다.

| 항목 | 실제 결과 | 증거 |
|---|---|---|
| 최초 Windows64 Development 빌드 | 성공, 오류 0·경고 519, 1,404.39초. 본편을 포함한 활성 씬 8개 | `player-build-result.json` |
| 최초 두 주인공 Player 완주 | Raon/Arin 각각 타이틀 선택 → 실제 생활 행동 → 가상 패드 공격·화린 AI 전투 → 두 피난민 보행 도착 → 정상 동의 → 화린 교체 → 릴리 안내 → 완료 저장·씬 재로드. 각 result passed=true, errors=[] | `PlayerRuns/Raon-20261018_112826`, `PlayerRuns/Arin-20261018_113119`, `player-runs-first.json` |
| 완료 후 늦은 등장 콜백 | 최초 완료 Player 저장을 Editor 본편으로 로드한 뒤 실제 Anchor 등장 콜백 호출 시 참가자 0→3 재활성 결함 재현. Dormant 단계에서만 등장하도록 가드 수정. 단계별 회귀 4개 추가 | `late-reveal-before.txt`, `late-reveal-edit-tests.json`(23/23) |
| 두 번째 스크립트 빌드 | 성공, 오류 0·경고 39, 472.19초. 늦은 등장 콜백 수정 포함 | `player-build-final.json`, `player-assembly-after-fix.json` |
| 최종 스크립트 빌드 | 성공, 오류 0·경고 33, 796.49초. 영입 직후 출전 명단 저장 수정 포함. MCP 상태 조회 시간 초과·재로드 재연결 뒤 빌드 보고서로 성공 확인 | `player-build-party-final.json`, `player-assembly-party-final.json` |
| 모델 준비 판정 포함 빌드 | 성공, 오류 0·경고 22, 356.97초. 콘텐츠 변경이 없어 직전 Addressables 번들을 재사용. PreferencesValue 설정의 빌드 동반 여부만 임시 false로 두고 완료 후 원래의 키 없음 상태 복원 확인 | `player-build-model-final.json`, `player-assembly-model-final.json` |
| 최종 라온 Player | 타이틀부터 실제 본편 완주, 영입 직후 저장·로드·모델 준비 후 화린 교체, 릴리 안내, 완료 씬 재진입 및 늦은 등장 콜백 차단 통과. passed=true, errors=[] | `PlayerRuns/Raon-20261018_123309` |
| 최종 아린 Player | 단독 재실행에서 타이틀부터 실제 본편 완주 및 라온과 동일한 영입 저장·교체·릴리·완료 재진입 검사 통과. passed=true, errors=[] | `PlayerRuns/Arin-20261018_123546`, `player-runs-final.json` |
| 아린 동시 실행 부팅 실패 | 별도 저장 폴더로 동시에 띄운 아린은 ActorDatabase 로드의 기존 15초 제한 초과로 본편 진입 전 실패. 로그 보존 후 해당 검사 프로세스만 종료하고 단독 재검사 | `PlayerRuns/Arin-20261018_123330/result.json`, 같은 이름 `.log` |
| 두 번째 라온 Player 실행 | 영입 직후 Save2/Load2 뒤 화린 교체 실패. 보유 명단은 2명이나 출전 명단이 라온 1명인 실제 저장 확인. 별도 복사 후 기다려도 복구되지 않음 | `PlayerRuns/Raon-20261018_114942/result.json`, `recruit-save-race-before.json` |
| 영입 저장 경계 수정 | EnsureCharacterUnlocked에서 출전 편입 의도를 모델 비동기 로딩 전에 확정. 늦게 끝난 로딩이 로드 후 명단을 다시 추가하지 않도록 수정. 관련 EditMode 26/26 통과. 수정 후 Editor 타이틀 라온 완주, 영입 직후 저장·로드·화린 교체 및 완료 후 늦은 등장 콜백 차단 통과. 콘솔 오류 0 | `party-fix-edit-tests.json`, `EditorFinal/Raon-20261018_120141/result.json`, `progress.txt` |
| 모델 준비 전 교체 판정 | 세 번째 Player 라온 실행은 출전 명단 보존을 통과했으나 CanSwapTo가 미준비 모델에도 true를 반환해 실제 교체 실패. 로그의 `CharacterType=Hwarin 모델 없음` 확인. 플레이어 바인딩 때 캐시한 PlayerSwapBehaviour에서 대상 모델 준비 여부도 검사하도록 수정. 모델/교체 컴포넌트 미준비 회귀 2개 추가 후 관련 EditMode 28/28·전체 Play Mode 24/24 통과 | `PlayerRuns/Raon-20261018_122032`, `model-ready-edit-tests.json`, `model-ready-play-tests.json` |
| 실제 구 저장의 본편 재개 | 원본 P04에서 이관한 Raon/Arin 복사본을 다시 새 빈 디렉터리 슬롯 9로 복사, 정상 LoadGameToScene 및 실제 패드 보행. 주인공·파티·성장·기존 아이템·장비 보존 확인 | `LegacyResume/Raon-20261018_113121`, `LegacyResume/Arin-20261018_113318`의 `resume-result.json`, `walk-result.json` |
| 구 생성기 차단 | 7개 실제 진입점을 호출해 변경 전 예외와 본편 씬 해시 불변 확인 | `generator-guards.txt` |
| 빌드 후 데이터 보존 | 기존 씬/Data/Prefab 8,016개 중 의도한 변경 20개, 추가 자동 변경·누락 0. 원본 저장 3개·씬 meta·BuildSettings 해시 보존 | `asset-changes-after-build.json`, `protected-hashes-during-build.json` |

구 저장 재개 시 기존 초기화가 Reine/Lian의 빈 장비 자리(모든 슬롯 -1)를 추가한다. 첫 라온 검사의 엄격 JSON 전체 동일 비교는 이 차이로 실패했다(`resume-result-first.json`). 기존 장비·인벤토리 모든 값의 보존과 추가 항목이 전부 빈 자리인지 분리 검사해 통과했고, 전후 원문을 증거에 남겼다. 실제 구 원본의 장착 슬롯은 비어 있으므로 장착 중인 장비의 실제 재개까지 검사한 것으로 확대하지 않는다. 합성 저장의 장비 보존 검사는 별도다.

11절의 전체 Play Mode 24/24는 늦은 콜백 수정 전 결과다. 이후 두 재실행은 테스트 0개 상태에서 초기화 시간 초과와 Unity Test Framework `PlayModeRunTask`의 NullReference로 종료했다(`late-reveal-play-init-failed.json`, `late-reveal-play-retry-failed.json`, `late-reveal-play-infrastructure-error.json`). 이를 게임 회귀 테스트 실패 또는 통과로 집계하지 않는다. 도메인 재로드와 정상 본편 실행 뒤, 영입 저장 수정까지 포함한 전체 Play Mode 재검사는 24/24 통과했다(`party-fix-play-tests.json`, 9.58초).

사망·정상 부활 첫 재검사는 전투 진입 전 가상 패드 경유점의 0.55m 판정에 걸려 중단됐다(`death-recovery-main.txt`). 경유점 허용 거리만 0.85m로 조정한 후속 본편 Editor 검사에서는 실제 적 피해로 사망 → 정상 포탈 부활 → (765, 78.7, 636)에서 실제 보행 재접근 → 실제 전투 해결까지 통과했고 안전 사실은 false를 유지했다(`death-recovery-final.txt`, `death-final.png`, `death-recovery-final.png`). 플레이어 이동·HP·처치·진행 사실 주입은 없다. 종료 뒤 비활성 참가자까지 IsAlive를 읽으려던 별도 진단은 ASC가 없어 NullReference로 실패했으며 이 진단 호출은 전투 성공 증거에 포함하지 않는다.

실물 패드 조작감, 주관적 전투·카메라 품질은 미검증이다. 최종 타이틀 라온·아린 프리뷰 모두 검이 몸과 떨어져 보이는 기존 표시 문제를 확인했다(최종 두 실행의 `character-select.png`). R03 현장 연결과 별도 문제로 보고하며 수정하지 않았다. 최종 두 실행의 `dialogue-handoff_acknowledged.png`에서는 릴리 실물·화자 이름·초상화와 얼굴을 가리지 않는 자세를 확인했다.

검사 입력은 가상 게임패드와 정상 상호작용·대화·저장 API다. 플래그·HP·처치를 주입하지 않았다. 실제 구 저장 복사본 재개는 12절의 별도 Editor 검사다. 최종 Player Assembly-CSharp SHA256은 `6CC24CDC7725CC4FF8B432E70BB55FC0445A1E5F48144ADD208C6A2D328E49D3`. 검사 소스는 output 사본과 같은지 확인한 뒤 Assets에서 제거했다. 최종 해시 비교는 기존 씬/Data/Prefab 8,016개 중 의도한 변경 20개·추가 변경/누락 0(`asset-changes-final.json`), 원본 저장 3개·씬 meta·BuildSettings 보존(`protected-hashes-final.json`)이다. 사용자 변경을 원복하거나 커밋하지 않았다.

정리 후 Unity는 Boot 씬 Edit Mode, 컴파일 완료·콘솔 오류 0이다. 임시 검사 스크립트와 meta가 Assets에 없고 Addressables 임시 preference도 제거됐음을 `cleanup-final.json`에 기록했다. 최종 두 Player는 정상 종료했다.
