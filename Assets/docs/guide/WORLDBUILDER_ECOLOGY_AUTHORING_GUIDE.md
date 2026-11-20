# WorldBuilder P06 동굴·바이옴·식생 저작

설계와 검증 상태는 [WorldBuilder 설계](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md)와 [P06 작업 상태](../../../docs/work-status/open-world/WORLDBUILDER.md#p06-ecology)에서 확인한다.

## 월드 연결

WorldBuilder 지형 저작 창에서 미리보기 씬의 `WorldDefinition`을 선택한다. **P06 · 동굴·바이옴·식생**을 열고 해당 월드의 `WorldEcology` SO를 연결한다. `worldGuid`는 Definition과 같아야 하며 `ecologyGuid`는 다른 저작 소유자와 구분한다. 적용 뒤에는 해당 두 값을 유지한다. Sculpt나 RAW 편집을 열어 둔 경우 먼저 캡처한다.

[P06 검증 씬](../../WorldBuilder/Worlds/f606f606f606f606f606f606f6060906/P06Validation.unity)을 단독으로 열면 [Definition](../../10.Datas/WorldBuilder/P06Validation/Definition.asset)과 [Ecology](../../10.Datas/WorldBuilder/P06Validation/Ecology.asset)를 연결해 확인할 수 있다. 서쪽 입구는 X476/Z303, 동쪽 출구는 X548/Z303, 강 위 교량은 X512/Z250이다.

## 동굴

- 카탈로그는 입구, 직선, 곡선, 분기, 방, 끝막이 모듈의 프리팹을 참조한다. 프리팹 루트의 `WorldCaveModule`에는 바닥용 정적 Collider를 연결한다. 모델의 내부 면과 벽, 지붕, 충돌 형상을 맞춘다. 단위 스케일에서 로컬 +Y가 위이고 yaw는 +Y 축 회전이다.
- 각 소켓은 로컬 바닥 위치와 바깥 방향 yaw, 규격, 통과 폭과 높이를 가진다. 외부 소켓은 입구에 하나 둔다. 나머지 소켓은 반대 방향의 같은 규격 소켓에 연결한다. 세계 좌표의 모듈 위치와 허용 회전을 저작하고 연결 결과를 검증한다.
- `caveGuid`, `moduleId`, `surfaceId`, 소켓 ID는 소문자 N 형식 GUID로 저장한다. 포털 ID는 월드, 동굴, 모듈, 소켓으로부터 파생된다. 동굴 내부 소품은 `surfaceId`를 지정하며 해당 모듈의 바닥 Collider를 조회한다.
- 입구의 지표 교차가 P02 HoleCut이 된다. 다른 동굴 및 수동 cut과 합집합으로 적용된다. 이동 또는 삭제는 선택한 동굴 ID로 실행하며 Undo를 지원한다. 지표/구조물/동선 충돌은 메시지에 표시된 위치를 기준으로 조사한다.
- P05 물리 볼륨은 동굴의 고도 제한 건조 체적을 조회한다. 같은 XZ에 있는 지상 수역의 SO와 ID는 유지된다.

## 바이옴과 식생

점유·보호 및 Hole이 있는 위치에서는 기본 레이어를 사용하고 식생을 차단한다. 나머지는 고도, 경사, 실제 저작 수계 경계까지의 거리와 습도 범위를 검사한다. 마스크가 비어 있거나 마스크 바깥에 있는 바이옴은 해당 셀에서 제외한다. 월드 전체를 대상으로 삼으려면 월드 사각형을 마스크로 넣는다. 여러 바이옴이 유효하면 priority가 큰 순서, 동률은 ID의 사전순으로 결정된다. 후보가 없으면 `defaultLayer`에 가중치 1을 준다. 레이어 인덱스는 Definition의 표에 저장하고 새 레이어를 뒤에 추가한다. 모든 타일에는 같은 레이어 배열을 쓰고 공유 경계는 같은 전역 좌표에서 평가한다.

- `TerrainTree`는 장식용 프리팹을 Terrain prototype에 등록하고 안정 ID를 미리보기 메타데이터에 기록한다.
- `Detail`은 메시 프리팹을 Terrain Detail에 등록한다. **detailResolution**과 표시 거리는 생태 SO에서 조절한다.
- `Prefab`은 P03 카탈로그의 프리팹과 배치 규칙을 연결한다. 상호작용 나무, 관목, 암석에 사용한다. 같은 리소스의 식생 출력 경로는 한 가지로 지정한다.
- 각 경로는 최종 Terrain과 배치 예약을 확인한다. 식생 footprint가 Hole, 도로, 교대, 구조물, 필수 동선, 수역 또는 보호 영역에 닿으면 거절한다. 고정 배치가 충돌하면 후보 ID와 사유를 읽고 원본을 조정한다.
- Terrain은 기존 URP 미리보기 재질을 사용한다. 검증에 쓰는 TerrainLayer 복제본의 광택은 0이다.

생태 저작 후 씬을 저장한다. 다음 작업에서 P02의 전역 재평가나 P03 배치 갱신을 실행하면 연결된 식생을 다시 평가한다. 동굴 지지와 피복, 기존 cut 범위가 맞지 않으면 트랜잭션을 중단한다.

## 사용자 테스트 명세

Editor Test Runner의 `T14_PlayerMotorTraversesSavedPortalsAndAllModuleShapes`로 KCC 충돌 검사를 재현할 수 있다. 저장 씬 양쪽 포털과 임시로 연결한 분기·곡선·방·끝막이를 실제 모터로 검증한다. 본편 입력과 카메라 품질은 아래 조작 검사에서 확인한다.

- **WB-P06-01**. 실제 플레이어가 초기화된 검증 세션에서 서쪽 입구와 동쪽 출구를 양방향으로 통과한다. 경사 진입 중 바닥 낙하, 걸림이나 천장 통과가 발생하면 실패다.
- **WB-P06-02**. 검증 씬의 동굴을 저작 창에서 이동한 뒤 Undo와 Redo를 실행한다. 다른 수동 Hole은 유지되고 대상 동굴의 입구, 충돌, 지표 재질과 식생이 함께 돌아오는지 확인한다.
- **WB-P06-03**. 지상에서 포털 주변을 돌아보고 통로 내부에서 양쪽 출구를 확인한다. 지표 경계, rock collar와 두 방향의 면을 보고 지상 수계와 식생이 자연스럽게 보이는지 판단한다.

