# 무기별 궁극기 연출 진행 상태

설계 정본: [궁극기 연출 설계](../../../Assets/docs/design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md) · [전투 위키](../../wiki/combat.md)

## 현재 판정

얼굴 아트 후속(2026-11-14): 사용자의 날카로운 인상 지적에 따라 Raon/Hwarin 컷인을 원본 모델 기반 초상화의 둥근 눈·부드러운 얼굴과 채색으로 재제작해 기존 경로에 교체했다. `.meta`/GUID 보존, Unity 재임포트·Sprite/기존 연결2/2 확인. [현재 적용본·프롬프트·이전 이미지·SR01~02](IMAGE_PROMPTS.md). 실제 패널과 최종 미술 품질은 사용자 테스트 대기다.

2026-11-14 코드·기존 궁극기 연결·10종 프로필을 반영했다. **사용자 카메라 구도 회귀를 받아 Raon/Hwarin의 자동 생성 Snapshot 연결을 해제하고 기존 카메라 경로로 복구했다.** 후보10종은 미적용으로 보관한다. [회귀 복구·검사6/6·남은 화면 확인](CAMERA_FRAMING_FIX.md). 실제 전투와 미술 품질 인수는 사용자 테스트 대기다. 커밋·스테이징·Player/Addressables 빌드는 하지 않았다.

| 단계 | 산출/자동 검사 | 미수행·사용자 확인 |
|---|---|---|
| P0 | Raon/Hwarin 얼굴 PNG 2종, 1536×1024 RGBA, Sprite 로드2/2·알파 유지·mipmap 꺼짐 | 잠정 채택, 외형·스타일 승인 대기. 나머지 얼굴 미생성 |
| P1 | Contracts 표시 계약·Actor 이벤트·전용 UI/프리팹·DB·기본 얼굴 연결. [EditMode11/11](evidence/p1-editmode.json), [PlayMode2/2](evidence/p1-playmode.json) | 실제 전투/T2·pause·사망·씬 전환·패드·21:9 검수 |
| P2 | Sword/GreatSword 실물 프로필·5비트·생성기. [초기 EditMode7/7](evidence/p2-editmode.json) | 실제 궁극기 에셋이 없어 해당 무기 실제 궁극기 재생 미수행 |
| P3 | 나머지8종 포함 프로필10종·기존 Raon=Katana/Hwarin=DoubleAxe만 적용. [재검사11/11](evidence/p3-editmode.json) | 없는 궁극기11개 임의 생성 안 함. 무기별 체감 대기 |
| P4 | CameraSnapshotProfile10종·5샷씩 후보 보관, 실제 연결 해제. 기존 클립5후보 조사. 최초 [구조 검사18/18](evidence/final-editmode.json), 회귀 복구 후 [관련 검사6/6](evidence/camera-framing-editmode.json) | 실제 구도 재확인·클립 적합성 검수 대기. Blender 신규 필요 미확정, Whip 접촉/T3 보류 |

단계 검사에는 중복 실행이 포함되므로 건수를 합산하지 않는다. 최초 구현18건은 생성기8·카탈로그1·실제 연결3·카메라6이었다. 구도 회귀 복구에서는 변경된 카메라 관련6건을 다시 실행해 실패0·건너뜀0을 확인했다. 이전18건을 실제 구도 합격의 근거로 쓰지 않는다. P1의 변경 영향이 없는 기존 결과11+2건은 유지한다.

## 변경 파일과 산출물

전체 경로는 [파일 인수 목록](FILES.txt)에 있다.

- P0: [이미지·공통/개별 프롬프트·출처·알파 점검](IMAGE_PROMPTS.md). ImageGen 기본 내장 도구로 생성·보정했다. 최종 PNG와 meta를 보존하며 PNG는 Git ignore 대상이다.
- P1: Contracts/FaceStripServices.cs·Services.cs, PlayerCharacterDefinitionSO, UltimateFaceStripEvent·UltimateRuntimeContext·UltimateSequencePlayer, UIFaceStrip·UIManager·UIServices, FaceStripPrefabBuilder, 실제 UIFaceStrip 프리팹·UI DB·캐릭터 정의2종, 관련 검사.
- P2/P3: WeaponPresentationProfileSO, 생성기·기본 SO 저작기, Ultimate 이벤트 생성 ID·Sequence 프로필 참조, 기존 궁극기 에디터의 생성/보존 조작, 프로필10종·흔들림 subasset·Sequence2종. [무기별 값·필요 궁극기 목록](PROFILE_CATALOG.md).
- P4: WeaponPresentationCameraBuilder, CameraSnapshots10종, Sequence 카메라 연결2종, 카메라 검사. [모션 재사용 조사](MOTION_REUSE_REVIEW.md).

## Astra 검증과 확정 계약

- namespace는 Data여도 기존 Ultimate 이벤트/Sequence와 직접 이벤트 템플릿을 담는 Profile은 실제 Actor 모듈이다. 공용 Contracts에 IFaceStripPresenter/FaceStripRequest를 두고 Actor는 Svc.FaceStrip, UI는 UISvc.FaceStrip 별칭을 쓴다. asmdef 역참조를 추가하지 않았다.
- context가 실제 presenter와 최신 소유 토큰을 보관한다. 이벤트 SO에 실행 토큰을 저장하지 않는다. 이전 Complete는 새 패널을 지우지 않으며 Restore가 lease를 해제한다. UIManager의 IUpdatableManager 계약으로 실제 pause를 확인하고 패널만 닫는다.
- UI는 unscaled, 발동은 기존 MotionSet 시계다. 신규 freeze는 실시간 판정 시점도 바꾸므로 제안값만 저장하고 실행하지 않는다.
- 생성 ID가 있는 자신의 표현 이벤트만 교체하고 기존 판정/콜백/TimeScale/카메라/Stage/배치/잠금/Ability/MotionKey를 보존한다. 일반 복사에서는 생성 ID를 지우며 선택 이벤트를 재생성에서 보존할 수 있다.
- 생성 성공 후 Undo 그룹을 닫는다. 설치된 Test Framework의 SaveUndoIndexTask가 현재 그룹을 저장하고 PerformUndoTask가 그 그룹까지 복구하므로, 그룹을 열어 둔 프로그램 저작이 다음 검사에서 되돌아갈 수 있었다. 회귀 검사로 보완했다.
- 카메라는 기존 Snapshot 체계를 재사용한다. 기본 Tier는 추천값이며 기존 Stage 설정은 Raon=false/Hwarin=true 그대로다. 3D 페이스캠과 T3는 구현하지 않았다.

## 실제 실행 모션 정본 교차 검증

Astra가 AbilitySet → 궁극기 Payload.MotionKey → 실제 플레이어 모델의 무기 MotionSet 매핑을 추적했고 리드가 Unity 실제 TotalDuration으로 확인했다.

| 대상 | 실제 모션 | 실제 길이 | 미리보기 길이 | 보관한 카메라 후보 길이(미적용) |
|---|---|---|---|---|
| Raon | Katana/FrankWeapon/Katana_Skill_Ultimate_RaonWeapon, GUID e058822f7534df34ca58fca059ce759c | 5.01000071s | 2.08333349s | 5.01000071s |
| Hwarin | DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Skill_2, GUID 69e9e4d16dc3b384fa868a8ab6f2943a | 1.83333337s | 동일 | 동일 |

[Raon 보정 근거](evidence/raon-actual-motion-correction.json)와 [최초 연결/컴파일 상태](evidence/p4-camera-bindings.json). Raon의 카메라 후보 키 시간과 생성 흔들림만 실제 길이 기준으로 정정했다(흔들림4.008001s). 미리보기 연결과 기존 Slow는 보존했다. Hwarin 얼굴 hold는1.2초 초안이다. 현재 자동 카메라 연결 해제와 나머지 데이터 보존은 [복구 후 상태](evidence/camera-framing-bindings.json)를 따른다.

에디터 생성 버튼은 실제 MotionKey 매핑으로 해석한 실행 모션을 명시적으로 선택해야 한다. 미리보기 길이를 자동 사용하지 않는다. 이 입력은 저작 때만 사용하며 런타임 정본을 추가하지 않는다. 기존 에디터 타임라인 폭/길이 경고는 미리보기 기준이라 Raon의 생성 이벤트에 초과 경고가 남을 수 있다. 런타임 이벤트를 잘라내지는 않는다.

## 보존·실패 이력과 검증 범위

- [시작 변경 목록](BASELINE.txt), [Sequence 원본](SEQUENCE_BASELINE.json), [Ability/MotionSet 원본 해시](COMBAT_HASH_BASELINE.json)를 보존했다.
- [기존 이벤트 비교](evidence/existing-event-comparison.json): Raon TimeScale, Hwarin Callback/CameraEffect의 기존 값3/3 동일. 판정/모션/수치를 이번 작업에서 변경하지 않았다.
- [해시 비교](evidence/combat-hash-comparison.json):2290개 중2286개 동일. 달라진4개는 같은 작업 시간대의 외부 궁수/P09Staff 변경이며 이 작업이 수정하지 않았다. 해당 사용자 변경을 원복하지 않았다. Raon/Hwarin 실행 모션·Ability 연결의 원본은 보존했다.
- [P1 최초 검사](evidence/p1-initial-editmode.json)는10통과/1실패. 일반 MonoBehaviour 비활성 콜백의 검증을 EditMode에서 실제 PlayMode로 옮겼고 최종11/11+2/2 통과했다.
- [P3 최초 검사](evidence/p3-initial-editmode.json)는2통과/2실패. DB/얼굴 저장과 Undo 그룹 문제를 복구한 뒤 실제 디스크 연결·검사 후 메모리 유지까지 확인했다.
- [Play 초기화 차단](evidence/p1-playmode-initialization-block.json)은 외부 ProjectileAttackDataTests 구문 오류로0건 실행이었다. 원 작성자가 복구했으며 리드는 그 파일을 변경하지 않았다. 기존 StaffMagicProjectilePlayModeTests 상대 namespace 오류는 Astra 검증 후 global:: 한정자 한 줄만 추가해 컴파일을 복구했다.
- 최초18건 후에는 얼굴/프로필/카메라 연결·저장 상태를 확인했다. 이후 사용자 화면에서 구도 실패가 확인돼 자동 카메라 연결은 해제했다. 이번 회귀 검사6건 후 얼굴/프로필/기존 이벤트 유지, cameraProfile=null, dirty=false, managed reference 누락0, 컴파일 실패 없음 확인. 요청 시작 시 CombatTestMap 씬·sceneDirty=false·Play 종료 상태로 복원했다.
- [Play 테스트 씬 합성 캡처](../../../output/weapon-ultimate-presentation/face-strip-playmode.png)는 리드와 독립 QA가 열어 실제 렌더를 확인했다. 실제 전투/T2 화면 품질의 인수 근거로 승격하지 않았다.
- 독립 QA가 계약·diff·디스크 GUID·검사 원본을 검토했다. 이전 연결 누락/카메라 입력 검증 누락은 해소됐고 최종 실제 모션 차이도 보정했다.

## 사용자 테스트와 남은 범위

[실행 명세와 회신 형식](USER_TESTS.md)을 따른다. 얼굴 스타일/캐릭터 일치, 패널 미감·반복 피로, 무기별 템포·무게감, 카메라 앵글/복귀, 실제 전투에서 pause·사망·씬 전환·T2·패드·16:9~21:9는 사용자 테스트 대기다.

[기존 클립5후보](MOTION_REUSE_REVIEW.md)는 연결·길이·커브를 조사했지만 실제 대상 모델·무기/칼집/활시위/채찍 본 조합의 시각 검수는 미수행이다. 재조합 불가가 확정되지 않아 Blender 신규 클립은 만들지 않았다. Whip 접촉 속박/던지기(T3)는 사용자 채택 결정 전 미구현이다. Bow 카메라는 어깨 너머 조준 초안이며 실제 투사체 추적은 미구현이다. 진입·전개·복귀의 미확정 VFX/모션은 빈 비트로 보류했다. 패널의 짧은 실시간 플래시는 패널 유지 끝 기준이며 긴 궁극기의 실제 피니시와 강제 동기화하지 않는다.

실제 캐릭터 정의는13개, Sequence는2개뿐이다. 없는11개 궁극기/얼굴은 목록만 남겼다. 자동 검사를 미술·플레이 인수나 전체 콘텐츠 완성으로 보고하지 않는다.

## 부산물 정리·인수

신규 임시 프로젝트/빌드/캐시 설치는 없고 삭제할 제작 부산물도 없다. Test Runner가 만든 이번 임시 씬은 종료 시 정리됐으며, 기존 날짜의 다른 InitTestScene은 보존했다. PNG2종·meta·SO·프리팹·제작 원본·검사 JSON·합성 캡처·미커밋 원본 비교 자료를 보존한다. ImageGen 제작 원본은 IMAGE_PROMPTS.md에 기록한 generated_images 위치에 있다. 최초 준비표는 output/harness/20261114-150355-6a29c523이며 최종 준비표/링크 검사는 파일 인수 목록과 최종 검증 기록을 따른다.

[최종 소스 준비표/단계 인수](../../../output/harness/20261114-160148-b7ea5252/review.md)의 지정 입력 해시 동일 확인과 git diff --check를 통과했다. 해시 동일은 실행 검증을 대신하지 않으며 실행 근거는 위 검사 JSON이다. 변경 문서의 로컬 링크도 파일 존재와 대조한다. PNG2종 합계3,631,492B, 합성 캡처250,925B를 보존했다.
