Siuha 기반 P09 모델 제작 — r01

결과와 사용
P09 빌더에서 Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset을 불러온다.
교체 얼굴 메시 필드는 Siuha_P09_Face이며, 헤어와 의상 변경 후에도 같은 얼굴을 유지한다.
기본 모델: Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab
대체 모델: Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09_Alternate.prefab
검수 씬: Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity

이번 산출물은 독립 모델과 제작 프리셋이다. 기존 Siuha 플레이어·보스의 교체 대상이나
새 NPC의 역할·배치 위치가 지정되지 않아 ActorDefinition·NpcData·전투·대화·월드는 연결하지 않았다.
검수 씬은 두 모델의 외형을 비교하는 씬이며 게임 NPC 행동 실행 씬은 아니다.
기존 ExternalAssets 모델, 공용 P09 재질과 사용자 변경 파일은 수정하지 않았다. 커밋하지 않았다.

제작 카드
원본: Assets/ExternalAssets/Character/Suiha/prefab/Suiha.prefab
원본 FBX: Assets/ExternalAssets/Character/Suiha/FBX/Suiha.fbx
사용자 표기는 Siuha, 실제 원본 폴더 표기는 Suiha다.
원본 얼굴: Body 렌더러, 11,271정점, 2재질 슬롯, 341 BlendShape.
원본 텍스처: Assets/ExternalAssets/Character/Suiha/Material/Texture/suiha_face.png
방향: 눈 주변 가로 최대 10%·세로 최대 20% 축소, 눈썹 높이·볼 폭·턱 폭과 높이·코 돌출·입 폭 조정.
원본 정면 홍채 정점 표본 높이 비율 0.783966. 마스크에 따라 부위별 실제 축소량은 다르다.
모든 원본 표정에 같은 공간 변형을 적용하여 눈꺼풀·안구·속눈썹의 대응을 유지한다.
순수 얼굴 조형 최대 변위: 4.8002mm. 두피 피팅 변위와 구분한다.
실제 P09 여성 No Physics 베이스에서 얼굴 폭 배율 1.007691과 77본 순서를 읽었다.
목: Siuha 20정점 경계와 실제 P09 몸체의 16정점 경계를 36삼각형으로 연결한다.
UV 분리 경계는 위치·표정·웨이트를 공유하고 몸체 경계의 웨이트·노멀을 복사한다.
두피: 원본 P09 얼굴 + SkinHead_Female 캡의 결합 표면에 투영. 눈 부품은 투영하지 않는다.
기본 슬롯: 여성, Hair03 / Brown01, Chest·Arm·Waist·Leg03, 머리 방어구 없음, 물리 끔.
대체 슬롯: Hair11 / Brown01, Chest·Arm·Waist·Leg05, 머리 방어구 없음, 물리 끔.
341개 원본 표정은 Blender 원본에 보존하고 게임 메시에는 P09 계약 36개만 담았다.
일부 감정 대응은 근사이며 36표정의 의미가 모두 원본 P09와 동일하다는 보장은 아니다.

ImageGen
FaceReference.png: source Unity 정면 렌더를 바탕으로 만든 조형 참고. 게임 적용 화면이 아니다.
Siuha_Face_Albedo.png: 기존 UV 배치를 기준으로 얼굴·귀·두피의 과한 분홍/흰색 차이를 줄인 편집본.
원본과 편집본은 별도 보존한다. 실제 게임 텍스처는 Assets/08.Models/Characters/SiuhaP09/에 복사했다.
imagegen_prompt.txt / texture_prompt.txt에 정확한 프롬프트를 보존했다.
얼굴 재질과 목·몸 재질은 Siuha 전용이다. 공용 P09 재질을 덮어쓰지 않는다.

재현
Unity execute_code에서 .csx를 메서드 본문으로 실행한다. 런타임 C#·빌더 구현 변경은 없다.
1. export_face.csx → source.bin / scalp.bin / coordinates.txt
2. Blender background --factory-startup --python sculpt_face.py → sculpt.bin / neck_topology.bin / .blend
3. validate_sculpt.py → sculpt_validation.json
4. import_face.csx → 프로젝트 소유 Mesh/Material/FaceMeshProfile
5. 최초 제작: build_model.csx → build_alternate.csx → update_appearance.csx
6. 생성 알베도를 프로젝트에 복사 → match_skin.csx
7. validate_seams.csx / validate_model.csx → output/SiuhaP09 검사 기록
8. capture_live_skin/full/expressions/walk/head_turn/seams.csx → 실제 Unity 렌더
9. export_final.csx → final.bin → Blender background로 finalize_blend.py
10. create_review_scene.csx → 검수 씬 최초 생성
실행 파일: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe (실제 5.2.2 LTS)
사용자 Blender 문서와 별도 background 프로세스를 사용했다.
첫 생성용 스크립트는 기존 모델/프리셋/씬이 있으면 중단한다. import·재질·외형 갱신은 이 제작 전용
경로를 갱신한다. 다른 정규 인물이 공유하기 시작하면 먼저 revision 경로로 분리하고 백업해야 한다.
MCP 연결이 끊긴 경우 실제 저장 파일과 상태부터 확인했으며, timeout만 보고 생성 작업을 반복하지 않았다.

산출물
Siuha_P09_Face.blend: 원본 정렬본, 새 조형본(341표정), 최종 메시(36표정·77본 그룹).
source.bin / sculpt.bin / final.bin: 원본·조형·최종 교환 데이터.
neck_topology.bin: 실제 두 경계의 대응과 연결 삼각형.
Unity .asset이 게임용 스킨 메시와 bindpose의 실행 원본이다. Blender는 조형/웨이트 보관용이며
P09 전체 스켈레톤·Avatar 실행은 Unity 프리팹을 사용한다. Blender 셰이더는 저작 미리보기다.
Evidence/: 최종 정면·35도·눈 감기·전신·맨목 캡처와 원본 정면 비교 근거.
output/SiuhaP09/: 그 외 측면/후면·표정·목 회전·보행·대체 조합 검사 이미지.

검증
Unity 6000.3.21f1 Editor에서 프리팹 저장 후 재로드, 프리셋 직렬화 및 얼굴 유지 확인.
정점/UV/노멀/탄젠트, 웨이트 합·본 인덱스, 유효 Humanoid, Missing Script/재질/외부 본 누락 0.
기본·대체 표정 BakeMesh 72회, Idle/Walk 12표본, Hair03/11 × Armor03/05 4조합 통과.
목 9자세 × 중립/36표정 333회: 최대 몸체 경계 오차 1.685874E-07m, UV 경계 오차 0m.
두피 376정점 투영 누락 0, 상부 330삼각형 뒤집힘 0. 모든 원본/수정 표정 유한 좌표.
게임 메시 11,307정점 / 77본 / 36표정 / 3재질 슬롯. 보고 시 런타임 Mesh 메모리 4,034,728 bytes.
기본 활성 렌더러 6 / 삼각형 40,575 / 재질 슬롯 11. 대체 7 / 43,890 / 11.
AI가 실제 Unity 직접 스킨 렌더와 표정·생활 모션 샘플을 점검했다. 사용자 미술 승인은 별도 대기.
Console Error 조회 0은 조회 시점의 상태이며 장시간 런타임 무오류 보장이 아니다.
Play Mode의 게임 등장·대화·시선·립싱크·컬링 재진입, Player 실행, 군중 성능은 미수행.
긴 머리·투구·망토·MagicaCloth와 전체 카탈로그 조합은 미검증이다. TESTS.txt를 따른다.
