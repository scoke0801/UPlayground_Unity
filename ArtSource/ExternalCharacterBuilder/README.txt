외부 모델 장비 빌더

현재 새 얼굴 + P09 헤어·의상 결과
P09 빌더에서 Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset을 불러온다.
모델: Assets/03.Prefabs/Characters/ExternalModels/Daon_P09Compatible.prefab.
사용·검증·제작 원본은 DaonFace/P09_COMPATIBILITY.txt를 참조한다.

목적
External Assets를 리깅·표정·의상 기반으로 사용하고, 별도 저작한 얼굴 메시·텍스처를 연결하여 새 캐릭터를 만든다.
Honoka_Traveler와 Katana_Traveler는 장비 구조 확인용 원본 외형 샘플이다. 새 캐릭터의 얼굴 완성본이 아니다.
새 얼굴 조형 이력은 DaonFace/README.txt에 있고, P09 호환 최종 연결은 위 Daon_P09_BuildPreset.asset을 사용한다.
정규 다온 프리팹과 대화·시나리오 참조는 이번 작업에서 변경하지 않았다.
외형 조립용 모델 2종과 P09 NPC 생성 연결 샘플 1종을 제공한다.

사용
1. 통합 툴 런처에서 '외부 모델 장비 빌더'를 검색한다. 기존 P09 빌더 툴바에도 '외부 모델 장비'가 있다.
2. 새 얼굴은 Assets/10.Datas/Actor/ExternalModels/Daon_NewIdentity.asset을 지정한다. 원본 구조 비교는 Honoka_Traveler.asset 또는 Katana_Traveler.asset을 사용한다.
3. 슬롯을 선택하고 회전 슬라이더로 확인한다. '프로필 편집'은 원본 모델, 렌더러 경로, 부착 본·위치, 재질 교체와 기본 선택을 편집한다.
4. 새 이름으로 프리팹을 생성한다. 기존 출력은 덮어쓰지 않는다. 저장 직전 미리보기의 슬롯 선택을 반영한다.
5. 자동으로 열린 P09 빌더에서 NPC 모션과 NPC 데이터를 지정한다. 외부 모델은 P09 얼굴·방어구·무기 이름 규칙을 적용하지 않는다.
6. 다른 모델은 Project 창에서 프리팹을 선택하고 '선택 모델 → 프로필'로 등록한다. 프로필의 authoringNotes에 렌더러 경로 목록이 기록된다.

지원 경계
- 루트 Animator에 유효한 Humanoid Avatar가 있는 프리팹을 사용한다.
- 같은 원본에 포함된 스킨 의상은 rendererPaths로 등록한다. 몸 가림 메시와 옷을 같은 Variant로 묶어 관통·신체 구멍을 방지한다.
- 다른 슬롯이 같은 Renderer를 제어할 수 없다. 한 슬롯의 여러 Variant가 공유하는 Renderer는 허용한다.
- 강체 장신구는 attachmentPrefab과 Humanoid 본, 위치·회전·크기 보정으로 연결한다. FBX 자체의 축·단위 보정을 보존한다.
- 체형이나 리그가 다른 스킨 의상은 자동 호환하지 않는다. Blender에서 피팅·웨이트 저작을 거친 뒤 해당 모델에 포함해야 한다.
- 런타임 외형 교체 API는 CharacterEquipmentVisuals.TryEquip(slotId, variantId)다. 인벤토리 EquipmentSO와 저장 데이터 연결은 이번 작업에 포함되지 않았다.
- P09 Actor 생성 연결은 NPC를 지원한다. Player/Enemy는 무기 소켓·GAS·전투 모션 검증 전까지 명시적으로 거부한다.
- MagicaCloth는 원본 구성을 유지한다. 슬롯을 끄더라도 시뮬레이션 비용이 자동 제거되는 구조는 아니다.
- 모델의 예술적 완성도와 게임 장면 적합성은 자동 검사 결과로 판정하지 않는다.

산출물
- 모델: Assets/03.Prefabs/Characters/ExternalModels/Honoka_Traveler_v2.prefab
- 모델: Assets/03.Prefabs/Characters/ExternalModels/Katana_Traveler_v2.prefab
- NPC: Assets/03.Prefabs/Characters/ExternalModels/Npc/NPC_External_Honoka_Traveler/NPC_External_Honoka_Traveler.prefab
- P09 프리셋: Assets/10.Datas/Actor/ExternalModels/Honoka_Traveler_NpcPreset.asset
- 신규 NPC 데이터·ActorDefinition과 ActorDatabase의 해당 신규 항목 1개가 생성되었다. 기존 항목은 유지한다.
- 장신구: Assets/08.Models/Equipment/TravelerPouch/
- Blender 원본: TravelerPouch.blend, 재생성 코드: build_traveler_pouch.py
- ImageGen 내장 도구 디자인 참고: TravelerPouch_Reference.png, 프롬프트: imagegen_prompt.txt
- Unity MCP 재현 코드: create_samples.csx, build_npc_sample.csx, capture_samples.csx
- 실제 Unity 캡처·참조 검사: output/ExternalCharacterBuilder/

검증 결과
- Unity에서 새 runtime/editor 타입 로드와 컴파일 확인.
- ExternalCharacterBuilderTests: 최종 8개 통과, 실패 0. 원본 얼굴·재질·블렌드셰이프 보존, 슬롯 전환, 중복 소유 거부,
  잘못된 경로·선택의 임시 인스턴스 정리, 저장 경로 보호, 기존 출력 GUID/내용 보존, FBX 부착 크기 검사.
- 중간 장신구 높이 검사 1개가 실패했고, FBX 루트 회전까지 보존하도록 수정 후 8개 전체 재실행 통과.
- 두 모델을 기존 NPC Idle로 샘플링한 에디터 캡처 8장 확인. 캡처는 실제 모델 렌더이며 ImageGen 결과가 아니다.
- NPC 생성 후 NpcActor/KCC/Animancer/ActorAnimator, NPC 모션 세트, 프리셋, Missing Script 0 확인.
- 기존 월드 씬을 저장하지 않고 임시 씬을 닫아 원래 StonegateCitadel 씬으로 복귀했다.
- 실제 대화·야간·걷기/회전의 Play Mode 검증은 사용자 테스트 대기. Player 빌드와 실행은 미수행.
- 주요 통합 완료 또는 배포 판정이 아니며, 최종 통합 시 테스트 작업 지침에 따른 Player 검증이 필요하다.

사용자 테스트 작업 명세
준비: Unity 6000.3.21f1+ / URP. 현재 작업 씬과 저장 데이터를 보존한다. 임시 테스트 씬에 NPC 샘플을 배치하고 저장 슬롯을 새로 사용한다.
E01: 장비 빌더에서 Honoka의 5개 슬롯을 각각 전환. 얼굴·목·눈이 유지되고 코트/장갑/목장식/뒤머리/파우치만 선택대로 바뀌어야 한다.
E02: Katana의 School/Sportswear 전환. 해당 몸 메시·신발이 함께 바뀌며 두 의상이 겹치거나 분홍색 재질·허공의 칼집이 나오면 실패.
E03: 새 이름으로 저장 후 프리팹 재로드. 선택 구성이 유지되어야 한다. 같은 이름으로 재저장하면 기존 파일을 보존한 채 거부해야 한다.
E04: NPC 샘플을 Play Mode에서 Idle·걷기·회전으로 확인. 파우치가 골반에 따라 움직이고 코트·손·허벅지와 심하게 관통하지 않아야 한다.
E05: 대화 카메라 거리의 정면/35도/측면, 낮/야간 조명으로 얼굴·목 연결·눈동자·머리카락을 확인. 목 틈, 사라지는 얼굴, 과한 반짝임은 실패.
E06: 실제 대화 기능 연결은 기존 검증용 NPC 대화 데이터를 복제한 테스트 프리팹에서 수행한다. 정규 다온/시나리오 연결을 바로 덮어쓰지 않는다.
회신: 항목 ID / 통과·실패·미수행 / 실제 증상 / 화면 또는 로그. Console 예외 유무와 외형 품질을 별도로 기록한다.
