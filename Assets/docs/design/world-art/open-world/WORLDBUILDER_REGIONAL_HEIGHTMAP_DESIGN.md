# WorldBuilder 지역형 HeightMap 오픈월드 설계 (v1.1)

- 종류: 설계 · 분야: 월드·아트
- 출처: [전달 폴더](../../../../../docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/README_FIRST.md) (2026-11-16 작성 외부 인계본). 상세 계약 원문은 [IMPLEMENTATION_SPEC.md](../../../../../docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/IMPLEMENTATION_SPEC.md) 29장, 이 문서는 요약과 이 저장소 기준의 판단이다.
- 상태: **설계만 있음. Unity 구현·컴파일·Play Mode·Player 빌드 모두 미수행.** 전달본의 검증([DOCUMENT_VALIDATION_v1_1.json](../../../../../docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/DOCUMENT_VALIDATION_v1_1.json))은 문서·RAW·이미지 파일 수준에 한정되며 `unityTestRun=false`, `playerBuildRun=false`다.
- 기존 [연속 월드 통합](OPEN_WORLD_INTEGRATION_PLAN.md)·[정규 월드 제작 설계안](REGULAR_WORLD_PRODUCTION_DESIGN.html)과의 관계는 아래 「기존 문서와의 관계」를 본다.

## 1. 한 줄 요약

HeightMap(16-bit RAW)을 지표의 기초 데이터로만 쓰고, 동굴·건물·물·폭포·식생은 별도 저작 레이어로 만든다. **에디터 생성 → 수작업 튜닝 → 부분 재생성 → 검증 → Bake** 흐름이며 런타임은 검증된 Bake만 로드한다. v1.1은 여기에 5개 바이옴과 3개 거점(POI)으로 이루어진 **지역형 샘플 C**를 추가했다.

## 2. 확정 결정 D01~D12

| ID | 결정 | 경계 |
|---|---|---|
| D01 | 에디터 생성→수작업 튜닝→부분 재생성→검증→Bake | 런타임 전체 월드 생성 없음 |
| D02 | Unity Terrain + 별도 Mesh/Prefab 혼합 | 동굴·오버행·건물·수면을 HeightMap으로 표현하지 않음 |
| D03 | 저작 원본과 생성 결과 분리 | TerrainData·Scene은 파생물. 원본 HeightField·레이어 명령·배치 오버라이드가 원천 |
| D04 | 고정 좌표·결정론·안정적 ID | Seed·알고리즘 변경은 마이그레이션 대상 |
| D05 | 규칙 값은 데이터, 새 동작은 코드 | 새 나무·건물·바이옴은 데이터 추가만으로 |
| D06 | 16-bit RAW가 높이 교환 표준 | PNG는 참고용. v1 Unity 입력은 RAW |
| D07 | URP 물 별도 제작 | 수면 Mesh+URP 셰이더+WaterVolume+폭포 프리셋 |
| D08 | 청크별 Additive Scene + Addressables | 기존 씬 로더·세이브·서비스를 대체하지 않고 어댑터로 연결 |
| D09 | 실패 시 이전 Published Bake 보존 | 검증 실패·취소·도메인 리로드 중 교체 금지 |
| D10 | 샘플 2,048m, 512m Terrain 4×4, 타일 513², 전역 2049² | 같은 구조로 4,096m/8×8 확장 시험 |
| D11 | 자동화보다 플레이 동선 우선 | 입구·거점·랜드마크·주요 경로는 고정 가능 |
| D12 | 기존 에셋·타입·미커밋 작업 보존 | 스크립트 일괄 이동·GUID 재발급·패키지 무단 업그레이드 금지 |

v1 비범위: 복셀 굴착, 런타임 지형 파괴, 유체 시뮬레이션, 전체 수문 시뮬레이션, 자동 도시 설계, 3D 모델 자동 제작, 멀티플레이 동기화, 기존 전투·카메라·퀘스트 재작성.

## 3. 아키텍처와 경로

`WorldDefinition`(저작 원본) → `WorldBuildPipeline`(Dirty 계산·검증, 에디터) → `WorldBakeManifest`(Published) → `WorldChunkCoordinator`(런타임, 기존 로더·Addressables 어댑터).

- 제안 asmdef: `WorldBuilder.Core`(순수 수치, UnityEngine 무의존) / `Data` / `Runtime` / `Integration`(기존 서비스 어댑터) / `Editor`. 테스트는 `Assets/Tests/WorldBuilder/`.
- 저작 자산은 `Assets/WorldBuilder/Worlds/<WorldGuid>/`(Authoring·Source·BakeRevisions·Published), 재생성 가능한 캐시는 `Library/WorldBuilder/`.
- 런타임은 Authoring 그래프·Editor API를 로드하지 않는다.
- 기존 asmdef 이름과 다르면 어댑터로 연결하고 전체 구조를 옮기지 않는다.

## 4. 좌표·RAW 규약 (가장 사고가 잘 나는 부분)

| 항목 | 규칙 |
|---|---|
| 축 | +X 동, +Z 북. 배열은 `heights[z,x]`. UV `u`는 동, `v`는 북으로 증가 |
| RAW | unsigned 16-bit little-endian, 헤더 없음, 행 우선. **canonical 첫 행은 남쪽(z=0)** |
| PNG | 첫 행이 북쪽. RAW와 상하 반대이므로 decode 때 1회 뒤집는다 |
| 크기 | 높이 513² 와 Hole 512²를 혼동 금지. `SetHoles`의 true=표면, false=구멍 |
| 방향 검사 | `orientation_test_33_r16le.raw`(SW=0.1, SE=0.3, NW=0.6, NE=0.8)로 뒤집힘·전치를 먼저 확인 |

## 5. 두 샘플 B / C — 혼용 금지

| 항목 | B · 기술 fixture | C · 지역 prototype |
|---|---|---|
| 목적 | 2km 타일·경계·배치·임포트 기능 검증 | 7개 지역 토폴로지·배치·수계 통합 계획 |
| 입력 파일 | `core/samples/openworld_2049_r16le.raw` | `atlas/01_Regional_Heightmap_2049_R16LE_SouthFirst.raw` |
| 해상도 | 2049² | 2049² |
| XZ 크기 | 2,048m × 2,048m (확정) | **미확정**. 이동 속도·해상도 예산으로 선택 |
| 높이 | originY=−80m, 범위 480m (−43.7~335.9m 실측) | originY=0m, 범위 2,800m (`worldY=sample/65535×2800`) |
| 상태 | 파일 검증, Unity 미검증. **불변 fixture** | 파일 방향·내용 검사, Unity 미검증 |

- B는 P00~P09/T01~T26 기능 검증용으로 유지한다. C가 있다고 B 단계를 P10으로 대체·생략하지 않는다.
- C의 `01_World_Heightmap_2049_R16LE.raw`(첫 행 북)와 `01_World_Heightmap_2049_16bit.png`는 참조용이고 **Unity 통합에는 SouthFirst RAW만** 쓴다. 무결성은 `Regional_Atlas_Manifest.json`의 SHA-256으로 확인한다.
- C의 XZ가 미확정이므로 2,049 샘플 간격(예: 2km면 1m, 8km면 약 4m)과 Terrain 해상도·LOD 예산을 함께 정해야 한다. 2,800m 수직 범위는 B(480m)의 약 5.8배라 같은 해상도에서 양자화 간격이 약 0.043m/단계로 커진다. 큰 월드는 타일별 고해상도 디테일 저작이 필요하다는 전달본의 주의와 같다.
- 컨셉 PNG 7장은 높이 데이터와 포토매칭되지 않은 AI 참조 아트다. Terrain 입력으로 쓰지 않고 실제 Unity 결과물처럼 보고하지 않는다.

## 6. 지역 계획 (C)

앵커 `(u,v)`와 영향 반경은 **수정 가능한 후보값**이고 실제 Region Mask가 아니다. 경사·수위·도로·성문·부두의 실사용 가능성을 검사한 뒤 수동 Lock하고 재생성 때 보존한다.

| ID | 종류 | 앵커 (u,v) | 반경 (u,v) | 핵심 요건 |
|---|---|---|---|---|
| dense_forest 우거진 숲 | Biome | 0.14, 0.47 | 0.16, 0.26 | 주요 길 2방향, 수관이 길·전투 시야를 과하게 가리지 않음 |
| autumn_forest 가을 숲 | Biome | 0.28, 0.76 | 0.19, 0.16 | 색만 다른 숲이 아니라 능선·조망·탐색 구조가 구별됨. 색은 높이가 아닌 식생·재질 |
| arid_badlands 메마른 지역 | Biome | 0.88, 0.48 | 0.15, 0.23 | 메사·협곡. 아치·오버행은 Mesh. 건천≠수계. 주 경로에 막다른 협곡 없음 |
| castle_plains 평야·성 | Biome + Capital POI | 0.49, 0.46 | 0.23, 0.21 | 성 부지 별도 평탄화. 성문과 필수 거점의 실제 Nav 연결 |
| high_alpine 고산 | Biome | 0.73, 0.87 | 0.29, 0.14 | 접근 가능한 목표 루트 1개 이상, 수직벽 검사, 수목한계선 아래만 수목 |
| riverside_village 강가 마을 | Settlement POI | 0.38, 0.54 | 0.055, 0.055 | River 종속. 설계 홍수 고도 이상, 교량 end-to-end 연결 |
| lakeside_village 호수 마을 | Settlement POI | 0.72, 0.21 | 0.065, 0.055 | Lake 종속. 설계 수위 이상, 수면 결함·떠 있는 부두·잠긴 건물 없음 |

- 강가·호수 마을은 바이옴이 아니라 WaterBody·SettlementFootprint에 종속된 POI다.
- 5개 바이옴 가중치의 합은 경계에서도 1이며 hard seam이 없어야 한다.
- 수계는 고산 수원 → 주강 → 강가 마을 → 호수/호수 마을의 유향 그래프다. 이미지의 어두운 선을 수문 그래프로 간주하지 않는다.
- 필수 루프: 성 ↔ 강가 마을 ↔ 호수 마을 ↔ 성.

## 7. 생성 우선순위

Regions & POI → BaseHeight → Hydrology → Reservations → Road/PlotGrade → TerrainOperations → Structures/CaveMesh/WaterMesh → Vegetation → Navigation/Streaming Validation → Stage → Publish.

Pin/Delete/Move/Lock은 재생성 후에도 유지한다. Region 경계·Anchor 이동은 영향받는 청크·수계·Nav 의존까지 Dirty 전파한다. Dirty 재생성과 Full 재생성의 논리 결과는 같아야 한다.

## 8. 구현 단계와 게이트

| 단계 | 내용 | 게이트 |
|---|---|---|
| P00 | 저장소 조사, INTEGRATION_AUDIT·PLAN | 연결 경로·타입·패키지 버전 기록 |
| P01 | HeightField·RAW codec·좌표·타일 split/join·샘플 importer | T01~04, 4×4 Terrain 생성, 모서리 방향 화면 확인 |
| P02 | TerrainOperation·Sculpt·RAW 왕복·Undo·Dirty DAG | T05~10 |
| P03 | 후보 격자·안정 ID·공간 해시·필터·Override | T06/07/11/12 |
| P04 | Region/POI/Reservation·평탄화·도로·교량 | T13 |
| P05 | 수계·URP 수면·WaterVolume·폭포 | T15/16 |
| P06 | 모듈 동굴·Holes·바이옴·식생·Exclusion | T14/24/26 |
| P07 | Nav 어댑터·검증기·Bake·Publish/Rollback·편집기 | T17/20/21 |
| P08 | Addressables 코디네이터·세이브·POI·물 연동 | T18/19/22/23, **실제 Player 빌드** |
| P09 | 전체 테스트·2km/4km 스트레스·프로파일 | T01~26 |
| P10-A~D | C 자료 계약 → 월드 계획 → 지역 자동 배치 → 실제 플레이 검증 | T27~T35 |

T27~T35: C 이미지가 Terrain RAW로 오인되지 않음 / RAW 방향·SHA / 바이옴 가중치 합=1·경계 컷 없음 / 성·강가·호수 지지·경사·침수·예약 / 수계 역경사·누수 없음 / 성-강가-호수 루프 Nav / Dirty·Full 동등과 고정 POI 불변 / Override 보존과 충돌 보고 / **T35 실제 TPS 시점 경관·주 경로 (사람 승인)**.

## 9. 이 저장소에서 적용할 때의 판단

- 전달본은 `Assets/02.Scripts/WorldBuilder/` 구조를 제안하지만 이 저장소의 asmdef 경계·서비스 계약(AGENTS.md, [아키텍처](../../../../.claude/rules/architecture.md))이 우선이다. 구현 전 P00에서 실제 asmdef·씬 로더·세이브·미니맵 서비스와 대조해 어댑터 지점을 정한다.
- 전달본이 인용한 Unity 6000.3.21f1 / URP 17.3.0 / Addressables 2.9.1은 외부 기록이다. 실제 `ProjectVersion.txt`·`manifest.json`로 재확인한다.
- 현재 월드(생명의 호수 등)는 이미 Terrain·NavMesh·Addressables로 구성돼 있다. WorldBuilder v1은 기존 월드를 교체하지 않으며, **신규 월드 제작 도구**인지 **기존 월드 편집 도구**인지는 아직 정해지지 않았다. 구현 착수 전 사용자 결정이 필요한 항목이다.
- 전달본은 `activateOnLoad=false` 상시 대기 스트리밍 금지, handle 소유권, Ready 전 이동 차단을 요구한다. 기존 Addressables 키는 보존한다.
- 빌드·Player 검증은 별도 사용자 지시 없이 실행하지 않는다(AGENTS.md 3항). P08의 Player 검증도 지시를 받은 뒤 한다.

## 10. 미결정·위험

1. C의 월드 XZ 크기(2km/4km/8km 등)와 Terrain 해상도·LOD 예산.
2. 기존 월드와의 관계(신규 월드 vs 기존 월드 편집 도구).
3. C의 수문 그래프는 수문 검증이 안 된 개념이다. 강 프로파일을 새로 저작해야 한다.
4. 컨셉 이미지와 HeightMap이 정합되지 않아 지역 외형 인수는 이미지 비교가 아니라 Unity 화면과 사용자 확인으로 한다.
5. 범위가 매우 크다(P00~P10, T01~T35). 단계별로 위키·상태 문서를 갱신하며 수직 단위로 진행한다.

## 11. 기존 문서와의 관계

| 문서 | 관계 |
|---|---|
| [OPEN_WORLD_INTEGRATION_PLAN.md](OPEN_WORLD_INTEGRATION_PLAN.md) | 시나리오 지역을 연속 월드로 잇는 현행 통합 계획. 본 설계는 그것을 대체하지 않는다 |
| [REGULAR_WORLD_PRODUCTION_DESIGN.html](REGULAR_WORLD_PRODUCTION_DESIGN.html) | 독립 정규 월드 제작 설계. 본 설계와 범위가 겹칠 수 있어 착수 전 대조한다 |

## 12. 전달 자료 목록

원본은 `docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff/`에 그대로 두었다(수정·이동 없음).

- `README_FIRST.md`, `AGENT_TASK_PROMPT.txt`, `IMPLEMENTATION_SPEC.md`(1,300행), `OpenWorld_HeightMap_Design_v1_1.html`(이미지 내장)
- `core/`: B 샘플(2049² RAW, 16개 513² 타일, 방향 검사 RAW, `sample_manifest.json`, `validation_report.json`), `Raw16HeightmapCodec.cs` 참고 코드, 샘플 생성 스크립트
- `atlas/`: C 샘플(RAW 2종·PNG), 지역 컨셉 이미지 7종, 앵커 도식, `Regional_Atlas_Manifest.json`
