# LakeVillageWorld 일반 수목 파괴 연결

- 정본: [환경 통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md), [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md).
- 최신 확장: [EastLands 상호작용 수목 확장](#eastlands-상호작용-수목-확장).
- 요청: 일반 나무를 실제 타격으로 파괴할 수 있게 한다. 보호 식생·채집 소유권·원본 구매 자산과 기존 배치 ID는 보존한다.
- 현재 단계: **일반 수목4,767개 낙하 완료 정책·드롭/FX·잔해 소거 구현 반영, EditMode80/80·본편 복제본15/15 통과**. 직접 공격 입력·KCC/Nav 체감·실제 저장/지역 왕복·성능·미술 인수는 사용자 테스트 대기다. [최신 구현·검증](#낙하-완료-수명-구현--2026-11-15).

## EastLands 상호작용 수목 확장

- 2026-11-21 타입 저작을 반영했다. Dreamscape EastLands의 원형과 대응 밑동·절단 줄기를 재사용했다. 원형은 메시 LOD 3단계와 임포스터를, 절단 모델은 메시 LOD 3단계를 유지한다. [낙하 완료 설계](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md).
- 배치용 프리팹 3종은 [직립 침엽수](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsConiferStraight.prefab), [굽은 침엽수](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsConiferTwisted.prefab), [키 큰 대나무](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsBambooTall.prefab)다. 프리팹 원본의 배치 ID는 비워 두고 씬 배치가 ID를 소유한다. 이번 적용 범위는 신규 배치용 프리팹이다.
- 종별 절단 피벗과 로컬 단위 낙하 레시피를 사용한다. 타격은 줄기 Capsule만 받으며 물리 Collider의 원본 치수를 보존했다. 내구도 100, 목재 1~4개, 낙하 완료 후 보상·FX·정리를 기존 정책에서 가져왔다. 낙하가 처음부터 막히면 침엽수는 밑동과 짧은 통나무를, 대나무는 밑동을 사용한다.
- Unity에서 새 프리팹 3개, SO 6개, 파생 재질 10개를 생성했다. 구매 의존 파일과 meta 102개를 생성 전후 SHA-256으로 대조해 변경 0개를 확인했다. [저작 스크립트](../../Tools/ReactiveEnvironment/eastlands-tree-assets.csx).
- Editor EditMode 환경 통합 테스트 55/55 통과. 신규 프리팹 3종의 축소·확대·초기 막힘 9/9에서 `HitRequest` 내구도 감소, 절단 모델 전환, 낙하 중 JSON 포즈 복원, 목재 보상 1회 생성과 잔해 소거를 확인했다. [EditMode 결과](../../output/ReactiveEnvironment/EastLandsTrees/editmode.json) · [프리팹 재현 결과](../../output/ReactiveEnvironment/EastLandsTrees/combat-probe.json) · [저작 결과](../../output/ReactiveEnvironment/EastLandsTrees/creation.json).
- 실제 물리 공간에서 수목 3종 × 배율 0.5/1/2의 인접 배치 9/9를 검사했다. 전투 형상 질의는 접촉 줄기만 반환했고, 두 나무 사이와 비활성 원형 위치는 피격 대상이 없었다. 첫 물리 검사는 fixture 등록이 누락되어 전투 검출기의 대상 생존 조건을 만족하지 못했고, 등록을 연결해 다시 실행했다. [줄기 피격 결과](../../output/ReactiveEnvironment/EastLandsTrees/trunk-hit.json).
- Unity 프리뷰에서 원형·절단 포즈 6장을 확인했다. 누락 스크립트·재질과 반응 셰이더 오류가 없었다. 첫 캡처의 청록색 임시 셰이더는 동기 렌더링으로 다시 촬영해 교체했다. 원형과 절단 프리뷰는 [환경 에셋 카탈로그 후속 항목](#환경-에셋-카탈로그-수목-확장)에 모았다. 본편 씬의 조명, LOD 전환 및 실제 낙하 감각은 사용자 테스트 대기다.
- 사용자 테스트 대기. 날씨 서비스를 제공하는 월드의 별도 테스트 슬롯에서 세 프리팹을 일반 GameObject 배치로 놓고 고유한 SceneEntityId가 있는지 확인한다. **EL-T01** 키보드·마우스 또는 패드로 수관과 줄기, 인접 두 그루 사이를 각각 공격한다. 닿은 줄기만 피해를 받고 파괴 후 서 있는 Collider가 사라져야 한다. **EL-T02** 열린 곳과 벽 사이에서 각각 벌목하고, 목재가 한 번만 나온 뒤 잔해가 사라지는지 본다. **EL-T03** 낙하 중과 보상 획득 이후 각각 저장·로드하거나 지역을 왕복한다. 형상이 올바르게 복원되고 목재가 다시 생성되지 않아야 한다. 결과는 항목 ID, 통과·실패·미수행, 실제 결과와 입력 장치, 필요 시 영상이나 로그로 기록한다. Player·Addressables 빌드는 미수행이다.
- 보존 산출물은 신규 프리팹·SO·재질의 meta, [검증 준비표](../../output/harness/20261121-155455-fc957254/review.md)와 카탈로그·검증 근거다. 임시 Unity 검사 씬은 닫았다.

## 환경 에셋 카탈로그 수목 확장

- 카탈로그에서 대응 절단 모델이 있는 [목서](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsOsmanthus.prefab)와 [대나무 군락](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsBambooGroup.prefab)을 배치용 프리팹으로 추가했다. 군락의 세 줄기는 각각 피격과 물리 Capsule을 갖고, 한 번의 공격·파괴·보상·저장으로 처리된다. 같은 원형의 구매 의존 파일·meta 88개 해시 변경 0개를 확인했다. [저작 결과](../../output/ReactiveEnvironment/EastLandsTrees/asset-catalog-creation.json).
- 신규 프리팹의 축소·확대·초기 막힘 6/6에서 실제 `HitRequest`, 방향성 낙하, JSON 복원, 목재 보상 1회와 잔해 소거를 확인했다. 별도 물리 질의 6/6에서 인접 개체 제외, 원형 전환 뒤 피격 해제, 군락 세 줄기 각각의 피격과 광역 형상 검출 1회를 확인했다. [파괴·복원](../../output/ReactiveEnvironment/EastLandsTrees/asset-catalog-combat.json) · [줄기 검출](../../output/ReactiveEnvironment/EastLandsTrees/asset-catalog-trunk-hit.json).
- 당시 출력 폴더가 사라져 Unity 프리뷰로 상호작용 수목 5종 목록을 다시 구성했다. 이후 위키에서 3,176개 전체 목록을 찾아 [수목·소품 추가](#수목과-파괴-소품-추가)에서 양방향으로 연결했다. [검증 준비표](../../output/harness/20261121-173948-bb9897e4/review.md).
- 이번 반영은 배치용 프리팹까지다. 위 EL-T01~03을 두 신규 타입에 적용해 본편 공격 입력, 낙하·가독성, 지역 왕복을 확인한다. 대나무 군락은 각 줄기에 대한 개별 공격과 세 줄기를 모두 포함한 범위 공격을 각각 시도해 피해와 목재 생성이 한 번인지 확인한다. 사용자 테스트 대기다.

## 수목과 파괴 소품 추가

- Unity 배치용 [붉은 활엽수](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveFarmRedTree.prefab)와 [작은 대나무](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveEastLandsBambooSmall.prefab)를 추가했다. 붉은 활엽수는 Farmlands C 붉은 잎과 기존 절단 잔해·레시피를 재사용한다. 작은 대나무는 원형·절단 모델에 맞춘 독립 벌목 레시피를 사용한다.
- [나무통](../../Assets/03.Prefabs/Environment/ReactiveProps/PF_ReactiveFarmBarrel.prefab)은 내구도 50, 가연, 타격 파괴로 목재 1개다. [도자기 화병](../../Assets/03.Prefabs/Environment/ReactiveProps/PF_ReactiveFarmVase.prefab)은 내구도 25, 비가연, 타격 파괴 시 먼지 FX를 사용한다. 두 소품은 Swap 레시피로 원형 형상과 충돌을 함께 해제한다. [설계](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md).
- Unity 복제본에서 수목의 축소·확대·초기 막힘과 두 소품의 충돌 해제·정리·저장 복원 **8/8**, 소품의 발화·물 반응 **2/2** 통과. 테스트 드롭 서비스는 빈 보상도 확정 원장에 기록한다. 화병의 실제 보상 입력은 빈 표인지 별도로 확인했다. 시작 파일 해시는 저작 스크립트별 Farmlands 106개, EastLands 60개가 모두 일치했다. [파괴·복원](../../output/ReactiveEnvironment/CatalogExpansion-20261121/combat-probe.json) · [열·물 반응](../../output/ReactiveEnvironment/CatalogExpansion-20261121/element-probe.json) · [저작 결과](../../output/ReactiveEnvironment/CatalogExpansion-20261121/creation.json).
- [상호작용형 카탈로그](../../output/asset-catalog/index.html) 수목 7종·소품 2종. 프리팹 경로, 검색, 미리보기 16장과 [전체 환경 카탈로그 3,176개](../asset-catalog/index.html)의 양방향 링크를 검사했다. 추가 캡처 6장은 Unity에서 확인했다. [검증 준비표](../../output/harness/20261121-174811-802dfc95/review.md).
- 사용자 테스트 대기. EL-T01~03으로 두 수목을 확인한다. **EL-P01** 별도 테스트 슬롯에서 일반 공격·화염·물로 나무통과 화병을 시험한다. 나무통이 타격으로 부서지면 목재 1개가 나오며, 화병은 파괴 후 통행이 열리고 보상이 없어야 한다. 저장·지역 왕복 이후 파괴와 보상이 중복되면 실패다. 파괴 순간의 FX·소리·플레이 감각은 본편에서 확인한다. 이번 적용 범위는 배치용 프리팹이며 본편 배치와 Player 검사는 미수행이다.

## 전체 카탈로그 조사와 수목 확장

- [전체 환경 에셋 3,176개](../asset-catalog/index.html)의 경로를 대조해 모두 확인했다. 카탈로그의 나무 분류 270개와 소품 859개는 Unity에서 전부 로드해 Collider, LOD, 재질, 반응·채집 소유와 메시 참조를 조사했다. 원래 촬영에서 빠진 44개는 카탈로그의 제외 사유를 보존했다. 나무 분류에는 절단·잔해·임포스터 부품 91개와 묘목·관목·덩굴·낙엽 24개가 섞여 있다. [전수 조사 CSV](../../output/ReactiveEnvironment/AllTreeAudit/inventory.csv) · [수목 Unity 원본](../../output/ReactiveEnvironment/AllTreeAudit/unity-tree-prefabs.json) · [소품 Unity 원본](../../output/ReactiveEnvironment/AllTreeAudit/unity-prop-prefabs.json).
- EastLands의 구매 절단 대응 수목 17종을 실측하고 미연결 11종을 [배치용 상호작용 프리팹](../../output/asset-catalog/tree.html)으로 연결했다. 6종은 단일 상부 줄기, 5종은 상부 2~3개를 원래 절단 위치에 조립해 같은 피벗으로 넘긴다. 원본 의존 파일·meta 252개 저작 전후 해시 변경 0개. [절단 위치·규격](../../output/ReactiveEnvironment/AllTreeAudit/eastlands-geometry.json) · [저작 결과](../../output/ReactiveEnvironment/AllTreeAudit/eastlands-creation.json) · [Unity 파괴·복원 33/33](../../output/ReactiveEnvironment/AllTreeAudit/eastlands-combat.json).
- Idyllic의 수목 55개는 Unity 메시 서명으로 15개 형상 그룹을 확인했다. 그중 [전나무 Fir_02](../../Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveIdyllicFir02.prefab)는 Blender에서 원본 LOD0~2를 각각 절단해 밑동과 낙목 6개 메시를 만들었다. 원본 UV는 유지하고, 높이 1.08m·지름 약 0.39m 단면에 나이테 텍스처를 적용했다. 첫 낙목 캡처에서 늘어난 잎은 낙목 전용 재질로 교정했다. [Blender 제작 원본](../../Tools/ReactiveEnvironment/Source/Fir02_FallParts.blend) · [내보내기](../../Assets/08.Models/ReactiveTrees/Fir02_FallParts.fbx) · [근접 단면](../../output/ReactiveEnvironment/AllTreeAudit/PF_ReactiveIdyllicFir02-cap.png) · [Unity 타격·복원 3/3](../../output/ReactiveEnvironment/AllTreeAudit/fir02-combat.json).
- [상호작용형 카탈로그](../../output/asset-catalog/index.html)는 수목 총 19종과 기존 소품 2종이다. 이번 저작 범위는 배치용 프리팹이며 기존 월드·저장 슬롯의 배치는 그대로다. Idyllic 나머지 14개 메시 형상, Toon Golden Valley 수관 원형 44개와 별도 저작이 필요한 나무, 소품의 추가 파손 제작은 조사표에 남아 있다.
- 최종 환경 통합 EditMode **55/55** 통과. Unity 캡처 25장은 EastLands 신규 11종 원형·절단과 Fir_02 원형·절단·근접 단면이다. 실패 진단용 임시 스크립트와 바람 비교 캡처를 정리하고 Blender 제작본·FBX·텍스처·meta와 최종 검증 근거를 보존했다. [EditMode 결과](../../output/ReactiveEnvironment/AllTreeAudit/editmode.json) · [검증 준비표](../../output/harness/20261121-181203-1db43904/review.md).
- 사용자 테스트 대기. EL-T01~03으로 작은 대나무, 갈라진 침엽수, 목서, Fir_02를 열린 곳과 벽 주변에서 확인한다. 여러 줄기가 동시에 화면에 들어오는 상황에서 타격 범위와 목재 보상, LOD가 바뀌는 거리에서 단면과 수관, 쓰러지는 방향과 통로를 확인한다. 자동 복제본 결과는 본편 성능·미술·입력 인수를 대체하지 않는다.

### Fir_02 밑동 잎 정리

- 밑동 LOD0~2에서 절단선 아래에 남았던 잎 카드를 제거했다. Unity에서 모든 밑동 LOD의 나무껍질·나이테 슬롯과 1.08m 절단 높이를 확인했다. 낙목 3개 LOD는 기존 삼각형 수와 재질 연결을 유지했다. [정리 전](../../output/ReactiveEnvironment/AllTreeAudit/PF_ReactiveIdyllicFir02-cap.png) · [정리 후](../../output/ReactiveEnvironment/AllTreeAudit/PF_ReactiveIdyllicFir02-clean-stump.png).
- Blender 원본과 FBX, Unity 프리팹, 카탈로그의 절단 캡처를 갱신했다. 축소·확대·낙하 길막의 피격·보상·저장 복원 **3/3** 통과. 본편 조명과 실제 플레이는 사용자 테스트 대기다. [Blender 출력 검증](../../output/ReactiveEnvironment/AllTreeAudit/fir02-blender.json) · [타격·복원](../../output/ReactiveEnvironment/AllTreeAudit/fir02-combat.json) · [준비표](../../output/harness/20261121-182941-7f811131/review.md).

### Fir_02 밑동 근접 실루엣 개선

- 잎 정리 후 남은 22삼각형 밑동을 둥근 절단 둘레, 굴곡 있는 나무껍질과 지면으로 퍼지는 뿌리로 다시 저작했다. LOD0/1/2는 원주 48/28/16분할과 1,152/448/160삼각형이다. Blender에서 세 메시의 닫힌 형상과 바깥 방향을 검사하고 Unity에서 삼각형 수, 재질 슬롯, 1.08m 절단 높이와 접지 범위를 대조했다. [이전 근접 컷](../../output/ReactiveEnvironment/AllTreeAudit/PF_ReactiveIdyllicFir02-clean-stump.png) · [최종 근접 컷](../../output/ReactiveEnvironment/AllTreeAudit/PF_ReactiveIdyllicFir02-rounded-stump.png).
- FBX·Blender 원본과 Unity 프리팹을 갱신하고 카탈로그에 새 낙목 컷을 반영했다. Unity 복제본 파괴·보상·저장 복원 **3/3** 통과. 본편 지형의 뿌리 접지와 실제 카메라 외형은 사용자 테스트 대기다. [재현 결과](../../output/ReactiveEnvironment/AllTreeAudit/fir02-rounded-combat.json) · [준비표](../../output/harness/20261121-183423-d4cf9753/review.md).

## 조사 완료 — 2026-11-15

Unity MCP로 비재생·미저장 변경 없는 본편 씬을 읽었다. ArenaTree 프로필 대상7,403개 중 명시 FullTree 원본 GUID 5종의 일반 수목은4,767개(A848/B874/C587/붉은 C1,590/D868)다. 나머지는 관목·풀·갈대·밑동·통나무·던전 구성과 고사목3개이며 이름 일괄 판정으로 전환하지 않는다. 보호 프로필829개는 별도 보존한다.

FullTree와 대응하는 구매 Stump/Trunk에는 3단계 메시 LOD가 있다. A의 FullTree 자식에는 y=-0.5 오프셋이 있으므로 절단 모델에도 같은 오프셋을 적용한다. 원형의 메시·재질·LOD/impostor·배치는 보존하고 잔해만 추가한다. 크기가 다른 배치에서 기존 월드 단위 낙하 레시피를 그대로 공유하지 않도록 선택적 로컬 치수 필드를 추가한다. 기존 FarmTreeFall은 월드 단위 동작을 유지한다.

기존 저장 레코드는 지역+SceneEntityId와 definitionId를 검사한다. 화학 계수와 논리 definitionId=`environment.ArenaTree`를 유지한 표현별 전용 SO를 사용해 기존 환경 저장을 이어받는다. 공유 ArenaTree SO 자체는 바꾸지 않는다. 원본/잔해는 한 엔티티이며 별도 연료·보상 ID를 발급하지 않는다.

## 적용 결과

- `Assets/10.Datas/Environment/TreeDestruction/`에 종류별 반응 정의5개와 낙하 레시피5개를 만들었다. 내구도100, 기존 목재 보상1~4개를 사용한다. A의 실제 절단 높이0.72에 맞춰 검사 반경을 로컬0.6으로 저작했다.
- `Assets/03.Prefabs/Environment/Reactive/TreeRemnants/`에 구매 절단 줄기·밑동·막힘 통나무를 재사용한 잔해5종을 만들었다. 붉은 C는 원래 붉은 잎 재질 대응을 보존한다. 원형은 기존 3 mesh LOD와 impostor, 잔해는 각 모델의 3 mesh LOD를 사용한다.
- 일반 수목 루트를 유지한 채 인스턴스의 프리팹 연결을 풀고 기존 시각/Collider 자식을 Intact 아래에 묶었다. Remnant는 비활성 중첩 프리팹이며 SceneEntityId·WorldPlacementMetadata·씬 배치 소유권은 유지한다. 물리/타격/비·열·연소 앵커와 잔해 NavMeshObstacle을 같은 원장의 파괴 전이에 연결했다.
- 나머지 식생과 보호829개는 전환하지 않았다. 밑동·풀·관목을 이름으로 나무 취급하지 않으며, 기존 장식 고사목3개도 이번 적용에서 제외됐다.
- 고사목3개의 추가 교체형 파괴는 [별도 검토 스크립트](../../Tools/ReactiveEnvironment/dead-tree-destruction-apply.csx)까지만 작성했다. 실행 요청은 자동 승인 검토에서 기존 Renderer/Mesh/Collider 제거를 포함한 범위 확장이라는 이유로 거절됐다. 우회 실행하지 않았고 추가 자산/씬 변경도 하지 않았다. 사용자 승인 전까지 적용하지 않는다.

## 검증과 인계

### 낙하 완료 뒤 드롭·FX·정리 설계 — 2026-11-15

- 사용자 원문: “파괴 연출 - 부서진 나무가 넘어간다. 이게 끝나면 드랍아이템이 스폰되고 FX 를 뿌리면서 파괴된 나무줄기는 제거하는 방향성으로 자연스럽게 처리되는 방향성으로 설계 짛냉”.
- [설계 정본](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md)을 작성했다. 기존 설계·저작 지침의 영속 잔해 정책과의 경계를 함께 갱신했다. 일반 수목의 낙하→정착→드롭/FX→전체 잔해 소거를 정책으로 정하고 큰 밑동·수관까지 정리하는 기본안을 명시했다.
- 보상은 파괴 즉시에서 낙하 완료 후로 옮긴다. 정상 낙하·도중 막힘·처음부터 막힘 모두 안전 정착 뒤 한 번 마무리하며, 드롭 서비스 지연 때는 시각 정리와 보상 pending을 분리한다. 기존 sourceId/보상 원장·배치 ID를 유지하고 새로운 재가공 보상을 만들지 않는다.
- 소거 시 충돌·피격·Nav·차폐와 연료/열 전파를 함께 종료하고, 논리 파괴/보상 기록은 보존한다. 단계별 저장/로드·구버전 이미 파괴된 나무의 조용한 이관·정리 FX 재생 억제·그림자/LOD 소거를 포함했다.
- 상태: **설계 문서 반영, 구현/데이터 저작/자동 검사/실제 플레이 미수행**. 기존40건·15개 검사와 피격 수정 결과는 이 수명/보상 변경의 검증 근거로 승계하지 않는다. 초기 타이밍은 낙하2.0초, 착지 여운0.10초, 소거0.45초이며 SO 저작 후 플레이로 조정한다.
- 이번 작업은 문서와 로컬 링크/diff만 검사했다. C#·SO·프리팹·씬·Unity 상태·다른 MCP 작업은 변경하지 않았다. 임시 산출물 없음. 다음 단계는 설계8절의 순서대로 상태/보상/저장부터 구현하는 것이다.

### BUG-DT-02 / FEEL-DT-01 — 옆 나무 피격과 큰 잔해

- 사용자 원문: “절단되고 난 이후의 나무가 거슬려. 일반적으로 파괴를 지원하는 게임에서 어떻게 처리하는 지?” / “그리고 피격에 의해 부서지는 판정이 이상해. 1번을 부셨는데 2번이 부셔지고있어”. [표시한 실제 플레이 사진](../../output/ReactiveEnvironment/TreeDestruction-20261115/user-hit-and-remnants.png)을 보존했다.
- BUG-DT-02: 결함/S2·실패 접수. LakeVillageWorld에서 표시1을 타격했는데 표시2가 파괴된다는 제보다. 빈도·무기·입력 장치·저장 슬롯은 모름, 이번 파괴 연결 뒤 관측한 회귀다. 기대는 실제 타격 범위 안의 나무만 피해를 받는 것이다.
- 원인 근거: `VegetationReactionAuthoring`은 모든 시각 LOD와 impostor를 감싼 Box trigger를 만들고 타격 허용 목록에 넣었다. 실제 일반 나무의 로컬 크기는 A28.04×38.90×26.97, B23.41×23.41×11.96, C/붉은 C38.86×38.86×19.33, D35.72×35.72×18.38이다. 배율이 적용된 넓은 상자끼리 겹쳐 빈 공간의 공격도 이웃 엔티티로 전달된다. 일반 공격의 InteractableObject 조회를 유지하면서 서 있는 나무의 입력을 저작한 줄기 치수의 capsule로 한정한다. 원래 물리 MeshCollider·일반 다중 타격 정책은 보존한다.
- 검증된 수정: [전환 원본](../../Tools/ReactiveEnvironment/tree-destruction-apply.csx)과 [기존 배치 수정](../../Tools/ReactiveEnvironment/tree-trunk-hit-apply.csx)은 넓은 Box를 비활성화하고 입력 허용 목록에서 제외한다. 기존 물리 MeshCollider는 유지하되 피격 입력과 분리하고, InteractableObject 층의 줄기 Capsule을 허용한다. 치수는 기존 SO의 trunkRadius/trunkLength와 저작한 절단 피벗을 사용한다.
- [실제 물리 질의 재현15개](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/probe.json) 최종 통과: [본편 나무5종×배율0.5/1/2 복제본](../../Tools/ReactiveEnvironment/tree-trunk-hit-probe.csx)을 독립 일반 Scene의 기본 물리 공간에 두고 실제 CombatHitDetector.CollectShapeHits를 호출했다. 이전 상자에서는 이웃도 맞고, 줄기 수정 후에는 첫 나무만 내구도75·이웃100이었다. 이어 첫 나무를 파괴하고 같은 지점에서 다시 질의해도 이웃을 맞히지 않았다. preview Scene은 기본 Physics 질의에서 제외되므로 첫 보조 검사의 재현 실패를 통과로 삼지 않고 실제 물리 Scene에서 다시 확인했다. fixture와 충돌 해석 캐시의 해당 복제본 항목은 finally에서 정리했다.
- 최종 적용 상태: **4,767/4,767개 줄기 피격 적용·저장 확인, 미적용0**. [전수 입력 검사](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/audit.json)는 유효4,767·오류0·dirty=false다. 원래 물리 MeshCollider가 없는 장식 배치214개는 새 장애물을 강제하지 않고 그대로 보존했다. 초기 검사에서 Collider 존재를 요구해214건을 잘못 실패시킨 조건은 원래 상태와 비교하는 방식으로 수정했다.
- [물리 원형 전후 대조](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/physical-mesh-preservation.json): 기존 MeshCollider17,920개 직렬화 블록의 변경/추가/누락0. [구성 전수](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/authoring-audit.json): 전체 식생15,396·보호829·중복 ID0·구성 오류0. [원형 대조](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/preservation.json): 대상 나무4,767개 ID/외형 변경0. 초기 기준 대비 비대상 `Sodam_Herbs_R2`의3개 LOD 행렬 차이는 별도 기록하고 건드리지 않았다. 구매 원본/meta234개 [해시 변경0](../../output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/source-preservation.json).
- 중단/재개 이력:1,000개 저장 뒤 MCP 연결 종료/10053·상태 조회 시간 초과로 멈췄다. 사용자 지시 후 실제 상태를 다시 조회해 지연 요청까지2,000개 반영된 것을 확인했다. Play/컴파일/임포트/테스트 중이거나 MCP 대기열에 다른 명령이 있으면 적용을 시작하지 않도록 검사하고250개 단위로 남은 배치를 처리했다. 열린 Editor 하나를 유지했고 다른 Editor 기동·강제 재시작·캐시 삭제는 하지 않았다.
- 사용자 데이터 보존: 미저장 사용자 씬을 `hit-fix/scene-with-user-changes-before.unity`(355,039,436바이트)에 보존한 뒤 저장했다. 원래 디스크 씬은 `hit-fix/scene-before.unity.gz`(31,619,408바이트)에 별도 보존했다. 둘 다 비추적 사용자 변경의 복원용이므로 삭제하지 않았다. 재현 fixture 외 임시 씬은 남기지 않았고 스크립트·대조 JSON은 재검증 근거로 보존한다.
- 남은 확인: 사용자가 원래 표시1 타격/표시2 보존과 밑동 앞 헛스윙을 재시험한다. 자동 검사는 실제 입력 통과가 아니다. 이번 변경은 씬 입력 Collider/허용 목록과 Assets 밖 csx뿐이므로 컴파일·Test Runner 전체 재실행·Player 빌드는 하지 않았다.
- FEEL-DT-01: 큰 밑동과 수관이 시야·이동을 방해한다는 체감 의견이다. 이후 사용자 지시로 [낙하 완료 뒤 드롭·FX·정리 설계](#낙하-완료-뒤-드롭fx정리-설계--2026-11-15)를 정본으로 확정했다. 현재 런타임은 아직 영속 잔해이며 새 수명 정책 구현은 대기다.

### BUG-DT-01 — 파괴 잎에 나무껍질 재질이 붙음

- 사용자 원문: “부숴진 나무 형태는 왜 이래?” 및 추가 사진1장. 사용자 실제 플레이 사진2장을 [첫 사진](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/user-before-01.png)·[추가 사진](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/user-before-02.png)으로 보존했다.
- 분류: 결함/S3. 사용자 제보 당시 실패, 현재 **수정 반영·자동 대조 통과·사용자 재시험 대기**. 파괴 후 수관의 판 형태 전체가 나무껍질로 보여 화면을 가린다. 씬 LakeVillageWorld, 캐릭터는 사진상 검은 장발 모델이며 식별 ID·무기·입력 장치·저장 슬롯·빈도는 모름. 이번 파괴 잔해 연결 후 관측한 회귀다.
- 원인 확인: 원본 FullTree/Trunk는 종류와 LOD마다 줄기/잎 슬롯 순서가 다르다. 생성기가 FullTree LOD0의0번=줄기,1번=잎, Trunk LOD1의0번=먼 잎으로 고정해 재질 대응표를 덮어썼다. C의 LOD0, B/C/붉은 C/D의 다른 LOD 등에서 서로 다른 표면 재질을 연결한 것이 실제 자산 조회로 확인됐다.
- 수정: [생성기](../../Tools/ReactiveEnvironment/tree-destruction-assets.csx)를 원본 Material 참조 기준으로 바꿨다. 붉은 C의 Close/Far 잎은 원본 GUID 두 쌍으로 명시한다. [수정 실행](../../Tools/ReactiveEnvironment/tree-remnant-material-repair.csx)은 프로젝트 잔해5종의60슬롯을 검사해 잘못된4종의34슬롯을 수정·저장했다(A는 이미 맞아 변경0). 당시 실행 중인 복제본도29,195 Renderer/35,426슬롯을 바로 수정했다. 씬·저장 슬롯은 저장하지 않았고 배치 ID·물리·내구도·보상은 바꾸지 않았다.
- [수정 원본 보고](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/repair.json)와 `material-fix/prefabs-before/`의 프리팹/meta4종 사본은 수정 전 근거로 보존한다. [슬롯 검사](../../Tools/ReactiveEnvironment/tree-remnant-material-audit.csx) 결과는 [자산60슬롯·실행4,767그루/57,204슬롯 모두 불일치0](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/audit.json)이며 sceneDirty=false다. [구매 원본/meta234개 해시 변경0](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/source-preservation.json).
- [수정 반영 뒤 실제 Game View](../../output/ReactiveEnvironment/TreeDestruction-20261115/material-fix/game-after-material-fix.png)는 플레이 중 카메라/입력을 바꾸지 않고 AI가 촬영한 화면이다. 사용자 제공 전 사진과 위치·시각이 같은 대조 촬영은 아니므로 최종 미술 인수로 처리하지 않는다.
- 재검증: 사용자는 같은 나무를 다시 보고 새 나무도 파괴해, 잎이 나무껍질 판으로 보이지 않는지 근/중/원거리에서 확인한다. 현재 Play에도 수정 재질을 반영했으므로 재시작을 선행 조건으로 두지 않는다. 사용자 재확인 전에는 해결 완료로 표시하지 않는다. 이번 변경은 재질 연결과 Assets 밖 저작/검사 csx뿐이어서 Play 종료·스크립트 컴파일·Test Runner 재실행·Player 빌드는 하지 않았다. 위40건/15개는 앞선 기능 검사이며 이번 외형 결함의 통과 근거로 재사용하지 않는다.

### 제외 고사목 실제 배치 사진 — 2026-11-15

출처 확인: 고사목은 외부 구매 나무가 아니라 프로젝트 `ArtSource/PoorVillage/build_poor_village.py`의 `Terrain_DeadTree()`(687행)에서 줄기·드러난 뿌리·부러진 가지를 절차적으로 만드는 Blender 저작 원본이 있다. 출력 메시 `Assets/08.Models/PoorVillage/Terrain_DeadTree.fbx`를 `Assets/03.Prefabs/Environment/PoorVillage/Terrain_DeadTree.prefab`이 참조한다. 프리팹 재질은 `Assets/10.Datas/World/PoorVillage/Houses/PoorHouse_Wood_Weathered.mat`다. 제작 미리보기 보고는844삼각형으로 기록한다. 이번 조회로 원본 작성자의 신원까지 확정하지는 않는다.

사용자 요청으로 Unity Scene View에서 원래 배치를 촬영했다. 잎 없이 회색 줄기·부러진 가지가 남은 같은 Terrain_DeadTree 모델3개다. 오브젝트·씬·에셋을 수정하지 않았고 촬영 후 뷰를 복원했다. 본편 dirty=false를 확인했다. 이 촬영은 고사목 전환 승인이 아니며 고사목은 비파괴 상태다.

| 배치 | 위치 XYZ | 실제 Unity 사진 |
|---|---|---|
| empty-space-0031_Terrain_DeadTree | 1073.00, 189.83, 1369.00 | [사진1](../../output/ReactiveEnvironment/TreeDestruction-20261115/photos/dead-tree-01-clear.png) |
| marked-0018_Terrain_DeadTree | 391.00, 155.46, 857.00 | [사진2](../../output/ReactiveEnvironment/TreeDestruction-20261115/photos/dead-tree-02-clear.png) |
| marked-0024_Terrain_DeadTree | 1376.00, 137.51, 1122.00 | [사진3](../../output/ReactiveEnvironment/TreeDestruction-20261115/photos/dead-tree-03.png) |

잘못된 프레이밍과 잎에 가린 중복 촬영2개는 삭제하고 위 실제 배치 사진3개를 인수 근거로 보존했다.

### 현재 수행 결과

- [레시피](../../Assets/02.Scripts/Data/Environment/EnvironmentDestructionRecipeSO.cs)에 선택적 로컬 치수 필드를 추가하고 [낙하 검사](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/ReactiveEnvironmentDestruction.cs)에 반영했다. 기존 레시피는 기본값 false로 기존 월드 단위 검사를 유지한다.
- [기존 통합 검사](../../Assets/02.Scripts/Editor/ReactiveEnvironmentIntegrationTests.cs)에 작은 배율·큰 배율·기존 월드 단위의 길이 경계 회귀3건을 추가했다. **Test Runner40/40 통과**, 실패/건너뜀0. [개별 결과](../../output/ReactiveEnvironment/TreeDestruction-20261115/editmode-results.json), job=`d513b5e08e6046b0bf7cf10af0d88f5e`.
- [전수 구성 검사](../../output/ReactiveEnvironment/TreeDestruction-20261115/authoring-audit.json): 식생15,396개 유효, 파괴4,767개, 보호829개·파괴 보호0, 누락/중복 ID0, 구성 오류0, 씬 dirty=false.
- [원형 보존 대조](../../output/ReactiveEnvironment/TreeDestruction-20261115/preservation.json): 전체15,396개 배치 ID·이름·위치·회전·배율·활성·원형 Renderer 수·메시·재질·월드 행렬 변경0. 프리팹 연결을 해제한 Unity GlobalObjectId와 별개로 저장의 SceneEntityId를 유지했다. 구매 원본과 meta234개 [해시 변경0](../../output/ReactiveEnvironment/TreeDestruction-20261115/source-preservation.json).
- [실제 피격 경로 보조 검사](../../output/ReactiveEnvironment/TreeDestruction-20261115/combat-probe.json): 본편 나무5종을 기존 preview fixture에 복제해 종류별 작은/큰 배율의 열린 공간·막힘, 총15개 시나리오 통과. Collider→CombatHitDetector→AttackData/HitRequest→ReceiveHit에서 내구도75/50/25/0, 파괴 전환·원형 Collider 비활성·낙하/막힘 분기·중간 낙하 JSON 포즈 복원을 확인했다. API 입력·합성 배치이며 플레이어 직접 입력 시험은 아니다.
- Unity 최종 컴파일 중=false·컴파일 실패=false. 실제 저장 직후와 최종 검사에서 본편 씬 dirty=false를 확인했다. 검사 fixture는 finally에서 해제했으며 본편을 파괴하거나 사용자 저장 슬롯에 쓰지 않았다.
- 첫 길이 회귀 검사 실패는 비활성 장애물 fixture 때문이어서 활성화 후40/40으로 재검증했다. 보조 검사 초기에 JsonUtility가 null 저장 서명을 바꿔 거부된 것은 도구 입력 문제였다. 실제 저장과 같은 Newtonsoft 경로로15개 모두 통과했으며 런타임 저장 검증을 완화하지 않았다. Console의 해당 실패 기록은 사용자 로그와 함께 보존했다.
- [자산 생성](../../Tools/ReactiveEnvironment/tree-destruction-assets.csx)·[씬 적용](../../Tools/ReactiveEnvironment/tree-destruction-apply.csx)·[전수 검사](../../Tools/ReactiveEnvironment/tree-destruction-audit.csx)·[피격 검사](../../Tools/ReactiveEnvironment/tree-destruction-combat-probe.csx)는 재현용으로 보존한다. 생성기는 기존 파일을 덮어쓰지 않으며 적용기는 기존 연결을 건너뛰는 방식이다.
- 작업 중 Play 전환의 응답 없음과 MCP 연결 종료가 있었다. Edit Mode 복귀 후 최신 사용자 씬을 다시 백업하고 이어서 적용했다. 전송 실패는 저장된 진행과 씬을 대조했으며 재전송돼도 이미 연결한 나무는 다시 만들지 않았다. 다른 Editor 기동·강제 종료·캐시 삭제는 하지 않았다.

- 사용자: Boot의 테스트 슬롯에서 일반 공격으로 A/B/C/붉은 C/D를 타격한다. 누적 피해로 내구도가 줄고 파괴 후 서 있는 나무·입력 Collider가 사라지며 밑동/낙목 또는 막힘 잔해로 전환되어야 한다. 목재 보상이 중복되지 않아야 한다. 자세한 [DT01~04 명세](reactive-environment-tests.md#일반-나무-파괴-연결-인수--2026-11-15)를 따른다.
- 잔해 Renderer42,903개가 비활성 구성으로 추가됐다. 실제 추가 메모리·초기 등록/강수 비용·낙하 중 Nav carving과 목표 기기 FPS는 측정하지 않았으며 종전 대기 성능 인수를 승계하지 않는다.
- Player/Addressables 빌드는 실행하지 않는다.

## 부산물·원본 보존

- 씬과 새 잔해 프리팹은 Git 무시 대상이다. 줄기 피격 수정 후 씬은357,626,607바이트로 저장했다. 생성한 SO/프리팹과 meta는 로컬 인수 대상이다.
- `output/ReactiveEnvironment/TreeDestruction-20261115/`의 작업 시작 전 씬과 대기 중 외부 변경을 보존한 적용 직전 씬 사본2개(합547,110,650바이트)는 Git 비추적 사용자 변경 복원용이므로 자동 삭제하지 않는다. baseline/current JSON은 전수 보존 대조의 원본 근거다. 로그·결과·구매 원본 해시와 함께 보존하며 삭제할 재생성 빌드/캐시는 없다.
- 준비표: `output/harness/20261115-153004-c28489c2/review.md`.
- [월드 위키](../wiki/world-art.md).
## 낙하 완료 수명 구현 — 2026-11-15

- 정본: [낙하 완료·보상·잔해 정리](../../Assets/docs/design/world-art/environment/TREE_DESTRUCTION_COMPLETION_DESIGN.md).
- 현재: **런타임·일반 수목5종 SO 반영·관련 자동 검사 통과, 사용자 테스트 대기**. 대상4,767개(A848/B874/C587/붉은 C1,590/D868)의 기존 레시피를 갱신했고 이번 작업에서 씬·배치 ID는 저장/변경하지 않았다. 다른 MCP의 폭포·동굴 씬 저장은 보존했다.
- [최종 본편 연결 감사](../../output/ReactiveEnvironment/TreeCompletion-20261115/final-audit.json): 대상4,767개·레시피5개, 잘못된 정책/레시피0·완료 FX 누락0·중복 ID0, Edit Mode/컴파일 종료·씬 미저장 변경0. 최종 준비표의 지정 소스·에셋/meta 해시 일치 확인.
- `EnvironmentDestructionRecipeSO`의 명시 완료 정책과 `EnvironmentDestructionSaveState.completion` 하위 버전을 추가했다. 누락된 구버전만 이관하며 명시된 잘못된 버전·단계·시간·포즈는 로드 교체 전 거부한다.
- 기존 중앙 작성자 `WorldStateManager.EnvironmentCompletion`에서 Falling→Settled→Clearing→Cleared를 진행한다. Clearing부터 보상 요청·입력/화학/강수/전파 종료, 단말 보상 pending 유지. 복원은 FX를 다시 실행하지 않는다.
- 전체 잔해의 Collider/Nav 해제와 고정 절단점 보상 앵커, 공유 GPU thermal.z의 독립 소거량, 줄기 그림자/깊이/노멀/모션 패스를 연결했다. 구매 원본과 배치 ID는 변경하지 않았다.
- 자동 검사: 최종 소스/셰이더 반영 후 환경 통합·표면·연출 EditMode **80/80 통과**([원본](../../output/ReactiveEnvironment/TreeCompletion-20261115/editmode-final-80.json), 실패0/건너뜀0). 본편5종 복제본×배율0.5/2/초기막힘 **15/15 통과**([원본](../../output/ReactiveEnvironment/TreeCompletion-20261115/combat-probe.json), [재현 도구](../../Tools/ReactiveEnvironment/tree-completion-combat-probe.csx)). 실제 HitRequest·JSON 포즈·드롭 생성1회·단말 복원·충돌/Nav/렌더 종료를 확인했다. 저장을 보상 전으로 되감은 막힘 사례는 동일 sourceId로 요청2회·생성1회다. 예상된 실패 주입 로그는 남아 있으므로 Console 전체 오류0이라고 표현하지 않는다.
- 독립 QA의 강수 Transform 등록 잔류·지역 좌표 변환 누락·미초기화/수직 방향 저장 허용을 수정했고 관련 검사 통과. 최종 읽기 전용 재검토에서 추가 확정 결함 없음. 원래 영속 잔해의 즉시 보상 경로도 통과했다.
- GPU 확인: 기존 생성기의 피벗 상수 좌표를 수정했다. 나무껍질·잎 파생2종36개 표면 패스에 실제 픽셀 위치를 전달하고, 큰 좌표의 sin 정밀도 손실을 피하는 독립 수명 디더로 변경했다. URP Lit 파생도 그림자/깊이/노멀/모션 패스를 유지한다. 기본/공유 버퍼 셰이더 변형 **58패스 컴파일·실패0·shader 오류0**([원본](../../output/ReactiveEnvironment/TreeCompletion-20261115/shader-pass-result.json)).
- 본편 A종 복제본의 실제 메시/재질을 x/z30,000m에서 GPU 렌더링했다. LOD0/1/2 픽셀은 시작81,518/80,060/71,860→중간69,633/68,161/55,958→완료0/0/0이다. [수치](../../output/ReactiveEnvironment/TreeCompletion-20261115/visual-result.json), [중간 LOD0](../../output/ReactiveEnvironment/TreeCompletion-20261115/fade-1-lod0.png), [완료](../../output/ReactiveEnvironment/TreeCompletion-20261115/fade-2-lod0.png), [재현 도구](../../Tools/ReactiveEnvironment/tree-completion-visual-check.csx). AI가 이미지를 직접 확인했다. 제어 조명·고정 단계의 메시 캡처이며 본편 조명/실제 프레임의 FX·소리·그림자 품질이나 GPU Resident Drawer Play 인수를 뜻하지 않는다.
- 시간/FX 저작: 기존 낙하2초 유지, 초기 막힘0.35초, 착지 여운0.10초, 페이드0.45초. 기존 `PF_EnvironmentWoodChips`·`PF_EnvironmentLandingDust`·낙엽과 목재 소리를 기존 풀/거리/음향 서비스로 사용한다. `TreePresentation.asset`의 추가 완료 필드만 저작했고 구매 원본·재질 슬롯을 바꾸지 않았다. [정책/FX 적용](../../Tools/ReactiveEnvironment/tree-completion-apply.csx), [기존 셰이더 이관](../../Tools/ReactiveEnvironment/tree-completion-shaders.csx).
- 다른 MCP의 폭포/동굴 작업·컴파일 때 대기하고 저장 상태·MCP 유휴를 확인한 뒤 이어 진행했다. Editor 재시작·잠금 해제·다른 작업 변경 원복은 하지 않았다. [TD-C01~06 실제 인수](reactive-environment-tests.md#낙하-완료드롭fx소거-사용자-인수--2026-11-15)는 사용자 테스트 대기다.
- 준비표: 최초 변경 전 `output/harness/20261115-174251-e96cae59/`, 최종 산출물 전수 `output/harness/20261115-181545-2a18d70e/`. 다른 MCP의 폭포 편집·NavMesh 갱신·최종 저장을 기다린 뒤 시작했다. Player/Addressables 빌드·커밋 없음.
- 부산물: 프로젝트 소유 URP 파생 패스4개와 `.meta`·재현 csx는 최종 소스이며 보존한다. 진단용 PNG3개/JSON1개와 중간 editmode-76.json은 참조/사용이 끝나 삭제했다. 최종 원본 근거14개943,348바이트와 준비표는 보존한다. 문서 로컬 링크 누락0, 관련 diff 공백 오류0이다.

