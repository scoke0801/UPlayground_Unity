# 월드와 아트

2026-11-18 **WorldBuilder P04 구현·최종 검증 중**: Region/POI·부지·Spline 도로·필수 교량 연결, 계획 SO/지형/씬 Undo와 실패 복원을 구현했다. 전체2km 규격을 유지한 별도512m 검증 구역에서 실제 연결 화면을 확인했고, 현재 관련91건을 재실행 중이다. [현재 상태·검사·남은 확인](../work-status/open-world/WORLDBUILDER.md#p04-planning), [P04 저작·사용자 테스트](../../Assets/docs/guide/WORLDBUILDER_PLANNING_AUTHORING_GUIDE.md). P05 수계 생성은 아직 진행하지 않았다.

2026-11-17 **WorldBuilder P03 구현·자동 검사 통과**: 전역 후보·안정 ID/sourceIdentity·회전 footprint 공간 해시·실제 필터·수동 Override와 P02 Dirty 연결. 최종 관련 **EditMode60/60**, Delete/변환 각20회 Undo/Redo·Dirty/Full·저장/재로드·결합 실패 복원 확인. 독립 QA 지적을 수정·재검증했다. [코드·데이터·검증·남은 항목](../work-status/open-world/WORLDBUILDER.md#p03--공통-배치-기반-2026-11-17), [배치 저작/테스트 지침](../../Assets/docs/guide/WORLDBUILDER_PLACEMENT_AUTHORING_GUIDE.md).

별도 [P03 검증 씬](../../Assets/WorldBuilder/Worlds/d303d303d303d303d303d303d303d303/P03Validation.unity)은 2,048m·2049²·700m 규격을 유지하고512m 구역에 기본 소품250개를 저장했다. 경계 양쪽123/127개·고정 footprint/spacing·Delete 부재·Pin 보존·실제 카메라 캡처 확인. B/C·전달본145파일 SHA 변경0. Dirty는 전역 재평가를 사용하며 부분 갱신 성능 최적화는 후속이다. 실제 창/Scene UX·기존 도메인 리로드/디스크 실패·미술 인수는 사용자 테스트 대기다. P03 종료 당시에는 P04 착수 전이었다.
2026-11-16 WorldBuilder 미리보기 광택 제보 수정: RGBA 알파1이 최대 smoothness로 해석돼 금속처럼 보였다. B/C 전용 TerrainLayer와 생성 코드를 **매끄러움 상수0**으로 수정했고 실제 Unity 전후 화면에서 반짝임 제거를 확인했다. 지형/원본34파일 SHA 변경0. [수정·검증·사용자 확인](../work-status/open-world/WORLDBUILDER.md#사용자-제보--미리보기-지형-광택-수정-2026-11-16).

2026-11-16 [빌드 식생 검정 원인·수정](../work-status/vegetation-build-black.md#지정-빌드-내부-분석과-수정--2026-11-16): 지정 Player 내부15종에서 SSAO off 변형0, 관련 식생·Terrain/Lit의 DBuffer off도0임을 확인했다. Quality RP 사전 필터가 원인이어서 기존 호수 Renderer를 기본 RP의 대체 Renderer로 등록하고 렌더 설정을 콘텐츠/Player 지문에 반영했다. 수정 전 실패 재현·수정 후 **BuildManager EditMode33/33 통과**, 같은 식생 셰이더의 DBuffer 입력 없는 on 변형에서RGB0/정상 off 변형에서초록색을 GPU로 재현했다. 이전 주변광 후보는 근거 불충분으로 보류. 임시 분석 자료5.10GB 제거·작은 근거 보존. 새 Player/Addressables 빌드·수정본 화면은 사용자 테스트 대기다.

2026-11-16 WorldBuilder 지역형 HeightMap: [설계 정본](../../Assets/docs/design/world-art/open-world/WORLDBUILDER_REGIONAL_HEIGHTMAP_DESIGN.md)에 따라 **P00 조사·P01 수치 기반·P02 비파괴 저작 구현과 자동 검사 통과**. [작업 상태·자산·검증·남은 확인](../work-status/open-world/WORLDBUILDER.md), [지형 저작 지침](../../Assets/docs/guide/WORLDBUILDER_TERRAIN_AUTHORING_GUIDE.md).

P01은 B(−80/480m)·C(0/700m)를 각각4×4 Terrain으로 저장하고 재로드·방향 화면을 확인했다. C700m의 경사 중앙값30.31°/p90 60.04°/p99 73.94°/최대83.28°여서 후속 필수 동선 조형이 필요하다. P01의 실제 KCC·동적 LOD·미술 인수는 사용자 테스트 대기다.

P02는 TerrainOperation·Sculpt/RAW 세션·Undo/Redo·Dirty DAG를 구현했다. **최종 EditMode38/38 통과**, 높이/Hole 각20회 Undo/Redo·Dirty/Full bit 동등·타 객체/native 브러시 Undo 격리 확인. 툴 런처 **월드 / 맵 → WorldBuilder 지형 저작**, C 저작 정의 연결 완료(명령0·씬dirty=false). P01파일94개와 B원본24파일 SHA 보존. 실제 브러시/외부 편집기 UX·디스크 중간 실패·실제 도메인 리로드 실험은 미수행/사용자 확인 대기다. P02 종료 당시 P04 이후·PlayMode·Player/Addressables 빌드는 미수행이었으며 컨셉 PNG를 Terrain 입력으로 사용하지 않는다.

미니맵 물 가독성 수정(2026-11-15): 활성 물 표면7개에 밝은 파랑 불투명 재질을 임시 적용해 재촬영·배경 연결했다. 촬영 후 원래 재질7/7 복원 확인, 씬·품질 설정 보존. [현재 지도·검증](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

폭포 미니맵 갱신(2026-11-15): 현재 호수 월드를3600×4200 재촬영해 LakeVillage MinimapConfig 배경에 반영했다. 기존 GUID·중심·범위 유지, 폭포 원본 픽셀과 임포트 참조 확인. 실제 HUD 가독성은 사용자 확인 대기. [작업 기록](../work-status/lake-waterfall.md#폭포-반영-미니맵-재촬영--2026-11-15).

- 2026-11-15 [빌드 근거리 검정 출력](../work-status/vegetation-build-black.md#거리-의존-제보와-빌드-수집-누락): 기본 RP는 SSAO/Decal on, 실행 호수 RP는 off인데 씬 참조 RP의 빌드 포함 옵션이 꺼진 설정 결함을 확인·1줄 수정했다. 실제 씬→RP→Renderer·빌드 프로필 정적 검사 및 **Unity 네이티브 빌드 RP 수집 목록에 기본/호수 RP 2개 포함 확인 통과**. 콘텐츠+Player 전체 빌드의 근/원거리 화면은 사용자 테스트 대기다. 앞선 HLSL 미초기화 경고 제거는 별도 보정이며 이 증상의 해결 근거가 아니다.

- 2026-11-15 [폭포 R4 표시 결함·개방형 공간](../work-status/lake-waterfall.md#r4-표시-결함개방형-공간--반영저장): 상류·낙수 단일 메시 연결, 사각 외벽·별도 길 제거, 사면 안쪽의 넓게 열린 공간과 중앙 상자 반영·저장. 캡슐27표본 막힘0·Nav 경로 Complete·ID15,751개 고유. 실제 이동·미술·보상 왕복은 사용자 테스트 대기. 아래 R3 형상은 수정 전 이력이다.

- 2026-11-15 [폭포 R3·숨은 동굴](../work-status/lake-waterfall.md#r3-낙수-수정숨은-동굴--반영검증): 낙수 접선·중력 곡선/두께/비말 보정, 폭포 뒤 실제 동굴9.5m·마른 바닥·진입 경사·보물상자1개 반영. 기존ID15,750개 유지+신규1개, 캡슐26표본 막힘0·동굴 Nav 경로 Complete·672타일 저장. 카메라/보상 계약 EditMode16/16, 실제 보상 데이터 중복/소비 복원 보조 확인. 실제 조작·발견·미술·저장 왕복은 사용자 테스트 대기다. [설계](../../Assets/docs/design/world-art/environment/LAKE_WATERFALL_CAVE_DESIGN.md).

- 2026-11-15 [폭포 R2 재저작·상류 시작 보정](../work-status/lake-waterfall.md#r2-재저작--현재-저장본): 폭18.6m, 지형 하상·낙구·호수 소로 재구성하고 추가 돌10개 제거·기존 돌 낮춤. T자 물판 대신 지형에 닫힌 상류 소로 연결했다. 지형 원본 보존·지정 밖 높이변경0·배치ID15,750개 보존·672타일 NavMesh 저장 확인. 주변 경로는2쌍 Complete/기존2쌍 Partial 유지. 별도 고사목3개의 자동성 반응 구성 변경이 있어 전체 보존 감사는 실패로 기록했다. 실제 이동·수영·미술 인수는 사용자 테스트 대기다. 아래 R1은 실패 이력이다.

- 2026-11-15 [폭포 측면·주변 돌 사용자 재제보](../work-status/lake-waterfall.md#사용자-재제보--실패-수정-대기): 상류 물판이 하상 없이 뜨고 돌이 선돌처럼 반복되는 미술 실패를 확인했다. 기존 정면 캡처를 인수 근거로 승계하지 않는다. 하상·낙구부터 다시 구성할 예정이며 다른 MCP의 같은 씬 작업과 겹쳐 Unity 쓰기는 대기 중이다.

- 2026-11-15 [호수 북동 폭포](../work-status/lake-waterfall.md): 북동 숲 사면에서 호수로 흐르는 폭포를 저장했다. RiverBasinWorld 낙수 재질·물보라 재사용, 전용 메시·재질과 호안 암반10개, 기존 장식 바위3개 보정. 고정 구도 확인·지형 높이 변경0. MCP 단절로 최종 재로드·충돌·물살 검증은 미수행이며 사용자 테스트 대기다. 별도 환경 피격체1,000개 변경은 주체 미확정 상태로 보존하고 백업 대조 근거를 기록했다.

- 2026-11-15 [사진 속 약초 작업대 수정](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-스크린샷-재제보--약초-작업대-실제-대상-수정): 앞선 다른 식탁 보정을 이 제보의 해결로 볼 수 없음을 정정했다. Hazel_Worktable_R2를 원래 충돌/접촉 범위에 맞추고 병·단지·약초 그릇 네 곳의 잘못된 압축 항아리 세트를 개별 원본 모델로 복구했다. 기존 기능 루트·진행 ID와 구매 원본 보존. 실제 Unity 전후 화면·접촉 측정 근거 연결.

- 2026-11-15 [NPC 전수 생활 보완](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md#사용자-지적에-따른-전수-확대): 연결 지역 포함 83명 조사·58명 행동 보완. 상점 장부·천·채소 작업면 3곳에 기존 FarmLand 자산 배치, 실제 호수 상점 화면 확인. 지역 전체 접촉·미술 인수는 사용자 테스트 대기.

- 2026-11-15 [생활 행동·식탁 접촉 수정](../work-status/npc-daily-animation/LAKE_ACTIVITY_FIX.md): 교체된 식탁의 실제 메시 상판 기준으로 소품 8개를 보정했다. 기존 위치·진행 ID 보존, 전체 월드 재생성 없음. 수치 검사와 실제 플레이 시각 확인은 구분하며 최종 사용자 테스트 대기.

- 2026-11-15 [잔상 텍스처·Fauna 렌더 오류 수정](../work-status/afterimage-fauna-render-errors.md): Fauna 깊이·노멀 샘플 수가 다르면 DepthOnly로 출력해 MSAA4 RenderGraph attachment 오류를 방지한다. Unity 컴파일·관련 EditMode22/22·PlayMode2/2 통과, RF02 본편 화면 품질 사용자 테스트 대기.

LakeVillageWorld 대기 프레임 저하(2026-11-14~15): [조사·최종 근거](../work-status/lake-village-idle-performance.md#debug-최종-판정--acceptance-3). **Editor Debug의 초기화 후 대기 기준 통과**: 맑음/비 각600프레임, 평균40.01/36.73FPS·임의500ms 최저33.81/31.46FPS.2560×1440/MSAA4와18,472대상을 유지하며 그림자25m/1cascade·SSAO off·빈 Decal 패스off를 전용 에셋에 저장했다. 최신 회귀100/100, 강수 위치/차폐 차이0, GPU 유효 표면45개 통과/8개 미관측이다. 단일 프레임 최대88.25ms와 기존 ActorDatabase 지역 배치 시간 초과2건은 남는다. 전체 NPC 정상 배치·이동/전투·미술 인수는 사용자 확인 대상이며 대기 수치를 전체 월드 품질 통과로 승계하지 않는다.

## LakeVillageWorld 식생 전수 전환 — 2026-11-14

나무 벌목 마무리(2026-11-15): [정본](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md)에 따라 **런타임·5종4,767개 SO/FX 반영·관련 자동 검사 통과**다. 낙하→정착→보상1회/목재·먼지FX→줄기·수관·밑동 소거를 연결했다. 저장/구버전 이관·보상 pending/좌표 복원·강수/열/전파·충돌/Nav 종료를 포함한다. EditMode80/80·본편 복제본15/15·기본/공유 버퍼 셰이더58패스 통과. 큰 월드 좌표의 디더 붕괴와 기존 생성기의 피벗 상수 좌표를 수정했고 LOD0/1/2 중간 소거·최종 픽셀0을 GPU 캡처로 확인했다. 독립 코드 재검토에서 추가 확정 결함 없음. 실제 입력·FX/청감·그림자·KCC/Nav/성능·미술 인수는 [사용자 테스트 대기](../work-status/reactive-environment-tests.md#낙하-완료드롭fx소거-사용자-인수--2026-11-15)다. [작업 상태·원본 근거](../work-status/tree-destruction.md#낙하-완료-수명-구현--2026-11-15).

옆 나무 피격 BUG-DT-02·큰 잔해 FEEL-DT-01(2026-11-15): 수관/impostor 입력 Box를 제외하고 본편4,767개를 줄기 Capsule 피격으로 전환·저장했다. 전수 입력 오류0, 실제 물리 재현15개에서 첫 나무75/이웃100 및 첫 나무 파괴 후에도 이웃 오탐0을 확인했다. 기존 물리 MeshCollider17,920개 변경0·대상 나무 ID/외형 변경0·구매 원본/meta234개 변경0. 다른 MCP/테스트 요청이 있으면 대기하도록 짧은 묶음으로 적용했으며 사용자 원래 입력 재시험은 대기다. 큰 잔해는 위 마무리 설계로 방향을 확정했으며 런타임 수명/형상 반영은 대기다. [최종 근거·스크립트·인수](../work-status/tree-destruction.md#bug-dt-02--feel-dt-01--옆-나무-피격과-큰-잔해).

나무 파괴 외형 결함 BUG-DT-01(2026-11-15): 원본별/LOD별 슬롯 순서를 고정한 생성기 때문에 잎에 나무껍질이 붙었다. 생성기를 재질 참조 기준으로 수정하고 잘못된 잔해4종34슬롯·실행 복제본을 바로 교정했다. 자산5종60슬롯·실행4,767그루57,204슬롯 불일치0, 구매 원본/meta234개 변경0·씬 저장 없음. [원인·사진·수정·재검증](../work-status/tree-destruction.md#bug-dt-01--파괴-잎에-나무껍질-재질이-붙음). 같은 나무의 근/중/원거리 외형은 사용자 재시험 대기이며 이전 기능 자동 검사 통과가 외형 통과를 뜻하지 않는다.

제외 고사목 식별(2026-11-15): [본편 실제 사진3장·좌표·제작 출처](../work-status/tree-destruction.md#제외-고사목-실제-배치-사진--2026-11-15)를 기록했다. 프로젝트 `ArtSource/PoorVillage/build_poor_village.py`의 절차적 Blender 모델 Terrain_DeadTree가 동일하게3개 배치되어 있다. 촬영 중 씬/오브젝트 변경 없이 뷰를 복원했다. 파괴 전환 승인은 아직 받지 않았다.

일반 수목 파괴 연결(2026-11-15): [작업 상태·검증 근거](../work-status/tree-destruction.md). FullTree5종4,767개에 내구도100·목재1~4개·종류별 Stump/Trunk·방향성 낙하/막힘 잔해를 연결해 본편 씬에 저장했다. 환경 통합40/40·복제본 실제 피격/JSON15개 시나리오·식생15,396개 전수 구성 통과, 배치 ID/원형 외형 변경0·구매 원본/meta234개 해시 변경0. 보호829개와 나머지 식생은 보존했다. 고사목3개 추가 전환은 자동 승인 검토에서 범위 확장/기존 구성 제거로 거절되어 미적용이다. [직접 공격·낙목/KCC/Nav·저장/지역 왕복·성능·미술 인수](../work-status/reactive-environment-tests.md#일반-나무-파괴-연결-인수--2026-11-15)는 사용자 테스트 대기이며 종전 대기 성능 인수를 승계하지 않는다.

전환 전 조사(2026-11-15): 당시 식생15,396개는 전부 비파괴였다. [조사 당시 구성](../work-status/reactive-environment.md#나무-파괴-연결-재확인--2026-11-15)은 과거 근거이며, 이후 일반 수목4,767개는 위 파괴 연결 결과로 대체됐다.

[최종 결과·검증 근거](../work-status/reactive-environment.md#식생-전수-전환-결과--2026-11-14): 식생15,396/15,396개와 Terrain10층을 실제 본편에 연결·저장했다. 재로드 후 전수 구성 유효, 누락/중복 ID·배치/활성/ID 변경0, 원본/메타332개 해시 변경0이다. 퀘스트/수중/채집 시각 식생829개를 비가연·비파괴 정책으로 보호한다. 외형·LOD·바람·알파와 기존 채집 소유권을 보존한다.

[표현 최적화·최종 자동 검증](../work-status/reactive-environment.md#공유-표현-버퍼와-전환-최종-자동-검증--2026-11-14): 활성 식생15,284개+Terrain 셀3,188개(총18,472대상), 강수 판정 전수 차이0. Native 강수 묶음과 공유 GPU 표현으로 강수 중앙 tick은 Editor에서220~230ms→67~74ms, 표현은약60ms→10ms로 줄었다. CPU 화학·전파·저장과 카메라 밖 대상 처리는 유지한다. GPU 일반/인스턴싱 출력 대조와 열/물·JSON·ID 재등록·Terrain 원본 복구를 확인했다.

**전수 전환·자동 검증과 최종 플레이 인수를 구분한다.** 일반 공격 입력·실제 저장/지역 왕복·KCC/Nav·모든 LOD/임포스터·목표 기기 성능·미술/청감은 [LV-V01~06 사용자 테스트 대기](../work-status/reactive-environment-tests.md#lakevillageworld-식생-전수-전환-인수-전환-완료사용자-검수-대기-2026-11-14)다. 강수 tick 잔여67~74ms는 목표 기기 FPS 통과 근거가 아니며 성능 인수는 보류다. 기존 재생성 원본 부재와 비식생을 포함한 W 전체 인수는 이번 식생 전환으로 해소되지 않는다.

## 유기체 모델링 품질 스킬

2026-11-15: 환경·소품 모델이 프리미티브 조합·플랫 셰이딩으로 각지고 로우폴리해 보이는 문제(예: PoorVillage `Terrain_DeadTree`)를 줄이려 [organic-modeling-quality 정본](../../.claude/skills/organic-modeling-quality/SKILL.md)과 공용 Blender 함수 [organic_lib.py](../../.claude/skills/organic-modeling-quality/templates/organic_lib.py)를 추가했다. `environment-artist` 스킬과 [artist 역할 정본](../../Tools/Harness/roles/artist.md)에서 참조한다.

- 구매 팩 FBX 20개 실측: 전부 스무스 셰이딩 100%·커스텀 노멀. 각져 보이는 원인은 셰이딩이 아니라 형태다. LOD0 삼각형 수는 활엽수 줄기 약1,400~3,000, 고사목 1,810~24,826, 그루터기 890, 통나무 744~860, 바위 소160~433/중252~1,334/대5,150. LOD1은 55~75%, LOD2는 30~55%.
- 검증: Blender 5.2.2 백그라운드 CLI에서 레시피 5종(나무·고사목·바위·통나무·그루터기)이 수치 기준 통과, FBX 왕복에서 tris·스무딩·노멀 보존. 하네스 검사 43개 통과.
- 첫 적용 비교(2026-11-15, Blender CLI 임시 씬, 저장·Unity 변경 없음): 기존 `Terrain_DeadTree`(844 tris, 스무스 69%, 90° 직각 에지, 막대형 뿌리·가지 15조각)와 새 레시피 고사목(3,996 tris, 스무스 100%, 중앙 에지각 13°, 한 덩어리)을 4구도로 렌더 비교했다. 새 쪽은 뿌리·가지가 줄기에서 이어져 나오고 실루엣이 유기적이다. 뿌리 굴곡은 아직 약하다. 기존 배치 3개 교체와 Unity 적용은 승인 전이라 하지 않았다.
- Unity 첫 적용(2026-11-15, A안 별도 에셋): `ArtSource/PoorVillage/build_deadtree_organic.py`(seed 37, base_r 0.55)로 `Assets/08.Models/PoorVillage/Terrain_DeadTree_Organic.fbx`(LOD0/1/2 3,990/2,592/1,596 tris, 기존 meta 설정 동일)와 같은 이름의 프리팹(PoorHouse_Wood_Weathered 재질, LOD2 MeshCollider)을 만들었다. 프리뷰 씬 비교 렌더(`output/PoorVillage/DeadTreeOrganic-20261115/`, Git 무시)에서 Y-up·스케일1·밑동 접지를 확인했다. 새 쪽이 높이 6.54m로 기존 5.28m보다 24% 크다(교체 시 약0.81배 보정 필요). LOD2 p90 에지각이 45.9°로 기준을 조금 넘는다. 기존 배치 3개 교체와 월드 조명 확인은 사용자 승인 전이라 하지 않았고, 미술 품질은 사용자 확인 대기다.
- 고사목 3개 교체(2026-11-15): 사용자 승인·권한 허용 후 LakeVillageWorld의 empty-space-0031·marked-0018·marked-0024를 `Terrain_DeadTree_Organic`으로 제자리 교체했다(Undo 1그룹, **씬 미저장**). 이름·경로·회전·SceneEntityId·placementGuid를 보존했다. 배율 ×0.8075로 높이를 기존과 같게(6.60/7.92/7.13m) 맞췄고 marked-0024만 접지를 위해 0.133m 내렸다. 반응 바인딩은 LOD 렌더러 3개와 루트 MeshCollider·입력 Box로 다시 연결하고 입력 범위·샘플을 새 형태에 맞췄다. `_sourceId`는 새 프리팹 GUID로 갱신했다. 교체로 생긴 null 참조·Console 오류 0. 전후 캡처(월드 조명)는 `output/PoorVillage/DeadTreeOrganic-20261115/replace/`(Git 무시)에 있다. 한계: 뿌리 노출 부족(원 피벗이 지형 아래라 ×0.81 축소와 겹쳐 뿌리가 묻힘), "부러진 고목" 인상 약화. 미술 품질과 Play 반응(피격·연소·비·채집·LOD)은 사용자 테스트 대기다. `dead-tree-destruction-apply.csx`는 새 구조와 맞지 않아 파괴 연결 전에 수정이 필요하다. 재생성 계획 JSON(`empty-space-plan.json`, `marked-infill-plan.json`)은 기존 프리팹 경로를 그대로 참조하므로 재생성 스크립트를 돌리면 기존 고사목으로 돌아간다.
- **미수행:** 대화형 Blender MCP 실행, Unity 임포트·lilToon 화면 확인, 크기·축 확인, 석조 소품 실증, 버텍스 컬러·AO 베이크. 미술 품질(기존 팩과의 어울림)은 사용자 확인 대기. 렌더는 셰이딩·실루엣 확인일 뿐이다.

## Unity FX 제작 스킬

2026-11-14: [uplayground-vfx 정본](../../.claude/skills/uplayground-vfx/SKILL.md)을 작성하고 Codex 사용자 스킬 폴더에 설치했다. 기존 Unity MCP·Shuriken·MotionEvent·무기 궤적·풀을 활용하며 VFX Graph 노드 편집 제한을 명시했다. 정본/설치본 구조 검사와 3개 파일 해시 대조 통과. 실제 FX 제작·Unity 실행·미술 검증은 미수행이다. [설치·검증·사용법](../work-status/uplayground-vfx-skill.md).
