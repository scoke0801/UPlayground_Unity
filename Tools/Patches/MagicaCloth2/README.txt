MagicaCloth2 캐시 ID 충돌로 인한 Bokusei 헤어 손상 수정

확인된 원인
연화의 Dress(81개 본), Hair(43개 본), FrontHair(5개 본)가 동일한
PreBuild ID 1bad0560을 사용했다. YeonHoa.asset에는 마지막 FrontHair만
남아 공유 배열은 5개, 인스턴스 배열은 81/43개인 상태가 되었다.
MagicaCloth 전역 배열 할당 위치가 서로 어긋나 이후 등록한 Bokusei의
Head 본이 다른 Cloth 소유로 분류되고 컬링 때문에 변환 행렬이 0으로 남았다.
실행 중 Head Transform은 정상인데 계산 행렬은 0인 것을 직접 확인했다.
헤어 입자는 최대 약 934m 떨어졌으며 NaN이 있었다.

반영 내용
- Dress/Hair에 개별 ID를 발급하고 공유 PreBuild 데이터를 다시 생성했다.
- NPC_YeonHoa 프리팹, PlayerModel_YeonHoa 및 이를 상속한 프리뷰,
  LakeOfLifeScenario의 R06/R07/R09 연화 배치에 적용했다.
- 씬은 캐시 ID 6줄, NPC/플레이어 모델은 각각 ID 2줄만 변경했다.
- PreBuildSerializeData.DataValidate에서 공유/고유 Transform 수와
  초기 위치/회전 배열 수를 비교한다. 불일치 시 등록 전에 실패시켜
  다른 Cloth의 전역 배열이 손상되는 것을 막는다.
- 첫 시도에서 수정한 삼각형 회전 퇴화 검사도 유지한다. 이 검사는 별도
  계산 결함을 막지만 이번 캐시 충돌의 직접 해결책은 아니었다.

재적용
플러그인과 플레이어 모델 폴더는 Git 제외 대상이다. 현재 로컬에는 적용했다.
플러그인 재설치/다른 체크아웃에서는 저장소 루트에서 각 파일에 대해 실행한다.
  git apply --check Tools/Patches/MagicaCloth2/prebuild-transform-validation.patch
  git apply Tools/Patches/MagicaCloth2/prebuild-transform-validation.patch
  git apply --check Tools/Patches/MagicaCloth2/triangle-rotation.patch
  git apply Tools/Patches/MagicaCloth2/triangle-rotation.patch
  git apply --check Tools/Patches/MagicaCloth2/yeonhoa-model-cache-ids.patch
  git apply Tools/Patches/MagicaCloth2/yeonhoa-model-cache-ids.patch
모델 패치는 같은 변경의 YeonHoa.asset과 함께 사용한다.
이미 적용되었거나 원본 버전이 달라 --check가 실패하면 강제 적용하지 않는다.

검증
- Unity 6000.3.21f1 컴파일 CS 오류 0.
- CheckPrebuildTransforms.csx: 정상 캐시 4개 및 충돌 재현 거부 2개.
- CheckTriangleRotation.csx: 평행/역평행/근접 평행/영벡터/무한대/정상 입력 6개.
  두 파일은 Unity MCP execute_code 메서드 본문으로 실행한다.
- 같은 씬을 기존 계속하기 경로로 재실행: 유효 Cloth 135개의 본 소유 ID 불일치 0.
- 해당 씬에 실제 Bokusei 모델 프리팹을 임시 생성하고 활성화한 뒤 이동·회전:
  헤어 63개, 상의 295개 입자의 위치/회전/기준 회전/다음 위치 NaN 0.
- 헤어/앞머리를 BakeMesh하여 정점 유한성 확인. 머리 메시 반폭은 약 0.15m,
  앞머리 반폭은 약 0.12m. 임시 오브젝트는 검사 후 제거했다.
- 재현 검사 중 Console 오류 0. 임시 모델 검사는 실제 플레이어 애니메이션과
  조작을 모두 검증한 것은 아니다. 사용자 이동·공격·캐릭터 교체 시각 확인은 별도다.
- Windows Player 빌드 미수행: 일반 데이터 및 검증 계산 수정 범위.

이전 사용자 수정이 포함된 씬 백업은 output/bokusei-hair-fix/prebuild-backup/에
보존했다. 기존 퀘스트/진행도 작업은 변경하지 않았다.
