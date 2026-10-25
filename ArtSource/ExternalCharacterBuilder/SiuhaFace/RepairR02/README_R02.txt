Siuha P09 R02 — 목·두상·헤어 수정

현재 결과
기본 프리셋: Assets/10.Datas/Actor/ExternalModels/Siuha_P09_BuildPreset.asset
대체 프리셋: Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Alternate_BuildPreset.asset
프리팹: Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab / Siuha_P09_Alternate.prefab
검수 씬: Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity
Blender 원본: 이 폴더의 Siuha_R02_HeadHairNeck.blend
Unity 렌더·검사 기록: Evidence/

R01의 목 색 차이와 길어진 두상을 사용자 지적에 따라 수정했다.
R01 자동 검사는 정점 연결을 확인했지만 미술 품질을 충분히 확인하지 못했다.
이전 설명·검사 명세는 *_R01_SUPERSEDED.txt이며 현재 품질의 근거로 사용하지 않는다.

변경
1. R01의 P09 두피 투영을 제거하고 Siuha 두상을 복원한 뒤 정수리만 낮췄다.
   정수리 높이 1.549231 → 1.508349m. 눈·눈썹·입·턱 앞면 좌표는 R01과 같다.
   기존 눈 축소 조형은 유지했다. 헤어를 맞추기 위한 두피 확장은 하지 않는다.
2. Blender 5.2.2에서 Hair03/11의 뿌리 높이·폭·앞머리를 조정했다.
   정점뿐 아니라 상부 가닥 면의 중점도 검사하여 두피 관통을 줄였다.
   원본 정점 수·삼각형·UV·본 웨이트·bindpose를 유지한 별도 Unity Mesh를 사용한다.
   FaceMeshProfile의 fittedMeshes에 연결하여 빌더에서도 두 헤어에 적용된다.
3. 얼굴의 열린 뒤통수를 252삼각형으로 닫고, 목에 중간 링 3개를 추가하여 252삼각형으로 연결했다.
   목과 몸체 경계의 위치·웨이트·노멀을 맞추고 UV 분리점의 노멀도 연결했다.
4. 목과 뒤통수는 얼굴과 같은 재질을 사용하며 얼굴 알베도의 피부 픽셀(.5,.15)을 샘플링한다.
   몸체에 같은 알베도와 셰이더 설정을 적용하여 색·그림자 설정 차이를 제거했다.
   저장 프리팹의 의상 내 피부 슬롯에 남았던 공용 Toon 피부도 Siuha 전용 피부로 교체했다.
   몸 피부는 단색 알베도 샘플을 사용하므로 원본 몸 텍스처의 미세한 색상 디테일은 사용하지 않는다.

범위
Siuha 전용 에셋과 공용 에디터 프로파일의 선택적 피팅 연결을 변경했다.
기존 HairAttachment 기능을 유지했다. 모든 대상을 먼저 검사한 다음 메시를 교체한다.
Hair03/11 두 종류가 검수 대상이다. 다른 헤어·투구·물리 조합은 별도 피팅이 필요하다.
기본 SkinHead_Female은 비활성 원본을 유지한다. Blender의 해당 피팅 오브젝트는 숨겨진 저작 참고다.
원본 Suiha/P09 에셋·공용 재질은 수정하지 않았다. 기존 액터·월드·전투·대화 연결이나 커밋은 하지 않았다.
열린 사용자 씬을 이 작업에서 저장하거나 교체하지 않았다.

재현
상위 폴더의 R01 final.bin과 source.bin은 R02 입력이므로 덮어쓰지 않는다.
R01 import_face.csx / match_skin.csx / sculpt_face.py만 다시 실행하면 이번 수정이 되돌아간다.
1. export_parts.csx는 P09 원본 헤어·몸체를 내보낸다. 현재 바이너리와 본 이름·텍스처 경로는 이미 보관했다.
2. Blender background --factory-startup --python repair_model.py
   → repaired_face.bin / Hair_03_fitted.bin / Hair_11_fitted.bin / .blend / repair_report.json
3. Unity execute_code로 import_repair.csx 실행.
   → 기존 얼굴 Mesh GUID를 유지하여 메시·재질·피팅 연결·두 프리팹 갱신.
4. 상위 validate_seams.csx / validate_model.csx의 출력 폴더를 output/SiuhaP09RepairR02로 바꿔 실행.
   이 폴더의 validate_fitted.csx도 실행한다.
5. capture_diagnostic.csx 및 상위 capture_seams/live_skin/full/expressions.csx로 렌더.
   상위 캡처 스크립트의 출력 폴더 역시 output/SiuhaP09RepairR02로 지정한다.
6. 기존 파일 백업과 SHA256은 backup/manifest.json에 있다.
   갱신 스크립트는 범용 transaction이 아니므로 재실행 전 수정된 대상도 별도 백업한다.

Blender에는 최종 얼굴 36 Shape Key·77 본 그룹, 헤어별 웨이트 그룹과 텍스처가 포함된다.
Unity Mesh가 bindpose 실행 원본이며 .blend에는 P09 전체 Armature/Avatar를 구성하지 않았다.
Before 헤어와 대체 Hair11은 숨김 상태다. Blender 재질은 저작 참고이며 최종 외형은 Unity에서 판단한다.

검증 결과
Unity 6000.3.21f1 / URP. P09FaceMeshProfileTests EditMode 12개 통과.
목 9자세 × 중립/36표정 = 333회: 몸체 연결 최대 오차 1.685874E-07m, UV 분리 경계 최대 오차 3.690066E-07m.
기본·대체 표정 BakeMesh 72회, Idle/Walk 12표본, Hair03/11 × Armor03/05 4조합 통과.
피팅 헤어 2종 × 프리팹 2종 × 생활 모션 6표본 = 24회: 원본 웨이트·UV·삼각형 유지, bounds 이탈 0.
저장 프리팹 내 이전 공용 피부 잔존 0. 유효 Humanoid, Missing Script·본·재질 누락 0.
게임 얼굴 11,560정점 / 77본 / 36표정 / 3재질 슬롯. 기본 41,043삼각형, 대체 44,358삼각형.
Blender 재생성 바이너리 SHA256 일치. 기존 에셋 meta 11개 보존. 최종 Console Error 조회 0.
정면·측면·뒤통수·맨목 측광·고개 숙임·표정·의상 포함 실제 Unity 렌더 확인.
Play Mode의 게임 등장·대화·시선·립싱크·컬링 재진입·물리와 Player 실행은 미수행.
사용자 미술 승인은 대기이며 자동 검사 통과를 미술 승인으로 간주하지 않는다.
