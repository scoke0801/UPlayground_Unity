폐기됨 — 2026-10-25 사용자 요청
Reine P09Builder 기본형·대체형, 전용 프리셋·모델 에셋·검수 씬을 Assets에서 제거했다.
아래 제작·재생성 절차는 과거 기록이며, 사용자의 새 제작 요청 없이 실행하지 않는다.
복구 보관본과 r02~r04 백업은 추가 사용자 요청으로 영구 삭제했다.
제작 원본·스크립트와 검수 이미지는 복구 백업 삭제 범위에서 제외했다.
원본 Spi_Reien/Reine과 공용 P09 빌더는 유지했다.
현재 revision: r04. 최신 결과는 R04_RESULT.txt를 우선한다.
얼굴/목 피부색 전달을 통일하고 Hair03 앞머리를 Reine 전용 메시로 피팅했다.
최신 이미지: output/ReineP09_r04/. 아래 r03/r02/r01은 이전 기록이다.

현재 revision: r03. 최신 결과는 R03_RESULT.txt를 우선한다.
헤어 부착 위치와 정면 코 명암을 수정했다. 최신 캡처: output/ReineP09_r03/FinalCompatible_*.png.
이하 r02/r01 설명과 캡처 경로는 이전 기록이다.
검은 코 메시·볼 점 제거, 코끝 얼룩 제거, 이마 축소·눈높이·입/턱 재조형,
기본 Hair11 단발 / 대체 Hair03, 최종 2,791정점. 기존 GUID와 프리팹 fileID 보존.
현재 이미지는 output/ReineP09/CloseUp_0.png 및 LiveSkin_*.png.
아래 r01의 헤어 선택·정점 수·이마 조형 설명은 수정 전 기록이다.
Reine 기반 P09 모델 r01

제작 결과
Reine 원본 얼굴의 토폴로지·UV·표정을 바탕으로 눈·눈썹·코·입·볼·턱을 조형했다.
눈 중심부의 변형 배율은 가로 0.90, 세로 0.82다. 화면 전체 눈 면적의 일괄 배율은 아니다.
눈꺼풀·안구·속눈썹과 원본 251개 표정에 같은 변형을 적용했다.
머리를 뚫고 나오던 긴 귀는 원본 ear_morph_small 변형을 0.9만큼 반영했다.
원본의 열린 정수리에 둥근 캡 49정점·120삼각형을 추가했다. 헤어 피팅을 위한 두피 확대는 하지 않았다.

제작 카드
- 대상: 독립 Reine 기반 P09 모델. 신규 인물의 역할·대사·월드 배치는 정의하지 않음.
- 원본: Assets/ExternalAssets/Character/Spi_Reien/CharacterPrefab/reien_original_pf.prefab
- 원본 FBX: Assets/ExternalAssets/Character/Spi_Reien/FBX/reien_fbx.fbx
- 원본 얼굴: ME_chr_reien_face_01, 3,412정점, 6재질, 251표정.
- 사용 슬롯: 피부 0, 눈 주변 1, 하이라이트 2, 눈썹 3, 홍채 4. 특수 감정 장식 5는 제외.
- P09 기반: PathConfig.BasePrefabFemaleNoPhysics의 실제 여성 골격, 77본·bindpose.
- 정렬: Reine 얼굴/눈높이와 실제 P09 몸 비율을 비교한 배율 0.90, 전방 0.020m 이동.
  귀까지 포함한 bounds 폭 자동 정렬은 얼굴이 지나치게 작아져 사용하지 않았다.
- 목: 원본 아래쪽 32정점 경계를 P09 몸체의 실제 16정점 경계에 48삼각형으로 연결.
  아래 경계는 몸체 위치·웨이트·노멀을 사용하며 UV 분리 경계의 위치와 표정 델타를 보존.
- 최종 얼굴: 2,808정점, 77본, 36 P09 표정, 6재질 슬롯. 원본 251표정은 Blender에 보존.
- 기본 조합: 여성 / 갈색 Hair03 / 의상03 / 투구 없음 / MagicaCloth 끔.
- 대체 조합: 갈색 Hair11 / 의상05. Hair11 머리 본 로컬 위치 (0, 0.015, 0)m, 배율 1.
- 피부: Reine 전용 ToonLook와 노출 피부 재질. 공용 피부 재질은 변경하지 않음.
- 알베도: 내장 ImageGen으로 붉은 눈 화장을 완화한 UV 텍스처. 눈 텍스처·하이라이트는 원본 재사용.
  프롬프트는 imagegen_prompt.txt. 생성 이미지는 3D 결과가 아닌 실제 얼굴 재질의 입력이다.

사용
통합 툴 런처 → P09 캐릭터 빌더 → 아래 프리셋 불러오기.
Assets/10.Datas/Actor/ExternalModels/Reine_P09_BuildPreset.asset
Assets/10.Datas/Actor/ExternalModels/Reine_P09_Alternate_BuildPreset.asset
외형의 FaceMeshProfile = Reine_P09_Face. ExternalModelPrefab은 비어 있는 P09 네이티브 경로다.
헤어·의상을 바꾸어도 얼굴 프로파일을 유지한다. 피부 톤은 Reine_P09_ToonLook이 소유한다.

산출물
Assets/03.Prefabs/Characters/ExternalModels/Reine_P09Compatible.prefab
Assets/03.Prefabs/Characters/ExternalModels/Reine_P09_Alternate.prefab
Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset
Assets/10.Datas/Actor/ExternalModels/Reine_P09_ToonLook.asset
Assets/08.Models/Characters/ReineP09/
Assets/01.Scenes/Tests/Equipment/Reine_P09_Review.unity
검수 씬에는 기본·대체·BodyOnly 모델과 카메라·방향광 2개가 있다.
BodyOnly는 Face02/Chest/Leg 세 렌더러만 표시하며 가시성은 씬 override다.
생활 AI·게임 서비스·대화 실행 씬은 아니다.

정규 연결 범위
기존 Reine은 PlayerModel_Reine, PlayerPreview_Reine, MonsterActor_Reien_Spear,
RedTree/R05/NPC_Reine에서 사용된다. 교체 대상이 특정되지 않은 모델 제작 요청으로 해석하여
독립 프리팹·프리셋을 만들었다. 해당 기존 액터·ActorDefinition·GAS·모션·월드·초상화는 변경하지 않았다.
따라서 게임 내 정규 Reine 교체나 월드 배치를 완료한 결과로 보고하지 않는다.
조사한 기존 프리팹과 원본 Spi_Reien 폴더의 git diff는 없음. 다른 사용자 변경은 유지했다.
열려 있던 LianLian_P09_Review 씬을 교체하거나 저장하지 않았다. 커밋하지 않았다.

재현 순서
Unity execute_code에서 .csx를 메서드 본문으로 실행한다. 런타임/에디터 공용 코드 수정 없음.
1. export_face.csx → source.bin, scalp.bin, coordinates.txt.
2. Blender background --factory-startup --python sculpt_face.py → sculpt.bin, topology.bin, .blend.
3. Reine_FaceAlbedo.png를 Assets/08.Models/Characters/ReineP09/에 복사.
4. import_face.csx → Mesh/Material/FaceProfile. 정수리 캡 생성도 이 단계에 포함.
5. 최초 생성에만 build_model.csx, build_alternate.csx.
6. update_appearance.csx → 전용 피부·ToonLook·헤어 위치와 두 프리셋·프리팹 적용.
7. 최초 생성에만 create_review_scene.csx, create_body_review.csx.
8. validate_model.csx, validate_seams.csx, verify_reload.csx, capture_*.csx.
9. export_final.csx → final.bin, Blender background finalize_blend.py → 최종 .blend.
Blender 경로: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe (5.2.2 LTS).
사용자 Blender 문서는 건드리지 않고 독립 background 프로세스를 사용했다.
.blend에는 정렬 원본·조형본·최종 게임 메시·UV·표정·본 그룹·텍스처가 있다.
P09 전체 캐릭터와 실제 bindpose는 Unity 프리팹/Mesh 에셋을 기준으로 한다.
final.bin은 위치·UV·삼각형·표정·웨이트·본 이름 교환본이며 리그가 포함된 FBX는 아니다.
재제작으로 기존 공유 출력물을 덮어쓰기 전에 별도 revision 또는 파일별 백업을 확보해야 한다.

AI가 수행한 검증
- Unity 6000.3.21f1 Editor 인메모리 스크립트 컴파일·임포트 정상, 최종 Console error 조회 0.
- P09FaceMeshProfileTests 12개 통과, 실패·스킵 0. 공용 계약 검사이며 Reine 미술 승인과 별개.
  최초 잘못된 네임스페이스 필터는 0개 실행이었으며 통과 근거로 사용하지 않았다.
- 기본/대체 모델 Missing Script·재질 누락·외부 본 참조 0, Humanoid Avatar 유효.
- 표정 스키닝 72회, Idle/Walk 12표본, 헤어03/11 × 의상03/05 4조합, 프리셋 직렬화 검사 통과.
- 목 9자세 × 중립/36표정 333회. 연결 최대 오차 1.685874E-07m, UV 경계 오차 0.
- 목 경계 16변, 예상 밖 열린 목 경계 0. 생활 모션 얼굴 bounds 이탈 0.
- 검수 씬 저장 후 재로드: 모델 3개, BodyOnly 활성 렌더러 3개, 얼굴 연결 유지.
- 실제 Unity 정면·35도·측면·전신·후면·표정·보행·목 회전·장비 없는 검수 캡처 확인.
- LiveSkin_*.png는 베이크 프리뷰를 해제한 실제 SkinnedMeshRenderer GPU 렌더다.
- 기본 활성 렌더러 6 / 30,543삼각형 / 14재질 슬롯; 대체 7 / 33,858 / 14.
  얼굴 메시 런타임 메모리 1,281,056 bytes. 군중 성능을 측정한 결과는 아니다.
- MCP 응답 연결이 끊긴 작업은 결과 파일의 시각·최종 정점 수를 확인했으며 중복 생성하지 않았다.

검증 자료
output/ReineP09/validation.txt, seam_validation.txt, reload.txt, editmode_tests.json.
Compare_Before_0.png / Compare_After_0.png는 동일 P09 바디·헤어·알베도·카메라에서 조형 전후 비교다.
Source_*.png는 원본 프리팹 자체다. 나머지 캡처는 이번 생성 프리팹 기준이다.
캡처 결과는 최종 재생성해 최신 메시·갈색 헤어 상태로 정리했다.

남은 항목
사용자 외형 확인, 실제 게임 Play Mode·대화/립싱크·시선·컬링 복귀·물리·투구/전체 헤어 조합·군중 성능은 미검증.
두피 캡과 뒤통수/목에는 가까운 장비 없는 시점에서 색 경계가 약하게 보인다. 열린 틈은 없으며 미술 확인 대상이다.
P09 감정 36개 중 일부는 원본 유사 표정 대응이며 표현의 시각적 동등성을 보장하지 않는다.
Windows Player는 독립 모델 자산 제작이며 고위험 코드/빌드 설정 변경이 없어 미수행.
TESTS.txt를 따라 확인하며 Editor 자동 검사와 사용자 미술 승인·게임 실행 통과를 구분한다.





