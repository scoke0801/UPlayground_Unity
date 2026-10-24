# 환경 생물 구현·배치와 확인 절차

## 반영 범위

`LakeOfLifeScenario`의 `FieldWildlife` 아래 사슴 6마리, 나비 12마리, 작은 새 6마리, 늑대 3마리, 돼지 3마리로 총 30마리를 배치했다. 입구, 약초 길, 호수 접근부, 꽃밭, 숲 가장자리와 마을 바깥에 나뉘어 있다. 전투 액터 등록과 Collider가 없어 락온·공격 대상이나 통행 장애물이 되지 않는다.

| 종류 | 모델 출처 | 기본 클립 |
|---|---|---|
| 암사슴·수사슴 | DreamscapeEastLands 기존 프리팹·LOD | 대기, 걷기, 달리기, 먹이 먹기 |
| 나비 3색 | Idyllic Fantasy Nature 기존 프리팹 | 색상별 비행 3개 |
| 작은 새 | Blender 신규 모델 + ImageGen 깃털 텍스처 | 대기, 날갯짓, 활공 |
| 돼지 | Farmlands 기존 프리팹 | 대기, 걷기, 달리기, 먹이 먹기 |
| 늑대 | Blender 신규 모델 + ImageGen 털 텍스처 | 대기, 걷기, 달리기, 냄새 맡기 |

새 대기·활공 클립은 프리뷰 및 후속 저작용으로 제공한다. 현재 필드의 새는 공중 배회·도주에 날갯짓을 사용한다. 착지·나뭇가지 앉기는 이번 구현에 포함하지 않는다.

## 조정할 위치

- 프리팹: `Assets/03.Prefabs/Environment/Wildlife/`
- 프로필·클립: `Assets/10.Datas/World/Wildlife/`
- 배치표: `Assets/10.Datas/World/Wildlife/LakeOfLife.wildlife.json`
- 지상 생물 제작표: `Assets/10.Datas/World/Wildlife/GroundSpecies.catalog.json` — 사슴·돼지·늑대의 원본 경로와 최초 생성 기본값. 기존 프로필의 수동 조정은 덮어쓰지 않는다.
- 새 모델: `Assets/08.Models/Wildlife/WoodlandSongbird.fbx`
- 새 텍스처: `Assets/04.Images/Environment/Wildlife/Songbird_Feathers.png`
- 제작 원본: `ArtSource/Wildlife/WoodlandSongbird.blend`, `build_songbird.py`, `generation.json`
- 늑대: `Assets/08.Models/Wildlife/WoodlandWolf.fbx`, `Assets/04.Images/Environment/Wildlife/Wolf_Fur.png`; 제작 원본은 `ArtSource/Wildlife/WoodlandWolf.blend`, `build_wolf.py`, `wolf_generation.json`.

통합 툴 런처에서 `환경 생물`로 검색하면 에셋 생성, 생명의 호수 배치, 연결 검증을 실행할 수 있다. 에셋 생성은 기존 프리팹과 프로필을 덮어쓰지 않는다. 배치는 같은 ID의 개체를 건너뛰며 씬 저장은 별도다. 배치 위치를 조정할 때는 씬 Transform과 재생성 기준인 배치표를 함께 수정한다.

`WildlifeProfileSO`에서 서식 반경, 이동·도주 속도, 감지·진정 거리, 판단 주기, 시뮬레이션 거리를 조정한다. 사슴은 NavMesh의 완전한 경로만 사용한다. 비행 개체는 기준 높이 주변에서 이동하며 장애물 SphereCast를 사용한다. 지형이나 큰 나무 배치를 바꾸면 비행 서식 범위도 다시 확인한다.

`WildlifeHabitat`가 필드 전체를 한 번에 갱신한다. 개별 생물에는 Update가 없다. 전투 액터 서비스를 확장하지 않고 `IActorQueryService.PlayerTransform`을 읽는다. 원거리에서는 NavMesh 이동, Animator와 Animancer 그래프를 정지하고 다시 가까워지면 이어서 재생한다. 활성/비활성 전환 시 서식 중심은 최초 배치 위치를 유지한다.

지상 생물의 `NavMeshAgent.agentTypeID`는 필드가 베이크한 기본 타입 `0`을 사용한다. 사슴·돼지 원본 패키지의 전용 Agent Type은 이 프로젝트에 등록되어 있지 않아, 그대로 복제하면 지면 위에 있어도 런타임 NavMesh에 연결되지 않는다. 제작 도구는 기본 타입을 지정하고, 배치·연결 검증은 각 개체의 Agent Type과 Area Mask로 접지 여부를 확인한다. 타입을 지정하지 않는 `NavMesh.SamplePosition` 검사만으로는 이 오류를 발견할 수 없다.

배회 정지 수정 확인: 실행 중 사슴 6마리·돼지 3마리의 기존 타입으로는 접지 조회가 모두 실패했고, 기본 타입으로는 9마리 모두 성공했다. 수정 적용 후 플레이어 주변 사슴 4마리·돼지 3마리가 약 26초 동안 시작점에서 1.4~3.5m 이동하고 걷기 클립을 재생했다. 원거리 사슴 2마리는 기존 거리 제한에 따라 정지했다. 이 결과는 실행 상태 계측이며 발 미끄러짐·도주·재진입의 사용자 직접 확인을 대체하지 않는다.

수정 후 Unity 컴파일과 프리팹 8개·씬 개체 30개의 연결 검증이 통과했다. 편집 모드에서 개체 하나에 잘못된 Agent Type을 임시 지정했을 때 접지 오류를 검출했고, 원복 후 재검증도 통과했다. 검사 씬은 저장하지 않았다. 결과: `output/WildlifeRoamingFix/validation.txt`.

나비 원본 클립의 이동·출몰용 크기 변화는 복제 클립에서 고정했다. 원본 패키지는 수정하지 않았다. 새의 좌우 날개 본은 서로 반대 방향의 로컬 축을 가지므로 회전값에 좌우 부호를 다시 곱하면 비대칭 날갯짓이 된다.

## 수행한 보조 검사

- Unity 컴파일 및 연결 검증: 프리팹 8개, 배치 30개, Missing Script·기본 클립·머티리얼·셰이더·Collider/전투 액터 혼입·NavMesh 접지 오류 0.
- 늑대·돼지 신규 모션 8개를 8시점씩 총 64회 샘플링하여 크기·지면 침투·루프·루트 이동을 검사했다. 늑대 달리기의 발끝 최저점은 평지 기준 -3.7cm이며 실제 발 미끄러짐은 W07에서 확인한다.
- 신규 6개체의 8방향 목적지 중 늑대 24/24, 돼지 19/24가 완전한 경로로 연결된다. 돼지 주변 건물로 차단된 후보는 런타임에서 거절한다. 근거: `output/Wildlife/wolf_pig_clips.txt`, `wolf_pig_routes.txt`.
- 사슴 6개 × 주변 8방향 = 48개 목적지의 완전한 NavMesh 경로 확인.
- 비행 이동, 서식 범위, 원거리 정지, 비활성/활성 복귀를 Editor 코드 호출로 검사. 실제 Play Mode 또는 플레이 감각 검증과 구분한다.
- 6개 프리팹의 이동 클립을 각각 8시점에서 샘플링하여 메시 크기 폭주·NaN을 검사하고 Unity 화면에서 외형 확인.
- 로그와 화면: `output/Wildlife/`. 미커밋 씬 보존 사본 `LakeOfLifeScenario.before.unity`에는 사용자 변경이 포함되므로 임의 삭제하지 않는다.

## 사용자 직접 테스트 대기

준비: Unity 6000.3.21f1 이상에서 기존 Boot 실행 경로로 생명의 호수에 진입한다. 저장 검증은 별도 테스트 슬롯을 사용하고 기존 진행 슬롯을 초기화하지 않는다. 사용한 키보드·게임패드를 결과에 적는다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| W01 | 입구 `(1078, 60, 437)`과 약초 길 `(1042, 62, 463)`에서 30초 관찰 | 사슴이 먹이를 먹고 배회하며 걸음과 이동이 맞는다. 지면 침수·발 미끄러짐·급격한 회전은 실패 |
| W02 | 사슴에 6m 안쪽까지 접근한 뒤 12m 이상 물러남 | 가까워지면 달려 피하고 멀어지면 진정한다. 물·절벽 통과, 경계에서 지속적인 떨림은 실패 |
| W03 | 꽃밭 `(1034, 82, 698)`, `(1058, 82, 710)` 관찰 | 작은 나비가 날갯짓하며 배회한다. 크기 폭주·주기적 소멸·셰이더 오류 분홍색은 실패 |
| W04 | 입구 및 호수 접근부 위쪽의 새 관찰 | 양 날개가 함께 움직이며 배회한다. 나무·건물에 계속 갇히거나 관통하면 실패 |
| W05 | 나비에서 45m, 사슴·새에서 90m 이상 멀어졌다가 복귀 | 가까워지면 움직임과 애니메이션 재개. 멈춘 자세 유지·순간이동·복귀 오류는 실패 |
| W06 | 생물 옆을 지나가며 락온, 필드 재진입, 일시정지·해제 | 통행과 락온을 방해하지 않고 재진입 시 30개체 유지. 일시정지 중 이동하지 않고 재개 후 정상 동작 |
| W07 | 늑대 `(946, 81, 617)` 8m 안쪽 접근 후 15m 이상 물러남 | 냄새 맡기·걷기·도주·진정이 이어진다. 네 발의 움직임, 발 미끄러짐, 나무 관통 여부를 확인한다. 환경용 늑대이므로 공격하지 않는다 |
| W08 | 돼지 `(1050, 60, 407)` 3m 안쪽 접근 후 7m 이상 물러남 | 먹이 먹기·걷기·도주·진정이 이어진다. 건물이나 길을 통과하지 않고 경로를 다시 선택한다 |

실패 시 항목 ID, 시작 위치, 실제 증상, 입력 장치와 Console 오류를 전달한다. 화면 문제는 가능하면 짧은 영상을 함께 남긴다. 회신 형식: `ID / 통과·실패·미수행 / 실제 결과 / 첨부 또는 로그`.

배회 정지 수정의 Editor Play Mode 확인은 위 실행 상태 계측 범위까지 수행했다. W01~W08의 전체 사용자 직접 테스트·Player 빌드·실기기 성능 측정은 미수행이다. 기존 모듈 경계나 직렬화 타입 이동, 빌드/Addressables 구성 변경은 없다. 이 검사는 구현 연결 확인이며 전체 게임 통합·출시 검증을 대신하지 않는다.

## ?? ?? ?? ??

- ? 6??: ?? ?? ??? ????? ?? ?? ? ?? ? ????? ? ?? ?? ?? ? ???? Editor API? ?? ????. ?? ?? ?? ??. `output/WildlifeBehavior/bird_checks.txt`.
- ??: ?? Windows Development Player?? ?? NavMeshAgent? ??? ??? ??? ????. `boot`, `deer-flee`, `deer-return` ?? ??. `output/WildlifeBehavior/Player/checks.txt`.
- ??: BT JSON ?? ?? ????? 0, ?? ??? Hitbox ??? ?? ??. ?? ? `MonsterAbilitySetIntegrationTests` 8? ??. ?? Ability ??? ?? ?? 7?? ?? ? ????? ????? ?? ???? ???. `output/WildlifeBehavior/ability_errors.txt`.
- Windows Development ??: Unity 6000.3.21f1, ?? 0, ?? 83, ? 252?. `output/WildlifeBehavior/build.txt`. ??? ????? ??? ????. ?? ?? ???? ??? ???? ????.
- ? Player ???? ??? ?? ?????????? ??? ?? ??? ??? ????. ?? ?? ????? ?? ?? ??? ? ??? ?? NullReferenceException? ???? ?? ??? timeout?? ????. ????? ??? ??? ?????, ?? ? ???? ?? ?? ????? ??? ?? ?? ??? ??? ???. `output/WildlifeBehavior/Player/errors.txt`, `Player.log`.
- ?? ?? ??, ??? ?? ?? ?????? ????, ?? ?? ????? ??? W04?W07 ??? ??? ??? ?? ??. ?? ?? ??? ??? ?? ??? ???? ???.

?? ??? ?? ? `dotnet build Assembly-CSharp.csproj --no-restore` ?? ???? ?? 0?? ????. Unity ??? ?? ???? Editor ???? ???? ???. ??? ?? ?? ? 4.36GB? ?? ??? ?? ? 144MB? ????, ??? ????? ?? ???? ??? ???. ???? ?? ??: `output/WildlifeBehavior/build_player.cs`.
