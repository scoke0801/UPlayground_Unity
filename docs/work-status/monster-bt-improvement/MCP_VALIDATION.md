# MCP 실전 검증과 수정

2026-11-14. 사용자의 직접 MCP 검증 요청으로 열린 단일 Editor에서 LakeVillageWorld를 실행했다. 씬·원본 SO를 저장하지 않았고 Player/Addressables 빌드도 하지 않았다. [전투 위키](../../wiki/combat.md) · [작업 상태](STATUS.md).

## 정상 컴파일 후 재검증 결과

사용자가 Play 종료와 컴파일을 완료했다고 알려준 뒤 Editor의 `playing=false`, `compiling=false`, Console 오류 0을 확인하고 재개했다.

- AI 어셈블리 EditMode **151/152 통과**. 실패 1건은 `LayaTransportTests`의 로컬 모의 HTTP 서버(127.0.0.1:18765) 응답 검사이며 기대 200/실제 0이었다. 몬스터 검증과 별개로 기록하고 코드를 변경하지 않았다. [전체 요약·실패](mcp/COMPILED_AI_TESTS.json).
- 이번 변경을 직접 다루는 이동·플레이어 읽기·BT 정적 검증 **43/43 통과**. 새 비충돌 레이어 제외/실제 벽 차단 2건 포함. [개별 결과](mcp/COMPILED_RELATED_TESTS.json).
- **E01 통과(관찰 범위):** 원본 해골 궁수 데이터(마스크 -1)를 그대로 사용해 가상 게임패드로 60초 접근. 구르기 0회, 최대 귀속 거리 17.904m, 복귀 및 교전 재개 확인. 런타임 SO 보정 없이 컴파일된 충돌 수정으로 실행했다. [기록](mcp/E01-compiled.txt).
- **E02 부분 확인:** 실제 게임패드 공격 입력 각 5회로 해골 궁수 HP 1.0→0.264, Humanoid 궁수 1.0→0.882. 최대 귀속 거리 각각 18.021m/18.000m(기존 오차 허용 0.2m 이내). 두 표본 모두 구르기 0회이므로 구르기 뒤 무적 해제·추적 공격 기회와 체감은 판정하지 않는다. [해골 기록](mcp/E02-compiled-before-slope-fix.txt) · [Humanoid 기록](mcp/E02-humanoid.txt).
- **E03 실패:** 해골이 공격 중 이동한 급경사 지점에서 복귀 정지. 현재 `(1132.452, 48.795, 389.405)`, 귀속 `(1118.367, 52.353, 398.170)`. 두 점 모두 NavMesh에 잡히지만 연결은 `PathPartial`, 코너 1개로 끝난다. [실측](mcp/E03-navmesh.txt). 이 위치의 지형·NavMesh 연결과 실제 통행 가능성을 수정·검증해야 한다. 전투 코드만으로 복귀 완료했다고 보고하지 않는다.
- 국소 우회 방향 확대/발판 조회 거리 축소를 Hot Reload로 적용해 비교했지만 각각 16.920m/16.590m에서 다시 막혔다. 효과가 충분하지 않아 **두 실험을 소스와 런타임 모두 원복**했다. [넓은 우회 기록](mcp/E03-wide-detour-only.txt) · [짧은 발판 기록](mcp/E03-local-probe-only.txt) · [컴파일 기준 동작 복구 receipt](mcp/HOT_RELOAD_COMPILED_BASELINE_RESTORED.json). 최종 소스는 새 충돌 필터를 포함한 사용자 컴파일본과 같다.

공격 자동화 첫 시도에서 비트필드 버튼에 DeltaStateEvent를 보내는 검사용 콜백 오류가 있었다. GamepadState 전체 이벤트로 고친 뒤 위 공격 기록을 수집했다. 해당 예외를 제품 오류나 공격 검사 통과로 기록하지 않는다.

[실제 전투 화면](mcp/E02-compiled-screen.png). 화면은 실행 근거이며 전투 체감 합격 판정이 아니다.

## 재현과 수정

- 실제 해골 궁수를 정상 생성 경로로 소환하고 가상 게임패드 이동으로 60초간 접근했다. 공격 없이 구르기 0회, 귀속 최대 거리 18.000m였지만 복귀가 17.788m에서 막혔다. [최초 기록](mcp/E01-before.txt).
- 안쪽 사선 우회 후보를 추가해 Hot Reload로 같은 Play에 적용했다. 14.222m까지 돌아왔지만 계속 막혔다. [우회만 적용한 기록](mcp/E01-detour-only.txt).
- 물리 조회가 실제 KCC 충돌 레이어를 무시해 비충돌 Pet 레이어의 겹친 펫을 장애물로 판정했다. 두 조회에 `movementObstacleMask & Motor.CollidableLayers`와 개별 무시 충돌 필터를 추가했다. 이후 사용자 정상 컴파일과 위 회귀 검사 2건, 실제 E01로 확인했다.
- 원본 BehaviorData를 보존한 런타임 복제본에만 KCC 마스크를 적용해 원인을 분리했다. 60초 재실행에서 최대 17.999m, 구르기 0회, 마지막 귀속 거리 1.239m로 복귀 후 공격 상태가 확인됐다. [런타임 보정 기록](mcp/E01-runtime-mask.txt). 이는 최종 소스 컴파일 검증이나 사용자 체감 합격을 대신하지 않는다.

## Hot Reload 도구 결함

최초 패치에서 Cecil의 `ldelem.any`를 Reflection.Emit 명령으로 바꾸지 못해 `UnsupportedIL`이 발생했다. Worker에 배열 읽기·쓰기 opcode 정규화를 추가했다. Worker Release 빌드 오류/경고 0, 실제 Cecil 추출 회귀 검사 2/2 통과. 같은 Play에서 Worker만 재시작해 우회 패치 적용을 확인했다(소스 revision 1, generation 1, 적용 1개).

[최초 실패](mcp/HOT_RELOAD_APPLY.json) · [수정 후 적용](mcp/HOT_RELOAD_FIXED_APPLY.json) · [Worker 검사](mcp/WORKER_ARRAY_TEST_RESULTS.txt).

## 남은 검증

- 충돌 조회 헬퍼는 현재 allowlist 밖이다. 배치 분석은 `FullCompileRequired`, 적용 0개로 전체 거부했다. [분석 receipt](mcp/HOT_RELOAD_FULL_COMPILE_REQUIRED.json).
- 최초 정상 컴파일 대기는 사용자 직접 종료·컴파일로 해소됐다. 새 회귀 검사와 E01 재현 결과는 위에 기록했다.
- 최초 E02는 입력 콜백만 호출해 구르기/명중을 확인하지 못했다. [최초 기록](mcp/E02-input-attempt.txt). 이후 정상 입력 경로의 공격 명중은 확인했지만 구르기 후 공격 기회와 체감은 미검증이다.
- E04는 플레이어 위치를 22/17/10m로 제어해 5주기 관찰했다. 복귀 진입과 종료 후 재교전을 확인했고 HP 비율은 1.0을 유지했다. 17m 동안 지속 감지/시야가 보장되지 않아 히스테리시스 전체 합격으로 판정하지 않는다. [위치 자극 기록](mcp/E04-position-stimulus.txt).
- E03은 위 급경사 사례에서 실패했다. E05~E08 및 R01/R02의 실제 시나리오는 아직 미수행이다. P3~P5 사용자 검수 선행 조건은 그대로 남는다.

## 종료 전 상태

검사용 가상 입력 장치·콜백과 이번 Play에 생성한 궁수 2개를 정리했다. 플레이어 위치와 테스트 무적(false)을 복구했다. 씬 dirty=false이며 Play는 유지했다. Hot Reload 실험 override를 제거하고 세션을 종료했다. 최종 충돌 조회 수정은 정상 컴파일본에 반영돼 있다. 별도 빌드·커밋은 하지 않았다.

이번 재검증의 원본 기록·화면은 위 `mcp/`에 보존하고 `output/monster-bt-mcp/`의 동일 사본 7개는 정리했다. 다른 작업의 임시 씬·아트·추적 밖 에셋과 이전 세션 자료는 건드리지 않았다. 인계 문서 링크와 diff 공백 검사를 확인했다.
