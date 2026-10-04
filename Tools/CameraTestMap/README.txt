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
6. 공중 감시자 SkySentinel은 (-9, 4.2, 1)에 1마리 배치한다.
   (-9, 0.1, -8) 부근에서 위를 향해 락온한다. 약 5.9m 날개 폭과 3초 날갯짓·부유 루프를 갖는다.
   루트 고도는 고정하며 시각 모델만 부유한다. 가까이 접근하거나 아래로 통과할 때 피치를 낮추지 않고
   제한된 피벗 상승·거리 프레이밍으로 플레이어와 락온 지점을 함께 담는지 확인한다.
   적 전신보다 플레이어 크기를 우선하며 기본 구도 대비 투영 배율 60%를 하한으로 사용한다.
7. 석상 거인 StoneColossus는 (11, 0.08, 21)에 1마리 배치한다.
   (11, 0.1, 11) 부근에서 락온한다. 약 6.2m 높이이며 몸통 대기 루프를 갖는다.
   정면에서 접근·후퇴하고 몸 주위를 돌며 포커스와 상체가 화면에 유지되는지 확인한다.

몬스터 표적
- 두 종류 모두 Blender로 제작한 카메라 시험용 무적·비공격 몬스터다.
  공중 추격/공격 AI를 새로 구현한 전투 몬스터는 아니다. 기존 훈련 액터의 초기화와 Freeze 계약을 사용한다.
- 원본: ArtSource/CameraTestMap/Monsters/CameraTestMonsters.blend
- 제작 미리보기: 같은 폴더의 CameraTestMonsters_preview.png
- FBX·URP 재질·대기 Animator Controller: Assets/05.Models/CameraTestMap/Monsters/
- 독립 프리팹 Variant: Assets/03.Prefabs/Actor/Monster/CameraTestMap/
- 배치·캡슐 규격: Assets/05.Models/CameraTestMap/Monsters/Monsters.layout.json
- 제작: Tools/CameraTestMap/build_camera_test_monsters.py
- Blender 원본 수정 후 반영: Tools/CameraTestMap/export_camera_test_monsters.py
- 통합 툴 런처 → 월드 / 카메라 → 카메라 테스트 몬스터 연결.
  기존 씬 배치를 보존하고 누락된 프리팹 인스턴스만 추가한다. 재실행해도 중복되지 않는다.
  이미 생성된 표적의 배치·캡슐은 Unity 씬/프리팹에서 수정한다.
- 공중 표적은 KCC 모터를 비활성화해 고도를 유지하며 몸체 캡슐의 충돌/락온은 유지한다.
  가짜 바닥 충돌체는 만들지 않는다. 대형 표적은 모델 크기에 맞춘 KCC 캡슐을 사용한다.
- 공용 Training Dummy, BT, GAS, CameraSettings는 이 추가 작업에서 수정하지 않는다.

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
  공중·대형 표적의 고도 유지·포커스 화면 유지·대기 애니메이션 재생도 검사한다.
  추가 표적 캡처는 SkySentinel.png, SkySentinelNear.png, StoneColossus.png, StoneColossusNear.png에 저장한다.
  두 몬스터의 원거리·근접에서 플레이어 투영 배율을 검사한다. 공중형은 피치와 플레이어 발밑도 검사한다.
  대상 상단 좌표는 기록하지만 전신 수용을 성공 조건으로 삼지 않는다.
- Test 씬 타입으로 일반 게임 HUD를 열지 않는다. 지역 미니맵/퀘스트 데이터도 요구하지 않는다.
- 자동 검사와 별도로 게임패드 손맛·시점 떨림·멀미·표적 가독성은 수동 플레이로 평가한다.

기존 허수아비 시험장 제작 검증 결과 (몬스터 추가 전, Unity 6000.3.21f1)
- Unity 컴파일 오류 0, 시험장 Missing Script 0.
- 환경 충돌체 23개, 실제 훈련 표적 6개, FBX 마커 좌표 및 경사로 발판 검사 통과.
- 실제 Play Mode: 벽 앞 거리 0.835m → 개방 공간 4.217m 복귀 확인.
- 표적 획득·오른쪽 전환·벽 가림 해제·4m 높은 발판 표적 추적 통과.
- 마지막 Smoke.txt: PASS. 실행 중 Error/Exception 없음.
- 게임패드 수동 체감 평가와 독립 Player Build는 이번 작업에서 수행하지 않았다.

공중형·대형 몬스터 추가 검증 결과 (Unity 6000.3.21f1)
- 환경 충돌체 23개와 기존 표적 6개를 보존하고, 몬스터 2개를 추가하여 표적 총 8개.
- Missing Script 0, FBX 크기·축·URP 재질·루프 Controller·캡슐 연결 검사 통과.
- 실제 Play Mode에서 기존 충돌·락온 획득/전환/가림/높은 발판 검사와 신규 표적 검사 모두 PASS.
- 공중형 포커스 화면 좌표 (0.50, 0.64), 대형 (0.50, 0.63). 고도 유지와 대기 애니메이션 재생 확인.
- 락온 표시 이펙트를 각 몬스터의 캡슐 중심에 연결했고 게임 화면 캡처로 확인.
- 검증은 열린 원본 에디터와 저장 데이터를 보존한 별도 프로젝트 사본에서 수행했다.
  사본의 GPU 스키닝 경로에서 Unity 네이티브 UpdateComputeSkinPoseBuffer 충돌이 발생하여,
  사본에 한해 CPU 스키닝으로 재검증했다. 원본 프로젝트의 그래픽 설정은 변경하지 않았다.
  원본 GPU 스키닝 환경의 수동 Play와 Player Build 검증은 남아 있다.
- 결과: Logs/CameraTestMap/Smoke.txt, MonsterPlayMode.log, SkySentinel.png, StoneColossus.png.

프레이밍 절제 검증 (Unity 6000.3.21f1)
- 전신 수용 대신 락온 포커스 위 최대 2m를 사용하고, 피벗 상승은 2.2m까지 제한한다.
- 프레이밍에 의한 플레이어 투영 배율 하한은 기본 구도의 60%다. FOV와 기본 줌은 유지한다.
- EditMode 76개, PlayMode 30개 및 기존 시험장 검사·공중/대형 원거리·근접 검사 PASS.
- 대형 근접 플레이어 배율 0.626, 공중 근접 0.60 및 피치 약 25° 확인.
- 결과와 캡처: Logs/LockOnFramingBalance/. 검증 사본은 CPU 스키닝을 사용했으며,
  원본 GPU 스키닝 환경의 수동 조작과 Player Build는 별도로 확인해야 한다.
