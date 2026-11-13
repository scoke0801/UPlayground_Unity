# 물·수영·물고기 상호작용 후속

- 설계 정본: [기존 물 통합 계약](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md)
- 이전 근거: [물 작업 상태](toon-water-world.md)
- 위키: [월드와 아트](../wiki/world-art.md), [플레이어와 화면](../wiki/player-ui.md)
- 판정: 코드·정규 물고기 30개체 저작 반영, 관련 EditMode 85/85·최종 MSAA4 Lab 17건·정규 강수 On/Off 2건·물고기 저장본 재로드 연결 검사 통과. 실제 입력·획득/저장 플레이·미술 인수는 사용자 테스트 대기.
- 최신 물가 일렁임: 웹 조사 후 정규 SO/시각 메시 저작 반영. 이번 관련 EditMode25/25·위상0/3/6/9/12초 실제 렌더5건·실제 지형6횡단선의 물선 이동 계산 확인. 사용자 TPS·비·미술/실기 성능은 대기. 아래 과거85/17건을 이번 재검증으로 승계하지 않는다.

## 반영 단계

### 물가 실제 일렁임 — 웹 조사와 적용

- 사용자 요청: 물 경계가 자연스럽게 일렁이도록 웹 조사 후 적용. 인위적 장식 경계 지양.
- 조사: [NVIDIA GPU Gems 1장](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models)은 기하 높이 파도와 노멀 세부 파문을 분리하고 파장·진폭·속도·방향의 관계를 설명한다. [Catlike Coding Waves](https://catlikecoding.com/unity/tutorials/flow/waves/)는 파장에 비해 메시가 성기면 파형이 사라지거나 톱니가 생김을 설명한다. [Looking Through Water](https://catlikecoding.com/unity/tutorials/flow/looking-through-water/)는 실제 수면/불투명 깊이 차이를 광학에 사용한다. 구형 Unity 예제 코드는 복사하지 않고 원리만 현행 URP 구현과 대조했다.
- 현재 확인: CPU 수영 높이·GPU 수면 정점/법선·수중 구간은 기존 두 파도 SO를 공유한다. 정규 호수 파장48/28m·속도0.8/0.5m/s의 주기는60/56초로 물가 변화가 지나치게 느리다. 시각 메시23,377정점·46,440삼각형, 보간 오차 상한0.01802m다.
- 적용 결정: 공통 파도 수식은 유지하고 정규 호수 SO에서 서로 다른 방향·주기의 저경사 파도를 저작한다. 수면 메시도 최단 파장당 최소6변 간격을 갖도록 기존 세분화 함수를 재사용한다. 쿼리 원본·저장/상호작용·수중 판정은 유지한다. 흰 포말 띠·경계 전용 사인파/스크롤 마스크는 추가하지 않는다. 이는 실시간 높이장 근사이며 천수 방정식·쇄파·굴절 파도 시뮬레이션 완료로 표현하지 않는다.
- 반영: [기존 파도 SO](../../Assets/10.Datas/World/ToonWater/LakeWorldDynamics.asset)에서 파장23/13.7m·속도2.4/1.8m/s(주기9.58/7.61초)·진폭0.045/0.025m·방향(1,0.3)/(0.6,1)로 변경했다. 파도 두 개가 겹칠 때의 높이 상한은±0.07m로 낮춰 작은 호수의 잔잔한 움직임을 유지한다. C#/HLSL 수식·렌더 경로·접촉/비 효과는 이번 단계에서 수정하지 않았다.
- 시각 메시: 기존 세분화 함수를 재사용해 [LakeShoreWindVisual.asset](../../Assets/10.Datas/World/ToonWater/LakeShoreWindVisual.asset)와 meta를 생성했다.104,025정점·207,328삼각형·11,636,903바이트. 정규 씬 diff는 MeshFilter 참조1줄 교체뿐이며 원본 시각 메시·쿼리·지형·물고기 배치/ID는 불변이다. [생성 근거](../../output/ToonWaterWorld/ShoreWaveResearch/mesh-generation.txt).
- 정규 구조 검사: 오류0, 파도 경사0.02376(한도0.35), 보간 오차 상한0.00748m(한도0.02m). [적용 근거](../../output/ToonWaterWorld/ShoreWaveResearch/application-validation.txt), [임시 위상 복구 후 검사](../../output/ToonWaterWorld/ShoreWaveResearch/final-validation.txt). 수직 체적 무제한·임의 수중 AlphaBlend/XR 제한 등 기존 경고는 유지한다.
- 이번 EditMode **25/25 통과**, 실패/건너뜀0: WaterRenderingContractTests·WaterSurfaceQueryTests로 공통 CPU/GPU 높이·광학·수역 선택/건조 구역을 확인했다. [MCP 원본 결과](../../output/ToonWaterWorld/ShoreWaveResearch/editmode-25.json). NUnit XML이나 실제 조작 인수로 표현하지 않는다.
- 물선 이동: 실제 Ground 콜라이더를 0.1m 간격으로 샘플한 x=964/972/980/988/996/1004의6횡단선에서 t=0~12초의 지형/수면 교차를 계산했다. 각각 이동 폭0.383/0.751/0.877/0.444/0.293/0.377m로 전진·후퇴 시점이 서로 다르다. [계산 결과](../../output/ToonWaterWorld/ShoreWaveResearch/shoreline-transects.tsv). CPU 높이장과 지형의 계산 결과이며 실제 입력·영상 측정과 구분한다.
- 시계열 실제 렌더: 동일 카메라·MSAA4·1280×720에서 [0초](../../output/ToonWaterWorld/ShoreWaveResearch/Phase-00.png)·[3초](../../output/ToonWaterWorld/ShoreWaveResearch/Phase-03.png)·[6초](../../output/ToonWaterWorld/ShoreWaveResearch/Phase-06.png)·[9초](../../output/ToonWaterWorld/ShoreWaveResearch/Phase-09.png)·[12초](../../output/ToonWaterWorld/ShoreWaveResearch/Phase-12.png)5건 통과. 모두 색4/깊이4·Body1/Cell94·자홍색0·카메라 전역 플래그 복구. PNG를 직접 열어 흰 띠 없이 지형을 따라 물선이 이동함을 확인했다. [메타데이터](../../output/ToonWaterWorld/ShoreWaveResearch/phase-captures.json). EditMode에서 속도/시계 비를 임시 조정해 위상을 주입하고 finally에서 SO 속도2.4/1.8을 복구했다. 실제 플레이 녹화나 동일 날씨/TPS 구도의 인수 결과가 아니다. 렌더 요청 CPU 시간은 GPU/FPS 측정이 아니다.
- 성능/인수 경계: 시각 수면 삼각형이 기존 대비약4.46배다. GPU 광학 셀94와 CPU 쿼리 원본은 유지하지만 실기 GPU 비용·최대 해상도/장시간 성능은 미측정이다. 이번에 Editor Play Mode 입력·실제 비 상태의 최종 미술·수영 경계 체감은 수행하지 않았다. Player/Addressables 빌드·커밋은 실행하지 않았다.
- 인수 절차 **WS07**: 정규 호수의 사용자 지적 물가(대략 x980,z745)에서 카메라를 고정하고15초 관찰한다. 물선이 지형 경사에 따라 조금씩 전진/후퇴하고 옆 구간이 동시에 같은 모양으로 움직이지 않아야 한다. 흰 테두리·등간격 scallop·톱니/깜빡임·색 띠가 보이면 실패. 맑은 날/비, 눈높이/상부 시점에서 각각 확인하고 얕은 물 걷기→수영 진입도 확인한다. 결과는 `WS07 / 통과·실패·미수행 / 날씨·위치·입력 / 실제 증상·화면`으로 회신한다. **사용자 테스트 대기**.
- 보존·정리: [최종 준비표](../../output/harness/20261113-143722-554a9768/review.md), [조사 단계 준비표](../../output/harness/20261113-143142-4ae1b6b1/review.md). 수정 전 정규 씬150,984,854바이트와 SO는 `output/ToonWaterWorld/ShoreWaveResearch/`에 보존했다(기존 미커밋 상태 포함). 원본 시각 메시·쿼리·구매 자산·다른 작업 변경은 보존한다. 검사 카메라/RT는 finally에서 해제했고 빌드·캐시·임시 검사 소스는 만들지 않았다. 최초 자연 시계 촬영 Shore-A는 구도 탐색 근거, 최종 위상 판정은 Phase 파일을 사용한다.

### 물가 경계 사용자 재지적

- 사용자 실제 비 화면에서 물가가 밝은 연속 띠와 규칙적으로 굽은 경계로 보이는 실패를 접수했다. 앞선 렌더 자동 검사 통과는 미술 품질의 통과가 아니며 물가 미술 인수는 실패 상태다.
- 원인: 실제 충격과 관계없이 얕은 수심 전체에 포말을 생성하고, 수심/월드 사인파로 변주한 코드가 가장자리에 반복되는 흰 띠를 만들었다. 물가를 더 많은 노이즈로 덮지 않고 **수심 기반 해안 포말 생성 자체를 제거**했다. 실제 파도 높이·광학·지형 접점 투명 전환과 접촉/비 파문은 유지한다. 재질·씬·저장 ID는 수정하지 않았다.
- 수정 전 셰이더는 [원본](../../output/ToonWaterWorld/ShorelineFeedback/WaterSceneResolve.before.shader)으로 보존했다. [셰이더 조회](../../output/ToonWaterWorld/ShorelineFeedback/shader-audit.txt)는 지원=true·오류0·기존 loop hint 경고2다. 정규 호수를 별도 임시 카메라로 MSAA4/1280×720의 [눈높이](../../output/ToonWaterWorld/ShorelineFeedback/Shore-EyeHeight.png)·[높은 시점](../../output/ToonWaterWorld/ShorelineFeedback/Shore-HighAngle.png)에서 실제 렌더하고 PNG를 직접 열어 연속 포말 띠·반복 굴곡이 사라진 것을 확인했다. 촬영 호출은 MCP 응답 타임아웃이 있었으나 실행 이력에서 성공(71.1초)을 확인했고 두 PNG가 생성됐다. CPU 작업 경과이며 성능 측정으로 취급하지 않는다. 검사 카메라는 finally에서 제거했다.
- 이번 검증은 셰이더 조회와 고정 시점 2건이며 기존 EditMode85/85·Lab17건을 다시 실행한 결과가 아니다. 사용자가 지적한 실제 비·TPS 구도와 동일한 재현으로 보고하지 않는다. **사용자 물가 미술 재확인 대기**: 같은 물가를 비/맑은 날 눈높이와 위에서 확인하고 흰 띠·등간격 굴곡이 남으면 실패다. 수면 접근·입수 시 실제 접촉 파문이 유지되는지도 확인한다.
- 이번 단계의 변경은 Resolve 셰이더와 설계·상태·위키뿐이다. 재질·씬·기존 미커밋 변경은 보존했다. 임시 재질 사본은 사용하지 않아 제거하고 수정 전 셰이더·실제 렌더 근거만 보관한다. 빌드·새 검사 도구·커밋은 만들지 않았다.

1. 화면·수영 코드 반영: 캐릭터 잠수에 따라 HUD가 덧씌우던 청록색/기포를 해제하고 숨 부족 경고만 유지한다. 물 광학은 기존 카메라별 합성이 소유한다. 수직 버튼 축과 자동 고속 전환을 제거하고 Dash 홀드+이동으로 고속 수영한다. 상승·하강은 기존 카메라 방향 이동만 사용한다. Space의 수면 턱 올라가기는 유지한다.
2. 물가·강수 코드 반영: 얕은 물의 일정한 불투명 착색을 깊이에 따라 줄이고 지형 접점의 투명 전환·월드 좌표 포말 변주를 적용했다. 비 법선의 후행 고리, 원거리 세부 감쇠, 정규 호수 SO의 강수 세기를 조정했다. SurfaceInfo W 정수부는 수역 ID, 소수부는 차폐·거리 감쇠 후 실제 비 파문 밝기로 사용한다. 일반 파도가 비 하이라이트로 바뀌지 않도록 실제 비 신호만 전달한다. 화면 미술 인수는 사용자 대기.
3. 물고기 저작 반영: 정규 LakeVillageWorld의 기존 농어 30개체에 DropItemActor·감지용 Trigger Sphere·SceneEntityId를 연결했다. 보상은 기존 물고기 아이템(100008) 1개이며 별도 보상/저장 경로를 추가하지 않았다. 수영 상태를 벗어나지 않고 즉시 획득한다. ID 30개 중복 없음·ItemData/Count/InteractionData 연결 검사를 거쳐 저장했다. [배치 근거](../../output/ToonWaterWorld/SwimInteractionFix/fish-placements.txt). 획득 후 보상 알림·인벤토리 실패 보존·소비/재방문 정책은 기존 배치 아이템과 같다. 실제 수영 중 획득/저장 플레이는 사용자 대기.

## 자동 검증

- Unity Editor 관련 EditMode **85/85 통과, 실패/건너뜀 0**: PlayerSwimTests·WaterRenderingContractTests·GroundDropLedgerTests. 새 HUD 착색 차단 2건과 빠른 수영 홀드 3건, 기존 카메라 수중 구간·CPU/GPU 파도·강수 차폐·획득 원자성/중복/인벤토리 실패/저장 원장 회귀를 포함한다. [MCP 원본 결과](../../output/ToonWaterWorld/SwimInteractionFix/editmode-85.json). NUnit XML 별도 확보나 실제 입력 테스트 통과로 표현하지 않는다.
- 정규 씬 저장 diff는 YAML 블록 기준 신규 120개(GameObject 참조·콜라이더·ID·Drop 각 30), 기존 생태 프리팹 인스턴스 1개 변경, 삭제 0이다. 기존 지형·구조물 블록은 불변이다. [블록 감사](../../output/ToonWaterWorld/SwimInteractionFix/scene-block-audit.json), [상세 diff](../../output/ToonWaterWorld/SwimInteractionFix/scene.diff).
- 중간 실제 렌더에서 물가 투명 전환과 비 On/Off의 원형 파문을 확인했다. 원거리 반짝임이 과해 비 신호 전달·세부 감쇠를 후속 수정했으므로 최종 렌더 결과는 별도 기록한다. [물가](../../output/ToonWaterWorld/SwimInteractionFix/Shore-EyeHeight.png). 강수는 실제 날씨 플레이가 아닌 임시 강도 주입이며 finally에서 기존 날씨 참조·강도를 복구했다.
- Resolve의 기존 loop hint 경고 2건은 남으며 Shader 오류와 구분한다. Player/Addressables 빌드·장시간/실기 성능은 미수행.

### 최종 확인

- 후속 셰이더 수정 후 기존 WaterLab을 MSAA4로 별도 폴더에서 실행했다. **17건 통과**, 색/깊이 샘플 모두4·자홍색0. 반잠수·두 수역·건조 동굴·정사영·Fog 소유권·날씨 수면 앞/뒤 차폐 검사 포함. [최종 캡처 메타데이터](../../output/ToonWaterWorld/Rendering/SwimInteractionFix-MSAA4/captures.json). 과거 MSAA1/2 결과를 이번 결과로 승계하지 않는다.
- 정규 호수의 [비 없음](../../output/ToonWaterWorld/SwimInteractionFix/Rain-Off-Final.png)·[비 있음](../../output/ToonWaterWorld/SwimInteractionFix/Rain-On-Final.png) 실제 출력 2건에서 색4/깊이4·자홍색0, 퍼지는 원형 고리와 원거리 감쇠를 확인했다. [메타데이터](../../output/ToonWaterWorld/SwimInteractionFix/rain-captures-final.json). 실제 비 플레이가 아닌 강도0/1 주입이며 입력·미술 인수와 구분한다.
- [최종 셰이더 검사](../../output/ToonWaterWorld/SwimInteractionFix/shader-audit-final.txt): Resolve/SurfaceInfo 지원=true, 오류0, C# 컴파일 실패=false, Trigger 조회=true. Resolve의 기존 경고2는 유지한다.
- 씬을 닫고 저장본을 다시 열어 물고기30·잘못된 연결0·기록된 ID30개 동일·미저장 상태없음을 확인하고 추가 로드한 호수 씬을 닫았다. [재로드 근거](../../output/ToonWaterWorld/SwimInteractionFix/reload-validation.txt). 검증 후 파일은 저장 직후와 동일하다. 실제 인벤토리 지급/사용자 세이브 왕복 검증으로 확대해 표현하지 않는다.
- 최종 Console에는 동시에 실행된 반응형 환경 테스트에서 발생한 오류3건이 있다. 스택은 `ReactiveEnvironmentIntegrationTests.AuthoredFarmTreeAndActualTerrainCellsSpreadBothWays` 2건과 `TerrainRangeBurnSaveReloadAndPartitionFailurePreserveSource`의 Assert.Throws 경로1건이다. 물 코드의 오류나 전체 Console0으로 보고하지 않고 원문을 [보존](../../output/ToonWaterWorld/SwimInteractionFix/console-final.json)한다. 해당 작업의 판정은 별도 작업 상태를 따른다.
- 이번 문서·위키 5개에서 로컬 링크128건 존재를 확인했다. [링크 감사](../../output/ToonWaterWorld/SwimInteractionFix/link-audit.json). 이후 추가된 최종 근거도 파일 존재를 재확인한다.

## 보존·정리

- 기존 다수 미커밋 변경은 보존한다. 구매 원본·물고기 애니메이션은 수정하지 않는다.
- 정규 씬은 Git 추적 밖이므로 수정 전 `output/ToonWaterWorld/SwimInteractionFix/LakeVillageWorld.before.unity`(150,903,933바이트)를 보관한다. 기존 사용자 상태를 포함하므로 자동 삭제하지 않는다.
- 최종 범위 준비표: [review](../../output/harness/20261113-133359-d3a26ca6/review.md). 최초 [준비표](../../output/harness/20261113-132339-c9d2270c/review.md)는 조사 단계 근거로 유지한다. Player·Addressables 빌드와 커밋은 실행하지 않는다.
- 중간 Rain-On/Off PNG와 최종 PNG는 원거리 과도한 반짝임 수정 전후 비교 근거로 보존한다. 최종 판정은 `Final` 파일을 사용한다. 검사 카메라·임시 재질/RT는 기존 검사기의 finally에서 해제했고 강수 주입은 복구했다. 삭제할 빌드·프로젝트 사본·도구 캐시는 만들지 않았다.

## 사용자 직접 테스트 — 사용자 테스트 대기

대상: 수정된 Unity Editor의 정규 LakeVillageWorld. 키보드/마우스, 원본 저장을 덮지 않는 테스트 슬롯. 실패 시 ID·입력 장치·날씨·시작 상태·실제 증상과 Console 로그를 남긴다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| WS01 | 캐릭터만 잠수시키고 카메라는 수면 밖 유지 → 카메라도 잠수 → 재부상 | 수면 밖 전체 화면에 청록색/기포가 덮이지 않는다. 카메라가 물속으로 들어간 영역만 수중 합성. 숨 부족 경고는 캐릭터 숨 기준 유지 |
| WS02 | 물속에서 이동 없이 Shift·Space·웅크리기 각각/동시 입력 | 버튼만으로 상승/하강하지 않음. 수면 위 턱에서 Space로 올라가기는 유지 |
| WS03 | 아래를 보고 전진하여 잠수 → 위를 보고 전진 → 뒤/옆 이동 → Shift 홀드/해제 | 시선+이동으로만 깊이 조정. Shift 홀드 중 빠른 수영, 해제 즉시 보통 속도 목표로 복귀. 키 해제 후 깊이 유지 |
| WS04 | 비/폭풍에서 물가와 수면을 근접·중거리로 관찰하고 지붕 아래와 비교 | 수면에 퍼지는 고리 파문이 보임. 마른 날/차폐 구역의 빗방울 없음. 물가에 일정한 청록 절단선·일정한 포말 띠가 보이지 않음 |
| WS05 | 물고기에 접근 → 상호작용 연타 → 인벤토리 확인 → 저장/로드 및 지역 재진입 | 한 개체당 물고기 1개와 획득 피드백. 잡은 개체만 사라지고 수영·숨 유지. 저장 뒤 다시 지급되지 않음 |
| WS06 | 인벤토리 수용 불가 상태/거리 밖/지형 뒤에서 잡기 시도 | 지급 실패 시 물고기 소멸·소비 기록 없음. 수용 가능 상태로 돌아오면 재시도 가능 |

회신: `WS ID / 통과·실패·미수행 / 실제 결과 / 입력·시작 상태 / 화면 또는 로그`.
