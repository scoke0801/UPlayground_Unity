# 무기별 궁극기 연출 + 사이드 얼굴 패널 설계

> 작성일: 2026-11-14 · 분류: **코드·데이터 반영/자동 검사 통과, 미술·플레이 인수 대기**. [현재 상태·자동 검사·남은 확인](../../../../../docs/work-status/weapon-ultimate-presentation/STATUS.md)을 따른다. P0 이미지 2종은 잠정 채택, P1~P4 코드·기존 에셋 연결·프로필/카메라 초안을 반영했다. 조건부 신규 클립 제작은 기존 후보의 실제 조합 시각 검수 후 필요가 확정되면 진행한다.
> 관련 정본: [연출 스테이지 설계](../../content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.md), [궁극기 에디터 TODO](../../../Complete/ULTIMATE_SEQUENCE_EDITOR_ADVANCEMENT_TODO.md), [전투 저작 가이드](../../../guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md)
> 사용자 구도 실패 반영: 자동 Snapshot의 실제 궁극기 연결은 해제했다. 카메라10종은 후보만 보관하며 기존 모션/Stage 카메라를 사용한다. [구도 회귀 복구](../../../../../docs/work-status/weapon-ultimate-presentation/CAMERA_FRAMING_FIX.md).
> 관련 코드: `GameActor/Combat/Ultimate/UltimateSequenceAsset.cs`, `UltimateTimelineEvent.cs`, `Data/Combat/Cinematic/CinematicStageSO.cs`, `Data/Enum/WeaponType.cs`

## 0. 결정이 필요한 것 (먼저 읽기)

| # | 질문 | 권장 | 영향 |
|---|---|---|---|
| D1 | 얼굴 소스 | **확정(사용자): Codex CLI ImageGen으로 스킬 연출용 얼굴 이미지 제작** (§3.2) | 아트 제작·스타일 통일 |
| D2 | 적용 범위 | **확정(사용자): "궁극기" 스킬에만 적용** | 제작량 |
| D3 | 연출 형태 | **확정(사용자 레퍼런스 이미지): 화면 좌측의 얼굴 패널(사이드 페이스 스트립)**. 전체화면 컷인 아님 (§3) | UI·구현 |
| D4 | 신규 애니메이션 클립을 Blender로 만드는 범위 | 요청 확정: 기존 클립으로 불가능한 피니셔만 (§7). 접촉/T3는 별도 결정 대기 | 작업량 최대 변수 |
| D5 | 카메라를 Blender에서 만들어 붙일까 | 요청 확정: `CameraSnapshotProfile`로 저작, Blender는 프리비즈 | 파이프라인 |

## 1. 웹 조사 결과 — 확인된 것과 못 한 것

### 1.1 확인된 사실 (출처 있음)

- **명일방주: 엔드필드** — 전투 스킬은 팀 공유 자원, 궁극기 에너지는 전투 스킬이 팀 전체에, 연계 스킬이 시전자에게만 채운다. 연계 스킬은 조건 충족 시 **화면에 프롬프트**가 뜨고 버튼으로 발동한다. 연계 스킬 중 **시간이 잠깐 느려지고 카메라가 살짝 줌인**한다는 서술이 있다. 궁극기는 "화려한 애니메이션"으로 평가되며, 궁극기 에너지가 차면 전투 스킬 버튼 홀드로 발동한다. 전투 카메라는 최적화됐고 3D 전환 후 손그림 질감이 조명·시점에 따라 변하도록 구현했다고 개발자 인터뷰에 있다.
  - 출처: [Prydwen 전투 기본](https://www.prydwen.gg/arknights-endfield/guides/combat-basics)(검색 요약으로만 확인, 직접 열람은 403), [Endfield 전투 가이드](https://arknights-f4s.pages.dev/endfield/guides/combat), [GameDeveloper 개발자 인터뷰](https://www.gamedeveloper.com/press-release/arknights-endfield-reimagined-an-in-depth-interview-with-light-zhong-and-yifeng-huang), [gamewith](https://gamewith.net/akendfield/72441)
- **일반 패턴** — 얼굴 컷인은 "Super Move Portrait Attack"로 불리는 오래된 관용구이고, 모델 표정이 충분하면 **컷인 대신 시전자 얼굴로 카메라를 줌**하는 대안이 흔하다. 짧은 슬로모션 프리즈 + 뚜렷한 카메라 컷이 발동 신호로 쓰인다.
  - 출처: [TV Tropes: Super Move Portrait Attack](https://tvtropes.org/pmwiki/pmwiki.php/VideoExamples/SuperMovePortraitAttack)

### 1.2 확인하지 못한 것 (정직한 공백)

- **엔드필드 궁극기의 얼굴 컷인 UI**(레이아웃·연출 길이·스킵 여부)는 검색으로 확인하지 못했다. 위 서술은 연출의 존재만 뒷받침한다.
- **"스칼렛 스트링스"**는 영문·한글 검색 모두 해당 게임 자료가 나오지 않았다. 표기가 다르거나(예: 다른 제목) 자료가 적은 작품일 수 있다. **정확한 제목·플랫폼·참고할 장면(영상 링크)을 알려주면** 다시 조사한다.
- 따라서 이 문서의 패널 설계는 특정 게임의 복제가 아니라 **위 일반 패턴을 이 프로젝트 구조에 맞춘 독자 설계**다. 사용자 레퍼런스 이미지(§3)로 형태를 확정했고, 속도·길이는 영상 확인 후 보정한다.

## 2. 현재 자산 — 재사용할 것과 빈 곳

**이미 있음 (새로 만들지 않는다)**

| 자산 | 용도 |
|---|---|
| `UltimateSequenceAsset` + `[SerializeReference] events` | 캐릭터별 시간축 연출. `startTime/duration` 이벤트 목록 |
| 이벤트: VFX·사운드/보이스·타임스케일·카메라 효과·카메라 흔들림·데미지 윈도우·스테이지 진입/복귀 | 비트 구성 부품 |
| `CinematicStageSO` T0~T3, 시전자 클론 무대, 실루엣 타깃 | 배경 격리형 연출 |
| `UltimateLetterboxSettings`, `UltimateSequenceUISettings.hideHud` | 시네마 바, HUD 숨김 |
| `CameraSnapshotProfile` | 연출 카메라 |
| `Dialogue` 포트레이트(`SpeakerPortraitTableSO`, `DialoguePortraitSetSO`) | 얼굴 이미지 소스 후보 |
| `WeaponType` 10종(Sword, SwordShield, GreatSword, Staff, Bow, Katana, DoubleAxe, Whip, Spear, DualBlade) | 연출 템플릿 키 |
| 현재 궁극기 에셋 | `UltimateSequence_Raon`, `UltimateSequence_Hwarin` 2종뿐 |

**빈 곳**

1. **얼굴 패널을 띄우는 UI 이벤트가 없다.** 타임라인에서 UI를 띄우는 이벤트 자체가 없다.
2. **무기별 연출 템플릿이 없다.** 캐릭터별 에셋을 처음부터 손으로 짜야 한다. 12캐릭터 × 수작업은 일관성·제작량 모두 문제다.
3. 전투 포트레이트(풀 바디가 아닌 흉상/얼굴 크롭)가 대화 포트레이트와 같은지 미확인 — 대화용은 표정 파츠 합성형(`UIDialoguePortraitPartImage`)이다.

## 3. 얼굴 UI 설계 — 사이드 페이스 스트립 (사용자 레퍼런스 반영)

사용자가 준 레퍼런스 화면(엔드필드 전투 중 캡처)에서 확인한 연출은 **전체 화면을 가리는 컷인이 아니다.**

- 화면 **왼쪽 가장자리**에 가로로 긴 반투명 패널이 붙고, 그 안에 시전자의 **얼굴(눈~턱, 얼굴이 패널보다 크게 삐져나옴)** 이 들어간다.
- 패널 배경은 옅은 선 무늬이고, 패널 밖으로 머리카락·어깨가 살짝 넘친다.
- **전투 화면(적·데미지 숫자·카메라)은 그대로 보인다.** 게임 플레이를 멈추지 않고 "누가 궁극기를 쓰는가"를 알리는 보조 연출이다.
- 위치는 HUD의 좌측 하단 영역(조이스틱 위, 화면 높이 약 45~65%)이다.

따라서 앞선 설계의 전체화면형 레이아웃(DiagonalSlash·ScreenWipe 등)은 **폐기**하고 아래 단일 형태로 대체한다. 이 레퍼런스는 사용자 제공 이미지 1장이므로 **슬라이드 인/아웃 속도, 유지 시간, 패널 폭**은 영상으로 확인되지 않았다. §3.4 값은 제안값이며 구현 후 사용자와 조정한다.

### 3.1 얼굴 소스 — 결정 반영

사용자 결정: **스킬 연출용 얼굴은 Codex CLI ImageGen으로 제작한다.** 따라서 대화 포트레이트(파츠 합성형)를 재사용하지 않고 **궁극기 전용 컷 이미지**를 새로 만든다. 3D 페이스캠(RenderTexture)은 제외한다(연출 스테이지 §7.12 결정 유지).

### 3.2 이미지 규격 (ImageGen 브리프)

| 항목 | 규격 |
|---|---|
| 구도 | 가슴 위 ~ 머리, 시선은 화면 정면 또는 살짝 아래. 궁극기답게 **눈빛이 강한 표정** 1종 |
| 비율·크기 | 가로형 패널용. 1024×512 이상, 얼굴이 패널 높이의 120~140% (패널 밖으로 넘침) |
| 배경 | **투명 PNG(알파)**. 패널 무늬는 이미지에 굽지 않고 UI 패널로 분리 |
| 일관성 | 12캐릭터가 같은 조명·선 굵기·채색으로 나오도록 **공통 스타일 프롬프트 + 캐릭터별 외형 설정(헤어·눈 색·의상 포인트)** 사용. 기존 캐릭터 설정화/대화 포트레이트를 레퍼런스 입력으로 넣는다 |
| 저장 | `Assets/04.Images/` 하위 전용 폴더(예: `SkillCutIn/`), 이름 `CutIn_<캐릭터>.png`. 구매 원본·기존 `.meta`는 건드리지 않음 |
| Unity 설정 | Sprite(2D), 알파 소스 유지, mipmap 끔, 압축 품질은 시각 확인 후 결정 |

- 외형이 캐릭터 정본과 맞는지, 스타일이 12종 간 통일되는지는 **사용자 확인이 필요**하다(미술 품질 판정 불가).
- 현재 궁극기 에셋이 있는 Raon·Hwarin부터 2종을 만들어 스타일을 확정한 뒤 나머지로 확장한다.

### 3.3 구조

```
UltimateFaceStripEvent : UltimateTimelineEvent     // namespace Data, 실제 Actor 모듈
  Sprite faceOverride            // 비면 캐릭터 정의의 궁극기 얼굴 이미지
  float inDuration / hold / outDuration
  bool useAccentOverride / Color accentOverride
UISvc.FaceStrip (IFaceStripPresenter)   // UI 소비자 계약. 이벤트는 UISvc만 본다
UIFaceStrip (프리팹)            // 패널 + 얼굴 Image + 얼굴 아래 이름/스킬명(선택)
PlayerCharacterDefinitionSO.ultimateFaceSprite   // 캐릭터별 기본 얼굴 (신규 필드)
```

- 실제 기존 Ultimate 이벤트는 `UPlayGround.Actor`에 있다. 새 이벤트도 Actor에 유지한다. 공용 Contracts의 `IFaceStripPresenter`/`FaceStripRequest`를 `Svc.FaceStrip`으로 호출하며 UI의 `UISvc.FaceStrip`은 동일 계약 별칭이다. Actor→UI/ Data→Actor 역참조를 추가하지 않는다(Astra 검증).
- 실행 context가 실제 presenter와 표시 토큰을 소유한다. 이벤트 SO에는 런타임 토큰을 저장하지 않는다. 이벤트 Complete는 실시간 퇴장을 끊지 않고 Restore가 토큰을 반납한다. 실제 pause는 `IGameTimeService.IsPaused`로 구분해 패널만 닫으며 궁극기 전투 실행은 바꾸지 않는다.
- **`hideHud=true`여도 스트립은 보여야 한다.** HUD 캔버스에 넣으면 같이 숨겨지므로 HUD와 분리된 전용 캔버스에 두고, 레터박스와 겹치면 레터박스 안쪽에 오도록 정렬한다.
- 표시 전용이다. 게임패드 포커스·입력을 가져가지 않는다.
- 모든 트윈 `SetUpdate(true)`로 타임스케일 무관하게 재생한다.
- 연출 스테이지(T2) 사용 시 전용 카메라가 켜져도 UI 오버레이는 영향받지 않아야 한다 — 검증 항목.

### 3.4 연출 값 (제안)

| 단계 | 시간 | 동작 |
|---|---|---|
| 등장 | 0.18s | 왼쪽 밖에서 슬라이드 인 + 얼굴이 약간 늦게 따라 들어오는 시차 |
| 유지 | 궁극기 핵심 구간 길이(기본 1.2~1.6s) | 약한 숨쉬기 움직임(1~2% 스케일), 피니시 직전 속성색 플래시 |
| 퇴장 | 0.2s | 페이드 + 왼쪽으로 슬라이드 아웃 |

- 궁극기 시작 비트(예고)에 맞춰 띄우고, 피니시 비트 종료 전에 퇴장시킨다.
- **연속 발동(스왑 입장·어시스트 중복)** 시 이전 스트립을 즉시 퇴장시키고 새 스트립으로 교체한다. 쌓이지 않는다.
- 같은 궁극기를 반복해도 짧은 연출이라 피로가 적다. 별도 스킵 옵션은 두지 않는다(전투를 멈추지 않으므로 불필요).
- 해상도·패널 폭은 비율(화면 폭의 약 20%)로 두고 16:9~21:9에서 확인한다.

### 3.5 적용 범위 — 결정 반영

사용자 결정: **"궁극기" 스킬에만 적용한다.** 일반 스킬·강스킬에는 스트립을 쓰지 않는다. 이전 문서의 "강스킬 짧은 컷인" 항목은 폐기한다.

## 4. 무기별 연출 시퀀스 설계

### 4.1 공통 비트 구조 (`PresentationBeat`)

모든 무기의 궁극기를 같은 5비트 뼈대로 표현하고, 무기별로 **각 비트의 카메라·템포·VFX 성격**만 바꾼다. 이 뼈대는 기존 `UltimateTimelineEvent` 목록을 생성·검증하는 틀이다.

| 비트 | 역할 | 대표 이벤트 |
|---|---|---|
| ① 예고 | 발동 신호 + 좌측 얼굴 패널 | FaceStrip, (선택) 짧은 TimeScale 프리즈, 보이스 |
| ② 진입 | 카메라가 무기 성격에 맞게 이동 | Camera 스냅샷/효과, (T2) StageEnter |
| ③ 전개 | 무기 고유의 반복·연타·충전 | MotionSet 클립, VFX, 데미지 윈도우 다단 |
| ④ 피니시 | 가장 큰 한 방, 히트스톱 | 타임스케일, 흔들림, 데미지 윈도우 |
| ⑤ 복귀 | 여운 후 조작권 반환 | StageExit, Letterbox 해제 |

### 4.2 무기별 프리셋

시간은 첫 제안값이며 모션 실측 후 보정한다. Tier는 [연출 스테이지 T0~T3](../../content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.md#3-tier-구조) 기준.

| 무기 | ② 카메라 | ③ 전개 | ④ 피니시 | Tier | 신규 클립 필요성 |
|---|---|---|---|---|---|
| Sword | 측면 트래킹 후 정면 | 다단 베기 4~5연, 베기선 VFX | 십자 베기 + 짧은 프리즈 | T1 | 낮음 (기존 콤보 재조합) |
| SwordShield | 낮은 앵글에서 방패 클로즈업 | 방패 가격 → 반동 → 베기 | 방패 충격파 | T1 | 낮음 |
| GreatSword | 로우앵글 돌진, 느린 줌 | 차징(타임스케일 낮춤) | 내려찍기 + 강한 흔들림·지면 파편 | T2 | 중간 (차지→슬램) |
| Katana | 정적 와이드 → 순간 컷 | 거합 대기 → 무음 → 다중 절단 잔상 | 납도 후 지연 절단선 폭발 | T2 | 중간 (납도 후 정지 포즈) |
| DualBlade | 빠른 오빗 | 고속 난무, 짧은 컷 전환 | 교차 마무리 | T2 | 중간 |
| DoubleAxe | 하이앵글 → 하향 | 회전 베기 | 양손 내려찍기 | T2 | 중간 |
| Spear | 후방 추적 후 측면 전환 | 돌진 관통 + 선형 VFX | 관통 후 회수 | T1~T2 | 중간 |
| Bow | 어깨 너머 → 화살 추적 카메라 | 조준 줌인 | 화살 궤적 따라 타깃 도달 | T2 | 높음 (조준 포즈·추적 카메라) |
| Staff | 상승 크레인 + 마법진 | 영창 + 마법진 전개 | 광역 폭발 | T2 | 중간 (영창·시전) |
| Whip | 휘감는 오빗, 궤적 추적 | 궤적 곡선 VFX, 속박 | 당기기/휘감아 던지기 | T2~T3 | 높음 (접촉·T3 가능성) |

원칙:
- **접촉 연출(잡기·베어 가르기)이 있으면 T3**, 아니면 T2까지(연출 스테이지 §3 경계 그대로). Whip의 속박/던지기는 T3 후보라 제작비가 가장 크다. **초기에는 비접촉 변형으로 시작**하는 것을 권장.
- 판정 타이밍은 **MotionSet(타이밍)·`AbilitySetSO`(수치)** 가 소유한다. 연출 이벤트는 표현만 하고, 데미지 윈도우는 기존 이벤트를 그대로 쓴다. 연출 때문에 수치를 바꾸지 않는다.
- 이름·스테이지로 분기하지 않는다. 무기 타입 키의 **데이터 템플릿**으로만 분기한다.

### 4.3 템플릿 데이터

```
WeaponPresentationProfileSO   // UPlayGround/전투/Weapon Presentation Profile
  WeaponType weaponType
  List<WeaponPresentationBeat> beats    // normalizedTime은 모션 비율, 이벤트 startTime은 비트 이후 초
  CinematicStageTier defaultTier
  CameraSnapshotProfile[] cameraPresets
  Color accentColor
  float suggestedFreezeDuration         // 저작 제안만, 타이밍 불변 적용에서는 실행 안 함
UltimateSequenceAsset
  WeaponPresentationProfileSO weaponProfile   // 신규. 비어 있으면 기존 동작 그대로
  string generatedPresentationId              // 에디터가 생성한 표현 이벤트만 교체
```

- 기존 `events`가 정본이다. 템플릿은 **에디터에서 이벤트 목록을 생성하는 시작점**(복사 후 수정)으로 두는 방식이 안전하다. 런타임에서 템플릿과 events를 동적으로 합치면 디버깅이 어려워진다. → **저작 시 생성, 런타임은 events만 실행**(권장).
- 새 SO는 `CreateAssetMenu` 규약(`UPlayGround/전투/…` 2단계)을 따른다.
- 프로필도 이벤트를 직접 담으므로 실제 Actor 모듈이다. 기존 이벤트·TimeScale·판정·Callback·Stage·배치·잠금·MotionSet·Ability·시계는 생성기가 보존한다. 얼굴/VFX/사운드/흔들림만 복제한다. 일반 복사/붙여넣기는 생성 ID를 지우며, 선택 이벤트를 '재생성에서 보존'할 수 있다.
- Sword/GreatSword는 생성기 수직 슬라이스 대상이다. 실제 정의는 Raon=Katana, Hwarin=DoubleAxe이므로 다른 무기를 강제로 연결하지 않는다. 카메라 후보 목록은 자동 합성하지 않고 P4에서 기존 소유권을 확인한 뒤 명시적으로 연결한다.
- 생성 길이는 실제 Ability MotionKey의 매핑으로 해석한 실행 모션을 사용한다. 에디터 생성 버튼은 그 모션을 명시적으로 선택해야 한다. Raon은 미리보기2.08333349초와 실제 궁극기5.01000071초가 달라 신규 카메라·표현 이벤트를 실제 길이로 보정했다. 미리보기/판정/기존 Slow는 보존한다.

## 5. 카메라 설계

- 카메라는 `CameraSnapshotProfile`과 `UltimateCameraEffectEvent`를 우선 쓴다. **새 카메라 프레임워크를 만들지 않는다.**
- 카메라 모듈은 내부에서 `Svc`/`IWorldActor`를 못 쓰므로, 새 요구(예: 화살 추적, 오빗)는 기존 계약·어댑터로 표현 가능한지 먼저 본다([카메라 이식 경계](../../../guide/CAMERA_MODULE_PORTABILITY_GUIDE.md)).
- 연출 카메라가 지형을 뚫는 문제는 T2 무대가 해결한다(연출 스테이지 §1). T1은 충돌 가드가 필요하다.
- 가림 규칙(`CameraOccluderNameRules`)은 건드리지 않는다.

## 6. 사운드·피드백

- 얼굴 패널 등장 SFX와 보이스는 기존 `UltimateSoundEvent`(bus Voice/SFX).
- 히트스톱·타임스케일은 기존 `GameTime.Request/Release`로 일원화한다. 신규 프리즈는 실시간 판정 시점도 바꾸므로 이번 타이밍 보존 적용에서는 프로필 제안값으로만 남기고 기존 TimeScale을 유지한다.
- 피니시마다 **속성 색 플래시 + 흔들림 강도**를 무기 프로필의 값으로 둔다. 무기 간 "무게감 순서"(Katana·Spear 가벼움 < Sword < GreatSword·DoubleAxe 무거움)를 흔들림·프리즈 길이로 표현한다.

## 7. Blender MCP 제작 범위

**원칙: 신규 클립 제작은 기존 클립으로 불가능한 피니셔에 한정한다.** 무기 콤보 클립은 이미 대량 보유(조사 11,445개 등록 기록)하므로 재조합이 먼저다.

| 작업 | 판단 |
|---|---|
| Sword·SwordShield 궁극기 | 기존 클립 재조합 먼저. 신규 불필요 가능성 높음 |
| GreatSword 슬램, Katana 납도 정지, Staff 영창, Bow 조준 | 신규 클립 후보 |
| Whip 접촉 피니시 | T3 결정 후 |
| 카메라 | **Blender에서 카메라 애니메이션을 만들어 Unity에 직행시키지 않는다.** 과거 대화 카메라에서 AnimationClip 직행을 배제한 전례가 있다(메모리: 대화 카메라 사전 녹화). Blender는 앵글·속도 **프리비즈**에만 쓰고, 확정값을 `CameraSnapshotProfile` 키로 옮긴다 |

- 리타게팅·원본 리그 분리·클립별 보정은 [무기 모션 리타게팅 가이드](../../../guide/WEAPON_MOTION_RETARGETING_GUIDE.md)를 따른다. Constraint 오차만으로 정렬 완료 판정 금지.
- Blender 확인과 Unity 적용 확인을 구분해 보고하고, **미술 품질은 사용자 확인**이다(공통 하네스 아트 인수 조건).
- 신규 클립은 담당이 맞는 `artist` 서브 Agent로 넘기는 것이 적합하지만, 위임 시 [ROLES.md](../../../../../Tools/Harness/ROLES.md) 브리프를 채워야 한다.

## 8. 구현 Phase

| Phase | 내용 | 완료 기준 |
|---|---|---|
| P0 | ImageGen 얼굴 이미지 2종(Raon·Hwarin) 제작·Sprite 임포트 | 잠정 채택 후 진행, 스타일 사용자 확인 대기 |
| P1 | `UltimateFaceStripEvent` + `UISvc.FaceStrip` + 전용 캔버스 + `UIFaceStrip` 프리팹. Raon 궁극기에 연결 | 컴파일 오류 0, Play에서 타임스케일·hideHud 영향 없음, 게임패드 포커스 유지 |
| P2 | `WeaponPresentationProfileSO` + 에디터 생성기. Sword·GreatSword 2종으로 수직 슬라이스 | 템플릿 → events 생성 → 재생, 판정 타이밍 불변 |
| P3 | 나머지 8무기 템플릿, 기존 Raon/Hwarin 궁극기에 맞는 무기만 연결 | 중복 발동은 즉시 교체. 큐·스킵·추가 캐릭터 궁극기 생성 안 함 |
| P4 | 기존 클립 조사 후 불가능한 것만 Blender 신규 제작, CameraSnapshotProfile 저작 | 3D 페이스캠 제외. Whip 접촉/T3는 사용자 결정 대기 |

각 Phase 종료 때 [전투 위키](../../../../../docs/wiki/combat.md)와 [상태 문서](../../../../../docs/work-status/weapon-ultimate-presentation/STATUS.md)를 갱신한다.

## 9. 검증 계획

- 자동: 컴파일 오류 0, `[SerializeReference]` 이벤트 managed reference 누락 0, 프로필 SO 검증(무기 타입 중복·비트 누락), EditMode로 템플릿 생성 결과 이벤트 수·시간 순서.
- 플레이: 타임스케일 0.2 구간에서 얼굴 패널 트윈 진행(`SetUpdate(true)`), 연출 중 일시정지·사망·씬 전환 시 패널 잔존 없음, 스왑 직후 중복 발동.
- 사용자 확인 대기 항목: 얼굴 이미지 스타일·패널 미감, 무기별 템포·무게감, 반복 피로. 자동 검사로 완료 보고하지 않는다.
- Player/Addressables 빌드는 사용자 지시 전에는 하지 않는다.

## 10. 열린 질문

1. 전용 ImageGen 얼굴 2종의 최종 스타일·외형 승인. 대화 포트레이트 공유 여부는 D1에서 별도 제작으로 확정했다.
2. 궁극기 중 상대·아군 파티원 반응(적 일시정지 범위)은 기존 `UltimateGameplayLockSettings`로 충분한가.
3. 궁극기 연출 중에도 패널 뒤로 전투 화면이 계속 보이므로, 패널이 데미지 숫자·적 HP를 가리지 않는 위치인지 실제 화면에서 확인.
4. 12캐릭터 × 무기 외형 차이로 같은 무기에서도 캐릭터 개성을 어디까지 덧씌우나(템플릿 override 비율).
5. 스칼렛 스트링스의 정확한 제목/참고 장면 (§1.2). 레퍼런스 이미지는 엔드필드로 보이며 패널 형태는 이미지 1장 기준.
