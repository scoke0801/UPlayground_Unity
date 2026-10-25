사용 제약: 두피·뒤통수 일부가 열린 얼굴 메시이므로 이를 가리는 헤어가 필수다.
민머리·열린 부위를 노출하는 헤어는 현재 지원하지 않는다. 지원하려면 두피·뒤통수 추가 저작과 재검수가 필요하다.
P09 빌더 연결: Bokusei_P09_BuildPreset(Hair13), Bokusei_P09_Alternate_BuildPreset(Hair03).
두 프리셋은 Assets/10.Datas/Actor/ExternalModels/ 아래에 있으며 같은 Bokusei_P09_Face를 사용한다.
제약 상세: Assets/docs/EXTERNAL_MODEL_P09_CONVERSION_GUIDE.md의 '기존 모델의 명시적 사용 제약'.

현재 수정본: r05. 아래 볼 돌출 교정 결과는 CHEEK_RESULT.txt를 따른다. r04 얼굴과 r02 목 경계·피부톤을 보존했다.
아래 r01 기록의 목 오차·피부 재질 설명은 r02에서 갱신되었다.

Bokusei 기반 P09 독립 모델 / r01 / 2026-10-25

제작 카드
용도: 기존 플레이어를 보존하는 독립 모델. 새 NPC의 이름·배치 대상은 지정되지 않았다.
원본: Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/01_Prefab/Bokusei.prefab
원본 얼굴: Body, 8,455정점, 311 BlendShape, 3재질. 원본 눈 본의 중립 회전 확인.
방향: 차분한 청년 여행자. 조금 좁고 긴 눈매, 낮춘 눈썹, 볼 폭 축소,
      넓힌 턱과 입, 코 돌출 조정, 귀 폭 축소, 갈색 눈과 밝은 상아색 피부.
기반: P09_Human_No_Physics_Male Variant, 실제 남성 Face_01 기준.
      머리 본 높이 기준 배율 1.03937829, P09 77본 배열과 bindpose 사용.
기본 슬롯: 갈색 Hair_13, Chest/Arm/Waist/Leg 03, 모자·수염 없음.
대체 슬롯: 갈색 Hair_03, Chest/Arm/Waist/Leg 05.
목 연결: 원본 목 경계 36위치(중복 UV 정점 포함 37정점)를 P09 16위치 경계에 보간.
         P09 경계 웨이트를 보간하고 표정에서 목 끝을 고정. 겹친 연결 메시 없음.
재질: 새 ImageGen 얼굴 atlas, 원본 face-parts atlas, 캐릭터 전용 몸 피부 재질.
      새 피부 재질은 ToonLookProfile을 통해 프리셋에 보존.

산출물
Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09Compatible.prefab
Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09_Alternate.prefab
Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset
Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_BuildPreset.asset
Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Alternate_BuildPreset.asset
Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_ToonLook.asset
Assets/08.Models/Characters/BokuseiP09/ 아래 런타임 Mesh·텍스처·재질
Assets/01.Scenes/Tests/Equipment/Bokusei_P09_Review.unity

사용
통합 툴 런처에서 P09 캐릭터 빌더를 열고 Bokusei_P09_BuildPreset을 불러온다.
FaceMeshProfile이 연결된 상태로 헤어·의상 슬롯을 변경한다.
검수 씬에는 기본형과 대체형이 있다. 모델 프리팹에는 PlayerActor/NpcActor가 없다.
ActorKind.Npc는 이후 빌더 생성 시의 기본값이며 이번 결과에 NPC 런타임 통합을 의미하지 않는다.
기존 Bokusei 플레이어, GAS, 모션, ActorDefinition, 대화, SceneEntityId, 월드 배치는 변경하지 않았다.
월드/기존 플레이어 반영과 초상화 교체는 이번 독립 모델 범위에 포함하지 않았다.

제작 원본과 재현 순서
ImageGen 입력: 원본 Tx_Bokusei_Face.png. 정확한 프롬프트는 imagegen_prompt.txt.
Bokusei_Face_Albedo.png: 생성 원본 보관, 같은 파일을 Assets에 복사.
export_face.csx → inspect_neck.py → sculpt_face.py → import_face.csx → build_model.csx
→ repair_geometry.csx → build_alternate.csx → match_skin.csx → create_review_scene.csx
→ export_final.csx → finalize_blend.py.
csx는 Unity MCP execute_code의 메서드 본문이며 Editor에서 실행한다.
Python은 별도 Blender 5.2.2 LTS background 프로세스에서 실행한다:
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --factory-startup --python <script>
열린 Blender 문서를 초기화하지 않았다. MCP로 설치/작업 상태 확인 후 독립 CLI를 사용했다.
처음부터 재현할 때 새 revision 경로를 사용한다. 생성 단계는 기존 에셋 충돌을 거부한다.
repair_geometry/match_skin은 이번 제작본만 갱신하는 후속 단계다.

Bokusei_P09_Face.blend: 원본 정렬본, 새 조형 311표정, 최종 Unity 36표정·77본 그룹 메시.
source.bin/sculpt.bin/final.bin: 재현용 교환 자료. 게임용 본·bindpose 원본은 Unity Mesh와 P09 prefab.
Blender에는 제작용 얼굴과 웨이트 그룹을 보관한다. 완성된 전신 armature/의상 .blend가 아니다.
최종 Blender 재질은 저작용 미리보기이며 실제 lilToon 결과는 Unity 캡처로 확인한다.
manifest.json: 파일 경로·GUID·SHA256. 기존 에셋 교체가 없어 정규 에셋 백업은 불필요했다.

검증 결과
Unity 6000.3.21f1. 신규 런타임/Editor C# 코드·모듈·플랫폼 설정 변경 없음.
Editor: 기본/대체 프리팹 및 씬 재로드 확인. Missing Script·누락 재질·외부 본 참조 0.
        Humanoid Avatar 유효, 36표정 순서·UV·노멀·탄젠트·웨이트 합 검사 통과.
        표정 스키닝 72회, Idle/보행 12표본, 헤어 13/03 × 의상 03/05 4조합 통과.
        36표정 모두 실제 정점 델타 존재. 의미가 비슷한 원본 표정을 대응했으며 36감정 전체 미술 승인은 아님.
        목 ±35도/중립 경계 최대 오차 0.1494mm, 허용 0.5mm. 표정 목 경계 이동 0.
        생활 모션 얼굴 bounds 이탈 0, 프리셋 직렬화·무효 경로 부분 적용 방지 통과.
관련 EditMode: P09FaceMeshProfileTests 3/3 통과, 실패·건너뜀 0. 기존 Daon fixture의 공용 계약 검사.
              신규 Bokusei 데이터는 validate_model.csx와 validate_neck.csx로 별도 검사.
Console: error CS와 Bokusei 관련 Error 필터 결과 각각 0.
GPU: LiveSkin 정면/35도/측면 실제 SkinnedMeshRenderer 렌더 확인.
미술: AI가 정면/35도/측면/후면/전신, 눈 감기·미소·아/오, 목 회전·보행·대체 조합 확인.
      최종 사용자 승인 대기. 목의 재질/경계선은 극근접·측광에서도 확인할 것.
Play Mode: 실제 등장·대화·런타임 시선·립싱크·카메라 이탈/복귀 사용자 테스트 대기.
Player: 일반 모델 에셋 제작으로 모듈/플랫폼 변경 없음. Windows 빌드 미수행.
물리/전투: 긴 머리·투구·망토·MagicaCloth 동작, Player/Enemy 무기·GAS·충돌 미검증.
피부색: 프리셋의 기본 SkinColor 01을 기준으로 맞춤. 다른 피부색은 얼굴 재질까지 별도 조정 필요.
성능: 기본 활성 렌더러 6/삼각형 37,994/재질 슬롯 11, 대체 7/40,466/11.
      얼굴 런타임 Mesh 메모리 3,937,360 bytes. 군중 프레임·Player 메모리 측정은 미수행.
작업 전후 활성 씬 P09_LianLian_Review, 미저장 변경 없음. 기존 작업 파일과 열린 씬 보존. 커밋하지 않음.

검증 근거
output/BokuseiP09/Review.png: 저장 후 재로드한 검수 씬의 두 모델 Unity 렌더.
Source_*: 원본 얼굴. SourceAligned_* / NewFace_*: 동일 P09 카메라/조명에서 원본/수정본 비교.
LiveSkin_*: 실제 GPU 스킨 렌더. 나머지 캡처는 P09PosePreview의 Editor 베이크 표본.
FullBody_*, Expression_*, HeadTurn_*, Walk_*, AlternateFace_*.
validation.txt, neck_validation.txt, TESTS.txt 참조.

범위 밖 발견
기존 Daon_P09_Head/Female_Face_02의 vertex stride 불일치와 Daon 재질 속성 오류가 Console에 남아 있다.
Bokusei 관련 오류와 구분했으며 해당 공유 에셋은 수정하지 않았다.

