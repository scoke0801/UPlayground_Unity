# 반응형 환경 — 작업 상태

- 설계 입력: [전달 설계](../전달용/environment_system_design_v1/environment_system_design_v1.html)
- 실제 정본: [통합 계약](../../Assets/docs/design/REACTIVE_ENVIRONMENT_INTEGRATION.md), [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)
- 인계: [사용자 테스트](reactive-environment-tests.md), [월드 위키](../wiki/world-art.md)
- 현재 판정: **등록된 환경 대상의 GAS 입력 → 날씨/전파/반응 → 사전 파괴/보상/저장 → 커스텀 툰 구현 반영. 최종 관련 EditMode 55/55 통과. 실제 월드 플레이·아트 인수는 사용자 테스트 대기.**
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

[샘플 생성기](../../Assets/02.Scripts/Editor/ReactiveEnvironmentToonSamples.cs)는 기존 상자를 툰으로 연결하고 아래 4종을 추가했다. Unity primitive와 [생성 leaf mesh](../../Assets/05.Models/ReactiveEnvironmentSample/Leaf.asset)가 원본이며 최종 환경 아트가 아니다.

| 프리팹 | 연결 내용 |
|---|---|
| [상자](../../Assets/03.Prefabs/Environment/ReactiveEnvironmentCrateSample.prefab) | 직접 파괴/화재 분기, 장작 1개 또는 자원 소진, 툰/전파 |
| [나무](../../Assets/03.Prefabs/Environment/ReactiveEnvironmentTreeSample.prefab) | 2 LOD·13 Renderer, 밑동/절단면/통나무/수관, 방향성 낙하/fallback, 소유 Nav 장애물, 원형/잔해의 열·비 샘플 |
| [바위](../../Assets/03.Prefabs/Environment/ReactiveEnvironmentRockSample.prefab) | 검 저항·둔기 피해 배율·그을림·사전 파손 조각·기존 돌 보상·Nav 해제 |
| [지붕](../../Assets/03.Prefabs/Environment/ReactiveEnvironmentRoofSample.prefab) | 실제 Ground 지붕 Collider와 별도 명중 proxy·보호 볼륨·파괴 시 차폐 해제. 카메라 이름 규칙도 등록 |
| [풀 패치](../../Assets/03.Prefabs/Environment/ReactiveEnvironmentGrassSample.prefab) | 한 패치의 연료/전파, 양면 leaf mesh·바람·소진 시 명시적 잎 coverage 소거 |

정의 5개·파괴 레시피 3개는 Assets/10.Datas/Environment, 툰 재질 5개는 Assets/11.Material에 있다. 프리팹 원본 GUID는 비워 두며 배치별 기존 SceneEntityId 발급을 사용한다. Git ignore 대상인 프리팹도 meta와 함께 로컬 인수 대상이다. 정규 씬은 이 작업에서 저장하지 않았다.

## 최종 검증

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
원본은 [최종 테스트](../../output/ReactiveEnvironment/toon-editmode-results.json), [자산 검사](../../output/ReactiveEnvironment/toon-asset-checks.json), [그래픽 정보/셰이더 오류](../../output/ReactiveEnvironment/toon-graphics-checks.json)다. 초기 단면의 [41건 결과](../../output/ReactiveEnvironment/editmode-results.json)는 이전 기록으로 보존한다.

[마름](../../output/ReactiveEnvironment/Toon/dry.png) · [젖음](../../output/ReactiveEnvironment/Toon/wet.png) · [그을림](../../output/ReactiveEnvironment/Toon/charred.png) · [연소](../../output/ReactiveEnvironment/Toon/burning.png)는 64px GPU 평면 캡처다. 전방 표면 반응은 픽셀로 검사했고 패턴·발광 구분도 확인했다. 실제 월드의 조명·그림자·바람/모션·카메라·형상 아트 인수를 대신하지 않는다.

중간 테스트는 domain reload로 결과 본문을 받지 못한 실행이 있어 통과 근거에서 제외했다. 기존 단언 직접 호출은 보조 근거였고, 최종 55건은 Test Runner의 실제 개별 결과가 있는 실행이다. 작업 시작 시 셰이더 생성 전부터 URP Blitter 중복 초기화 예외를 관측했다. 이 작업에서 그 원인을 수정하거나 전체 월드 렌더러 문제가 해소됐다고 판정하지 않았다.

## 사용자 확인과 범위 경계

[RE-01~15](reactive-environment-tests.md)는 사용자 테스트 대기다. 실제 조작/전투 감각·낙목과 KCC 접촉·Nav carving 지연·지붕 파괴 후 비·아이템 획득/슬롯 파일 왕복·최종 맵 아트와 CPU/GPU/메모리 성능은 미확인이다. Player/Addressables 빌드는 실행하지 않았다.

현재 구현은 등록된 소품/풀 패치의 실행 경로다. 전체 콘텐츠 closure의 Managed/Exempt 빌드 게이트, 지형 대량 셀/공간 젖음 atlas, 인접 맵 전파, 독립 자식 논리 엔티티 분배, 원거리 proxy/날씨 이력 catch-up/영속 입력 dedup, 낙목 접촉 피해·최종 VFX/아트·P6 릴리스 인수는 별도 범위다. 언로드/시간 점프는 명시한 SuspendDomain 정책이며 대규모 월드 성능 검증 완료가 아니다.

## 단계 인계와 부산물

후속 준비표는 output/harness/20261112-205710-87e15461에 있다. 단계별로 위키와 상태를 갱신했으며 최종 산출물과 메타·링크 확인은 [최종 인계 기록](../../output/harness/20261112-214629-66481a0a/review.md)에 연결했다. 테스트 preview scene·메모리 객체·runtime 재질은 TearDown/finally/반환 경로에서 정리했다. 초기/최종 결과 JSON·GPU 캡처·생성 자산/meta·저작 원본 C#은 재현/인수 근거로 보존한다. 기존 사용자 변경과 다른 작업의 씬·아트·물/생물 변경은 보존했다. 커밋하지 않았다.


## 환경 에셋 치트 추가

- [기존 치트 콘솔](../../Assets/02.Scripts/Manager/Cheat/Editor/CheatConsoleWindow.cs)에 환경 테스트 진입점을 추가했다. 기존 [환경 반응 모니터](../../Assets/02.Scripts/Editor/ReactiveEnvironmentMonitor.cs)를 확장해 새 메뉴/매니저/런타임 UI 없이 제공한다.
- Editor Play 중 5종 샘플/사용자 지정 프리팹 소환, 전방 거리·Ground 탐색, 대상 목록/Hierarchy 선택, 열·물·구조 피해·남은 내구도 파괴, 소환한 개체의 교체/제거를 지원한다.
- [소환 치트](../../Assets/02.Scripts/Editor/ReactiveEnvironmentCheats.cs)는 플레이어 소유 씬에 복제하고 활성화 전에 새 GUID를 부여한다. 원본 프리팹과 정규 배치는 보존한다. 교체도 새 GUID다. 보상과 저장 원장은 실제 경로를 사용하므로 폐기 가능한 슬롯에서 사용한다. 이미 나온 보상은 되돌리지 않으며 런타임 개체를 다음 세션에 재소환하지 않는다.
- C# 오류 검색 0. [치트 단언](../../Assets/02.Scripts/Editor/ReactiveEnvironmentCheatTests.cs) 2개를 execute_code로 직접 호출해 통과했다(ID/씬 격리·원본 보존·입력 거부). Test Runner는 tests_running으로 요청을 거절해 새 실행 결과를 얻지 못했으며 다른 실행을 중단/초기화하지 않았다. [결과 기록](../../output/ReactiveEnvironment/cheat-checks.json). 이전 55건은 앞선 통합 스냅샷의 근거로 유지하며 이번 치트 결과에 합산하지 않는다.
- [사용법](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md#환경-에셋-테스트-치트), [RE-C01~03](reactive-environment-tests.md#치트-진입-확인-re-c0103)은 실제 Play 버튼·입력·반응 확인 대기다. 게임 내 F12 패널/Development Player 치트는 추가하지 않았다. MSAA/RenderGraph 오류 수정은 이 치트 작업에 포함하지 않았다.
- 새 C# meta는 Unity가 생성했고 테스트 preview scene/임시 staging은 finally로 해제한다. 씬/프리팹/구매 원본을 저장하지 않았으며 다른 작업 파일도 보존했다. Player/Addressables 빌드·커밋 미수행. 좁은 에디터 도구 확장이므로 새 준비표를 만들지 않고 기존 상태/저작 지침/분야 위키에 인계한다.


## 일반 씬·식생 적용성 검토

코드/저작 도구 정적 조사 결과, 반응 코어·명시 프로필·원소 입력·보상/저장은 일반 소품·나무에도 재사용 가능하다. 다만 원형/잔해 필수, 전용 셰이더/전체 LOD 바인딩, 물리 명중 Collider/레이어, GatheringActor 병용 금지, 재생성 배치 ID 보존 제약이 있어 단순 컴포넌트 일괄 부착으로 일반화되지 않는다.

정규 맵 지면 도구는 Terrain Detail/인스턴싱을 사용한다. 따라서 개체 소품/나무 경로와 밀집 풀의 셀 어댑터를 분리하고 반응 계산을 공유하는 방향을 권장한다. 현재 매 tick 무변경 ApplySnapshot과 객체별 재질 clone도 대량 적용 전 개선/측정 대상이다.

[설계 검토안](../../Assets/docs/design/REACTIVE_ENVIRONMENT_INTEGRATION.md#일반-씬-오브젝트식생-확장-검토안)에 대상별 적용 표·6가지 제약·3단계 도입/인수 기준을 기록했다. 이번 요청은 검토이므로 런타임·씬·프리팹을 변경하지 않았다. 코드 조사와 문서 링크 확인만 수행했고 컴파일/Play/성능 검사를 새로 실행하지 않았다. 일반화 구현·대량 식생 성능·사용자 인수는 미수행이다.
