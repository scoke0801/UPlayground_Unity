# LakeVillageWorld 북동 폭포

## 폭포 반영 미니맵 재촬영 — 2026-11-15

후속 수정: 사용자 원문 “물이 안보이잖아. 물에 강제적으로 색상을 입혀서 촬영 후 원복하는 형태로 재촬영해”. 앞선 호수 한 곳만 어두운 색으로 촬영한 결과는 가독성 실패다. 현재 이미지는 활성 물 표면7개(호수·상류/폭포·개울·연안 등)를 밝은 파랑 불투명 Unlit 재질로 일시 교체해 다시 촬영한 버전이다. 각 Renderer의 원래 전체 재질 배열은 finally에서 복원했다. [복원 감사](../../output/lake-waterfall/minimap/forced-color-audit.txt):7/7 복원 True. 전체와 폭포 원본 영역에서 파란 물길을 확인했다. Sprite 연결·좌표 유지, sceneDirty=False·QualitySettings 해시 전후 동일. 재촬영 직전 이미지는 `minimap/forced-color-backup/`에 보존했다. 아래 최초 촬영 기록은 실패 이력이며 현재 결과로 승계하지 않는다.

- 현재 LakeVillageWorld를 기존 MinimapCaptureUtility로3600×4200 재촬영해 `Assets/10.Datas/World/LakeVillage/MapBackground.png`를 갱신했다. [전체](../../output/lake-waterfall/minimap/after-preview.png), [폭포 원본 픽셀](../../output/lake-waterfall/minimap/waterfall-detail.png).
- SceneContext.WorldMapConfig가 참조하는 `World/LakeVillage/MinimapConfig.asset`의 backgroundSprite 연결을 확인·저장했다. 이미지 GUID `369896ec80b47a84cb8b18f6cf06b64d`, 중심(1200,700), 범위2400×2800m 유지. Standalone BC7·원본 해상도 유지.
- 직교 촬영 때 호수만 기존 도구 방식의 임시 지도 재질로 변경하고 원래 LakeVisualV2/LakeWater.mat로 복원했다. 폭포는 현재 배치의 실제 렌더다. 임시 카메라 잔존0·씬 dirty=False, QualitySettings SHA-256 전후 동일.
- 최초 호출 응답은 시간 초과였으나 이미지 생성과 후속 Sprite 임포트·설정·복원을 별도 확인했다. 중복 촬영하지 않았다.
- 이전 PNG/meta/Config는 `output/lake-waterfall/minimap/backup/`에 보존. 저작 기록: `Tools/OpenWorld/capture-lake-waterfall-minimap.csx`. 실제 플레이 HUD 가독성은 사용자 확인 대기. 빌드·런타임 코드 변경 없음.

## R4 표시 결함·개방형 공간 — 반영·저장

사용자 원문: “표시부분 고쳐. 보물상자는 중앙에 위치하게. 고쳐.” 추가 지시: “입구가 명확한 동굴일 필요도 없어. 그냥 넓게 뚫려있는게 더 자연스럽고 앞에 길도 인위적으로 배치할 필요도 없다.”

- BUG-LW-04 / S3: 상류·낙수의 겹친 물판과 폭 차이, 사각 외벽·진입 경사 노출. 기존 R3 화면 확인을 미술 통과로 승계하지 않는다.
- 상류 끝 행과 낙수를 공유 정점으로 합친 단일 메시/재질로 변경했다. 겹친 접근판과 중복 스킨을 제거하고 상단 페이드를 연결했다. 상류 수영 쿼리는 기존 메시를 유지한다.
- 공간을 사면 안으로 3m 옮기고 전면을 넓혔다. 별도 `Cliff` 외벽과 `Ramp` 진입로는 비활성화했다. 내부 바닥 자체가 하상으로 내려가며 상부 암반과 양옆은 기존 지형 높이에 연결된다. 새 장식 돌은 없다.
- 상자는 내부 중심선 X=0(직렬화 오차 약0.00005m)에 정렬하고 바닥에 접지했다. 기존 ID `8ce0e1fbf3384abca2ad4d0996d433b7`, 보상·상호작용 계약은 유지했다.
- 현재 정본: R4 `WideFall_R4.asset`, `LakeWaterfallCave_Interior_R4.asset`, `NaturalOverhang.asset`; Terrain/NavMesh는 R3 자산이다. 상부 암반은 기존 Roof 이름 규칙을 사용한다. 중간 스크립트 `fix-lake-waterfall-r4.csx`를 최종 결과에 재실행하지 않는다.
- 근접 확인에서 Terrain 구멍 경계가 가느다란 면·빈 틈으로 남는 것을 추가 발견했다. 개방부를 넓히고 상부와 측면을 기존 사면에 봉합했다. 진단용 Terrain 복제는 알파맵 외형이 달라 원래 R3 참조로 복구했으며 사용하지 않는 R4 Terrain 사본은 진단 자료로 보존했다. MCP 삭제 안전 검사에 막혀 삭제는 실행되지 않았다.
- [최종 물리·경로](../../output/lake-waterfall/r4/physics.txt): 중앙 진입→상자 앞 캡슐27표본 막힘0·바닥 누락0, NavMesh 경로 Complete. ID15,751개 모두 고유. 이는 실제 KCC 조작을 대신하지 않는다.
- [위쪽](../../output/lake-waterfall/r4/after-overhead.png), [측면](../../output/lake-waterfall/r4/after-side.png), [열린 공간 근접](../../output/lake-waterfall/r4/open-recess.png), [중앙 상자](../../output/lake-waterfall/r4/cave-chest.png). 고정 카메라 확인이며 실제 이동 시 외형·발견·입수/귀환은 사용자 테스트 대기다.
- 최종 저장 `saved=True`, `dirty=False`. 같은 씬 재로드 확인도 수행했으며 이후 추가 보정을 다시 저장했다. 런타임 코드·보상 데이터 변경, Player/Addressables 빌드, 커밋 없음. 기존 R3 계약 테스트16/16은 과거 근거이며 이번에 재실행한 것으로 보고하지 않는다.
- R4 직전 씬·R3 에셋·셰이더 백업과 진단/최종 캡처 보존. 다른 나무 작업 완료·Editor idle 확인 후 진행했다. 사용자 미술 인수와 실제 상자 획득·저장 왕복은 대기다.

## R3 낙수 수정·숨은 동굴 — 반영·검증

- 사용자 원문: “물줄기가 떨어질 때 버그 처럼 보임.개선해줘. 그리고 폭포 뒤에 동굴을 만들고 보물상자를 배치해줘. 이건 게임을 탐험할 맛이 나게하는 중요한 요소야”.
- BUG-LW-02 / S3 / 사용자 측면 사진에서 낙수면이 꺾인 얇은 선처럼 보임. 기존 궤적 `s∝√k, 높이∝k²`가 수평 거리 기준 4차 곡선을 만들어 낙구의 유입 접선·중력 궤적과 맞지 않았다. 측면 두께·난류 표현도 부족했다. R2 시각 인수 실패이며 기존 정면 캡처를 해결 근거로 승계하지 않는다.
- [숨은 동굴 설계](../../Assets/docs/design/world-art/environment/LAKE_WATERFALL_CAVE_DESIGN.md)에 따라 실제 동굴·마른 바닥·진입/귀환·일회 보상 계약을 함께 구성한다. 현재 조사는 편집 대기 상태에서 진행했으며 다른 MCP가 같은 씬을 쓰면 대기한다.

### 반영

- 약18.6m 폭 유지. 낙구 유입 경사와 접선을 잇는 중력 궤적(`전진6.2m, 낙하17m`)으로 바꿨다. 얇은 단일 판 대신 최대13cm 두께의 앞·뒤 면과 측면을 만들고, 흐름을 따라 낙수 비말3곳을 더했다. 분리된 안개 덩어리2개는 제거했다. 전용 `R3/Fall.asset` 8,918정점·17,640삼각형, 원본 셰이더는 보존한다.
- `AuthoredEnvironment/LakeWaterfallCave`: 입구 `(1142.56,78.15,1026.14)`, 너비약8.7m·깊이9.5m·천장높이약4.25m. 기존 지형 사본 `R3/Terrain.asset`에 국소 구멍260셀을 내고 실제 내벽/바닥/천장·앞 암벽·상부 지면 덮개·진입 경사에 MeshCollider를 연결했다. 상류는 덮개 위로 유지한다. 기존 `DeepLake` 경계는 수정하지 않았다.
- 신규 장식 돌 무더기 없이 암굴 자체를 구성했다. 출입구 윤곽을 비대칭으로 보정하고, 내부 메시 이음새와 뒤쪽 UV 소용돌이를 수정했다. 재질은 기존 `PoorProp_Stone_Field` 텍스처를 참조하는 전용 복사본, 원본 미수정.
- 상자1개: `(1147.01,79.09,1032.41)`, 기존 `Dng_Chest.prefab`, `DropItemActor`, `DungeonTreasure.asset` 재사용. `SceneEntityId=8ce0e1fbf3384abca2ad4d0996d433b7`, 보상은 기존 **고급 회복물약(itemId200003)3개**다. 퀘스트 잠금 없이 발견 순서와 무관하게 획득하며, 획득 후 상자도 기존 배치 드롭 정책에 따라 소진·숨김 처리된다.
- 이름 규칙 정본 `CameraOccluderNameRules.asset`에 동굴의 실물 천장·내벽·암벽(Auto+실내 가드), 진입 경사(Solid)를 연결했다. 씬에 CameraOccluder를 심지 않았다. 규칙 에셋 작업 전 사본은 `r3/backup/`에 보존했다.
- `R3/Navigation.asset` 재생성·씬 연결·저장. 672타일,11,953,140바이트, 동굴 입구→안쪽 경로 Complete. 낙수 메시에는 NavMesh 제외를 지정했고, 기존 수집 방식은 PhysicsColliders다.

### 검증과 남은 인수

- [장면 감사](../../output/lake-waterfall/r3/scene-audit.json): 기존15,750개 ID 유지·상자1개 추가, 총15,751개 모두 고유. 상자 ID1개, 누락 스크립트0·재질0, 동굴 메시 유한값 통과.
- [상호작용·물리 보조 검사](../../output/lake-waterfall/r3/interaction-audit.json): 실제 보상 데이터로 격리 Ledger 첫 획득성공/두 번째거절·총3개 지급·소비 데이터 Import후 재지급거절. **실 플레이어 인벤토리나 저장 슬롯을 수정하지 않았다.** 상자 위치의 수면 없음으로 마른 공간 확인.
- 입구 앞→상자 앞 캐릭터 캡슐26표본(반경.35m) 막힘0·바닥 누락0. 천장 상향 레이3/3이 내벽 천장에 맞았다. 실제 KCC·키보드/패드 조작 검증의 대체는 아니다.
- [EditMode16/16](../../output/lake-waterfall/r3/editmode-tests.json): GroundDropLedgerTests8개와 CameraOccluderNameRulesTests8개 모두 통과, 건너뜀0. 저장 복원·중복 방지 **공유 계약 검사**이며 실제 새 상자의 사용자 조작 검증은 남는다.
- [측면](../../output/lake-waterfall/r3/after-side.png), [상자](../../output/lake-waterfall/r3/cave-chest.png), [동굴 안에서 출구](../../output/lake-waterfall/r3/cave-exit.png). AI 고정 카메라 확인이며 최종 미술·발견의 즐거움은 사용자 확인 대상이다.
- 최종 저장 응답은 `saved=true, isDirty=false`, Terrain/NavMesh 모두 R3 전용 경로·672타일로 확인했다. 마지막 재촬영·Console 조회의 MCP 응답은 시간 초과였으므로 최종 Console0을 주장하지 않는다. 앞서 확보한 촬영과 검사 근거를 보존했다.
- 런타임 C# 변경·Player/Addressables 빌드·커밋 없음. 다른 MCP의 나무 코드 변경·검사는 보존하며 같은 Unity 검사 실행이 감지되면 쓰기를 대기했다.
- **사용자 테스트 대기:** 물줄기가 측면/이동 시 버그처럼 보이지 않는지, 호수에서 동굴을 발견·진입·상자 획득·물가로 귀환, 실제 저장/로드·지역 왕복 후 중복 지급 방지, 게임패드/카메라·밤/비·성능.

확인 절차: 호수 북동 폭포로 접근 → 물보라 옆에서 뒤쪽으로 들어감 → 낮은 경사를 올라 마른 동굴로 진입 → 상자 앞에서 기존 상호작용 입력 → 고급 회복물약3개 획득 → 같은 길로 귀환 → 저장/로드·지역 재진입 후 상자 재지급 없음 확인. 결과는 `R3-낙수/발견/진입/보상/귀환/저장 | 통과·실패·미수행 | 증상·위치`로 남긴다.

제작 기록은 `fix-lake-waterfall-r3.csx`, `build-lake-waterfall-cave-r3.csx`이며 이후 UV·비말·재질을 직접 보정한 저장 씬과 R3 자산이 정본이다. 전체 생성기나 제작 기록을 재실행하지 않는다. R3 직전 씬·카메라 규칙 백업과 제작 원본/파생 자산/meta·검사 JSON·캡처를 보존한다.

## R2 재저작 — 현재 저장본

사용자 추가 지시: “폭포는 좀 더 넓게, 측면에서 봐도 자연스럽게 / 인위적인 돌 등의 배치는 줄인다.” 이후 상류 사진으로 “물줄기가 이렇게 시작되는게 어색해”라고 재제보했다.

- **반영·저장 / 사용자 미술·플레이 확인 대기.** 다른 나무 작업의 완료와 Unity 편집 대기를 확인한 뒤 수행했다. 이전 R1 결과와 실패 이력은 아래에 보존한다.
- 폭을 **18.6m**로 넓히고 사면을 따라 길게 뜨던 물판을 실제 하상·낙구·짧은 낙하면·소로 교체했다. 지형 원본은 `WorldThemesABC/Terrain.asset`, 현재 씬 전용 사본은 `Assets/10.Datas/World/OpenWorld/LakeWaterfall/R2/Terrain.asset`이다.
- 상류는 높은 지형 안에서 닫히는 작은 소로 바꿨다. 물가를 실제 지형과 만나는 폭으로 계산하고 출구 하상을 낮춰 물길을 연결했다. 시작 메시 첫 행은 지형 안에 묻힌다. 폭 표본을 완화해 사진의 가로 절단면·날카로운 돌출을 줄였다.
- 처음 추가한 장식 바위 10개는 제거했다. 기존 `Lake_CliffBank_12_R2`·`15_R2`, `ShoreRock_77`·`80`은 낮추고 지형 최저점 기준으로 묻었다. 중복 `Lake_CliffBank_13_R2`는 개별 비활성화했다. 물길을 가로지르던 `SlopeCanopy_12`는 ID를 유지해 옆 숲 `(1124,87.58,1054)`에 재안착했다.
- 기존 호안·선반 7개 일괄 비활성화 시도는 자동 승인 검토가 범위·복원 위험을 이유로 거절했다. 해당 일괄 작업은 실행하지 않았고 추가 제거를 우회하지 않았다. 이미 조정한 바위의 접지만 보정했다.
- 삭제된 바위가 수면 안에 남아 보이던 현상은 수면 숨김 진단에서 사라졌다. R2 전용 `WideFall.shader`가 낙수 색을 프레임버퍼에 직접 알파 합성하도록 바꿔 배경 재투영을 없앴다. `Headwater.mat`는 상류의 굴절을 끄고 불투명도를 고정했다. 공용 셰이더·구매 원본은 수정하지 않았다.
- 하류 소는 호수 **77m** 수위로 연결했다. 별도 소 시각 메시를 겹쳐 그리지 않고 기존 호수 수면을 사용하며, 소 메시에는 수영 쿼리만 남겼다. 낙구 물살 컴포넌트를 연결했다.

### R2 검증 근거

- [메시·지형 전수 감사](../../output/lake-waterfall/r2/final-audit.json): 당시 높이 변경 4,401표본·최대15.57712m, 허용 구간 밖 변경0. 배치 ID15,750개 보존. 누락 스크립트0·재질0, 메시 유한값·셰이더 진단 통과. 이후 상류 메시 폭 완화·재질 접촉 페이드와 허용 구간 안의 물가186표본을 보정했다. [최종 국소 대조·저장 확인](../../output/lake-waterfall/r2/final-save.json)은 변경4,400표본·최대15.57712m다. Console 오류 조회0.
- NavMesh 전용 사본 `R2/Navigation.asset` 재생성·씬 연결, 디스크 역직렬화 **672타일** 확인. 최종 물가 보정 후 다시 갱신·저장해 **11,951,220바이트**이며 씬 `isDirty=false`·지형 dirty=false를 재확인했다. 주변 경로4쌍의 전후 상태 동일: 북쪽 산책길1쌍과 동쪽길1쌍 Complete, 기존 다른 북쪽 연결2쌍 Partial 유지. **전부 연결 통과가 아니다.** [작업 완료 표식](../../output/lake-waterfall/r2/navigation.txt).
- [정면](../../output/lake-waterfall/r2/after-front.png), [측면](../../output/lake-waterfall/r2/after-side.png), [상류 시작](../../output/lake-waterfall/r2/source-head.png), [위쪽](../../output/lake-waterfall/r2/after-overhead.png), [조망](../../output/lake-waterfall/r2/after-vista.png). 고정 카메라와 Scene View로 확인했으며 사용자 인수를 대신하지 않는다.
- [최신 저장본 차이](../../output/lake-waterfall/r2/saved-scene-audit.json): R2 직전 저장본과 비교했다. 별도 **고사목3개**(`marked-0018_Terrain_DeadTree`, `empty-space-0031_Terrain_DeadTree`, `marked-0024_Terrain_DeadTree`)의 반응 샘플·Collider·ReactiveEnvironmentObject도 변경돼 보존 검사기는 실패로 남겼다. 이 세 고사목은 폭포 범위 밖이며 저작 스크립트로 수정하지 않았다. 기존 미커밋 환경 코드의 자동 갱신 가능성이 있으나 원인을 확정하지 않았고 임의 원복하지 않았다. 나무 피격 구성 전부 불변이라고 보고하지 않는다.
- 보존: R2 직전 최신 씬 약356MB, 기존 R1 미저장 내용 포함 씬 백업, 원본 Terrain/NavMesh, 파생 자산·각 meta·검수 캡처·검사 JSON. 실패 진단 캡처는 원인 근거라 유지했다. Player·Addressables 빌드와 커밋 없음.
- **미수행:** 실제 조작으로 상류→낙구→입수→육지 복귀, 밤·비·이동 카메라·성능, 미술 인수. 사용자 확인은 아래 LW-01~03을 현재 R2 저장본에서 수행한다. 낙구 `(1144,93.5,1029)`, 상류 소 약 `(1157,101.1,1055)`.

저작 기록: `rebuild-lake-waterfall-r2.csx`·`refine-lake-waterfall-r2-source.csx`는 중간 제작 기록이다. 이후 직접 보정된 저장 씬·R2 메시/재질/지형이 정본이며 반복 적용하지 않는다. 촬영은 `capture-lake-waterfall-r2.csx`, 저장본 비교는 `audit-lake-waterfall-r2.mjs`다.

## 사용자 재제보 — 실패, 수정 대기

원문: “주변 돌이 너무 인위적임. [Image #1] 그리고 이런 폭포가 어디있니?”

- BUG-LW-01 / S3 / 실패: 사용자 Scene View 측면 사진에서 상류 물판이 하상 없이 공중에 떠 있고 사면 위로 비스듬히 이어진다. 최초 저작의 지형과 독립된 수면 프로필이 원인이다. 앞선 정면 캡처 확인을 미술 통과로 볼 수 없다. 빈도·캐릭터·입력·슬롯은 모름, 지형 저작 결함이라 조사에 불필요하다. 회귀: 이번 신규 배치에서 발생.
- FEEL-LW-01 / 실패: 돌의 세로 비율과 반복 배치가 인위적이라는 사용자 의견. 사진에서도 고립된 선돌 형태가 반복된다. 환경 구성과 모델 선택·변형을 다시 검토한다.
- 수정 방향: 최초 추가 암반은 걷어내고, 상류 물이 담기는 실제 하상·호안·낙구와 짧은 낙하면·하류 소를 함께 조형한다. 낙구 외의 물판은 지형에서 뜨지 않아야 한다. 세로로 늘인 돌 대신 낮은 암반 덩어리가 사면에 묻혀 이어지도록 한다. 지형 변경 시 접지와 NavMesh 재검증이 필요하다.
- 사용자 지시: 다른 MCP와 Unity 작업이 겹치면 대기. 나무 파괴 작업의 같은 씬 사용 기록을 확인했으며 현재 Unity 쓰기는 대기한다. 아래 저장 결과는 **수정 전 실패본의 이력**이다.

- 요청: RiverBasinWorld처럼 호수 맵의 적절한 곳에 폭포 배치.
- 지침: [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md), [월드 저작](../../Tools/OpenWorld/README.md).
- 조사 완료: 호수 수면 77m, 북동 사면 약 74~101m. 북쪽 산책길은 후보지 서쪽 약 70m에 있다. 기존 진행 개체가 없는 사면을 선택했다.
- 방향: 북동 숲에서 바위 사이로 솟는 물길 → 낙수 → 호수. 지형·NavMesh·진행 배치는 유지하며 호안 장식 바위 세 곳은 물길을 열도록 조정했다.
- 출처: RiverBasinWorld VisualR3의 RiverFall 셰이더·전용 복제 재질과 물보라 인스턴스, Polyart DreamscapeEastLands RiverRock 프리팹. 구매 원본은 수정하지 않는다.
- 보존: `output/lake-waterfall/backup/LakeVillageWorld.before.unity`는 작업 전 **미저장 편집 내용 포함** 씬 사본이다. 임의 삭제하지 않는다.
- 기준 캡처: `output/lake-waterfall/before-overview.png`, `north-front.png`, `north-high.png`. 조감도 첫 촬영은 깊이/불투명 텍스처 없는 임시 카메라라 물 표현 인수 근거로 쓰지 않는다. 이후 근경은 URP 깊이/불투명 텍스처를 켜서 촬영했다.
- 현재 판정: **배치 저장 반영 / 사용자 테스트 대기**. MCP `get_active`에서 `LakeVillageWorld`, `isDirty=false`를 확인한 저장본에 폭포 루트와 참조가 존재한다. 이후 MCP 연결이 종료돼 최종 충돌체 추가·추가 감사 명령은 성공으로 판정하지 않았다. Unity 재시작·두 번째 Editor 실행은 하지 않았다.

## 저장된 결과

- 씬: `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`.
- 루트: `AuthoredEnvironment/LakeWaterfall_Northeast`, 좌표 `(1147, 0, 1033)`. 낙구 주변 `(1147, 96.8, 1033)`, 입수 지점 `(1133, 77, 1010)`.
- 전용 메시·수면 쿼리·재질: `Assets/10.Datas/World/OpenWorld/LakeWaterfall/`의 `Fall.asset`, `SpringQuery.asset`, `Fall.mat`와 각 `.meta`.
- 낙수 2,775정점·5,280삼각형, 신규 호안 암반 10개, 입수 물보라 3개와 미스트 1개(자식 파티클 포함 7개 시스템), 기존 `WaterfallLink`로 호수와 상류 수면을 연결했다. 낙수면 전체를 수영 수면으로 등록하지 않았다.
- 낙구 약 96.8m에서 호수 77m로 이어진다. 마지막 면을 좁히고 수면 아래로 내려 직사각형 앞치마를 없앴다. 전용 재질을 호수보다 먼저 그려 호수 수면과 입수 물보라가 마지막 경계를 덮도록 했다. 공용 셰이더와 구매 원본 재질은 수정하지 않았다.
- 기존 장식 바위: `Lake_CliffBank_13_R2` X+9m, `Lake_CliffBank_15_R2` X-9m/Z+4m, `ShoreRock_80` X-9m/Z-3m. 크기·높이는 유지했다.
- `apply-lake-waterfall.csx`는 최초 배치 기록이며 최종 저장본 재생성 도구가 아니다. 전체 월드 생성기와 함께 재실행하지 않는다.

## 확인 범위와 제한

- AI/Unity: 최초 배치 전후 기존 Transform 변경 0, Terrain 높이 표본 변경 0. 이후 위 세 장식 바위를 명시적으로 보정했다. 지형·NavMesh는 저작하지 않았다.
- 고정 카메라 정면·측면·위쪽·조망 촬영을 확인했다. 최종 정면은 [after-front.png](../../output/lake-waterfall/after-front.png), 위쪽은 [after-overhead.png](../../output/lake-waterfall/after-overhead.png), 조망은 [after-vista.png](../../output/lake-waterfall/after-vista.png). 실제 입력이나 밤·비 검증을 뜻하지 않는다. 주변 기존 암반의 늘어진 질감은 별도 개선 대상이다.
- 저장본 보조 검사: `node Tools/OpenWorld/audit-lake-waterfall.mjs`, [원본 결과](../../output/lake-waterfall/saved-scene-audit.json). 기존 직렬화 문서 삭제 0, 신규 폭포 자산 `.meta` 누락 0. **Unity 재로드·Missing Script·성능 검사를 대체하지 않는다.**
- 백업 대조에서 폭포 이외의 환경 반응 피격체 1,000개도 Box 비활성화·Capsule 추가·참조 교체 상태임을 발견했다(기존 문서 3,000개, 신규 Capsule 문서 1,000개). 다른 작업의 미커밋 환경 코드가 있는 상태이며 이 변경의 실행 주체는 확정하지 않았다. 단순 재직렬화가 아닌 동작 변경이므로 임의 원복하지 않고 작업 전 씬과 함께 보존했다. 이 차이를 폭포 작업의 보존 검사 통과로 보고하지 않는다.
- 최종 누락 참조 검사·신규 바위 충돌체 추가·물살 추가 명령은 MCP 단절로 성공 여부를 확인하지 못했다. 저장본에는 추가 물살 컴포넌트가 없고 신규 암반의 개별 충돌체도 검증되지 않았다. 이동·낙하 품질 인수는 보류다.
- 원본 보존 백업 약 354MB와 비교 JSON·최종 캡처를 유지한다. 실패한 Scene View 캡처 2개만 정리했다. 빌드·커밋 없음.

## 사용자 확인

준비: LakeVillageWorld를 열고 Hierarchy에서 `LakeWaterfall_Northeast`를 선택해 F로 찾는다. 호수 북동쪽 `(1133,77,1010)`에서 올려다본다.

1. LW-01: 호숫가·북쪽 산책길에서 물줄기와 물보라가 보이고 기존 길이 막히지 않는지 확인한다.
2. LW-02: 입수 부근으로 이동·수영하고 육지로 복귀한다. 보이지 않는 벽·허공 수영·바위 통과가 있으면 실패다. 신규 바위 충돌체와 낙구 물살은 아직 검증되지 않았다.
3. LW-03: 낮·밤·비에서 색·흐름·거품과 호수 접합을 본다. 직사각형 경계·심한 깜빡임·분홍 재질이 있으면 실패다.

회신: `항목 / 통과·실패·미수행 / 실제 증상·위치 / 필요 시 화면`.

