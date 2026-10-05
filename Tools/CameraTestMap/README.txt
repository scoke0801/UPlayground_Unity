카메라 수동 확인용 시험장

독립 실행 릴리즈 빌드
- UPlayGround 통합 툴 런처 → 월드 / 카메라 → 카메라 테스트 맵 릴리즈 빌드.
- 출력: Builds/CameraTestMap/Release/날짜-시간/CameraTestMap.exe.
- Windows 64비트, Development Build 비활성, CameraTestMap 단일 씬으로 바로 시작한다.
- Addressables를 새로 빌드하고 로컬 리소스를 함께 포함한다. 일반 게임의 빌드 씬 목록은 변경하지 않는다.
- 시험장 패키지의 로컬 번들은 시작 시 전체 CRC 검사를 생략한다. 대형 LZ4 번들 전체 해제로 인한
  15초 리소스 로드 타임아웃을 방지하며, 원본 그룹 설정과 원격 번들 검사 정책은 보존한다.
- 배포할 때는 exe만 복사하지 말고 출력 폴더 전체를 전달한다. 빌드 결과는 같은 폴더의 BuildReport.txt에 기록한다.
- 배치 실행 메서드: UPlayGround.Tool.Editor.CameraTestMapReleaseBuilder.Build.

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
   대상 방향으로 부드럽게 회전하고, 멈춘 대상에서 회전이 계속되지 않는지 확인한다.
   바닥 원은 중심 (16, 0, 0)의 반경 4/8/12m 가이드다.
   획득 16m / 해제 20m 표기는 제작 시 CameraSettings 기준이며, 설정 변경 시 함께 갱신한다.
4. 표적 4를 락온하고 (22, 0, 3)으로 이동해 가림벽 뒤로 숨긴다.
   가림 유예 뒤 해제되고 엉뚱한 표적으로 자동 전환하지 않는지 확인한다.
5. 북쪽 보라 경사로(진행 10m / 상승 4m)로 올라간다.
   표적 6을 아래·위에서 추적하고, 발판 끝에서 내려갈 때 시점과 충돌을 확인한다.
6. 공중 감시자 SkySentinel은 (-9, 4.2, 1)에 1마리 배치한다.
   (-9, 0.1, -8) 부근에서 위를 향해 락온한다. 약 5.9m 날개 폭과 3초 날갯짓·부유 루프를 갖는다.
   루트 고도는 고정하며 시각 모델만 부유한다. 가까이 접근하거나 아래로 통과할 때 갑작스러운 회전 없이
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


수동 확인
- 락온한 채 벽을 등지고 접근·후퇴한다. 카메라는 벽 앞에서 멈추고 공간이 열리면 천천히 복귀해야 한다.
- 양 벽 복도에서 락온을 유지하고 좌우 이동·정지·후진한다. 접촉 때문에 피치·회전 방향이 왕복하면 안 된다.
- 같은 벽 모서리를 여러 번 통과한다. 매 프레임 짧게 열린 틈으로 카메라가 다시 튀어나오면 안 된다.
- 지상·공중 표적에서 반복하고, 락온 해제·대상 전환·대화/필살기 복귀의 입력 상태를 확인한다.
- 툴 런처의 카메라 테스트 맵 검증은 씬 데이터와 Missing Script를 확인하는 저작 도구다.
- 카메라 자동 테스트·재생 Fixture·PlayMode 스모크 실행·CameraDiag 기록 코드는 제거했다.
- 이전 실행 파일은 변경 전 카메라를 포함한다. 현재 코드를 확인하려면 다시 빌드한다.
