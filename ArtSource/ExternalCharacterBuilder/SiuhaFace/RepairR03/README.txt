Siuha P09 R03 — 얼굴 대비 두상·헤어 비율 및 Review 씬

대상 확인
요청 스크린샷의 대상은 Siuha_P09Compatible / Hair03 / Armor03이다.
현재 얼굴 프로필은 Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset이다.
기본·대체 프리셋 및 Siuha_P09_Review.unity에서만 해당 프로필·프리팹의 참조를 확인했다.
다른 인물의 미술 승인이나 Lili 피팅 수치를 이번 조형에 사용하지 않았다.
정규 Player/NPC 데이터와 월드 배치 변경은 이번 씬 개선 범위에 포함하지 않았다.

변경
부착 위치와 배율만 조정한 4개 후보를 먼저 비교했다. 기존 윗두상이 헤어를 관통해 채택하지 않았다.
Blender 5.2.2 background에서 R02를 입력으로 연속적인 조형을 적용했다.
얼굴 아래쪽 폭은 최대 7% 넓히고 윗두상의 폭·깊이는 별도로 줄였다.
정수리 1.508349 → 1.483799m, 약 24.55mm 감소. 몸체 경계 16정점은 정확히 유지했다.
눈을 얼굴에 비해 더 크게 만드는 별도 조정은 하지 않았다. 얼굴과 눈·눈썹·입 및 36표정에 같은 변형을 적용했다.
Hair03/11도 같은 두상 변형을 기준으로 낮추고 앞뒤 폭을 줄였다.
옆머리 안쪽 가닥을 최대 6mm 바깥으로 조정해 눈가·볼을 과도하게 가리지 않게 했다.
뒤통수의 긴 가닥에는 곡면 여유를 유지해 피부가 드러나는 부분을 수정했다.
이전 UV·삼각형·본·웨이트·bindpose와 피부 재질은 유지했다.
과정 중 정점별 투영은 가닥 면을 찌그러뜨려 폐기했다. 최종 결과는 연속 변형을 사용한다.

반영
Assets/08.Models/Characters/SiuhaP09/Siuha_P09_Head.asset
Assets/08.Models/Characters/SiuhaP09/Siuha_Hair_03_Fitted.asset
Assets/08.Models/Characters/SiuhaP09/Siuha_Hair_11_Fitted.asset
기존 Mesh GUID와 프리팹 연결을 유지하여 두 BuildPreset과 두 프리팹에 반영했다.
Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity에 Siuha_P09_BodyOnly를 추가했다.
BodyOnly는 기존 프리팹 인스턴스의 가시성 override로 Face02/Chest/Leg만 표시한다.
Armor·Hair·팔·손발 보조 파츠·무기·장식은 숨겼다. 이 가시성 변경은 프리팹에 Apply하지 않았다.
기존 두 모델의 위치·회전은 보존하고 검수 카메라를 3개 모델을 볼 수 있는 구도로 조정했다.
씬을 저장한 뒤 PreviewScene으로 다시 읽어 BodyOnly의 3개 렌더러와 얼굴 참조를 확인했다.
작업 중 다른 Review 씬이 열려 있어 최종 캡처는 씬 이름과 에셋 경로를 고정해 별도 PreviewScene에서 만들었다.
범용 이름의 검수 카메라를 검색한 캡처는 사용하지 않는다. Evidence/review_capture_identity.txt가 대상 기록이다.

제작 원본과 재현
Siuha_R03_HeadHairNeck.blend: 얼굴 36 Shape Key, 본 그룹, 헤어 웨이트, 텍스처 포함.
R02 폴더의 repaired_face.bin 및 Siuha_R02_HeadHairNeck.blend를 원본 입력으로 보존한다.
1. Blender --background --factory-startup --python fit_proportions.py
2. Unity execute_code에서 import_proportions.csx 실행.
3. update_review_scene.csx 실행. 열린 대상 씬에 미저장 변경이 있으면 저장 전에 중단한다.
4. capture_review_scene.csx로 저장된 Siuha 씬을 격리해서 캡처한다.
   PreviewRenderUtility 카메라에는 CopyFrom을 쓰지 않는다. 전용 scene culling mask가 덮이면 다른 열린 씬이 찍힌다.
5. 상위 validate_seams.csx / validate_model.csx 및 R02/validate_fitted.csx의 출력 경로를
   output/SiuhaP09RepairR03로 바꿔 검사한다. 마지막 도구는 이번에도 헤어 2개 계약을 그대로 검사한다.
6. 상위 capture_seams/expressions.csx, R02/capture_diagnostic.csx도 R03 출력 경로로 실행한다.
재실행 전 현재 수정 파일을 백업한다. 초기 백업 28개와 SHA256은 backup/manifest.json에 있다.
R02/R01 import를 단독 실행하면 이번 조형이 되돌아간다. 최신 제작 순서는 위 순서를 따른다.
원본 ExternalAssets와 다른 인물의 에셋, 런타임 코드·공용 빌더 코드는 변경하지 않았다. 커밋하지 않았다.

검증
목 9자세 × 중립/36표정 333회: 몸체 경계 최대 오차 1.685874E-07m, UV 분리 경계 최대 오차 3.690066E-07m.
두 프리팹 표정 72회, Idle/Walk 12표본, 헤어·의상 4조합, 프리셋 재로드 통과.
헤어 24표본의 bounds 이탈 0, 원본 UV·삼각형·웨이트 유지, 공용 피부 재질 잔존 0.
Missing Script/본/재질 누락 0, Humanoid 유효. 최종 Console Error 조회 0.
정면 전후·측면·후면·맨목 측광·눈 감기·입 벌림·고개 숙임을 AI가 Unity 렌더로 확인했다.
자동 검사와 AI 렌더 확인은 사용자 미술 승인을 대신하지 않는다.
Play Mode 실제 추종·대화·시선·컬링 재진입·MagicaCloth·전체 헤어 카탈로그·Player는 미수행이다.
동일 카탈로그 두 헤어의 모델 검수 범위이며 다른 헤어로의 확장을 검증했다고 보고하지 않는다.

사용자 확인
R03-01: Siuha_P09_Review에서 기본·대체 모델의 정면과 옆면을 확인한다.
         얼굴 대비 정수리 높이, 이마·귀·옆머리 위치가 자연스러워야 한다.
R03-02: BodyOnly의 앞·뒤·턱 아래를 확인한다. 목 연결 틈이나 이전 색상 단절이 없어야 한다.
R03-03: 빌더에서 기존 두 Siuha 프리셋을 다시 불러와 Hair03/11을 선택한다.
         씬과 같은 비율·얼굴·피부를 유지해야 한다.
R03-04: Play Mode에서 목 회전·눈 감기·입 벌림·이동·카메라 재진입을 확인한다.
         헤어 추종 지연·피부 관통·부품 소멸이 없어야 한다. 현재 사용자 테스트 대기다.
회신 형식: 항목 ID / 통과·실패·미수행 / 각도·헤어·표정 / 실제 증상.
