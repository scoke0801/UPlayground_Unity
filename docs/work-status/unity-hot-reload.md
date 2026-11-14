# Unity Hot Reload — 작업 상태

- 2026-11-14 몬스터 실제 Play 패치에서 Cecil `ldelem.any`/`stelem.any` 정규화 누락을 발견해 Worker를 수정했다. Release 빌드 오류/경고 0, 배열 추출 회귀 검사 2/2 통과, 동일 Play에서 실패했던 게임 메서드 패치 적용(revision 1/generation 1)을 확인했다. [원래 작업 재현·receipt·제한](monster-bt-improvement/MCP_VALIDATION.md). 전체 도구 검증 등급은 아래 기존 판정을 유지한다.

- 설계 정본: [DESIGN_SPEC.md](../전달용/Unity_HotReload_Agent_Handoff/DESIGN_SPEC.md) · 열람본 [HTML](../전달용/Unity_HotReload_Agent_Handoff/Unity_HotReload_Design_and_Agent_Handoff.html) · 분야 위키 [개발과 검증](../wiki/development.md)
- 사용 가이드: [HOT_RELOAD_USAGE_GUIDE.md](../../Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md)
- MCP 연동·컴파일 복구 확장 설계: [UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md](../../Assets/docs/design/dev-tools/UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md) — D0 조사·문서화, D1~D4 미구현.
- 결정 기록: [ADR 0001 범위·백엔드](unity-hot-reload/adr/0001-scope-and-backend.md) · [ADR 0002 G1 증명·의존성](unity-hot-reload/adr/0002-backend-proof.md)
- 기계 판독 진행: [progress.json](unity-hot-reload/progress.json) · 증거: [evidence/](unity-hot-reload/evidence/)
- **현재 판정: `IMPLEMENTED_UNVERIFIED`** — G0~G7 핵심 경로는 실제 Unity에서 자동 검증했으나, Player 빌드·도구 제거 clean compile·clean checkout 재설치·60분 soak·Play 중 실제 게임 관찰이 남아 `VERIFIED_V1`이 아니다. 커밋하지 않았다.

## 실제 환경 (G0)

| 항목 | 값 (실측) |
| --- | --- |
| Unity | 6000.3.21f1 (c02631ffc030), Editor PID 15476, Mono 6.13, API NET_Standard_2_0, 백엔드 Mono2x, 대상 StandaloneWindows64 |
| OS/CPU | Windows 11 Pro 10.0.22631, 12th Gen i5-12400F, x64 |
| 컴파일 | CodeOptimization Debug → Bee `1900b0aEDbg.dag/*.rsp`(`/optimize-`, `/deterministic`, langversion 9.0, Unity 내장 generator 3종, ILPP 후처리 대상 전 어셈블리) |
| Unity 내장 Roslyn | 4.3.0 (파일 4.300.122.52613) → Worker Microsoft.CodeAnalysis.CSharp 4.3.0 고정 |
| .NET SDK | 7.0.102, 10.0.301 (Worker `global.json` 10.0.301, net10.0) |
| Play Mode | Enter Play Mode Options 켜짐, `DisableDomainReload` |
| Auto Refresh | EditorPrefs `kAutoRefreshMode=0`(사용자 설정). 도구는 이 값을 바꾸지 않는다 |
| 기존 의존성 | Harmony·MonoMod 없음. `com.unity.nuget.mono-cecil` 1.11.6, Burst 1.8.30, Unity MCP(CoplayDev v10.3.0) |
| 동시 작업 | 같은 Editor를 다른 작업 주체가 쓰고 있었다(무관한 EditMode 테스트 54건 실행 관찰). 사용자 미커밋 변경 45개 파일은 수정하지 않았다(런처 파일은 기존 변경 위에 1줄 추가). |

## 구현 범위

| 위치 | 내용 |
| --- | --- |
| `Packages/com.uplayground.hotreload/Editor/` (asmdef `UPlayGround.HotReload.Editor`, Editor 전용, autoReferenced false) | `Protocol/`(Worker와 공유하는 IR·메시지), `Compilation/`(rsp 컨텍스트 캡처, 내용주소 스냅샷, Worker 클라이언트), `Patching/`(원본 멤버 재연결 `RuntimeResolver`, generator별 재구성 `PreparedBody`, 단일 owner `HarmonyBackend`), `Core/`(세대·계획·트랜잭션·journal·설정·RefreshLease·`HotReloadCoordinator` 상태 머신), `Watching/`, `Bootstrap/`(이벤트 단일 구독, 빌드 가드, import 감지), `AgentBridge/`(파일 inbox/outbox), `UI/`(UI Toolkit 창), `ThirdParty/Harmony/` |
| `Tools/HotReload.Worker/` | net10.0 Worker: rsp를 `CSharpCommandLineParser`로 해석해 전체 어셈블리 컴파일, Cecil IR 추출·정규화·지문, 표면/본문 diff, 적격성·생성 코드·스택 검증. 산출물 `bin/`은 Git 추적 밖 |
| `Packages/com.uplayground.hotreload/Tests/` | Editor 전용 fixture 2개 asmdef(+한글/공백 경로 파일), 테스트 asmdef(48건) |
| 게임 코드(G9) | `PlayerGroundMoveState.GetMaxMovementSpeed`, `EncounterCameraComposition.ResolveLockDistance`, `CombatPolicyResolver.GetPoiseDamageMultiplier`에 `#if UNITY_EDITOR [MethodImpl(NoInlining)]`만 추가 |
| 설정 | `ProjectSettings/ProjectHotReload.json`(위 3개 allowlist, enabledByDefault false). 개인 `UserSettings/ProjectHotReload.json`은 더 엄격하게만 적용 |
| 런처 | `UPlaygroundToolsLauncher` 디버그 카테고리 "Project Hot Reload" (`[UPlaygroundTool("UPlayGround/Debug/Project Hot Reload")]`) |

실제 통합 API: `CompilationPipeline.GetAssemblies/GetAssemblyNameFromScriptPath/compilationStarted/RequestScriptCompilation`, `AssemblyReloadEvents.beforeAssemblyReload`, `EditorApplication.update/playModeStateChanged/quitting`, `AssetDatabase.DisallowAutoRefresh/AllowAutoRefresh/Refresh`, `AssetPostprocessor.OnPostprocessAllAssets`, `IPreprocessBuildWithReport`, Harmony `Patch(transpiler)/Unpatch(owner)/GetPatchInfo`, Roslyn `CSharpCommandLineParser.Parse("@rsp")`·`CSharpGeneratorDriver`, Cecil `ModuleDefinition.ReadModule`. Unity MCP에는 전용 Hot Reload 도구를 추가하지 않았다(파일 bridge가 기본 연결).

## Gate 판정

| Gate | 판정 | 근거 |
| --- | --- | --- |
| G0 환경·기준 | PASS | 위 표, [G0 probe](unity-hot-reload/evidence/G0/project-baseline-probe.txt): Camera+Actor 558파일 W0↔A0 표면 0·본문 0 차이, 기준선 6.9s |
| G1 백엔드 증명 | PASS | `G1BackendProofTests` 7/7 (의존성 교체 전후 2회), [ADR 0002](unity-hot-reload/adr/0002-backend-proof.md) |
| G2 패키지·생명주기 | PARTIAL | T35·**T31 PASS**(실제 Play 20회, [증거](unity-hot-reload/evidence/G2/t31-driver.log)), T32·T33·T34 부분 |
| G3 Worker 컴파일러 | PASS | T05·T14·T15·T16, 실제 Actor/Camera parity |
| G4 IL·검증기 | PARTIAL | T01~T16·T36~T39·T41 PASS, **T40(Burst/Jobs/ILPP 대상 거부 테스트) 미수행** |
| G5 트랜잭션·롤백 | PASS(T28 일부) | T17~T23·T27 PASS, T28은 MVID 직접 조작 미주입 |
| G6 감시·revision | PARTIAL | T24·T26·T29 PASS(overlay 소스), 실제 FileSystemWatcher는 임시 폴더 단위 검증. 프로젝트 파일 디스크 저장→자동 적용은 사용자 테스트 대기 |
| G7 UI·Bridge | PARTIAL | Bridge e2e analyze→apply→중복→rollback·T30 PASS. UI 창 버튼 조작은 미수행(사용자 확인 대기) |
| G8 회귀 | PARTIAL | 아래 집계. Worker 단독 .NET 테스트 프로젝트는 만들지 않았다(Unity 테스트가 실제 Worker를 사용) |
| G9 실제 코드 | PARTIAL | Edit Mode에서 3개 실제 대상 parity·교체·관찰·원복 PASS. Play 중 이동·락온·전투 관찰, 패치 후 정상 컴파일 동작 일치 미수행 |
| G10 성능 | PARTIAL | 100회 패치 p50 107ms·p95 126ms·max 132ms, 로드 어셈블리 386→386, 관리 힙 +656KB, 세대 한도 보호 PASS. 60분 soak·Editor/Worker RSS·handle 미측정 |
| G11 빌드·제거 | NOT_RUN | 사용자 지시 없는 Player 빌드 금지. 정적 경계 감사만 PASS |
| G12 검수 | NOT_RUN | 단일 에이전트 자기 점검만. 독립 검수 미수행 |

## 필수 테스트 45개 + smoke 3개 집계 (실제 실행 기준)

- **PASS 36:** T01–T27, T29, T30, T31, T35–T39, T41 (T11은 `int/float.TryParse` out 호출로 검증, Raycast는 미포함). T31은 NUnit 테스트가 아니라 `T31PlayCycleDriver`로 검증했다(아래).
- **FAIL 0**
- **PARTIAL 6:** T28(MVID 조작 미주입), T32(Domain Reload ON 미시험), T33(실제 Ctrl+R 대신 같은 이벤트 처리기 직접 호출), T34(Editor 재시작 미시험), T42(100회 단기 측정만), T43(정적 감사만, Player 빌드 없음)
- **NOT_RUN 3:** T40, T44(도구 제거 후 clean compile), T45(clean checkout 재현)
- **Smoke:** P01 PARTIAL(hook 설치·제거만), P02·P03 PARTIAL(Edit Mode 직접 호출로 새 규칙·원복 관찰, Play 중 관찰 미수행)

실행 명령: Unity MCP `run_tests(EditMode, assembly=UPlayGround.HotReload.Editor.Tests)`. 1차 47건 중 45 PASS·2 FAIL(watcher 단언, T31 기본 180s 제한 초과) → 원인 수정 후 재실행([최근 결과](#최근-전체-실행)). Worker 빌드: `dotnet build Tools/HotReload.Worker/src/HotReload.Worker.csproj -c Release -o Tools/HotReload.Worker/bin/Release` (경고 0, 오류 0).

### 최근 전체 실행

2026-11-13 00:07~00:49, `UPlayGround.HotReload.Editor.Tests` 48건: **47 PASS / 1 FAIL / 0 SKIP**, 1350.9초. 실패는 `G2LifecycleTests.T31_PlayModeEnterExit20` 하나이며 `Timeout value of 1200000 ms was exceeded`(테스트 속성 Timeout을 180초→1200초로 올린 뒤에도 초과). 같은 실행에서 G4(IL 매트릭스)·G5(트랜잭션)·G7(Bridge)·G9(실제 코드 smoke)·G10·G11 정적 감사는 모두 통과했다. 첫 실행(47건)은 45/2 FAIL이었고 watcher 단언은 수정 후 통과했다.

T31 조사 메모: Editor 비포커스(`blocked_reason: editor_unfocused`)에서 Play 전환이 매우 느렸고, 출력에는 Play 진입 로그가 20회를 넘게 반복되어 있다. 테스트가 20회에서 끝나지 않고 반복되는지, 전환 지연인지는 **미규명**이다. 이 때문에 "Play 20회 후 lease·watcher·Worker 누수 없음"은 **검증되지 않았다**(테스트 코드는 각 회차마다 세션 정리를 단언하도록 되어 있다). 다음 행동: 회차마다 카운터 증거를 파일로 남기도록 고치고, 5회 단위로 나눠 포커스된 Editor에서 실행해 원인을 분리한다.

**T31 해소(2026-11-13): 원인은 무한 반복이 아니라 시간 부족이었다.** `Tests/Editor/T31PlayCycleDriver.cs`가 `EditorApplication.update` 상태 기계로 실제 Play 진입→Start→종료를 20회 수행했다(MCP `execute_code`로 `Begin(20)` 호출). 결과 20/20: 매 회차 Start `Ok`, 종료 후 `active=False lease=0 watchers=0 workers=0 subs=5`. 회차당 약 52~56초(진입)+약 16초(종료)로 총 1388초(약 23분)이며 기존 NUnit 제한 1200초를 넘는다. 포커스된 Editor 기준이며, 비포커스 첫 회차는 진입 77초였다. 관리 힙은 1.30~1.36GB 범위에서 오르내렸고 단조 증가하지 않았다(11회차 일시 1.5GB). 한계: 증거는 Domain Reload OFF 설정 기준이며 로드 어셈블리 수·handle·Worker RSS는 측정하지 않았다. NUnit `T31_PlayModeEnterExitOnce`는 UTF가 Play 전환 뒤 열거자를 다시 만들어 단독 실행이 불안정하므로 `[Explicit]`로 두고 전체 실행에서 제외했다.

이전 분리 진행 기록: 테스트를 회차 로그(`Library/ProjectHotReload/evidence/G2/t31-cycles.log`)를 남기는 1회짜리 `T31_PlayModeEnterExitOnce`로 바꿨다. 관찰: (1) 비포커스 Editor에서도 Play 진입 1회는 약 4.7초였다 → 전환 지연만으로 20분 초과를 설명하기 어렵다. (2) Play 진입 뒤 `RunPlayCycles` 지역 변수(`clock`)가 null이 되어 NRE가 났다 → Unity Test Framework가 Play 전환 뒤 열거자를 다시 만들어 지역 상태가 사라지는 것으로 의심되며, 원래 T31이 20회 넘게 반복된 원인 후보다(미확정). (3) 단독 실행하면 `Start`가 `BaselineMissing`으로 실패한다 → 다른 테스트가 만든 기준선에 의존하는 것으로 의심(미확정). 컴파일 직후 실행한 첫 시도는 타이밍 오류였다. T31은 계속 FAIL이며 PASS로 바꾸지 않았다.

## 발견·수정한 문제

1. 병합본 Harmony → Burst 해싱 오류. Thin+MonoMod로 교체 ([ADR 0002](unity-hot-reload/adr/0002-backend-proof.md)).
2. MonoMod.Backports net452가 BCL 타입 재정의 → MCP execute_code 등 CodeDom 컴파일 실패. netstandard 빌드로 교체.
3. `SessionState` 이름이 `UnityEditor.SessionState`와 충돌 → `HotReloadState`.
4. Unity Mono `FileSystemWatcher`는 폴링형(첫 이벤트 약 800ms, 곧바로 rename된 중간 이름 미관찰). 설계대로 최종 상태는 내용 해시로 판정하며 테스트 단언을 최종 상태 기준으로 고쳤다. 실효 debounce는 설정값(300ms)보다 길다.

## 사용법·복구

1. 대상 메서드에 `#if UNITY_EDITOR [System.Runtime.CompilerServices.MethodImpl(NoInlining)] #endif`를 추가하고 정상 컴파일한다. `ProjectSettings/ProjectHotReload.json`의 `allowlistedAssemblies`/`allowlistedMethods`(`네임스페이스.타입::메서드` 또는 `::*`)에 등록한다.
2. Worker를 빌드한다(위 명령). Play Mode 진입 → 툴 런처 디버그 › Project Hot Reload › Start. 상단 "Auto Refresh held by this session" 배지가 보이는 동안 다른 에셋 import도 지연될 수 있다.
3. 대상 메서드 본문을 저장하면 debounce·분석 후 자동 적용된다. 거부 사유(필드·시그니처·람다·async·allowlist 밖·NoInlining 없음)는 진단 탭에 정확한 코드와 위치로 표시된다.
4. Rollback/Revert All은 **코드만** 되돌린다. 소스·HP·위치·저장은 그대로이며 같은 소스는 자동 재적용하지 않는다(새 저장 또는 Reapply).
5. 끝낼 때 Play를 종료하거나 Stop & Recompile → 패치·lease·watcher·Worker가 정리되고 소스가 바뀌었으면 정상 컴파일이 요청된다. `Quarantined`면 추가 패치가 막히며 정상 컴파일 또는 Editor 재시작이 필요하다.
6. 에이전트: `Library/ProjectHotReload/bridge/bridge-info.json`·`session-token`을 읽고 `inbox/<GUID>.json`(임시 파일 후 rename)으로 `status → begin_edit_batch → 수정 → end_edit_batch(분석 결과 planId) → apply(planId, expectedSourceRevision, expectedEffectiveGeneration)`. 결과는 `outbox/<GUID>.json`.
7. 활성 세션 중 빌드는 `BuildRequiresRecompile`로 막힌다. 패키지 제거: `Packages/com.uplayground.hotreload` 삭제, `ProjectSettings/ProjectHotReload.json`·런처 1줄 제거(게임 코드의 NoInlining은 Editor 조건부라 남겨도 무해).

## 사용자 테스트 명세 (Play 관찰, 미수행)

1. Play 진입 → Project Hot Reload Start → 상태 `Idle · 대상 3개` 확인.
2. P01: 달리기/질주 중 `PlayerGroundMoveState.GetMaxMovementSpeed`의 Sprint 반환을 `* 1.5f`로 저장 → 다음 프레임부터 질주 속도 증가, 플레이어 HP·입력·현재 상태·Animancer 재생이 끊기지 않는지. Rollback → 속도 원복, HP는 원복되지 않음.
3. P02: 다수 적 전투에서 락온 → `ResolveLockDistance`의 비스윕 반환에 `+ 2f` 저장 → 락온 카메라 거리 증가, 락온 대상·카메라 리그 유지.
4. P03: `GetPoiseDamageMultiplier` 반환을 2배로 저장 → 같은 공격의 Poise 감소/브레이크가 빨라지는지, 적·플레이어 참조 유지.
5. 각 단계 후 Stop & Recompile → 같은 변경이 정상 컴파일된 게임에서도 같은 동작인지(소스를 되돌렸다면 원래 동작).
6. UI 각 탭·버튼(Pause/Resume, Dry Run→Apply, Revert All), 진단 클릭 시 파일 이동.

## 남은 작업과 다음 행동

### 2026-11-13 웹 조사·MCP 작업 흐름 확장 설계(D0)

- 사용자 방향: 향후 Unity MCP C# 작업에서 Hot Reload를 우선 사용하고, 재컴파일 차단이 도구 결함이면 Hot Reload를 수정하여 같은 작업을 재검증한다. 이번 요청 범위는 설계부터이며 런타임 구현은 변경하지 않았다.
- 상용 제품의 공식 기능·필드·Burst/Jobs·기술 질문·문제 해결 문서와 Unity 6000.3 컴파일 요청·Auto Refresh·Play 진입 문서를 조사했다. 출처와 프로젝트 판단을 [확장 설계](../../Assets/docs/design/dev-tools/UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md)에 함께 기록했다. 상용 지원 범위를 커스텀 도구의 지원으로 승계하지 않는다.
- 설계 범위: 변경 전 적격성 분류, 파일 Bridge 배치, 정상 컴파일·리로드를 넘는 작업 추적, MCP 복귀 확인, 차단 원인별 최소 수정·회귀 검사·동일 작업 재수행. 구현 순서는 D1 진단/Bridge → D2 컴파일 복구 → D3 검증 대상 확대 → D4 선택적 Edit Mode/MCP 어댑터다.
- AGENTS.md의 작업별 필독 표와 기존 사용 가이드에 향후 운영 원칙을 연결했다. 현재 Play 전용·세 메서드 allowlist·Play 종료 제한을 유지하고, 미구현 기능을 현재 절차와 구분했다.
- 자동 검사: `Tools/Harness/Test-Harness.ps1` 43건 통과(Unity 미실행, 합성 준비표 자동 정리). 새 설계·개발 도구 목차·가이드·AGENTS의 로컬 링크 43개 대상 존재 확인. 변경 문서 diff 공백 검사 통과. 신규 설계에 `.meta`를 동반했다.
- 미수행: D1~D4 구현, 실제 컴파일·패치·MCP 재연결, 사용자 Play 인수. 도구 전체의 `IMPLEMENTED_UNVERIFIED` 판정은 유지한다. 기존 G0 증거 링크 `unity-hot-reload/evidence/G0/project-baseline-probe.txt`의 대상 누락은 재확인했으며, 이 문서 작업으로 복원하거나 과거 검증을 재수행하지 않았다.
- 부산물: 신규 설계·meta와 지침·목차·상태 기록만 보존. 웹 원문 복제·임시 조사 파일·별도 빌드 산출물은 만들지 않았고 기존 사용자 미커밋 변경은 보존했다.

### 2026-11-13 Unity MCP 병행 사용 검토

- 실제 Editor의 파일 Bridge에 `status`를 보내 `completed/Ok`, `SessionState=Disabled`, 적용 메서드 0개 응답을 확인했다. Worker 실행 파일은 존재한다. 이번 점검은 과거 패치 테스트의 재실행이 아니다.
- `HotReloadBootstrap`의 컴파일 시작·스크립트 import 무효화 처리와 설치된 MCP `ManageScript`의 import 예약을 대조했다. 활성 세션에서는 MCP 스크립트 수정·Refresh 대신 파일 수정과 Bridge 배치를 사용해야 한다. 전용 MCP 도구는 현재 없다.
- 일반 Edit Mode 코드 변경은 MCP `refresh_unity`의 Refresh·정상 컴파일 요청 뒤 상태·Console을 확인하는 경로로 이어갈 수 있다. Unity 6용 설치 구현은 컴파일 요청 시 완료 대기를 생략하므로, 호출 반환을 컴파일 완료로 간주하면 안 된다. Editor 재시작은 기본 절차가 아니다.
- 현재 allowlist 세 메서드와 Play 전용 Start 제한은 유지한다. 일반 코드 전체의 무중단 반영으로 확대 판정하지 않는다.
- Console 조회 1회는 `Timeout receiving Unity response`; 이후 상태 응답은 Editor 세부 정보가 비어 있었다. 따라서 이번 실행의 정상 컴파일·MCP 재연결·Play 중 실제 패치 관찰은 미수행이다. 사용자 미커밋 코드가 다수 있어 검토 목적으로 임의 컴파일하거나 수정하지 않았다.
- [사용 가이드](../../Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md)에 일반 컴파일/상태 유지 경로, Bridge Start·완료 receipt 확인을 보완했다. 상태 요청 산출물은 Library Bridge outbox에 남으며 추적 소스·에셋은 변경하지 않았다.

- Player 빌드 시 도구·Harmony·Worker·fixture 미포함 확인(T43): 사용자 빌드 지시 후 기존 빌드 경로로 수행하고 산출물 `*_Data/Managed` 검사.
- T44: 패키지 임시 제거 후 clean compile(격리 worktree 권장). T45: clean checkout에서 Worker 빌드·테스트 재현.
- T40: Burst Job/ILPP 생성 코드를 가진 대상을 fixture에 추가해 거부 확인.
- T32(Domain Reload ON), T34(Editor 재시작), G10 60분·100회 디스크 저장·Play 전환 soak(메모리·handle·Worker RSS).
- 독립 검수(G12)는 [REVIEW_REQUEST.md](../전달용/Unity_HotReload_Agent_Handoff/REVIEW_REQUEST.md)로 별도 세션에 요청.
