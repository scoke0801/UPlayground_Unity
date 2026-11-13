# Project Hot Reload 사용 가이드

Play 중에 지정한 메서드의 **본문만** 저장 즉시 교체하는 Editor 전용 도구다. 수치 조정·로직 튜닝을 Domain Reload 없이 반복할 때 쓴다.

- 작업 상태·검증 결과: [unity-hot-reload.md](../../../docs/work-status/unity-hot-reload.md)
- 설계 정본: [DESIGN_SPEC.md](../../../docs/전달용/Unity_HotReload_Agent_Handoff/DESIGN_SPEC.md)
- MCP 연동·컴파일 복구 확장 설계: [UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md](../design/dev-tools/UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md)
- 현재 판정은 `IMPLEMENTED_UNVERIFIED`다. Play 중 실제 게임 관찰(P01~P03)과 UI 조작은 아직 사용자 확인 전이므로, 동작이 다르면 상태 문서에 기록한다.

## 할 수 있는 것과 없는 것

| 가능 | 불가능 (진단 탭에 거부 사유로 표시) |
| --- | --- |
| allowlist에 등록한 메서드의 **본문** 변경 | 필드 추가·삭제, 시그니처 변경 |
| 호출 대상·상수·분기 변경 | 람다, async/iterator 등 생성 코드를 바꾸는 변경 |
| | allowlist 밖 메서드, `NoInlining` 없는 메서드 |
| | Burst/Jobs/ILPP 대상 (T40 거부 테스트는 아직 미수행) |

## 1. 준비 (대상 메서드마다 한 번)

1. 대상 메서드에 아래를 붙이고 **정상 컴파일**한다. 인라인되면 패치가 적용되지 않는다. Player 빌드에 영향이 없도록 Editor 조건부로 둔다.
   ```csharp
   #if UNITY_EDITOR
   [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
   #endif
   ```
2. `ProjectSettings/ProjectHotReload.json`에 등록한다.
   - `allowlistedAssemblies`: 대상 어셈블리 이름
   - `allowlistedMethods`: `네임스페이스.타입::메서드` 또는 `::*`
   - 개인 `UserSettings/ProjectHotReload.json`은 이 설정보다 **더 엄격하게만** 적용된다.
3. Worker를 빌드한다 (처음 한 번, 또는 Worker 코드를 바꾼 뒤).
   ```
   dotnet build Tools/HotReload.Worker/src/HotReload.Worker.csproj -c Release -o Tools/HotReload.Worker/bin/Release
   ```

현재 등록된 대상: `PlayerGroundMoveState.GetMaxMovementSpeed`, `EncounterCameraComposition.ResolveLockDistance`, `CombatPolicyResolver.GetPoiseDamageMultiplier`.

## 2. 사용

1. Play 모드로 들어간다.
2. 툴 런처 **디버그 › Project Hot Reload**를 열고 **Start**를 누른다. `Idle · 대상 N개`가 보이면 준비 완료다.
3. 대상 메서드 본문을 고치고 저장한다. debounce와 분석을 거쳐 자동 적용된다. 파일 감시는 폴링형이라 실제 반응은 설정 debounce(300ms)보다 늦을 수 있다.
4. 거부되면 진단 탭에서 사유 코드와 위치를 확인하고, 항목을 클릭하면 해당 파일로 이동한다.

## 3. 되돌리기와 종료

| 동작 | 결과 |
| --- | --- |
| Rollback / Revert All | **코드만** 원복. HP·위치·저장 데이터는 그대로. 같은 소스는 자동 재적용되지 않으며, 새로 저장하거나 Reapply 해야 한다 |
| Play 종료 / Stop & Recompile | 패치·lease·watcher·Worker 정리. 소스가 바뀌었으면 정상 컴파일 요청 |
| 상태 `Quarantined` | 추가 패치 차단. 정상 컴파일 또는 Editor 재시작 필요 |

**패치는 임시다.** 소스를 되돌리지 않고 종료하면 정상 컴파일된 코드가 곧 최종 동작이므로, 종료 뒤 같은 동작인지 확인한다.

## 4. 주의

- 세션 중 상단에 "Auto Refresh held by this session" 배지가 보인다. 이 동안 다른 에셋 import가 지연될 수 있다. 도구는 사용자의 Auto Refresh 설정을 바꾸지 않는다.
- 세션이 활성이면 빌드가 `BuildRequiresRecompile`로 막힌다.
- 외부에서 스크립트를 import하거나 정상 컴파일이 시작되면 세션이 무효화되고 자신의 패치를 정리한다.
- 커밋 전에는 Rollback 또는 Stop으로 패치를 정리하고, 의도한 소스 변경만 남았는지 확인한다.

## 5. 에이전트 연동

에이전트는 파일 bridge로 조작한다. Unity MCP에는 전용 도구가 없다.

1. `Library/ProjectHotReload/bridge/bridge-info.json`과 `session-token`을 읽는다.
2. `inbox/<GUID>.json`을 임시 파일로 쓴 뒤 rename한다.
3. 순서: `status` → Play 중 비활성 상태라면 `start` → `begin_edit_batch` → 소스 수정 → `end_edit_batch`(분석 완료 결과의 `planId`) → `apply(planId, expectedSourceRevision, expectedEffectiveGeneration)`.
4. 결과는 `outbox/<GUID>.json`에서 읽는다. `accepted`는 분석 접수이며 완료가 아니다. 최종 receipt의 `State`, `ResultCode`, `SessionId`, `SourceRevision`, `EffectiveGeneration`을 확인한다. 세션 종속 요청에는 현재 `sessionId`, 배치 시작·종료에는 같은 `batchOwner`를 보낸다. 적용 요청의 예상 revision·generation은 분석 완료 receipt 값을 사용한다.

### Unity MCP와 함께 작업할 때

앞으로 MCP로 C#을 수정할 때는 **변경 전 Hot Reload 적격성을 확인하고 지원 범위에서 우선 사용한다.** 현재는 Play 중 등록된 세 메서드만 해당하며, Edit Mode에서 쓰기 위해 불필요하게 Play를 시작하지 않는다. 소스 변경 전 세션·배치 상태를 확인하고 다른 작업의 세션을 가로채지 않는다.

재컴파일이나 MCP 후속 작업이 막히면 먼저 소스 오류·지원 경계·도구 결함·Unity 진행 중·통신 단절을 구분한다. **도구 결함이면 Hot Reload를 수정하고 관련 회귀 검사와 원래 막힌 작업을 재수행한다.** 동일 요청·Refresh 반복이나 Editor 재시작만으로 종료하지 않는다. 차단 원인·수정·재시도 결과는 기존 작업 상태 문서에 남긴다. 현재 Play 종료 제한은 다른 MCP 명령으로 우회하지 않는다.

확장 설계의 비동기 컴파일 작업 추적·자동 복구·Edit Mode 패치·MCP 전용 어댑터는 아직 구현되지 않았다. 아래는 현재 도구로 수행할 절차다.

- **일반 코드 작업(Edit Mode):** 소스 저장 뒤 `refresh_unity(mode="force", scope="all", compile="request", wait_for_ready=false)`로 정상 컴파일을 요청한다. Editor를 종료할 필요는 없다. 이후 `mcpforunity://editor/state`에서 컴파일 시작·완료 또는 Domain Reload 진행을 확인하고, 컴파일·리로드가 끝난 상태와 Console 컴파일 오류 부재를 확인한 뒤 후속 MCP 작업을 진행한다. 요청 직후의 `ready_for_tools=true`만으로 완료를 판단하지 않는다.
- **Play 상태 유지:** 위 Bridge 배치를 사용하고 소스는 파일 도구로 수정한다. 현재 MCP의 `ManageScript` 경로는 import/컴파일을 예약하므로 활성 Hot Reload 세션과 함께 쓰지 않는다. `refresh_unity`의 Refresh·컴파일 요청, 스크립트 import도 세션을 무효화한다.
- **지원 범위 밖 변경:** 필드·타입·시그니처·생성 코드 변경이나 allowlist 밖 수정은 정상 컴파일로 전환한다. Play 상태 보존은 보장하지 않으며, Editor 재시작은 기본 절차가 아니다. 현재 설정은 에이전트의 `recompile_and_stop`을 통한 Play 종료를 허용하지 않는다(`allowPlayModeExitFromAgent=false`).
- **현재 범위:** 등록된 세 메서드만 대상으로 사용한다. 새 대상은 allowlist와 `NoInlining` 준비 후 정상 컴파일이 먼저 필요하다.

2026-11-13 연결 점검: 실제 파일 Bridge `status`가 `completed/Ok`, `SessionState=Disabled`로 응답했다. Worker 실행 파일 존재도 확인했다. 이번 점검에서 코드 수정·패치·정상 컴파일·Play 관찰은 수행하지 않았다. MCP Console 조회는 한 차례 응답 시간 초과였고 이후 상태 조회는 Editor 세부 정보가 비어 있어, MCP 후속 작업 연결 안정성은 미확인이다.

## 6. 직접 확인 체크리스트 (Play 관찰, 미수행)

| 항목 | 방법 | 기대 결과 |
| --- | --- | --- |
| P01 | 질주 중 `GetMaxMovementSpeed`의 Sprint 반환을 `* 1.5f`로 저장 | 다음 프레임부터 속도 증가. HP·입력·현재 상태·Animancer 재생 유지. Rollback 시 속도만 원복 |
| P02 | 다수 적 락온 중 `ResolveLockDistance` 비스윕 반환에 `+ 2f` 저장 | 락온 카메라 거리 증가. 락온 대상·카메라 리그 유지 |
| P03 | `GetPoiseDamageMultiplier` 반환을 2배로 저장 | 같은 공격의 Poise 감소·브레이크가 빨라짐. 적·플레이어 참조 유지 |
| 정상 컴파일 | 각 단계 후 Stop & Recompile | 정상 컴파일된 게임도 같은 동작 (소스를 되돌렸다면 원래 동작) |
| UI | 각 탭, Pause/Resume, Dry Run→Apply, Revert All | 버튼이 의도대로 동작, 진단 클릭 시 파일 이동 |

## 7. 문제 해결

| 증상 | 확인 |
| --- | --- |
| Start가 `BaselineMissing` | 컴파일 중이거나 마지막 컴파일이 실패한 상태. 정상 컴파일 완료 후 다시 Start |
| Start가 `Play Mode에서만 세션을 시작합니다` | Play 모드 진입 후 Start |
| 적용되지 않음 | `NoInlining` 부착 여부, allowlist 등록, 진단 탭의 거부 사유 |
| `WorkerUnavailable` | Worker 빌드 여부(1단계 3번) |
| `Quarantined` | 정상 컴파일 또는 Editor 재시작 |
| Play 진입이 매우 느림 | 월드 씬 기준 1회 약 1분(실측 52~77초). 비포커스 Editor에서 더 느릴 수 있다 |

## 8. 제거

`Packages/com.uplayground.hotreload`를 삭제하고 `ProjectSettings/ProjectHotReload.json`과 툴 런처의 해당 1줄을 지운다. 게임 코드의 `NoInlining`은 Editor 조건부라 남겨도 무해하다. 제거 후 clean compile은 아직 검증하지 않았다(T44).
