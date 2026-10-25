다온 새 얼굴 1차 조형

원본 얼굴을 그대로 쓰라는 요청이 아니었다. 외부 모델은 리깅·표정 토폴로지와 의상 기반으로만 활용한다.
현재 결과는 Honoka 기반 새 얼굴 조형 시안이다. 머리카락과 의상에는 원본 디자인이 남아 있으며, 정규 다온의 최종 외형으로 확정하지 않았다.

실제 변경
- Blender MCP에서 머리 길이, 눈 높이·간격·눈꼬리, 광대·턱선, 코와 입의 비례를 변경했다.
- Body 정점 20,610개 중 6,957개 변경. 최대 이동 0.01850535m. 목 아래 정점은 보존했다.
- ImageGen으로 얼굴 알베도와 갈색 원형 동공 아틀라스를 제작했다. 뿔은 표시하지 않는다.
- 원본 Toon 노멀을 보존해 과하게 날카로운 코 음영을 줄였다. 귀 끝과 코 깊이를 추가 조정했다.
- 블렌드셰이프 140개를 보존했다. 이 수에는 표정뿐 아니라 몸 가림·체형 키도 포함된다.

사용
통합 툴 런처 → 외부 모델 장비 빌더 → Assets/10.Datas/Actor/ExternalModels/Daon_NewIdentity.asset.
조립 결과: Assets/03.Prefabs/Characters/ExternalModels/Daon_NewIdentity.prefab.
제작용 원본: Assets/08.Models/Characters/DaonNewIdentity/Daon_NewIdentity_Base.prefab.
머리 메시·얼굴·눈 재질은 제작용 원본에 연결되어 장비 선택과 새 프리팹 생성 후에도 유지된다.
기존 Honoka NPC 샘플은 새 얼굴 NPC가 아니다. 정규 다온과 기존 시나리오는 교체하지 않았다.

재현 순서
1. 원본 Honoka를 가져온 Blender의 ExternalCharacter_Workbench에서 sculpt_face.py를 실행한다.
2. sculpt.bin은 Blender 정점 순서의 중립형·모든 셰이프 좌표를 담은 교환 원본이다.
3. Unity MCP에서 import_face.csx → build_character.csx → refine_face.csx → apply_eyes.csx 순서로 실행한다.
   생성 스크립트는 빈 출력 경로용이다. 기존 결과에 반복 실행하지 않는다. refine_face.csx도 추가 변형이므로 중복 적용하지 않는다.
4. capture_face.csx, capture_expressions.csx, validate_face.csx로 실제 Unity 렌더와 구조 검사를 수행한다.
Blender .blend 저장은 응답 시간 초과로 확인되지 않았다. 복구 원본은 sculpt.bin과 제작 스크립트이며 .blend가 있다고 간주하지 않는다.

검증
- UV, 삼각형, bindpose, bone weights 원본과 일치.
- 블렌드셰이프 140개 이름·프레임 수 보존 및 각 100% BakeMesh의 유한 좌표 검사 통과.
- 장비 5슬롯·10외형 전환 동안 새 얼굴 메시·눈 재질 유지.
- Humanoid Avatar 유효, Missing Script 0, Unity Console 오류 0.
- 동일 Idle/조명 정면·35도·90도 비교와 눈 감기·미소·aa·웃는 눈 캡처 확인.
- 대화·야간·걷기 Play Mode, Player 실행, 전체 표정 조합의 시각 품질은 미검증이다.
- 구조 검사 통과는 얼굴의 최종 미술 품질 승인과 다르다.

이미지
Face_AnatomyReference.png는 얼굴 해부학 참고용 생성 이미지다. 게임 결과물이 아니다.
실제 적용 결과는 output/DaonFaceRedesign/NewFace_0.png, NewFace_35.png, NewFace_90.png다.
원본 비교는 SourceFace_0.png 등, 표정은 Expression_*.png, 수치 검사는 validation.txt다.
ImageGen 프롬프트는 imagegen_prompts.txt에 보존했다.
