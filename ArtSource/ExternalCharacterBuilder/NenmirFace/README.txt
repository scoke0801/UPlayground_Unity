Nenmir 기반 P09 모델 — r04 (기존 P09 모델 비교 후 얼굴 비율·명암 수정)

사용 제약: 두피·뒤통수 일부가 열린 얼굴 메시이므로 이를 가리는 헤어가 필수다.
민머리·열린 부위를 노출하는 헤어는 현재 지원하지 않는다. 지원하려면 두피·뒤통수 추가 저작과 재검수가 필요하다.
P09 빌더 연결: Nenmir_P09_BuildPreset(Hair03), Nenmir_P09_Alternate_BuildPreset(Hair11).
두 프리셋은 Assets/10.Datas/Actor/ExternalModels/ 아래에 있으며 같은 Nenmir_P09_Face를 사용한다.
제약 상세: Assets/docs/EXTERNAL_MODEL_P09_CONVERSION_GUIDE.md의 '기존 모델의 명시적 사용 제약'.

r04 현재 결과
기본 P09 여성 얼굴과 Lili/Reine/Siuha/Daon/Bokusei를 동일 카메라·조명·Idle로 조사했다.
입을 올린 r03에서 턱 여백이 길어졌고 눈썹–눈 간격이 넓었으며 얼굴 명암이 약했다.
시각적 판단의 근거와 동일 조건 전후 화면: output/NenmirP09ArtReview/index.html.
눈 크기 배율 0.86/0.78 유지. 눈썹을 낮추고 입을 내리면서 턱끝을 올려 아래 얼굴을 정리했다.
눈 주변과 볼·턱의 폭을 연속적으로 조정했다. 낮은 볼 폭은 r01 대비 10.47~15.36% 작다.
실제 메시 기준 눈썹–안구 평균 높이 차이 30.33→25.29mm, 입술–턱끝 34.53→27.15mm.
얼굴 재질의 채도 0.65→0.88, 감마 1→1.35, 표면 음영 반응을 조정했다.
텍스처 재생성은 하지 않았다. appearance.json이 얼굴 재질 조정값의 제작 원본이다.

검증
정점 3,968/본 77/표정 36/메시 GUID 유지. 바깥 피부 삼각형 뒤집힘 0.
표정 스키닝 72회, 생활 모션 12회, 헤어/의상 4조합, 목 9자세×37표정 333회 통과.
몸체 목 연결 최대 오차 1.685874E-07m, UV 경계 오차 0. 실제 GPU 렌더 2종×3각도 확인.
눈 감기·웃기·아/오 표정 캡처 확인. 열린 검수 씬의 두 모델이 수정 메시·재질을 참조함을 확인했다.
사용자 검수 씬은 저장하거나 교체하지 않았다. 프리팹/프로파일/헤어는 r03의 참조를 유지한다.
형상·재질 반영과 자동 검사는 완료했으나 최종 외형 만족도는 사용자 확인 대기이다.
Play Mode/Player/실제 게임 대화는 미수행이다.

재현
sculpt_face.py → import_face.csx → match_skin.csx → validate_model.csx/validate_seams.csx
→ validate_shape_fix.py → export_final.csx → finalize_blend.py.
현재 실렌더: capture_art_review.csx. 비교 모델: capture_references.csx.
백업: output/NenmirP09ArtReview/backup_r03/manifest.json (커밋되지 않은 직전 결과 복구용).
.blend는 최종 얼굴·36표정·웨이트를 보관하며 실제 머티리얼 결과는 Unity가 기준이다.

아래 이전 기록은 경과 보존용이며 현재 결과는 위 r04가 우선한다.
Nenmir 기반 P09 모델 — r03 (이목구비 재배치와 헤어 피팅)

r03 현재 결과
축소된 윤곽에 맞춰 양쪽 눈·눈썹을 안쪽 최대 3.4mm, 아래 3mm로 옮겼다.
코는 아래 최대 1.2mm, 입은 위 최대 3.5mm로 조정하고 입 폭 배율을 1.08→0.98로 줄였다.
기존 작은 눈 크기는 유지하며 눈꺼풀·눈 표면·속눈썹과 모든 표정에 같은 변형을 적용했다.
Hair03/11은 전용 피팅 메시로 옆머리 아래 폭을 각각 최대 9%/6% 줄였다.
부착점은 둘 다 뒤 2mm, 위로 Hair03 2mm/Hair11 5mm 조정했다.
공용 원본 헤어·본·웨이트·UV·토폴로지는 보존한다. 다른 헤어는 피팅 대상이 아니다.
기본/대체 프리팹과 얼굴 프로파일에 메시·부착값을 저장했고 프리셋은 같은 프로파일을 참조한다.
현재 사용자 씬은 저장하거나 교체하지 않았다. 사용자 씬의 개별 override는 확인이 필요하다.

검증
- 바깥 피부와 피팅 헤어의 삼각형 뒤집힘 0. 정수리(높이 1.465m 이상) 변경 0.
- 눈 영역 실제 평균 이동: 안쪽 3.371mm, 아래 3.000mm.
- 표정 스키닝 72회, 생활 모션 12회, 헤어/의상 4조합 통과.
- 헤어 원본 스킨·UV·토폴로지 보존, 반복 적용·슬롯 교체 후 메시/부착값 유지 통과.
- 목 9자세×중립/36표정 333회, 몸체 목 연결 최대 오차 1.685874E-07m, UV 경계 오차 0.
- 실제 GPU 스킨 렌더 정면/35도/측면 2종 확인. Console Error 조회 0.
- Play Mode·Player·게임 대화·최종 사용자 외형 승인은 미수행.
캡처: output/NenmirP09ProportionFix/LiveSkin/.
백업: output/NenmirP09ProportionFix/backup_20261025_112242/manifest.json.
재현: sculpt_face.py → import_face.csx → fit_hair.py → import_hair.csx
→ validate_model.csx/validate_seams.csx/validate_shape_fix.py → export_final.csx → finalize_blend.py.
헤어 최초 추출은 export_hair.csx. .blend는 최종 얼굴·36표정·웨이트를 보관하며,
헤어 제작 정보는 hair_source.bin/hair_fitted.bin/hair_fit.json과 fit_hair.py에 보관한다.

아래는 이전 제작 기록이며 현재 결과는 위 r03 기록이 우선한다.
Nenmir 기반 P09 모델 — r02 (볼·턱 폭과 목 연결 형상 수정)

r02 현재 결과
사용자 근접 화면에서 r01의 턱 아래 단차와 넓은 볼이 반려되었다.
목 경계가 벌어지지 않는 수치 검사만으로 자연스러운 형상을 확인할 수 없었다.
목 보정의 y=-0.022 절단 조건을 제거했다. 연결된 바깥 피부에만 목 둘레에서 35mm에 걸쳐
보정량이 연속적으로 줄어들도록 적용하여 턱 아래 면이 갑자기 접히는 원인을 수정했다.
아래 볼·턱의 4개 단면 폭을 기존보다 8.40~16.16% 줄이고 앞쪽 볼 깊이를 최대 3mm 줄였다.
중립형에서 눈 높이(1.399m) 이상의 정점 변화는 0이다. 귀 안쪽을 볼 축소에서 제외했다.
원본 대비 바깥 피부 삼각형 뒤집힘 0. 목 영역 인접 면 각도의 95백분위는 63.60도→56.30도.
3,968정점·36표정·77본·토폴로지·메시 GUID를 유지한다. 기본/대체 프리팹은 같은 메시를 상속한다.
프리팹·BuildPreset·텍스처·재질·meta는 백업 체크섬과 동일하다. 사용자 씬을 저장하거나 교체하지 않았다.
목 9자세×중립/36표정 333회, 표정 스키닝 72회, 생활 모션 12회, 헤어/의상 4조합 검사 통과.
몸체 목 연결 최대 오차 1.685874E-07m, UV 경계 오차 0. Console Error 조회 0.
옷과 머리를 제외한 동일 각도 전후 캡처와 머리를 포함한 실제 GPU 스킨 렌더를 확인했다.
근거: output/NenmirP09ShapeFix/Before_*, After_*, AfterLive_*와 shape_fix_validation.json.
백업: output/NenmirP09ShapeFix/backup_20261025_111310/manifest.json.
재현: sculpt_face.py → import_face.csx → validate_model.csx/validate_seams.csx
→ export_final.csx → finalize_blend.py. 형상 회귀 검사는 validate_shape_fix.py.
최종 사용자 외형 승인·Play Mode·Player는 미수행이다. r01의 테스트 5개는 이전 실행 기록이다.
아래 내용은 최초 제작 기록이며 형상 수치와 변경 결과는 위 r02 기록이 우선한다.

제작 범위
신규 P09 모델, 얼굴 프로파일, 기본/대체 빌더 프리셋, 검수 씬을 제작했다.
기존 전투용 Nenmir 교체나 신규 NPC의 역할·월드 배치 대상은 지정되지 않았다.
기존 ActorDefinition·NpcData·대화·GAS·월드 씬은 변경하지 않았다.
제작 식별자 Nenmir_P09는 새로운 스토리 인물의 이름이 아니다.

사용
통합 툴 런처 → P09 캐릭터 빌더 → 프리셋 불러오기:
Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_BuildPreset.asset
대체: Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_Alternate_BuildPreset.asset
얼굴: Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_Face.asset
모델: Assets/03.Prefabs/Characters/ExternalModels/Nenmir_P09Compatible.prefab
대체 모델: Assets/03.Prefabs/Characters/ExternalModels/Nenmir_P09_Alternate.prefab
검수 씬: Assets/01.Scenes/Tests/Equipment/Nenmir_P09_Review.unity
검수 씬은 모델 확인용이며 NPC 생활 행동이나 전투를 실행하는 씬이 아니다.

제작 카드
원본: Assets/ExternalAssets/Character/Nenmir/Prefab/Nenmir_Faceonly.prefab
원본 얼굴: Body, 3,932정점 / 3재질 슬롯 / 173 BlendShape.
원본 FBX: Assets/ExternalAssets/Character/Nenmir/FBX/MB_Nenmir.fbx
원본 가중 본: Head, Neck, Left_Eye, Right_Eye.
P09 기반: 실제 여성 No Physics 베이스의 Female_Face_01과 Female_Body_Chest.
원본/대상 머리 높이로 배율 1.13708186을 산출했다. 좌표·본 순서는 coordinates.json.
눈 중심 영역의 가로 변형 배율 0.86, 세로 0.78. 주변으로 부드럽게 감쇠한다.
동공만 줄이지 않고 눈꺼풀·속눈썹·눈 표면과 표정 좌표를 같은 연속 변형으로 조정했다.
눈썹 높이, 볼 폭, 턱 폭·높이, 코 돌출, 입 폭, 귀와 뒤통수도 실제 메시로 조형했다.
정점 이동 3,660개에는 얼굴 조형과 P09 피팅이 모두 포함된다.
원본 20정점 목 경계와 P09 16정점 경계를 36삼각형으로 연결했다.
연결 하단 위치·웨이트·노멀은 실제 P09 몸체에서 가져왔다.
최종 얼굴: 3,968정점 / 36표정 / 77본 / 4재질 슬롯.
기본: 갈색 Hair03 + 청록 튜닉/팔/허리/다리 03, 투구 없음, MagicaCloth 끔.
대체: 갈색 Hair11 + 의상 05. 두 구성은 같은 얼굴 에셋을 사용한다.
헤어·몸체·의상의 공용 원본은 수정하지 않고 기존 P09_Residents_ToonLook을 참조한다.
피부·청보라색 눈 텍스처는 내장 ImageGen으로 원본 UV 아틀라스를 편집했다.
최종 텍스처: Assets/08.Models/Characters/NenmirP09/Nenmir_Face_Albedo.png
정확한 프롬프트: imagegen_prompt.txt. CLI/API 생성은 사용하지 않았다.
얼굴 재질의 조명 반응은 P09 피부와 맞추고 과도한 보조 색·발광을 사용하지 않는다.

산출물과 재현
Unity execute_code에서 csx를 메서드 본문으로 실행한다. 런타임/Editor C# 변경은 없다.
1. export_face.csx → source.bin, coordinates.json
2. Blender background에서 sculpt_face.py → repair_sculpt.bin, repair_topology.bin, .blend
3. 생성 텍스처를 위 Assets 경로에 두고 import_face.csx
4. 최초 build_model.csx → update_appearance.csx → match_skin.csx → build_alternate.csx
5. validate_model.csx, validate_seams.csx, capture_*.csx
6. export_final.csx → finalize_blend.py, 최초 create_review_scene.csx
Blender: C:/Program Files/Blender Foundation/Blender 5.2/blender.exe
--background --factory-startup --python <script>로 실행하여 열린 Blender 문서를 보존했다.
Nenmir_P09_Face.blend에는 정렬 원본, 173표정 조형본, 최종 36표정·77본 그룹 메시가 있다.
실행 Armature/Avatar/bindpose의 원본은 Unity P09 베이스와 Unity Mesh 에셋이다.
source.bin / repair_sculpt.bin / repair_topology.bin / final.bin은 메시 교환 데이터다.
parameters.json은 눈·볼·턱·코·입·목의 조형 설정이다.
이미 다른 인물에 공유한 뒤에는 기존 경로를 재생성하지 말고 revision 경로를 분리한다.

검증 — Unity 6000.3.21f1
Editor 데이터 검사 통과: Missing Script·외부 본·누락 재질 0, 유효한 Humanoid Avatar.
UV·노멀·탄젠트·유한 좌표·웨이트 합·본 인덱스·36표정 순서 검사 통과.
두 모델 표정 스키닝 72회, Idle/보행 12표본, 얼굴 bounds 이탈 0.
Hair03/11 × 의상03/05 4조합에서 얼굴 유지, 프리셋 재로드·무효 경로 거부 통과.
목 9자세 × 중립/36표정 = 333회. 몸체 경계 최대 오차 1.685874E-07m, UV 경계 0m.
P09FaceMeshProfileTests 5/5 통과. 기존 Daon fixture 검사이며 Nenmir 검사는 위 전용 csx다.
테스트 작업 ID: 9051d860fc4f46cab962fbb594f11e70.
최종 Console Error 조회 0. 프로젝트 C# 수정 없이 Unity에서 실제 에셋 로드와 검사를 수행했다.
실제 Unity 정면/35도/측면/전신/후면/눈 감기/미소/아·오/고개/보행 캡처를 저장했다.
LiveSkin은 미리보기 베이크 오브젝트를 제거한 실제 SkinnedMeshRenderer 렌더다.
Compare_Before/After는 같은 모델·재질·조명·카메라에서 원본 정렬 좌표와 조형 좌표를 비교한다.
Source_*는 외부 원본의 별도 렌더다. 생성 텍스처 이미지를 게임 결과로 사용하지 않는다.
근거: output/NenmirP09/validation.txt, seam_validation.txt, *.png.
성능 수량: 기본 6렌더러/31,949삼각형/12재질 슬롯, 대체 7/35,264/12.
얼굴 Mesh 런타임 메모리 1,877,356 bytes(검사 시점).

남은 확인
최종 사용자 미술 승인·Play Mode·게임 대화/시선/립싱크·Player·군중 성능은 미검증이다.
36표정 중 일부 감정은 원본 근사 대응이다. 전체 감정의 미술적 동등성을 보장하지 않는다.
원본 얼굴은 두피 상단이 열린 구조다. Hair03/11로 가려지는 조합을 확인했으며 민머리와
두피가 넓게 노출되는 헤어, 전체 P09 카탈로그 호환을 보장하지 않는다.
긴 머리·투구·망토·물리 조합과 Player/Enemy 무기·GAS·모션 통합은 별도 범위다.
목 아래 피부는 P09 몸체 텍스처를 사용하므로 근접 측광의 피부 질감 차이는 사용자 확인 대상이다.
사용자 직접 검수 절차는 TESTS.txt를 따른다. 커밋하지 않았다.


