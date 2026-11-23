# 퀘스트 길안내 연출 (나비 · 교체형)

- 요청: 미니맵 HUD 없이, 키를 누르면 퀘스트 목표 방향 길을 나비 연출로 보여 주고 이후 바람 등으로 교체 가능하게 한다.
- 설계 정본: [퀘스트 길안내 연출 설계](../../Assets/docs/design/ui-player/QUEST_PATH_GUIDANCE_DESIGN.md)
- 분야 위키: [플레이어와 화면](../wiki/player-ui.md)

## 현재 단계

P0~P4 구현 반영(2026-11-22). 이후 가독성 개선 2차로 나비 행렬을 더했다(아래 "나비 행렬"). 정상 컴파일 오류 0, 길안내 EditMode 14/14 포함 UI·Input EditMode 208/208 통과. 실제 월드 퀘스트 목표 길안내·연출 미관·입력 체감은 **사용자 테스트 대기**.

확정 결정: 탭 1회 재생(쿨다운 1.5초), 경로를 확인하지 못하면(부분 경로 포함) 아무 연출도 하지 않는다(직선 폴백·길 없음 반응 없음). 미니맵 HUD는 기본 비표시.

## 후속 변경 (2026-11-22, 사용자 피드백)

- **시간·날씨 HUD 제거**: `UI_HUD_GamePlay`가 `HudWorldClock`을 표시하지 않는다. `UI_HUD_WorldClock`·프리팹·빌더·`UIKeyType`·`WorldClockWeatherTests`는 보존.
- **퀘스트 UI X 노출**: `UI_HUD_Quest.PeekQuestInfo()`를 `OnPerformedGuide`가 호출. 표시할 퀘스트(추적/활성 메인)가 있으면 `_peekShowSeconds`(6초) 노출 후 페이드아웃, 연속 입력은 타이머 갱신, 퀘스트 없으면 비노출. 길 유무와 무관. 달성 연출 패널은 그대로. 페이드는 unscaled 시간 직접 진행. 프리팹 위치를 좌상단 위쪽(미니맵 자리)으로 200 올림(`Image`·`QuestDetailPanel` anchoredPosition).
- **전투 중 X 차단**: `PlayerCombatStateTracker.IsInCombat`은 교전 이벤트 후 30초와 20m 안 어그로 몬스터(`HasAggroTarget`)만 잡아, 더 먼 곳에서 추적 중인 적이나 어그로 목표가 플레이어가 아닌 경우가 새 수 있다. `GuidanceController.IsEngagedInCombat`이 `IsInCombat`에 더해 `NpcThreatScanner`(어그로 몬스터 위치 공유 스캔)로 `combatThreatRadius`(기본 40m) 안 어그로 몬스터를 전투로 본다. 나비·퀘스트 노출 모두 차단, 재생 중 전투 시작 시 페이드아웃 유지. `allowInCombat` 제거.
- **길 없음 반응 삭제**: `BeginNoPath`·NoPath 모드·`noPath*` 필드·`noPathSfxKey`와 관련 테스트 2개 제거, 에셋 2개 직렬화 정리. 시작 SFX는 경로를 찾아 연출을 시작할 때만 재생.
- 검증: 컴파일 오류 0, UI·Input EditMode 208/208(알려진 Locale 실패 3건도 이번 실행에서는 통과). Play Mode(RiverBasin4km): 퀘스트 없는 상태에서 `PeekQuestInfo`는 false, 강제 노출 시 좌상단 위치·다른 HUD 비겹침을 스크린샷으로 확인, 만료 후 알파 0·패널 비활성 확인. 실제 퀘스트·X 키 입력·전투 판정·6초 체감은 **사용자 테스트 대기**.

## 단계 표

| 단계 | 내용 | 상태 |
|---|---|---|
| P0 | 계약·컨트롤러·NavMesh 경로 제공자, 경로 분류 | 구현·자동 검사 통과 (기즈모는 만들지 않음 — 아래 참고) |
| P1 | 나비 연출·입력 (길 없음 반응은 후속 변경에서 삭제) | 구현·Play Mode 1회 육안 확인(합성 경로), 실제 월드는 사용자 대기 |
| P2 | 재생 중 억제·재계산·퀘스트 이벤트 | 구현·자동 검사 통과(컨트롤러 통합 경로는 Play Mode 미검증) |
| P3 | 바람 구현체로 교체 가능성 증명 | 구현·EditMode 통과, 프로필 `GuideProfile_Wind`, 화면 확인은 아래 기록 |
| P4 | 미니맵 HUD 기본 비표시 | 구현(`_showMinimapHud` 기본 false), 화면 확인은 사용자 대기 |

## 반영된 코드·에셋

| 구분 | 경로 |
|---|---|
| 데이터 | `Assets/02.Scripts/Data/UI/GuideProfileSO.cs`, `Assets/10.Datas/UI/GuideProfile_Butterfly.asset`, `GuideProfile_Wind.asset` |
| 경로·계약 | `Assets/02.Scripts/UI/HUD/Guidance/GuidePath.cs`, `GuideContracts.cs`, `NavMeshGuidePathProvider.cs`, `GuideEffectFactory.cs` |
| 연출 | `ButterflyGuideEffect.cs`, `ButterflyStream.cs`(나비 행렬), `WindGuideEffect.cs`, 프리팹 `Assets/03.Prefabs/UI/Guidance/GuideButterfly.prefab`(+controller), `GuideWind.prefab`(+머티리얼) |
| 오케스트레이터 | `GuidanceController.cs` — `UI_HUD_GamePlay`가 생성·입력 연결, `UI_HUD_GamePlay.prefab`의 `_guideProfile`에 나비 프로필 연결 |
| 입력 | `PlayerAction.Guide`(`InputDefine.cs`), `PlayerInputActions.inputactions`의 `Guide` 액션(키보드 X, 게임패드 L3+서쪽 버튼), 바인딩 목록 항목, 입력 계약 검사기 조합 예외(`UIInputContractValidator.cs`) |
| 테스트 | `Assets/Tests/EditMode/UI/GuidanceTests.cs` |

`.gitignore`가 `Assets/03.Prefabs/UI/*`를 제외하고 있어 프리팹 변경(`UI_HUD_GamePlay` 연결, `GuideButterfly`, `GuideWind`)은 Git 상태에 나타나지 않는다.

연출을 바꾸려면 `UI_HUD_GamePlay.prefab`의 `_guideProfile`에 `GuideProfile_Wind`를 연결한다.

## 검증

- 정상 컴파일: 오류 0(기존 경고만). 입력 에셋 재생성 확인(`PlayerInputActions.cs`에 `Guide` 포함).
- EditMode `GuidanceTests` **16/16 통과**: 경로 표본·투영, NavMesh 완성 경로, 끊어진 섬은 길 없음, 멀리 떨어진 목표는 길 없음, 나비·바람의 재생·종료·즉시 정지·경로 교체·길 없음 반응, 연출 팩토리.
- UI·Input 어셈블리 EditMode **204/207**. 실패 3건(`CharacterGaugeTests`, `CombatTestOverlayTests`, `SkillTreeVisualTests`)은 모두 `No Locale could be selected … Did you build the Addressables?`이며 이번 변경과 무관한 것으로 판단한다. 변경 전 상태에서의 재현은 확인하지 않았다. Addressables 빌드는 지시 전 실행하지 않는다.
- 시험 중 발견·수정: `GuidePath.ProjectDistance`가 시작 거리보다 앞선 값을 돌려주던 결함, 입력 계약 검사(`InputContractTests`)가 막은 게임패드 조합의 물리 경로 중복(조합 예외 확장으로 해결).
- Play Mode(RiverBasin4km 프리뷰, 합성 경로): HUD에서 컨트롤러 생성, 길 없음 반응 재생, 나비가 곡선 경로 앞에서 나는 것 확인. 나비 날개폭이 약 5cm로 너무 작아 프리팹 스케일을 80으로 키운 뒤 보이는 것을 확인했다. 이 씬은 NavMesh가 없어 실제 경로 계산·퀘스트 목표 연결은 이 방법으로 확인하지 못했다.
- 기즈모: 설계의 "경로 디버그 기즈모"는 만들지 않았다. 경로 분류는 EditMode 테스트로 검증하고, 실월드 대조는 사용자 테스트 중 필요하면 추가한다.

## 가독성 개선 (2026-11-22, 사용자 피드백 "알아보기 어려워 / 더 오래 / 경로가 잘 보이게 / 참고: Ghost of Tsushima")

- 나비마다 금빛 후광(파티클)과 긴 꼬리(약 2.2초)를 붙여 붉은 날개가 지형에 묻혀도 위치·진행 방향이 보이게 했다.
- 카메라에서 멀수록 크기를 키운다(`distanceScaleReference` 8m, 최대 2.6배).
- 연출 시간 5초 → 12초, 마지막 1.6초 페이드아웃. 앞서가는 거리 3~26m, 옆 흩어짐·팔랑임 진폭을 줄여 꼬리가 경로선에 모이게 했다.
- 경로 띠(`pathRibbonPrefab`): 플레이어 앞 최대 30m 경로를 따라 금빛 바람결 무늬가 흐르는 LineRenderer. NavMesh 표면 높이를 따라 지형에 파묻히지 않고, 먼 쪽일수록 넓어진다. 프로필의 `pathRibbonPrefab`을 비우면 끈다.
- 시험 중 발견: 코드로 만든 URP 파티클 머티리얼은 가산 혼합 값(`_SrcBlend`·`_DstBlend`·`_ZWrite`)을 직접 지정해야 한다. 지정 전에는 불투명 회색 판으로 보였고, 4개 머티리얼에 지정한 뒤 반투명 금빛으로 보이는 것을 Play Mode에서 확인했다.
- 개선 후 컴파일 오류 0건, `GuidanceTests` **16/16 재통과**. 경로 띠는 테스트 프로필에 프리팹이 없어 테스트 범위 밖이다.
- 한계: 화면 확인은 합성 경로로만 했다. 마지막 폭 곡선 변경 후 화면은 Play Mode 화면이 다른 지역으로 바뀌어 확인하지 못했다. 실제 월드 가독성은 사용자 확인 대기다.

## 나비 행렬 (2026-11-22, 사용자 피드백 "더 많은 나비가 경로로 / 아직 못 알아볼 정도 / 더 오래")

- 원인 판단: 군집 10마리가 모두 "플레이어 앞 lead 지점 ±2m"를 목표로 삼아 화면에서는 한 덩어리로만 보였고, 길의 굽이는 드러나지 않았다. 붉은 실제 나비는 원거리에서 점이 됐다.
- 설계: 군집(실제 나비 프리팹)은 6마리로 줄여 가까운 곳의 "진짜 나비" 역할만 맡기고, 경로 전체를 흐르는 **행렬**(`ButterflyStream`)을 새로 더했다.
  - 64마리가 플레이어 앞 1m에서 차례로(3초에 걸쳐) 날아올라 경로를 따라 7m/s(±30%)로 흐르고, 플레이어 앞 36m에 닿으면 흐려지며 다시 플레이어 앞에서 날아오른다. 늘 경로 전체에 흩어져 있어 궤적이 곧 길의 선이 된다. 목표 근처에서는 목표 위를 감싸 돌며 모인다.
  - 표시: 빌보드 나비 텍스처(흰 속·금빛·짙은 주황 테두리 + 옅은 후광)를 ParticleSystem 하나에 `SetParticles`로 쓴다. 날갯짓은 가로 폭을 접었다 펴는 것으로 흉내 낸다(셰이더·Animator 없음). 짙은 테두리는 하늘·눈·모래 같은 밝은 배경에서, 흰 속은 어두운 배경에서 윤곽을 만든다.
  - 거리 보정: 화면 높이의 `streamMinScreenFraction`(2.2%, 1080p 약 24px)보다 작아지지 않게 월드 크기를 키운다(기존 군집의 2.6배 상한 방식 대신 픽셀 하한).
  - 잔광: 행렬 나비가 1초에 5개씩 빛가루를 떨어뜨리고, 빛가루는 2.4초 동안 천천히 떠오르며 사라진다(가산 혼합, Noise). 지나간 길이 선처럼 남는다.
  - 지속: 연출 12초 → 18초, 페이드아웃 1.6초 → 2.4초. 페이드아웃 구간에서는 행렬이 새로 날아오르지 않고 날던 나비가 앞으로 흘러가며 사라진다. 탭 1회·재생 중 재입력 무시·종료 후 쿨다운은 그대로다.
  - 경로 띠 길이 30m → 36m(행렬과 같은 길이).
- 계약: `IGuideEffect`·`GuideContext`·컨트롤러·바람 연출은 바꾸지 않았다. 테스트용으로 `ButterflyGuideEffect.Stream`(읽기 전용)을 공개했다.
- 데이터: `GuideProfileSO` "나비 행렬" 헤더에 `streamPrefab`·`afterglowPrefab`·`streamCount`·`streamLength`·`streamNearOffset`·`streamSpeed`·`streamSpeedVariance`·`streamReleaseDuration`·`streamEdgeFadeDistance`·`streamLateralSpread`·`streamHeightSpread`·`streamFlutterAmplitude`·`streamSize`·`streamMinScreenFraction`·`streamFlapFrequency`·`streamColors`·`afterglowRate`·`afterglowLifetime`·`afterglowSizeRatio`·`afterglowRiseSpeed`. 프리팹이 비어 있으면 행렬은 계산만 하고 그리지 않는다(바람 프로필 영향 없음).
- 에셋(`Assets/03.Prefabs/UI/Guidance/`, `.gitignore` 대상이라 Git 상태에 안 나타남): `GuideButterflyStream.prefab`·`.mat`, `GuideButterflyAfterglow.prefab`·`.mat`, `GuideButterflySprite.png`(Python으로 절차 생성, 256px).
- 성능(추정, 프로파일러 미측정): 행렬 본체 1 + 잔광 1 드로콜, 동시 입자 본체 64·잔광 약 760(Play Mode 측정값). 군집이 10→6마리로 줄어 SkinnedMesh·Animator·후광·꼬리 개체가 각 4개 감소. 재생 중 관리 힙 할당 0은 EditMode 테스트(`AllocatingGCMemory`, 표시 프리팹 포함 64마리)로 통과.
- 검증: 정상 컴파일 오류 0, UI·Input EditMode **214/214** 통과(행렬 테스트 6개 추가: 경로 전체 분포, 끝에서 다시 날아오름, 페이드아웃 중 재비행 없음·종료 시 비움, 표시 개체 재사용, 경로 교체, 할당 0). `GuideProfileSO` 전용 커스텀 인스펙터는 없음(기본 인스펙터에 새 필드가 그대로 보임).
- NavMesh가 있는 다른 월드 씬(`P04Validation`·`P05Validation` 등 `ValidationNavMesh.asset` 보유)은 확인만 했고, 시험 중 다른 쪽에서 Play Mode를 쓰고 있어 씬을 바꾸지 않았다.
- 화면 확인(Play Mode, RiverBasin4km, 합성 S자 경로를 지면 높이에 맞춤, 별도 카메라): 초록 초원 3인칭 시점에서 S자 첫 굽이가 빛나는 나비 행렬로 읽힘, 태양 쪽을 보는 시점(해는 화면 밖), 측면 56m 원거리·고도 시점에서 점선 곡선으로 읽힘, 낮은 시점에서 하늘·눈 덮인 산 배경 대비 확인. 측정: 행렬 64마리 모두 비행, 경로상 1.2~35.1m에 분포, 1080p 기준 화면 크기 25~73px. 사용자 플레이 카메라로는 사막·눈 지역에서 초기 버전만 봤다.
- 화면 미확인: 밤·실내·숲 그늘, 언덕 너머로 이어지는 경로, 실제 NavMesh 경로(이 씬에 NavMesh 없음), 실제 3인칭 카메라 추종 중 걷기·멈춤 체감, 블룸 유무(이 씬 카메라는 후처리 꺼짐).
- 남은 위험: 가까운 시점에서 경로 띠(기존 `GuideRibbon`)가 큼직한 반투명 사각 무늬로 보인다. 행렬이 길을 충분히 보여 주면 띠를 줄이거나 끄는 것(`pathRibbonPrefab` 비우기)을 사용자 확인 후 결정한다.

## 사용자 직접 확인 — 모두 사용자 테스트 대기

대상: NavMesh가 있는 정규 월드, 퀘스트를 추적 중인 상태, 키보드 또는 실제 패드.

| ID | 조작 | 기대 결과·실패 기준 |
|---|---|---|
| GD01 | 추적 중인 퀘스트의 목표가 있는 곳에서 X(패드 L3+서쪽 버튼) 탭 | 플레이어 앞에서 빛나는 나비 행렬이 날아올라 길을 따라 약 36m 앞까지 끊임없이 흐르고(약 18초), 지나간 자리에 빛가루가 남음. 행렬의 굽이만 보고 길 방향을 알 수 있어야 함. 길과 다른 방향으로 날거나 한 덩어리로 뭉쳐 굽이가 안 보이면 실패 |
| GD02 | 재생 중 걷거나 멈춤 | 행렬은 늘 플레이어 앞 약 36m를 따라 함께 이동하고, 붉은 실제 나비 군집은 걸으면 앞서 기다리며 멈추면 점점 더 멀리(최대 26m) 길을 보여 줌. 나비가 플레이어 뒤로 처지면 실패 |
| GD03 | 추적 퀘스트가 없거나 길이 끊어진 목표에서 탭 | 나비는 나오지 않음(퀘스트가 있으면 퀘스트 UI만 약 6초 표시). 퀘스트도 없으면 아무 변화 없음 |
| GD04 | 탭을 연타 / 전투 중(적이 멀리서 추적 중 포함) / 대화·메뉴 중 | 연타는 쿨다운·타이머 갱신, 전투 중에는 나비·퀘스트 UI 모두 안 나옴, 대화 중에는 시작하지 않음 |
| GD08 | 평소 화면 / X 탭 | 시간·날씨 표시 없음, 퀘스트 UI 평소 숨김, X로 좌상단에 잠시 표시 후 사라짐 |
| GD05 | 길 밖으로 크게 벗어나기 / 퀘스트 완료·추적 해제 | 길을 다시 계산하거나 부드럽게 사라짐 |
| GD06 | 미니맵 HUD가 기본으로 보이지 않는지, 월드 마커는 그대로인지 | 미니맵 없음, 목표 마커 유지 |
| GD07 | 나비 크기·색·가독성(초원·숲·모래·눈·하늘 배경, 밤·실내 포함) | 멀리서도 읽히고 시야를 가리지 않음. 조정 위치: `GuideProfile_Butterfly`의 "나비 행렬" 헤더(`streamCount` 수, `streamSize`·`streamMinScreenFraction` 크기, `streamColors` 색, `afterglow*` 잔광, `streamLength` 길이), 연출 길이는 `duration` |

회신: `GD ID / 통과·실패·미수행 / 입력 장치·위치 / 실제 반응 / Console 오류 또는 화면`.

## 남은 항목

- 실제 월드 퀘스트 목표 3~5곳(가까운 곳, 먼 곳, 실내, 스트리밍 경계 너머, 도달 불가)에서 경로 상태 대조.
- 바람 프리팹의 화면 확인은 기록 섹션에 결과를 덧붙인다. 미관 품질은 사용자 확인이다.
- Guide 키 안내 글리프·설정 화면 표시, 첫 사용 안내(미니맵이 없어진 뒤 발견성).
