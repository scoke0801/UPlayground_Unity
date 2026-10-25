Siuha P09 R04 — 원본 헤어 복원과 부착 위치 수정

요청: 다른 P09Builder 모델과 비교하고 머리 스케일을 바꾸지 않는다.
R03의 두상·헤어 압축은 폐기한다. 눈 크기를 포함한 R02 얼굴 11,560정점을 정확히 복원했다.
목 연결과 피부 재질은 유지한다. 새 두피 확대·압축, 얼굴 확대, 헤어 정점 변형은 적용하지 않았다.

비교 근거
P09 여성 기본 베이스, Daon_P09Compatible, Lili_P09Compatible, LianLian_P09Compatible을
같은 카메라·조명·Idle 0.35초, 정면과 75도에서 렌더했다. 기존 모델은 원본 Hair03을 사용한다.
각 인물의 승인된 부착 스케일은 비교 이미지에 그대로 남겼으며 Siuha에 복사하지 않았다.
P09 기본·다온의 Hair03 윗가닥과 옆가닥을 기준으로 원래 실루엣을 복원했다.
Evidence/Compare_*는 비교 당시 상태이고 Compare_Siuha_R03_*는 수정 전이다.
Evidence/Compare_Position1_*는 선택한 Hair03 위치 후보, Final_* 및 LiveSkin_*는 저장 후 결과다.

최종 적용값 (Head 로컬 좌표, 단위 m)
Hair03: positionOffset (0, -0.025, 0), localScale (1, 1, 1).
Hair11: positionOffset (0, 0.012, 0), localScale (1, 1, 1).
Head localScale (1, 1, 1). 두 헤어는 공용 P09 원본 Mesh를 그대로 참조한다.
Siuha_P09_Face.fittedMeshes는 비어 있다. 이전 Siuha_Hair_*_Fitted.asset은 이력 보관용이며
현재 프로필·프리팹·검수 씬에서 사용하지 않는다. 원본 P09 메시와 다른 인물은 수정하지 않았다.
Hair11의 앞머리는 눈을 가리지 않도록 높이만 올렸다.

적용 대상
Assets/08.Models/Characters/SiuhaP09/Siuha_P09_Head.asset
Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset
Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09Compatible.prefab
Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09_Alternate.prefab
Assets/01.Scenes/Tests/Equipment/Siuha_P09_Review.unity
씬의 BodyOnly·기본·대체 모델, 카메라, 의상은 유지한다. 기존 에셋 GUID를 보존한다.

재현 및 복구
backup/manifest.json은 수정 직전 28개 파일과 SHA256 목록이다. backup 아래 원본을 보관한다.
Unity execute_code 메서드 본문으로 apply_native_hair.csx 실행 후 update_scene.csx를 실행한다.
전자는 RepairR02/repaired_face.bin으로 얼굴을 복구하고 원본 헤어 참조·위치를 적용한다.
후자는 정확히 Siuha 검수 씬만 갱신하며 저장되지 않은 변경이 있으면 중단한다.
공용 빌더의 삭제·생성 경로는 사용하지 않는다. 전체 작업의 원자적 트랜잭션을 보장하지 않으므로
실패 시 부분 저장 여부를 확인하고 manifest의 대상만 백업에서 복구한다.
validate_native_hair.csx와 상위 validate_seams.csx / validate_model.csx로 검증한다.
검수 씬 캡처는 RepairR03/capture_review_scene.csx의 출력 경로를 R04로 바꿔 사용했다.
Blender 재조형 없이 기존 원본 메시와 프로필 부착 위치만으로 수정했다.

검증 결과
R02 얼굴 정점 11,560개 정확 일치, 두 프리팹 원본 Hair03/11 Mesh 참조 정확 일치.
프로필 2회 재적용, Head·헤어 스케일 1, 모션 bounds 24회, 저장 씬 재로드 통과.
목 9자세 × 중립·36표정 333회: 몸체 연결 최대 오차 1.685874E-07m.
표정 스키닝 72회, Idle/Walk 12회, 헤어·의상 4조합, 프리셋 재로드 통과.
정면·측면·숙인 후면, 실제 SkinnedMeshRenderer 렌더, 표정, 검수 씬 이미지를 확인했다.
Play Mode 게임 등장·대화·시선·물리·컬링 재진입과 Player 빌드는 미수행이다.
최종 Console 조회는 Unity MCP 연결이 없어 실패했다. Console 오류 0을 확인했다고 보고하지 않는다.
R04 자동 검증과 미술적 승인 여부는 구분한다. 커밋하지 않았다.
