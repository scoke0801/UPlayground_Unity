# LakeVillageWorld 대기 프레임 저하 — 2026-11-14~15

상태: **Editor Debug의 초기화 후 대기 성능 기준 통과**. 최종 저장본 acceptance-3은 맑음/비 각600프레임에서 임의500ms 구간 최저33.81/31.46FPS였다. 해상도/식생 수를 유지하며 실시간 그림자를25m/1cascade로 줄이고 SSAO/빈 Decal 패스를 껐다. 단일 프레임 최대88.25ms와 기존 지역 배치 로더 오류는 남는다. 전투·이동·전체 NPC 배치·미술 인수는 별도 사용자 확인 대상이다. 아래는 단계별 조사 이력이며 현재 판정은 문서 끝의 최종 표를 우선한다. [월드 위키](../wiki/world-art.md).

## 원인과 기준선

사용자 제보 1 FPS / 719.8ms. 기존 미커밋 변경을 보존하고 열린 단일 Editor의 LakeVillageWorld에서 직접 재현했다. RTX 3060 Ti, 활성 환경 대상18,472개. Profiler CPU 프레임793~896ms, FixedUpdate당38~51ms가 프레임당16~17회 실행된다. ReactiveEnvironment.Tick81~141ms, 렌더 루프21~24ms. 물리 스텝 추격이 CPU 비용을 증폭한다. Draw Calls4,038·Triangles11,713,934는 별도 렌더 비용이며 이번 증상의 주원인으로 단정하지 않는다.

일시정지 후 실제 FixedTickEnvironment(0.02) 직접5회:40.68/38.89/37.62/39.02/37.69ms. 식생의 _combustionEffects를 임시 제외하고 finally 복원한 비교3회:8.15/7.39/7.55ms, Terrain 연출만1.70~1.95ms. 효과 없는 개체도 앵커·바람·서비스·카메라 예산을 매 물리 스텝 조회한다. 합성 비교는 FPS 인수 결과가 아니다. 측정용 Play 종료, 씬/사용자 저장 슬롯에는 기록하지 않았다.

## 수정 범위

출력 가능한 화염/연기·남은 소화 잔연·반환할 emitter가 없는 표현은 조회를 생략한다. 화학/강수/전파/저장과 낙하·피격은 유지한다. 대상은 Hot Reload allowlist 밖이며 속성 추가도 필요하므로 정상 컴파일을 사용한다.

## 검증 및 남은 확인

수정 후 컴파일·연출 회귀·동일 월드 프레임 측정 예정. Player/Addressables 빌드·커밋은 수행하지 않는다. 최종 미술·일반 입력·화재/강수·저장 왕복은 사용자 테스트 대기.

## 물리 갱신 축소 단계

첫 개선 후 실제 FixedTickEnvironment는8.16~10.55ms였고 프레임125~167ms 구간과 부하 증가 시271~840ms가 남아30FPS 미달이었다. 유휴 전체 대상의 네이티브 생존 확인·무동작 FixedTick 호출을 생략하도록 HasPendingFixedTick 계약을 추가했다. 기존 대상은 기본 true, Terrain 셀은 false, 식생은 낙하/착지·피격·연소 연출 대기 상태로 결정한다. 파괴 참조 전수 정리는 기존 논리 tick이 계속 수행한다. 연소·피격·낙하 재활성 회귀를 포함해 관련 EditMode52/52 통과(job158a2aca95f24c28a710b232de38eed4). 동일 월드 재측정 중.

사용자 추가 목표: 최소30FPS 확보까지 개선 지속(/goal). 현재 목표 미달이며 활성 진행 중이다.

## 강수·렌더 비용 축소 단계

강수 분해(대상18,472·Rain0.3854): 실제 논리 tick59~66ms, 별도 단계 수집27~28ms·물리11ms·논리4ms. Transform/활성 조회12ms와5개 처마 볼륨의 OBB 조회7ms를 확인했다. 처마 OBB 앞에 현재 Transform으로 계산한 보수적 AABB를 추가했다. 물리 쿼리는 첫 hit1개로 외부 지붕 유무를 판정하고, 자기 Collider가 먼저 맞으면 기존32 hit/포화 경로로 되돌려 판정을 유지한다. Terrain 유휴 연출은 셀 snapshot 변경이나 잔연 대기가 없으면 패치 재집계를 생략한다.

프로젝트 소유 식생 파생 재질34개만 인스턴싱을 활성화했고 재생성 저작기도 같은 설정을 사용한다. 비교 실험의 임시 변경은 전부 복원한 뒤 카탈로그 소유 재질만 Undo group으로 수정·저장했다. 건강한 표면의 반경0 상처 MPB를 생성하지 않아 불필요한 비인스턴스 속성을 피한다. 관련 EditMode70/70 통과(job0b1b8b56548447fdbff262109f72b084): 연출·통합·표면·강수·저작, 회전 처마/이동/자기 충돌/포화/이전 hit 정리 포함. 현재 성능 재측정 중,30FPS 목표 미달 상태를 유지한다.

## 유휴 순회 후속과 컴파일 반영 확인

단계3 Profiler OFF 자연 날씨의180프레임은 평균36.85ms(27.14FPS), 중앙33.85ms·P95 52.55ms·최대58.78ms로 목표 미달이었다. [원본](../../output/LakeVillagePerformance/frames-stage3.csv). 중복 IsAvailable 조회를 논리 스텝 시작 한 번으로 모으고 이후 비활성/파괴는 기존 Unregister tombstone으로 처리한다. 비파괴·보상 완료 대상의 보상 서비스 조회도 생략한다. 카메라/거리 기반 논리 생략은 없다.

MCP if_dirty Refresh가 외부 편집을 아직 가져오지 않은 상태에서 compile 요청만 하고 준비 완료를 반환하는 경우를 실제 로드 메서드 부재로 확인했다. force/all Refresh 후 새 검사 메서드 존재·어셈블리 리로드를 직접 확인하고 [최신 EditMode72/72](../../output/LakeVillagePerformance/editmode-72.json)를 통과했다(job97bdb65342c24a32918e26c1746525be). 앞선70건은 그 당시 로드된 변경 범위의 결과이며 최신 전체 검증으로 승계하지 않는다. 처마 broad phase는 Bounds.Contains 네이티브 호출 대신 관리 코드 축 비교를 사용한다.

## 고정 조건 측정과 후속

단계4의2560×1440 Game View 최대화·Profiler OFF·날씨 미리보기0/1 각180프레임: 맑음 평균33.13ms(30.18FPS)·P95 54.87ms·최대60.85ms, 강수 평균37.67ms(26.54FPS)·P95 78.77ms·최대81.55ms. [원본](../../output/LakeVillagePerformance/frames-stage4-focused.csv). 평균만30을 넘긴 맑음도 최소 프레임 목표 통과로 처리하지 않는다. 미리보기는 자동 복원했고 Game View 최대화도 종료 전에 원래 상태로 복원했다. 일반 편집 레이아웃의 초반 단계4 표본에는1.35초 초기 스파이크가 섞여 별도 보존하며 안정 상태 성능 근거로 사용하지 않는다.

강수 단계 후속은 같은 스텝의 날씨 값 공유, Awake에서 샘플 GameObject 캐시, 처마 AABB의 참조 조회·축 비교를 사용한다. 유휴 연소 표현은 Inactive 상태/잔여 emitter 유무를 먼저 확인해 Unity 생존 조회를 피한다. 최신 컴파일·회귀·성능 측정 진행 중.

## 사용자 확인 명세

- LV-P01 / 사용자 테스트 대기: LakeVillageWorld, 기존 품질/시야·2560×1440, Game View에 포커스. 초기 로딩 뒤5초 대기하고 이동 없이 표시 FPS를10초 관찰한다. 맑음과 강수에서 각각30FPS 아래로 지속 하락하거나0.2초 간격의 큰 끊김이 보이면 실패. 실제 표시 최저/평균 FPS·날씨·해상도·Game View 최대화 여부를 남긴다.
- LV-P02 / 사용자 테스트 대기: 가까운 나무에 일반 타격→불 부여 공격→비/물 소화. 피격 흔들림·파손/착지·발화가 즉시 시작되고 소화 후 잔연이 끝나야 한다. 먼 곳/화면 밖 반응과 재접근 시 상태가 유지되는지 확인한다. 반응 누락·입자 무한 반복·낙하 관통/정착 실패는 실패다.
- LV-P03 / 사용자 테스트 대기: 별도 검증용 저장 슬롯에서 반응시킨 대상과 Terrain 풀 상태를 저장→지역 왕복→로드한다. 젖음/연소·파손·채집 보상 중복이 없어야 한다. 기존 실제 저장 슬롯은 자동 검사에서 기록하지 않았다.

회신: 항목 ID / 통과·실패·미수행 / 실제 결과 / 필요 시 화면 또는 Console.

## 단계5 판정과 추가 경로

최신 Debug·최대화·맑음/강수 각300프레임에서도 평균29.64/24.57FPS, 최저30프레임 창25.61/21.67FPS로 목표 미달이었다. [원본](../../output/LakeVillagePerformance/frames-stage5-focused.csv). 샘플 GameObject·날씨 공유와 처마 복사를 줄였지만 단일 논리 tick 부하가 남는다. 관련73/73은 해당 코드까지 통과했으며, 이후 정상 표면의 상처 위치 조회·구조 SetActive·불꽃 SetActive를 실제 전이 때만 하도록 추가 수정했다. 유휴 효과도 최초 Heating/연소 snapshot 때 지연 생성한다.

Editor Debug/Release 비용을 분리하기 위해 CompilationPipeline.codeOptimization을 Release로 바꾸고 정상 리로드 뒤 실제 Release 값을 확인했다. 초기 설정 직후 강제 Refresh를 겹친 실행은 Debug로 돌아가 있었으므로 그 결과를 Release 측정으로 기록하지 않는다. 두 번째 설정 뒤 컴파일·리로드가 끝난 현재 값은 Release다. 이 모드에서는 스크립트 디버깅에 제약이 있으며 Player/Addressables 빌드를 뜻하지 않는다. 성능 비교 진행 중이며 최종 유지/복원 여부를 명시한다.

## Release 비교와 정상 표면 후속

단계6의 Release·최대화·맑음/강수 각300프레임은 평균35.22/31.12FPS, 최저30프레임 창30.89/27.89FPS였다. [원본](../../output/LakeVillagePerformance/frames-stage6b-release.csv). 강수 창이30에 못 미쳐 목표는 계속 활성이다. 첫 Release 표본은 초기화 대기 중 수집 기한이 지나0건이므로 성능 통과 근거에서 제외했다. 별도 [강수 비용 분해](../../output/LakeVillagePerformance/stages-stage6-release.txt)는 실제tick28.9~30.2ms, 수집9.6~14.0ms·물리6.1~7.2ms다. 별도 분해 후반은 젖음 포화 영향으로 전체tick과 동일 조건이 아니므로 합계를 전후 개선율로 사용하지 않는다.

반복 구조 재질38개에 대한 인스턴싱 비교는 draw 감소가 미미해 전부 원복했고 제품 변경에 포함하지 않았다. 정규 저작 원본/구매 재질은 수정하지 않았다. 다음 변경은 기존 GPU 표면이 있고 연소/파손/내구도/파괴 참조가 그대로인 평상시 젖음 snapshot에서 구조·상처·연출 연결을 건너뛰고 표면만 갱신하는 경로다. 발화 전이·내구도 변화는 기존 전체 경로로 복귀한다.

## 단계7 최종 측정

같은 대상18,472개·2560×1440·Game View 최대화·Release·Profiler OFF, 초기300프레임 이후 맑음/강수 각300프레임을 수집했다. [600프레임 원본](../../output/LakeVillagePerformance/frames-stage7-release.csv)·[통계](../../output/LakeVillagePerformance/summary-stage7.json).

| 조건 | 평균 FPS | 가장 느린0.5초 창 FPS | 중앙/P95 프레임 | 단일 최대 프레임 |
|---|---:|---:|---:|---:|
| 맑음 |37.18|31.65|26.13/32.94ms|70.46ms|
| 강수1/Storm1 |36.06|31.43|24.24/49.67ms|55.17ms|

완료 판단의 FPS 단위는 기존 `UIFrameRateDisplay`가 실제 경과 시간으로 갱신하는0.5초 구간이다. 모든 단일 프레임이33.3ms 이하라는 판정은 아니다. 강수 tick의 순간 프레임 증가와 최종 미술/플레이 인수는 남는다. [실제 표시값30회](../../output/LakeVillagePerformance/ui-fps-final.csv)도 맑음34~43FPS·강수34~41FPS를 확인했다. 초기 조회는 표시가 비활성이라 실패했고, 기존 Overlay API로 측정 동안만 켠 뒤 원래 숨김 상태로 복원했다. 표시 계산/갱신 주기와 FPS 텍스트 코드는 바꾸지 않았다.

관련 최종 EditMode74/74(job5f03533ee91a4d5fbe34a4b8831a008c)는 Release에서 젖음 전용 snapshot 후 발화와 기존 연출/강수/표면/저작/통합을 통과했다. [원본 결과](../../output/LakeVillagePerformance/editmode-74.json). Player·Addressables 빌드는 별도 지시가 없어 미수행이다.

## 최종 인계

원래 편집기 레이아웃·Game View 포커스·Release·2560×1440·강수1/Storm1에서 [실제 FPS 표시15회](../../output/LakeVillagePerformance/ui-fps-normal-layout.csv)도31~41FPS를 확인했다. 따라서 이번 대기 상태 목표는 기존0.5초 표시 주기 기준 최소30FPS 확보로 판정한다. 단일 프레임33.3ms 보장이나 일반 이동/전투·대규모 화재 성능 통과를 뜻하지 않는다. FPS 계산/표시 코드는 변경하지 않았다.

유휴 물리 연출의 전수 네이티브 조회·추격 반복을 줄였고, 같은 수의 논리 대상/식생·강수 판정·전파/저장을 유지했다. 정상 젖음 표면은 공유 표현만 갱신하고 발화/내구도/파손은 전체 경로를 계속 사용한다. 파생 식생 재질34개는 m_EnableInstancingVariants 0→1만 변경됐음을 전수 diff로 확인했다. Unity 자동 저장에서 추가된 두 셰이더 기본 속성은 자동 재직렬화로 분류해 해당 줄만 제거했다. 원본 구매 재질·meta·씬·지형 원본을 수정하지 않았고, LakeVillageWorld 씬273,555,090바이트/19:26:29 시각을 유지한다.

최종 Editor는 LakeVillageWorld Edit Mode, Game View는 이전 최대화 상태로 복원했다. 날씨 미리보기·임시 구조 재질38개 변경·프로파일러/CPU 수집·FPS Overlay를 해제했다. 종료 뒤 표면 GPU 버퍼 active0/null·키워드 해제·씬 dirty=False를 확인했다. **Editor 코드 최적화 모드는 Release로 유지**한다. 스크립트 중단점 디버깅이 필요하면 Debug로 돌릴 수 있으나 그 조건의30FPS 유지는 이번 최종 성능 판정 범위가 아니다. Player·Addressables 빌드·커밋·실제 저장 슬롯 쓰기는 하지 않았다.

부산물은 output/LakeVillagePerformance의 약37KB 측정/실패 준비/검사 근거와 하네스 준비표만 보존한다. 기존 사용자 변경·구매/MCP 원본·메타·다른 작업 출력은 정리 대상에서 제외했다. 임시 실행 코드는 메모리 안에서만 사용했고 완료/시간 제한 때 콜백을 해제했다. [월드 위키](../wiki/world-art.md)에서 이 상태 문서로 연결한다.

## Debug 기준 후속 — 사용자 목표 정정

사용자가 Debug 모드30FPS를 기준으로 제시했다. 앞선 Release31FPS 결과를 이 목표의 완료로 승계하지 않는다. Editor를 Debug로 복원하고 최종 코드의 같은 맑음/강수·해상도·기존 레이아웃을 재측정한 뒤 필요한 병목만 추가 개선한다. 식생 수·논리 반응/전파·저장과 기존 사용자 변경은 유지한다. 현재 목표는 활성/미완료다.

### Debug 기준선과 첫 개선

최종 Release 코드 그대로 Debug로 재컴파일한 기준선은 맑음 평균24.54FPS·최저0.5초 창20.95FPS, 강수 평균18.38FPS·최저12.93FPS였다. [600프레임](../../output/LakeVillagePerformance/Debug/baseline-frames.csv)·[실제 표시](../../output/LakeVillagePerformance/Debug/baseline-ui.csv)·[Debug EditMode74/74](../../output/LakeVillagePerformance/Debug/edit-baseline-74.json). 이전 목표의 조건 차이를 확인한 것이며 Debug 완료로 보지 않는다.

[강수 분해](../../output/LakeVillagePerformance/Debug/baseline-stages.txt)에서 실제tick44~49ms·수집약18ms·물리 결과 약10ms, FixedTick1.2~2.1ms를 측정했다. 마른 안정 상태는 검증/리비전 증가를 유지하며 동일 화학 계산을 생략하고, 입력/비/미완료 파손/연료0 전이는 기존 경로를 유지한다. 최소tickSeconds는 등록된 정의별 참조 수로 추적해 매 프레임 전체 대상을 재조회하지 않는다. 물리 연출은 등록/상태/피격 입력에서 깨우고 마지막 대기 표현이 끝나면 전수 순회도 멈춘다. Unity6 EntityId의 빈 hit 판정으로 빈 물리 결과의 Collider 오브젝트 조회를 피한다. 새 컴파일·회귀·성능 재측정 진행 중이다.

첫 Debug 확장 회귀는94건 중93건 통과·1건 실패했다. 새 검사에서 원래부터 연료가0인 비가연 프로필도 Spent가 된다고 잘못 기대했다. 기존 규칙은 initialFuel>0인 연료 소진만 Spent로 전이하고 원래 연료0 프로필은 Inactive를 유지한다. 제품 규칙을 바꾸지 않고, 소진 복원 상태와 원래 비연료 상태를 별개 반례로 수정했다. [최초 결과](../../output/LakeVillagePerformance/Debug/edit-first-94.json). 컴파일/검사 연결은 Editor 재시작 없이 복구됐다. 재검증 진행 중.

반례 기대값 수정 뒤 Debug EditMode94/94 통과(jobb4a98c50f69f42d1bf9734f5e08437c2). [결과](../../output/LakeVillagePerformance/Debug/edit-loop-94.json). 정의 참조 수의 중복 등록/로드/제거와 실제 tickSeconds 변경, 입력 없는 화학 상태의 리비전·비/열 재활성, 기존 피격/낙하/연소·차폐·표면 회귀를 포함한다. 반복 순회 개선 단계의 성능 재측정으로 진행한다.

### Debug 렌더러 인덱스 단계 — 검증 진행 중

첫 순회 개선 뒤 같은 조건의 Debug는 맑음 평균26.01FPS/최저0.5초 창21.62FPS, 비20.34FPS/최저14.77FPS로 미달이다. [프레임](../../output/LakeVillagePerformance/Debug/loop1-frames.csv)·[표시](../../output/LakeVillagePerformance/Debug/loop1-ui.csv). 프레임 제한 해제 실험도 개선 근거가 없어 원래 target60/vSync1로 복원했다.

Unity6 MeshRenderer 전용 ShaderUserValue의 상위 비트를 소유 표식으로 사용해 공유 버퍼 인덱스를 전달한다. 기존 MPB·다른 소유 값·부분 반응 슬롯이 있으면 기존 MPB 경로를 사용한다. 정상 식생에 수만 개의 MPB를 만들지 않아 SRP 배칭 참여를 회복하는 목적이며, 식생/논리 대상 수나 상태 전파 주기를 줄이지 않는다. [Debug EditMode96/96](../../output/LakeVillagePerformance/Debug/edit-native-96.json)은 GPU 버퍼 성장/재사용과 인덱스1·소유권·기존 회귀를 포함한다. 그 뒤 해제 시 뒤늦게 생긴 타 모듈 MPB를 지우지 않도록 보완했으므로 최신 소스 검사는 다시 필요하다.

전용 인덱스 GPU 비교 준비 과정에서 원본과 반응 슬롯 적용 범위·바람·SRP 미리보기 조명이 달라지는 도구 조건을 발견했다. 조건을 맞춘 비교는53재질에서 기존 상태 경로와 최대1/255 차이였으나, 미리보기의8재질은 상태 변화가 보이지 않아 전수 시각 인수로 쓰지 않는다. [중간 결과](../../output/LakeVillagePerformance/Debug/renderer-value-gpu-comparison.txt). 실제 월드 배칭/FPS·화면 확인이 남아 있어 Debug30FPS 달성으로 판정하지 않는다.

Test Runner 이후 빈 테스트 씬이 남고 MCP가 타임아웃하는 구간을 확인했다. 단일 Editor에서 정상 컴파일/재연결 후 LakeVillageWorld를 다시 열었으며 재시작·다른 Editor·Library/Temp 삭제는 하지 않았다. Play 초기화와 실제 활성 대상 수 확인 후 [Debug 계측기](../../Tools/ReactiveEnvironment/lake-debug-performance.csx)로 재측정한다. 화면 확인용 Computer Use는 Unity 접근이 허용되지 않아 사용할 수 없었다.

실제 월드에서 활성 식생15,284개 중 전용 표면15,255개·MPB가 있는 Renderer89개를 확인했다([바인딩](../../output/LakeVillagePerformance/Debug/native1-binding.txt)). 그러나 [맑음/비600프레임](../../output/LakeVillagePerformance/Debug/native1-frames.csv)·[표시](../../output/LakeVillagePerformance/Debug/native1-ui.csv)는30FPS 미달이다. MPB 감소를 성능 성공으로 보지 않는다. [Profiler 표본](../../output/LakeVillagePerformance/Debug/native1-profile.txt)에서 Editor 재그리기와 렌더 비용도 확인했으며, 이 표본은 Profiler ON이므로 FPS 인수 근거가 아니다.

[직접 단계 분해](../../output/LakeVillagePerformance/Debug/native1-stages.txt)는 rainCollect18~20ms·job/결과8.7~9.5ms·논리4.3ms다. 빈 hit 전체 순회를 Burst 작업으로 옮기고 실제 충돌 인덱스만 기존 관리 코드 판정에 넘긴다. 쿼리 수·차폐/자기 Collider·포화 판정은 유지한다. 소유권 보완과 이 변경을 포함한 [최신 Debug96/96](../../output/LakeVillagePerformance/Debug/edit-hit-scan-96.json)이 통과했다(jobb97fc08edb9a4223af4e778260ee394e). 이후 실제 성능 측정은 진행 중이다.

GPU 미리보기는 비교 조건을 분리하고 배경 alpha 대신 실제 픽셀 색 변화로 관측 범위를 판정했다. [최종 비교](../../output/LakeVillagePerformance/Debug/renderer-value-gpu-comparison.txt)는53개 비교·45개 유효 표면의 건조 보존/젖음/연소 변화 통과,8개는 원본/파생 모두 그 시점의 미리보기에서 출력이 없어 시각 미검증이다. 모든 쌍의 상태 경로 차이는 최대1/255다. SRP 미리보기 조명 차이 때문에 비교 동안만 배칭을 끄고 finally 원래 값으로 복원했다. 실제 월드 시각 인수와 해당8개를 통과로 승계하지 않는다.

### Debug 대기 프레임과 명령 준비 단계

같은 세션 [렌더러 인덱스 A/B](../../output/LakeVillagePerformance/Debug/ab1-frames.csv)에서 native 평균22.47FPS, 임시 MPB 경로15.31FPS였다. 전환 직후 배칭 재구성/셰이더 준비 스파이크를 포함하므로 정밀한 정상 상태 단독 성능 인수로 쓰지 않는다. 임시 MPB는 모두 해제했다. [그림자 실험](../../output/LakeVillagePerformance/Debug/shadow1-frames.csv)은80m/3 cascade와40m/2 cascade 모두30FPS 미달이어서 원래80m/3으로 복원했다. 품질 설정 변경을 최종 반영하지 않았다.

물리 씬 반복 조회를 한 tick당 한 번으로 줄이고, 논리 tick 사이의 보상/목록 정리 전수 순회를 대기 보상이 없을 때 생략했다. 등록/로드/피격/논리 변화는 보상 대기 상태를 깨우고, 실패한 보상은 다음 프레임에도 재시도한다. 파손 보상은0.001초 갱신에서도 지급하고 중복 지급하지 않는 회귀를 추가해 [Debug96/96](../../output/LakeVillagePerformance/Debug/edit-idle-gate-96.json)이 통과했다(jobaa287e323b1d467a959d19d639253b69).

이후 지붕 명령의 구조체 준비도 Burst 작업으로 옮겼다. 현재 위치·레이어·거리·backface·같은 물리 씬을 유지하고, 의존 작업을 모두 완료한 뒤 기존 차폐 판정을 사용한다. 예외 때도 마지막 예약 작업을 완료한 후 버퍼를 해제할 수 있게 한다. 이 후속 소스의 검사/실제 성능 측정은 진행 중이다. 현재 Editor Game View의 maximized=True를 확인했으므로 native/품질 실험을 원래 편집 레이아웃 성능 통과 근거로 간주하지 않는다. [화면](../../output/LakeVillagePerformance/Debug/native-world.png)은 캡처 직후 정지 비용을 포함하므로 화면 속6FPS도 정규 성능 표본으로 쓰지 않는다.

### 11월15일 후속 — 안정화·비교 조건과 강수 보호 판정

명령 준비까지 [Debug96/96](../../output/LakeVillagePerformance/Debug/edit-command-jobs-96.json) 통과(job6b72e3804f904dbfac34220fad0abcea). 이후 원래 편집 레이아웃으로 복원한 [native2](../../output/LakeVillagePerformance/Debug/native2-frames.csv)는 맑음23.88FPS, 비16.13FPS였으며 비에4.12초 프레임이 포함됐다. 설정 전환 직후의 배칭 재구성·셰이더 준비, 계속 흐르는 게임 시각에 따른 조명 변화가 일부 A/B에 섞이므로 다른 시각의 평균만으로 개선 완료를 판정하지 않는다.

임시 그림자/해상도/렌더 기능 실험은 모두 원복했다. SourceDepth/AfterOpaque/ScreenSpace 데칼 조합도 MSAA+렌더 레이어가 법선 prepass를 강제해 유의한 해결이 아니었다. [passes1](../../output/LakeVillagePerformance/Debug/passes1-frames.csv). 현재 활성 URP DecalProjector0개이며 프로젝트 추적 Assets에서 해당 스크립트 GUID 참조도 발견하지 못했지만, 최종 RendererData의 기능을 아직 끄지 않았다. GPU 가림 off A/B 평균30.77FPS 표본도 최저0.5초 창23.26FPS로 미달이다([occlusion1](../../output/LakeVillagePerformance/Debug/occlusion1-frames.csv)). native 렌더러 배칭·GPU 가림 설정의 전환 직후와 정상 상태를 구분한다.

[안정화 계측](../../Tools/ReactiveEnvironment/lake-debug-steady-performance.csx)은 최소600프레임·30초와 셰이더 준비 종료 뒤 측정을 시작한다. 측정 시작 후 스파이크는 제외하지 않는다. [steady-off-1](../../output/LakeVillagePerformance/Debug/steady-off-1-frames.csv)·[조건](../../output/LakeVillagePerformance/Debug/steady-off-1-metadata.txt)은 Debug/2560×1440/scale1/MSAA4/80m·3 cascade/기존 레이아웃, ShaderCompilingFrames0에서도 맑음25.11FPS·최저20.57, 비21.49FPS·최저16.48로 미달이다. 단일 프레임 샘플이나 일시적으로 높아진 평균을 완료로 승계하지 않는다. VSync0/무제한 실험도 개선이 작아 원래 cap60/vSync1로 복원했다.

[Profiler OFF 구간 기록](../../output/LakeVillagePerformance/Debug/recorders1-timings.csv)에서 RenderTotal 평균12.5~13.9ms, 환경 갱신 평균 맑음3.34ms/비7.69ms를 확인했다. 게임 카메라는1개이며 깊은 프로파일링 off, RenderGraph 캐시 on·검증 on이다. 18,472대상은 모두1개 강수 샘플을 쓴다. 관리 배열에 입력을 모아 한 번에 Native로 복사하고, 같은 tick의 현재 보호 볼륨 AABB/OBB 판정을 Strict Burst 작업으로 옮겼다. 보호 볼륨 안은 길이0 쿼리와 별도 보호 표식으로 처리하며, 외부/자기 Collider·포화 규칙은 유지한다. 여러 날씨 영역 및 비활성 볼륨의 range 재구성 회귀까지 [최신 Debug97/97](../../output/LakeVillagePerformance/Debug/edit-burst-shelter-97.json) 통과(job5afc5e311312470391bbb04ff2eef6d5). 실제 시간 절감과 최종30FPS는 재측정 중이다.

### 11월15일 위치 작업 수집 — 최신 소스 검증

강수 위치를 등록/해제 수명에 묶인 TransformAccessArray 읽기 전용 작업으로 수집한다. 매 tick 현재 부모 이동·회전·비균일 스케일을 반영하고, 원래 활성 GameObject 검사는 유지한다. 중복 Transform은 참조 수로 관리하며 swap 제거·슬롯 재사용·Dispose 후 재등록 때 이전 등록 표식이 새 소유자에 접근하지 않는다. 새 필드의 System.NonSerialized 누락으로 최초 컴파일이 실패했으므로 그 상태의98건 검사는 최신 소스 통과로 인정하지 않는다(94통과/4연출 실패). 수정·정상 컴파일 뒤 [Debug100/100](../../output/LakeVillagePerformance/Debug/edit-position-jobs-100.json) 통과(job30885886bbbf4dfab415b7528f2d8823).

[실제 월드 전수 대조](../../output/LakeVillagePerformance/Debug/burst-rain-parity-positions.txt)는18,472쿼리·보호587개, 직접 Transform 위치 차이0/최대오차0, 기존 직접 차폐 판정 차이0이었다. 수집18.476ms·완료7.163ms는 단일 분해 표본이며 FPS 인수가 아니다. 시계 고정 [가림 비교](../../output/LakeVillagePerformance/Debug/fixed-cull-1-metadata.txt)는 맑음24.50/가림off24.11/비off22.53FPS로 미달이며, 가림off를 최종 반영하지 않는다. [렌더 품질 비교](../../output/LakeVillagePerformance/Debug/fixed-quality-1-metadata.txt)의40m/2cascade+빈 데칼 패스off+AO Depth 조합은 평균35.73FPS였으나 표시 최저27FPS로 미달이다.35m/1cascade+scale0.85/MSAA2 조합에는925ms 스파이크가 있어 완료로 승계하지 않는다. 모든 임시 품질 값과 시계 속도는 복원했다. 정상 시계·원래8시 시작 시각에서 최종 성능 검증은 남는다.

### 최종 하한 보완 — 중복 생존 조회

30m/1cascade, AO SourceDepth, 빈 Decal 기능off를 LakeVillage 전용 RP/Renderer 에셋에 저장했다. 해상도2560×1440/scale1/MSAA4·GPU 가림on은 유지했다. 이 두 에셋은 Git 추적 밖이므로 git diff가 비었다는 사실로 변경 없음을 판단하지 않는다. GUID60140dc81ad77624695c9f527e515b6f/e490e390172dee24badc651d231f0299를 경로와 대조한 뒤 Undo group과 SaveAssetIfDirty로 저장했으며 씬 파일은 바꾸지 않았다.

[첫 저장본 재실행](../../output/LakeVillagePerformance/Debug/acceptance-1-summary.json)은 기존 표시 최저 맑음32/비31FPS였으나 모든 시작점의 누적500ms 구간을 대조하면 비29.03FPS로 미달이었다. 평균37.17/34.89FPS를 최소30FPS 완료로 보고하지 않는다. 비의 하한을 보완하기 위해 Transform 작업의 Vector4.w에 이번 tick의 유효 표식을 쓰고, 무효 Transform의 실행 생략에 대비해 앞선 작업으로 표식을 비운다. 작업 결과가 유효한 앵커는 중복 Unity 생존 조회를 하지 않되 GameObject.activeInHierarchy 검사는 유지한다. 차폐 생산자 생존은 같은 배치의 첫 조회에서 확인한다. 예약 예외에서도 마지막 의존 작업을 완료한다.

파괴된 앵커의 이전 위치 재사용 반례까지 [최신 Debug100/100](../../output/LakeVillagePerformance/Debug/edit-validity-jobs-100.json) 통과(job0d4b7c1e416f45118326bcb0d3c4728a). [최신 월드 전수 대조](../../output/LakeVillagePerformance/Debug/burst-rain-parity-validity.txt)에서18,472쿼리·보호587개·위치/차폐 차이0을 유지하며 수집은18.476→11.904ms, 완료7.121ms였다. 단일 분해 표본이며 최종FPS는 acceptance-2 재실행으로 판단한다. [GPU 비교](../../output/LakeVillagePerformance/Debug/renderer-value-gpu-comparison.txt)도 정리한 계측 도구로 재실행해45개 유효 표면 통과/8개 미관측을 유지했다.

## Debug 최종 판정 — acceptance-3

최신 코드의 acceptance-2도 맑음/비 평균37.65/34.75FPS였으나 임의500ms 최저29.96/27.86FPS로 실패했다([분포](../../output/LakeVillagePerformance/Debug/acceptance-2-summary.json)). 이 때문에 그림자30m와 SSAO on을 완료 설정으로 남기지 않았다. 최종 LakeVillage 전용 프로필은 **그림자80m/3cascade→25m/1cascade, SSAO off, 사용 중인 DecalProjector0인 Decal 패스off**다. SSAO 설정 SourceDepth는 비활성 기능의 저장값으로 남으며 별도 비용을 발생시키지 않는다. 접촉 음영과 원거리 그림자 품질을 낮춘 변경이며 화질 무손실 최적화라고 보고하지 않는다. RenderScale1/2560×1440/MSAA4·GPU 가림on·LOD 배율1·카메라·식생/논리 대상 수는 유지했다.

새 Play 실행의8시 시작, 최소600프레임·30초와 셰이더 준비 종료 후 조건별600프레임을 계측했다. 게임 시계 정상 배율1, Editor Debug, 기존 편집 레이아웃(maximized=false), Profiler/deep profiling off, debugger 미연결, cap60/vSync1이다. 날씨 미리보기만 맑음/강수1·폭풍1로 전환하고 완료 때 원래 미리보기/오버레이/레이아웃을 복원했다. 측정 시작 이후의 지연 프레임은 제외하지 않았다. [조건](../../output/LakeVillagePerformance/Debug/acceptance-3-metadata.txt)·[전체 프레임](../../output/LakeVillagePerformance/Debug/acceptance-3-frames.csv)·[기존 FPS 표시](../../output/LakeVillagePerformance/Debug/acceptance-3-ui.csv)·[분포](../../output/LakeVillagePerformance/Debug/acceptance-3-summary.json).

| 조건 | 평균FPS | 기존 표시 최저FPS | 모든 시작점의 누적500ms 최저FPS | p95/p99/최대 프레임ms |
|---|---:|---:|---:|---|
| 맑음 | 40.01 | 35 | 33.81 | 37.48 / 43.62 / 77.35 |
| 강수1·폭풍1 | 36.73 | 33 | 31.46 | 56.79 / 60.97 / 88.25 |

두 조건 모두 기존 표시와 더 엄격한 임의500ms 구간에서30FPS 이상으로 **대기 성능 기준 통과**다. 모든 개별 프레임이33.33ms 이내라는 판정은 아니다. 맑음84/600·비85/600프레임이33.33ms를 넘으며, 주기적 강수 tick의 순간 지연은 잔여 위험이다. 장시간/이동/화재/전투나 모든 시간대·기기의 성능을 이 짧은 대기 표본으로 승계하지 않는다. [최종 화면](../../output/LakeVillagePerformance/Debug/accepted-world.png)은 카메라 렌더 캡처이며 FPS 인수 근거가 아니다. 최종 미술 인수는 사용자 확인 대상이다.

최신 Debug 회귀100/100과18,472개 강수 쿼리의 위치/차폐 차이0을 최종 소스에 대한 검증으로 연결한다. GPU 비교45개 유효 표면 통과/8개 미관측을 유지한다. 종료는 단일 기존 Editor의 Edit Mode/Debug, LakeVillageWorld isDirty=false, 공유 GPU 표면 사용 수0·버퍼null·키워드off다. 씬273,555,090바이트/2026-11-14 19:26:29를 유지한다. Player/Addressables 빌드·명시적 저장 슬롯 쓰기·커밋·Editor 재시작은 하지 않았다.

**별도 기존 오류:** 종료 Console에서 RuntimePlacementLoader의 ActorDatabase 준비 시간 초과2건을 확인했다. [이전 월드 검수 기록](../../Assets/docs/design/world-art/lake-of-life-world/LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md)과 [물/강수 상태](water-rain-visibility-cost.md)에도 기록된 별도 초기화 문제이며 이번 작업에서 로더를 수정하지 않았다. Console 전체 오류0 또는 전체 NPC가 정상 배치된 월드의 성능 통과로 보고하지 않는다. 이 한계가 있는 현재 씬의 대기 측정 결과다.

### 사용자 직접 확인

LV-D01: 같은 Editor Debug·LakeVillageWorld에서 Play하고 초기화/셰이더 준비 후 같은 시작 위치에서 기존 FPS 표시가30 이상인지 확인한다. 필요 시 날씨 도구로 맑음/비를 각각 유지한다.30 미만 구간은 FPS·시각·날씨와 함께 전달한다.

LV-D02: 이동/전투/점화·소화·나무 낙하·실제 저장/재진입과 NPC 정상 배치를 확인한다. 이번 대기 계측으로 통과 처리하지 않으며 지역 배치 시간 초과를 먼저 구분한다. **사용자 테스트 대기**.

LV-D03:25m 그림자 끝 경계와 SSAO off의 접촉 음영,8개 미관측 표면·모든LOD/임포스터를 실제 화면에서 확인한다. 미술 인수는 **사용자 테스트 대기**. 회신: 항목ID / 통과·실패·미수행 / 실제 결과 / 화면·로그.

정리: 미참조 임시 품질/작은 메시/후보 계측 도구3개와 중간 미리보기PNG7개를 제거했다. 원본 측정 CSV/JSON/실패 로그·최종 화면·기준선/안정화/최종 인수 도구는 보존했다. 구매 원본·사용자 변경·메타는 정리하지 않았다.

최종 [준비표 검토](../../output/harness/20261115-015308-537a3f8b/review.md)와 입력 해시 일치를 확인했다. [종료 Console 원문](../../output/LakeVillagePerformance/Debug/final-console.json)을 보존했으며 문서/위키/설계3개 로컬 링크 누락0, 변경 코드의 git diff --check 오류0이다. 줄바꿈 변환 안내는 코드 오류로 세지 않는다.
