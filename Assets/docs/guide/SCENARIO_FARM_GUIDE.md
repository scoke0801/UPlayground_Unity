# 시나리오 농장 배치와 검증

## 배치

- 씬: `Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity`
- Hierarchy: `ScenarioFarm`. 마을 서쪽, 월드 좌표 중심 약 `(981, 62, 402)`.
- 밭 4개, 당근 12곳·호박 12곳, 고정 작업 주민 2명.
- Blender MCP로 만든 밭고랑을 지형에 맞춰 베이크했다. 원본은 `ArtSource/Farming/CultivatedBed.blend`, Unity 모델은 `Assets/08.Models/Farming/CultivatedBed.fbx`다.
- 흙 알베도는 ImageGen으로 제작했다. 작물·농기구는 기존 Farmlands 리소스, 주민은 기존 P09 주민 프리팹을 바탕으로 한 별도 Variant다. 기존 주민의 ID·일과·위치는 변경하지 않았다.
- 밭과 겹치는 `Forest_Entrance_122`, `Shrubs_019/113/127`, `RockDebris_030`, `Forest_Floor_145`와 일부 풀 렌더러를 비활성화했다. 기존 NavMesh GUID를 보존하여 다시 베이크했다.

## 조정할 데이터

`Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Farming/`에서 편집한다.

| 에셋 | 설정 |
|---|---|
| `Harvest_Carrot`, `Harvest_Pumpkin` | 손 채집, 1회 수확에 1개, `regrowGameMinutes = 360` |
| `Farm_Carrot` | 아이템 ID 200021, 체력 10 회복, 무게 0.2 |
| `Farm_Pumpkin` | 아이템 ID 200022, 체력 20 회복, 무게 0.6 |
| `Farm_Tending_0/1` | 기존 Humanoid Gathering 클립, LoopSelf 반복, 속도 0.9/0.8 |
| `FarmWorker_0/1_Definition` | 별도 Actor ID `scenario.farm.worker.0/1`, ActorDatabase 등록 |

수확 아이템은 ItemDatabase에 등록했으며 기존 획득 알림·인벤토리·소모품 사용 경로를 따른다. 주민 작업은 `NpcBrain.WorkMotion → NpcIdleState → ActorAnimator`를 사용한다. 주민이 작물을 소모하거나 플레이어 보상을 선점하지 않는다.

## 저장·재생 경계

- 수확 순간의 누적 게임 분을 `WorldStateSaveData.interactionConsumedMinutes`에 맵/SceneEntityId별로 저장한다.
- 게임 시간 360분 후 해당 지점만 다시 활성화한다. 실제 시간 6시간이나 게임 종료 중의 벽시계 시간은 아니다. 휴식·시간 건너뛰기는 기존 게임 시계를 따른다.
- 몬스터 재생성은 시간제 농작물을 조기 복구하지 않는다. `regrowGameMinutes = 0`인 기존 채집물은 기존 월드 재생성 규칙을 유지한다.
- 씬 재진입과 로드에서 같은 수확 시각을 사용한다. 같은 씬에서 수확 전 저장을 불러오면 작물을 복구한다.
- 소모 시각 필드가 없는 이전 저장은 최초 복원 시각부터 시간을 센다. 새 게임에서는 소모·시간 기록을 함께 비운다. 별도 회차 전용 농장 리셋은 추가하지 않았으며 기존 WorldState 저장 경계를 따른다.

## 수행한 검사

- Unity 6000.3.21f1 컴파일 오류 0.
- `GatheringRegrowthTests`: EditMode 4개 통과. 359분/360분 경계, 재수확, 몬스터 재생성 분리, 수확 전후 저장 복원, 이전 저장 호환, 일반 채집 복구를 검사했다.
- Unity 로드 기준 수확 지점 24개·주민 2명 참조 검사, 고유 수확 ID, 아이템 등록·아이콘·모션 연결, Missing Script 0, 각 수확 지점 주변 NavMesh 검사 통과.
- 근거: `output/ScenarioFarm/AssetValidation.txt`, `output/ScenarioFarm/TestResults.json`.
- 화면은 Editor 배치 검수다. 실제 입력에 의한 수확과 NPC 작업의 런타임 연속 재생은 **사용자 테스트 대기**다. 일반 변경의 빠른 검사 범위로 진행했으며 Windows Player 빌드는 미수행이다. 저장 필드 추가의 Player 저장/로드 확인도 남아 있다.

## 사용자 테스트 명세

기존 저장 슬롯을 덮어쓰지 않는 별도 테스트 슬롯을 준비한다. 기존 Boot 진입 경로로 시나리오 지역에 들어간다. 에디터에서 위 좌표로 이동하거나 `ScenarioFarm`을 찾아 위치를 확인한다. 키보드와 게임패드 각각 HUD에 표시되는 상호작용 입력을 사용한다.

| ID | 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| F01 | 당근과 호박을 각각 수확한다. 같은 자리에서 입력을 반복한다. | 각 1개가 지급되고 작물이 사라진다. 추가 지급·무반응·입력 고착이 없어야 한다. |
| F02 | 수확물을 체력이 줄어든 캐릭터에게 사용한다. | 당근 10·호박 20 회복, 각 1개 소모. 가득 찬 체력에서는 낭비하지 않는다. |
| F03 | 수확 후 게임 시간을 359분, 이어 1분 진행한다. 기존 시간 치트 또는 휴식을 사용할 수 있다. | 359분에는 숨고 360분에 복구된다. 재수확도 1회만 지급된다. |
| F04 | 수확 직후 저장하고 같은 씬에서 로드한다. 이후 지역 이탈·재진입한다. 수확 전 저장도 다시 불러온다. | 수확 후 저장은 남은 시간을 유지하고, 수확 전 저장은 작물을 복원한다. |
| F05 | 주민 2명을 30초 이상 관찰하고 가까이 지나간다. | 땅을 돌보는 동작을 반복하며 제자리로 돌아온다. T포즈 지속·미끄러짐·땅 밑 침하·작물 관통·이동 방해가 없어야 한다. |
| F06 | 게임패드로 밭 사이를 오가며 수확하고 메뉴를 열었다 닫는다. | 가까운 작물을 선택할 수 있고 조작이 복구된다. 기존 일시정지 시간 정책을 따른다. |
| F07 | 후속 Windows Player 검증 시 F01/F04/F05를 반복한다. | 빌드에서 아이콘·농작물·주민이 표시되고 저장/로드·반복 작업이 동일하게 동작한다. |

회신 형식: `항목 ID / 통과·실패·미수행 / 실제 결과 / 입력 장치·시작 슬롯 / 필요 시 화면 또는 Editor.log`.

## 보관 산출물

`output/ScenarioFarm/LakeOfLifeScenario.before.unity`와 `Navigation.before.asset`은 작업 전 미커밋 사용자 변경이 포함된 백업이다. 해당 사용자 변경을 보존하기 위해 남겼으며, 커밋 또는 사용자 확인 전 삭제하지 않는다. 이번 작업은 커밋하지 않았다.
