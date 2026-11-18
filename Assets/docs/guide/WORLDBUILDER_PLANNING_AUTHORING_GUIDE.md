# WorldBuilder 계획·구조물 저작 (P04)

정본 설계는 [지역 설계](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md), 진행·실행 근거는 [작업 상태](../../../docs/work-status/open-world/WORLDBUILDER.md)에서 확인한다. 이 단계는 계획 입력·건물 부지·도로·교량 연결을 다룬다. 실제 수계 생성과 게임 월드 스트리밍 연결은 후속 단계다.

## 검증 씬 열기

1. 현재 씬을 저장하고 `Assets/WorldBuilder/Worlds/d404d404d404d404d404d404d404d404/P04Validation.unity`를 연다.
2. 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**에서 `Assets/10.Datas/WorldBuilder/P04Validation/Definition.asset`을 연결한다. 메뉴 표기는 기존 P02/P03 창을 따른다.
3. `P04 · 계획·건물·도로·교량`을 펼친다. 계획 SO를 Inspector에서 열어 Region 다각형·POI·건물/도로/교량 프리셋을 확인한다.
4. 전체 월드는 2,048m·2049²·originY0·verticalRange700m다. 첫 확인 구역은 x256~768/z0~512의 512m 정사각형이다. Scene View에서 (512,12,250)을 중심으로 본다.

기존 B/C/P03 씬에 이 fixture를 생성하거나 덮어쓰지 않는다. 재현용 API `WorldPlanningValidationFixture.CreateValidationScene()`은 이미 존재하는 폴더를 거절한다. 프리미티브는 아트 인수 대상이 아니다.

## 데이터와 적용

- `WorldPlan`: 고정 GUID를 가진 Region·POI·예약 다각형·건물·도로·교량 원본. GUID는 적용 후 바꾸지 않는다. 빈 새 계획만 월드 GUID를 초기화할 수 있다.
- `StructurePlanningPreset`: catalog GUID, footprint, pivot, 지지점, 출입 소켓, 평균/최대 경사·절토/성토 제한. 확정 고도를 부지에 반영하며 지지점과 실제 프리팹 바닥을 맞춘다.
- `RoadPlanningPreset`: 폭·배치 제외 폭·falloff·경사·절토/성토·곡률·UV 반복·재질. A* 후보를 곡선으로 만들고 금지 영역을 벗어나면 접선을 줄여 다시 검사한다. 조건을 만족하지 못하면 적용 전체를 거절한다.
- `BridgePlanningPreset`: 수평 상판 프리팹 span/폭/두께·계획 수위 여유고·접근부 제한. 양 교대 소켓 높이는 같아야 한다. 물의 시뮬레이션이나 생성 결과로 가장하지 않으며 수계 ID·다각형·계획 수위를 명시 입력한다.

`계획 검증·Dirty/Full 재생성`은 이전 계획 소유 패치/예약만 교체한다. Sculpt·RAW 세션이 활성 상태거나 미캡처 지형/계층/Spline 편집이 있으면 적용을 막는다. sourceRevision은 기존 Base·좌표·Profile 의미를 유지한다. P02 사용자 연산과 P03 Manual/Pin/Delete 원본은 유지한다. 실패하면 작업 전체를 복원한다.

POI 이동은 GUID를 입력하고 `POI 앵커 읽기` → XYZ 변경 → `POI 앵커 이동·고도 확정` 순서다. Lock된 앵커도 명시적 이동은 가능하며 자동 재생성으로 위치를 바꾸지 않는다. `부지 고도 제안 읽기 (미적용)`은 계획 패치를 제외한 부지 내부 중앙 고도를 XYZ 필드에 제안한다. 여러 건물이 연결된 POI는 건물별 offset을 뺀 제안의 중앙값을 사용한다. SO/지형은 아직 바뀌지 않으며 `이동·고도 확정`으로 적용한다.

도로 계층의 실제 `SplineContainer`를 Scene View에서 편집한 뒤 `선택 도로 Spline 캡처/Lock`을 누른다. 위치/접선을 원본 SO로 캡처한다. roll/pitch banking은 지원하지 않아 거절한다. 다른 도로의 미캡처 편집을 함께 버리지 않는다. 잠긴 도로는 POI 이동 시 임의로 새 경로에 연결되지 않고 소켓 불일치를 보고한다. 파생 Mesh/Collider/재질 또는 구조물 자식 변경은 먼저 Undo하고 원본 프리셋을 수정한다.

`계획 구조물·도로 전체 삭제 (Undo 가능)`는 현재 계획의 구조물·도로·교량과 소유 패치/예약을 함께 제거한다. 다른 수동 저작은 보존한다. 삭제와 이동은 Unity Undo/Redo 대상이다. 파생 패치/메시는 content hash 경로에 보관하며 Undo가 참조할 수 있으므로 임의 삭제하지 않는다.

## 접근 검증과 경계

도로는 전역 곡선을 먼저 계산하고 타일 경계에서 같은 정점·노멀·접선·누적 UV를 복제한다. 교량 상판에서는 도로 메시를 중복 생성하지 않는다. 도로는 보호 다각형·건물·기존 Manual/Pin footprint를 피하고, 교대의 Hole과 지지 높이를 확인한다.

검증 씬의 `WorldPlanningNavigationPreview`는 별도 NavMesh 데이터만 등록한다. `WorldBridgeAccessGate`는 양쪽 소켓 준비 상태를 모두 받아야 장벽/장애물을 연다. 아직 실제 청크 로더에 연결하지 않았다. Play Mode에서 이 씬의 교량이 처음 닫혀 있는 것은 기본 상태다. 현재 자동 테스트는 준비 신호를 명시 전달해128m 합성 fixture와 저장된512m 구역 모두에서 두 건물 내부 출입구 사이의 실제 NavMeshAgent 통과를 검사한다. 교량 중앙 횡단 시 Collider 지지도 raycast로 확인한다. KCC/TPS 조작 품질 인수와 구분한다. 검증 NavMesh는 기준 씬용이며 임의 계획 편집 후 자동으로 최신 Bake가 된다고 가정하지 않는다. 정식 게임 Navigation/청크 로더 연결은 P08 범위다.

## 사용자 테스트 명세

- WB-P04-01: 512m 구역에서 건물 바닥의 뜸/매몰·출입구·도로와 교량 접속을 직접 확인한다. 지붕은 카메라 가림 규칙 Auto, 도로/교량은 Solid 분류이며 CameraOccluder 컴포넌트를 추가하지 않는다.
- WB-P04-02: 첫 POI `00000000000000000000000000000003`의 앵커를 옮기고 이전 부지가 복원되며 새 부지/도로가 생성되는지 확인한다. Undo/Redo에서 지형·씬·SO가 함께 돌아와야 한다.
- WB-P04-03: 도로 Spline을 편집하고 일반 재생성의 차단, 선택 캡처/Lock, 재생성 후 보존을 확인한다. 금지 영역을 침범하면 기존 결과를 유지해야 한다.
- WB-P04-04: 계획 전체 삭제와 Undo/Redo를 실행하고 P03 Pin이 유지되는지 확인한다.
- WB-P04-05: 계획 수위/교량 span/접근 경사를 불가능한 값으로 바꾸어 오류 메시지와 기존 지형/씬 보존을 확인한다. 원본 프리셋 편집은 별도 Undo 대상이므로 수정값을 직접 복구한다.

실제 브러시·외부 RAW UX, 활성 세션의 실제 도메인 리로드 복원, 중간 디스크 실패, 실제 KCC와 미술 품질은 기존 사용자 테스트 대기 항목으로 유지한다.



교량 프리팹은 로컬 +Z 방향의 길이, 중앙 XZ pivot, 상판 Y=0, root 단위 배율을 사용한다. Catalog 반치수의 두 배가 span/상판 폭과 일치하고 pivotHeight는0이어야 한다. 메타데이터 일치 오차는 고정1mm이며 소켓 spanTolerance를 크게 해 이 검사를 우회할 수 없다. 다른 pivot/축의 구매 원본은 수정하지 말고 해당 계약을 만족하는 별도 어댑터 프리팹을 저작한다. 실제 모델·Collider와 선언한 span/두께/지지점의 일치는 프리셋 저작자가 확인해야 하며, 자동 통과 근거는 별도 검증 프리미티브에 한정한다.

주동선이 특정 교량을 반드시 지나야 하면 `WorldPlan.roads[].requiredBridgeGuid`에 그 교량 ID를 지정한다. 비우면 A* 비용에 따라 수계를 우회할 수 있다. 지정한 교량이 소실되거나 잠긴 Spline이 양쪽 교대 소켓을 지나지 않으면 오류로 막는다. 자동 경로는 출입구→교대A→교대B→출입구 순서로 제안하며, 교량 내부 접선은 상판 축에 정렬한다. 별도 검증 씬은 이 필수 연결을 사용한다.
