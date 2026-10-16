# 화린 대화 연출 구현·사용자 검수 명세

## 현재 판정

### 옆머리 메시 변형 사용자 검수 통과 (2026-10-16)

- 최신 수정 후 사용자 회신 “통과로 기록”에 따라 대화창 옆 작은 초상화의 Neutral 옆머리 흔들림을 **사용자 시각 검수 통과**로 기록한다. 끝부분만 벌레처럼 움직이던 문제의 수정 판정이며, 아래 강체 회전 실패·비활성화 기록은 이전 이력이다.
- 눈 깜빡임의 기존 사용자 통과 판정은 유지한다. 의상 흔들림 미구현, 다른 표정의 파츠·연결·검수 및 일러스트 리소스 부족은 그대로 남긴다.
- 확대 전경, Keep·pause·로그 복귀·Cancel 후 재진입, 성능과 기타 미검수 항목까지 통과한 것으로 확대 해석하지 않는다. Unity Test Runner 실행과 에이전트의 Play Mode 직접 검증은 미수행이며, 전체 캐릭터 확장 조건은 아직 충족하지 않았다.

### 옆머리 연속 메시 변형으로 재제작 (2026-10-16)

- 끝 조각 강체 회전을 제거하고 옆머리를 관자놀이 높이부터 252px 높이의 연결된 파츠로 다시 추출했다. 새 파츠 `Hair_Side_L_Bend.png`/`Hair_Side_R_Bend.png`와 `Body_Neutral_Bend.png`를 사용한다. 기존 PNG와 GUID는 보존한다.
- 내장 ImageGen으로 옆머리 아래 피부·귀·목깃 보충 그림 `output/HwarinPortrait/Source/Clean_Broad.png`를 생성했다. 이 출력은 실제 알파를 포함하며, 새로운 합성에서는 해당 알파를 보존한다. 과거 불투명 배경에 사용했던 색상 추정 처리는 이 파츠에 사용하지 않는다.
- `UIDialoguePortraitPartImage`는 12×24 격자(파츠당 325정점)로 표시한다. 위 30%와 얼굴에 닿는 안쪽 30%는 고정하고 두 경계에서 매끄럽게 증가하는 가중치로 변위가 이어진다. 얼굴 쪽 틈과 조각 회전을 줄이기 위한 방식이다. 일반 정지 파츠는 기존 Image 사각형 표시로 폴백한다.
- 데이터: 양쪽 `swayDegrees=0`, `bendPixels=(8,0)/(9,0)`, 주기 4.2초, 위상 0/0.15. 좌우를 거의 같은 방향으로 움직인다. 변위는 원본 픽셀 단위이며 기존 표시 배율을 적용한다. 얼굴·뿔·몸통·눈 프레임/타이밍은 유지한다. 의상은 여전히 미구현이다.
- 기존 Animator의 unscaledDeltaTime과 대화 pause·Clear 수명주기를 사용하며 별도 Update나 트윈은 추가하지 않았다. 같은 표정 Keep은 위상을 유지한다. Data의 새 필드는 기본 변위 0으로 미설정 에셋의 기존 동작을 보존한다.
- 확인: 확대 양 끝/중립 합성 보드, 파일·GUID·연결 및 메시 파라미터 정적 검사. `dotnet build --no-restore` UI 오류 0/경고 29, UI.Tests 오류 0/경고 32. 샌드박스 SDK 경로 접근 실패 후 동일 빌드를 권한 상승으로 재실행했다. 새 파일은 임시 MSBuild targets로 포함했으며 생성 csproj는 수정하지 않았다. 로그 `Logs/HwarinPortraitBend-build.log`, `Logs/HwarinPortraitBend-tests-build.log`.
- `DialoguePortraitBendTests`에 양/음 변위의 뿌리 고정·연속성, 얼굴 쪽 고정, 정지 복원 총 4케이스를 추가했다. **컴파일만 확인했으며 Unity Test Runner 실행은 미수행**이다. 작은 초상화의 인게임 옆머리 움직임은 위 사용자 회신으로 통과했다. Unity 임포트 로그 직접 확인·성능 검증은 미수행이다. 아래 ‘비활성화’는 이전 실패 이력이며 현재 새 메시 파츠는 활성 상태다.
- `output/HwarinPortrait/Preview_Bend.html`은 생성된 동일 파츠와 변형식의 브라우저 애니메이션 비교용이다. Unity 캡처가 아니며 Unity 검증을 대체하지 않는다. `Review_Bend.png`는 확대 정지 비교다. 파일과 프롬프트는 같은 output 폴더에 보관했다.

재검수: Play Mode를 종료하고 임포트/컴파일 오류가 없는지 확인한 뒤 Boot → LakeOfLifeScenario → ‘화린 연출 검수’. review_00에서 AUTO를 끄고 20초 관찰한다. 관자놀이 쪽은 고정되고 옆머리 바깥 윤곽이 중간부터 부드럽게 움직여야 한다. 끝 조각만 회전, 피부 틈·이중 머리·배경 조각·뿔 움직임은 실패다. review_12 확대, Keep 연속 진행, pause/로그 복귀, Cancel 후 재진입도 확인한다. 눈 깜빡임 기존 사용자 통과 기록은 유지하되 수정 버전의 회귀 유무를 별도로 회신한다. `H2D03 / 통과·실패 / 증상 / 10초 영상` 형식을 사용한다.

### 머리 끝 독립 회전 품질 실패 — 비활성화 (2026-10-16)

- 사용자 첨부 화면 및 회신: 표시한 볼 양옆의 끝부분만 벌레처럼 움직인다. 수치 확대 후 머리카락 시각 검수 실패로 기록한다. 정지 캡처 자체로 움직임을 관찰한 것은 아니며 움직임 판정은 사용자 회신에 근거한다.
- 원인 판단: 옆머리 전체가 아니라 끝부분의 작은 다각형만 분리한 뒤 강체 회전한 저작 방식이다. 몸통 이미지에 남은 상부 머리는 정지하므로 아래 조각만 독립적으로 움직인다. 각도 확대는 이 단절을 더 드러낸다. 앞선 가시성 진단만으로 수치를 올린 접근을 철회한다.
- 즉시 조치: 좌우 hair 파츠의 swayDegrees를 모두 0으로 변경하고 재연결·보드 스크립트에도 반영했다. 눈 프레임과 깜빡임 설정은 유지한다. 이는 불량 연출 차단이며 머리카락 기능 완성이 아니다.
- 재제작 조건: 관자놀이/상부부터 이어지는 옆머리 덩어리를 확보하고, 얼굴·뿔과 분리된 보충 그림 및 겹침 영역을 준비한다. 뿌리 고정, 중간부터 끝까지 연속적인 메시 변형으로 연결하며 독립된 끝 조각의 회전으로 대체하지 않는다. 실제 대화 표시 크기의 움직이는 시제품에서 먼저 검수하고 게임에 연결한다.
- 최신 상태: 눈 깜빡임 사용자 통과 / 머리 흔들림 실패·비활성 / 의상 미구현. 아래 수치 재조정 기록은 실패 이력이다. 정적 설정 확인만 수행했으며 변경 후 Play Mode는 미수행이다.

### 2D 사용자 회신 및 머리카락 재조정 (2026-10-16)

- 사용자 확인: 대화창 옆 작은 초상화의 눈 깜빡임 통과. 머리카락·의상 움직임은 보이지 않음. 큰 전경의 결과로 확대하지 않는다.
- 판정: 작은 초상화 머리 흔들림은 시각 검수 실패. 의상은 애초 가동 파츠가 없어 미구현이며 통과 대상이 아니다. 눈 깜빡임 통과는 유지하고 접합부·pause·종료 복구 등 별도 항목은 미검수로 남긴다.
- 코드상 눈과 머리는 같은 Update/ApplyParts에서 갱신된다. 기존 0.9/1.2도는 피벗에서 머리 끝까지 약 90px인 파츠에서 원본 기준 약 1.4/1.9px 최대 변위다. 전신을 축소하면 더 작아지므로 낮은 가시성을 원인 후보로 확인했다. 실제 실행 중 Transform 회전을 측정한 것은 아니다.
- 조정: 좌 3.5도/3.2초, 우 4도/3.7초. 눈 프레임·깜빡임 간격·초상화 크기·런타임 코드는 변경하지 않았다. 재생성 스크립트도 같은 설정으로 동기화했다. 양 끝 각도의 확대 합성 보드를 확인했으며 실제 플레이 품질은 재검수 대기다.
- 재검수: Play Mode 종료 후 데이터 임포트를 기다리고 Boot → LakeOfLifeScenario → ‘화린 연출 검수’. review_00에서 AUTO를 끄고 15초 동안 양쪽 볼 옆의 가장 아래 머리 끝을 본다. 머리 전체·뿔·의상은 고정이다. 움직임 식별 가능/접합부 틈 없음/눈 깜빡임 유지 여부를 각각 회신한다. 여전히 보이지 않으면 같은 상태의 10초 영상과 UI의 PortraitPart 세 번째·네 번째 자식 localRotation 변화 여부를 확인한다. review_12 전경은 별도 검수한다.

H2D02의 눈 깜빡임은 사용자 통과, 작은 초상화 머리 흔들림은 수정 후 재검수 대기다. H2D03의 큰 전경과 H2D04는 미검수다.

### Neutral 2D 시제품 적용 (2026-10-16)

이 절은 아래 기존 리소스 부족 기록을 Neutral 범위에서 갱신한다. 나머지 미검수 상태는 유지한다.

- 기준: `Assets/04.Images/UI/dialogue/Portraits/Hwarin/Hwarin_Neutral.png` (768×2048). 기존 3D 전신 PNG는 보존하고 화린 세트의 defaultPortrait/Neutral 및 검수용 전경 액션만 일러스트로 연결했다. 대사·노드 ID·보상·3D 얼굴 설정은 변경하지 않았다.
- 내장 ImageGen으로 반감음·감음 및 옆머리 아래 보충 그림을 생성했다. 생성본 전체를 교체하지 않고 눈 영역과 옆머리 아래 영역만 사용했다. 원본은 알파가 있는 PNG이며, 보충 그림의 배경이 불투명해지는 부분은 합성 과정에서 투명 영역으로 복원했다.
- `Parts/Body_Neutral.png`, `Eyes_Open.png`, `Eyes_Half.png`, `Eyes_Closed.png`, `Hair_Side_L.png`, `Hair_Side_R.png`와 `.meta`를 추가했다. 몸통·눈·왼쪽 머리·오른쪽 머리 순서로 합성한다. 캔버스 좌표와 피벗은 세트에 기록했다.
- 눈은 기존 2.5~6초 간격/0.16초 재생을 사용한다. 옆머리는 뿌리 근처 피벗으로 좌 0.9도/4.6초, 우 1.2도/5.4초의 작은 회전을 준다. 긴 머리 메시 리그가 아니며 큰 흔들림으로 값을 올리지 않는다. 얼굴·뿔·몸통은 고정한다.
- `hasCompleteParts=1`은 Neutral의 네 레이어 합성이 가능한 데이터 상태다. 의상·6표정·Unity 시각 품질 검수 통과를 뜻하지 않는다. 추가 5표정 PNG는 로컬에 있지만 정렬·파츠 검수와 세트 연결은 하지 않았으며 기존 Neutral 폴백을 유지한다.
- AI 확인: 원본/생성본 캔버스 크기, 추출 파츠, 눈 3상태와 머리 ±1.2도 확대 합성 보드 확인. Unity 임포트·Play Mode·성능·Player는 미수행이다. 런타임 C# 변경은 없다.
- 생성본·프롬프트·좌표 추출/연결 스크립트·확대 보드는 `output/HwarinPortrait/`에 있다. 로컬 이미지 폴더와 output은 `.gitignore` 대상이므로 공유 시 PNG와 `.meta`를 별도로 포함해야 한다.

| ID | 실행 절차 | 기대 결과·실패 기준 | 판정 |
|---|---|---|---|
| H2D01 | Unity에서 새 PNG 임포트 완료 후 세트 Neutral의 4개 파츠와 눈 3프레임 참조 확인 | Missing 참조 없음, canvasSize 768×2048 | 사용자 테스트 대기 |
| H2D02 | Boot → LakeOfLifeScenario → 통합 런처 ‘화린 연출 검수’, review_00에서 AUTO 해제 후 30초 대기 | 눈이 닫혔다 열림. 눈 주변 사각 경계·얼굴 위치 이동·원본 눈 잔존은 실패 | 사용자 테스트 대기 |
| H2D03 | review_12 전경을 확대 관찰 | 양옆 머리 끝만 작게 흔들림. 뿔·얼굴 고정. 접합부 구멍·배경색 조각·이중 머리 끝은 실패 | 사용자 테스트 대기 |
| H2D04 | Keep 연속 진행, 로그/일시정지 복귀, Hidden→Keep, Cancel 후 재진입 | 같은 표정에서 주기 유지, pause 중 정지, 숨김/종료 뒤 파츠 잔류 없음. Console 예외 별도 확인 | 사용자 테스트 대기 |

회신: `H2D 항목 ID / 통과·실패·미수행 / 실제 결과 / 화면 또는 로그`.

### 기존 판정과 남은 범위

- **사용자 검수 통과 (2026-10-16):** 마지막 머리 숙임 회귀 수정 후 사용자가 “통과임”으로 확인했다. 해당 재현 대화의 머리 자세·눈동자 시선 방향을 통과로 기록한다. 3D 깜빡임 작동도 앞선 사용자 피드백으로 확인했다. 아래 실패·대기 기록은 수정 과정의 이력이며 이 최신 판정이 우선한다. D03/D04 전체 조건이나 화린 전체 확장 게이트 통과로 확대하지 않는다.

- 화린만 연결했다. 다른 캐릭터 확장 금지: 화린 사용자 검수 게이트가 아직 열리지 않았다.
- 구현 반영: Legacy/Default/Set/Keep/Hidden, 캐릭터별 세션 기억, 초상화·이력·선택적 전경 해석, 모델 얼굴 바인딩과 머리/눈 시선 보정, 기존 눈 표정에 양보하는 자동 깜빡임, 파츠 합성/흔들림/눈 프레임 재생 코드.
- 리소스 부족: 추가 5표정, 분리 파츠, 2D 눈 프레임. 실제 에셋에는 없는 이미지를 채우지 않았다. 6개 키 중 Neutral만 기존 정지 이미지가 있으며 나머지는 Neutral 폴백이다. **6표정 및 2D 애니메이션 완성 아님.**
- 사용자 테스트 대기: 회전 한계·다양한 거리/높이·표정 충돌·클리핑·입력·중단·실기 성능 등 전체 검수표의 나머지 조건. 이번 재현 대화의 머리·눈 방향과 깜빡임 확인은 위 통과 기록으로 구분한다. 파츠 리소스가 없는 항목은 리소스 확보 후 판정한다.

## 눈 움직임 없음 재검수

- 사용자 결과: 3D 눈 움직임 없음, 일러스트 변화 없음. 기존 자동 검사 통과와 별개로 실제 시각 검수 실패로 기록한다.
- 확인한 결함: DialogueManager가 IUpdatableManager만 구현하여 GameManager의 LateUpdate 목록에 등록되지 않았다. 따라서 OnLateUpdate의 얼굴 시선·깜빡임 적용이 호출되지 않았다.
- 수정: ILateUpdatableManager 등록 추가. Update 복원과 LateUpdate 적용 계약을 함께 검사하는 DialogueManagerRegistersBothFaceRestoreAndApplyTicks 테스트 추가.
- 재현: Play Mode를 종료하고 스크립트 재컴파일 후 Boot → LakeOfLifeScenario → 통합 툴 런처의 '화린 연출 검수'. review_00에서 AUTO를 끄고 10초 관찰한다. 정상 눈 표정을 다른 클립이 점유하지 않으면 3D 눈꺼풀이 주기적으로 닫혔다 열려야 한다. 시선은 무작위로 흔들리지 않으며 대화 상대를 바라본다. 상대가 정면이면 눈 회전이 작을 수 있으므로 Scene 뷰에서 상대를 좌우로 옮겨 Head/LeftEye/RightEye의 회전과 제한 범위를 확인한다.
- review_13에서는 명시 억제로 시선·깜빡임을 멈춘다. Cancel 후 보정이 남지 않는지도 확인한다. 결과는 D03/D04/D06 재검증으로 남긴다.
- 일러스트: review_00~05가 같은 그림인 것은 5표정 이미지 누락에 따른 폴백이다. 2D 눈 프레임·머리카락·의상 파츠도 없어 일러스트의 눈 깜빡임과 파츠 흔들림은 미완성이다. 이번 틱 수정으로 2D 애니메이션이 생기지는 않는다.
- 보조 컴파일: Assembly-CSharp.csproj 오류 0·경고 64, UPlayGround.Dialogue.Tests.csproj 오류 0·경고 0. 테스트 프로젝트의 첫 시도는 의존 DLL 생성 전 CS0006으로 실패했고 런타임 빌드 완료 후 재시도는 통과했다. 로그: Logs/HwarinPresentation-tickfix-build.log, Logs/HwarinPresentation-tickfix-tests-build.log.
- 검증: 수정 후 Play Mode 시각 확인과 새 EditMode 테스트 실행은 사용자 테스트 대기. 열린 Unity 프로젝트에 별도 배치 Editor를 띄우지 않았다. 이전 44개 통과 기록은 이번 수정 검증 결과가 아니다.

## 기획과 실제 리소스의 차이

1. 기존 구조 대화의 화린 6개 발화는 노드 portrait로 Player_Hwarin_Full.png를 사용한다. 화자 테이블은 Hwarin_Dialogue_Portrait.png를 참조한다. 두 파일을 직접 확인했으며 전자는 흑발·붉은 의상의 3D 전신 이미지, 후자는 백발·푸른 겉옷의 일러스트다. 이번 연결은 실제 구조 대화의 기존 외형을 보존한다. 두 그림을 서로 다른 감정으로 혼용하지 않는다.
2. 새 전역 CharacterDialoguePresentationCatalog 대신 이미 로드되는 SpeakerPortraitTableSO.presentationSets를 쓴다. 추가 Addressables 그룹/주소와 전역 매니저는 없다. 모델 얼굴 프로필은 모델 프리팹이 직접 참조한다.
3. 실제 구조 대화는 SpeakerActorBindingTable에서 화린→MonsterHonoka→Actor/DataBase/Humanoid/MonsterHwarin.asset→MonsterActor_Hwarin_2Handed.prefab으로 해석된다. 플레이어 모델뿐 아니라 이 대역과 기존 MonsterActor_Hwarin.prefab에도 동일 얼굴 프로필을 연결했다. 공용 IDialogueFaceActor 계약으로 관리 틱을 받으며, 몬스터는 홀드 중 Idle이고 교전하지 않을 때만 얼굴 보정을 적용한다.
4. 기존 FootIK/WeaponIK는 몸과 장비용이다. 얼굴 제어는 CharacterModelData의 직렬화 DialogueFaceBinding으로 추가했고, DialogueManager의 기존 OnUpdate에서 자신이 쓴 값을 복원하고 OnLateUpdate에서 애니메이션 위에 적용한다. 새 액터 Update나 전역 매니저는 없다. MagicaCloth 후처리 순서의 시각적 영향은 D03/D04에서 확인해야 한다.
5. Honoka.fbx의 Head/LeftEye/RightEye, Body의 Eye_Blink를 사용한다. 외부 Face 애니메이션의 Eye_* 채널을 조사해 모델 바인딩에 억제 목록을 기록했다. 채널 인덱스는 모델 연결 때 Mesh에서 캐시하며 명시 눈 표정이 있으면 자동 깜빡임을 생략한다. 채널 값 복원은 자신이 마지막으로 쓴 값이 남아 있을 때만 한다.
6. 청자에게는 같은 시선·깜빡임이 적용된다. 2D는 기존 한 화자 표시 영역을 유지한다. 별도 청자 표정 표시 영역은 추가하지 않았다. 노드 suppressFaceLook/suppressFaceBlink는 해당 라인의 참여 플레이어 전체에 적용된다.

## 실제 적용 파일과 경로

- 씬: Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity (이번 작업에서 수정하지 않음).
- 실제 대화: Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioHwarinRescue.asset.
- 보존 ID: graphId=dlg_test_honoka_rescue. 화린 노드 _02=Worried, _04=Serious, _07=Serious, _09=Serious, _11=Worried, _13=Smile. 대사·선택지·액션·진행 연결은 그대로다. 기존 노드 portrait를 새 세트의 기본 참조로 옮겼다.
- 모델: Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Hwarin.prefab / CharacterModelData.dialogueFace.
- 실제 대역: Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Hwarin_2Handed.prefab 및 기존 Assets/03.Prefabs/Actor/Monster/MonsterActor_Hwarin.prefab / MonsterActor._dialogueFace.
- 위 Actor 프리팹 폴더는 저장소 .gitignore 대상이다. 로컬 파일에는 반영했으나 Git diff 목록에는 나타나지 않으므로 공유·배포 시 세 프리팹을 별도로 포함해야 한다.
- 설정: Assets/10.Datas/Dialogue/Presentation/Hwarin/DialoguePortraitSet_Hwarin.asset 및 DialogueFaceProfile_Hwarin.asset.
- 테이블: Assets/10.Datas/Dialogue/SpeakerPortraitTable.asset의 presentationSets에 화린 1개만 연결.
- 테스트: 같은 Hwarin 폴더의 DLG_HwarinPresentationReview.asset (graphId=dlg_hwarin_presentation_review).
- 전경 테스트: Action_HwarinPresentationForeground.asset. 기존 힌트 그림을 테스트 배경으로 쓰며 새 스토리 장면으로 배포하지 않는다.
- 실행: UPlayGround 통합 툴 런처에서 “화린 연출 검수” 검색 → 실행. 등록 ID: UPlayGround/내러티브/대화/화린 연출 검수. Edit Mode에서는 검수 그래프 선택, Play Mode에서는 대화 시작.

## 저작·수명주기

Legacy=0이므로 미이관 노드는 기존 Resolver 결과 그대로다. 새 모드의 portrait 중복 지정은 그래프 에디터와 기존 DialoguePortraitValidator에서 오류로 표시한다. Set이 빠르게 스킵돼도 이력 기록 경로에서 표정 기억을 갱신하므로 착지 노드 Keep이 이를 유지한다. Hidden은 표시만 숨기고 기억은 지우지 않는다. Default는 Neutral을 기억한다. 종료·Cancel·씬 이탈은 기억을 삭제하며 영구 저장하지 않는다. JSON import/export에도 새 필드를 포함했다.

전경 연결은 applyExpressionToForeground를 켜고 기존 전경이 같은 표정 세트의 이미지일 때만 적용된다. 배경·다른 인물 삽화는 교체하지 않는다. 파츠는 별도 자식 Image로 부모의 기존 팬·줌·등장을 따른다. 그림 세트가 hasCompleteParts를 명시하고 모든 기본 파츠가 존재할 때만 정지 이미지를 숨긴다. 파츠가 부족하면 기본 이미지 유지이며 애니메이션 완성으로 판정하지 않는다. 추가 비동기 로딩은 없다.

얼굴 프로필이 없는 모델은 미적용이다. 시선 목표는 기존 대화 상대의 얼굴 소켓이며, 눈 중심이 지정된 플레이어 모델은 그 점을 우선한다. 얼굴 기준점이 없으면 발 위치를 응시하지 않고 시선을 생략한다. 근접 한계 안쪽에서는 시선을 적용하지 않는다. 모델 교체·비활성화·홀드 종료·전투/피격 상태에서는 자신의 보정을 해제한다. 시선과 3D 깜빡임은 명시 pause에서 타이머가 정지하며 월드 timeScale=0에서는 unscaledDeltaTime으로 진행한다.

## 부족 리소스 납품 목록

기준 외형을 먼저 확정한다. 이번 폴백은 Player_Hwarin_Full.png이며, 백발 일러스트를 기준으로 채택하려면 외형 불일치부터 아트 검수해야 한다. 아래 경로는 **요구 경로이며 현재 파일이 존재한다는 뜻이 아니다**.

목표 폴더: Assets/04.Images/UI/Dialogue/Portraits/Hwarin/

| 파일/파츠 | 요구 |
|---|---|
| Hwarin_Neutral.png | 동일 외형의 기본 완성 이미지. 기존 전신을 유지할 경우 참조 재사용 가능 |
| Hwarin_Smile.png, Hwarin_Serious.png, Hwarin_Displeased.png, Hwarin_Worried.png, Hwarin_Surprised.png | 같은 포즈·캔버스·눈높이·배율의 추가 5표정 |
| Parts/Body.png, Parts/Face_<표정>.png | 움직이지 않는 몸통과 표정 얼굴, 원래 파츠 좌표 보존 |
| Parts/Hair_Back.png, Hair_Front.png, Hair_Side_L.png, Hair_Side_R.png | 뿌리 피벗과 겹침 여유, 움직일 때 드러나는 목·어깨의 보충 그림 |
| Parts/Cloth_Ribbon.png, Cloth_Sleeve_L.png, Cloth_Sleeve_R.png, Cloth_Hem.png | 실제 가동부만 분리, 접합부 보충 그림, 비대칭 보존 |
| Eyes/<표정>_Open.png, _Half.png, _Closed.png | 6표정 각각 열린/중간/감은 눈. 감은 눈 유지 표정은 hasClosedEyes 지정 |
| Hwarin_Parts_Source.psd 또는 동등 레이어 원본 | 피벗·파츠 좌표·레이어 순서·캔버스/크롭 기준 기록 |

Assets/04.Images와 Assets/ExternalAssets/UI에서 표정/파츠를 확인하지 못했다. Honoka 외부 얼굴 클립은 3D BlendShape 리소스이며 2D 눈 프레임으로 간주하지 않는다. 새 플러그인은 도입하지 않았다.

## 사용자 테스트 준비

Unity 6000.3.21f1 URP. 기존 저장 슬롯을 복제하거나 폐기 가능한 새 테스트 슬롯을 사용한다. Assets/01.Scenes/GameLogic/Boot.unity에서 Play하고 기존 흐름으로 LakeOfLifeScenario에 진입한다. 퀘스트 대화가 열려 있으면 정상 종료 후 툴 런처의 화린 연출 검수를 실행한다. 보상/플래그 액션이 없는 별도 그래프라 실제 구조 대화의 진행을 완료하지 않는다. 런처는 씬을 저장하거나 사용자 배치를 변경하지 않는다.

검수 그래프의 노드 순서: review_00~05=Neutral/Smile/Serious/Displeased/Worried/Surprised, 06=Keep 선택지, 07/08=분기 Smile/Serious, 09=Default 합류, 10=Hidden, 11=Keep, 12=전경 팬·줌, 13=시선/깜빡임 억제, 14=Legacy, 15=Player 예약 화자, 16=End. 대사 문자열은 기존 구조 대화에서 복사한 검수용 반복이며 새 스토리 대사가 아니다.

시선 조절: Play 중 Hierarchy에서 화린 대역/현재 모델과 상대 플레이어의 Head를 확인한다. 상대 액터 위치를 이동하고 Scene 뷰와 Game 뷰를 함께 본다. KCC가 위치를 복원하면 상대의 부모를 움직이지 말고 대화를 종료한 뒤 액터 시작 위치를 바꿔 재실행한다. 변경한 Play Mode 위치는 저장하지 않는다.

| ID | 실행 경로·절차 | 기대 결과·실패 기준 | 현재 상태 |
|---|---|---|---|
| D01 | 검수 그래프 00~05→06→09→10→11 | 6키·Keep/Default/Hidden 전환. 현재 추가 5표정은 기본 폴백이어야 함. 이미지 확보 후 눈높이·크롭·잔존 파츠 검수 | 기능 사용자 테스트 대기 / 6표정 리소스 부족 |
| D02 | 06 선택지에서 30초 대기, 두 선택을 각각 재실행해 07/08→09 | 선택 전 기억 유지, 분기별 표정, 합류 Neutral. 반대 분기 표정 유입 실패 | 사용자 테스트 대기 |
| D03 | 00 대기 중 상대를 좌우·상하·뒤·근접으로 이동 | 머리 좌우40/위20/아래25도 출발값, 눈12/10도. 뒤쪽은 영향도 완화, 근접0.45m 안에서는 보정 생략. 목 꺾임·사시·루트 떨림 실패 | 사용자 테스트 대기 |
| D04 | 12와13에서 머리 움직이는 기존 대화 제스처 비교. 검수 그래프 복제본의 13에 기존 speakerMotionId 지정 | 13의 suppressFaceLook가 명시 제스처를 보존. 원본 모션 데이터는 수정하지 않음 | 사용자 테스트 대기 |
| D05 | 00에서60초 대기, 모델 Body의 Eye_Half 또는 Eye_Blink를 명시 값으로 설정 후 해제,13 비교 | 자동 눈깜빡임이 명시 눈 표정을 덮지 않음. 2D는 눈 프레임 확보 후 검수 | 3D 사용자 테스트 대기 / 2D 리소스 부족 |
| D06 | 파츠 확보 후 hasCompleteParts 지정,00/12 확대 관찰 | 머리카락·옷 파츠 뿌리 고정, 접합부 구멍·얼굴 변형 없음. 현재 정지 이미지 흔들림 없음이 정상 | 리소스 부족 |
| D07 | 00부터 Submit 연타·AUTO·타이핑 스킵·강제 Skip,06에서 선택 | 최신 도착 노드와 이력 표정 일치, 선택지 자동 선택 없음, 잔상 없음 | 사용자 테스트 대기 |
| D08 | 00에서 월드 timeScale=0, 대화 정지/로그 열람, 복귀 | 월드 정지 중 얼굴 연출 진행, 대화 pause 중 타이머 정지. 복귀 시 튐 없음 | 사용자 테스트 대기 |
| D09 | 00/12에서 Cancel→재시작, 씬 이탈, 상대 제거, 파티 모델 교체 각각 실행 | 기존 표정·타깃·보정·파츠 잔류 없음, 이동 복귀, 다른 얼굴에 화린 프로필 적용 안 됨 | 사용자 테스트 대기 |
| D10 | 별도 검수 그래프 복제본에서15의 Player/당신/Protagonist를 비교. 파티 활성 캐릭터와 주인공을 다르게 설정 후 시작 | 기존 Main 세션은 주인공 외형 홀드를 사용하므로 진입 후 실제 ActiveCharacterType도 함께 기록. 미이관 인물은 기존 이미지 유지 | 사용자 테스트 대기 |
| D11 | 세트/모델 복제본에서 표정·파츠·프로필·Head·눈꺼풀 참조 하나씩 제거 | 표정→Neutral→기존 이미지→숨김, 파츠 누락 시 정지 이미지, 본/채널 누락 시 해당 제어만 생략. 진행과 Cancel 가능 | 사용자 테스트 대기 |
| D12 | 패드만으로00~16,06선택·로그·Cancel·재진입 | 초기 포커스·로그 복원·Submit·Cancel 가능. 마우스 필요하면 실패 | 사용자 테스트 대기 |
| D13 | 12에서 전경과 배경 팬·줌 관찰 후13, 실제 기존 삽화 대화도 재생 | 전경만 표정 연결, 배경 보존, 기존 팬·줌 충돌 없음. 파츠 움직임은 확보 후 판정 | 사용자 테스트 대기 / 파츠 부족 |
| D14 | 같은 위치·카메라·해상도에서 프로필 미연결 사본과 연결본 각각60초 Profile | 기능 기인 매 프레임 GC0, CPU/GPU·텍스처 메모리·진입 끊김 기록. 목표 기기 예산 합의 전 성능 통과 금지 | 미측정 |
| D15 | 실제 구조 대화의 기존 경로와 미이관 NPC 대화 재생, 구조 전/후 저장·로드·취소 재진입 | 기존 ID·진행·보상 보존, 미이관 portrait 우선순위 동일. 구조 완료를 검수 그래프가 발생시키면 실패 | 사용자 테스트 대기 |

각 항목에서 진행 성공과 Console 예외 유무를 별도로 판정한다. 회신: ID / 통과·실패·미수행 / 실제 결과 / 화면·영상·로그. 실패 시 노드 ID, 입력 장치, 시작 슬롯 상태를 포함한다.

## 검증 기록

자동 검사 결과는 실행 완료 후 아래에 기록한다. Unity Editor Play Mode 시각·패드 검수와 Windows Player 검증은 사용자 테스트 대기다. 이번 통합은 최종 완료 전 Windows Player에서 같은 경로를 검증해야 하며, EditMode 통과만으로 대체하지 않는다.

## 변경 파일 목록

다음은 이번 작업에서 직접 편집한 파일이며 .meta는 별도로 함께 생성·보존한다. 기존 사용자 변경 파일 전체 목록과 구분한다.

- `Assets/02.Scripts/Data/Dialogue/DialogueFaceProfileSO.cs`
- `Assets/02.Scripts/Data/Dialogue/DialogueNodeSO.cs`
- `Assets/02.Scripts/Data/Dialogue/DialoguePortraitSetSO.cs`
- `Assets/02.Scripts/Data/Dialogue/SpeakerPortraitTableSO.cs`
- `Assets/02.Scripts/Data/Editor/Dialogue/DialogueJsonIO.cs`
- `Assets/02.Scripts/Data/Editor/Dialogue/DialoguePortraitValidator.cs`
- `Assets/02.Scripts/Editor/Dialogue/HwarinPresentationReviewTool.cs`
- `Assets/02.Scripts/Editor/DialogueGraphEditor.cs`
- `Assets/02.Scripts/GameActor/Component/Player/CharacterModelData.cs`
- `Assets/02.Scripts/GameActor/Component/Player/DialogueFaceBinding.cs`
- `Assets/02.Scripts/GameActor/Contracts/IDialogueFaceActor.cs`
- `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.DialogueFace.cs`
- `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs`
- `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Components.cs`
- `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Dialogue.cs`
- `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Lifecycle.cs`
- `Assets/02.Scripts/Manager/Dialogue/DialogueManager.Expressions.cs`
- `Assets/02.Scripts/Manager/Dialogue/DialogueManager.Stage.cs`
- `Assets/02.Scripts/Manager/Dialogue/DialogueManager.cs`
- `Assets/02.Scripts/Story/Dialogue/ShowAnimatedDialogueIllustrationActionSO.cs`
- `Assets/02.Scripts/Tool/Editor/UPlaygroundToolsLauncher.cs`
- `Assets/02.Scripts/UI/Contracts/UIServices.cs`
- `Assets/02.Scripts/UI/Dialogue/UIDialoguePortraitAnimator.cs`
- `Assets/02.Scripts/UI/Dialogue/UI_Scene_Dialogue.cs`
- `Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Hwarin_2Handed.prefab`
- `Assets/03.Prefabs/Actor/Monster/MonsterActor_Hwarin.prefab`
- `Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Hwarin.prefab`
- `Assets/10.Datas/Dialogue/Presentation/Hwarin/Action_HwarinPresentationForeground.asset`
- `Assets/10.Datas/Dialogue/Presentation/Hwarin/DLG_HwarinPresentationReview.asset`
- `Assets/10.Datas/Dialogue/Presentation/Hwarin/DialogueFaceProfile_Hwarin.asset`
- `Assets/10.Datas/Dialogue/Presentation/Hwarin/DialoguePortraitSet_Hwarin.asset`
- `Assets/10.Datas/Dialogue/SpeakerPortraitTable.asset`
- `Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/DLG_ScenarioHwarinRescue.asset`
- `Assets/Tests/EditMode/Combat/DialogueFaceBindingTests.cs`
- `Assets/Tests/EditMode/Dialogue/DialogueExpressionSessionTests.cs`
- `Assets/docs/design/PLAYABLE_CHARACTER_DIALOGUE_PRESENTATION_PLAN.md`

## 확인된 경고와 남은 작업

- 기존 DialoguePortraitValidator 실행에서 화린 추가 5표정 부족 경고가 의도대로 출력됐다. 같은 전체 검사에서 묘령 초상화 미해석 14줄(예: Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Lake_ShrineArrival.asset)도 발견됐다. 화린 범위 밖이므로 수정하지 않았다.
- 대역 연동 중 ActorStateId 네임스페이스 누락으로 발생한 컴파일 실패는 수정했다. 해당 실패와 최종 재실행 결과를 구분한다.
- 다음 작업은 원화 외형 기준 확정 → 추가 표정/파츠/눈 프레임 확보 → 세트 매핑과 hasCompleteParts 검수 → D01~D15 직접 실행 → 최종 Player 빌드/실행이다.
- 정지 이미지 폴백, 코드 경로 존재, EditMode 검사 통과는 시각적인 2D 애니메이션 완성 또는 AAA 품질 통과를 뜻하지 않는다.

## 최종 자동 검증 결과

- 실행 환경: Unity 6000.3.21f1, Windows Editor batchmode/nographics, EditMode. AI가 실행했다.
- 최종 스크립트 컴파일 통과. 관련 EditMode **44개 통과 / 실패 0 / 건너뜀 0**. 기존 Dialogue 테스트, DialogueIllustrationPrefabTests, DialogueIllustrationMotionTests와 신규 표정 세션·실제 모델/대역 얼굴 바인딩 검사를 포함한다.
- 결과: Logs/HwarinPresentation-EditMode-final.xml / Logs/HwarinPresentation-EditMode-final.log.
- 기존 DialoguePortraitValidator도 테스트에서 실행했다. 검사 호출은 통과했지만 화린 5표정 부족과 묘령 14줄 미해석 경고는 해결되지 않은 데이터 항목이다. 전체 아트 데이터 완전 통과로 해석하지 않는다.
- 정적 비교: 이번 작업 시작 직전 실제 구조 대화와 비교해 연출 필드 및 portrait 이관을 제외한 대사·선택지·분기·액션·ID 동일. Logs/HwarinPresentation-static.json에 변경 파일과 기존 에셋 GUID를 기록했다.
- git diff --check 통과. 기존 사용자 변경을 되돌리거나 커밋하지 않았다. 기존 에셋의 일괄 재직렬화는 수행하지 않았다.
- 미수행: Editor Play Mode 시각/패드 검수, 프로파일링, Windows Player 빌드/실행. 모두 사용자 테스트 대기이며 이번 자동 검사로 대체하지 않는다.

## 눈동자 방향 오차 재검수

- 사용자 재검수: 3D 깜빡임은 작동하지만 눈동자가 엉뚱한 곳을 바라봄(화면 첨부). 깜빡임 작동 확인과 시선 품질 실패를 분리한다.
- 조사: 실제 화린 대역의 LeftEye/RightEye localRotation은 y=0.1478094, w=0.9890159(약 17도)다. 기존 Initialize는 현재 포즈에서 root.forward를 역변환해 정면 축으로 저장하므로, 프리팹에 저장된 눈 포즈와 재초기화 때 애니메이션 포즈를 보정 기준에 섞는다.
- 수정: SkinnedMeshRenderer.bones와 sharedMesh.bindposes에서 해당 본의 정면 기준축을 구한다. 바인드 정보가 없는 본은 기존 계산을 유지한다. 메시·프리팹의 저장된 회전과 GUID는 변경하지 않는다. 양 눈의 목표 방향 공유 및 회전 제한·깜빡임 소유권은 유지한다.
- 회귀 검사: EyeDirectionUsesBindPoseInsteadOfAuthoredEyeRotation. 초기 눈 회전 17도·액터 회전 63도에서도 정면 목표를 응시하고 Clear 시 초기 눈 포즈로 복원하는지 검사한다.
- 재검수 D03: Play 종료 → 재컴파일 → Boot → LakeOfLifeScenario → 화린 연출 검수 review_00, AUTO OFF. 정면 대화 상대를 응시하는지, Scene 뷰에서 상대를 좌우로 옮겼을 때 양 눈이 같은 방향으로 따라가는지 확인한다. 실제 구조 대화에서도 같은 확인을 반복한다. 사시·흰자 과다 노출·지속적인 옆눈이 있으면 실패다.
- 실제 메시의 광학적 정면과 시각적 자연스러움은 사용자 재검수 대기다. 첨부 화면만으로 실제 상대 좌표와 카메라 방향의 관계를 확정하지 않는다. 일러스트 리소스 부족 상태는 동일하다.

- 이번 수정 보조 검증: UPlayGround.Actor.csproj 컴파일 오류 0·경고 2, UPlayGround.Combat.Tests.csproj 오류 0·경고 0. 로그는 Logs/HwarinPresentation-eyeaxis-build.log 및 Logs/HwarinPresentation-eyeaxis-tests-build.log. 새 EditMode 테스트 실행 및 Play Mode 시각 검증은 미수행이며 사용자 재검수 대기다.

## 머리 과도한 숙임 회귀 수정

- 사용자 재검수 실패: 눈 기준축 수정 후 머리가 가슴 방향으로 꺾임. 직전 수정은 실제 모델 검증 없이 적용한 회귀로 기록한다.
- 원인: 메시의 bindpose에 런타임 renderer 방향을 직접 넣어 임포트 좌표계를 혼용했고 머리 기준축까지 변경했다. 앞 절의 직접 bindposes 변환 설명은 이 수정으로 대체한다.
- 수정: 머리 기준축은 바인드 변환 이전 방식으로 복구. 눈만 (eyeBindPose × inverse(headBindPose))로 머리 기준 정면을 눈 공간에 옮겨 공통 메시 좌표계를 상쇄한다. 프리팹·애니메이션·시선 수치 변경 없음.
- 회귀 검사 보강: 기존 초기 눈 17도·액터 63도 조건에 공통 메시 임포트 축 0도/90도 케이스를 추가하고 눈뿐 아니라 머리 정면 유지도 확인한다.
- 보조 컴파일: Actor 오류 0·경고 2, Combat.Tests 오류 0·경고 0. Logs/HwarinPresentation-headfix-build.log 및 Logs/HwarinPresentation-headfix-tests-build.log. 새 EditMode 실행과 실제 화면 검증은 미수행.
- 재검수: Play 종료·재컴파일 후 Boot에서 다시 진입하여 첨부 화면과 같은 실제 화린 대사를 재생한다. 머리가 가슴으로 꺾이지 않고 상대 얼굴을 향해야 한다. 고개 자세, 양 눈 방향, 깜빡임을 각각 판정한다. 이전 시선 방향 수정은 통과 처리하지 않는다.

## 사용자 최종 회신 — 머리·눈 방향 수정 통과 (2026-10-16)

- 확인 주체·근거: 사용자 직접 플레이 재검수, 회신 “통과임. 문서 기록해줘”.
- 결과: 마지막 수정이 적용된 재현 대화의 머리 자세와 눈동자 방향 통과. 앞서 확인한 3D 깜빡임 작동도 유지 기록한다.
- 범위: 보고된 시선 오차와 머리 과도한 숙임 회귀의 해결 확인이다. D01~D15 전체, 모든 거리·각도, 패드, Cancel/종료/씬 이탈/모델 교체 검증까지 통과한 것으로 해석하지 않는다.
- 남은 항목: 추가 5표정·분리 파츠·2D 눈 프레임 부족, 나머지 직접 검수, 새 회귀 테스트 실행·성능·Player 검증. 다른 캐릭터 확장은 아직 보류한다.
- 이번 변경은 문서 기록만 수행했으며 코드·에셋 변경과 추가 테스트 실행은 없다.
