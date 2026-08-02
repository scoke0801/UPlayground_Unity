# 궁극기 시퀀스 에디터 고도화 백로그

> 작성일: 2026-07-23
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 백로그(미구현 계획). 각 항목 구현 PR 시 본 문서에서 해당 카드를 제거하거나 완료 표기한다.
> 관련 문서: `Assets/docs/design/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md` (런타임 설계)
> 관련 코드:
> - `Assets/02.Scripts/Editor/Ultimate/UltimateSequenceEditorWindow.cs` (UIToolkit 윈도우)
> - `Assets/02.Scripts/Editor/Ultimate/UltimateTimelineTrackView.cs` (인터랙티브 타임라인 캔버스)
> - `Assets/02.Scripts/Editor/Ultimate/UltimateEventClipboard.cs` (타입 메타·딥클론·클립보드)
> - `Assets/02.Scripts/Editor/UIToolkit/Styles/UltimateSequenceEditor.uss`
> - 런타임: `Assets/02.Scripts/GameActor/Component/Player/UltimateSequencePlayer.cs`, `GameActor/Combat/Ultimate/UltimateTimelineEvent.cs`

---

## 1. 개요 / 현재 상태

궁극기 시퀀스 에디터를 IMGUI에서 UI Toolkit으로 전환하는 **1차 작업은 완료(2026-07-23)** 되었다. 본 문서는 그때 다음 단계로 미룬 "기능 고도화 + UX 개선" 항목만 백로그로 남긴다.

### 이미 구현된 것 (재제안 금지)

1차 UIToolkit 전환에서 아래는 **이미 동작**한다. 백로그 항목이 이 위에 얹히는 것이므로 재발명하지 않는다.

| 영역 | 구현 |
|------|------|
| 윈도우 골격 | `CreateGUI` + `TwoPaneSplitView`(타임라인 \| 인스펙터), 공용 `--up-*` 토큰 재사용 |
| 인터랙티브 타임라인 | 블록 드래그 이동 / 좌·우 가장자리 리사이즈, `motionSet` 길이 클램프 |
| 다중 선택 | Ctrl/Shift 클릭 토글, 빈 곳 마퀴 박스 선택, `Ctrl+A`, 그룹 리지드 이동 |
| 레인 패킹 | 겹치는 이벤트를 픽셀 공간에서 여러 행으로 자동 분리 (줌마다 재계산) |
| 복사/붙여넣기 | 에셋 간 리플렉션 딥클론(`prefab`/`AudioClip`/카메라 SO는 참조 공유), `Ctrl+C/V/D` |
| 스냅 | fps 기준 `Round(t*fps)/fps` 토글 + fps 필드 |
| 인스펙터 | 선택 이벤트 `PropertyField` 바인딩(전 필드), 시퀀스 설정 바인딩 |
| 검증 | 툴바 pill(정상/경고 N/오류) + 인스펙터 상세 뱃지 |
| PlayMode | 60ms 폴링으로 `RuntimeContext.ElapsedTime` 커서 표시, 테스트 실행/중단 |
| 기타 | 우클릭 컨텍스트 메뉴, 줌 슬라이더, 이동 없는 클릭은 Undo·dirty 미기록 |

### 공통 제약 (모든 항목에 적용)

- **실행 Motion의 단일 소스는 유지**한다. 에디터는 `motionSet`/타임라인 이벤트만 저작하고, 공격 수치·MotionReference 규약(`CLAUDE.md` Ability 절)을 건드리지 않는다.
- 타임라인 시간축 기준은 런타임과 일치해야 한다. 런타임은 `timelineUseUnscaledTime`가 꺼지면 `ActorAnimator.CurrentMotionSetTime`을, 켜지면 unscaled 누적을 쓴다(`UltimateSequencePlayer.Update`). 에디터 프리뷰도 이 두 축을 구분해야 한다.
- 신규 UI는 `up-editor-root` 테마 토큰과 `UltimateSequenceEditor.uss` 클래스 체계를 따른다. 인라인 색상 하드코딩 최소화.

---

## 2. 우선순위 요약

| # | 항목 | 가치 | 난이도 | 리스크 | 권장 순서 |
|---|------|------|--------|--------|-----------|
| A | 에디터 타임 프리뷰 스크러빙 | 매우 높음 | 높음 | 중 (모션 샘플링 부수효과) | 1 |
| B | 이벤트 템플릿 / 프리셋 | 높음 | 낮음 | 낮음 | 2 |
| C | 스냅 가이드 라인 / 정렬 스냅 | 중 | 낮음 | 낮음 | 3 |
| D | 클립보드 시각 표시 · 시간 기준 붙여넣기 | 중 | 낮음 | 낮음 | 3 |
| E | 이벤트 검증 심화 (참조 누락·중복·순서) | 중 | 중 | 낮음 | 4 |
| F | 다중 시퀀스 브라우저 / 캐릭터 스코프 | 중 | 중 | 낮음 | 4 |
| G | 카메라·모션 통합 프리뷰 트랙 | 높음 | 높음 | 중 | 5 |

---

## 3. 백로그 카드

### A. 에디터 타임 프리뷰 스크러빙 (PlayMode 없이)

- **목적**: PlayMode에 들어가지 않고 타임라인 커서를 문질러(scrub) 모션 포즈 + 이벤트 발화 타이밍을 즉시 확인. 현재는 PlayMode에서만 커서가 움직인다(`UltimateSequenceEditorWindow.PollPlayMode`).
- **근거**: 궁극기는 타격·VFX·카메라 타이밍을 프레임 단위로 맞추는 작업이라, PlayMode 왕복 비용이 저작 속도의 최대 병목. 명조식 연출 저작 워크플로의 핵심.
- **구현 스케치**:
  - 타임라인 룰러 영역 드래그로 "프리뷰 커서" 이동(현 `UltimateTimelineTrackView`는 룰러 드래그를 예약만 해둔 상태 — PlayMode 커서 `SetPlayCursor`와 분리된 사용자 커서 채널 추가).
  - 모션 포즈: `MotionSetEditorWindow`(`Assets/02.Scripts/Editor/`)의 루트모션 프리뷰 인프라를 재사용해 프리뷰 아바타에 `MotionSet`을 시간 `t`로 샘플링한다. 해당 프리뷰가 Player 런타임에 이중 적용되지 않도록 프리뷰 경계를 분리할 것.
  - 이벤트 드라이런: 실제 `Execute`/`Complete` 호출은 위험(프리팹 Instantiate, 사운드 재생, 카메라 점유). 대신 "발화 마커"만 표시하는 **드라이런 모드**를 기본값으로. 실제 발화는 명시적 토글에서만.
- **주의/리스크**:
  - `UltimateTimelineEvent.Execute`는 부수효과가 크다(VFX Instantiate, `Svc.Sound.PlayClip`, `CameraManager` 점유, `Svc.GameTime.Request`). 에디트 모드에서 절대 무분별 호출 금지.
  - 시간축 두 종류(`timelineUseUnscaledTime`) 모두에서 커서→모션시간 매핑이 일치해야 한다.
  - 편집 모드 모션 샘플링은 씬/프리뷰 유틸 상태를 오염시키지 않도록 `PreviewRenderUtility` 또는 MotionSetEditor 전용 프리뷰 경계를 사용.
- **완료 기준**: PlayMode 없이 커서 스크럽 시 프리뷰 아바타 포즈가 갱신되고, 각 이벤트의 발화/종료 지점에 마커가 표시된다. 실제 부수효과는 발생하지 않는다.

### B. 이벤트 템플릿 / 프리셋

- **목적**: 자주 쓰는 이벤트 묶음(예: "타격 1세트 = DamageWindow + CameraShake + Sound")을 한 번에 추가.
- **근거**: 캐릭터별 궁극기가 34개 AbilitySet 규모로 늘면 동일 패턴 반복 저작 비용이 큼. 복사/붙여넣기는 이미 있으나(에셋 간), 프로젝트 표준 프리셋은 별도.
- **구현 스케치**:
  - `UltimateEventClipboard.Kinds`와 나란히 `프리셋` 정의 추가(코드 상수 또는 소형 SO `UltimateEventPresetSO`).
  - 추가 메뉴(`＋ 이벤트`)에 "프리셋" 하위 항목. 선택 시 `AppendEvents`로 딥클론 삽입(이미 존재하는 경로 재사용).
  - 선택 이벤트들을 "프리셋으로 저장" → 클립보드 클론을 프리셋 목록에 등록.
- **주의/리스크**: 프리셋에 담긴 `prefab`/SO 참조는 클립보드와 동일하게 참조 공유로 취급. 캐릭터 전용 에셋 참조가 섞이면 부적절 — 참조 없는 "구조 프리셋"과 "완전 프리셋"을 구분.
- **완료 기준**: 프리셋 추가 1회로 다중 이벤트가 올바른 상대 타이밍으로 삽입되고 선택된다.

### C. 스냅 가이드 라인 / 정렬 스냅

- **목적**: 드래그 중 다른 이벤트 경계·모션 끝·프레임 격자에 붙는 시각 가이드 라인 표시. 현재 스냅은 fps 격자에만 적용되고 시각 피드백이 없다.
- **근거**: "이 타격을 저 VFX 시작에 정확히 맞추기" 같은 정렬 작업의 정확도/속도 향상.
- **구현 스케치**:
  - `UltimateTimelineTrackView.OnPointerMove`의 드래그 값 계산 뒤, 후보 스냅 타겟(다른 블록의 start/end, 모션 끝 `duration`, 격자)과의 근접도를 검사해 가장 가까운 것에 흡착.
  - `DrawGrid`와 별개 오버레이 레이어(`generateVisualContent`)에 세로 가이드 라인 1~2개 렌더.
  - 스냅 임계값은 픽셀 기준(예: 6px). Alt 키로 스냅 일시 해제.
- **주의/리스크**: 기존 fps 스냅(`SnapValue`)과 우선순위 정의 필요(이벤트 경계 스냅 > fps 격자 스냅 등). 드래그 성능 유지(후보 수가 적어 O(n) 허용).
- **완료 기준**: 드래그 중 근접 경계에 가이드 라인이 뜨고 값이 흡착되며, Alt로 해제된다.

### D. 클립보드 시각 표시 · 시간 기준 붙여넣기

- **목적**: 현재 클립보드에 몇 개가 담겼는지 표시하고, 붙여넣기 위치를 "현재 커서 시간" 또는 "원본 시간 유지" 중 선택.
- **근거**: 현재 `PasteClipboard`는 원본 시간 그대로 append만 한다. 커서 기준 붙여넣기가 있으면 특정 구간으로 이벤트를 옮겨 재사용하기 쉽다.
- **구현 스케치**:
  - 툴바 `붙여넣기` 버튼 라벨에 개수 표시(`UltimateEventClipboard.Count`). 이미 `HasContent`로 enable 토글 중이므로 라벨만 확장.
  - 컨텍스트 메뉴에 "여기에 붙여넣기(커서 시간)" 추가 → 클론들의 최소 start를 커서 시간으로 평행 이동 후 `AppendEvents`.
- **주의/리스크**: 시간 이동 시 모션 길이 초과 클램프 정책을 드래그와 일치시킬 것.
- **완료 기준**: 클립보드 개수가 보이고, 커서 기준 붙여넣기가 상대 타이밍을 유지한 채 이동 삽입된다.

### E. 이벤트 검증 심화

- **목적**: 현재 검증(에셋 유효성 / 카메라 프로필 없음 / 모션 길이 초과)을 넘어, 이벤트 단위 문제를 잡는다.
- **후보 규칙**:
  - `UltimateSpawnVfxEvent.prefab`, `UltimateSoundEvent.clip`, `UltimateCameraShakeEvent.shake` 등 **필수 참조 누락**.
  - `UltimateDamageWindowEvent` 구간 중복/겹침(콜리전 활성/비활성 페어링 꼬임 가능성).
  - `UltimateTimeScaleEvent` `duration==0`(즉시 종료로 무효) 경고.
  - `UltimateCustomCallbackEvent.callbackName` 공백/미존재 수신자.
- **구현 스케치**: `UltimateSequenceEditorWindow.Validate`의 `items` 수집부에 타입별 규칙 추가. 심각도(0/1/2) 체계 그대로 사용. 이벤트별 문제는 해당 블록에 뱃지/외곽선으로 역표시하면 UX 향상(`UltimateTimelineTrackView`에 per-block 경고 클래스 추가).
- **주의/리스크**: `SendMessage` 수신자 존재 검사는 런타임 캐스터 타입 의존이라 에디터에서 완전 검증 불가 — "확인 불가" 수준 경고로만.
- **완료 기준**: 규칙 위반 이벤트가 pill/뱃지와 블록 표시에 함께 드러난다.

### F. 다중 시퀀스 브라우저 / 캐릭터 스코프

- **목적**: 좌측에 프로젝트 내 `UltimateSequenceAsset` 목록(캐릭터별 그룹)을 두고 빠르게 전환. 현재는 ObjectField/Selection 연동만 지원.
- **근거**: `GameplayAbilityEditorWindow`의 스코프 팝업/리스트 패턴과 동일한 저작 편의. 캐릭터별 1개 원칙(`UltimateSequencePlayer.ResolveAsset`)과 잘 맞음.
- **구현 스케치**: `AssetDatabase.FindAssets("t:UltimateSequenceAsset")`로 수집 → `ownerType` 그룹핑 → 3패널(목록 \| 타임라인 \| 인스펙터)로 확장. 누락/중복 소유자 경고.
- **주의/리스크**: 3패널 확장 시 최소 폭·스플릿 저장 로직 추가 필요(`MotionEditorShell`의 EditorPrefs 폭 저장 패턴 참고).
- **완료 기준**: 캐릭터별 에셋을 목록에서 선택해 즉시 편집, 소유자 누락/중복이 목록에 표시된다.

### G. 카메라·모션 통합 프리뷰 트랙

- **목적**: 타임라인에 모션 이벤트(MotionSet)와 카메라 스냅샷 시퀀스(`cameraProfile`) 타이밍을 **읽기 전용 참조 트랙**으로 겹쳐 표시. 궁극기 이벤트를 이것들에 맞춰 배치.
- **근거**: 궁극기는 모션·카메라·연출 이벤트 3자 동기화가 본질. 서로 다른 창을 오가지 않고 한 화면에서 정렬.
- **구현 스케치**: 별도 상단 참조 레인 2개(모션 이벤트 마커, 카메라 샷 경계)를 `UltimateTimelineTrackView`에 옵션 표시. 편집 불가·정보 전용. 데이터는 `MotionSet` 이벤트 목록과 `CameraSnapshotProfile` 샷 목록에서 읽음.
- **주의/리스크**: 두 소스의 시간축 정의가 시퀀스 시간축과 일치하는지 확인(카메라 프로필은 자체 블렌드/공전 타이밍을 가질 수 있음). 읽기 전용 유지로 소스 오염 방지.
- **완료 기준**: 참조 트랙 토글 시 모션/카메라 타이밍이 궁극기 이벤트와 같은 축에 표시된다.

---

## 4. 비목표 (Non-goals)

- 공격 수치·MotionReference·AbilitySet 저작은 본 에디터 범위 밖(각각 Ability Editor / MotionSet Editor 소관).
- 런타임 `UltimateSequencePlayer` 실행 로직 변경은 본 백로그에 포함하지 않는다(에디터 저작 UX 한정).
- 카메라 스냅샷 자체 편집은 `CameraSnapshotEditorWindow`(퀵링크로 이미 연결됨) 소관.
