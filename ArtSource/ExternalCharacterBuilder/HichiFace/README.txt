사용 제약: 두피·뒤통수 일부가 열린 얼굴 메시이므로 이를 가리는 헤어가 필수다.
민머리·열린 부위를 노출하는 헤어는 현재 지원하지 않는다. 지원하려면 두피·뒤통수 추가 저작과 재검수가 필요하다.
P09 빌더 연결: Hichi_P09_BuildPreset(Hair01), Hichi_P09_Alternate_BuildPreset(Hair13).
두 프리셋은 Assets/10.Datas/Actor/ExternalModels/ 아래에 있으며 같은 Hichi_P09_Face를 사용한다.
제약 상세: Assets/docs/EXTERNAL_MODEL_P09_CONVERSION_GUIDE.md의 '기존 모델의 명시적 사용 제약'.

Hichi 기반 P09 신규 얼굴 제작 카드 / r03

최신 보정: r03 의상 없는 목의 곡면·피부 연결을 수정했다.
BARE_NECK_FIX_RESULT.txt와 output/HichiBareNeckFix/가 최신 결과다.
최종 메시·final.bin·.blend는 9,878정점 / 15,287삼각형 / 36표정이다.
아래 r01 제작 기록의 수치·목 재질 공유 설명은 과거 상태다.
r02 이후 export_neck_body → match_bare_skin → build_bare_neck.py → import_bare_neck
→ validate_bare_neck / validate_bare_preservation → export_final → finalize_blend 순서다.
보존 검사는 export_final 이후 실행한다. build_bare_neck.py는 r02 백업을 입력으로 쓴다.

대상: 새 모델·얼굴 프로파일·BuildPreset. 기존 유라의 Player/Monster 모델과 분리.
원본: Assets/ExternalAssets/Character/Hichi/Prefab/Hichi.prefab의 hichi_face.
원본 얼굴: 9,774정점 / 재질 2개 / 표정 324개. 실제 가중 본 Head, LeftEye, RightEye.
방향: 눈 세로 폭 축소, 낮춘 눈썹, 높은 코, 넓어진 입, 정돈된 볼과 둥근 턱.
텍스처: 원본 UV를 기준으로 ImageGen이 생성한 밝은 피부·호박색 눈.
P09 기반: 실제 BasePrefabFemaleNoPhysics, 얼굴 본 배열 77개. 눈 웨이트 대응 보존.
기본: P09 Hair_01 + Armor_003 / 갈색. 대체: Hair_13 + Armor_011 / 갈색. 물리·투구 없음.
제작 범위: HichiFace, HichiP09, Hichi_P09 이름의 신규 경로만 사용.
기존 원본·유라·정규 주민·사용자 씬을 교체하지 않는다. 기존 파일 백업 대상 없음.
구현 반영: 신규 모델·프리셋·검수 씬 제작 완료. 미술 승인은 사용자 확인 대기.

산출물과 사용법
- 기본 모델: Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab
- 대체 모델: Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09_Alternate.prefab
- 기본 프리셋: Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset
- 대체 프리셋: Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Alternate_BuildPreset.asset
- 얼굴: Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Face.asset
- 피부 톤: Assets/10.Datas/Actor/ExternalModels/Hichi_P09_ToonLook.asset
- 검수 씬: Assets/01.Scenes/Tests/Equipment/P09_Hichi_Review.unity
- 모델 데이터: Assets/08.Models/Characters/HichiP09/
- 실제 Unity 렌더·검증: output/HichiP09/
- 제작 원본: 이 폴더의 Hichi_P09_Face.blend, source.json, sculpt.bin, final.bin,
  ImageGen 프롬프트와 csx/py 재현 코드.

통합 툴 런처 → P09 캐릭터 빌더 → Hichi_P09_BuildPreset 불러오기.
교체 얼굴 메시가 Hichi_P09_Face이고 ExternalModelPrefab은 비어 있어야 한다.
헤어와 의상 선택을 바꿔도 새 얼굴 프로파일이 유지된다.
두 프리팹은 모델 전용이다. Actor 생성, 대화, 충돌, NPC 데이터, GAS는 추가하지 않았다.
검수 씬은 Idle 자세 비교용으로 Animator를 정지해 두었다.

제작 내용
- Blender에서 눈·눈썹·코·입·볼·턱에 연속 변형을 적용했다. 조형 최대 변위 약 3.63mm.
- 입력 324개 중 비어 있지 않은 316개 표정을 같은 변형으로 처리해 Blender에 보존했다.
- 런타임에는 P09 소비 순서의 36개 표정만 연결했다. 일부 감정 표정은 가장 가까운
  원본 표현으로 대응하므로 이름 일치만으로 감정 표현의 미술 승인을 뜻하지 않는다.
- Head/LeftEye/RightEye 웨이트를 P09 Head/eye_L/eye_R로 대응했다.
  원본 눈 본의 현재 회전은 굽지 않고 바인드 정점에서 제작했다.
- 목 임계 높이로 잘라 얻은 초기 면에서 틈을 발견했다. 그 면을 제거하고 실제 Hichi
  목 경계 22점과 P09 몸통 경계 16점을 38삼각형으로 연결했다.
- 최종 얼굴은 9,888정점 / 15,155삼각형 / 77 bindpose / 36표정 / 3재질 슬롯.
  목은 얼굴 재질을 공유한다. 최종 목·표정·웨이트는 final.bin과 .blend에도 기록했다.
- 공유 Daon 피부를 수정하지 않고 Hichi 전용 피부 재질·ToonLook을 만들어 밝기를 맞췄다.

재현 순서
1. Unity export_source.csx. Hichi 원본과 실제 P09 베이스를 읽는다.
2. imagegen_prompt.txt 입력/출력 경로로 텍스처 준비.
3. Blender sculpt_face.py. 원본 순서로 조형과 표정을 sculpt.bin에 저장한다.
   이번에는 MCP 조형 완료 뒤 .blend 저장 호출이 300초 timeout되었다.
   바이너리 완료를 확인하고 조형을 반복하지 않은 채 별도 background Blender 5.2.2 LTS의
   save_blend.py로 원본을 복원·저장했다. 사용자 Blender 씬은 초기화하지 않았다.
   실행 파일: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe
   인자: --background --factory-startup --python <이 폴더의 save_blend.py>
4. Unity import_face → build_model → finish_look → connect_neck → build_alternate → match_skin.
   connect_neck 이후 기본 모델에 FaceMeshProfile.Apply를 다시 호출해 목 재질을 반영한다.
   correct_pupil.csx는 최초 저작 중 잘못 대응한 축소 표정을 교정한 기록이며,
   현재 import_face.csx에는 올바른 eye_small 대응이 반영되어 있다.
   신규 생성 스크립트는 기존 에셋이 있으면 중단한다. 재제작을 위해 기존 에셋을 삭제하지 않는다.
5. validate_model, validate_pose_bounds, validate_neck와 capture_*.csx 실행.
6. create_review_scene → capture_scene. 저장 후 additive 재로드 검사를 수행하고 원래 씬으로 복귀한다.
7. export_final.csx → 별도 background Blender finalize_blend.py.
   최종 Unity 메시를 36표정·77본 vertex group과 함께 .blend에 보관한다.
   Blender 파일은 메시 저작 원본이며 게임용 P09 골격은 Unity 베이스가 소유한다.

검증 결과 — AI 자동 검사 / Unity 6000.3.21f1
- P09FaceMeshProfileTests: EditMode 3개 통과 / 실패 0. 공용 기존 Daon fixture 테스트다.
  최초 전체 클래스 경로에서 Tests 네임스페이스를 빠뜨려 0개가 선택됐으며,
  실제 경로로 다시 실행해 3개 실행을 확인했다. editmode_tests.json에 최종 결과 저장.
- Hichi 전용 검사: 기본·대체 프리팹 재로드, 유효 Humanoid Avatar, Missing Script 0,
  본·재질 누락 0, 정규화 웨이트·유한 좌표·빈 표정 오류 0.
- Hair_01/03/13와 Chest_03/11 교체 후 얼굴 유지 검사 통과.
- Idle·Walk 각각 0.2/0.5/0.8 표본 6개: 얼굴 rootBone 기준 bounds 이탈 0.
- 목 회전 -35/0/35도 × P09 표정 36개 = 108개 표본: 상단 이음새 최대 오차 0m.
  하단 복제 정점 54개는 P09 원본 위치·웨이트와 정확히 일치.
- 실제 Unity 정면·35도·측면·후면·전신·눈 감기·미소·입 벌림·목 회전·보행 캡처 저장.
  P09PosePreview 표본은 Editor 정적 베이크다. LiveSkin/Alternate/ReviewScene_LiveSkinned는
  베이크 렌더러를 제거한 실제 SkinnedMeshRenderer 렌더 결과다.
- 검수 씬 저장 후 재로드: 루트 5개, 두 모델 Missing Script 0.
  기본 전체 모델: 활성 렌더러 6개 / 41,754삼각형.
  대체 전체 모델: 활성 렌더러 7개 / 45,124삼각형. 군중 성능 통과를 의미하지 않는다.
- 캡처 초기 정리 코드에서 Camera.targetTexture 해제 전 RenderTexture를 파기해 오류 1건 발생.
  capture_scene.csx의 해제 순서를 고친 뒤 같은 캡처를 다시 실행했고 추가 오류는 없었다.
- 런타임·Editor C# 소스/asmdef 변경 없음. 동적 제작 C# 컴파일·실행과 Unity 에셋 로드 확인.
- 원본 Hichi, 유라 플레이어/몬스터, 작업 전 열려 있던 검수 씬을 포함한 311개 파일의
  SHA256 일치. 기존 tracked 변경 목록도 작업 시작과 동일. 사용자 수정은 보존했다.
- 커밋하지 않았다. Windows Player 빌드·실행은 이번 모델 에셋 제작 범위에서 생략했다.

적용 경계와 남은 검증
- 사용자가 정규 교체 대상/월드 인물을 지정하지 않았으므로 신규 모델로 제작했다.
  발견한 기존 사용처는 유라의 플레이어/몬스터 모델이며 새 얼굴의 교체 대상으로 추정하지 않았다.
  정규 NPC 교체·ActorDefinition·월드 배치·초상화 반영을 완료했다고 보고하지 않는다.
- 미술 승인, 실제 Play Mode 대화·시선·립싱크·이동·재진입은 사용자 테스트 대기.
- 눈 본 웨이트는 보존했지만 실제 시선 추적 런타임은 미검증.
- 기본/대체 두 조합만 미술 캡처로 확인했다. 긴 머리·투구·망토 물리와 전체 의상 카탈로그는 미검증.
  원본 및 P09처럼 머리카락이 덮는 위쪽 두피가 열려 있어 대머리 모델로 사용할 수 없다.
- Player/Enemy 무기·GAS·모션·충돌 통합, 군중 성능, Player 빌드/실행은 미수행.
- 사용자 직접 테스트 절차는 TESTS.txt를 따른다.
