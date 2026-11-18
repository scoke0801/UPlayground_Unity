# WorldBuilder P03 공통 배치 저작

- 설계: [지역형 HeightMap](../design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md), 전달 명세 10·11장.
- 상태와 실행 근거: [WORLDBUILDER](../../../docs/work-status/open-world/WORLDBUILDER.md).
- 지형 수정·Sculpt·RAW 세션은 [P02 지형 지침](WORLDBUILDER_TERRAIN_AUTHORING_GUIDE.md)을 따른다.

## 열기

1. `Assets/WorldBuilder/Worlds/d303d303d303d303d303d303d303d303/P03Validation.unity`를 연다. B/C 검증 씬과 분리된 합성 월드다.
2. 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**을 열고 저작 정의에 `Assets/10.Datas/WorldBuilder/P03Validation/Definition.asset`을 연결한다.
3. **P03 · 공통 배치**를 펼친다. 전체 월드는 2,048m·2049²·originY=0·700m이며 첫 사용 영역은 X=256~768, Z=0~512의 512m 구역이다. X=512 타일 경계를 가로지른다.
4. 미리보기 소품은 기본 Cube 프리팹이다. 얇은 선은 타일 경계 표시이며 배치 엔진의 점유 객체가 아니다. 큰 고정 소품이 경계 중앙을 예약한다.

## 데이터와 평가

`WorldDefinition` Inspector의 Catalog·Rules·Reservations가 원본이다. Catalog·Rule SO와 수치는 `Assets/10.Datas/WorldBuilder/`에서 관리한다. Catalog GUID는 실제 프리팹 자산 GUID와 같아야 한다. 프리팹 root의 배율은 단위 배율로 두고, 지오메트리 크기는 자식에 저작한다. footprint는 root pivot을 중심으로 하는 X/Z 반치수, pivotHeight는 지지면부터 root까지의 높이다.

P03은 Terrain 표면, 높이·경사, footprint 전체 Hole 교차, 지지면 높이 차, 사각 예약/제외 영역, 회전 OBB 점유·최소 간격을 평가한다. spacing은 두 규칙 중 큰 값을 사용하고 확장 OBB로 보수적으로 검사한다. 수직 배치와 Y축 회전을 지원한다. 비균일 배율은 Catalog에서 허용한 프리팹만 가능하다. Terrain 이외 surfaceId는 `SurfaceMismatch`로 거절한다. 아직 없는 수계·바이옴·중요 구조물 Physics/출입구 검사를 통과한 것으로 취급하지 않는다.

후보는 타일별 난수 상태가 아니라 전역 셀·slot 키에서 생성된다. ID는 버전1 canonical 바이너리 키 SHA-256의 앞128bit, sourceIdentity는 그 키의 Base64 원문이다. seed는 별도 난수 키에만 들어간다. seed 변경에는 epoch 증가가 필수이며 Base·sourceRevision·Sculpt 패치를 초기화하지 않는다. 규칙 GUID를 바꾸면 기존 후보와 연결되지 않는다.

Full/Dirty 버튼은 같은 전역 평가를 수행한다. 이웃 충돌이 연쇄로 바뀔 수 있으므로 현재 Dirty 최적화는 전역 재평가로 제한한다. 타일 소유권은 저장 단위이며 충돌 검색을 자르지 않는다. 규칙당 후보 한도는 1,000,000이다. 넓은 영역에 촘촘한 격자를 지정하면 명시적으로 거절한다.

출력·거절 이유별 개수와 후보/Override ID를 확인한다. 창의 후보 상세는 앞256건, 전체 진단은 `WorldPlacementAuthoring.LastResult.Diagnostics`에 있다. Mandatory 최소 개수 미달은 오류, 장식 밀도 미달은 경고다. 오류가 있으면 이전 씬·Definition을 유지한다.

## 수동 의도 기록

- **Pin:** 씬의 소품을 선택하고 **선택 객체 ID 읽기 → Pin**. 기존 ID·lineage·월드 XYZ를 유지한다. rule 제거/epoch 변경 후에도 남으며 원래 대상 소실은 orphan으로 보고한다.
- **Delete:** ID를 읽고 **Delete**. 같은 epoch의 재생성에서 돌아오지 않는다. 자동 대상의 epoch가 달라지면 오래된 tombstone은 orphan이며 새 후보에 전이하지 않는다.
- **Move / Rotate / Scale:** **대상 변환 읽기** 후 위치·Y 회전·배율을 수정하고 적용한다. Terrain 재접지를 켜면 XZ+offset으로 접지하고, 끄면 절대 XYZ를 유지한다. 지지면 허용 오차를 넘으면 오류다.
- **ReplacePrefab:** Catalog GUID를 넣고 적용한다. ID를 유지하고 새 footprint·배율·pivot으로 다시 검사한다. 충돌은 오류이며 이전 결과를 복원한다.
- **Manual Placement:** Catalog GUID·위치·배율·접지 정책을 지정하고 추가한다. 사용자 UUID와 별도 sourceIdentity를 보관하며 자동 후보보다 먼저 공간을 예약한다.
- **ExcludeRegion:** 사각 영역을 지정해 추가한다. seed와 무관하게 공간을 비운다. 데이터의 ruleTag를 지정하면 해당 태그에만 적용한다.
- 기록을 취소하려면 창 아래의 해당 Override 삭제 버튼을 쓴다. 작업은 Undo/Redo에 기록한다. 서로 다른 의도가 충돌하면 진단을 확인하고 해당 의도를 명시적으로 수정한다.

Scene View에서 직접 바꾸거나 삭제한 미캡처 객체는 재생성이 덮어쓰지 않는다. 해당 Scene 변경을 Undo한 뒤 창에서 의도를 기록한다. 가까운 새 후보에 자동으로 연결하지 않는다. Pin/Manual끼리 겹치거나 지형 변경으로 절대 anchor가 뜨면 오류로 기존 결과를 유지한다.

## 사용자 테스트 명세

키보드/마우스, 위 검증 씬·Definition을 사용한다. 원본 B/C 씬을 저장 대상으로 쓰지 않는다. 결과는 `ID / 통과·실패·미수행 / 실제 결과 / 캡처 또는 로그`로 전달한다.

| ID | 순서 | 기대 결과·실패 기준 |
|---|---|---|
| WB-P03-01 | X512 경계 양쪽 소품을 근접·상공에서 보고 재생성3회 | 위치/ID가 같고 경계의 과밀·고정 footprint 침범 없음 |
| WB-P03-02 | 자동 소품 하나 Delete → 재생성 → Undo/Redo | 같은 epoch에서 재등장하지 않음. Undo로 복원, Redo로 다시 삭제 |
| WB-P03-03 | 다른 소품 Pin → 재생성 → seed1/epoch1 적용 | Pin 위치·ID 유지, 기존 자동 대상의 orphan 보고. 무관한 새 소품에 Override 전이 없음 |
| WB-P03-04 | 대상 변환 읽기 → 위치/Y회전/배율 적용 → Undo/Redo | 허용된 지지/간격이면 반영. 고정 소품과 겹치면 전체 작업 복원 |
| WB-P03-05 | Terrain anchor 수동 소품 추가·ExcludeRegion·ReplacePrefab | 각 의도 보존, 새 프리팹 footprint 검증. GUID 누락은 오류 |
| WB-P03-06 | 작은 높이 명령 → 배치 갱신 → Full 재생성 | 논리 결과 동일, 기존 Sculpt 패치·원본·Pin 보존. Pin을 뜨게 하는 변경은 오류로 전체 복원 |

실패 시 ID·seed/epoch·규칙 GUID·조작 순서·진단 코드·예상/실제 화면을 남긴다. 조작성과 시각 품질은 자동 수치 검사와 별도 인수다. 실제 도메인 리로드 중 세션 복원·중간 디스크 실패·P02 외부 편집기 UX는 기존 미검증 상태를 유지한다.
