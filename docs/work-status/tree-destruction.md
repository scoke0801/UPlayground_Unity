# LakeVillageWorld 일반 수목 파괴 연결

- 정본: [환경 통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md), [저작 지침](../../Assets/docs/guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md).
- 요청: 일반 나무를 실제 타격으로 파괴할 수 있게 한다. 보호 식생·채집 소유권·원본 구매 자산과 기존 배치 ID는 보존한다.
- 현재 단계: **일반 수목4,767개 낙하 완료 정책·드롭/FX·잔해 소거 구현 반영, EditMode80/80·본편 복제본15/15 통과**. 직접 공격 입력·KCC/Nav 체감·실제 저장/지역 왕복·성능·미술 인수는 사용자 테스트 대기다. [최신 구현·검증](#낙하-완료-수명-구현--2026-11-15).

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

