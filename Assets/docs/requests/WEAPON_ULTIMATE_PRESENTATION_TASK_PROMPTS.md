# 무기별 궁극기 연출 + 사이드 얼굴 패널 — 작업 요청 프롬프트

새 세션에 아래 코드 블록을 그대로 붙여 넣는다. P0부터 P4까지 한 번에 진행하도록 쓴 단일 프롬프트다. 이 문서는 현재 지시가 아니다. 쓰기 전에 설계와 최신 코드·에셋을 대조한다.

기준 설계: [무기별 궁극기 연출 + 사이드 얼굴 패널 설계](../design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md)

## 시작 전에 사용자가 채울 것

| 항목 | 기본값 | 바꾸려면 |
|---|---|---|
| 스칼렛 스트링스 참고 자료 | 없음. 사용자가 준 엔드필드 캡처 1장 기준 | 프롬프트 맨 아래 `[참고 자료]`에 제목·영상 링크를 적는다 |
| Blender 신규 클립 범위 | 피니셔 후보 5종(GreatSword 슬램, Katana 납도, Staff 영창, Bow 조준, Whip 접촉) 중 **기존 클립 재조합으로 안 되는 것만** | 줄을 고친다 |
| ImageGen 시안 확정 | AI가 Raon·Hwarin 2종을 만들고 **잠정 승인 상태**로 다음 단계 진행. 최종 스타일은 사용자가 확인 | 시안 확정 전 중단하려면 "P0에서 멈춘다"를 추가 |

## 프롬프트

```
UPlayground 저장소(C:/UsingProject/UnityProject/UPlayground)에서 아래 작업을 P0부터 P4까지 순서대로 끝까지 진행해라. 제안·계획만 반환하지 말고 각 단계의 조사·구현·데이터 연결·검사·인계 기록까지 수행한다.

## 목표
궁극기 스킬 발동 시 화면 왼쪽에 시전자 얼굴 패널(사이드 페이스 스트립)을 띄우고, 무기 10종별 궁극기 연출 템플릿을 제공한다. 패널 형태는 사용자 레퍼런스 기준이다: 화면 좌측 가장자리의 가로 반투명 패널, 얼굴이 패널 밖으로 살짝 넘침, 전투 화면은 계속 보임, 전체화면 컷인 아님. 적용은 궁극기에만 한다.

## 먼저 읽을 것
- AGENTS.md, docs/wiki/README.md, docs/wiki/combat.md
- 기준 설계: Assets/docs/design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md (이 설계가 정본이다. 단 실제 코드·에셋과 대조하고, 다른 결정은 이유·영향·검증 방법을 인계에 남긴다)
- Assets/docs/design/content/presentation/CINEMATIC_STAGE_SYSTEM_DESIGN.md
- Assets/docs/guide/CODE_AUTHORING_GUIDE.md, UI_UX_AUTHORING_GUIDE.md, COMBAT_SYSTEM_AUTHORING_GUIDE.md, TEST_VALIDATION_GUIDE.md, CAMERA_MODULE_PORTABILITY_GUIDE.md, WEAPON_MOTION_RETARGETING_GUIDE.md
- .claude/rules/architecture.md, code-conventions.md
- Tools/Harness/README.md, Tools/Harness/ROLES.md
- 코드: GameActor/Combat/Ultimate/*, GameActor/Component/Player/UltimateSequencePlayer.cs, Data/Combat/Cinematic/CinematicStageSO.cs, Data/Enum/WeaponType.cs, UI/Contracts/UIServices.cs

## 이미 정해진 결정 (사용자 확정)
1. 얼굴 이미지는 Codex CLI ImageGen으로 궁극기 전용 컷 이미지를 새로 만든다. 대화 포트레이트(파츠 합성형)는 재사용하지 않는다.
2. 적용 범위는 "궁극기" 스킬뿐이다. 일반 스킬·강스킬에는 쓰지 않는다.
3. 형태는 사이드 페이스 스트립이다(전체화면 컷인 아님).
4. 3D 페이스캠(RenderTexture)은 만들지 않는다(연출 스테이지 §7.12 유지).

## 공통 규칙
- 작업 전 미커밋 변경을 기록하고 보존한다. 요청 없이 커밋·스테이징하지 않는다. 구매 원본·Git 추적 밖 아트·.meta를 보존한다.
- 판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다. 연출 이벤트는 표현만 하고 판정 타이밍·수치를 바꾸지 않는다. 캐릭터·스테이지 이름으로 분기하지 않는다. 무기 타입 키의 데이터 템플릿으로만 분기한다.
- 기존 UltimateTimelineEvent 체계(`[SerializeReference]`)와 CinematicStage T0~T3, CameraSnapshotProfile, 레터박스, GameTime.Request/Release를 재사용한다. 새 카메라 프레임워크를 만들지 않는다.
- 새 Data 이벤트는 UI 구현을 참조하지 않는다. UISvc 계약을 경유한다. 하위 모듈에 매니저 싱글톤 참조를 새로 추가하지 않는다. 이벤트 클래스를 다른 어셈블리로 옮기면 [MovedFrom]을 유지한다.
- UI 트윈은 모두 SetUpdate(true). 패널은 표시 전용이며 게임패드 포커스·입력을 가져가지 않는다. hideHud=true여도 패널이 보이도록 HUD와 분리된 캔버스에 둔다.
- 핫패스에서 GetComponent·Find*·LINQ·매 프레임 할당·문자열 결합 금지. 새 SO는 CreateAssetMenu `UPlayGround/전투/…` 2단계 규약. 사람이 쓰는 에디터 도구만 [UPlaygroundTool("ID")]로 툴 런처에 올리고 일회성 생성기는 올리지 않는다.
- 코드 주석·문서·커밋 메시지는 한국어. 플레이어 노출 텍스트에 개발·시스템 용어를 쓰지 않는다.
- Unity Editor는 하나다. MCP·Test Runner로 다루고 두 번째 Editor를 열거나 Library·Temp·잠금 파일을 지우지 않는다. C# 수정은 Hot Reload 운영 가이드를 따른다. 별도 요청 없이 Player·Addressables 빌드를 하지 않는다.
- 검증 중 Assets/10.Datas/·Assets/03.Prefabs/가 자동 재직렬화되면 diff를 검사해 자동 변경분만 원복한다.
- 미술 품질·플레이 체감은 AI가 대신 판정하지 않는다. 자동 검사 결과와 `사용자 테스트 대기` 항목을 분리해 보고한다. 미검증을 완료로 보고하지 않는다.
- 단계 종료마다: 부산물 정리 → docs/work-status/weapon-ultimate-presentation/STATUS.md(없으면 생성)와 docs/wiki/combat.md 갱신 → 로컬 링크 확인 → 다음 단계. 설계 문서의 상태 표기도 같이 갱신한다.

## 단계

### P0 — 얼굴 이미지 스타일 확정
1. 궁극기 에셋이 있는 Raon·Hwarin의 캐릭터 설정(PlayerCharacterDefinitionSO, 기존 포트레이트·모델 캡처)을 조사해 외형 레퍼런스를 모은다.
2. 설계 §3.2 규격(알파 PNG, 1024×512 이상, 얼굴이 패널 높이의 120~140%, 가슴 위~머리, 강한 눈빛 표정 1종, 패널 무늬는 이미지에 굽지 않음)으로 공통 스타일 프롬프트와 캐릭터별 외형 프롬프트를 작성한다. 프롬프트를 docs/work-status/weapon-ultimate-presentation/에 남긴다.
3. Codex CLI ImageGen으로 Raon·Hwarin 각 1종을 생성해 Assets/04.Images/SkillCutIn/CutIn_<캐릭터>.png로 저장하고 Unity에 Sprite(알파 유지, mipmap 끔)로 임포트한다. 기존 에셋과 .meta를 덮어쓰지 않는다.
4. 두 이미지의 일관성(조명·선 굵기·채색)과 캐릭터 정본 일치를 가능한 범위에서 점검하되 미술 품질 판정은 하지 않는다. 결과를 `잠정 승인, 사용자 스타일 확인 대기`로 기록하고 다음 단계로 진행한다. 나머지 10캐릭터는 스타일 확정 전이므로 만들지 않는다(P3에서 스타일이 승인된 경우에만 확장 — 미승인이면 프롬프트 세트와 생성 절차만 남긴다).

### P1 — 얼굴 패널 이벤트와 UI
1. UltimateFaceStripEvent(Data 모듈): faceOverride, inDuration/hold/outDuration, accentOverride. 비면 PlayerCharacterDefinitionSO의 신규 필드 ultimateFaceSprite를 쓴다. 필드 추가 시 커스텀 인스펙터에도 반영한다.
2. UISvc.FaceStrip(IFaceStripPresenter) 계약, 전용 캔버스, UIFaceStrip 프리팹(패널 + 얼굴 Image + 스킬명/이름 선택). 패널 폭은 화면 폭의 약 20%, 위치는 좌측 하단 영역(화면 높이 약 45~65%)으로 데미지 숫자·적 HP를 가리지 않는지 확인한다.
3. 연출 값 초안: 등장 0.18s(얼굴이 약간 늦게 따라 들어오는 시차), 유지 1.2~1.6s(1~2% 숨쉬기, 피니시 직전 속성색 플래시), 퇴장 0.2s. 값은 SO/이벤트 필드로 뺀다.
4. 연속 발동(스왑 입장·어시스트 중복) 시 이전 패널을 즉시 퇴장시키고 새 패널로 교체한다. 쌓이지 않는다.
5. Raon 궁극기(UltimateSequence_Raon)에 이벤트를 연결한다. Ultimate 에디터 저작 항목에 새 이벤트를 노출한다.
6. 검증: Unity 컴파일 오류 0, managed reference 누락 0, EditMode 테스트(이벤트 직렬화, 교체 규칙), Play Mode에서 타임스케일 0.2 구간 트윈 진행, hideHud 영향 없음, 연출 스테이지 T2 전용 카메라와 UI 오버레이 독립, 일시정지·사망·씬 전환 시 패널 잔존 없음, 16:9~21:9 레이아웃. 화면 캡처로 확인하되 미감은 사용자 확인 대기로 남긴다.

### P2 — 무기 연출 템플릿 수직 슬라이스 (Sword, GreatSword)
1. WeaponPresentationProfileSO(weaponType, 기본 Tier, 카메라 프리셋, 강조색, 비트별 이벤트 템플릿)를 만든다. 설계 §4.1의 5비트(예고·진입·전개·피니시·복귀) 뼈대를 쓴다.
2. 에디터 생성기: 템플릿 → UltimateSequenceAsset.events 생성(복사 후 수정). 런타임은 events만 실행하고 템플릿을 동적으로 합치지 않는다. UltimateSequenceAsset에 weaponProfile 참조는 두되 비어 있으면 기존 동작이 그대로다.
3. Sword·GreatSword 프로필을 설계 §4.2 표대로 만들고 Raon(또는 해당 무기 캐릭터) 궁극기에 적용한다. 판정 타이밍(MotionSet·AbilitySet)은 변경하지 않는다. 변경 전후 판정 이벤트 시간이 같은지 diff로 확인한다.
4. 검증: 프로필 SO 검증(무기 타입 중복·비트 누락), 템플릿 → 이벤트 생성 결과 EditMode 테스트, Play에서 재생. 연출 체감은 사용자 테스트 대기.

### P3 — 나머지 무기 템플릿
1. SwordShield, Katana, DualBlade, DoubleAxe, Spear, Bow, Staff, Whip 프로필을 설계 §4.2 표대로 작성한다. 모션이 미확정인 공격은 보류로 표시하고 가짜 연결을 만들지 않는다.
2. 무기별 무게감 순서(Katana·Spear < Sword < GreatSword·DoubleAxe)를 흔들림·프리즈 길이 값으로 표현한다. 무게감 순서와 연출 길이가 프로필 간 모순되지 않는지 표로 정리한다.
3. 실제 궁극기 에셋이 있는 캐릭터(현재 Raon·Hwarin)에만 적용한다. 없는 캐릭터의 에셋을 임의로 만들지 않는다. 필요한 에셋 목록은 STATUS에 남긴다.
4. 검증: 프로필 10종 일괄 검증, 템플릿 생성 일괄 테스트. 체감은 사용자 테스트 대기.

### P4 — 신규 클립·카메라 (필요한 것만)
1. 무기별로 기존 MotionSet 클립 재조합이 가능한지 먼저 조사한다. 후보: GreatSword 차지→슬램, Katana 납도 정지, Staff 영창, Bow 조준, Whip 접촉 피니시. 재조합 불가가 확인된 것만 신규 제작 대상으로 올린다. 조사 결과 표를 STATUS에 남긴다.
2. 신규 대상이 있으면 ROLES.md 위임 기준에 맞춰 artist 서브 Agent에 위임 브리프(범위·소유 파일·원본 리그/대상 모델/클립별 보정 분리·완료 기준)를 채워 넘긴다. Blender MCP로 클립을 만들고 WEAPON_MOTION_RETARGETING_GUIDE를 따른다. Constraint 오차만으로 정렬 완료를 판정하지 않는다. Blender 확인과 Unity 적용 확인을 구분해 보고한다.
3. 카메라는 Blender에서 카메라 애니메이션을 Unity로 직행시키지 않는다. Blender는 앵글·속도 프리비즈로만 쓰고, 확정한 값을 CameraSnapshotProfile 키로 옮겨 프로필의 cameraPresets에 넣는다. 카메라 모듈 내부에서 Svc·IWorldActor를 직접 쓰지 않는다(어댑터 경유).
4. Whip 같은 접촉 연출은 T3가 필요한지 판단하고, 비용이 크면 비접촉 변형으로 시작한 뒤 결정을 사용자에게 올린다. T3를 임의로 구현하지 않는다.
5. 신규 클립·카메라 적용 후 해당 무기 궁극기에 연결하고 판정 타이밍 불변을 다시 확인한다.

## 중단·보고 조건
- 컴파일 오류·managed reference 누락을 복구하지 못하면 해당 단계에서 멈추고 원인과 시도한 것을 보고한다. 우회로 저장·일괄 재직렬화하지 않는다.
- 설계와 실제 코드가 충돌해 설계 변경이 필요하면 근거와 대안을 인계에 남기고, 영향이 큰 변경만 사용자에게 묻는다. 나머지는 기본값으로 진행한다.
- 마지막 보고에는 단계별로 변경 파일, 자동 검사 대상·개수·통과/실패/미수행, `사용자 테스트 대기` 항목(얼굴 이미지 스타일, 패널 미감, 무기별 템포·무게감, 신규 클립 품질), 갱신한 위키·상태 문서 링크를 넣는다.

[참고 자료]
(여기에 스칼렛 스트링스 제목·영상 링크, 엔드필드 궁극기 영상 링크가 있으면 적는다. 없으면 비워 둔다.)
```

## 요약

| 단계 | 산출 | 사용자 확인이 남는 것 |
|---|---|---|
| P0 | Raon·Hwarin 얼굴 이미지 2종, 프롬프트 세트 | 이미지 스타일·캐릭터 일치 |
| P1 | 패널 이벤트·UI·Raon 연결 | 패널 미감·속도·위치 |
| P2 | 프로필 SO·생성기·Sword/GreatSword | 템포·무게감 |
| P3 | 나머지 8종 프로필 | 무기별 체감 |
| P4 | 필요한 신규 클립·카메라 프리셋 | 클립 품질, T3 여부 |
