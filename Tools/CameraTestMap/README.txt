카메라 테스트 맵

실행
- Assets/01.Scenes/Test/CameraTestMap.unity를 열고 Play를 누른다.
- 또는 UPlayGround 통합 툴 런처 → 월드 / 카메라 → 카메라 테스트 맵 열기.
- SceneContext가 실제 게임 매니저를 초기화한다. Boot 씬을 먼저 열 필요가 없다.
- 기존 Player 프리팹, CameraSettings, 훈련 허수아비 프리팹을 사용한다.
- 기본 캐릭터 모델을 씬에 내장하고 Swap.InitializeTo로 이동 시작 전에 준비한다.
  표적 AI는 기존 Freeze API로 멈추며, 공용 프리팹/BT/GAS 데이터는 수정하지 않는다.
- 별도 지역 진행/퀘스트/저장 데이터를 연결하지 않았다. 빌드 씬 목록도 변경하지 않았다.

조작 (기본 PlayerInputActions)
- 이동: WASD / 왼쪽 스틱. 시점: 마우스 / 오른쪽 스틱.
- 락온 획득·해제: 마우스 가운데 버튼 / 오른쪽 스틱 누르기.
- 표적 전환: Tab / 오른쪽 스틱 방향 입력. 스틱은 중립 복귀 후 다시 입력.
- 사용자 입력 재설정이 있으면 실제 바인딩이 우선한다.

배치와 확인 순서 (Unity 월드 좌표, 1 unit = 1m)
1. 시작점 (0, 0.1, -22)에서 왼쪽 주황 구역으로 이동한다.
   벽 (-18, 2, -12)을 등지고 접근하여 카메라 암이 당겨지는지 확인한다.
   벽에서 벗어나면 부드럽게 원래 거리로 복귀하고, 코너에서 떨림이 없어야 한다.
2. x=-24.7의 2m 복도와 x=-21.35의 3.5m 복도를 통과한다.
   좁은 복도의 천장 아래에서 위·아래 시점을 돌려 벽/천장 관통을 확인한다.
   넓은 복도 끝 허수아비를 락온한 채 진입·후퇴해 충돌과 구도 보정의 합성을 본다.
3. 오른쪽 청록 구역에서 (16, 0, -7)을 기준으로 표적 1~3을 획득·전환한다.
   작은 시점 변경에서 구도가 유지되고, 화면 경계 밖으로 밀리면 보정되는지 확인한다.
   바닥 원은 중심 (16, 0, 0)의 반경 4/8/12m 가이드다.
   획득 16m / 해제 20m 표기는 제작 시 CameraSettings 기준이며, 설정 변경 시 함께 갱신한다.
4. 표적 4를 락온하고 (22, 0, 3)으로 이동해 가림벽 뒤로 숨긴다.
   가림 유예 뒤 해제되고 엉뚱한 표적으로 자동 전환하지 않는지 확인한다.
5. 북쪽 보라 경사로(진행 10m / 상승 4m)로 올라간다.
   표적 6을 아래·위에서 추적하고, 발판 끝에서 내려갈 때 시점과 충돌을 확인한다.

블렌더 연결
- 원본: ArtSource/CameraTestMap/CameraTestMap.blend
- 전체 조감도: ArtSource/CameraTestMap/CameraTestMap_overview.png
- Unity 모델: Assets/05.Models/CameraTestMap/CameraTestMap.fbx
- 배치 원본: Assets/05.Models/CameraTestMap/CameraTestMap.layout.json
- 생성 스크립트: Tools/CameraTestMap/build_camera_test_map.py
- .blend는 Assets 밖에 두어 Unity의 블렌더 자동 실행에 의존하지 않는다.
- 모델의 COL_ 메시만 Ground 레이어 MeshCollider를 갖는다.
  VIS_ 바닥 선·문자는 Ignore Raycast 레이어이며 충돌체가 없다.
- Unity 씬은 FBX 프리팹 인스턴스를 유지하고 URP Lit 머티리얼 7개를 명시 매핑한다.
- 블렌더 원본 수정 후 Tools/CameraTestMap/export_camera_test_map.py를 블렌더에서 실행한다.
  이 스크립트는 FBX→Unity의 X축 반전을 보상하여 배치 JSON/씬의 좌표를 일치시킨다.
  일반 FBX 내보내기만 사용하면 좌우가 반전되므로 이 스크립트를 사용한다.
  기존 오브젝트 이름은 보존한다. 기존 메시 형태 수정은 재임포트로 반영된다.
  COL_ 오브젝트를 새로 추가하면 Unity에서도 Ground 레이어와 MeshCollider를 붙인다.
- 씬 생성 도구는 기존 씬을 덮어쓰지 않고 연다. 배치 JSON은 최초 생성 시 사용한다.
  이후 표적 위치는 Unity 씬에서 수정하며, 검증 도구로 발판 연결을 검사한다.
- 새 블렌더 파일에서 생성 스크립트를 실행하면 원본/FBX/배치 JSON을 재생성한다.
  이미 CameraTestMap 씬이 열려 있으면 이름/GUID 연결 보존을 위해 생성 스크립트는 중단한다.
  수동 수정한 원본을 보존해야 한다면 먼저 별도 사본을 만든다.

검증
- 툴 런처의 카메라 테스트 맵 검증: 모델 축·Ground 충돌체·표적 발판·Missing Script 검사.
- 카메라 테스트 맵 PlayMode 검증: 실제 플레이어를 이동시켜 충돌·락온을 자동 검사한다.
  결과는 Logs/CameraTestMap/Smoke.txt, 실행 캡처는 PlayMode.png에 저장한다.
- Test 씬 타입으로 일반 게임 HUD를 열지 않는다. 지역 미니맵/퀘스트 데이터도 요구하지 않는다.
- 자동 검사와 별도로 게임패드 손맛·시점 떨림·멀미·표적 가독성은 수동 플레이로 평가한다.

제작 검증 결과 (Unity 6000.3.21f1)
- Unity 컴파일 오류 0, 시험장 Missing Script 0.
- 환경 충돌체 23개, 실제 훈련 표적 6개, FBX 마커 좌표 및 경사로 발판 검사 통과.
- 실제 Play Mode: 벽 앞 거리 0.835m → 개방 공간 4.217m 복귀 확인.
- 표적 획득·오른쪽 전환·벽 가림 해제·4m 높은 발판 표적 추적 통과.
- 마지막 Smoke.txt: PASS. 실행 중 Error/Exception 없음.
- 게임패드 수동 체감 평가와 독립 Player Build는 이번 작업에서 수행하지 않았다.
