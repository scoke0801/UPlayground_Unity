# WorldBuilder 수계 저작 · P05

[설계 정본](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md), [실행 근거·현재 판정](../../../docs/work-status/open-world/WORLDBUILDER.md#p05-착수--2026-11-18). P06 동굴·바이옴·식생, Published/Bake, 게임 로더는 이 단계에 포함하지 않는다.

## 검증 씬과 원본

- 전용 씬: `Assets/WorldBuilder/Worlds/e505e505e505e505e505e505e505e505/P05Validation.unity`.
- 원본: `Assets/10.Datas/WorldBuilder/P05Validation/Definition.asset`, `WaterSystem.asset`, `RiverPreset.asset`, `UpperRiverPreset.asset`, `WaterfallPreset.asset`.
- 전체 규격은 2,048m·2049²·originY0·verticalRange700m, 512m Terrain16개다. 첫 확인 구역은 X256~768/Z0~512다. 기존 B/C/P03/P04 씬에 시연 배치를 넣지 않는다.
- 재현 API는 `WorldWaterValidationFixture.CreateValidationScene()`이다. 기존 폴더를 거절하므로 이미 생성된 씬을 다시 생성하지 말고 연다. MCP 응답이 늦어도 명령을 반복하지 않는다.

P04의 두 건물·필수 교량 횡단을 새 월드에서 재현한다. 계획 ID9가 실제 주강 ID9에 연결된다. 원래 P04 자산 GUID를 바꾸거나 새 수역에 근접 연결하지 않는다. 검증 NavMesh와 실제 게임 로더/스트리밍·정식 Bake는 구분한다.

## 저작 원본과 물리 계약

`WaterSystemDefinition`은 안정 systemGuid/worldGuid, 수역 목록, 제외 체적, 폭포 edge를 가진다. `WaterBodyPreset`은 폭·요구 깊이·shore 폭·강둑 폭/여유고·falloff·절토/성토·흐름·UV·탐색 한도·표현 리소스의 원본이다. `WaterfallPreset`은 최소 낙차6m·하부 pool 여유·소켓 오차·입자/소리/거리/중복 제한을 가진다.

지형 바닥은 stage20 파생 연산, 수면은 청크별 Mesh/URP Shader, 게임플레이는 `WaterVolume`이다. `sourceRevision`은 기존 Base·좌표·Profile 의미를 유지하며 수계 버전으로 바꾸지 않는다. 수역 이동·삭제는 이번 시스템 소유 연산/예약만 교체한다. P04 Plot/RoadGrade·교대와 P02 Sculpt/RAW·보호 명령은 유지한다.

`IWaterQuery.TrySample(position)`은 유한 좌표, bounds broadphase, 실제 polygon/river strip 포함, 지형 바닥 이상·조회 상한, 제외 체적 검사를 통과할 때만 성공한다. surfaceY는 파동 없는 물리 높이, depth는 **수면에서 실제 native Terrain 바닥까지**의 양수 거리, flow는 world m/s, bodyId는 waterGuid다. 발의 잠김 깊이는 surfaceY−feetY로 별도 계산한다. 시각 파동·알파·Renderer bounds는 물리 깊이에 쓰지 않는다.

겹침은 priority 내림차순 → 실제 polygon 면적 오름차순 → waterGuid ordinal 순이다. `TryRaycastDown`도 동일 수면 함수와 제외 체적을 사용한다. 기존 `FootstepWaterSurface`의 선택적 어댑터로 수영/발소리/수중 조회를 연결한다. 기존 월드의 어댑터 없는 수면 동작은 유지한다. 폭포 curtain은 일반 수면/수영 체적을 만들지 않는다. P05 probe 시연은 플레이어 수영 품질 인수가 아니다.

## 강·호수·바다

강 route는 상류→하류 순서의 확정 world XYZ다. 노드 수위는 역전할 수 없다. route 구간의 호 길이에 선형인 수위와 miter strip의 삼각형 보간을 Mesh·query가 공유한다. 청크 평면에서 위치/수위/UV/flow를 함께 절단한다. 급회전·self-intersection·월드 밖 경로는 Conflict다. 강 양 끝 cap은 수심 계산에서 제외해 입출구에 얕은 턱을 만들지 않는다.

자동 제안은 수원·하구·필수 경유점을 유한 비용장에서 연결한다. 탐색 한도 초과/막힘은 위치가 있는 Conflict다. locked route는 그대로 반환한다. 침식 시뮬레이션을 사용하지 않는다. Native 높이장 보간에도 요구 깊이/강둑 여유고가 남도록 조형 plateau에 격자 여유를 둔다. 최종 높이장 전체 구간을 다시 검사하며 안전한 단면을 만들 수 없으면 거절한다.

호수는 단순 polygon과 일정 worldY 수위다. 자기 교차/퇴화/월드 밖 입력을 거절하고 전체 호안의 낮은 외측 연결을 검사한다. 월드의 모든 낮은 지형을 같은 호수에 묶지 않는다. 바다는 명시 polygon이 월드 경계에 닿아야 한다. 수면은 오목 polygon도 경계 밖 삼각형 없이 분할한다.

## 변경·실패·Undo

툴 런처 **월드 / 맵 → WorldBuilder 지형 저작 → P05 · 수계·수면·폭포**에서 Definition과 수계 SO를 연결한다. waterGuid를 입력하고 수위·이동량을 지정해 전용 버튼으로 변경한다. 강 제안·확정/Lock은 명시적인 적용 명령이다. 자동 재생성으로 확정 경로를 바꾸지 않는다.

코드에서는 `WorldWaterAuthoring.Edit(definition, source, mutation)` 안에서 **source의 수계 데이터만** 변경한다. Inspector에서 미리 고친 preset은 별도의 Unity Undo 대상이다. 연결된 preset이나 외부 자산 변경을 이 콜백의 복원 범위로 간주하지 않는다. 수면/Volume/FX 파생 계층을 직접 편집하면 재생성을 막으므로 Undo하거나 소유 계층 밖으로 옮긴다.

검증 → 불변 패치 저장 → P02 소유 연산 교체/native Terrain/배치 → 수면·체적·폭포 계층 교체 → 저장 순서이며 하나의 Undo 그룹이다. 실패하면 SO·연산·native Terrain·계층을 복원한다. 파생 패치/메시와 Undo용 캐시를 임의 삭제하지 않는다. 수계 삭제로 P04 연결 ID가 소실되면 Conflict다. 교량을 임의 이동·확장하거나 도로 grade로 강바닥을 메우지 않는다.

수동 Sculpt가 요구 깊이·호안·P04 지지를 깨면 오류를 반환한다. 미캡처 Sculpt/활성 RAW 세션은 재생성으로 덮지 않는다. Dirty 영역은 이전/새 패치와 falloff의 합집합이며 P02 지역 지형 평가·P03 배치 갱신을 사용한다. 수면/체적은 전역 경로를 기준으로 결정론적으로 다시 생성한다. 이 경로의 부분 갱신 성능 최적화는 별도다.

## URP와 폭포 리소스

P05 수면 Shader는 flow UV·scroll normal·depth fade·shore foam을 쓴다. Depth Texture가 필요하고 Opaque Texture는 사용하지 않는다. P05 카메라만 `requiresDepthOption=On`, `requiresColorOption=Off`로 둔다. 기존 게임 Renderer·품질·재질을 전역 수정하지 않는다. 굴절/반사는 v1 필수 항목이 아니다.

폭포는 확정 upper lip→lower landing와 유향 상/하류 수역 ID로 만든 별도 curtain/ribbon, 양쪽 접합 foam, 낙수 particle·mist·실제 AudioClip emitter다. 거리별 방출률/음량을 줄이고 가까운 emitter는 거리·GUID로 정렬해 중복 제한한다. 입자 bounds에는 생성 폭, 수명 동안의 속도/중력 이동, 최대 크기를 포함한다.

검증 리소스는 기존 ToonWaterfall shader와 FootstepWaterSplash 재질, 구매 물 입수 효과음을 재사용한다. 입수 효과음 반복은 **기술 검증용 임시 음원**이며 폭포 ambience 청감 인수와 구분한다. 별도 폭포 loop 음원과 미술 품질은 사용자 확인 대상이다.

## 사용자 테스트

P05 씬만 단독으로 열고 Play한다. 기존 저장 데이터나 게임 초기화가 필요하지 않다. 결과 형식은 `ID / 통과·실패·미수행 / 실제 결과 / 로그·화면`이다.

| ID | 절차 | 기대 결과/실패 기준 |
|---|---|---|
| WB-P05-01 | 주강과 교량을 가까이/멀리, X512 타일 양쪽에서 확인 | 수면 높이/UV/흐름의 끊김·뒤집힘·분홍 재질이 없고 상판 아래 여유가 유지됨 |
| WB-P05-02 | 물리 조회 probe를 수면 아래·밖·제외 체적에 이동 | bodyId·surfaceY·depth·flow가 일관되고 수역 밖/바닥 아래/제외 체적에서는 실패 |
| WB-P05-03 | 폭포 상/하단과 뒷면, 화면 가장자리를 근/원거리로 확인 | curtain 접합·foam·mist·낙수가 보이고 외곽의 조기 culling이 없음. 거리별 소리·방출 감쇠 |
| WB-P05-04 | Edit Mode에서 호수 수위와 lower landing Y를 함께 변경, 이동·삭제 및 Undo/Redo | SO·native Terrain·수면/Volume/FX가 함께 돌아오고 다른 수계·Pin·Sculpt·P04 연결 유지 |
| WB-P05-05 | 불가능한 수위·교량 수역 ID 소실·강바닥 상승 Sculpt를 시도 | 위치/원인 있는 Conflict, 사용자 수정 자동 덮기 없음, 이전 정상 결과 보존 |

실제 KCC 수영·장시간 입력·저작 UX·미술/청감 인수, 활성 Sculpt 세션의 실제 도메인 리로드 복원, 중간 디스크 실패와 Player/Addressables 검증은 자동 수치 검사로 대체하지 않는다.
