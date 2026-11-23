# 퀘스트 길안내 연출 설계 (나비 · 교체형)

- 작업 상태: [quest-path-guidance](../../../../docs/work-status/quest-path-guidance.md)
- 분야 위키: [플레이어와 화면](../../../../docs/wiki/player-ui.md)
- 상태: **P0~P4 구현 반영**(2026-11-22). 구현 범위·검증은 작업 상태 문서를 따른다. 실제 월드 길안내·미관·입력 체감은 사용자 테스트 대기.

## 0. 구현 시 확정·변경된 사항

- 미니맵 HUD는 `UI_HUD_GamePlay`의 `_showMinimapHud`(기본 false)로 기본 비표시. 목표 좌표 소스인 `MinimapMarkerRegistry`는 유지한다.
- 입력은 `PlayerAction.Guide`: 키보드 X, 게임패드 L3 + 서쪽 버튼(기존 슈퍼 점프 L3 + 남쪽 버튼과 같은 조합 방식). 입력 계약 검사기에 조합 예외를 추가했다.
- 연출 종류는 `GuideEffectFactory`가 `GuideProfileSO.effectKind`로 고른다. 구현체는 `ButterflyGuideEffect`, `WindGuideEffect`.
- **시간·날씨 HUD 제거**(2026-11-22): `UI_HUD_GamePlay`가 `HudWorldClock`을 더 이상 표시하지 않는다. `UI_HUD_WorldClock`·프리팹·빌더·`UIKeyType`은 보존한다.
- **퀘스트 UI는 X 입력 때만 노출**: `UI_HUD_Quest`는 평소 숨고, `UI_HUD_GamePlay.OnPerformedGuide`가 `PeekQuestInfo()`를 호출하면 표시할 퀘스트(추적 중 또는 활성 메인)가 있을 때 `_peekShowSeconds`(기본 6초) 동안 보였다가 부드럽게 사라진다. 연속 입력은 타이머만 갱신하고, 퀘스트가 없으면 아무것도 노출하지 않는다. 길(경로) 유무와 무관하다. 달성 연출 패널은 그대로 동작한다. 위치는 미니맵 자리인 좌상단 위쪽으로 200 올렸다(프리팹 `UI_HUD_Quest`).
- **전투 중 X 차단**: 나비와 퀘스트 노출 모두 막는다. 판정은 `PlayerCombat.IsInCombat`(교전 이벤트 후 30초, 20m 안 어그로 몬스터)에 더해, 플레이어를 노리는 어그로 몬스터가 `GuideProfileSO.combatThreatRadius`(기본 40m) 안에 있으면 전투로 본다(`NpcThreatScanner` 재사용). `allowInCombat`은 제거했다(항상 차단).
- **길 없음 반응 삭제**: 길을 확인하지 못하면 연출·SFX 없이 끝난다. `IGuideEffect.BeginNoPath`, NoPath 모드, `noPath*` 필드·SFX 키를 제거했다. 시작 SFX는 연출을 실제로 시작할 때만 재생한다.
- 재생 중 0.5초마다 플레이어 사망(즉시 정리)·전투 시작(페이드아웃)·경로 이탈(`repathDistance` 초과 시 재계산, 실패하면 페이드아웃)을 확인한다. 퀘스트 추적·완료·실패·목표 갱신 이벤트도 페이드아웃한다.
- 대화·팝업·컷신 중 입력은 HUD 입력 레이어(0)가 막는다.
- **나비 행렬 추가**(2026-11-22, 사용자 피드백 "더 많은 나비가 경로로 날아가게 / 가시성 부족"): 군집 한 덩어리만으로는 길의 굽이가 보이지 않아, 경로 전체를 흐르는 행렬 층을 더했다(아래 연출 후보 표). `IGuideEffect` 계약은 바꾸지 않았다.

## 1. 목적과 결정 사항

미니맵 HUD를 쓰지 않고, 특정 키를 누르면 추적 중인 퀘스트의 목표 지점으로 가는 **길**을 월드 안의 연출(1차: 나비 군집)로 보여준다. 연출은 나중에 바람 등으로 교체할 수 있어야 한다.

| 결정 | 내용 |
|---|---|
| 입력 | **탭 1회**로 연출 1회 재생(나비 프로필 기준 약 18초, `duration`). 연속 입력은 쿨다운(기본 1.5초)으로 흡수. 홀드 지속 방식은 쓰지 않는다. |
| 경로 없음 | **진행하지 않는다.** 직선 방향 폴백도, 길 없음 반응도 만들지 않는다. 연출·SFX 없이 아무것도 나오지 않는다(퀘스트 UI 노출은 별개로 수행). |
| 부분 경로 | 스트리밍 미로드로 NavMesh가 끊긴 `PathPartial`은 "경로 없음"으로 취급한다. 끝점까지만 보여 주면 막다른 길로 오인하게 되므로 길 전체를 확인한 경우에만 재생한다. |

## 2. 재사용하는 기존 코드

| 필요 | 기존 자산 |
|---|---|
| 목표 해석 | `QuestObjectiveMarker.ResolveLocationId` → `MinimapMarkerRegistry.TryGet` → `registrar.transform` (`QuestWorldMarkerBridge`와 동일 경로) |
| 경로 계산 | 월드의 `NavMeshSurface`, `NavMesh.CalculatePath` (`PetFollowController`, `ContinuousWorldRegion` 참고) |
| 상태 이벤트 | Quest 이벤트(수락·완료·추적·해제·목표갱신) 구독 방식 |

미니맵 HUD(`UI_HUD_Minimap`, `UIKeyType.Minimap`)는 끄더라도 `MinimapMarkerRegistry`/`MinimapMarkerRegistrar`는 목표 좌표의 소스이므로 유지한다.

## 3. 구조: 경로와 연출 분리

```
[Guide 입력] → GuidanceController
   1) 목표 해석(위 2절)
   2) IGuidePathProvider.TryBuild → GuidePath (실패 시 아무 연출 없이 종료)
   3) IGuideEffect.Begin(GuideContext)
GuideProfileSO가 연출 종류와 수치를 정한다.
```

- `GuidePath`: 월드 좌표 점(재사용 버퍼), 누적 거리, 총 길이, 상태(`Complete`/`None`). 상태에 `Partial`·`Fallback`은 두지 않는다.
- `IGuidePathProvider`: `TryBuild(from, to, GuidePath result)`. 1차 구현은 `NavMeshGuidePathProvider`(코너를 곡선으로 보간).
- `IGuideEffect`: `Begin(GuideContext)`, `Tick(dt)`, `Stop(bool immediate)`. 연출은 퀘스트·NavMesh·입력을 알지 못하고 경로와 프로필만 받는다. 이것이 교체의 경계다.
- `GuideContext`: 경로, 플레이어 Transform, 프로필, 종료 콜백.

### 연출 후보

| 연출 | 방식 |
|---|---|
| 나비 | 두 층. **군집**: 실제 나비 프리팹 몇 마리(`agentCount`, 기본 6)가 플레이어 전방 시야에서 나타나 경로를 따라 플레이어보다 `leadDistance` 앞서 난다. 플레이어가 멀어지면 속도를 줄이고 가까워지면 올린다. 목표 반경 `arriveRadius`에서 선회 후 소멸. **행렬**(`ButterflyStream`): 빛나는 빌보드 나비 수십 마리(`streamCount`, 기본 64)가 플레이어 앞(`streamNearOffset`)에서 날아올라 `streamLength`(36m)까지 경로를 따라 흐르고, 끝에 닿으면 다시 플레이어 앞에서 날아오른다. 나비가 경로 전체에 늘 흩어져 있어 궤적만 봐도 길의 굽이가 읽힌다. 지나간 자리에는 빛가루(잔광)가 몇 초 남는다. 원거리에서도 화면 높이의 `streamMinScreenFraction` 아래로 작아지지 않는다. 행렬은 ParticleSystem 두 개(본체·잔광)에 미리 할당한 배열로 위치를 쓰므로 드로콜 2, 재생 중 할당 0이다. |
| 바람 | VFX Graph 리본/낙엽이 경로 스플라인을 따라 흐른다(입자 수가 많아 GPU 경로). |
| 기타 | 빛 입자·발자국 등은 `IGuideEffect` 구현만 추가한다. |

## 4. 경로 규칙과 위험

1. 길안내는 "걸어서 갈 수 있는 길"로 정의한다. 점프·등반·수영 지름길은 NavMesh에 없을 수 있다.
2. 스트리밍 월드에서 목표 쪽 NavMesh가 로드되지 않았으면 경로 없음(연출 없음). 퀘스트 목표가 항상 길 없음이 되는 지역이 없는지 검증 대상에 포함한다.
3. 연출 높이는 지면 샘플 + 높이 오프셋으로 보정한다(실내·다층 대응).
4. 재계산: 연출 시작 시 1회, 재생 중 플레이어가 경로에서 `repathDistance` 이상 벗어났거나 목표가 바뀔 때만. `NavMeshPath`를 재사용하고 매 프레임 계산하지 않는다.
5. 추적 퀘스트 없음, 추적 억제(`IsQuestTrackingSuppressed`), 목표 좌표 없음은 모두 "경로 없음"으로 통일하며 연출하지 않는다.

## 5. 입력·피드백

- `Guide` 입력 액션 1개. 키보드·게임패드 바인딩은 UI 입력 프롬프트 체계와 맞춘다. HUD 입력 레이어(0)에서 처리하고 대화·팝업·컷신·일시정지 중에는 무시한다.
- 전투 중에는 항상 무시한다(위 0절 판정 방식).
- 연출을 실제로 시작할 때 가벼운 날갯짓 SFX(`startSfxKey`)를 재생한다.
- 재생 중 재입력은 무시한다(쿨다운 기본 1.5초는 재생 종료 후 기준).
- 야간에는 발광 옵션으로 대비를 확보한다.

## 6. 데이터와 모듈 배치

`GuideProfileSO`(`Assets/10.Datas/`, 메뉴 `UPlayGround/Guide/Profile`): 연출 종류·프리팹 주소(Addressables), 개체 수, 스폰 반경, `leadDistance`, 속도·팔랑임 진폭/주파수, 수명, 쿨다운, `repathDistance`, `arriveRadius`, `combatThreatRadius`, 야간 옵션, 시작 사운드 키. 지역·퀘스트별 오버라이드는 세 번째 요구가 생길 때까지 만들지 않는다.

- `Data`: `GuideProfileSO`
- 계약(`IGuidePathProvider`, `IGuideEffect`, `GuidePath`): UI 소비자 모듈이 소유(UI가 사용하는 계약은 `UISvc` 경유)
- `GuidanceController`, `NavMeshGuidePathProvider`: UI 모듈의 `QuestWorldMarkerBridge` 근처. 신규 매니저 싱글톤 참조 금지.
- 연출 구현·프리팹: Gameplay 쪽 폴더. 연출 풀은 연출 전용으로 새로 만든다(`FootstepEffectPool`은 internal이므로 재사용하지 않는다).

## 7. 단계

1. **P0** 계약 + 컨트롤러 + NavMesh 경로 제공자 + 경로 디버그 기즈모. 연출 없이 경로 정확성(Complete/None 분류)을 먼저 검증한다.
2. **P1** 나비 연출·풀링·리드 거리 추종·도착 선회·입력·SFX·경로 없음 반응.
3. **P2** 억제 조건(전투·대화·컷신·추적 억제), 저장/로드·씬 전환 후 상태.
4. **P3** 바람 구현체를 하나 추가해 `IGuideEffect`가 실제로 교체를 허용하는지 증명한다. 나비에 과하게 맞춰진 계약이 드러나면 여기서 고친다.
5. **P4** 미니맵 HUD 비표시 정리와 가이드·위키 갱신.

## 8. 검증

- 자동(EditMode): 경로 제공자 분류(완전/경로 없음)·버퍼 재사용, 쿨다운, 재계산 조건, 입력 억제 조건.
- 실월드 로그 대조: 가까운 목표, 먼 목표, 실내, 스트리밍 경계 너머, 도달 불가 목표 각 1곳 이상.
- 연출의 가독성·미관과 실제 입력 체감은 자동으로 판정하지 않고 **사용자 테스트 대기**로 분리한다.
- Player·Addressables 빌드는 별도 지시 전 실행하지 않는다.
