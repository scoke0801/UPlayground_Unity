# 반응형 환경 저작

[설계 계약](../design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md) · [상태/검증](../../../docs/work-status/reactive-environment.md) · [사용자 테스트](../../../docs/work-status/reactive-environment-tests.md)

## 프로필과 원소 입력

ReactiveEnvironmentDefinitionSO에서 definitionId, canIgnite/canBreak, 연료/열/젖음/그을림 계수, 종류별 구조 배율, 전파 거리/rate/벽/surfaceLayerId와 원인별 보상을 명시한다. 물체 이름·색·외형에서 가연성이나 보상을 추정하지 않는다. canBreak=false인 소품은 원형·잔해·파괴 레시피가 필요 없다. 젖기만 허용하려면 canIgnite=false, initialFuel=0, scorchPerHeat=0으로, 그을림도 허용하려면 scorchPerHeat를 별도로 저작한다.

GAS는 Ability Editor의 Payload → baseInfo → hitPhases → environment에 kind/heatImpulse/waterImpulse를 저작한다. 기본 0은 원소 없음이다. impulse에 deltaTime을 곱하지 않는다. 기존 근접/명시 형상·잔상의 HitRequest는 환경 payload를 보존한다. 지속 공급은 EnvironmentElementEmitter의 radius/heatPerSecond/waterPerSecond/lifetime을 쓴다. 같은 외부 공격에는 TryApplyImpact의 실행 ID+sequence를 재사용하고 다음 연타만 sequence를 늘린다.

## 선택 대상과 프리팹 저작

1. 툴 런처 → 월드/맵 → **반응 환경 저작**을 연다. 구매 프리팹은 Project에서 선택하고 **프로젝트 wrapper 생성**으로 Assets/03.Prefabs/Environment 아래 새 경로에 감싼다. 기존 파일을 덮어쓰지 않는다. 구매 원본 프리팹·재질을 직접 수정하지 않는다.
2. wrapper의 Prefab Mode 또는 씬 인스턴스 루트를 선택한다. 프로필·날씨를 지정하고 **변경 범위/누락 미리보기**를 확인한다. 연결은 선택 루트에만 적용한다. 프로필·SceneEntityId·열/비 샘플·피격 후보를 연결하며 Collider 형상·재질·잔해를 자동 제작하지 않는다. 적용은 Undo 그룹 하나이며 실패 시 전체 롤백한다. Prefab Mode의 저장은 Unity의 기존 저장/취소를 사용한다.
3. Inspector의 surfaceBindings에 반응할 Renderer와 재질 slot index를 넣고, 같은 Renderer의 나머지 슬롯은 excludedSlots에 넣는다. 통째로 제외하는 Renderer는 excludedSurfaces에 지정한다. 모든 비활성 형상과 LOD를 포함한다. 미지원 재질을 조용히 무시하지 않는다. 기존 surfaces 배열은 전체 슬롯 호환용이며 신규 저작은 명시 바인딩을 쓴다.
4. 호환 재질은 프로젝트 소유 복사본에 Reactive Toon을 지정하고 원본 BaseMap/BaseColor/Normal/UV/알파를 확인한다. Body/Foliage/Interior, 양면·AlphaClip·바람·연료 coverage는 작성자가 결정한다. 결합된 줄기+잎 슬롯 전체를 Foliage coverage로 지정하면 줄기까지 사라질 수 있으므로 금지한다. 소거하는 잎 Renderer에는 게임플레이 Collider를 두지 않는다.
5. hitColliders에는 실제 타격을 받을 Collider만 넣는다. 해당 Collider의 가까운 ReactiveEnvironmentObject가 같은 root여야 한다. InteractableObject 등 기존 전투 조회 레이어를 확인한다. 반응 목록 밖의 Collider는 물리/차폐용으로 둘 수 있다. 열/비 샘플은 실제 모델의 표면·전파 위치를 대표하도록 옮기고 비활성 원형 대신 활성 잔해에도 별도 샘플을 둔다.
6. **구성 검사**로 표면/슬롯/LOD를 확인한다. 프리팹 원본 GUID는 비워 두고, 씬 인스턴스의 기존 GUID는 유지한다. 등록 단계에서 실제 날씨·소유 지역·배치 ID·프로필·파괴·채집 구성을 다시 검사한다. 등록 실패는 성공이 아니다.

씬 프리팹 저작은 누락된 WorldPlacementMetadata에 DirectPrefab/RuntimeData를 연결한다. 기존 Map Placement Tool → Bake의 sceneEntityGuid/placementGuid를 보존하고 RuntimePlacementLoader로 재생성한다. 이미 발급한 레코드를 삭제 후 새로 만들거나 위치 해시로 대체하지 않는다. 단순 씬 오브젝트를 재생성하려면 먼저 프로젝트 프리팹과 배치 레코드로 확정한다.

## 나무 파괴와 채집

파괴를 켠 대상만 root 아래 독립 원형/잔해, 유효한 EnvironmentDestructionRecipeSO, ReactiveEnvironmentDestruction을 연결한다. root 자체를 원형/잔해/화염으로 지정하지 않는다. 밑동·수관·통나무·모든 LOD를 한 root 상태로 묶는다. DirectionalFall은 실제 모델의 절단 높이·낙하 pivot·줄기 길이/반경·장애물 검사 레이어·막힘 fallback을 명시한다. 이를 모델 bounds나 이름에서 자동 확정하지 않는다. 별도 통나무 엔티티로 초기 연료/보상을 다시 만들지 않는다.

원형/잔해의 Collider·지붕 shelterColliders·standing/remnant NavMeshObstacle을 같은 파괴 전이에 연결한다. 게임플레이 잔해에 TTL을 붙이지 않는다. 낙하 중 저장/로드, KCC 접촉, Nav carving 지연과 통로는 실제 플레이로 확인한다. 새 구조물은 [카메라 가림 체크리스트](CAMERA_OCCLUSION_FADE_GUIDE.md)를 따른다.

GatheringActor와 함께 쓸 때는 ownsGathering을 켜고 canBreak=true로 설정한다. 체력·소모·보상·저장은 환경 원장이 소유한다. 채집의 충격 보상은 기존 InteractableActorSO.dropItems를 재사용하며 별도 환경 충격 보상표에 복제하지 않는다. 화재 보상은 프로필에서 별도로 정한다. 낚시와 regrowGameMinutes>0은 현재 거부된다. 기존 채집 완료 저장은 같은 ID의 파괴/보상확정으로 인계되므로 새 ID를 발급해 중복 지급을 유발하지 않는다.

## Terrain 풀

1. 실제 Terrain을 선택하고 저작 창에서 **셀 어댑터 추가**를 실행한다. TerrainDetailReaction Inspector에 프로필·날씨·detailLayers·같은 순서의 호환 mesh prefab·detailPixelsPerCell·cellRegion·maximumCells·interactionHeight를 저작한다. 풀 셀 프로필은 canBreak=false/레시피 없음/드롭 없음이어야 한다. 잎이나 셀마다 컴포넌트/Collider를 붙이지 않는다.
2. 각 Detail 레이어에 원래 메시·스케일·알파·색을 보존하는 프로젝트 소유 툰 프리팹을 지정한다. 프로토타입은 Terrain 전체 레이어의 표현이므로 구역 밖의 기본 외형도 확인한다. 대상 셀 밖의 반응 마스크는 0이며 다른 레이어는 보존한다.
3. **원본 revision 기록**을 눌러 실제 TerrainData GUID+dependency hash를 저장한다. **셀 구성 검사**로 범위·예산·프로토타입·날씨·ID를 확인한다. 원본 데이터·밀도·해상도·레이어 순서·셀 크기/높이 정책을 바꾸면 partitionVersion과 revision을 함께 검토한다. 불일치 저장은 거부되며 자동 초기화하지 않는다. 이관 또는 새 테스트 슬롯을 사용한다.
4. Play에서 원본 TerrainData가 runtime 복제본으로 바뀌고 종료/언로드에서 원본으로 돌아오는지 확인한다. 밀도는 원본 밀도×연료 coverage로 바뀌며, 젖음/그을림/연소는 셀 마스크로 보인다. 범위 공격은 Sphere/회전 Box/Capsule과 셀 부피를 검사하며 같은 공격의 sweep는 기존 중복 제거를 공유한다. 정규 투사체 폭발/장판도 셀에 입력하고 대상별 쿨다운을 공유한다. 범위 없는 직격 전용 투사체에는 emitter/범위 입력이 필요하다.
5. 저장 ID는 Terrain 배치 ID+전역 셀 좌표다. 원본 레코드 ID와 구획을 재진입/맵 재생성에서 유지한다. 저장/로드는 잔여 tick·pending 입력까지 기존 원장을 사용한다. 언로드 중 catch-up은 하지 않는다. Terrain Tree는 보존만 하며 GameObject 나무로 자동 전환하지 않는다.

실제 LakeVillageWorld의 TerrainData는 WorldThemesABC/Terrain.asset, Terrain 위치는 (0,0,-700)이다. [LakeVillageDetails preset](../../10.Datas/Environment/Profiles/LakeVillageDetails.preset)은 Detail 레이어 1의 (116,102)부터 4×4셀, 셀당 4×4 Detail pixel을 지정한다. 조사 시 해당 구역 원본 밀도 합은 367이다. 전용 [TerrainGrass 프로필](../../10.Datas/Environment/Profiles/TerrainGrass.asset)은 약 9.4×10.9m 셀 간격에 맞춰 spreadRadius=16m, spreadHeatPerSecond=3을 명시했다. 작은 GameObject 풀의 4m 프로필과 분리하며, 실제 Terrain에서 인접 셀이 연료 소진 전에 발화하는지를 검사한다. 기존 Terrain에 어댑터를 추가한 뒤 Inspector Preset Selector로 적용하고 날씨·기존 배치 ID를 확인한다. Terrain을 겹쳐 복제하지 않는다. 정규 씬에 자동 적용/저장하는 기능은 아니다.

## 실제 보유 에셋 적용 예

| 산출물 | 원본과 명시 범위 |
|---|---|
| [CargoCrate wrapper](../../03.Prefabs/Environment/Reactive/PF_ReactiveCargoCrate.prefab) | 기존 WorldCities CargoCrate 중첩, Body 한 슬롯, 비파괴 젖음/그을림 |
| [ArenaTree wrapper](../../03.Prefabs/Environment/Reactive/PF_ReactiveArenaTree.prefab) | 구매 LowPolyFantasyArena2 Tree_01 중첩, 3 LOD 같은 상태, 비파괴 연소/그을림. 결합 줄기/수관이므로 coverage 소거 없음 |
| [FarmTree 벌목/채집 wrapper](../../03.Prefabs/Environment/Reactive/PF_ReactiveFarmTree.prefab) | 구매 FarmLand GenTree A의 FullTree/Stump/Trunk/Log 중첩. 원본 cut 부근 y=1.22m, 배치 스케일 0.4, 절단 pivot 높이 0.488m·검사 길이 6.6m/반경 0.42m를 명시. 3 mesh LOD·줄기/잎 슬롯·밑동/통나무가 한 상태를 공유. 미지원 impostor는 wrapper에서 끄고 명시 제외. 기존 Tree 상호작용/보상 사용 |
| [FarmGrass wrapper](../../03.Prefabs/Environment/Reactive/PF_ReactiveFarmGrass.prefab) | 구매 FarmLand SmallGrass_01 중첩, Foliage/바람/알파/연료 소거, root 명중 trigger |
| [Terrain용 FarmDetail](../../03.Prefabs/Environment/Reactive/PF_ReactiveFarmDetail.prefab) | 같은 구매 풀 메시+프로젝트 툰 재질, 컴포넌트/Collider 없는 Detail 인스턴싱 원본 |

이 예는 기존 구매/프로젝트 메시와 텍스처·구매 절단 메시를 재사용한다. 실제 절단면 접합·낙하/KCC·Nav·최종 아트 인수를 대신하지 않는다. 예의 프로필은 외형 추론 없이 이번 저작에서 명시한 검수 대상 값이며 게임 전체 밸런스 확정값이 아니다. 구 ReactiveEnvironment*Sample 프리팹·전용 재질/메시/정의/레시피·생성기는 폐기했다. [자산 출처와 삭제 감사](../../../output/ReactiveEnvironment/Generalization/sample-reference-audit.json)는 역사 결과와 구분한다.

## 환경 에셋 테스트 치트

Editor Play 중 툴 런처 → 유틸 → 치트 콘솔 → 환경 에셋 테스트, 또는 월드/맵 → 환경 반응 모니터를 연다. 사용자가 **테스트 프리팹을 직접 지정**한다. 샘플 버튼·자동 경로 선택은 없다.

Boot 경로로 테스트 슬롯에서 시작하고 활성 플레이어/날씨/소유 지역을 준비한다. 지정 프리팹을 전방 Ground에 소환하면 활성화 전에 새 GUID를 발급한다. Hierarchy 선택 대상 또는 창의 소환 목록을 선택해 열·물·구조 입력을 준다. 등록=True와 실제 tick 후 결과를 함께 확인한다. 교체/제거는 이 창이 소환한 대상만 가능하며 이미 나온 보상·저장 원장은 유지된다. 소환 개체는 다음 세션에 자동 복원하지 않으므로 저장 인수는 정식 배치로 한다.

중앙 대상/저장 기록/최근 공급원/차폐 Raycast 카운터와 Terrain Inspector의 셀/밀도 쓰기 수를 확인한다. Unity Profiler의 ReactiveEnvironment.Tick/Export에서 실제 맵 비용을 측정한다. 개체 PropertyBlock의 배칭 영향·TerrainData 복제 메모리·전체 저장량과 카메라 이동 spike는 사용자 성능 인수 대상이다. 물 MSAA/RenderGraph 오류가 재현되면 렌더링 결과와 별도로 기록한다. Player/Addressables 검증 빌드는 실행하지 않는다.

## 연출 저작 (현재 구현)

`TreePresentation.asset` / `GrassPresentation.asset`을 반응 정의의 presentation에 연결한다. 값은 `Assets/10.Datas/Environment/Profiles/`에서 저작한다. 프로젝트 효과는 `Assets/03.Prefabs/Environment/Reactive/Effects/`이며 구매 원본 효과·재질을 수정하지 않는다.

- `burnAnchors`는 활성 줄기·수관·잔해의 시각 앵커다. 활성 앵커 수로 방출량을 나누며 논리 전파 샘플/열량은 늘리지 않는다. 원형·잔해 전환 때 비활성 앵커 효과는 반환한다. 기존 `_flame` 경로는 호환용이며 새 효과와 이중 연결하지 않는다.
- `visualRoot`에는 Collider가 없는 시각 자식만 허용한다. FarmTree는 원본 중첩 모델의 Collider를 wrapper 내 고정 형상으로 복사하고 시각 자식에서 제거하는 override를 사용한다. 구매 원본은 유지한다. hitColliders는 복사 후 실제 Collider를 다시 참조해야 한다.
- `impactAnchor`는 채집·접촉 정보 없는 외부 구조 입력·저장 복원의 상처 위치다. 전투는 HitPoint와 캐시된 실제 Collider를 사용한다. 목재 재질의 명중 부근만 손상 표현을 켠다. Collider raycast로 표면 법선을 구할 수 없으면 공격 역방향을 사용한다. 마지막 접촉 위치는 저장하지 않으며 내구도 단계만 복원한다.
- 국소 상처는 shader의 `_UseImpactWound`와 `woundRadius`, `damagedStage`, `severeStage`를 사용한다. 현재 세 단계 색/균열 범위 표현이다. 모델을 실제로 파낸 절단 홈 메시·깊이 인수와 혼동하지 않는다. 실제 cut cap 접합은 원본 Stump/Trunk를 유지하며 사용자 검수한다.
- `landingAnchor`는 저작한 수관 접촉점이다. 지면 레이어·probe 거리와 효과·소리를 실제 모델에 맞춰 확인한다. 떨어지는 Collider로 KCC를 강제 밀거나 접촉 피해를 생성하지 않는다. fallAcceleration은 단조 가속이며 저장 progress는 계속 각도 비율이다.
- 잎은 `_FoliageBottom/Top`, 높이, 역광과 원본 활성 `_Alpha_Clip`을 명시한다. FarmTree의 현재 원본 LeavesGraph 활성 색은 `_Foliage_Color_Bottom/Top`이다. 직렬화에 남은 구 `_Color_Tint`·`_Color_Bottom`이나 노멀 필드를 활성 속성이라고 가정하지 않는다. FarmTree Close/Far는 coverage를 끄고 탄화 색만 사용한다. 모든 LOD와 양면·바람·그림자를 실제 게임에서도 확인한다.
- 화염/연기는 world-space·무자동재생·충돌 꺼짐·스크립트 없음으로 저작한다. 소화는 StopEmitting 후 자연 반환, 언로드는 즉시 clear다. Cooling 잔연은 heat 문턱/시간 상한, 실제 소화 전이의 젖음 증가는 짧은 증기 감쇠를 사용한다. 냉각 저장의 첫 적용으로 증기를 재생하지 않는다.
- Terrain은 `terrainPatchCells`×동일 크기 셀을 시각 패치로 묶는다. 셀 저장·밀도·전파 구획은 바뀌지 않는다. 거리/화면 가장자리 여유(viewportMargin) 예산은 효과만 생략한다. 정규 씬 자동 적용/저장은 하지 않는다.

환경 반응 모니터의 **날씨·입력·전파 진단 기록**은 시각 캡처와 함께 대상 ID·프로필을 기록한다. active sample별 sheltered, Current.Rain/ready, exposure/contact, pending impulse/지속 물, 최근 공급 heat/s와 반경 내 source별 거리·map·layer·차폐·바람을 확인한다. 진단 버튼의 차폐 조회는 논리 tick 카운터에 합산하지 않는다.

## 실제 실행의 고정 갱신과 효과 거리

WorldStateManager는 IUpdatableManager(화학/전파)와 IFixedUpdatableManager(연출/낙하)에 모두 등록되어야 한다. 메서드가 존재하는 것과 GameManager가 실제 호출하는 것은 별개다. 실제 Play에서 등록 목록과 효과/낙하 진행을 확인한다.

EnvironmentPresentationProfileSO.maximumDistance는 기존 플레이어 거리 제한과 실제 주 카메라 거리 제한에 함께 적용한다. 화면 안이어도 카메라가 예산 밖이면 효과만 반환하고 CPU 연소/전파/저장은 유지한다. 재접근은 현재 snapshot으로 효과를 다시 대여한다. 부분 강수 노출을 저작하려면 서로 다른 수관 위치의 활성 비 샘플이 필요하다. FarmTree 원형의 현재 활성 샘플1개는 노출0/1만 표현하며, 이번 부분 차폐 시험의3샘플 임시 구성을 정규 wrapper에 자동 저장하지 않았다.

[MCP 실행 근거·미수행](../../../docs/work-status/reactive-environment.md#mcp-실제-실행-검증--2026-11-13)을 참고한다. 정적/표현 snapshot 사진은 화재·소화·저장 성공 근거가 아니다.

## 정규 월드 확장 저작 준비

후속 [전 맵 전환·불 부여·낙뢰 설계](../design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#정규-월드-연결-확장-설계)를 적용하기 전에 아래를 확인한다. 현재 GameplayEffectSO.environmentHeatImpulse, HitPhaseData.receivesImbuedHeat/imbuedHeatWeight, EnvironmentLightningProfileSO와 EnvironmentLightningStrike/지역 저장을 사용한다. 구현·검증·차단은 [현재 상태](../../../docs/work-status/reactive-environment.md#정규-월드-연결-구현-진행--2026-11-13)를 따른다. SO는 기존 Editor API/Undo로 저작하고 무관한 기본값 재직렬화는 정리한다.

1. 정규 씬과 생성/배치 원본을 함께 조사하고 원본 GUID·배치 ID·논리 루트별 대응표를 만든다. 기존 wrapper 4종이나 Terrain 16셀 시험 구역만으로 전체 전환을 대신하지 않는다.
2. 원형 유지형 구성/원본별 wrapper, 비가연·비파괴 프로필, 각 LOD 슬롯, 실제 크기의 Collider/비·열 샘플/파괴 형상을 저작한다. 원본·meta·기존 ID·씬 참조·진행/채집 소유권·CameraOccluder 이름 규칙을 보존한다.
3. 기존 `GE_ElementalImbue_Fire`와 실제 공격 AbilitySet/HitPhase를 대응시킨다. 부여 열량의 정본은 Effect, 수신/분배는 페이즈다. 평상시 공격을 전부 영구 Fire 입력으로 바꾸지 않는다. 다중탄·장판·잔상은 같은 snapshot과 열 예산을 소비하는지 확인한다.
4. 날씨 낙뢰는 기존 원경 섬광과 실제 지표 타격을 구분한다. 프로필의 후보 구역/레이어/열량/반경과 실제 접점·지역·사건 ID를 기록한다. 기존 preview의 화면 시험이 자동으로 월드 발화/저장 입력이 되지 않게 한다.
5. 대표 구획 검증 후 정규 구획/맵별 적용과 재생성 원본을 함께 갱신한다. 미처리/미분류0과 제외 근거, 실제 플레이·저장/지역 왕복·KCC/Nav·목표 기기 예산을 [확장 인수](../../../docs/work-status/reactive-environment-tests.md#정규-월드-연결-확장-인수)에서 구분해 보고한다.


불 부여 페이즈는 명시 열과 부여 열량×가중치의 max를 사용한다. 실제 PlayerCharacterDefinitionSO의 AbilitySet에서 236 파일/247 Payload/397페이즈를 연결했다. 패턴 탄수는 요청의 environmentImpulseDivisor로 확정 열을 나누고, 분열은 부모 snapshot을 자식 수에 나누며 부여를 다시 조회하지 않는다. 지속 장판·접촉+폭발은 대상별 단발 원소 예산을 공유한다. 지속 공급이 필요하면 별도 EnvironmentElementEmitter rate를 저작한다.

낙뢰 프로필 areas.sourceBounds는 지역 원본 좌표이며 y최댓값에서 아래로 조회한다. surfaceLayerId는 물리 LayerMask/Detail 번호와 별개다. surfaceLayers는 첫 타격면/범위 수집, blockerLayers는 주변 차폐에 쓴다. 열은 중앙 커밋에서 기존 대상 pendingHeat로 즉시 인계하며 지역 난수·순번·남은 간격과 함께 저장한다. 언로드/시간 점프를 누적 재생하지 않는다. 정규 호수 프로필은 LakeOfLifeEnvironment에 연결했고, 접점 불티는 기존 입자 풀 cap만 사용한다. 사용자 제공 천둥3종은 공용/호수/가뭄 날씨 프리팹에 연결했다. 실제 낙뢰 시험은 환경 반응 모니터의 명시 요청 버튼, 화면 시험은 기존 날씨 미리보기를 사용하며 기록을 구분한다.

낙뢰의 첫 수면은 `FootstepWaterSurface.TryRaycastDown`으로 조회 거리 안의 가장 높은 실제 삼각형/체적을 선택한다. 발소리·수영의 수역 우선순위는 물리 첫 면 선택에 쓰지 않는다. 바닥 Collider가 없어도 유효 수면 사건을 만들며, 더 높은 지붕/구조물이 있으면 물리 첫 면을 우선한다. 수면 사건은 열0이며 바닥을 가열하지 않는다.

전수 조사는 `ReactiveEnvironmentWorldAudit.Write`를 사용한다. `terrainDetails`는 모든 Detail 레이어 전체 밀도·32픽셀 사용 타일·원본 GUID와 배율/인스턴싱을 포함한다. `pendingRoots`는 미분류와 `GatheringReview`를 합산한 후보 수다. GameActor 소유라는 이유만으로 채집물을 전환 제외 완료로 세지 않는다. VFX는 해당 논리 루트의 모든 Renderer가 입자/선/궤적일 때만 기존 표현 시스템으로 분류하며 메시가 섞인 루트는 계속 검토한다. [현재 상태](../../../docs/work-status/reactive-environment.md#통합-후속-조사와-수면-낙뢰-수정--2026-11-13)에서 원본 버전과 전환 전후 검증을 확인한다.