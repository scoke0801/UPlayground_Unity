# 반응형 환경 통합 계약

상위 입력은 [전달 설계](../../../docs/전달용/environment_system_design_v1/environment_system_design_v1.html)다. [작업 상태](../../../docs/work-status/reactive-environment.md)에서 구현·자동 검사·사용자 인수를 구분한다. 반복 저작은 [저작 지침](../guide/REACTIVE_ENVIRONMENT_AUTHORING_GUIDE.md)을 따른다.

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

[ToonReactive.shader](../../06.Shaders/Environment/ToonReactive.shader)와 [EnvironmentSurface.hlsl](../../06.Shaders/Environment/EnvironmentSurface.hlsl)이 한 coverage/변형 함수를 공유한다.

- EnvState0 = Wetness/Char/Damage/Burning, EnvState1 = Heat/예약 0/예약 0/예약 0. CPU 상태를 읽으며 GPU가 연료·피해·보상을 작성하지 않는다.
- 합성은 기본 색/노멀 → 그을림/균열 → 젖음 색/제한된 하이라이트 → 툰 명암/그림자/간접광/추가 광원 → 연소 경계 발광 → Fog다. 소화하면 발광만 사라지고 그을림/손상은 유지한다.
- Body/Foliage/Interior 역할을 재질에 명시한다. 실루엣 소거는 명시적으로 허용한 잎만 사용한다. 일반 구조물의 Collider를 셰이더 구멍으로 대체하지 않는다.
- ForwardOnly, SRPDefaultUnlit 외곽선, ShadowCaster, DepthOnly, DepthNormalsOnly, DepthNormals, MotionVectors, Meta의 8개 패스다. 알파 clip·연료 coverage·LOD crossfade·바람을 공유한다. 모션 벡터는 이전 프레임의 바람을 기존 단일 날씨 작성자가 함께 공급한다.
- 기존 전역 젖음 작성자를 추가하지 않는다. 바람은 기존 _LW_Wind를 읽는다. 불투명/잎/절단면은 같은 셰이더의 명시 재질 설정으로 분리한다.
- ReactiveEnvironmentSurface는 객체 안의 동일 원본을 하나의 runtime material lease로 공유하고 모든 LOD/slot을 갱신한다. 원본 sharedMaterial을 변경하지 않으며 언로드/풀 반환 때 원래 슬롯을 복원한다. 미지원 셰이더·누락 LOD·잘못된 coverage/Collider 구성은 저작 검사에서 거부한다. 기존 LegacyTint는 명시 fallback으로만 유지한다.
- 상수는 UnityPerMaterial에 두었다. runtime material lease의 CPU/GPU/메모리 비용은 실제 맵에서 측정해야 하며 SRP Batcher 구조만으로 성능 통과를 주장하지 않는다.

## 범위 경계

현재 완료 범위는 등록된 소품/풀 패치의 반응·전파·사전 파괴·보상·저장·커스텀 툰 경로다. 구매 프리팹/정규 씬을 자동 교체하지 않는다. 새로운 관리 대상은 프로필·샘플·재질·레시피를 명시 저작한다.

전체 콘텐츠 closure의 Managed/Exempt 빌드 게이트, 지형 대량 셀/공간 젖음 atlas, 맵 경계 전파, 독립 자식 엔티티 분배, 원거리 proxy/catch-up/영속 입력 dedup, 낙목 접촉 피해와 최종 VFX/아트·성능 인수는 별도 범위다. 언로드/시간 점프 중에는 화학을 정지(SuspendDomain)한다. 이 경계를 전체 P0~P6 릴리스 완료로 바꾸어 표현하지 않는다.


## 일반 씬 오브젝트·식생 확장 검토안

이 절은 코드 조사에 근거한 **미구현 제안**이다. 기존 지원 범위나 검증 결과를 확장하지 않는다.

### 적용 단위

| 대상 | 권장 논리 단위 | 현재 재사용과 추가 작업 |
|---|---|---|
| 배치 소품·바위·울타리 | 상호작용 가능한 개체 또는 파괴 부위 | 반응 코어/피격/보상/저장은 재사용. 기존 모델의 비파괴 반응과 표면 바인딩 저작 보완 |
| GameObject 나무 | 한 그루, 밑동/수관/통나무는 같은 소유 상태 | 기존 DirectionalFall 재사용. 실제 나무별 절단점·피벗·Collider·잔해/LOD를 명시 저작 |
| GameObject 풀·관목 | 한 덤불/작은 패치 | 기존 패치 반응 재사용. 잎 소거와 Collider/보상 정책을 분리 |
| Terrain Detail 풀/꽃 | terrain의 안정적인 셀 또는 패치 | 개별 MonoBehaviour 바인딩 대신 범위 입력→셀 상태→밀도/표현 마스크 어댑터 필요 |
| Terrain Tree로 배치한 나무 | 안정적인 배치 ID를 가진 나무 기록 | Terrain 인스턴스용 식별/표현 어댑터 필요. 벌목 대상의 원형 숨김+실물 대체는 중복 Collider/보상/저장까지 같은 ID로 인계 |
| 대형 건물·긴 벽 | 지붕/벽 등 의미 있는 부위 | 한 중심점·하나의 젖음/열 상태로 전체를 묶으면 국소 피격/비 차폐 표현이 부정확해짐 |

정규 지면 저작 도구 LakeVillageGroundDetailTool은 DetailPrototype/useInstancing과 SetDetailLayer를 사용한다. 따라서 GameObject 풀 샘플 지원을 정규 Terrain 풀 지원으로 간주하면 안 된다. Terrain Tree 항목은 해당 배치 방식을 사용할 때의 확장 조건이며 현재 월드 전체의 나무가 그 방식이라는 판정이 아니다.

### 먼저 해소할 제약

1. **반응과 파괴 바인딩 분리.** 현재 ReactiveEnvironmentObject.IsAuthoringValid는 canBreak=false여도 원형/잔해를 필수로 요구하고 ApplySnapshot에서 항상 전환한다. 젖기/그을림만 받는 바위·벽은 기존 Renderer를 그대로 사용할 수 있어야 한다. 파괴를 선택한 대상만 레시피/잔해를 필수 검사한다.
2. **외형 호환성의 명시.** ReactiveEnvironmentSurface.Validate는 모든 연결 슬롯이 전용 Reactive Toon이고 모든 LOD가 연결되어야 통과한다. 구매 재질을 자동 교체하지 않는다. 기존 구조를 감싼 프로젝트 소유 wrapper/variant에 호환 재질을 저작하고, trunk/leaf/장식 등 반응하는 Renderer/slot과 제외 대상을 명시한다. LegacyTint는 제한된 색 표현이며 동일 품질 대안이 아니다.
3. **저작 자동화와 런타임 책임 분리.** 선택 대상에 프로필·표면·열/비 샘플·충돌·저장 ID를 연결하는 에디터 적용/검사 도구를 둔다. 후보 자동 수집 후 적용 전 결과/누락을 제시하고 Undo와 GUID/원본 보존을 보장한다. 이름/텍스처 색으로 가연성이나 자원을 추론하지 않는다. 낙목 피벗/잔해/크기는 검수 없이 자동 확정하지 않는다.
4. **명중과 채집의 단일 소유권.** 물리 명중에는 IDamageable에 도달하는 Collider와 조회 레이어가 필요하다. 풀 셀에는 물리 Collider를 풀잎마다 추가하지 않고 공격 범위를 공간 셀에 전달한다. GatheringActor와 현 컴포넌트의 병용은 현재 거부된다. 채집 가능한 나무/풀은 상태/보상 원장을 누가 소유하는지 정한 뒤 어댑터로 연결해야 한다.
5. **안정적인 배치 ID.** 개별 씬 배치는 기존 RegionOwnership+SceneEntityId를 유지한다. 재생성 맵은 배치 원본의 안정 ID를 재사용해야 한다. 위치 해시나 Terrain 배열 index만으로 영구 ID를 만들지 않는다. 셀은 저장된 terrain ID+고정 셀 좌표/구획 버전 정책을 쓰며, 해상도/배치 변경의 이관·초기화 규칙을 명시한다.
6. **대량 처리는 별도 표현 경로.** 현재 모든 활성 대상은 중앙 고정 tick에 순회하고 변경이 없어도 ApplySnapshot을 호출한다. 각 객체의 원본 재질별 runtime clone, 반복 표면 샘플/차폐 검사, 대상별 전파 셀 조회, 전체 상태 저장은 숲/초원 규모의 통과 근거가 없다. 먼저 revision 기반 표현 갱신·활동 대상 tick·저장량과 재질 수를 측정하고, 풀은 셀 상태/dirty 영역과 인스턴싱 표현을 연결한다. 물·불 입력 누적·날씨·저장 결과를 일치시키기 전에 단순 거리 비활성화로 최적화하지 않는다.

### 권장 구현 순서와 인수

- 1단계: 기존 개체 경로에서 비파괴 반응 지원과 선택 대상 저작/검사 도구를 만든다. 구매 모델의 나무 1종·소품 1종·풀 패치 1종으로 실제 외형을 유지하며 검증한다.
- 2단계: 현재 맵의 Terrain Detail 풀에 한정해 셀 상태/범위 입력/렌더링·저장 어댑터를 수직으로 연결한다. 코어 반응식은 공유하고 셀 표현을 GameObject 바인더에 억지로 넣지 않는다. 별도 엔티티 인터페이스는 두 실행 경로가 확정된 뒤 필요한 계약만 추출한다.
- 3단계: 대상 수를 고정한 맵에서 중앙 tick/차폐 Raycast/활성 화염 수/재질 수/저장 크기와 카메라 이동 spike를 측정한다. 화재 전파·소화·벌목·채집/중복 보상·재진입·저장/로드·맵 재생성의 동일 ID를 함께 확인한다.

실제 아트/성능 인수와 알려진 물 MSAA 렌더링 문제는 별도 확인한다. 현재 코드를 모든 씬 오브젝트에 자동 부착하거나 모든 Terrain 식생을 GameObject로 전환하는 작업은 승인·구현된 것으로 취급하지 않는다.
