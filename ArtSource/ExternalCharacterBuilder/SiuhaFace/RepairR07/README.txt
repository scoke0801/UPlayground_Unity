Siuha P09 — 전체 헤어 맞춤과 목 윤곽 교정

적용
Hair01~13 전체를 Siuha 전용 메시로 피팅했다. 기존 Hair03·11 위치 보정은 초기화하고,
13종 모두 동일한 두상 변형 기준을 사용한다. 머리 본·헤어 부착 본 배율은 (1,1,1)이다.
얼굴 크기·눈·표정·두피 형상은 유지하고 목 하부와 접합 링의 정점만 수정했다.
상부 목의 앞면이 몸 경계보다 안으로 들어가던 단면을 최대 9mm 조정하고, 연결부의
노멀 기반 베지어 부풀음을 제거했다. 몸 경계 16점·몸체 메시·기존 스킨 웨이트는 유지했다.

헤어는 정수리 높이·앞뒤 간격·옆 폭을 조정한 뒤 P09 기준 두상과 Siuha 두상의 차이를
가닥 층 전체에 적용한다. 점마다 두피 표면으로 눌러 붙이지 않아 겹면·검은 얼룩을 피한다.
UV·정점 수·삼각형·본 순서·웨이트·bindpose는 각 P09 원본 헤어와 동일하다.
공용 P09/Suiha 원본, 다른 캐릭터, 공용 빌더 코드와 런타임 코드는 변경하지 않았다.

저장 대상
Assets/08.Models/Characters/SiuhaP09/Siuha_P09_Head.asset
Assets/08.Models/Characters/SiuhaP09/HairFitsR07/Hair_01_Siuha.asset ~ Hair_13_Siuha.asset
Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset
Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab
Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09_Alternate.prefab
Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity
기본·대체 프리셋은 기존의 같은 얼굴 프로필을 참조하므로 별도 변경 없이 새 피팅을 사용한다.
Review의 의상 모델 두 개와 BodyOnly 배치·가시성, 기존 에셋 GUID와 프리팹 fileID를 유지했다.
프리팹 diff는 헤어 Renderer·Transform·ParentConstraint 각 13개에 한정된다.
씬 BodyOnly 활성 렌더러는 Female_Face_02, Female_Body_Chest, Female_Body_Leg 세 개다.

제작·재현
1. export_parts.csx: 원본 P09 헤어 13종·기준 두상과 본 목록을 내보낸다.
2. Blender 5.2.2 background --factory-startup --python fit_geometry.py
   입력은 RepairR02/repaired_face.bin과 이 폴더의 원본 P09 .bin이다.
   fit_parameters.json이 조형 수치를 소유한다. 기존 보정 결과를 다시 입력하지 않는다.
3. load_candidate.csx: 저장하지 않는 후보 프로필과 메시를 만든다.
4. validate_candidate.csx 및 상위 validate_seams.csx로 연결·스킨을 검사한다.
   후자의 후보 이름은 Siuha_R07_Face, 출력 경로는 output/SiuhaP09RepairR07로 바꾼다.
5. capture_catalog.csx로 실제 Unity 스킨을 촬영한다. stamp, hairIds, views를 호출부에서 선언한다.
   후보 촬영은 config.FaceMeshProfile에 Siuha_R07_Candidate를 지정한다.
6. commit_candidate.csx: 백업 이후 변경 없음·두 프리팹의 전체 연결을 먼저 검사하고 저장한다.
   이미 HairFitsR07이 존재하면 중복 적용을 거부한다. 재조정은 새 revision과 별도 백업으로 진행한다.
   저장 중 예외가 발생하면 기존 5개 대상은 백업으로 복구하고 새 폴더는 조사용으로 남긴다.
   이 작업에서 강제 저장 실패를 주입한 롤백 테스트는 수행하지 않았다.
7. 저장된 프로필로 26개 조합을 재검사하고, 씬은 별도 Preview Scene으로 다시 로드했다.

Siuha_AllHair_Neck.blend는 최종 형상·UV·36개 얼굴 Shape Key·본 그룹/웨이트 보관본이다.
전체 P09 Armature·Avatar·게임 재질의 실행 원본은 Unity Mesh와 프리팹이다.
이번에 같은 Python 저작 스크립트를 재실행한 헤어 좌표와 저장 에셋의 좌표가 정확히 일치했다.

검증: AI가 Unity 6000.3.21f1 Editor와 Blender에서 수행
- 헤어 13 × 상의 03/05 = 26조합: 빌더 교체·반복 적용·머리 배율 1·원본 스킨 계약 유지.
- 헤어 26조합 × 목 자세 9 = 234회 BakeMesh의 유한 좌표 확인. 물리 동작 검사가 아니다.
- 목 9자세 × 중립/36표정 = 333회: 몸 경계 최대 틈 0.000000169m, UV 분리점 0.000000258m.
- 목 경계 16변, 예상 밖 열린 목 경계 0. 36표정·77본·11,560정점 유지.
- 두 프리팹 fileID 전부 유지, 기존 수정 대상 .meta 5개 정확 일치.
- 저장 씬 3개 인스턴스의 헤어 13종 참조, 위치, BodyOnly 가시성, Missing Script 0 확인.
- 저장 메시로 정면·75도·150도 전체 13종 캡처, 같은 최종 후보로 아래쪽 좌우55도 캡처.
- Console Error 최종 조회 0. C# 런타임/Editor 소스 변경이 없어 재컴파일·NUnit 전체 실행은 하지 않았다.

surface_validation.json은 머리 위쪽 면 내부 표본의 방사형 진단이다.
헤어 내부 뿌리에는 두피 안쪽 표본이 남아 있으나 모두 바깥 가닥에 가려진다(uncoveredSamples=0).
이 수치는 전체 메시의 비관통 증명이나 모든 시점·동작의 미술 승인이 아니다.
사용자 미술 확인, Play Mode, MagicaCloth, 투구·망토 조합, Player 실행은 미수행이다.
리그나 런타임/플랫폼 구성 변경이 없어 Windows Player 빌드는 이번 단계에서 수행하지 않았다.

근거
output/SiuhaP09RepairR07/Before_After.png: 왼쪽 수정 전, 오른쪽 수정 후. 위 목, 아래 Hair11.
output/SiuhaP09RepairR07/Saved_Catalog_0.png / _75.png / _150.png: 왼쪽 위부터 행 순서로 Hair01~13.
output/SiuhaP09RepairR07/Saved/: 저장 에셋으로 촬영한 개별 이미지.
output/SiuhaP09RepairR07/Low/: 최종 후보의 아래쪽 사선 이미지.
output/SiuhaP09RepairR07/catalog_validation.txt / seam_validation.txt: 검사 결과.
output/SiuhaP09RepairR07/backup/manifest.json: 수정 직전 5개 파일과 각 .meta의 SHA256 및 백업.
백업은 커밋되지 않은 기존 사용자 작업을 포함하므로 삭제하지 않는다. 커밋하지 않았다.
보관 크기: R07 제작 원본·스크립트 약 6.44MiB, 작업 직전 백업 약 15.17MiB. Player 빌드·프로젝트 사본은 만들지 않았다.
사용자 확인 절차는 TESTS.txt를 따른다.
