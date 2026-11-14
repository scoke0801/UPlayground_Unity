# 반응형 환경 — 작업 상태

> 최신 설계 추가: [정규 월드 연결 확장](#정규-월드-연결-확장-설계--2026-11-13). 씬 환경 모델 전체 전환·불속성 부여 점화·날씨 낙뢰 점화를 후속 구현 범위로 정했다. 실제 실행 근거는 [MCP 검증](#mcp-실제-실행-검증--2026-11-13)을 유지하며, 이번 설계 추가를 실제 씬 적용이나 새 플레이 검증 완료로 승격하지 않는다.


- 설계 입력: [전달 설계](../전달용/environment_system_design_v1/environment_system_design_v1.html)
- 실제 정본: [통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md), [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)
- 인계: [사용자 테스트](reactive-environment-tests.md), [월드 위키](../wiki/world-art.md)
- 현재 판정: **실제 Play 실행·사진·수지 로그를 확보하고 결함 2건 수정/재검증을 마쳤다. 최신 관련 EditMode 38/38 통과. 실제 사용자 입력·KCC/Nav·깊은 절단 홈 메시·최종 미술·장시간/실기 성능은 미수행 또는 사용자 테스트 대기다.**
- 전체 P0~P6 릴리스 완료나 정규 월드 전체 자동 전환을 뜻하지 않는다.

## 반영한 실행 경로

| 영역 | 실제 반영 |
|---|---|
| [HitPhase 환경 입력](../../Assets/02.Scripts/Data/Environment/EnvironmentHitPayload.cs) | 명시적 Generic/Slash/Blunt/Explosion·Heat/Water. 플레이어/잔상/몬스터/조합형·레거시 투사체 복사와 불변 HitRequest에 보존. 기존 다중 발사 분할 도구의 원소 총량도 보존 |
| [반응 코어](../../Assets/02.Scripts/Core/Environment/EnvironmentReaction.cs) | 수분 우선, 발화/소화 히스테리시스, 연료/그을림/구조 독립, 타격 종류 배율, 외부 그을림, impulse/rate 분리 |
| [지속 공급원](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/EnvironmentElementEmitter.cs) | 논리 rate·범위·수명. 짧은 마지막 수명도 적분, 매 tick dedup 키를 쌓지 않음 |
| [공간 전파](../../Assets/02.Scripts/Manager/Save/WorldStateManager.EnvironmentSources.cs) | 재사용 bucket 공간 해시, 거리/바람/벽/층/맵 필터, 새 발화는 다음 tick부터. tick 시작 상태로 입력을 확정한 뒤 표현 커밋 |
| 날씨/차폐 | 기존 EnvironmentController/Effects 사용. 활성 원형/잔해별 표면 샘플, 별도 열 중심, 자체 Collider 제외. 전역 젖음 작성자는 추가하지 않음 |
| [파괴 레시피](../../Assets/02.Scripts/Data/Environment/EnvironmentDestructionRecipeSO.cs), [실행](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/ReactiveEnvironmentDestruction.cs) | 사전 파손 형상·낙하 캡슐 substep/후보 가림·막힘 안전 정착·짧은 붕괴 fallback·소유 Collider/차폐/NavMeshObstacle 전환 |
| [월드 상태](../../Assets/02.Scripts/Manager/Save/WorldStateManager.Environment.cs) | RegionOwnership+SceneEntityId, 고정 화학/물리 갱신, 충돌/차폐/Nav revision 분리, pending 보상과 기존 sourceId 원장, 구버전 호환/유효성 확인 후 일괄 복원 |
| [저장 DTO](../../Assets/02.Scripts/Data/Save/ReactiveEnvironmentSaveEntry.cs) | 열/물/지속 물 pending·화학 잔여 시각·파괴 진행/방향·보상 확정. 잔해는 한 논리 엔티티의 연료를 이어 받아 중복 재충전 없음 |
| [커스텀 툰](../../Assets/06.Shaders/Environment/ToonReactive.shader), [공유 HLSL](../../Assets/06.Shaders/Environment/EnvironmentSurface.hlsl) | Wet/Char/Crack/Burning, 노멀·명암/간접광·Forward+ 추가 광원/그림자·젖음 하이라이트·연소 경계 발광·Fog. Body/Foliage/Interior 재질 |
| [표면 바인더](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/ReactiveEnvironmentSurface.cs) | 모든 LOD/slot을 묶는 runtime material lease, 원본/키워드 보존 및 반환, 누락 LOD/미지원 shader/잘못된 coverage 검사. LegacyTint 명시 fallback 유지 |

툰은 ForwardOnly·외곽선·ShadowCaster·DepthOnly·DepthNormalsOnly·DepthNormals·MotionVectors·Meta **8개 패스**다. 알파·연료 coverage·바람·LOD 함수를 공유하며 이전 프레임 바람도 기존 단일 날씨 작성자가 공급한다. GPU는 상태를 표현하며 CPU 피해·연료·보상을 변경하지 않는다.

기존 Ability/Payload의 새 원소 입력 기본값은 0이다. 기존 스킬의 이름/색을 보고 열·물을 임의로 채우지 않았다. 실제 스킬별 입력은 저작 지침의 GAS 필드에서 명시한다. 공유 Motion/AbilitySet과 기존 구매 프리팹/정규 씬을 일괄 변형하지 않았다.

최종 회귀에서 플레이어·몬스터·잔상·투사체의 물리 조회에 환경 레이어를 공통으로 포함했다. 조준/탐지용 액터 필터는 보존한다. 잘못된 환경 저장을 읽을 때 기존 처치/소비/리스폰 진행을 지우기 전에 유효성을 검사하도록 복원 순서도 보강했다.

## 실제 자산

샘플 생성기 (폐기된 역사 산출물)는 기존 상자를 툰으로 연결하고 아래 4종을 추가했다. Unity primitive와 생성 leaf mesh (폐기된 역사 산출물)가 원본이며 최종 환경 아트가 아니다.

| 프리팹 | 연결 내용 |
|---|---|
| 상자 (폐기된 역사 산출물) | 직접 파괴/화재 분기, 장작 1개 또는 자원 소진, 툰/전파 |
| 나무 (폐기된 역사 산출물) | 2 LOD·13 Renderer, 밑동/절단면/통나무/수관, 방향성 낙하/fallback, 소유 Nav 장애물, 원형/잔해의 열·비 샘플 |
| 바위 (폐기된 역사 산출물) | 검 저항·둔기 피해 배율·그을림·사전 파손 조각·기존 돌 보상·Nav 해제 |
| 지붕 (폐기된 역사 산출물) | 실제 Ground 지붕 Collider와 별도 명중 proxy·보호 볼륨·파괴 시 차폐 해제. 카메라 이름 규칙도 등록 |
| 풀 패치 (폐기된 역사 산출물) | 한 패치의 연료/전파, 양면 leaf mesh·바람·소진 시 명시적 잎 coverage 소거 |

정의 5개·파괴 레시피 3개는 Assets/10.Datas/Environment, 툰 재질 5개는 Assets/11.Material에 있다. 프리팹 원본 GUID는 비워 두며 배치별 기존 SceneEntityId 발급을 사용한다. Git ignore 대상인 프리팹도 meta와 함께 로컬 인수 대상이다. 정규 씬은 이 작업에서 저장하지 않았다.

## 폐기 전 구현의 검증 기록

Unity **6000.3.21f1 / URP 17.3.0 / Direct3D11 / RTX 3060 Ti** Editor에서 확인했다.

| 검사 | 실제 결과 |
|---|---|
| C# 컴파일 | 최종 오류 검색 0, 관련 타입 로드 성공 |
| EnvironmentReactionTests | 17/17 통과: 원소 순서·저항·연료/그을림/구조·중복·지속 물 계수/저장 등 |
| ReactiveEnvironmentIntegrationTests | 9/9 통과: 전파 지연/층, 짧은 물 공급, 파괴 포즈, 지붕/차폐 revision, pending 보상·JSON 복원·언로드·구버전·실패 복원·오류 로드 시 기존 월드 진행 보존 |
| ReactiveEnvironmentSurfaceTests | 4/4 통과: 8패스/노멀/LOD/Forward+ 변형 컴파일, GPU 젖음/소화 발광/잎 소거, 원본 보존/모든 슬롯 lease/누락 LOD |
| EnvironmentHitPayloadTests | 2/2 통과: HitPhase→공격 복사→불변 요청→잔상 복사·환경 조회 레이어 보존 |
| 기존 ExplicitCollisionDetectionTests | 23/23 통과: 다중 Collider·명시적 타격 회귀 |
| 샘플 인스턴스 로드 | 5/5 정의·전체 저작 구성 유효, Missing Script 0, 프리팹 배치 GUID 비어 있음, 지붕 카메라 규칙 유효 |

**합계 55/55 통과, 실패/건너뜀 0**, job: 18e68c4c978e42a6a537683d885afc74.
원본은 최종 테스트, 자산 검사, 그래픽 정보/셰이더 오류다. 초기 단면의 41건 결과는 이전 기록으로 보존한다.

마름 · 젖음 · 그을림 · 연소는 64px GPU 평면 캡처다. 전방 표면 반응은 픽셀로 검사했고 패턴·발광 구분도 확인했다. 실제 월드의 조명·그림자·바람/모션·카메라·형상 아트 인수를 대신하지 않는다.

중간 테스트는 domain reload로 결과 본문을 받지 못한 실행이 있어 통과 근거에서 제외했다. 기존 단언 직접 호출은 보조 근거였고, 최종 55건은 Test Runner의 실제 개별 결과가 있는 실행이다. 작업 시작 시 셰이더 생성 전부터 URP Blitter 중복 초기화 예외를 관측했다. 이 작업에서 그 원인을 수정하거나 전체 월드 렌더러 문제가 해소됐다고 판정하지 않았다.

## 사용자 확인과 범위 경계

[RE-01~15](reactive-environment-tests.md)는 사용자 테스트 대기다. 실제 조작/전투 감각·낙목과 KCC 접촉·Nav carving 지연·지붕 파괴 후 비·아이템 획득/슬롯 파일 왕복·최종 맵 아트와 CPU/GPU/메모리 성능은 미확인이다. Player/Addressables 빌드는 실행하지 않았다.

현재 구현은 등록된 소품/풀 패치의 실행 경로다. 전체 콘텐츠 closure의 Managed/Exempt 빌드 게이트, 지형 대량 셀/공간 젖음 atlas, 인접 맵 전파, 독립 자식 논리 엔티티 분배, 원거리 proxy/날씨 이력 catch-up/영속 입력 dedup, 낙목 접촉 피해·최종 VFX/아트·P6 릴리스 인수는 별도 범위다. 언로드/시간 점프는 명시한 SuspendDomain 정책이며 대규모 월드 성능 검증 완료가 아니다.

## 단계 인계와 부산물

후속 준비표는 output/harness/20261112-205710-87e15461에 있다. 단계별로 위키와 상태를 갱신했으며 최종 산출물과 메타·링크 확인은 최종 인계 기록에 연결했다. 테스트 preview scene·메모리 객체·runtime 재질은 TearDown/finally/반환 경로에서 정리했다. 초기/최종 결과 JSON·GPU 캡처·생성 자산/meta·저작 원본 C#은 재현/인수 근거로 보존한다. 기존 사용자 변경과 다른 작업의 씬·아트·물/생물 변경은 보존했다. 커밋하지 않았다.


## 폐기 전 치트 추가 기록

- [기존 치트 콘솔](../../Assets/02.Scripts/Manager/Cheat/Editor/CheatConsoleWindow.cs)에 환경 테스트 진입점을 추가했다. 기존 [환경 반응 모니터](../../Assets/02.Scripts/Editor/ReactiveEnvironmentMonitor.cs)를 확장해 새 메뉴/매니저/런타임 UI 없이 제공한다.
- Editor Play 중 5종 샘플/사용자 지정 프리팹 소환, 전방 거리·Ground 탐색, 대상 목록/Hierarchy 선택, 열·물·구조 피해·남은 내구도 파괴, 소환한 개체의 교체/제거를 지원한다.
- [소환 치트](../../Assets/02.Scripts/Editor/ReactiveEnvironmentCheats.cs)는 플레이어 소유 씬에 복제하고 활성화 전에 새 GUID를 부여한다. 원본 프리팹과 정규 배치는 보존한다. 교체도 새 GUID다. 보상과 저장 원장은 실제 경로를 사용하므로 폐기 가능한 슬롯에서 사용한다. 이미 나온 보상은 되돌리지 않으며 런타임 개체를 다음 세션에 재소환하지 않는다.
- C# 오류 검색 0. [치트 단언](../../Assets/02.Scripts/Editor/ReactiveEnvironmentCheatTests.cs) 2개를 execute_code로 직접 호출해 통과했다(ID/씬 격리·원본 보존·입력 거부). Test Runner는 tests_running으로 요청을 거절해 새 실행 결과를 얻지 못했으며 다른 실행을 중단/초기화하지 않았다. 결과 기록. 이전 55건은 앞선 통합 스냅샷의 근거로 유지하며 이번 치트 결과에 합산하지 않는다.
- [사용법](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md#환경-에셋-테스트-치트), [RE-C01~03](reactive-environment-tests.md#치트-진입-확인-re-c0103)은 실제 Play 버튼·입력·반응 확인 대기다. 게임 내 F12 패널/Development Player 치트는 추가하지 않았다. MSAA/RenderGraph 오류 수정은 이 치트 작업에 포함하지 않았다.
- 새 C# meta는 Unity가 생성했고 테스트 preview scene/임시 staging은 finally로 해제한다. 씬/프리팹/구매 원본을 저장하지 않았으며 다른 작업 파일도 보존했다. Player/Addressables 빌드·커밋 미수행. 좁은 에디터 도구 확장이므로 새 준비표를 만들지 않고 기존 상태/저작 지침/분야 위키에 인계한다.


## 일반 씬·식생 적용성 검토 (구현 전)

코드/저작 도구 정적 조사 결과, 반응 코어·명시 프로필·원소 입력·보상/저장은 일반 소품·나무에도 재사용 가능하다. 다만 원형/잔해 필수, 전용 셰이더/전체 LOD 바인딩, 물리 명중 Collider/레이어, GatheringActor 병용 금지, 재생성 배치 ID 보존 제약이 있어 단순 컴포넌트 일괄 부착으로 일반화되지 않는다.

정규 맵 지면 도구는 Terrain Detail/인스턴싱을 사용한다. 따라서 개체 소품/나무 경로와 밀집 풀의 셀 어댑터를 분리하고 반응 계산을 공유하는 방향을 권장한다. 현재 매 tick 무변경 ApplySnapshot과 객체별 재질 clone도 대량 적용 전 개선/측정 대상이다.

[설계 검토안](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#일반-씬-오브젝트식생-확장-검토안)에 대상별 적용 표·6가지 제약·3단계 도입/인수 기준을 기록했다. 당시 검토 단계에서는 런타임·씬·프리팹을 변경하지 않았다. 코드 조사와 문서 링크 확인만 수행했고 컴파일/Play/성능 검사를 새로 실행하지 않았다. 일반화 구현·대량 식생 성능·사용자 인수는 미수행이다.

## 일반화 구현 — 연결부 반영 단계 (2026-11-13)

- 준비표: [20261113-113845-66a6796a](../../output/harness/20261113-113845-66a6796a/review.md). 기존 사용자 변경을 확인하고 보존한다.
- 개체의 비파괴 바인딩과 명시 Renderer/슬롯/제외 검사, PropertyBlock 표현, 채집 입력의 환경 원장 위임을 반영 중이다. 파괴 대상만 원형/잔해/레시피를 요구한다.
- Terrain Detail 현재 경로는 LakeVillageGroundDetailTool의 인스턴싱·SetDetailLayer다. Terrain당 어댑터 하나와 순수 C# 셀로 중앙 계산/전파/저장에 연결한다. 원본 TerrainData는 런타임 복제하고 원복한다.
- 컴파일/신규 회귀 검사·실제 보유 에셋 적용·샘플 삭제는 이 단계에서 아직 미확정이다. 과거 55/55는 이 변경의 검증 근거가 아니다.

## 자산·저작 단계 인계 (2026-11-13)

실제 보유 메시/텍스처의 wrapper 3종과 Terrain용 풀 프리팹, 프로필/전용 재질, 현재 맵 16셀 저작 preset을 생성했다. [출처/목록](../../output/ReactiveEnvironment/Generalization/created-assets.txt), [저작 절차](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)를 따른다. 정규 씬은 자동 저장하지 않았다. 19,419개 일반 직렬화 파일과 대형 파일 37개에서 샘플 전용 GUID 외부 참조 0을 확인하고 21개 전용 자산/생성기와 meta·빈 메시 폴더·샘플 지붕 규칙을 제거했다. 공유 셰이더/런타임·구매 원본·역사 결과와 GPU 캡처는 보존했다. 새 코드의 추가 회귀/실제 자산 로드·아트/성능 인수는 다음 검증 단계에서 구분한다.

## 일반화 최종 판정

**구현 반영·관련 Editor 자동 검사 통과 / 실제 플레이·아트·성능은 사용자 테스트 대기**다. 정본은 [설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#일반-씬-오브젝트식생-확장-검토안), 반복 작업은 [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md), 인수는 [RE-G01~14 / RE-C01~03](reactive-environment-tests.md)을 따른다.

### 반영 범위

- 비파괴 개체는 원형/잔해 없이 기존 Renderer를 사용한다. 파괴 대상만 형상·레시피·충돌 전환을 요구한다. 반응/제외 Renderer·slot·LOD·실제 명중 Collider를 명시하고, 일반 근접과 투사체가 공용 Collider 해석을 사용한다.
- 개체 표현은 MaterialPropertyBlock으로 바꾸어 재질 clone을 제거했다. 무변경 표면/형상·Terrain mask/밀도 쓰기를 생략한다. 초기 PropertyBlock과 풀 반환 후 다시 연결한 블록도 보존한다. tick revision만으로 dirty를 판단하지 않는다.
- GatheringActor는 명시 소유권 계약을 통해 환경 내구도/원장에 입력만 위임한다. 기존 Tree의 dropItems를 재사용하고 구 consumedInteractables도 보상확정 상태로 인계한다. 환경 소진 시 IsInteracting도 false가 되어 기존 PlayerInteractionState가 채집 반복을 종료한다. 낚시/시간 재성장은 거부한다.
- Terrain당 어댑터 하나·셀당 C# 상태로 공용 계산/날씨/전파/저장을 공유한다. 근접 Sphere/회전 Box/Capsule, 정규 ProjectileRuntime 폭발/장판, 지속 열/물 emitter가 연결된다. 투사체 범위는 기존 대상별 쿨다운으로 대기 입력 중복을 막고 풀 반환 때 셀 참조를 정리한다. 잎/셀별 MonoBehaviour·Collider는 없다.
- Terrain 원본을 runtime 복제하고 Collider도 복제본을 사용한다. 셀 mask는 젖음/그을림/연소, 원본 밀도×연료 coverage는 Detail 밀도를 표현한다. 구획/해상도/레이어/원본 밀도 hash 불일치는 저장을 덮지 않고 실패·원본 복구한다. 재진입은 기존 저장을 재적용한다.
- 씬 프리팹 저작은 기존 WorldPlacementMetadata/Bake/RuntimePlacementLoader에 연결하고 기존 배치 GUID를 보존한다. 재생성기가 레코드를 버리고 새 ID를 발급하는 방식은 지원하지 않는다. 중앙 대상/저장/공급원/화염 차폐 조회 카운터와 Tick/Export Profiler marker를 제공한다. 전체 저장 순회와 runtime TerrainData 복제 비용은 실제 맵 측정이 남는다.

### 자산 폐기와 보존

[출처/생성 목록](../../output/ReactiveEnvironment/Generalization/created-assets.txt), [삭제 감사](../../output/ReactiveEnvironment/Generalization/sample-reference-audit.json), [실제 로드 검사](../../output/ReactiveEnvironment/Generalization/asset-checks.json)를 보존한다.

- 새 프로젝트 소유 산출물은 wrapper **4개**(CargoCrate/ArenaTree/FarmGrass/FarmTree), Detail 프리팹 1개, 프로필 5개·낙하 레시피 1개·현재 맵 preset 1개·재질 6개다. 자산+meta 36파일, 95,432 bytes다. 기존 메시/텍스처를 참조하며 TerrainData·메시·텍스처를 디스크 복제하지 않았다.
- 실제 벌목 FarmTree는 구매 FarmLand GenTree A의 FullTree/Stump/Trunk/Log를 중첩한다. 원본 절단 cap 부근 y=1.22m에 pivot, 배치 스케일 0.4, 길이 6.6m/반경 0.42m를 명시했다. Renderer 13개 중 mesh 표면 12개를 반응시킨다. 원본 impostor는 wrapper에서 비활성/명시 제외하고 mesh 3 LOD를 사용한다. 밑동·수관·통나무·fallback은 한 상태/보상이다. 절단면 접합·비주얼·충돌은 사용자 인수 전이다.
- 실제 LakeVillageWorld는 WorldThemesABC/Terrain.asset, Terrain 위치 (0,0,-700), Detail prototype 10개 모두 인스턴싱, Terrain Tree 0개였다. 레이어 1의 (116,102)부터 4×4셀(픽셀 크기 4), 원본 밀도 합 367을 preset으로 저작했다. 실제 Terrain 원본으로도 복제·구성·복구 및 인접 셀 발화 검사를 통과했다. 전용 TerrainGrass는 셀 간격(약 9.4×10.9m)에 맞춘 반경 16m/rate 3으로 작은 GameObject 풀 프로필과 분리한다. 정규 씬 자동 교체/저장은 하지 않았다.
- 구 샘플 프리팹 5개·정의 5개·레시피 3개·재질 5개·leaf 메시 1개·생성기 2개와 각각의 meta를 제거했다. 직렬화 파일 19,419개+대형 37개를 검사했고 제거 집합 밖 GUID 참조는 0이었다. 빈 sample mesh 폴더/meta와 sample 지붕 이름 규칙 한 항목도 제거했다.
- 공유 반응 코어·날씨/GAS/전파/보상/저장·툰 셰이더·기존 사용자/구매 원본과 meta·역사 JSON/GPU 캡처는 보존했다. 치트의 샘플 경로/자동 선택을 제거했고 테스트는 격리 fixture를 사용한다. 과거 55/55를 현재 결과에 합산하지 않는다.

### 실제 검증

Unity **6000.3.21f1** Editor에서 최종 소스 관련 **77/77 통과, 실패/건너뜀 0**. [NUnit XML](../../output/ReactiveEnvironment/Generalization/editmode-final.xml), [요약](../../output/ReactiveEnvironment/Generalization/editmode-final-summary.json)이 최종 근거다. 반응 17·통합 18·표면 5·치트 2·환경 payload 2·명시 충돌 23·투사체 데이터 10건이다.

통합 검사는 비파괴 원형/레시피 선택성, 명시 Collider, 실제 wrapper 4종의 전체 LOD/한 상태·원본 재질 보존, 채집/전투 중복 보상과 구 소비 저장 인계, Undo/기존 GUID, 배치 레코드 재생성, 실제 Terrain preset, 범위 형상 오검출, 투사체 셀 쿨다운/대기 열량 1회 적분, 밀도/저장/재진입/불일치 복구를 포함한다. 표면은 기존 8패스/노멀/LOD/Forward+ 변형·GPU 평면 검사와 PropertyBlock 보존을 포함한다. 성능 임계값이나 플레이 품질 통과 판정은 아니다.

MCP Test Runner의 완료 전 스냅샷은 67/67 통과였다([중간 JSON](../../output/ReactiveEnvironment/Generalization/editmode-before-projectile.json)). 추가 투사체 연결 뒤 MCP가 실제 Unity 작업 목록이 빈 상태에서도 tests_running을 반환했다. 실행 취소/상태 초기화 없이 기존 Unity Test Runner API로 같은 검사와 투사체 fixture를 실행해 최종 XML을 얻었다(최종 XML start-time 2026-11-13 03:47:27Z, runSynchronously=true). 다른 작업의 AmbientFauna 검사 완료를 이 작업 결과로 받지 않도록 fixture를 필터링했고, 최종 7개 fixture/77건의 XML을 확인했다. 임시 관찰자 C#/meta는 완료 후 제거했고 [소스 기록](../../output/ReactiveEnvironment/Generalization/native-validation-source.txt)만 보존한다. 불일치 저장 테스트가 예상한 오류 로그 1건은 LogAssert로 검증한 실패 복구 시나리오다.

초기에는 비활성 fixture 등록/Collider 소유 검사 실패를 발견해 수정했고 현재 최종 실행은 전부 통과했다. 다른 작업의 AmbientFauna asmdef TestRunner 중복 참조 오류도 일시 관측했으나 해당 파일을 수정하지 않았고 최종 Unity 컴파일에서는 해소되어 있었다. 실제 물 MSAA/RenderGraph·월드 렌더링 재현은 이번 Editor 검사로 확인하지 않았으며 RE-G14 대기다.

### 사용자 테스트와 미구현 경계

RE-G01~14/RE-C01~03은 **사용자 테스트 대기**다. 실제 아트·절단 접합·낙하/KCC 접촉·Nav carving/통로·패드/키보드 GAS 전투·실제 슬롯 파일 저장/재실행·대량 숲/풀 CPU/GPU/메모리·저장 크기·카메라 이동 spike를 자동 검사로 대체하지 않는다. Terrain 전체 복제/프로토타입 재질과 개체 PropertyBlock의 배칭 비용도 측정 전이다.

Terrain Tree의 자동 GameObject 인계는 미구현이며 현재 Tree 0과 구분한다. 향후 원형 표시/내장 충돌 해제 → 같은 ID/보상 원장의 단일 실물 생성 순서를 지켜야 한다. 원거리 proxy/catch-up·전체 월드 자동 부착·인접 맵 전파·독립 자식 엔티티·낙목 접촉 피해·전체 콘텐츠 게이트는 별도 범위다. 범위 없는 직격 전용 투사체의 풀 반응은 emitter/범위 입력을 저작해야 한다. 낚시/시간 재성장과 환경의 통합 정책도 별도다.

검증용 preview 객체·fixture 프리팹·runtime 재질/텍스처/TerrainData·임시 관찰자 C#/meta는 반환/정리했다. 시작 시 존재한 InitTestScene07609c79-7301-45ee-a1dc-19f869958af6과 다른 작업 변경은 보존했다. Player/Addressables 빌드·커밋은 실행하지 않았다. [분야 위키](../wiki/world-art.md)와 [준비표](../../output/harness/20261113-113845-66a6796a/review.md)로 인계한다.

최종 정리 후 [컴파일 검사](../../output/ReactiveEnvironment/Generalization/compile-checks.json)는 compilationFailed=false·셰이더 오류 0·임시 관찰자 타입 미존재였다. [산출물/소스 해시](../../output/ReactiveEnvironment/Generalization/snapshot-manifest.json) 112파일의 누락/meta 누락 0, [문서 링크](../../output/ReactiveEnvironment/Generalization/document-links.json) 122건의 경로 누락 0을 확인했다. 이 수치는 아트/성능 인수가 아니다.


## 연출 개선 설계 — 2026-11-13

### 범위와 현재 판정

사용자 제공 화면 2장과 ‘파손 과정이 어색함·비에도 소화가 보이지 않음·연기 필요·전파 검토’ 피드백을 기준으로 정적 조사와 웹 조사를 수행했다. 이번 범위는 **방향성 검토와 [후속 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_PRESENTATION_DESIGN.md) 작성까지**다. 런타임 코드·셰이더·게임 에셋·씬을 변경하거나 Unity 실행 상태를 조작하지 않았다. 추가 문서와 그 meta, 통합 계약·본 상태·분야 위키·준비표만 변경했다.

미술 판정은 **개선 필요/인수 미완료**다. 과거 EditMode 77/77 및 제한된 렌더 검사는 보존하되 현재 연출 품질 통과로 승계하지 않는다. 제공 화면의 실행 시점 heat/wet/fuel/rain/exposure와 정확한 대상 ID는 확보하지 않았으므로 해당 장면의 소화 실패를 재현·확정한 것으로 보고하지 않는다. 원본 이미지는 대화 첨부이며 저장소 캡처로 별도 복제하지 않았다.

### 코드·에셋 대조 결과

| 항목 | 정적으로 확인한 사실 | 아직 확정하지 않은 것 |
|---|---|---|
| 잎 소거/붉은 선 | [표면 셰이더](../../Assets/06.Shaders/Environment/EnvironmentSurface.hlsl)의 EnvClip은 Foliage fuel coverage를 object-space noise와 비교한다. EnvForward는 damage 균열과 char 경계 발광을 합성한다. FarmTree 잎 재질에도 crackOpacity 0.65가 있다 | 첫 화면의 검은 수관이 초기 재질 이식 문제인지 이미 탄화한 상태인지. 원본 atlas/UV/양면 법선과 모든 LOD의 실제 비교 필요 |
| 연기·화염 | [실제 FarmTree wrapper](../../Assets/03.Prefabs/Environment/Reactive/PF_ReactiveFarmTree.prefab)의 _flame은 null이다. [대상 컴포넌트](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/ReactiveEnvironmentObject.cs)는 선택적 _flame 활성 전환과 표면을 적용하며 전용 연기 단계 연결은 없다 | 사용자 화면의 정확한 프리팹/instance override. 구매 효과 후보의 URP/미술 적합성 |
| 비 | SampleRain은 연결된 weather의 IsReady/Current.Rain과 활성 rain sample 차폐 비율을 사용한다. [날씨 컨트롤러](../../Assets/02.Scripts/Gameplay/World/LivingWorld/EnvironmentController.cs)의 preview도 Current를 교체하며, [비 표현](../../Assets/02.Scripts/Gameplay/World/LivingWorld/EnvironmentEffects.cs)은 같은 sample.Rain에서 카메라 실내/수중·focus를 반영한다 | 사용자 장면의 CPU Rain, 샘플 차폐, pending 열/물, 외부 지속 열원. ‘화면 비만 있고 CPU 입력은 없다’는 원인을 확인한 것은 아님 |
| 파손 | [파괴 표현](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/ReactiveEnvironmentDestruction.cs)은 isBroken 전이의 원형/잔해와 방향 낙하·안전 캡슐 검사를 제공한다. 국소 절단 홈/칩/잔연을 포함한 완성 연출은 없다 | 실제 모델 접합, 타격 중 누적 단계, 낙하 무게와 KCC/Nav 접촉 품질 |
| 전파 | [중앙 source 계산](../../Assets/02.Scripts/Manager/Save/WorldStateManager.EnvironmentSources.cs)은 tick 시작 Burning 대상, 공간 해시, 거리/바람/차폐, 같은 map/layer, 자기 owner 제외를 사용한다. 새 점화는 다음 tick부터 전파한다 | 실제 맵의 등록 대상 비율과 샘플 거리/차폐. 모든 장식 나무에 번지는 기능으로 해석하면 안 됨 |
| 표면 구획 | FarmTree의 spreadRadius=5/rate=1/layer=0. TerrainGrass는 radius=16/rate=3/layer=0으로 셀 간격에 대응한다 | 두 프로필의 surfaceLayerId는 호환된다. 실제 Tree↔풀 전파는 셀 대표점 거리/차폐/열 수지에 따라 다르며 런타임 확인은 미수행. Detail 레이어 번호를 전파 layer로 혼동하지 않음 |

### 비·전파 수식 검산

[반응 코어](../../Assets/02.Scripts/Core/Environment/EnvironmentReaction.cs)와 [FarmTree 프로필](../../Assets/10.Datas/Environment/Profiles/FarmTree.asset)을 대입한 독립 JavaScript 수식 검산이다. **Unity 테스트/실행 재현이 아니다.** dt=0.2, 완전 노출, 초기 heat=1/wet=0/fuel=60/Burning, 외부 열·물 없음, 현 프로필 고정 조건이다. 수분은 clamp(wet + dt × (0.35R − 0.02 − 0.08heat)), 열은 clamp(heat + dt × (selfHeat − 0.25heat − 0.7R − 0.6wet))이며 wet≥0.6 또는 heat≤0.25에서 소화한다. selfHeat는 연소 중 wet<0.6일 때 0.6이다.

| Rain R | 첫 연소 종료 예상 | 해석 |
|---|---|---|
| 0 / 0.1 / 0.25 | 연료 소진까지 약 30초 유지 | 약한 비는 고열 증발/자기 발열을 이기지 못해 wet이 쌓이지 않을 수 있음 |
| 0.5 | 약 6.4초 | wet 약 0.602, heat 약 0.182에서 소화 |
| 1 | 약 1.8초 | heat 약 0.228, wet 약 0.497에서 소화 |

이 결과는 ‘비 소화가 미구현’이라는 결론을 반박하지만 화면의 비 강도/대상 노출이 위 조건이었다는 증거는 아니다. 연료 소진 시각은 부동소수·substep 경계에 따라 달라지며 exact assertion으로 쓰지 않는다. 파괴 뒤에도 동일 상태/열원이 유지된다는 가정이며 모델/샘플 전환·지역 수명은 포함하지 않았다. 약한 비에서 작은 불도 점차 꺼지게 할 후속 목표와 현 수치가 다르므로 프로필/수분 수지를 조정할 필요가 있다.

추가로 무풍·차폐 없음·같은 map/layer·수신 heat=0/wet=0·외부 비 없음·heatMultiplier=1, 충분히 오래 타는 단일 source에서 공급열 q=1−d/5, 냉각=0.25heat다. 거리 3m는 약 2.2초, 4m는 약 6.6초에 발화열 0.65 도달을 예상한다. 4.5m는 열 평형 0.4여서 단독 점화가 안 된다. **반경 안이라고 항상 번지는 것은 아니다.** 실제 수신 샘플 거리·바람·젖음·원 source의 연소 종료에 따라 달라진다. 이 수치는 이전 테스트 통과를 대신하는 새 자동 검증 결과가 아니다.

### 기존 효과 후보와 보존 정책

파일 존재를 확인한 재사용 후보는 Polyart SharedResources/Effects의 P_Smoke, P_Smoke_Long, PS_ChimneySmoke, P_CandleFlame, FreeQuickEffectsVol1/Prefabs의 vfx_Flames_01, Cartoon FX Remaster의 CFXR Smoke Source 3D다. [보유 환경 효과 폴더](../../Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Effects)와 [보유 FX 폴더](../../Assets/ExternalAssets/FX)를 우선 조사한다. 이름/존재만 조사했으며 환경 연소에 적합한 효과로 인수하거나 연결하지 않았다. 원본 수정 없이 프로젝트 소유 variant와 기존 풀/수명 서비스의 적합성을 후속 확인한다. 목재 칩·낙엽·절단면 보완 제작 필요 여부도 실제 모델과 후보 검수 후 결정한다.

이번 세션은 자산 폐기/제작을 하지 않았다. 앞선 primitive 샘플 폐기와 원본·공유 런타임 보존 판정은 유지한다. 새로운 샘플 콘텐츠를 도입하지 않는다.

### 후속 순서·검증과 인계

설계의 P0 기본 수관/상태 진단 → P1 소화·전파 수지 → P2 화염/연기/잔연 → P3 타격/벌목 → P4 실제 배치/성능 인수 순서다. 제안한 강수 소화 시간과 약한 비 정책은 아직 구현/플레이 검증되지 않았다. 상세 인수 조합은 새 설계에 있으며 구현 후 [사용자 테스트 명세](reactive-environment-tests.md)에 실행 가능한 절차로 추가한다.

이번 검증은 소스/에셋 정적 대조, 1차 웹 자료 조사, 명시 조건의 수식 검산, 문서 4개의 로컬 링크 122건 경로/Markdown 앵커 누락 0, 신규 문서 meta 존재 및 UTF-8 대체문자 0 확인이다. Unity 컴파일·EditMode·Play Mode·렌더링·실기 성능 검사는 설계 전용 범위이므로 **미수행**이다. 실제 벌목/KCC/Nav·비 소화·연기/전파·아트 품질은 후속 구현 후 사용자 테스트 대기다. 물 MSAA/RenderGraph의 최근 별도 수정 기록은 [물 상태](toon-water-world.md)를 따르며 이번 화면에서 오류가 남았는지/해결됐는지 확인하지 않았다.

[월드 위키](../wiki/world-art.md)와 [준비표](../../output/harness/20261113-132204-2ac83e3c/review.md)로 인계한다. 임시 테스트/캡처는 만들지 않았고 준비표는 문서 변경 범위와 검증 한계의 근거로 보존한다. Player/Addressables 빌드·커밋은 실행하지 않았다.

## 연출 구현 — 진단·연결 단계 (2026-11-13)

기존 미커밋 변경을 보존하고 연출 설계를 실제 코드·로드한 에셋과 대조했다. Unity는 시작 시 AmbientFaunaTest Play였으며 임의 종료하지 않았다. 이후 Edit 상태를 확인한 뒤 같은 Editor에서 컴파일했다. `output/ReactiveEnvironment/Presentation/baseline-status.txt`와 수정 전 프로젝트 자산 사본을 보존한다. 구매 원본·씬은 저장하지 않는다.

- FarmTree 원본 Close/Far 잎은 같은 atlas와 별도 LeavesGraph 색/법선/역광/바람 설정을 사용한다. wrapper는 atlas와 UV를 유지하지만 원본 조명과 동등하지 않다. 잎 crackOpacity=0.65, 연료 noise 소거와 발광이 확인됐다. 잎 역할에서 균열/경계 발광을 차단하고 FarmTree의 두 잎 재질은 coverage를 끄며 외곽선을 제거했다. 탄화는 잎 전체 색으로 표현한다. 초기 검은 수관과 실제 char 상태는 원본 화면 수치가 없어 확정하지 않는다. 모든 LOD의 실제 미술 비교는 대기다.
- FarmTree/TerrainGrass는 둘 다 surfaceLayerId=0이다. Detail 레이어 1과 별개다. tick 시작 source/다음 tick 전파·거리·차폐·바람 계산은 유지하고 pending 입력/최근 공급량/반경 내 source의 거리·map·layer·차폐 진단을 추가했다. 열 송수신 앵커는 늘리지 않았다. 새 burn anchor는 VFX 전용이므로 공급 열 총량이 변하지 않는다.
- 코어 수식 변경 없이 가연 프로필 4종의 rainAbsorption=0.7, drying=0.005, evaporation=0.015로 약한 비 수분 수지를 조정했다. Unity 회귀와 실제 비 소화 인수는 다음 검증 단계에서 별도로 기록한다.
- P_Fire는 URP였고 P_Smoke/먼지는 레거시였다. 프로젝트 복사본의 URP 재질·입자 상한·world-space·무자동재생을 저작했다. 기존 GameObjectManager FX 갱신에서 대여/자연 반환을 처리한다. 파일 존재만으로 미술 인수하지 않는다.
- 같은 snapshot의 연소 표현, 명중/채집 공용 피드백, 시각 자식 반응, 국소 3단계 상처, 가속 낙하의 실제 각도 substep, Terrain 패치 VFX를 연결했다. 효과·소리는 상태/보상을 작성하지 않는다. 모델 절단 접합·이펙트·사운드 최종 품질과 KCC/Nav는 사용자 테스트 대기다.

준비표: [20261113-132753-64b92438](../../output/harness/20261113-132753-64b92438/review.md). 연출 프로필·효과 wrapper 저작 후 현재 변경본 검증을 진행한다. 과거 77/77은 현재 통과 수에 합산하지 않는다.

## 연출 구현 최종 검증 — 2026-11-13

판정은 **연출 연결·프로필·관련 Editor 자동 검사 반영 / 실제 플레이·미술·실기 성능 인수 대기**다. [연출 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_PRESENTATION_DESIGN.md)와 [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)을 구현 기준으로 유지한다. 사용자 인수는 [RE-P01~10](reactive-environment-tests.md#연출-구현-인수--re-p0110-2026-11-13)으로 분리했다.

### 구현과 자산

- **기본 수관:** 원본과 wrapper의 같은 카메라·조명·0.4 배율, mesh LOD0/1/2를 실제 Unity preview로 비교했다. wrapper가 원본 회색 atlas만 이식하고 활성 LeavesGraph의 `_Foliage_Color_Bottom/Top`을 누락한 것을 확인했다. 활성 색/높이/알파를 커스텀 툰에 연결하고 양면 법선 경로·바람·기존 전 패스를 유지했다. 직렬화에만 남은 구 Color_Tint/Normal_Map을 이식 대상으로 잘못 조회한 중간본은 바로잡았으며 최종 잎에는 원본 활성 셰이더처럼 노멀맵을 사용하지 않는다. FarmTree 잎은 균열·연소 경계 발광·내부 noise 소거·외곽선을 끈다. ArenaTree는 수관/줄기 결합 슬롯이므로 전체 균열/경계 발광을 꺼 잎에 목재 표현이 들어가지 않게 했다. 마스크가 없는 그을림은 점무늬 대신 색 누적으로 표시한다. 제공된 검은 수관 화면의 정확한 char/fuel은 여전히 미확정이다.
- **소화·진단:** 코어 수식/날씨/GAS 입력을 유지하고 가연 프로필 4종만 조정했다. 모니터에 Current.Rain/IsReady, 활성 sample별 차폐와 exposure/contact, ID/프로필/map/layer, heat/wet/fuel, pending heat·물 impulse·지속 물, 최근 공급량과 source 거리/차폐/바람을 추가했다. 진단용 차폐 조회는 논리 tick 카운터를 오염시키지 않는다.
- **화염·연기:** 새 연출 SO와 줄기·수관 외곽·잔해의 명시 VFX 앵커, 기존 GameObjectManager FX 갱신의 대여/자연 반환을 연결했다. 열·젖음에 따라 화염 rate/크기/상승 속도가 줄고, 화염·연기는 분리된다. Cooling/Spent는 잔열 문턱+시간 상한, 실제 젖음 소화 전이는 짧은 증기 감쇠를 사용한다. 첫 냉각 로드로 증기를 재생하지 않는다. 무변경 rate/바람/포즈는 다시 쓰지 않으며 같은 snapshot은 Play를 재호출하지 않는다. 거리·화면 여유·효과별 인스턴스/입자 상한은 논리와 독립이다.
- **Terrain:** 기존 셀/밀도/저장을 유지한 2×2셀 시각 패치다. 잎/셀별 MonoBehaviour·Collider·ParticleSystem을 만들지 않는다. 효과 상한·시야 생략에도 원래 화학/전파/저장은 진행한다. 새로운 열 송수신 앵커는 추가하지 않아 공급 총량과 tick 시작 snapshot/다음 tick 전파를 유지했다.
- **명중·벌목:** 전투/채집/명시 중복 제거 입력은 승인 후 실제 내구도 감소 때만 같은 피드백을 사용한다. 실제 HitPoint와 캐시 Collider raycast 법선(실패 시 공격 역방향), 채집은 실제 절단 앵커를 사용한다. 칩·작은 수관 낙엽·기존 Sound 서비스의 음향, Collider 없는 시각 자식 반응, 내구도 기반 3단계 국소 재질 상처를 연결했다. 원본 구매 Collider는 보존하고 wrapper 안에서 고정 형상으로 복사한 override와 hit 참조를 유지했다. 낙하 가속은 2.2이며 전체 실제 각도 구간의 캡슐 substep·fallback·안전 포즈를 보존한다. 조회는 소유 PhysicsScene을 사용한다. 저장 progress는 각도 비율을 유지해 과거 낙하 중 저장의 포즈를 바꾸지 않는다. 지면이 조회된 정착에서만 먼지·낙엽·소리를 요청한다. KCC 강제 밀기·낙목 피해·새 보상 작성자는 추가하지 않았다.

프로젝트 소유 변경/추가는 wrapper **3종**(FarmTree/ArenaTree/FarmGrass), 반응 프로필 **4종**과 기존 낙하 레시피 **1종**, 연출 SO **2종**, 기존 FarmTree/ArenaTree 재질 **4종**, 효과 프리팹/공유 재질 **각 5종**, 작은 칩/잎 메시 **2종**과 각각의 meta다. 효과 원본은 Polyart P_Fire/P_Smoke/P_Dust_Gray이며 원본 텍스처·기존 alpha 수명 곡선을 재사용한다. 불의 원본 HDR 색을 유지하고 연기/먼지의 레거시 재질은 프로젝트 URP 복사본으로 변환했다. 칩/낙엽은 프로젝트 소유 작은 메시다. 음향은 보유 Axe impact와 Leaves landing clip을 참조하며 실제 소리 적합성은 사용자 대기다.

구매 원본 프리팹/재질/메시/텍스처, TerrainData·Detail preset·Detail 인스턴싱 프리팹, CargoCrate, 기존 GUID/저장/보상 원장·채집 소유권·GAS·날씨·전파 코어와 다른 작업 변경을 보존했다. 정규 씬은 적용·저장하지 않았다. 폐기된 Sample 콘텐츠는 복원하지 않았다.

### 검증과 근거

Unity 6000.3.21f1의 현재 소스 관련 EditMode **64/64, 실패/건너뜀 0**을 확인했다. 최종 회귀는 연출 16·통합 21·표면 6·치트 2·반응 코어 17·환경 payload 2건이다. 원본은 [최종 상세 JSON](../../output/ReactiveEnvironment/Presentation/editmode-final.json), [컴파일/셰이더/임시 서비스 정리](../../output/ReactiveEnvironment/Presentation/compile-checks.json), [컴파일 DLL 시각](../../output/ReactiveEnvironment/Presentation/compiled-assemblies.json)이다. 과거 77/77과 합산하지 않는다.

강수별 독립 실행은 실제 Unity에서 로드한 현재 FarmTree 프로필과 반응 코어를 호출했다. 초기 heat=1/wet=0/fuel=60/Burning, dt=0.2, exposure=1, 외부 공급 없음 조건에서 Rain 0.1/0.25/0.5/1은 **12.0/4.0/2.0/1.0초**에 Cooling, 잔여 fuel은 **36.4/52.4/56.4/58.4**였다. Rain 0은 약 30초 연료 소진으로 Spent다. [값 기록](../../output/ReactiveEnvironment/Presentation/rain-results.txt). 실제 월드 비 파티클/차폐/외부 emitter 재현 결과로 확대하지 않는다.

실제 Terrain 원본을 격리 복제해 같은 layer0, 대표점 거리 3m, 무풍·차폐 없음에서 **나무 프로필→Detail 셀 / Detail 셀→나무 프로필** 발화와 다음 tick 전파를 검사했다. 이 테스트의 나무는 수지 fixture이며 실제 wrapper 구성/LOD는 별도 통합 검사다. 본편 배치 전체나 임의 장식 모델에 전파된다는 뜻이 아니다. 가속 낙하는 22/45/70도 중간 장애물과 안전 정착, 기존/가속 낙하 저장 포즈를 검사했다. 실제 KCC/Nav와 연속 각도의 미술 인수는 별도다.

[원본 LOD0](../../output/ReactiveEnvironment/Presentation/source-lod0.png), [회색 이식 상태](../../output/ReactiveEnvironment/Presentation/wrapper-before-tint-lod0.png), [수정 LOD0](../../output/ReactiveEnvironment/Presentation/wrapper-lod0.png), [LOD1](../../output/ReactiveEnvironment/Presentation/wrapper-lod1.png), [LOD2](../../output/ReactiveEnvironment/Presentation/wrapper-lod2.png), [연소](../../output/ReactiveEnvironment/Presentation/burning.png), [냉각 잔연](../../output/ReactiveEnvironment/Presentation/cooling.png), [탄화](../../output/ReactiveEnvironment/Presentation/charred.png), [연기 단독](../../output/ReactiveEnvironment/Presentation/smoke-isolated.png)을 AI가 시각 확인했다. 실제 상태 어댑터와 FX 풀을 호출한 preview이며 Boot/실제 입력/시간 경과 촬영은 아니다. [촬영 코드](../../output/ReactiveEnvironment/Presentation/capture-state-source.txt)를 보존했다. 최종 미술 인수로 승격하지 않는다.

중간 실패는 논리 fixture의 잘못된 표면 backend 2건, Test Runner 미저장 씬의 additive 생성 제한 3건이었다. fixture를 수정하고 preview PhysicsScene 검사로 변경했다. 실제 효과 촬영에서 속도 곡선 모드 불일치를 발견해 수정했고 실제 프리팹을 빌린 회귀를 추가했다. 마지막 일부 편집에서 컴파일 요청이 최신 DLL 반영보다 빨리 반환해 오래된 fixture로 화면 예산 검사가 실패한 결과는 최종 근거에서 제외했다. force compile 뒤 소스보다 DLL 시각이 새롭고 최신 필드가 로드됐는지 확인한 다음 최종 검사를 실행했다. 실행 중 다른 테스트를 취소하거나 두 번째 Editor를 열지 않았다.

### 남은 인수와 정리

**RE-P01~10 / 기존 RE-G·RE-C는 사용자 테스트 대기**다. 국소 상처는 현재 재질 기반 3단계이며 실제로 깊게 파인 절단 홈 메시까지 제작한 것이 아니다. 실제 원본 절단 cap 접합/피벗·음향·벌목 무게, 원본과 툰의 역광·그림자·바람 동등성, 본편 비 소화/차폐/전파, KCC/Nav/통로, 정식 슬롯 저장·지역 왕복·긴 화재, 실기 CPU/GPU/GC/메모리/효과 풀 최초 확장 spike는 미수행이다. 마지막 상처 위치 자체는 저장하지 않고 절단 앵커에서 내구도 단계를 복원한다. 원거리·타 맵 자동 전파·전체 장식 자동 부착은 기존 범위 밖이다.

물 MSAA/RenderGraph 오류는 이 물 없는 preview에서 재현하지 않았다. 실제 물 월드 렌더링 인수는 미수행이며 오류 프레임은 입자/깊이/알파 인수 근거에서 제외한다. 별도 물 작업을 해결한 것으로 보고하지 않는다.

이번 작업은 임시 C# 관찰자·샘플 씬·Player 사본을 만들지 않았다. preview 객체/런타임 효과/서비스는 반환·정리했다. 시작 시 미커밋 상태 기록과 수정 전 프로젝트 자산 사본, 테스트 상세/작은 촬영/원본 출처·해시·링크 결과를 보존한다. 사용자 변경이 든 사본은 커밋되지 않아 삭제하지 않는다. Player/Addressables 빌드·커밋은 실행하지 않았다. [월드 위키](../wiki/world-art.md)와 [준비표](../../output/harness/20261113-132753-64b92438/review.md)로 인계한다.

문서/준비표 7종의 로컬 링크/Markdown 앵커 **174건, 경로·앵커 누락 0**을 확인했다([링크 결과](../../output/ReactiveEnvironment/Presentation/document-links.json)). 최종 소스·자산·meta는 [해시 목록](../../output/ReactiveEnvironment/Presentation/snapshot-manifest.json), 자산 보존/출처는 [목록](../../output/ReactiveEnvironment/Presentation/assets.txt)을 따른다.

최종 실자산 재로드에서도 wrapper 4종의 정의 유효·반응/제외·전체 LOD 슬롯 누락 0, FarmTree 시각 자식 Collider 0을 확인했다([실자산 검사](../../output/ReactiveEnvironment/Presentation/asset-checks.json)). 최종 Console의 저장 호환 오류 1건은 기존 통합 검사의 LogAssert.Expect가 확인한 불일치 저장 복구 시나리오다. 물/RenderGraph 오류로 해석하지 않는다.

## MCP 실제 실행 검증 — 2026-11-13

현재 실행 근거는 [이번 전용 보고서](../../output/ReactiveEnvironment/Live-20261113/REPORT.md)에 추가한다. LakeVillageWorld 미저장 변경 없음·Play/테스트 비실행 확인 후 기존 씬에서 Play에 진입해 GameManager Ready/활성 플레이어/실제 날씨를 확인했다. 정규 씬 저장 없이 별도 런타임 씬 RE_Live_Isolated와 독립 소유 맵 RE-Live-20261113에 기존 소환 치트로 wrapper를 등록했다.

1단계: 실제 초기 heat0/char0/fuel60/durability100. 시간표 비로 wet0.04486인 첫 대상은 보존 기록으로만 사용하고, 통제 회차는 Rain0 미리보기와 새 ID로 heat0/wet0/char0/fuel60을 확인했다. 구매 원본·wrapper LOD0/1/2를 같은 위치/0.4 배율·카메라·조명에서 실제 Play 카메라로 촬영했다.

2단계 최초 실패: 권위 Burning·연료 소비 중에도 ParticleSystem 대여0. WorldStateManager에 OnFixedUpdate 메서드는 있으나 IFixedUpdatableManager 계약이 누락되어 GameManager 고정 갱신 목록에 없었다. 화염·잔연·낙하·타격 회복이 실제 실행에서 갱신되지 않는 결함이다. 계약 선언과 회귀 검사를 추가하며, 수정 후 같은 통제 강수 조건을 재검증한다. 수정 전 사진/수지/등록 상태/소스 사본은 전용 폴더에 보존한다. 기존 정적 preview의 통과를 실제 실행 통과로 승격하지 않는다.

3단계 실행 중간 결과: 수정 후 GameManager 고정 tick 등록=True, 실제 화염/연기 생성 확인. 통제 강수 Rain0/0.1/0.25/0.5/1은 각각 약29.979초 연료소진(Spent) / 11.879 / 3.838 / 2.047 / 0.910초 Cooling이었다. 소화 잔여 fuel35.9999/51.99997/55.59998/57.99999. 점화 직후 이미 한 화학 tick을 소비한 heat1/wet0/fuel59.6에서 강수를 시작했으며 관측 오차는 tick0.2초와 관찰 프레임 간격을 포함한다. R0는 대부분 배율1, 이후 배율2로 실제 게임 tick을 진행했고 수동 Tick/권위 snapshot 주입은 하지 않았다. 사진은 전용 폴더 After의 실제 Play 카메라 Render이며 기존 preview가 아니다.

실제 wrapper 두 그루의 heatPoint 거리3/4/4.5m×무풍/정풍/역풍과3m벽 총10회에서 입력·공급·발화/미발화를 기록했다. Terrain Detail 레이어 번호와 논리 layer0은 구분한다. 세부 수치·차폐·남은 항목은 최종 전용 보고서로 인계한다. 전투 준비와 무관한 RuntimePlacementLoader ActorDatabase 시간초과3건이 Console에 있었으며 별도 월드 배치 문제로 보존한다. 현재까지 물 MSAA/RenderGraph 오류는 관찰되지 않았으나 물 렌더링 인수는 아니다.

4단계: 실제 TerrainData+LakeVillageDetails preset의 양방향 전파, 원본 밀도367 보존, 명중/채집 공유 내구도·35/70% 손상·가속 낙하·벽/통로/사방 차단 fallback을 관찰했다. 전투는 ReceiveHit 불변 요청 직접 전달, 채집은 GatheringActor.OnAnimationEvent(OnHit) 직접 호출이다. 키보드/게임패드·GAS 발동/무기 sweep 성공으로 승격하지 않는다. 정식 SaveGameDetailed/LoadGame은 파일 목적지만 전용 폴더로 격리해 슬롯177613 저장·2회 로드 성공, 보상1건 유지. 연소/소화/낙하의 비활성·격리 런타임 씬 unload/reentry에서 같은 ID 복원과 SuspendDomain을 확인했다. 본편 지역 스트리밍 왕복·KCC/Nav는 미수행이다.

5단계: 36대상에서 화염24+연기24 상한, 비 소화/반복snapshot 후 leased/draining/active0을 확인했다. 다만 카메라 원거리 요청 시 효과가 계속 남는 현상을 추가 재현했다. 별도 실제 카메라 포즈 기록 CAM1에서 거리200.0256m·viewport(0.50,0.48,200.01)·플레이어 거리26.699m, 대여4를 확인했다. 기존 최대 거리 검사가 플레이어에만 적용된 결함으로 판단해 실제 카메라 거리도 같은 SO 예산으로 검사하도록 보완한다. 재검증은 후속 기록을 따른다.

관련 회귀38/38의 상세 결과(job d58959361ee842adb2b08c5cef1ffe0a)를 확보했다. 이는 카메라 거리 보완 이전 결과이며 후속 최신 결과와 구분한다. 동시 Editor 재컴파일/테스트 전환 중 일부 후속 촬영 시도가 배치 소멸/응답 단절로 실패해 시험 중인 작업을 취소하지 않고 유휴 상태에서 보완했다. 실패 사진·관찰 코드·원본 결과를 보존한다.

### MCP 최종 판정

실제 고정 tick 등록/연출 복구와 카메라 거리 보완을 모두 재검증했다. 카메라17.165m 대여4→200.026m 화면 안0→재접근4이며 논리 Burning/연료 소비는 지속했다. 최종 active입자시스템0/입자수0, Play 종료 후 RE_ 런타임 객체0·LakeVillageWorld dirty=False를 확인했다. DLL06:19:13 UTC가 수정 소스06:18:56 UTC보다 최신이며 로드 MVID도 기록했다. 최신 관련 EditMode38/38, 실패/건너뜀0(job08e757b8c80341caafab5f065685ee8a).

최종 [깨끗한 초기/탄화 비교](../../output/ReactiveEnvironment/Live-20261113/FinalComparisons/P02-green-vs-char-SNAPSHOT-ONLY.png)와 Arena 결합slot0/전LOD 사진은 표현 snapshot임을 명시했다. 자연 연소/비 소화/전파/벌목/수명은 별도 실제 중앙 tick 로그다. [최종 보고서·항목별 판정](../../output/ReactiveEnvironment/Live-20261113/REPORT.md), [연속 원본 프레임](../../output/ReactiveEnvironment/Live-20261113/sequence.html), [작업만의 diff](../../output/ReactiveEnvironment/Live-20261113/task-only.diff)를 보존한다.

프로젝트 URP/Quality MSAA는4였지만 증거용 RenderTexture는1샘플이며 정규 물 렌더링 인수가 아니다. 실제 환경 프레임에서 물 MSAA/RenderGraph 오류는 관측하지 않았다. 시작/종료 서비스 경고와 ActorDatabase 지역 배치 시간초과는 별도 기록으로 유지한다. 실제 입력/음향 청감/KCC/Nav/본편 스트리밍/긴 화재/실기 성능·최종 미술은 미수행 또는 사용자 테스트 대기다. 현재 상처는 재질 기반이며 깊은 절단 홈 메시가 아니다.

런타임2파일·회귀2파일과 문서만 수정했다. 구매 원본·wrapper/프로필/재질·TerrainData·meta·정규 씬은 이번 작업에서 저장하지 않았다. 기존 미커밋·다른 작업 변경은 보존했고 임시 런타임 씬/배치/카메라/고정 조명/효과는 정리했다. 격리 세이브와 실패 사진·관찰 코드·해시는 증거로 남겼다. Player/Addressables 빌드·커밋은 실행하지 않았다. [월드 위키](../wiki/world-art.md)로 인계한다.

최종 종료 Console 예외: PetManager.Despawn150의 NullReferenceException1건(GameManager.OnDestroy 경유)을 별도 발견했다. 환경/물 렌더 프레임 오류가 아니며 요청 범위 밖 종료 수명 문제라 임의 수정하지 않았다. [전문](../../output/ReactiveEnvironment/Live-20261113/console-errors-final.json). 최종 Console 전체 오류0으로 보고하지 않는다. 문서7종의 로컬 링크/Markdown 앵커210건에서 누락0/잘못된 앵커0을 확인했다.

시작 이후 두 CameraTestMap 몬스터 프리팹의 재질 override 변경을 발견해 diff를 확인했다. 의미 있는 다른 작업 변경이므로 환경 검증의 자동 재직렬화로 단정하거나 되돌리지 않았다. 원본/저작 자산의 현재 해시와 사진별 메타데이터 완전성은 전용 보고서의 보존 목록을 따른다.

최종 링크 확인216건 누락0. 사진211개와 상태/실패/세이브 근거 약75MB를 인수 증거로 보존했다. 실제 재검증과 미수행 구분은 최종 보고서의 개별 판정을 따른다.

## 정규 월드 연결 확장 설계 — 2026-11-13

사용자 요청: 씬에 배치된 환경 모델 전체를 반응형으로 전환하고, 불속성 부여 공격과 날씨 번개의 실제 타격점에서 불이 시작되게 하는 설계를 추가한다. 정본은 [통합 계약의 확장 설계](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#정규-월드-연결-확장-설계), 표현은 [연출 연결](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_PRESENTATION_DESIGN.md#정규-월드-전환불-부여낙뢰의-연출-연결), 후속 검증은 [RE-W/F/L 인수](reactive-environment-tests.md#정규-월드-연결-확장-인수)다.

### 이번에 확정한 계약

- 전환은 원본 외형/LOD·Transform·기존 SceneEntityId/placementGuid·씬 참조·채집/진행/보상을 보존한다. 씬뿐 아니라 배치/재생성 원본과 런타임 소환을 포함해 미처리/미분류0을 목표로 한다. 물/액터/지형 본체 등 기존 시스템 소유 대상은 근거를 남겨 분류하며 미지원 환경 모델을 무조건 제외하지 않는다.
- 불 부여는 기존 활성 GameplayEffect/ElementOverride의 유효 출처와 우선순위를 재사용한다. 기존 Fire Effect에는 환경 열 수치가 없으므로 후속 데이터/공통 공격 snapshot 연결이 필요하다. 일반 공격 전부에 영구 열을 저작하는 방식은 사용하지 않는다. 명시 페이즈 열과 부여 열은 기본 max 합성, 다중탄/다단은 저작한 분배 규약을 따른다.
- `EnvironmentEffects.Storm.cs`의 현재 번개는 원경 공중 끝점이며 물리 타격이 없다. 날씨/지역 경로에서 실제 첫 타격면을 정한 사건 하나가 화면 접점과 중앙 단발 열 입력을 함께 공급하도록 설계했다. 카메라·FX와 논리를 분리하고 비/젖음/비가연성·저장 무중복·언로드 정지를 유지한다.

### 구현/검증 상태

| 단계 | 현재 상태 | 다음 행동 |
|---|---|---|
| 설계 추가 | 완료 | 아래 로컬 링크/상태/명세 확인 |
| W0 전수 조사 | 미착수 | 정규 씬·생성/배치 원본·환경 프리팹·Terrain의 실제 대상 수/ID/대응 확정 |
| W1 대표 구획 전환 | 미착수 | 원형 유지형/원본별 wrapper·비가연 소품·보호 구조물의 실제 구성/저장 검증 |
| F1 불 부여 점화 | 미착수 | Effect 열량/페이즈 수신·분배와 공통 AttackData snapshot 구현·저작·실제 입력 검증 |
| L1 낙뢰 점화 | 미착수 | 지역 스케줄·실제 타격점 사건·단발 환경 입력·저장 경계 구현/저작 |
| W2 전 맵 적용 | 미착수 | 대표 구획 인수 후 모든 전환 대상과 원본 레코드/생성기 적용 |
| V 확장 인수 | 미수행 | RE-W01~08 / RE-F01~06 / RE-L01~07의 실제 실행·사진·저장·성능 |

이 단계에서는 현재 코드/데이터의 정적 대조와 문서·로컬 링크 검증만 수행한다. Runtime/Editor C#·씬·프리팹·SO·구매 원본/meta는 변경하지 않으며, Unity Play/Test Runner·Player/Addressables 빌드·커밋을 실행하지 않는다. 앞선 MCP/38건 결과는 기존 구현의 근거로 보존하고 새 기능 통과로 합산하지 않는다. 준비표는 `output/harness/20261113-160457-ed4e4079`, 분야 인계는 [월드 위키](../wiki/world-art.md)다. 새 임시 씬/객체/서비스가 없어 런타임 정리는 해당 없음이다.

설계 단계 검증: 문서7종의 로컬 링크/Markdown 앵커214건에서 누락0·잘못된 앵커0을 확인했다. 후속 인수 ID는 W8/F6/L7 총21개이며 전부 미수행으로 표시했다. 이번 단계는 설계 문서 수정이므로 새 Unity 실행/회귀 테스트를 하지 않았다. 정본·작업 상태·표현·저작 준비와 월드/전투 위키를 연결했고 준비표 review.md에 범위와 보존/미수행을 기록했다.


## 정규 월드 연결 구현 진행 — 2026-11-13

F1: 유효 ElementOverride의 출처/열량을 공격 생성 시 확정하고, 공통 페이즈 합성(max), 차지·몬스터·잔상·투사체 복사 및 다중 발사 저작 분배에 연결했다. Fire Effect 열량 2, 실제 PlayerCharacterDefinitionSO가 참조하는 236 파일 안 247 Payload의 397 유효 명중 페이즈에 수신/공격별 분배를 저작했다. 기존 지속시간·비용·쿨다운·MotionKey는 보존했다. 첫 새 경계 EditMode 2/2 통과, 실제 입력 RE-F01~06은 사용자 테스트 대기다. 데이터 전수 검사 결과와 후속 낙뢰·월드 적용은 아래에 이어 기록한다.

정규 LakeVillageWorld는 조사 시작 시 dirty=False, AuthoredEnvironment Renderer 62,575개다. 이 수는 논리 개체/전환 완료 수가 아니다. 구매 impostor·결합 메시·진행 소유권까지 조사 중이며 W0/W1/W2 완료를 선언하지 않는다. 수정 전 사용자 변경 보호 사본과 GAS 대상 목록은 `output/ReactiveEnvironment/WorldIntegration-20261113/`에 보존했다.


### L1 핵심 연결과 자동 검사

- [낙뢰 프로필](../../Assets/02.Scripts/Data/Environment/EnvironmentLightningProfileSO.cs), [지역 사건·저장 DTO](../../Assets/02.Scripts/Data/Save/EnvironmentLightningSaveState.cs), [소비 계약](../../Assets/02.Scripts/Contracts/IEnvironmentLightningService.cs), [단일 작성자](../../Assets/02.Scripts/Manager/Save/WorldStateManager.Lightning.cs), 기존 EnvironmentController/EnvironmentEffects.Storm에 연결했다. 후보는 원본 지역 좌표이며 WorldCoordinates로 월드 좌표로 변환한다. 카메라·실내·효과 cap·렌더 성공은 사건 생성 조건이 아니다.
- 순서: 지역 게임 시각/난수 → 실제 첫 물리 표면 → 기존 수역 메시가 더 위에 있으면 수면 접점 → 같은 map/논리 layer의 직접 대상·물리 범위·Terrain 셀 → 중복 없는 단발 열. 수면은 물 시스템을 유지하고 바닥 관통 열을 공급하지 않는다. 비·wet/fuel·발화/소화는 기존 반응 코어가 계산한다.
- 저장 경계는 동기 CommitLightning 종료다. 이 구간에는 yield·외부 콜백·FX가 없다. 사건은 지연 이벤트 대기열을 두지 않고 기존 대상 record.pendingHeat에 즉시 인계한다. 그 대기 열과 지역 randomState/sequence/committedSequence/remainingMinutes를 같은 ExportEnvironment에서 저장한다. 로드 후 커밋 사건을 재생하지 않고 첫 갱신·언로드/시간 점프는 Suspend한다. 잘못된 낙뢰 저장은 활성 환경 교체 전에 거부한다.
- [호수 프로필](../../Assets/10.Datas/Environment/Lightning_LakeOfLife.asset): 게임 시간 간격8~22분, 열2, 반경3m, 위에서500m 조회, 원본 지역 x5~2395/z-695~2095/y450, surfaceLayerId0. 대상층은 Default/Ground/InteractableObject다. 나무·소품의 전환이 끝났다는 의미는 아니다.
- LakeOfLifeEnvironment에 프로필을 연결했다. 본선 끝점·접촉 불티·섬광 위치·천둥 거리 계산은 확정 접점을 공유한다. [접촉 불티](../../Assets/03.Prefabs/Environment/Reactive/Effects/PF_EnvironmentLightningContact.prefab)는 기존 Weather_Lightning 재질과 ParticleSystem으로 저작하고 기존 IParticleEffectService 풀(최대4, 300m 시각 제한)을 사용한다. 열 입력을 추가하지 않는다. 화면 미리보기는 기존 장식 번개만 사용한다.
- 사용자가 추가한 Lightning 음원 중 BSB_Thunder_3113(5.03초)/3114(11.20초)/3179(13.76초) 모노48kHz3종을 골라 공용 EnvironmentEffects·LakeOfLife·DroughtVillage 프리팹의 _thunderClips에 연결했다. 원본 WAV/OGG와 meta5종은 보존했다. 청감·실제 플레이 믹싱은 사용자 확인 전이다.
- 환경 반응 모니터에 ‘선택 위치에 실제 낙뢰 요청 (저장 반영)’을 추가했다. 프로필 후보 높이에서 첫 면을 다시 확인하는 같은 요청 경로이며 map/사건/게임 시각/접점/법선/층/열/프로필을 로그에 남긴다. 사용자 실제 명중 시험과 직접 열 주입을 구분한다.

자동 검사: 현재 Unity 컴파일 후 신규 타입 로드·MCP 복귀 확인, 관련 EditMode38/38·독립 PlayMode2/2 통과. [원본 결과](../../output/ReactiveEnvironment/WorldIntegration-20261113/test-results.json). PlayMode는 실제 물리 첫 접점, 마른 대상 발화, 지붕 아래 관통 방지, LineRenderer 끝점, 천둥 거리 지연, 시간 점프 억제와 기존 Staff 마력탄/레이저 재사용이다. EditMode는 대기 열/순번 저장 왕복·중복 사건·잘못된 저장 보존·우선순위/만료 후 snapshot·패턴/분열 분배·접촉/폭발/장판 단발성·Terrain 저장 회귀를 포함한다. 새로운 접점 불티의 최종 화면은 미인수다.

### W0 전수 조사와 실제 차단

[조사기](../../Assets/02.Scripts/Editor/ReactiveEnvironmentWorldAudit.cs)는 비활성·중첩·모든 LOD·원본 GUID·GlobalObjectId·기존 배치/저장 ID·Transform·표면/Collider/재질·Terrain·RuntimePlacementData를 읽기만 한다. 목록의 Unclassified는 미분류 후보이며 물/VFX/기존 소유 개체의 검수가 끝난 환경 개수로 간주하지 않는다. 원본/생성본에 같은 개체가 있으므로 아래 수량을 합산하지 않는다.

| 씬 | 논리 루트 후보 | 반응 구성 | 미분류 후보 | Runtime 레코드 |
|---|---:|---:|---:|---:|
| LakeVillageWorld | 24,813 | 0 | 24,654 | 480 |
| DroughtVillage 원본 | 1,066 | 0 | 1,057 | 0 |
| StonegateCitadel 원본 | 2,766 | 0 | 2,752 | 0 |
| SaltwindHarbor 원본 | 1,408 | 0 | 1,398 | 0 |
| DroughtVillageRegion | 217 | 0 | 208 | 0 |
| StonegateCitadelRegion | 16 | 0 | 2 | 0 |
| SaltwindHarborRegion | 12 | 0 | 2 | 0 |

[호수 목록](../../output/ReactiveEnvironment/WorldIntegration-20261113/host-inventory.json), [재생성 원본 존재 검사](../../output/ReactiveEnvironment/WorldIntegration-20261113/source-scene-availability.json). 호수 Renderer68,909개 중 AuthoredEnvironment는62,575개다. Terrain1개·Detail10층·해상도1024·Tree0·셀 어댑터없음. 호수의 미분류 원본 대응 후보는 기존 prefab GUID153종과 prefab 출처 없는 후보605개다. FarmLand 줄기·잎·impostor, 결합 식생/지형, 진행 구조물과 채집 소유권은 실제 저작을 더 해야 한다. 기존 FarmTree wrapper처럼 impostor를 끄는 방식으로 전 맵 LOD 보존 완료를 선언하지 않는다.

**차단:** OpenWorldSliceAuthoring이 LakePath로 지정한 Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity와 meta가 없다. 해당 경로는 Git 추적도 없다. 현재 다른 씬으로 임의 대체하거나 정규 씬을 원본으로 복사해 순환 재생성하지 않았다. 사용자에게 이전/변경 경로를 확인 요청했다. W1 대표 구획 및 W2 씬/배치 원본 전체 적용은 미수행이며 ‘미처리·미분류0’에 도달하지 않았다. 원본 복구/정본 경로 확정, 원본별 외형/LOD·진행 정책 저작 후 이어간다.

### 보존과 남은 인수

GAS 전수 검사는 오류7·경고599: PlayerDefault의 빈 Ability/Ultimate 페이즈2, CharacterPassiveDatabase의 중복ID4·누락TriggeredEffect1이다. 이번 신규 열량 필드 오류는 없으며 해당 기존 콘텐츠는 임의 수정하지 않았다. 전체 GAS 검증 통과로 보고하지 않는다.

236 공격 파일은 [보존 대조](../../output/ReactiveEnvironment/WorldIntegration-20261113/asset-preservation-check.json)에서 새 수신/가중치만 제거하면 작업 전 파일과 동일함을 확인했다. Fire Effect도 새 열량 외에는 동일하다. 구매 원본·음원5종·meta·기존 미커밋은 보존했다. 작업 중 다른 주체의 커밋으로 Git 기준선이 바뀐 것은 그대로 유지했으며 이 작업에서 커밋 명령을 실행하지 않았다.

정규 씬은 저장하거나 변환하지 않았고 최종 조회 dirty=False, Edit Mode다. 씬151MB와 기존 사용자 변경이 포함된 before 보호 사본, 전수 조사 JSON, 결과 JSON은 output/ReactiveEnvironment/WorldIntegration-20261113에 재개·복구 근거로 보존한다. 새 Player/Addressables 빌드·임시 프로젝트 사본·추가 Editor는 만들지 않았다. 기존 사용자 InitTestScene 파일은 보존했다. 실제 RE-W01~08/F01~06/L01~07의 일반 입력·정규 폭풍·지역 왕복·저장·KCC/Nav·목표 기기 성능·최종 미술/청감은 사용자 테스트 대기 또는 W 전환 미수행이다.


최종 재검증: 수면 접점/열0, 미리보기 전환 뒤에도 이미 확정한 실제 섬광 접점 유지, 명시 Terrain 논리층 우선·다른 지역 수면 모호성 거부를 반영했다. 현재 소스의 EditMode38/38와 PlayMode2/2 통과. 기존 Staff 단독 fixture에는 IActorObjectService 미등록 경고1이 남아 있으며, 새 낙뢰 fixture 출력은 비어 있다. 경고까지0이라고 보고하지 않는다. [실제 에셋/천둥 연결 조회](../../output/ReactiveEnvironment/WorldIntegration-20261113/asset-audit.txt), [GAS 오류 원본](../../output/ReactiveEnvironment/WorldIntegration-20261113/gas-validation.txt), [최종 준비표](../../output/harness/20261113-165936-dcee960a/review.md). 기존 사용자 변경과 전환 재개 자료 약205MB를 보존하고 초기/잘못된 준비표3개만 정리했다([기록](../../output/ReactiveEnvironment/WorldIntegration-20261113/cleanup.json)).

문서 종료 검사: 갱신 문서5개의 로컬 링크/앵커208건 누락0, git diff --check 통과. 최종 Console 오류 조회0건이며 Staff fixture의 별도 서비스 경고1은 위 기록을 유지한다.

## 통합 후속 조사와 수면 낙뢰 수정 — 2026-11-13

정본과 기존 변경을 대조한 뒤 코드 수정·전수 재조회·현재 소스 자동 검사를 수행했다. **W1/W2 전환은 아직 미수행**이다. 새 환경 모델 적용 0개·새 에셋 연결 0개이며, 아래 조사를 전환 완료로 계산하지 않는다. [월드 위키](../wiki/world-art.md)와 [현재 인수 구분](reactive-environment-tests.md#통합-후속-검증--2026-11-13)으로 인계한다.

### 재생성 원본의 이력과 확인 대기

이전의 ‘Git 추적이 없다’는 현재 인덱스에 대한 설명이다. 전체 이력에는 원본이 있다. `d74dbc02`의 마지막 원본은 113,271,273바이트이며, `82274a46`에서 100MB 초과 사유로 씬 추적을 제외했고 `2b71aefb`에서 meta와 빌드 씬 등록이 제거됐다. 기존 NPC 상태 문서에도 원본 meta 삭제가 사용자 변경임이 기록돼 있다. 다른 경로로 이동했다는 근거는 확인하지 못했다. 현재 생성기의 `LakePath`는 여전히 누락된 경로를 가리킨다.

Git blob `b716dd086bc079c72c41e65626657fd0885c9c69`를 [복구 후보](../../output/ReactiveEnvironment/Continuation-20261113/LakeOfLifeScenario.d74dbc02.unity)로 바이트 그대로 추출했다. `git hash-object` 일치, SHA256 `4D369D78FAD9AD98AC1AB9852C9C96849C0FC3031DC42821B97D55ABC9921EAC`, 원래 meta GUID `1d16ed83654f4f14e8628ca754f42067`을 확인했다. [이력 기록](../../output/ReactiveEnvironment/Continuation-20261113/source-history.json)을 보존한다.

11월 2일 이후 미추적 원본 편집의 존재를 Git만으로 확정할 수 없어 **이 Git 버전으로 복구할지, 이후 원본 경로가 있는지 사용자에게 질문한 상태**다. 답변 없이 Assets에 과거 버전을 정본으로 복구하거나 생성본을 원본으로 복제하지 않았다. 원본 버전 확정 → 원본과 현행 후속 생성/폴리시 입력 대조 → 대표 전환/재생성 검증이 다음 순서다. 단순히 파일만 되살려 현행 아트를 재생성해도 된다고 판정하지 않는다.

### 현재 전수 수량과 분류 근거

[현재 전체 목록](../../output/ReactiveEnvironment/Continuation-20261113/host-inventory-final.json)은 원본 GUID와 메시 subasset GUID/local ID, 실제 VFX·수역 소유 컴포넌트, 배치 ID·Transform·LOD/재질 목록을 기록한다. 메시가 섞인 prefab을 VFX로 통째로 제외하지 않는 회귀 검사도 추가했다.

| 항목 | 현재 수량/판정 |
|---|---|
| LakeVillageWorld 논리 루트 후보 | 24,813 |
| 반응 구성 / 이번 새 적용 | 0 / 0 |
| 미분류 후보 | 24,623 |
| 채집 정책 검토 대기 | 45 — GatheringActor 소유를 확인했지만 환경 전환 제외로 인정하지 않음 |
| 미처리 후보 합계 | 24,668 = 미분류 24,623 + 채집 검토 45. 검수 완료 환경 개체 수는 아직 미확정 |
| 기존 소유 분류 | 145 = 기존 액터 114 + VFX 전용 루트 31. 실제 컴포넌트 근거를 목록에 보존 |
| 미분류 원본 대응 | prefab GUID 265종 + prefab 출처 없는 후보 591개 |
| 런타임 배치 | 480레코드 → ActorDatabase의 actorId 99종. 누락/중복 정의 0 |

앞선 ‘미분류 prefab GUID 153종’은 재집계와 맞지 않는다. 기존 JSON 자체를 다시 집계하면 269종/무출처605개, VFX 분류 후에는 265종/591개다. 숫자를 전환 완료 수나 제외 승인 수로 재해석하지 않는다. [런타임 원본 대응](../../output/ReactiveEnvironment/Continuation-20261113/runtime-source-closure.tsv)은 각 actorId의 실제 정의·prefab·GameActor 소유 타입을 기록한다. 직접 prefab 참조가 빈 480건을 환경 prefab 누락으로 계산하지 않는다. 씬·배치 레코드·소환 원본 변경은 수행하지 않았다.

Terrain은 실제 `WorldThemesABC/Terrain.asset`의 10층·각 1024×1024 픽셀을 모두 읽었다. Tree 0, 레이어별 비어 있지 않은 픽셀 합149,086·밀도 합520,856이다. 이는 실제 렌더 인스턴스 수가 아니다. 32픽셀 타일의 사용 구역 전체와 원본 prefab GUID·배율/인스턴싱·dependency hash는 목록의 `terrainDetails`에 보존했다.

| Detail 층 | 밀도 합 | 사용 픽셀 | 사용 타일 |
|---|---:|---:|---:|
| 0 | 333,981 | 113,698 | 257 |
| 1 | 12,153 | 7,683 | 19 |
| 2 | 12,103 | 7,669 | 19 |
| 3 | 2,166 | 1,668 | 30 |
| 4 | 2,064 | 1,438 | 19 |
| 5 | 2,027 | 1,404 | 16 |
| 6 | 31,372 | 2,809 | 12 |
| 7 | 12,579 | 1,929 | 8 |
| 8 | 55,881 | 5,667 | 13 |
| 9 | 56,530 | 5,121 | 12 |

전체 사용 구역 조사는 완료했지만 10층의 반응형 표현 전환·원본과 모든 LOD/impostor·결합 식생의 외형/소유 정책은 미완료다. 가장 많은 SmallGrass_03도 원본이 3LOD와 색 변주·vertex color·dither·바람을 사용한다. 기존 단일 FarmGrass 재질로 바꾸면 보존 계약을 만족한다고 볼 수 없어 임의 대체하지 않았다.

### 구현과 자동 검증

`WorldStateManager.Lightning`은 바닥 Collider가 없는 수면에서 사건을 만들지 못하고, 겹친 수면은 발소리 우선순위에 따라 아래 면을 선택할 수 있었다. `FootstepWaterSurface.TryRaycastDown`을 추가해 실제 삼각형·체적/건조 공간·조회 거리 안의 가장 높은 수면을 찾고, 물리 첫 면과 높이를 비교하도록 수정했다. 기존 발소리/수영의 우선순위 조회는 유지한다. 수면 사건은 열0·단발 저장 경로를 그대로 사용한다. Hot Reload allowlist 밖의 새 메서드/Editor 변경이므로 Edit Mode 정상 컴파일·리로드·MCP 복귀를 확인했다.

- EditMode **107/107 통과**: 기존 관련38건 + 조사기2건 + 기존 수면/수영66건 + 새 수면 조회1건이다. 정확한 선택·개별 결과는 [최종 결과](../../output/ReactiveEnvironment/Continuation-20261113/edit-final.json)를 따른다. 최초 조사기 검사는 Unity의 missing MeshFilter에 `?.`를 사용해 1건 실패했으며 명시적 Unity null 검사로 수정 후 전체107건을 재실행했다([실패 근거](../../output/ReactiveEnvironment/Continuation-20261113/edit-first.json)).
- PlayMode **2건 완료·succeeded·실패0**: 기존 Staff와 확장한 실제 낙뢰 fixture. 바닥 없는 수면·낮은 수면의 높은 발소리 우선순위·제한 체적·수면 위 지붕을 추가했다. [작업 결과](../../output/ReactiveEnvironment/Continuation-20261113/play-final.json)는 완료 진행2/2와 실패목록0을 보존한다. 종료 뒤 추가 리로드로 MCP의 개별 result/output은 null이므로 개별 출력·Staff 경고가 사라졌다고 보고하지 않는다.
- 기존 GAS 오류7·경고599와 Staff 서비스 미등록 경고1 기록은 유지한다. 이번에 GAS 전체 검사를 다시 실행하거나 기존 문제를 수정하지 않았다. 종료 Console에는 별도 ToolCatalog 등록 경고3건이 있어 전체 경고0으로 판정하지 않는다.

실제 일반 입력·정규 폭풍·기존 저장/지역 왕복·KCC/Nav·장시간/목표 기기 성능·미술/청감 인수는 사용자 테스트 대기다. 원본·원본별 재질/모든 LOD·채집/진행 정책이 해결되지 않아 W 전체 인수는 미완료다.

### 보존과 부산물

LakeVillageWorld 디스크 SHA256은 이전 보호 사본과 동일한 `0C0209FAD870C1CA7E08FB2618FFF3B5CB2B5D061FF78E47BA0A12733D00FC69`다. 열린 씬 dirty=False·Edit Mode를 확인했으며 원본·구매 에셋·기존 `.meta`·배치/저장 ID는 수정하지 않았다. 시작 상태는 [기록](../../output/ReactiveEnvironment/Continuation-20261113/start-status.txt)에 보존했다. 다른 작업의 콘텐츠/셰이더/도구 변경을 원복하지 않았다. Player/Addressables 빌드·커밋·추가 Editor는 실행하지 않았다. 복구 후보와 보호 사본은 확인/재개 자료로 보존하며, 이번 중복 초기 조사 JSON만 정리 대상으로 삼는다.

후속 종료 확인: 최종 조사기(채집 검토 분리 포함)에서 EditMode107/107을 다시 확인했다. Console의 저장 호환 거부 오류1은 `TerrainRangeBurnSaveReloadAndPartitionFailurePreserveSource`의 `LogAssert.Expect`에 대응하는 예상 오류이며, 전체 Console 오류0으로 보고하지 않는다. 문서4개 로컬 링크/앵커211건 누락0·잘못된 앵커0, 대상 diff --check 통과. [최종 준비표](../../output/harness/20261113-174714-05f6f43c/review.md)와 [부산물 정리](../../output/ReactiveEnvironment/Continuation-20261113/cleanup.json)를 보존했다. 이번 중복 초기 조사본42MB만 삭제했고, 이번 재개 자료는 약172MB다. 기존 WorldIntegration 보호 사본에는 손대지 않았다.