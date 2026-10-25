LianLian P09 r03 — 헤어 부착 위치·폭 재조정 / BodyOnly 검수

최신 기준은 이 r03 기록이다. 아래 r02의 헤어 검수는 사용자 지적에 따라 재검토했다.
기존 얼굴·두피 5,305정점의 위치, 36표정 위치 델타와 본 웨이트를 보존한다.
헤어를 맞추려고 두피를 늘리거나 P09 두상으로 투영하지 않는다. 눈 크기도 r02와 동일하다.

헤어 설정 (Head 본 로컬)
- Hair03: positionOffset=(0.015,-0.018,0)m, localScale=(0.88,1,0.95).
- Hair11: positionOffset=(0.015,0,0)m, localScale=(0.88,1,0.95).
- x 배율은 앞뒤 폭, z 배율은 좌우 폭이다. 높이는 유지한다.
- 원본 공유 Hair 메시·UV·본·bindpose는 수정하지 않았다. fittedMeshes는 비어 있다.
- 얼굴 프로파일과 두 프리팹 및 Review 씬에 저장되어 빌더 재적용에도 유지된다.

Body 전용 검수에서 추가로 수정한 결함
- 원본의 열린 뒤통수 42변 경계에 630삼각형을 추가했다. 기존 두피 위치를 바꾸지 않고 누락 면을 닫는다.
- 뒤통수 UV는 기존 알베도의 연속된 피부 영역을 사용하고 경계 색을 대응했다. 새 텍스처를 만들지 않았다.
- 목 연결면이 몸통의 앞/뒤 UV 섬 사이를 가로지르던 문제를 UV 정점 4개 분리로 수정했다.
- 마지막 몸통 경계 16정점의 위치·웨이트와 기존 얼굴 표정을 보존했다. 노멀·탄젠트는 새 연결면까지 다시 계산했다.
- 최종 5,646정점 / 36표정 / 77본 / 3재질 슬롯. 얼굴·목·뒤통수 순서다.

검수 씬
Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity
기존 기본/대체 모델을 유지하고 LianLian_P09_BodyOnly를 (1.95,0,0)에 추가했다.
BodyOnly는 Female_Face_02 / Female_Body_Chest / Female_Body_Leg만 표시한다.
표시 override는 씬 인스턴스에만 있으며 정규 프리팹에 적용하지 않았다.

재현
1. R02 원본 상태는 head_before_completion.bin에 보관되어 있다.
2. Blender background에서 complete_skull.py: 뒤통수 면·목 UV 분리 → completed_head.bin.
   기존 정점/표정 보존, 목 UV 섬 횡단 금지, 뒤통수 경계 닫힘을 assert로 검사한다.
3. Unity에서 import_completed_head.csx → fit_hair.csx.
4. validate_hair_fit.csx로 검수 씬 부착 값을 저장하고 verify_reload.csx로 디스크 재로드 검사.
5. export_final.csx → Blender finalize_blend.py로 최종 게임 메시를 .blend에 보관한다.
import_face.csx만 재실행하면 R02 메시로 돌아가므로 위 완료 단계를 이어서 실행해야 한다.

검증
- 표정 72회, Idle/Walk 12표본, 헤어/의상 4조합, Humanoid/본/재질/Missing Script 검사 통과.
- 목 9자세 × 중립/36표정 333회: 몸체 접합 최대 오차 1.685874E-07m, UV 분리 경계 0m.
- 디스크 씬 재로드: 모델 3개, BodyOnly 활성 렌더러 정확히 3개, 헤어 위치·배율 유지.
- 헤어 부착 반복 적용 3회: 누적 오차·얼굴 정점 변경 0. 기존 .meta 9개 SHA256 동일.
- Unity GPU 렌더: 기본/대체/BodyOnly 정면·70·110·180도, 헤어라인 좌우 확대, BodyOnly 측면 확인.
- 기본/대체 고개 ±35도 추종 검사와 캡처. Play Mode/Player/물리/전체 카탈로그/사용자 미술 승인은 미검증.
증거: output/LianLianHairFitR03/Final, HairlineFront, BodyUv, HeadTurn 및 validation.txt, seam_validation.txt, reload.txt.
복구: output/LianLianHairFitR03/backup/manifest.json. 기존 미커밋 작업의 파일별 SHA256 백업.

--- 이전 제작 기록 (r03 우선) ---
LianLian P09 r02 — 두피 복원 / 헤어 위치 조정

사용자가 요청한 방식에 맞춰 헤어용 두피 투영 변형을 제거했다.
원본 정렬 머리에 눈·눈썹·코·입·턱 조형만 적용하며, 헤어를 맞추려고 머리 표면을 늘리지 않는다.
최종 메시 654정점에서 기존 피팅 변형을 복원했다(최대 0.02843521m).
UV·삼각형·36표정 위치 델타·웨이트·본 이름은 r01과 동일하며 .meta 9개를 보존했다.
기존 658개 피팅 정점 수는 원본 조형 단계의 값이고, 654개는 최종 게임 메시에서 비교한 값이다.

헤어는 기존 P09FaceMeshProfileSO.hairAttachments로 부착 위치만 조정한다.
Hair03: 머리 본 로컬 (0.01, -0.025, 0)m = 뒤 1cm / 아래 2.5cm.
Hair11: 머리 본 로컬 (0.01, 0, 0)m = 뒤 1cm.
둘 다 localScale=(1,1,1). 공용 헤어 메시·바인드 포즈·머리 크기는 유지한다.
두 프리팹과 얼굴 프로파일, 검수 씬, Blender 원본에 반영했다.
재현 시 import_face.csx 다음에 fit_hair.csx를 실행한다.

r02 검사
- 프리팹 저장 후 부착 위치 확인, 3회 반복 적용 누적 오차 0, 적용에 따른 얼굴 정점 변화 0.
- 표정 72회·생활 모션 12표본·헤어/의상 4조합 재검사 통과.
- 목 9자세 × 중립/36표정 333회 통과. 접합 오차 1.685874E-07m, UV 경계 오차 0.
- 실제 GPU 스키닝 8각도 및 위쪽 시점 8각도, 고개 회전 ±35도 Editor 캡처 확인.
- 최종 Console error 조회 0. Play Mode와 사용자 외형 승인은 별도 미검증.
근거: output/LianLianHairFit/validation.txt, seam_validation.txt, hair_validation.txt, preservation.json.
현재 이미지: output/LianLianHairFit/LiveSkin/LianLian_P09Compatible_0.png.
백업: output/LianLianHairFit/backup/manifest.json. 커밋되지 않은 기존 작업 복구용으로 보관한다.
이하 r01의 두피 피팅 설명과 이미지보다 r02 결과를 우선한다.

--- r01 제작 기록 ---
LianLian 기반 P09 모델 r01

신규 모델과 P09Builder 프리셋 제작을 반영했다. 눈 영역 중심부의 가로를 90%, 세로를 78%로
조형하고 눈꺼풀·안구·속눈썹 및 모든 원본 표정에 같은 변형을 적용했다. 눈썹·코·입·턱도 조정했다.
이 수치는 조형 함수의 중심 영역 배율이며 얼굴 전체 또는 화면상 눈 면적의 일괄 배율은 아니다.
기존 Lian 플레이어·몬스터·NPC 교체와 월드 배치는 지정되지 않아 수행하지 않았다.

바로 사용
통합 툴 런처 → P09 캐릭터 빌더 → 프리셋 불러오기:
Assets/10.Datas/Actor/ExternalModels/LianLian_P09_BuildPreset.asset
외형 탭 교체 얼굴 메시: LianLian_P09_Face.
헤어·의상을 변경해도 이 얼굴을 유지한다. ExternalModelPrefab은 비어 있는 P09 네이티브 경로다.

모델: Assets/03.Prefabs/Characters/ExternalModels/LianLian_P09Compatible.prefab
대체: Assets/03.Prefabs/Characters/ExternalModels/LianLian_P09_Alternate.prefab
대체 프리셋: Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Alternate_BuildPreset.asset
검수 씬: Assets/01.Scenes/Tests/Equipment/LianLian_P09_Review.unity
씬에는 기본·대체 모델, 카메라, 방향광 2개가 있다. 실제 생활 AI·대화 실행 씬은 아니다.
얼굴 에셋: Assets/08.Models/Characters/LianLianP09/LianLian_P09_Head.asset
얼굴 프로파일: Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset

원본 및 제작
전체 LianLian.prefab의 Body를 사용했다. FaceOnly.prefab은 일부 본 참조가 null이다.
원본 5,281정점·112표정, 실제 가중 본 Head. 실제 P09 여성 베이스에서 배율 0.8812599와
77개 본 순서·bindpose를 읽었다. 원본 덮개 26삼각형을 제거하고 목 26→16 경계에 42삼각형을 연결했다.
P09 몸의 목 경계 위치·웨이트·노멀을 복사하여 목 회전 시 틈이 나지 않게 했다.
두피는 P09 원본 얼굴과 뒤통수 캡을 합친 표면에 맞췄다. 두피 658정점 피팅, 투영 누락 0.
최종 얼굴 5,305정점·77본·36표정·2재질 슬롯. 원본 표정 112개는 Blender 원본에 보관했다.
36개 표정은 실제 원본 델타 대응이다. 일부 감정은 근사 대응이며 시각적 동등성을 보장하지 않는다.
원본 눈은 본이 아닌 Lookat BlendShape를 사용한다. 게임 메시의 P09 36표정 계약에는 시선 shape가
포함되지 않아 P09 eye_L/eye_R 본으로 안구만 움직이는 기능은 지원하지 않는다. 고개 회전은 지원한다.

텍스처: 내장 ImageGen 참조 편집. 볼 밴드 제거, 갈색 눈썹, 호박색 홍채.
프롬프트: imagegen_prompt.txt. 사용본: Assets/08.Models/Characters/LianLianP09/LianLian_FaceAlbedo.png.
얼굴 재질은 몸체 ToonLook의 조명 설정을 복제해 원본 분홍 SDF 그림자를 제거했다.
공용 P09 메시·재질은 수정하지 않았다.
기본: 여성, 갈색 Hair03·의상03. 대체: 갈색 Hair11·의상05. 투구 없음, MagicaCloth 끔.

재현
Unity execute_code에서 .csx를 메서드 본문으로 실행한다. 새 메뉴·런타임 C# 추가 없음.
1. export_face.csx → source.bin / scalp.bin / coordinates.txt
2. Blender background --factory-startup --python sculpt_face.py → sculpt.bin / topology.bin / .blend
3. 알베도를 Assets/08.Models/Characters/LianLianP09/에 복사 → import_face.csx → match_skin.csx
4. 최초 제작에만 build_model.csx → build_alternate.csx → create_review_scene.csx
5. validate_model.csx / validate_seams.csx / verify_reload.csx 및 capture_*.csx
6. export_final.csx → Blender background finalize_blend.py
Blender: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe (5.2.2 LTS).
사용자 Blender 문서는 건드리지 않고 별도 background 프로세스를 사용했다.
.blend에는 정렬 원본, 조형본, 최종 Unity 메시와 표정·본 그룹이 있다. P09 전체 캐릭터는 Unity 프리팹이다.
final.bin은 최종 위치·UV·삼각형·표정·웨이트 교환본이며 실제 bindpose는 Unity Mesh 에셋에 있다.
재제작 시 이미 공유 중인 출력은 먼저 revision 경로를 분리하거나 파일별 백업 후 갱신해야 한다.

검증 결과 — AI 수행
- 기존 P09FaceMeshProfileTests EditMode 5개 통과, 실패·스킵 0.
  이 테스트는 공용 프로파일 계약 검사이며 LianLian 미술 검수와 구분한다.
- LianLian 기본/대체 모델: Missing Script·재질·외부 본 누락 0, Humanoid 유효.
- 표정 스키닝 72회, Idle/Walk 12표본, 헤어03/11 × 의상03/05의 4조합, 프리셋 재로드 통과.
- 목 9자세 × 중립/36표정 333회: 몸체 연결 최대 오차 1.685874E-07m, UV 경계 오차 0.
- 목 연결 경계 16변, 예상 밖 열린 목 경계 0, 검사 표본의 얼굴 bounds 이탈 0.
- 실제 Unity 정면·35도·측면·전신·후면·눈 감기·미소·발음·걷기·목 좌우 회전 캡처.
- 별도 프리뷰 장비 없는 목/두피 12장. 실제 SkinnedMeshRenderer GPU 렌더 3장.
- 검수 씬 저장 후 재로드: 두 모델의 얼굴 프로파일, 카메라·조명·Missing Script 검사 통과.
- 최종 Unity Console error 조회 0. 에디터 실행 코드 컴파일·에셋 임포트 정상. 별도 전체 재컴파일은 안 했다.
- 원본 LianLian 및 기존 Lian 플레이어·몬스터 프리팹 git diff 없음.
- 런타임 얼굴 메시 메모리 2,196,672 bytes.
  기본 활성 렌더러 6 / 34,541삼각형 / 10재질 슬롯, 대체 7 / 37,856 / 10.
  이는 자산 계수이며 실제 NPC 군중 성능 통과를 의미하지 않는다.

검증 근거
output/LianLianP09/validation.txt, seam_validation.txt, reload.txt, editmode_tests.json.
Compare_Before_0.png / Compare_After_0.png: 같은 P09 바디·헤어·새 알베도·조명·카메라에서 조형 전후 비교.
Before는 P09 좌표로 정렬한 원본 머리 정점이며 두피 피팅 전이라 헤어 안착도 달라진다.
SourceFace_*.png는 원래 LianLian 프리팹 캡처다. 비교 이미지와 혼동하지 않는다.
LiveSkin_*.png: 메시 베이크 프리뷰를 해제한 실제 GPU 스키닝 렌더.

남은 확인
사용자 미술 확인·Play Mode·게임 등장 경로·대화/립싱크·물리·전체 헤어/투구·군중 성능은 미검증.
무기·GAS·모션의 Player/Enemy 통합은 범위에 없다. Windows Player 빌드는 독립 모델 자산 제작으로 미수행.
TESTS.txt의 항목에 따라 확인한다. 기존 사용자 씬과 변경 파일은 유지했고 커밋하지 않았다.


