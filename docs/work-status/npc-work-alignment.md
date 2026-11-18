# NPC 작업 접촉 전 정렬 구현

2026-11-17. [설계](../../Assets/docs/design/npc/NPC_WORK_ALIGNMENT.md) · [NPC 위키](../wiki/npc.md) · [선행 조사](kinemation-motion-warping-investigation.md).

## 현재 상태

**후속 확대 구현 반영:** 조이·헤이즐·모건 작업점 4곳과 소담 고정 작업에 선택적 재생 전 정렬을 연결했다. 확대본의 관련 테스트 어셈블리까지 보조 C# 컴파일 오류 0·저장 연결 검사 통과. Unity 회귀 검사는 연결 종료로 미수행이며 아래 연목의 56/56 결과를 확대본에 승계하지 않는다. 실제 플레이·미술은 사용자 테스트 대기다.

선택적 정렬 SO, NpcRoutine/KCC 소비·도착 실패 처리, 연목 원본 프리팹·재생성 입력 연결을 반영했다. **구현 반영 / Unity 정상 컴파일·관련 EditMode 56/56 통과 / 사용자 플레이·미술 확인 대기**다. 전체 NPC나 임의 월드 아이템 수집 기능의 완료가 아니다.

## 변경 범위

- `NpcWorkAlignmentSO`: 접촉 전 수평 목표와 제한값의 단일 소스.
- `NpcWorkPoint`: 선택적 SO 참조.
- `NpcRoutine`: 작업 시작 시 목표 고정, 모션 시간 기반 정렬, 모터 소비 후 실제 도착 검사, 접촉 이벤트의 원격 집기 차단, 중단 초기화.
- `NpcRoutineState`: KCC 이동·회전·이동 후 콜백 연결.
- `RepairWorkAlignment.asset` 및 `YeonmokRoutine.prefab`: 연목 수선점만 활성화. 기존 망치·접촉 0.8초·종료 모션은 보존한다.
- `NpcLifeAIAuthoring.Layout`: 정규 월드 접촉 복원 시 원본 프리팹의 정렬 설정도 복원한다.
- `NpcWorkPropTests.cs` 안의 `NpcWorkAlignmentTests`: 주기별 도착·수직 보존·정지·중복 표본·차단 판정·진입 제한·무효 데이터·접촉 전 높이 이탈 회귀 검사.

## 검증 진행 기록

- `dotnet build UPlayGround.Actor.csproj --no-restore`: 오류 0, 경고 26. SDK 읽기 권한 부족으로 첫 실행 실패 후 승인된 보조 컴파일로 통과했다. Unity 정상 컴파일과 구분한다.
- 세션에 Unity MCP 함수가 노출되지 않아 설치된 MCP의 기존 로컬 전송(6400 포트, 해당 프로젝트 확인)을 이용했다. Editor는 Edit Mode이며 원래 활성 씬은 `Assets/WorldBuilder/Worlds/35c2ca802bd64575961ac7cda330ac91/Preview/WorldPreview.unity`, dirty=false였다.
- 타입·필드 추가는 Hot Reload 지원 밖이므로 기존 Editor에 Refresh·정상 컴파일을 요청했다. 두 번째 Editor·Player/Addressables 빌드는 실행하지 않는다.
- Hot Reload bridge status는 초기 무응답 뒤 `completed/Ok`, SessionState=Disabled로 응답했다. 해당 배치를 시작하거나 다른 세션을 종료하지 않았다.
- Unity 정상 컴파일 후 타입·SO 유효성·연목 프리팹의 같은 SO 참조·종료 0.55초 < 실제 집기 이벤트 0.8초·captureDistance=0.015m를 확인했다. [연결 검사](npc-work-alignment-bindings.json). Editor는 Edit Mode, 컴파일 중 아님, scriptCompilationFailed=false였다.
- 최초 기존 회귀 44/44 통과에는 새 정렬 테스트가 누락되어 있었다. 신규 독립 테스트 스크립트가 Unity AssetDatabase에 등록되지 않는 상태를 확인했고 명시 import 뒤에도 남아, 이번 작업의 신규 테스트 클래스만 기존 NpcWorkPropTests.cs로 합쳤다. 임시 신규 스크립트·meta는 제거했다. 최종 실행에 신규 정렬 12건 포함 여부를 직접 확인했다.
- 최종 EditMode **56/56 통과**, 실패·건너뜀 0, 1.961초. 정렬 12 + 실물 소품 7 + 생활 AI 21 + 일과 계약 16이다. [실행 ID](npc-work-alignment-test-start.json) · [상세 원본](npc-work-alignment-test-results.json). 정렬 수학/실물 예약 계약의 EditMode 검사이며 실제 KCC 교전·NPC 영상·미술 검사를 대신하지 않는다.
- 정규 LakeVillageWorld의 연목은 수정한 YeonmokRoutine 프리팹 instance `760901296`을 참조하고 `_alignment` override가 없음을 확인했다. 따라서 새 SO를 상속하며 기존 R3 모션 override는 유지한다. [씬 상속 검사](npc-work-alignment-scene-inheritance.json). 대형 정규 씬은 수정·재저장하지 않았다. 정규 월드를 새로 로드하여 실행하는 검증은 미수행이다.
- 최종값은 기존 R3 발 고정을 고려해 기존 1.5cm 도착 반경만 보정한다. 범용 기본값 15cm는 실제 연목 설정에 적용하지 않는다. 다른 NPC의 작업점 설정은 변경하지 않았다.

## 보존·정리

기존 `.claude/settings.local.json`, 초기 InitTestScene, Blender 원본·백업·scratchpad와 작업 중 확인된 별도 lilToon 설정 변경은 보존했다. 구매 MotionWarping 원본·기존 모션·망치·정규 씬·저장 데이터는 변경하지 않았다. 연목 프리팹의 의도한 변경은 SO 참조 1줄이며 자동 재직렬화를 완료 증거로 사용하지 않았다.

준비표는 `output/harness/20261117-210916-90c2dab6/`에 유지한다. 최종 검사 원본은 위 상태 문서 옆 JSON에 보존했다. 이번 연결에만 사용한 임시 TCP 전송 스크립트·요청 JSON과 자기 Hot Reload 요청·응답(요청 토큰 포함)을 검증 후 정리했다. Unity Library/Temp와 공유 .NET 보조 컴파일 출력은 정리 대상으로 삼지 않았다. 커밋·Player/Addressables 빌드·전체 월드 재생성은 하지 않았다.

## 사용자 직접 확인

대상은 LakeVillageWorld의 연목·기존 작업대·망치·P09 R3 모션이다. 저장 데이터 변경 없이 낮 작업 일과에서 확인한다.

| ID | 절차 | 기대 결과 |
|---|---|---|
| WA01 | 연목이 수선점에 접근해 망치를 집고 반복 작업하는 모습을 근접/플레이 카메라로 관찰 | 몸이 짧게 정렬되고 손이 망치에 닿은 뒤 같은 실물이 손을 따라간다. 발 미끄러짐·몸 급회전·순간이동이 보이지 않는다. |
| WA02 | 접촉 전/후에 대화를 걸고 종료하거나 위험 반응 뒤 재개 | 접촉 전이면 현장 망치 유지, 접촉 후면 기존 손의 망치 보존. 중복 생성·소유권 탈취·원격 집기 없이 재개한다. |
| WA03 | 정상 종료·다음 일과·재접근 관찰 | 망치를 내려놓고 조작/일과가 정상 진행한다. 다음 실행에 이전 정렬값이 남지 않는다. |

결과 회신: `ID / 통과·실패·미수행 / 실제 증상 / 필요 시 영상·로그`. 손 접촉·발 미끄러짐·정상 속도 품질은 사용자 확인 전 완료로 판정하지 않는다.

## 다른 NPC 확대 — 2026-11-17

### 구현과 연결

조이 조리점, 헤이즐 약재 포장점, 모건 화물점 2곳은 재생 전에 정밀 도착·방향 정렬을 끝낸다. 처음부터 접촉하는 반복 클립을 재생하면서 몸을 이동시키지 않는다. 각 원본 프리팹에 Cook/Herb/CargoWorkAlignment SO를 연결했다. 휴식·방문·미완성 단계와 기존 모션·손 소품은 보존했다.

소담은 NpcRoutine이 아니라 Brain의 고정 WorkMotion 경로였다. 원본 Npc_Sodam 프리팹의 선택적 WorkAlignment와 NpcIdleState를 연결했다. 홈 위치/방향을 사용해 KCC 보행으로 정렬하고, 도착 전에 손 모션을 재개하지 않는다. 시간 정지·대화·일과 이동 우선순위를 유지하며, 허용 거리 밖이거나 복귀 시간이 초과되면 작업 대신 대기한다. 작업 시작 뒤 보행 방향이 남지 않도록 접근 상태를 해제한다.

정규 LakeVillageWorld의 Joy/Hazel/Morgan/Sodam은 각각 변경한 프리팹을 참조하고 해당 정렬 필드 override가 없음을 정적으로 확인했다. [상속 확인](npc-work-alignment-expansion-inheritance.json). 씬 자체는 수정·재저장하지 않았다. 월드 재생성·약초 작업대 보정 경로는 기존 원본 프리팹을 사용하므로 새 선택 필드를 제거하지 않는다.

### 검증·제한

최종 `dotnet build UPlayGround.Combat.Tests.csproj --no-restore --nologo -v:q`는 오류 0·경고 26, 11.92초로 통과했다. Data/Actor/관련 테스트 어셈블리의 소스 컴파일이며 Unity 정상 컴파일·테스트 실행을 대체하지 않는다. [원본 로그](../../output/harness/20261117-221549-4bd8e873/compile-final.log) · [검증 구분](npc-work-alignment-expansion-validation.json).

[저장 연결 검사](npc-work-alignment-expansion-bindings.json): SO 4개·프리팹 참조 5개·GUID·재생 전 모드 일치 확인. Joy/Hazel/Morgan의 추적 프리팹 변경은 각각 1/1/2줄이다. 소담 원본 프리팹은 Git 무시 대상이므로 Git diff에 나타나지 않지만 Brain 참조 1줄이 실제 파일에 반영되어 있음을 검사했다. 해당 prefab/meta와 기존 외형은 보존하며 강제 Git 추가하지 않았다.

기존 MCP 로컬 연결은 초기 Edit Mode·컴파일 오류 없음 상태 조회 이후 연결 종료가 반복됐다. 요청 전송 중 종료와 응답 대기 중 종료를 구분해 확인했으나 이후 정상 응답을 확보하지 못했다. 다른 세션·Editor를 종료하거나 도구 설정을 변경하지 않았다. Unity 정상 컴파일·확대본 EditMode/PlayMode는 미확인이며 무응답·연결 종료를 테스트 통과로 간주하지 않는다. `NpcWorkAlignmentTests`의 재생 전 정밀 도착 반경 검사와 `NpcIdleBehaviourTests`의 배치/모션 교체 시 설정 보존 검사를 추가하고 컴파일했으나 실행하지 못했다.

이번 작업에는 신규 애니메이션·IK·실물 집기 이벤트 제작이 없다. 조이/헤이즐의 기존 임시 손 소품과 소담의 약초 표시 이벤트를 새 실물 집기 구현으로 보고하지 않는다. 실제 작업·접촉·중단/재개는 사용자 테스트 대기다.

| ID | 대상·절차 | 기대 결과 |
|---|---|---|
| WA04 | 조이가 조리 작업점에 도착해 반복하고 이탈 | 위치·방향을 맞춘 뒤 작업 시작. 작업 중 몸 이동·조리대 관통 없음 |
| WA05 | 헤이즐이 약재 포장 작업에 접근·재개 | 작업 자세를 먼저 맞추고 병·손 위치가 안정적으로 유지 |
| WA06 | 모건의 두 화물점에서 접근→점검→다음 지점 | 각 전용 모션 유지, 작업 중 양손 접촉 위치를 뒤늦은 이동으로 흐트러뜨리지 않음 |
| WA07 | 소담에게 대화하거나 위험 반응을 유발한 뒤 복귀 | 자리·방향 정렬 뒤 기존 약초 동작 재개. 막히면 제자리 손 작업을 시작하지 않음 |

초기 설정은 검사 가능한 연결·실행 계약이며 미술 품질 승인과 구분한다. 다음 검증은 확대본의 정식 Unity 회귀 실행 및 WA04~WA07이다.

이번 확대의 준비표는 `output/harness/20261117-221549-4bd8e873/`에 유지한다. 임시 전송 스크립트·진단 코드·요청 JSON·중복 컴파일 로그는 정리하고 최종 컴파일 로그·준비표·연결 검사 근거만 보존했다. 기존 WorldBuilder 병행 변경·사용자 파일·씬·구매 원본·meta는 건드리지 않았다. Player/Addressables 빌드·커밋·전체 월드 재생성은 수행하지 않았다.
