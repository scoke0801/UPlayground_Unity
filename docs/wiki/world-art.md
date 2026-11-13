# 월드와 아트

수중·강수 프레임 저하 후속(2026-11-13): [조사·개선·측정 상태](../work-status/water-rain-performance.md). 정규 호수 광학용 94셀의 파면 탐색을 최대 파도 진폭 구간으로 제한하고 연속 물 구간의 반복 병합·근평면 밖 파도 평가를 생략했다. 시계 고정/예열한 합성 Probe 중앙값은 수중18.76→2.85ms·수면2.97→1.15ms이며 게임 FPS 개선율은 아니다. 최종 EditMode32/32·정규 호수 MSAA4 렌더6조건·대상 셰이더4종 오류0을 확인했다(기존 loop 경고 보존). 실제 게임 프레임 절감량·강수 입자·입력·화면 품질은 WR01~03 사용자 테스트 대기다.

커스텀 셰이더 오버헤드 개선(2026-11-13): [조사·수정·검증·측정 명세](../work-status/custom-shader-overhead-review.md). 관리 수면 일반 패스 정점 퇴화, 구름의 기여 0/비활성 형상 계산 생략, 식생의 결과 0 정점 반복 생략, 반응 없는 표면의 패턴 생략, 수중 반사 샘플 조건화를 Shader/HLSL 5개에 반영했다. Unity GPU 전후 비교 70조건 통과·최종 식생 18조건 재검사 통과, 대상 셰이더 8종 SetPass 성공/오류0·기존 표면 EditMode4/4 통과. 새 식생 미초기화 경고는 단일 반환으로 제거했고 나머지 루프/구매 파생 셰이더 경고는 상세 기록을 유지한다. Frame Debugger 조회는 통신 실패, 최종 비활성 확인. 물 전체 화면 광학·RT/복사 구조는 유지했으며 실제 GPU 절감량·SO01~04 화면 품질은 사용자 측정 대기다.

물·비 비가시 비용 후속(2026-11-13): [조사·수정·검증 상태](../work-status/water-rain-visibility-cost.md). 완전 감쇠/차폐된 비 파문과 원경 접촉 파문의 반복 계산을 생략하고, 물 체적 frustum 제외·숨김 렌더러 제외·전용 강수 입자 Pause를 반영했다. 수면이 안 보이는 수중/강수 복구와 실제 GPU 파도/광학을 포함한 직접 assertion 5건 통과. 최종 셰이더 4종 오류 0, 런타임 셰이더 경고 0(Probe의 기존 루프 경고 2건 보존). 일시정지 Play Mode로 Test Runner 실행은 거절되었다. 실제 화면·재진입·GPU 성능은 WV01~04 사용자 테스트 대기.

## 정본

- [맵 저작](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [배치 도구](../../Assets/docs/guide/MAP_PLACEMENT_TOOL_GUIDE.md)
- [카메라 가림](../../Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md), [지형·물 실패 사례](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- [지형 아트 재작업 설계](../../Assets/docs/design/world-art/terrain-theme/WORLD_THEME_TERRAIN_REWORK_DESIGN.html)
- [NPC·환경 등 통합 인계](../work-status/living-world/I_INTEGRATION.md)
- [드롭 아이템 월드 모델 제작](../../Assets/docs/guide/DROP_ITEM_WORLD_MODEL_AUTHORING_GUIDE.md) — 실행 절차·함정은 [drop-item-world-model 스킬](../../.claude/skills/drop-item-world-model/SKILL.md), 공용 빌드 라이브러리 [dropitem_lib.py](../../ArtSource/DropItems/_lib/dropitem_lib.py)

맵은 시선·이동 선택·발견·보상을 만드는 공간이다. 기존 구매 아트·MCP 제작 원본·미커밋 씬 복구 자료를 보존한다. 셀 수·GUID 보존·Missing Script 0은 미술 인수나 실제 플레이 성공을 뜻하지 않는다.

## 전달 설계 3종

환경 생물 근거리 소멸 후속(2026-11-13): [원인·수정·AF-C1](../work-status/ambient-fauna.md#근거리-소멸-사용자-피드백--2026-11-13). 기존 화면 예산이 접근 시 군집 전체를 탈락시키는 조건을 확인해 본편 Quality SO 한도만4%/6%/8%로 조정했다. 실제 추정 함수·나비6마리·거리/화각/품질72조건의 예산 유지 검사 통과. 개체수/크기·GPU 용량 상한·비가시 반환은 유지. 실제 플레이/화면 AF-C1은 사용자 테스트 대기이며 다른 진행 중 검사로 Test Runner 재실행은 시작되지 않았다.

환경 생물 배치 조사(2026-11-13): [현재 배치 조사](../work-status/ambient-fauna.md#현재-배치-조사--2026-11-13). 열린 본편 편집 씬에서 GPU23그룹/Capacity56·CPU104/fallback17을 재조회했다. 나비는17곳에 각2마리·반경0.7m, 날벌레/반딧불/부유 입자는 각2곳의 표면 베이크와 수동 개체 수를 사용한다. 앵커 간 순회 없이 각 개체가 고정 안전 구에 머무른다. 시간·강수·전투·시야/거리 예산으로 표시가 줄어든다. Play/검사/씬 저장은 하지 않았으며 실제 화면 품질 판정은 남아 있다.

2026-11-12 설계 문서와 관련 소스를 정적으로 대조했다. [공통 연결·책임 경계](../전달용/README.md)에 원문 역할과 설계 간 계약을 정리했다. 원문 위치는 보존하며 기존 시스템 설계를 자동 대체하지 않는다.

| 설계 입력 | 분석·작업 상태 | 현재 판단 |
|---|---|---|
| [GPU 환경 생물](../전달용/ambient_fauna_design/01_환경생물_정규설계.md) · [역할·백엔드 ADR](../../Assets/docs/design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md) · [저작](../../Assets/docs/guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md) | [상태·충족표·최종 근거](../work-status/ambient-fauna.md#최종-인계--2026-11-13) | 전용 Gnat/Firefly·종별 행동/캡슐·지형 변경 억제·셀 준비 수명·볼륨/표면/제외/stale 베이크·품질/화면 예산 반영. 본편23그룹/Capacity56(나비34·폭6~10cm 유지), CPU104/fallback17 보존. 현재 비가시 자원 수정 후 EditMode22/22·PlayMode5/5 통과, 이전 저장/재로드·Undo/Redo 기록 보존. 실제 TPS/야간 최종 미술·지역 왕복·실기 성능은 사용자 대기 |
| [반응형 환경](../전달용/environment_system_design_v1/environment_system_design_v1.html) | [상태](../work-status/reactive-environment.md), [통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md), [저작](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md), [사용자 테스트](../work-status/reactive-environment-tests.md) | 일반 개체의 비파괴/명시 슬롯·채집 원장 통합, Terrain Detail 셀/범위 입력·상태 마스크·밀도·저장 연결 반영. 실제 보유 에셋 wrapper 4종(실제 벌목/채집 포함)/Detail 1종/현재 맵 preset 저작. primitive 샘플 전용 21개+meta 폐기. 기존 Unity 컴파일 오류 0·EditMode 77/77 근거 보존. 사용자 연출 개선 요청으로 후속 설계 작성, 구현 미착수. 실제 아트·벌목/KCC·Nav·대량 성능은 인수 미완료 |
| [툰 워터 월드](../전달용/toon_water_world/toon_water_world_implementation_design.html) | [상태](../work-status/toon-water-world.md), [통합 계약](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md) | 단일 합성·반잠수·Fog 중복 방지·파도/접촉/비·폭포를 구현하고 정규 호수에 연결. 기존 단일 샘플 EditMode 90/90·Lab 14건·정규 4건 성공 후 사용자 MSAA 4배 불일치 오류 접수. 색/깊이 샘플 유지 수정 후 1/2/4배 Lab 42건·정규 4배 2건 통과, Console 오류0. 사용자 실제 플레이 재확인은 WU07 대기. 실제 입력·미술·실기 성능은 사용자 테스트 대기. GPU 생태계·원자 수중 채집은 별도 후속 |

세 문서의 초기 저장소 조사 전체가 완료된 것은 아니다. 초기 문서 분석 시점에는 구현·아트 제작·Unity 테스트·Player 빌드를 실행하지 않았다. 이후 구현과 검증은 각 상태 문서의 현재 판정을 따른다. 상세 남은 조사와 검증은 각 상태 문서에서 관리한다. 원문·상태·위키의 로컬 링크 80건 대상 존재를 확인했고 누락은 없었다. 임시 산출물은 없으며 전달 원문·도식·템플릿과 기존 사용자 변경은 보존했다.

## 반응형 환경 연출 후속

2026-11-13 사용자 화면에서 수관·붉은 균열·파손 과정의 미술 개선 요청과 비 소화/연기/전파 검토를 받았다. [연출 개선 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_PRESENTATION_DESIGN.md)에 표면 역할 분리, 국소 타격/절단/낙하, 화염·연기·소화 잔연, 비 소화 목표와 실제 전파 연결을 정리했다. **이번 단계는 웹 조사·정적 검토·설계만 완료**했으며 코드/게임 에셋은 변경하지 않았다. 기존 77건 자동 검사는 미술 통과 근거가 아니다.

전파와 비 냉각은 구현되어 있지만 실제 FarmTree의 화염 연결은 null이며 전용 연기 단계가 없다. 현 프로필은 약한 비에서 불이 유지될 수 있다. FarmTree/TerrainGrass의 전파 layer는 모두 0으로 호환되지만 실제 나무↔풀 전파는 셀 거리/차폐/열 수지 확인이 남았다. 화면의 실제 강수/차폐/열원은 미확인이다. [조사 근거·수식 검산·후속 순서](../work-status/reactive-environment.md#연출-개선-설계--2026-11-13)를 기준으로 구현을 이어간다. 실제 화면/벌목/KCC/Nav/성능 인수와 물 MSAA/RenderGraph의 현재 실행 확인은 남아 있다.

## 실제 작업 상태

반응형 환경 일반화는 [설계 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#일반-씬-오브젝트식생-확장-검토안)과 [상태](../work-status/reactive-environment.md)를 기준으로 한다. 개체 PropertyBlock·명시 피격/슬롯·채집 단일 소유권과 Terrain당 셀 어댑터를 구현했다. 현재 LakeVillageWorld의 실제 TerrainData/Tree 0·Detail 인스턴싱 경로를 조사하고 16셀(원본 밀도 합 367) 저작 preset을 만들었다. 정규 씬 자동 교체/저장·Terrain Tree 인계·전체 월드 성능 완료를 주장하지 않는다. 샘플 전용 자산/생성기 21개+meta와 전용 지붕 규칙을 폐기했고 공유 런타임/툰·구매 원본·역사 검증 결과는 보존했다. 최종 Test Runner 77/77(반응/통합/표면/치트/충돌/payload/투사체 데이터)·wrapper 4/4 실제 로드/원본 재질 보존/Missing Script 0을 확인했다. 새 검증과 사용자 대기는 [최종 판정](../work-status/reactive-environment.md#일반화-최종-판정)에서 구분한다. 치트는 사용자가 지정한 실제 프리팹만 시험한다. 물 MSAA/RenderGraph의 현재 플레이 재현 여부는 별도 사용자 확인 대상이다.

| 항목 | 진행 근거 | 현재 판단과 다음 작업 |
|---|---|---|
| 테마 독립 제작 | A00 체크포인트(2026-11-13 output 정리로 삭제) | 원문 중간의 ‘최신 재개 기록’에 사용자 제작 중단과 미술 실패가 명시됨. 앞부분의 ‘제작 중’, 과거 Editor lease를 새 작업의 권한으로 쓰지 않음. 재작업 설계와 현재 기준선을 확인하고 재개 여부부터 판단 |
| 채석장·고원·항구 정규 통합 | ABC 결과·테스트 명세(2026-11-13 output 정리로 삭제) | 정규 씬/전용 자원 통합 기록. 미술 인수·실제 TPS·성능 미완료. 장거리 6조회 중 3 Partial 및 기존 누락 참조/빈 재질을 후속 문제로 유지 |
| 강 유역 월드 | 제작 결과·검사 결과(2026-11-13 output 정리로 삭제). 교훈은 [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)에 남음 | Boot Ready와 가림 규칙 8/8 기록. 입력 기반 검증 아님. 미술 NO-SHIP/사용자 인수 대기, 서비스 경고·Localization 오류 기록. 결과 표의 한 항목만 보고 전체 통과로 판단하지 않음 |
| 드롭 아이템 월드 모델 (22종) | [MANIFEST(상태·프롬프트 전문·검수 표)](../../ArtSource/DropItems/MANIFEST.md), 스크린샷 `ArtSource/DropItems/*/Review/` | 2026-11-12 기준 22종 전부 `ItemSO.worldPrefab` 연결. 실제 드롭 경로(`SpawnItems`)로 등장·정규화(긴 변 0.55m)·획득을 확인했고, 위치 지정·라이브 카메라 캡처로 낮·밤·회전 검수까지 마쳤다. 드롭 관련 EditMode 13/13 통과. 사용자 확인이 남은 결정: Item_Gem·Eye는 사용자 판정으로 1차 형태 유지, HP 포션 5종은 메시를 공유해 아이콘별 병 모양 대신 희귀도 색으로만 구분, 경험의 물약은 원뿔 병, 소금 자루는 아이콘 기준 가방 형태, 구운 고기는 세운 자세. 8m에서 작게 보이는 문제(깃털·광석·열쇠)는 공통 `tokenSize` 문제. 펫 외 자동 수거 경로는 범위 밖이라 미조사 |
| 본편 OpenWorld 개편·보수 | 기록·작업 단위(2026-11-13 output 정리로 삭제) | 현행 씬·지역 데이터가 기준선이다. 과거 기록을 기준선 선택 근거로 쓰지 않음 |

## 아트 인수

### Non Humanoid 액터 툰 셰이더 전환 검토 (2026-11-13)

[전환 작업 상태](../work-status/non-humanoid-toon-migration.md): 실제32개를 재조회한 뒤 일반28개·Stone Black/Normal2개에 프로젝트 소유 복제 재질32개를 적용했다(913슬롯). Dryad 유지, Inferno는 시간에 따른 용암 UV 타일링을 잃지 않도록 원본 Graph 유지. Raon·Jun 동일 조명 대표4종 낮/밤/역광 전후24장과 Stone/Dryad·정규 월드 비교를 남겼다. 원본/meta100개와 재질 외 직렬화32개 보존, 재로드 누락0, 관련 EditMode18/18·복제 재질32개 피격 원복/전투 재질 생성 API 검사 통과. 실제 이동·공격·LOD·풀 왕복·근접 디더·실기 성능·최종 미술은 사용자 테스트 대기다.

[검토·대상 목록·후속 검증](../work-status/non-humanoid-toon-shader-review.md): 열린 Unity에서 몬스터 32개 프리팹의 실제 재질을 조회했다(카메라 시험 2개·허수아비 포함). 일반 URP/Lit군 28개는 lilToon 전환 가능, Dryad 1개는 이미 lilToonMulti, Stone Golem 3개는 용암/눈/거미줄 전용 Graph 합성 이식이 필요하다. 리그의 Humanoid 여부와 외형 분류는 다르며 Ent/Skeleton/Golem도 포함했다. 기존 계층 변환기는 공유 원본과 VFX까지 변경할 수 있어 프로젝트 소유 복제 재질과 몸체 슬롯의 명시 교체를 권장한다. **검토만 완료**했으며 게임 에셋·코드는 변경하지 않았다. 교체 렌더·전투 피드백·성능·최종 미술 인수는 미수행/사용자 테스트 대기다.

[물 사용자 피드백 후속](../work-status/water-swim-interaction-fix.md): HUD·수영·물고기30개체 연결의 기존 검증은 보존한다. 물가 포말 변주는 사용자 실제 화면에서 인위적인 흰 띠로 실패했다. 수심만으로 만드는 해안 포말 코드를 제거하고 실제 접촉/강수 파문만 유지하도록 수정했다. 셰이더 오류0(기존 경고2)·MSAA4 눈높이/높은 시점 실제 렌더2건에서 띠/반복 굴곡 제거 확인. 실제 비·TPS 구도의 사용자 물가 미술 재확인 대기.

자연스러운 물가 일렁임 후속: GPU Gems·Catlike Coding 조사 후 정규 파도 SO를 주기9.58/7.61초·상이한 방향·높이 상한±7cm로 저작하고 시각 메시104,025정점/207,328삼각형을 연결했다. 실제 높이장이 지형과 만나는 물선을 움직이며 흰 경계 띠는 복구하지 않는다. 보간 오차0.75cm·관련 EditMode25/25·MSAA4 위상5건 통과, 실제 지형6횡단선에서12초간 물선 이동29~88cm 확인. 실제 TPS·비·미술·실기 성능은 사용자 WS07 대기. [조사·적용·검증 상태](../work-status/water-swim-interaction-fix.md#물가-실제-일렁임--웹-조사와-적용).

제작 출처·원본·Unity 산출물과 meta를 식별하고, 작업에 필요한 스케일·피벗·노멀·UV·재질·충돌·LOD를 확인한다. Blender 검수와 Unity 적용 검수를 구분한다. 실제 TPS 화면·조명·카메라 가림·동선은 사용자 확인을 남기며 수치 통과로 대체하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `월드·아트` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)
수면 경계가 약하고 수중에서 비가 보인다는 사용자 피드백에 따라 기존 재질 색/불투명도 복구와 날씨 수중 차폐를 반영했다. 관련 EditMode 24/24·MSAA 1/2/4배 렌더 51건·정규 호수 눈높이/높은 시점/잠수 3건 통과, Console 오류0. [최종 화면·WU08](../work-status/toon-water-world.md)의 실제 비/잠수 전환과 미술 인수는 사용자 확인 대기다.

환경 생물 후속 구현(2026-11-13): [후속 작업 상태](../work-status/ambient-fauna.md#최종-인계--2026-11-13). Blender 전용 곤충 메시·종별 행동/캡슐 반응·표면/볼륨/제외/stale 베이크·품질/화면 예산·기존 지역 준비 수명 연결을 반영했다. 본편23그룹/Capacity56(나비34 유지), CPU104/fallback17·Missing Script0을 Unity에서 확인. 실제 화면 근거와 Editor 단기CPU 측정은 상태 문서 참조. 현재 EditMode22/22·PlayMode3/3·저장/재로드·지형 변경 억제 검사 통과. 실제 TPS·야간 플레이·장시간/실기 성능·미술 인수는 사용자 테스트 대기.
환경 생물 궤도 무늬 후속: 사용자 화면의 8자/고리 실패를 접수하고 공통 삼각함수 목표와 초기 고리 분포를 제거했다. 검증 씬은2/4/3/4개로 수정했고64개 독립 GPU 회귀 포함 PlayMode4/4 통과, 실제 Unity 출력에서 공통8자/선이 사라짐을 확인했다. 실제 움직임의 최종 미술 판단은 사용자 대기다. [후속 상태](../work-status/ambient-fauna.md#궤도-무늬-사용자-피드백-후속--2026-11-13).

### 반응형 환경 연출 구현·검증 (2026-11-13)

[연출 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_PRESENTATION_DESIGN.md)와 실제 구매/프로젝트 자산을 대조해 잎 균열·소거 정책, 약한 비 프로필, 진단, 풀 기반 화염/연기와 Terrain 패치, 공용 타격/가속 낙하를 연결했다. [현재 상태](../work-status/reactive-environment.md#연출-구현-최종-검증--2026-11-13)에서 현재 소스 Unity 컴파일·셰이더 오류 0, 관련 EditMode 64/64와 실제 원본/수정 전후의 LOD preview 근거를 확인한다. 원본·기존 미커밋·권위 상태/전파/저장/보상은 보존하며 실제 미술·비 소화·전파·벌목/KCC/Nav·실기 성능은 사용자 테스트 대기다.

환경 생물 비가시 자원 검증: draw만 frustum cull하고 GPU는 계속 돌 수 있던 경로를 확인했다. 실제 가시성 heartbeat와 초기 할당 억제·2프레임 유예 후 버퍼 반환·CPU fallback 비활성 유지·예산 후보 제외를 반영했다. 실제 카메라 검사에서 그룹 buffer0B·비가시30프레임 추가Dispatch0·재진입/일시정지/거리/layer/CPU 복구 통과, EditMode22/22·PlayMode5/5 통과. 호스트 CPU 관리·shared 자산 메모리·벽 뒤 frustum 내부 비용은 별도다. [비가시 검증](../work-status/ambient-fauna.md#비가시-자원-검증--2026-11-13).

실제 MCP 실행 후속(2026-11-13): [실행 상태](../work-status/reactive-environment.md#mcp-실제-실행-검증--2026-11-13). 초기 등록/원본 비교 촬영 후 실제 고정 갱신 계약 누락으로 화염·연기 미생성을 발견했다. 수정 전 근거를 보존하고 같은 조건의 재검증을 진행한다. 과거 preview와 구분한다.

MCP 재검증 진행: 고정 갱신 등록 수정 후 실제 Play에서 화염/연기·강수5단계 소화와 거리/바람/벽10회 전파를 관찰했다. [실행 상태](../work-status/reactive-environment.md#mcp-실제-실행-검증--2026-11-13)의 현재 근거와 미수행을 따른다. preview를 실기 인수로 승계하지 않는다.

실제 실행 인수 후속: [MCP 검증](../work-status/reactive-environment.md#mcp-실제-실행-검증--2026-11-13)에서 Terrain 양방향·저장/보상·낙하/fallback·수명 근거를 확인한다. 고정 tick 누락에 이어 카메라200m 화면 안 효과 대여 유지 문제를 재현해 수정하고 있다. 자동검사와 미술·실기 성능 판정은 분리한다.

### 반응형 환경 MCP 실제 실행 최종 (2026-11-13)

[최종 실행 상태](../work-status/reactive-environment.md#mcp-실제-실행-검증--2026-11-13)·[사진/개별 판정](../../output/ReactiveEnvironment/Live-20261113/REPORT.md): 실제 강수5회, 차폐/열원6회, 거리/바람/벽10회, 실제 Terrain 양방향, 피격/채집 경계·낙하4조건, 정식 저장/로드·보상1회, 수명·36대상 예산을 관찰했다. 고정 tick 미등록과 카메라200m 효과 유지 결함을 수정하고 같은 조건으로 재검증했다. 최신 관련 EditMode38/38, 최종효과0·임시객체0·정규씬 dirtyFalse. 실제 입력·KCC/Nav·깊은 홈 메시·최종 미술·실기/장시간 성능은 미완료다. 구매 원본·meta·기존 미커밋·다른 작업 변경은 보존했고 빌드·커밋하지 않았다.

인계 주의: 최종 Play 종료에서 PetManager.Despawn150 예외1건을 별도 기록했다. 환경/물 렌더 오류로 해석하거나 Console 전체 오류0으로 보고하지 않는다. [실행 보고서](../../output/ReactiveEnvironment/Live-20261113/REPORT.md) 참조.

### 정규 월드 연결 확장 설계 추가 (2026-11-13)

[확장 통합 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#정규-월드-연결-확장-설계)·[작업 상태/단계](../work-status/reactive-environment.md#정규-월드-연결-확장-설계--2026-11-13)·[RE-W/F/L 인수 명세](../work-status/reactive-environment-tests.md#정규-월드-연결-확장-인수): 씬 환경 모델의 전수 전환과 배치/재생성·ID 보존, 유효한 불 부여 Effect→공통 공격 열 snapshot, 지역 날씨의 실제 낙뢰 접점→중앙 단발 열 입력을 설계했다. 기존 단일 반응·전파·날씨·GAS·저장/보상·FX 풀을 유지한다. **설계 추가만 완료, 전수 조사/정규 씬 적용/새 데이터·코드 구현/플레이 검증은 미착수 또는 미수행**이다. 이전 MCP 사진과 자동 검사를 신규 연결 완료로 승계하지 않는다.
