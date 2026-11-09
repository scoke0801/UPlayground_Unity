# 카메라 가림 페이드 가이드

카메라가 장애물을 만났을 때 **무조건 앞으로 당겨지지 않고**, 장애물을 디더로 흐리게 만들어 시야를 확보하는 시스템이다.
임의 셰이더의 오브젝트(lilToon, Shader Graph, 외부 에셋)를 **셰이더 수정 없이** 흐리게 하는 것이 설계 목표다.

## 동작 요약

```text
CollisionCameraModifier(800)
  ├─ CameraCollision.Evaluate      Solid 장애물만 카메라 암을 접는다
  └─ CameraOcclusionFader.Probe    카메라→피벗(락온 시 대상 포함) 시야선을 훑어 Fade 대상을 등록
CameraManager.OnLateUpdate
  └─ CameraOcclusionFader.Advance  슬롯별 가시율 진행, 프록시 생성/제거, 상태 발행
CameraOccluderFadeFeature (URP RendererFeature, AfterRenderingOpaques)
  └─ 스텐실 베이어 마스크 4장 → 프록시 렌더러 그리기(스텐실 비교) → 마스크 지우기
```

### 왜 프록시인가
메인 패스에서 특정 렌더러만 빼면서 원본 셰이더로 다시 그릴 방법이 이것뿐이었다.

| 시도 | 결과 |
|---|---|
| 머티리얼 렌더 큐를 5000 초과로 이동 | 메인 패스에서는 빠지지만 `RenderQueueRange`가 0~5000만 허용(필드 대입도 예외)해 다시 고를 수 없다 |
| 물리 레이어 변경 | KCC·레이캐스트·상호작용 마스크가 깨진다 |
| **프록시(채택)** | 원본은 `ShadowsOnly`(그림자 유지), 같은 메시·머티리얼의 프록시를 전용 레이어에서 Feature가 그린다 |

프록시는 콜라이더가 없는 렌더링 전용 오브젝트라 물리에 영향이 없다. 원본의 라이트맵·프로브·렌더링 레이어·`MaterialPropertyBlock`을 복사한다.

### 디더 방식
화면 전체에 4x4 베이어 값(0~15)을 스텐실 상위 4비트(`0xF0`)에 비트 평면 4장으로 쓰고, 프록시는 스텐실 비교(`ref > 베이어값`)로 픽셀 단위 소거된다. 끝나면 같은 비트를 0으로 지워 다른 스텐실 사용자(lilToon 등)에 영향을 주지 않는다. 가시 단계는 0~16(1/16 단위)이다.

## 분류 규칙 (`OccluderClassifier`)

| 순서 | 조건 | 결과 |
|---|---|---|
| 1 | `CameraOccluder` 컴포넌트, 없으면 이름 규칙 | 지정값(Solid/Fade/Ignore, Auto면 아래 계속) |
| 2 | `TerrainCollider`, `Ground` 레이어(`CameraConfig.SolidObstacleLayer`) | Solid |
| 3 | 페이드할 `MeshRenderer` 또는 `SkinnedMeshRenderer`가 없음 (보이지 않는 벽) | Solid |
| 4 | 두께 ≤ 0.6m, 가로 ≥ 2m, 가로/두께 ≥ 4 (바닥·슬랩) | Solid |
| 5 | 그룹 렌더러 수 > 64 (월드 전체를 흐리는 사고 방지) | Solid |
| 6 | 그 외 | **Fade** |

확신이 없으면 항상 Solid로 떨어져 기존 카메라 동작을 유지한다. 판정은 콜라이더별로 1회 계산해 캐시한다.

`LakeVillageWorld` 실측(2026-11-07): 카메라 관련 콜라이더 17,890개 중 Fade 16,533개(92.4%). Solid는 지형 1, Ground 레이어 322, 바닥형 1,016, 렌더러 없음 18.

## 실내 가드 (`LimitInterior`)

Fade 벽은 카메라가 통과하므로, 막지 않으면 카메라가 **지붕 아래 실내에 들어앉는** 문제가 생긴다.
`CollisionCameraModifier`가 `Collision.Evaluate` 전에 요청 거리를 한 번 제한한다.

- 플레이어(피벗)가 지붕 밖이고 카메라 끝점이 지붕 아래면, 끝점에서 피벗 쪽으로 0.5m씩 되짚어 **지붕 밖 첫 지점**까지 암을 줄인다.
- 지붕 판정은 지점 위로 40m, 사방 5개 레이 중 2개 이상이 **'지붕 제공'으로 표시된 콜라이더**에 맞는지로 한다. 표시되지 않은 콜라이더(나무 캐노피, 처마 소품 등)는 지붕으로 보지 않는다. 자동 감지를 쓰지 않는 이유는 월드의 나무 프리팹 수백 개가 캐노피 콜라이더를 가져 나무 아래에서 카메라가 불필요하게 당겨지기 때문이다.
- 플레이어도 지붕 아래(실내, 처마 밑)면 제한하지 않는다.
- 벽을 통과해 건물 **뒤쪽 열린 공간**으로 나가는 것은 허용한다. 이것이 Fade의 본래 목적이다.
- 제한은 `CameraCollision`의 즉시 축소·지연 복귀 스무딩을 그대로 따르므로 가장자리에서 튀지 않는다.
- 끄려면 `CameraSettings.enableOcclusionInteriorGuard`를 해제한다.

한계: 지붕 콜라이더가 없는 건물과 지붕 없는 담장 안뜰은 실내로 판정할 수 없다. 이런 곳은 Solid로 지정한다(`CameraOccluder` 또는 아래 "이름 규칙").

`LakeVillageWorld`의 집 4채 실측(자동 감지 시절): 카메라가 집 중심으로 7.8m 파고들려 할 때 4.1~5.3m로 제한됐다(8개 중 7개).

## 이름 규칙 (`CameraOccluderNameRulesSO`)

환경 프리팹은 `.gitignore` 대상이고, 월드 씬은 소스 씬에서 재생성되며, 같은 건물이 프리팹 인스턴스이기도 하고 풀린 키트 부품(`TGV_Roof_*` 등)이기도 하다. 그래서 프리팹·씬에 컴포넌트를 심지 않고, 추적되는 에셋 `Assets/10.Datas/Camera/CameraOccluderNameRules.asset` 하나를 분류기가 런타임에 읽는다. `CameraSettings.occlusionNameRules`가 이 에셋을 참조한다.

- 판정 우선순위: 오브젝트에 직접 붙인 `CameraOccluder` → 이름 규칙 → 자동 판정.
- 이름 비교: 콜라이더 오브젝트부터 최대 12단계 조상까지 올라가며 첫 일치를 쓴다. 규칙 목록은 위에서부터 먼저 일치한 것이 이긴다.
- 일치 방식: `isPrefix` 꺼짐은 이름이 같거나 `이름 (3)`, `이름(Clone)`처럼 뒤에 공백·괄호가 붙은 경우만. `isPrefix` 켜짐은 패턴으로 시작하면 일치.
- 결과는 콜라이더별 1회 계산해 캐시하므로 프레임 비용이 없다(규칙 수만큼의 비교는 최초 분류 때만).
- 규칙 추가: 에셋의 `Rules`에 이름, 모드, 지붕 제공 여부, 근거를 적는다. 재컴파일이나 도구 실행은 필요 없다. 프리팹 이름을 바꾸는 월드 제작 파이프라인은 규칙도 함께 갱신해야 한다.
- 검증: EditMode 테스트 `CameraOccluderNameRulesTests`가 일치 규칙, 분류기 연동, 규칙 에셋의 중복·빈 패턴, `CameraSettings` 연결을 확인한다.

현재 규칙(68개):

| 구분 | 모드 | 지붕 제공 | 근거 |
|---|---|---|---|
| 집·성·항구·동양풍·던전 건물, 마을 키트 지붕·바닥(`TGV_Roof_`, `TGV_Floor_`, `Village_House_`, `SM_Farm_Roof_`), 던전 `ceiling` | Auto | 켬 | 지붕 콜라이더 확인(키 큰 건물은 지붕이 14~17m 위) |
| 지붕 콜라이더 없는 건물 (`CastleTower`, `HarborLighthouse`, `Castle_Bastion_Tower`) | Solid | 끔 | 45m 레이로도 지붕이 잡히지 않음 |
| 성벽·테라스 (`Castle_Wall*`, `CastleWall`, `Castle_Walls`, `FortressTerraces`) | Solid | 끔 | 지붕 없는 안뜰을 둘러쌈 |
| 밟는 구조물 (다리·계단·경사로·부두 등) | Solid | 끔 | 카메라가 아래로 빠지면 안 됨 |
| 던전 벽 모듈 | Solid | 끔 | 인접 방이 비치지 않게 |
| 대형 폐허 (`BriarRing`, `ChainTemple`, `SunkenHall`) | Solid | 끔 | 지붕이 잡히지 않음 |

미지정(판정 근거 부족으로 보류): `EastStyle_TileHouse_Prototype`, `Keep`, `Watch`, `House_Lookout`(내부 점이 모두 콜라이더 안이라 지붕 유무를 측정할 수 없음), `Prop_Pavilion`(사방이 열린 정자라 지붕 아래 진입을 막지 않음). `TGV_*` 등 키트 규칙은 씬 YAML 이름 조사 기준이며 Play Mode 실측은 하지 않았다.

## 신규 건물·구조물 배치 체크리스트

건물, 성벽, 다리, 던전 모듈, 키트 부품을 새로 배치하거나 월드를 재생성할 때마다 확인한다. 규칙이 없는 건물은 기본 Fade이고 지붕으로 인정되지 않아 카메라가 안으로 파고들 수 있다.

1. 새로 쓰는 프리팹·키트 부품 이름이 `CameraOccluderNameRules.asset`의 규칙에 걸리는지 확인한다. 같은 건물이 프리팹이면서 풀린 키트 부품으로도 쓰이면 양쪽 이름을 모두 확인한다.
2. 분류하고 규칙을 추가한다.

   | 대상 | 규칙 |
   |---|---|
   | 지붕·천장·위가 막힌 바닥(2층)이 있는 건물과 그 부품 | Auto + 지붕 제공 |
   | 지붕 콜라이더가 없는 탑·등대, 지붕 없는 성벽·안뜰 | Solid |
   | 밟는 구조물(다리·계단·경사로·부두), 던전 벽 모듈 | Solid |
   | 나무·소품·사방이 열린 정자 | 규칙 없음(Fade) |

3. 지붕 제공은 콜라이더가 실제로 있어야 동작한다. 위로 레이를 쏴서 지붕 콜라이더가 잡히는지 확인한다(키 큰 건물은 14~17m 위).
4. 풀린 키트는 규칙이 패턴(`isPrefix`)인지 확인한다. 부품 이름마다 규칙을 따로 쓰지 않는다.
5. 규칙을 바꾼 뒤 `CameraOccluderNameRulesTests`를 실행한다. 중복·빈 패턴, 우선순위를 잡는다.
6. 건물 가장자리와 안쪽으로 카메라를 밀어 파고들지 않는지, 나무 아래에서 카메라가 당겨지지 않는지를 Play Mode에서 확인한다.

## 저작

- 건물 한 채처럼 함께 사라져야 하면 루트에 `CameraOccluder`를 붙이고 `Fade Whole Hierarchy`를 켠다.
- 카메라가 절대 통과하면 안 되는 면(실내 외벽, 다리 상판)은 `Solid`로 지정한다.
- 카메라와 무관해야 하는 소품은 `Ignore`로 지정한다.
- 튜닝은 `CameraSettings`의 "충돌 · 가림 페이드" 섹션에서 한다. 최소 가시율, 페이드 시간, 해제 유예, 동시 덩어리 수(최대 8), 디더 점 크기를 조정한다.

## 렌더러 설정 (필수)

활성 렌더러(`PC_Renderer`)에 다음이 모두 있어야 한다. 하나라도 빠지면 페이드가 꺼지거나 벽이 불투명하게 남는다.

1. 레이어 `CameraFadeProxy` (현재 슬롯 18). 이 이름은 `CameraConfig.FadeProxyLayerName`이다.
2. `CameraOccluderFadeFeature`를 Renderer Features에 추가하고 `Mask Shader`를 지정한다.
3. Renderer Data의 **Prepass / Opaque / Transparent Layer Mask**에서 `CameraFadeProxy`를 제외한다.

안전장치:
- Feature가 렌더러에 없으면 `CameraOccluderFadeState.IsFeatureRegistered`가 false가 되어 페이더가 꺼지고 기존 카메라 충돌로 돌아간다.
- 레이어 마스크 제외 누락은 EditMode 테스트 `ActiveRenderer_ExcludesProxyLayerFromMainPassesAndHostsFeature`가 잡는다.
- 레이어가 정의되지 않으면 페이드가 꺼진다.

## 알려진 한계

- **SSAO/뎁스 노멀 프리패스**에 프록시가 빠지므로, 흐려지는 동안 해당 오브젝트의 AO와 `_CameraDepthTexture` 기여가 없다.
- 정적 메시와 스킨드 메시를 지원한다. Animator 계층의 몸 충돌체는 같은 리그의 렌더러를 한 그룹으로 처리하며, 스킨드 프록시는 원본 본을 공유하고 블렌드 셰이프를 동기화한다. 파티클은 제외한다. LODGroup 소속 렌더러는 프록시가 LOD와 무관하게 그려진다.
- 렌더러에 Feature가 없는 다른 카메라(프리뷰, 시네마틱 스테이지)는 프록시를 보지 못해 페이드 중인 오브젝트가 그림자만 남는다. 해당 카메라는 `CameraOccluder`가 활성인 구간에서만 문제가 된다.
- 락온 시선 판정(`CameraLockOn`)은 여전히 물리 기준이라, 벽 뒤 적은 시각적으로 비쳐도 유예(`lockOnOcclusionGraceTime`) 후 해제될 수 있다.
- 지형 나무(Terrain tree) 콜라이더는 `TerrainCollider`에 속하므로 Solid로 남는다.

## 검증

- EditMode: `UPlayGround.CameraOcclusion.Tests` (분류, 프록시 생성·정리, 해제, 최대 그룹, 충돌 제외, 렌더러 설정).
- 임시 씬 렌더링 실측: 레벨 8에서 벽 픽셀 비율 0.500, 복원 시 기준과 픽셀 단위 동일, 그림자 유지 확인.
- 미검증: 실제 게임 Play Mode의 체감(락온·전투 중 카메라 이동), 건물 내부 진입 시 구도.
