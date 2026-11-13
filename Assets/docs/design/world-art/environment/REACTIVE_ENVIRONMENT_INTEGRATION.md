# 반응형 환경 통합 계약

상위 입력은 [전달 설계](../../../../../docs/전달용/environment_system_design_v1/environment_system_design_v1.html)다. [작업 상태](../../../../../docs/work-status/reactive-environment.md)에서 구현·자동 검사·사용자 인수를 구분한다. 반복 저작은 [저작 지침](../../../guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)을 따른다.

## 지원하는 실행 경로

GAS HitPhase 환경 payload → 플레이어/잔상/몬스터 AttackData → 불변 HitRequest → ReactiveEnvironmentObject → WorldStateManager의 단일 작성자 → 반응 상태/파괴 전이/보상/저장 → 읽기 전용 툰 표면으로 연결한다. 새 패키지·런타임 asmdef·매니저는 추가하지 않는다.

- HitPhaseData.environment는 기계 입력 종류(Generic/Slash/Blunt/Explosion)와 열·물 impulse를 갖는다. Damage에서 원소를 계산하지 않는다. 기존 에셋의 새 필드 기본값은 원소 없음이며, 기존 스킬의 색·이름을 보고 자동으로 수치를 채우지 않는다.
- 근접·잔상·몬스터·조합형/레거시 투사체의 복사와 HitRequest 변환에서 같은 payload를 보존한다. 다중 발사로 페이즈 총량을 분할하는 기존 도구도 원소 총량을 나눈다.
- 기존 CombatHitDetector의 단일 대상 중복 제거를 유지한다. 외부 명중 API에는 실행 ID+sequence를 사용한다. 지속 열/물은 EnvironmentElementEmitter의 rate를 한 번 적분하며 dedup 키를 매 tick 생성하지 않는다.
- 구조 피해는 명중 직후 커밋한다. 화학은 중앙 고정 substep에서 수분 → 열 → 발화/소화 → 연료/그을림/구조 순서로 계산한다. 즉시 물 impulse와 지속 물 rate의 냉각 계수를 구분한다.
- 전파는 tick 시작의 연소 상태를 공간 해시에 넣고 거리·바람·벽·surfaceLayerId·소유 맵을 검사한다. 새로 점화된 대상은 다음 tick부터 전파한다. 모든 논리를 계산한 뒤 표현/충돌을 전환해 등록 순서에 따라 벽이 먼저 사라지는 결과를 피한다.
- 날씨는 EnvironmentController.Current를 읽고 비 노출은 EnvironmentEffects.IsRainSheltered를 재사용한다. 표면/열 샘플을 구분하고, 비활성 원형의 샘플 대신 활성 잔해의 샘플을 사용한다. 자체 Collider를 제외한다. 현재 수직 강수 차폐이며 비스듬한 비는 지원 범위 밖이다.
- 파괴는 원형/사전 파손 잔해 전환과 DirectionalFall이다. 낙하 궤적의 캡슐 substep으로 가림을 검사하고, 시작부터 막힌 후보는 짧은 붕괴로 분기한다. 진행 중 장애물은 직전 안전 포즈에서 정착시킨다. 액터를 회전 Collider로 강제 밀거나 접촉 피해를 임의로 생성하지 않는다.
- Collider·차폐 볼륨·NavMeshObstacle 전환을 같은 파괴 상태에 맞추며 충돌/차폐/길찾기 revision을 분리한다. NavMesh carving 반영 지연과 실제 KCC 접촉은 플레이 확인 대상이다.
- 잔해 여러 조각은 한 논리 엔티티의 형상이다. 부모의 연료·열·젖음·그을림을 그대로 유지한다. 개별 조각마다 초기 연료/보상을 다시 생성하지 않는다. 충격/화재 보상은 기존 IGroundDropService sourceId와 월드 저장에 한 번만 확정한다.
- 저장 키는 RegionOwnership + SceneEntityId다. 화학 잔여 시각, pending 원소/지속 물, 파괴 진행·방향과 보상 확정을 함께 보존한다. 로드 중 지급하지 않으며, 호환/수치 검증 후 환경 상태를 일괄 교체한다.

## 커스텀 툰

[ToonReactive.shader](../../../../06.Shaders/Environment/ToonReactive.shader)와 [EnvironmentSurface.hlsl](../../../../06.Shaders/Environment/EnvironmentSurface.hlsl)이 한 coverage/변형 함수를 공유한다.

- EnvState0 = Wetness/Char/Damage/Burning, EnvState1 = Heat/예약 0/예약 0/예약 0. CPU 상태를 읽으며 GPU가 연료·피해·보상을 작성하지 않는다.
- 합성은 기본 색/노멀 → 그을림/균열 → 젖음 색/제한된 하이라이트 → 툰 명암/그림자/간접광/추가 광원 → 연소 경계 발광 → Fog다. 소화하면 발광만 사라지고 그을림/손상은 유지한다.
- Body/Foliage/Interior 역할을 재질에 명시한다. 실루엣 소거는 명시적으로 허용한 잎만 사용한다. 일반 구조물의 Collider를 셰이더 구멍으로 대체하지 않는다.
- ForwardOnly, SRPDefaultUnlit 외곽선, ShadowCaster, DepthOnly, DepthNormalsOnly, DepthNormals, MotionVectors, Meta의 8개 패스다. 알파 clip·연료 coverage·LOD crossfade·바람을 공유한다. 모션 벡터는 이전 프레임의 바람을 기존 단일 날씨 작성자가 함께 공급한다.
- 기존 전역 젖음 작성자를 추가하지 않는다. 바람은 기존 _LW_Wind를 읽는다. 불투명/잎/절단면은 같은 셰이더의 명시 재질 설정으로 분리한다.
- ReactiveEnvironmentSurface는 명시한 Renderer/재질 슬롯의 MaterialPropertyBlock만 변경한다. 원본 재질을 복제하거나 수정하지 않고, 언로드/풀 반환 때 기존 블록을 복원한다. 모든 Renderer/LOD/슬롯에 반응 또는 제외 결정을 요구한다. 미지원 셰이더·잘못된 coverage/Collider 구성은 거부한다. LegacyTint는 제한된 명시 fallback이다.
- 상수는 UnityPerMaterial에 둔다. 개체 PropertyBlock의 SRP Batcher 영향은 실제 맵에서 측정한다. Terrain Detail은 Terrain별 공유 재질/상태 마스크와 기존 인스턴싱을 사용한다.

## 범위 경계

현재 완료 범위는 등록된 소품/풀 패치의 반응·전파·사전 파괴·보상·저장·커스텀 툰 경로다. 구매 프리팹/정규 씬을 자동 교체하지 않는다. 새로운 관리 대상은 프로필·샘플·재질·레시피를 명시 저작한다.

전체 콘텐츠 closure의 Managed/Exempt 빌드 게이트, 전체 월드 셀 전환·성능 인수, 맵 경계 전파, 독립 자식 엔티티 분배, 원거리 proxy/catch-up/영속 입력 dedup, 낙목 접촉 피해와 최종 VFX/아트·성능 인수는 별도 범위다. 언로드/시간 점프 중에는 화학을 정지(SuspendDomain)한다. 이 경계를 전체 P0~P6 릴리스 완료로 바꾸어 표현하지 않는다.


## 일반 씬 오브젝트·식생 확장 검토안

이 절의 개체/Detail 연결 계약은 구현에 반영한다. 실제 자동 검사·미수행·사용자 인수의 현재 판정은 [작업 상태](../../../../../docs/work-status/reactive-environment.md#일반화-최종-판정)를 따른다. 과거 primitive 샘플 통과는 새 구현의 근거가 아니다.

### 상태 작성자와 표현 단위

- 기존 WorldStateManager + EnvironmentReaction이 유일한 상태 작성자다. IEnvironmentReactionTarget은 소유 Transform/ID/프로필/열 위치/날씨 샘플/표현만 연결한다. 개체와 셀 모두 동일한 고정 substep·원소 누적·전파 지연·벽·바람·저장 검증을 사용한다.
- 일반 소품/나무: ReactiveEnvironmentObject 한 개가 상태를 소유한다. 비파괴 프로필은 원형/잔해/레시피 없이 기존 Renderer로 젖음·그을림·연소를 표현한다. canIgnite/canBreak/scorchPerHeat 등은 프로필에 명시하며 외형이나 이름으로 추정하지 않는다.
- 파괴 프로필만 원형/잔해/레시피/ReactiveEnvironmentDestruction을 필수로 한다. 밑동·수관·통나무·모든 LOD는 같은 root의 표면 바인딩이며 독립 연료/보상을 생성하지 않는다. 절단/낙하 피벗·실제 길이/반경·Collider·fallback은 모델 기준으로 저작한다. 임의 자동 메시 절단은 하지 않는다.
- 구매 프리팹은 중첩 wrapper/variant에 연결하고 프로젝트 소유 호환 재질만 사용한다. 각 Renderer/slot은 반응 목록 또는 명시 제외 목록에 있어야 한다. 연결된 피격 Collider만 CombatHitDetector가 환경 대상으로 해석한다. 물리 통로 Collider와 반응 대상 Collider는 구분할 수 있다.
- GatheringActor 병용은 ownsGathering을 명시한 비재성장·비낚시 대상에 허용한다. 채집 애니메이션 입력도 환경 내구도에 전달하고 충격 보상은 기존 InteractableActorSO.dropItems, 화재 보상은 환경 프로필을 사용한다. GatheringActor의 별도 드롭/Destroy/소모 저장 경로를 실행하지 않는다. 구 consumedInteractables 기록은 같은 ID의 파괴/보상확정 상태로 인계한다. 낚시·시간 재성장은 연결 검사에서 거부한다.

### 저작과 안정 배치

반응 환경 저작 창은 변경 대상/Renderer/슬롯 수/Collider 후보·누락을 표시하고 프로필·날씨·열/비 샘플·피격 Collider·SceneEntityId를 연결한다. 구매 원본/프로젝트 프리팹 에셋 직접 변경을 거부하고 Prefab Mode 또는 씬 인스턴스를 편집한다. Undo 그룹 전체를 실패 시 되돌리며 기존 씬 GUID는 유지한다. 새 wrapper 저장은 기존 경로를 덮어쓰지 않고 생성 실패 시 새 파일만 제거한다. 프리팹 원본 배치 ID는 비운다.

씬 프리팹에는 기존 WorldPlacementMetadata의 DirectPrefab/RuntimeData 출처를 연결한다. Bake는 SceneEntityId를 sceneEntityGuid에 보존하고 RuntimePlacementLoader가 활성 등록 전에 같은 레코드 ID를 복원한다. 재생성기는 이 배치 레코드를 보존해야 한다. 원본 레코드를 버리고 위치 해시/새 GUID로 대체하는 재생성은 지원 정책이 아니다. Terrain도 한 배치 ID를 가진다.

### Terrain Detail 셀

정규 지면의 LakeVillageGroundDetailTool은 DetailPrototype/useInstancing/SetDetailLayer를 사용한다. 현재 LakeVillageWorld는 WorldThemesABC/Terrain.asset을 사용하며 원래 도구의 하드코딩 Terrain.asset과 경로가 다르다. 어댑터는 실제 Terrain에 연결된 데이터를 읽는다.

- Terrain당 TerrainDetailReaction 하나, 비어 있지 않은 셀당 순수 C# 객체를 둔다. 풀잎/셀마다 MonoBehaviour·Collider를 만들지 않는다. 명시 detailLayers/대응 호환 mesh prefab/셀 크기/구역/최대 셀 수/상호작용 높이를 저작한다. 구역 밖·제외 레이어는 상태 변경 대상이 아니다.
- 공용 근접/명시 형상 검출기는 IEnvironmentCellHitService로 셀 후보를 수집한다. Sphere/회전 Box/Capsule과 셀 부피의 교차를 검사한 뒤 기존 IDamageable 중복 제거와 HitRequest 환경 payload를 사용한다. EnvironmentElementEmitter도 같은 셀에 지속 열/물을 전달한다. 물리 Collider가 없는 영역에도 타격이 도달한다. 정규 ProjectileRuntime의 폭발/장판은 같은 셀 서비스와 대상별 쿨다운으로 연결한다. 셀의 원소 payload는 기존 물리 AOE 대상과 같은 입력이며 풀별 AttackData 복제는 하지 않는다. 범위 없는 직격 전용 투사체로 Terrain 풀을 태우려면 emitter/범위 입력을 별도로 저작한다.
- 같은 중앙 tick에서 셀의 비/차폐·전파를 계산한다. Terrain 전체를 자기 Collider로 제외하지 않으므로 지형에 의한 화염 차폐를 유지한다. 공용 source의 owner와 차폐 제외 계층을 분리한다.
- 런타임 TerrainData를 복제하고 TerrainCollider도 같은 복제본으로 연결한다. 원본 Detail 밀도를 셀별로 보관하고 연료 비율(표현용 8bit coverage) 변화 때만 해당 영역의 밀도를 재작성한다. 젖음/그을림/연소는 Terrain별 RGBA 상태 마스크를 기존 Reactive Toon에 전달한다. 마스크는 프레임/로드 커밋 후 한 번 업로드한다. 프로토타입별 재질/숨김 mesh 객체만 만들며 셀별 재질은 없다. 열/비 샘플은 interactionHeight의 중간 높이에 두어 지면에서 시작하는 자기 차폐를 피한다. 전파 반경/rate는 셀 중심 간격과 연소 시간에 맞는 별도 프로필을 명시한다.
- 저장 ID는 Terrain SceneEntityId + detail + 전역 셀 좌표다. persistenceSignature는 저작 원본 revision·partitionVersion·셀 크기·Detail 해상도·선택 레이어·셀 원본 밀도 hash를 포함한다. 재진입은 원본에서 복제 후 기존 저장을 재적용한다. 구획/원본 밀도와 저장이 맞지 않으면 초기화로 덮지 않고 실패시키고 Terrain 원본을 복원한다. 이관 없이 version만 올려 기존 저장을 자동 재사용하지 않는다. 의도적 초기화는 새 저장 슬롯 또는 검수한 저장 이관으로 한다. 언로드/시간 점프는 기존 SuspendDomain 정책이다.
- Terrain Tree는 읽기 전용으로 보존하며 GameObject로 자동 전환하지 않는다. 현재 정규 Terrain의 Tree 수는 0이다. 향후 Tree 인계는 원형 인스턴스/내장 충돌을 먼저 비활성화한 뒤 같은 저장 ID·보상 확정 원장으로 실물을 하나만 생성해야 한다. 이 인계 구현과 검증은 현재 범위 밖이다.

### 비용과 인수 경계

개체의 동일 상태는 형상/표면을 재적용하지 않는다. 표면 값 비교는 tick revision과 분리하고 할당을 만들지 않는다. 객체별 재질 clone을 없애고, 입력 대상/활성 키는 사전 조회한다. 비가 0이면 비 차폐를 조회하지 않는다. 중앙 시뮬레이션의 날씨/누적/전파/저장 결과를 바꾸는 거리 정지나 활동 대상 생략은 도입하지 않는다.

중앙 tick·저장은 ReactiveEnvironment.Tick/Export Profiler marker, 대상/저장/공급원/차폐 조회 카운터로 조사한다. 저장은 활성/언로드 엔티티의 기존 전체 기록 방식이며 대규모 sparse 압축이나 원거리 proxy가 아니다. TerrainData 전체 런타임 복제·프로토타입 재질·PropertyBlock의 CPU/GPU/메모리 비용과 저장량은 실제 맵 사용자 측정이 필요하다. 물 MSAA/RenderGraph 오류가 재현되면 렌더링 인수의 별도 제약으로 기록한다.
