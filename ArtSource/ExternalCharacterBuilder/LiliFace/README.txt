현재 승인 상태 및 기본 몸체 검수본

사용자가 현재 Lili 얼굴·헤어가 잘 맞는 것을 확인했다. 직전 추가 틈 신고는 대상 착오로 정정되었다.
추가 헤어 변형은 중단했으며 r05의 Lili 에셋을 유지한다. 전체 헤어 카탈로그 승인으로 확대하지 않는다.
Lili_P09_Review.unity에 Lili_P09_BodyOnly를 (1.95,0,0)에 추가했다.
기본 Body의 Female_Face_02 / Female_Body_Chest / Female_Body_Leg만 표시한다.
Hair·Armor·무기·장식·손발 보조 파츠는 숨기고 기존 의상 프리팹에는 적용하지 않는다.
기존 의상 모델 두 개는 유지하고 검수 카메라가 세 모델을 함께 보도록 조정했다.
재현: create_body_review.csx (기존 검수본이 있으면 중복 생성하지 않음).
저장 후 재로드와 활성 렌더러 3개를 검사했다. 근거: output/LiliP09BodyReview/validation.txt, Review.png.
현재 외형 사용자 확인과 Play Mode·물리 검증은 별개다.

Lili P09 r05 — 이마와 앞머리 사이의 배경 노출 수정

r04의 전체 이동만으로는 아래쪽 사선의 큰 틈이 남았다. 사용자 근접 화면을 기준으로 재검수했다.
현재는 Hair 03/11 부착 본의 앞뒤 폭(X)을 0.88로 맞추고 머리 기준 위치 X를 0.02m로 조정한다.
03의 Y=-0.01m, 11의 Y=0m는 유지한다. 위치와 배율 모두 절대값이며 재적용해도 누적되지 않는다.
얼굴·두피·헤어 공유 메시·바인드 포즈는 이번 수정에서 변경하지 않았다. 얼굴 파일 SHA256 동일 확인.
앞머리 본만 옮긴 후보는 정수리 머리카락 사이가 벌어져 제외했고, 헤어 전체 깊이 조정으로 결정했다.
P09FaceMeshProfileSO.HairAttachment.localScale의 기본값은 (1,1,1). 기존 프로필의 생략된 필드는
기존 크기로 읽으며 0/음수/NaN/Infinity 배율은 적용 전에 거부한다.

재현: fit_hair.csx → capture_hair_front.csx. r04의 얼굴 재생성은 필요하지 않다.
확인: Unity 컴파일 오류 0, P09FaceMeshProfileTests 10/10 통과.
Idle/Walk 12샘플에서 저장된 헤어 배율·부착 설정과 각 프리팹의 활성 Hair 03/11 머리 추종 검사.
비활성 헤어는 제약 평가 대상이 아니므로 추종 위치 검사를 활성 헤어에 적용한다.
실제 GPU 렌더의 좌우 50도/아래 시점과 후면, Editor 미리보기의 정면/측면/상부에서 확인했다.
검증: output/LiliP09FrontFit/validation.txt, LiveSkin/*.png, face_before.json.
백업: output/LiliP09FrontFit/backup/ — 커밋되지 않은 사용자 상태 보존용.
Play Mode·MagicaCloth 물리 및 최종 사용자 외형 승인은 대기. 일반 모델/Editor 변경으로 Player 빌드는 생략.
다른 캐릭터 씬이 열려 있어 씬 전환·저장은 하지 않았다. Lili 기본/대체 프리팹과 공용 얼굴 프로필에 반영했다.

아래 r04 및 이전 내용은 제작 이력이다. 현재 헤어 값은 r05를 우선한다.

Lili 기반 P09 얼굴 모델 — r04 (두피 복원·Hair 부착 위치 조정)

현재 결과는 이 r04를 기준으로 한다. 아래 r03/r02/r01은 이전 제작 기록이다.
repair_seams.py에서 P09 두피 표면으로의 투영을 제거했다. Lili 조형 원본의 상부 좌표를
1μm 이내로 보존하며, 기존 목 복원·경계 연결·웨이트 보정은 유지한다.
Hair 03은 기본 자세 기준 아래 1cm·뒤 1cm, Hair 11은 앞머리가 눈을 덮지 않도록 뒤 1cm 이동했다.
위치는 Lili_P09_Face.asset의 hairAttachments에 저장한다. 머리 본 로컬 좌표에서
03=(0.01,-0.01,0), 11=(0.01,0,0)이며, 회전·스케일·공유 헤어 메시·바인드 포즈는 변경하지 않았다.
P09FaceMeshProfileSO.Apply는 모든 얼굴/헤어 경로를 확인한 뒤 절대 오프셋을 적용한다.
제약을 잠가 머리 애니메이션 중 부착 위치를 유지하며 반복 적용해도 이동량은 누적되지 않는다.
기본/대체 프리팹과 열린 검수 씬 인스턴스의 반영을 확인했다. 검수 씬 자체는 저장하지 않았다.

재현: repair_seams.py → import_face.csx → fit_hair.csx → validate_model.csx/validate_seams.csx
→ capture_hair_fit.csx → export_final.csx → finalize_blend.py.
fit_hair.csx는 이번 Lili 전용 저작 절차이며 기존 에셋의 GUID/fileID를 유지한다.
기존 에셋 갱신 전에는 output/LiliP09HairFit/backup처럼 커밋되지 않은 상태를 백업한다.
다른 헤어에는 이번 03/11 피팅값을 임의로 적용하지 않는다. 새 조합은 별도 검수가 필요하다.

AI 자동 검사: Unity 6000.3.21f1 컴파일 오류 0, P09FaceMeshProfileTests 5/5 통과.
표정 스키닝 72회, 생활 모션 12회, 헤어/의상 4조합, 목·표정 333회 통과.
두피 원본 위치 보존 4,612정점, 상부 삼각형 546개 뒤집힘 0.
목 연결 최대 오차 0.000000169m. 6,499정점/77본/36표정 및 얼굴 메시 GUID 유지.
프리팹 diff는 Hair 03/11 위치·제약 오프셋·잠금 각 3항목뿐이다.
정면/좌우/후면/위아래 Editor 샘플과 저장된 프리팹의 실제 GPU 스킨 렌더를 확인했다.
Play Mode·물리·최종 외형 승인은 사용자 테스트 대기. Player 빌드는 일반 모델/Editor 변경으로 생략.
근거: output/LiliP09HairFit/validation.txt, seam_validation.txt, LiveSkin/*.png.
Blender 제작 원본의 Lili_P09_Final도 갱신했다. 숨겨진 과거 조형 오브젝트는 최종 산출물이 아니다.

Lili 기반 P09 얼굴 모델 — r03 (이마 두피 돌출 수정)

r03 현재 결과
r02의 두피 정상 판정은 사용자 근접 화면으로 반려되었다. 목 연결 검사 통과는 두피 외형 검증이 아니었다.
SkinHead_Female을 완전한 두피로 오인한 것이 원인이다. 실제로는 얼굴 앞면이 열린 뒤통수 캡이며,
r02는 대상 455정점 중 237정점에서 투영에 실패해 이마와 주변 두피가 불연속으로 늘어났다.
원본 P09 얼굴과 뒤통수 캡을 합친 닫힌 표면으로 투영하고, 옆면의 변형 마스크도 연속적으로 바꿨다.
현재 피팅 491정점 중 투영 누락 0, 상부 삼각형 546개 뒤집힘 0을 저장 전 assert로 검사한다.
게임 메시에서 r02 대비 위치 변경은 269정점이다. 높이 1.395m 아래 위치, 삼각형·웨이트는 동일하다.
메시 GUID/fileID, 6,499정점/36표정/77본, 기존 목 연결을 보존했다.
헤어/무채색 두피의 좌우·측면·위/아래 근접 캡처 18장과 저장 후 실제 GPU 렌더 2장을 확인했다.
목·표정 333회, 프리팹 표정 72회, 생활 모션 12회 및 헤어/의상 4조합 검사는 통과했다.
Play Mode와 사용자 외형 승인은 별도 대기다. r03 확인 항목은 TESTS.txt의 R00을 따른다.
근거: output/LiliP09ScalpFix/의 Before_*, Clay_*, Hair_*, Low_*, GPU_* 이미지와 검사 결과.
수정 전 백업: output/LiliP09ScalpFix/backup/manifest.json. 새 근접 촬영: capture_scalp.csx.
아래 r02/r01 기록보다 이 r03 결과와 수정된 repair_seams.py를 우선한다.

r02 기록 (두피 외형 판정은 r03에서 정정)
r01에서 제거했던 342정점은 목뿐 아니라 턱 아래·귀 뒤 연결면까지 포함했다.
이 면을 복원하고 P09 몸체의 16개 경계 정점에 40삼각형으로 연결했다.
접합부의 위치·웨이트·노멀은 실제 Female_Body_Chest를 기준으로 한다.
압축된 목의 아래쪽은 P09 목 곡면으로 투영하고 삼각형 보간으로 웨이트를 옮겨 돌출을 줄였다.
P09 원본 얼굴과 몸의 뒤쪽 목 웨이트도 일부 달라 위치만 복사하면 회전 중 틈이 생겼다.
UV 경계는 정점을 분리하되 위치·웨이트·표정을 공유하고 노멀을 맞춘다.
두피는 열려 있는 P09 얼굴 앞면 대신 SkinHead_Female 전체 표면을 기준으로 피팅했다.
얼굴 정면 조형·눈 텍스처·P09 공용 헤어와 몸체 에셋은 유지한다.

현재 게임용 메시: 6,499정점 / 36표정 / 77본 / 5재질 슬롯.
메시 GUID 90a0b7f5a27351f43ba8d6e6b90c1364, fileID 4300000 보존.
기본·대체 프리팹과 빌더 프리셋은 같은 메시 참조로 수정본을 사용한다.
미저장 검수 씬은 저장하거나 교체하지 않았다. 런타임/빌더 C# 변경 없음.

r02 재현: export_face.csx → sculpt_face.py → inspect_parts.py → repair_seams.py
→ import_face.csx → validate_seams.csx/validate_model.csx → export_final.csx → finalize_blend.py.
repair_seams.py는 Blender background로 실행하며 scalp.bin과 원본 source.bin을 읽는다.
repair_sculpt.bin/repair_topology.bin을 import_face.csx가 사용하므로 예전 목 절단 결과가 재생성되지 않는다.
기존 .blend의 원본/초기 조형 오브젝트는 보관하고 Lili_P09_Final을 현재 메시로 갱신했다.

r02 검증 결과
- 저장된 메시: 목 9자세 × 중립/36표정 = 333회 검사 통과.
  몸체 경계 최대 오차 1.685874E-07m, UV 경계 최대 오차 5.230286E-07m.
  예상 밖 열린 목 경계 0, 몸체 연결 경계 16변.
- 기본/대체 프리팹: Missing Script/재질/외부 본 누락 0, Humanoid 유효.
  표정 72회, Idle/Walk 12표본, 헤어03/11 × 의상03/05 4조합, 프리셋 재로드 통과.
- 장비 없이 Hair03/11, 중립/고개 들기, 정면·측면·후면 Editor 캡처 12장.
  Hair03 실제 SkinnedMeshRenderer GPU 캡처 2장. AI 시각 점검이며 사용자 미술 승인과 구분한다.
- 메시 메모리 6,103,448 bytes. 기본 35,735삼각형, 대체 39,050삼각형.
- Play Mode·전체 헤어/장비·물리 검증과 사용자 미술 확인은 대기. Player 빌드 미수행.
- Console에 실행 코드의 RenderTexture 해제 및 137표정 메시의 -1 인덱스 오류가 남아 있다.
  현재 Lili는 36표정이며 해당 발생 작업은 식별하지 못했다. Console 전체 오류 0으로 보고하지 않는다.

검증 근거: output/LiliP09Repair/seam_validation.txt, model_validation.txt, Final_*.png, GPU_*.png.
수정 전 커밋되지 않은 파일 백업: output/LiliP09Repair/backup/manifest.json 및 파일 사본.
아래 r01 제작 기록의 수치·목 절단 절차·검증은 과거 기록이며 현재 결과는 위 r02를 우선한다.

제작 범위
기존 Lili의 플레이어·적·스토리 NPC와 분리된 신규 모델 및 P09Builder 프리셋이다.
기존 Lili 교체 또는 신규 월드 인물의 역할·배치 대상은 확정하지 않았다. ActorDefinition, NpcData,
대화, 전투 데이터, 기존 월드 씬을 이 작업에서 변경하지 않았다. 이름은 제작 식별자이며 새 스토리 설정이 아니다.
원본과 기존 사용자 변경은 유지했고 커밋하지 않았다.

사용
1. 통합 툴 런처에서 P09 캐릭터 빌더를 연다.
2. 프리셋 불러오기 → Assets/10.Datas/Actor/ExternalModels/Lili_P09_BuildPreset.asset
3. 외형 탭의 교체 얼굴 메시가 Lili_P09_Face인지 확인한다. 헤어·의상을 변경해도 유지된다.
4. 독립 모델: Assets/03.Prefabs/Characters/ExternalModels/Lili_P09Compatible.prefab
5. 검수 씬: Assets/01.Scenes/Tests/Equipment/Lili_P09_Review.unity
   기본 튜닉/대체 방어구 모델 2개, 카메라 1개, 방향광 2개. Idle 자세의 모델 검수 씬이다.
   NPC 생활 행동을 실행하는 게임 통합 씬은 아니다.
6. 대체 프리셋: Assets/10.Datas/Actor/ExternalModels/Lili_P09_Alternate_BuildPreset.asset

제작 카드
원본: Assets/ExternalAssets/Character/lili/Prefabs/lili.prefab 및 Models/lili.fbx
원본 얼굴: face, 정점 8293, 재질 슬롯 7, 표정 132.
방향: 눈의 간격·높이·눈꼬리, 눈썹 높이, 코 돌출, 입 폭, 볼 폭, 턱 폭·높이를 연속 변형.
      어두운 갈색 머리와 녹색/호박색 홍채. 원본의 UV와 입안·속눈썹 토폴로지 활용.
최대 순수 조형 변위: 약 4.65 mm. 좌표·배율·두피/목 피팅과 별도로 측정.
실제 P09 여성 No Physics 베이스에서 얼굴 폭 비율 0.8931851, 본 순서 77개를 읽었다.
다온의 0.93 배율이나 원본 재질 슬롯을 복사하지 않았다.
좌표 측정: coordinates.txt. 조형 통계: sculpt_report.json.
Lili 목의 두 연결 부품 342정점을 제거하고 P09 목 110정점을 추가했다.
P09 목은 원래 UV·웨이트를 유지한다. 조형한 턱을 목 표면으로 투영하지 않는다.
최종 얼굴: 정점 6227, 본 77, 재질 슬롯 5, P09 표정 36.
원본 표정 132개는 Blender 제작 원본에 보관하고 Unity에는 사용하는 P09 계약만 남겼다.
일부 감정은 좌우 원본 표정을 합성한 근사 대응이다. 이름·순서 검사가 시각적 동등성 승인을 의미하지 않는다.
기본: 여성, Hair_03/Brown, Chest·Arm·Waist·Leg 03, 머리 방어구 없음, MagicaCloth 끔.
대체: Hair_11/Brown, Chest·Arm·Waist·Leg 05, 머리 방어구 없음, MagicaCloth 끔.
공용 재질: 기존 P09_Residents_ToonLook을 읽기 전용으로 재사용.
새 눈: 내장 ImageGen 편집, 프롬프트 imagegen_prompt.txt. 피부 텍스처는 원본 재사용.
원본 눈 재질의 보라색 발광은 프로젝트 소유 복제 재질에서 끄고 새 홍채를 적용했다.

재현 순서
Unity execute_code에서 각 .csx를 메서드 본문으로 실행한다. 새 메뉴나 런타임 클래스를 추가하지 않았다.
1. export_face.csx → source.bin 및 좌표 측정
2. Blender 5.2 background --factory-startup --python sculpt_face.py
3. Blender 내장 Python으로 inspect_parts.py → source_neck_indices.txt
4. Lili_JadeEyes.png를 Assets/08.Models/Characters/LiliP09/에 복사한 뒤 import_face.csx
5. 새 경로 최초 생성 시 build_model.csx. 이후 update_appearance.csx, build_alternate.csx
6. validate_model.csx, capture_*.csx. 검수 씬 최초 생성 시 create_review_scene.csx
7. export_final.csx → final.bin, Blender background로 finalize_blend.py
Blender 실행 파일: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe
사용자 Blender 문서를 건드리지 않도록 별도 background 프로세스를 사용했다.
build_model/create_review_scene는 기존 산출물이 있으면 중단한다.
import_face/update_appearance/build_alternate는 이 제작 전용 경로를 갱신한다.
추후 정규 캐릭터가 이 산출물을 공유하면 먼저 revision별 경로로 분리하고 백업해야 한다.

산출물
Lili_P09_Face.blend: 원본 정렬본, 새 조형본(132표정), 최종 Unity 목 연결본(36표정·77본 그룹).
Blender의 머티리얼은 저작용 미리보기이며 최종 lilToon 조명은 Unity에서 확인한다.
리그/Avatar/bindpose의 실행 원본은 Unity P09 베이스와 Lili_P09_Head.asset이다.
source.bin/sculpt.bin/final.bin: 재현 가능한 메시 교환 자료. Unity .asset이 게임용 스킨 메시다.
Lili_JadeEyes.png와 imagegen_prompt.txt: 프로젝트에 복사된 생성 텍스처와 정확한 프롬프트.
output/LiliP09/: 원본/수정본 0·35·90도, 전신/후면, 표정, 보행, 목 회전, 직접 스킨 렌더.
LiveSkin_*: 베이크 미리보기 오브젝트를 제거한 실제 SkinnedMeshRenderer의 Unity 렌더.
NewFace/FullBody/Expression/Walk/HeadTurn: 기존 P09PosePreview의 Editor 모션 샘플 렌더.
Diagnostic/Native 이미지는 피팅 중간 조사 자료이며 최종 결과가 아니다.

검증 결과 (Unity 6000.3.21f1)
구현 반영: 신규 모델·얼굴 프로파일·기본/대체 BuildPreset·검수 씬 저장 및 재로드 완료.
정규 월드: 기존 Lili/ActorDefinition/SceneEntityId를 변경하지 않음. 배치 대상 미확정.
Editor: Missing Script·누락 재질·외부 본 참조 0, 유효한 Humanoid Avatar.
        36표정 순서·웨이트 합·유한 좌표·UV/노멀/탄젠트 채널 통과.
        두 모델 표정 BakeMesh 72회, Idle/보행 표본 12회, 얼굴 bounds 이탈 0.
        헤어 03/11 × 의상 03/05 네 조합 얼굴 유지, 무효 경로 부분 적용 방지, 프리셋 재로드 통과.
자동 테스트: P09FaceMeshProfileTests 3/3 통과(기존 Daon fixture). 신규 Lili 검사는 validate_model.csx.
Unity Console: error CS 조회 0, Lili 관련 Error 조회 0. 새 런타임/Editor C# 파일 변경 없음.
미술: AI가 정면/35도/측면/후면, 눈 감기·미소·아/오, 목 ±35도, 보행 샘플을 확인.
      최종 사용자 승인 대기. 모든 36감정의 미술 검수가 완료된 것은 아니다.
GPU: LiveSkin 정면/35도/측면에서 실제 SkinnedMeshRenderer 표시 확인.
Play Mode: 게임 등장·대화·립싱크·런타임 시선·카메라 이탈/복귀는 사용자 테스트 대기.
Player: 일반 모델 에셋 작업이며 모듈/플랫폼 변경 없음. 이번 작업에서는 빌드하지 않음.
물리: 긴 머리·투구·망토·MagicaCloth 조합 미검증. Player/Enemy 무기·GAS·모션 통합 미수행.
성능: 기본 활성 렌더러 6/삼각형 35265/재질 슬롯 13, 대체 7/38580/13.
      얼굴 런타임 Mesh 메모리 5,866,168 bytes. 군중 수·하드웨어별 프레임 측정은 미수행.
상세 검사: output/LiliP09/validation.txt, TESTS.txt.

범위 밖 발견
Console에서 기존 Daon_P09_Head/Female_Face_02의 vertex stride 불일치 오류 3건이 확인되었다.
Lili 검사와 분리해 보고하며 이 작업에서 Daon 공유 메시를 수정하지 않았다.
작업 중 활성 씬 전환과 NPC_Myomyo 프리팹의 활성화 값 변경도 감지했다.
직접 수정 대상이 아니며 사용자 변경일 수 있어 보존했다.
